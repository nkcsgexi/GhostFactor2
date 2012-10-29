using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using BlackHen.Threading;
using NLog;
using Roslyn.Services;
using Roslyn.Services.Editor;
using warnings.configuration;
using warnings.ui;
using warnings.util;

namespace warnings.components.ui
{
    /* delegate for update a control component. */
    public delegate void UIUpdate();

    /*
     * This the view part in the MVC pattern. It registers to the event of code issue changes. When code issues change, this component
     * will ask the latest issues and update the form.
     */
    internal class RefactoringFormViewComponent : IFactorComponent
    {
        /* Singleton this component. */
        private static IFactorComponent instance = new RefactoringFormViewComponent();

        public static IFactorComponent GetInstance()
        {
            return instance;
        }

        /* A work queue for long running task, such as keeping the windows displaying. */
        private WorkQueue longRunningQueue;

        /* A work queue for short running task, such as updating items to the form. */
        private WorkQueue shortTaskQueue;
    
        /* The form instance where new warnings should be added to. */
        private RefactoringWariningsForm form;

        private RefactoringFormViewComponent()
        {
            form = new RefactoringWariningsForm();
            longRunningQueue = new WorkQueue() {ConcurrentLimit = 1};
            shortTaskQueue = new WorkQueue(){ConcurrentLimit = 1};
            GhostFactorComponents.RefactoringCodeIssueComputerComponent.AddGlobalWarnings += OnAddGlobalWarnings;
            GhostFactorComponents.RefactoringCodeIssueComputerComponent.RemoveGlobalWarnings += OnRemoveGlobalWarnings;
            GhostFactorComponents.RefactoringCodeIssueComputerComponent.ProblematicRefactoringCountChanged += OnProblematicRefactoringsCountChanged;
        }

        private void OnProblematicRefactoringsCountChanged(int newCount)
        {
            shortTaskQueue.Add(new ResetRefactoringCountWorkItem(form, newCount));
        }

        private void OnRemoveGlobalWarnings(Predicate<IRefactoringWarningMessage> removeCondition)
        {
            shortTaskQueue.Add(new RemoveWarningsWorkItem(form, removeCondition));
        }

        private void OnAddGlobalWarnings(IEnumerable<IRefactoringWarningMessage> messages)
        {
            shortTaskQueue.Add(new AddWarningsWorkItem(form, messages));
        }


        public void Enqueue(IWorkItem item)
        {
            shortTaskQueue.Add(item);
        }

        public string GetName()
        {
            return "Refactoring Form Component";
        }

        public int GetWorkQueueLength()
        {
            return shortTaskQueue.Count;
        }

        public void Start()
        {
            // Create an work item for showing dialog and add this work item
            // to the work longRunningQueue.
            longRunningQueue.Add(new ShowingFormWorkItem(form));
        }

        /* Work item for adding refactoring errors in the form. */
        private class AddWarningsWorkItem : WorkItem
        {
            private readonly RefactoringWariningsForm form;
            private readonly Logger logger;
            private readonly IEnumerable<IRefactoringWarningMessage> messages;

            internal AddWarningsWorkItem(RefactoringWariningsForm form, 
                IEnumerable<IRefactoringWarningMessage> messages)
            {
                this.form = form;
                this.messages = messages;
                this.logger = NLoggerUtil.GetNLogger(typeof (AddWarningsWorkItem));
            }

            public override void Perform()
            {
                // Add messages to the form. 
                form.Invoke(new UIUpdate(AddRefactoringWarnings));
            }

            private void AddRefactoringWarnings()
            {
                logger.Info("Adding messages to the form.");
                form.AddRefactoringWarnings(messages);
            }
        }

        /* This work item is for removing warnings in the refactoring warning list. */
        private class RemoveWarningsWorkItem : WorkItem
        {
            private readonly Predicate<IRefactoringWarningMessage> removeCondition;
            private readonly RefactoringWariningsForm form;

            internal RemoveWarningsWorkItem(RefactoringWariningsForm form, 
                Predicate<IRefactoringWarningMessage> removeCondition)
            {
                this.form = form;
                this.removeCondition = removeCondition;
            }

            public override void Perform()
            {
                // Invoke the delegate method to remove warnings.
                form.Invoke(new UIUpdate(RemoveWarnings));
            }

            private void RemoveWarnings()
            {
                form.RemoveRefactoringWarnings(removeCondition);
            }
        }

        /* Reset the refactoring count showing in the form. */
        private class ResetRefactoringCountWorkItem : WorkItem
        {
            private readonly RefactoringWariningsForm form;
            private readonly int newCount;

            internal ResetRefactoringCountWorkItem(RefactoringWariningsForm form, int newCount)
            {
                this.form = form;
                this.newCount = newCount;
            }

            public override void Perform()
            {
                form.Invoke(new UIUpdate(ResetRefactoringCount));
            }

            private void ResetRefactoringCount()
            {
                form.SetProblematicRefactoringsCount(newCount);
            }
        }


        /* Work item for showing the form, unlike other workitem, this work item does not stop. */
        private class ShowingFormWorkItem : WorkItem
        {
            private readonly Form form;

            internal ShowingFormWorkItem(Form form)
            {
                this.form = form;
            }

            public override void Perform()
            {
                form.ShowDialog();
            }
        }
    }
}
