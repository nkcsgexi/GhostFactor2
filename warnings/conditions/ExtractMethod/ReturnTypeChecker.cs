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
        /* Checker for whether the extracted method returns the right value. */
        private class ReturnTypeChecker : ExtractMethodConditionChecker
        {
            private Logger logger = NLoggerUtil.GetNLogger(typeof (ReturnTypeChecker));

            public override Predicate<SyntaxNode> GetIssuedNodeFilter()
            {
                return n => n.Kind == SyntaxKind.MethodDeclaration;
            }

            protected override ICodeIssueComputer CheckCondition(
                IManualExtractMethodRefactoring input)
            {
                var before = input.BeforeDocument;
                var after = input.AfterDocument;

                // Calculate the outflow data
                IEnumerable<ISymbol> flowOuts;
                if (input.ExtractedStatements != null)
                    flowOuts = GetFlowOutData(input.ExtractedStatements, before);
                else
                    flowOuts = GetFlowOutData(input.ExtractedExpression, before);

                // Get the returning data of the return statements.
                var delaration = input.ExtractedMethodDeclaration;
                var methodAnalyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
                methodAnalyzer.SetMethodDeclaration(delaration);

                // Get the returning data in the return statements of the extracted method, also log them.
                var returningData = GetMethodReturningData(methodAnalyzer, after);

                // Missing symbols that are in the flow out before but not in the returning data.
                // Remove this symbol.
                var missing = ConditionCheckersUtils.RemoveThisSymbol(
                    ConditionCheckersUtils.GetSymbolListExceptByName(flowOuts, returningData));

                if (missing.Any())
                {
                   
                    return new ReturnTypeCheckingResult(input.ExtractedMethodDeclaration, 
                        input.ExtractMethodInvocation, ConditionCheckersUtils.GetTypeNameTuples(missing), 
                            input.MetaData);
                }
                return new NullCodeIssueComputer();
            }

            private IEnumerable<ISymbol> GetFlowOutData(IEnumerable<SyntaxNode> statements, IDocument document)
            {
                var statementsDataFlowAnalyzer = AnalyzerFactory.GetStatementsDataFlowAnalyzer();
                statementsDataFlowAnalyzer.SetDocument(document);
                statementsDataFlowAnalyzer.SetStatements(statements);
                var flowOuts = statementsDataFlowAnalyzer.GetFlowOutData();
                logger.Info("Statements Flowing Out Data: " + StringUtil.ConcatenateAll(", ",
                    flowOuts.Select(s => s.Name)));
                return flowOuts;
            }

            private IEnumerable<ISymbol> GetFlowOutData(SyntaxNode expression, IDocument document)
            {
                var expressionDataFlowAnalyzer = AnalyzerFactory.GetExpressionDataFlowAnalyzer();
                expressionDataFlowAnalyzer.SetDocument(document);
                expressionDataFlowAnalyzer.SetExpression(expression);
                var flowOuts = expressionDataFlowAnalyzer.GetFlowOutData();
                logger.Info("Expression Flowing Out Data: " + StringUtil.ConcatenateAll(", ",
                    flowOuts.Select(s => s.Name)));
                return flowOuts;
            }


            private IEnumerable<ISymbol> GetMethodReturningData(IMethodDeclarationAnalyzer methodDeclarationAnalyzer,
                                                                IDocument document)
            {

                // The returning data from the return statements is initiated as empty.
                var returningData = Enumerable.Empty<ISymbol>();

                // If having return statement, then retuning data could be not empty.
                if (methodDeclarationAnalyzer.HasReturnStatement())
                {
                    // Get all the return statements.
                    var return_statements = methodDeclarationAnalyzer.GetReturnStatements();

                    // Get the data flow analyzer for statements.
                    var dataFlowAnalyzer = AnalyzerFactory.GetStatementsDataFlowAnalyzer();

                    // Set the document to be the after one.
                    dataFlowAnalyzer.SetDocument(document);

                    // A list containing one statement.
                    var stats = new List<SyntaxNode>();

                    foreach (var s in return_statements)
                    {
                        // make the list empty first.
                        stats.Clear();

                        // Analyze one single return statement at each iteration
                        stats.Add(s);
                        dataFlowAnalyzer.SetStatements(stats);

                        // Combining with the current result.
                        returningData = returningData.Union(dataFlowAnalyzer.GetFlowInData());
                    }
                }
                logger.Info("Returning Data: " + StringUtil.ConcatenateAll(", ", returningData.Select(s => s.Name)));
                return returningData;
            }

            /* Code issue computers for the checking results of retrun RefactoringType.*/
            private class ReturnTypeCheckingResult : SingleDocumentValidCodeIssueComputer
            {
                /* The RefactoringType/name tuples for missing return values. */
                private readonly IEnumerable<Tuple<string, string>> typeNameTuples;

                /* Declaration of the extracted method. */
                private readonly SyntaxNode declaration;
                private readonly SyntaxNode invocation;

                private readonly IComparer<SyntaxNode> methodNameComparer;
               

                public ReturnTypeCheckingResult(SyntaxNode declaration, SyntaxNode invocation, 
                    IEnumerable<Tuple<string, string>> typeNameTuples, RefactoringMetaData metaData) : base(metaData)
                {
                    this.declaration = declaration;
                    this.invocation = invocation;
                    this.typeNameTuples = typeNameTuples;
                    this.methodNameComparer = RefactoringDetectionUtils.GetMethodDeclarationNameComparer();
                }

                public override IEnumerable<SyntaxNode> GetPossibleSyntaxNodes(IDocument document)
                {
                    return ((SyntaxNode)document.GetSyntaxRoot()).DescendantNodes(n => n.Kind != SyntaxKind.MethodDeclaration)
                        .Where(n => n.Kind == SyntaxKind.MethodDeclaration);
                }

                public override IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node)
                {
                    // If the given node is not method invocation, return directly.
                    if (node.Kind == SyntaxKind.MethodDeclaration)
                    {
                        // If the method is the with the same name, then issue the issue to this method.
                        if (methodNameComparer.Compare(node, declaration) == 0)
                        {
                            yield return new CodeIssue(CodeIssue.Severity.Error, node.Span,
                                "Missing return values: " + StringUtil.ConcatenateAll(",",typeNameTuples.Select( t => t.Item2)),
                                    // Create a quick fix for adding the first missing return value.
                                    new ICodeAction[]{new AddReturnValueCodeAction(document, declaration, invocation, 
                                        typeNameTuples, this) });
                        }
                    }
                }

                public override bool Equals(ICodeIssueComputer o)
                {
                    if (IsIssuedToSameDocument(o))
                    {
                        // If the other is not in the same RefactoringType, return false
                        if (o is ReturnTypeCheckingResult)
                        {
                            var other = (ReturnTypeCheckingResult) o;
                            var methodsComparator = RefactoringDetectionUtils.GetMethodDeclarationNameComparer();

                            // If the method declarations are equal to each other.
                            return methodsComparator.Compare(declaration, other.declaration) == 0;
                        }
                    }
                    return false;
                }

                public override RefactoringType RefactoringType
                {
                    get { return RefactoringType.EXTRACT_METHOD; }
                }
            }

            /* Code action to add a return value automatically. */
            private class AddReturnValueCodeAction : ICodeAction
            {
                private readonly IEnumerable<Tuple<string, string>> typeNameTuples;
                private readonly SyntaxNode declaration;
                private readonly SyntaxNode invocation;
                private readonly IDocument document;
                private readonly Logger logger;
                private readonly ICodeIssueComputer computer;

                // Can only handle one tuple, even though multiple are passed in.
                private readonly Tuple<string, string> handledTypeName;

                internal AddReturnValueCodeAction(IDocument document, SyntaxNode declaration, 
                    SyntaxNode invocation, IEnumerable<Tuple<string, string>> typeNameTuples, 
                        ICodeIssueComputer computer)
                {
                    this.document = document;
                    this.declaration = declaration;
                    this.invocation = invocation;
                    this.typeNameTuples = typeNameTuples;
                    this.handledTypeName = typeNameTuples.FirstOrDefault();
                    this.computer = computer;
                    this.logger = NLoggerUtil.GetNLogger(typeof (AddReturnValueCodeAction));
                }

                public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
                {
            
                    // Use the rewrite visiter to change the target method declaration.
                    var newRoot = new AddReturnValueRewriter(declaration, invocation,
                        handledTypeName.Item1, handledTypeName.Item2).
                            Visit((SyntaxNode) document.GetSyntaxRoot());

                    // Update the document with the new root and return the code action.
                    var updatedDocument = document.UpdateSyntaxRoot(newRoot);
                    var updatedSolution = document.Project.Solution.UpdateDocument(updatedDocument);
                    return new CodeActionEdit(null, updatedSolution, ConditionCheckersUtils.
                        GetRemoveCodeIssueComputerOperation(computer));
                }

                public ImageSource Icon
                {
                    get { return ResourcePool.GetIcon(); }
                }

                public string Description
                {
                    get { return "Add return value " + handledTypeName.Item2; }
                }

                /* Sytnax rewriter for updating a given method declaration by adding the given returning value. */
                private class AddReturnValueRewriter : SyntaxRewriter
                {
                    private readonly SyntaxNode declaration;
                    private readonly SyntaxNode invocation;
                    private readonly SyntaxNode invokingMethod;
                    private readonly string returnSymbolName;
                    private readonly string returnSymbolType;
                    private readonly IComparer<SyntaxNode> methodNameComparer;
                    private readonly IMethodInvocationAnalyzer methodInvocationAnalyzer;
                    private readonly ISyntaxNodeAnalyzer syntaxNodeAnalyzer;

                    internal AddReturnValueRewriter(SyntaxNode declaration, SyntaxNode invocation,
                        String returnSymbolType, String returnSymbolName)
                    {
                        this.declaration = declaration;
                        this.invocation = invocation;
                        this.invokingMethod = GetOutSideMethod(invocation);
                        this.returnSymbolType = returnSymbolType;
                        this.returnSymbolName = returnSymbolName;
                        this.methodNameComparer = RefactoringDetectionUtils.GetMethodDeclarationNameComparer();
                        this.methodInvocationAnalyzer = AnalyzerFactory.GetMethodInvocationAnalyzer();
                        
                    }

                    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
                    {
                        // If the declaration is the invoking method, visit its decendent to update invocations.
                        if (methodNameComparer.Compare(node, invokingMethod) == 0)
                            return base.VisitMethodDeclaration(node);

                        // If the declaration is the extracted method, update its body to add return statement.
                        if (methodNameComparer.Compare(node, declaration) == 0)
                        {
                            // Use method methodInvocationAnalyzer to add the return value and change 
                            // the return RefactoringType.
                            var methodAnalyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
                            methodAnalyzer.SetMethodDeclaration(node);
                            methodAnalyzer.ChangeReturnValue(returnSymbolName);
                            return methodAnalyzer.ChangeReturnType(returnSymbolType);
                        }
                        return node;
                    }

                    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
                    {
                        methodInvocationAnalyzer.SetMethodInvocation(node);
                        if(methodInvocationAnalyzer.HasSameMethodName(invocation))
                        {
                            if (NeedAssignment(invocation))
                            {
                                // Return an expression with the assignment to the missed return value.
                                return Syntax.ParseExpression(returnSymbolName + " = " + node.GetText()).
                                    WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(
                                        node.GetTrailingTrivia());
                            }
                        }
                        return node;
                    }

                    private SyntaxNode GetOutSideMethod(SyntaxNode node)
                    {
                        var syntaxNodeAnalyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
                        syntaxNodeAnalyzer.SetSyntaxNode(node);
                        return syntaxNodeAnalyzer.GetClosestAncestor(n => n.Kind == SyntaxKind.MethodDeclaration);
                    }

                    private bool NeedAssignment(SyntaxNode invocation)
                    {
                        return true;
                    }
                }
            }
        }
    }
}
