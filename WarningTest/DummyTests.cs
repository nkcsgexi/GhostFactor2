using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Compilers.CSharp;

namespace WarningTest
{
    [TestClass]
    public class DummyTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var assignment = Syntax.ParseExpression("i = 1");
            Assert.IsNotNull(assignment);
            RegionDirectiveSyntax start;
            EndRegionDirectiveSyntax end;
        }
    }
}
