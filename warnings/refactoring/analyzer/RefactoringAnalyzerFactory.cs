using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace warnings.refactoring
{
    public class RefactoringAnalyzerFactory
    {
        public static IManualExtractMethodAnalyzer CreateManualExtractMethodAnalyzer()
        {
            return new ManualExtractMethodAnalyzer();
        }
    }
}
