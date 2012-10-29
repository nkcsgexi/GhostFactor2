using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class ChangeSignatureDetectorTests
    {
        private readonly string sourceBefore;
        private readonly string sourceAfter;
        private readonly IExternalRefactoringDetector detector;
        private readonly Logger logger;

        public ChangeSignatureDetectorTests()
        {
            sourceBefore = FileUtil.ReadAllText(TestUtil.GetFakeSourceFolder() + "ChangeMethodSignatureBefore.txt");
            sourceAfter = FileUtil.ReadAllText(TestUtil.GetFakeSourceFolder() + "ChangeMethodSignatureAfter.txt");
            detector = RefactoringDetectorFactory.CreateChangeMethodSignatureDetector();
            detector.SetSourceBefore(sourceBefore);
            detector.SetSourceAfter(sourceAfter);
            logger = NLoggerUtil.GetNLogger(typeof (ChangeSignatureDetectorTests));
        }


        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(sourceAfter);
            Assert.IsNotNull(sourceBefore);
            Assert.IsTrue(detector.HasRefactoring());
            var refactorings = detector.GetRefactorings();
            Assert.IsTrue(refactorings.Count() == 2);
            var refactoring = (IChangeMethodSignatureRefactoring) refactorings.First();
            var map = refactoring.ParametersMap;
            logger.Info(refactoring.ChangedMethodDeclaration);
            logger.Info(refactoring.ParametersMap.Count());
            Assert.IsTrue(map.Count() == 2);
            Assert.AreEqual(map.ElementAt(0).Item1, 0);
            Assert.AreEqual(map.ElementAt(0).Item2, 1);
            Assert.AreEqual(map.ElementAt(1).Item1, 1);
            Assert.AreEqual(map.ElementAt(1).Item2, 0);
        }

        [TestMethod]
        public void TestMethod2()
        {
            Assert.IsTrue(detector.HasRefactoring());
            var refactoring = (IChangeMethodSignatureRefactoring)detector.GetRefactorings().ElementAt(1);
            var map = refactoring.ParametersMap;

            logger.Info(map.ElementAt(0).ToString());
            logger.Info(map.ElementAt(1).ToString());
            logger.Info(map.ElementAt(2).ToString());

            Assert.IsTrue(map.Count() == 3);
            Assert.AreEqual(map.ElementAt(0).Item1, 0);
            Assert.AreEqual(map.ElementAt(0).Item2, 2);
            Assert.AreEqual(map.ElementAt(1).Item1, 1);
            Assert.AreEqual(map.ElementAt(1).Item2, 0);
            Assert.AreEqual(map.ElementAt(2).Item1, 2);
            Assert.AreEqual(map.ElementAt(2).Item2, 1);
        }
    }
}
