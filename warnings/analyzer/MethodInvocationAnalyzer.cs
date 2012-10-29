using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using warnings.util;

namespace warnings.analyzer
{
    /* Analyzer for getting information of a method invocation. */
    public interface IMethodInvocationAnalyzer
    {
        void SetMethodInvocation(SyntaxNode invocation);
        SyntaxNode GetMethodName();
        IEnumerable<SyntaxNode> GetArguments();
        bool HasArguments();
        SyntaxNode ReorderAuguments(IEnumerable<Tuple<int, int>> mapping);
        SyntaxNode AddArguments(IEnumerable<string> names);
    }

    internal class MethodInvocationAnalyzer : IMethodInvocationAnalyzer
    {

        private static int ANALYZER_COUNT = 0;

        public static int GetCount()
        {
            return ANALYZER_COUNT;
        }

        internal MethodInvocationAnalyzer()
        {
            ANALYZER_COUNT++;
        }

        ~MethodInvocationAnalyzer()
        {
            ANALYZER_COUNT--;
        }

        private InvocationExpressionSyntax invocation;

        private Logger logger = NLoggerUtil.GetNLogger(typeof (MethodInvocationAnalyzer));

        public void SetMethodInvocation(SyntaxNode invocation)
        {
            this.invocation = (InvocationExpressionSyntax) invocation;
        }

        public SyntaxNode GetMethodName()
        {
            // The left parentheses is the rightmost position for the method name.
            int rightMost = invocation.ArgumentList.OpenParenToken.Span.Start;

            // Order the decendents by its span
            var orderedDecendents = invocation.DescendantNodes().OrderBy(n => n.Span.Length);

            // The decendent whose length is the most and end is before ( should be method name node.
            var name = orderedDecendents.Last(n => n.Span.End <= rightMost);
            return name;
        }

        public IEnumerable<SyntaxNode> GetArguments()
        {
            return invocation.ArgumentList.Arguments;
        }

        public bool HasArguments()
        {
            return invocation.ArgumentList.Arguments.Any();
        }


        /* Reorder the arguments according to the given mappings. */
        public SyntaxNode ReorderAuguments(IEnumerable<Tuple<int, int>> mapping)
        {
            // For saving all the arguments after ordering.
            var orderedArguments = Syntax.SeparatedList<ArgumentSyntax>();
            var arguments = GetArguments();
        
            // Sort the mappings by after positions.
            mapping = mapping.OrderBy(m => m.Item2);
           
            // For every map in the sorted mapping list, append the orginal arguments to the
            // sorted list.
            foreach (var map in mapping)
            {
                // Get the argument should be at this position.
                var argument = (ArgumentSyntax) arguments.ElementAt(map.Item1);

                // Get the trailing and leading trivia of the original argument and add these
                // trivia to the new argument.
                var leadingTrivia = arguments.ElementAt(map.Item2).GetLeadingTrivia();
                var trailingTrivia = arguments.ElementAt(map.Item2).GetTrailingTrivia();
                argument = argument.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
                
                // Append this argument to the list.
                orderedArguments = orderedArguments.Add(argument);
            }

            // Update the argumentlist by the ordered arguments.
            var originalArgumentList = invocation.ArgumentList;
            var orderedArgumentsList = originalArgumentList.Update(
                openParenToken: invocation.ArgumentList.OpenParenToken,
                arguments: orderedArguments,
                closeParenToken: invocation.ArgumentList.CloseParenToken
            );

            // Update the invocation using the original expression and the ordered arguments' list.
            return invocation.Update(invocation.Expression, orderedArgumentsList);
        }

        /* Add extra arguments to this invocation. */
        public SyntaxNode AddArguments(IEnumerable<string> names)
        {
            // Convert all the name in names to an argument and add these arguements to 
            // the method invocation.
            invocation = invocation.AddArgumentListArguments(
                names.Select(name => Syntax.Argument( Syntax.ParseExpression(name).WithLeadingTrivia(
                    Syntax.ParseLeadingTrivia(" ")))).ToArray());
            return invocation;
        }
    }
}
