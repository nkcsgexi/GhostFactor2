using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace warnings.util
{
    public class SyntaxNodePair
    {
        public SyntaxNodePair(BlockSyntax NodeBefore, BlockSyntax NodeAfter)
        {
            this.NodeBefore = NodeBefore;
            this.NodeAfter = NodeAfter;
        }

        public SyntaxNode NodeBefore { private set; get; }
        public SyntaxNode NodeAfter { private set; get; }
    }
}
