using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using warnings.util;

namespace warnings.analyzer.comparators
{
    /* Compare whether two method declarations are of same qualified name.*/
    public class MethodsComparator : IComparer<SyntaxNode> 
    {
        public int Compare(SyntaxNode x, SyntaxNode y)
        {
            var methodAnalyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
            methodAnalyzer.SetMethodDeclaration(x);
            var qualifiedName1 = methodAnalyzer.GetQualifiedName();
            methodAnalyzer.SetMethodDeclaration(y);
            var qualifiedName2 = methodAnalyzer.GetQualifiedName();
            return qualifiedName1.CompareTo(qualifiedName2);
        }
    }

    /* Comparing two enumerables of strings, if they contained same things*/
    public class StringEnumerablesComparator : IComparer<IEnumerable<string>>
    {
        public int Compare(IEnumerable<string> x, IEnumerable<string> y)
        {
            var set1 = x.OrderBy(s => s);
            var set2 = y.OrderBy(s => s);
            if(set1.Count() == set2.Count())
            {
                for(int i = 0; i< set1.Count(); i++)
                {
                    var s1 = set1.ElementAt(i);
                    var s2 = set2.ElementAt(i);
                    if(!s1.Equals(s2))
                    {
                        return -1;
                    }
                }
                return 0;
            }
            return -1;
        }
    }

    /* 
     * Comparer between two syntax node, if contains exact code then return 0, otherwise
     * return 1;
     */
    public class SyntaxNodeExactComparer : IComparer<SyntaxNode>
    {
        public int Compare(SyntaxNode n1, SyntaxNode n2)
        {
            var s1 = n1.GetText().Replace(" ", "");
            var s2 = n2.GetText().Replace(" ", "");
            if(s1.Equals(s2))
                return 0;
            return 1;
        }
    }
}
