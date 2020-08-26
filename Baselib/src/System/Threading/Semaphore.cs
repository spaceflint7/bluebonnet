
namespace system.threading
{

    public class Semaphore : WaitHandle
    {
        [java.attr.RetainType] java.util.concurrent.atomic.AtomicInteger current;
        [java.attr.RetainType] int maximum;

        public Semaphore(int initialCount, int maximumCount)
        {
            if (initialCount < 0 || maximumCount < 1)
                throw new System.ArgumentOutOfRangeException();
            if (initialCount > maximumCount)
                throw new System.ArgumentException();

            current = new java.util.concurrent.atomic.AtomicInteger(initialCount);
            maximum = maximumCount;
        }

        public int Release()
        {
            int previousCount = current.getAndIncrement();
            if (previousCount >= maximum)
            {
                while (current.decrementAndGet() > maximum)
                    ;
                throw new System.Threading.SemaphoreFullException();
            }
            base.Signal();
            return previousCount;
        }

        public int Release(int releaseCount)
        {
            if (releaseCount < 1)
                throw new System.ArgumentOutOfRangeException();
            int previousCount = current.get();
            while (releaseCount-- > 0)
                Release();
            return previousCount;
        }

        protected override bool CanAcquire(java.lang.Thread thread) => (current.get() > 0);

        protected override void DoAcquire(java.lang.Thread thread) => current.decrementAndGet();

        protected override bool ShouldWakeAll() => true;
    }
}
