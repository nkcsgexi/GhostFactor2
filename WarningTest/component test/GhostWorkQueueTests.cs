using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlackHen.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WarningTest.component_test
{
    [TestClass]
    public class GhostWorkQueueTests
    {
        private readonly WorkQueue queue;
        private readonly WorkItem item;

        public GhostWorkQueueTests()
        {
            this.queue = new WorkQueue();
            this.item = new WaitWorkItem();
        }

        private class WaitWorkItem : WorkItem
        {
            public override void Perform()
            {
                Thread.Sleep(20000);
            }
        }


        [TestMethod]
        public void TestMethod1()
        {
            queue.Add(item);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var executor = new WorkItemSynchronizedExecutor(item, queue);
            executor.Execute();
        }
    }
}
