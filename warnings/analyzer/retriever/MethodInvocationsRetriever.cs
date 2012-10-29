
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
    /* Retrieve all the invocations of the given method declaration in a given document. */
    public interface IMethodInvocationsRetriever
    {
        void SetDocument(IDocument document);
        void SetMethodDeclaration(SyntaxNode declaration);
        IEnumerable<SyntaxNode> GetInvocations();
    }


    internal class MethodInvocationsRetriever : IMethodInvocationsRetriever
    { 
        private readonly Logger logger = NLoggerUtil.
            GetNLogger(typeof(IMethodInvocationsRetriever));

        private ISemanticModel model;
        private SyntaxNode root;
        private MethodDeclarationSyntax declaration;
        private IDocument document;



        public void SetDocument(IDocument document)
        {
            root = (SyntaxNode) document.GetSyntaxRoot();
            model = document.GetSemanticModel();
            this.document = document;
        }

        public void SetMethodDeclaration(SyntaxNode declaration)
        {
            this.declaration = (MethodDeclarationSyntax)declaration;
        }

        public IEnumerable<SyntaxNode> GetInvocations()
        {
            var results = new List<SyntaxNode>();

            // Get all the method invocations.
            var invocations = root.DescendantNodes().
                Where(n => n.Kind == SyntaxKind.InvocationExpression).
                Select(n => (InvocationExpressionSyntax)n);

            // Get the qualified name of the RefactoringType that contains the method declaration.
            var qualifiedNameAnalyzer = AnalyzerFactory.GetQualifiedNameAnalyzer();
            qualifiedNameAnalyzer.SetSyntaxNode(declaration);
            var declarationScopeName = qualifiedNameAnalyzer.GetOutsideTypeQualifiedName();

            // Get the invocation analyzer, member access analyzer and a typable retriever.
            var invocationAnalyzer = AnalyzerFactory.GetMethodInvocationAnalyzer();
            var memberAccessAnalyzer = AnalyzerFactory.GetMemberAccessAnalyzer();
            var typableRetriever = RetrieverFactory.GetTypablesRetriever();

            // For each method invocations.
            foreach (InvocationExpressionSyntax invocation in invocations)
            {
                // Get the invoked method name.
                invocationAnalyzer.SetMethodInvocation(invocation);
                var invokedName = invocationAnalyzer.GetMethodName();

                // If invoked name is a identifier name and the name is equal to the declared method,
                // the invocation is direct invoking by simple name.
                if(invokedName.Kind == SyntaxKind.IdentifierName && 
                    invokedName.GetText().Equals(declaration.Identifier.ValueText))
                {
                    // If the invocation's scope is equal to declaration's, we find an invcation.
                    qualifiedNameAnalyzer.SetSyntaxNode(invocation);
                    if (qualifiedNameAnalyzer.GetOutsideTypeQualifiedName().Equals(declarationScopeName))
                    {
                        logger.Info(invocation.GetText());
                        results.Add(invocation);
                    }
                }

                // If invoked name is a member access expression, possibly an indirect access to the method
                // by qualifier name.
                if(invokedName.Kind == SyntaxKind.MemberAccessExpression)
                {
                    // If the right side of the member access expression is equal to the method name,
                    // it is more possible to access the declared method.
                    memberAccessAnalyzer.SetMemberAccess(invokedName);
                    if(memberAccessAnalyzer.GetRightPart().GetText().Equals(declaration.Identifier.ValueText))
                    {
                        // For the left side of the member access expression, query whether it is the RefactoringType 
                        // that contains the method declaration. 
                        typableRetriever.SetDocument(document);
                        var type = typableRetriever.GetMemberAccessType(memberAccessAnalyzer.GetLeftPart());
                        if (type.ToString().Equals(declarationScopeName))
                        {
                            logger.Info(invocation.GetText());
                            results.Add(invocation);
                        }
                    }
                }              
            }
            return results.AsEnumerable();
        }

    }
}
