using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using warnings.conditions;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class EMDetectorTests
    {
        private static readonly string fileBefore = 
            TestUtil.GetFakeSourceFolder() + "EMDetectorBefore.cs";


        private static readonly string fileAfter = 
            TestUtil.GetFakeSourceFolder() + "EMDetectorAfter.cs";

        private IRefactoringConditionsList conditionsList =
            ConditionCheckingFactory.GetConditionsListByRefactoringType(RefactoringType.EXTRACT_METHOD);

        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof (EMDetectorTests));

        [TestMethod]
        public void TestMethod1()
        {
            var detector = RefactoringDetectorFactory.GetRefactoringDetectorByType
                (RefactoringType.EXTRACT_METHOD);
            var sourceBefore = FileUtil.ReadAllText(fileBefore);
            var sourceAfter = FileUtil.ReadAllText(fileAfter);
            detector.SetSourceBefore(sourceBefore);
            detector.SetSourceAfter(sourceAfter);
            Assert.IsTrue(detector.HasRefactoring());
            foreach (var refactoring in detector.GetRefactorings())
            {
                logger.Info(refactoring.ToString());
            }
            conditionsList.CheckAllConditions(detector.GetBeforeDocument(), detector.GetAfterDocument(),
                detector.GetRefactorings().First());
        }

        [TestMethod]
        public void TestMethod2()
        {
            var detector = RefactoringDetectorFactory.GetRefactoringDetectorByType
                (RefactoringType.EXTRACT_METHOD);
            var sourceBefore = FileUtil.ReadAllText(fileBefore);
            var sourceAfter = FileUtil.ReadAllText(fileAfter);
            detector.SetSourceBefore(sourceBefore);
            detector.SetSourceAfter(sourceAfter);
            Assert.IsTrue(detector.HasRefactoring());
            foreach (var refactoring in detector.GetRefactorings())
            {
                logger.Info(refactoring.ToString());
            }
            conditionsList.CheckAllConditions(detector.GetBeforeDocument(), detector.GetAfterDocument(),
                detector.GetRefactorings().First());
        }

        
        [TestMethod]
        public void Test3()
        {
            var detector = RefactoringDetectorFactory.GetRefactoringDetectorByType
                (RefactoringType.EXTRACT_METHOD);
            var soureceBefore = FileUtil.ReadAllText(TestUtil.GetFakeSourceFolder() +
                                                        "DriverBefore.txt");
            var soureceAfter = FileUtil.ReadAllText(TestUtil.GetFakeSourceFolder() +
                                                    "DriverAfter.txt");
            detector.SetSourceBefore(soureceBefore);
            detector.SetSourceAfter(soureceAfter);
            Assert.IsTrue(detector.HasRefactoring());
            var refactoring = detector.GetRefactorings().First();
            var condition = ConditionCheckingFactory.GetConditionsListByRefactoringType
                (RefactoringType.EXTRACT_METHOD);
            condition.CheckAllConditions(detector.GetBeforeDocument(), detector.GetAfterDocument()
                    , refactoring);
        }

    }
}
