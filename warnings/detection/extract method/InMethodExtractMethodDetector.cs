using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
    internal abstract class InMethodExtractMethodDetector :  IRefactoringDetector,
        IBeforeAndAfterDocumentKeeper
    {
        protected MethodDeclarationSyntax callerAfter;
        protected MethodDeclarationSyntax callerBefore;
        protected MethodDeclarationSyntax calleeAfter;

        protected IDocument documentBefore;
        protected IDocument documentAfter;

        protected ManualRefactoring refactoring;

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

        public void SetDocumentBefore(IDocument documentBefore)
        {
            this.documentBefore = documentBefore;
        }

        public void SetDocumentAfter(IDocument documentAfter)
        {
            this.documentAfter = documentAfter;
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

        public static InMethodExtractMethodDetector GetInMethodExtractMethodDetectorWithoutInvocation()
        {
            return new InMethodExtractMethodDetectorWithoutInvocation();
        }

        private class InMethodExtractMethodDetectorWithoutInvocation : InMethodExtractMethodDetector
        {
            private const double THRESHHOLD = 0.5;
            private static Logger Logger =NLoggerUtil.GetNLogger(typeof
                (InMethodExtractMethodDetectorWithoutInvocation));


            public override bool HasRefactoring()
            {
                refactoring = null;
                if (IsMethodWithStatements(callerBefore) && IsMethodWithStatements(calleeAfter))
                {
                    // Get all the blocks in the caller before, including the method body.
                    var blocks = callerBefore.Body.DescendantNodesAndSelf().OfType<BlockSyntax>().ToList();
                    if (blocks.Any())
                    {
                        var longestCommonStatements = GetLongestCommonStatements(blocks);
                        refactoring = TryGetRefactoring(longestCommonStatements);
                    }
                }
                return refactoring != null;
            }

            private ManualRefactoring TryGetRefactoring(IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> 
                longestCommonStatements)
            {
                // If the longest common statements are found and the number of common statements exceeds 
                // the threshhold.
                if (AreMostStatementsCommon(longestCommonStatements, calleeAfter.Body.Statements))
                {
                    return ManualRefactoringFactory.CreateManualExtractMethodRefactoring
                        (documentBefore, documentAfter, calleeAfter, null, longestCommonStatements.Select
                            (p => p.Key));
                }
                return null;
            }

            private bool AreMostStatementsCommon(IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> 
                commonStatements, SyntaxList<StatementSyntax> statements)
            {
                return (float)commonStatements.Count()/statements.Count() > THRESHHOLD;
            }

            private IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetLongestCommonStatements
                (IEnumerable<BlockSyntax> blocks)
            {
                // Find the longest common statements by comparing each of these blocks and the body of the 
                // newly added method declaration.
                int maxCommon = 0;
                var comparer = new SyntaxNodeExactComparer();
                var longestCommonStatements = Enumerable.Empty<KeyValuePair<SyntaxNode, SyntaxNode>>();
                foreach (var block in blocks)
                {
                    var pairs = RefactoringDetectionUtils.GetLongestCommonStatements(block.Statements,
                        calleeAfter.Body.Statements, comparer);
                    if (pairs.Count() > maxCommon)
                    {
                        maxCommon = pairs.Count();
                        longestCommonStatements = pairs.ToList();
                    }                   
                }
                return longestCommonStatements;
            }

            private bool IsMethodWithStatements(SyntaxNode m)
            {
                var method = (MethodDeclarationSyntax) m;
                return method.Body != null && method.Body.Statements != null;
            }
        }

        /// <summary>
        /// In method extract method detector that is based on the common statements count. 
        /// </summary>
        private class InMethodExtractMethodDetectorByCommonStatements : InMethodExtractMethodDetector
        {
            private readonly static int MAX_COMMON_STATEMENTS = 0;
            private readonly Logger logger;

            internal InMethodExtractMethodDetectorByCommonStatements()
            {
                logger = NLoggerUtil.GetNLogger(typeof 
                    (InMethodExtractMethodDetectorByCommonStatements));
            }

            public override bool HasRefactoring()
            {
                refactoring = null;

                // Get all the invocations of the added method in the body of common method after.
                var invocations = ASTUtil.GetAllInvocationsInMethod(callerAfter, calleeAfter, 
                    (SyntaxTree) documentAfter.GetSyntaxTree());

                if (invocations.Any())
                {
                    // Get the first invocation.
                    var invocation = invocations.First();
                    var changedBlockPairs = RefactoringDetectionUtils.GetChangedBlocks(callerBefore.Body,
                        callerAfter.Body);
                    LogChangedBlocks(changedBlockPairs);

                    if (changedBlockPairs.Count() == 1)
                    {
                        // Get the statements in the method after and the new method.
                        var statements1 = ((BlockSyntax) changedBlockPairs.First().NodeBefore).Statements;
                        var statements2 = calleeAfter.Body.Statements;

                        // Get their longest common statements.
                        var commons = RefactoringDetectionUtils.GetLongestCommonStatements(statements1,
                            statements2, new SyntaxNodeExactComparer());

                        logger.Info("Common statements count: " + commons.Count());

                        // If the number of common statements is larger than the threshhold, a refactoring 
                        // is detected.
                        if (commons.Count() > MAX_COMMON_STATEMENTS)
                        {
                            refactoring = ManualRefactoringFactory.CreateManualExtractMethodRefactoring
                                (documentBefore, documentAfter, calleeAfter, invocation,
                                 commons.Select(p => p.Key));
                            return true;
                        }
                    }
                }
                return false;
            }

            private void LogChangedBlocks(IEnumerable<SyntaxNodePair> pairs)
            {
                logger.Debug("Changed blocks count: " + pairs.Count());
                foreach (var pair in pairs)
                {
                    logger.Debug("Block before:\n" + pair.NodeBefore.GetText());
                    logger.Debug("Block after:\n" + pair.NodeAfter.GetText());
                }
            }
        }

        /// <summary>
        /// Extract method detector for a given caller and an added callee.
        /// </summary>
        private class InMethodExtractMethodDectectorByStringDistances : InMethodExtractMethodDetector
        {
            private readonly Logger logger;

            public InMethodExtractMethodDectectorByStringDistances()
            {
                logger = NLoggerUtil.GetNLogger(typeof 
                    (InMethodExtractMethodDectectorByStringDistances));
            }

            public override bool HasRefactoring()
            {
                // Get all the invocations of the added method in the after method.
                var invocations = ASTUtil.GetAllInvocationsInMethod(callerAfter, calleeAfter,
                    (SyntaxTree) documentAfter.GetSyntaxTree());

                if (invocations.Any())
                {
                    // Only consider the first invocation among them.
                    var invocation = invocations.First();

                    //Flatten the caller after by replacing callee invocation with the code in the calle 
                    //method body.                 
                    String callerAfterFlattenned = ASTUtil.FlattenMethodInvocation(callerAfter,
                                                                                   calleeAfter, invocation);

                    var beforeWithoutSpace = callerBefore.GetFullText().Replace(" ", "");

                    // The distance between flattened caller after and the caller before.
                    int dis1 = StringUtil.GetStringDistance(callerAfterFlattenned.Replace(" ", ""),
                                                            beforeWithoutSpace);

                    // The distance between caller after and the caller before.
                    int dis2 = StringUtil.GetStringDistance(callerAfter.GetFullText().Replace(" ", ""),
                                                            beforeWithoutSpace);
                    logger.Info("Distance Gain by Flattening:" + (dis2 - dis1));

                    // Check whether the distance is shortened by flatten. 
                    if (dis2 > dis1)
                    {
                        // If similar enough, a manual refactoring instance is likely to be detected 
                        // and created.
                        var analyzer = RefactoringAnalyzerFactory.CreateManualExtractMethodAnalyzer();
                        analyzer.SetDocumentBefore(documentBefore);
                        analyzer.SetDocumentAfter(documentAfter);
                        analyzer.SetMethodDeclarationBeforeExtracting(callerBefore);
                        analyzer.SetExtractedMethodDeclaration(calleeAfter);
                        analyzer.SetInvocationExpression(invocation);

                        // If the analyzer can get a refactoring from the given information, 
                        // get the refactoring and return true.
                        if (analyzer.CanGetRefactoring())
                        {
                            refactoring = analyzer.GetRefactoring();
                            return true;
                        }
                    }
                }
                return false;
            }
        }

    }
}
