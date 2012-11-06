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
    public static class GhostFactorComponents
    {

        /// <summary>
        /// Component for handle configurations.
        /// </summary>
        public static readonly GlobalConfigurationComponent configurationComponent =
            GlobalConfigurationComponent.GetInstance();

        /// <summary>
        /// Component for saving different version of source files.
        /// </summary>
        public static readonly IHistorySavingComponent historyComponent = 
            HistorySavingComponent.GetInstance();

        /// <summary>
        /// Component for detecting the performed refactorings.
        /// </summary>
        public static readonly ISearchRefactoringComponent searchRefactoringComponent =
            SearchRefactoringComponent.GetInstance();

        /// <summary>
        /// Component for checking the correctness of detected refactorings.
        /// </summary>
        public static readonly IConditionCheckingComponent conditionCheckingComponent = 
            ConditionCheckingComponent.GetInstance();

        /// <summary>
        /// Component for saving and calculating the code issues that will be presented to the
        /// editors.
        /// </summary>
        public static readonly ICodeIssueComputersRepository RefactoringCodeIssueComputerComponent =
            RefactoringCodeIssueComputersComponent.GetInstance();

        /// <summary>
        /// Component for handling user interface related issues.
        /// </summary>
        public static readonly IUIComponent refactoringFormComponent = 
            RefactoringFormViewComponent.GetInstance();
    
    }
}
