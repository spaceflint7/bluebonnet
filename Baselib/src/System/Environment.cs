
namespace system
{

    public static class Environment
    {



        internal static string GetResourceString(string key)
        {
            if (key == "AggregateException_ToString")
                return "{0}{1}---> (Inner Exception #{2}) {3}{4}{5}";

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



        public static void Exit(int exitCode) => java.lang.System.exit(exitCode);



        //
        // GetEnvironmentVariable
        //

        public static string GetEnvironmentVariable(string variable)
        {
            ThrowHelper.ThrowIfNull(variable);
            return java.lang.System.getenv(variable);
        }

        public static string GetEnvironmentVariable(string variable,
                                                    System.EnvironmentVariableTarget target)
        {
            if (target != System.EnvironmentVariableTarget.Process)
                throw new System.ArgumentException();
            return GetEnvironmentVariable(variable);
        }

    }
}
