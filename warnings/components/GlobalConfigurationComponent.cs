using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BlackHen.Threading;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.conditions;
using warnings.refactoring;
using warnings.util;

namespace warnings.components
{
    public delegate void SupportedRefactoringTypesChanged(IEnumerable<RefactoringType> currentTypes);

    /// <summary>
    /// Global configurations for GhostFactor.
    /// </summary>
    public class GlobalConfigurationComponent
    {
        private static GlobalConfigurationComponent instance;
        public static GlobalConfigurationComponent GetInstance()
        {
            return instance ?? (instance = new GlobalConfigurationComponent());
        }

        public SupportedRefactoringTypesChanged supportedRefactoringTypesChangedEvent;
        
        private readonly List<RefactoringType> supportedTypes;
        private readonly WorkQueue queue;
        private readonly Logger logger;
        private ISolution solution;

        private GlobalConfigurationComponent()
        {
            supportedTypes = new List<RefactoringType>();
            queue = new WorkQueue {ConcurrentLimit = 1, WorkerPool = new WorkThreadPool(1, 1)};            
            queue.FailedWorkItem += OnItemFailed;
            logger = NLoggerUtil.GetNLogger(typeof (GlobalConfigurationComponent));
        }

        private void OnItemFailed(object sender, WorkItemEventArgs workItemEventArgs)
        {
            logger.Fatal("Work item failed: " + workItemEventArgs.WorkItem);
            logger.Fatal(Environment.NewLine + workItemEventArgs.WorkItem.FailedException);
        }

        /// <summary>
        /// Add a supported refactoring type.
        /// </summary>
        /// <param name="type"></param>
        public void AddSupportedRefactoringTypes(IEnumerable<RefactoringType> types)
        {
            lock (supportedTypes)
            {
                foreach (var type in types)
                {
                    if (!supportedTypes.Contains(type))
                    {
                        supportedTypes.Add(type);
                    }
                }
                supportedRefactoringTypesChangedEvent(supportedTypes);
            }
        }
        
        /// <summary>
        /// Remove a supported refactoring type.
        /// </summary>
        /// <param name="type"></param>
        public void RemoveSupportedRefactoringTypes(IEnumerable<RefactoringType> types)
        {
            lock (supportedTypes)
            {
                foreach (var type in types)
                {
                    if (supportedTypes.Contains(type))
                    {
                        supportedTypes.Remove(type);
                    }
                }
                supportedRefactoringTypesChangedEvent(supportedTypes);
            }
        }

        /// <summary>
        /// Whether a given refactoring RefactoringType is supported by GhostFactor.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsSupported(RefactoringType type)
        {
            lock (supportedTypes)
            {
                return supportedTypes.Contains(type);
            }
        }

       


        /// <summary>
        /// Get the time interval between two snapshots, in millisencond.
        /// </summary>
        /// <returns></returns>
        public int GetSnapshotTakingInterval()
        {
            return 5000;
        }

        /// <summary>
        /// Get the time interval between two queries of all the refactoring warnings in the solution, 
        /// used by the refactoring form. 
        /// </summary>
        /// <returns></returns>
        public int GetRefactoringWarningListUpdateInterval()
        {
            return 6000;
        }


        public bool ShutDown()
        {
            return false;
        }


        /// <summary>
        /// Get the search depth for different refactoring types, how many snapshots to look back.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetSearchDepth(RefactoringType type)
        {
            switch (type)
            {
                case RefactoringType.RENAME:
                    return 10;
                case RefactoringType.EXTRACT_METHOD:
                    return 30;
                case RefactoringType.CHANGE_METHOD_SIGNATURE:
                    return 10;
                case RefactoringType.INLINE_METHOD:
                    return 10;
                default:
                    throw new Exception("Unknown Refactoring Type.");
            }
        }


        /// <summary>
        /// Whether current running support quick fix of refactoring warnings.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool SupportQuickFix(RefactoringConditionType type)
        {
            switch (type)
            {
                case RefactoringConditionType.EXTRACT_METHOD_RETURN_VALUE:
                    return true;
                case RefactoringConditionType.EXTRACT_METHOD_PARAMETER:
                    return false;
                case RefactoringConditionType.INLINE_METHOD_MODIFIED_DATA:
                    return false;
                case RefactoringConditionType.CHANGE_METHOD_SIGNATURE_UNUPDATED:
                    return false;
                default:
                    throw new Exception("Unknown Refactoring Type.");
            }
        }

        /// <summary>
        /// Get the number of maximum meaningful versions of a same source file.
        /// </summary>
        /// <returns></returns>
        public int GetHistoryRecordsMaximumLength()
        {
            return RefactoringTypeUtil.GetAllValidRefactoringTypes().
                Select(GetSearchDepth).Max() + 10;
        }

        /// <summary>
        /// Get all refatoring types that are currently supported.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RefactoringType> GetSupportedRefactoringTypes()
        {
            return RefactoringTypeUtil.GetAllValidRefactoringTypes().Where(IsSupported);
        }

        /// <summary>
        /// Get the global filters to the input syntax node. It is likely to issue a problem to the node 
        /// iff the node met with at least one of the filters.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Predicate<SyntaxNode>> GetIssuedNodeFilters()
        {
            return GetSupportedRefactoringTypes().SelectMany(t => ConditionCheckingFactory.
                GetConditionsListByRefactoringType(t).GetIssuedNodeFilters());
        }

        public void SetSolution(ISolution solution)
        {
            this.solution = solution;
        }

        public ISolution GetSolution()
        {
            return solution;
        }

        
        public WorkQueue GetGlobalWorkQueue()
        {
            return queue;
        }
    }
}
