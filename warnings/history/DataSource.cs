using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using warnings.util;

namespace warnings.source.history
{
    public partial class CodeHistory
    {
        internal interface IDataSource
        {
            string ReadData(string path);
            void WriteData(string path, string data);
        }

        private static class DataSourceFactory
        {
            public static IDataSource GetMemoryDataSource()
            {
                return new MemoryDataSource();
            }

            public static IDataSource GetDiskDataSource()
            {
                return new DiskDataSource();
            }

            private class MemoryDataSource : IDataSource
            {
                private string data;

                public string ReadData(string path)
                {
                    return data;
                }

                public void WriteData(string path, string data)
                {
                    this.data = data;
                }
            }

            private class DiskDataSource : IDataSource
            {
                public string ReadData(string path)
                {
                    return FileUtil.ReadAllText(path);
                }

                public void WriteData(string path, string data)
                {
                    var fs = FileUtil.CreateFile(path);
                    FileUtil.WriteToFileStream(fs, data);
                }
            }
        }
    }
}
