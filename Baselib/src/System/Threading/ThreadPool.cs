
namespace system.threading
{

    public static class ThreadPool
    {

        public static bool QueueUserWorkItem(System.Threading.WaitCallback callBack)
            => QueueUserWorkItem(callBack, null);



        public static bool QueueUserWorkItem(System.Threading.WaitCallback callBack,
                                             object state)
        {
            if (callBack == null)
                throw new System.ArgumentNullException();
            java.lang.Runnable.Delegate runnable = () => callBack(state);
            JavaThreadPool.execute(runnable.AsInterface());
            return true;
        }



        public static bool QueueUserWorkItem<TState>(System.Action<TState> callBack,
                                                     TState state, bool preferLocal)
        {
            java.lang.Runnable.Delegate runnable = () => callBack(state);
            JavaThreadPool.execute(runnable.AsInterface());
            return true;
        }



        // helper method for Timer::run, because TimerCallback is not castable to WaitCallback
        public static void QueueTimerWorkItem(TimerCallback func, object data)
        {
            JavaThreadPool.execute(((java.lang.Runnable.Delegate) (() => func(data))).AsInterface());
        }



        public static void UnsafeQueueCustomWorkItem(IThreadPoolWorkItem workItem, bool forceGlobal)
        {
            java.lang.Runnable.Delegate runnable = () => workItem.ExecuteWorkItem();
            JavaThreadPool.execute(runnable.AsInterface());
        }



        public static bool TryPopCustomWorkItem(IThreadPoolWorkItem workItem) => false;



        [java.attr.RetainType] private static java.util.concurrent.ThreadPoolExecutor JavaThreadPool =
                                    new java.util.concurrent.ThreadPoolExecutor(
                                        0, 32767,   60, java.util.concurrent.TimeUnit.SECONDS,
                                        new java.util.concurrent.ArrayBlockingQueue(1, true),
                                        ((java.util.concurrent.ThreadFactory.Delegate) (r =>
                                        {   var thread = java.util.concurrent.Executors
                                                            .defaultThreadFactory().newThread(r);
                                            thread.setDaemon(true);
                                            return thread;
                                        })).AsInterface());

    }



    public interface IThreadPoolWorkItem
    {
        void ExecuteWorkItem();
        void MarkAborted(System.Threading.ThreadAbortException tae);
    }

}
