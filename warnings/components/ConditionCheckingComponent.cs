using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlackHen.Threading;
using NLog;
using Roslyn.Services;
using warnings.conditions;
using warnings.refactoring;
using warnings.source.history;
using warnings.util;

namespace warnings.components
{
    public interface IConditionCheckingComponent : IFactorComponent
    {
        void CheckRefactoringCondition(IDocument before, IDocument after, ManualRefactoring refactoring);
    }

    /* The component to handle condition checkings for all the refactoring types. */
    internal class ConditionCheckingComponent : IConditionCheckingComponent
    {
        private static IConditionCheckingComponent instance = new ConditionCheckingComponent();

        public static IConditionCheckingComponent GetInstance()
        {
            return instance;
        }
   
        private readonly WorkQueue queue;
        private readonly Logger logger;

        private ConditionCheckingComponent()
        {
            // A single thread workqueue.
            queue = new WorkQueue();
            queue.ConcurrentLimit = 1;
            
            // Set listener for failed work item.
            queue.FailedWorkItem += onFailedItem;

            // Initiate the logger.
            logger = NLoggerUtil.GetNLogger(typeof (ConditionCheckingComponent));
        }

        private void onFailedItem(object sender, WorkItemEventArgs workItemEventArgs)
        {
            logger.Fatal("Work item failed: " + workItemEventArgs.WorkItem);
        }

        public void Enqueue(IWorkItem item)
        {
            queue.Add(item);
        }

        public string GetName()
        {
            return "Conditions Checking Component";
        }

        public int GetWorkQueueLength()
        {
            return queue.Count;
        }

        public void Start()
        {
        }

        public void CheckRefactoringCondition(IDocument before, IDocument after, ManualRefactoring refactoring)
        {
            Enqueue(new ConditionCheckWorkItem(before, after, refactoring));
        }

        /* The work item to be pushed to the condition checking component. */
        private class ConditionCheckWorkItem : WorkItem
        {
            // The refactoring instance from detector. 
            private readonly ManualRefactoring refactoring;

            // A document instance whose code is identical the detector's before code.
            private readonly IDocument after;

            // A document instance whose code is identical to the detector's after code.
            private readonly IDocument before;

            private readonly Logger logger;

            public ConditionCheckWorkItem(IDocument before, IDocument after, ManualRefactoring refactoring)
            {
                this.before = before;
                this.after = after;
                this.refactoring = refactoring;
                logger = NLoggerUtil.GetNLogger(typeof(ConditionCheckWorkItem));
            }

            public override void Perform()
            {
                try
                {
                    IEnumerable<ICodeIssueComputer> computers = Enumerable.Empty<ICodeIssueComputer>();
                    switch (refactoring.RefactoringType)
                    {
                        // Checking all conditions for extract method.
                        case RefactoringType.EXTRACT_METHOD:
                            logger.Info("Checking conditions for extract method.");
                            computers = ConditionCheckingFactory.GetExtractMethodConditionsList().
                                CheckAllConditions(before, after, refactoring);
                            break;

                        // Checking all conditions for rename.
                        case RefactoringType.RENAME:
                            logger.Info("Checking conditions for rename.");
                            computers = ConditionCheckingFactory.GetRenameConditionsList().
                                CheckAllConditions(before, after, refactoring);
                            break;

                        // Checking conditions for change method signature.
                        case RefactoringType.CHANGE_METHOD_SIGNATURE:
                            logger.Info("Checking conditions for change method signature.");
                            computers = ConditionCheckingFactory.GetChangeMethodSignatureConditionsList().
                                CheckAllConditions(before, after, refactoring);
                            break;

                        // Checking conditions for inline method refactorings.
                        case RefactoringType.INLINE_METHOD:
                            logger.Info("Checking conditions for inline method.");
                            computers = ConditionCheckingFactory.GetInlineMethodConditionsList().
                                CheckAllConditions(before, after, refactoring);
                            break;

                        default:
                            logger.Fatal("Unknown refactoring RefactoringType for conditions checking.");
                            break;
                    }

                    // Add issue computers to the issue component.
                    GhostFactorComponents.RefactoringCodeIssueComputerComponent.AddCodeIssueComputers(computers);
                }
                catch (Exception e)
                {
                    // All exception shall go to the fatal log.
                    logger.Fatal(e);
                }
            }
        }
    }

    
}


