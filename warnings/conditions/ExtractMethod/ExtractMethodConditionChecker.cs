using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.analyzer;
using warnings.refactoring;

namespace warnings.conditions
{
    /* Condition list for extract method. */
    internal partial class ExtractMethodConditionsList : RefactoringConditionsList
    {
        private ExtractMethodConditionsList()
        {
        }

        public static IRefactoringConditionsList CreateInstance()
        {
            return new ExtractMethodConditionsList();
        }

        protected override IEnumerable<IRefactoringConditionChecker> GetAllConditionCheckers()
        {
            var checkers = new List<IRefactoringConditionChecker>();
            checkers.Add(new ParametersChecker());
            checkers.Add(new ReturnTypeChecker());
            return checkers;
        }

        public override RefactoringType RefactoringType
        {
            get { return RefactoringType.EXTRACT_METHOD; }
        }

        /* All the condition checkers for extract method should implement this. */
        private abstract class ExtractMethodConditionChecker : IRefactoringConditionChecker
        {
            public RefactoringType RefactoringType
            {
                get { return RefactoringType.EXTRACT_METHOD; }
            }

            public IConditionCheckingResult CheckCondition(ManualRefactoring input)
            {
                return CheckCondition((IManualExtractMethodRefactoring)input);
            }

            public abstract Predicate<SyntaxNode> GetIssuedNodeFilter();

            protected abstract IConditionCheckingResult CheckCondition(
                IManualExtractMethodRefactoring refactoring);
            public abstract RefactoringConditionType RefactoringConditionType { get; }
        }
    }

}
