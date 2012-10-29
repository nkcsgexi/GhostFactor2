using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using warnings.quickfix;
using warnings.refactoring;

namespace warnings.conditions
{
    /* The interface that can be queried about refactoring RefactoringType. */
    public interface IHasRefactoringType
    {
        RefactoringType RefactoringType { get; }
    }

    /* All refactoring conditions should be derived from this interface. */
    public interface IRefactoringConditionChecker : IHasRefactoringType
    {
        ICodeIssueComputer CheckCondition(IDocument before, IDocument after, IManualRefactoring input);  
    }

    /* interface that containing checkings for all the conditions of a refactoring RefactoringType. */
    public interface IRefactoringConditionsList : IHasRefactoringType
    {
        IEnumerable<ICodeIssueComputer> CheckAllConditions(IDocument before, IDocument after, IManualRefactoring input);
        int GetCheckerCount();
    }

    /* Refactoring conditions for a specific refactoring RefactoringType is stored in.*/
    public abstract class RefactoringConditionsList : IRefactoringConditionsList
    {
        /* suppose to return all the condition checkers for this specific refactoring. */
        public IEnumerable<ICodeIssueComputer> CheckAllConditions(IDocument before, IDocument after, IManualRefactoring input)
        {
            var results = new List<ICodeIssueComputer>();
            
            // Check all conditions, and push the results into the list.
            results.AddRange(GetAllConditionCheckers().Select(checker => checker.CheckCondition(before, after, input)));
            return results.AsEnumerable();
        }

        public int GetCheckerCount()
        {
            return GetAllConditionCheckers().Count();
        }

        protected abstract IEnumerable<IRefactoringConditionChecker> GetAllConditionCheckers();
        public abstract RefactoringType RefactoringType { get; }
    }

    /* This interface is used returning values for condition checkers. It is a convenient way of computing code issues. */
    public interface ICodeIssueComputer : IEquatable<ICodeIssueComputer>, IHasRefactoringType
    {
        IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node);
    }

    /* The null code issue computer return no code issue at any time. */
    public class NullCodeIssueComputer : ICodeIssueComputer
    {
        public IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node)
        {
            return Enumerable.Empty<CodeIssue>();
        }

        public bool Equals(ICodeIssueComputer other)
        {
            return other is NullCodeIssueComputer;
        }

        public RefactoringType RefactoringType
        {
            get { return RefactoringType.UNKOWN; }
        }
    }

    /* Any code issue computers that really have content shall derive from this abstract class. */
    public abstract class ValidCodeIssueComputer : ICodeIssueComputer
    {
        public abstract bool Equals(ICodeIssueComputer other);
        public abstract RefactoringType RefactoringType { get; }
        public abstract IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node);
    }
}
