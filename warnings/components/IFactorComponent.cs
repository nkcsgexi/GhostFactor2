using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BlackHen.Threading;
using Roslyn.Services;
using warnings.components.ui;

namespace warnings.components
{
    /* All the components in GhostFactor shall be implementing this interface.*/
    public interface IFactorComponent
    {
        void Enqueue(IWorkItem item);
        string GetName();
        int GetWorkQueueLength();
        void Start();
    }

    public class GhostFactorComponents
    {
        /* Component for saving the source code at certain time interval. */
        public static readonly IHistorySavingComponent historyComponent = HistorySavingComponent.GetInstance();

        /* Component for traversing the source code history and looking for manual rename refactoring. */
        public static readonly ISearchRefactoringComponent searchRenameComponent = SearchRenameComponent.GetInstance();

        /* Component for traversing the source code history and looking for manual extract method refactorings. */
        public static readonly ISearchRefactoringComponent searchExtractMethodComponent = SearchExtractMethodComponent.getInstance();

        /* Component searching in the history records for change method signature refactoring that cannot trigger compiler issues. */
        public static readonly ISearchRefactoringComponent searchChangeMethodSignatureComponent =
            SearchChangeMethodSignatureComponent.GetInstance();
        
        /* Component searches for performed inline method refactorings. */
        public static readonly ISearchRefactoringComponent searchInlineMethodComponent =
            SearchInlineMethodComponent.GetInstance();

        
        /* Component for checking the conditions of detected manual refactorings. */
        public static readonly IConditionCheckingComponent conditionCheckingComponent = ConditionCheckingComponent.GetInstance();

        /* Component for keeping track of all the refactoring issues and posting them to the editor.*/
        public static readonly ICodeIssueComputersRepository RefactoringCodeIssueComputerComponent =
            RefactoringCodeIssueComputersComponent.GetInstance();

        public static IDocumentSearcher searchRealDocumentComponent;

        public static IFactorComponent refactoringFormComponent = RefactoringFormViewComponent.GetInstance(); 

        public static void StartAllComponents(ISolution solution)
        {
            // Start the history keeping component.
            historyComponent.Start();
            
            // Initiate and start search real document component.
            searchRealDocumentComponent = SearchRealDocumentComponent.GetInstance(solution);

            // Start the refactoring form component, a new window will be displayed.
            refactoringFormComponent.Start();
        }
    }
}
