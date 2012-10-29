using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class SyntaxNodesAnalyzerTests
    {
        private readonly IDocument document;

        private readonly ISyntaxNodeAnalyzer nodeAnalyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();

        private readonly ISyntaxNodesAnalyzer nodesAnalyzer = AnalyzerFactory.GetSyntaxNodesAnalyzer();

        private readonly IMethodDeclarationAnalyzer _methodDeclarationAnalyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();

        private readonly IDocumentAnalyzer documentAnalyzer = AnalyzerFactory.GetDocumentAnalyzer();

        private readonly IEnumerable<SyntaxNode> methods;

        private readonly Logger logger;

        private readonly int METHOD_COUNT = 5;

        public SyntaxNodesAnalyzerTests()
        {
            var code = TestUtil.GetFakeSourceFolder() + "SyntaxNodesAnalyzerExamples.cs";
            var converter = new String2IDocumentConverter();
            document = (IDocument) converter.Convert(FileUtil.ReadAllText(code), null, null, null);
            logger = NLoggerUtil.GetNLogger(typeof (MethodAnalyzerTests));

            documentAnalyzer.SetDocument(document);
            var namespaceDec = documentAnalyzer.GetNamespaceDecalarations().First();
            var classDec = documentAnalyzer.GetClassDeclarations((NamespaceDeclarationSyntax)namespaceDec).First();
            methods = documentAnalyzer.GetMethodDeclarations((ClassDeclarationSyntax)classDec);
        }

        private MethodDeclarationSyntax getMethod(int index)
        {
            Assert.IsTrue(index < METHOD_COUNT);
            return (MethodDeclarationSyntax) methods.ElementAt(index);
        }

        private IEnumerable<SyntaxNode> GetStatementsInMethod(int index)
        {
            _methodDeclarationAnalyzer.SetMethodDeclaration(getMethod(index));
            return _methodDeclarationAnalyzer.GetStatements();
        } 

        private IEnumerable<SyntaxNode> SelectSubSet(IEnumerable<SyntaxNode> nodes, int[] indexes)
        {
            var selected = new List<SyntaxNode>();
            foreach (int index in indexes)
            {
                selected.Add(nodes.ElementAt(index));
            }
            return selected.AsEnumerable();
        }
        [TestMethod]
        public void TestMethod1()
        {
            var nodes = GetStatementsInMethod(0);
            nodesAnalyzer.SetSyntaxNodes(nodes);
            Assert.IsTrue(nodesAnalyzer.GetLongestNode().GetText().Equals("var list = new List<int>();"));
            
            for (int i = 0; i < nodes.Count() - 1; i++)
            {
                nodeAnalyzer.SetSyntaxNode(nodes.ElementAt(i));
                Assert.IsTrue(nodeAnalyzer.IsNeighborredWith(nodes.ElementAt(i + 1)));
            }

            for (int i = 0; i < nodes.Count() - 2; i++)
            {
                nodeAnalyzer.SetSyntaxNode(nodes.ElementAt(i));
                Assert.IsFalse(nodeAnalyzer.IsNeighborredWith(nodes.ElementAt(i + 2)));
            }

            for (int i = 0; i < nodes.Count() - 3; i++)
            {
                nodeAnalyzer.SetSyntaxNode(nodes.ElementAt(i));
                Assert.IsFalse(nodeAnalyzer.IsNeighborredWith(nodes.ElementAt(i + 3)));
            }
        }
        [TestMethod]
        public void TestMethod2()
        {
            var nodes = GetStatementsInMethod(0);
            nodesAnalyzer.SetSyntaxNodes(SelectSubSet(nodes, new int[] {0, 1, 3, 4, 5, 7, 8, 9, 10}));
            var groups = nodesAnalyzer.GetNeighborredNodesGroups();
            var longest = nodesAnalyzer.GetLongestNeighborredNodesGroup();
            Assert.IsTrue(groups.Count() == 3);

            Assert.IsTrue(longest.Count() == 4);
            for (int i = 0; i < 4; i ++ )
                Assert.IsTrue(longest.ElementAt(i).GetText().Equals(nodes.ElementAt(i + 7).GetText()));
        }
    }
}
