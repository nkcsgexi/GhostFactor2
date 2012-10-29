using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace warnings.retriever
{
    public class RetrieverFactory
    {
        public static IRenamableRetriever GetRenamableRetriever()
        {
            return new RenamablesRetriever();
        }

        public static ITypablesRetriever GetTypablesRetriever()
        {
            return new TypableRetriever();
        }

        public static IMethodInvocationsRetriever GetMethodInvocationRetriever()
        {
            return new MethodInvocationsRetriever();
        }
    }
}
