using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using BlackHen.Threading;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.analyzer;
using warnings.configuration;
using warnings.refactoring;
using warnings.source;
using warnings.source.history;
using warnings.util;

namespace warnings.components
{
    public interface IHistorySavingComponent : IFactorComponent
    {
        void UpdateActiveDocument(IDocument document);
    }

    /* Component for recording new version of a source code file.*/
    public class HistorySavingComponent : IHistorySavingComponent
    {
        /* Singleton the instance. */
        private static HistorySavingComponent instance = new HistorySavingComponent();

        public static IHistorySavingComponent GetInstance()
        {
            return instance;
        }

        /* Internal work queue for handling all the tasks. */
        private readonly WorkQueue queue;

        /* logger for the history saving component. */
        private readonly Logger logger;

        /* Current active document boxed to facilitate update. */
        private StrongBox<IDocument> activeDocumentBox;

        /* Timer for triggering saving current version. */
        private readonly ComponentTimer timer;

        /* Timer interval used by timer. */
        private readonly int TIME_INTERVAL = GlobalConfigurations.GetSnapshotTakingInterval();

        private HistorySavingComponent()
        {
            this.queue = new WorkQueue();
            this.queue.ConcurrentLimit = 1;
            this.queue.FailedWorkItem += onFailedWorkItem;
            this.queue.CompletedWorkItem += onCompleteWorkItem;

            // Initiate the component timer.
            this.timer = new ComponentTimer( TIME_INTERVAL, TimeUpHandler);

            logger = NLoggerUtil.GetNLogger(typeof (HistorySavingComponent));            
            activeDocumentBox = new StrongBox<IDocument>();
        }

        /* Add a new work item to the queue. */
        public void Enqueue(IWorkItem item)
        {
        }

        /* Return the name of this work queue. */
        public string GetName()
        {
            return "HistorySavingComponent";
        }

        /* The length of this work queue. */
        public int GetWorkQueueLength()
        {
            return queue.Count;
        }

        /* Start this component by starting the timing thread. */
        public void Start()
        {
            this.timer.start();
        }

        public void UpdateActiveDocument(IDocument newDoc)
        {
             queue.Add(new UpdateActiveDocumentWorkItem(activeDocumentBox, newDoc));
        }

        private class UpdateActiveDocumentWorkItem : WorkItem
        {
            private readonly IDocument document;
            private readonly StrongBox<IDocument> documentBox;

            internal UpdateActiveDocumentWorkItem(StrongBox<IDocument> documentBox, 
                IDocument document)
            {
                this.documentBox = documentBox;
                this.document = document;
            }

            public override void Perform()
            {
                documentBox.Value = document;
            }
        }

        /* handler when time up event is triggered. */
        private void TimeUpHandler(object o, EventArgs args)
        {
            // If no active document is not null, continue with saving.
            if (activeDocumentBox.Value != null)
            {
                // When timer is triggered, save current active file to the versions. 
                queue.Add(new HistorySavingWorkItem(activeDocumentBox.Value));
            }
        }

        private void onFailedWorkItem(object sender, WorkItemEventArgs workItemEventArgs)
        {
            logger.Fatal("WorkItem failed: " + workItemEventArgs.WorkItem.FailedException);
        }

        private void onCompleteWorkItem(object sender, WorkItemEventArgs e)
        {
            var timable = e.WorkItem as TimableWorkItem;
            if (timable != null)
            {
                var timeSpan = ((TimableWorkItem) e.WorkItem).GetProcessingTime();
                logger.Info("Work item processing time: " + timeSpan.TotalMilliseconds);
            }
        }

        /* The work item supposed to added to HistorySavingComponent. */
        private class HistorySavingWorkItem : TimableWorkItem
        {
            private readonly static SavedDocumentRecords records = new SavedDocumentRecords();
            private readonly Logger logger;
            private readonly IDocument document;

            /* Retrieve all the properties needed to save this new record. */
            internal HistorySavingWorkItem(IDocument document)
            {
                this.logger = NLoggerUtil.GetNLogger(typeof(HistorySavingWorkItem));
                this.document = document;
            }

            public override void Perform()
            {
                try
                {
                    if (records.IsDocumentUpdated(document))
                    {
                        var id = document.Id;
                        var code = document.GetText().GetText();
                        logger.Info("Saved document:" + id.UniqueName);

                        // Add the new IDocuemnt to the code history.
                        CodeHistory.GetInstance().AddRecord(id.UniqueName, code);

                        // Update the records of saved documents.
                        records.AddSavedDocument(document);

                        // Add work item to search component.
                        StartRefactoringSearch(id);
                    }
                }
                catch (Exception e)
                {
                    // Stacktrace of Exception will be logged.
                    logger.Fatal(e.StackTrace);
                }
            }

            private void StartRefactoringSearch(DocumentId documentId)
            {
                // Get the latest record of the file just editted.    
                ICodeHistoryRecord record = CodeHistory.GetInstance().
                    GetLatestRecord(documentId.UniqueName);
                GhostFactorComponents.searchRefactoringComponent.
                    StartRefactoringSearch(record, documentId);
            }
            
            /* 
             * This class records whether a newly coming document is an update from its previous version, 
             * reducing the redundancy of saving multiple versions of same code.
             */
            private class SavedDocumentRecords
            {
                /* Dictionary saves document id and its version number of its latest saved code.*/
                private Dictionary<string, VersionStamp> dictionary = 
                    new Dictionary<string, VersionStamp>();

                /* Whether we have saved a document before. */
                private bool HasSaved(IDocument document)
                {
                    return dictionary.ContainsKey(document.Id.UniqueName);
                }

                /* Whether a document differs from its previous version. */
                public bool IsDocumentUpdated(IDocument document)
                {
                    if (HasSaved(document))
                    {
                        VersionStamp version;
                        dictionary.TryGetValue(document.Id.UniqueName, out version);
                        return document.GetTextVersion().IsNewerThan(version);
                    }
                    return true;
                }

                /* Add a newly saved document to the dictionary for future reference. */
                public void AddSavedDocument(IDocument document)
                {
                    if (HasSaved(document))
                    {
                        dictionary.Remove(document.Id.UniqueName);
                    }
                    dictionary.Add(document.Id.UniqueName, 
                        document.GetTextVersion());
                }
            }
        }
    }
}
