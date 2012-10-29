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
	///   Provides the methods and properties to manage the scheduling of an <see cref="IWorkItem">work item</see>.
	/// </summary>
	/// <remarks>
	///   <b>IWorkQueue</b> provides the methods and properties to the manage the scheduling of an <see cref="IWorkItem"/>.
	///   Its primary responsibility is to determine when and it what order work items are executed.
	///   <para>
	///   The <see cref="WorkItemStateChanged"/> method is invoked by an <see cref="IWorkItem"/> to inform the <b>WorkQueue</b>
	///   of a <see cref="IWorkItem.State"/> change.  It is the responsible of the <b>WorkQueue</b> to
	///   perform the appropiate logic for the given state.
	///   </para>
	///   <para>
	///   </para>
	/// </remarks>
   public interface IWorkQueue
   {
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
      ///   It is the responsible of the <see cref="IWorkQueue"/> to  perform the appropiate logic for the 
      ///   new <see cref="IWorkItem.State"/>.
      /// </remarks>
      void WorkItemStateChanged (IWorkItem workItem, WorkItemState previousState);

      /// <summary>
      ///   Invoked by an <see cref="IResourcePool"/> when an exception is thrown outside of normal
      ///   processing.
      /// </summary>
      /// <param name="e">
      ///   A <see cref="ResourceExceptionEventArgs"/> that contains the event data.
      /// </param>
      /// <remarks>
      ///   <b>HandleResourceException</b> is called by an <see cref="IResourcePool"/> when
      ///   an exception is thrown outside of the <see cref="IWork.Perform">normal processing</see>
      ///   of a <see cref="IWorkItem"/>.
      /// </remarks>
      /// <seealso cref="IResourcePool"/>
      void HandleResourceException(ResourceExceptionEventArgs e);
   }
}
