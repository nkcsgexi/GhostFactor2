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
	///   Specifies the state of a <see cref="WorkItem"/>.
	/// </summary>
	/// <remarks>
	///   <img src="WorkItemState.png" alt="WorkItem state transistions"/>
	/// </remarks>
	public enum WorkItemState
	{
      /// <summary>
      ///   Not assigned to a <see cref="WorkQueue"/>.
      /// </summary>
      Created = 0,

      /// <summary>
      ///   Waiting for a <see cref="Thread"/> to execute on.
      /// </summary>
      Scheduled,

      /// <summary>
      ///   Waiting for another <see cref="WorkItem"/> to complete, so it can run concurrently.
      /// </summary>
      Queued,

      /// <summary>
      ///   Executing on a <see cref="Thread"/>.
      /// </summary>
      Running,

      /// <summary>
      ///   Recovering from a thrown <see cref="Exception"/>.
      /// </summary>
      Failing,

      /// <summary>
      ///   Finished executing.
      /// </summary>
      Completed
	}
}
