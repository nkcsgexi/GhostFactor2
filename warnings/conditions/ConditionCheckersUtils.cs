using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;
using warnings.analyzer;
using warnings.analyzer.comparators;
using warnings.components;

namespace warnings.conditions
{
    public class ConditionCheckersUtils
    {
        /* Except one symbol list from the other by symbol name. */
        public static IEnumerable<ISymbol> GetSymbolListExceptByName(IEnumerable<ISymbol> original, 
            IEnumerable<ISymbol> except)
        {
            var result = new List<ISymbol>();
            foreach (ISymbol o in original)
            {
                if (!except.Any(e => e.Name.Equals(o.Name)))
                {
                    result.Add(o);
                }
            }
            return result;
        }

        /* Remove 'this' symbol in a list of symbols. */
        public static IEnumerable<ISymbol> RemoveThisSymbol(IEnumerable<ISymbol> original)
        {
            return original.Where(s => !s.Name.Equals("this"));
        }

        /* Get the RefactoringType name tuples by a given symbol list. */
        public static IEnumerable<Tuple<string, string>> GetTypeNameTuples(IEnumerable<ISymbol> symbols)
        {
            var typeNameTuples = new List<Tuple<string, string>>();
            var symbolAnalyzer = AnalyzerFactory.GetSymbolAnalyzer();
            foreach (var symbol in symbols)
            {
                var symbolName = symbol.Name;
                symbolAnalyzer.SetSymbol(symbol);
                var typeName = symbolAnalyzer.GetSymbolTypeName();
                typeNameTuples.Add(Tuple.Create(typeName, symbolName));
            }
            return typeNameTuples.AsEnumerable();
        }

        /* Get needed typeNameTuples if extracting statements. */
        public static IEnumerable<ISymbol> GetFlowInData(IEnumerable<SyntaxNode> statements, IDocument document)
        {
            var statementsDataFlowAnalyzer = AnalyzerFactory.GetStatementsDataFlowAnalyzer();
            statementsDataFlowAnalyzer.SetDocument(document);
            statementsDataFlowAnalyzer.SetStatements(statements);
            return statementsDataFlowAnalyzer.GetFlowInData();
        }

        /* Get the data read and written in a given set of statements. */
        public static IEnumerable<ISymbol> GetUsedData(IEnumerable<SyntaxNode> statements, IDocument document)
        {
            var statementsDataFlowAnalyzer = AnalyzerFactory.GetStatementsDataFlowAnalyzer();
            statementsDataFlowAnalyzer.SetDocument(document);
            statementsDataFlowAnalyzer.SetStatements(statements);
            return statementsDataFlowAnalyzer.GetUsedData();
        }

        /// <summary>
        /// Get the declared data in a sequence of statements.
        /// </summary>
        /// <param name="statements"></param>
        /// <param name="document"></param>
        /// <returns></returns>
 
        public static IEnumerable<ISymbol> GetUsedButNotDeclaredData(IEnumerable<SyntaxNode> statements, 
            IDocument document)
        {
            var usedData = GetUsedData(statements, document);
            var statementsDataFlowAnalyzer = AnalyzerFactory.GetStatementsDataFlowAnalyzer();
            statementsDataFlowAnalyzer.SetDocument(document);
            statementsDataFlowAnalyzer.SetStatements(statements);
            var declaredData = statementsDataFlowAnalyzer.GetDeclaredData();
            return usedData.Except(declaredData);
        }


        public static IEnumerable<ISymbol> GetFlowOutData(SyntaxNode statement, IDocument document)
        {
            return GetFlowOutData(new[] { statement }, document);
        }

        /* Get needed typeNameTuples if extracting an expression. */
        public static IEnumerable<ISymbol> GetFlowInData(SyntaxNode expression, IDocument document)
        {
            var expressionDataFlowAnalyzer = AnalyzerFactory.GetExpressionDataFlowAnalyzer();
            expressionDataFlowAnalyzer.SetDocument(document);
            expressionDataFlowAnalyzer.SetExpression(expression);
            return expressionDataFlowAnalyzer.GetFlowInData();
        }


        /* Given a set of statements, get the symbols that are flowed out. */
        public static IEnumerable<ISymbol> GetFlowOutData(IEnumerable<SyntaxNode> statements, IDocument 
            document)
        {
            var analyzer = AnalyzerFactory.GetStatementsDataFlowAnalyzer();
            analyzer.SetStatements(statements);
            analyzer.SetDocument(document);
            return analyzer.GetFlowOutData();
        }


        /* Compare if two lists of symbols contain exactly same symbols, same means names are same. */
        public static bool AreSymbolListsEqual(IEnumerable<ISymbol> list1, IEnumerable<ISymbol> list2)
        {
            if(!GetSymbolListExceptByName(list1, list2).Any())
            {
                if(!GetSymbolListExceptByName(list2, list1).Any())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the statement that is enclosing the given node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static SyntaxNode GetStatementEnclosingNode(SyntaxNode node)
        {
            var analyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
            analyzer.SetSyntaxNode(node);
            return analyzer.GetClosestAncestor(n => n is StatementSyntax);
        }

        public static ICodeActionOperation GetRemoveCodeIssueComputerOperation(ICodeIssueComputer computer)
        {
            return new RemoveCodeIssueComputerOperation(computer);
        }

        private class RemoveCodeIssueComputerOperation : ICodeActionOperation
        {
            private readonly ICodeIssueComputer computer;

            public RemoveCodeIssueComputerOperation(ICodeIssueComputer computer)
            {
                this.computer = computer;
            }

            public void Apply(IWorkspace workspace, CancellationToken cancellationToken = new 
                CancellationToken())
            {
                GhostFactorComponents.RefactoringCodeIssueComputerComponent.RemoveCodeIssueComputers(new[] 
                {computer});
            }

            public object GetPreview(CancellationToken cancellationToken = new CancellationToken())
            {
                return null;
            }
        }

        public static bool AreStringTuplesSame(IEnumerable<Tuple<string, string>> tuples1,
                IEnumerable<Tuple<string, string>> tuples2)
        {
            // Prepare the comparer for sets of string tuples. 
            var tupleComparer = new StringTupleEqualityComparer();
            var setsComparer = new SetsEqualityCompare<Tuple<string, string>>
                (tupleComparer);

            // If the parameter sets differ, this is an updated version of code issue
            // computer.
            return setsComparer.Equals(tuples1, tuples2);
        }

        /// <summary>
        /// Try to get the method declaration that is outside of the given node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static SyntaxNode TryGetOutsideMethod(SyntaxNode node)
        {
            var methods = node.Ancestors().OfType<MethodDeclarationSyntax>().ToList();
            if (methods.Any())
            {
                return methods.First();
            }
            return null;
        }
    }
}
