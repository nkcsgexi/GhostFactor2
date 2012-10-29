#region Copyright (c) 2004 Richard Schneider (Black Hen Limited) 
/*
   Copyright (c) 2004 Richard Schneider (Black Hen Limited) 
   All rights are reserved.

   Permission to use, copy, modify, and distribute this software 
   for any purpose and without any fee is hereby granted, 
   provided this notice is included in its entirety in the 
   documentation and in the source files.
  
   This software and any related documentation is provided "as is" 
   without any warranty of any kind, either express or implied, 
   including, without limitation, the implied warranties of 
   merchantibility or fitness for a particular purpose. The entire 
   risk arising out of use or performance of the software remains 
   with you. 
   
   In no event shall Richard Schneider, Black Hen Limited, or their agents 
   be liable for any cost, loss, or damage incurred due to the 
   use, malfunction, or misuse of the software or the inaccuracy 
   of the documentation associated with the software. 
*/
#endregion

// TODO: thread harvesting

using BenTools.Data;
using System;
using System.Collections;
using System.Threading;


namespace BlackHen.Threading
{
   /// <summary>
   ///   Provides a pool of threads that can be used to run <see cref="IWorkItem">work items</see>.
   /// </summary>
   /// <threadsafety static="true" instance="true" />
   /// <remarks>
   ///   <b>WorkThreadPool</b> manages a pool of worker <see cref="Thread">threads</see>.  
   ///   A <see cref="IWorkQueue">work queue</see> uses the <see cref="BeginWork"/> method 
   ///   to request that a <see cref="IWorkItem">work item</see> be executed by a thread in the
   ///   thread pool.
   ///   <para>
   ///   The <see cref="MinThreads"/> and <see cref="MaxThreads"/> properties specify the minimum and
   ///   maximum number of <b>threads</b> manager by the pool.  
   ///   </para>
   ///   <para>
   ///   The <see cref="Default"/> static property
   ///   returns a <b>WorkThreadPool</b> that is always available.  This instance, is typically used by
   ///   all <b>work queues</b>.
   ///   </para>
   ///   <para>
   ///   The <see cref="Dispose"/> method, performs an orderly shutdown of the <b>threads</b>.  The method
   ///   waits for all working threads to complete, before terminating the thread.
   ///   </para>
   /// </remarks>
   public class WorkThreadPool : IResourcePool, IDisposable
   {
      private int minThreads;
      private int maxThreads;
      private ArrayList workers;
      private int waiters;

      internal HighPriorityQueue workQueue;

      /// <summary>
      ///   A lock when accessing our events.
      /// </summary>
      private readonly object eventLock = new object();

      private static WorkThreadPool defaultThreadPool = null;

      /// <summary>
      ///   Gets the default <see cref="WorkThreadPool"/> that is always available.
      /// </summary>
      /// <remarks>
      ///   Typically, the <b>Default</b> instance is used by all <see cref="IWorkQueue">work queues</see>.
      /// </remarks>
      public static WorkThreadPool Default
      {
         get
         {
            if (defaultThreadPool == null)
            {
               lock (typeof(WorkThreadPool))
               {
                  if (defaultThreadPool == null)
                     defaultThreadPool = new WorkThreadPool();
               }
            }
            return defaultThreadPool;
         }
      }

  
      #region Constructors

      /// <summary>
      ///   Creates a new instance of the <see cref="WorkThreadPool"/> class.
      /// </summary>
      /// <seealso cref="Default"/>
      public WorkThreadPool () : this(1, 25)
      {
      }

      /// <summary>
      ///   Creates a new instance of the <see cref="WorkThreadPool"/> class with the
      ///   specified <see cref="MinThreads"/> and <see cref="MaxThreads"/>.
      /// </summary>
      /// <param name="minThreads">
      ///   The mininum number of threads.
      /// </param>
      /// <param name="maxThreads">
      ///   The maximum number of threads.
      /// </param>
      /// <seealso cref="Default"/>
      public WorkThreadPool (int minThreads, int maxThreads)
      {
         if (0 >= maxThreads)
            throw new ArgumentOutOfRangeException("maxThreads", maxThreads, "Must be greater than zero.");

         workQueue = new HighPriorityQueue();
         workers = new ArrayList(maxThreads);
         
         this.maxThreads = maxThreads;
         MinThreads = minThreads;
      }

      #endregion

      #region IDisposable Members

      /// <summary>
      ///   Terminates the threads of the current WorkThreadPool before it is reclaimed by the garbage collector.
      /// </summary>
      ~WorkThreadPool()
      {
         Dispose(false);
      }


      /// <summary>
      ///   Performs an orderly shutdown of the pooled <b>threads</b>.
      /// </summary>
      /// <exception cref="InvalidOperationException">
      ///   <c>this</c> equals <see cref="Default"/>.
      /// </exception>
      /// <remarks>
      ///   <b>Dispose</b> performs an orderly shutdown of the <b>threads</b>.  The method
      ///   waits for each working thread to complete, before terminating the thread.
      /// </remarks>
      public void Dispose()
      {
         if (this == defaultThreadPool)
            throw new InvalidOperationException("The Default WorkThreadPool can not be disposed.");

         Dispose(true);
         GC.SuppressFinalize(this);
      }

      private void Dispose(bool disposing)
      {
         if (workers == null)
            return;

         // Tell all workers to shutdown.
         lock (this)
         {
            foreach (WorkThread worker in workers)
            {
               worker.Stop();
            }
            Monitor.PulseAll(this);
         }

         // Wait for the worker threads to quit.
         while (workers.Count > 0)
         {
            Thread thread = null;
            lock (this)
            {
               if (workers.Count > 0)
               {
                  thread = DeleteThread();
                  Monitor.PulseAll(this);
               }
            }
            if (thread != null)
               thread.Join();
         }
         workers = null;
      }
      #endregion

      #region Properties
      /// <summary>
      ///   Gets or sets the number of idle threads the ThreadPool maintains in anticipation of new requests.
      /// </summary>
      /// <value>
      ///   The minimum number of worker threads in the thread pool.
      /// </value>
      /// <exception cref="ArgumentOutOfRangeException">
      ///   When setting and <i>value</i> is less than zero or greater than <see cref="MaxThreads"/>.
      /// </exception>
      /// <remarks>
      ///   <b>MinThreads</b> is the minimum number of idle threads maintained by the thread pool in order to reduce
      ///   the time required to satisfy requests for thread pool threads. Idle threads in excess of the 
      ///   minimum can be terminated, to save system resources. 
      /// </remarks>
      public int MinThreads
      {
         get
         {
            return minThreads;
         }
         set
         {
            if (value < 0)
               throw new ArgumentOutOfRangeException("MinThreads", value, "Must be positive or zero.");
            if (value > MaxThreads)
               throw new ArgumentOutOfRangeException("MinThreads", value, "Must be less than MaxThreads.");

            minThreads = value;
            lock (this)
            {
               while (workers.Count < minThreads)
                  CreateThread();
            }
         }
      }

      /// <summary>
      ///   Gets or sets the number of requests to the thread pool that can be active concurrently.
      /// </summary>
      /// <value>
      ///   The maximum number of worker threads in the thread pool.
      /// </value>
      /// <exception cref="ArgumentOutOfRangeException">
      ///   When setting and <i>value</i> is less than or equal to zero.
      /// </exception>
      /// <remarks>
      ///   <b>MaxThreads</b> is the number of <see cref="BeginWork">requests</see> to the thread pool that can
      ///   be active concurrently.  All requests above the number remain queued until a thread pool thread
      ///   become available.
      ///   <para>
      ///   When setting and <i>value</i> is less than the current <b>MaxThreads</b>, the appropiate number
      ///   of threads will be deleted.
      ///   </para>
      /// </remarks>
      public int MaxThreads
      {
         get
         {
            return maxThreads;
         }
         set
         {
            if (0 >= value)
               throw new ArgumentOutOfRangeException("MinThreads", value, "Must be greater than zero.");

            maxThreads = value;
            lock (this)
            {
               while (workers.Count > maxThreads)
                  DeleteThread();
            }
            if (MinThreads > maxThreads)
               MinThreads = maxThreads;
         }
      }
      #endregion

      #region WorkThread management
      private void CreateThread()
      {
         WorkThread worker = new WorkThread(this);
         workers.Add(worker);

         Thread thread = new Thread(new ThreadStart(worker.Start));
         thread.Name = "WT #" + workers.Count;
         thread.IsBackground = true;

         worker.Thread = thread;
         thread.Start();
      }

      private Thread DeleteThread()
      {
         int i = workers.Count - 1;
         WorkThread worker = (WorkThread) workers[i];

         worker.Stop();
         workers.RemoveAt(i);

         return worker.Thread;
      }
      #endregion

      #region IThreadPool Members

      /// <summary>
      ///   Requests that an <see cref="IWorkItem">work item</see> is run on a <see cref="Thread"/>.
      /// </summary>
      /// <param name="workItem">
      ///   The <see cref="IWorkItem"/> to execute.
      /// </param>
      /// <remarks>
      ///   <b>BeginWork</b> queues the <paramref name="workItem"/> for execution.  When a <see cref="Thread"/> in the pool
      ///   becomes available, the <see cref="IWorkItem.State"/> of the <paramref name="workItem"/>
      ///   is set to <see cref="WorkItem.State">Running</see>
      ///   and its <see cref="IWork.Perform"/> method is invoked.
      /// </remarks>
      public void BeginWork(IWorkItem workItem)
      {
         if (workItem == null)
            throw new ArgumentNullException();

         lock (this)
         {
            // Queue the work.
            workQueue.Push(workItem);

            // If all workers are busy, then create a thread if the limit
            // is not reached.
            if (waiters == 0 && workers.Count < MaxThreads)
               CreateThread();

            // Wakeup a worker.
            Monitor.Pulse(this);
         }
      }

      #endregion

      #region Raised Events
      /// <summary>
      ///   Occurs when an untrapped thread exception is thrown.
      /// </summary>
      /// <remarks>
      ///   The <b>ThreadException</b> event occurs when 
      ///   an <see cref="Exception"/> is thrown in a thread outside of the 
      ///   <see cref="IWork.Perform">IWork.Perform</see> method.
      /// </remarks>
      public event ResourceExceptionEventHandler ThreadException
      {
         add
         {
            lock (eventLock)
            {
               threadException += value;
            }
         }
         remove
         {
            lock (eventLock)
            {
               threadException -= value;
            }
         }
      }
      private event ResourceExceptionEventHandler threadException;
      /// <summary>
      ///   Raises the <see cref="ThreadException"/> event.
      /// </summary>
      /// <param name="e">
      ///   A <see cref="ResourceExceptionEventArgs"/> that contains the event data.
      /// </param>
      /// <remarks>
      ///   The <b>OnThreadException</b> method allows derived classes to handle the <see cref="ThreadException"/>
      ///   event without attaching a delegate. This is the preferred technique for handling the event in a derived class.
      ///   <para>
      ///   When a derived class calls the <b>OnThreadException</b> method, it raises the <see cref="ThreadException"/> event by 
      ///   invoking the event handler through a delegate. For more information, see 
      ///   <a href="ms-help://MS.VSCC.2003/MS.MSDNQTR.2004JAN.1033/cpguide/html/cpconProvidingEventFunctionality.htm">Raising an Event</a>.
      ///   </para>
      /// </remarks>
      protected virtual void OnThreadException(ResourceExceptionEventArgs e)
      {
         ResourceExceptionEventHandler handler;

         lock (eventLock)
         {
            handler = threadException;
         }
         if (handler != null)
            handler(this, e);
      }
      #endregion

      #region WorkThread class
      private class WorkThread
      {
         private WorkThreadPool threadPool;
         private volatile bool stopping;
         private Thread thread;

         public WorkThread (WorkThreadPool threadPool)
         {
            this.threadPool = threadPool;
         }

         public Thread Thread
         {
            get {return thread;}
            set {thread = value;}
         }

         public void Start()
         {
            restart:
            try
            {
               while (!stopping)
               {
                  IWorkItem work = GetWork();

                  // Perform the work.
                  if (work != null)
                     DoWork(work);

                  // Yield to other threads, including the User Interface (if any).
                  Thread.Sleep(0);
               }
            }
            catch (ThreadAbortException)
            {
               // Abort nicely.
               Stop();
               Thread.ResetAbort();
            }
            catch (Exception e)
            {
               // This should not happen!!!
               threadPool.OnThreadException(new ResourceExceptionEventArgs(this, e));
               goto restart;
            }
         }

         public IWorkItem GetWork()
         {
            // Get some work
            lock (threadPool)
            {
               if (stopping)
                  return null;

               if (threadPool.workQueue.Count == 0)
               {
                  ++threadPool.waiters;
                  Monitor.Wait(threadPool);
                  --threadPool.waiters;
               }

               if (stopping)
                  return null;
               if (threadPool.workQueue.Count > 0)
                  return (IWorkItem) threadPool.workQueue.Pop();
            }

            return null;
         }

         public void DoWork (IWorkItem workItem)
         {
            ThreadPriority originalPriority = Thread.CurrentThread.Priority;

            try
            {
               if (workItem.Priority != originalPriority)
                  Thread.CurrentThread.Priority = workItem.Priority;

               workItem.State = WorkItemState.Running;
               try
               {
                  workItem.Perform();
               }
               catch (Exception e)
               {
                  workItem.FailedException = e;
                  workItem.State = WorkItemState.Failing;
               }
               workItem.State = WorkItemState.Completed;
            }
            catch (Exception e)
            {
               // If no work queue for the item, then let the WorkThreadPool raise
               // the exception event.
               if (workItem == null || workItem.WorkQueue == null)
                  throw;

               workItem.WorkQueue.HandleResourceException(new ResourceExceptionEventArgs(this, workItem, e));
            }
            finally
            {
               if (Thread.CurrentThread.Priority != originalPriority)
                  Thread.CurrentThread.Priority = originalPriority;
            }
         }

         public void Stop()
         {
            stopping = true;
         }
      }
      #endregion

   }
}
