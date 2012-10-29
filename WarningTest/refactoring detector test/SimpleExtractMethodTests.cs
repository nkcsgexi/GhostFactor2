using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.CSharp;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.util;

namespace WarningTest.refactoring_detector_test
{
    [TestClass]
    public class SimpleExtractMethodTests
    {
        private readonly Logger logger;
        private readonly IExternalRefactoringDetector detector;
        private readonly string sourceBefore;
        private readonly string sourceAfter;

        public SimpleExtractMethodTests()
        {
            this.logger = NLoggerUtil.GetNLogger(typeof (SimpleExtractMethodTests));
            this.sourceBefore = FileUtil.ReadAllText(TestUtil.GetFakeSourceFolder() + "SimpleExtractMethodBefore.txt");
            this.sourceAfter = FileUtil.ReadAllText(TestUtil.GetFakeSourceFolder() + "SimpleExtractMethodAfter.txt");
            this.detector = RefactoringDetectorFactory.CreateDummyExtractMethodDetector();
        }

        [TestMethod]
        public void TestMethod1()
        {
            detector.SetSourceBefore(sourceBefore);
            detector.SetSourceAfter(sourceAfter);
            Assert.IsTrue(detector.HasRefactoring());
            Assert.IsTrue(detector.GetRefactorings().Count() == 1);
        }

        [TestMethod]
        public void TestMethod2()
        {   
            detector.SetSourceAfter(sourceAfter);
            detector.SetSourceBefore(sourceBefore);
            Assert.IsTrue(detector.HasRefactoring());
            Assert.IsTrue(detector.GetRefactorings().Count() == 1);
            var refactoring = (ISimpleExtractMethodRefactoring)detector.GetRefactorings().First();
            var method = (MethodDeclarationSyntax) refactoring.addedMethod;
            Assert.IsTrue(method.Identifier.ValueText.Equals("bar"));
        }

        [TestMethod]
        public void TestMethod3()
        {
            detector.SetSourceAfter(sourceAfter);
            detector.SetSourceBefore(sourceBefore);
            Assert.IsTrue(detector.HasRefactoring());
            Assert.IsTrue(detector.GetRefactorings().Count() == 1);
            detector.SetSourceAfter(sourceAfter);
            detector.SetSourceBefore(sourceBefore);
            Assert.IsTrue(detector.HasRefactoring());
            Assert.IsTrue(detector.GetRefactorings().Count() == 1);
        }
    }
}
