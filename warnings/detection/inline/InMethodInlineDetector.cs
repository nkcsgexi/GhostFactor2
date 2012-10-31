using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using warnings.analyzer;
using warnings.analyzer.comparators;
using warnings.util;

namespace warnings.refactoring.detection
{
    /* In method refactoring detector for inline method. */
    internal interface IInMethodInlineDetector: IInternalRefactoringDetector
    {
        void SetRemovedMethod(SyntaxNode method);
        void SetRemovedInvocations(IEnumerable<SyntaxNode> invocations);
    }

    /* 
     * Two types of in method detector can be created. One is fined grained detector, another is dummy detector. Dummy detector
     * almost does nothing but fast.
     */
    internal class InMethodInlineDetectorFactory
    {
        public static IInMethodInlineDetector GetFineGrainedDetector()
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

            public abstract bool HasRefactoring();

            internal InMethodInlineDetector()
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
        }


        /* Inline method refactoring detector in the method level. */
        private class InMethodInlineRefactoringDetector : InMethodInlineDetector
        {
            private readonly static int COUNT_THRESHHOLD = 1;
            private readonly Logger logger;

            internal InMethodInlineRefactoringDetector()
            {
                this.logger = NLoggerUtil.GetNLogger(typeof(InMethodInlineRefactoringDetector));
            }

            public override bool HasRefactoring()
            {
                refactorings.Clear();

                // Get the inlined statements.
                var inlinedStatements = GetLongestNeigboredStatements(GetCommonStatements(methodAfter, methodRemoved));
                logger.Info("Longest common statements length: " + inlinedStatements.Count());

                // If the inlined statements are above threshhold, an inline method refactoring is detected.
                if (inlinedStatements.Count() > COUNT_THRESHHOLD)
                {
                    var refactoring = ManualRefactoringFactory.CreateManualInlineMethodRefactoring
                        // Only considering the first invocation.
                        (methodBefore, methodAfter, methodRemoved, invocationsRemoved.First(), inlinedStatements);
                    refactorings.Add(refactoring);
                    return true;
                }
                return false;
            }

            private IEnumerable<SyntaxNode> GetCommonStatements(SyntaxNode methodAfter, SyntaxNode inlinedMethod)
            {
                // Get all the statements in the caller after inlining. 
                var methodAnalyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
                methodAnalyzer.SetMethodDeclaration(methodAfter);
                var afterMethodStatements = methodAnalyzer.GetStatements();

                // Get all the statements in the inlined method.
                methodAnalyzer.SetMethodDeclaration(inlinedMethod);
                var inlinedMethodStatements = methodAnalyzer.GetStatements();
                var commonStatements = new List<SyntaxNode>();
                var statementComparerer = new SyntaxNodeExactComparer();

                // Get the statements in the caller method after inlining that also appear in the
                // inlined method.
                foreach (var afterStatement in afterMethodStatements)
                {
                    foreach (var inlinedStatement in inlinedMethodStatements)
                    {
                        if (statementComparerer.Compare(afterStatement, inlinedStatement) == 0)
                        {
                            logger.Info("Common statement: " + inlinedStatement);
                            commonStatements.Add(afterStatement);
                        }
                    }
                }
                return commonStatements;
            }

            private IEnumerable<SyntaxNode> GetLongestNeigboredStatements(IEnumerable<SyntaxNode> statements)
            {
                var analyzer = AnalyzerFactory.GetSyntaxNodesAnalyzer();
                analyzer.SetSyntaxNodes(statements);
                return analyzer.GetLongestNeighborredNodesGroup();
            }
        }

        /* A dummy inline detector. */
        private class DummyInMethodInlineDetector : InMethodInlineDetector
        {
            public override bool HasRefactoring()
            {
                refactorings.Clear();
                var refactoring = ManualRefactoringFactory.CreateSimpleInlineMethodRefactoring(methodBefore, methodAfter, 
                    methodRemoved);
                refactorings.Add(refactoring);
                return true;
            }
        }
    }
}
