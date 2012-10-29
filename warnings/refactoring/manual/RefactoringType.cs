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
}
