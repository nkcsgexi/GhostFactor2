using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Services;
using Roslyn.Services.Editor;
using warnings.conditions;
using warnings.refactoring;
using warnings.util;

namespace warnings.components.ui
{
    /* The interface that describes the warnings shown in the list of refactoring warnings. */
    public interface IRefactoringWarningMessage : IHasRefactoringType
    {
        string File { get; }
        int Line { get; }
        string Description { get; }
        ICodeIssueComputer CodeIssueComputer { get; }
    }

    public class RefactoringWarningMessageFactory
    {
        public static IRefactoringWarningMessage CreateRefactoringWarningMessage(IDocument document, CodeIssue issue, ICodeIssueComputer computer)
        {
            var instance = new RefactoringWarningMessage
            {
                File = document.Name,
                Line = document.GetText().GetLineFromPosition(issue.TextSpan.Start).LineNumber,
                Description = issue.Description,
                CodeIssueComputer = computer
            };
            return instance;
        }

        private class RefactoringWarningMessage : IRefactoringWarningMessage
        {
            public string File { get; internal set; }
            public int Line { get; internal set; }
            public string Description { get; internal set; }
            public ICodeIssueComputer CodeIssueComputer { get; internal set; }

            public RefactoringType RefactoringType
            {
                get { return CodeIssueComputer.RefactoringType; }
            }

            public String ToString()
            {
                var converter = new RefactoringType2StringConverter();
                var type = (string) converter.Convert(RefactoringType, null, null, null);
                var sb = new StringBuilder();
                sb.AppendLine(File);
                sb.AppendLine(Line.ToString());
                sb.AppendLine(type);
                sb.AppendLine(Description);
                return sb.ToString();
            }

            internal RefactoringWarningMessage()
            {
            }
        }
    }

 
}
