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
    public class IfStatementAnalyzerTests
    {
        private readonly IIfStatementAnalyzer analyzer;

        public IfStatementAnalyzerTests()
        {
            this.analyzer = AnalyzerFactory.GetIfStatementAnalyzer();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var source = @"
                if(true)
                {int i; i = 0; i = 1;}
                else
                {int k; k = 0; k = 1;}";
            var statement = Syntax.ParseStatement(source);
            Assert.IsNotNull(statement);
            analyzer.SetIfStatement(statement);
            Assert.IsTrue(analyzer.WithElse());
            Assert.IsNotNull(analyzer.GetBlockUnderElse());
            Assert.IsNotNull(analyzer.GetBlockUnderIf());
            Assert.IsTrue(analyzer.GetDirectBlocks().Count() == 2);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var source = @"
                if(true)
                int i = 1;
                else
                {int k; k = 0; k = 1;}";
            var statement = Syntax.ParseStatement(source);
            Assert.IsNotNull(statement);
            analyzer.SetIfStatement(statement);
            Assert.IsTrue(analyzer.WithElse());
            Assert.IsFalse(analyzer.HasBlockUnderIf());
            Assert.IsTrue(analyzer.WithElse() && analyzer.HasBlockUnderElse());
            Assert.IsFalse(analyzer.HasBlockUnderIf());
            Assert.IsTrue(analyzer.GetDirectBlocks().Count() == 1);
        }
      
        [TestMethod]
        public void TestMethod3()
        {
            var source = @"
                if(true)
                {int i = 1; i = 1}
                else if (true)
                {int k; k = 0; k = 1;}
                else {int j; j =1;}";
            var statement = Syntax.ParseStatement(source);
            Assert.IsNotNull(statement);
            analyzer.SetIfStatement(statement);
            Assert.IsTrue(analyzer.GetDirectBlocks().Count() == 3);
        }

        [TestMethod]
        public  void TestMethod4()
        {
            var source = @"    if (true)
                if (true)
                {
                    int i = 0;
                }";
            var statement = Syntax.ParseStatement(source);
            Assert.IsNotNull(statement);
            analyzer.SetIfStatement(statement);
            Assert.IsTrue(analyzer.GetDirectBlocks().Count() == 1);
        }

        [TestMethod]
        public void TestMethod5()
        {
            var source = @" if(true)
                if(true)
                {
                    
                }
                else
                {
                
                }
            else
            {
                
            }
            ";
            var statement = Syntax.ParseStatement(source);
            Assert.IsNotNull(statement);
            analyzer.SetIfStatement(statement);
            Assert.IsTrue(analyzer.GetDirectBlocks().Count() == 3);
        }
    }
}
