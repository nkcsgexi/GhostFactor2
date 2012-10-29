using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace warnings.change
{

    class ASTChange : IChange
    {
        public string performChange(string s)
        {
            return null;
        }

        public void setChange(SyntaxTree before, SyntaxTree after)
        {
           /* SyntaxNode rootB = before.GetRoot();
            SyntaxNode rootA = after.GetRoot();
            */
        }
    }


}
