using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.util;

namespace warnings.analyzer
{
    /* Analyzer for a method declaration. */
    public interface IMethodDeclarationAnalyzer
    {
        void SetMethodDeclaration(SyntaxNode method);
        SyntaxToken GetMethodName();
        String GetQualifiedName();


        /* Statement related queries. */
        SyntaxNode GetBlock();
        IEnumerable<SyntaxNode> GetStatements();
        IEnumerable<SyntaxNode> GetStatementsByIndexRange(int start, int end);
        IEnumerable<SyntaxNode> GetStatementsBefore(int position);
        SyntaxNode GetStatementAt(int position);
        IEnumerable<SyntaxNode> GetStatementsAfter(int position);
        IEnumerable<SyntaxNode> GetReturnStatements();
        
        /* Parameter related queries. */
        IEnumerable<SyntaxNode> GetParameters();
        IEnumerable<IEnumerable<SyntaxNode>> GetParameterUsages();

        SyntaxNode GetReturnType();
        bool HasReturnStatement();
        string DumpTree();

        /* Update part of the method delaration. */
        SyntaxNode ChangeReturnValue(string symbolName);
        SyntaxNode ChangeReturnType(string typeName);
        SyntaxNode AddParameters(IEnumerable<Tuple<string, string>> parameters);
 
    }

    internal class MethodDeclarationAnalyzer : IMethodDeclarationAnalyzer
    {
        private static int ANALYZER_COUNT = 0;

        public static int GetCount()
        {
            return ANALYZER_COUNT;
        }

        private MethodDeclarationSyntax method;
        private Logger logger = NLoggerUtil.GetNLogger(typeof(MethodDeclarationAnalyzer));

        internal MethodDeclarationAnalyzer()
        {
            Interlocked.Increment(ref ANALYZER_COUNT);
        }

        ~MethodDeclarationAnalyzer()
        {
            Interlocked.Decrement(ref ANALYZER_COUNT);
        }

        public void SetMethodDeclaration(SyntaxNode method)
        {
            this.method = (MethodDeclarationSyntax) method;   
        }

        public SyntaxToken GetMethodName()
        {
            return method.Identifier;
        }

        /* Get fully qualified name of this method. */
        public string GetQualifiedName()
        {
            // Get the qualified name of the RefactoringType containing this method.
            var qualifiedAnalyzer = AnalyzerFactory.GetQualifiedNameAnalyzer();
            qualifiedAnalyzer.SetSyntaxNode(method);

            // Combine the RefactoringType's qualified name and the method's identifier.
            return qualifiedAnalyzer.GetOutsideTypeQualifiedName() + "." + method.Identifier.ValueText;
        }

        public SyntaxNode GetBlock()
        {
            return method.Body;
        }

        public IEnumerable<SyntaxNode> GetStatements()
        {
            var block = (BlockSyntax) GetBlock();
            var statements = block.Statements;  
            return statements.OrderBy(n => n.Span.Start).AsEnumerable();
         }

        /* Get a subset of all the containing statements, start and end index are inclusive. */
        public IEnumerable<SyntaxNode> GetStatementsByIndexRange(int start, int end)
        {
            var statements = GetStatements();
            var subList = new List<SyntaxNode>();
            for(int i = start; i <= end; i++)
            {
                subList.Add(statements.ElementAt(i));
            }
            return subList.AsEnumerable();
        }


        public IEnumerable<SyntaxNode> GetStatementsBefore(int position)
        {
            // Get all the statements first.
            IEnumerable<SyntaxNode> statements = GetStatements();

            // Initiate an empty statement list.
            IList<SyntaxNode> result = new List<SyntaxNode>(); 
            
            // Iterate all the statement.
            foreach(var statement in statements)
            {
                // For statement whose end point is before the position, add it to the result
                if (statement.Span.End < position)
                {
                    result.Add(statement);
                }
            }
            return result.AsEnumerable();
        }

        public SyntaxNode GetStatementAt(int position)
        {
            // Get all the statements first.
            IEnumerable<SyntaxNode> statements = GetStatements();
            
            // Select the first statement whose span intersects with the position.
            return statements.First(s => s.Span.IntersectsWith(position));
        }

        public IEnumerable<SyntaxNode> GetStatementsAfter(int position)
        {
            // Get all the statements first.
            IEnumerable<SyntaxNode> statements = GetStatements();

            // Initiate an empty statement list.
            IList<SyntaxNode> result = new List<SyntaxNode>();

            // Iterate all the statement.
            foreach (var statement in statements)
            {
                // For statement whose end point is after the position, add it to the result
                if (statement.Span.Start > position)
                {
                    result.Add(statement);
                }
            }
            return result.AsEnumerable();
        }

        public IEnumerable<SyntaxNode> GetParameters()
        {
            // Any node that in the parameter RefactoringType, different from argument RefactoringType
            var paras = method.DescendantNodes().Where(n => n.Kind == SyntaxKind.Parameter);
            return paras.Any() ? paras : Enumerable.Empty<SyntaxNode>();
        }

        public IEnumerable<IEnumerable<SyntaxNode>> GetParameterUsages()
        {
            // Containing the results.
            var list = new List<IEnumerable<SyntaxNode>>();
            
            // All the parameters taken.
            var parameters = GetParameters();

            // Block of the method declaration.
            var block = ASTUtil.GetBlockOfMethod(method);

            // For each parameter.
            // ATTENTION: foreach will throw null exception if it has nothing in it.
            foreach (ParameterSyntax para in parameters)
            {
                // Need a new subList to copy out nodes, IEnumerable is a read only interface that will not copy out as new
                // elements. To copy out as new elements, a list is needed.
                // ATTENTION: cannot list.add(block.DecendantNodes()...), because if have multiple paras the previous added IEnumerable 
                // will be rewrite.
                var sublist = new List<SyntaxNode>();

                // Only able to retrieve the parameter usages if the method block exists.
                if (block != null)
                {
                    // If an identifier name equals the paraemeter's name, it is one usage of the 
                    // parameter.
                    sublist.AddRange(block.DescendantNodes().Where(n => n.Kind == SyntaxKind.IdentifierName
                        && n.GetText().Equals(para.Identifier.ValueText)));    
                }
                logger.Info("Parameter " + para.Identifier + " usage:" +
                               StringUtil.ConcatenateAll(",", sublist.Select(n => n.Span.ToString())));
                list.Add(sublist.AsEnumerable());
            }
            return list.AsEnumerable();
        }

        public SyntaxNode GetReturnType()
        {
            // The return RefactoringType's span start shall before the limit.
            int limit = 0;

            // Get the para list of this method.
            var paras = method.DescendantNodes().First(n => n.Kind == SyntaxKind.ParameterList);
            if (paras == null)
            {
                var block = ASTUtil.GetBlockOfMethod(method);
                limit = block.Span.Start;
            }
            else
            {
                limit = paras.Span.Start;
            }
            // Get all the predefined types before the limit, such as void, int and long.
            var types = method.DescendantNodes().Where(n => n.Kind == SyntaxKind.PredefinedType &&
                                                                    n.Span.Start < limit);

            // Get all the generic types from the limt, such as IEnumerable<int>.
            var genericTypes = method.DescendantNodes().Where(n => n.Kind == SyntaxKind.GenericName &&
                                                                   n.Span.Start < limit);
            
            
            // Get all identifiers before the limit.
            var identifiers = method.DescendantNodes().Where(n => n.Kind == SyntaxKind.IdentifierName &&
                                                                  n.Span.Start < limit);

            if (types.Count() == 1)
            {
                return types.First();
            }
            if (genericTypes.Count() == 1)
            {
                return genericTypes.First();
            }
            
            int leftMost = int.MaxValue;
            SyntaxNode leftMostIdentifier = null;

            // For all the identifiers, the leftmost one should be the return RefactoringType.
            foreach (SyntaxNode node in identifiers)
            {
                if (node.Span.Start < leftMost)
                {
                    leftMost = node.Span.Start;
                    leftMostIdentifier = node;
                }
            }

            return leftMostIdentifier;
        }

        public IEnumerable<SyntaxNode> GetReturnStatements()
        {
            return method.DescendantNodes().Where(n => n.Kind == SyntaxKind.ReturnStatement);
        }

        public bool HasReturnStatement()
        {
            // Get the return statement of the method.
            var returns = method.DescendantNodes().Where(n => n.Kind == SyntaxKind.ReturnStatement);
            
            // Return if no such statement.
            return returns.Any();

        }

        public string DumpTree()
        {
            var analyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
            analyzer.SetSyntaxNode(method);
            return analyzer.DumpTree();
        }

        /* Update every return statements in the method declaration (if any), to return the specified symbol.*/
        public SyntaxNode ChangeReturnValue(string symbolName)
        {
            // Create a return statement with the given symbol name.
            var returnStatement = Syntax.ReturnStatement
                (Syntax.ParseToken("return").WithTrailingTrivia(Syntax.Whitespace(" ")), 
                Syntax.ParseExpression(symbolName), Syntax.ParseToken(";"));

            // Replace every return statements in the method with the created new return statement.
            method = method.ReplaceNodes(
                method.DescendantNodes().Where(n => n.Kind == SyntaxKind.ReturnStatement),
                (n1, n2) => returnStatement);

            // Get all statements in the updated method.
            var statements = GetStatements();

            // If the last statement is not return statement, it needs adding one.
            if (statements.Last().Kind != SyntaxKind.ReturnStatement)
            {
                // Get the leading and trailing white space of the last statement.
                var leading = statements.Last().GetLeadingTrivia();
                var trailing = statements.Last().GetTrailingTrivia();

                // Add a return statement to the end of the body.
                method = method.AddBodyStatements(new StatementSyntax[] 
                    {returnStatement.WithLeadingTrivia(leading).WithTrailingTrivia(trailing)});
            }
            return method;
        }


        public SyntaxNode ChangeReturnType(string typeName)
        {
            // Get the trivias of the existing return RefactoringType.
            var trailing = method.ReturnType.GetTrailingTrivia();
            var leading = method.ReturnType.GetLeadingTrivia();

            // Replace the existing return RefactoringType with a new one by the given RefactoringType name.
            return method.ReplaceNodes(new[] { method.ReturnType}, 
                (n1, n2) => Syntax.ParseTypeName(typeName).WithTrailingTrivia(trailing).WithLeadingTrivia(leading));
        }


        /* Add parameters to the method declaration acccroding to the given RefactoringType-name tuples. <int, a>. */
        public SyntaxNode AddParameters(IEnumerable<Tuple<string, string>> parameters)
        {
            foreach (var tuple in parameters)
            {
                // Create parameters according to the corrent tuple.
                var parameter = Syntax.Parameter(Syntax.List<AttributeDeclarationSyntax>(), Syntax.TokenList(), 
                    Syntax.ParseTypeName(tuple.Item1).WithTrailingTrivia(Syntax.Whitespace(" ")), 
                        Syntax.ParseToken(tuple.Item2), null);
                
                // Add the parameters to the method declaration.
                method = method.AddParameterListParameters(parameter);
            }
            return method;
        }
    }
}
