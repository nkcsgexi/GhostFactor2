using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.util;

namespace warnings.source.history
{
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

    internal class CompilationUnitRecord : ICodeHistoryRecord
    {
        /* The root folder to where this file is stored. */
        public static readonly String ROOT = "Source";

        /* The metadata describing this souce version. */
        private readonly IRecordMetaData metaData;

        /* File extension for the source record. */
        private static readonly String EXTENSION = ".rec";

        public static ICodeHistoryRecord CreateNewCodeRecord(String uniqueName,
                String source){
            // current time in ticks
            long time = DateTime.Now.Ticks;

            // record file name
            string recordfilename = time + EXTENSION;
            string sourcePath = ROOT + Path.DirectorySeparatorChar + recordfilename;
            FileUtil.WriteToFileStream(FileUtil.CreateFile(sourcePath), source);
            IRecordMetaData metaData =
                RecordMetaData.CreateMetaData(uniqueName, sourcePath, "", time);
            return new CompilationUnitRecord(metaData);
        }

        public string GetUniqueName()
        {
            return metaData.GetUniqueName();
        }

        public string GetSimpleName()
        {
            return Path.GetFileName(GetUniqueName());
        }

        public string GetSource()
        {
            return FileUtil.ReadAllText(metaData.GetSourcePath());
        }

        public long GetTime()
        {
            return metaData.GetTime();
        }

        public bool HasPreviousRecord()
        {
            return File.Exists(metaData.GetPreviousMetaPath());
        }

        public ICodeHistoryRecord GetPreviousRecord()
        {      
            IRecordMetaData previousMetaData = RecordMetaData.ReadMetaData(metaData.GetPreviousMetaPath());
            return new CompilationUnitRecord(previousMetaData);
        }


        public ICodeHistoryRecord CreateNextRecord(string source)
        {
            var time = DateTime.Now.Ticks;
            var recordfilename = time + EXTENSION;
            var sourcePath = ROOT + Path.DirectorySeparatorChar + recordfilename;
            var fs = FileUtil.CreateFile(sourcePath);
            FileUtil.WriteToFileStream(fs, source);
            var nextMetaData =
                RecordMetaData.CreateMetaData(GetUniqueName(), sourcePath, metaData.GetMetaDataPath(), time);
            return new CompilationUnitRecord(nextMetaData);
        }

        /* Convert the source code to an IDocument instance. */
        public IDocument Convert2Document()
        {
            var converter = new String2IDocumentConverter();
            return (IDocument)converter.Convert(GetSource(), null, null, null);
        }

        private CompilationUnitRecord(IRecordMetaData metaData)
        {
            this.metaData = metaData;
        }

        public bool Equals(ICodeHistoryRecord other)
        {
            return GetTime() == other.GetTime();
        }
    }
}
