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
        String GetSolution();
        String GetNameSpace();
        String GetFile();
        String GetSource();
        String GetKey();
        SyntaxTree GetSyntaxTree();
        long GetTime();
        bool HasPreviousRecord();
        ICodeHistoryRecord GetPreviousRecord();
        ICodeHistoryRecord CreateNextRecord(string source);
        IDocument Convert2Document();
        void Delete();
    }

    class CompilationUnitRecord : ICodeHistoryRecord
    {
        /* The metadata describing this souce version. */
        private readonly IRecordMetaData metaData;

        /* The root folder to where this file is stored. */
        public static readonly String ROOT = "Source";

        /* File extension for the source record. */
        private static readonly String EXTENSION = ".rec";

        public static ICodeHistoryRecord CreateNewCodeRecord(String solution, String package, String file,
                String source){

            // current time in ticks
            long time = DateTime.Now.Ticks;

            // record file name
            string recordfilename = file + time + EXTENSION;
            string sourcePath = ROOT + Path.DirectorySeparatorChar + recordfilename;
            FileUtil.WriteToFileStream(FileUtil.CreateFile(sourcePath), source);
            IRecordMetaData metaData =
                RecordMetaData.createMetaData(solution, package, file, sourcePath, "", time);
            return new CompilationUnitRecord(metaData);
        }


        public string GetSolution()
        {
            return metaData.getSolution();
        }

        public string GetNameSpace()
        {
            return metaData.getNameSpace();
        }

        public string GetFile()
        {
            return metaData.getFile();
        }

        public string GetSource()
        {
            return FileUtil.ReadAllText(metaData.getSourcePath());
        }

        public string GetKey()
        {
            return GetSolution() + GetNameSpace() + GetFile();
        }

        public SyntaxTree GetSyntaxTree()
        {
            throw new NotImplementedException();
        }

        public long GetTime()
        {
            return metaData.getTime();
        }

        public bool HasPreviousRecord()
        {
            return File.Exists(metaData.getPreviousMetaPath());
        }

        public ICodeHistoryRecord GetPreviousRecord()
        {      
            IRecordMetaData previousMetaData = RecordMetaData.readMetaData(metaData.getPreviousMetaPath());
            return new CompilationUnitRecord(previousMetaData);
        }

        public ICodeHistoryRecord CreateNextRecord(string source)
        {
            long time = DateTime.Now.Ticks;
            string recordfilename = metaData.getFile() + time + EXTENSION;
            string sourcePath = ROOT + Path.DirectorySeparatorChar + recordfilename;
            FileStream fs = FileUtil.CreateFile(sourcePath);
            FileUtil.WriteToFileStream(fs, source);
            IRecordMetaData nextMetaData =
                RecordMetaData.createMetaData(metaData.getSolution(), metaData.getNameSpace(), metaData.getFile(),
                    sourcePath, metaData.getMetaDataPath(), time);
            return new CompilationUnitRecord(nextMetaData);
        }

        /* Convert the source code to an IDocument instance. */
        public IDocument Convert2Document()
        {
            var converter = new String2IDocumentConverter();
            return (IDocument)converter.Convert(GetSource(), null, null, null);
        }

        public void Delete()
        {
            FileUtil.Delete(metaData.getSourcePath());
            FileUtil.Delete(metaData.getMetaDataPath());
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
