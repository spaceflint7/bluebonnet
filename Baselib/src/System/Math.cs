
namespace system
{

    public static class Math
    {
        public static int Abs(int a) => (a < 0) ? -a : a;
        public static int Min(int a, int b) => (a <= b) ? a : b;
        public static int Max(int a, int b) => (a >= b) ? a : b;

        public static uint Min(uint a, uint b) => (a <= b) ? a : b;
        public static uint Max(uint a, uint b) => (a >= b) ? a : b;

        public static long Abs(long a) => (a < 0) ? -a : a;
        public static long Min(long a, long b) => (a <= b) ? a : b;
        public static long Max(long a, long b) => (a >= b) ? a : b;

        public static double Sin(double a) => java.lang.Math.sin(a);
        public static double Cos(double a) => java.lang.Math.cos(a);

        public static double Pow(double a, double b) => java.lang.Math.pow(a, b);

        public static void OverflowException()
            => throw new System.OverflowException("Arithmetic operation resulted in an overflow.");
    }

}
