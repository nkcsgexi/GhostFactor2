using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;

namespace warnings.util
{
    /*
     * Contains multiple utility methods for workspace, solutions and 
     */
    public class RoslynUtil
    {
        /* Get an instance of ISolution from the given path. */
        public static ISolution GetSolution(String path)
        {
            return Workspace.LoadSolution(path).CurrentSolution;
        }

        /* Get the instance of a project in the solution who has the given name.*/
        public static IProject GetProject(ISolution solution, String name)
        {
            return solution.Projects.First(p => p.Name.Equals(name));
        }

        /* Get the document with the specified name in the project.*/
        public static IDocument GetDocument(IProject project, String name)
        {
            return project.Documents.First(d => d.Name.Equals(name));
         }

        /* 
         * Update one ducument in a solution to the specified string, and return the instance of
         * the modified ISolution.
         */
        public static IDocument UpdateDocumentToString(IDocument document, String s)
        {
            IText text = new StringText(s);
            CommonSyntaxNode node = ASTUtil.GetSyntaxTreeFromSource(s).GetRoot();
            return document.Project.Solution.UpdateDocument(document.Id, text).GetDocument(document.Id);
        }
    }

    /* Containing utility methods for Roslyn refactoring APIs. */
    public class RoslynRefactoringUtil
    {
        /* check whether extract method can be performed. */
        public static bool CheckExtractMethodPreconditions(IDocument document, int start, int length)
        {
            var service = ServiceArchive.getInstance().ExtractMethodService;
            var result = service.ExtractMethod(document, new TextSpan(start, length));
            return result.Succeeded;
        }

        /* Perform the extract method and returns the changed tree. */
        public static CommonSyntaxNode PerformExtractMethod(IDocument document, int start, int length)
        {
            var service = ServiceArchive.getInstance().ExtractMethodService;
            var result = service.ExtractMethod(document, new TextSpan(start, length));
            return result.ResultingTree;
        }

        /* Check the preconditions of rename refactoring, return true if can be finished succesfully. */
        public static bool CheckRenamePreconditions(IDocument document, int start, int length)
        {
            var service = ServiceArchive.getInstance().RenameService;
            ISemanticModel model;
            document.TryGetSemanticModel(out model);
            return false;
        }
   }
}
