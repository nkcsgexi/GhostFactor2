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
    public interface IConditionCheckingComponent{
        void CheckRefactoringCondition(ManualRefactoring refactoring);
    }

    /// <summary>
    ///  The component to handle condition checkings for all the refactoring types.
    /// </summary>
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
            queue = new WorkQueue();
            queue.ConcurrentLimit = 1;
            
            queue.FailedWorkItem += onFailedItem;
            queue.CompletedWorkItem += OnCompletedWorkItem;


            logger = NLoggerUtil.GetNLogger(typeof (ConditionCheckingComponent));
        }

        private void OnCompletedWorkItem(object sender, WorkItemEventArgs workItemEventArgs)
        {
            var timable = workItemEventArgs.WorkItem as TimableWorkItem;
            if(timable != null)
            {
                logger.Info("Condition checking processing time:" + timable.GetProcessingTime());
            }
        }

        private void onFailedItem(object sender, WorkItemEventArgs workItemEventArgs)
        {
            logger.Fatal("Condition checking work item failed:\n" + 
                workItemEventArgs.WorkItem.FailedException);
        }

        public void CheckRefactoringCondition(ManualRefactoring 
            refactoring)
        {
            queue.Add(new ConditionCheckWorkItem(refactoring));
        }

        /// <summary>
        /// The work item to be pushed to the condition checking component.
        /// </summary>
        private class ConditionCheckWorkItem : TimableWorkItem
        {
            // The refactoring instance from detector. 
            private readonly ManualRefactoring refactoring;

            private readonly Logger logger;

            public ConditionCheckWorkItem(ManualRefactoring refactoring)
            {
                this.refactoring = refactoring;
                logger = NLoggerUtil.GetNLogger(typeof(ConditionCheckWorkItem));
            }

            public override void Perform()
            {
                IEnumerable<IConditionCheckingResult> results = Enumerable.Empty<ICodeIssueComputer>();

                // Get the condition list corresponding to the refactoring type and check all 
                // of the conditions.
                var list = ConditionCheckingFactory.GetConditionsListByRefactoringType
                    (refactoring.RefactoringType);
                results = list.CheckAllConditions(refactoring);

                var correctRefactorings = results.OfType<ICorrectRefactoringResult>();
                var issueComputers = results.OfType<ICodeIssueComputer>();

                if(correctRefactorings.Any())
                {
                    // Try to resolve existing code issue computers by the correct refactorings.
                    GhostFactorComponents.RefactoringCodeIssueComputerComponent.
                        TryToResolveExistingIssueComputers(correctRefactorings);
                }

                if(issueComputers.Any())
                {
                    // Add issue computers to the issue component.
                    GhostFactorComponents.RefactoringCodeIssueComputerComponent.AddCodeIssueComputers
                        (issueComputers);
                }
            }
        }
    }
}


