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

    public class RefactoringTypeUtil
    {
        public static IEnumerable<RefactoringType> GetAllValidRefactoringTypes()
        {
            var types = Enum.GetValues(typeof(RefactoringType)).Cast<RefactoringType>();
            return types.Where(t => t != RefactoringType.UNKOWN);
        }
    }
}
