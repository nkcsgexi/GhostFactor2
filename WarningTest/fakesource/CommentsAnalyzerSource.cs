using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WarningTest.fakesource
{
    [TestClass]
    public class CommentsAnalyzerSource
    {
        [TestMethod]
        public void TestMethod1()
        {
            // This is the method body of method1;
        }

        [TestMethod]
        public void TestMethod2()
        {
            // This is the method body of method2.
        }

        /// <summary>
        /// Helper method.
        /// </summary>
        private void helper()
        {
            // helper method comments line 1
            // helper method comments line 2
        }
    }
}
