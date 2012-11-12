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
    /// <summary>
    /// Interface for those objects that has condition type associated.
    /// </summary>
    public interface IHasConditionType
    {
        RefactoringConditionType RefactoringConditionType { get; }
    }


    /* The interface that can be queried about refactoring RefactoringType. */
    public interface IHasRefactoringType
    {
        RefactoringType RefactoringType { get; }
    }

    /* All refactoring conditions should be derived from this interface. */
    public interface IRefactoringConditionChecker : IHasRefactoringType, IHasConditionType
    {
        IConditionCheckingResult CheckCondition(ManualRefactoring input);
        Predicate<SyntaxNode> GetIssuedNodeFilter();
    }

    /* interface that containing checkings for all the conditions of a refactoring RefactoringType. */
    public interface IRefactoringConditionsList : IHasRefactoringType
    {
        IEnumerable<IConditionCheckingResult> CheckAllConditions(ManualRefactoring input);
        IEnumerable<Predicate<SyntaxNode>> GetIssuedNodeFilters();
        int GetCheckerCount();
    }

    /// <summary>
    /// Refactoring conditions for a specific refactoring RefactoringType is stored in.
    /// </summary>
    public abstract class RefactoringConditionsList : IRefactoringConditionsList
    {
        /* suppose to return all the condition checkers for this specific refactoring. */
        public IEnumerable<IConditionCheckingResult> CheckAllConditions(ManualRefactoring input)
        {
            var results = new List<IConditionCheckingResult>();
            
            // Check all conditions, and push the results into the list.
            results.AddRange(GetAllConditionCheckers().Select(
                checker => checker.CheckCondition(input)));
            return results.AsEnumerable();
        }

        public IEnumerable<Predicate<SyntaxNode>> GetIssuedNodeFilters()
        {
           return GetAllConditionCheckers().Select(c => c.GetIssuedNodeFilter()).ToList();
        }

        public int GetCheckerCount()
        {
            return GetAllConditionCheckers().Count();
        }

        protected abstract IEnumerable<IRefactoringConditionChecker> GetAllConditionCheckers();
        public abstract RefactoringType RefactoringType { get; }
    }


    public interface IConditionCheckingResult : IHasRefactoringType, IHasConditionType
    {
        bool IsDocumentCorrect(IDocument document);  
    }


    /// <summary>
    /// This interface is used returning values for condition checkers. It is a convenient way of computing 
    /// code issues.
    /// </summary>
    public interface ICodeIssueComputer : IEquatable<ICodeIssueComputer>, IConditionCheckingResult
    {
        bool IsIssueResolved(ICorrectRefactoringResult correctRefactoringResult);
        IEnumerable<IDocument> GetPossibleDocuments(ISolution solution);
        IEnumerable<SyntaxNode> GetPossibleSyntaxNodes(IDocument document);
        IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node);
    }


    /// <summary>
    /// Other than returning condition violations, any condition checking can return a correct refactoring. 
    /// </summary>
    public interface ICorrectRefactoringResult : IConditionCheckingResult
    {
        ManualRefactoring refactoring { get; }
    }


    /// <summary>
    /// The null code issue computer return no code issue at any time.
    /// </summary>
    public class SingleDocumentCorrectRefactoringResult : ICorrectRefactoringResult
    {
        public ManualRefactoring refactoring { get; private set; }
        public RefactoringConditionType RefactoringConditionType { get; private set; }
        public RefactoringType RefactoringType { get; private set; }

        internal SingleDocumentCorrectRefactoringResult(ManualRefactoring refactoring,
            RefactoringConditionType RefactoringConditionType)
        {
            this.refactoring = refactoring;
            this.RefactoringType = refactoring.RefactoringType;
            this.RefactoringConditionType = RefactoringConditionType;
        }

        public bool IsDocumentCorrect(IDocument document)
        {
            return document.Id == refactoring.MetaData.DocumentId;
        }      
    }

    /// <summary>
    /// For some code issue computer, such as parameter checker for extract method. Developer may add 
    /// parameters manually, so later computer is used to replace the previous one. This is the interface for
    /// this kind of issue computers. 
    /// </summary>
    public interface IUpdatableCodeIssueComputer
    {
        bool IsUpdatedComputer(ICodeIssueComputer o);
    }

    public abstract class SingleDocumentValidCodeIssueComputer : ICodeIssueComputer
    {
        private readonly RefactoringMetaData metaData;

        protected SingleDocumentValidCodeIssueComputer(RefactoringMetaData metaData)
        {
            this.metaData = metaData;
        }

        public bool IsDocumentCorrect(IDocument document)
        {
            return document.Id.Equals(metaData.DocumentId);
        }

        /// <summary>
        /// Get the possible documents where this issue lies, in this case, only return the 
        /// document associated with the known documentId.
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        public IEnumerable<IDocument> GetPossibleDocuments(ISolution solution)
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

        public abstract bool Equals(ICodeIssueComputer other);
        public abstract RefactoringType RefactoringType { get; }
        public abstract RefactoringConditionType RefactoringConditionType { get; }
        public abstract IEnumerable<SyntaxNode> GetPossibleSyntaxNodes(IDocument document);
        public abstract IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node);
        public abstract bool IsIssueResolved(ICorrectRefactoringResult correctRefactoringResult);
    }
}
