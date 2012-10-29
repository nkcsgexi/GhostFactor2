using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace warnings.util.Cache
{
    /* This type is used to cache a bunch of syntax nodes. */
    public class SyntaxNodesCachable : ICacheable
    {
        private readonly IEnumerable<SyntaxNode> nodes;

        public SyntaxNodesCachable(IEnumerable<SyntaxNode> nodes)
        {
            this.nodes = nodes;
        }

        public IEnumerable<SyntaxNode> GetNodes()
        {
            return nodes;
        }

        public int BytesUsed
        {
            get { return nodes.Count(); }
        }
    }
}
