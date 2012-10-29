using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;

namespace warnings.analyzer
{
    /* Semantic model analyzer that wrap several functionalities. */
    public interface ISemanticModelAnalyzer
    {
        void SetSemanticMode(ISemanticModel model);
        
        /* Get all the accessible symbols at the given postion in the document. */
        IEnumerable<ISymbol> GetAccessibleSymbols(int position);
    }

    internal class SemanticModelAnalyzer : ISemanticModelAnalyzer
    {
        private ISemanticModel model;

        public void SetSemanticMode(ISemanticModel model)
        {
            this.model = model;
        }
        
        public IEnumerable<ISymbol> GetAccessibleSymbols(int position)
        {
            return model.LookupSymbols(position);
        }
    }
}
