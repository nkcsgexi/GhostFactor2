using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    /* Common interface for data flow anlayzer. */
    public interface IDataFlowAnalyzer
    {
        void SetDocument(IDocument document);
        IEnumerable<ISymbol> GetFlowInData();
        IEnumerable<ISymbol> GetFlowOutData();
        IEnumerable<ISymbol> GetWrittenData();
        IEnumerable<ISymbol> GetReadData();
    }


    /* Analyzer for one or more statement. */
    public interface IStatementsDataFlowAnalyzer : IDataFlowAnalyzer
    {
        void SetStatements(IEnumerable<SyntaxNode> statements);
    }

    /* Analyzer for a single expression. */
    public interface IExpressionDataFlowAnalyzer : IDataFlowAnalyzer
    {
        void SetExpression(SyntaxNode expression);
    }


    internal class ExpressionDataFlowAnalyzer : IExpressionDataFlowAnalyzer
    {
        private static int ANALYZER_COUNT = 0;

        public static int GetCount()
        {
            return ANALYZER_COUNT;
        }

        private SyntaxNode expression;

        private ISemanticModel model;

        internal ExpressionDataFlowAnalyzer()
        {  
            Interlocked.Increment(ref ANALYZER_COUNT);
        }

        ~ExpressionDataFlowAnalyzer()
        {
            Interlocked.Decrement(ref ANALYZER_COUNT);
        }

        public void SetDocument(IDocument document)
        {
            model = document.GetSemanticModel();
        }

        public void SetExpression(SyntaxNode expression)
        {
            this.expression = expression;
        }

        public IEnumerable<ISymbol> GetFlowOutData()
        {
            var analysis = model.AnalyzeExpressionDataFlow(expression);
            return analysis.DataFlowsOut;
        }

        public IEnumerable<ISymbol> GetWrittenData()
        {
            var analysis = model.AnalyzeExpressionDataFlow(expression);
            return analysis.WrittenInside;
        }

        public IEnumerable<ISymbol> GetReadData()
        {
            var analysis = model.AnalyzeExpressionDataFlow(expression);
            return analysis.ReadInside;
        }

        public IEnumerable<ISymbol> GetFlowInData()
        {
            var analysis = model.AnalyzeExpressionDataFlow(expression);
            return analysis.DataFlowsIn;
        }
    }

    internal class StatementsDataFlowAnalyzer : IStatementsDataFlowAnalyzer
    {
        private static int ANALYZER_COUNT = 0;

        public static int GetCount()
        {
            return ANALYZER_COUNT;
        }

        private ISemanticModel model;

        private IEnumerable<SyntaxNode> statements;

        internal StatementsDataFlowAnalyzer()
        {
            Interlocked.Increment(ref ANALYZER_COUNT);
        }

        ~StatementsDataFlowAnalyzer()
        {
            Interlocked.Decrement(ref ANALYZER_COUNT);
        }


        public void SetDocument(IDocument document)
        {
            model = document.GetSemanticModel();
        }


        public void SetStatements(IEnumerable<SyntaxNode> statements)
        {
            this.statements = statements.OrderBy(s => s.Span.Start);
        }

        public IEnumerable<ISymbol> GetFlowInData()
        {
            IRegionDataFlowAnalysis analysis = model.AnalyzeStatementsDataFlow(statements.First(), statements.Last());
            return analysis.DataFlowsIn;
        }

        public IEnumerable<ISymbol> GetFlowOutData()
        {
            IRegionDataFlowAnalysis analysis = model.AnalyzeStatementsDataFlow(statements.First(), statements.Last());
            return analysis.DataFlowsOut;
        }

        public IEnumerable<ISymbol> GetWrittenData()
        {
            IRegionDataFlowAnalysis analysis = model.AnalyzeStatementsDataFlow(statements.First(), statements.Last());
            return analysis.WrittenInside;
        }

        public IEnumerable<ISymbol> GetReadData()
        {
            IRegionDataFlowAnalysis analysis = model.AnalyzeStatementsDataFlow(statements.First(), statements.Last());
            return analysis.ReadInside;
        }
    }
}
