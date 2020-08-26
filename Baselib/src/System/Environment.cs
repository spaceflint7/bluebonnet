
namespace system
{

    public static class Environment
    {



        internal static string GetResourceString(string key)
        {
            return "(" + key + "?)";
        }

        internal static string GetResourceString(string key, params object[] values)
        {
            string s = GetResourceString(key);
            foreach (var v in values)
                s += " " + v.ToString();
            return s;
        }



        public static string NewLine => _Newline ?? ( _Newline = java.lang.String.format("%n", null) );
        [java.attr.RetainType] static string _Newline;



        public static int CurrentManagedThreadId => 0;



        public static bool IsWindows8OrAbove => false;

        public static int TickCount
        {
            get
            {
                return (int) java.lang.System.currentTimeMillis();
            }
        }



        // same as system.threading.PlatformHelper.ProcessorCount
        public static int ProcessorCount => java.lang.Runtime.getRuntime().availableProcessors();

    }
}
