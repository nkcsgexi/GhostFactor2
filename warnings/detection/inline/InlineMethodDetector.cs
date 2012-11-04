using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.util;

namespace warnings.refactoring.detection
{
    internal class InlineMethodDetector : IExternalRefactoringDetector
    {
        private readonly Logger logger;
        private readonly IInMethodInlineDetector inMethodDetector;
        private readonly List<ManualRefactoring> refactorings;

        private string sourceAfter;
        private string sourceBefore;
     
        private IDocument beforeDoc;
        private IDocument afterDoc;



        internal InlineMethodDetector(IInMethodInlineDetector inMethodDetector)
        {
            this.logger = NLoggerUtil.GetNLogger(typeof (InlineMethodDetector));
            this.refactorings = new List<ManualRefactoring>();
            this.inMethodDetector = inMethodDetector;
        }

        public bool HasRefactoring()
        {
            refactorings.Clear();

            beforeDoc = RefactoringDetectionUtils.Convert2IDocument(sourceBefore);
            afterDoc = RefactoringDetectionUtils.Convert2IDocument(sourceAfter);

            // Parse the source code before and after to get the tree structures.
            var treeBefore = (SyntaxTree)beforeDoc.GetSyntaxTree();
            var treeAfter = (SyntaxTree)afterDoc.GetSyntaxTree();

            // Parse the before and after code and get the roots of these trees.
            var rootBefore = treeBefore.GetRoot();
            var rootAfter = treeAfter.GetRoot();

            // Get classes contained in these root.
            var classesBefore = ASTUtil.GetClassDeclarations(rootBefore);
            var classesAfter = ASTUtil.GetClassDeclarations(rootAfter);

            // Get the class pairs with common class name
            var commonNodePairs = RefactoringDetectionUtils.GetCommonNodePairs(classesBefore, classesAfter,
                RefactoringDetectionUtils.GetClassDeclarationNameComparer());
            var inClassDetector = new InClassInlineMethodDetector(beforeDoc, afterDoc, inMethodDetector);

            // For each pair of common class.
            foreach (var pair in commonNodePairs)
            {
                // Get the common class before and after. 
                var classBefore = pair.Key;
                var classAfter = pair.Value;

                // Invoke in class detector to find refactorings.
                inClassDetector.SetSyntaxNodeBefore(classBefore);
                inClassDetector.SetSyntaxNodeAfter(classAfter);
                if(inClassDetector.HasRefactoring())
                {
                    refactorings.AddRange(inClassDetector.GetRefactorings());
                }
            }
            return refactorings.Any();
        }

        public IEnumerable<ManualRefactoring> GetRefactorings()
        {
            return refactorings;
        }

        public void SetSourceBefore(string source)
        {
            this.sourceBefore = source;
        }

        public string GetSourceBefore()
        {
            return sourceBefore;
        }

        public void SetSourceAfter(string source)
        {
            this.sourceAfter = source;
        }

        public string GetSourceAfter()
        {
            return sourceAfter;
        }

        public IDocument GetBeforeDocument()
        {
            return beforeDoc;
        }

        public IDocument GetAfterDocument()
        {
            return afterDoc;
        }

        /* Inline refactoring detector in the class level. */
        private class InClassInlineMethodDetector : IInternalRefactoringDetector
        {
            private readonly Logger logger;
            private readonly List<ManualRefactoring> refactorings;
            private readonly IDocument beforeDoc;
            private readonly IDocument afterDoc;
            private readonly IInMethodInlineDetector inMethodDetector;

            private SyntaxNode beforeClass; 
            private SyntaxNode afterClass;
         

            internal InClassInlineMethodDetector(IDocument beforeDoc, IDocument afterDoc,
                IInMethodInlineDetector inMethodDetector)
            {
                logger = NLoggerUtil.GetNLogger(typeof (InClassInlineMethodDetector));
                refactorings = new List<ManualRefactoring>();
                this.beforeDoc = beforeDoc;
                this.afterDoc = afterDoc;
                this.inMethodDetector = inMethodDetector;
            }

            public bool HasRefactoring()
            {
                refactorings.Clear();

                // Get the methods in before and after class.
                var methodsBefore = ASTUtil.GetMethodsDeclarations(beforeClass);
                var methodsAfter = ASTUtil.GetMethodsDeclarations(afterClass);

                // Get the common methods in before and after class. Common means same name.
                var commonMethodsPairs = RefactoringDetectionUtils.GetCommonNodePairs(methodsBefore, 
                    methodsAfter, RefactoringDetectionUtils.GetMethodDeclarationNameComparer());

                // Get the methods that are in the before version but are not in the after version.
                var removedMethodsBefore = methodsBefore.Except(commonMethodsPairs.Select(p => p.Key));
            
                // For each removed method.
                foreach (var removed in removedMethodsBefore)
                {
                    // For each pair of common methods.
                    foreach (var pair in commonMethodsPairs)
                    {
                        // Get the invocations of the removed method. 
                        var invocations = ASTUtil.GetAllInvocationsInMethod(pair.Key, removed, 
                            (SyntaxTree) beforeDoc.GetSyntaxTree());

                        // If such invocations exist
                        if(invocations.Any())
                        {
                            // Configure the in method detector.
                            inMethodDetector.SetSyntaxNodeBefore(pair.Key);
                            inMethodDetector.SetSyntaxNodeAfter(pair.Value);
                            inMethodDetector.SetRemovedMethod(removed);
                            inMethodDetector.SetRemovedInvocations(invocations);
                            inMethodDetector.SetDocumentBefore(beforeDoc);
                            inMethodDetector.SetDocumentAfter(afterDoc);

                            // If a refactoring is detected, add it to the list.
                            if(inMethodDetector.HasRefactoring())
                            {
                                refactorings.AddRange(inMethodDetector.GetRefactorings());
                            }
                        }
                    }
                }
                return refactorings.Any();
            }

            public IEnumerable<ManualRefactoring> GetRefactorings()
            {
                return refactorings;
            }

            public void SetSyntaxNodeBefore(SyntaxNode before)
            {
                this.beforeClass = before;
            }

            public void SetSyntaxNodeAfter(SyntaxNode after)
            {
                this.afterClass = after;
            }
        }


        public RefactoringType RefactoringType { get { return RefactoringType.INLINE_METHOD; } }
    }
}
