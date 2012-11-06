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
using warnings.refactoring;
using warnings.source;
using warnings.source.history;
using warnings.util;

namespace warnings.components
{
    public interface IHistorySavingComponent
    {
        void UpdateDocument(IDocument document);
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

        private HistorySavingComponent()
        {
            this.queue = new WorkQueue();
            this.queue.ConcurrentLimit = 1;
            this.queue.FailedWorkItem += onFailedWorkItem;
            this.queue.CompletedWorkItem += onCompleteWorkItem;

            logger = NLoggerUtil.GetNLogger(typeof (HistorySavingComponent));            
        }

   
        public void UpdateDocument(IDocument newDoc)
        {
            queue.Add(new HistorySavingWorkItem(newDoc));
        }

        
        private void onFailedWorkItem(object sender, WorkItemEventArgs workItemEventArgs)
        {
            logger.Fatal("Save code history record failed:\n" + 
                workItemEventArgs.WorkItem.FailedException);
        }

        private void onCompleteWorkItem(object sender, WorkItemEventArgs e)
        {

        }

        /* The work item supposed to added to HistorySavingComponent. */
        private class HistorySavingWorkItem : TimableWorkItem
        {
            private static readonly SavedDocumentRecords records = new SavedDocumentRecords();
            private static readonly Logger logger = NLoggerUtil.GetNLogger
                (typeof (HistorySavingWorkItem));
            private readonly IDocument document;

            /* Retrieve all the properties needed to save this new record. */
            internal HistorySavingWorkItem(IDocument document)
            {
                this.document = document;
            }

            public override void Perform()
            {
                if (records.IsDocumentUpdated(document))
                {
                    var id = document.Id;
                    var code = document.GetText().GetText();
                    logger.Info("Saved document:" + id.UniqueName);

                    // Add the new IDocuemnt to the code history.
                    CodeHistory.GetInstance().AddRecord(id.UniqueName, code);
                    
                    records.AddSavedDocument(document);
                    StartRefactoringSearch(id);
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
