using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using warnings.retriever;
using warnings.util;

namespace SourceCommentAnalyzer
{
    public class CommentStrategy : IInterestedContentExtractingStrategy
    {
        public IEnumerable<string> GetInterestedContent(string content)
        {
            return GetComments(content).Where(IsCommentQuestion).ToList();
        }

        public bool HasInterestedContent(IEnumerable<string> interestedContent)
        {
            return interestedContent != null && interestedContent.Any();
        }

        public string DumpInformation(IEnumerable<string> interestedContent)
        {
            return StringUtil.ConcatenateAll(Environment.NewLine, interestedContent);
        }

        /// <summary>
        /// Whether a certain comment is a question.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        private static bool IsCommentQuestion(string comment)
        {
            return comment.Contains("?") || comment.Contains(". Where ") ||
                comment.Contains(". What ") || comment.Contains(". When ") ||
                    comment.Contains(". Why ") || comment.Contains(". Who ") ||
                        comment.Contains(". How ");
        }

        private static IEnumerable<string> GetComments(string code)
        {
            var commentsRetriever = RetrieverFactory.GetCommentsRetriever(); ;
            var root = ASTUtil.GetSyntaxTreeFromSource(code).GetRoot();
            commentsRetriever.SetSyntaxNode(root);
            var comments = commentsRetriever.GetNonDocumentComments();
            return comments.Select(c => c.GetText());
        }
    }
}
