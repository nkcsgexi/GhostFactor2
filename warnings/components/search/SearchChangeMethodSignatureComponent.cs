using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.conditions;
using warnings.configuration;
using warnings.quickfix;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.retriever;
using warnings.source;
using warnings.source.history;
using warnings.util;

namespace warnings.components
{
    internal class SearchChangeMethodSignatureComponent : SearchRefactoringComponent
    {
        private static readonly ISearchRefactoringComponent instance = new SearchChangeMethodSignatureComponent();

        public static ISearchRefactoringComponent GetInstance()
        {
            return instance;
        }

        public override string GetName()
        {
            return "SearchChangeMethodSignatureComponent";
        }

        public override Logger GetLogger()
        {
            return NLoggerUtil.GetNLogger(typeof (SearchChangeMethodSignatureComponent));
        }

        public override void StartRefactoringSearch(ICodeHistoryRecord record)
        {
            Enqueue(new SearchChangeMethodSignatureWorkItem(record));
        }

        private class SearchChangeMethodSignatureWorkItem : SearchRefactoringWorkitem
        {
            public SearchChangeMethodSignatureWorkItem(ICodeHistoryRecord latestRecord)
                : base(latestRecord)
            {
            }

            protected override IExternalRefactoringDetector GetRefactoringDetector()
            {
                return RefactoringDetectorFactory.CreateChangeMethodSignatureDetector();
            }

            protected override int GetSearchDepth()
            {
                return GlobalConfigurations.GetSearchDepth(RefactoringType.CHANGE_METHOD_SIGNATURE);
            }

            protected override void OnRefactoringDetected(IDocument before, IDocument after,
                                                          IEnumerable<ManualRefactoring> refactorings)
            {
                logger.Info("Change Method Signature Detected.");

                // Enqueue the condition checking process for this detected refactoring.
                GhostFactorComponents.conditionCheckingComponent.CheckRefactoringCondition(before, after, refactorings.First());
            }

            protected override void OnNoRefactoringDetected(ICodeHistoryRecord record)
            {
                //logger.Info("No change method signature detected.");
            }

            public override Logger GetLogger()
            {
                return NLoggerUtil.GetNLogger(typeof (SearchChangeMethodSignatureWorkItem));
            }

        }
    }
}
