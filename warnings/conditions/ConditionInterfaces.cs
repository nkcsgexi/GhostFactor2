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
        ICodeIssueComputer CheckCondition(IDocument before, IDocument after, ManualRefactoring input);  
    }

    /* interface that containing checkings for all the conditions of a refactoring RefactoringType. */
    public interface IRefactoringConditionsList : IHasRefactoringType
    {
        IEnumerable<ICodeIssueComputer> CheckAllConditions(IDocument before, IDocument after, ManualRefactoring input);
        int GetCheckerCount();
    }

    /* Refactoring conditions for a specific refactoring RefactoringType is stored in.*/
    public abstract class RefactoringConditionsList : IRefactoringConditionsList
    {
        /* suppose to return all the condition checkers for this specific refactoring. */
        public IEnumerable<ICodeIssueComputer> CheckAllConditions(IDocument before, IDocument after, ManualRefactoring input)
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
        bool IsDocumentCorrect(IDocument document);
        IEnumerable<IDocument> GetPossibleDocuments(ISolution solution);
        IEnumerable<SyntaxNode> GetPossibleSyntaxNodes(IDocument document);
        IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node);
    }

    /* The null code issue computer return no code issue at any time. */
    public class NullCodeIssueComputer : ICodeIssueComputer
    {
        public bool IsDocumentCorrect(IDocument document)
        {
            return false;
        }

        public IEnumerable<IDocument> GetPossibleDocuments(ISolution solution)
        {
            return Enumerable.Empty<IDocument>();
        }

        public IEnumerable<SyntaxNode> GetPossibleSyntaxNodes(IDocument document)
        {
            return Enumerable.Empty<SyntaxNode>();
        }

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
        public abstract bool IsDocumentCorrect(IDocument document);
        public abstract IEnumerable<IDocument> GetPossibleDocuments(ISolution solution); 
        public abstract IEnumerable<SyntaxNode> GetPossibleSyntaxNodes(IDocument document);
        public abstract IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node);
    }

    internal abstract class SingleDocumentValidCodeIssueComputer : ValidCodeIssueComputer
    {
        private readonly RefactoringMetaData metaData;

        protected SingleDocumentValidCodeIssueComputer(RefactoringMetaData metaData)
        {
            this.metaData = metaData;
        }

        public sealed override bool IsDocumentCorrect(IDocument document)
        {
            return document.Id.Equals(metaData.DocumentId);
        }

        /// <summary>
        /// Get the possible documents where this issue lies, in this case, only return the 
        /// document associated with the known documentId.
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        public sealed override IEnumerable<IDocument> GetPossibleDocuments(ISolution solution)
        {
            return new[] {solution.GetDocument(metaData.DocumentId)};
        }

        /// <summary>
        /// Utility used to decide whether two computers are applied to the same document.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        protected bool IsIssuedToSameDocument(ICodeIssueComputer a)
        {
            var another = a as SingleDocumentValidCodeIssueComputer;
            if (another != null)
            {
                return metaData.DocumentId.Equals(another.metaData.DocumentId);
            }
            return false;
        }
    }
}
