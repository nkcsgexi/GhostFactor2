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

            protected override ICodeIssueComputer CheckCondition(IDocument before, IDocument after,
                                                                 IManualExtractMethodRefactoring input)
            {
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
                        ConditionCheckersUtils.GetTypeNameTuples(missing), input.MetaData.DocumentUniqueName);
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
                private IEnumerable<Tuple<string, string>> typeNameTuples;

                /* Declaration of the extracted method. */
                private SyntaxNode declaration;

                private IComparer<SyntaxNode> methodNameComparer; 

                public ReturnTypeCheckingResult(SyntaxNode declaration,
                    IEnumerable<Tuple<string, string>> typeNameTuples, string uniqueName) :base(uniqueName)
                {
                    this.declaration = declaration;
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
                                    new ICodeAction[]{new AddReturnValueCodeAction(document, declaration, typeNameTuples) });
                        }
                    }
                }

                public override bool Equals(ICodeIssueComputer o)
                {
                    // If the other is not in the same RefactoringType, return false
                    if (o is ReturnTypeCheckingResult)
                    {
                        var other = (ReturnTypeCheckingResult) o;
                        var methodsComparator = new MethodsComparator();
                        var stringEnumerablesComparator = new StringEnumerablesComparator();

                        // If the method declarations are equal to each other.
                        return methodsComparator.Compare(declaration, other.declaration) == 0 &&
                               // Also the contained return names are equal to each other, return true;
                               stringEnumerablesComparator.Compare(typeNameTuples.Select(t => t.Item2),
                                                                   other.typeNameTuples.Select(t => t.Item2)) == 0;
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
                private readonly IDocument document;
                private readonly Logger logger;

                // Can only handle one tuple, even though multiple are passed in.
                private readonly Tuple<string, string> handledTypeName;

                internal AddReturnValueCodeAction(IDocument document, SyntaxNode declaration,
                                                  IEnumerable<Tuple<string, string>> typeNameTuples)
                {
                    this.document = document;
                    this.declaration = declaration;
                    this.typeNameTuples = typeNameTuples;
                    this.handledTypeName = typeNameTuples.FirstOrDefault();
                    this.logger = NLoggerUtil.GetNLogger(typeof (AddReturnValueCodeAction));
                }

                public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
                {
            
                    // Use the rewrite visiter to change the target method declaration.
                    var newRoot = new AddReturnValueRewriter(declaration,
                        handledTypeName.Item1, handledTypeName.Item2).
                            Visit((SyntaxNode) document.GetSyntaxRoot());

                    // Update the document with the new root and return the code action.
                    var updatedDocument = document.UpdateSyntaxRoot(newRoot);
                    return new CodeActionEdit(updatedDocument);
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
                    private readonly string returnSymbolName;
                    private readonly string returnSymbolType;

                    internal AddReturnValueRewriter(SyntaxNode declaration, String returnSymbolType,
                                                    String returnSymbolName)
                    {
                        this.declaration = declaration;
                        this.returnSymbolType = returnSymbolType;
                        this.returnSymbolName = returnSymbolName;
                    }

                    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
                    {
                        if (node.Span.Equals(declaration.Span))
                        {
                            // Use method analyzer to add the return value and change the return RefactoringType.
                            var methodAnalyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
                            methodAnalyzer.SetMethodDeclaration(node);
                            methodAnalyzer.ChangeReturnValue(returnSymbolName);
                            return methodAnalyzer.ChangeReturnType(returnSymbolType);
                        }
                        return node;
                    }
                }
            }
        }
    }
}
