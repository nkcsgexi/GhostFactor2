using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace warnings.retriever
{
    /* For retrieving the typable identifiers in a given IDocument. */
    public interface ITypablesRetriever
    {
        void SetDocument(IDocument document);
        IEnumerable<Tuple<SyntaxNode, ITypeSymbol>> GetTypableIdentifierTypeTuples();
        IEnumerable<Tuple<SyntaxNode, ITypeSymbol>> GetMemberAccessAndAccessedTypes();
        ITypeSymbol GetMemberAccessType(SyntaxNode node);
    }

    internal class TypableRetriever : ITypablesRetriever
    {
        private ISemanticModel model;
        private SyntaxNode root;
        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof (TypableRetriever));

        public void SetDocument(IDocument document)
        {
            model = document.GetSemanticModel();
            root = (SyntaxNode) document.GetSyntaxRoot();
        }

        /* 
         * Get tuples of node and RefactoringType. Nodes shall be identifier names. 
         * ATTENTION: the declarations cannot get RefactoringType info, only identifiers(references) can.
         */
        public IEnumerable<Tuple<SyntaxNode, ITypeSymbol>> GetTypableIdentifierTypeTuples()
        {
            var typedIdentifiers = new List<Tuple<SyntaxNode, ITypeSymbol>>();

            // Get all identifiers.
            var identifiers = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.IdentifierName);
            foreach (SyntaxNode id in identifiers)
            {
                // Query RefactoringType information of an identifier.
                var info = model.GetTypeInfo(id);

                // If RefactoringType is retrieved, add to the result.
                if (info.Type != null)
                    typedIdentifiers.Add(Tuple.Create(id, info.Type));
            }
            return typedIdentifiers.AsEnumerable();
        }

        /* 
         * Get the member access in the document, returning a tuple of member access node and the RefactoringType it is accessing.
         */
        public IEnumerable<Tuple<SyntaxNode, ITypeSymbol>> GetMemberAccessAndAccessedTypes()
        {
            // For all the classes whose memeber is accessed and their types. 
            var typedAccesses = new List<Tuple<SyntaxNode, ITypeSymbol>>();

            // Get all nodes whose RefactoringType is member access.
            var accesses = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.MemberAccessExpression);

            // For all the access in the list.
            foreach (SyntaxNode access in accesses)
            {
                // Get left and right side of the access.
                var analyzer = AnalyzerFactory.GetMemberAccessAnalyzer();
                analyzer.SetMemberAccess(access);
                var left = analyzer.GetLeftPart();
              
                // Query about the RefactoringType of the left side of the access, if it is not null, add to the results.
                // ATTENTION: mode.GetTypeInfo() cannot get primitive RefactoringType such as int.
                var infor = model.GetTypeInfo(left);
                if(infor.Type != null)
                    typedAccesses.Add(Tuple.Create(access, infor.Type));
            }
            return typedAccesses.AsEnumerable();
        }

        /* Given a node of the left side of a member access, e.g. A for A.B, as an input, return the RefactoringType symbol. */
        public ITypeSymbol GetMemberAccessType(SyntaxNode node)
        {
            return model.GetTypeInfo(node).Type;
        }

        
    }
}
