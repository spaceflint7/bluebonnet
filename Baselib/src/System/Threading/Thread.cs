
using System;

namespace system.threading
{

    public sealed class Thread
    {

        [java.attr.RetainType] java.lang.Thread JavaThread;
        [java.attr.RetainType] int threadId;

        public ExecutionContext _executionContext;
        public SynchronizationContext _synchronizationContext;



        private Thread()
        {
            JavaThread = java.lang.Thread.currentThread();
            SetThreadId();
        }



        public Thread(System.Threading.ThreadStart start)
        {
            java.lang.Runnable.Delegate runnable = () => start();
            JavaThread = new java.lang.Thread(runnable.AsInterface());
            SetThreadId();
        }



        private void SetThreadId()
            => threadId = system.runtime.compilerservices.IdentifierWeakTable.GlobalGenerate(this);



        public void Start() => JavaThread.start();

        public void Join() => JavaThread.join();

        public bool Join(long millisecondsTimeout)
        {
            if (millisecondsTimeout <= 0)
            {
                if (millisecondsTimeout == System.Threading.Timeout.Infinite)
                    millisecondsTimeout = 0;
                else if (millisecondsTimeout == 0)
                    millisecondsTimeout = 1;
                else
                    throw new System.ArgumentOutOfRangeException();
            }
            else if (millisecondsTimeout >= System.Int32.MaxValue)
                throw new System.ArgumentOutOfRangeException();

            if (JavaThread.getState() == java.lang.Thread.State.NEW)
                throw new System.Threading.ThreadStateException();

            JavaThread.join((int) millisecondsTimeout);
            return JavaThread.isAlive();
        }

        public bool Join(System.TimeSpan timeout) => Join((long) timeout.TotalMilliseconds);



        public static Thread CurrentThread
        {
            get
            {
                var current = (Thread) ThreadReference.get();
                if (object.ReferenceEquals(current, null))
                    ThreadReference.set(current = new Thread());
                return current;
            }
        }

        public int ManagedThreadId => threadId;

        public static void Sleep(int millis) => java.lang.Thread.sleep(millis);

        public static void SpinWait(int count)
        {
            while (count-- > 0)
                java.lang.Thread.yield();
        }

        public static bool Yield()
        {
            var flag = YieldedFlag;
            YieldedFlag = ! flag;
            return flag;
        }

        public System.Threading.ThreadState ThreadState
        {
            get
            {
                var javaThread = JavaThread;
                var javaState = javaThread.getState();
                System.Threading.ThreadState resultState =
                    javaState == java.lang.Thread.State.RUNNABLE ? System.Threading.ThreadState.Running
                  : javaState == java.lang.Thread.State.NEW      ? System.Threading.ThreadState.Unstarted
                  : javaState == java.lang.Thread.State.TERMINATED ? System.Threading.ThreadState.Stopped
                  :                                            System.Threading.ThreadState.WaitSleepJoin;
                if (javaThread.isDaemon())
                    resultState |= System.Threading.ThreadState.Background;
                return resultState;
            }
        }

        public system.globalization.CultureInfo CurrentCulture
        {
            get => system.globalization.CultureInfo.InvariantCulture;
            set => throw new System.PlatformNotSupportedException();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static void MemoryBarrier()
        {
            // accessing volatile field within a synchronized method.
            // hopefully this is enough to trigger a full memory barrier.
            var a = BarrierHelper;
            BarrierHelper = null;
        }



        [java.attr.RetainType] static volatile object BarrierHelper;
        [java.attr.RetainType] static volatile bool YieldedFlag;
        [java.attr.RetainType] static java.lang.ThreadLocal ThreadReference =
                                                            new java.lang.ThreadLocal();
        /*[java.attr.RetainType] static readonly
                system.runtime.compilerservices.IdentifierWeakTable idTable =
                                            new system.runtime.compilerservices.IdentifierWeakTable();*/
    }



    // defined here because the real System.Threading.StackCrawlMark is inaccessible
    public enum StackCrawlMark
    {
        LookForMe = 0,
        LookForMyCaller = 1,
        LookForMyCallersCaller = 2,
        LookForThread = 3
    }

}
