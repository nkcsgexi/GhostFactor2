using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using warnings.source;
using warnings.source.history;

namespace WarningTest
{
    [TestClass]
    public class HistoryRecordTests
    {
        [TestMethod]
        public void addSingleRecord()
        {
            ICodeHistory history = CodeHistory.GetInstance();
            string source = TestUtil.generateRandomString(50);
            history.addRecord("test", "test", "test", source);
            Assert.IsTrue(history.hasRecord("test", "test", "test"));
            ICodeHistoryRecord record = history.GetLatestRecord("test", "test", "test");
            Assert.IsFalse(record.HasPreviousRecord());
            Assert.IsTrue(record.GetSource().Equals(source));
        }

        [TestMethod]
        public void addTwoRecords()
        {
            ICodeHistory history = CodeHistory.GetInstance();
            String[] source = new string[2];
            source[0] = TestUtil.generateRandomString(50);
            source[1] = TestUtil.generateRandomString(50);
            history.addRecord("test", "test", "test", source[0]);
            Assert.IsTrue(history.hasRecord("test","test","test"));
            history.addRecord("test", "test", "test", source[1]);
            Assert.IsTrue(history.hasRecord("test", "test", "test"));
            ICodeHistoryRecord record = history.GetLatestRecord("test", "test", "test");
            Assert.IsTrue(record.HasPreviousRecord());
            Assert.IsTrue(record.GetSource().Equals(source[1]));
            record = record.GetPreviousRecord();
            Assert.IsFalse(record.HasPreviousRecord());
            Assert.IsTrue(record.GetSource().Equals(source[0]));
        }


        [TestMethod]
        public void addMultipleRecords()
        {
            int count = 1000;
            int sourceLength = 1000;
            ICodeHistory history = CodeHistory.GetInstance();
            string[] source = new string[count];
            for(int i = 0; i < count; i++)
            {
                source[i] = TestUtil.generateRandomString(sourceLength);
                history.addRecord("test", "test", "test", source[i]);
            }
            Assert.IsTrue(history.hasRecord("test", "test", "test"));
            ICodeHistoryRecord record = history.GetLatestRecord("test", "test", "test");
            for(int i = count - 1; i > 0 ; i --)
            {
                Assert.IsNotNull(record);
                Assert.IsTrue(record.GetSolution().Equals("test"));
                Assert.IsTrue(record.GetNameSpace().Equals("test"));
                Assert.IsTrue(record.GetFile().Equals("test"));
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
            ICodeHistory history = CodeHistory.GetInstance();
            String[] source = new string[2];
            source[0] = TestUtil.generateRandomString(50);
            source[1] = TestUtil.generateRandomString(50);
            history.addRecord("test","test", "test", source[0]);
            history.addRecord("test","test", "test1", source[1]);
            Assert.IsTrue(history.hasRecord("test", "test", "test"));
            Assert.IsTrue(history.hasRecord("test", "test", "test1"));
            ICodeHistoryRecord record = history.GetLatestRecord("test", "test", "test");
            ICodeHistoryRecord record1 = history.GetLatestRecord("test", "test", "test1");
            Assert.IsFalse(record.HasPreviousRecord());
            Assert.IsFalse(record1.HasPreviousRecord());
            Assert.IsTrue(record.GetSource().Equals(source[0]));
            Assert.IsTrue(record1.GetSource().Equals(source[1]));
            Assert.IsTrue(record.GetFile().Equals("test"));
            Assert.IsTrue(record1.GetFile().Equals("test1"));
        }
    }
}
