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
        SyntaxNode GetDeclarationSyntaxNode();
        string GetSymbolTypeName();
    }

    internal class SymbolAnalyzer : ISymbolAnalyzer
    {
        private ISymbol symbol;
        private IDocument document;
        private readonly string OBJECT = "object";
        //private readonly Logger logger = NLoggerUtil.GetNLogger(typeof (ISymbolAnalyzer));

        public void SetSymbol(ISymbol symbol)
        {
            this.symbol = symbol;
        }

        public SyntaxNode GetDeclarationSyntaxNode()
        {
            // Get the default declaration location of the symbol
            var definition = symbol.OriginalDefinition.Locations.FirstOrDefault();
            
            // Get the source code and span of the definition
            var source = definition.SourceTree.GetRoot().GetText();
            var span = definition.SourceSpan;

            // Convert the source code to IDocument to use document analyzer.
            var converter = new String2IDocumentConverter();
            document = (IDocument) converter.Convert(source, null, null, null);
            var analyzer = AnalyzerFactory.GetDocumentAnalyzer();
            analyzer.SetDocument(document);
            return analyzer.
                // First get all the declarations in the document.
                GetAllDeclarations().
                    // Select the declaration that contains the symbol declaration. 
                    Where(d => d.Span.Contains(span)).
                        // Order them by the length, the shortest shall be the declaration for the symbol.
                        OrderBy(d => d.Span.Length).First();
        }

        public string GetSymbolTypeName()
        {
            return GetTypeName(GetDeclarationSyntaxNode());
        }

        /* Get the RefactoringType name of a declaration. */
        private string GetTypeName(SyntaxNode node)
        {
            // If this declaration is an instance declarator.
            if(node.Kind == SyntaxKind.VariableDeclarator)
            {
                var declarator = (VariableDeclaratorSyntax) node;

                // Get the declaration contains this declarator.
                var declaration = (VariableDeclarationSyntax)declarator.Ancestors().
                    // Whose kind is declaration.
                    Where(n => n.Kind == SyntaxKind.VariableDeclaration).
                        // Order them by the span length.
                        OrderBy(n => n.Span.Length).
                            // Get the shortest.
                            First();

                // If the RefactoringType declared is var, get the real RefactoringType string.
                if(declaration.Type.IsVar)
                {
                    return HandleVarDeclaration(declarator);
                }

                // Return the plain name for the declaration.
                return declaration.Type.PlainName;
            }
            
            // If not declarator, return object.
            return OBJECT;
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
