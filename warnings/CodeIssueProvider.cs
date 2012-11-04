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
using warnings.configuration;
using warnings.quickfix;
using warnings.util;

namespace warnings
{
    [ExportSyntaxNodeCodeIssueProvider("CodeIssue", LanguageNames.CSharp)]
    class CodeIssueProvider : ICodeIssueProvider
    {
        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxNode node, 
            CancellationToken cancellationToken)
        {
            if (!GlobalConfigurations.ShutDown())
            {
                SetGlobalData(document);

                // Add the new record to the history component.
                GhostFactorComponents.historyComponent.UpdateDocument(document);
                return GhostFactorComponents.RefactoringCodeIssueComputerComponent.
                    GetCodeIssues(document, (SyntaxNode) node);
            }
            return null;
        }

        private void SetGlobalData(IDocument document)
        {
            GlobalData.Solution = document.Project.Solution;
        }


        #region Unimplemented ICodeIssueProvider members

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxToken token, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxTrivia trivia, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion
    }


}
