using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using warnings.components;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class ComponentTimerTests
    {
        private Logger log = NLoggerUtil.GetNLogger(typeof (ComponentTimerTests));

        [TestMethod]
        public void TestMethod1()
        {
            var timer = new ComponentTimer(100, handler);
            timer.start();
            Thread.Sleep(2000);
        }

        private int i = 0;

        void handler(object handler, EventArgs arg)
        {
            log.Info(i ++);
        }
    }
}
