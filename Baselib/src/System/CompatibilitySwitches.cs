
namespace system
{

    internal static class CompatibilitySwitches
    {

        // used by System.IO.StreamWriter
        public static bool IsAppEarlierThanWindowsPhone8 => false;



        //
        // System.TimeSpan
        //

        public static class TimeSpan
        {
            // System.TimeSpan.LegacyFormatMode is a native call, which is
            // forwarded to the method below, see NativeMethodClasses in CodeCall.
            public static bool LegacyFormatMode() => false;
        }

        public static bool IsNetFx40TimeSpanLegacyFormatMode => false;

    }

}
