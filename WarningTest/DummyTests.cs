using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WarningTest
{
    [TestClass]
    public class DummyTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsFalse(1 != 1);
        }
    }
}
