using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Services;
using warnings.configuration;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.source.history;
using warnings.util;

namespace warnings.components
{
    internal class SearchInlineMethodComponent : SearchRefactoringComponent
    {
        private static ISearchRefactoringComponent instance = new SearchInlineMethodComponent();

        public static ISearchRefactoringComponent GetInstance()
        {
            return instance;
        }

        public override string GetName()
        {
            return "Search inline method refactoring component.";
        }

        public override Logger GetLogger()
        {
            return NLoggerUtil.GetNLogger(typeof (SearchInlineMethodComponent));
        }

        public override void StartRefactoringSearch(ICodeHistoryRecord record)
        {
            Enqueue(new SearchInlineMethodWorkItem(record));
        }

        private class SearchInlineMethodWorkItem : SearchRefactoringWorkitem
        {
            public SearchInlineMethodWorkItem(ICodeHistoryRecord latestRecord) : base(latestRecord)
            {
            }

            protected override IExternalRefactoringDetector GetRefactoringDetector()
            {
                return RefactoringDetectorFactory.CreateInlineMethodDetector();
            }

            protected override int GetSearchDepth()
            {
                return GlobalConfigurations.GetSearchDepth(RefactoringType.INLINE_METHOD);
            }

            protected override void OnRefactoringDetected(IDocument before, IDocument after, IEnumerable<IManualRefactoring> refactorings)
            {
                logger.Info("Inline method detected.");
                GhostFactorComponents.conditionCheckingComponent.CheckRefactoringCondition(before, after, 
                    refactorings.First());
            }

            protected override void OnNoRefactoringDetected(ICodeHistoryRecord after)
            {
            }

            public override Logger GetLogger()
            {
                return NLoggerUtil.GetNLogger(typeof (SearchInlineMethodWorkItem));
            }
        }

    }



}
