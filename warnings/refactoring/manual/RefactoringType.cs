using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace warnings.refactoring
{
    public enum RefactoringType
    {
        RENAME,
        EXTRACT_METHOD,
        CHANGE_METHOD_SIGNATURE,
        INLINE_METHOD,
        UNKOWN
    }

    public enum RefactoringConditionType
    {
        EXTRACT_METHOD_PARAMETER,
        EXTRACT_METHOD_RETURN_VALUE,
        CHANGE_METHOD_SIGNATURE_UNUPDATED,
        INLINE_METHOD_MODIFIED_DATA
    }


    public class RefactoringTypeUtil
    {
        public static IEnumerable<RefactoringType> GetAllValidRefactoringTypes()
        {
            var types = Enum.GetValues(typeof(RefactoringType)).Cast<RefactoringType>();
            return types.Where(t => t != RefactoringType.UNKOWN);
        }
    }
}
