using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace warnings.components
{
    /* Set a timer for certain componet; invoke the component when time is up. */
    public class ComponentTimer
    {
        /* The time interval after which the timeup event shall be triggered. */
        public int timeInterval { get; private set; }

        /* The time up event. */
        private event doSomething TimesUp;

        /* The delegate for time up event handler. */
        public delegate void doSomething(object sender, EventArgs e);

        /* The thread for keeping time. */
        private Thread thread;

        public ComponentTimer(int timeInterval, doSomething handler)
        {
            this.timeInterval = timeInterval;
            this.thread = new Thread(run);
            this.thread.Priority = ThreadPriority.BelowNormal;
            TimesUp += handler;
        }

        public void start()
        {
            // Start the thread if not started.
            if(thread.ThreadState == ThreadState.Unstarted)
                thread.Start();
        }

        public void end()
        {
            // End the thread;
            thread.Abort();
        }

        /* The actual code for keeping time and trigger event. */
        private void run()
        {
            while(true)
            {
                Thread.Sleep(timeInterval);
                TimesUp(this, null);
            }
        }

    }
}
