using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.analyzer;
using warnings.retriever;
using warnings.util;

namespace WarningTest.analyzer_test
{
    [TestClass]
    public class SolutionAnalyzerTests
    {
        private readonly Logger logger;
        private readonly ISolution solution;
        private readonly ISolutionAnalyzer analyzer;
        private readonly IProject project;
        private readonly IEnumerable<IDocument> documents;

        private readonly SyntaxNode rootOne;
        private readonly SyntaxNode rootTwo;

        private readonly ISemanticModel modelOne;
        private readonly ISemanticModel modelTwo;

        public SolutionAnalyzerTests()
        {
            try
            {
                this.logger = NLoggerUtil.GetNLogger(typeof (SolutionAnalyzerTests));
                Assert.IsTrue(File.Exists(TestUtil.GetAnotherSolutionPath()));
                this.solution = Solution.Load(TestUtil.GetAnotherSolutionPath());
                this.analyzer = AnalyzerFactory.GetSolutionAnalyzer();
                analyzer.SetSolution(solution);
                Assert.IsNotNull(solution);
                Assert.IsTrue(analyzer.GetProjects().Count() == 1);
                project = analyzer.GetProjects().First();
                Assert.IsNotNull(project);
                documents = analyzer.GetDocuments(project);
                Assert.IsTrue(documents.Count() == 2);
                rootOne = (SyntaxNode) documents.ElementAt(0).GetSyntaxRoot();
                rootTwo = (SyntaxNode) documents.ElementAt(1).GetSyntaxRoot();
                modelOne = documents.ElementAt(0).GetSemanticModel();
                modelTwo = documents.ElementAt(1).GetSemanticModel();
            }catch(Exception e)
            {
                logger.Fatal(e);
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            var increment = rootOne.DescendantNodes().First(n => n.GetText().Equals("issue.increment()"));
            Assert.IsNotNull(increment);
            var symbols = modelOne.LookupSymbols(increment.Span.Start);
            Assert.IsNotNull(symbols);
            Assert.IsTrue(symbols.Any());

            var issue = increment.DescendantNodes().First(n => n.GetText().EndsWith("issue"));

            var typeInfo = modelOne.GetTypeInfo(issue);
            logger.Info(typeInfo.Type.ToString());

        }

        [TestMethod]
        public void TestMethod2()
        {
            var getcount = rootOne.DescendantNodes().First(n => n.GetText().Equals("GetCount"));
            Assert.IsNotNull(getcount);
            var nodeAnalyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
            nodeAnalyzer.SetSyntaxNode(getcount);
            logger.Info(nodeAnalyzer.DumpTree());
            var typeinfo = modelOne.GetTypeInfo(getcount);
            logger.Info(typeinfo.Type.ToString());
        }

        [TestMethod]
        public void TestMethod3()
        {
            var integer = rootOne.DescendantNodes().First(n => n.GetText().Equals("integer"));
            var typeinfo = modelOne.GetTypeInfo(integer);
            logger.Info(typeinfo.Type.ToString());
        }

        [TestMethod]
        public void TestMethod4()
        {
            var typableRetrievers = RetrieverFactory.GetTypablesRetriever();
            typableRetrievers.SetDocument(documents.ElementAt(0));
            var tuples = typableRetrievers.GetTypableIdentifierTypeTuples();
            foreach (var tuple in tuples)
            {
                logger.Info(tuple.Item1.GetText() + ":" + tuple.Item2.ToString());
            }
        }
        [TestMethod]
        public void TestMethod5()
        {
            var typableRetrievers = RetrieverFactory.GetTypablesRetriever();
            typableRetrievers.SetDocument(documents.ElementAt(0));
            var tuples = typableRetrievers.GetMemberAccessAndAccessedTypes();
            foreach (var tuple in tuples)
            {
                logger.Info(tuple.Item1.GetText() + ":" + tuple.Item2);
            }
        }
    }
}
