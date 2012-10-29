using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using warnings.resources;

namespace WarningTest
{
    [TestClass]
    public class ResourcesTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var icon = ResourcePool.GetIcon();
            Assert.IsNotNull(icon);
        }
    }
}
