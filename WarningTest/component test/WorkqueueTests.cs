using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlackHen.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class WorkqueueTests
    {
        private static Logger log = NLoggerUtil.GetNLogger(typeof (WorkqueueTests));
        private static WorkQueue queue = new WorkQueue();

        static WorkqueueTests()
        {
            queue.ConcurrentLimit = 1;
        }

        internal class TestItem : WorkItem
        {
            private readonly int number;

            internal TestItem(int number)
            {
                this.number = number;
            }
            public override void Perform()
            {
                log.Info("Perform Item " + number);
            }
        }

        internal class IntEventArgs : EventArgs
        {
            public int number { get; private set; }

            internal IntEventArgs(int number)
            {
                this.number = number;
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            for (int i = 0; i < 100; i ++ )
            {
                queue.WaitAll();
                queue.Add(new TestItem(i));
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            queue.Add(new TestItem(0));
            queue.AllWorkCompleted += new EventHandler(addNewItem);
            Thread.Sleep(2000);
         }

        private int count = 1;

        private void addNewItem(object sender, EventArgs e)
        {
            queue.Add(new TestItem(count));
            count++;
        }

    }


   
}
