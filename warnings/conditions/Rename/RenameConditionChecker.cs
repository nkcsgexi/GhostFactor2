using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Services;
using warnings.refactoring;

namespace warnings.conditions
{
    /* all the condition checkers for rename refactoring shall derive from this. */
    abstract class RenameConditionChecker : IRefactoringConditionChecker
    {
        public RefactoringType RefactoringType
        {
            get { return RefactoringType.RENAME; }
        }

        public abstract ICodeIssueComputer CheckCondition(IDocument before, IDocument after, IManualRefactoring input);
    }

    /* This class includes all the conditions to check for rename refactoring. */
    internal class RenameConditionsList : RefactoringConditionsList
    {
        private static Lazy<RenameConditionsList> instance = new Lazy<RenameConditionsList>();

        public static IRefactoringConditionsList GetInstance()
        {
            if (instance.IsValueCreated)
                return instance.Value;
            return new RenameConditionsList();
        }

        private RenameConditionsList()
        {
        }

        protected override IEnumerable<IRefactoringConditionChecker> GetAllConditionCheckers()
        {
            List<IRefactoringConditionChecker> list = new List<IRefactoringConditionChecker>();
            // Add all the checkers here.

            return list.AsEnumerable();
        }

        public override RefactoringType RefactoringType
        {
            get { return RefactoringType.RENAME; }
        }
    }
}
