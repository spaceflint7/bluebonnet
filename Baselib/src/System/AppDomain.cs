
namespace system
{
    public sealed class AppDomain
    {

        public static AppDomain CurrentDomain
            => System.Threading.LazyInitializer.EnsureInitialized<AppDomain>(ref theInstance);

        public event System.EventHandler DomainUnload
        {
            add
            {
            }
            remove
            {
            }
        }

        private static AppDomain theInstance;
    }
}
