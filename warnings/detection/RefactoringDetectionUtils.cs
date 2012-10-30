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
        /* A comparer between two class declarations, return 0 if they have the same identifier. */
        private class ClassNameComparer : IComparer<SyntaxNode>
        {
            public int Compare(SyntaxNode x, SyntaxNode y)
            {
                var classX = (ClassDeclarationSyntax) x;
                var classY = (ClassDeclarationSyntax)y;
                if(classX.Identifier.ValueText.Equals(classY.Identifier.ValueText))
                {
                    return 0;
                }
                return 1;
            }
        }

        /* A comparer between two method declarations, return 0 if they have the same method name. */
        private class MethodNameComparer : IComparer<SyntaxNode>
        {
            public int Compare(SyntaxNode x, SyntaxNode y)
            {
                var methodX = (MethodDeclarationSyntax)x;
                var methodY = (MethodDeclarationSyntax)y;
                if (methodX.Identifier.ValueText.Equals(methodY.Identifier.ValueText))
                {
                    return 0;
                }
                return 1;
            }
        }

        /* Compare the two nodes by the qualified name of the type enclosing these nodes. */
         private class NodeOutsideTypeComparer : IComparer<SyntaxNode>
         {
             public int Compare(SyntaxNode x, SyntaxNode y)
             {
                 var analyzer = AnalyzerFactory.GetQualifiedNameAnalyzer();
                 analyzer.SetSyntaxNode(x);
                 var nameX = analyzer.GetOutsideTypeQualifiedName();
                 analyzer.SetSyntaxNode(y);
                 var nameY = analyzer.GetOutsideTypeQualifiedName();
                 return nameX.Equals(nameY) ? 0 : 1;
             }
         }

        public static IComparer<SyntaxNode> GetClassDeclarationNameComparer()
        {
            return new ClassNameComparer();
        }

        public static IComparer<SyntaxNode> GetMethodDeclarationNameComparer()
        {
            return new MethodNameComparer();
        }

        public static IComparer<SyntaxNode> GetNodeOutSideTypeQualifiedNameComparer()
        {
            return new NodeOutsideTypeComparer();
        }

        /* Get the common node pairs in before and after set of nodes. */
        public static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetCommonNodePairs(IEnumerable<SyntaxNode> beforeNodes,
            IEnumerable<SyntaxNode> afterNodes, IComparer<SyntaxNode> comparer)
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
        public static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetLongestCommonStatements(IEnumerable<SyntaxNode> statements1, 
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

        /* Convert a string to an instance of document. */
        public static IDocument Convert2IDocument(string code)
        {
            var converter = new String2IDocumentConverter();
            return (IDocument) converter.Convert(code, null, null, null);
        }
    }
}
