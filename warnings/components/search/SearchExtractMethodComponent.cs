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
using warnings.source.history;
using warnings.util;

namespace warnings.components
{
    /* This component is for detecting manual extract method refactoring in the code history. */
    internal class SearchExtractMethodComponent : SearchRefactoringComponent
    {
        /* Singleton this component. */
        private static ISearchRefactoringComponent instance = new SearchExtractMethodComponent();

        public static ISearchRefactoringComponent getInstance()
        {
            return instance;
        }

        private SearchExtractMethodComponent()
        {
        }

        public override string GetName()
        {
            return "Search Extract Method Component";
        }

        public override Logger GetLogger()
        {
            return NLoggerUtil.GetNLogger(typeof (SearchExtractMethodComponent));
        }

        public override void StartRefactoringSearch(ICodeHistoryRecord record)
        {
             Enqueue(new SearchExtractMethodWorkitem(record));
        }

        /* The kind of work item for search extract method component. */
        private class SearchExtractMethodWorkitem : SearchRefactoringWorkitem
        {
            public SearchExtractMethodWorkitem(ICodeHistoryRecord latestRecord)
                : base(latestRecord)
            {
            }

            protected override IExternalRefactoringDetector GetRefactoringDetector()
            {
                return RefactoringDetectorFactory.CreateExtractMethodDetectorBasedOnCommonStatements();
            }

            protected override int GetSearchDepth()
            {
                return GlobalConfigurations.GetSearchDepth(RefactoringType.EXTRACT_METHOD);
            }

            protected override void OnRefactoringDetected(IDocument before, IDocument after,
                IEnumerable<IManualRefactoring> refactorings)
            {
                logger.Info("Extract Method dectected.");
                logger.Info(refactorings.First().ToString());

                // Get the first refactoring detected.
                IManualRefactoring refactoring = refactorings.First();

                // Enqueue workitem for conditions checking component.
                GhostFactorComponents.conditionCheckingComponent.CheckRefactoringCondition(before, after, refactoring);
            }

            protected override void OnNoRefactoringDetected(ICodeHistoryRecord record)
            {
                logger.Info("No extract method detected.");
            }

            public override Logger GetLogger()
            {
                return NLoggerUtil.GetNLogger(typeof(SearchExtractMethodWorkitem));
            }
        }
    }
}
