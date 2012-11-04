using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using warnings.refactoring;

namespace warnings.conditions
{
    public class ConditionCheckingFactory
    {
        public static IRefactoringConditionsList GetConditionsListByRefactoringType
            (RefactoringType type)
        {
            switch (type)
            {
                case RefactoringType.RENAME:
                    return RenameConditionsList.GetInstance();
                case RefactoringType.INLINE_METHOD:
                    return InlineMethodConditionCheckersList.GetInstance();
                case RefactoringType.EXTRACT_METHOD:
                    return ExtractMethodConditionsList.GetInstance();
                case RefactoringType.CHANGE_METHOD_SIGNATURE:
                    return ChangeMethodSignatureConditionsList.GetInstance();
                default:
                    throw new Exception("Unsupported Condition list.");
            }
        }
    }
}
