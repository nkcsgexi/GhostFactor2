using System;
using System.Collections.Generic;
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
    /* Component for detecting manually conducted rename refactoring. */
    internal class SearchRenameComponent : SearchRefactoringComponent
    {
        /* Singleton this component. */
        private static ISearchRefactoringComponent instance = new SearchRenameComponent();

        public static ISearchRefactoringComponent GetInstance()
        {
            return instance;
        }

        private SearchRenameComponent()
        {
            
        }

        public override string GetName()
        {
            return "Search Rename Component";
        }

        public override Logger GetLogger()
        {
            return NLoggerUtil.GetNLogger(typeof (SearchRenameComponent));
        }

        public override void StartRefactoringSearch(ICodeHistoryRecord record)
        {
            Enqueue(new SearchRenameWorkItem(record));
        }

        /* Item to be schedule to rename searching component. */
        private class SearchRenameWorkItem : SearchRefactoringWorkitem
        {
            public SearchRenameWorkItem(ICodeHistoryRecord latestRecord)
                : base(latestRecord)
            {
            }

            protected override IExternalRefactoringDetector GetRefactoringDetector()
            {
                return RefactoringDetectorFactory.CreateRenameDetector();
            }

            protected override int GetSearchDepth()
            {
                return GlobalConfigurations.GetSearchDepth(RefactoringType.RENAME);
            }

            protected override void OnRefactoringDetected(IDocument before, IDocument after,
                IEnumerable<ManualRefactoring> refactorings)
            {
                logger.Info("Rename dectected.");
            }

            protected override void OnNoRefactoringDetected(ICodeHistoryRecord record)
            {
                //logger.Info("No Rename Detected.");
            }

            public override Logger GetLogger()
            {
                return NLoggerUtil.GetNLogger(typeof(SearchRenameWorkItem));
            }
        }
    }
}
