
namespace system.diagnostics
{

    public sealed class Debugger
    {

        public static void NotifyOfCrossThreadDependency()
        {
            // called by method GetCompletionAction(Task, ref MoveNextRunner)
            // in class System.Runtime.CompilerServices.AsyncMethodBuilderCore
        }

        public static bool IsAttached => false;

    }

}

namespace system.diagnostics.tracing
{

    public class EventSource : System.IDisposable
    {

        public bool IsEnabled() => false;

        public bool IsEnabled(System.Diagnostics.Tracing.EventLevel level,
                              System.Diagnostics.Tracing.EventKeywords keywords) => false;

        public void Dispose()
        {
        }

    }

    public sealed class FrameworkEventSource : EventSource
    {

        public static readonly FrameworkEventSource Log = new FrameworkEventSource();

        public static bool IsInitialized => true;

    }

}

namespace system.threading.tasks
{
    public sealed class TplEtwProvider : system.diagnostics.tracing.EventSource
    {

        public static TplEtwProvider Log = new TplEtwProvider();

        public void NewID(int dummy)
        {
            // called by method NewId
            // in class System.Threading.Tasks.Task
        }

        public void RunningContinuation(int dummyID, object dummyObject)
        {
            // called by method FinishContinuations
            // in class System.Threading.Tasks.Task
        }

    }

}

namespace system.collections.concurrent
{
    internal sealed class CDSCollectionETWBCLProvider : system.diagnostics.tracing.EventSource
    {
        public static CDSCollectionETWBCLProvider Log = new CDSCollectionETWBCLProvider();
    }
}
