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
using warnings.refactoring.detection;
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
                        ConditionCheckersUtils.GetTypeNameTuples(missing), input.MetaData.DocumentUniqueName);
                }
                else
                {
                    // Otherwise, return no problem.
                    return new NullCodeIssueComputer();
                }
            }

            /* Code issue computer for parameter checking results. */
            private class ParameterCheckingCodeIssueComputer : SingleDocumentValidCodeIssueComputer
            {
                /* Declaration of the extracted method. */
                private SyntaxNode declaration;

                /* The missing typeNameTuples' RefactoringType and name tuples. */
                private IEnumerable<Tuple<string, string>> typeNameTuples;

                private readonly IComparer<SyntaxNode> methodNameComparer;

                public ParameterCheckingCodeIssueComputer(SyntaxNode declaration,
                    IEnumerable<Tuple<string, string>> typeNameTuples, string documentUniqueName) 
                        : base(documentUniqueName)
                {
                    this.declaration = declaration;
                    this.typeNameTuples = typeNameTuples;
                    this.methodNameComparer = RefactoringDetectionUtils.GetMethodDeclarationNameComparer();
                }

                public override IEnumerable<SyntaxNode> GetPossibleSyntaxNodes(IDocument document)
                {
                    return ((SyntaxNode)document.GetSyntaxRoot()).DescendantNodes(n => n.Kind != 
                        SyntaxKind.MethodDeclaration).Where(n => n.Kind == SyntaxKind.MethodDeclaration);
                }

                public override IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node)
                {
                    // If the node is not method declaration, does not proceed.
                    if (node.Kind == SyntaxKind.MethodDeclaration)
                    {
                        // If the given node is the declaration, return a new issue.
                        if (methodNameComparer.Compare(node, declaration) == 0)
                        {
                            yield return new CodeIssue(CodeIssue.Severity.Error, node.Span,
                                "Missing parameters: " + StringUtil.ConcatenateAll(",", typeNameTuples.Select(n => n.Item2)),
                                    new ICodeAction[] { new AddParamterCodeAction(document, declaration, typeNameTuples, this) });
                        }
                    }
                }

                public override bool Equals(ICodeIssueComputer o)
                {
                    // If the other is not in the same RefactoringType, return false
                    if (o is ParameterCheckingCodeIssueComputer)
                    {
                        var other = (ParameterCheckingCodeIssueComputer) o;
                        var methodsComparator = RefactoringDetectionUtils.GetMethodDeclarationNameComparer();
                   
                        // If the method declarations are equal to each other.
                        return methodsComparator.Compare(declaration, other.declaration) == 0;
                    }
                    return false;
                }

                private class AddParamterCodeAction : ICodeAction
                {
                    private readonly IEnumerable<Tuple<string, string>> typeNameTuples;
                    private readonly SyntaxNode declaration;
                    private readonly IDocument document;
                    private readonly ICodeIssueComputer computer;

                    internal AddParamterCodeAction(IDocument document, SyntaxNode declaration, 
                        IEnumerable<Tuple<string, string>> typeNameTuples, ICodeIssueComputer computer)
                    {
                        this.document = document;
                        this.typeNameTuples = typeNameTuples;
                        this.declaration = declaration;
                        this.computer = computer;
                    }

                    public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
                    {
                        var updatedDocument = updateMethodDeclaration(document);
                        updatedDocument = updateMethodInvocations(updatedDocument);
                        var updatedSolution = document.Project.Solution.UpdateDocument(updatedDocument);
                        var edit = new CodeActionEdit(null, updatedSolution, 
                            ConditionCheckersUtils.GetRemoveCodeIssueComputerOperation(computer));
                        return edit;
                    }

                    public ImageSource Icon
                    {
                        get { return ResourcePool.GetIcon(); }
                    }

                    public string Description
                    {
                        get { return "Add paramters " + StringUtil.ConcatenateAll(",", typeNameTuples.Select(t => t.Item2)); }
                    }

                    private IDocument updateMethodDeclaration(IDocument document)
                    {
                        // Get the simplified name of the method
                        var methodName = ((MethodDeclarationSyntax) declaration).Identifier.ValueText;
                        var documentAnalyzer = AnalyzerFactory.GetDocumentAnalyzer();
                        documentAnalyzer.SetDocument(document);
                      
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
                            return document.UpdateSyntaxRoot(updatedRoot);
                        }
                        return document;
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
                    private IDocument updateMethodInvocations(IDocument document)
                    {
                        // Get the retriever for method invocations.
                        var retriever = RetrieverFactory.GetMethodInvocationRetriever();
                        retriever.SetMethodDeclaration(declaration);
             
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
                            document = document.UpdateSyntaxRoot(updatedRoot);
                        }
                        return document;
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
