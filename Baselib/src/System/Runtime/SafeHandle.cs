
namespace system.runtime.interopservices
{

    public abstract class SafeHandle : System.Runtime.ConstrainedExecution.CriticalFinalizerObject,
                                       System.IDisposable
    {

        protected SafeHandle(long invalidHandleValue, bool ownsHandle)
        {
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
        }

    }

}


namespace microsoft.win32.safehandles
{

    public abstract class SafeHandleZeroOrMinusOneIsInvalid : system.runtime.interopservices.SafeHandle
    {

        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle) : base(0, ownsHandle)
        {
        }

    }

}
