using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using warnings.analyzer;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class StatementAnalyzerTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var analyzer = AnalyzerFactory.GetStatementAnalyzer();
            analyzer.SetSource("print();");
            Assert.IsTrue(analyzer.HasMethodInvocation("print"));
        }
    }
}
