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
using warnings.components;
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
        ///  Checker for whether the extracted method returns the right value.
        /// </summary>
        private class ReturnTypeChecker : ExtractMethodConditionChecker
        {
            private Logger logger = NLoggerUtil.GetNLogger(typeof (ReturnTypeChecker));

            public override Predicate<SyntaxNode> GetIssuedNodeFilter()
            {
                return n => n is TypeSyntax;
            }

            protected override IConditionCheckingResult CheckCondition(
                IManualExtractMethodRefactoring refactoring)
            {
                var before = refactoring.BeforeDocument;
                var after = refactoring.AfterDocument;

                // Calculate the outflow data
                IEnumerable<ISymbol> flowOuts;
                if (refactoring.ExtractedStatements != null)
                    flowOuts = GetFlowOutData(refactoring.ExtractedStatements, before);
                else
                    flowOuts = GetFlowOutData(refactoring.ExtractedExpression, before);

                // Get the returning data of the return statements.
                var delaration = refactoring.ExtractedMethodDeclaration;
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
                    return new ReturnTypeCheckingResult(refactoring.ExtractedMethodDeclaration, 
                        refactoring.ExtractMethodInvocation, ConditionCheckersUtils.GetTypeNameTuples(missing)
                            ,refactoring.MetaData);
                }
                return new SingleDocumentCorrectRefactoringResult(refactoring, this.RefactoringConditionType);
            }

            public override RefactoringConditionType RefactoringConditionType
            {
                get { return RefactoringConditionType.EXTRACT_METHOD_RETURN_VALUE;}
            }

            private IEnumerable<ISymbol> GetFlowOutData(IEnumerable<SyntaxNode> statements, IDocument 
                document)
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


            private IEnumerable<ISymbol> GetMethodReturningData(IMethodDeclarationAnalyzer 
                methodDeclarationAnalyzer, IDocument document)
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
                logger.Info("Returning Data: " + StringUtil.ConcatenateAll(", ", returningData.Select(s => 
                    s.Name)));
                return returningData;
            }

            /// <summary>
            /// Code issue computers for the checking results of retrun RefactoringType.
            /// </summary>
            private class ReturnTypeCheckingResult : SingleDocumentValidCodeIssueComputer, 
                IUpdatableCodeIssueComputer
            {
                /* The RefactoringType/name tuples for missing return values. */
                private readonly IEnumerable<Tuple<string, string>> typeNameTuples;

                /* Declaration of the extracted method. */
                private readonly SyntaxNode declaration;
                private readonly SyntaxNode invocation;

                private readonly IComparer<SyntaxNode> methodNameComparer;
               

                public ReturnTypeCheckingResult(SyntaxNode declaration, SyntaxNode invocation, 
                    IEnumerable<Tuple<string, string>> typeNameTuples, RefactoringMetaData metaData) : 
                        base(metaData)
                {
                    this.declaration = declaration;
                    this.invocation = invocation;
                    this.typeNameTuples = typeNameTuples;
                    this.methodNameComparer = new MethodNameComparer();
                }

                /// <summary>
                /// Given a refactoring good at the given condition, check whether this refactoring has 
                /// resolved this issue.
                /// </summary>
                /// <returns></returns>
                public override bool IsIssueResolved(ICorrectRefactoringResult result)
                {
                    // Convert the refactoring of correct refactoring to the desired type.
                    var single = result as ISingleDocumentResult;
                    var refactoring = result.refactoring as IManualExtractMethodRefactoring;

                    if (single != null && refactoring != null)
                    {
                        if (result.RefactoringConditionType == RefactoringConditionType.
                            EXTRACT_METHOD_RETURN_VALUE && IsIssuedToSameDocument(single))
                        {
                            // If the method names are same and return value check is passed, we consider
                            // the missing return value issue has been resolved.
                            return methodNameComparer.Compare(refactoring.ExtractedMethodDeclaration, 
                                declaration) == 0;
                        }
                    }
                    return false;
                }

                public override IEnumerable<SyntaxNode> GetPossibleSyntaxNodes(IDocument document)
                {
                    return ((SyntaxNode) document.GetSyntaxRoot()).DescendantNodes(n => n.Kind != SyntaxKind.
                        MethodDeclaration).OfType<MethodDeclarationSyntax>().Select(m => m.ReturnType);
                }

                public override IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node)
                {
                    var type = node as TypeSyntax;
                    if (type != null)
                    {
                        var method = (MethodDeclarationSyntax)ConditionCheckersUtils.TryGetOutsideMethod
                            (node);
                        if(method != null && methodNameComparer.Compare(method, declaration) == 0)
                        {
                            if (method.ReturnType.Span.Equals(node.Span))
                            {
                                return typeNameTuples.Select(t => GetReturnValueCodeIssue(document, 
                                    node, t));
                            }
                        }
                    }
                    return Enumerable.Empty<CodeIssue>();                   
                }

                private CodeIssue GetReturnValueCodeIssue(IDocument document, SyntaxNode node, Tuple<string, 
                    string> typeNameTuple)
                {
                    if (GhostFactorComponents.configurationComponent.SupportQuickFix
                        (RefactoringConditionType.EXTRACT_METHOD_RETURN_VALUE))
                    {
                        return new CodeIssue(CodeIssue.Severity.Error, node.Span, GetErrorDescription
                            (typeNameTuple), new ICodeAction[]{new AddReturnValueCodeAction(document, 
                                declaration, invocation, typeNameTuple, this)});
                    }           
                    return new CodeIssue(CodeIssue.Severity.Error, node.Span, GetErrorDescription
                        (typeNameTuple));
                }

                private string GetErrorDescription(Tuple<string, string> typeNameTuple)
                {
                    return "Extracted method needs return value: " + typeNameTuple.Item1 + " " + 
                        typeNameTuple.Item2;
                }

                public override bool Equals(ICodeIssueComputer o)
                {
                    if (IsIssuedToSameDocument(o))
                    {
                        // If the other is not in the same RefactoringType, return false
                        var other = o as ReturnTypeCheckingResult;
                        if (other != null)
                        {
                            if(methodNameComparer.Compare(declaration, other.declaration) == 0)
                            {
                                return ConditionCheckersUtils.AreStringTuplesSame(typeNameTuples, 
                                    other.typeNameTuples);
                            }
                        }
                    }
                    return false;
                }

                public bool IsUpdatedComputer(IUpdatableCodeIssueComputer o)
                {
                    var other = o as ReturnTypeCheckingResult;
                    if (other != null && other.GetDocumentId() == GetDocumentId())
                    {
                        if (methodNameComparer.Compare(declaration, other.declaration) == 0)
                        {
                            return !ConditionCheckersUtils.AreStringTuplesSame(this.typeNameTuples,
                                other.typeNameTuples);
                        }
                    }
                    return false;
                }

                public override RefactoringType RefactoringType
                {
                    get { return RefactoringType.EXTRACT_METHOD; }
                }

                public override RefactoringConditionType RefactoringConditionType
                {
                    get { return RefactoringConditionType.EXTRACT_METHOD_RETURN_VALUE; }
                }
            }

            /// <summary>
            /// Code action to add a return value automatically.
            /// </summary>
            private class AddReturnValueCodeAction : ICodeAction
            {
                private readonly Tuple<string, string> typeNameTuple;
                private readonly SyntaxNode declaration;
                private readonly SyntaxNode invocation;
                private readonly IDocument document;
                private readonly ICodeIssueComputer computer;

                internal AddReturnValueCodeAction(IDocument document, SyntaxNode declaration, 
                    SyntaxNode invocation, Tuple<string, string> typeNameTuple, 
                        ICodeIssueComputer computer)
                {
                    this.document = document;
                    this.declaration = declaration;
                    this.invocation = invocation;
                    this.typeNameTuple = typeNameTuple;
                    this.computer = computer;
                }

                public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
                {
            
                    // Use the rewrite visiter to change the target method declaration.
                    var newRoot = new AddReturnValueRewriter(declaration, invocation,
                        typeNameTuple.Item1, typeNameTuple.Item2).
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
                    get { return "Add return value " + typeNameTuple.Item2; }
                }

                /// <summary>
                /// Sytnax rewriter for updating a given method declaration by adding the given returning 
                /// value.
                /// </summary>
                private class AddReturnValueRewriter : SyntaxRewriter
                {
                    private readonly SyntaxNode declaration;
                    private readonly SyntaxNode invocation;
                    private readonly SyntaxNode invokingMethod;
                    private readonly string returnSymbolName;
                    private readonly string returnSymbolType;
                    private readonly IComparer<SyntaxNode> methodNameComparer;
                    private readonly IMethodInvocationAnalyzer methodInvocationAnalyzer;
                   
                    internal AddReturnValueRewriter(SyntaxNode declaration, SyntaxNode invocation,
                        String returnSymbolType, String returnSymbolName)
                    {
                        this.declaration = declaration;
                        this.invocation = invocation;
                        this.invokingMethod = GetOutSideMethod(invocation);
                        this.returnSymbolType = returnSymbolType;
                        this.returnSymbolName = returnSymbolName;
                        this.methodNameComparer = new MethodNameComparer();
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
                        if (invocation != null)
                        {
                            methodInvocationAnalyzer.SetMethodInvocation(node);
                            if (methodInvocationAnalyzer.HasSameMethodName(invocation))
                            {
                                if (NeedAssignment(invocation))
                                {
                                    // Return an expression with the assignment to the missed return value.
                                    return Syntax.ParseExpression(returnSymbolName + " = " + node.GetText()).
                                        WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(
                                            node.GetTrailingTrivia());
                                }
                            }
                        }
                        return null;
                    }

                    private SyntaxNode GetOutSideMethod(SyntaxNode node)
                    {
                        var syntaxNodeAnalyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
                        syntaxNodeAnalyzer.SetSyntaxNode(node);
                        return syntaxNodeAnalyzer.GetClosestAncestor(n => n.Kind == SyntaxKind.
                            MethodDeclaration);
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
