using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.conditions;
using warnings.refactoring;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class ExtractMethodConditionCheckTests
    {
        private Logger logger = NLoggerUtil.GetNLogger(typeof (ExtractMethodConditionCheckTests));

        private IDocument before, after;

        private IEnumerable<SyntaxNode> beforeMethods;

        private IEnumerable<SyntaxNode> afterMethods;

        private IRefactoringConditionsList conditionsList = ConditionCheckingFactory.GetExtractMethodConditionsList();

        public ExtractMethodConditionCheckTests()
        {
            string fileBefore = TestUtil.GetFakeSourceFolder() + "EMDetectorBefore.cs";
            string fileAfter = TestUtil.GetFakeSourceFolder() + "EMDetectorAfter.cs";
            var converter = new String2IDocumentConverter();

            // Get before and after document.
            before = (IDocument) converter.Convert(FileUtil.ReadAllText(fileBefore), typeof(IDocument), null, null);
            after = (IDocument)converter.Convert(FileUtil.ReadAllText(fileAfter), typeof(IDocument), null, null);

            // Get all the methods in before and after.
            beforeMethods = GetAllMethod(before);
            afterMethods = GetAllMethod(after);

        }

        private IEnumerable<SyntaxNode> GetAllMethod(IDocument doc)
        {
            var docAnalyzer = AnalyzerFactory.GetDocumentAnalyzer();
            docAnalyzer.SetDocument(doc);
            IEnumerable<SyntaxNode> methods = docAnalyzer.GetMethodDeclarations(
                docAnalyzer.GetClassDeclarations(
                    docAnalyzer.GetNamespaceDecalarations().First()).First());
            return methods;
        }

        /* Get the method declaration of the given name in the after document. */
        private SyntaxNode GetExtractedMethodDeclaration(string methodName)
        {
            return afterMethods.First(n => ((MethodDeclarationSyntax)n).Identifier.Value.Equals(methodName));
        }

        /* Get invocations of the method (calleeName) in the method (callername) of the after document. */
        private IEnumerable<SyntaxNode> GetInvokingExtractedMethod(string callerName, string calleeName)
        {
            var invokingMethood = afterMethods.First(n => ((MethodDeclarationSyntax)n).Identifier.Value.Equals(callerName));
            var invs = invokingMethood.DescendantNodes().Where(n => n.Kind == SyntaxKind.InvocationExpression);
            var invocationAnalyzer = AnalyzerFactory.GetMethodInvocationAnalyzer();
            var invocations = new List<SyntaxNode>();
            foreach (var inv in invs)
            {
                invocationAnalyzer.SetMethodInvocation(inv);
                if (invocationAnalyzer.GetMethodName().GetText().Equals(calleeName))
                    invocations.Add(inv);
            }
            return invocations.AsEnumerable();
        }

        /* Get statements in the method (methodName) of the before document, given the start and end index. */
        private IEnumerable<SyntaxNode> GetStatementsBeforeExtract(string methodName, int start, int end)
        {
            var originalMethod = beforeMethods.First(n => ((MethodDeclarationSyntax)n).Identifier.Value.Equals(methodName));
            var methodAnalyzer = AnalyzerFactory.GetMethodDeclarationAnalyzer();
            methodAnalyzer.SetMethodDeclaration(originalMethod);
            return methodAnalyzer.GetStatementsByIndexRange(start, end);
        }


        /* Get the manual refactoring extracts statements indexed 'start' to 'end' in 'methodN' (before) to 'extractedN' (after)*/
        private IManualExtractMethodRefactoring GetTestInput(int methodIndex, int start, int end)
        {
            var declaration = GetExtractedMethodDeclaration("extracted" + methodIndex);
            var invocation = GetInvokingExtractedMethod("method" + methodIndex, "extracted" + methodIndex).First();
            var statements = GetStatementsBeforeExtract("method" + methodIndex, start, end);
            return ManualRefactoringFactory.
                CreateManualExtractMethodRefactoring(declaration, invocation, statements);
        }
            
            
        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(before);
            Assert.IsNotNull(after);
            Assert.IsTrue(beforeMethods.Any());
            Assert.IsTrue(afterMethods.Any());
        }

        [TestMethod]
        public void TestMethod2()
        {
            var refactoring = GetTestInput(1, 0, 2);
            var results = conditionsList.CheckAllConditions(before, after, refactoring);
           
        }

        [TestMethod]
        public void TestMethod3()
        {
            var refactoring = GetTestInput(2, 2, 2);
            var results = conditionsList.CheckAllConditions(before, after, refactoring);
            
        }

        [TestMethod]
        public void TestMethod4()
        {
            var refactoring = GetTestInput(3, 1, 1);
            var results = conditionsList.CheckAllConditions(before, after, refactoring);
        }

        [TestMethod]
        public void TestMethod5()
        {
            var refactoring = GetTestInput(4, 2, 2);
            var results = conditionsList.CheckAllConditions(before, after, refactoring);
        }

     
    }
}
