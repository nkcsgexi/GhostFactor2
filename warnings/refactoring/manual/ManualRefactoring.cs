using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.conditions;
using warnings.util;

namespace warnings.refactoring
{
    public class RefactoringMetaData
    {
        public DocumentId DocumentId { set; get; }
        public string DocumentUniqueName { get; set; }
    }

    /* Refactoring input that shall be feed in to the checker. */
    public abstract class ManualRefactoring : IHasRefactoringType
    {
        public IDocument BeforeDocument { get; private set; }
        public IDocument AfterDocument { get; private set; }

        protected ManualRefactoring(IDocument BeforeDocument, IDocument AfterDocument)
        {
            MetaData = new RefactoringMetaData();
            this.BeforeDocument = BeforeDocument;
            this.AfterDocument = AfterDocument;
        }

        public RefactoringMetaData MetaData { set; get; }
         
        // Map the refactoring a new pair of document, whose code are identical to the 
        // original sources from where the refactoring is detected. 
        public abstract void MapToDocuments(IDocument before, IDocument after);
        public abstract RefactoringType RefactoringType { get; }
    }



    /* public interface for communicateing a manual extract method refactoring.*/
    public abstract class IManualExtractMethodRefactoring : ManualRefactoring
    {
        /* Method declaration node of the extracted method. */
        protected IManualExtractMethodRefactoring(IDocument BeforeDocument, 
            IDocument AfterDocument) : base(BeforeDocument, AfterDocument)
        {
        }

        public SyntaxNode ExtractedMethodDeclaration { get; protected set; }

        /* Method invocation node where the extracted method is invoked. */
        public SyntaxNode ExtractMethodInvocation { get; protected set; }

        /* Statements to extract in the original code. */
        public IEnumerable<SyntaxNode> ExtractedStatements { get; protected set; }

        /* Expression to extract in the original code. */
        public SyntaxNode ExtractedExpression { get; protected set; }

    }
    
    /* Describing a simply detected extract method refactoring. */
    public abstract class ISimpleExtractMethodRefactoring : ManualRefactoring
    {
        protected ISimpleExtractMethodRefactoring(IDocument BeforeDocument, 
            IDocument AfterDocument) : base(BeforeDocument, AfterDocument)
        {
        }

        public SyntaxNode callerBefore { get; protected set; }
        public SyntaxNode callerAfter { get; protected set; }
        public SyntaxNode addedMethod { get; protected set; }
    }

    /* public interface for communicating a manual rename refactoring. */
    public abstract class IManualRenameRefactoring : ManualRefactoring
    {
        protected IManualRenameRefactoring(IDocument BeforeDocument, 
            IDocument AfterDocument) : base(BeforeDocument, AfterDocument)
        {
        }
    }

    /* public interface for a manual change method signature refactoring. */
    public abstract class IChangeMethodSignatureRefactoring : ManualRefactoring
    {
 
        protected IChangeMethodSignatureRefactoring(IDocument BeforeDocument, 
            IDocument AfterDocument) : base(BeforeDocument, AfterDocument)
        {
        }
        /* New method declaration after the signature is updated. */
        public SyntaxNode ChangedMethodDeclaration { get; protected set; }

        /* Parameters' map from previous version to new version. */
        public List<Tuple<int, int>> ParametersMap { get; protected set; }
    }
  

    /* Interface used to represent a manual inline method refactoring. */
    public abstract class IInlineMethodRefactoring : ManualRefactoring
    {
        protected IInlineMethodRefactoring(IDocument BeforeDocument, 
            IDocument AfterDocument) : base(BeforeDocument, AfterDocument)
        {
        }

        public SyntaxNode CallerMethodBefore { get; protected set; }
        public SyntaxNode CallerMethodAfter { get; protected set; }
        public SyntaxNode InlinedMethod { get; protected set; }
        public SyntaxNode InlinedMethodInvocation { get; protected set; }
        public IEnumerable<SyntaxNode> InlinedStatementsInMethodAfter { get; protected set; }
    }

    /* Interface used for describing a simply detected inline method refactoring. */
    public abstract class ISimpleInlineMethodRefactoring : ManualRefactoring
    {
        protected ISimpleInlineMethodRefactoring(IDocument BeforeDocument, 
            IDocument AfterDocument) : base(BeforeDocument, AfterDocument)
        {
        }

        public SyntaxNode callerBefore { get; protected set; }
        public SyntaxNode callerAfter { get; protected set; }
        public SyntaxNode methodRemoved { get; protected set; }
    }
}
