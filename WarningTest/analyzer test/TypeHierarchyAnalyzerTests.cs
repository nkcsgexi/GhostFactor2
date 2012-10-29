using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace WarningTest.analyzer_test
{
    [TestClass]
    public class TypeHierarchyAnalyzerTests
    {
        private readonly IDocument document;
        private readonly ITypeHierarchyAnalyzer analyzer;
        private readonly SyntaxNode root;

        public TypeHierarchyAnalyzerTests()
        {
            var source = FileUtil.ReadAllText(TestUtil.GetFakeSourceFolder() + "/TypeHierarchyFakeSource.cs");
            var converter = new String2IDocumentConverter();
            document = (IDocument)converter.Convert(source, null, null, null);
            analyzer = AnalyzerFactory.GetTypeHierarchyAnalyzer();
            analyzer.SetSemanticModel(document.GetSemanticModel());
            root = (SyntaxNode) document.GetSyntaxRoot();
        }

        SyntaxNode GetInterfaceDeclaration(string identifier)
        {
            var interfaces = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.InterfaceDeclaration).
                Select(n =>(InterfaceDeclarationSyntax)n);
            return interfaces.First(i => i.Identifier.ValueText.Equals(identifier));
        }

        SyntaxNode GetClassDeclaration(string identifier)
        {
            var classes = root.DescendantNodes().Where(n => n.Kind == SyntaxKind.ClassDeclaration).
                Select(n => (ClassDeclarationSyntax)n);
            return classes.First(c => c.Identifier.ValueText.Equals(identifier));
        }

        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(document);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var dec = GetClassDeclaration("TypeHierarchyFakeSource");
            Assert.IsNotNull(dec);
            analyzer.SetDeclaration(dec);
            var type = analyzer.GetDeclarationType();
            Assert.IsNotNull(type);
        }   

        [TestMethod]
        public void TestMethod3()
        {
            var dec = GetClassDeclaration("TypeHierarchyFakeSource");
            analyzer.SetDeclaration(dec);
            var baseTypes = analyzer.GetBaseTypes();
            Assert.IsTrue(baseTypes.Count() == 3);
            Assert.IsTrue(baseTypes.ElementAt(0).Name.Equals("HierarchyFakeSource"));
            Assert.IsTrue(baseTypes.ElementAt(1).Name.Equals("FakeSource"));
            Assert.IsTrue(baseTypes.ElementAt(2).Name.Equals("Object"));
        }

        [TestMethod]
        public void TestMethod4()
        {
            var dec = GetClassDeclaration("TypeHierarchyFakeSource");
            analyzer.SetDeclaration(dec);
            var interfaces = analyzer.GetImplementedInterfaces();
            Assert.IsTrue(interfaces.Count() == 2);
            Assert.IsTrue(interfaces.ElementAt(0).Name.Equals("IFakeSource"));
            Assert.IsTrue(interfaces.ElementAt(1).Name.Equals("IFake"));
        }

        [TestMethod]
        public void TestMethod5()
        {
            var dec = GetClassDeclaration("TypeHierarchyFakeSource");
            analyzer.SetDeclaration(dec);
            var containedType = analyzer.GetContainedTypes();
            Assert.IsTrue(containedType.Count() == 3);
            Assert.IsTrue(containedType.ElementAt(0).Name.Equals("InnerClassLevelOne1"));
            Assert.IsTrue(containedType.ElementAt(1).Name.Equals("InnerClassLevelOne2"));
            Assert.IsTrue(containedType.ElementAt(2).Name.Equals("InnerClassLevelOne3"));
        }

        [TestMethod]
        public void TestMethod6()
        {
            var dec = GetClassDeclaration("InnerClassLevelTwo");
            analyzer.SetDeclaration(dec);
            var containingTypes = analyzer.GetContainingTypes();
            Assert.IsTrue(containingTypes.Count() == 2);
            Assert.IsTrue(containingTypes.ElementAt(0).Name.Equals("InnerClassLevelOne1"));
            Assert.IsTrue(containingTypes.ElementAt(1).Name.Equals("TypeHierarchyFakeSource"));
        }
    }
}
