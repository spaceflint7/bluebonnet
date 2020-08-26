
namespace system
{

    public struct Decimal
    {

        [java.attr.RetainType] private java.math.BigDecimal v;

        public static readonly Decimal Zero;
        public static readonly Decimal One;
        public static readonly Decimal MinusOne;
        public static readonly Decimal MaxValue;
        public static readonly Decimal MinValue;

        //
        // constructors
        //

        public Decimal(int intValue)
        {
            v = new java.math.BigDecimal(intValue);
        }

        public Decimal(uint uintValue)
        {
            v = new java.math.BigDecimal((long) uintValue);
        }

        public Decimal(long longValue)
        {
            v = new java.math.BigDecimal(longValue);
        }

        public Decimal(ulong ulongValue)
        {
            var long31 = (long) (ulongValue & System.Int64.MaxValue);
            if (long31 != (long) ulongValue)
            {
                v = new java.math.BigDecimal(
                            java.math.BigInteger.valueOf(long31)
                                    .add(java.math.BigInteger.ONE.shiftLeft(63)));
            }
            else
                v = new java.math.BigDecimal(long31);
        }

        public Decimal(float floatValue)
        {
            v = new java.math.BigDecimal(floatValue, java.math.MathContext.DECIMAL32)
                                        .stripTrailingZeros();
        }

        public Decimal(double doubleValue)
        {
            v = new java.math.BigDecimal(doubleValue, java.math.MathContext.DECIMAL64)
                                        .stripTrailingZeros();
        }

        public Decimal(int[] bits)
        {
            if (bits == null)
                throw new System.ArgumentNullException();
            if (bits.Length == 4)
            {
                int flg = bits[3];
                if ((flg & 0x7F00FFFF) == 0)
                {
                    bool neg = ((flg & 0x80000000) != 0) ? true : false;
                    byte scl = (byte) ((flg & 0x00FF0000) >> 16);
                    if (scl <= 28)
                    {
                        v = null;
                        SetBits(bits[0], bits[1], bits[2], neg, scl);
                    }
                }
            }
            throw new System.ArgumentException();
        }

        public Decimal(int lo, int mid, int hi, bool neg, byte scl)
        {
            v = null;
            SetBits(lo, mid, hi, neg, scl);
        }

        //
        //
        //

        public void SetBits(int lo, int mid, int hi, bool neg, byte scl)
        {
            //var bytes = new byte[13];
        }

        public static int[] GetBits(Decimal d)
        {
            var bigDec = d.v.stripTrailingZeros();

            var bits = new int[4];
            bits[3] = bigDec.scale() << 16;

            var bigInt = bigDec.unscaledValue();
            if (bigInt.signum() < 0)
            {
                bigInt = bigInt.negate();
                bits[3] |= unchecked ((int) 0x80000000);
            }

            bits[0] = bigInt.intValue();
            bigInt = bigInt.shiftRight(32);
            bits[1] = bigInt.intValue();
            bigInt = bigInt.shiftRight(32);
            bits[2] = bigInt.intValue();

            return bits;
        }

        //
        // value methods
        //

        public override bool Equals(object obj)
        {
            if (obj is Decimal objDecimal)
                return objDecimal.v.compareTo(v) == 0;
            return false;
        }

        public override int GetHashCode()
        {
            // this implementation matches .Net framework; .Net Core does something else
            var x = java.lang.Double.doubleToRawLongBits(v.doubleValue());
            var lo = (int) (x & 0xFFFFFFF0);
            var hi = (int) (x >> 32);
            return hi ^ lo;
        }

        public override string ToString() => v.ToString();

        //
        // converters
        //

        public static double ToDouble(Decimal d) => d.v.doubleValue();

        //
        // static constants
        //

        static Decimal()
        {
            Zero = new Decimal((int) 0);
            One = new Decimal((int) 1);
            MinusOne = new Decimal((int) -1);
            MaxValue = new Decimal(-1, -1, -1, false, 0);
            MinValue = new Decimal(-1, -1, -1, true, 0);
        }
    }
}

