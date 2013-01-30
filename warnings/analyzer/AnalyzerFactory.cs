using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace warnings.analyzer
{
    /* Factory method for returning different types of _retriever, one instance of each anlayzer is enough. */
    public class AnalyzerFactory
    {
        public static IMethodDeclarationAnalyzer GetMethodDeclarationAnalyzer()
        {
            return new MethodDeclarationAnalyzer();
        }

        public static IMethodInvocationAnalyzer GetMethodInvocationAnalyzer()
        {
            return new MethodInvocationAnalyzer();
        }

        public static IDocumentAnalyzer GetDocumentAnalyzer()
        {
            return new DocumentAnalyzer();
        }

        public static ISolutionAnalyzer GetSolutionAnalyzer()
        {
            return new SolutionAnalyzer();
        }

        public static IStatementAnalyzer GetStatementAnalyzer()
        {
            return new StatementAnalyzer();
        }

        public static ISyntaxNodeAnalyzer GetSyntaxNodeAnalyzer()
        {
            return new SyntaxNodeAnalyzer();
        }

        public static ISyntaxNodesAnalyzer GetSyntaxNodesAnalyzer()
        {
            return new SyntaxNodesAnalyzer();
        }

        public static IStatementsDataFlowAnalyzer GetStatementsDataFlowAnalyzer()
        {
            return new StatementsDataFlowAnalyzer();
        }

        public static IExpressionDataFlowAnalyzer GetExpressionDataFlowAnalyzer()
        {
            return new ExpressionDataFlowAnalyzer();
        }

        public static IParameterAnalyzer GetParameterAnalyzer()
        {
            return new ParameterAnalyzer();
        }

        public static ISemanticModelAnalyzer GetSemanticModelAnalyzer()
        {
            return new SemanticModelAnalyzer();
        }

        public static IMemberAccessAnalyzer GetMemberAccessAnalyzer()
        {
            return new MemberAccessAnalyzer();
        }

        public static IQualifiedNameAnalyzer GetQualifiedNameAnalyzer()
        {
            return new QualifiedNameAnalyzer();
        }

        public static ISymbolAnalyzer GetSymbolAnalyzer()
        {
            return new SymbolAnalyzer();
        }

        public static ITypeHierarchyAnalyzer GetTypeHierarchyAnalyzer()
        {
            return new TypeHierarchyAnalyzer();
        }

        public static IReturnStatementAnalyzer GetReturnStatementAnalyzer()
        {
            return new ReturnStatementAnalyzer();
        }

        public static IBlocksAnalyzer GetBlockAnalyzer()
        {
            return new BlockAnalyzer();
        }

        public static IDeclaratorAnalyzer GetDeclaratorAnalyzer()
        {
            return new DeclaratorAnalyzer();
        }

        public static IIfStatementAnalyzer GetIfStatementAnalyzer()
        {
            return new IfStatementAnalyzer();
        }

        public static IForEachStatementAnalyzer GetForEachStatementAnalyzer()
        {
            return new ForEachStatementAnalyzer();
        }
    }
}
