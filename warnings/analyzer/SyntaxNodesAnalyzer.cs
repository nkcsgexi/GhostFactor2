using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.util;

namespace warnings.analyzer
{
    /* Analyzer for a bunch of syntax nodes together. */
    public interface ISyntaxNodesAnalyzer
    {
        void SetSyntaxNodes(IEnumerable<SyntaxNode> nodes);
        SyntaxNode GetLongestNode();
        IEnumerable<SyntaxNode> RemoveSubNodes();
        IEnumerable<IEnumerable<SyntaxNode>> GetNeighborredNodesGroups();
        IEnumerable<SyntaxNode> GetLongestNeighborredNodesGroup();
        IEnumerable<SyntaxNode> MapToAnotherDocument(IDocument document); 
        int GetStartPosition();
        int GetEndPosition();
    }
    internal class SyntaxNodesAnalyzer : ISyntaxNodesAnalyzer
    {
        private static int ANALYZER_COUNT = 0;

        public static int GetCount()
        {
            return ANALYZER_COUNT;
        }

        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof (ISyntaxNodesAnalyzer));
        private IEnumerable<SyntaxNode> nodes;
        
        internal SyntaxNodesAnalyzer()
        {
            Interlocked.Increment(ref ANALYZER_COUNT);
        }

        ~SyntaxNodesAnalyzer()
        {
            Interlocked.Decrement(ref ANALYZER_COUNT);
        }    

        public void SetSyntaxNodes(IEnumerable<SyntaxNode> nodes)
        {
            this.nodes = nodes;
        }

        /* Get the longest node among all the given nodes. */
        public SyntaxNode GetLongestNode()
        {
            int longestSpan = int.MinValue;
            SyntaxNode longestNode = null;
            foreach (var node in nodes)
            {
                if(node.Span.Length > longestSpan)
                {
                    longestSpan = node.Span.Length;
                    longestNode = node;
                }
            }
            return longestNode;
        }

        /* 
         * Get groups of nodes. In each group, nodes are sequentially ordered and no code in 
         * between of each two nearby nodes. 
         */
        public IEnumerable<IEnumerable<SyntaxNode>> GetNeighborredNodesGroups()
        {
            // Remove all the nodes contained by other node in the list, also sort the list by starting position
            var sortedNodes = RemoveSubNodes().OrderBy(n => n.Span.Start);
            var breakIndexes = new List<int>();
            var analyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
            logger.Debug("Sourted node count: " + sortedNodes.Count());

            // Iterate all the nodes, if one is not neighborred with its previous one, add to the break indexes.
            for (int i = 1; i < sortedNodes.Count(); i++)
            {
                analyzer.SetSyntaxNode(nodes.ElementAt(i));
                if (!analyzer.IsNeighborredWith(sortedNodes.ElementAt(i - 1)))
                {
                    breakIndexes.Add(i);
                }
            }

            // The number of elements shall be the last break index.
            breakIndexes.Add(nodes.Count());

            var lists = new List<IEnumerable<SyntaxNode>>();
            int start = 0;

            // Iterate each break index, from 'last break index' to 'current break index -1' 
            // is a group of neiborred syntax node.
            foreach (var end in breakIndexes)
            {
                lists.Add(sortedNodes.Skip(start).Take(end - start));
                start = end;
            }
            return lists.AsEnumerable();
        }

        public IEnumerable<SyntaxNode> GetLongestNeighborredNodesGroup()
        {
            // Get all the neighborred nodes groups.
            var groups = GetNeighborredNodesGroups();
            
            IEnumerable<SyntaxNode> longestGroup = null;
            int longestSpan = int.MinValue;

            // Compute the length of each group and remember the largest one.
            foreach (var group in groups)
            {
                int groupLength = 0;
                foreach (var node in group)
                {
                    groupLength += node.Span.Length;
                }
                if(groupLength > longestSpan)
                {
                    longestSpan = groupLength;
                    longestGroup = group;
                }
            }
            return longestGroup;
        }

        /* Map all the nodes to another document. */
        public IEnumerable<SyntaxNode> MapToAnotherDocument(IDocument document)
        {
            var analyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
            var list = new List<SyntaxNode>();

            foreach (SyntaxNode node in nodes)
            {
                analyzer.SetSyntaxNode(node);
                list.Add(analyzer.MapToAnotherDocument(document));
            }
            return list.AsEnumerable();
        }


        /* The leftmost postion for all the nodes. */
        public int GetStartPosition()
        {
            int start = int.MaxValue;
            foreach (var syntaxNode in nodes)
            {
                if (syntaxNode.Span.Start < start)
                    start = syntaxNode.Span.Start;
            }
            return start;
        }

        /* The rightmost position for all the nodes. */
        public int GetEndPosition()
        {
            int end = int.MinValue;
            foreach (var node in nodes)
            {
                if (node.Span.End > end)
                    end = node.Span.End;
            }
            return end;
        }

        /* Given a list of syntax node, remove all the nodes that are contained by other node in the same list. */
        public IEnumerable<SyntaxNode> RemoveSubNodes()
        {
            // Create a list of contained nodes.
            var nodesContainedByOtherNode = new List<SyntaxNode>();

            // All the node in the list is likely to be parent.
            foreach (var parentNode in nodes)
            {
                logger.Debug("Parent Node: " + parentNode);       
                var containedByParentNode = nodes.Where(n => parentNode.DescendantNodes().Contains(n));
         
                // Add all the nodes that are contained in the range and not the same node with the parent.
                nodesContainedByOtherNode.AddRange(containedByParentNode);
            }

            logger.Debug("Sub nodes count: " + nodesContainedByOtherNode.Count);

            // Remove the contained nodes from the original list and return it.
            return nodes.Except(nodesContainedByOtherNode).AsEnumerable();
        }
    }
}
