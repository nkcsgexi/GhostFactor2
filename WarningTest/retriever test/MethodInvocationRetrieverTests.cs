using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using WarningTest.analyzer_test;
using warnings.analyzer;
using warnings.retriever;
using warnings.util;

namespace WarningTest.retriever_test
{
    [TestClass]
    public class MethodInvocationRetrieverTests
    {
        private readonly Logger logger;
        private readonly ISolution solution;
        private readonly IDocument document1;
        private readonly IDocument document2;
        private readonly SyntaxNode declaration;

       

        public MethodInvocationRetrieverTests()
        {
            this.logger = NLoggerUtil.GetNLogger(typeof(SolutionAnalyzerTests));
            Assert.IsTrue(File.Exists(TestUtil.GetAnotherSolutionPath()));
            this.solution = Solution.Load(TestUtil.GetAnotherSolutionPath());
            Assert.IsNotNull(solution);
            var analyzer = AnalyzerFactory.GetSolutionAnalyzer();
            analyzer.SetSolution(solution);
            this.document1 = analyzer.GetDocuments(analyzer.GetProjects().First()).ElementAt(0);
            this.document2 = analyzer.GetDocuments(analyzer.GetProjects().First()).ElementAt(1);
            Assert.IsNotNull(document1);
            Assert.IsNotNull(document2);
        }

        private SyntaxNode GetMethodDeclaration(IDocument document, string methodName)
        {
            var root = (SyntaxNode) document.GetSyntaxRoot();
            var methods = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.MethodDeclaration).Select(
                n => (MethodDeclarationSyntax) n);
            return methods.First(n => n.Identifier.ValueText.Equals(methodName));
        }


        [TestMethod]
        public void TestMethod1()
        {
            var method1 = GetMethodDeclaration(document1, "method1");
            var method2 = GetMethodDeclaration(document2, "increment");
            Assert.IsNotNull(method1);
            Assert.IsNotNull(method2);
        }

        [TestMethod]
        public void TestMethod2()
        {  
            var method2 = GetMethodDeclaration(document2, "increment");
            var retriever = RetrieverFactory.GetMethodInvocationRetriever();
            retriever.SetDocument(document1);
            retriever.SetMethodDeclaration(method2);
            var invocations = retriever.GetInvocations();
            Assert.IsNotNull(invocations);
            Assert.IsTrue(invocations.Count() == 1);
        }

        [TestMethod]
        public void TestMethod3()
        {
            var method = GetMethodDeclaration(document2, "increment");
            var retriever = RetrieverFactory.GetMethodInvocationRetriever();
            retriever.SetDocument(document2);
            retriever.SetMethodDeclaration(method);
            var invocations = retriever.GetInvocations();
            Assert.IsNotNull(invocations);
            Assert.IsTrue(invocations.Count() == 2);
        }

        [TestMethod]
        public void TestMethod4()
        {
            var method = GetMethodDeclaration(document1, "method1");
            var retriever = RetrieverFactory.GetMethodInvocationRetriever();
            retriever.SetDocument(document1);
            retriever.SetMethodDeclaration(method);
            var invocations = retriever.GetInvocations();
            Assert.IsNotNull(invocations);
            Assert.IsTrue(invocations.Count() == 1);
        }

        [TestMethod]
        public void TestMethod5()
        {
            var method = GetMethodDeclaration(document2, "decrement");
            var retriever = RetrieverFactory.GetMethodInvocationRetriever();
            retriever.SetDocument(document1);
            retriever.SetMethodDeclaration(method);
            var invocations = retriever.GetInvocations();
            Assert.IsNotNull(invocations);
            Assert.IsTrue(invocations.Count() == 1);
        }
    }
}
