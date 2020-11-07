
using System;

namespace system.threading
{

    public static class Monitor
    {

        public static void Enter(object obj) => GetLock(obj).lockInterruptibly();
        public static void Enter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();
            Enter(obj);
            lockTaken = true;
        }



        public static bool TryEnter(object obj) => GetLock(obj).tryLock();
        public static void TryEnter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();
            lockTaken = TryEnter(obj);
        }



        public static bool TryEnter(object obj, int millis)
        {
            if (millis < 0)
            {
                if (millis == -1)
                {
                    Enter(obj);
                    return true;
                }
                throw new ArgumentOutOfRangeException();
            }
            return GetLock(obj).tryLock(millis, java.util.concurrent.TimeUnit.MILLISECONDS);
        }
        public static void TryEnter(object obj, int millis, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();
            lockTaken = TryEnter(obj, millis);
        }



        public static bool TryEnter(object obj, TimeSpan timeout)
            => TryEnter(obj, MillisecondsTimeoutFromTimeSpan(timeout));

        public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();
            lockTaken = TryEnter(obj, timeout);
        }



        public static void Exit(object obj) => GetLock(obj).unlock();



        public static bool IsEntered(object obj) => GetLock(obj).isHeldByCurrentThread();



        public static void Pulse(object obj) => GetCond(obj).signal();
        public static void PulseAll(object obj) => GetCond(obj).signalAll();



        public static bool Wait(object obj)
        {
            GetCond(obj).@await();
            return true;
        }

        public static bool Wait(object obj, int millis)
        {
            if (millis < 0)
            {
                if (millis == -1)
                    return Wait(obj);

                throw new ArgumentOutOfRangeException();
            }
            return GetCond(obj).@await(millis, java.util.concurrent.TimeUnit.MILLISECONDS);
        }

        public static bool Wait(object obj, TimeSpan timeout)
            => Wait(obj, MillisecondsTimeoutFromTimeSpan(timeout));



        static int MillisecondsTimeoutFromTimeSpan(TimeSpan timeout)
        {
            var millis = (long) timeout.TotalMilliseconds;
            if (millis < -1 || millis > (long) System.Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException();
            }
            return (int) millis;
        }



        static java.util.concurrent.locks.ReentrantLock GetLock(object obj)
        {
            if (object.ReferenceEquals(obj, null))
                throw new ArgumentNullException();

            if (obj is system.Array.ProxySyncRoot objArray)
                obj = objArray.SyncRoot;
            var @lock = (java.util.concurrent.locks.ReentrantLock) LockCache.GetOrAdd(obj, null);
            if (object.ReferenceEquals(@lock, null))
            {
                @lock = (java.util.concurrent.locks.ReentrantLock)
                            LockCache.GetOrAdd(obj, new java.util.concurrent.locks.ReentrantLock());
            }

            return @lock;
        }



        static java.util.concurrent.locks.Condition GetCond(object obj)
        {
            if (object.ReferenceEquals(obj, null))
                throw new ArgumentNullException();

            if (obj is system.Array.ProxySyncRoot objArray)
                obj = objArray.SyncRoot;

            var cond = (java.util.concurrent.locks.Condition) CondCache.GetOrAdd(obj, null);
            if (object.ReferenceEquals(cond, null))
            {
                cond = (java.util.concurrent.locks.Condition)
                            CondCache.GetOrAdd(obj, GetLock(obj).newCondition());
            }

            return cond;
        }



        private static void ThrowLockTakenException() => throw new ArgumentException("LockTaken");



        static Monitor()
        {
            system.Util.DefineException(
                (java.lang.Class) typeof(java.lang.InterruptedException),
                (exc) => new System.Threading.ThreadInterruptedException(exc.getMessage())
            );

            system.Util.DefineException(
                (java.lang.Class) typeof(java.lang.IllegalMonitorStateException),
                (exc) => new System.Threading.SynchronizationLockException(exc.getMessage())
            );
        }



        [java.attr.RetainType] private static readonly system.runtime.compilerservices.ConditionalWeakTable LockCache
            = new system.runtime.compilerservices.ConditionalWeakTable();

        [java.attr.RetainType] private static readonly system.runtime.compilerservices.ConditionalWeakTable CondCache
            = new system.runtime.compilerservices.ConditionalWeakTable();

    }

}
