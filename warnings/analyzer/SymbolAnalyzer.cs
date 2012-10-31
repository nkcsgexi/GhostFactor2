using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.util;

namespace warnings.analyzer
{
    /* Symbol analyzer for a given symbol to get the decalaration of symbol and its RefactoringType. */
    public interface ISymbolAnalyzer
    {
        void SetSymbol(ISymbol symbol);
        SyntaxToken GetDeclarationToken();
        string GetSymbolTypeName();
    }

    internal class SymbolAnalyzer : ISymbolAnalyzer
    {
        private ISymbol symbol;
        private IDocument document;
        private readonly string OBJECT = "object";
    
        public void SetSymbol(ISymbol symbol)
        {
            this.symbol = symbol;
        }

        public SyntaxToken GetDeclarationToken()
        {
            // Get the default declaration location of the symbol
            var definition = symbol.OriginalDefinition.Locations.FirstOrDefault();

            // Get the source code and span of the definition
            var root = (SyntaxNode)definition.SourceTree.GetRoot(); 
            var span = definition.SourceSpan;
            var token = root.DescendantTokens(n => n.Span.Contains(span)).First(n => n.Span.Equals(span));
            return token;
        }

        public string GetSymbolTypeName()
        {
            return GetTypeName(GetDeclarationToken());
        }

        /* Get the RefactoringType name of a declaration. */
        private string GetTypeName(SyntaxToken token)
        {
            var parent = GetParentDeclaratorOrParameter(token);
            
            // If this declaration is an instance declarator, get the type name of the
            // declarator.
            if(parent.Kind == SyntaxKind.VariableDeclarator)
            {
                return GetDeclaratorTypeName(parent);
            }

            // If the declaration is a parameter, get the type name of the parameter.
            if(parent.Kind == SyntaxKind.Parameter)
            {
                return GetParameterTypeName(parent);
            }
            
            // If not declarator, return object.
            return OBJECT;
        }

        private SyntaxNode GetParentDeclaratorOrParameter(SyntaxToken token)
        {
            var analyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
            analyzer.SetSyntaxNode(token.Parent);
            return analyzer.GetClosestAncestor(n => n.Kind == SyntaxKind.VariableDeclarator 
                || n.Kind == SyntaxKind.Parameter);
        }

        private string GetDeclaratorTypeName(SyntaxNode node)
        {
            // Get the declared type of the declarator.
            var analyzer = AnalyzerFactory.GetDeclaratorAnalyzer();
            analyzer.SetDeclarator(node);
            var type = (TypeSyntax)analyzer.GetDeclaredType();
    
            // Handle var declaration.
            if (type.IsVar)
            {
                return HandleVarDeclaration((VariableDeclaratorSyntax)node);
            }

            return type.GetText();
        }

        private string GetParameterTypeName(SyntaxNode node)
        {
            var analyzer = AnalyzerFactory.GetParameterAnalyzer();
            analyzer.SetParameter(node);
            return analyzer.GetParameterType().GetText();
        }


        /* Get the real RefactoringType name when the declaration is using var keyword. */
        private string HandleVarDeclaration(VariableDeclaratorSyntax declarator)
        {
            // For var declaration, RefactoringType is implied in the value of the initializer.
            var value = declarator.Initializer.Value;

            // Get the semantic model.
            var model = document.GetSemanticModel();

            // If can successfully get the RefactoringType of the value, return the name of the value,
            // otherwise return object.
            if (model.GetTypeInfo(value).Type != null)
            {
                return model.GetTypeInfo(value).Type.Name;
            }
            return OBJECT;
        }
    }
}
