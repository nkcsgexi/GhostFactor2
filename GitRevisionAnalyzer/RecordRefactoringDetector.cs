using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitSharp;
using NLog;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.source.history;
using warnings.util;

namespace GitRevisionAnalyzer
{
    /* Detect refactoring by a given ICodeHistoryRecord instance to look back.*/

    public class RecordRefactoringDetector
    {
        /* Look back count. */
        private static readonly int LOOK_BACK_LIMIT = 30;

        /* 
         * Search depth, for how many records from the head that we start searching. 
         * The complexity of algorithm is basically LOOK_BACK_LIMIT * SEARCH_DEPTH.
         */
        private static readonly int SEARCH_DEPTH = 1;


        private static readonly string DETECTED_REFACTORINGS_ROOT = "DetectedRefactorings/";

        /* Several detectors.*/
        private static readonly IExternalRefactoringDetector[] externalRefactoringDetectors = new IExternalRefactoringDetector[] 
        { 
            RefactoringDetectorFactory.CreateDummyExtractMethodDetector(),
            RefactoringDetectorFactory.CreateDummyInlineMethodDetector()
        };

        private readonly Logger logger;

        private string solutionName;
        private string fileName;

        // The count of all refactorings detected in this head chain.
        private int refactoringsCount;

        public RecordRefactoringDetector()
        {
            this.logger = NLoggerUtil.GetNLogger(typeof (RecordRefactoringDetector));
            refactoringsCount = 0;
        }

         public void DetectRefactorings(ICodeHistoryRecord head)
         {
             int depth = 0;

             // For every head in the head chain, look back to detect refactorings.
             // We do not search records that are older than SEARCH_DEPTH away from the head.
             for (var current = head; current.HasPreviousRecord() && depth < SEARCH_DEPTH; 
                 current = current.GetPreviousRecord(), depth ++)
             {
                 logger.Info("Detect refactorings with depth of head: " + depth);
                 LookBackToDetectRefactorings(current);
             }
         }

        private void LookBackToDetectRefactorings(ICodeHistoryRecord head)
        {
            // Retriever the solution and file names.
            this.solutionName = head.GetSolution();
            this.fileName = head.GetFile();

            var currentRecord = head;

            // Look back until no parent or reach the search depth.
            for (int i = 0; i < LOOK_BACK_LIMIT && currentRecord.HasPreviousRecord(); 
                i++, currentRecord = currentRecord.GetPreviousRecord())
            {
                try
                {
                    logger.Info("Looking back " + i + " revisions.");

                    // Handle current record.
                    if(HandlePreviousRecordWithDetectors(currentRecord.GetPreviousRecord(), head))
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    logger.Fatal(e);
                }
            }
        }

        /* Use all the refactoring detectors to detect refactoring, return whether a refactoring is detected. */
        private bool HandlePreviousRecordWithDetectors(ICodeHistoryRecord before, ICodeHistoryRecord head)
        {
            // If a refactoring is found, initially false.
            bool hasRefactorings = false;

            // For each detectors.
            foreach (var detector in externalRefactoringDetectors)
            {
                // If found by the detector hasRefactoring is set to be true.
                if(HandlePreviousRecord(before, head, detector))
                {
                    hasRefactorings = true;
                }
            }
            return hasRefactorings;
        }


        /* Handle a previous record with the head of the record chain. */
        private bool HandlePreviousRecord(ICodeHistoryRecord previous, ICodeHistoryRecord head, 
            IExternalRefactoringDetector detector)
        {
            var after = head.GetSource();
            var before = previous.GetSource();

            // Detect if a refactoring happens before head and the previous record.
            if (DetectRefactoringByDetector(before, after, detector))
            {
                // If there is a refactoring detected, search intermediate refactorings.
                DetectIntermediateRefactorings(previous, head.GetPreviousRecord(), detector);
                return true;
            }
            return false;
        }

        /* Detect intermediate refactorings the determined before record. */ 
        private void DetectIntermediateRefactorings(ICodeHistoryRecord baseBefore, ICodeHistoryRecord head, 
            IExternalRefactoringDetector detector)
        {
            // Get the source code for base before.
            var before = baseBefore.GetSource();

            // From the head, iteratively get previous record (until reach base before record) 
            // and detect if refactorings exist.
            for (var currentRecord = head; !currentRecord.Equals(baseBefore); 
                currentRecord = currentRecord.GetPreviousRecord())
            {
                var after = currentRecord.GetSource();
                DetectRefactoringByDetector(before, after, detector);
            }
        }

        private bool DetectRefactoringByDetector(string before, string after, IExternalRefactoringDetector detector)
        {
            // Set source before and after. 
            detector.SetSourceBefore(before);
            detector.SetSourceAfter(after);

            // If a refactoring is detected.
            if (detector.HasRefactoring())
            {
                // Get the detected refactorings, and log them.
                var refactorings = detector.GetRefactorings();
                foreach (var refactoring in refactorings)
                {
                    var path = HandleDetectedRefactoring(before, after, refactoring);
                    refactoringsCount ++;
                    logger.Info("Refactoring detected! Saved at " + path);
                }
                return true;
            }
            return false;
        }

        /* Handle a detected refactoring by saveing it at an independent file. */
        private string HandleDetectedRefactoring(string before, string after, IManualRefactoring refactoring)
        {
            // Get the folder and the file name for this detected refactoring.
            string refactoringDirectory = DETECTED_REFACTORINGS_ROOT + solutionName + "/" + GetRefactoringType(refactoring);
            string refactoringFilePath = refactoringDirectory + "/" + fileName + refactoringsCount + ".txt";

            // If the directory does not exist, create it.
            if(!Directory.Exists(refactoringDirectory))
            {
                Directory.CreateDirectory(refactoringDirectory);
            }

            // Streaming all the needed information to the file.
            var stream = File.CreateText(refactoringFilePath);
            stream.WriteLine("Source Before:");
            stream.WriteLine(before);
            stream.WriteLine("Source After:");
            stream.WriteLine(after);
            stream.WriteLine("Detected Refactoring:");
            stream.WriteLine(refactoring.ToString());
            stream.Flush();
            stream.Close();

            // Return the saved refactoring file path.
            return refactoringFilePath;
        }

        private string GetRefactoringType(IManualRefactoring refactoring)
        {
            var converter = new RefactoringType2StringConverter();
            return (string)converter.Convert(refactoring.RefactoringType, null, null, null);
        }

    }
}
