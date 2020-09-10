
namespace system.runtime.compilerservices {

    public static class RuntimeHelpers
    {

        public static void PrepareConstrainedRegions()
        {
            // this method is described as a do-nothing marker for the JIT
        }

        public static int get_OffsetToStringData()
        {
            // a call to this method is generated when a string variable
            // is assigned to a pointer:  fixed (char* ptr = str) { ... }
            return 0;
        }

        public static int GetHashCode(object o)
            => (o != null) ? o.GetHashCode() : 0;

    }

}
