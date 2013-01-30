using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace warnings.retriever
{
    /// <summary>
    /// Interface for the comment _retriever.
    /// </summary>
    public interface ICommentsRetriever
    {
        void SetSyntaxNode(SyntaxNode node);
        IEnumerable<SyntaxTrivia> GetComments();

        /// <summary>
        /// Get those comments that go to the automatically generated documents, such as this comment.
        /// </summary>
        /// <returns></returns>
        IEnumerable<SyntaxTrivia> GetDocumentComments();

        /// <summary>
        /// Get those comments that do not go to the auto-generated documents, like the in-line comments in
        /// a method body. 
        /// </summary>
        /// <returns></returns>
        IEnumerable<SyntaxTrivia> GetNonDocumentComments();
    }

    internal class CommentsRetriever : ICommentsRetriever
    {
        private SyntaxNode node;

        public void SetSyntaxNode(SyntaxNode node)
        {
            this.node = node;
        }

        public IEnumerable<SyntaxTrivia> GetComments()
        {
            // Get all the trivias that are of the type comment.
            return GetDocumentComments().Concat(GetNonDocumentComments());
        }

        public IEnumerable<SyntaxTrivia> GetDocumentComments()
        {
            return node.DescendantTrivia().Where(n => n.Kind == SyntaxKind.DocumentationComment);
        }

        public IEnumerable<SyntaxTrivia> GetNonDocumentComments()
        { 
            return node.DescendantTrivia().Where(n => n.Kind == SyntaxKind.SingleLineCommentTrivia ||
                    n.Kind == SyntaxKind.MultiLineCommentTrivia);
        }
    }
}
