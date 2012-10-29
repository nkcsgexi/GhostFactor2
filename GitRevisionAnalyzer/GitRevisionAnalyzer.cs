using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BlackHen.Threading;
using GitSharp;
using GitSharp.Commands;
using NLog;
using warnings.refactoring.detection;
using warnings.source.history;
using warnings.util;

namespace GitRevisionAnalyzer
{
    internal class GitRevisionAnalyzer
    {
        private static string INPUT_FILE_PATH = "input.txt";

        /* Work queue for handling all the work item, single thread. */
        private readonly WorkQueue queue;

        private bool finished { set; get; }

        private GitRevisionAnalyzer()
        {
            queue = new WorkQueue {ConcurrentLimit = 1};
            queue.AllWorkCompleted += AllWorkCompleted;
            finished = false;
        }

        private void AllWorkCompleted(object sender, EventArgs eventArgs)
        {
            finished = true;
        }

        private class RefactoringDetectionWorkItem : WorkItem
        {
            private readonly string gitHttp;
            private readonly Logger logger = NLoggerUtil.
                GetNLogger(typeof (GitRevisionAnalyzer));

            internal RefactoringDetectionWorkItem(string gitHttp)
            {
                this.gitHttp = gitHttp;
            }

            public override void Perform()
            {
                try
                {
                    logger.Info("start handling " + gitHttp);

                    // Create a project with the given url.
                    var project = new GitProject(gitHttp);

                    // Clone the project to local.
                    project.Clone();
                    logger.Info("clone finished.");

                    // Add commits to the code history, and get all the heads of this project history.
                    project.AddCommitsToCodeHistory(CodeHistory.GetInstance());
                    var records = project.GetHeadHitoryRecords(CodeHistory.GetInstance());
                    logger.Info("save history record finished. ");

                    // Create a detector, and start detecting from each head.
                    var detector = new RecordRefactoringDetector();

                    // To track the progress.
                    int totalRecordCount = records.Count();
                    int finishedCount = 0;
                    foreach (var record in records)
                    {
                        if (!ExcludeFile(record.GetFile()))
                        {
                            logger.Info("Start refactoring detection for " + record.GetFile() + " in " + gitHttp);
                            logger.Info("Finished count: " + finishedCount +"; Total count: "+ totalRecordCount);
                            detector.DetectRefactorings(record);
                        }
                        finishedCount ++;
                    }
                }catch(Exception e)
                {
                    logger.Fatal(e);
                }
            }

            private bool ExcludeFile(string file)
            {
                var excludedFiles = new[] {"AssemblyInfo.cs"};
                return excludedFiles.Contains(file);
            }
        }

        private void AddNewGitHttp(string gitHttp)
        {
            // Add job with the highest priority.
            queue.Add(new RefactoringDetectionWorkItem(gitHttp){Priority = ThreadPriority.Normal});
        }

        //git://github.com/nkcsgexi/ghostfactor1.git
        static void Main(string[] args)
        {
            var analyzer = new GitRevisionAnalyzer();
            var urls = FileUtil.ReadFileLines(INPUT_FILE_PATH);
            foreach (var url in urls)
            {
                analyzer.AddNewGitHttp(url);
            }

            while(false == analyzer.finished)
                Thread.Sleep(10000);
        }
    }
}
