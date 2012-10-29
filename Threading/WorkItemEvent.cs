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
   ///   Represents the method that will handle the events associated with an <see cref="IWorkItem"/>.
   /// </summary>
   /// <param name="sender">
   ///   The source of the event.
   /// </param>
   /// <param name="e">
   ///   A <see cref="WorkItemEventArgs"/> than contains the event data.
   /// </param>
   public delegate void WorkItemEventHandler(object sender, WorkItemEventArgs e);

   /// <summary>
   ///   Provides data for the events asscociated with an <see cref="IWorkItem"/>.
   /// </summary>
   public class WorkItemEventArgs : EventArgs
   {
      IWorkItem workItem;

      private WorkItemEventArgs()
      {
      }

      /// <summary>
      ///   Initialise a new instance of the <see cref="WorkItemEventArgs"/> class with the
      ///   specified <see cref="IWorkItem"/>.
      /// </summary>
      /// <param name="workItem">
      ///   The <see cref="IWorkItem"/> associated with the event.
      /// </param>
      /// <remarks>
      ///   Use this constructor to create and initialize a new instance of the <see cref="WorkItemEventArgs"/>
      ///   with the specified <paramref name="workItem"/>.
      /// </remarks>
      public WorkItemEventArgs (IWorkItem workItem) : base()
      {
         this.workItem = workItem;
      }

      /// <summary>
      ///   Gets the <see cref="BlackHen.Threading.WorkItem"/> associated with the event.
      /// </summary>
      /// <value>
      ///   The <see cref="BlackHen.Threading.WorkItem"/> that caused the event.
      /// </value>
      public IWorkItem WorkItem
      {
         get {return workItem;}
      }

   }
}
