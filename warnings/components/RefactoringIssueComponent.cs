using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BlackHen.Threading;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using warnings.analyzer;
using warnings.components.ui;
using warnings.conditions;
using warnings.quickfix;
using warnings.util;

namespace warnings.components
{
    /* Used for any listeners to get the new warnings of the entire solution. */
    public delegate void AddGlobalRefactoringWarnings(IEnumerable<IRefactoringWarningMessage> messages);

    /* Used for any listeners to the event of removing refactoring warnings. */
    public delegate void RemoveGlobalRefactoringWarnings(Predicate<IRefactoringWarningMessage> 
        removeCondition);

    /* Used for any listeners who are instersted at how many refactorings are problematic. */
    public delegate void ProblematicRefactoringsCountChanged(int newCount);


    /* A repository for issue computers to be queried, added, and deleted. */
    public interface ICodeIssueComputersRepository
    {
        event AddGlobalRefactoringWarnings AddGlobalWarnings;
        event RemoveGlobalRefactoringWarnings RemoveGlobalWarnings;
        event ProblematicRefactoringsCountChanged ProblematicRefactoringCountChanged;

        void TryToResolveExistingIssueComputers(IEnumerable<ICorrectRefactoringResult> correctRefactorings);
        void AddCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers);
        void RemoveCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers);
        IEnumerable<CodeIssue> GetCodeIssues(IDocument document, SyntaxNode node);
    }
  
    internal class RefactoringCodeIssueComputersComponent : ICodeIssueComputersRepository
    {
        private static readonly ICodeIssueComputersRepository intance =
            new RefactoringCodeIssueComputersComponent();

        public static ICodeIssueComputersRepository GetInstance()
        {
            return intance;
        }

        private readonly IList<ICodeIssueComputer> codeIssueComputers;
        private readonly WorkQueue queue;
        private readonly Logger logger;
        private readonly IEnumerable<Predicate<SyntaxNode>> nodeFilter;

        /* Used for any listener to the event of code issue computers added or removed. */
        private delegate void CodeIssueComputersAdded(IEnumerable<ICodeIssueComputer> newCodeIssueComputers);

        /* Event when new code issues are added.*/
        private event CodeIssueComputersAdded codeIssueComputersAddedEvent;

        /* Event when new global refactoring addWarningsEvent are ready to be added. */
        public event AddGlobalRefactoringWarnings AddGlobalWarnings;

        /* Event when old global refactoring addWarningsEvent are ready to be removed. */
        public event RemoveGlobalRefactoringWarnings RemoveGlobalWarnings;

        public event ProblematicRefactoringsCountChanged ProblematicRefactoringCountChanged;


 

        private RefactoringCodeIssueComputersComponent()
        {
            codeIssueComputers = new List<ICodeIssueComputer>();

            // Single thread workqueue.
            queue = new WorkQueue {ConcurrentLimit = 1};

            // Add a listener for failed work item.
            queue.FailedWorkItem += OnItemFailed;
            logger = NLoggerUtil.GetNLogger(typeof (RefactoringCodeIssueComputersComponent));
            nodeFilter = GhostFactorComponents.configurationComponent.GetIssuedNodeFilters();
            codeIssueComputersAddedEvent += OnCodeIssueComputersAdded;
        }

        /* When code issue computers are added, this method will be called. */
        private void OnCodeIssueComputersAdded(IEnumerable<ICodeIssueComputer> computers)
        {
            var item = new GetSolutionRefactoringWarningsWorkItem(GhostFactorComponents.configurationComponent
                .GetSolution(), computers, AddGlobalWarnings);
            queue.Add(item);
        }

        private void OnItemFailed(object sender, WorkItemEventArgs workItemEventArgs)
        {
            logger.Fatal("Work item failed: " + workItemEventArgs.WorkItem);
            logger.Fatal(workItemEventArgs.WorkItem.FailedException.StackTrace);
        }


        /// <summary>
        /// This method takes input of a list of correct refactorings and use these refactorings to query 
        /// about existing code issue computers. If these refactorings successfully resolve an existing code
        /// issue computer, then the code issue computer will be removed.
        /// </summary>
        /// <param name="correctRefactorings"></param>
        public void TryToResolveExistingIssueComputers(IEnumerable<ICorrectRefactoringResult> 
            correctRefactorings)
        {
            queue.Add(new ResolveExistingIssueComputerWorkItem(codeIssueComputers, correctRefactorings));
        }

        private class ResolveExistingIssueComputerWorkItem : WorkItem
        {
            private readonly IEnumerable<ICorrectRefactoringResult> correctRefactorings;
            private readonly IList<ICodeIssueComputer> codeIssueComputers;

            public ResolveExistingIssueComputerWorkItem(IList<ICodeIssueComputer> codeIssueComputers,
                IEnumerable<ICorrectRefactoringResult> correctRefactorings)
            {
                this.codeIssueComputers = codeIssueComputers;
                this.correctRefactorings = correctRefactorings;
            }

            public override void Perform()
            {
                var resolvedComputers = codeIssueComputers.Where(computer => correctRefactorings.Any
                    (computer.IsIssueResolved));
                foreach (var resolved in resolvedComputers)
                {
                    codeIssueComputers.Remove(resolved);
                }
            }
        }

        /// <summary>
        /// Add a set of condition checking results.
        /// </summary>
        /// <param name="computers"></param>
        public void AddCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers)
        {
            queue.Add(new AddCodeIssueComputersWorkItem(codeIssueComputers, computers));
        }

        /// <summary>
        /// Remove a list of code issue computers from the current list.
        /// </summary>
        /// <param name="computers"></param>
        public void RemoveCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers)
        {
            queue.Add(new RemoveCodeIssueComputersWorkItem(codeIssueComputers, computers));
        }

        /* Get the code issues in the given node of the given document. */
        public IEnumerable<CodeIssue> GetCodeIssues(IDocument document, SyntaxNode node)
        {
            // Check if an issue is likely to happen to the node.
            if (nodeFilter.Any(f => f.Invoke(node)))
            {
                // Create a work item for this task.
                var item = new GetDocumentNodeCodeIssueWorkItem(document, node, codeIssueComputers);
                new WorkItemSynchronizedExecutor(item, queue).Execute();
                return item.GetCodeIssues();
            }
            return Enumerable.Empty<CodeIssue>();
        }


        /// <summary>
        /// Work item to add new issue computers to the repository.
        /// </summary>
        private class AddCodeIssueComputersWorkItem : WorkItem
        {
            private readonly IList<ICodeIssueComputer> currentComputers;
            private readonly IEnumerable<ICodeIssueComputer> newComputers;
            private readonly Logger logger = NLoggerUtil.GetNLogger(typeof (AddCodeIssueComputersWorkItem));
       
            public AddCodeIssueComputersWorkItem(IList<ICodeIssueComputer> currentComputers, 
                IEnumerable<ICodeIssueComputer> newComputers)
            {
                this.currentComputers = currentComputers;
                this.newComputers = newComputers;
            }

            public override void Perform()
            {
                foreach (var computer in newComputers)
                {
                    if(!currentComputers.Contains(computer))
                    {
                        // Try to update an old version of computer by this new one.
                        var updatable = computer as IUpdatableCodeIssueComputer;
                        if(updatable != null)
                        {
                            var staleComputers = currentComputers.Where(updatable.IsUpdatedComputer).ToList();
                            if(staleComputers.Any()) 
                            {
                                logger.Debug("Code issue computer updated.");
                                foreach (var stale in staleComputers)
                                {
                                    currentComputers.Remove(stale);
                                }
                            }
                        }

                        // Add the computer.
                        currentComputers.Add(computer);
                    }
                }
            }
        }

        /// <summary>
        /// Work item to remove computers from the given computer list.
        /// </summary>
        private class RemoveCodeIssueComputersWorkItem: WorkItem
        {
            private readonly IList<ICodeIssueComputer> currentComputers;
            private readonly IEnumerable<ICodeIssueComputer> toRemoveComputers;

            internal RemoveCodeIssueComputersWorkItem(IList<ICodeIssueComputer> currentComputers,
                IEnumerable<ICodeIssueComputer> toRemoveComputers)
            {
                this.currentComputers = currentComputers;
                this.toRemoveComputers = toRemoveComputers;
             }

            public override void Perform()
            {
                foreach (ICodeIssueComputer computer in toRemoveComputers)
                {
                    currentComputers.Remove(computer);
                }
            }
        }

        /// <summary>
        /// Work item for getting code issues in a given syntax node. 
        /// </summary>
        private class GetDocumentNodeCodeIssueWorkItem : WorkItem
        {
            private readonly IDocument document;
            private readonly SyntaxNode node;
            private readonly IEnumerable<ICodeIssueComputer> computers;
            private readonly List<CodeIssue> results;

            internal GetDocumentNodeCodeIssueWorkItem(IDocument document, SyntaxNode node, 
                IEnumerable<ICodeIssueComputer> computers)
            {
                this.document = document;
                this.node = node;
                this.computers = computers;
                results = new List<CodeIssue>();
            }

            public override void Perform()
            {
                foreach (var computer in computers)
                {
                    if (computer.IsDocumentCorrect(document))
                    {
                        results.AddRange(computer.ComputeCodeIssues(document, node));
                    }
                }
            }

            public IEnumerable<CodeIssue> GetCodeIssues()
            {
                return results;
            }
        }

        /// <summary>
        /// Work item for getting all the refactoring addWarningsEvent in a given solution 
        /// and a set of computers to add element to the refactoring warning window.
        /// </summary>
        private class GetSolutionRefactoringWarningsWorkItem : WorkItem
        {
            private readonly IEnumerable<ICodeIssueComputer> computers;
            private readonly ISolution solution;
            private readonly Logger logger;
            private readonly AddGlobalRefactoringWarnings addWarningsEvent;


            internal GetSolutionRefactoringWarningsWorkItem(ISolution solution, 
                IEnumerable<ICodeIssueComputer> computers, 
                AddGlobalRefactoringWarnings addWarningsEvent)
            {
                this.solution = solution;
                this.computers = computers;
                this.logger = NLoggerUtil.GetNLogger(typeof (GetSolutionRefactoringWarningsWorkItem));
                this.addWarningsEvent = addWarningsEvent;
            }

            public override void Perform()
            {
                var messagesList = new List<IRefactoringWarningMessage>();

                foreach (ICodeIssueComputer computer in computers)
                {
                    var documents = computer.GetPossibleDocuments(solution);
                    foreach (IDocument document in documents)
                    {
                        var nodes = computer.GetPossibleSyntaxNodes(document);
                        if (nodes.Any())
                        {
                            // Find all the issues in the document. 
                            var issues = nodes.SelectMany(n => computer.
                                ComputeCodeIssues(document, n));

                            // For each code issue in the document, create a warning 
                            // message and add it to the list.
                            foreach (CodeIssue issue in issues)
                            {
                                var warningMessage = RefactoringWarningMessageFactory.
                                    CreateRefactoringWarningMessage(document, issue, computer);
                                messagesList.Add(warningMessage);
                                logger.Info("Create a refactoring warning.");
                            }
                        }
                    }
                }
               
                // Inform all the listeners that new messages are available.
                addWarningsEvent(messagesList.AsEnumerable());
            }
        }
    }
}
