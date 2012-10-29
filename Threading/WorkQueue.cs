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

using BenTools.Data;
using System;
using System.Threading;

namespace BlackHen.Threading
{
	/// <summary>
	///   Allows concurrent execution of <see cref="WorkItem">work items</see>.
	/// </summary>
   /// <threadsafety static="true" instance="true" />
   /// <remarks>
	///   <b>WorkQueue</b> manages a queue of work that is to be performed.  A <see cref="WorkItem"/>
	///   is scheduled for execution by calling the <see cref="Add"/> method.
   ///   If the <see cref="ConcurrentLimit"/> is not reached, then the <b>WorkItem</b>
   ///   is immediately executed on the <see cref="WorkerPool"/>.  Otherwise it is placed in a
   ///   holding queue and is executed when another <b>WorkItem</b> has <see cref="CompletedWorkItem">completed</see>.
   ///   <para>
   ///   The holding queue is prioritised.  The <b>WorkItem</b> with the highest valued <see cref="WorkItem.Priority"/>
   ///   is the next item to execute.
   ///   </para>
   ///   <para>
   ///   The <b>WorkQueue</b> can be controlled with the <see cref="Pause"/>, <see cref="Resume"/> and
   ///   <see cref="Clear"/> methods.  The <see cref="AllWorkCompleted"/> event or the <see cref="WaitAll"/>
   ///   method can be used to determine when the last <b>WorkItem</b> has completed.
   ///   </para>
   ///   <para>
   ///   The <see cref="RunningWorkItem"/>, <see cref="FailedWorkItem"/> and <see cref="CompletedWorkItem"/>
   ///   events is used to monitor the specific stages of a work item. 
   ///   The <see cref="ChangedWorkItemState"/> event is used to monitor all <see cref="IWorkItem.State"/>
   ///   transitions.
   ///   </para>
   ///   <para>
   ///   The <see cref="WorkerException"/> event is raised when the <see cref="WorkerPool"/> throws an 
   ///   <see cref="Exception"/> that is not related to the <b>work item</b>.  
   ///   After this event is raised the <b>WorkQueue</b> is in an inconsistent state and should
   ///   not be used again.
   ///   </para>
   /// </remarks>
	public class WorkQueue : IWorkQueue
	{
      private HighPriorityQueue queue;
      private IResourcePool resourcePool;
      private object completed = new object();
      private int concurrentLimit = 4;
      private bool pausing;

      private int runningItems;
      private volatile Exception internalException;

      /// <summary>
      ///   A lock when accessing our events.
      /// </summary>
      private readonly object eventLock = new object();

      #region Constructors

      /// <summary>
      ///   Creates a new instance of the <see cref="WorkQueue"/> class.
      /// </summary>
		public WorkQueue()
		{
         queue = new HighPriorityQueue();
		}
      #endregion

      #region Properties

      /// <summary>
      ///   Gets or sets the <see cref="IResourcePool"/> that performs the <see cref="IWorkItem"/>.
      /// </summary>
      /// <value>
      ///   An object that implements <see cref="IResourcePool"/>.  The default is <see cref="WorkThreadPool.Default"/>.
      /// </value>
      /// <remarks>
      ///   The <b>WorkerPool</b> allocates "workers" to perform an <see cref="IWorkItem"/>.
      ///   When the <see cref="WorkItem.State"/> of a <b>work item</b> becomes
      ///   <see cref="WorkItemState">Scheduled</see>, the <see cref="IResourcePool.BeginWork"/>
      ///   method of the <b>WorkerPool</b> is called.
      /// </remarks>
      public IResourcePool WorkerPool
      {
         get
         {
            if (resourcePool == null)
            {
               resourcePool = WorkThreadPool.Default;
            }
            return resourcePool;
         }
         set
         {
            resourcePool = value;
         }
      }

      /// <summary>
      ///   Gets the number of items that are waiting or running.
      /// </summary>
      /// <value>
      ///   The number of items that are waiting or running.
      /// </value>
      public int Count
      {
         get {return runningItems + queue.Count;}
      }

      #endregion

      #region Scheduling
      /// <summary>
      ///   Add some work to execute.
      /// </summary>
      /// <param name="workItem">
      ///   An <see cref="IWorkItem"/> to execute.
      /// </param>
      /// <remarks>
      ///   If the <see cref="ConcurrentLimit"/> is not reached and not <see cref="Pause">pausing</see>, 
      ///   then the <paramref name="workItem"/>
      ///   is immediately executed on the <see cref="WorkerPool"/>.  Otherwise it is placed in a
      ///   holding queue and executed when another <see cref="IWorkItem"/> completes.
      /// </remarks>
      public void Add(IWorkItem workItem)
      {
         if (workItem == null)
            throw new ArgumentNullException("workItem");
         if (internalException != null)
            throw new NotSupportedException("WorkQueue encountered an internal error.", internalException);

         // Assign it to this queue.
         workItem.WorkQueue = this;

         // Can we schedule it for execution now?
         lock (this)
         {
            if (!pausing && runningItems < ConcurrentLimit)
            {
               workItem.State = WorkItemState.Scheduled;
            }
            else
            {
               // Add the workitem to queue.
               queue.Push(workItem);
               workItem.State = WorkItemState.Queued;
            }
         }
      }

      private bool DoNextWorkItem()
      {
         lock(this)
         {
            // Get some work and start it.
            if (!pausing && runningItems < ConcurrentLimit && queue.Count != 0)
            {
               IWorkItem item = (IWorkItem) queue.Pop();
               item.State = WorkItemState.Scheduled;
               return true;
            }
         }

         return false;
      }

      #endregion

      #region Queue Control
      /// <summary>
      ///   Gets or sets the limit on concurrently running <see cref="WorkItem">work items</see>.
      /// </summary>
      /// <value>
      ///   An <see cref="int"/>.  The default value is 4.
      /// </value>
      /// <remarks>
      ///   <b>ConcurrentLimit</b> is the maximum number of <see cref="WorkItem">work items</see>
      ///   that can be concurrently execution.
      /// </remarks>
      public int ConcurrentLimit
      {
         get 
         {
            return concurrentLimit;
         }
         set 
         {
            concurrentLimit = value;
         }
      }

      /// <summary>
      ///   Stop executing <see cref="WorkItem">work items</see>.
      /// </summary>
      /// <seealso cref="Resume"/>
      /// <remarks>
      ///   <b>Pause</b> inhibits the <see cref="Add"/> method from immediately executing a <see cref="WorkItem"/>.
      ///   However, work items that are already executing will continue to completion.
      ///   <para>
      ///   Calling <b>Pause</b> is equivalent to setting the <see cref="Pausing"/> property to <b>true</b>.
      ///   </para>
      /// </remarks>
      /// <seealso cref="Resume"/>
      public void Pause()
      {
         Pausing = true;
      }

      /// <summary>
      ///   Resume executing the <see cref="WorkItem">work items</see>.
      /// </summary>
      /// <remarks>
      ///   <para>
      ///   Calling <b>Resume</b> is equivalent to setting the <see cref="Pausing"/> property to <b>false</b>.
      ///   </para>
      /// </remarks>
      /// <seealso cref="Pause"/>
      public void Resume()
      {
         Pausing = false;
      }

      /// <summary>
      ///   Removes any <see cref="WorkItem"/> that is queued to execute.
      /// </summary>
      /// <remarks>
      ///   <b>Clear</b> removes any <see cref="WorkItem"/> that is queued to execute.
      ///   However, work items that are already executing will continue 
      ///   to completion.
      /// </remarks>
      public void Clear()
      {
         lock (this)
         {
            queue.Clear();
         }
      }


      /// <summary>
      ///   Determines if a <see cref="WorkItem"/> is only queued for execution.
      /// </summary>
      /// <value>
      ///   <b>true</b> if a <see cref="WorkItem"/> is only queued; otherwise, <b>false</b>
      ///   to indicate that a <b>WorkItem</b> can be executed.
      /// </value>
      /// <remarks>
      ///   Setting <b>Pausing</b> to <b>true, </b>inhibits the <see cref="Add"/> method from immediately
      ///   executing a <see cref="WorkItem"/>.  However, work items that are already executing will continue 
      ///   to completion.
      /// </remarks>
      public bool Pausing
      {
         get {return pausing;}
         set
         {
            if (pausing != value)
            {
               pausing = value;

               // Start executing some work.
               while (!pausing && DoNextWorkItem())
               {
               }
            }
         }
      }

      #endregion

      #region Waiting
      /// <summary>
      ///   Waits for all work to complete.
      /// </summary>
      /// <remarks>
      ///   <b>WaitAll</b> returns when all work is completed.
      ///   <para>
      ///   If the <b>WorkQueue</b> is <see cref="Pause">pausing</see> then the <see cref="InvalidOperationException"/>
      ///   is <c>throw</c> to avoid an infinite wait.
      ///   </para>
      ///   <para>
      ///   Any type of <see cref="Exception"/> is thrown when a WorkQueue <see cref="Thread"/> throws an
      ///   exception outside of the <see cref="WorkItem.Perform"/> method.
      ///   </para>
      /// </remarks>
      /// <exception cref="InvalidOperationException">
      ///   If the <b>WorkQueue</b> is <see cref="Pause">pausing</see>.
      /// </exception>
      /// <seealso cref="AllWorkCompleted">AllWorkCompleted event</seealso>
      public void WaitAll()
      {
         lock (this)
         {
            if (internalException != null)
               throw internalException;

            if (pausing)
               throw new InvalidOperationException("The queue is paused, no work will be performed.");

            if (runningItems == 0 && queue.Count == 0)
               return;
         }

         lock (completed)
         {
            if (internalException != null)
               throw internalException;

            if (runningItems == 0 && queue.Count == 0)
               return;

            Monitor.Wait(completed);

            if (internalException != null)
               throw internalException;
         }
      }

      /// <summary>
      ///   Waits for all work to complete or a specified amount of time elapses.
      /// </summary>
      /// <param name="timeout">
      ///   A <see cref="TimeSpan"/> representing the amount of time to wait before this method returns.
      /// </param>
      /// <returns>
      ///   <b>true</b> if all work is completed; otherwise, <b>false</b> to indicate that the specified
      ///   time has elapsed.
      /// </returns>
      /// <remarks>
      ///   <b>WaitAll</b> returns when all work is completed or the specified amount of time has elapsed.
      ///   <para>
      ///   Any type of <see cref="Exception"/> is thrown when a WorkQueue <see cref="Thread"/> throws an
      ///   exception outside of the <see cref="WorkItem.Perform"/> method.
      ///   </para>
      /// </remarks>
      /// <seealso cref="AllWorkCompleted">AllWorkCompleted event</seealso>
      public bool WaitAll(TimeSpan timeout)
      {
         lock (this)
         {
            if (internalException != null)
               throw internalException;
         }

         lock (completed)
         {
            if (!Monitor.Wait(completed, timeout))
               return false;

            if (internalException != null)
               throw internalException;
         }

         return true;
      }
      #endregion

      #region Raised Events

      /// <summary>
      ///   Occurs when the state of a work item is changed.
      /// </summary>
      /// <remarks>
      ///   The <b>ChangedWorkItemState</b> event is raised when the <see cref="IWorkItem.State"/>
      ///   property of a <see cref="IWorkItem"/> is changed.
      /// </remarks>
      public event ChangedWorkItemStateEventHandler ChangedWorkItemState
      {
         add
         {
            lock (eventLock)
            {
               changedWorkItemState += value;
            }
         }
         remove
         {
            lock (eventLock)
            {
               changedWorkItemState -= value;
            }
         }
      }
      private ChangedWorkItemStateEventHandler changedWorkItemState;

      /// <summary>
      ///   Raises the <see cref="ChangedWorkItemState"/> event.
      /// </summary>
      /// <param name="workItem">
      ///   The <see cref="IWorkItem"/> that has changed <see cref="IWorkItem.State"/>.
      /// </param>
      /// <param name="previousState">
      ///    One of the <see cref="WorkItemState"/> values indicating the previous state of the <paramref name="workItem"/>.
      /// </param>
      /// <remarks>
      ///   The <b>OnChangedWorkItemState</b> method allows derived classes to handle the event without attaching a delegate. This
      ///   is the preferred technique for handling the event in a derived class.
      ///   <para>
      ///   When a derived class calls the <b>OnChangedWorkItemState</b> method, it raises the <see cref="ChangedWorkItemState"/> event by 
      ///   invoking the event handler through a delegate. For more information, see 
      ///   <a href="ms-help://MS.VSCC.2003/MS.MSDNQTR.2004JAN.1033/cpguide/html/cpconProvidingEventFunctionality.htm">Raising an Event</a>.
      ///   </para>
      /// </remarks>
      protected virtual void OnChangedWorkItemState(IWorkItem workItem, WorkItemState previousState)
      {
         ChangedWorkItemStateEventHandler handler;

         lock (eventLock)
         {
            handler = changedWorkItemState;
         }
         if (handler != null)
         {
            handler (this, new ChangedWorkItemStateEventArgs(workItem, previousState));
         }
      }

      /// <summary>
      ///   Occurs when the last <see cref="IWorkItem"/> has completed.
      /// </summary>
      public event EventHandler AllWorkCompleted
      {
         add
         {
            lock (eventLock)
            {
               allWorkCompleted += value;
            }
         }
         remove
         {
            lock (eventLock)
            {
               allWorkCompleted -= value;
            }
         }
      }
      private EventHandler allWorkCompleted;

      /// <summary>
      ///   Raises the <see cref="AllWorkCompleted"/> event.
      /// </summary>
      /// <param name="e">
      ///   An <see cref="EventArgs"/> that contains the event data.
      /// </param>
      /// <remarks>
      ///   The <b>OnAllWorkCompleted</b> method allows derived classes to handle the event without attaching a delegate. This
      ///   is the preferred technique for handling the event in a derived class.
      ///   <para>
      ///   When a derived class calls the <b>OnAllWorkCompleted</b> method, it raises the <see cref="AllWorkCompleted"/> event by 
      ///   invoking the event handler through a delegate. For more information, see 
      ///   <a href="ms-help://MS.VSCC.2003/MS.MSDNQTR.2004JAN.1033/cpguide/html/cpconProvidingEventFunctionality.htm">Raising an Event</a>.
      ///   </para>
      /// </remarks>
      protected virtual void OnAllWorkCompleted(EventArgs e)
      {
         EventHandler handler;

         lock (eventLock)
         {
            handler = allWorkCompleted;
         }
         if (handler != null)
         {
            handler (this, e);
         }
      }


      /// <summary>
      ///   Occurs when an <see cref="IWorkItem"/> is starting execution.
      /// </summary>
      public event WorkItemEventHandler RunningWorkItem
      {
         add
         {
            lock (eventLock)
            {
               runningWorkItem += value;
            }
         }
         remove
         {
            lock (eventLock)
            {
               runningWorkItem -= value;
            }
         }
      }
      private event WorkItemEventHandler runningWorkItem;

      /// <summary>
      ///   Raises the <see cref="RunningWorkItem"/> event.
      /// </summary>
      /// <param name="workItem">
      ///   The <see cref="IWorkItem"/> that has started.
      /// </param>
      /// <remarks>
      ///   The <b>OnRunningWorkItem</b> method allows derived classes to handle the <see cref="RunningWorkItem"/>
      ///   event without attaching a delegate. This is the preferred technique for handling the event in a derived class.
      ///   <para>
      ///   When a derived class calls the <b>OnStartedWorkItem</b> method, it raises the <see cref="RunningWorkItem"/> event by 
      ///   invoking the event handler through a delegate. For more information, see 
      ///   <a href="ms-help://MS.VSCC.2003/MS.MSDNQTR.2004JAN.1033/cpguide/html/cpconProvidingEventFunctionality.htm">Raising an Event</a>.
      ///   </para>
      /// </remarks>
      protected virtual void OnRunningWorkItem(IWorkItem workItem)
      {
         WorkItemEventHandler handler;

         lock (eventLock)
         {
            handler = runningWorkItem;
         }
         if (handler != null)
         {
            handler (this, new WorkItemEventArgs(workItem));
         }
      }


      /// <summary>
      ///   Occurs when an <see cref="IWorkItem"/> has completed execution.
      /// </summary>
      public event WorkItemEventHandler CompletedWorkItem
      {
         add
         {
            lock (eventLock)
            {
               completedWorkItem += value;
            }
         }
         remove
         {
            lock (eventLock)
            {
               completedWorkItem -= value;
            }
         }
      }
      private event WorkItemEventHandler completedWorkItem;
      /// <summary>
      ///   Raises the <see cref="CompletedWorkItem"/> event.
      /// </summary>
      /// <param name="workItem">
      ///   The <see cref="IWorkItem"/> that has completed.
      /// </param>
      /// <remarks>
      ///   The <b>OnCompletedWorkItem</b> method allows derived classes to handle the <see cref="CompletedWorkItem"/>
      ///   event without attaching a delegate. This is the preferred technique for handling the event in a derived class.
      ///   <para>
      ///   When a derived class calls the <b>OnCompletedWorkItem</b> method, it raises the <see cref="CompletedWorkItem"/> event by 
      ///   invoking the event handler through a delegate. For more information, see 
      ///   <a href="ms-help://MS.VSCC.2003/MS.MSDNQTR.2004JAN.1033/cpguide/html/cpconProvidingEventFunctionality.htm">Raising an Event</a>.
      ///   </para>
      /// </remarks>
      protected virtual void OnCompletedWorkItem(IWorkItem workItem)
      {
         WorkItemEventHandler handler;

         lock (eventLock)
         {
            handler = completedWorkItem;
         }
         if (handler != null)
         {
            handler (this, new WorkItemEventArgs(workItem));
         }
      }


      /// <summary>
      ///   Occurs when an <see cref="IWorkItem"/> has failed execution.
      /// </summary>
      /// <remarks>
      ///   The <see cref="FailedWorkItem"/> event is raised when an <see cref="IWorkItem"/>
      ///   throws an <see cref="Exception"/>.  The <see cref="IWorkItem.FailedException"/> property
      ///   contains the <b>Exception</b>.
      /// </remarks>
      public event WorkItemEventHandler FailedWorkItem
      {
         add
         {
            lock (eventLock)
            {
               failedWorkItem += value;
            }
         }
         remove
         {
            lock (eventLock)
            {
               failedWorkItem -= value;
            }
         }
      }
      private event WorkItemEventHandler failedWorkItem;
      /// <summary>
      ///   Raises the <see cref="FailedWorkItem"/> event.
      /// </summary>
      /// <param name="workItem">
      ///   The <see cref="IWorkItem"/> that failed.
      /// </param>
      /// <remarks>
      ///   The <b>OnFailedWorkItem</b> method allows derived classes to handle the <see cref="FailedWorkItem"/>
      ///   event without attaching a delegate. This is the preferred technique for handling the event in a derived class.
      ///   <para>
      ///   When a derived class calls the <b>OnFailedWorkItem</b> method, it raises the <see cref="FailedWorkItem"/> event by 
      ///   invoking the event handler through a delegate. For more information, see 
      ///   <a href="ms-help://MS.VSCC.2003/MS.MSDNQTR.2004JAN.1033/cpguide/html/cpconProvidingEventFunctionality.htm">Raising an Event</a>.
      ///   </para>
      /// </remarks>
      protected virtual void OnFailedWorkItem(IWorkItem workItem)
      {
         WorkItemEventHandler handler;

         lock (eventLock)
         {
            handler = failedWorkItem;
         }
         if (handler != null)
         {
            handler (this, new WorkItemEventArgs(workItem));
         }
      }

      /// <summary>
      ///   Occurs when the <see cref="WorkerPool"/> throws an 
      ///   <see cref="Exception"/> that is not related to the work item.
      /// </summary>
      /// <remarks>
      ///   The <b>WorkerException</b> event occurs when 
      ///   an <see cref="Exception"/> is thrown in the <see cref="WorkerPool"/> outside of the 
      ///   <see cref="IWork.Perform">WorkItem.Perform</see> method.
      ///   <para>
      ///   The <b>WorkQueue</b> is <see cref="Pause">paused</see> and in an inconsistent state.
      ///   The <b>WorkQueue</b> should not be used again.
      ///   </para>
      /// </remarks>
      public event ResourceExceptionEventHandler WorkerException
      {
         add
         {
            lock (eventLock)
            {
               workerException += value;
            }
         }
         remove
         {
            lock (eventLock)
            {
               workerException -= value;
            }
         }
      }
      private event ResourceExceptionEventHandler workerException;
      /// <summary>
      ///   Raises the <see cref="WorkerException"/> event.
      /// </summary>
      /// <param name="e">
      ///   A <see cref="ResourceExceptionEventArgs"/> that contains the event data.
      /// </param>
      /// <remarks>
      ///   The <b>OnWorkerException</b> method allows derived classes to handle the <see cref="WorkerException"/>
      ///   event without attaching a delegate. This is the preferred technique for handling the event in a derived class.
      ///   <para>
      ///   When a derived class calls the <b>OnWorkerException</b> method, it raises the <see cref="WorkerException"/> event by 
      ///   invoking the event handler through a delegate. For more information, see 
      ///   <a href="ms-help://MS.VSCC.2003/MS.MSDNQTR.2004JAN.1033/cpguide/html/cpconProvidingEventFunctionality.htm">Raising an Event</a>.
      ///   </para>
      ///   <para>
      ///   The <b>WorkQueue</b> is <see cref="Pause">paused</see> and in an inconsistent state.
      ///   The <b>WorkQueue</b> should not be used again.
      ///   </para>
      /// </remarks>
      protected virtual void OnWorkerException(ResourceExceptionEventArgs e)
      {
         ResourceExceptionEventHandler handler;

         lock (eventLock)
         {
            handler = workerException;
         }
         if (handler != null)
            handler(this, e);
      }
      #endregion

      #region IWorkQueue Members

      /// <summary>
      ///   Invoked by an <see cref="IWorkItem"/> to inform a work queue that its <see cref="IWorkItem.State"/>
      ///   has changed.
      /// </summary>
      /// <param name="workItem">
      ///   The <see cref="IWorkItem"/> that has changed <see cref="IWorkItem.State"/>.
      /// </param>
      /// <param name="previousState">
      ///    One of the <see cref="WorkItemState"/> values indicating the previous state of the <paramref name="workItem"/>.
      /// </param>
      /// <remarks>
      ///   The <see cref="ChangedWorkItemState"/> event is raised by calling the
      ///   <see cref="OnChangedWorkItemState"/> method.  Then the following actions are performed, 
      ///   based on the new <see cref="IWorkItem.State"/> of <paramref name="workItem"/>:
      ///   <list type="table">
      ///   <listheader>
      ///     <term>State</term>
      ///     <description>Action</description>
      ///   </listheader>
      ///   <item>
      ///     <term><see cref="WorkItemState">Scheduled</see></term>
      ///     <description>Assign the <paramref name="workItem"/> to the <see cref="WorkerPool"/>.</description>
      ///   </item>
      ///   <item>
      ///     <term><see cref="WorkItemState">Running</see></term>
      ///     <description>Raise the <see cref="RunningWorkItem"/> event.</description>
      ///   </item>
      ///   <item>
      ///     <term><see cref="WorkItemState">Failing</see></term>
      ///     <description>Raise the <see cref="FailedWorkItem"/> event.</description>
      ///   </item>
      ///   <item>
      ///     <term><see cref="WorkItemState">Completed</see></term>
      ///     <description>Raise the <see cref="CompletedWorkItem"/> event and schedule the next work item in the queue.</description>
      ///   </item>
      ///   </list>
      /// </remarks>
      public void WorkItemStateChanged(IWorkItem workItem, WorkItemState previousState)
      {
         OnChangedWorkItemState(workItem, previousState);

         switch (workItem.State)
         {
            case WorkItemState.Scheduled:
               lock (this)
               {
                  // Housekeeping chores.
                  ++runningItems;

                  // Now start it.
                 WorkerPool.BeginWork(workItem);
               }
               break;

            case WorkItemState.Running:
               OnRunningWorkItem(workItem);
               break;

            case WorkItemState.Failing:
               OnFailedWorkItem(workItem);
               break;

            case WorkItemState.Completed:
               bool allDone = false;
               lock (this)
               {
                  --runningItems;
                  allDone = queue.Count == 0 && runningItems == 0;
               }

               // Tell the world that the workitem has completed.
               OnCompletedWorkItem(workItem);

               // Find some more work.
               if (allDone)
               {
                  // Wakeup.
                  OnAllWorkCompleted(EventArgs.Empty);
                  lock (completed)
                  {
                     Monitor.PulseAll(completed);
                  }
               }
               else
               {
                  DoNextWorkItem();
               }
               break;
         }

      }

      /// <summary>
      ///   Invoked by the <see cref="WorkerPool"/> when an exception is thrown outside of normal
      ///   processing.
      /// </summary>
      /// <param name="e">
      ///   A <see cref="ResourceExceptionEventArgs"/> that contains the event data.
      /// </param>
      /// <remarks>
      ///   <b>HandleResourceException</b> is called by the <see cref="WorkerPool"/> when
      ///   an exception is thrown outside of the <see cref="IWork.Perform">normal processing</see>
      ///   of a <see cref="IWorkItem"/>.
      ///   <para>
      ///   An exception at this point leaves the <see cref="WorkQueue"/> in an inconsistent state.  The
      ///   follow actions are performed:
      ///   <list type="bullet">
      ///   <item><description>The queue is <see cref="Pause">paused</see></description></item>
      ///   <item><description>The <see cref="WorkerException"/> event is raised.</description></item>
      ///   <item><description>All threads calling <see cref="WaitAll"/> will receive the exception.</description></item>
      ///   </list>
      ///   </para>
      /// </remarks>
      public void HandleResourceException(ResourceExceptionEventArgs e)
      {
         lock (completed)
         {
            Pause();
            internalException = e.Exception;

            // Tell the world.
            OnWorkerException(e);

            // Wakeup any threads in WaitAll and let them throw the exception.
            Monitor.PulseAll(completed);
         }

      }

      #endregion
   }
}
