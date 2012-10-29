using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.analyzer;

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
                var name = o.Name;
                if (!except.Any(e => e.Name.Equals(name)))
                {
                    result.Add(o);
                }
            }
            return result.AsEnumerable();
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
        public static IEnumerable<ISymbol> GetFlowOutData(IEnumerable<SyntaxNode> statements, IDocument document)
        {
            var analyzer = AnalyzerFactory.GetStatementsDataFlowAnalyzer();
            analyzer.SetStatements(statements);
            analyzer.SetDocument(document);
            return analyzer.GetFlowOutData();
        }


        /* Compare if two lists of symbols contain exactly same symbols, same means names are same. */
        public static bool CompareSymbolListByName(IEnumerable<ISymbol> list1, IEnumerable<ISymbol> list2)
        {
            if(GetSymbolListExceptByName(list1, list2).Any())
            {
                if(GetSymbolListExceptByName(list2, list1).Any())
                {
                    return true;
                }
            }
            return false;
        }

        /* Get the statement that is enclosing the given node. */
        public static SyntaxNode GetStatementEnclosingNode(SyntaxNode node)
        {
            var analyzer = AnalyzerFactory.GetSyntaxNodeAnalyzer();
            analyzer.SetSyntaxNode(node);
            return analyzer.GetClosestAncestor(n => n is StatementSyntax);
        }


    }
}
