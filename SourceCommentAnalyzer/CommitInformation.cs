using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp;
using NLog;
using warnings.util;

namespace SourceCommentAnalyzer
{
    public class CommitInformation
    {
        private readonly string authorEmail;
        private readonly string committerEmail;
        private readonly DateTimeOffset commitDate;
        private readonly IInterestedContentExtractingStrategy strategy;

        private readonly List<SourceFileChange> sourceFileChanges;
        
        public CommitInformation(Commit commit, IInterestedContentExtractingStrategy strategy)
        {
            this.authorEmail = commit.Author.EmailAddress;
            this.committerEmail = commit.Committer.EmailAddress;
            this.commitDate = commit.CommitDate;
            this.strategy = strategy;
            this.sourceFileChanges = GetSourceChanges(commit, strategy);
        }

        private class SourceFileChange : ISourceFileChange
        {
            public string fileName { get; set; }
            public IEnumerable<string> interestedContents { get; set; }
        }

        private List<SourceFileChange> GetSourceChanges(Commit commit, 
            IInterestedContentExtractingStrategy strategy)
        {
            var sourceChanges = new List<SourceFileChange>();

            // Get the changes that are changing the C# source code.
            var changes = commit.Changes.Where(c => c.Name.EndsWith(".cs") &&
                c.ChangedObject.IsBlob);

            if (changes.Any())
            {
                // For each change in the found changes.
                foreach (var change in changes)
                {
                    // Get the file name of where the change is performed on.
                    string fileName = change.Name;

                    // Get the source after change.
                    string source = ((Blob)change.ChangedObject).Data;

                    // Get the interested content from the source code.
                    var interestedContent = strategy.GetInterestedContent(source);
                    if (strategy.HasInterestedContent(interestedContent))
                    {
                        sourceChanges.Add(new SourceFileChange()
                        {
                            fileName = fileName,
                            interestedContents = strategy.GetInterestedContent(source)
                        });
                    }
                }
            }
            return sourceChanges;
        }

        public string GetAuthorEmail()
        {
            return authorEmail;
        }

        public string GetCommitterEmail()
        {
            return committerEmail;
        }

        public DateTimeOffset GetCommitDate()
        {
            return commitDate;
        }

        public IEnumerable<ISourceFileChange> GetAllSourceCodeChanges()
        {
            return sourceFileChanges;
        }

        /// <summary>
        /// Return all the source changes that have interested content.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ISourceFileChange> GetSourceCodeChangesWithInterestedContent()
        {
            return sourceFileChanges.Where(c => strategy.HasInterestedContent(c.interestedContents));
        }


        public bool HasInterestedContent()
        {
            return sourceFileChanges.Any();
        }

        public string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Author: " + authorEmail);
            sb.AppendLine("Committer: " + committerEmail);
            sb.AppendLine("Date: " + GetCommitDate().ToString());

            foreach (var fileChange in sourceFileChanges)
            {
                sb.AppendLine(fileChange.fileName);
                sb.AppendLine(strategy.DumpInformation(fileChange.interestedContents));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Remove some interested content by the given file name and contents.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="contents"></param>
        public void RemoveInterestedContent(string fileName, IEnumerable<string> contents)
        {
            if(HasChangedFile(fileName))
            {
                var change = sourceFileChanges.First(c => c.fileName.Equals(fileName));
                change.interestedContents = change.interestedContents.Except(contents);
                if(change.interestedContents.Any() == false)
                {
                    sourceFileChanges.Remove(change);
                }
            }
        }
        
        /// <summary>
        /// Return whether this commit has changed a file with the given name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool HasChangedFile(string fileName)
        {
            return sourceFileChanges.Any(c => c.fileName.Equals(fileName));
        }
    }

    public interface IInterestedContentExtractingStrategy
    {
        IEnumerable<string> GetInterestedContent(string content);
        bool HasInterestedContent(IEnumerable<string> interestedContent);
        string DumpInformation(IEnumerable<string> interestedContent);
    }

    public interface ISourceFileChange
    {
        string fileName { get; }
        IEnumerable<string> interestedContents { get; }
    }
}
