
namespace system
{

    public static class Math
    {
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

        public static double   Pow(double a, double b) => java.lang.Math.pow(a, b);
        public static double   Sqrt(double a)          => java.lang.Math.sqrt(a);
        public static double ScaleB(double x, int n)   => java.lang.Math.scalb(x, n);

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
    }

}
