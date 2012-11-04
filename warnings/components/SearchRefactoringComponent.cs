using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using BlackHen.Threading;
using NLog;
using Roslyn.Services;
using warnings.configuration;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.source;
using warnings.source.history;
using warnings.util;

namespace warnings.components
{
    public interface ISearchRefactoringComponent
    {
        void StartRefactoringSearch(ICodeHistoryRecord record, DocumentId documentId);
    }

    /* Component for searching a manual refactoring in the code history. */
    internal class SearchRefactoringComponent : ISearchRefactoringComponent
    {
        private static ISearchRefactoringComponent instance;
        public static ISearchRefactoringComponent GetInstance()
        {
            return instance ?? (instance = new SearchRefactoringComponent());
        }

        private readonly WorkQueue queue;
        protected readonly Logger logger;

        private SearchRefactoringComponent()
        {
            queue = new WorkQueue {ConcurrentLimit = 1};
            queue.FailedWorkItem += onFailedWorkItem;
            queue.CompletedWorkItem += onCompletedWorkItem;

            logger = NLoggerUtil.GetNLogger(typeof (ISearchRefactoringComponent));
        }

        private void onCompletedWorkItem(object sender, WorkItemEventArgs workItemEventArgs)
        {
            var timable = workItemEventArgs.WorkItem as TimableWorkItem;
            if(timable != null)
            {
                logger.Info("Search item time: " + timable.GetProcessingTime().
                    TotalMilliseconds);
            }
        }

        private void onFailedWorkItem(object sender, WorkItemEventArgs e)
        {
            logger.Fatal("Search refactoring work item failed.\n" + 
                e.WorkItem.FailedException);
        }

        public void StartRefactoringSearch(ICodeHistoryRecord record, DocumentId documentId)
        {
            queue.Add(new SearchRefactoringWorkitem(record, documentId));
        }

        /// <summary>
        /// Work item for searching refactorings of all supported refactoring types.
        /// </summary>
        private class SearchRefactoringWorkitem : TimableWorkItem
        {
            private readonly ICodeHistoryRecord latestRecord;
            private readonly DocumentId documentId;
            private readonly Logger logger;

            internal SearchRefactoringWorkitem(ICodeHistoryRecord latestRecord, 
                DocumentId documentId)
            {
                this.latestRecord = latestRecord;
                this.documentId = documentId;
                logger = NLoggerUtil.GetNLogger(typeof(SearchRefactoringWorkitem));
            }

            public override void Perform()
            {
                try
                {
                    var sourceAfter = latestRecord.GetSource();

                    int lookBackCount = 1;
                    for (var currentRecord = latestRecord;
                         currentRecord.HasPreviousRecord();
                         currentRecord = currentRecord.GetPreviousRecord(), lookBackCount++)
                    {
                        // Get the detectors that are currently applicable.
                        var currentDetectors = GetActiveDetectors(lookBackCount);
                        if (currentDetectors.Any())
                        {
                            // Get the source code of the previous record.
                            var sourceBefore = currentRecord.GetPreviousRecord().GetSource();

                            // Set source before and after for the applicable detectors.
                            foreach (var detector in currentDetectors)
                            {
                                detector.SetSourceBefore(sourceBefore);
                                detector.SetSourceAfter(sourceAfter);
                            }

                            var detectedRefactorings = GetDetectRefactorings(currentDetectors);
                            if (detectedRefactorings.Any())
                            {
                                var detectedRefactoring = detectedRefactorings.First();
                                detectedRefactoring.Refactoring.MetaData.DocumentId = documentId;
                                detectedRefactoring.Refactoring.MetaData.DocumentUniqueName =
                                    documentId.UniqueName;
                                OnRefactoringDetected(detectedRefactoring.BeforeDocument,
                                                      detectedRefactoring.AfterDocument,
                                                      detectedRefactoring.Refactoring);
                                return;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    OnNoRefactoringDetected(latestRecord);
                }catch(Exception e)
                {
                    logger.Fatal(e);
                }
            }

            /// <summary>
            /// Given a set of refactorin detectors and the count of current look back,
            /// returns the detectors that are still active, i.e., not excel its look back 
            /// limit.
            /// </summary>
            /// <param name="detectors">Input detectors.</param>
            /// <param name="lookBack">The number of current look backs.</param>
            /// <returns></returns>
            private List<IExternalRefactoringDetector> GetActiveDetectors
                (int lookBack)
            {
                var validTypes = GlobalConfigurations.GetSupportedRefactoringTypes().
                    Where(t => GlobalConfigurations.GetSearchDepth(t) > lookBack).ToList();
                return validTypes.Select(RefactoringDetectorFactory.
                    GetRefactoringDetectorByType).ToList();
            }

            /// <summary>
            /// Given a set of detectors whose source before and after are set, return the 
            /// detected refactorings by applying them.
            /// </summary>
            /// <param name="detectors"></param>
            /// <returns></returns>
            private List<DetectedRefactoring> GetDetectRefactorings
                (IEnumerable<IExternalRefactoringDetector> detectors)
            {
                var detectedRefactorings = new List<DetectedRefactoring>();
                foreach (var detector in detectors)
                {
                    if(detector.HasRefactoring())
                    {
                        var beforeDoc = detector.GetBeforeDocument();
                        var afterDoc = detector.GetAfterDocument();
                        var refactoring = detector.GetRefactorings().First();
                        detectedRefactorings.Add(new DetectedRefactoring(beforeDoc, 
                            afterDoc, refactoring));
                    }
                }
                return detectedRefactorings;
            }

            private class DetectedRefactoring
            {
                public IDocument BeforeDocument { private set; get; }
                public IDocument AfterDocument { private set; get; }
                public ManualRefactoring Refactoring { private set; get; }

                internal DetectedRefactoring(IDocument BeforeDocument, IDocument AfterDocument,
                    ManualRefactoring Refactoring)
                {
                    this.BeforeDocument = BeforeDocument;
                    this.AfterDocument = AfterDocument;
                    this.Refactoring = Refactoring;
                }
            }


            private void OnRefactoringDetected(IDocument beforeDocument, IDocument afterDocument,
              ManualRefactoring refactoring)
            {
                logger.Info("Refactoring detected:");
                logger.Info(refactoring.ToString);
                GhostFactorComponents.conditionCheckingComponent.CheckRefactoringCondition
                    (beforeDocument, afterDocument, refactoring);
            }

            private void OnNoRefactoringDetected(ICodeHistoryRecord after)
            {
                logger.Info("No refactoring detected.");
            }
        }
    }
}
