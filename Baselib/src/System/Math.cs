
namespace system
{

    public static class Math
    {
        public static int Min(int a, int b) => (a <= b) ? a : b;
        public static int Max(int a, int b) => (a >= b) ? a : b;

        public static uint Min(uint a, uint b) => (a <= b) ? a : b;
        public static uint Max(uint a, uint b) => (a >= b) ? a : b;

        public static double Sin(double a) => java.lang.Math.sin(a);
        public static double Cos(double a) => java.lang.Math.cos(a);

        public static void OverflowException()
            => throw new System.OverflowException("Arithmetic operation resulted in an overflow.");
    }

}
