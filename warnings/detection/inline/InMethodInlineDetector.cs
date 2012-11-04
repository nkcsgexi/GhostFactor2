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
    /* In method refactoring detector for inline method. */
    internal interface IInMethodInlineDetector: IInternalRefactoringDetector, IBeforeAndAfterDocumentKeeper
    {
        void SetRemovedMethod(SyntaxNode method);
        void SetRemovedInvocations(IEnumerable<SyntaxNode> invocations);
    }

    /* 
     * Two types of in method detector can be created. One is fined grained detector, another is dummy detector. 
     * Dummy detector almost does nothing but fast.
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

                // Get the changed blocks between the method before and method after.
                var changedBlocks = RefactoringDetectionUtils.GetChangedBlocks(GetMethodBlock(methodBefore), 
                    GetMethodBlock(methodAfter));
                
                // If only one block changes, it is likely to have inline method refactoring.
                if (changedBlocks.Count() == 1)
                {
                    var removedMethodStatements = RefactoringDetectionUtils.GetMethodStatements(methodRemoved);
                    var changedBlockAfterStatements = ((BlockSyntax) changedBlocks.First().NodeAfter).Statements;
                    var inlinedStatements = RefactoringDetectionUtils.GetLongestCommonStatements(
                        removedMethodStatements, changedBlockAfterStatements, new SyntaxNodeExactComparer()).
                            Select(p => p.Value);        
                   
                    // If the inlined statements are above threshhold, an inline method refactoring is detected.
                    if (inlinedStatements.Count() > COUNT_THRESHHOLD)
                    {
                        var refactoring = ManualRefactoringFactory.CreateManualInlineMethodRefactoring
                            // Only considering the first invocation.
                            (docBefore, docAfter, methodBefore, methodAfter, methodRemoved, 
                                invocationsRemoved.First(), inlinedStatements);
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
        }

        /* A dummy inline detector. */
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
