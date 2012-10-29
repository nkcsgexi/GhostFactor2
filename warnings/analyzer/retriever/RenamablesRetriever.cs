using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace warnings.retriever
{
    /* 
     * For retrieving all the renamable things in a given document, the list of renamable things shall keep increasing, right now
     * only solving the most used ones, class declarations, method declarations, and variable declarators. In addtion, it can retrieve
     * all the member access, like A.C.D() for method access or A.C.B for field access, and also all identifiers in the given 
     * document.
     */
    public interface IRenamableRetriever
    {
        void SetRoot(SyntaxNode root);

        /* First all the things in declarations that can be renamed. */
        IEnumerable<SyntaxNode> GetNamespaceDeclarationNames();
        IEnumerable<SyntaxToken> GetClassDeclarationIdentifiers();
        IEnumerable<SyntaxToken> GetMethodDeclarationIdentifiers();
        IEnumerable<SyntaxToken> GetVariableDeclaratorIdentifiers();
        IEnumerable<SyntaxToken> GetMethodParameterDeclarationIdentifiers();
        IEnumerable<SyntaxToken> GetAllDeclarationIdentifiers(); 
            
        /* Next, all the names in refering that can be renamed. */
        IEnumerable<SyntaxNode> GetMemberAccesses();
        IEnumerable<SyntaxNode> GetIdentifierNodes();
    }

    internal class RenamablesRetriever : IRenamableRetriever
    {
        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof(RenamablesRetriever));

        private SyntaxNode root;

        public void SetRoot(SyntaxNode root)
        {
            this.root = root;
        }

        public IEnumerable<SyntaxNode> GetNamespaceDeclarationNames()
        {
            // All declarations of namespace
            var declarations = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.NamespaceDeclaration);
            
            // Select their names, their RefactoringType is NameSyntax.
            var list = (from NamespaceDeclarationSyntax dec in declarations select dec.Name);
            logger.Info("Get all identifiers in namespace declarations.");
            return list.AsEnumerable();
        }

        public IEnumerable<SyntaxToken> GetMethodDeclarationIdentifiers()
        {
            // All the method declarations.
            // ATTENTION: should not use DescendantNodes(n => n.Kind == ...), it does not yield anything. 
            var declarations = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.MethodDeclaration);
            
            // Select method identifiers from them.
            var list = (from MethodDeclarationSyntax dec in declarations select dec.Identifier).ToList();

            // Logging the retrieved results.
            logger.Info("Method declaration identifiers: " + StringUtil.ConcatenateAll(",", list.Select(n => n.ValueText)));

            // Sorting all the tokens to facilitate comparison.
            return list.OrderBy(n => n.Span.Start).AsEnumerable();
        }

        public IEnumerable<SyntaxToken> GetVariableDeclaratorIdentifiers()
        {
            // All the variable declarators, not declarations. One declaration can include several declarators. 
            // such as int a, b.
            var declarators = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.VariableDeclarator);
            var list = (from VariableDeclaratorSyntax dec in declarators select dec.Identifier).ToList();

            // Logging the retrieved results.
            logger.Info("Variable declaration identifiers: " + StringUtil.ConcatenateAll(",", list.Select(n => n.ValueText)));
            return list.OrderBy(n => n.Span.Start).AsEnumerable();
        }

        public IEnumerable<SyntaxToken> GetMethodParameterDeclarationIdentifiers()
        {
            // Get all the parameters, parameters are in the method's signature (declarations), while arguments are
            // in the method invocation.
            var declartions = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.Parameter);
            var list = (from ParameterSyntax para in declartions select para.Identifier);

            // Logging
            logger.Info("Parameters declaration identifiers: " + StringUtil.ConcatenateAll(",", list.Select(n => n.ValueText)));
            return list.OrderBy(n => n.Span.Start).AsEnumerable();
        }

        /* 
         * Identifier tokens exist in the declarations. Their parent can be applied on GetDeclaredSymbol to get the
         * declared symbols.
         */
        public IEnumerable<SyntaxToken> GetAllDeclarationIdentifiers()
        {
            var allTokens = new List<SyntaxToken>();

            // Add identifiers in all kinds of delarations.
            allTokens.AddRange(GetClassDeclarationIdentifiers());
            allTokens.AddRange(GetMethodDeclarationIdentifiers());
            allTokens.AddRange(GetVariableDeclaratorIdentifiers());
            allTokens.AddRange(GetMethodParameterDeclarationIdentifiers());

            return allTokens.AsEnumerable();
        }


        public IEnumerable<SyntaxToken> GetClassDeclarationIdentifiers()
        {
            var declarations = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.ClassDeclaration);
            var list = (from ClassDeclarationSyntax dec in declarations select dec.Identifier).ToList();
         
            // Logging the retrieved results.
            logger.Info("Class declaration identifiers: " + StringUtil.ConcatenateAll(",", list.Select(n => n.ValueText)));
            return list.OrderBy(n => n.Span.Start).AsEnumerable();
        }

        /* Get all the expressions for accessing members of classes. A.B.C is an access to members.*/
        public IEnumerable<SyntaxNode> GetMemberAccesses()
        {
            // All the member accessings.
            var accesses = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.MemberAccessExpression);

            // Use nodes analyzer to remove nodes whose parent is also in the list.
            var analyzer = AnalyzerFactory.GetSyntaxNodesAnalyzer();
            analyzer.SetSyntaxNodes(accesses);
            accesses = analyzer.RemoveSubNodes();
            logger.Info("Member Accessings: " + StringUtil.ConcatenateAll(",", accesses.Select(n => n.GetText())));
            return accesses.OrderBy(n => n.Span.Start).AsEnumerable();
        }

        /* Identifier nodes are in referring, not declaring. */
        public IEnumerable<SyntaxNode> GetIdentifierNodes()
        {
            // Get all nodes whose RefactoringType is identifier name.
            var names = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.IdentifierName);
            logger.Info("Get all indetifier names.");
            return names.OrderBy(n => n.Span.Start).AsEnumerable();
        }
    }
}
