using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class RoslynUtilTests
    {
        Logger logger = NLoggerUtil.GetNLogger(typeof(RoslynUtilTests));
        private int field1, field2;

        [TestMethod]
        public void TestMethod1()
        {
            ISolution solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            Assert.IsNotNull(solution);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            var project = RoslynUtil.GetProject(solution, "WarningTest");
            Assert.IsNotNull(project);
        }

        [TestMethod]
        public void TestMethod3()
        {
            var solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            var project = RoslynUtil.GetProject(solution, "WarningTest");
            var document = RoslynUtil.GetDocument(project, "ASTUtilTests.cs");
            Assert.IsNotNull(document);
            document = RoslynUtil.GetDocument(project, "EMDetectorAfter.cs");
            Assert.IsNotNull(document);
        }

        [TestMethod]
        public void TestMethod4()
        {
            String updatedString = "Updated String";
            var solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            var project = RoslynUtil.GetProject(solution, "WarningTest");
            var document = RoslynUtil.GetDocument(project, "TryToUpdate.cs");
            Assert.IsNotNull(document);
            document = RoslynUtil.UpdateDocumentToString(document, updatedString);  
            Assert.IsNotNull(document);
            Assert.IsTrue(document.GetText().GetText().Equals(updatedString));
        }

        [TestMethod]
        public void TestMethod5()
        {
            var solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            var project = RoslynUtil.GetProject(solution, "WarningTest");
            var document = RoslynUtil.GetDocument(project, "RoslynUtilTests.cs");
            var analyzer = AnalyzerFactory.GetDocumentAnalyzer();
            analyzer.SetDocument(document);
            Assert.IsNotNull(document);
            try
            {
                logger.Info(analyzer.DumpSyntaxTree());
            }catch( Exception e)
            {
                logger.Fatal(e);
            }
        }

        [TestMethod]
        public void TestMethod6()
        {
            var solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            var project = RoslynUtil.GetProject(solution, "WarningTest");
            Assert.IsNotNull(project);
            foreach (IDocument document in project.Documents)
            {
                string name = document.Name;
                logger.Info(name);
            }
        }

        [TestMethod]
        public void TestMethod7()
        {
            var solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            var analyzer = AnalyzerFactory.GetSolutionAnalyzer(); 
            analyzer.SetSolution(solution);
            logger.Info(analyzer.DumpSolutionStructure());
        }

        [TestMethod]
        public void TestMethod8()
        {
            var solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            var project = RoslynUtil.GetProject(solution, "WarningTest");
            var document = RoslynUtil.GetDocument(project, "RoslynUtilTests.cs");
            var analyzer = AnalyzerFactory.GetDocumentAnalyzer();
            analyzer.SetDocument(document);
            var symbol = GetFirstLocalVariable(document);
            logger.Info(symbol);
            Assert.IsNotNull(symbol);
        }

        private ISymbol GetFirstLocalVariable(IDocument document)
        {
            IDocumentAnalyzer analyzer = AnalyzerFactory.GetDocumentAnalyzer();
            analyzer.SetDocument(document);
            logger.Info(analyzer.DumpSyntaxTree);
            var first_namespace = analyzer.GetNamespaceDecalarations().First();
            var first_class = analyzer.GetClassDeclarations((NamespaceDeclarationSyntax)first_namespace).First();
            var first_method = analyzer.GetMethodDeclarations((ClassDeclarationSyntax)first_class).First();
            var first_variable = analyzer.GetVariableDeclarations((MethodDeclarationSyntax)first_method).First();
            logger.Info(first_variable);
            return analyzer.GetSymbol(first_method);
        }

        [TestMethod]
        public void TestMethod9()
        {
            var solution = RoslynUtil.GetSolution(TestUtil.GetSolutionPath());
            var project = RoslynUtil.GetProject(solution, "WarningTest");
            var document = RoslynUtil.GetDocument(project, "RoslynUtilTests.cs");
            var analyzer = AnalyzerFactory.GetDocumentAnalyzer();
            analyzer.SetDocument(document);

            var namespaceDec = analyzer.GetNamespaceDecalarations().First();
            Assert.IsNotNull(namespaceDec);
            Assert.IsNotNull(analyzer.GetSymbol(namespaceDec));

            var classDec = analyzer.GetClassDeclarations((NamespaceDeclarationSyntax) namespaceDec).First();
            Assert.IsNotNull(classDec);
            Assert.IsNotNull(analyzer.GetSymbol(classDec));

            var methodDec = analyzer.GetMethodDeclarations((ClassDeclarationSyntax) classDec).First();
            Assert.IsNotNull(methodDec);
            Assert.IsNotNull(analyzer.GetSymbol(methodDec));

            var fieldDec = analyzer.GetFieldDeclarations((ClassDeclarationSyntax) classDec).FirstOrDefault();
            Assert.IsNotNull(fieldDec);
            Assert.IsNotNull(analyzer.GetSymbol(fieldDec));

            var variableDec = analyzer.GetVariableDeclarations((MethodDeclarationSyntax) methodDec).FirstOrDefault();
            Assert.IsNotNull(variableDec);
            Assert.IsNotNull(analyzer.GetSymbol(variableDec));
        }

    }
}
