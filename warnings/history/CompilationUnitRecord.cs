using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.components;
using warnings.util;

namespace warnings.source.history
{
    public partial class CodeHistory
    {
        internal partial class CompilationUnitRecord : ICodeHistoryRecord
        {
            public static readonly String ROOT = "Source";
            private static readonly String EXTENSION = ".rec";
            
            private readonly String uniqueName;
            private readonly String sourePath;
            private readonly long time;

            private ICodeHistoryRecord previousRecord;
            private IDataSource dataSource;

            public static ICodeHistoryRecord CreateNewCodeRecord(String uniqueName,
                                                                 String source)
            {
                // current time in ticks
                long time = DateTime.Now.Ticks;

                // record file name
                string recordfilename = time + EXTENSION;
                string sourcePath = ROOT + Path.DirectorySeparatorChar + recordfilename;

                var dataSource = DataSourceFactory.GetMemoryDataSource();
                dataSource.WriteData(sourcePath, source);
                return new CompilationUnitRecord(uniqueName, sourcePath,
                    time, dataSource, null);
            }

            public string GetUniqueName()
            {
                return uniqueName;
            }

            public string GetSimpleName()
            {
                return Path.GetFileName(GetUniqueName());
            }

            public string GetSource()
            {
                return dataSource.ReadData(sourePath);
            }

            public long GetTime()
            {
                return time;
            }

            public bool HasPreviousRecord()
            {
                return previousRecord != null;
            }

            public ICodeHistoryRecord GetPreviousRecord()
            {
                return previousRecord;
            }

            public ICodeHistoryRecord CreateNextRecord(string source)
            {
                var time = DateTime.Now.Ticks;
                var recordfilename = time + EXTENSION;
                var sourcePath = ROOT + Path.DirectorySeparatorChar + recordfilename;
                var dataSource = DataSourceFactory.GetMemoryDataSource();
                dataSource.WriteData(sourcePath, source);
                var record =  new CompilationUnitRecord(uniqueName, sourePath, time,
                    dataSource, this);
                PruneStaleRecords(record, GhostFactorComponents.configurationComponent.
                    GetHistoryRecordsMaximumLength());
                return record;
            }

            public IDocument Convert2Document()
            {
                var converter = new String2IDocumentConverter();
                return (IDocument) converter.Convert(GetSource(), null, null, null);
            }
            
            public bool Equals(ICodeHistoryRecord other)
            {
                return GetTime() == other.GetTime();
            }

            private CompilationUnitRecord(string uniqueName, string sourePath, long time,
                IDataSource dataSource, ICodeHistoryRecord previousRecord)
            {
                this.uniqueName = uniqueName;
                this.sourePath = sourePath;
                this.time = time;
                this.dataSource = dataSource;
                this.previousRecord = previousRecord;
            }

            private void PruneStaleRecords(CompilationUnitRecord record, int maxLength)
            {
                int length = 0;
                for (CompilationUnitRecord current = record; current.HasPreviousRecord(); 
                    current = (CompilationUnitRecord)current.GetPreviousRecord(), length ++)
                {
                    // If the current length equals the maximum length, then prone all
                    // the previous records.
                    if(length == maxLength)
                    {
                        current.previousRecord = null;
                        break;
                    }
                }
            }
        }
    }
}
