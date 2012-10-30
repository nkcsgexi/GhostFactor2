using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using warnings.resources;

namespace WarningTest.util_test
{
    [TestClass]
    public class ResourceTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            ResourcePool.GetIcon();
        }
    }
}
