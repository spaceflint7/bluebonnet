
namespace system.threading
{
    public class SynchronizationContext
    {
        private bool _requireWaitNotification;

        public SynchronizationContext()
        {
        }

        public static SynchronizationContext Current => Thread.CurrentThread._synchronizationContext;

        // for use by System.Runtime.CompilerServices.AsyncVoidMethodBuilder
        public static SynchronizationContext CurrentNoFlow => null;

        protected void SetWaitNotificationRequired() => _requireWaitNotification = true;

        public bool IsWaitNotificationRequired() => _requireWaitNotification;

        public virtual void Send(System.Threading.SendOrPostCallback d, object state)
            => d(state);

        public virtual void Post(System.Threading.SendOrPostCallback d, object state)
            => ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(d), state);
            //=> ThreadPool.QueueUserWorkItem(s => s.d(s.state), (d, state), false);

        public virtual void OperationStarted()
        {
        }

        public virtual void OperationCompleted()
        {
        }

        public static void SetSynchronizationContext(SynchronizationContext syncContext)
            => Thread.CurrentThread._synchronizationContext = syncContext;

        public virtual SynchronizationContext CreateCopy() => new SynchronizationContext();
    }
}
