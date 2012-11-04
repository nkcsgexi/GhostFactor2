using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace warnings.refactoring
{
    /* Continue the class of manual refactoring factory, this part create simply detected refactorings. */
    public partial class ManualRefactoringFactory
    {
        public static ISimpleExtractMethodRefactoring CreateSimpleExtractMethodRefactoring(
            IDocument before, IDocument after,
            SyntaxNode callerBefore,
            SyntaxNode callerAfter, SyntaxNode methodAdded)
        {
            return new SimpleExtractMethodRefactoring(before, after, callerBefore, 
                callerAfter, methodAdded);
        }

        public static ISimpleInlineMethodRefactoring CreateSimpleInlineMethodRefactoring(
            IDocument before, IDocument after, SyntaxNode callerBefore,
            SyntaxNode callerAfter, SyntaxNode methodRemoved)
        {
            return new SimpleInlineMethodRefactoring(before, after, callerBefore, 
                callerAfter, methodRemoved);
        }

        private class SimpleExtractMethodRefactoring : ISimpleExtractMethodRefactoring
        {
            internal SimpleExtractMethodRefactoring(IDocument before, IDocument after,
                SyntaxNode callerBefore, SyntaxNode callerAfter,
                SyntaxNode addedMethod) : base(before, after)
            {
                this.callerBefore = callerBefore;
                this.callerAfter = callerAfter;
                this.addedMethod = addedMethod;
            }


            public override RefactoringType RefactoringType
            {
                get { return RefactoringType.EXTRACT_METHOD; }
            }

            public override void MapToDocuments(IDocument before, IDocument after) { }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Simple extract method refactoring:");
                sb.AppendLine("Caller before:");
                sb.AppendLine(callerBefore.GetText());
                sb.AppendLine("Caller after:");
                sb.AppendLine(callerAfter.GetText());
                sb.AppendLine("Added method:");
                sb.AppendLine(addedMethod.GetText());
                return sb.ToString();
            }
        }

        private class SimpleInlineMethodRefactoring : ISimpleInlineMethodRefactoring
        {
            internal SimpleInlineMethodRefactoring(IDocument before, IDocument after, 
                SyntaxNode callerBefore, SyntaxNode callerAfter, 
                SyntaxNode methodRemoved) :base(before, after)
            {
                this.callerBefore = callerBefore;
                this.callerAfter = callerAfter;
                this.methodRemoved = methodRemoved;
            }

            public override RefactoringType RefactoringType
            {
                get { return RefactoringType.INLINE_METHOD; }
            }

            public override void MapToDocuments(IDocument before, IDocument after)
            {
                throw new NotImplementedException();
            }

            public string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Simple inline method refactoring:");
                sb.AppendLine("Caller before:");
                sb.AppendLine(callerBefore.GetText());
                sb.AppendLine("Caller after:");
                sb.AppendLine(callerAfter.GetText());
                sb.AppendLine("Removed method:");
                sb.AppendLine(methodRemoved.GetText());
                return sb.ToString();
            }
        }
    }
}
