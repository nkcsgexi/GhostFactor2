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
using warnings.retriever;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class RenamableRetrieverTests
    {
        private readonly IRenamableRetriever retriever;

        private readonly Logger logger = NLoggerUtil.GetNLogger(typeof(MethodAnalyzerTests));

        private readonly SyntaxNode root;

        private readonly IDocument document;

        private readonly ISemanticModel model;

        public RenamableRetrieverTests()
        {
            var code = TestUtil.GetFakeSourceFolder() + "MethodAnalyzerExample.cs";
            var converter = new String2IDocumentConverter();
            this.document = (IDocument) converter.Convert(FileUtil.ReadAllText(code), null, null, null);
            model = document.GetSemanticModel();
            logger = NLoggerUtil.GetNLogger(typeof (MethodAnalyzerTests));
            root = (SyntaxNode) document.GetSyntaxRoot();
            retriever = RetrieverFactory.GetRenamableRetriever(); 
            retriever.SetRoot(root);
        }


        [TestMethod]
        public void TestMethod1()
        {
            var classes = retriever.GetClassDeclarationIdentifiers();
            Assert.IsTrue(classes.Count() == 1);
            Assert.IsTrue(classes.First().GetText().Equals("MethodAnalyzerExample"));
            var symbol = model.GetDeclaredSymbol(classes.First().Parent);
            Assert.IsTrue(symbol.Name.Equals("MethodAnalyzerExample"));

        }

        [TestMethod]
        public void TestMethod2()
        {
            var methods = retriever.GetMethodDeclarationIdentifiers();
            Assert.IsTrue(methods.Count() == 5);
            Assert.IsTrue(methods.ElementAt(0).GetText().Equals("method1"));
            Assert.IsTrue(methods.ElementAt(1).GetText().Equals("method2"));
            Assert.IsTrue(methods.ElementAt(2).GetText().Equals("method3"));
            Assert.IsTrue(methods.ElementAt(3).GetText().Equals("method4"));
            Assert.IsTrue(methods.ElementAt(4).GetText().Equals("method5"));
        }

        [TestMethod]
        public void TestMethod3()
        {
            var varibles = retriever.GetVariableDeclaratorIdentifiers();
            Assert.IsTrue(varibles.Count() == 5);
            Assert.IsTrue(varibles.ElementAt(0).GetText().Equals("field1"));
            Assert.IsTrue(varibles.ElementAt(1).GetText().Equals("field2"));
            Assert.IsTrue(varibles.ElementAt(2).GetText().Equals("field3"));
            Assert.IsTrue(varibles.ElementAt(3).GetText().Equals("variable4"));
            Assert.IsTrue(varibles.ElementAt(4).GetText().Equals("variable5"));
        }

        [TestMethod]
        public void TestMethod4()
        {
            var variables = retriever.GetMethodParameterDeclarationIdentifiers();
            Assert.IsTrue(variables.Count() == 6);
            Assert.IsTrue(variables.ElementAt(0).GetText().Equals("a"));
            Assert.IsTrue(variables.ElementAt(1).GetText().Equals("a"));
            Assert.IsTrue(variables.ElementAt(2).GetText().Equals("b"));
            Assert.IsTrue(variables.ElementAt(3).GetText().Equals("c"));
            Assert.IsTrue(variables.ElementAt(4).GetText().Equals("d"));
            Assert.IsTrue(variables.ElementAt(5).GetText().Equals("e"));
        }


        [TestMethod]
        public void TestMethod5()
        {
            var identifiers = retriever.GetAllDeclarationIdentifiers();
            foreach (var id in identifiers)
            {
                var s = model.GetDeclaredSymbol(id.Parent);
                Assert.IsNotNull(s);
                Assert.IsTrue(s.Name.Equals(id.ValueText));
            }
        }

        [TestMethod]
        public void TestMethod6()
        {
            var accesses = retriever.GetMemberAccesses();
            var refers = retriever.GetIdentifierNodes();
            foreach (var access in accesses)
            {
                var infor = model.GetSymbolInfo(access);
                logger.Info(access.GetText() + "," + infor.Symbol);
            }
            foreach (SyntaxNode refer in refers)
            {
                var infor = model.GetSymbolInfo(refer);
                logger.Info(refer.GetText() + "," + infor.Symbol);
            }
        }

     

    }
}
