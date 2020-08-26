
namespace system
{

    public static class GC
    {

        public static void Collect() => java.lang.System.gc();

        public static void SuppressFinalize(object obj)
        {
            if (obj is SuppressibleFinalize objSuppressibleFinalize)
            {
                // any class that declares a Finalize() method should also implement
                // this interface, see also CodeMisc::CreateSuppressibleFinalize()
                objSuppressibleFinalize.Set();
            }
            else if (obj == null)
                throw new System.ArgumentNullException();
        }

        public interface SuppressibleFinalize
        {
            void Set();
        }

    }

}
