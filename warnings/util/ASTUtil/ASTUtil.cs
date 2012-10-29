using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;

namespace warnings.util
{
    public class ASTUtil
    {
        /* Builds Syntax Tree from the source code of a file. */
        public static SyntaxTree GetSyntaxTreeFromSource(String source)
        {
            return SyntaxTree.ParseCompilationUnit(source);
        }

        public static List<MethodDeclarationSyntax> GetAllMethodDeclarations(SyntaxTree tree)
        {
            SyntaxNode root = tree.GetRoot();
            var methods = new List<MethodDeclarationSyntax>();
            IEnumerable<SyntaxNode> ite = root.DescendantNodes();
            foreach (SyntaxNode node in ite)
            {
                if (node.Kind == SyntaxKind.MethodDeclaration)
                    methods.Add((MethodDeclarationSyntax)node);
            }
            return methods;
        }

        public static SyntaxNode GetBlockOfMethod(SyntaxNode method)
        {
            return method.DescendantNodes().FirstOrDefault(n => n.Kind == SyntaxKind.Block);
        }

        public static IEnumerable<SyntaxNode> GetStatementsInNode(SyntaxNode block)
        {       
            return block.DescendantNodes().Where(n => n is StatementSyntax);
        }

        /* Create the semantic model of a given tree. */
        public static SemanticModel CreateSemanticModel(SyntaxTree tree)
        {
            return Compilation.Create("compilation").AddSyntaxTrees(tree)
                .AddReferences(new AssemblyFileReference(typeof(object).Assembly.Location))
                .GetSemanticModel(tree);
        }

        /* Return true if caller is actually calling the callee, otherwise return false. */
        public static bool IsInvoking(SyntaxNode caller, SyntaxNode callee, SyntaxTree tree)
        {
            return GetAllInvocationsInMethod(caller, callee, tree).Any();
        }

        /* Get all the invocations of callee in the body of caller method. */
        public static IEnumerable<InvocationExpressionSyntax> GetAllInvocationsInMethod
            (SyntaxNode caller, SyntaxNode callee, SyntaxTree tree)
        {    
            // Create semantic model of the given tree.
            SemanticModel model = CreateSemanticModel(tree);

            // Get the entry of callee in the symble table.
            Symbol calleeSymbol = model.GetDeclaredSymbol((MethodDeclarationSyntax)callee);

            // Get all the invocations in the caller.
            var allInvocations = caller.DescendantNodes().
                Where(n => n.Kind == SyntaxKind.InvocationExpression).
                    Select(n => (InvocationExpressionSyntax) n);

            // Among all the invocations, select the ones that are calling the callee symbol.
            return allInvocations.Where(i => model.GetSymbolInfo(i).Symbol == calleeSymbol);
        }

        /* Flatten the caller by replacing a invocation of the callee with the code in the callee. */
        public static String FlattenMethodInvocation(SyntaxNode caller, SyntaxNode callee, SyntaxNode invocation)
        {
            // Get the statements in the callee method body except the return statement;
            var statements = GetStatementsInNode(GetBlockOfMethod(callee))
                .Where(s => !(s is ReturnStatementSyntax));

            // Combine the statements into one string;
            String replacer = StringUtil.ConcatenateAll("", statements.Select(s => s.GetFullText()).ToArray());
            
            String callerString = caller.GetFullText();
            
            // Replace the invocation with the replacer.
            return callerString.Replace(invocation.GetText(), replacer);
        }

        /* Get the class declarations contained in a root node. */
        public static IEnumerable<SyntaxNode> GetClassDeclarations(SyntaxNode root)
        {
            // Get all the classes contained in a root node, do NOT parse into the method declaration.
            return root.DescendantNodes(n => n.Kind != SyntaxKind.MethodDeclaration).
                Where(n => n.Kind == SyntaxKind.ClassDeclaration);
        }

        /* Get all the method declarations contained in a root node. */
        public static IEnumerable<SyntaxNode> GetMethodsDeclarations(SyntaxNode root)
        {
            // Do not need to parse into the method.
            return root.DescendantNodes(n => n.Kind != SyntaxKind.MethodDeclaration).
                Where(n => n.Kind == SyntaxKind.MethodDeclaration);
        }

        /* Check whether two method declarations have the same name. */
        public static bool AreMethodsNameSame(SyntaxNode method1, SyntaxNode method2)
        {
            var analyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
            analyzer.SetMethodDeclaration(method1);
            var name1 = analyzer.GetMethodName();
            analyzer.SetMethodDeclaration(method2);
            var name2 = analyzer.GetMethodName();
            return name1.Equals(name2);
        }

        /* Whether two nodes contain the same code, without considering the space. */
        public static bool AreSyntaxNodesSame(SyntaxNode node1, SyntaxNode node2)
        {
            var text1 = node1.GetFullText().Replace(" ", "");
            var text2 = node2.GetFullText().Replace(" ", "");
            return text1.Equals(text2);
        }

        /* Get an instance of read-only syntax list by given the enumerable of nodes. */
        public static SyntaxList<SyntaxNode> GetSyntaxList(IEnumerable<SyntaxNode> nodes)
        {
            return Syntax.List(nodes);
        }
    }
}
