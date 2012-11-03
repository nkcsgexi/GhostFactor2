using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using warnings.configuration;
using warnings.source;
using warnings.source.history;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class HistoryRecordTests
    {
        private readonly Logger logger;

        public HistoryRecordTests()
        {
            logger = NLoggerUtil.GetNLogger(typeof (HistoryRecordTests));
        }



        [TestMethod]
        public void addSingleRecord()
        {
            ICodeHistory history = CodeHistory.GetInstance();
            string source = TestUtil.GenerateRandomString(50);
            history.AddRecord("test", source);
            Assert.IsTrue(history.HasRecord("test"));
            ICodeHistoryRecord record = history.GetLatestRecord("test");
            Assert.IsFalse(record.HasPreviousRecord());
            Assert.IsTrue(record.GetSource().Equals(source));
        }

        [TestMethod]
        public void addTwoRecords()
        {
            ICodeHistory history = CodeHistory.GetInstance();
            String[] source = new string[2];
            source[0] = TestUtil.GenerateRandomString(50);
            source[1] = TestUtil.GenerateRandomString(50);
            history.AddRecord("test", source[0]);
            Assert.IsTrue(history.HasRecord("test"));
            history.AddRecord("test", source[1]);
            Assert.IsTrue(history.HasRecord("test"));
            ICodeHistoryRecord record = history.GetLatestRecord("test");
            Assert.IsTrue(record.HasPreviousRecord());
            Assert.IsTrue(record.GetSource().Equals(source[1]));
            record = record.GetPreviousRecord();
            Assert.IsFalse(record.HasPreviousRecord());
            Assert.IsTrue(record.GetSource().Equals(source[0]));
        }


        [TestMethod]
        public void addMultipleRecords()
        {
            int count = 50;
            int sourceLength = 1000;
            ICodeHistory history = CodeHistory.GetInstance();
            string[] source = new string[count];
            for(int i = 0; i < count; i++)
            {
                source[i] = TestUtil.GenerateRandomString(sourceLength);
                history.AddRecord("test", source[i]);
            }
            Assert.IsTrue(history.HasRecord("test"));
            ICodeHistoryRecord record = history.GetLatestRecord("test");
            for(int i = count - 1; i > 0 ; i --)
            {
                Assert.IsNotNull(record);
                Assert.IsTrue(record.GetUniqueName().Equals("test"));
                if(!record.GetSource().Equals(source[i]))
                {
                    logger.Fatal(i);
                    logger.Fatal(record.GetSource());
                    logger.Fatal(source[i]);
                }
                Assert.IsTrue(record.GetSource().Equals(source[i]));
                Assert.IsTrue(record.HasPreviousRecord());
                record = record.GetPreviousRecord();
            }
            Assert.IsFalse(record.HasPreviousRecord());
            Assert.IsTrue(record.GetSource().Equals(source[0]));
        }

        [TestMethod]
        public void addTwoDifferentFileRecords()
        {
            var history = CodeHistory.GetInstance();
            String[] source = new string[2];
            source[0] = TestUtil.GenerateRandomString(50);
            source[1] = TestUtil.GenerateRandomString(50);
            history.AddRecord("test", source[0]);
            history.AddRecord("test1", source[1]);
            Assert.IsTrue(history.HasRecord("test"));
            Assert.IsTrue(history.HasRecord("test1"));
            ICodeHistoryRecord record = history.GetLatestRecord("test");
            ICodeHistoryRecord record1 = history.GetLatestRecord("test1");
            Assert.IsFalse(record.HasPreviousRecord());
            Assert.IsFalse(record1.HasPreviousRecord());
            Assert.IsTrue(record.GetSource().Equals(source[0]));
            Assert.IsTrue(record1.GetSource().Equals(source[1]));
            Assert.IsTrue(record.GetUniqueName().Equals("test"));
            Assert.IsTrue(record1.GetUniqueName().Equals("test1"));
        }

        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsTrue(GlobalConfigurations.
                GetHistoryRecordsMaximumLength() == 20);
        }
    }
}
