using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace warnings.refactoring.detection
{
    internal class RefactoringDetectionUtils
    {
        /// <summary>
        /// Get the common node pairs in before and after set of nodes.
        /// </summary>
        /// <param name="beforeNodes"></param>
        /// <param name="afterNodes"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetCommonNodePairs
                (IEnumerable<SyntaxNode> beforeNodes, IEnumerable<SyntaxNode> afterNodes, 
                    IComparer<SyntaxNode> comparer)
        {
            var result = new List<KeyValuePair<SyntaxNode, SyntaxNode>>();
            foreach (var before in beforeNodes)
            {
                foreach (var after in afterNodes)
                {
                    if (comparer.Compare(before, after) == 0)
                    {
                        result.Add(new KeyValuePair<SyntaxNode, SyntaxNode>(before, after));
                        break;
                    }
                }
            }
            return result;
        }

        /* Get the longest common statements list between the given two statements list. */
        public static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetLongestCommonStatements
            (IEnumerable<SyntaxNode> statements1, 
            IEnumerable<SyntaxNode> statements2, IComparer<SyntaxNode> comparer)
        {
            var results = new List<KeyValuePair<SyntaxNode, SyntaxNode>>();
            var matrix = new int[statements1.Count(), statements2.Count()];
            int lcs = -1;
            int end1 = -1;
            int end2 = -1;

            for (int i = 0; i < statements1.Count(); i++)
            {
                for (int j = 0; j < statements2.Count(); j++)
                {
                    if (comparer.Compare(statements1.ElementAt(i), statements2.ElementAt(j)) == 0)
                    {
                        if (i == 0 || j == 0)
                        {
                            matrix[i, j] = 1;
                        }
                        else
                            matrix[i, j] = matrix[i - 1, j - 1] + 1;
                        if (matrix[i, j] > lcs)
                        {
                            lcs = matrix[i, j];
                            end1 = i;
                            end2 = j;
                        }

                    }
                    else
                        matrix[i, j] = 0;
                }
            }

            int start1 = end1 - lcs + 1;
            int start2 = end2 - lcs + 1;

            for(int i = 0; i < lcs; i ++)
            {
                var s1 = statements1.ElementAt(i + start1);
                var s2 = statements2.ElementAt(i + start2);
                results.Add(new KeyValuePair<SyntaxNode, SyntaxNode>(s1, s2));
            }

            return results;
        }

       /// <summary>
        /// Convert a string to an instance of document.
       /// </summary>
       /// <param name="code"></param>
       /// <returns></returns>
        public static IDocument Convert2IDocument(string code)
        {
            var converter = new String2IDocumentConverter();
            return (IDocument) converter.Convert(code, null, null, null);
        }


        /// <summary>
        /// Get the contained blocks whose statements are changed (added, removed)
        /// </summary>
        /// <param name="blockBefore"></param>
        /// <param name="blockAfter"></param>
        /// <returns></returns>
        public static IEnumerable<SyntaxNodePair> GetChangedBlocks(SyntaxNode blockBefore, SyntaxNode 
            blockAfter)
        {
            var analyzer = AnalyzerFactory.GetBlockAnalyzer();
            analyzer.SetBlockBefore(blockBefore);
            analyzer.SetBlockAfter(blockAfter);
            return analyzer.GetChangedBlocks();
        }

        /// <summary>
        /// Get all the statements in a method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static IEnumerable<SyntaxNode> GetMethodStatements(SyntaxNode method)
        {
            var methodAnalyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
            methodAnalyzer.SetMethodDeclaration(method);
            return methodAnalyzer.GetStatements();
        }
    }
}
