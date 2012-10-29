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
    public delegate void RemoveGlobalRefactoringWarnings(Predicate<IRefactoringWarningMessage> removeCondition);

    /* Used for any listeners who are instersted at how many refactorings are problematic. */
    public delegate void ProblematicRefactoringsCountChanged(int newCount);


    /* A repository for issue computers to be queried, added, and deleted. */
    public interface ICodeIssueComputersRepository
    {
        event AddGlobalRefactoringWarnings AddGlobalWarnings;
        event RemoveGlobalRefactoringWarnings RemoveGlobalWarnings;
        event ProblematicRefactoringsCountChanged ProblematicRefactoringCountChanged;
 
    
        void AddCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers);
        void RemoveCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers);
        IEnumerable<CodeIssue> GetCodeIssues(IDocument document, SyntaxNode node);
    }
  
    internal class RefactoringCodeIssueComputersComponent : IFactorComponent, ICodeIssueComputersRepository
    {
        /* Singleton this component. */
        private static readonly ICodeIssueComputersRepository intance =
            new RefactoringCodeIssueComputersComponent();

        public static ICodeIssueComputersRepository GetInstance()
        {
            return intance;
        }

        /* Saving all of the code issue computers. */
        private readonly IList<ICodeIssueComputer> codeIssueComputers;

        /* The black list of computers that are not allowed to add. */
        private readonly CodeIssueComputersBlackList blackList;


        /* Used for any listener to the event of code issue computers added or removed. */
        private delegate void CodeIssueComputersAdded(IEnumerable<ICodeIssueComputer> newCodeIssueComputers);

        /* Event when new code issues are added.*/
        private event CodeIssueComputersAdded codeIssueComputersAddedEvent;

        /* Event when new global refactoring addWarningsEvent are ready to be added. */
        public event AddGlobalRefactoringWarnings AddGlobalWarnings;

        /* Event when old global refactoring addWarningsEvent are ready to be removed. */
        public event RemoveGlobalRefactoringWarnings RemoveGlobalWarnings;

        public event ProblematicRefactoringsCountChanged ProblematicRefactoringCountChanged;


        /* A single thread workqueue. */
        private WorkQueue queue;

        private Logger logger;

        private RefactoringCodeIssueComputersComponent()
        {
            codeIssueComputers = new List<ICodeIssueComputer>();

            // Single thread workqueue.
            queue = new WorkQueue {ConcurrentLimit = 1};

            // Add a listener for failed work item.
            queue.FailedWorkItem += OnItemFailed;
            logger = NLoggerUtil.GetNLogger(typeof (RefactoringCodeIssueComputersComponent));

            blackList = new CodeIssueComputersBlackList(5);

            codeIssueComputersAddedEvent += OnCodeIssueComputersAdded;
        }

        /* When code issue computers are added, this method will be called. */
        private void OnCodeIssueComputersAdded(IEnumerable<ICodeIssueComputer> computers)
        {
            var solution = GhostFactorComponents.searchRealDocumentComponent.GetSolution();
            
            // Create a work item for this task.
            var item = new GetSolutionRefactoringWarningsWorkItem(solution, computers, AddGlobalWarnings);
            queue.Add(item);
        }

        private void OnItemFailed(object sender, WorkItemEventArgs workItemEventArgs)
        {
            logger.Fatal("Work item failed.");
        }

        public void Enqueue(IWorkItem item)
        {
            queue.Add(item);
        }

        public string GetName()
        {
            return "Refactoring Issues Componenet.";
        }

        public int GetWorkQueueLength()
        {
            return queue.Count;
        }

        public void Start()
        {
        }


        /* Add a list of code issue computers to the current list. */
        public void AddCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers)
        {
            // Create a code issue adding work item and push it to the work queue.
            queue.Add(new AddCodeIssueComputersWorkItem(codeIssueComputers, computers, blackList, codeIssueComputersAddedEvent, 
                ProblematicRefactoringCountChanged));
        }

        /* Remove a list of code issue computers from the current list. */
        public void RemoveCodeIssueComputers(IEnumerable<ICodeIssueComputer> computers)
        {
            queue.Add(new RemoveCodeIssueComputersWorkItem(codeIssueComputers, computers, blackList, RemoveGlobalWarnings, 
                ProblematicRefactoringCountChanged));
        }

        /* Get the code issues in the given node of the given document. */
        public IEnumerable<CodeIssue> GetCodeIssues(IDocument document, SyntaxNode node)
        {
            // Create a work item for this task.
            var item = new GetDocumentNodeCodeIssueWorkItem(document, node, codeIssueComputers);
            new WorkItemSynchronizedExecutor(item, queue).Execute();
            return item.GetCodeIssues();
        }


        private class CodeIssueComputersBlackList
        {
            private readonly int maxCount;
            private readonly IList<ICodeIssueComputer> blackList; 
            
            internal CodeIssueComputersBlackList(int maxCount)
            {
                this.maxCount = maxCount;
                this.blackList = new List<ICodeIssueComputer>();
            }

            public void Add(ICodeIssueComputer computer)
            {
                if (blackList.Count() == maxCount)
                {
                    blackList.RemoveAt(maxCount - 1);
                }
                blackList.Add(computer);
            }

            public bool IsBlack(ICodeIssueComputer computer)
            {
                return blackList.Contains(computer);
            }
        }



        /* Work item to add new issue computers to the repository. */
        private class AddCodeIssueComputersWorkItem : WorkItem
        {
            private readonly IList<ICodeIssueComputer> currentComputers;
            private readonly IEnumerable<ICodeIssueComputer> newComputers;
            private readonly CodeIssueComputersAdded changeEvent;
            private readonly ProblematicRefactoringsCountChanged countChangd;
            private readonly CodeIssueComputersBlackList blackList;

            public AddCodeIssueComputersWorkItem(IList<ICodeIssueComputer> currentComputers, 
                IEnumerable<ICodeIssueComputer> newComputers, CodeIssueComputersBlackList blackList, 
                CodeIssueComputersAdded changeEvent, ProblematicRefactoringsCountChanged countChanged)
            {
                this.currentComputers = currentComputers;
                this.newComputers = newComputers;
                this.blackList = blackList;
                this.changeEvent = changeEvent;
                this.countChangd = countChanged;
            }

            public override void Perform()
            {
                var addedComputers = new List<ICodeIssueComputer>();

                // For every computer in the new computers list. 
                foreach (var computer in newComputers)
                {
                    // If a computer is not already in the list.
                    if (!currentComputers.Contains(computer) && 
                        // And the computer is not null computer.
                        !(computer is NullCodeIssueComputer) &&
                        !blackList.IsBlack(computer))
                    {
                        currentComputers.Add(computer);
                        addedComputers.Add(computer);
                    }
                }

                // If has added new computers, then send messages to listeners.
                if (addedComputers.Any())
                {
                    changeEvent(addedComputers.AsEnumerable());
                    countChangd(currentComputers.Count());
                }
            }
        }

        /* Work item to remove computers from the given computer list. */
        private class RemoveCodeIssueComputersWorkItem: WorkItem
        {
            private readonly IList<ICodeIssueComputer> currentComputers;
            private readonly IEnumerable<ICodeIssueComputer> toRemoveComputers;
            private readonly RemoveGlobalRefactoringWarnings removeWarningEvent;
            private readonly ProblematicRefactoringsCountChanged countChangedEvent;
            private readonly CodeIssueComputersBlackList blackList;
            private readonly Logger logger;

            internal RemoveCodeIssueComputersWorkItem(IList<ICodeIssueComputer> currentComputers,
                IEnumerable<ICodeIssueComputer> toRemoveComputers, CodeIssueComputersBlackList blackList,
                RemoveGlobalRefactoringWarnings removeWarningEvent, 
                ProblematicRefactoringsCountChanged countChangedEvent)
            {
                this.currentComputers = currentComputers;
                this.toRemoveComputers = toRemoveComputers;
                this.blackList = blackList;
                this.removeWarningEvent = removeWarningEvent;
                this.countChangedEvent = countChangedEvent;
                this.logger = NLoggerUtil.GetNLogger(typeof (RemoveCodeIssueComputersWorkItem));
            }

            public override void Perform()
            {
                foreach (ICodeIssueComputer computer in toRemoveComputers)
                {
                    if(!currentComputers.Remove(computer))
                    {
                        logger.Fatal("Cannot remove a code issue computer.");
                    }

                    // Add the removed computer to the black list.
                    blackList.Add(computer);
                }

                // Invoke the event, the remove condition is any messages whose code issue computer is in 
                // the toRemove list.
                removeWarningEvent(n => toRemoveComputers.Contains(n.CodeIssueComputer));
                countChangedEvent(currentComputers.Count);
            }
        }

        /* Work item for getting code issues in a given syntax node. */
        private class GetDocumentNodeCodeIssueWorkItem : WorkItem
        {
            private readonly IDocument document;
            private readonly SyntaxNode node;
            private readonly IEnumerable<ICodeIssueComputer> computers;
            private IEnumerable<CodeIssue> results;

            internal GetDocumentNodeCodeIssueWorkItem(IDocument document, SyntaxNode node, 
                IEnumerable<ICodeIssueComputer> computers)
            {
                this.document = document;
                this.node = node;
                this.computers = computers;
            }

            public override void Perform()
            {
                results = computers.SelectMany(c => c.ComputeCodeIssues(document, node));
             }

            public IEnumerable<CodeIssue> GetCodeIssues()
            {
                return results;
            }
        }

     
        /* Work item for getting all the refactoring addWarningsEvent in a given solution and a set of computers. */
        private class GetSolutionRefactoringWarningsWorkItem : WorkItem
        {
            private readonly IEnumerable<ICodeIssueComputer> computers;
            private readonly ISolution solution;
            private readonly Logger logger;
            private readonly AddGlobalRefactoringWarnings addWarningsEvent;


            internal GetSolutionRefactoringWarningsWorkItem(ISolution solution, IEnumerable<ICodeIssueComputer> computers, 
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

                // Get all the documents.
                var analyzer = AnalyzerFactory.GetSolutionAnalyzer();
                analyzer.SetSolution(solution);
                var documents = analyzer.GetAllDocuments();

                // For each of the document.
                foreach (IDocument document in documents)
                {
                    // Get all the decendant nodes. 
                    var nodes = ((SyntaxNode) document.GetSyntaxRoot()).DescendantNodes();
                    
                    // For each computer in the given list.
                    foreach (ICodeIssueComputer computer in computers)
                    {
                        // Find all the issues in the document. 
                        var issues = nodes.SelectMany(n => computer.ComputeCodeIssues(document, n));
                        
                        // For each code issue in the document, create a warning message and add it to the list.
                        foreach (CodeIssue issue in issues)
                        {
                            var warningMessage = RefactoringWarningMessageFactory.
                                CreateRefactoringWarningMessage(document, issue, computer);
                            messagesList.Add(warningMessage);
                            logger.Info("Create a refactoring warning.");
                        }
                    }
                }
               
                // Inform all the listeners that new messages are available.
                addWarningsEvent(messagesList.AsEnumerable());
            }
        }
    }
}
