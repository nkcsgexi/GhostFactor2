using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using warnings.analyzer;
using warnings.analyzer.comparators;
using warnings.quickfix;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.resources;
using warnings.retriever;
using warnings.util;
using warnings.util.Cache;

namespace warnings.conditions
{
    internal partial class ChangeMethodSignatureConditionsList
    {
        /* Code issue of an invocation whose method declaration's signature is updated. */
        private class UnupdatedMethodSignatureChecker : IRefactoringConditionChecker
        {
            private static IRefactoringConditionChecker instance;

            public static IRefactoringConditionChecker GetInstance()
            {
                if (instance == null)
                {
                    instance = new UnupdatedMethodSignatureChecker();
                }
                return instance;
            }

            public RefactoringType RefactoringType
            {
                get { return RefactoringType.CHANGE_METHOD_SIGNATURE; }
            }

            public IConditionCheckingResult CheckCondition(ManualRefactoring input)
            {
                var signatureRefactoring = (IChangeMethodSignatureRefactoring) input;
                return new UnchangedMethodInvocationComputer(((IChangeMethodSignatureRefactoring) input).
                    ChangedMethodDeclaration, signatureRefactoring.ParametersMap.AsEnumerable(), 
                        input.MetaData);
            }

            public Predicate<SyntaxNode> GetIssuedNodeFilter()
            {
                return n => n.Kind == SyntaxKind.InvocationExpression;
            }


            /// <summary>
            /// The computer for calculating the unchanged method invocations.
            /// </summary>
            private class UnchangedMethodInvocationComputer : SingleDocumentValidCodeIssueComputer
            {
                private readonly SyntaxNode declaration;
                private readonly IEnumerable<Tuple<int, int>> mappings;

                public UnchangedMethodInvocationComputer(SyntaxNode declaration, IEnumerable<Tuple<int, int>> 
                    mappings, RefactoringMetaData metaData) : base(metaData)
                {
                    this.declaration = declaration;
                    this.mappings = mappings;
                }


                public override bool IsIssueResolved(ICorrectRefactoringResult correctRefactoringResult)
                {
                    throw new NotImplementedException();
                }

                public override IEnumerable<SyntaxNode> GetPossibleSyntaxNodes(IDocument document)
                {
                    return ((SyntaxNode)document.GetSyntaxRoot()).DescendantNodes().
                        Where(n => n.Kind == SyntaxKind.InvocationExpression);
                }

                public override IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node)
                {
                    if (node.Kind == SyntaxKind.InvocationExpression)
                    {
                        var invocations = GetInvocations(document);

                        // If the given node is in the invocations, return a corresponding code issue.
                        if (invocations.Any(n => HasSameMethodName(n, node)))
                        {
                            yield return new CodeIssue(CodeIssue.Severity.Error, node.Span, 
                                "Method invocation needs update.",
                                // With the code action of change this signature with singleDocumentCorrect arguments order
                                new ICodeAction[]{ new CorrectAllSignaturesInSolution(document, declaration, 
                                    mappings, this)
                                });
                        }
                    }
                }

                private bool HasSameMethodName(SyntaxNode invocation1, SyntaxNode invocation2)
                {
                    var analyzer = AnalyzerFactory.GetMethodInvocationAnalyzer();
                    analyzer.SetMethodInvocation(invocation1);
                    var name1 = analyzer.GetMethodName().GetText();
                    analyzer.SetMethodInvocation(invocation2);
                    var name2 = analyzer.GetMethodName().GetText();
                    return name1.Equals(name2);
                }

                /* Get all the invocations in a document by brutal force. */
                private IEnumerable<SyntaxNode> GetInvocations(IDocument document)
                {
                    // Retrievers for method invocations.
                    var retriever = RetrieverFactory.GetMethodInvocationRetriever();
                    retriever.SetDocument(document);

                    // Get all the invocations in the current solution.
                    retriever.SetMethodDeclaration(declaration);
                    return retriever.GetInvocations();
                }

                public override bool Equals(ICodeIssueComputer o)
                {
                    if (IsIssuedToSameDocument(o))
                    {
                        var other = o as UnchangedMethodInvocationComputer;
                        if (other != null)
                        {
                            var comparator = new MethodNameComparer();
                            return comparator.Compare(declaration, other.declaration) == 0;
                        }
                    }
                    return false;
                }

                public override RefactoringType RefactoringType
                {
                    get { return RefactoringType.CHANGE_METHOD_SIGNATURE; }
                }

                public override RefactoringConditionType RefactoringConditionType
                {
                    get { return RefactoringConditionType.CHANGE_METHOD_SIGNATURE_UNUPDATED; }
                }
            }

            /// <summary>
            /// Correct the current invocation expression by changing all the parameters to the right places.
            /// </summary>
            private class CorrectSignatureCodeAction : ICodeAction
            {
                private readonly IDocument document;
                private readonly InvocationExpressionSyntax invocation;
                private readonly IEnumerable<Tuple<int, int>> mappings;
                private readonly ICodeIssueComputer computer;

                public CorrectSignatureCodeAction(IDocument document, SyntaxNode invocation,
                        IEnumerable<Tuple<int, int>> mappings, ICodeIssueComputer computer)
                {
                    this.document = document;
                    this.invocation = (InvocationExpressionSyntax) invocation;
                    this.mappings = mappings;
                    this.computer = computer;
                }

                public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
                {
                    // Get the updated invocation.
                    var analyzer = AnalyzerFactory.GetMethodInvocationAnalyzer();
                    analyzer.SetMethodInvocation(invocation);
                    var updatedInvocation = analyzer.ReorderAuguments(mappings);

                    // Rewriting this node.
                    var rewriter = new SingleInvocationRewriter(invocation, updatedInvocation);
                    var newRoot = rewriter.Visit((SyntaxNode) document.GetSyntaxRoot());
                    var updatedSolution = document.Project.Solution.UpdateDocument(document.Id, newRoot);
                    return new CodeActionEdit(null, updatedSolution, ConditionCheckersUtils.
                        GetRemoveCodeIssueComputerOperation(computer));
                }

                public ImageSource Icon
                {
                    get { return ResourcePool.GetIcon(); }
                }

                public string Description
                {
                    get { return "Change Method Signature Automatically."; }
                }

                /// <summary>
                /// Syntax rewriter to update a single method invocation.
                /// </summary>
                private class SingleInvocationRewriter : SyntaxRewriter
                {
                    private readonly SyntaxNode updatedInvocation;
                    private readonly SyntaxNode invocation;

                    internal SingleInvocationRewriter(SyntaxNode invocation, SyntaxNode updatedInvocation)
                    {
                        this.invocation = invocation;
                        this.updatedInvocation = updatedInvocation;
                    }

                    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax 
                        visitedInvocation)
                    {
                        // If the visited node is the invocation where the issue was issued.
                        if (visitedInvocation.Span.Equals(invocation.Span))
                        {
                            // Change it to the updated invocatio.
                            return updatedInvocation;
                        }
                        return visitedInvocation;
                    }
                }
            }

            /// <summary>
            /// Code action that corrects all the invocations of a given method declaration in a solution.
            /// </summary>
            private class CorrectAllSignaturesInSolution : ICodeAction
            {
                private readonly SyntaxNode declaration;
                private readonly IEnumerable<Tuple<int, int>> mappings;
                private readonly IDocument document;
                private readonly ICodeIssueComputer computer;

                internal CorrectAllSignaturesInSolution(IDocument document, SyntaxNode declaration,
                    IEnumerable<Tuple<int, int>> mappings, ICodeIssueComputer computer)
                {
                    this.document = document;
                    this.declaration = declaration;
                    this.mappings = mappings;
                    this.computer = computer;
                }

                public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
                {
                    // Get an invocation retriever and set the declaration and document.
                    var retriever = RetrieverFactory.GetMethodInvocationRetriever();
                    retriever.SetMethodDeclaration(declaration);
                    retriever.SetDocument(document);

                    // Get all the invocations in the document.
                    var invocations = retriever.GetInvocations();

                    // If there is some invocations of the given declaration in the specfic document
                    if (invocations.Any())
                    {
                        var rewriter = new MultipleInvocationsRewriter(invocations, mappings);
                        var updatedRoot = rewriter.Visit((SyntaxNode) document.GetSyntaxRoot());
                        var updatedSolution = document.Project.Solution.UpdateDocument(document.Id, 
                            updatedRoot);

                        return new CodeActionEdit(null, updatedSolution, ConditionCheckersUtils.
                            GetRemoveCodeIssueComputerOperation(computer));
                    }
                    return null;
                }

                public ImageSource Icon
                {
                    get { return ResourcePool.GetIcon(); }
                }

                public string Description
                {
                    get { return "Correct all the non-updated invocations."; }
                }

               
                /// <summary>
                ///  Rewriting several invocations in a given document according to the given invocation nodes 
                /// and the arguments mapping information.
                /// </summary>
                private class MultipleInvocationsRewriter : SyntaxRewriter
                {
                    private readonly IEnumerable<SyntaxNode> invocations;
                    private readonly IEnumerable<Tuple<int, int>> mappings;

                    internal MultipleInvocationsRewriter(IEnumerable<SyntaxNode> invocations,
                                                         IEnumerable<Tuple<int, int>> mappings)
                    {
                        this.invocations = invocations;
                        this.mappings = mappings;
                    }

                    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax 
                        visitedInvocation)
                    {
                        // If the visited node is among the given invocations.
                        if (invocations.Any(i => i.Span.Equals(visitedInvocation.Span)))
                        {
                            // Reorder the arguments of this invocation and return the reorderred invocation.
                            var analyzer = AnalyzerFactory.GetMethodInvocationAnalyzer();
                            analyzer.SetMethodInvocation(visitedInvocation);
                            return analyzer.ReorderAuguments(mappings);
                        }
                        return visitedInvocation;
                    }
                }
            }

            public RefactoringConditionType RefactoringConditionType 
            { 
                get { return RefactoringConditionType.CHANGE_METHOD_SIGNATURE_UNUPDATED;}
            }
        }
    }
}
