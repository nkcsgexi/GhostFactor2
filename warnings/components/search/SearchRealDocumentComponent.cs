using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BlackHen.Threading;
using Roslyn.Services;
using warnings.analyzer;

namespace warnings.components
{
    /* All the queries of real document shall implement this interface.*/
    public interface IDocumentSearchCondition
    {
        bool IsDocumentMetWithCondition(IDocument document);
    }

    /* Interface for searchers of document, given a search condition, returns the searched document. */
    public interface IDocumentSearcher
    {
        IDocument SearchForDocument(IDocumentSearchCondition condition);
        ISolution GetSolution();
    }

    internal class SearchRealDocumentComponent : IDocumentSearcher, IFactorComponent
    {
        /* Singleton this component. */
        private static IDocumentSearcher instance;

        public static IDocumentSearcher GetInstance(ISolution solution)
        {
            if(instance == null)
                instance =  new SearchRealDocumentComponent(solution);
            return instance;
        }

        /* The real solution where the document should be searched. */
        private readonly ISolution solution;
        private readonly WorkQueue queue;

        private SearchRealDocumentComponent(ISolution solution)
        {
            this.solution = solution;

            // Queue with single thread. 
            this.queue = new WorkQueue {ConcurrentLimit = 1};
        }

        public void Enqueue(IWorkItem item)
        {
            queue.Add(item);
        }

        public string GetName()
        {
            return "Search real document component.";
        }

        public int GetWorkQueueLength()
        {
            return queue.Count;
        }

        public void Start()
        {
        }

        public IDocument SearchForDocument(IDocumentSearchCondition condition)
        {
            // Get an item for searching document, by the given condition. 
            var item = new SearchDocumentWorkItem(solution, condition);
            queue.Add(item);
            
            // Waiting to the finishing of the workitem.
            while (item.State != WorkItemState.Completed && item.State != WorkItemState.Failing) ;
            
            // Return the result.
            return item.GetSearchedDocument();
        }

        public ISolution GetSolution()
        {
            return solution;
        }


        /* The workitem to be pushed to the search document work queue. */
        private class SearchDocumentWorkItem : WorkItem
        {
            // A shared instance of solution analyzer. 
            private static readonly ISolutionAnalyzer analyzer =
                AnalyzerFactory.GetSolutionAnalyzer();

            // Where the searching should be performed.
            private readonly ISolution solution;

            // The condition for the searched document to be met.
            private readonly IDocumentSearchCondition condition;

            // The result of the search.
            private IDocument document;

            internal SearchDocumentWorkItem(ISolution solution, IDocumentSearchCondition condition)
            {
                this.solution = solution;
                this.condition = condition;
            }

            public override void Perform()
            {
                // Set the solution of analyzer.
                analyzer.SetSolution(solution);

                // Get all the documents first, next get all of these that met with the given condition.
                var docs = analyzer.GetAllDocuments().Where(d => condition.IsDocumentMetWithCondition(d));

                // If there are documents found, remember the first od default one.
                if(docs.Any())
                {
                    document = docs.FirstOrDefault();
                }
            }

            /* Get the searched document. */
            public IDocument GetSearchedDocument()
            {
                return document;
            }
        }
    }
}
