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
    /* Refactoring input that shall be feed in to the checker. */
    public interface IManualRefactoring : IHasRefactoringType
    {
        string ToString();

        // Map the refactoring a new pair of document, whose code are identical to the 
        // original sources from where the refactoring is detected. 
        void MapToDocuments(IDocument before, IDocument after);
    }



    /* public interface for communicateing a manual extract method refactoring.*/
    public interface IManualExtractMethodRefactoring : IManualRefactoring
    {
        /* Method declaration node of the extracted method. */
        SyntaxNode ExtractedMethodDeclaration { get; }

        /* Method invocation node where the extracted method is invoked. */
        SyntaxNode ExtractMethodInvocation { get; }

        /* Statements to extract in the original code. */
        IEnumerable<SyntaxNode> ExtractedStatements { get; }

        /* Expression to extract in the original code. */
        SyntaxNode ExtractedExpression { get; }

    }
    
    /* Describing a simply detected extract method refactoring. */
    public interface ISimpleExtractMethodRefactoring : IManualRefactoring
    {
        SyntaxNode callerBefore { get; }
        SyntaxNode callerAfter { get; }
        SyntaxNode addedMethod { get; }
    }

    /* public interface for communicating a manual rename refactoring. */
    public interface IManualRenameRefactoring : IManualRefactoring
    {

    }

    /* public interface for a manual change method signature refactoring. */
    public interface IChangeMethodSignatureRefactoring : IManualRefactoring
    {
        /* New method declaration after the signature is updated. */
        SyntaxNode ChangedMethodDeclaration { get; }

        /* Parameters' map from previous version to new version. */
        List<Tuple<int, int>> ParametersMap { get; }
    }
  

    /* Interface used to represent a manual inline method refactoring. */
    public interface IInlineMethodRefactoring : IManualRefactoring
    {
        SyntaxNode CallerMethodBefore { get; }
        SyntaxNode CallerMethodAfter { get; }
        SyntaxNode InlinedMethod { get; }
        SyntaxNode InlinedMethodInvocation { get; }
        IEnumerable<SyntaxNode> InlinedStatementsInMethodAfter { get; }
    }

    /* Interface used for describing a simply detected inline method refactoring. */
    public interface ISimpleInlineMethodRefactoring: IManualRefactoring
    {
        SyntaxNode callerBefore { get; }
        SyntaxNode callerAfter { get; }
        SyntaxNode methodRemoved { get; }
    }
}
