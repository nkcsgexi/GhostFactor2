using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.analyzer.comparators;
using warnings.util;

namespace warnings.refactoring.detection
{
    /// <summary>
    /// In method refactoring detector for inline method.
    /// </summary>
    internal interface IInMethodInlineDetector: IInternalRefactoringDetector, IBeforeAndAfterDocumentKeeper
    {
        void SetRemovedMethod(SyntaxNode method);
        void SetRemovedInvocations(IEnumerable<SyntaxNode> invocations);
    }

    /// <summary>
    /// Two types of in method detector can be created. One is fined grained detector, another is dummy 
    /// detector. Dummy detector almost does nothing but fast.
    /// </summary>
    internal class InMethodInlineDetectorFactory
    {
        public static IInMethodInlineDetector GetInlineDetectorByStatement()
        {
            return new InMethodInlineRefactoringDetector();
        }

        public static IInMethodInlineDetector GetDummyDetector()
        {
            return new DummyInMethodInlineDetector();
        }

        private abstract class InMethodInlineDetector : IInMethodInlineDetector
        {
            protected readonly List<ManualRefactoring> refactorings;
            protected SyntaxNode methodBefore;
            protected SyntaxNode methodAfter;
            protected SyntaxNode methodRemoved;
            protected IEnumerable<SyntaxNode> invocationsRemoved;
            protected IDocument docBefore;
            protected IDocument docAfter;

            public abstract bool HasRefactoring();

            protected InMethodInlineDetector()
            {
                refactorings = new List<ManualRefactoring>();
            }

            public IEnumerable<ManualRefactoring> GetRefactorings()
            {
                return refactorings;
            }

            public void SetSyntaxNodeBefore(SyntaxNode before)
            {
                this.methodBefore = before;
            }

            public void SetSyntaxNodeAfter(SyntaxNode after)
            {
                this.methodAfter = after;
            }

            public void SetRemovedMethod(SyntaxNode methodRemoved)
            {
                this.methodRemoved = methodRemoved;
            }

            public void SetRemovedInvocations(IEnumerable<SyntaxNode> invocationsRemoved)
            {
                this.invocationsRemoved = invocationsRemoved;
            }

            public void SetDocumentBefore(IDocument docBefore)
            {
                this.docBefore = docBefore;
            }

            public void SetDocumentAfter(IDocument docAfter)
            {
                this.docAfter = docAfter;
            }
        }


        /// <summary>
        /// Inline method refactoring detector in the method level.
        /// </summary>
        private class InMethodInlineRefactoringDetector : InMethodInlineDetector
        {
            private readonly static int COUNT_THRESHHOLD = 0;
            private readonly Logger logger;
            private readonly IComparer<SyntaxNode> syntaxNodeExactComparer; 

            internal InMethodInlineRefactoringDetector()
            {
                this.logger = NLoggerUtil.GetNLogger(typeof(InMethodInlineRefactoringDetector));
                this.syntaxNodeExactComparer = new SyntaxNodeExactComparer();
            }

            public override bool HasRefactoring()
            {
                refactorings.Clear();

                // Get the changed blocks between the method before and method after.
                var changedBlocks = RefactoringDetectionUtils.GetChangedBlocks(GetMethodBlock(methodBefore), 
                    GetMethodBlock(methodAfter)).ToList();
                
                // If only one block changes, it is likely to have inline method refactoring.
                if (changedBlocks.Count() == 1)
                {
                    var removedMethodStatements = RefactoringDetectionUtils.GetMethodStatements(methodRemoved);
                    var changedBlockBeforeStatements = ((BlockSyntax) changedBlocks.First().NodeBefore).
                        Statements;
                    var changedBlockAfterStatements = ((BlockSyntax) changedBlocks.First().NodeAfter).
                        Statements;
                    var inlinedStatements = RefactoringDetectionUtils.GetLongestCommonStatements(
                        removedMethodStatements, changedBlockAfterStatements, syntaxNodeExactComparer).
                            Select(p => p.Value).ToList();        
                   
                    // If the inlined statements are above threshhold, an inline method refactoring is detected.
                    if (inlinedStatements.Count() > COUNT_THRESHHOLD)
                    {
                        var refactoring = CreateInlineRefactoring(changedBlockBeforeStatements, 
                            changedBlockAfterStatements);
                        refactorings.Add(refactoring);
                        return true;
                    }
                }
                return false;
            }

            private SyntaxNode GetMethodBlock(SyntaxNode method)
            {
                return ((MethodDeclarationSyntax)method).Body;
            }

            private ManualRefactoring CreateInlineRefactoring(IEnumerable<SyntaxNode> statementsBefore,
                IEnumerable<SyntaxNode> statementsAfter )
            {
                return ManualRefactoringFactory.CreateManualInlineMethodRefactoring
                    // Only considering the first invocation.
                    (docBefore, docAfter, methodBefore, methodAfter, methodRemoved,
                       invocationsRemoved.First(), GetInlinedStatements(statementsBefore.ToList(), 
                            statementsAfter.ToList(), invocationsRemoved.First()));
            }

            /// <summary>
            /// Calculate the inlined statements, given the statements before and after inlining a called 
            /// method.
            /// </summary>
            /// <param name="beforeStatements"></param>
            /// <param name="afterStatements"></param>
            /// <param name="invocation"></param>
            /// <returns></returns>
            private IEnumerable<SyntaxNode> GetInlinedStatements(IList<SyntaxNode> beforeStatements,
                IList<SyntaxNode> afterStatements, SyntaxNode invocation)
            {
                SyntaxNode invokingStatemet;
                if(TryGetStatementEnclosingInvocation(invocation, out invokingStatemet))
                {
                    // Get the index of the invocation statement in the before statements.
                    var index = GetSyntaxNodeIndex(beforeStatements, invokingStatemet);

                    // Get the statements before and after the invocation statements.
                    var statementsBeforeInvocation = GetSubNodes(beforeStatements, 0, index);
                    var statementsAfterInvocation = GetSubNodes(beforeStatements, index + 1,
                        beforeStatements.Count() - index - 1);

                    // Get the statements length in the after statements list that are syntactically equal to
                    // the statements before and after the invocation.
                    var startCommonLength = CommonNodesLengthFromStart(statementsBeforeInvocation.ToList(), 
                        afterStatements);
                    var endCommonLength = CommonNodesLengthFromEnd(statementsAfterInvocation.ToList(), 
                        afterStatements);

                    // The start index of the inlined statements.
                    var startIndex = startCommonLength;

                    // The length of the inlined statements.
                    var length = afterStatements.Count() - startCommonLength - endCommonLength;
                    return GetSubNodes(afterStatements, startIndex, length);

                }
                return Enumerable.Empty<SyntaxNode>();
            }


            /// <summary>
            /// Get the common nodes count from either the start of the end of both nodes enumerables.
            /// </summary>
            /// <param name="nodes1"></param>
            /// <param name="nodes2"></param>
            /// <returns></returns>
            private int CommonNodesLengthFromStart(IList<SyntaxNode> nodes1, IList<SyntaxNode> nodes2)
            {
                for (int i = 0; i < nodes1.Count() && i < nodes2.Count(); i ++)
                {
                    var n1 = nodes1.ElementAt(i);
                    var n2 = nodes2.ElementAt(i);
                    if (syntaxNodeExactComparer.Compare(n1, n2) != 0)
                        return i;
                }
                return Math.Min(nodes1.Count(), nodes2.Count());
            }

            private int CommonNodesLengthFromEnd(IList<SyntaxNode> nodes1, IList<SyntaxNode> nodes2)
            {
                nodes1 = nodes1.Reverse().ToList();
                nodes2 = nodes2.Reverse().ToList();
                return CommonNodesLengthFromStart(nodes1, nodes2);
            }

            /// <summary>
            /// Get the subnodes list of a given nodes list.
            /// </summary>
            /// <param name="nodes"></param>
            /// <param name="start"></param>
            /// <param name="length"></param>
            /// <returns></returns>
            private IEnumerable<SyntaxNode> GetSubNodes(IEnumerable<SyntaxNode> nodes, int start, int length)
            {
                var list = nodes.ToList();
                return list.GetRange(start, length);
            }


            /// <summary>
            /// Try to get the statement syntax node that is enclosing the given method invocation.
            /// </summary>
            /// <param name="invocation"></param>
            /// <param name="statement"></param>
            /// <returns></returns>
            private bool TryGetStatementEnclosingInvocation(SyntaxNode invocation, out SyntaxNode statement)
            {
                var statements = invocation.AncestorsAndSelf().OfType<StatementSyntax>().ToList();
                if(statements.Any())
                {
                    statement = statements.First();
                    return true;
                }
                statement = null;
                return false;
            }
   
            /// <summary>
            /// Given an enumerable of syntax node, get the element index of the given node.
            /// </summary>
            /// <param name="nodeList"></param>
            /// <param name="node"></param>
            /// <returns></returns>
            private int GetSyntaxNodeIndex(IEnumerable<SyntaxNode> nodeList, SyntaxNode node)
            {
                var list = nodeList.ToList();
                return list.IndexOf(node);
            }
        }

        /// <summary>
        /// A dummy inline detector.
        /// </summary>
        private class DummyInMethodInlineDetector : InMethodInlineDetector
        {
            public override bool HasRefactoring()
            {
                refactorings.Clear();
                var refactoring = ManualRefactoringFactory.CreateSimpleInlineMethodRefactoring(
                    docBefore, docAfter, methodBefore, methodAfter, methodRemoved);
                refactorings.Add(refactoring);
                return true;
            }
        }
    }
}
