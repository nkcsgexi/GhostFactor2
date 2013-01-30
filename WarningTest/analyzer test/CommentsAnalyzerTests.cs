using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.CSharp;
using warnings.analyzer;
using warnings.retriever;
using warnings.util;

namespace WarningTest.analyzer_test
{
    [TestClass]
    public class CommentsAnalyzerTests
    {
        private readonly SyntaxNode root;
        private readonly String path = TestUtil.GetFakeSourceFolder() + "CommentsAnalyzerSource.cs";
        private readonly String code;
        private readonly ICommentsRetriever retriever;
        private readonly Logger logger;

        public CommentsAnalyzerTests()
        {
            this.code = FileUtil.ReadAllText(path);
            this.root = ASTUtil.GetSyntaxTreeFromSource(code).GetRoot();
            this.retriever = RetrieverFactory.GetCommentsRetriever();
            this.logger = NLoggerUtil.GetNLogger(typeof (CommentsAnalyzerTests));
        }

        private string DumpNode(SyntaxNode node)
        {
            var nodeAnalyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
            nodeAnalyzer.SetSyntaxNode(node);
            return nodeAnalyzer.DumpTree();
        }

        /// <summary>
        /// Print all the tokens.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string PrintAllTokens(SyntaxNode node)
        {
            var tokens = node.DescendantTokens();
            var sb = new StringBuilder();
            foreach (var token in tokens)
            {
                sb.AppendLine(token.Kind.ToString()+ " " + token.GetText());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Print all the trivias.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string PrintAllTrivia(SyntaxNode node)
        {
            var trivias = node.DescendantTrivia();
            var sb = new StringBuilder();
            foreach (var trivia in trivias)
            {
                sb.AppendLine(trivia.Kind.ToString() + " " + trivia.GetText());
            }
            return sb.ToString();
        }

        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(code);
            Assert.IsNotNull(path);
            Assert.IsNotNull(root);
        }

        [TestMethod]
        public void TestMethod2()
        {
            retriever.SetSyntaxNode(root);
            var comments = retriever.GetComments();
            Assert.IsNotNull(comments);
            Assert.IsTrue(comments.Any());
            Assert.IsTrue(comments.Count() == 5);
            Assert.IsTrue(retriever.GetDocumentComments().Count() == 1);
            Assert.IsTrue(retriever.GetNonDocumentComments().Count() == 4);
        }
    }
}
