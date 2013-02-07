using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitRevisionAnalyzer;
using GitSharp;
using NLog;
using Roslyn.Compilers.CSharp;
using warnings.retriever;
using warnings.util;
using MySql.Data.MySqlClient;

namespace SourceCommentAnalyzer
{
    class Program
    {
        private static Logger logger = NLoggerUtil.GetNLogger(typeof(Program));

        static void Main(string[] args)
        {
           
            var project = new GitProject(@"git://github.com/hbons/SparkleShare.git");
            project.Clone();
            var infors = AnalyzeRevisionHistory(project.GetCurrentCommit(), new CommentStrategy());

            var operations = new MySqlOperation();

         //   operations.DropTable();
         //   operations.CreateTable();

            foreach (var infor in infors)
            {
                foreach (var change in infor.GetSourceCodeChangesWithInterestedContent())
                {
                    foreach (var content in change.interestedContents)
                    {
                        operations.InsertRecord(project.GetSourceFolder(), infor.GetCommitDate(),
                            infor.GetAuthorEmail(), infor.GetCommitterEmail(), change.fileName, content);
                    }
                }
            }

            operations.Close();

        }

        private static void RemoveDuplication(IEnumerable<CommitInformation> 
            informations)
        {
            var changeList = new List<ISourceFileChange>();
            foreach (var infor in informations)
            {
                foreach (var change in changeList)
                {
                    infor.RemoveInterestedContent(change.fileName, change.interestedContents);
                }
                changeList.AddRange(infor.GetAllSourceCodeChanges());
            }
        }

        private static IEnumerable<CommitInformation> AnalyzeRevisionHistory(Commit head, 
            IInterestedContentExtractingStrategy strategy)
        {
            var commitInfors = new List<CommitInformation>();

            // Translate every commit to a commit information and add them to the list.
            for (; head != null; head = head.Parent)
            {
                var infor = new CommitInformation(head, strategy);
                if(infor.HasInterestedContent())
                {
                    commitInfors.Insert(0, infor);
                };   
            }
            return commitInfors;
        }

    }
}
