using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.detection;
using warnings.source;
using warnings.refactoring;

namespace warnings.refactoring.detection
{
    public interface IRefactoringDetector
    {
        Boolean HasRefactoring();
        IEnumerable<ManualRefactoring> GetRefactorings();
    }

    public interface IBeforeAndAfterSourceKeeper
    {
        void SetSourceBefore(String source);
        string GetSourceBefore();
        void SetSourceAfter(String source);
        string GetSourceAfter();
    }

    public interface IBeforeAndAfterSyntaxTreeKeeper
    {
        void SetSyntaxTreeBefore(SyntaxTree before);
        void SetSyntaxTreeAfter(SyntaxTree after);
    }

    public interface IBeforeAndAfterSyntaxNodeKeeper
    {
        void SetSyntaxNodeBefore(SyntaxNode before);
        void SetSyntaxNodeAfter(SyntaxNode after);
    }

    public interface IExternalRefactoringDetector : IRefactoringDetector, IBeforeAndAfterSourceKeeper
    {
        IDocument GetBeforeDocument();
        IDocument GetAfterDocument();
    }

    internal interface IInternalRefactoringDetector : IRefactoringDetector, IBeforeAndAfterSyntaxNodeKeeper
    {
        
    }


    public static class RefactoringDetectorFactory
    {
        public static IExternalRefactoringDetector CreateRenameDetector()
        {
            return new RenameDetector();
        }

        public static IExternalRefactoringDetector CreateExtractMethodDetectorBasedOnStringDistances()
        {
            return new ExtractMethodDetector(InMethodExtractMethodDetectorFactory.GetInMethodExtractMethodDetectorByStringDistances());
        }

        public static IExternalRefactoringDetector CreateExtractMethodDetectorBasedOnCommonStatements()
        {
            return new ExtractMethodDetector(InMethodExtractMethodDetectorFactory.GetInMethodExtractMethodDetectorByCommonStatements());
        }

        public static IExternalRefactoringDetector CreateDummyExtractMethodDetector()
        {
            return new SimpleExtractMethodDetector();
        }

        public static IExternalRefactoringDetector CreateChangeMethodSignatureDetector()
        {
            return  new ChangeMethodSignatureDetector();
        }

        public static IExternalRefactoringDetector CreateInlineMethodDetector()
        {
            return new InlineMethodDetector(InMethodInlineDetectorFactory.GetFineGrainedDetector());
        }

        public static IExternalRefactoringDetector CreateDummyInlineMethodDetector()
        {
            return new InlineMethodDetector(InMethodInlineDetectorFactory.GetDummyDetector());
        }
    }
}
