using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Editor;
using warnings.analyzer;
using warnings.refactoring;
using warnings.resources;
using warnings.util;

namespace warnings.conditions
{
    partial class InlineMethodConditionCheckersList
    {
        private class ChangedVariableValuesChecker : InlineMethodConditionsChecker
        {
            private readonly Logger logger = NLoggerUtil.GetNLogger(typeof(ChangedVariableValuesChecker));

            public override ICodeIssueComputer CheckInlineMethodCondition(IDocument before, IDocument after, 
                IInlineMethodRefactoring refactoring)
            {
                // Get the out going symbols before the method is inlined.
                var writtenSymbolsBeforeInline = ConditionCheckersUtils.GetFlowOutData(ConditionCheckersUtils.GetStatementEnclosingNode
                    (refactoring.InlinedMethodInvocation), before);

                // Get the out going symbols after the method is inlined.
                var writtenSymbolsAfterInline = ConditionCheckersUtils.GetFlowOutData(refactoring.InlinedStatementsInMethodAfter, after);
                
                // Calculate the symbols that are added by inlining method.
                var addedSymbols = ConditionCheckersUtils.GetSymbolListExceptByName(writtenSymbolsAfterInline, 
                    writtenSymbolsBeforeInline);

                // Calculate the symbols that are removed by inlining method.
                var missingSymbols = ConditionCheckersUtils.GetSymbolListExceptByName(writtenSymbolsBeforeInline,
                    writtenSymbolsAfterInline);

                // Remove 'this' symbol, it is trivial to include.
                addedSymbols = ConditionCheckersUtils.RemoveThisSymbol(addedSymbols);
                missingSymbols = ConditionCheckersUtils.RemoveThisSymbol(missingSymbols);

                // If found any missing and additional symbols, return a code issue computer.
                if(addedSymbols.Any() || missingSymbols.Any())
                {
                    logger.Info("Additional changed symbols: " + StringUtil.ConcatenateAll(",", addedSymbols.Select(s => s.Name)));
                    logger.Info("Missing changed symbols: " + StringUtil.ConcatenateAll(",", missingSymbols.Select(s => s.Name)));
                    return new ModifiedFlowOutData(refactoring.CallerMethodAfter, refactoring.InlinedMethod, refactoring.InlinedMethodInvocation,
                        refactoring.InlinedStatementsInMethodAfter, addedSymbols, missingSymbols);
                }
                return new NullCodeIssueComputer();
            }

    

            private class ModifiedFlowOutData : ValidCodeIssueComputer
            {
                private readonly IEnumerable<ISymbol> missingSymbols;
                private readonly IEnumerable<ISymbol> addedSymbols;
                private readonly SyntaxNode methodAfter;
                private readonly SyntaxNode inlinedMethod;
                private readonly SyntaxNode inlinedMethodInvocation;
                private readonly IEnumerable<SyntaxNode> inlinedStatements;
          

                internal ModifiedFlowOutData(SyntaxNode methodAfter, SyntaxNode inlinedMethod, SyntaxNode inlinedMethodInvocation,
                    IEnumerable<SyntaxNode> inlinedStatements, IEnumerable<ISymbol> addedSymbols, IEnumerable<ISymbol> missingSymbols)
                {
                    this.methodAfter = methodAfter;
                    this.inlinedMethod = inlinedMethod;
                    this.inlinedMethodInvocation = inlinedMethodInvocation;
                    this.inlinedStatements = inlinedStatements;
                    this.addedSymbols = addedSymbols;
                    this.missingSymbols = missingSymbols;
                }

                public override bool Equals(ICodeIssueComputer o)
                {
                    if(o is ModifiedFlowOutData)
                    {
                        var other = (ModifiedFlowOutData) o;
                        if(ConditionCheckersUtils.CompareSymbolListByName(other.missingSymbols, this.missingSymbols))
                        {
                            if(ConditionCheckersUtils.CompareSymbolListByName(other.addedSymbols, this.addedSymbols))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }

                public override RefactoringType RefactoringType
                {
                    get { return RefactoringType.INLINE_METHOD; }
                }

                public override IEnumerable<CodeIssue> ComputeCodeIssues(IDocument document, SyntaxNode node)
                {
                    // The node should be a statement instance and the document is correct.
                    if(node is StatementSyntax && IsDocumentRight(document))
                    {
                        // Get the methodAfter containing the node.
                        var method = GetContainingMethod(node);

                        // If the outside methodAfter can be found and the methodAfter has the same name with the inlined methodAfter.
                        if(method != null && ASTUtil.AreMethodsNameSame(method, methodAfter))
                        {
                            // Get the statements in the found methodAfter that map with detected inlined statements.
                            var statements = GetCurrentInlinedStatements(method);

                            // If current node is among these statemens, return a code issue at the node.
                            if(statements.Contains(node))
                            {
                                yield return new CodeIssue(CodeIssue.Severity.Error, node.Span, GetDescription(), 
                                    new ICodeAction[]{new ModifiedFlowOutDataFix(document, methodAfter, inlinedMethod, inlinedMethodInvocation, 
                                        inlinedStatements, addedSymbols, missingSymbols)});
                            }
                        }   
                    }
                }

                /* Get the description of the issue. */
                private string GetDescription()
                {
                    var sb = new StringBuilder();
                    if (addedSymbols.Any())
                    {
                        sb.AppendLine("Inlined method may change variables: " +
                                      StringUtil.ConcatenateAll(",", addedSymbols.Select(s => s.Name)));
                    }
                    if (missingSymbols.Any())
                    {
                        sb.AppendLine("Inlined method may fail to change variables: " +
                                      StringUtil.ConcatenateAll(",", missingSymbols.Select(s => s.Name)));
                    }
                    return sb.ToString();
                }

                /* Is the document where the inline methodAfter refactoring happened? */
                private bool IsDocumentRight(IDocument document)
                {
                    // Get the qualified name of the type that encloses the methodAfter.
                    var analyzer = AnalyzerFactory.GetQualifiedNameAnalyzer();
                    analyzer.SetSyntaxNode(methodAfter);
                    var containingMethodName = analyzer.GetOutsideTypeQualifiedName();

                    // Get the qualified names of types that are contained in the document.
                    analyzer.SetSyntaxNode((SyntaxNode)document.GetSyntaxRoot());
                    var documentContainedNames = analyzer.GetInsideQualifiedNames();

                    // If the type names in the document contains the name we want. 
                    return documentContainedNames.Contains(containingMethodName);
                }

                /* Get the methodAfter that encloses a syntax node. */
                private SyntaxNode GetContainingMethod(SyntaxNode node)
                {
                    SyntaxNode method;
                    for (method = node; method != null && method.Kind != SyntaxKind.MethodDeclaration; method = method.Parent) ;
                    return method;
                }

                /* Get statements in the current methodAfter that map with the previously detected inlined statements. */
                private IEnumerable<SyntaxNode> GetCurrentInlinedStatements(SyntaxNode method)
                {
                    var list = new List<SyntaxNode>();
                    
                    // Get statements in the current methodAfter. 
                    var analyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
                    analyzer.SetMethodDeclaration(method);
                    var statements = analyzer.GetStatements();

                    // If any of the statements is same with the detected statement, add it to the list.
                    foreach (var statement in statements)
                    {
                        if(inlinedStatements.Any(i => ASTUtil.AreSyntaxNodesSame(i, statement)))
                        {
                            list.Add(statement);
                        }
                    }

                    // Get the longest group of sequential statements.
                    return GetSequentialStatements(list);
                }

                // Get the longest sequential statements.
                private IEnumerable<SyntaxNode> GetSequentialStatements(IEnumerable<SyntaxNode> list)
                {
                    var analyzer = AnalyzerFactory.GetSyntaxNodesAnalyzer();
                    analyzer.SetSyntaxNodes(list);
                    list = analyzer.RemoveSubNodes();
                    analyzer.SetSyntaxNodes(list);
                    return analyzer.GetLongestNeighborredNodesGroup();
                }


                private class ModifiedFlowOutDataFix : ICodeAction
                {
                    private readonly Logger logger;
                    private readonly IDocument document;
                    private readonly SyntaxNode methodAfter;
                    private readonly SyntaxNode inlinedMethod;
                    private readonly SyntaxNode inlinedMethodInvocation;
                    private readonly IEnumerable<SyntaxNode> inlinedStatements;
                    private readonly IEnumerable<ISymbol> addedSymbols;
                    private readonly IEnumerable<ISymbol> missingSymbols;
                    
                    internal ModifiedFlowOutDataFix(IDocument document, SyntaxNode methodAfter, 
                        SyntaxNode inlinedMethod, SyntaxNode inlinedMethodInvocation,
                        IEnumerable<SyntaxNode> inlinedStatements, 
                        IEnumerable<ISymbol> addedSymbols, IEnumerable<ISymbol> missingSymbols)
                    {
                        this.logger = NLoggerUtil.GetNLogger(typeof (ModifiedFlowOutDataFix));
                        this.document = document;
                        this.methodAfter = methodAfter;
                        this.inlinedMethod = inlinedMethod;
                        this.inlinedMethodInvocation = inlinedMethodInvocation;
                        this.inlinedStatements = inlinedStatements;
                        this.addedSymbols = addedSymbols;
                        this.missingSymbols = missingSymbols;
                    }

                    public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
                    {
                        var modifidStatements = inlinedStatements;

                        // If additional symbols are modified, add statements to fix the problem. 
                        if(addedSymbols.Any())
                        {
                            foreach (var s in addedSymbols)
                            {
                                modifidStatements = AddAddedSymbolsFixStatements(modifidStatements, s);
                            }
                        }

                        // If missing symbols that should be modified, add statement to fix this problem.
                        if(missingSymbols.Any())
                        {
                            modifidStatements = AddMissingSymbolsFixStatements(modifidStatements);
                        }

                        // Update methodAfter by changing the inlined statements with updated statements. 
                        var updatedMethod = UpdateMethodStatements(methodAfter, inlinedStatements, modifidStatements);
                        
                        logger.Debug("Inlined statements are:" + Environment.NewLine + StringUtil.ConcatenateAll
                            (Environment.NewLine, inlinedStatements.Select(s => s.GetText())));
                        logger.Debug("After Fixing, inlined statements are:" + Environment.NewLine + StringUtil.ConcatenateAll
                            (Environment.NewLine, modifidStatements.Select(s => s.GetText())));
                        logger.Debug(updatedMethod);

                        // Update root and document, return the code edition. 
                        var root = (SyntaxNode) document.GetSyntaxRoot();
                        var updatedRoot = root.ReplaceNodes(new[] {methodAfter}, (node1, node2) => updatedMethod);
                        return new CodeActionEdit(document.UpdateSyntaxRoot(updatedRoot));
                    }


                    private IEnumerable<SyntaxNode> AddMissingSymbolsFixStatements(IEnumerable<SyntaxNode> statements)
                    {
                        // Get the statement where the invocation of the inline method is.
                        var invokingStatement = ConditionCheckersUtils.GetStatementEnclosingNode(inlinedMethodInvocation);
                        
                        // Get the return statements of the inlined method.
                        var analyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
                        analyzer.SetMethodDeclaration(inlinedMethod);
                        var returnStatements = analyzer.GetReturnStatements();

                        // Select the most meaningful returned expression.
                        var returnedExpression = GetMeaningfulReturnedExpression(returnStatements);
                        
                        // If the returned expression can be found.
                        if(returnedExpression != null)
                        {
                            // Construct a fixing statement, the fixing statement is simply copying the statement of invocation, and
                            // replace the invocation with the returned expression in the body of the inlined method. The expression will be
                            // enclosed by a pair of parentheses before replacement.
                            var fixingStatement = invokingStatement.ReplaceNodes(new[] { inlinedMethodInvocation }, 
                                (node1, node2) => AddParenthesesToExpression(returnedExpression));
                            var updatedStatements = new List<SyntaxNode>();
                            updatedStatements.AddRange(statements);
                            updatedStatements.Add(fixingStatement);
                            return updatedStatements;
                        }
                        return statements;
                    }

                    /* 
                     * For all the given return statements, select the ones that are returning non null. If there are multiple such statements, return
                     * the last one in the method body.
                     */
                    private SyntaxNode GetMeaningfulReturnedExpression(IEnumerable<SyntaxNode> returnStatements)
                    {
                        var list = new List<SyntaxNode>();
                        var analyzer = AnalyzerFactory.GetReturnStatementAnalyzer();
                        foreach (var statement in returnStatements)
                        {
                            analyzer.SetReturnStatement(statement);
                            if(!analyzer.IsReturningNull())
                            {
                                list.Add(analyzer.GetReturnedExpression());
                            }
                        }
                        return list.Last();
                    }

                    /* Add a pair of parentheses to a given expression. */
                    private SyntaxNode AddParenthesesToExpression(SyntaxNode expression)
                    {
                        return Syntax.ParseExpression("(" + expression.GetText() + ")");
                    }


                    private IEnumerable<SyntaxNode> AddAddedSymbolsFixStatements(IEnumerable<SyntaxNode> statements, ISymbol s)
                    {
                        // Temp local variable to save the original value.
                        var tempName = "original" + s.Name;

                        // Assign the additional symbol to the temp.
                        var assign = Syntax.ParseStatement("var "+ tempName + " = " + s.Name +";");

                        // Assign the temp variable back to the symbol.
                        var assignBack = Syntax.ParseStatement(s.Name + " = " + tempName + ";");

                        // Add the assignment and assignment back to the proper positions of these statements.
                        var list = new List<SyntaxNode>();
                        list.Add(assign);
                        list.AddRange(statements);
                        list.Add(assignBack);
                        return list;
                    }

                    private SyntaxNode UpdateMethodStatements(SyntaxNode method, IEnumerable<SyntaxNode> originalStatements, 
                        IEnumerable<SyntaxNode> newStatements)
                    {
                        // Get the block and the statements in the block of the given methodAfter.
                        var analyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
                        analyzer.SetMethodDeclaration(method);
                        var block = (BlockSyntax) analyzer.GetBlock();
                        var statements = block.Statements;
                        
                        // Get the start and end position of these inlined statements.
                        var start = statements.IndexOf((StatementSyntax) originalStatements.First());
                        var end = statements.IndexOf((StatementSyntax) originalStatements.Last());
                        logger.Debug("Start: " + start);
                        logger.Debug("End: " + end);

                        // New updated statements.
                        var updatedStatements = new List<SyntaxNode>();
                        
                        // Copy the statements before inlined statements to the updated statements list.
                        for (int i = 0; i < start; i ++ )
                        {
                            updatedStatements.Add(statements.ElementAt(i));
                        }
                        
                        // Get the trivia of the last statement.
                        var leadingSpace = updatedStatements.Last().GetLeadingTrivia();
                        var trailingSpace = updatedStatements.Last().GetTrailingTrivia();

                        // Copy the udpated inlined statements to the list.
                        updatedStatements.AddRange(newStatements.Select(s => ((StatementSyntax)s).
                            WithLeadingTrivia(leadingSpace).WithTrailingTrivia(trailingSpace)).ToArray());
                        
                        // Copy the statements after the inlined statements back to the list.
                        for(int i = end + 1; i< statements.Count; i++ )
                        {
                            updatedStatements.Add(statements.ElementAt(i));
                        }
                        logger.Debug("Updated statements count:" + updatedStatements.Count);

                        // Get a new block with the updated statement list.
                        var updatedBlock = block.Update(block.OpenBraceToken, ASTUtil.GetSyntaxList(updatedStatements), 
                            block.CloseBraceToken);
                        logger.Debug("Updated block:" + Environment.NewLine + updatedBlock.GetText());

                        // Update the block in the methodAfter.
                        return new BlockRewriter(updatedBlock).Visit(method);
                    }

                    private class BlockRewriter : SyntaxRewriter
                    {
                        private readonly SyntaxNode updatedBlock;

                        internal BlockRewriter(SyntaxNode updatedBlock)
                        {
                            this.updatedBlock = updatedBlock;
                        }

                        public override SyntaxNode VisitBlock(BlockSyntax node)
                        {
                            return updatedBlock;
                        }
                    }

                    public ImageSource Icon
                    {
                        get { return ResourcePool.GetIcon(); }
                    }

                    public string Description
                    {
                        get { return "Fix the changed data by inlining methodAfter."; }
                    }
                }
            }
        }
    }
}
