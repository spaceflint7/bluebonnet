
namespace system.threading
{

    public sealed class ExecutionContext : System.IDisposable,
                                           System.Runtime.Serialization.ISerializable
    {

        private static readonly ExecutionContext Default = new ExecutionContext(true);

        private readonly bool m_isFlowSuppressed = false;
        private readonly bool m_isDefault;



        private ExecutionContext(bool isDefault) => m_isDefault = isDefault;



        public static ExecutionContext PreAllocatedDefault => Default;
        public bool IsPreAllocatedDefault => m_isDefault;



        // System.IDisposable::Dispose
        public void Dispose()
        {
        }

        // System.Runtime.Serialization.ISerializable::GetObjectData
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
                                  System.Runtime.Serialization.StreamingContext context)
            => throw new System.PlatformNotSupportedException();



        public static ExecutionContext Capture()
        {
            var executionContext = Thread.CurrentThread._executionContext;
            return   (executionContext == null) ? Default
                   : (executionContext.m_isFlowSuppressed ? null : executionContext);
        }

        public static ExecutionContext Capture(ref StackCrawlMark stackMark, CaptureOptions options)
        {
            return Capture();
        }

        public static ExecutionContext FastCapture()
        {
            // called by constructor(Action, bool)
            // in class System.Threading.Tasks.AwaitTaskContinuation
            return Capture();
        }

        public static bool IsFlowSuppressed()
        {
            var executionContext = Thread.CurrentThread._executionContext;
            return executionContext != null && executionContext.m_isFlowSuppressed;
        }

        public static void EstablishCopyOnWriteScope(ref ExecutionContextSwitcher ecs)
            => ecs.Save();

        public static void Run(ExecutionContext executionContext,
                               System.Threading.ContextCallback callback, object state)
        {
            if (executionContext == null)
                throw new System.InvalidOperationException();
            ExecutionContextSwitcher ecs = default(ExecutionContextSwitcher);
            ecs.Save();
            try
            {
                callback.Invoke(state);
            }
            finally
            {
                ecs.Undo();
            }
        }

        public static void Run(ExecutionContext executionContext,
                               System.Threading.ContextCallback callback,
                               object state, bool preserveSyncCtx)
            => Run(executionContext, callback, state);



        [System.Flags]
        public enum CaptureOptions
        {
            None = 0x00,
            IgnoreSyncCtx = 0x01,
            OptimizeDefaultCase = 0x02,
        }
    }



    public struct ExecutionContextSwitcher
    {
        Thread currentThread;
        ExecutionContext executionContext;
        SynchronizationContext synchronizationContext;

        public void Save()
        {
            currentThread = Thread.CurrentThread;
            executionContext = currentThread._executionContext;
            synchronizationContext = currentThread._synchronizationContext;
        }

        public void Undo()
        {
            currentThread._synchronizationContext = synchronizationContext;
            currentThread._executionContext = executionContext;
        }

    }

}

