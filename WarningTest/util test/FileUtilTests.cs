using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using warnings.util;

namespace WarningTest.util_test
{
    [TestClass]
    public class FileUtilTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var allFiles = FileUtil.GetFilesFromDirectory(TestUtil.GetFakeSourceFolder(), "*.*");
            Assert.IsNotNull(allFiles);
            Assert.IsTrue(allFiles.Any());

            var csFiles = FileUtil.GetFilesFromDirectory(TestUtil.GetFakeSourceFolder(), "*.cs");
            Assert.IsNotNull(csFiles);
            Assert.IsTrue(csFiles.Any());
            Assert.IsTrue(csFiles.Count() > 5);

            var dllFiles = FileUtil.GetFilesFromDirectory(TestUtil.GetFakeSourceFolder(), "*.dll");
            Assert.IsNotNull(dllFiles);
            Assert.IsFalse(dllFiles.Any());

        }
    }
}
