using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Compilers.CSharp;
using warnings.analyzer;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class CallGraphTest
    {
        private readonly string path =
            @"D:\VS workspaces\BeneWar\warnings\WarningTest\CallGraphTest.cs";
        private readonly string anotherPath =
            @"D:\VS workspaces\BeneWar\warnings\WarningTest\MethodBodyRetrieverTest.cs";

        private readonly string source;
        private readonly SyntaxTree tree;
        private readonly ClassDeclarationSyntax classDeclaration;
        private readonly CallGraph graph;


        private readonly string anotherSource;
        private readonly SyntaxTree anotherTree;
        private readonly ClassDeclarationSyntax anotherClass;
        private readonly CallGraph anotherGraph;

        public CallGraphTest()
        {
            source = FileUtil.ReadAllText(path);
            tree = ASTUtil.GetSyntaxTreeFromSource(source);
            classDeclaration = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().
                First(c => c.Identifier.Value.Equals("CallGraphTest"));
            graph = new CallGraphBuilder(classDeclaration, tree).BuildCallGraph();



        }

        /* Test the vertices. */
        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsTrue(graph.hasVertice("TestMethod1"));
            Assert.IsTrue(graph.hasVertice("TestMethod2"));
            Assert.IsTrue(graph.hasVertice("foo"));
            Assert.IsTrue(graph.hasVertice("bar"));
            Assert.IsFalse(graph.hasVertice("RandomName1"));
            Assert.IsFalse(graph.hasVertice("RandomName2"));
            Assert.IsFalse(graph.hasVertice("RandomName3"));
            Assert.IsFalse(graph.hasVertice(""));
        }

        /* Test the edges in the call graph. */
        [TestMethod]
        public void TestMethod2()
        {
            foo();
            bar();
            Assert.IsTrue(graph.hasEdge("TestMethod2", "foo"));
            Assert.IsTrue(graph.hasEdge("TestMethod2", "bar"));
            Assert.IsTrue(graph.hasEdge("foo", "bar"));
            Assert.IsFalse(graph.hasEdge("bar", "foo"));
            Assert.IsFalse(graph.hasEdge("foo", "TestMethod2"));
            Assert.IsFalse(graph.hasEdge("bar", "TestMethod2"));
        }

        private void foo()
        {
            bar();
        }

        private void bar()
        {
            
        }

        [TestMethod]
        public void TestMethod3()
        {
            
        }
    }
}
