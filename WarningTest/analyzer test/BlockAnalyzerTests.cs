using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace WarningTest.analyzer_test
{
    [TestClass]
    public class BlockAnalyzerTests
    {
        private readonly IDocument beforeDocument;
        private readonly IDocument afterDocument;

        public BlockAnalyzerTests()
        {
            this.beforeDocument = TestUtil.GetDocumentForFakeSource("blockAnalyzerBefore.txt");
            this.afterDocument = TestUtil.GetDocumentForFakeSource("blockAnalyzerAfter.txt");
        }

        private SyntaxNode GetMethodBlock(IDocument document, string name)
        {
            var methods = ASTUtil.GetMethodsDeclarations((SyntaxNode) document.GetSyntaxRoot());
            var method = (MethodDeclarationSyntax)methods.First(m => ((MethodDeclarationSyntax) m).
                Identifier.ValueText.Equals(name));
            return method.Body;
        }


        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(beforeDocument);
            Assert.IsNotNull(afterDocument);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var block1 = GetMethodBlock(beforeDocument, "GetChangedSubBlocks");
            var block2 = GetMethodBlock(afterDocument, "GetChangedSubBlocks");
            Assert.IsNotNull(block1);
            Assert.IsNotNull(block2);
            var analyzer = AnalyzerFactory.GetBlockAnalyzer();
            analyzer.SetBlockBefore(block1);
            analyzer.SetBlockAfter(block2);
            var blocks = analyzer.GetChangedBlocks();
            Assert.IsTrue(blocks.Count() == 1);
            Assert.IsTrue(((BlockSyntax)blocks.First().NodeAfter).Statements.Count == 5);
            Assert.IsTrue(((BlockSyntax)blocks.First().NodeBefore).Statements.Count == 3);
        }
    }
}
