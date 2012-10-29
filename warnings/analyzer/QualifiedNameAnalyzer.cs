using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using warnings.util;

namespace warnings.analyzer
{
    /* public analyzer for name scope for a declaration, such as a method declaration. */
    public interface IQualifiedNameAnalyzer
    {
        void SetSyntaxNode(SyntaxNode declaration);
        string GetOutsideTypeQualifiedName();
        IEnumerable<string> GetInsideQualifiedNames();
    }

    internal class QualifiedNameAnalyzer : IQualifiedNameAnalyzer
    {
        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof (QualifiedNameAnalyzer));  
        private SyntaxNode node;
      
        public void SetSyntaxNode(SyntaxNode node)
        {
            this.node = node;
        }

        /*
         * For a given node, get the qualified name of the RefactoringType that contains in this node. For example,
         * if given this method as input, the output shall be warnings.analyzer.QualifiedNameAnalyzer.
         */
        public string GetOutsideTypeQualifiedName()
        {
            return GetOutsideTypeQualifiedName(node);
        }

        /* 
         * For a given node, get the qualified names of the types that are contained in this node. For example,
         * if given this document as input, the output shall be warnings.analyzer.QualifiedNameAnalyzer and
         * warnings.analyzer.IQualifiedNameAnalyzer.
         */
        public IEnumerable<string> GetInsideQualifiedNames()
        {
            // For all the decendent nodes.
            return node.DescendantNodes().
                // Selecting the ones of the typd class declaration.
                Where(n => n.Kind == SyntaxKind.ClassDeclaration).
                // Get the qulified name of that class declaration.
                Select(GetOutsideTypeQualifiedName);
        }


        private string GetOutsideTypeQualifiedName(SyntaxNode node)
        {
            // Get the name space name enclosing this node.
            string namespaceName = node.AncestorsAndSelf().
                // ancestors whose kind is name space node.
                Where(n => n.Kind == SyntaxKind.NamespaceDeclaration).
                // conver to the syntax and get the name.
                Select(n => (NamespaceDeclarationSyntax)n).First().Name.PlainName;

            // Get the class name enclosing this node.
            var classesNames = node.AncestorsAndSelf().
                // ancestors whose kind is class node.
                Where(n => n.Kind == SyntaxKind.ClassDeclaration).
                // convert each one to the kind class node syntax.
                Select(n => (ClassDeclarationSyntax)n).
                // order all the class decs by their length, in decending order.
                OrderByDescending(n => n.Span.Length).
                // select their names. 
                Select(n => n.Identifier.ValueText);

            // Combine all the names to get the scope string.
            var qualifiedName = namespaceName + "." + StringUtil.ConcatenateAll(".", classesNames);
            logger.Info(qualifiedName);
            return qualifiedName;
        }
    }
}
