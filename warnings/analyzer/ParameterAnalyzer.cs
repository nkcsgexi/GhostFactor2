using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using NLog;
using warnings.util;

namespace warnings.analyzer
{
    /* Analyzer for a parameter of a method declaration. */
    public interface IParameterAnalyzer
    {
        void SetParameter(SyntaxNode node);
        SyntaxToken GetIdentifier();
        SyntaxTokenList GetModifiers();
        SyntaxNode GetParameterType();
    }


    internal class ParameterAnalyzer : IParameterAnalyzer
    {
        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof(IParameterAnalyzer));
        private ParameterSyntax parameter;

        public void SetParameter(SyntaxNode node)
        {
            parameter = (ParameterSyntax) node;
        }

        public SyntaxToken GetIdentifier()
        {
            return parameter.Identifier;
        }
        
        public SyntaxTokenList GetModifiers()
        {
            return parameter.Modifiers;
        }

        public SyntaxNode GetParameterType()
        {
            logger.Debug(parameter.GetText());

            // Get the leftmost limiter of the RefactoringType.
            int leftmost;
            var modifiers = GetModifiers();
            if(modifiers.Any())
            {
                // If having modifiers, the leftmost point shall be the end of the
                // last modifier.
                leftmost = modifiers.OrderBy(n => n.Span.End).Last().Span.End;
            }
            else
            {
                // If no modifier, the leftmost point shall be the start of the parameter
                leftmost = parameter.Span.Start;
            }

            // Right limiter of the parameter RefactoringType shall be the identifer's start point.
            int rightmost = GetIdentifier().Span.Start;

            // Get all the nodes in bwtween and the longest one is the parameter RefactoringType.
            var nodes = parameter.DescendantNodes().Where
                (n => n.Span.Start >= leftmost && n.Span.End <= rightmost);
            return nodes.OrderBy(n => n.Span.Length).Last();
        }
    }
}
