using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using warnings.util;

namespace warnings.analyzer
{
    public interface IIfStatementAnalyzer
    {
        void SetIfStatement(SyntaxNode ifStatement);
        SyntaxNode GetElseClause();
        bool HasBlockUnderIf();
        SyntaxNode GetBlockUnderIf();
        bool HasBlockUnderElse();
        SyntaxNode GetBlockUnderElse();
        bool WithElse();
        IEnumerable<SyntaxNode> GetDirectBlocks();
    }

    internal class IfStatementAnalyzer : IIfStatementAnalyzer
    {
        private readonly Logger logger = NLoggerUtil.GetNLogger
            (typeof (IIfStatementAnalyzer));
        private IfStatementSyntax ifStatement;

        public void SetIfStatement(SyntaxNode ifStatement)
        {
            this.ifStatement = (IfStatementSyntax)ifStatement;
        }

        public SyntaxNode GetElseClause()
        {
            return ifStatement.Else;
        }

        public bool HasBlockUnderIf()
        {
            return HasBlockChild(ifStatement);
        }

        public SyntaxNode GetBlockUnderIf()
        {
            return GetFirstBlockChild(ifStatement);
        }

        public bool HasBlockUnderElse()
        {
            return HasBlockChild(ifStatement.Else);
        }

        public SyntaxNode GetBlockUnderElse()
        {
            return GetFirstBlockChild(ifStatement.Else);
        }

        public bool WithElse()
        {
            return ifStatement.Else != null;
        }

        public IEnumerable<SyntaxNode> GetDirectBlocks()
        {
            return GetBlocks(ifStatement);
        }

        private IEnumerable<SyntaxNode> GetBlocks(IfStatementSyntax statement)
        {
            var list = new List<SyntaxNode>();

            // Add the block in the if branch.
            if(statement.Statement.Kind == SyntaxKind.Block)
            {
                list.Add(statement.Statement);
            }

            // If the if statement has an else branch.
            if (statement.Else != null)
            {
                // If the else has a statement.
                if(statement.Else.Statement != null)
                {
                    var elseStatement = statement.Else.Statement;
                    
                    // If the statement with else is another if statement, handle recursively
                    if(elseStatement.Kind == SyntaxKind.IfStatement)
                    {
                        list.AddRange(GetBlocks((IfStatementSyntax) elseStatement));
                    }   

                    // If the statement with else is a block, add the block to the blocks.
                    if (elseStatement.Kind == SyntaxKind.Block)
                    {
                        list.Add(elseStatement);
                    }
                }
            }
            return list;
        }

        private bool HasBlockChild(SyntaxNode node)
        {
            return node.ChildNodes().Any(n => n.Kind == SyntaxKind.Block);
        }

        private SyntaxNode GetFirstBlockChild(SyntaxNode node)
        {
            return node.ChildNodes().First(n => n.Kind == SyntaxKind.Block);
        }

    }
}
