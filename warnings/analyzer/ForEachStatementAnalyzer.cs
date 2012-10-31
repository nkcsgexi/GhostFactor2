using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using Roslyn.Compilers.CSharp;

namespace warnings.analyzer
{
    public interface IForEachStatementAnalyzer
    {
        void SetStatement(SyntaxNode statement);
        SyntaxToken GetIdentifier();
        SyntaxNode GetIdentifierType();
    }

    internal class ForEachStatementAnalyzer : IForEachStatementAnalyzer
    {
        private ForEachStatementSyntax statement;

        public void SetStatement(SyntaxNode statement)
        {
            this.statement = (ForEachStatementSyntax) statement;
        }

        public SyntaxToken GetIdentifier()
        {
            return statement.Identifier;
        }

        public SyntaxNode GetIdentifierType()
        {
            return statement.Type;
        }
    }
}
