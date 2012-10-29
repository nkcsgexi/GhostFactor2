using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace WarningTest.analyzer_test
{
    [TestClass]
    public class MethodInvocationAnalyzerTests
    {
        private readonly IMethodInvocationAnalyzer analyzer;
        private IDocument document;
        private IEnumerable<SyntaxNode> invocations;
        private readonly Logger logger;

        public MethodInvocationAnalyzerTests()
        {
            this.analyzer = AnalyzerFactory.GetMethodInvocationAnalyzer();
            var converter = new String2IDocumentConverter();
            this.document = (IDocument) converter.Convert(FileUtil.ReadAllText(
                TestUtil.GetFakeSourceFolder() + "ChangeMethodSignatureAfter.txt"), null, null, null);
            this.invocations = ((SyntaxNode)document.GetSyntaxRoot()).
                DescendantNodes().Where(i => i.Kind == SyntaxKind.InvocationExpression);
            this.logger = NLoggerUtil.GetNLogger(typeof(MethodInvocationAnalyzerTests));
        }

        SyntaxNode GetInvocation(string method)
        {
            foreach (var invocation in invocations)
            {
                analyzer.SetMethodInvocation(invocation);
                if(analyzer.GetMethodName().GetText().Equals(method))
                {
                    return invocation;
                }
            }
            return null;
        }

        IEnumerable<Tuple<int, int>> GetReverseMaps(int count)
        {
            var results = new List<Tuple<int, int>>();
            for (int i = 0; i < count; i++)
            {
                results.Add(Tuple.Create(i, count - i - 1));
            }
            return results.AsEnumerable();
        }
            
        [TestMethod]
        public void TestMethod1()
        {
            try
            {
                var invocation = GetInvocation("changeSignatureExample0");
                var before = invocation.GetText();
                Assert.IsNotNull(document);
                Assert.IsNotNull(invocation);
                analyzer.SetMethodInvocation(invocation);
                var after = analyzer.ReorderAuguments(GetReverseMaps(3)).GetText();
                Assert.IsTrue(before.Equals(invocation.GetText()));
                Assert.IsFalse(before.Equals(after));
                logger.Info(before);
                logger.Info(after);
            }catch(Exception e)
            {
                logger.Fatal(e);
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            var invocation = GetInvocation("changeSignatureExample0");
            analyzer.SetMethodInvocation(invocation);
            analyzer.AddArguments(new string[] {"name"});

        }
    }
}
