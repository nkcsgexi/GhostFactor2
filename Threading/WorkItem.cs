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

using System;
using System.Globalization;
using System.Threading;

namespace BlackHen.Threading
{

	/// <summary>
	///   Represents some work that must be performed.
	/// </summary>
	/// <remarks>
	///   The order of execution of <b>WorkItems</b> is determined by the <see cref="Priority"/> property.
	///   Higher valued priorities will execute earlier.
	///   <para>
	///   Derived classes must implement the <see cref="Perform"/> method.  When <b>Perform()</b> is called,
	///   the <see cref="CultureInfo.CurrentUICulture "/> is set to <b>CurrentUICulture</b> of the
	///   <see cref="Thread"/> that created the <b>WorkItem</b>.
	///   </para>
	/// </remarks>
	public abstract class WorkItem : IWorkItem, IComparable
	{
      private ThreadPriority priority;
      private DateTime createdTime;
      private DateTime startedTime;
      private DateTime completedTime;
      private Exception failedException;
      private WorkItemState state;
      private IWorkQueue workQueue;

      private CultureInfo uiCulture;

      #region Constructors
      /// <summary>
      ///   Creates a new instance of the <see cref="WorkItem"/> class.
      /// </summary>
      protected WorkItem()
      {
         createdTime = DateTime.Now;
         priority = ThreadPriority.Normal;
         state = WorkItemState.Created;

         // Capture the invokers context.
         uiCulture = Thread.CurrentThread.CurrentUICulture;
      }
      #endregion

      #region Properties

      /// <summary>
      ///   Gets or sets the <see cref="IWorkQueue"/> containing this <see cref="IWorkItem"/>.
      /// </summary>
      /// <value>
      ///   The <see cref="IWorkQueue"/> that is scheduling this <see cref="IWorkItem"/>.
      /// </value>
      public IWorkQueue WorkQueue
      {
         get
         {
            return workQueue;
         }
         set
         {
            if (workQueue != value)
            {
               if (workQueue != null)
                  throw new NotSupportedException(String.Format("'{0}' is assigned to another WorkQueue '{1}'.", this, workQueue));

               workQueue = value;
            }
         }
      }

      /// <summary>
      ///   Gets or sets the <see cref="WorkItemState">state</see>.
      /// </summary>
      /// <value>
      ///   One of the <see cref="WorkItemState"/> values indicating the state of the current <b>WorkItem</b>. 
      ///   The initial value is <b>Created</b>.
      /// </value>
      /// <exception cref="InvalidTransitionException">
      ///   The <b>State</b> can not be transitioned to <paramref name="value"/>.
      /// </exception>
      /// <remarks>
      ///   The <b>State</b> represents where the <see cref="WorkItem"/> is in processing pipeline.
      ///   The following transition can take place:
      ///   <para>
      ///   <img src="WorkItemState.png" alt="WorkItem state transistions"/>
      ///   </para>
      ///   <para>
      ///   If the <see cref="WorkQueue"/> is not <b>null</b>, then its 
      ///   <see cref="IWorkQueue.WorkItemStateChanged"/> method is called.
      ///   </para>
      /// </remarks>
      /// <seealso cref="ValidateStateTransition"/>
      public WorkItemState State
      {
         get {return state;}
         set
         {
            ValidateStateTransition (state, value);

            WorkItemState prev = state;
            state = value;
            switch (state)
            {
               case WorkItemState.Running:
                  StartedTime = DateTime.Now;
                  ApplyInvokerContext();
                  break;

               case WorkItemState.Completed:
                  CompletedTime = DateTime.Now;
                  break;
            }

            if (WorkQueue != null)
               WorkQueue.WorkItemStateChanged(this, prev);
         }
      }

      /// <summary>
      ///   Validate a state transition.
      /// </summary>
      /// <param name="currentState">
      ///   One of the <see cref="WorkItemState"/> values indicating the current <see cref="WorkItemState"/>. 
      /// </param>
      /// <param name="nextState">
      ///   One of the <see cref="WorkItemState"/> values indicating the requested <see cref="WorkItemState"/>. 
      /// </param>
      /// <exception cref="InvalidTransitionException">
      ///   The transition from <paramref name="current"/> to  <paramref name="value"/> is invalid.
      /// </exception>
      /// <remarks>
      ///   <b>ValidateStateTransition</b> throws <see cref="InvalidTransitionException"/> if
      ///   the transition from <paramref name="current"/> to  <paramref name="value"/> is invalid.
      ///   <para>
      ///   Derived class can use this method for extra validation.
      ///   </para>
      /// </remarks>
      protected virtual void ValidateStateTransition(WorkItemState currentState, WorkItemState nextState)
      {
         switch (currentState)
         {
            case WorkItemState.Completed:
               break;

            case WorkItemState.Created:
               if (nextState == WorkItemState.Scheduled || nextState == WorkItemState.Queued)
                  return;
               break;

            case WorkItemState.Failing:
               if (nextState == WorkItemState.Completed)
                  return;
               break;

            case WorkItemState.Queued:
               if (nextState == WorkItemState.Scheduled)
                  return;
               break;

            case WorkItemState.Running:
               if (nextState == WorkItemState.Completed || nextState == WorkItemState.Failing)
                  return;
               break;

            case WorkItemState.Scheduled:
               if (nextState == WorkItemState.Running)
                  return;
               break;

            default:
               break;
         }

         throw new InvalidTransitionException(this, currentState, nextState);
      }

      /// <summary>
      ///   Gets or sets the <see cref="Exception"/> that caused the <see cref="WorkItem"/> to
      ///   failed.
      /// </summary>
      public Exception FailedException
      {
         get {return failedException;}
         set {failedException = value;}
      }

      /// <summary>
      ///   Gets or sets the scheduling priority.
      /// </summary>
      /// <value>
      ///   One of the <see cref="ThreadPriority"/> values. The default value is <b>Normal</b>.
      /// </value>
      /// <remarks>
      ///   <b>Prioriry</b> specifies the relative importance of one <see cref="WorkItem"/> versus another.
      /// </remarks>
      /// <exception cref="ArgumentOutOfRangeException">
      ///   <paramref name="value"/> is not valid.
      /// </exception>
      public ThreadPriority Priority
      {
         get
         {
            return priority;
         }
         set
         {
            if (!Enum.IsDefined(typeof(ThreadPriority), value))
               throw new ArgumentOutOfRangeException("value", value, "Not a valid value.");

            priority = value;
         }
      }
      #endregion

      #region Processing
      /// <summary>
      ///   Perform the work.
      /// </summary>
      /// <remarks>
      ///   <b>Perform</b> performs the work. 
      ///   <para>
      ///   Before the method is called, the <see cref="CultureInfo.CurrentUICulture"/> of the <i>invoker</i>
      ///   is applied to this <see cref="Thread"/>.  The <i>invoker's</i> culture is capture when 
      ///   the <see cref="WorkItem"/> is constructed.
      ///   </para>
      ///   <para>
      ///   A thrown <see cref="Exception"/> is caught by the <see cref="IResourcePool"/> and the
      ///   workitem's
      ///   <see cref="FailedException"/> property is set and its <see cref="State"/> changed
      ///   to <see cref="WorkItemState">Failing</see>.
      ///   </para>
      ///   <para>
      ///   This is an <b>abstract</b> method and must be implmented by derived classes.
      ///   </para>
      /// </remarks>
      public abstract void Perform();

      /// <summary>
      ///   Changes the "context" to the context of creator of the <see cref="WorkItem"/>.
      /// </summary>
      /// <remarks>
      ///   The <see cref="CultureInfo.CurrentUICulture"/> of the <i>invoker</i> is applied
      ///   to this <see cref="Thread"/>.  The <i>invoker</i> context is defined when 
      ///   the <see cref="WorkItem"/> is constructed.
      /// </remarks>
      internal void ApplyInvokerContext()
      {
         Thread thisThread = Thread.CurrentThread;

         if (uiCulture != thisThread.CurrentUICulture)
            thisThread.CurrentUICulture = uiCulture;
      }

      #endregion

      #region Times
      /// <summary>
      ///   Gets or sets the time when processing <see cref="Perform">started</see>.
      /// </summary>
      /// <value>
      ///   A <see cref="DateTime"/> indicating when the <b>WorkItem</b> started.
      /// </value>
      /// <remarks>
      ///   The <b>StartedTime</b> is set when the <see cref="WorkItem"/> enters the
      ///   <see cref="WorkItemState">Running</see> state.
      /// </remarks>
      public DateTime StartedTime
      {
         get {return startedTime;}
         set {startedTime = value;}
      }

      /// <summary>
      ///   Gets or sets the time when processing completed.
      /// </summary>
      /// <value>
      ///   A <see cref="DateTime"/> indicating when the <b>WorkItem</b> finished.
      /// </value>
      /// <remarks>
      ///   The <b>CompletedTime</b> is set when the <see cref="WorkItem"/> enters the
      ///   <see cref="WorkItemState">Completed</see> state.
      /// </remarks>
      public DateTime CompletedTime
      {
         get {return completedTime;}
         set 
         {
            completedTime = value;
         }
      }

      /// <summary>
      ///   Gets or sets the time when the instance was created.
      /// </summary>
      /// <value>
      ///   A <see cref="DateTime"/> indicating when the <b>WorkItem</b> was created.
      /// </value>
      /// <remarks>
      ///   The <b>CreatedTime</b> is set when the <see cref="WorkItem"/> is constructed.
      /// </remarks>
      public DateTime CreatedTime
      {
         get {return createdTime;}
         set 
         {
            createdTime = value;
         }
      }

      /// <summary>
      ///   Gets the elapsed processing time.
      /// </summary>
      /// <value>
      ///   A <see cref="TimeSpan"/> indicating the amount of time spent processing.
      /// </value>
      /// <remarks>
      ///   <b>ProcessingTime</b> is the difference between the <see cref="CompletedTime"/> and 
      ///   <see cref="StartedTime"/>.
      /// </remarks>
      public TimeSpan ProcessingTime
      {
         get {return CompletedTime - StartedTime;}
      }
      #endregion

      #region IComparable Members

      /// <summary>
      ///   Compares this instance with a specified <see cref="object"/>.
      /// </summary>
      /// <param name="obj">
      ///   An <see cref="object"/> that is a <see cref="WorkItem"/>.
      /// </param>
      /// <returns>
      ///   A 32-bit signed integer indicating the lexical relationship between the <see cref="WorkItem.Priority"/>
      ///   of the operands.
      /// </returns>
      public int CompareTo(object obj)
      {
         WorkItem wi = obj as WorkItem;
         if (wi == null)
            throw new ArgumentException("Not a WorkItem.");
 
         return this.Priority - wi.Priority;
      }

      #endregion
   
      #region WorkItem.Empty
      /// <summary>
      ///   Creates  a <see cref="WorkItem"/> that does nothing.
      /// </summary>
      public static WorkItem Empty
      {
         get {return new EmptyWorkItem();}
      }

      private class EmptyWorkItem : WorkItem
      {
         public override void Perform()
         {
            // Do nothing.
         }

      }

      #endregion
   }
}
