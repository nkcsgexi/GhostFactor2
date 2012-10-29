using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Services;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class RoslynRefactoringUtilTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            bool ready = RoslynRefactoringUtil.CheckRenamePreconditions(null, 0, 0);
            Assert.IsTrue(ready);
        }

        [TestMethod]
        public void TestMethod2()
        {
     /*       var services = ServiceArchive.GetInstance();
            Assert.IsNotNull(services);
            Assert.IsTrue(services.isImportedAttributeReady());*/
          //  Assert.IsNotNull(services.extractMethodService);
        }

        [TestMethod]
        public void TestMethod4()
        {
            var doc = loadExtractMethodExampleDoc();
            Assert.IsNotNull(doc);
            Assert.IsFalse(RoslynRefactoringUtil.CheckExtractMethodPreconditions(doc, 0, 1));
        }


        private IDocument loadExtractMethodExampleDoc()
        {
            var solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            var project = RoslynUtil.GetProject(solution, "WarningTest");
            var document = RoslynUtil.GetDocument(project, "ExtractMethodExample.cs");
            return document;
        }
    }
}
