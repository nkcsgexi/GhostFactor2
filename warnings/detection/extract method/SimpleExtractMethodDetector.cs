using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using warnings.analyzer;
using warnings.refactoring;
using warnings.refactoring.detection;
using warnings.util;

namespace warnings.detection
{
    /// <summary>
    ///  This is extract method detector whose purpose is to maximize efficiency. Algorithm adopted is 
    /// simple: when a new method is added the the class, and an exsisting method is calling this new method. 
    /// This new added method is likely an extraction from the original method.
    /// </summary>
    internal class SimpleExtractMethodDetector : IExternalRefactoringDetector
    {
        private readonly List<ManualRefactoring> refactorings;

        private string sourceBefore;
        private string sourceAfter;
        private IDocument documentAfter;
        private IDocument documentBefore;

        public SimpleExtractMethodDetector()
        {
            refactorings = new List<ManualRefactoring>();
        }

        public bool HasRefactoring()
        {
            // Clear the memory before.
            refactorings.Clear();

            documentBefore = RefactoringDetectionUtils.Convert2IDocument(sourceBefore);
            documentAfter = RefactoringDetectionUtils.Convert2IDocument(sourceAfter);

            // Get the syntax tree in before and after source.
            var treeBefore = (SyntaxTree)documentBefore.GetSyntaxTree();
            var treeAfter = (SyntaxTree)documentAfter.GetSyntaxTree();

            // Get their roots.
            var rootBefore = treeBefore.GetRoot();
            var rootAfter = treeAfter.GetRoot();

            // Get the clasess in the before and after tree.
            var beforeClasses = ASTUtil.GetClassDeclarations(rootBefore);
            var afterClasses = ASTUtil.GetClassDeclarations(rootAfter);

            var inClassDetector = new InClassExtractMethodDetector(documentBefore, 
                documentAfter);

            foreach (ClassDeclarationSyntax beforeClass in beforeClasses)
            {
                foreach (ClassDeclarationSyntax afterClass in afterClasses)
                {
                    // If the before class and after class have the identical name.
                    if (beforeClass.Identifier.ValueText.Equals(afterClass.Identifier.ValueText))
                    {
                        // Configure the in class detector.
                        inClassDetector.SetSyntaxNodeBefore(beforeClass);
                        inClassDetector.SetSyntaxNodeAfter(afterClass);

                        // If the detector finds some refactoring, add these refactorings to the list.
                        if (inClassDetector.HasRefactoring())
                        {
                            refactorings.AddRange(inClassDetector.GetRefactorings());
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
            return documentBefore;
        }

        public IDocument GetAfterDocument()
        {
            return documentAfter;
        }

        private class InClassExtractMethodDetector : IRefactoringDetector, IBeforeAndAfterSyntaxNodeKeeper
        {
            private readonly Logger logger;
            private readonly List<ManualRefactoring> refactorings;

            private readonly IDocument docAfter;
            private readonly IDocument docBefore;

            private SyntaxNode classAfter;
            private SyntaxNode classBefore;


            internal InClassExtractMethodDetector(IDocument docBefore, IDocument docAfter)
            {
                this.logger = NLoggerUtil.GetNLogger(typeof (InClassExtractMethodDetector));
                this.docBefore = docBefore;
                this.docAfter = docAfter;
                this.refactorings = new List<ManualRefactoring>();
            }

            public bool HasRefactoring()
            {
                refactorings.Clear();

                // Get the methods in both before and after classes.
                var methodsBefore = GetInClassMethods(classBefore);
                var methodsAfter = GetInClassMethods(classAfter);

                // Get the methods added and in common, both represented by methods after. 
                var addedMethods = GetMethodsAdded(methodsBefore, methodsAfter);
                var commonMethodPairs = GetCommonMethodPairs(methodsBefore, methodsAfter);

                foreach (var pair in commonMethodPairs)
                {
                    foreach (var addedMethod in addedMethods)
                    {
                        // We only consider non-public method to be extracted method.
                        if (!IsMethodPublic(addedMethod))
                        {
                            // Get the invocations of the added method in the body of the common method.
                            var invocations = ASTUtil.GetAllInvocationsInMethod(pair[1], addedMethod, 
                                (SyntaxTree) docAfter.GetSyntaxTree());

                            // If invocations are not empty
                            if (invocations.Any())
                            {
                                // Create a refactoring instance and added it to the refactoring list.
                                var refactoring = ManualRefactoringFactory.CreateSimpleExtractMethodRefactoring
                                    (docBefore, docAfter, pair[0], pair[1], addedMethod);
                                refactorings.Add(refactoring);
                            }
                        }
                    }
                }
                return refactorings.Any();
            }

            public IEnumerable<ManualRefactoring> GetRefactorings()
            {
                return refactorings.AsEnumerable();
            }

            public void SetSyntaxNodeBefore(SyntaxNode before)
            {
                this.classBefore = before;
            }

            public void SetSyntaxNodeAfter(SyntaxNode after)
            {
                this.classAfter = after;
            }

            private bool IsMethodPublic(SyntaxNode method)
            {
                var methodDec = (MethodDeclarationSyntax) method;
                return methodDec.Modifiers.Any(m => m.Kind == SyntaxKind.PublicKeyword);
            }

            /* Get new methods added in the after methods list to the before methods list.*/
            private IEnumerable<SyntaxNode> GetMethodsAdded(IEnumerable<SyntaxNode> methodsBefore, 
                IEnumerable<SyntaxNode> methodsAfter)
            {
                var addedMethods = new List<SyntaxNode>();
                foreach (SyntaxNode after in methodsAfter)
                {
                    if(!methodsBefore.Any(before => AreTwoMethodsNamesSame(before, after)))
                    {
                        addedMethods.Add(after);
                    }
                }
                return addedMethods;
            }

            /// <summary>
            /// Get common methods in the method lists before and after, result in a list of SyntaxNode[]. 
            /// Each SyntaxNode[] has two element, element at 0 is in the methodsBefore and element at 1 is 
            /// in the methodsAfter.
            /// </summary>
            /// <param name="methodsBefore"></param>
            /// <param name="methodsAfter"></param>
            /// <returns></returns>
            private IEnumerable<SyntaxNode[]> GetCommonMethodPairs(IEnumerable<SyntaxNode> methodsBefore, 
                IEnumerable<SyntaxNode> methodsAfter)
            {
                return methodsBefore.Join(methodsAfter, GetMethodName, GetMethodName, (beforeMethod, 
                    afterMethod) => new []{beforeMethod, afterMethod});
            }

            private string GetMethodName(SyntaxNode method)
            {
                return ((MethodDeclarationSyntax)method).Identifier.ValueText;
            }


            private bool AreTwoMethodsNamesSame(SyntaxNode before, SyntaxNode after)
            {
                return GetMethodName(before).Equals(GetMethodName(after));
            }


            private IEnumerable<SyntaxNode> GetInClassMethods(SyntaxNode root)
            {
                // Get the decendent whose type is method declaration, to do this, we do not need to 
                // parse into the method.
                return root.DescendantNodes(n => n.Kind != SyntaxKind.MethodDeclaration).Where(
                    n => n.Kind == SyntaxKind.MethodDeclaration);
            }
        }


        public RefactoringType RefactoringType 
        { 
            get { return RefactoringType.EXTRACT_METHOD;}
        }
    }
}
