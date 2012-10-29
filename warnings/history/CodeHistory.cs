using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using NLog;
using warnings.util;

namespace warnings.source.history
{
    public interface ICodeHistory
    {
        void AddRecord(String uniqueName, String source);
        bool HasRecord(String uniqueName);
        ICodeHistoryRecord GetLatestRecord(String uniqueName);
    }

    public class CodeHistory : ICodeHistory
    {
        /* Singleton the code history instance. */
        private static CodeHistory instance = new CodeHistory();
        public static ICodeHistory GetInstance()
        {
            return instance;
        }

        /* Combined key and the latest record under that key. */
        private readonly Dictionary<String, ICodeHistoryRecord> latestRecordDictionary;

        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof (CodeHistory));

        private CodeHistory()
        {
            latestRecordDictionary = new Dictionary<String, ICodeHistoryRecord>();

            // Delete and recreate the folder for saving the source code record.
            FileUtil.DeleteDirectory(CompilationUnitRecord.ROOT);
            FileUtil.CreateDirectory(CompilationUnitRecord.ROOT);

            // Delete and recreate the folder for saving the metadata record.
            FileUtil.DeleteDirectory(RecordMetaData.ROOT);
            FileUtil.CreateDirectory(RecordMetaData.ROOT);
        }

        /* Add a new record, the latest record will be replaced.*/
        public void AddRecord(string uniqueName, string source)
        {
            if(HasRecord(uniqueName))
            {
                ICodeHistoryRecord record = GetLatestRecord(uniqueName);
                ICodeHistoryRecord nextRecord = record.CreateNextRecord(source);
                latestRecordDictionary.Remove(uniqueName);
                latestRecordDictionary.Add(uniqueName, nextRecord);
            }
            else
            {
                ICodeHistoryRecord record = CompilationUnitRecord.CreateNewCodeRecord(uniqueName, source);
                latestRecordDictionary.Add(uniqueName, record);
            }
        }

        public bool HasRecord(string uniqueName)
        {
            return latestRecordDictionary.ContainsKey(uniqueName);
        }

        public ICodeHistoryRecord GetLatestRecord(String uniqueName)
        {
            ICodeHistoryRecord record;
            if (!latestRecordDictionary.TryGetValue(uniqueName, out record))
                logger.Fatal("Try to get record that does not exist.");
            return record;
        }
    }
}
