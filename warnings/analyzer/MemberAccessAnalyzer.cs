using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace warnings.analyzer
{
    /* Analyzer for an access member expression, either property, field, or method of an non-primitive object.*/
    public interface IMemberAccessAnalyzer
    {
        void SetMemberAccess(SyntaxNode node);

        /* Get the part before ".", e.g. "A" of "A.B". */
        SyntaxNode GetLeftPart();

        /* Get the part after ".", e.g. "B" of "A.B". */
        SyntaxNode GetRightPart();
    }

    internal class MemberAccessAnalyzer : IMemberAccessAnalyzer
    {
        private MemberAccessExpressionSyntax access;

        public void SetMemberAccess(SyntaxNode node)
        {
            this.access = (MemberAccessExpressionSyntax) node;
        }

        public SyntaxNode GetLeftPart()
        {
            // Get all the decendent nodes lying on the left side of the '.' operation.
            var nodesLeft = access.DescendantNodes().Where(n => n.Span.End <= access.OperatorToken.Span.Start);

            // Get the longest subnode, and it is the complete left part.
            var longest = nodesLeft.OrderBy(n => n.Span.Length).Last();
            return longest;
        }

        public SyntaxNode GetRightPart()
        {
            // Get all decendent nodes on the right side of the '.' operator.
            var nodesRight = access.DescendantNodes().Where(n => n.Span.Start >= access.OperatorToken.Span.End);

            // Among all these nodes, the longest one is the entire right side of the access expression.
            var longest = nodesRight.OrderBy(n => n.Span.Length).Last();
            return longest;
        }
    }
}
