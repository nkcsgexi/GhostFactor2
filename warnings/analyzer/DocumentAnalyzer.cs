using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.util;

namespace warnings.analyzer
{
    /* An anlayzer of the basic structure for the given document. */

    public interface IDocumentAnalyzer
    {
        void SetDocument(IDocument document);
        IEnumerable<SyntaxNode> GetNamespaceDecalarations();
        IEnumerable<SyntaxNode> GetClassDeclarations(SyntaxNode mamespaceDeclaration);      
        IEnumerable<SyntaxNode> GetFieldDeclarations(SyntaxNode classDeclaration);
        IEnumerable<SyntaxNode> GetMethodDeclarations(SyntaxNode classDeclaration);
        IEnumerable<SyntaxNode> GetVariableDeclarations(SyntaxNode methodDeclaration);
        IEnumerable<SyntaxNode> GetAllDeclarations();
        IEnumerable<ISymbol> GetAllDeclaredSymbols(); 
        String GetKey();

        /* Given a node of declaration, returns the symbol in the semantic model. */
        ISymbol GetSymbol(SyntaxNode declaration);

        /* Get the first symbol of different types for testing purpuses. */
        ISymbol GetFirstLocalVariable();
        ISymbol GetFirstClass();
        ISymbol GetFirstNamespace();
        ISymbol GetFirstMethod();
        String DumpSyntaxTree();

        /* Whether this document contains the definition of the RefactoringType of the given qualified name.*/
        bool ContainsQualifiedName(string qualifiedName);
    }

    internal class DocumentAnalyzer : IDocumentAnalyzer
    {
        private static int ANALYZER_COUNT = 0;

        public static int GetCount()
        {
            return ANALYZER_COUNT;
        }

        private IDocument document;

        private Logger logger;
                    
        /* the root for the syntax tree of the document. */
        private SyntaxNode root;


        internal DocumentAnalyzer()
        {  
            this.logger = NLoggerUtil.GetNLogger(typeof (DocumentAnalyzer));
            Interlocked.Increment(ref ANALYZER_COUNT);
        }

        ~DocumentAnalyzer()
        {
            Interlocked.Decrement(ref ANALYZER_COUNT);
        }


        public void SetDocument(IDocument document)
        {
            this.document = document;
            this.root = (SyntaxNode)document.GetSyntaxRoot();
        }

        public IEnumerable<SyntaxNode> GetNamespaceDecalarations()
        {
            return GetDecendantOfKind(root, SyntaxKind.NamespaceDeclaration);
         }

        public IEnumerable<SyntaxNode> GetClassDeclarations(SyntaxNode mamespaceDeclaration)
        {
            return GetDecendantOfKind(mamespaceDeclaration, SyntaxKind.ClassDeclaration);
        }

        /* A field declaration can consist of several declarator.*/
        public IEnumerable<SyntaxNode> GetFieldDeclarations(SyntaxNode classDeclaration)
        {
            // First get all the field declarations in the class.
            var fields =  GetDecendantOfKind(classDeclaration, SyntaxKind.FieldDeclaration);
            IEnumerable<SyntaxNode> result = null;

            // iterate all these fields
            foreach (FieldDeclarationSyntax field in fields)
            {
                // For each field, find all its containing declarators and cancatenate them.
                if(result == null)
                {
                    result = GetDecendantOfKind(field, SyntaxKind.VariableDeclarator); 
                }
                else
                {
                    result = result.Concat(GetDecendantOfKind(field, SyntaxKind.VariableDeclarator));
                }
            }

            return result;
        }

        public IEnumerable<SyntaxNode> GetMethodDeclarations(SyntaxNode classDeclaration)
        {
            return GetDecendantOfKind(classDeclaration, SyntaxKind.MethodDeclaration);
        }

        public IEnumerable<SyntaxNode> GetVariableDeclarations(SyntaxNode methodDeclaration)
        {
            return GetDecendantOfKind(methodDeclaration, SyntaxKind.VariableDeclarator);
        }


        public string GetKey()
        {
            throw new NotImplementedException();
        }

        public ISymbol GetSymbol(SyntaxNode declaration)
        {
            ISemanticModel model = document.GetSemanticModel();
            
            // Only the following kind of declarations can find their symbol, otherwise throw exception.
            switch (declaration.Kind)
            {
                    case SyntaxKind.NamespaceDeclaration:
                        return model.GetDeclaredSymbol(declaration);

                    case SyntaxKind.ClassDeclaration:
                        return model.GetDeclaredSymbol(declaration);

                    case SyntaxKind.MethodDeclaration:
                        return model.GetDeclaredSymbol(declaration);

                    case SyntaxKind.VariableDeclarator:
                        return model.GetDeclaredSymbol(declaration);
                    
                    default:
                        logger.Fatal("Cannot find symbol to a node that is not a declaration.");
                        break;
            }
            return null;
        }

      

        public string DumpSyntaxTree()
        {
            var sb = new StringBuilder();
            sb.AppendLine();

            // Iterate each namespace in the file.
            foreach (NamespaceDeclarationSyntax namespaceDec in GetNamespaceDecalarations())
            {
                sb.AppendLine(namespaceDec.Name.PlainName);
                
                // Iterate the classes in each namepace.
                foreach (ClassDeclarationSyntax classDec in GetClassDeclarations(namespaceDec))
                {
                    sb.Append("\t");
                    sb.AppendLine(classDec.Identifier.ValueText);
                    
                    // Iterate the field declarations in each class, print only the first one.
                    foreach (VariableDeclaratorSyntax field in GetFieldDeclarations(classDec))
                    {
                        sb.Append("\t\t");
                        sb.AppendLine(field.Identifier.ValueText);
                    }

                    // Iterate the methods declarations in each class.
                    foreach (MethodDeclarationSyntax method in GetMethodDeclarations(classDec))
                    {
                        sb.Append("\t\t");
                        sb.AppendLine(method.Identifier.ValueText);

                        // Iterate the local variable declarations in each method, print only the first one
                        // in a declaration.
                        foreach (VariableDeclaratorSyntax variable in GetVariableDeclarations(method))
                        {
                            sb.Append("\t\t\t");
                            sb.AppendLine(variable.Identifier.ValueText);
                        }
                    }
                }
            }
            return sb.ToString();
        }

        public bool ContainsQualifiedName(string qualifiedName)
        {
            var nameAnalyzer = AnalyzerFactory.GetQualifiedNameAnalyzer();
            nameAnalyzer.SetSyntaxNode((SyntaxNode) document.GetSyntaxRoot());
            return nameAnalyzer.GetInsideQualifiedNames().Contains(qualifiedName);
        }

        public ISymbol GetFirstLocalVariable()
        {
            var first_namespace = GetNamespaceDecalarations().First();
            var first_class = GetClassDeclarations(first_namespace).First();
            var first_method = GetMethodDeclarations(first_class).First();
            var first_variable = GetVariableDeclarations(first_method).First();
            return GetSymbol(first_variable);
        }
        
        public ISymbol GetFirstMethod()
        {
            var first_namespace = GetNamespaceDecalarations().First();
            var first_class = GetClassDeclarations(first_namespace).First();
            var first_method = GetMethodDeclarations(first_class).First();
            return GetSymbol(first_method);
        }

        public ISymbol GetFirstClass()
        {
            var first_namespace = GetNamespaceDecalarations().First();
            var first_class = GetClassDeclarations(first_namespace).First();
            return GetSymbol(first_class);
        }

        public ISymbol GetFirstNamespace()
        {
            var first_namespace = GetNamespaceDecalarations().First();
            return GetSymbol(first_namespace);
        }

        private IEnumerable<SyntaxNode> GetDecendantOfKind(SyntaxNode parent, SyntaxKind kind)
        {
            return parent.DescendantNodes().Where(n => n.Kind == kind);
         }


        public IEnumerable<SyntaxNode> GetAllDeclarations()
        {
            var declarations = new List<SyntaxNode>();

            // Add all the namespace declarations.
            declarations.AddRange(GetNamespaceDecalarations());

            // Add all the class declarations.
            declarations.AddRange(GetNamespaceDecalarations().SelectMany(GetClassDeclarations));
            
            // Add all the method declarations.
            declarations.AddRange(GetNamespaceDecalarations().SelectMany(GetClassDeclarations).
                SelectMany(GetMethodDeclarations));

            // Add all the field declarations.
            declarations.AddRange(GetNamespaceDecalarations().SelectMany(GetClassDeclarations).
                SelectMany(GetFieldDeclarations));

            // Add all the local variable declarations;
            declarations.AddRange(GetNamespaceDecalarations().SelectMany(GetClassDeclarations).
                SelectMany(GetMethodDeclarations).SelectMany(GetVariableDeclarations));
            return declarations.AsEnumerable();
        }

        /* For all the declarations in the document, get corresponding symbols declared. */
        public IEnumerable<ISymbol> GetAllDeclaredSymbols()
        {
            var declarations = GetAllDeclarations();
            var model = document.GetSemanticModel();
            return declarations.Select(d => model.GetDeclaredSymbol(d));
        }
    }

    internal class AnalyzerWalker : SyntaxWalker
    {
        private Logger logger;

        public AnalyzerWalker()
        {
            this.logger = NLoggerUtil.GetNLogger(typeof (AnalyzerWalker));
        }

        public override void Visit(SyntaxNode node)
        {
            base.Visit(node);
            logger.Info(node.ToString());
        }
    }
}
