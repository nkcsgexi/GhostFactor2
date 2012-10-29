using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BlackHen.Threading
{
    public delegate void OnWorkFinished(WorkItem sender);

    /* A decorator that issues a finish event when the perform method is finished. */
    public class FinishAwareWorkItem : WorkItem
    {
        private WorkItem item;

        public event OnWorkFinished OnWorkFinished;

        public FinishAwareWorkItem(WorkItem item)
        {
            this.item = item;
        }

        public override void Perform()
        {
            item.Perform();
            OnWorkFinished(this);
        }
    }

    /* An executor that runs a work sender in synchronized manner. */
    public class WorkItemSynchronizedExecutor
    {
        private readonly FinishAwareWorkItem finishAwareWorkItem;
        private readonly WorkQueue queue;

        public WorkItemSynchronizedExecutor(WorkItem item, WorkQueue queue)
        {
            this.finishAwareWorkItem = new FinishAwareWorkItem(item);
            this.queue = queue;
        }
       
        public void Execute()
        {
            var reset = new AutoResetEvent(false);
            finishAwareWorkItem.OnWorkFinished += delegate{reset.Set();};
            queue.Add(finishAwareWorkItem);
            reset.WaitOne();
        }
    }
}
