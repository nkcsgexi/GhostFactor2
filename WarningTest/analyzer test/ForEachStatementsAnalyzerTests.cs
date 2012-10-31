using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.CSharp;
using warnings.analyzer;
using warnings.util;

namespace WarningTest.analyzer_test
{
    [TestClass]
    public class ForEachStatementsAnalyzerTests
    {
        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof (ForEachStatementsAnalyzerTests));
        private readonly IForEachStatementAnalyzer forEachStatementAnalyzer = AnalyzerFactory.GetForEachStatementAnalyzer();
        private readonly ISyntaxNodeAnalyzer syntaxNodeAnalyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();

        [TestMethod]
        public void TestMethod1()
        {
            var code = "foreach (var ios in new int[5]);";
            var statement = Syntax.ParseStatement(code);
            Assert.IsNotNull(statement);
            syntaxNodeAnalyzer.SetSyntaxNode(statement);
            logger.Info(syntaxNodeAnalyzer.DumpTree());
            forEachStatementAnalyzer.SetStatement(statement);
            Assert.IsTrue(forEachStatementAnalyzer.GetIdentifier().GetText().Equals("ios"));
            Assert.IsTrue(forEachStatementAnalyzer.GetIdentifierType().GetText().Equals("var"));
        }
    }
}
