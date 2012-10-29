using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class MethodBodyRetrieverTest
    {
        private static readonly string path = 
            @"D:\VS workspaces\BeneWar\warnings\WarningTest\MethodBodyRetrieverTest.cs";
        private static readonly int TEST_METHODS_COUNT = 3;

        [TestMethod]
        public void TestMethod1()
        {
            string source = FileUtil.ReadAllText(path);
            IMethodRetriever retriever = new MethodBodyRetriever();
            retriever.setSource(source);
            Assert.IsTrue(retriever.getMethodBodiesCount() == TEST_METHODS_COUNT);
        }

        [TestMethod]
        public void TestMethod2()
        {
            string source = FileUtil.ReadAllText(path);
            IMethodRetriever retriever = new MethodBodyRetriever();
            retriever.setSource(source);
            String[] bodies = retriever.getMethodBodies();
            Assert.IsTrue(bodies[1].StartsWith("string source = FileUtil.ReadAllText(path);"));
            Assert.IsTrue(bodies[0].StartsWith("string source = FileUtil.ReadAllText(path);"));
            Assert.IsTrue(bodies[0].EndsWith("Assert.IsTrue(retriever.getMethodBodiesCount() == TEST_METHODS_COUNT);\r\n"));
            Assert.IsTrue(bodies[0].Contains("IMethodBodyRetriever retriever = new MethodBodyRetriever();"));
            Assert.IsTrue(bodies[0].Contains("retriever.setSource(source);"));        
        }

        [TestMethod]
        public void TestMethod3()
        {
            string source = FileUtil.ReadAllText(path);
            IMethodRetriever retriever = new MethodBodyRetriever();
            retriever.setSource(source);
            String[] names = retriever.getMethodNames();
            Assert.IsTrue(names.Count() == TEST_METHODS_COUNT);
            Assert.IsTrue(names[0].Equals("TestMethod1"));
            Assert.IsTrue(names[1].Equals("TestMethod2"));
            Assert.IsTrue(names[2].Equals("TestMethod3"));
        }
    }
}
