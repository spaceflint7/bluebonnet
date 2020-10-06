
namespace system.diagnostics
{

    public class Stopwatch
    {

        [java.attr.RetainType] private long elapsed;
        [java.attr.RetainType] private long startTimeStamp;
        [java.attr.RetainType] private bool isRunning;

        public static readonly long Frequency = 1000000000L;    // nanos per second
        public static readonly bool IsHighResolution = true;

        public static long GetTimestamp() => java.lang.System.nanoTime();

        //
        //
        //

        public void Start()
        {
            if (! isRunning)
            {
                startTimeStamp = GetTimestamp();
                isRunning = true;
            }
        }

        public static Stopwatch StartNew()
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            return s;
        }

        public void Stop()
        {
            if (isRunning)
            {
                elapsed += GetTimestamp() - startTimeStamp;
                if (elapsed < 0)
                    elapsed = 0;

                isRunning = false;
            }
        }

        public void Reset()
        {
            elapsed = 0;
            isRunning = false;
            startTimeStamp = 0;
        }

        public void Restart()
        {
            elapsed = 0;
            isRunning = true;
            startTimeStamp = GetTimestamp();
        }

        //
        //
        //

        public long GetRawElapsedTicks()
        {
            var ticks = elapsed;
            if (isRunning)
                ticks += GetTimestamp() - startTimeStamp;
            return ticks;
        }

        // a Stopwatch tick, in this implementation, is one nanosecond
        public long ElapsedTicks => GetRawElapsedTicks();

        // a DateTime 'tick' is a 100-nanosecond interval
        public long GetElapsedDateTimeTicks() => GetRawElapsedTicks() / 100;
        public System.TimeSpan Elapsed => new System.TimeSpan(GetElapsedDateTimeTicks());

        // a millisecond is a 1,000,000-nanosecond interval
        public long ElapsedMilliseconds => GetRawElapsedTicks() / 1000000;

        public bool IsRunning => isRunning;

    }

}

