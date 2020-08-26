
namespace system.runtime.exceptionservices
{

    public sealed class ExceptionDispatchInfo
    {

        private System.Exception m_Exception;

        private ExceptionDispatchInfo(System.Exception exception) => m_Exception = exception;

        public static ExceptionDispatchInfo Capture(System.Exception source)
        {
            if (source == null)
            {
                throw new System.ArgumentNullException(
                        "source", Environment.GetResourceString("ArgumentNull_Obj"));
            }
            return new ExceptionDispatchInfo(source);
        }

        public System.Exception SourceException => m_Exception;

        public void Throw() => throw new System.AggregateException(m_Exception);

    }

}
