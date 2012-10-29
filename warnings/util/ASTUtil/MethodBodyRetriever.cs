using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace warnings.util
{
    public interface IMethodRetriever
    {
        void setSource(String source);
        int getMethodBodiesCount();
        String[] getMethodNames();
        String[] getMethodBodies();
        Boolean hasMethodBodies();
    }

    /**
     * @Author Xi Ge
     * feeding this class with the source code, it can retrieve the method bodies 
     * and method names, seperately.
     */
    public class MethodBodyRetriever : IMethodRetriever
    {
        private String source = null;
        private SyntaxTree tree = null;
        private List<MethodDeclarationSyntax> methods;
        private List<string> blocks;
        private List<string> methodNames; 

        public void setSource(string source)
        {
            this.source = source;
            this.tree = ASTUtil.GetSyntaxTreeFromSource(source);
            methods = ASTUtil.GetAllMethodDeclarations(tree);
            blocks = new List<string>();
            methodNames = new List<string>();
            foreach (MethodDeclarationSyntax method in methods)
            {
                SyntaxNode block = ASTUtil.GetBlockOfMethod(method);
                if(block != null)
                {
                    StringBuilder sb = new StringBuilder();
                    IEnumerable<SyntaxNode> stats = ASTUtil.GetStatementsInNode(block);
                    foreach(StatementSyntax st in stats)
                    {
                        sb.AppendLine(st.GetText());
                    }
                    blocks.Add(sb.ToString());
                }
                else
                {
                    blocks.Add("");
                }
                methodNames.Add(method.Identifier.GetText());
            }
        }

        public int getMethodBodiesCount()
        {
            return methods.Count;
        }


        public string[] getMethodBodies()
        {
            return blocks.ToArray();
        }

        public string[] getMethodNames()
        {
            return methodNames.ToArray();
        }

        public bool hasMethodBodies()
        {
            return getMethodBodiesCount() > 0;
        }


    }
}
