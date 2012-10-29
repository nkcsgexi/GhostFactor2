using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using warnings.util;

namespace warnings.source.history
{
    public interface IRecordMetaData
    {
        String GetPreviousMetaPath();
        String GetUniqueName();
        String GetSourcePath();
        String GetMetaDataPath();
        long GetTime();
    }
    internal class RecordMetaData : IRecordMetaData
    {
        public static readonly String ROOT = "MetaData";

        private static readonly int RECORD_COUNT = 4;
        private static readonly String EXTENSION = ".met";

        private readonly String uniqueName;
        private readonly String sourePath;
        private readonly String previousMetaPath;
        private readonly long time;
        private readonly String metaDataPath;

        public static IRecordMetaData CreateMetaData(String uniqueName, String sourcePath, String previousMetaPath, long time)
        {
            String metaDataPath = ROOT + Path.DirectorySeparatorChar + time + EXTENSION;
            var sb = new StringBuilder();
            sb.AppendLine(sourcePath);
            sb.AppendLine(previousMetaPath);
            sb.AppendLine(uniqueName);
            sb.AppendLine(Convert.ToString(time));
            FileUtil.WriteToFileStream(FileUtil.CreateFile(metaDataPath), sb.ToString());
            return new RecordMetaData(uniqueName, sourcePath, previousMetaPath, time, metaDataPath);
        }

        public static RecordMetaData ReadMetaData(String metaDataPath)
        {
            String[] lines = FileUtil.ReadFileLines(metaDataPath, 0, RECORD_COUNT - 1);
            String sourcePath = lines[0];
            String previousMetaPath = lines[1];
            String uniqueName = lines[2];
            long time = Convert.ToInt64(lines[3]);
            return new RecordMetaData(uniqueName, sourcePath, previousMetaPath, time, metaDataPath);
        }

        private RecordMetaData(String uniqueName, String sourePath,
                               String previousMetaPath, long time, String metaDataPath)
        {
            this.uniqueName = uniqueName;
            this.sourePath = sourePath;
            this.previousMetaPath = previousMetaPath;
            this.time = time;
            this.metaDataPath = metaDataPath;
        }

        public string GetUniqueName()
        {
            return uniqueName;
        }

        public string GetSourcePath()
        {
            return sourePath;
        }

        public string GetMetaDataPath()
        {
            return metaDataPath;
        }

        public string GetPreviousMetaPath()
        {
            return previousMetaPath;
        }

        public long GetTime()
        {
            return time;
        }
    }
}
