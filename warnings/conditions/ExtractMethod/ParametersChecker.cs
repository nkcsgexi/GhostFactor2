using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;
using warnings.analyzer;
using warnings.analyzer.comparators;
using warnings.refactoring;
using warnings.resources;
using warnings.retriever;
using warnings.util;

namespace warnings.conditions
{
    internal partial class ExtractMethodConditionsList
    {
        /* This checker is checking whether the extracted method has taken enough or more than enough parameters than actual need. */
        private class ParametersChecker : ExtractMethodConditionChecker
        {
            private Logger logger = NLoggerUtil.GetNLogger(typeof (ParametersChecker));

            protected override ICodeIssueComputer CheckCondition(IDocument before, IDocument after,
                                                                 IManualExtractMethodRefactoring input)
            {
                var invocation = (InvocationExpressionSyntax) input.ExtractMethodInvocation;

                // Calculate the needed typeNameTuples, depending on what to extract.
                IEnumerable<ISymbol> needed;
                if (input.ExtractedStatements != null)
                    needed = ConditionCheckersUtils.GetFlowInData(input.ExtractedStatements, before);
                else
                    needed = ConditionCheckersUtils.GetFlowInData(input.ExtractedExpression, before);

                // Logging the needed typeNameTuples.
                logger.Info("Needed typeNameTuples: " + StringUtil.ConcatenateAll(",", needed.Select(s => s.Name)));

                // Calculate the used symbols in the method declaration.
                var expressionDataFlowAnalyzer = AnalyzerFactory.GetExpressionDataFlowAnalyzer();
                expressionDataFlowAnalyzer.SetDocument(after);
                expressionDataFlowAnalyzer.SetExpression(invocation);
                var used = expressionDataFlowAnalyzer.GetFlowInData();

                // Logging the used typeNameTuples.
                logger.Info("Used typeNameTuples: " + StringUtil.ConcatenateAll(",", used.Select(s => s.Name)));

                // Calculate the missing symbols and the extra symbols, also, trivial to show 'this' so remove.
                var missing = ConditionCheckersUtils.RemoveThisSymbol(
                    ConditionCheckersUtils.GetSymbolListExceptByName(needed, used));

                // if missing is not empty, then some typeNameTuples are needed. 
                if (missing.Any())
                {
                    logger.Info("Missing Parameters Issue Found.");
                    return new ParameterCheckingCodeIssueComputer(input.ExtractedMethodDeclaration,
                        ConditionCheckersUtils.GetTypeNameTuples(missing));
                }
                else
                {
                    // Otherwise, return no problem.
                    return new NullCodeIssueComputer();
                }
            }

            /* Code issue computer for parameter checking results. */
            private class ParameterCheckingCodeIssueComputer : ValidCodeIssueComputer
            {
                /* Declaration of the extracted method. */
                private SyntaxNode declaration;

                /* The missing typeNameTuples' RefactoringType and name tuples. */
                private IEnumerable<Tuple<string, string>> typeNameTuples;

                public ParameterCheckingCodeIssueComputer(SyntaxNode declaration,
                                                          IEnumerable<Tuple<string, string>> typeNameTuples)
                {
                    this.declaration = declaration;
                    this.typeNameTuples = typeNameTuples;
                }

                public override IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node)
                {
                    // If the node is not method invocation, does not proceed.
                    if (node.Kind == SyntaxKind.InvocationExpression)
                    {
                        // Find all invocations of the extracted method.
                        var retriever = RetrieverFactory.GetMethodInvocationRetriever();
                        retriever.SetDocument(document);
                        retriever.SetMethodDeclaration(declaration);
                        var invocations = retriever.GetInvocations();

                        // If the given node is one of these invocations, return a new issue.
                        if (invocations.Any(i => i.Span.Equals(node.Span)))
                        {
                            yield return new CodeIssue(CodeIssue.Severity.Error, node.Span,
                                "Missing parameters: " + StringUtil.ConcatenateAll(",", typeNameTuples.Select(n => n.Item2)),
                                    new ICodeAction[] { new AddParamterCodeAction(document.Project.Solution, declaration, typeNameTuples) });
                        }
                    }
                }

                public override bool Equals(ICodeIssueComputer o)
                {
                    // If the other is not in the same RefactoringType, return false
                    if (o is ParameterCheckingCodeIssueComputer)
                    {
                        var other = (ParameterCheckingCodeIssueComputer) o;
                        var methodsComparator = new MethodsComparator();
                        var stringEnumerablesComparator = new StringEnumerablesComparator();

                        // If the method declarations are equal to each other.
                        return methodsComparator.Compare(declaration, other.declaration) == 0 &&
                               // Also the contained parameter names are equal to each other, return true;
                               stringEnumerablesComparator.Compare(typeNameTuples.Select(t => t.Item2),
                                                                   other.typeNameTuples.Select(t => t.Item2)) == 0;
                    }
                    return false;
                }

                private class AddParamterCodeAction : ICodeAction
                {
                    private readonly IEnumerable<Tuple<string, string>> typeNameTuples;
                    private readonly SyntaxNode declaration;
                    private readonly ISolution originalSolution;

                    internal AddParamterCodeAction(ISolution originalSolution, SyntaxNode declaration,
                                                   IEnumerable<Tuple<string, string>> typeNameTuples)
                    {
                        this.originalSolution = originalSolution;
                        this.typeNameTuples = typeNameTuples;
                        this.declaration = declaration;
                    }

                    public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
                    {
                        var updatedSolution = updateMethodDeclaration(originalSolution);
                        updatedSolution = updateMethodInvocations(updatedSolution);
                        return new CodeActionEdit(updatedSolution);
                    }

                    public ImageSource Icon
                    {
                        get { return ResourcePool.GetIcon(); }
                    }

                    public string Description
                    {
                        get { return "Add paramters " + StringUtil.ConcatenateAll(",", typeNameTuples.Select(t => t.Item2)); }
                    }

                    private ISolution updateMethodDeclaration(ISolution solution)
                    {
                        // Get the qualified name of the RefactoringType containing the declaration.
                        var qualifidedNameAnalyzer = AnalyzerFactory.GetQualifiedNameAnalyzer();
                        qualifidedNameAnalyzer.SetSyntaxNode(declaration);
                        var typeName = qualifidedNameAnalyzer.GetOutsideTypeQualifiedName();

                        // Get all documents in the given originalSolution.
                        var solutionAnalyzer = AnalyzerFactory.GetSolutionAnalyzer();
                        solutionAnalyzer.SetSolution(solution);
                        var documents = solutionAnalyzer.GetAllDocuments();

                        // Get the simplified name of the method
                        var methodName = ((MethodDeclarationSyntax) declaration).Identifier.ValueText;

                        // For each document in the originalSolution.
                        foreach (var document in documents)
                        {
                            var documentAnalyzer = AnalyzerFactory.GetDocumentAnalyzer();
                            documentAnalyzer.SetDocument(document);

                            // If the document contains the RefactoringType in which the method is declared.
                            if (documentAnalyzer.ContainsQualifiedName(typeName))
                            {
                                // Get the root of the current document.
                                var root = ((SyntaxNode) document.GetSyntaxRoot());

                                // Find the method
                                SyntaxNode method = root.DescendantNodes().Where(
                                    // Find all the method declarations.
                                    n => n.Kind == SyntaxKind.MethodDeclaration).
                                    // Convert all of them to the RefactoringType MethodDeclarationSyntax.
                                    Select(n => (MethodDeclarationSyntax) n).
                                    // Get the one whose name is same with the given method declaration.
                                    First(m => m.Identifier.ValueText.Equals(methodName));

                                // If we can find this method.
                                if (method != null)
                                {
                                    // Get the updated method declaration.
                                    var methodAnalyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
                                    methodAnalyzer.SetMethodDeclaration(method);
                                    var updatedMethod = methodAnalyzer.AddParameters(typeNameTuples);

                                    // Update the root, document and finally return the code action.
                                    var updatedRoot = new MethodDeclarationRewriter(method, updatedMethod).Visit(root);
                                    return solution.UpdateDocument(document.Id, updatedRoot);
                                }
                            }
                        }
                        return solution;
                    }

                    /* Sytnax writer to change a method to an updated one. */
                    private class MethodDeclarationRewriter : SyntaxRewriter
                    {
                        private readonly SyntaxNode originalMethod;
                        private readonly SyntaxNode updatedMethod;

                        internal MethodDeclarationRewriter(SyntaxNode originalMethod, SyntaxNode updatedMethod)
                        {
                            this.originalMethod = originalMethod;
                            this.updatedMethod = updatedMethod;
                        }

                        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
                        {
                            if (node.Span.Equals(originalMethod.Span))
                            {
                                return updatedMethod;
                            }
                            return node;
                        }
                    }

                    /* Update all the method invocations in the solution. */
                    private ISolution updateMethodInvocations(ISolution solution)
                    {
                        // Get all the documents in the solution.
                        var solutionAnalyzer = AnalyzerFactory.GetSolutionAnalyzer();
                        solutionAnalyzer.SetSolution(solution);
                        var documents = solutionAnalyzer.GetAllDocuments();

                        // Get the retriever for method invocations.
                        var retriever = RetrieverFactory.GetMethodInvocationRetriever();
                        retriever.SetMethodDeclaration(declaration);

                        // For each document
                        foreach (var document in documents)
                        {
                            // Get all the invocations in the document for the given method
                            // declaration.
                            retriever.SetDocument(document);
                            var invocations = retriever.GetInvocations();

                            // If there are invocations in the document.
                            if (invocations.Any())
                            {
                                // Update root
                                var root = (SyntaxNode) document.GetSyntaxRoot();
                                var updatedRoot = new InvocationsAddArgumentsRewriter(invocations, typeNameTuples.Select(t => t.Item2)).
                                    Visit(root);

                                // Update solution by update the document.
                                solution = solution.UpdateDocument(document.Id, updatedRoot);
                            }
                        }
                        return solution;
                    }

                    // Syntax rewriter for adding arguments to given method invocations.
                    private class InvocationsAddArgumentsRewriter : SyntaxRewriter
                    {
                        private readonly IEnumerable<string> addedArguments;
                        private readonly IEnumerable<SyntaxNode> invocations;
                        private readonly IMethodInvocationAnalyzer analyzer;

                        internal InvocationsAddArgumentsRewriter(IEnumerable<SyntaxNode> invocations,
                                                                 IEnumerable<string> addedArguments)
                        {
                            this.invocations = invocations;
                            this.addedArguments = addedArguments;
                            this.analyzer = AnalyzerFactory.GetMethodInvocationAnalyzer();
                        }

                        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
                        {
                            if (invocations.Any(i => i.Span.Equals(node.Span)))
                            {
                                analyzer.SetMethodInvocation(node);
                                return analyzer.AddArguments(addedArguments);
                            }
                            return node;
                        }
                    }
                }

                public override RefactoringType RefactoringType
                {
                    get { return RefactoringType.EXTRACT_METHOD; }
                }
            }
        }
    }
}
