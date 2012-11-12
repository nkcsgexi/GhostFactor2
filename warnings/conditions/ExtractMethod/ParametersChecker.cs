using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using NLog;
using Roslyn.Compilers;
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
        /// <summary>
        /// This checker is checking whether the extracted method has taken enough or more than enough 
        /// parameters than actual need.
        /// </summary>
        private class ParametersChecker : ExtractMethodConditionChecker
        {
            private Logger logger = NLoggerUtil.GetNLogger(typeof (ParametersChecker));

            public override Predicate<SyntaxNode> GetIssuedNodeFilter()
            {
                return n => n.Kind == SyntaxKind.MethodDeclaration;
            }

            protected override ICodeIssueComputer CheckCondition(
                IManualExtractMethodRefactoring input)
            {
                var before = input.BeforeDocument;
                var after = input.AfterDocument;

                var invocation = (InvocationExpressionSyntax) input.ExtractMethodInvocation;

                // Calculate the needed typeNameTuples, depending on what to extract.
                IEnumerable<ISymbol> needed;
                if (input.ExtractedStatements != null)
                    needed = ConditionCheckersUtils.GetUsedButNotDeclaredData(input.ExtractedStatements, 
                        before);
                else
                    needed = ConditionCheckersUtils.GetFlowInData(input.ExtractedExpression, before);

                // Logging the needed typeNameTuples.
                logger.Info("Needed typeNameTuples: " + StringUtil.ConcatenateAll(",", needed.Select(s => 
                    s.Name)));

                // Calculate the used symbols in the method declaration.
                var expressionDataFlowAnalyzer = AnalyzerFactory.GetExpressionDataFlowAnalyzer();
                expressionDataFlowAnalyzer.SetDocument(after);
                expressionDataFlowAnalyzer.SetExpression(invocation);
                var used = expressionDataFlowAnalyzer.GetFlowInData();

                // Logging the used typeNameTuples.
                logger.Info("Used typeNameTuples: " + StringUtil.ConcatenateAll(",", used.Select(s => 
                    s.Name)));

                // Calculate the missing symbols and the extra symbols, also, trivial to show 'this' so 
                // remove.
                var missing = ConditionCheckersUtils.RemoveThisSymbol(
                    ConditionCheckersUtils.GetSymbolListExceptByName(needed, used));

                // if missing is not empty, then some typeNameTuples are needed. 
                if (missing.Any())
                {
                    logger.Info("Missing Parameters Issue Found.");
                    return new ParameterCheckingCodeIssueComputer(input.ExtractedMethodDeclaration,
                        ConditionCheckersUtils.GetTypeNameTuples(missing), input.MetaData);
                }
                else
                {
                    // Otherwise, return no problem.
                    return new NullCodeIssueComputer();
                }
            }

            /// <summary>
            /// Code issue computer for parameter checking results.
            /// </summary>
            private class ParameterCheckingCodeIssueComputer : SingleDocumentValidCodeIssueComputer
            {
                /* Declaration of the extracted method. */
                private SyntaxNode declaration;

                /* The missing typeNameTuples' RefactoringType and name tuples. */
                private IEnumerable<Tuple<string, string>> typeNameTuples;

                private readonly IComparer<SyntaxNode> methodNameComparer;

                public ParameterCheckingCodeIssueComputer(SyntaxNode declaration,
                    IEnumerable<Tuple<string, string>> typeNameTuples, 
                        RefactoringMetaData metaData) : base(metaData)
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
                            // For each type name tuple, generate one issue with it.
                            return typeNameTuples.Select(t => GetMissingParameterIssue(document, node, t));
                        }
                    }
                    return Enumerable.Empty<CodeIssue>();
                }

                private CodeIssue GetMissingParameterIssue(IDocument document, SyntaxNode node, 
                    Tuple<string, string> typeNameTuple)
                {
                    return new CodeIssue(CodeIssue.Severity.Error, node.Span, "Missing parameter: " +
                      typeNameTuple.Item2, new ICodeAction[] {new AddParamterCodeAction(document, node,
                          typeNameTuple, this)});
                }


                public override bool Equals(ICodeIssueComputer o)
                {
                    if (IsIssuedToSameDocument(o))
                    {
                        var another = o as ParameterCheckingCodeIssueComputer;

                        // If the other is not in the same RefactoringType, return false
                        if (another != null)
                        {
                            var other = (ParameterCheckingCodeIssueComputer) o;
                            var methodsComparator = RefactoringDetectionUtils.
                                GetMethodDeclarationNameComparer();

                            // If the method declarations are equal to each other, compare the missing 
                            // parameters.
                            if(methodsComparator.Compare(declaration, other.declaration) == 0)
                            {
                                // First get the equality comparer of two string tuples.
                                var tupleComparer = RefactoringDetectionUtils.
                                    GetStringTuplesEqualityComparer();

                                // Next get the equality comparer of two sets.
                                var setsComparer = new SetsEqualityCompare<Tuple<string, string>>
                                    (tupleComparer);
                                return setsComparer.Equals(typeNameTuples, another.typeNameTuples);
                            }
                        }
                    }
                    return false;
                }

                /// <summary>
                /// Code action for adding a single one parameter.
                /// </summary>
                private class AddParamterCodeAction : ICodeAction
                {
                    private readonly Tuple<string, string> typeNameTuple;
                    private readonly SyntaxNode declaration;
                    private readonly IDocument document;
                    private readonly ICodeIssueComputer computer;

                    internal AddParamterCodeAction(IDocument document, SyntaxNode declaration, 
                        Tuple<string, string> typeNameTuple, ICodeIssueComputer computer)
                    {
                        this.document = document;
                        this.typeNameTuple = typeNameTuple;
                        this.declaration = declaration;
                        this.computer = computer;
                    }

                    public CodeActionEdit GetEdit(CancellationToken cancellationToken = new 
                        CancellationToken())
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
                        get { return "Add paramters " + typeNameTuple.Item2; }
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
                            var updatedMethod = methodAnalyzer.AddParameters(new[] {typeNameTuple});

                            // Update the root, document and finally return the code action.
                            var updatedRoot = new MethodDeclarationRewriter(method, updatedMethod).
                                Visit(root);
                            return document.UpdateSyntaxRoot(updatedRoot);
                        }
                        return document;
                    }

                    /// <summary>
                    /// Sytnax writer to change a method to an updated one.
                    /// </summary>
                    private class MethodDeclarationRewriter : SyntaxRewriter
                    {
                        private readonly SyntaxNode originalMethod;
                        private readonly SyntaxNode updatedMethod;
                        private readonly IComparer<SyntaxNode> methodNameComparer; 

                        internal MethodDeclarationRewriter(SyntaxNode originalMethod, SyntaxNode 
                            updatedMethod)
                        {
                            this.originalMethod = originalMethod;
                            this.updatedMethod = updatedMethod;
                            this.methodNameComparer = RefactoringDetectionUtils.
                                GetMethodDeclarationNameComparer();
                        }

                        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
                        {
                            // If the visited method has the same name with the target extracted method.
                            if (methodNameComparer.Compare(node, originalMethod) == 0)
                            {
                                return updatedMethod;
                            }
                            return node;
                        }
                    }

                    /// <summary>
                    /// Update all the method invocations in the solution. 
                    /// </summary>
                    /// <param name="document"></param>
                    /// <returns></returns>
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
                            var updatedRoot = new InvocationsAddArgumentsRewriter(invocations, 
                                typeNameTuple.Item2).Visit(root);

                            // Update solution by update the document.
                            document = document.UpdateSyntaxRoot(updatedRoot);
                        }
                        return document;
                    }

                    /// <summary>
                    /// Syntax rewriter for adding arguments to given method invocations.
                    /// </summary>
                    private class InvocationsAddArgumentsRewriter : SyntaxRewriter
                    {
                        private readonly string addedArgument;
                        private readonly IEnumerable<SyntaxNode> invocations;
                        private readonly IMethodInvocationAnalyzer analyzer;

                        internal InvocationsAddArgumentsRewriter(IEnumerable<SyntaxNode> invocations,
                            string addedArgument)
                        {
                            this.invocations = invocations;
                            this.addedArgument = addedArgument;
                            this.analyzer = AnalyzerFactory.GetMethodInvocationAnalyzer();
                        }

                        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
                        {
                            if (invocations.Any(i => ASTUtil.AreSyntaxNodesSame(i, node)))
                            {
                                analyzer.SetMethodInvocation(node);
                                return analyzer.AddArguments(new[] {addedArgument});
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
