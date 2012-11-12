using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using warnings.util;

namespace warnings.analyzer.comparators
{
 
    /// <summary>
    /// Compare
    /// </summary>
    public class ClassNameComparer : IComparer<SyntaxNode>
    {
        public int Compare(SyntaxNode x, SyntaxNode y)
        {
            var classX = x as ClassDeclarationSyntax;
            var classY = y as ClassDeclarationSyntax;
            if (classX != null && classY != null)
            {
                if (classX.Identifier.ValueText.Equals(classY.Identifier.ValueText))
                {
                    return 0;
                }
            }
            return 1;
        }
    }

    /* A comparer between two method declarations, return 0 if they have the same method name. */
    public class MethodNameComparer : IComparer<SyntaxNode>
    {
        public int Compare(SyntaxNode x, SyntaxNode y)
        {
            var methodX = x as MethodDeclarationSyntax;
            var methodY = y as MethodDeclarationSyntax;
            if (methodX != null && methodY != null)
            {
                if (methodX.Identifier.ValueText.Equals(methodY.Identifier.ValueText))
                {
                    return 0;
                }
            }
            return 1;
        }
    }

    /* Compare the two nodes by the qualified name of the type enclosing these nodes. */
    public class NodeOutsideTypeComparer : IComparer<SyntaxNode>
    {
        public int Compare(SyntaxNode x, SyntaxNode y)
        {
            if (x != null && y != null)
            {
                var analyzer = AnalyzerFactory.GetQualifiedNameAnalyzer();
                analyzer.SetSyntaxNode(x);
                var nameX = analyzer.GetOutsideTypeQualifiedName();
                analyzer.SetSyntaxNode(y);
                var nameY = analyzer.GetOutsideTypeQualifiedName();
                return nameX.Equals(nameY) ? 0 : 1;
            }
            return 1;
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
    /// <summary>
    /// 
    /// </summary>
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

    /// <summary>
    /// Given two sets of elements, compare the elements in both sets by using the specified
    /// equality comparer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SetsEqualityCompare<T> : IEqualityComparer<IEnumerable<T>>
    {
        private readonly IEqualityComparer<T> elementEqualityComparer;

        public SetsEqualityCompare(IEqualityComparer<T> elementEqualityComparer)
        {
            this.elementEqualityComparer = elementEqualityComparer;
        }


        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            var added = x.Except(y, elementEqualityComparer);
            var missing = y.Except(x, elementEqualityComparer);
            return !added.Any() && !missing.Any();
        }

        public int GetHashCode(IEnumerable<T> obj)
        {
            return 0;
        }
    }


    /// <summary>
    /// The string tuple equality comparer.
    /// </summary>
    public class StringTupleEqualityComparer : IEqualityComparer<Tuple<string, string>>
    {
        public bool Equals(Tuple<string, string> x, Tuple<string, string> y)
        {
            return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2);
        }

        public int GetHashCode(Tuple<string, string> obj)
        {
            return 1;
        }
    }

    /// <summary>
    /// The equality comparer for integer tuple.
    /// </summary>
    public class IntTupleEqualityComparer: IEqualityComparer<Tuple<int, int>>
    {
        public bool Equals(Tuple<int, int> x, Tuple<int, int> y)
        {
            return x.Item1 == y.Item1 && x.Item2 == y.Item2;
        }

        public int GetHashCode(Tuple<int, int> obj)
        {
            return 1;
        }
    }
}
