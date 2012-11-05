using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using warnings.conditions;
using warnings.refactoring;

namespace warnings.configuration
{
    /// <summary>
    /// Global configurations for GhostFactor.
    /// </summary>
    public class GlobalConfigurations
    {
        /// <summary>
        /// Whether a given refactoring RefactoringType is supported by GhostFactor.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsSupported(RefactoringType type)
        {
            switch (type)
            {
                case RefactoringType.RENAME:
                    return false;
                case RefactoringType.EXTRACT_METHOD:
                    return false;
                case RefactoringType.CHANGE_METHOD_SIGNATURE:
                    return false;
                case RefactoringType.INLINE_METHOD:
                    return true;
                default:
                    throw new Exception("Unknown Refactoring Type.");
            }
        }

        /// <summary>
        /// Get the time interval between two snapshots, in millisencond.
        /// </summary>
        /// <returns></returns>
        public static int GetSnapshotTakingInterval()
        {
            return 5000;
        }

        /// <summary>
        /// Get the time interval between two queries of all the refactoring warnings in the solution, 
        /// used by the refactoring form. 
        /// </summary>
        /// <returns></returns>
        public static int GetRefactoringWarningListUpdateInterval()
        {
            return 6000;
        }


        public static bool ShutDown()
        {
            return false;
        }


        /// <summary>
        /// Get the search depth for different refactoring types, how many snapshots to look back.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the global filters to the input syntax node. It is likely to issue a problem to the node 
        /// iff the node met with at least one of the filters.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Predicate<SyntaxNode>> GetIssuedNodeFilters()
        {
            return GetSupportedRefactoringTypes().SelectMany(t => ConditionCheckingFactory.
                GetConditionsListByRefactoringType(t).GetIssuedNodeFilters());
        }
    }
}
