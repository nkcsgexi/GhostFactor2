using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Compilers.CSharp;
using warnings.analyzer;

namespace WarningTest.analyzer_test
{
    [TestClass]
    public class ReturnStatementAnalyzerTests
    {
        private readonly IReturnStatementAnalyzer analyzer;

        public ReturnStatementAnalyzerTests()
        {
            this.analyzer = AnalyzerFactory.GetReturnStatementAnalyzer();
        }

        private SyntaxNode ParseStatement(string code)
        {
            return Syntax.ParseStatement(code);
        }


        [TestMethod]
        public void TestMethod1()
        {
            var node = Syntax.ParseStatement("return 1;");
            analyzer.SetReturnStatement(node);
            var exp = analyzer.GetReturnedExpression();
            Assert.IsTrue(exp.GetText().Equals("1"));
        }

        [TestMethod]
        public void TestMethod2()
        {
            var node = ParseStatement("return null;");
            analyzer.SetReturnStatement(node);
            var exp = analyzer.GetReturnedExpression();
            Assert.IsTrue(exp.GetText().Equals("null"));
            Assert.IsTrue(analyzer.IsReturningNull());
        }
    }
}
