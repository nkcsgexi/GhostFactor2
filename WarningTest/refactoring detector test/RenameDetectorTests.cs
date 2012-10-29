using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using warnings.refactoring.detection;
using warnings.retriever;
using warnings.util;

namespace WarningTest
{
    [TestClass]
    public class RenameDetectorTests
    {
        private readonly SyntaxNode before;
        
        private readonly SyntaxNode after;

        private readonly IExternalRefactoringDetector detector;
        
        private readonly Logger logger;

        public RenameDetectorTests()
        {
            var sourceBefore = FileUtil.ReadAllText(TestUtil.GetFakeSourceFolder() + "RenameDetectorExampleBefore.txt");
            var sourceAfter = FileUtil.ReadAllText(TestUtil.GetFakeSourceFolder() + "RenameDetectorExampleAfter.txt");
            before = ASTUtil.GetSyntaxTreeFromSource(sourceBefore).GetRoot();
            after = ASTUtil.GetSyntaxTreeFromSource(sourceAfter).GetRoot();
            detector = RefactoringDetectorFactory.CreateRenameDetector();
            logger = NLoggerUtil.GetNLogger(typeof (RenameDetectorTests));
        }

        private SyntaxNode ModifyIdentifierInAfterSource(SyntaxNode node, int idIndex,String newName)
        {
            var retriever = RetrieverFactory.GetRenamableRetriever();
            retriever.SetRoot(node);
            var tokens = retriever.GetIdentifierNodes();
            Assert.IsTrue(idIndex < tokens.Count());
            TextSpan span = tokens.ElementAt(idIndex).Span;
            string beforeTokenCode = node.GetText().Substring(0, span.Start);
            string afterTokenCode = node.GetText().Substring(span.End);
            return ASTUtil.GetSyntaxTreeFromSource(beforeTokenCode + newName + afterTokenCode).GetRoot();
        }



        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(before);
            Assert.IsNotNull(after);
            Assert.IsNotNull(detector);
            detector.SetSourceBefore(before.GetText());
            detector.SetSourceAfter(after.GetText());
            Assert.IsFalse(detector.HasRefactoring());
        }

        [TestMethod]
        public void TestMethod2()
        {
            detector.SetSourceBefore(before.GetText());
            for (int i = 0; i < 100; i++)
            {
                // Change one identifier.
                var changedAfter = ModifyIdentifierInAfterSource(after, i, "newNameInjectedForTest");
                detector.SetSourceAfter(changedAfter.GetText());
                Assert.IsTrue(detector.HasRefactoring());
            }
        }
        [TestMethod]
        public void TestMethod3()
        {
            detector.SetSourceBefore(before.GetText());

            // Change two identifiers.
            var changedAfter = ModifyIdentifierInAfterSource
                (ModifyIdentifierInAfterSource(after, 50, "newNameInjectedForTest"), 60, "newNameInjectedForTest");
            detector.SetSourceAfter(changedAfter.GetText());
            Assert.IsFalse (detector.HasRefactoring());

        }

    }
}
