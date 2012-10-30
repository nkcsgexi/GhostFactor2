using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.util;

namespace warnings.analyzer
{

    /* Analyzer for any syntax node. */
    public interface ISyntaxNodeAnalyzer
    {
        void SetSyntaxNode(SyntaxNode node);

        /* Are the nodes neighbors, means no code in between. */
        bool IsNeighborredWith(SyntaxNode another);

        /* Get the first ancestor of the node that met with the given condition. */
        SyntaxNode GetClosestAncestor(Predicate<SyntaxNode> condition);

        /* Get the common parent of these two nodes. */
        SyntaxNode GetCommonParent(SyntaxNode another);

        /* Map this node to a new document whose text is equal to the node's tree.*/
        SyntaxNode MapToAnotherDocument(IDocument target);

        /* Get a tree-like structure depicting all the decendent nodes and itself. */
        string DumpTree();
    }

    internal class SyntaxNodeAnalyzer : ISyntaxNodeAnalyzer
    {
        private static int ANALYZER_COUNT = 0;

        public static int GetCount()
        {
            return ANALYZER_COUNT;
        }

        private SyntaxNode node;

        private readonly Logger logger;

        internal SyntaxNodeAnalyzer()
        {
            Interlocked.Increment(ref ANALYZER_COUNT);
            logger = NLoggerUtil.GetNLogger(typeof (SyntaxNodeAnalyzer));
        }

        ~SyntaxNodeAnalyzer()
        {
            Interlocked.Decrement(ref ANALYZER_COUNT);
        }

        public void SetSyntaxNode(SyntaxNode node)
        {
            this.node = node;
        }

        public bool IsNeighborredWith(SyntaxNode another)
        {
            // Get the nearest common acncestor.
            var parent = GetCommonParent(another);

            // If the ancestor has decendent whose span is between node and another node, then they are not neighborored,
            // otherwise they are neighbors.
            return !parent.DescendantNodes().Any(
                // n should between node and another node.
                n => n.Span.CompareTo(node.Span) * n.Span.CompareTo(another.Span) < 0
                // and n should not overlap with node and another node. 
                && !n.Span.OverlapsWith(node.Span)
                && !n.Span.OverlapsWith(another.Span));
        }

        public SyntaxNode GetClosestAncestor(Predicate<SyntaxNode> condition)
        {
            SyntaxNode ancestor;

            // Iteratively get parent node until the node met with the given condition.
            for (ancestor = node; ancestor != null && !condition.Invoke(ancestor); ancestor = ancestor.Parent) ;
            return ancestor;
        }

        public SyntaxNode GetCommonParent(SyntaxNode another)
        {
            // Get list of common ancestors.
            var commonAncestors = node.Ancestors().Where(a => a.Span.Contains(another.Span));
            
            // Sort the list by span length.
            var sortedCommonAncestors = commonAncestors.OrderBy(n => n.Span.Length);
            
            // The ancestor of the least length is the nearest common ancestor.
            return sortedCommonAncestors.First();
        }

        public SyntaxNode MapToAnotherDocument(IDocument target)
        {
            // Do not need to parse into the node that does not include the node.
            return (SyntaxNode)target.GetSyntaxRoot().DescendantNodes(n => n.Span.Contains(node.Span)).
                First(n => n.Span.Equals(node.Span));
        }

        public string DumpTree()
        {
            return Environment.NewLine + PrintPretty(node, "", true);
        }


        /* algorithm copied from http://stackoverflow.com/questions/1649027/how-do-i-print-out-a-tree-structure . */
        private string PrintPretty(SyntaxNode node,string indent, bool last)
        {
            var sb = new StringBuilder();
            sb.Append(indent);
            if (last)
            {
                sb.Append("\\-");
                indent += "\t";
            }
            else
            {
                sb.Append("|-");
                indent += "|\t";
            }
            sb.AppendLine(node.Kind.ToString() + ":" + StringUtil.ReplaceNewLine(node.GetText(), ""));

            for (int i = 0; i < node.ChildNodes().Count() ; i++)
            {
                var child = node.ChildNodes().ElementAt(i);
                sb.Append(PrintPretty(child, indent, i == node.ChildNodes().Count() - 1));
            }

            return sb.ToString();
        }
    }

}
