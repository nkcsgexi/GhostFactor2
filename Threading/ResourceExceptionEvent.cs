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
   ///   Represents the method that will handle the <see cref="WorkQueue.WorkerException"/> 
   ///   and <see cref="WorkThreadPool.ThreadException"/> events.
   /// </summary>
   /// <param name="sender">
   ///   The source of the event.
   /// </param>
   /// <param name="e">
   ///   A <see cref="ResourceExceptionEventArgs"/> than contains the event data.
   /// </param>
   public delegate void ResourceExceptionEventHandler(object sender, ResourceExceptionEventArgs e);

   /// <summary>
   ///   Provides data for the <see cref="WorkQueue.WorkerException"/> 
   ///   and <see cref="WorkThreadPool.ThreadException"/> events.
   /// </summary>
   /// <remarks>
   ///   A <b>ResourceExceptionEventArgs</b> is created by a resource (worker) when 
   ///   an exception is thrown outside of the <see cref="IWork.Perform">WorkItem.Perform</see> method.
   ///   <b>ResourceExceptionEventArgs</b>  contains the <see cref="Exception"/>
   ///   that caused the event to be raised.
   /// </remarks>
   public sealed class ResourceExceptionEventArgs : EventArgs
   {
      private object resource;
      private System.Exception exception;
      private IWorkItem workItem;

      private ResourceExceptionEventArgs()
      {
      }

      /// <summary>
      ///   Initialise a new instance of the <see cref="ResourceExceptionEventArgs"/> class with the
      ///   specified resource and <see cref="System.Exception"/>.
      /// </summary>
      /// <param name="resource">
      ///   The <see cref="object"/> that raised the exception.
      /// </param>
      /// <param name="exception">
      ///   The <see cref="System.Exception"/> that occured.
      /// </param>
      /// <remarks>
      ///   Use this constructor to create and initialize a new instance of the <see cref="ResourceExceptionEventArgs"/>
      ///   with the specified <see cref="System.Exception"/>.
      /// </remarks>
      public ResourceExceptionEventArgs (object resource, Exception exception) : base()
      {
         this.resource = resource;
         this.exception = exception;
      }

      /// <summary>
      ///   Initialise a new instance of the <see cref="ResourceExceptionEventArgs"/> class with the
      ///   specified resource, <see cref="IWorkItem"/> and <see cref="System.Exception"/>.
      /// </summary>
      /// <param name="resource">
      ///   The <see cref="object"/> that raised the exception.
      /// </param>
      /// <param name="workItem">
      ///   The <see cref="IWorkItem"/> that the <paramref name="resource"/> was working on.
      /// </param>
      /// <param name="exception">
      ///   The <see cref="System.Exception"/> that occured.
      /// </param>
      /// <remarks>
      ///   Use this constructor to create and initialize a new instance of the <see cref="ResourceExceptionEventArgs"/>
      ///   with the specified <see cref="System.Exception"/>.
      /// </remarks>
      public ResourceExceptionEventArgs (object resource, IWorkItem workItem, Exception exception) : base()
      {
         this.resource = resource;
         this.workItem = workItem;
         this.exception = exception;
      }

      /// <summary>
      ///   Gets the exception that occured.
      /// </summary>
      /// <value>
      ///   The <see cref="System.Exception"/> that occured.
      /// </value>
      public System.Exception Exception
      {
         get {return exception;}
      }

      /// <summary>
      ///   Gets the work item.
      /// </summary>
      /// <value>
      ///   A <see cref="IWorkItem"/> or <b>null</b>.
      /// </value>
      public IWorkItem WorkItem
      {
         get {return workItem;}
      }

      /// <summary>
      ///   Gets the resource that raised the exception.
      /// </summary>
      /// <remarks>
      ///   A <b>Resource</b> is something that can perform <see cref="IWork">work</see>.
      /// </remarks>
      public object Resource
      {
         get {return resource;}
      }

   }
}
