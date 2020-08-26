
namespace system.threading
{

    public static class PlatformHelper
    {

        // same as system.Environment.ProcessorCount
        public static int ProcessorCount => java.lang.Runtime.getRuntime().availableProcessors();

        public static bool IsSingleProcessor => ProcessorCount == 1;

    }

}
