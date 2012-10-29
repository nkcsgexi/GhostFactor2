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
using System.Threading;

namespace BlackHen.Threading
{

   /// <summary>
   ///   Defines an interface to allow work to be managed by an <see cref="IWorkQueue"/>
   /// </summary>
   /// <remarks>
   ///   <b>IWorkItem</b> extends the <see cref="IWork"/> interface, allowing work to be managed by 
   ///   a <see cref="IWorkQueue">work queue</see>.
   /// </remarks>
   public interface IWorkItem : IWork
   {
      /// <summary>
      ///   Gets or sets the <see cref="IWorkQueue"/> that manages this <see cref="IWorkItem"/>.
      /// </summary>
      /// <value>
      ///   The <see cref="IWorkQueue"/> that is scheduling this <see cref="IWorkItem"/>.
      /// </value>
      IWorkQueue WorkQueue {get; set;}

      /// <summary>
      ///   Gets or sets the <see cref="WorkItemState">state</see>.
      /// </summary>
      /// <value>
      ///   One of the <see cref="WorkItemState"/> values indicating the state of the current <b>WorkItem</b>. 
      ///   The initial value is <b>Created</b>.
      /// </value>
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
      WorkItemState State {get; set;}

      /// <summary>
      ///   Gets or sets the <see cref="Exception"/> that caused the <see cref="WorkItem"/> to
      ///   failed.
      /// </summary>
      Exception FailedException {get; set;}

      /// <summary>
      ///   Gets or sets the scheduling priority.
      /// </summary>
      /// <value>
      ///   One of the <see cref="ThreadPriority"/> values. The default value is <b>Normal</b>.
      /// </value>
      /// <remarks>
      ///   <b>Prioriry</b> specifies the relative importance of one <see cref="WorkItem"/> versus another.
      /// </remarks>
      ThreadPriority Priority {get; set;}
   }
}
