using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using warnings.conditions;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.util;

namespace WarningTest.issue_resolve_tests
{
    [TestClass]
    public class InlineMethodIssueResolver
    {
        private readonly IExternalRefactoringDetector detector;
        private readonly IRefactoringConditionsList checker;
        private readonly string code0;
        private readonly string code1;
        private readonly string code2;


        public InlineMethodIssueResolver()
        {
            this.detector = RefactoringDetectorFactory.GetRefactoringDetectorByType(RefactoringType.
                INLINE_METHOD);
            this.checker = ConditionCheckingFactory.GetConditionsListByRefactoringType(RefactoringType.
                INLINE_METHOD);
            this.code0 = FileUtil.ReadAllText(TestUtil.GetStudyFakeSourceFolder() + 
                "ConsoleLibInlinebefore.txt");
            this.code1 = FileUtil.ReadAllText(TestUtil.GetStudyFakeSourceFolder() +
                "ConsoleLibInlineafter.txt");
            this.code2 = FileUtil.ReadAllText(TestUtil.GetStudyFakeSourceFolder() +
                "ConsoleLibInlineResolved.txt");
        }


        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(detector);
            Assert.IsNotNull(code0);
            Assert.IsNotNull(code1);
            Assert.IsNotNull(code2);
        }

        [TestMethod]
        public void TestMethod2()
        {
            detector.SetSourceBefore(code0);
            detector.SetSourceAfter(code1);
            Assert.IsTrue(detector.HasRefactoring());
            var refactoring = detector.GetRefactorings().First();
            var results = checker.CheckAllConditions(refactoring);
            Assert.IsTrue(results.Any(r => r is ICodeIssueComputer));
        }

        [TestMethod]
        public void TestMethod3()
        {
            detector.SetSourceBefore(code0);
            detector.SetSourceAfter(code2);
            Assert.IsTrue(detector.HasRefactoring());
            var refactoring = detector.GetRefactorings().First();
            var results = checker.CheckAllConditions(refactoring);
            Assert.IsFalse(results.Any(r => r is ICodeIssueComputer));
        }
    }
}
