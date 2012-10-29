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
        String getPreviousMetaPath();
        String getNameSpace();
        String getSolution();
        String getFile();
        String getSourcePath();
        String getMetaDataPath();
        long getTime();
    }
    internal class RecordMetaData : IRecordMetaData
    {
        private static readonly int RECORD_COUNT = 6;
        public static readonly String ROOT = "MetaData";
        private static readonly String EXTENSION = ".met";

        private readonly String sourePath;
        private readonly String previousMetaPath;
        private readonly String nameSpace;
        private readonly String solution;
        private readonly String file;
        private readonly long time;

        private readonly String metaDataPath;

        public static IRecordMetaData createMetaData(String solution, String nameSpace, String file,
                                                     String sourcePath, String previousMetaPath, long time)
        {
            String metaDataPath = ROOT + Path.DirectorySeparatorChar + file + time + EXTENSION;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(sourcePath);
            sb.AppendLine(previousMetaPath);
            sb.AppendLine(nameSpace);
            sb.AppendLine(solution);
            sb.AppendLine(file);
            sb.AppendLine(Convert.ToString(time));
            FileUtil.WriteToFileStream(FileUtil.CreateFile(metaDataPath), sb.ToString());
            return new RecordMetaData(solution, nameSpace, file, sourcePath, previousMetaPath, time, metaDataPath);
        }

        public static RecordMetaData readMetaData(String metaDataPath)
        {
            String[] lines = FileUtil.ReadFileLines(metaDataPath, 0, RECORD_COUNT - 1);
            String sourcePath = lines[0];
            String previousMetaPath = lines[1];
            String nameSpace = lines[2];
            String solution = lines[3];
            String file = lines[4];
            long time = Convert.ToInt64(lines[5]);
            return new RecordMetaData(solution, nameSpace, file, sourcePath, previousMetaPath, time, metaDataPath);
        }

        private RecordMetaData(String solution, String nameSpace, String file, String sourePath,
                               String previousMetaPath, long time, String metaDataPath)
        {
            this.sourePath = sourePath;
            this.previousMetaPath = previousMetaPath;
            this.nameSpace = nameSpace;
            this.solution = solution;
            this.file = file;
            this.time = time;
            this.metaDataPath = metaDataPath;

        }

        public string getSourcePath()
        {
            return sourePath;
        }

        public string getMetaDataPath()
        {
            return metaDataPath;
        }

        public string getPreviousMetaPath()
        {
            return previousMetaPath;
        }

        public string getNameSpace()
        {
            return nameSpace;
        }

        public string getSolution()
        {
            return solution;
        }

        public string getFile()
        {
            return file;
        }

        public long getTime()
        {
            return time;
        }
    }
}
