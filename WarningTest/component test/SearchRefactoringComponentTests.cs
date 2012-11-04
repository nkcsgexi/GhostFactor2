using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using warnings.components;
using warnings.source.history;

namespace WarningTest.component_test
{
    [TestClass]
    public class SearchRefactoringComponentTests
    {
        private readonly ICodeHistory history;


        public SearchRefactoringComponentTests()
        {
            this.history = CodeHistory.GetInstance();
        }


        [TestMethod]
        public void TestMethod1()
        {
            for (int i = 1; i < 100; i++ )
                history.AddRecord("test1", "c");
            Assert.IsTrue(history.HasRecord("test1"));
            var record = history.GetLatestRecord("test1");
            GhostFactorComponents.searchRefactoringComponent.StartRefactoringSearch(record,
                null);
            Thread.Sleep(int.MaxValue);
        }
    }
}
