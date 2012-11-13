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

        private delegate void CodeIssueComputersAdded(IEnumerable<ICodeIssueComputer> newCodeIssueComputers);
        private event CodeIssueComputersAdded codeIssueComputersAdded;

        private delegate void CodeIssueComputersRemoved(IEnumerable<ICodeIssueComputer> removedComputers);
        private event CodeIssueComputersRemoved codeIssueComputersRemoved;


        public event AddGlobalRefactoringWarnings AddGlobalWarnings;
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

            codeIssueComputersAdded += OnCodeIssueComputersAdded;
            codeIssueComputersRemoved += OnCodeIssueComputersRemoved;
        }

        private void OnCodeIssueComputersRemoved(IEnumerable<ICodeIssueComputer> removedComputers)
        {
            logger.Info("Removed " + removedComputers.Count() +" code issue computers.");
        }

        private void OnCodeIssueComputersAdded(IEnumerable<ICodeIssueComputer> newCodeIssueComputers)
        {
            logger.Info("Added " + newCodeIssueComputers.Count() + " code issue computers.");
        }


        private void OnItemFailed(object sender, WorkItemEventArgs workItemEventArgs)
        {
            logger.Fatal("Work item failed: " + workItemEventArgs.WorkItem);
            logger.Fatal(workItemEventArgs.WorkItem.FailedException);
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
            queue.Add(new ResolveExistingIssueComputerWorkItem(codeIssueComputers, correctRefactorings, 
                codeIssueComputersRemoved));
        }

        private class ResolveExistingIssueComputerWorkItem : WorkItem
        {
            private readonly IEnumerable<ICorrectRefactoringResult> correctRefactorings;
            private readonly IList<ICodeIssueComputer> codeIssueComputers;
            private readonly CodeIssueComputersRemoved codeIssueComputersRemoved;

            public ResolveExistingIssueComputerWorkItem(IList<ICodeIssueComputer> codeIssueComputers,
                IEnumerable<ICorrectRefactoringResult> correctRefactorings, CodeIssueComputersRemoved 
                codeIssueComputersRemoved)
            {
                this.codeIssueComputers = codeIssueComputers;
                this.correctRefactorings = correctRefactorings;
                this.codeIssueComputersRemoved = codeIssueComputersRemoved;
            }

            public override void Perform()
            {
                // Using the correct refactorings to resolve existing code issue computers.
                var resolvedComputers = codeIssueComputers.Where(computer => correctRefactorings.Any
                    (computer.IsIssueResolved)).ToList();

                // Remove these resolved code issue computers.
                foreach (var resolved in resolvedComputers)
                {
                    codeIssueComputers.Remove(resolved);
                }

                // Triger the event of removed computers.
                codeIssueComputersRemoved(resolvedComputers);
            }
        }

        /// <summary>
        /// Add a set of condition checking results.
        /// </summary>
        /// <param name="computers"></param>
        public void AddCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers)
        {
            queue.Add(new AddCodeIssueComputersWorkItem(codeIssueComputers, computers, 
                codeIssueComputersAdded, codeIssueComputersRemoved));
        }

        /// <summary>
        /// Remove a list of code issue computers from the current list.
        /// </summary>
        /// <param name="computers">The computers to be removed.</param>
        public void RemoveCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers)
        {
            queue.Add(new RemoveCodeIssueComputersWorkItem(codeIssueComputers, computers, 
                codeIssueComputersRemoved));
        }

        /// <summary>
        /// Get the code issues in the given node of the given document. 
        /// </summary>
        /// <param name="document"></param>
        /// <param name="node"></param>
        /// <returns></returns>
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
            private static readonly Logger logger = NLoggerUtil.GetNLogger(typeof
                (AddCodeIssueComputersWorkItem)); 

            private readonly IList<ICodeIssueComputer> currentComputers;
            private readonly IEnumerable<ICodeIssueComputer> newComputers;
            
            private readonly CodeIssueComputersAdded codeIssueComputersAdded;
            private readonly CodeIssueComputersRemoved codeIssueComputersRemoved;

           
            internal AddCodeIssueComputersWorkItem(IList<ICodeIssueComputer> currentComputers, 
                IEnumerable<ICodeIssueComputer> newComputers, CodeIssueComputersAdded codeIssueComputersAdded,
                CodeIssueComputersRemoved codeIssueComputersRemoved)
            {
                this.currentComputers = currentComputers;
                this.newComputers = newComputers;
                this.codeIssueComputersAdded = codeIssueComputersAdded;
                this.codeIssueComputersRemoved = codeIssueComputersRemoved;
            }

            public override void Perform()
            {
                var addedComputers = new List<ICodeIssueComputer>();
                var removedComputers = new List<ICodeIssueComputer>();

                var updatables = currentComputers.OfType<IUpdatableCodeIssueComputer>().ToList();
                foreach (var computer in newComputers)
                {
                    if (!currentComputers.Contains(computer))
                    {
                        var updatable = computer as IUpdatableCodeIssueComputer;

                        // Try to update an old version of computer by this new one.
                        if (updatables.Any() && updatable != null)
                        {
                            var staleComputers = updatables.Where(u => u.IsUpdatedComputer(updatable)).
                                ToList();
                            if (staleComputers.Any())
                            {
                                foreach (ICodeIssueComputer stale in staleComputers)
                                {
                                    currentComputers.Remove(stale);
                                    removedComputers.Add(stale);
                                }
                            }
                        }

                        // Add the computer.
                        currentComputers.Add(computer);
                        addedComputers.Add(computer);
                    }
                }
             
                // Triger the event of added code issue computers.
                if (addedComputers.Any())
                {
                    codeIssueComputersAdded(addedComputers);
                }

                // Trigger removed computers event if any.
                if(removedComputers.Any())
                {
                    codeIssueComputersRemoved(removedComputers);
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
            private readonly CodeIssueComputersRemoved codeIssueComputersRemoved;

            internal RemoveCodeIssueComputersWorkItem(IList<ICodeIssueComputer> currentComputers,
                IEnumerable<ICodeIssueComputer> toRemoveComputers, CodeIssueComputersRemoved 
                codeIssueComputersRemoved)
            {
                this.currentComputers = currentComputers;
                this.toRemoveComputers = toRemoveComputers;
                this.codeIssueComputersRemoved = codeIssueComputersRemoved;
            }

            public override void Perform()
            {
                var removedComputers = new List<ICodeIssueComputer>();
                foreach (ICodeIssueComputer computer in toRemoveComputers)
                {
                    if(currentComputers.Remove(computer))
                    {
                        removedComputers.Add(computer);
                    }
                }
                codeIssueComputersRemoved(removedComputers);
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
