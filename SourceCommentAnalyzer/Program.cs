using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using warnings.retriever;
using warnings.util;

namespace SourceCommentAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = NLoggerUtil.GetNLogger(typeof (Program));
            var commentsRetriever = RetrieverFactory.GetCommentsRetriever();
            var retriver = new CSharpSourceRetriver("");
            for (retriver.Reset(); ;retriver.MoveNext() )
            {
                string code = (string) retriver.Current;
                var root = ASTUtil.GetSyntaxTreeFromSource(code).GetRoot();
                commentsRetriever.SetSyntaxNode(root);
                var comments = commentsRetriever.GetNonDocumentComments();
 
            }
        }

      





        private class CSharpSourceRetriver : IEnumerator
        {
            private string folder;
            private IEnumerator sourcePathEnumerator;

            internal CSharpSourceRetriver(string folder)
            {
                this.folder = folder;
                this.sourcePathEnumerator = FileUtil.GetFilesFromDirectory(folder, "*.cs").GetEnumerator();
            }

            public bool MoveNext()
            {
                return sourcePathEnumerator.MoveNext();
            }

            public void Reset()
            {
                sourcePathEnumerator.Reset();
            }

            public object Current
            {
                get
                {
                    string code = FileUtil.ReadAllText((string) sourcePathEnumerator.Current);
                    return code;
                }
                private set { }
            }
        }
    }
}
