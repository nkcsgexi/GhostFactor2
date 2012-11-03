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

            logger = NLoggerUtil.GetNLogger(typeof (ISearchRefactoringComponent));
        }

        private void onFailedWorkItem(object sender, WorkItemEventArgs e)
        {
            logger.Fatal("WorkItem failed.");
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
                try
                {
                    var detectedRefactorings = new List<DetectedRefactoring>();
                    var detectors = GetRefactoringDetectors();
                    SetDetectorsSourceAfter(detectors, latestRecord.GetSource());

                    int lookBackCount = 1;
                    for (var currentRecord = latestRecord; currentRecord.HasPreviousRecord() && detectors.Any()
                        ; currentRecord = currentRecord.GetPreviousRecord(), lookBackCount++)
                    {
                        detectors = GetActiveDetectors(detectors, lookBackCount);
                        SetDetectorsSourceBefore(detectors, currentRecord.GetPreviousRecord().GetSource());
                        detectors = GetDetectRefactorings(detectors, detectedRefactorings);
                    }

                    if (detectedRefactorings.Any())
                    {
                        foreach (var detectedRefactoring in detectedRefactorings)
                        {
                            detectedRefactoring.Refactoring.MetaData.DocumentId = documentId;
                            detectedRefactoring.Refactoring.MetaData.DocumentUniqueName =
                                documentId.UniqueName;
                        }
                        OnRefactoringDetected(detectedRefactorings);
                    }
                    else
                    {
                        OnNoRefactoringDetected(latestRecord);
                    }
                }
                catch (Exception e)
                {
                    logger.Fatal(e);
                }
            }

            private IEnumerable<IExternalRefactoringDetector> GetRefactoringDetectors()
            {
                return GlobalConfigurations.GetSupportedRefactoringTypes().Select
                    (RefactoringDetectorFactory.GetRefactoringDetectorByType);
            }

            private void SetDetectorsSourceBefore(IEnumerable<IExternalRefactoringDetector> detectors,
                string before)
            {
                foreach (var detector in detectors)
                {
                    detector.SetSourceBefore(before);
                }
            }

            private IEnumerable<IExternalRefactoringDetector> GetActiveDetectors
                (IEnumerable<IExternalRefactoringDetector> detectors, int lookBack)
            {
                return detectors.Where(d => GlobalConfigurations.GetSearchDepth(d.RefactoringType) > lookBack);
            }

            private void SetDetectorsSourceAfter(IEnumerable<IExternalRefactoringDetector> detectors,
                string after)
            {
                foreach (var detector in detectors)
                {
                    detector.SetSourceAfter(after);
                }
            }

            private IEnumerable<IExternalRefactoringDetector> GetDetectRefactorings
                (IEnumerable<IExternalRefactoringDetector> detectors,
                    List<DetectedRefactoring> detectedRefactorings)
            {
                var successfulDetectors = detectors.Where(d => d.HasRefactoring());
                detectedRefactorings.AddRange(successfulDetectors.Select
                    (d => new DetectedRefactoring(d.GetBeforeDocument(), d.GetAfterDocument(),
                        d.GetRefactorings().First())));
                return detectors.Except(successfulDetectors);
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


            private void OnRefactoringDetected(IEnumerable<DetectedRefactoring> detectedrefactorings)
            {

            }
            private void OnNoRefactoringDetected(ICodeHistoryRecord after)
            {

            }
        }
    }
}
