
namespace system.threading
{

    public class Mutex : WaitHandle
    {
        [java.attr.RetainType] volatile java.lang.Thread owner;
        [java.attr.RetainType] volatile int count;

        public Mutex()
        {
        }

        public Mutex(bool initialState)
        {
            if (initialState)
            {
                owner = java.lang.Thread.currentThread();
                count = 1;
            }
        }

        public void ReleaseMutex()
        {
            if (owner != java.lang.Thread.currentThread())
                throw new System.ApplicationException("Mutex not held");
            if (--count == 0)
            {
                owner = null;
                base.Signal();
            }
        }

        protected override bool CanAcquire(java.lang.Thread thread)
        {
            var owner = this.owner;
            return (owner == null || owner == thread);
        }

        protected override void DoAcquire(java.lang.Thread thread)
        {
            owner = thread;
            ++count;
        }
    }

}
