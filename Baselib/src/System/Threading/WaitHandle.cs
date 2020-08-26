
namespace system.threading
{

    public abstract class WaitHandle : System.IDisposable
    {

        // obsolete since .Net 2.0
        public virtual System.IntPtr Handle => throw new System.NotSupportedException();
        public Microsoft.Win32.SafeHandles.SafeWaitHandle SafeWaitHandle
            => throw new System.NotSupportedException();



        [java.attr.RetainType] readonly java.util.concurrent.ConcurrentLinkedQueue
                                    waitQueue = new java.util.concurrent.ConcurrentLinkedQueue();



        protected WaitHandle()
        {
            //m_id = system.runtime.compilerservices.IdentifierWeakTable.Generate(this);
        }



        //
        // Wait
        //


        public virtual bool WaitOne()
            => WaitOne(-1);

        public virtual bool WaitOne(System.TimeSpan timeout, bool exitContext)
            => WaitOne(timeout);
        public virtual bool WaitOne(System.TimeSpan timeout)
            => Wait(new WaitHandle[1] { this }, true, timeout);

        public virtual bool WaitOne(int millisecondsTimeout, bool exitContext)
            => WaitOne(millisecondsTimeout);
        public virtual bool WaitOne(int millisecondsTimeout)
            => Wait(new WaitHandle[1] { this }, true, millisecondsTimeout);



        public static bool WaitAll(WaitHandle[] waitObjs)
            => WaitAll(waitObjs, -1);

        public static bool WaitAll(WaitHandle[] waitObjs, System.TimeSpan timeout, bool exitContext)
            => WaitAll(waitObjs, timeout);
        public static bool WaitAll(WaitHandle[] waitObjs, System.TimeSpan timeout)
            => Wait(waitObjs, true, timeout);

        public static bool WaitAll(WaitHandle[] waitObjs, int millisecondsTimeout, bool exitContext)
            => WaitAll(waitObjs, millisecondsTimeout);
        public static bool WaitAll(WaitHandle[] waitObjs, int millisecondsTimeout)
            => Wait(waitObjs, true, millisecondsTimeout);


        static bool Wait(WaitHandle[] waitObjs, bool waitAll, System.TimeSpan timeout)
        {
            long millisecondsTimeout = (long) timeout.TotalMilliseconds;
            if (millisecondsTimeout > System.Int32.MaxValue)
                throw new System.ArgumentOutOfRangeException("timeout");
            return Wait(waitObjs, waitAll, (int) millisecondsTimeout);
        }



        static bool Wait(WaitHandle[] waitObjs, bool waitAll, int millisecondsTimeout)
        {
            if (waitObjs == null)
                throw new System.ArgumentNullException();
            int waitCount = waitObjs.Length;
            if (waitCount == 0)
                throw new System.ArgumentException();
            if (waitCount > 64)
                throw new System.NotSupportedException();
            if (millisecondsTimeout < -1)
                throw new System.ArgumentOutOfRangeException();

            var thread = java.lang.Thread.currentThread();
            if (DoAcquire(waitObjs, waitCount, waitAll, thread))
                return true;
            else if (millisecondsTimeout == 0)
                return false;

            var parkInfo = new ParkInfo(waitObjs, waitAll);
            for (int i = 0; i < waitCount; i++)
                waitObjs[i].waitQueue.add(thread);

            try
            {
                long elapsedTime = 0;
                long lastTime = java.lang.System.nanoTime();
                for (;;)
                {
                    if (millisecondsTimeout == -1)
                        java.util.concurrent.locks.LockSupport.park(parkInfo);
                    else
                    {
                        java.util.concurrent.locks.LockSupport.parkNanos(
                                parkInfo, (millisecondsTimeout - elapsedTime) * 1000000);
                    }

                    if (java.lang.Thread.interrupted())
                        throw new java.lang.InterruptedException();

                    if (DoAcquire(waitObjs, waitCount, waitAll, thread))
                        return true;

                    if (millisecondsTimeout != -1)
                    {
                        long thisTime = java.lang.System.nanoTime();
                        elapsedTime += thisTime - lastTime;
                        if (elapsedTime >= millisecondsTimeout)
                            return false;
                        thisTime = lastTime;
                    }
                }
            }
            finally
            {
                for (int i = 0; i < waitCount; i++)
                {
                    while (waitObjs[i].waitQueue.remove(thread))
                        ;
                }
            }
        }



        protected void Signal()
        {
            if (! waitQueue.isEmpty())
            {
                bool wakeAll = ShouldWakeAll();

                var waitThreads = waitQueue.toArray();
                for (int i = 0; i < waitThreads.Length; i++)
                {
                    var thread = (java.lang.Thread) waitThreads[i];
                    var parkInfo =
                            java.util.concurrent.locks.LockSupport.getBlocker(thread) as ParkInfo;
                    if (parkInfo != null)
                    {
                        if (CanAcquire(parkInfo.waitObjs, parkInfo.waitObjs.Length,
                                       parkInfo.waitAll, thread))
                        {
                            java.util.concurrent.locks.LockSupport.unpark(thread);
                            if (! wakeAll)
                                break;
                        }
                    }
                }
            }
        }



        static bool CanAcquire(WaitHandle[] waitObjs, int waitCount, bool waitAll,
                               java.lang.Thread thread)
        {
            for (int i = 0; i < waitCount; i++)
            {
                if (waitObjs[i].CanAcquire(thread))
                {
                    if (! waitAll)
                        return true;
                }
                else
                {
                    if (waitAll)
                        return false;
                }
            }
            return true;
        }



        static bool DoAcquire(WaitHandle[] waitObjs, int waitCount, bool waitAll,
                              java.lang.Thread thread)
        {
            if (! CanAcquire(waitObjs, waitCount, waitAll, thread))
                return false;
            javaLock.@lock();
            try
            {
                if (! CanAcquire(waitObjs, waitCount, waitAll, thread))
                    return false;
                for (int i = 0; i < waitCount; i++)
                    waitObjs[i].DoAcquire(thread);
                return true;
            }
            finally
            {
                javaLock.@unlock();
            }
        }



        protected abstract bool CanAcquire(java.lang.Thread thread);
        protected abstract void DoAcquire(java.lang.Thread thread);
        protected virtual bool ShouldWakeAll() => false;



        public virtual void Close()
        {
        }

        protected virtual void Dispose(bool explicitDisposing)
        {
        }

        public void Dispose()
        {
        }



        //protected static readonly System.IntPtr InvalidHandle = new System.IntPtr(-1);

        [java.attr.RetainType] static readonly java.util.concurrent.locks.ReentrantLock javaLock =
                        new java.util.concurrent.locks.ReentrantLock();

        [java.attr.RetainType] static readonly java.util.HashMap map = new java.util.HashMap();



        class ParkInfo
        {
            public WaitHandle[] waitObjs;
            public bool waitAll;
            public ParkInfo(WaitHandle[] _waitObjs, bool _waitAll)
            {
                waitObjs = _waitObjs;
                waitAll = _waitAll;
            }
        }
    }

}
