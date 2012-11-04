using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            public ICodeIssueComputer CheckCondition(IDocument before, IDocument after, ManualRefactoring input)
            {
                return CheckCondition(before, after, (IManualExtractMethodRefactoring)input);
            }

    
            protected abstract ICodeIssueComputer CheckCondition(IDocument before, IDocument after, 
                IManualExtractMethodRefactoring input);
        }
    }

}
