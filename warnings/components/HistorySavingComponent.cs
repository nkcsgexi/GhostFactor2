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
    public delegate void WorkOnDocumentChanged(IDocument document);

    public interface IHistorySavingComponent
    {
        event WorkOnDocumentChanged OnWorkDocumentChanged;
        void UpdateDocument(IDocument document);
    }

    /// <summary>
    /// Component for recording new version of a source code file.
    /// </summary>
    public class HistorySavingComponent : IHistorySavingComponent
    {
        private static HistorySavingComponent instance = new HistorySavingComponent();

        public static IHistorySavingComponent GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// Internal work queue for handling all the tasks.
        /// </summary>
        private readonly WorkQueue queue;

        /// <summary>
        /// logger for the history saving component.
        /// </summary>
        private readonly Logger logger;

        private HistorySavingComponent()
        {
            this.queue = GhostFactorComponents.configurationComponent.GetGlobalWorkQueue();           
            logger = NLoggerUtil.GetNLogger(typeof (HistorySavingComponent));         
        }

        public event WorkOnDocumentChanged OnWorkDocumentChanged;

        public void UpdateDocument(IDocument newDoc)
        {
            queue.Add(new HistorySavingWorkItem(newDoc, OnWorkDocumentChanged));
        }

        /// <summary>
        /// The work item supposed to added to HistorySavingComponent. 
        /// </summary>
        private class HistorySavingWorkItem : TimableWorkItem
        {
            private static readonly SavedDocumentRecords records = new SavedDocumentRecords();
            private static readonly Logger logger = NLoggerUtil.GetNLogger
                (typeof (HistorySavingWorkItem));

            private readonly IDocument document;
            private readonly WorkOnDocumentChanged onWorkOnDocumentChanged;

            /// <summary>
            /// Retrieve all the properties needed to save this new record.
            /// </summary>
            /// <param name="document"></param>
            /// <param name="onWorkOnDocumentChanged"></param>
            internal HistorySavingWorkItem(IDocument document, WorkOnDocumentChanged onWorkOnDocumentChanged)
            {
                this.document = document;
                this.onWorkOnDocumentChanged = onWorkOnDocumentChanged;
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
                    onWorkOnDocumentChanged(document);
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
            
             
             /// <summary>
             /// This class records whether a newly coming document is an update from its previous version,  
             /// reducing the redundancy of saving multiple versions of same code.
             /// </summary>      
            private class SavedDocumentRecords
            {
                /* Dictionary saves document id and its version number of its latest saved code.*/
                private readonly Dictionary<string, VersionStamp> dictionary = 
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
