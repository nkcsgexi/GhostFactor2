using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.analyzer.comparators;
using warnings.util;

namespace warnings.refactoring.detection
{
    /// <summary>
    /// This is a detector for extract method refactoring. After setting the code before and after some time      
    /// interval, the detector should be able to tell whether there is a reafactoring performed. 
    /// </summary>
    internal class ExtractMethodDetector : IExternalRefactoringDetector
    {
        private String before;
        private String after;

        private IDocument beforeDoc;
        private IDocument afterDoc;
        
        /* Detected manual refactorings.*/
        private IEnumerable<ManualRefactoring> refactorings;

        private readonly InMethodExtractMethodDetector inMethodDetector;
        private readonly Logger logger;
  

        internal ExtractMethodDetector(InMethodExtractMethodDetector inMethodDetector)
        {
            this.inMethodDetector = inMethodDetector;
            this.logger = NLoggerUtil.GetNLogger(typeof (ExtractMethodDetector));
        }

        public void SetSourceBefore(String before)
        {
            this.before = before;
            this.beforeDoc = RefactoringDetectionUtils.Convert2IDocument(before);
        }

        public string GetSourceBefore()
        {
            return before;
        }

        public void SetSourceAfter(String after)
        {
            this.after = after;
            this.afterDoc = RefactoringDetectionUtils.Convert2IDocument(after);
        }

        public string GetSourceAfter()
        {
            return after;
        }

        public IDocument GetBeforeDocument()
        {
            return beforeDoc;
        }

        public IDocument GetAfterDocument()
        {
            return afterDoc;
        }

        public bool HasRefactoring()
        {
            refactorings = Enumerable.Empty<ManualRefactoring>();

            var treeBefore = (SyntaxTree) beforeDoc.GetSyntaxTree();
            var treeAfter = (SyntaxTree) afterDoc.GetSyntaxTree();

            // Get the classes in the code before and after.
            var classesBefore = treeBefore.GetRoot().DescendantNodes(n => n.Kind != 
                SyntaxKind.MethodDeclaration).Where(n => n.Kind == SyntaxKind.ClassDeclaration);
            var classesAfter = treeAfter.GetRoot().DescendantNodes(n => n.Kind != 
                SyntaxKind.MethodDeclaration).Where(n => n.Kind == SyntaxKind.ClassDeclaration);
            
            // Get the pairs of class declaration in the code before and after class;
            var pairs = RefactoringDetectionUtils.GetCommonNodePairs(classesBefore, classesAfter,
                new ClassNameComparer());
            
            foreach (var pair in pairs)
            {
                // Configure in class detector.
                var detector = new InClassExtractMethodDetector(
                    (ClassDeclarationSyntax)pair.Key, (ClassDeclarationSyntax)pair.Value, 
                        inMethodDetector);
                detector.SetDocumentBefore(beforeDoc);
                detector.SetDocumentAfter(afterDoc);
                
                // Start detection.
                if(detector.HasRefactoring())
                {
                    refactorings = refactorings.Union(detector.GetRefactorings());
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<ManualRefactoring> GetRefactorings()
        {
            return refactorings;
        }

        /// <summary>
        /// Extract method detector for same classes before and after.
        /// </summary>
        private class InClassExtractMethodDetector : IRefactoringDetector, 
            IBeforeAndAfterDocumentKeeper
        {
            private readonly ClassDeclarationSyntax classBefore;
            private readonly ClassDeclarationSyntax classAfter;
            private readonly InMethodExtractMethodDetector inMethodDetector;
            private IDocument docAfter;
            private IDocument docBefore;


            /* The detected refactorings. */
            private IEnumerable<ManualRefactoring> refactorings;

            private Logger logger;
           

            public InClassExtractMethodDetector(ClassDeclarationSyntax classBefore,
                ClassDeclarationSyntax classAfter, InMethodExtractMethodDetector 
                inMethodDetector)
            {
                this.classBefore = classBefore;
                this.classAfter = classAfter;
                this.inMethodDetector = inMethodDetector;
                logger = NLoggerUtil.GetNLogger(typeof(InClassExtractMethodDetector));
            }

            public Boolean HasRefactoring()
            {
                refactorings = Enumerable.Empty<ManualRefactoring>();

                // Get the methods that are newly added.
                var addedMethods = GetAddedMethod(classBefore, classAfter);
                logger.Info("Added method count: " + addedMethods.Count());

                // Get the common methods between the classes before and after.
                var commonMethods = GetCommonMethod(classBefore, classAfter);
                logger.Info("Common method count: " + commonMethods.Count());

                // Find the suspicious pairs of callers and callees; callers are common; callees are
                // added;  
                foreach (var pair in commonMethods)
                {
                    var methodBefore = (MethodDeclarationSyntax) pair.Key;
                    var methodAfter = (MethodDeclarationSyntax) pair.Value;
                    foreach (MethodDeclarationSyntax addedMethod in addedMethods)
                    {
                        logger.Info("Caller: " + methodAfter.Identifier.ValueText);
                        logger.Info("Callee: " + addedMethod.Identifier.ValueText);
                      
                        // Configure the in method extract method detector.
                        inMethodDetector.SetCallerBefore(methodBefore);
                        inMethodDetector.SetCallerAfter(methodAfter);
                        inMethodDetector.SetCalleeAfter(addedMethod);
                        inMethodDetector.SetDocumentBefore(docBefore);
                        inMethodDetector.SetDocumentAfter(docAfter);

                        // Start to detect.
                        if (inMethodDetector.HasRefactoring())
                        {
                            refactorings = refactorings.Union(inMethodDetector.GetRefactorings());
                            return true;
                        }
                    }
                }
                return false;
            }


            private IEnumerable<SyntaxNode> GetAddedMethod(ClassDeclarationSyntax before, 
                ClassDeclarationSyntax after)
            {
                var methodsBefore = ASTUtil.GetMethodsDeclarations(before);
                var methodsAfter = ASTUtil.GetMethodsDeclarations(after);
                return methodsAfter.Except(methodsBefore, new MethodNameEqualityComparer());
            }

            private IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetCommonMethod(
                ClassDeclarationSyntax before, ClassDeclarationSyntax after)
            {
                var methodsBefore = ASTUtil.GetMethodsDeclarations(before);
                var methodsAfter = ASTUtil.GetMethodsDeclarations(after);
                return RefactoringDetectionUtils.GetCommonNodePairs(methodsBefore, methodsAfter, 
                    new MethodNameComparer());
            }

            private class MethodNameEqualityComparer : IEqualityComparer<SyntaxNode>
            {
                public bool Equals(SyntaxNode x, SyntaxNode y)
                {
                    var name1 = ((MethodDeclarationSyntax) x).Identifier.ValueText;
                    var name2 = ((MethodDeclarationSyntax) y).Identifier.ValueText;
                    return name1.Equals(name2);
                }

                public int GetHashCode(SyntaxNode obj)
                {
                    return 1;
                }
            }

            public IEnumerable<ManualRefactoring> GetRefactorings()
            {
                return this.refactorings;
            }

            public void SetDocumentBefore(IDocument docBefore)
            {
                this.docBefore = docBefore;
            }

            public void SetDocumentAfter(IDocument docAfter)
            {
                this.docAfter = docAfter;
            }
        }

        public RefactoringType RefactoringType 
        { 
            get { return RefactoringType.EXTRACT_METHOD;}
        }
    }  
}
