using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace warnings.analyzer
{
    public interface IReturnStatementAnalyzer
    {
        void SetReturnStatement(SyntaxNode statement);
        SyntaxNode GetReturnedExpression();
        bool IsReturningNull();
    }

    internal class ReturnStatementAnalyzer : IReturnStatementAnalyzer
    {
        private ReturnStatementSyntax statement;

        public void SetReturnStatement(SyntaxNode statement)
        {
            this.statement = (ReturnStatementSyntax) statement;
        }

        public SyntaxNode GetReturnedExpression()
        {
            return statement.Expression;
        }

        public bool IsReturningNull()
        {
            return statement.Expression.EquivalentTo(Syntax.ParseExpression("null"));
        }
    }

  
}
