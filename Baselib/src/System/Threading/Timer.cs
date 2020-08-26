
namespace system.threading
{

    public delegate void TimerCallback(object state);

    public sealed class Timer : java.util.TimerTask, System.IDisposable
    {

        [java.attr.RetainType] private TimerCallback callbackFunc;
        [java.attr.RetainType] private object callbackData;
        [java.attr.RetainType] private bool scheduled;
        [java.attr.RetainType] private bool disposed;



        public Timer(TimerCallback callback)
        {
            if (callback == null)
                throw new System.ArgumentNullException();
            callbackFunc = callback;
            callbackData = this;
        }



        public Timer(TimerCallback callback, object state, long dueTime, long period)
            => Setup(callback, state, dueTime, period, true);

        public Timer(TimerCallback callback, object state, int dueTime, int period)
            => Setup(callback, state, dueTime, period, true);

        public Timer(TimerCallback callback, object state, uint dueTime, uint period)
            => Setup(callback, state, dueTime, period, true);

        public Timer(TimerCallback callback, object state,
                     System.TimeSpan dueTime, System.TimeSpan period)
            => Setup(callback, state, (long) dueTime.TotalMilliseconds,
                     (long) period.TotalMilliseconds, true);



        public bool Change(long dueTime, long period) => Setup(null, null, dueTime, period, false);

        public bool Change(int dueTime, int period) => Setup(null, null, dueTime, period, false);



        private bool Setup(TimerCallback callback, object state, long dueTime, long period, bool init)
        {
            if (dueTime < -1)
                throw new System.ArgumentOutOfRangeException("dueTime");
            if (period < -1)
                throw new System.ArgumentOutOfRangeException("period");
            if (dueTime > MAX_SUPPORTED_TIMEOUT)
                throw new System.ArgumentOutOfRangeException("dueTime");
            if (period > MAX_SUPPORTED_TIMEOUT)
                throw new System.ArgumentOutOfRangeException("period");

            if (init)
            {
                if (callback == null)
                    throw new System.ArgumentNullException();

                callbackFunc = callback;
                callbackData = state;
            }
            else
            {
                if (disposed)
                    throw new System.ObjectDisposedException(null);

                if (scheduled)
                {
                    cancel();
                    scheduled = false;
                }
            }

            if (dueTime != System.Threading.Timeout.Infinite)
            {
                if (period != System.Threading.Timeout.Infinite)
                {
                    _JavaTimer.schedule(this, dueTime, period);
                }
                else
                {
                    _JavaTimer.schedule(this, dueTime);
                }

                scheduled = true;
            }

            return true;
        }



        public void Dispose()
        {
            if (scheduled)
            {
                cancel();
                scheduled = false;
            }
            disposed = true;
        }



        public void KeepRootedWhileScheduled()
        {
            // called by method Delay(int, CancellationToken)
            // in class System.Threading.Tasks.Task
        }



        [java.attr.RetainName] public override void run()
            => ThreadPool.QueueTimerWorkItem(callbackFunc, callbackData);



        private const uint MAX_SUPPORTED_TIMEOUT = (uint) 0xFFFFFFFE;

        [java.attr.RetainType] private static readonly java.util.Timer _JavaTimer =
                                        new java.util.Timer("system.threading.Timer", true);

    }

}
