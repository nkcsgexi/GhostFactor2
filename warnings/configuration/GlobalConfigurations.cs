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
                    return false;
                default:
                    throw new Exception("Unknown Refactoring Type.");
            }
        }

        /* Get the time interval between two snapshots, in millisencond. */
        public static int GetSnapshotTakingInterval()
        {
            return 5000;
        }

        /* 
         * Get the time interval between two queries of all the refactoring warnings in 
         * the solution, used by the refactoring form. 
         */
        public static int GetRefactoringWarningListUpdateInterval()
        {
            return 6000;
        }


        public static bool ShutDown()
        {
            return false;
        }


        /* Get the search depth for different refactoring types, how many snapshots to look back. */
        public static int GetSearchDepth(RefactoringType type)
        {
            switch (type)
            {
                case RefactoringType.RENAME:
                    return 10;
                case RefactoringType.EXTRACT_METHOD:
                    return 10;
                case RefactoringType.CHANGE_METHOD_SIGNATURE:
                    return 10;
                case RefactoringType.INLINE_METHOD:
                    return 10;
                default:
                    throw new Exception("Unknown Refactoring Type.");
            }
        }

        /// <summary>
        /// Get the number of maximum meaningful versions of a same source file.
        /// </summary>
        /// <returns></returns>
        public static int GetHistoryRecordsMaximumLength()
        {
            return RefactoringTypeUtil.GetAllValidRefactoringTypes().
                Select(GetSearchDepth).Max() + 10;
        }

        /// <summary>
        /// Get all refatoring types that are currently supported.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<RefactoringType> GetSupportedRefactoringTypes()
        {
            return RefactoringTypeUtil.GetAllValidRefactoringTypes().Where(IsSupported);
        }

    }
}
