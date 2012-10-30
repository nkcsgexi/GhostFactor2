using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using warnings.analyzer.comparators;
using warnings.util;

namespace warnings.refactoring.detection
{
    internal abstract class InMethodExtractMethodDetector :  IRefactoringDetector, IBeforeAndAfterSyntaxTreeKeeper
    {
        protected MethodDeclarationSyntax callerAfter;
        protected MethodDeclarationSyntax callerBefore;
        protected MethodDeclarationSyntax calleeAfter;
        protected ManualRefactoring refactoring;
        protected SyntaxTree treeAfter;
        protected SyntaxTree treeBefore;

        public void SetCallerBefore(MethodDeclarationSyntax callerBefore)
        {
            this.callerBefore = callerBefore;
        }

        public void SetCallerAfter(MethodDeclarationSyntax callerAfter)
        {
            this.callerAfter = callerAfter;
        }

        public void SetCalleeAfter(MethodDeclarationSyntax calleeAfter)
        {
            this.calleeAfter = calleeAfter;
        }

        public IEnumerable<ManualRefactoring> GetRefactorings()
        {
            yield return refactoring;
        }
       
        public void SetSyntaxTreeBefore(SyntaxTree treeBefore)
        {
            this.treeBefore = treeBefore;
        }

        public void SetSyntaxTreeAfter(SyntaxTree treeAfter)
        {
            this.treeAfter = treeAfter;
        }

        public abstract bool HasRefactoring();
    }


    internal class InMethodExtractMethodDetectorFactory
    {
        public static InMethodExtractMethodDetector GetInMethodExtractMethodDetectorByCommonStatements()
        {
            return new InMethodExtractMethodDetectorByCommonStatements();
        }

        public static InMethodExtractMethodDetector GetInMethodExtractMethodDetectorByStringDistances()
        {
            return new InMethodExtractMethodDectectorByStringDistances();
        }

        /* In method extract method detector that is based on the common statements count. */
        private class InMethodExtractMethodDetectorByCommonStatements : InMethodExtractMethodDetector
        {
            private readonly static int MAX_COMMON_STATEMENTS = 0;
            private readonly Logger logger;
            public InMethodExtractMethodDetectorByCommonStatements()
            {
                logger = NLoggerUtil.GetNLogger(typeof (InMethodExtractMethodDetectorByCommonStatements));
            }

            public override bool HasRefactoring()
            {
                refactoring = null;

                // Get the first invocation of the new method in the after-version of method.
                var invocation = ASTUtil.GetAllInvocationsInMethod(callerAfter, calleeAfter, treeAfter).First();

                // Get the statements in the method after and the new method.
                var statements1 = callerBefore.Body.Statements;
                var statements2 = calleeAfter.Body.Statements;

                // Get their longest common statements.
                var commons = RefactoringDetectionUtils.GetLongestCommonStatements(statements1, statements2, 
                    new SyntaxNodeExactComparer());
                
                logger.Info("Common statements count: " + commons.Count());
                // If the number of common statements is larger than the threshhold, a refactoring is detected.
                if (commons.Count() > MAX_COMMON_STATEMENTS)
                {
                    refactoring = ManualRefactoringFactory.CreateManualExtractMethodRefactoring(calleeAfter, invocation, 
                        commons.Select(p => p.Key));
                    return true;
                }
                return false;
            }
        }

        /* Extract method detector for a given caller and an added callee. */
        private class InMethodExtractMethodDectectorByStringDistances : InMethodExtractMethodDetector
        {
            private readonly Logger logger;

            public InMethodExtractMethodDectectorByStringDistances()
            {
                logger = NLoggerUtil.GetNLogger(typeof (InMethodExtractMethodDectectorByStringDistances));
            }

            public override bool HasRefactoring()
            {
                // Get the first invocation of callee in the caller method body.
                var invocation = ASTUtil.GetAllInvocationsInMethod(callerAfter, calleeAfter, treeAfter).First();

                /* Flatten the caller after by replacing callee invocation with the code in the calle method body. */
                String callerAfterFlattenned = ASTUtil.FlattenMethodInvocation(callerAfter, calleeAfter, invocation);

                var beforeWithoutSpace = callerBefore.GetFullText().Replace(" ", "");

                // The distance between flattened caller after and the caller before.
                int dis1 = StringUtil.GetStringDistance(callerAfterFlattenned.Replace(" ", ""), beforeWithoutSpace);

                // The distance between caller after and the caller before.
                int dis2 = StringUtil.GetStringDistance(callerAfter.GetFullText().Replace(" ", ""), beforeWithoutSpace);
                logger.Info("Distance Gain by Flattening:" + (dis2 - dis1));

                // Check whether the distance is shortened by flatten. 
                if (dis2 > dis1)
                {
                    // If similar enough, a manual refactoring instance is likely to be detected and created.
                    var analyzer = RefactoringAnalyzerFactory.CreateManualExtractMethodAnalyzer();
                    analyzer.SetMethodDeclarationBeforeExtracting(callerBefore);
                    analyzer.SetExtractedMethodDeclaration(calleeAfter);
                    analyzer.SetInvocationExpression(invocation);

                    // If the analyzer can get a refactoring from the given information, get the refactoring 
                    // and return true.
                    if (analyzer.CanGetRefactoring())
                    {
                        refactoring = analyzer.GetRefactoring();
                        return true;
                    }
                }
                return false;
            }
        }

    }
}
