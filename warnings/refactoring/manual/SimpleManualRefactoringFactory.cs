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
        public static ISimpleExtractMethodRefactoring CreateSimpleExtractMethodRefactoring(SyntaxNode callerBefore,
            SyntaxNode callerAfter, SyntaxNode methodAdded)
        {
            return new SimpleExtractMethodRefactoring(callerBefore, callerAfter, methodAdded);
        }

        public static ISimpleInlineMethodRefactoring CreateSimpleInlineMethodRefactoring(SyntaxNode callerBefore,
            SyntaxNode callerAfter, SyntaxNode methodRemoved)
        {
            return new SimpleInlineMethodRefactoring(callerBefore, callerAfter, methodRemoved);
        }

        private class SimpleExtractMethodRefactoring : ISimpleExtractMethodRefactoring
        {
            internal SimpleExtractMethodRefactoring(SyntaxNode callerBefore, SyntaxNode callerAfter,
                    SyntaxNode addedMethod)
            {
                this.callerBefore = callerBefore;
                this.callerAfter = callerAfter;
                this.addedMethod = addedMethod;
            }

            public SyntaxNode callerBefore { get; private set; }

            public SyntaxNode callerAfter { get; private set; }

            public SyntaxNode addedMethod { get; private set; }

            public RefactoringType RefactoringType
            {
                get { return RefactoringType.EXTRACT_METHOD; }
            }

            public void MapToDocuments(IDocument before, IDocument after) { }

            public string ToString()
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
            internal SimpleInlineMethodRefactoring(SyntaxNode callerBefore, SyntaxNode callerAfter, 
                SyntaxNode methodRemoved)
            {
                this.callerBefore = callerBefore;
                this.callerAfter = callerAfter;
                this.methodRemoved = methodRemoved;
            }

            public RefactoringType RefactoringType
            {
                get { return RefactoringType.INLINE_METHOD; }
            }

            public void MapToDocuments(IDocument before, IDocument after)
            {
                throw new NotImplementedException();
            }

            public SyntaxNode callerBefore { get; private set; }

            public SyntaxNode callerAfter { get; private set; }

            public SyntaxNode methodRemoved { get; private set; }

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
