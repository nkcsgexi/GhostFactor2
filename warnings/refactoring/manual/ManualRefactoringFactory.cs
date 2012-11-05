using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace warnings.refactoring
{
    public partial class ManualRefactoringFactory
    {
        /* Create a manual extract method refactoring that extracts statements. */
        public static IManualExtractMethodRefactoring CreateManualExtractMethodRefactoring(
            IDocument before, IDocument after, SyntaxNode declaration,
            SyntaxNode invocation, IEnumerable<SyntaxNode> statements)
        {
            return new ManualExtractMethodRefactoring(before, after, declaration, invocation, 
                statements);
        }

        /* Create a manual extract method refacoting that extracts a expression. */
        public static IManualExtractMethodRefactoring CreateManualExtractMethodRefactoring(
            IDocument before, IDocument after, SyntaxNode declaration,
            SyntaxNode invocation, SyntaxNode expression)
        {
            return new ManualExtractMethodRefactoring(before, after, declaration, invocation, 
                expression);
        }

        /* 
         * Create a manual rename refactoring, the token (of RefactoringType identifier token) 
         * is where the rename is performed on, the new name is the name given to the identifier. 
         * Token is in the before version. 
         */
        public static IManualRenameRefactoring CreateManualRenameRefactoring(
            IDocument before, IDocument after, SyntaxNode node, string newName)
        {
            return new ManualRenameRefactoring(before, after, node, newName);
        }

        /* Create a manual change method signature refactoring. */
        public static IChangeMethodSignatureRefactoring CreateManualChangeMethodSignatureRefactoring
            (IDocument before, IDocument after, SyntaxNode afterMethod, 
            List<Tuple<int, int>> parametersMap)
        {
            return new ChangeMethodSignatureRefactoring(before, after, afterMethod, parametersMap);
        }

        /* Create an instance of manual inline method refactoring. */
        public static IInlineMethodRefactoring CreateManualInlineMethodRefactoring(
            IDocument before, IDocument after, SyntaxNode methodBefore,
            SyntaxNode methodAfter,SyntaxNode methodInlined,SyntaxNode inlinedMethodInvocation,
                IEnumerable<SyntaxNode> inlinedStatements)
        {
            return new InlineMethodRefactoring(before, after, methodBefore, methodAfter, 
                methodInlined, inlinedMethodInvocation, inlinedStatements);
        }

        private class ManualRenameRefactoring : IManualRenameRefactoring
        {
            private readonly string newName;
            private readonly SyntaxNode node;

            public ManualRenameRefactoring(IDocument before, IDocument after, 
                SyntaxNode node, string newName) : base(before, after)
            {
                this.node = node;
                this.newName = newName;
            }

            public override RefactoringType RefactoringType
            {
                get { return RefactoringType.RENAME; }
            }
        }

        /* Containing all the information about the extract method information. */
        private class ManualExtractMethodRefactoring : IManualExtractMethodRefactoring
        {
            public override RefactoringType RefactoringType
            {
                get { return RefactoringType.EXTRACT_METHOD; }
            }

            internal ManualExtractMethodRefactoring(IDocument before, IDocument after, 
                SyntaxNode declaration, SyntaxNode invocation,
                IEnumerable<SyntaxNode> statements) :base(before, after)
            {
                ExtractedMethodDeclaration = declaration;
                ExtractMethodInvocation = invocation;
                ExtractedStatements = statements;
                ExtractedExpression = null;
            }

            internal ManualExtractMethodRefactoring(IDocument before, IDocument after, 
                SyntaxNode declaration, SyntaxNode invocation, 
                SyntaxNode expression):base(before, after)
            {
                ExtractedMethodDeclaration = declaration;
                ExtractMethodInvocation = invocation;
                ExtractedExpression = expression;
                ExtractedStatements = null;
            }

            /* Output the information of a detected extract method refactoring for testing and log 
             * purposes.*/
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Extract Method Refactoring:");
                sb.AppendLine("Extracted Method Declaration:\n" + ExtractedMethodDeclaration);
                if (ExtractedStatements == null)
                    sb.AppendLine("Extracted Expression:\n" + ExtractedExpression);
                else
                    sb.AppendLine("Extracted Statements:\n" +
                                  StringUtil.ConcatenateAll("\n", ExtractedStatements.
                                  Select(s => s.GetText())));
                return sb.ToString();
            }
        }

        /* Describing a change method signature refactoring. */
        private class ChangeMethodSignatureRefactoring : IChangeMethodSignatureRefactoring
        {
            public ChangeMethodSignatureRefactoring(IDocument before, IDocument after, 
                SyntaxNode ChangedMethodDeclaration,
                List<Tuple<int, int>> ParametersMap):base(before, after)
            {
                this.ChangedMethodDeclaration = ChangedMethodDeclaration;
                this.ParametersMap = ParametersMap;
            }

            public override RefactoringType RefactoringType
            {
                get { return RefactoringType.CHANGE_METHOD_SIGNATURE; }
            }
        }

        /* Describing a inline method refactoring. */
        private class InlineMethodRefactoring : IInlineMethodRefactoring
        {
            internal InlineMethodRefactoring(IDocument before, IDocument after,
                SyntaxNode CallerMethodBefore, SyntaxNode CallerMethodAfter,
                SyntaxNode InlinedMethod, SyntaxNode InlinedMethodInvocation,
                    IEnumerable<SyntaxNode> InlinedStatementsInMethodAfter) :base(before, after)
            {
                this.CallerMethodAfter = CallerMethodAfter;
                this.CallerMethodBefore = CallerMethodBefore;
                this.InlinedMethod = InlinedMethod;
                this.InlinedStatementsInMethodAfter = InlinedStatementsInMethodAfter;
                this.InlinedMethodInvocation = InlinedMethodInvocation;
            }

            public override RefactoringType RefactoringType
            {
                get { return RefactoringType.INLINE_METHOD; }
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Inline method refactoring:");
                sb.AppendLine("Caller method before:");
                sb.AppendLine(CallerMethodBefore.GetText());
                sb.AppendLine("Caller method after:");
                sb.AppendLine(CallerMethodAfter.GetText());
                sb.AppendLine("Inlined method:");
                sb.AppendLine(InlinedMethod.GetText());
                return sb.ToString();
            }
        }
    }
}
