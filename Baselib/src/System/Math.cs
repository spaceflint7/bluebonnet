
namespace system
{

    public static class Math
    {

        public const double E   = 2.7182818284590451;
        public const double PI  = 3.1415926535897931;
        public const double Tau = 6.2831853071795862;

        public static int   Sign(sbyte a)               => java.lang.Integer.signum(a);
        public static sbyte  Abs(sbyte a)               => (sbyte) ((a < 0) ? -a : a);
        public static sbyte  Min(sbyte a, sbyte b)      => (a <= b) ? a : b;
        public static sbyte  Max(sbyte a, sbyte b)      => (a >= b) ? a : b;
        public static byte   Min(byte a, byte b)        => (a <= b) ? a : b;
        public static byte   Max(byte a, byte b)        => (a >= b) ? a : b;

        public static int   Sign(short a)               => java.lang.Integer.signum(a);
        public static short  Abs(short a)               => (short) ((a < 0) ? -a : a);
        public static short  Min(short a, short b)      => (a <= b) ? a : b;
        public static short  Max(short a, short b)      => (a >= b) ? a : b;
        public static ushort Min(ushort a, ushort b)    => (a <= b) ? a : b;
        public static ushort Max(ushort a, ushort b)    => (a >= b) ? a : b;

        public static int   Sign(int a)                 => java.lang.Integer.signum(a);
        public static int    Abs(int a)                 => (a < 0) ? -a : a;
        public static int    Min(int a, int b)          => (a <= b) ? a : b;
        public static int    Max(int a, int b)          => (a >= b) ? a : b;
        public static uint   Min(uint a, uint b)        => (a <= b) ? a : b;
        public static uint   Max(uint a, uint b)        => (a >= b) ? a : b;

        public static int   Sign(long a)                 => java.lang.Long.signum(a);
        public static long   Abs(long a)                => (a < 0) ? -a : a;
        public static long   Min(long a, long b)        => (a <= b) ? a : b;
        public static long   Max(long a, long b)        => (a >= b) ? a : b;
        public static ulong  Min(ulong a, ulong b)      => (a <= b) ? a : b;
        public static ulong  Max(ulong a, ulong b)      => (a >= b) ? a : b;

        public static int   Sign(float a)               => (int) java.lang.Math.signum(a);
        public static float  Abs(float a)               => (a < 0) ? -a : a;
        public static float  Min(float a, float b)      => (a <= b) ? a : b;
        public static float  Max(float a, float b)      => (a >= b) ? a : b;

        public static int   Sign(double a)              => (int) java.lang.Math.signum(a);
        public static double Abs(double a)              => (a < 0) ? -a : a;
        public static double Min(double a, double b)    => (a <= b) ? a : b;
        public static double Max(double a, double b)    => (a >= b) ? a : b;

        public static double  Sin(double a) => java.lang.Math.sin(a);
        public static double Sinh(double a) => java.lang.Math.sinh(a);
        public static double  Cos(double a) => java.lang.Math.cos(a);
        public static double Cosh(double a) => java.lang.Math.cosh(a);
        public static double  Tan(double a) => java.lang.Math.tan(a);
        public static double Tanh(double a) => java.lang.Math.tanh(a);

        public static double  Asin(double a) => java.lang.Math.asin(a);
        public static double Asinh(double a) =>
            java.lang.Math.log(a + java.lang.Math.sqrt(a * a + 1.0));
        public static double  Acos(double a) => java.lang.Math.acos(a);
        public static double Acosh(double a) =>
            java.lang.Math.log(a + java.lang.Math.sqrt(a * a - 1.0));
        public static double  Atan(double a) => java.lang.Math.atan(a);
        public static double Atan2(double x, double y) => java.lang.Math.atan2(x, y);
        public static double Atanh(double a) => 0.5 * java.lang.Math.log((1 + a) / (1 - a));

        public static double  Cbrt(double a)           => java.lang.Math.cbrt(a);
        public static double   Exp(double a)           => java.lang.Math.exp(a);
        public static double   Pow(double a, double b) => java.lang.Math.pow(a, b);
        public static double   Sqrt(double a)          => java.lang.Math.sqrt(a);
        public static double ScaleB(double x, int n)   => java.lang.Math.scalb(x, n);

        public static double Ceiling(double a) => java.lang.Math.ceil(a);
        public static double   Floor(double a) => java.lang.Math.floor(a);
        public static double IEEERemainder(double x, double y) =>
                java.lang.Math.IEEEremainder(x, y);
        public static double CopySign(double x, double y) => java.lang.Math.copySign(x, y);

        public static double Round(double a)
        {
            // java round clamps the values to Long.MIN_VALUE .. Long.MAX_VALUE
            // so we have to use floor rather than round
            if (a > System.Int64.MaxValue)
                return java.lang.Math.floor(a + 0.5);
            else if (a < System.Int64.MinValue)
                return java.lang.Math.floor(a - 0.5);
            else
                return java.lang.Math.round(a);
        }

        public static double Truncate(double a)
        {
            if (! (java.lang.Double.isInfinite(a) || java.lang.Double.isNaN(a)))
                a = (double) ((long) a);
            return a;
        }

        public static void OverflowException()
            => throw new System.OverflowException("Arithmetic operation resulted in an overflow.");

        public static double BitDecrement(double a)
        {
            // java.lang.Math.nextDown(double)
            if (java.lang.Double.isNaN(a) || a == java.lang.Double.NEGATIVE_INFINITY)
                return a;
            if (a == 0.0)
                return -java.lang.Double.MIN_VALUE;
            return java.lang.Double.longBitsToDouble(
                            java.lang.Double.doubleToRawLongBits(a) +
                                           ((a > 0.0) ? -1L : 1L));
        }

        public static double BitIncrement(double a)
        {
            // java.lang.Math.nextUp(double)
            if (java.lang.Double.isNaN(a) || a == java.lang.Double.POSITIVE_INFINITY)
                return a;
            a += 0.0;
            return java.lang.Double.longBitsToDouble(
                            java.lang.Double.doubleToRawLongBits(a) +
                                           ((a >= 0.0) ? 1L : -1L));
        }

        public static double MaxMagnitude(double x, double y)
        {
            var ax = java.lang.Math.abs(x);
            var ay = java.lang.Math.abs(y);
            return (   (ax > ay)
                     || (ax == ay && x >= 0.0)
                     || java.lang.Double.isNaN(ax)) ? x : y;
        }

        public static double MinMagnitude(double x, double y)
        {
            var ax = java.lang.Math.abs(x);
            var ay = java.lang.Math.abs(y);
            return (   (ax < ay)
                     || (ax == ay && x < 0.0)
                     || java.lang.Double.isNaN(ax)) ? x : y;
        }

        public static double   Log(double a) => java.lang.Math.log(a);
        public static double Log10(double a) => java.lang.Math.log10(a);
        public static double  Log2(double a) => java.lang.Math.log(a) / java.lang.Math.log(2.0);
        public static int    ILogB(double a) => (int) (java.lang.Math.log(a) / java.lang.Math.log(2.0));

        public static double Log(double a, double b)
        {
            if (java.lang.Double.isNaN(a))
                return a;
            if (java.lang.Double.isNaN(b))
                return b;
            if (    (b == 1.0)
                 || (    (a != 1.0)
                      && (    b == 0.0
                           || b == java.lang.Double.POSITIVE_INFINITY)))
                return java.lang.Double.NaN;
            return java.lang.Math.log(a) / java.lang.Math.log(b);
        }

    }



    public static class MathF
    {

        public const float E   = 2.71828175f;
        public const float PI  = 3.14159274f;
        public const float Tau = 6.28318548f;

        public static int   Sign(float a)               => (int) java.lang.Math.signum(a);
        public static float  Abs(float a)               => (a < 0) ? -a : a;
        public static float  Min(float a, float b)      => (a <= b) ? a : b;
        public static float  Max(float a, float b)      => (a >= b) ? a : b;

        public static float   Sin(float a) => (float) java.lang.Math.sin(a);
        public static float  Sinh(float a) => (float) java.lang.Math.sinh(a);
        public static float   Cos(float a) => (float) java.lang.Math.cos(a);
        public static float  Cosh(float a) => (float) java.lang.Math.cosh(a);
        public static float   Tan(float a) => (float) java.lang.Math.tan(a);
        public static float  Tanh(float a) => (float) java.lang.Math.tanh(a);

        public static float  Asin(float a) => (float) java.lang.Math.asin(a);
        public static float Asinh(float a) => (float)
            java.lang.Math.log(a + java.lang.Math.sqrt((double) a * a + 1.0));
        public static float  Acos(float a) => (float) java.lang.Math.acos(a);
        public static float Acosh(float a) => (float)
            java.lang.Math.log(a + java.lang.Math.sqrt((double) a * a - 1.0));
        public static float  Atan(float a) => (float) java.lang.Math.atan(a);
        public static float Atan2(float x, float y) => (float) java.lang.Math.atan2(x, y);
        public static float Atanh(float a) =>
            (float) java.lang.Math.log((1 + a) / (1 - a)) * 0.5f;

        public static float Cbrt(float a) => (float) java.lang.Math.cbrt(a);
        public static float Exp(float a) => (float) java.lang.Math.exp(a);
        public static float Pow(float a, float b) => (float) java.lang.Math.pow(a, b);
        public static float Sqrt(float a) => (float) java.lang.Math.sqrt(a);
        public static float ScaleB(float x, int n) => (float) java.lang.Math.scalb(x, n);

        public static float Ceiling(float a) => (float) java.lang.Math.ceil(a);
        public static float Floor(float a) => (float) java.lang.Math.floor(a);
        public static float IEEERemainder(float x, float y) =>
                (float) java.lang.Math.IEEEremainder(x, y);
        public static float CopySign(float x, float y) => java.lang.Math.copySign(x, y);

        public static float Round(float a)
        {
            // java round clamps the values to Long.MIN_VALUE .. Long.MAX_VALUE
            // so we have to use floor rather than round
            if (a > System.Int64.MaxValue)
                return (float) java.lang.Math.floor(a + 0.5);
            else if (a < System.Int64.MinValue)
                return (float) java.lang.Math.floor(a - 0.5);
            else
                return (float) java.lang.Math.round(a);
        }

        public static float Truncate(float a)
        {
            if (! (java.lang.Float.isInfinite(a) || java.lang.Float.isNaN(a)))
                a = (float) ((long) a);
            return a;
        }

        public static float BitDecrement(float a)
        {
            // java.lang.Math.nextDown(float)
            if (java.lang.Float.isNaN(a) || a == java.lang.Float.NEGATIVE_INFINITY)
                return a;
            if (a == 0.0f)
                return -java.lang.Float.MIN_VALUE;
            return java.lang.Float.intBitsToFloat(
                            java.lang.Float.floatToRawIntBits(a) +
                                           ((a > 0.0f) ? -1 : 1));
        }

        public static float BitIncrement(float a)
        {
            // java.lang.Math.nextUp(float)
            if (java.lang.Float.isNaN(a) || a == java.lang.Float.POSITIVE_INFINITY)
                return a;
            a += 0.0f;
            return java.lang.Float.intBitsToFloat(
                            java.lang.Float.floatToRawIntBits(a) +
                                           ((a >= 0.0f) ? 1 : -1));
        }

        public static float MaxMagnitude(float x, float y)
        {
            var ax = java.lang.Math.abs(x);
            var ay = java.lang.Math.abs(y);
            return (   (ax > ay)
                     || (ax == ay && x >= 0.0f)
                     || java.lang.Float.isNaN(ax)) ? x : y;
        }

        public static float MinMagnitude(float x, float y)
        {
            var ax = java.lang.Math.abs(x);
            var ay = java.lang.Math.abs(y);
            return (   (ax < ay)
                     || (ax == ay && x < 0.0f)
                     || java.lang.Float.isNaN(ax)) ? x : y;
        }

        public static float   Log(float a) => (float) java.lang.Math.log(a);
        public static float Log10(float a) => (float) java.lang.Math.log10(a);
        public static float  Log2(float a) => (float) (java.lang.Math.log(a) / java.lang.Math.log(2.0));
        public static int   ILogB(float a) => (int) (java.lang.Math.log(a) / java.lang.Math.log(2.0));

        public static float Log(float a, float b)
        {
            if (java.lang.Float.isNaN(a))
                return a;
            if (java.lang.Float.isNaN(b))
                return b;
            if (    (b == 1.0f)
                 || (    (a != 1.0f)
                      && (    b == 0.0f
                           || b == java.lang.Float.POSITIVE_INFINITY)))
                return java.lang.Float.NaN;
            return (float) (java.lang.Math.log(a) / java.lang.Math.log(b));
        }

    }

}
