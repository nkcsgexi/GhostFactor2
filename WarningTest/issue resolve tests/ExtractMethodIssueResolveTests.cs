using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using warnings.components;
using warnings.conditions;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.util;

namespace WarningTest.issue_resolve_tests
{
    [TestClass]
    public class ExtractMethodIssueResolveTests
    {
        private readonly IExternalRefactoringDetector detector;
        private readonly IRefactoringConditionsList conditions;
        private readonly string source1;
        private readonly string source2;
        private readonly string source3;
        private readonly string source4;
        private readonly string source5;
        private readonly string source6;


        public ExtractMethodIssueResolveTests()
        {
            detector = RefactoringDetectorFactory.GetRefactoringDetectorByType(RefactoringType.
                EXTRACT_METHOD);
            conditions = ConditionCheckingFactory.GetConditionsListByRefactoringType(RefactoringType.
                EXTRACT_METHOD);
            source1 = FileUtil.ReadAllText(TestUtil.GetStudyFakeSourceFolder() + "ExtractMethod1.txt");
            source2 = FileUtil.ReadAllText(TestUtil.GetStudyFakeSourceFolder() + "ExtractMethod2.txt");
            source3 = FileUtil.ReadAllText(TestUtil.GetStudyFakeSourceFolder() + "ExtractMethod3.txt");
            source4 = FileUtil.ReadAllText(TestUtil.GetStudyFakeSourceFolder() + "ExtractMethod4.txt");
            source5 = FileUtil.ReadAllText(TestUtil.GetStudyFakeSourceFolder() + "ExtractMethod5.txt");
            source6 = FileUtil.ReadAllText(TestUtil.GetStudyFakeSourceFolder() + "ExtractMethod6.txt");
           
        }

        [TestMethod]
        public void TestMethod1()
        {
            detector.SetSourceBefore(source1);
            detector.SetSourceAfter(source2);
            Assert.IsTrue(detector.HasRefactoring());
            Assert.IsTrue(detector.GetRefactorings().Count() == 1);
            var results1 = conditions.CheckAllConditions(detector.GetRefactorings().First());
            Assert.IsTrue(results1.Count() == 2);

            detector.SetSourceBefore(source1);
            detector.SetSourceAfter(source3);
            Assert.IsTrue(detector.HasRefactoring());
            Assert.IsTrue(detector.GetRefactorings().Count() == 1);
            var results2 = conditions.CheckAllConditions(detector.GetRefactorings().First());
            Assert.IsTrue(results2.Count() == 2);

            detector.SetSourceBefore(source1);
            detector.SetSourceAfter(source4);
            Assert.IsTrue(detector.HasRefactoring());
            Assert.IsTrue(detector.GetRefactorings().Count() == 1);
            var results3 = conditions.CheckAllConditions(detector.GetRefactorings().First());
            Assert.IsTrue(results3.Count() == 2);

            detector.SetSourceBefore(source1);
            detector.SetSourceAfter(source5);
            Assert.IsTrue(detector.HasRefactoring());
            Assert.IsTrue(detector.GetRefactorings().Count() == 1);
            var results4 = conditions.CheckAllConditions(detector.GetRefactorings().First());
            Assert.IsTrue(results4.Count() == 2);

            detector.SetSourceBefore(source1);
            detector.SetSourceAfter(source6);
            Assert.IsTrue(detector.HasRefactoring());
            Assert.IsTrue(detector.GetRefactorings().Count() == 1);
            var results5 = conditions.CheckAllConditions(detector.GetRefactorings().First());
            Assert.IsTrue(results5.Count() == 2);

            var para1 = results1.First(r => r.RefactoringConditionType == RefactoringConditionType.
                EXTRACT_METHOD_PARAMETER);
            var return1 = results1.First(r => r.RefactoringConditionType == RefactoringConditionType.
                EXTRACT_METHOD_RETURN_VALUE);
            var para2 = results2.First(r => r.RefactoringConditionType == RefactoringConditionType.
                EXTRACT_METHOD_PARAMETER);
            var return2 = results2.First(r => r.RefactoringConditionType == RefactoringConditionType.
              EXTRACT_METHOD_RETURN_VALUE);
            var para3 = results3.First(r => r.RefactoringConditionType == RefactoringConditionType.
                EXTRACT_METHOD_PARAMETER);
            var return3 = results3.First(r => r.RefactoringConditionType == RefactoringConditionType.
              EXTRACT_METHOD_RETURN_VALUE);
            var para4 = results4.First(r => r.RefactoringConditionType == RefactoringConditionType.
               EXTRACT_METHOD_PARAMETER);
            var return4 = results4.First(r => r.RefactoringConditionType == RefactoringConditionType.
              EXTRACT_METHOD_RETURN_VALUE);
            var para5 = results5.First(r => r.RefactoringConditionType == RefactoringConditionType.
              EXTRACT_METHOD_PARAMETER);
            var return5 = results5.First(r => r.RefactoringConditionType == RefactoringConditionType.
              EXTRACT_METHOD_RETURN_VALUE);

            Assert.IsNotNull(para1);
            Assert.IsNotNull(para2);
            Assert.IsNotNull(para3);
            Assert.IsNotNull(para4);
            Assert.IsNotNull(para5);
            Assert.IsTrue(para5 is ICorrectRefactoringResult);

            Assert.IsNotNull(return1);
            Assert.IsNotNull(return2);
            Assert.IsNotNull(return3);
            Assert.IsNotNull(return4);
            Assert.IsNotNull(return5);

            Assert.IsTrue(para1 is IUpdatableCodeIssueComputer);
            Assert.IsTrue(para2 is IUpdatableCodeIssueComputer);
            Assert.IsTrue(para3 is IUpdatableCodeIssueComputer);
            Assert.IsTrue(return1 is IUpdatableCodeIssueComputer);
            Assert.IsTrue(return2 is IUpdatableCodeIssueComputer);
            Assert.IsTrue(return3 is IUpdatableCodeIssueComputer);

            Assert.IsTrue(((IUpdatableCodeIssueComputer)para1).IsUpdatedComputer((IUpdatableCodeIssueComputer)
                para2));
            Assert.IsTrue(((IUpdatableCodeIssueComputer)para2).IsUpdatedComputer((IUpdatableCodeIssueComputer)
                para3));
            Assert.IsTrue(((IUpdatableCodeIssueComputer)para3).IsUpdatedComputer((IUpdatableCodeIssueComputer)
                para4));

            Assert.IsTrue(((ICodeIssueComputer)para1).IsIssueResolved((ICorrectRefactoringResult) para5));
            Assert.IsTrue(((ICodeIssueComputer)para2).IsIssueResolved((ICorrectRefactoringResult) para5));
            Assert.IsTrue(((ICodeIssueComputer)para3).IsIssueResolved((ICorrectRefactoringResult) para5));
            Assert.IsTrue(((ICodeIssueComputer)para4).IsIssueResolved((ICorrectRefactoringResult) para5));


            Assert.IsFalse(((IUpdatableCodeIssueComputer)para1).IsUpdatedComputer((IUpdatableCodeIssueComputer)
                return2));
            Assert.IsFalse(((IUpdatableCodeIssueComputer)para2).IsUpdatedComputer((IUpdatableCodeIssueComputer)
                return3));
            Assert.IsFalse(((IUpdatableCodeIssueComputer)para1).IsUpdatedComputer((IUpdatableCodeIssueComputer)
                return3));


            GhostFactorComponents.RefactoringCodeIssueComputerComponent.AddCodeIssueComputers(results1.
                OfType<ICodeIssueComputer>());
            GhostFactorComponents.RefactoringCodeIssueComputerComponent.AddCodeIssueComputers(results2.
                OfType<ICodeIssueComputer>());
            GhostFactorComponents.RefactoringCodeIssueComputerComponent.AddCodeIssueComputers(results3.
                OfType<ICodeIssueComputer>());
            
            Thread.Sleep(5000);
        }
    }
}
