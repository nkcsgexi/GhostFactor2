using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Services;
using warnings.refactoring;

namespace warnings.conditions
{
    internal partial class ChangeMethodSignatureConditionsList : RefactoringConditionsList
    {
        private static IRefactoringConditionsList list;
        public static IRefactoringConditionsList GetInstance()
        {
            if(list == null)
                list = new ChangeMethodSignatureConditionsList();
            return list;
        }

        protected override IEnumerable<IRefactoringConditionChecker> GetAllConditionCheckers()
        {
            var checkers = new List<IRefactoringConditionChecker>();
            checkers.Add(UnupdatedMethodSignatureChecker.GetInstance());
            return checkers;
        }

        public override RefactoringType RefactoringType
        {
            get { return RefactoringType.CHANGE_METHOD_SIGNATURE; }
        }
    }
}
