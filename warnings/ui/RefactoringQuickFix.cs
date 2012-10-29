using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using NLog;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;
using warnings.analyzer;
using warnings.resources;
using warnings.util;

namespace warnings.quickfix
{
    internal class RefactoringQuickFix : ICodeAction
    {
        private IDocument document;
        private Logger logger;

        internal  RefactoringQuickFix (IDocument document)
        {
            this.document = document;
            this.logger = NLoggerUtil.GetNLogger(typeof (RefactoringQuickFix));
        }

        public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
        {
            IDocumentAnalyzer analyzer = new DocumentAnalyzer();
            analyzer.SetDocument(document);
            ISolution solution = document.Project.Solution;
            IWorkspace workspace = Workspace.GetWorkspace(document.GetText().Container);

            // Simply return the first local variable in the first method.
            ISymbol symbol = analyzer.GetFirstClass();
            try
            {
                ServiceArchive.getInstance().RenameService.RenameSymbol(workspace, solution, symbol, "blah");
                logger.Info(document.GetText());
            }
            catch (Exception e)
            {
                logger.Fatal(e);
            }
            return new CodeActionEdit(solution);            
        }

        public ImageSource Icon
        {
            get { return ResourcePool.GetIcon(); }
        }

        public string Description
        {
            get { return "Finishing Refactoring"; }
        }
    }
}
