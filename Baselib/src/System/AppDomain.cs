
using System;
using JavaUnsafe = system.runtime.interopservices.JavaUnsafe;

namespace system
{
    public sealed class AppDomain
    {
        //
        // Singleton
        //

        public static AppDomain CurrentDomain
            => System.Threading.LazyInitializer.EnsureInitialized<AppDomain>(ref theInstance);

        private static AppDomain theInstance;

        public AppDomain()
        {
        }

        //
        // DomainUnload
        //

        public event EventHandler DomainUnload
        {
            add
            {
            }
            remove
            {
            }
        }

        //
        // UnhandledException
        //

        public event UnhandledExceptionEventHandler UnhandledException
        {
            add
            {
                lock (this)
                {
                    if (! _UnhandledExceptionInstalled)
                    {
                        java.lang.Thread.setDefaultUncaughtExceptionHandler(
                            ((java.lang.Thread.UncaughtExceptionHandler.Delegate)
                                _UnhandledExceptionHandler).AsInterface());

                        _UnhandledExceptionInstalled = true;
                    }
                    _UnhandledExceptionEvent += value;
                }
            }
            remove
            {
                _UnhandledExceptionEvent -= value;
            }
        }

        private UnhandledExceptionEventHandler _UnhandledExceptionEvent;
        private volatile bool _UnhandledExceptionInstalled;

        private void _UnhandledExceptionHandler(java.lang.Thread thread,
                                                java.lang.Throwable exc)
        {
            exc = system.Util.TranslateException(exc);
            var evt = _UnhandledExceptionEvent;
            if (evt != null)
            {
                var args = new UnhandledExceptionEventArgs(exc, true);
                evt(theInstance, args);
            }
            exc.printStackTrace();
        }

        //
        // GetAssemblies
        //

        public System.Reflection.Assembly[] GetAssemblies()
            => new System.Reflection.Assembly[] {
                    system.reflection.RuntimeAssembly.CurrentAssembly };
    }
}
