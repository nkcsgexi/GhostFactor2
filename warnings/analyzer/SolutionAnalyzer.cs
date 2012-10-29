using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Services;

namespace warnings.analyzer
{
    /* An analyzer for basic solution struture. */
    public interface ISolutionAnalyzer
    {
        void SetSolution(ISolution solution);
        IEnumerable<IProject> GetProjects();
        IEnumerable<IDocument> GetDocuments(IProject project);
        IEnumerable<IDocument> GetAllDocuments(); 
        String DumpSolutionStructure();
    }


    internal class SolutionAnalyzer : ISolutionAnalyzer
    {
        private static int ANALYZER_COUNT = 0;

        public static int GetCount()
        {
            return ANALYZER_COUNT;
        }

        private ISolution solution;

        internal SolutionAnalyzer()
        {
            Interlocked.Increment(ref ANALYZER_COUNT);
        }

        ~SolutionAnalyzer()
        {
            Interlocked.Decrement(ref ANALYZER_COUNT);
        }

        public void SetSolution(ISolution solution)
        {
            this.solution = solution;
        }

        public IEnumerable<IProject> GetProjects()
        {
            return solution.Projects;
        }

        public IEnumerable<IDocument> GetDocuments(IProject project)
        {
            return project.Documents;
        }

        public IEnumerable<IDocument> GetAllDocuments()
        {
            var list = new List<IDocument>();
            foreach (var project in GetProjects())
            {
                list.AddRange(GetDocuments(project));
            }
            return list.AsEnumerable();
        }

        public string DumpSolutionStructure()
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("solution");

            foreach (IProject project in GetProjects())
            {
                sb.AppendLine("\t" + project.Name);
                foreach(IDocument document in project.Documents)
                {
                    sb.AppendLine("\t\t" + document.Name);
                }
            }

            return sb.ToString();
        }
    }
}
