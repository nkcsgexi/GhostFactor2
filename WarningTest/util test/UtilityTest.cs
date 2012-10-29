using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Services;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class UtilityTest
    {
        String first = "abcdefgh";
        String second = "123456";

        private Logger logger = NLoggerUtil.GetNLogger(typeof (UtilityTest));

        // regression to Insert
        [TestMethod]
        public void TestMethod1()
        {
            String result = StringUtil.ReplaceWith(first, second, 0, 0);
            Assert.IsTrue(result.Equals(second + first));
            result = StringUtil.ReplaceWith(first, second, 1, 0);
            Assert.IsTrue(result.Equals("a123456bcdefgh"));
            result = StringUtil.ReplaceWith(first, second, first.Length, 0);
            Assert.IsTrue(result.Equals(first + second));
        }

        [TestMethod]
        public void TestMethod2()
        {
            String result = StringUtil.ReplaceWith(first, second, 0, 1);
            Assert.IsTrue(result.Equals("123456bcdefgh"));
            result = StringUtil.ReplaceWith(first, second, 0, 2);
            Assert.IsTrue(result.Equals("123456cdefgh"));
        }

        [TestMethod]
        public void TestMethod3()
        {
            string first2 = "abcd";
            int near = StringUtil.GetStringDistance(first, first2);
            int far = StringUtil.GetStringDistance(first, second);
            Assert.IsTrue(far > near);
            string second2 = "123242";
            near = StringUtil.GetStringDistance(second, second2);
            Assert.IsTrue(far > near);
            far = StringUtil.GetStringDistance(second2, first);
            Assert.IsTrue(far > near);
        }

        [TestMethod]
        public void TestMethod4()
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Debug("debug");
            logger.Error("error");
            logger.Fatal("fatal");
            logger.Info("info");
        }

        [TestMethod]
        public void TestMethod5()
        {
            string path = TestUtil.GetTestProjectPath() + "UtilityTest.cs";
            string code = FileUtil.ReadAllText(path);
            Assert.IsNotNull(code);
            Assert.IsFalse(code.Equals(""));
            var converter = new String2IDocumentConverter();
            IDocument document = (IDocument) converter.Convert(code, typeof (IDocument), null, null);
            Assert.IsNotNull(document);
            logger.Info(document.GetText());
         }
    }
}
