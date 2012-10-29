using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers.CSharp;
using warnings.util;

namespace warnings.analyzer
{
    public interface IStatementAnalyzer 
    {
        void SetSource(string source);
        void SetSyntaxNode(SyntaxNode statement);
        bool IsStatement();
        bool HasMethodInvocation(string methodName);
        SyntaxNode GetMethodDeclaration();
    }

    internal class StatementAnalyzer : IStatementAnalyzer
    {
        private static int ANALYZER_COUNT = 0;

        public static int GetCount()
        {
            return ANALYZER_COUNT;
        }

        private string source;
        private SyntaxNode statement;

        internal StatementAnalyzer()
        {
            Interlocked.Increment(ref ANALYZER_COUNT);
        }

        ~StatementAnalyzer()
        {
            Interlocked.Decrement(ref ANALYZER_COUNT);
        }

        public void SetSource(string source)
        {
            this.source = source;
            statement = ASTUtil.GetSyntaxTreeFromSource(this.source).GetRoot();
        }

        public void SetSyntaxNode(SyntaxNode statement)
        {
            this.statement = statement;
            this.source = statement.GetText();
        }

        public bool IsStatement()
        {
            return statement is StatementSyntax;
        }

        public bool HasMethodInvocation(string methodName)
        {
            IEnumerable<SyntaxNode> nodes = statement.DescendantNodes();
            foreach (SyntaxNode n in nodes)
            {
                // select the node if it is invocation of a method
                if (n is InvocationExpressionSyntax)
                {
                    // first node in the invocation should be the method name, including member access
                    // expression.
                    String method = n.DescendantNodes().First().GetText();
                    if (method.EndsWith(methodName))
                        return true;
                }
            }
            return false;
        }

        public SyntaxNode GetMethodDeclaration()
        {
            return statement.Ancestors().First(
                    n => n.Kind == SyntaxKind.MethodDeclaration 
                        && n.Span.OverlapsWith(statement.Span));
        }
    }
}
