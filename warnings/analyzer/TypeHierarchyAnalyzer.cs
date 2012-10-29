using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;

namespace warnings.analyzer
{
    /* Analyzer to get the entire RefactoringType hierarchy of a given RefactoringType. */
    public interface ITypeHierarchyAnalyzer
    {
        void SetSemanticModel(ISemanticModel model);
        void SetDeclaration(SyntaxNode declaration);
        INamedTypeSymbol GetDeclarationType();
        IEnumerable<INamedTypeSymbol> GetBaseTypes();
        IEnumerable<INamedTypeSymbol> GetImplementedInterfaces();
        IEnumerable<INamedTypeSymbol> GetContainingTypes();
        IEnumerable<INamedTypeSymbol> GetContainedTypes();
    }

    internal class TypeHierarchyAnalyzer : ITypeHierarchyAnalyzer
    {
        private ISemanticModel model;
        private SyntaxNode declaration;

        /* Set the semantic model. */
        public void SetSemanticModel(ISemanticModel model)
        {
            this.model = model;
        }

        /* Set the RefactoringType declaration. */
        public void SetDeclaration(SyntaxNode declaration)
        {
            this.declaration = declaration;
        }

        /* Get all the base RefactoringType hierarchy. */
        public IEnumerable<INamedTypeSymbol> GetBaseTypes()
        {
            var type = GetDeclarationType();
            var baseTypes = new List<INamedTypeSymbol>();

            // Iteratively get all the base types hierarchy.
            for (var currentType = type; currentType.BaseType != null; currentType = currentType.BaseType)
            {
                baseTypes.Add(currentType.BaseType);
            }
            return baseTypes.AsEnumerable();
        }

        /* Get all the interfaces implemented by this RefactoringType. */
        public IEnumerable<INamedTypeSymbol> GetImplementedInterfaces()
        {
            var interfaces = new List<INamedTypeSymbol>();

            // Iteratively get all the implemented interface for this RefactoringType and all
            // of its super types.
            for (var currentType = GetDeclarationType(); currentType != null; 
                currentType = currentType.BaseType)
            {
                interfaces.AddRange(currentType.Interfaces.AsEnumerable());
            }

            // For all the handled interfaces. 
            var handledInterfaces = new List<INamedTypeSymbol>();

            // If still some unhandled interfaces.
            for (; interfaces.Any(); )
            {
                // Get the first of unhandled interface.
                var first = interfaces.First();  

                // Add its implemented interfaces to the unhandled list.
                interfaces.AddRange(first.Interfaces.AsEnumerable());

                // Add itself to the handled list.
                handledInterfaces.Add(first);

                // Remove itself from the unhandled list.
                interfaces.RemoveAt(0);
            }

            return handledInterfaces.AsEnumerable();
        }

        /* Get all the types that are containing this RefactoringType declaration. */
        public IEnumerable<INamedTypeSymbol> GetContainingTypes()
        {
            var type = GetDeclarationType();
            var containingTypeList = new List<INamedTypeSymbol>();

            // Iteratively get all the containing types.
            for (var currentType = type; currentType.ContainingType != null; currentType = currentType.ContainingType)
            {
                containingTypeList.Add(currentType.ContainingType);
            }
            return containingTypeList.AsEnumerable();
        }

        /* Get all the RefactoringType declarations that are in this RefactoringType declaration. */
        public IEnumerable<INamedTypeSymbol> GetContainedTypes()
        {
            var type = GetDeclarationType();
            return type.GetTypeMembers().AsEnumerable();
        }

        /* Get the RefactoringType symbol for the declaration node. */
        public INamedTypeSymbol GetDeclarationType()
        {
            return (INamedTypeSymbol) model.GetDeclaredSymbol(declaration);
        }

    }
}
