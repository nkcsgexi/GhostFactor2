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
        IEnumerable<ISymbol> GetUsedData();
        IEnumerable<ISymbol> GetDeclaredData();
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
        private SyntaxNode expression;

        private ISemanticModel model;

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

        public IEnumerable<ISymbol> GetUsedData()
        {
            var analysis = model.AnalyzeExpressionDataFlow(expression);
            return analysis.ReadInside.Union(analysis.WrittenInside);
        }

        public IEnumerable<ISymbol> GetDeclaredData()
        {
            var analysis = model.AnalyzeExpressionDataFlow(expression);
            return analysis.VariablesDeclared;
        }

        public IEnumerable<ISymbol> GetFlowInData()
        {
            var analysis = model.AnalyzeExpressionDataFlow(expression);
            return analysis.DataFlowsIn;
        }
    }

    internal class StatementsDataFlowAnalyzer : IStatementsDataFlowAnalyzer
    {
        private static Logger logger = NLoggerUtil.
            GetNLogger(typeof (IStatementsDataFlowAnalyzer));
        private ISemanticModel model;
        private IEnumerable<SyntaxNode> statements;

        public void SetDocument(IDocument document)
        {
            model = document.GetSemanticModel();
        }


        public void SetStatements(IEnumerable<SyntaxNode> statements)
        {
            this.statements = statements.OrderBy(s => s.Span.Start).ToList();
        }

        private IRegionDataFlowAnalysis GetAnalysisResult()
        {
            if(statements.Count() == 1)
            {
                logger.Info(statements.First().GetText());
                return model.AnalyzeStatementDataFlow(statements.First());
            }
            return model.AnalyzeStatementsDataFlow(statements.First(), statements.Last());
        }
        

        public IEnumerable<ISymbol> GetFlowInData()
        {
            IRegionDataFlowAnalysis analysis = GetAnalysisResult();
            return analysis.DataFlowsIn;
        }

        public IEnumerable<ISymbol> GetFlowOutData()
        {
            IRegionDataFlowAnalysis analysis = GetAnalysisResult();
            return analysis.DataFlowsOut;
        }

        public IEnumerable<ISymbol> GetWrittenData()
        {
            IRegionDataFlowAnalysis analysis = GetAnalysisResult();
            return analysis.WrittenInside;
        }

        public IEnumerable<ISymbol> GetReadData()
        {
            IRegionDataFlowAnalysis analysis = GetAnalysisResult();
            return analysis.ReadInside;
        }

        public IEnumerable<ISymbol> GetUsedData()
        {
            IRegionDataFlowAnalysis analysis = GetAnalysisResult();
            return analysis.ReadInside.Union(analysis.WrittenInside);
        }

        public IEnumerable<ISymbol> GetDeclaredData()
        {
            IRegionDataFlowAnalysis analysis = GetAnalysisResult();
            return analysis.VariablesDeclared;
        }
    }
}
