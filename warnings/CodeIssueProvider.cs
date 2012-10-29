using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows;
using NLog;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;
using Roslyn.Services.Host;
using warnings.analyzer;
using warnings.components;
using warnings.quickfix;
using warnings.util;

namespace warnings
{
    [ExportSyntaxNodeCodeIssueProvider("CodeIssue", LanguageNames.CSharp)]
    class CodeIssueProvider : ICodeIssueProvider
    {
        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof(CodeIssueProvider));

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxNode node, CancellationToken cancellationToken)
        {
            initialize(document);

            // Add the new record to the history component.
            GhostFactorComponents.historyComponent.UpdateActiveDocument(document);
            return GhostFactorComponents.RefactoringCodeIssueComputerComponent.GetCodeIssues(document, (SyntaxNode) node);  
        }

        private bool initialized = false;

        /* Code runs only once when getIssues is called. */
        private void initialize(IDocument document)
        {
            try
            {
                if (initialized == false)
                {
                    // Start all the components.
                    GhostFactorComponents.StartAllComponents(document.Project.Solution);
                    initialized = true;
                }
            }catch(Exception e)
            {
                logger.Fatal(e);
            }
        }


        #region Unimplemented ICodeIssueProvider members

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxToken token, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxTrivia trivia, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion
    }


}
