using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using warnings.util;

namespace warnings.analyzer
{
    public interface IBlocksAnalyzer
    {
        void SetBlockBefore(SyntaxNode blockBefore);
        void SetBlockAfter(SyntaxNode blockAfter);
        IEnumerable<SyntaxNodePair> GetChangedBlocks();
    }

    internal class BlockAnalyzer : IBlocksAnalyzer
    {
        private readonly IIfStatementAnalyzer ifAnalyzer = 
            AnalyzerFactory.GetIfStatementAnalyzer();
        private BlockSyntax blockBefore;
        private BlockSyntax blockAfter;

        public void SetBlockBefore(SyntaxNode blockBefore)
        {
            this.blockBefore = (BlockSyntax) blockBefore;
        }

        public void SetBlockAfter(SyntaxNode blockAfter)
        {
            this.blockAfter = (BlockSyntax) blockAfter;
        }

        public IEnumerable<SyntaxNodePair> GetChangedBlocks()
        {
            return GetChangedSubBlocks(blockBefore, blockAfter);
        }

        private IEnumerable<SyntaxNodePair> GetChangedSubBlocks(BlockSyntax before, BlockSyntax after)
        {
            if(!before.EquivalentTo(after))
            {
                if (AreStatementsCountSame(before, after))
                {
                    var subBlocksBefore = GetChildBlocks(before);
                    var subBlocksAfter = GetChildBlocks(after);
                    if (subBlocksBefore.Count() == subBlocksAfter.Count())
                    {
                        var pairs = new List<SyntaxNodePair>();
                        for (int i = 0; i < subBlocksBefore.Count(); i++)
                        {
                            var subBlockB = subBlocksBefore.ElementAt(i);
                            var subBlockA = subBlocksAfter.ElementAt(i);
                            pairs.AddRange(GetChangedSubBlocks(subBlockB, subBlockA));
                        }
                        return pairs;
                    }
                }
                return new[] {new SyntaxNodePair(before, after)};
            }
            return new SyntaxNodePair[]{};
        }


        private IEnumerable<BlockSyntax> GetChildBlocks(BlockSyntax block)
        {
            return block.Statements.SelectMany(GetDirectBlockOfStatement);
        }

        /// <summary>
        ///  Get the direct block contained in a statement, for any if statement, the direct blocks may be two,
        /// on is from if block and the other is else block.
        /// </summary>
        /// <param name="statement"></param>
        /// <returns></returns>
        private IEnumerable<BlockSyntax> GetDirectBlockOfStatement(StatementSyntax statement)
        {
            if (statement.Kind == SyntaxKind.IfStatement)
            {
                ifAnalyzer.SetIfStatement(statement);
                return ifAnalyzer.GetDirectBlocks().Select(b => (BlockSyntax)b);
            }
            return statement.ChildNodes().Where(c => c.Kind == SyntaxKind.Block).
                Select(b => (BlockSyntax)b);        
        }

        private bool AreStatementsCountSame(BlockSyntax before, BlockSyntax after)
        {
            return before.Statements.Count == after.Statements.Count;
        }



    }
}
