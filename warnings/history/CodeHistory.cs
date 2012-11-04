using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Services;
using warnings.util;

namespace warnings.source.history
{
    public interface ICodeHistory
    {
        void AddRecord(String uniqueName, String source);
        bool HasRecord(String uniqueName);
        ICodeHistoryRecord GetLatestRecord(String uniqueName);
    }

    public interface ICodeHistoryRecord : IEquatable<ICodeHistoryRecord>
    {
        String GetUniqueName();
        String GetSimpleName();
        String GetSource();
        long GetTime();
        bool HasPreviousRecord();
        ICodeHistoryRecord GetPreviousRecord();
        ICodeHistoryRecord CreateNextRecord(string source);
        IDocument Convert2Document();
    }

  

    public partial class CodeHistory : ICodeHistory
    {
        /* Singleton the code history instance. */
        private static CodeHistory instance; 
            
        public static ICodeHistory GetInstance()
        {
            if(instance == null)
                instance = new CodeHistory();
            return instance;
        }

        /* Combined key and the latest record under that key. */
        private readonly Dictionary<String, ICodeHistoryRecord> latestRecordDictionary;

        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof (CodeHistory));

        private CodeHistory()
        {
            latestRecordDictionary = new Dictionary<String, ICodeHistoryRecord>();
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
                ICodeHistoryRecord record = CompilationUnitRecord.CreateNewCodeRecord(uniqueName, 
                    source);
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
