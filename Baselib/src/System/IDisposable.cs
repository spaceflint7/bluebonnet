
namespace system
{

    [java.attr.AsInterface]
    public abstract class IDisposable : java.lang.AutoCloseable
    {

        public abstract void Dispose();

        [java.attr.RetainName]
        public void close()
        {
            ((System.IDisposable) this).Dispose();
        }

    }

}

