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
        void addRecord(String solution, String nameSpace, String file, String source);
        bool hasRecord(String solution, String nameSpace, String file);
        ICodeHistoryRecord GetLatestRecord(string solution, string nameSpace, string file);
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

        private String combineKey(String solution, String nameSpace, String file)
        {
            return solution + nameSpace + file;
        }

        /* Add a new record, the latest record will be replaced.*/
        public void addRecord(string solution, string nameSpace, string file, string source)
        {
            String key = combineKey(solution, nameSpace, file);
            if(hasRecord(solution, nameSpace, file))
            {
                ICodeHistoryRecord record = GetLatestRecord(solution, nameSpace, file);
                ICodeHistoryRecord nextRecord = record.CreateNextRecord(source);
                latestRecordDictionary.Remove(key);
                latestRecordDictionary.Add(key, nextRecord);
            }
            else
            {
                ICodeHistoryRecord record = CompilationUnitRecord.CreateNewCodeRecord(solution, nameSpace, file, source);
                latestRecordDictionary.Add(key, record);
            }
        }

        public bool hasRecord(string solution, string nameSpace, string file)
        {
            String key = combineKey(solution, nameSpace, file);
            return latestRecordDictionary.ContainsKey(key);
        }

        public ICodeHistoryRecord GetLatestRecord(string solution, string nameSpace, string file)
        {
            Contract.Requires(hasRecord(solution, nameSpace, file));
            String key = combineKey(solution, nameSpace, file);
            ICodeHistoryRecord record;
            if(!latestRecordDictionary.TryGetValue(key, out record))
                logger.Fatal("Try to get record that does not exist.");
            return record;
        }
    }
}
