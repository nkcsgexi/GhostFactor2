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
   ///   Defines an interface that allows some work to be performed.
   /// </summary>
   /// <remarks>
   ///   <b>IWorkItem</b> specifies the <see cref="Perform"/> method, that does the actual processing
   ///   of work.
   /// </remarks>
   /// <seeaslo cref="IWorkItem"/>
   public interface IWork
	{
      /// <summary>
      ///   Perform the work.
      /// </summary>
      /// <remarks>
      ///   <b>Perform</b> does the processing of work. 
      /// </remarks>
      void Perform();
   }
}
