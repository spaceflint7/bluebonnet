
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



        //
        // GetAssemblies
        //

        public System.Reflection.Assembly[] GetAssemblies()
        {
            var classLoaderObject =
                    ((java.lang.Object) (object) this).getClass().getClassLoader();
            InitClassLoaderFields(classLoaderObject);
            var domains = (java.util.Set) Util.JavaUnsafe.getObject(
                                                classLoaderObject, _DomainsOffset);
            int n = domains.size();
            int i = 0;
            var assemblies = new System.Reflection.Assembly[n];
            for (var it = domains.iterator(); it.hasNext() && i < n; i++)
            {
                var domain = (java.security.ProtectionDomain) it.next();
                assemblies[i] =
                    system.reflection.RuntimeAssembly.GetAssemblyForDomain(domain);
            }
            return assemblies;
        }

        //
        // GetClassesInAssembly
        //

        public java.util.Vector GetAllClasses()
        {
            var classLoaderObject =
                    ((java.lang.Object) (object) this).getClass().getClassLoader();
            InitClassLoaderFields(classLoaderObject);
            return (java.util.Vector) Util.JavaUnsafe.getObject(
                                                classLoaderObject, _ClassesOffset);
        }

        //
        // InitClassLoaderFields
        //

        private void InitClassLoaderFields(java.lang.ClassLoader classLoaderObject)
        {
            if (_ClassesOffset == -1 || _DomainsOffset == -1)
            {
                var classLoaderClass =
                        ((java.lang.Object) (object) classLoaderObject).getClass();
                for (;;)
                {
                    if (classLoaderClass ==
                                (java.lang.Class) typeof(java.lang.ClassLoader))
                        break;
                    classLoaderClass = classLoaderClass.getSuperclass();
                }

                _ClassesOffset = Util.JavaUnsafe.objectFieldOffset(
                                            classLoaderClass.getDeclaredField("classes"));
                _DomainsOffset = Util.JavaUnsafe.objectFieldOffset(
                                            classLoaderClass.getDeclaredField("domains"));
            }
        }

        [java.attr.RetainType] static long _ClassesOffset = -1;
        [java.attr.RetainType] static long _DomainsOffset = -1;
    }
}
