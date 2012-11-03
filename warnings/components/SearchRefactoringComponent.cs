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
    public interface ISearchRefactoringComponent : IFactorComponent
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

        public void Enqueue(IWorkItem item)
        {
        }

        public int GetWorkQueueLength()
        {
            return queue.Count;
        }

        public void Start()
        {
        }

        public string GetName()
        {
            return "Refactoring search component";
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

            internal SearchRefactoringWorkitem(ICodeHistoryRecord latestRecord, DocumentId documentId)
            {
                this.latestRecord = latestRecord;
                this.documentId = documentId;
                logger = NLoggerUtil.GetNLogger(typeof(SearchRefactoringWorkitem));
            }

            public override void Perform()
            {
                IEnumerable<DetectedRefactoring> detectedRefactorings;
                var detectors = GetRefactoringDetectors();
                var sourceAfter = latestRecord.GetSource();

                int lookBackCount = 1;
                for (var currentRecord = latestRecord; currentRecord.HasPreviousRecord() && 
                    detectors.Any(); currentRecord = currentRecord.GetPreviousRecord(), 
                        lookBackCount++)
                {
                    detectors = GetActiveDetectors(detectors, lookBackCount);
                    var sourceBefore = currentRecord.GetPreviousRecord().GetSource();
                    
                    SetDetectorsSource(detectors, sourceBefore, sourceAfter);
                    detectedRefactorings = GetDetectRefactorings(detectors);
                    if(detectedRefactorings.Any())
                    {
                        var detectedRefactoring = detectedRefactorings.First();
                        detectedRefactoring.Refactoring.MetaData.DocumentId = documentId;
                        detectedRefactoring.Refactoring.MetaData.DocumentUniqueName =
                            documentId.UniqueName;
                        OnRefactoringDetected(detectedRefactoring.BeforeDocument,
                            detectedRefactoring.AfterDocument, detectedRefactoring.Refactoring);
                        return;
                    }
                }
                OnNoRefactoringDetected(latestRecord);
            }

            private IEnumerable<IExternalRefactoringDetector> GetRefactoringDetectors()
            {
                return GlobalConfigurations.GetSupportedRefactoringTypes().Select
                    (RefactoringDetectorFactory.GetRefactoringDetectorByType);
            }

            private void SetDetectorsSource(IEnumerable<IExternalRefactoringDetector> detectors,
                string before, string after)
            {
                foreach (var detector in detectors)
                {
                    detector.SetSourceBefore(before);
                    detector.SetSourceAfter(after);
                }
            }

            private IEnumerable<IExternalRefactoringDetector> GetActiveDetectors
                (IEnumerable<IExternalRefactoringDetector> detectors, int lookBack)
            {
                var activeDetectors = new List<IExternalRefactoringDetector>();
                activeDetectors.AddRange(detectors.Where(d => GlobalConfigurations.GetSearchDepth(d.RefactoringType) >
                    lookBack));
                return activeDetectors;
            }

            private IEnumerable<DetectedRefactoring> GetDetectRefactorings
                (IEnumerable<IExternalRefactoringDetector> detectors)
            {
                return detectors.Where(d => d.HasRefactoring()).Select
                    (d => new DetectedRefactoring(d.GetBeforeDocument(), d.GetAfterDocument(),
                        d.GetRefactorings().First()));
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
