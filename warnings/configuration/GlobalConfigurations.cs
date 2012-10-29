using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using warnings.refactoring;

namespace warnings.configuration
{
    /* Global configurations for GhostFactor.*/
    public class GlobalConfigurations
    {
        /* Whether a given refactoring RefactoringType is supported by GhostFactor. */
        public static bool IsSupported(RefactoringType type)
        {
            switch (type)
            {
                case RefactoringType.RENAME:
                    return false;
                case RefactoringType.EXTRACT_METHOD:
                    return true;
                case RefactoringType.CHANGE_METHOD_SIGNATURE:
                    return false;
                case RefactoringType.INLINE_METHOD:
                    return true;
                default:
                    throw new Exception("Unknown Refactoring Type.");
            }
        }

        /* Get the time interval between two snapshots, in millisencond. */
        public static int GetSnapshotTakingInterval()
        {
            return 2000;
        }

        /* 
         * Get the time interval between two queries of all the refactoring warnings in the solution, used by
         * the refactoring form. 
         */
        public static int GetRefactoringWarningListUpdateInterval()
        {
            return 6000;
        }


        /* Get the search depth for different refactoring types, how many snapshots to look back. */
        public static int GetSearchDepth(RefactoringType type)
        {
            switch (type)
            {
                case RefactoringType.RENAME:
                    return 30;
                case RefactoringType.EXTRACT_METHOD:
                    return 30;
                case RefactoringType.CHANGE_METHOD_SIGNATURE:
                    return 30;
                case RefactoringType.INLINE_METHOD:
                    return 30;
                default:
                    throw new Exception("Unknown Refactoring Type.");
            }
        }
    }
}
