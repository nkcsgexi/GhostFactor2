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
        public static readonly IHistorySavingComponent historyComponent = 
            HistorySavingComponent.GetInstance();

        /* Component for traversing the source code history and looking for manual rename refactoring. */
        public static readonly ISearchRefactoringComponent searchRefactoringComponent =
            SearchRefactoringComponent.GetInstance();
        
        /* Component for checking the conditions of detected manual refactorings. */
        public static readonly IConditionCheckingComponent conditionCheckingComponent = 
            ConditionCheckingComponent.GetInstance();

        /* Component for keeping track of all the refactoring issues and posting them to the editor.*/
        public static readonly ICodeIssueComputersRepository RefactoringCodeIssueComputerComponent =
            RefactoringCodeIssueComputersComponent.GetInstance();

        public static IFactorComponent refactoringFormComponent = 
            RefactoringFormViewComponent.GetInstance(); 

        public static void StartAllComponents()
        {
            // Start the history keeping component.
            historyComponent.Start();
            

            // Start the refactoring form component, a new window will be displayed.
            refactoringFormComponent.Start();
        }
    }
}
