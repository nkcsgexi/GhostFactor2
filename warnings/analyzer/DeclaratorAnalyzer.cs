using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace warnings.analyzer
{
    public interface IDeclaratorAnalyzer
    {
        void SetDeclarator(SyntaxNode declarator);
        SyntaxNode GetDeclaration();
        SyntaxNode GetDeclaredType();
    }

    internal class DeclaratorAnalyzer : IDeclaratorAnalyzer
    {
        private VariableDeclaratorSyntax declarator;

        public void SetDeclarator(SyntaxNode declarator)
        {
            this.declarator = (VariableDeclaratorSyntax) declarator;
        }

        public SyntaxNode GetDeclaration()
        {
            // Get the declaration contains this declarator.
            return declarator.Ancestors().
                // Whose kind is declaration.
                Where(n => n.Kind == SyntaxKind.VariableDeclaration).
                // Order them by the span length.
                    OrderBy(n => n.Span.Length).
                // Get the shortest.
                        First();
        }

        public SyntaxNode GetDeclaredType()
        {
            var declaration = (VariableDeclarationSyntax)GetDeclaration();
            return declaration.Type;
        }
    }
}
