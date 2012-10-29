using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.analyzer;
using warnings.retriever;
using warnings.util;

namespace WarningTest.retriever_test
{
    [TestClass]
    public class TypableRetrieverTests
    {
        private readonly ITypablesRetriever retriever;
        private readonly IDocument document;
        private readonly Logger logger;
        private readonly IDocumentAnalyzer analyzer;

        public TypableRetrieverTests()
        {
            this.retriever = RetrieverFactory.GetTypablesRetriever();
            var code = TestUtil.GetFakeSourceFolder() + "MethodAnalyzerExample.cs";
            var converter = new String2IDocumentConverter();
            this.document = (IDocument)converter.Convert(FileUtil.ReadAllText(code), null, null, null);
            this.logger = NLoggerUtil.GetNLogger(typeof (TypableRetrieverTests));
            this.analyzer = AnalyzerFactory.GetDocumentAnalyzer();
            retriever.SetDocument(document);
            analyzer.SetDocument(document);
        }


        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(retriever);
            Assert.IsNotNull(document);
            var tuples = retriever.GetTypableIdentifierTypeTuples();
            foreach (var tuple in tuples)
            {
                logger.Info(tuple.Item1 + ":" + tuple.Item2.Name);   
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            var symbols = analyzer.GetAllDeclaredSymbols();
            var symbolAnalyzer = AnalyzerFactory.GetSymbolAnalyzer();
            foreach (var symbol in symbols)
            {
                symbolAnalyzer.SetSymbol(symbol);
                logger.Info(symbolAnalyzer.GetDeclarationSyntaxNode());
                logger.Info(symbolAnalyzer.GetSymbolTypeName());
            }
        }
    }
}
