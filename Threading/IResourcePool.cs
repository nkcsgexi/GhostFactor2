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

namespace BlackHen.Threading
{
	/// <summary>
	///   Provides a pool of resources that can be used to perform a <see cref="IWorkItem">work item</see>.
	/// </summary>
	/// <remarks>
	///   The <see cref="BeginWork"/> method is invoked by an <see cref="IWorkQueue"/> when the
	///   <see cref="WorkItem.State"/> of an <see cref="IWorkItem"/> becomes <see cref="WorkItemState">Scheduled</see>.
	///   <para>
	///   An <b>IResourcePool</b> must catch any <see cref="Exception"/> thrown by an <b>IWorkItem</b>
	///   when it invokes the <see cref="IWork.Perform"/> method.  The <see cref="IWorkItem.FailedException"/>
	///   property of the <b>IWorkItem</b> must be set and its <see cref="IWorkItem.State"/> changed
	///   to <see cref="WorkItemState">Failing</see>.
	///   </para>
	///   If the <b>IResourcePool</b> throws an exception while performing an <b>IWorkItem</b>, it must
	///   invoke the <see cref="IWorkQueue.HandleResourceException"/> of <see cref="IWorkItem.WorkQueue"/>.
	/// </remarks>
	/// <example>
	///   The following demonstrates exception handling by a hypothetical thread:
	///   <code>
   ///private WorkLoop
	///{
	///  while (WorkItem workItem = NextWork())
	///  {
   ///   try
   ///   {
   ///    
   ///      // Do the work.
   ///      workItem.State = WorkItemState.Running
   ///      try
   ///      {
   ///        workItem.Perform();
   ///      }
   ///      catch (Exception e)
   ///      {
   ///         // Exception in workitem.
   ///         workItem.FailedException = e;
   ///         workItem.State = WorkItemState.Failing;
   ///      }
   ///   
   ///      // Workitem is done processed, either failed or succeeded.
   ///      workItem.State = WorkItemState.Completed;
   ///   }
   ///   
   ///   catch (Exception e)
   ///   {
   ///     // Internal exception!!!
   ///     workItem.WorkQueue.HandleResourceException(new ResourceExceptionEventArgs(this, e));
   ///   }
   ///  }
   /// }
   ///   </code>
	/// </example>
	public interface IResourcePool
	{
      /// <summary>
      ///   Requests that an <see cref="IWorkItem">work item</see> is performed by a resource
      ///   in the pool.
      /// </summary>
      /// <param name="workItem">
      ///   The <see cref="IWorkItem"/> to execute.
      /// </param>
      /// <remarks>
      ///   <b>BeginWork</b> queues the <paramref name="workItem"/> for execution.  When a resource in the pool
      ///   becomes available, the <see cref="IWorkItem.State"/> of the <paramref name="workItem"/>
      ///   is set to <see cref="WorkItem.State">Running</see>
      ///   and its <see cref="IWork.Perform"/> method is invoked.
      /// </remarks>
      void BeginWork(IWorkItem workItem);
	}
}
