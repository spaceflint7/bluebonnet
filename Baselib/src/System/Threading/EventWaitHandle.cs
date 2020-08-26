
namespace system.threading
{

    public class EventWaitHandle : WaitHandle
    {
        [java.attr.RetainType] volatile int signalled;
        [java.attr.RetainType] bool autoReset;

        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode)
        {
            if (mode ==  System.Threading.EventResetMode.AutoReset)
                autoReset = true;
            if (initialState)
                signalled = 1;
        }

        public bool Reset()
        {
            signalled = 0;
            return true;
        }

        public bool Set()
        {
            signalled = 1;
            base.Signal();
            return true;
        }

        protected override bool CanAcquire(java.lang.Thread thread)
            => signalled != 0;

        protected override void DoAcquire(java.lang.Thread thread)
        {
            if (autoReset)
                signalled = 0;
        }

        protected override bool ShouldWakeAll() => (! autoReset);
    }

}
