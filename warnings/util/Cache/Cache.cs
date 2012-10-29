#region Using directives

using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace warnings.util
{
	/// <summary>
	/// Interface definition for the <c>ICacheable</c> items.
	/// </summary>
	public interface ICacheable
	{
		int BytesUsed { get; }
	}

	/// <summary>
	/// Class definition for a cacheable collection of items.  Specifically, 
	/// a maximum size for the cache may be defined and objects added will 
	/// be kept-alive whilst there is sufficient cache space.  Once an item
	/// has be pushed out of the cache, it is subject to the normal GC conditions.
	/// </summary>
	/// <typeparam name="Key">Type of the identifier to use.</typeparam>
	/// <typeparam name="Value">Type of the values to be stored.</typeparam>
	/// <remarks>Some limitations have been made on the types used in this
	/// generic.  Mainly, the <c>Key</c> must implement the <c>IComparable</c>
	/// interface and the <c>Value</c> must implement the <c>ICacheable</c>
	/// interface.</remarks>
	public class Cache<Key,Value> 
		where Key: IComparable<Key>
		where Value: ICacheable
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public Cache()	{}

		/// <summary>
		/// Alternative constructor allowing maximum cache size to be specified
		/// at construction time.
		/// </summary>
		/// <param name="maxBytes">Maximum size of the cache.</param>
		public Cache(int maxBytes)	
		{
			this.maxBytes = maxBytes;
		}

		/// <summary>
		/// The <c>maxBytes</c> variable stores the maximum size of
		/// the item cache.  This is the sum of all of the items.  Once
		/// this size is breached.  The cache starts dropping out items.
		/// If they are reconstructable, then the items are destroyed but
		/// left in the cache allowing them to come alive again.
		/// </summary>
		private int maxBytes = 1024 * 1024;
		public int MaxBytes
		{
			get { return maxBytes; }
			set { maxBytes = value; }
		}

		/// <summary>
		/// The private container representing the cache.
		/// </summary>
		private Dictionary<Key, Value> cacheStore = null;

		/// <summary>
		/// The private queue maintaining a basic order of items
		/// for purging when space necessitates.
		/// </summary>
		private Queue<Key> arrivalOrder = null;

		/// <summary>
		/// Returns the current usage of the cache.
		/// </summary>
		/// <value>Current cache's managed size.</value>
		/// <remarks>The cache is at the mercy of the <c>ICacheable</c> items
		/// returning their true size.</remarks>
		public int CurrentCacheUsage
		{
			get
			{
				int size = 0;
				if (cacheStore != null)
				{
					foreach( KeyValuePair<Key,Value> c in cacheStore )
					{
						size += (c.Value as ICacheable).BytesUsed;
					}
				}
				return size;
			}
		}

		/// <summary>
		/// This function is used when it has been determined that the cache is
		/// too full to fit the new item.  The function is called with the parameter
		/// of the number of bytes needed.  A basic purging algorthim is used to make
		/// space.
		/// </summary>
		/// <param name="bytes">Number of bytes needed within the cache.</param>
		/// <remarks>This purge function may be improved with some hit-count being
		/// maintained.</remarks>
		private void PurgeSpace(int bytes)
		{
			// Purge space using a slight modification of the first-in-first-out system.
			// Basically, the first item added (the oldest) will be the first removed, however,
			// any item accessed is given a "touch" and moved to the end of the queue.
			// This should ensure items in use stay within the cache (unless the cache is too
			// small).
			if (cacheStore != null && 
				 arrivalOrder != null && 
				 cacheStore.Count != 0)
			{
				int purged = 0;
				while (MaxBytes <= (CurrentCacheUsage + bytes) && arrivalOrder.Count > 0 )
				{
					Key k = arrivalOrder.Dequeue();
					int freeing = cacheStore[k].BytesUsed;
					purged += freeing;

					System.Diagnostics.Debug.WriteLine(
						string.Format("{0} being made available for GC freeing {1} bytes in the cache", 
						k, freeing));

					cacheStore.Remove(k);
				}
			}
		}

		/// <summary>
		/// The internal function to store the items within the cache.
		/// </summary>
		/// <param name="k">Identifier or key for the item.</param>
		/// <param name="v">The actual item to store/cache.</param>
		private void StoreItem(Key k, Value v)
		{
			if (cacheStore == null)
			{
				// Create the stores.
				cacheStore = new Dictionary<Key, Value>();
				arrivalOrder = new Queue<Key>();
			}
			arrivalOrder.Enqueue(k);
			cacheStore.Add(k, v);
		}

		/// <summary>
		/// Add a new item into the cache.
		/// </summary>
		/// <param name="k">Identifier or key for item to add.</param>
		/// <param name="v">Actual item to store.</param>
		public void Add(Key k, Value v)
      {
			// Check if we're using this yet
			if (ContainsKey(k))
			{
				// Simple replacement by removing and adding again, this
				// will ensure we do the size calculation in only one place.
				Remove(k);
			}

			// Need to get current total size and see if this will fit.
			int projectedUsage = v.BytesUsed + this.CurrentCacheUsage;
			if (projectedUsage > maxBytes)
			{
				System.Diagnostics.Debug.WriteLine(string.Format("Need to make space for {0} bytes, currently using {1}", v.BytesUsed, CurrentCacheUsage));
				PurgeSpace(v.BytesUsed);
			}

			// Store this value now..
			StoreItem(k, v);
		}

		/// <summary>
		/// Remove the specified item from the cache.
		/// </summary>
		/// <param name="k">Identifier for the item to remove.</param>
		public void Remove(Key k)
		{
			if ( ContainsKey(k) )
			{
				RemoveKeyFromQueue(k);
				cacheStore.Remove(k);
			}
		}

		/// <summary>
		/// Internal function to dequeue a specified value.
		/// </summary>
		/// <param name="k">Identifier of item to remove.</param>
		/// <remarks>In worst case senarios, a new queue needs to be rebuilt.  
		/// Perhaps a List acting like a queue would work better.</remarks>
		private void RemoveKeyFromQueue(Key k)
		{
			if (arrivalOrder.Contains(k))
			{
				if (arrivalOrder.Peek().CompareTo(k) == 0)
					arrivalOrder.Dequeue();
				else
				{
					Queue<Key> tempQueue = new Queue<Key>();
					int oldQueueSize = arrivalOrder.Count;
					while (arrivalOrder.Count > 0)
					{
						Key tempValue = arrivalOrder.Dequeue();

						if (tempValue.CompareTo(k) != 0)
							tempQueue.Enqueue(tempValue);
					}
					arrivalOrder = tempQueue;
				}
			}
		}

		/// <summary>
		/// Touch or refresh a specified item.  This allows the specified
		/// item to be moved to the end of the dispose queue.  E.g. when it
		/// is known that this item would benifit from not being purged.
		/// </summary>
		/// <param name="k">Identifier of item to touch.</param>
		public void Touch(Key k)
		{
			RemoveKeyFromQueue(k);
			arrivalOrder.Enqueue(k);   // Put at end of queue.
		}

		/// <summary>
		/// Returns the item associated with the supplied identifier.
		/// </summary>
		/// <param name="k">Identifier for the value to be returned.</param>
		/// <returns>Item value corresponding to Key supplied.</returns>
		/// <remarks>Accessing a stored item in this way automatically
		/// forces the item to the end of the purge queue.</remarks>
		public Value GetValue(Key k)
		{
			if (cacheStore != null && cacheStore.ContainsKey(k))
			{
				Touch(k);
				return cacheStore[k];
			}
			else
				return default(Value);
		}

		/// <summary>
		/// Determines whether the cache contains the specific key.
		/// </summary>
		/// <param name="k">Key to locate in the cache.</param>
		/// <returns><c>true</c> if the cache contains the specified key; 
		/// otherwise <c>false</c>.</returns>
		public bool ContainsKey(Key k)
		{
			return (cacheStore != null && cacheStore.ContainsKey(k));
		}

		/// <summary>
		/// Indexer into the cache using the associated key to specify
		/// the value to return.
		/// </summary>
		/// <param name="k">Key identifying value to return.</param>
		/// <returns>The value asspciated to the supplied key.</returns>
		public Value this[Key k] { get { return GetValue(k); } }
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection of
		/// keys.
		/// </summary>
		/// <returns>The enumerator for keys.</returns>
		public IEnumerable<Key> GetKeys()
		{
			foreach (Key k in arrivalOrder)
				yield return k;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection of
		/// values within the cache.
		/// </summary>
		/// <returns>The enumerator for the values.</returns>
		public IEnumerable<Value> GetValues()
		{
			foreach (KeyValuePair<Key, Value> i in cacheStore)
				yield return i.Value;
		}

		/// <summary>
		/// Returns the <c>KeyValuePair&lt;Key,Value&gt;</c> for the cache 
		/// collection.
		/// </summary>
		/// <returns>The enumerator for the cache, returning both the
		/// key and the value as a pair.</returns>
		/// <remarks>The return value from this function can be 
		/// thought of as being like the C++ Standard Template 
		/// Library's std::pair template.</remarks>
		public IEnumerable<KeyValuePair<Key, Value>> GetItems()
		{
			foreach (KeyValuePair<Key, Value> i in cacheStore)
				yield return i;
		}

		/// <summary>
		/// The default enumerator for the cache collection.  Returns an enumerator allowing the traversing of the values, much like the <see cref="GetValues"/>.
		/// </summary>
		/// <returns>The enumerator for the values.</returns>
		public IEnumerator<Value> GetEnumerator()
		{
			foreach ( KeyValuePair<Key,Value> i in cacheStore)
				yield return i.Value;
		}

		/// <summary>
		/// Gets the number of items stored in the cache.
		/// </summary>
		/// <value>The number of items stored in the cache. </value>
		public int Count 
		{ 
			get { return (cacheStore == null) ? 0 : cacheStore.Count; } 
		}

		/// <summary>
		/// Empties the cache of all items.
		/// </summary>
		public void PurgeAll()
		{
			System.Diagnostics.Debug.WriteLine(string.Format("Purging cached collection of {0} items",cacheStore.Count));
			arrivalOrder.Clear();
			cacheStore.Clear();
		}
	}
}
