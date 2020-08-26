
namespace system.numerics
{

    public struct BigInteger
    {

        [java.attr.RetainType] private java.math.BigInteger v;

        [java.attr.RetainType] private static readonly BigInteger _val1;
        [java.attr.RetainType] private static readonly BigInteger _val0;
        [java.attr.RetainType] private static readonly BigInteger _valm1;

        //
        // constructors
        //

        public BigInteger(int intValue)
        {
            v = java.math.BigInteger.valueOf((long) intValue);
        }

        public BigInteger(uint uintValue)
        {
            v = java.math.BigInteger.valueOf((long) uintValue);
        }

        public BigInteger(long longValue)
        {
            v = java.math.BigInteger.valueOf(longValue);
        }

        public BigInteger(ulong ulongValue)
        {
            var long31 = (long) (ulongValue & System.Int64.MaxValue);
            v = java.math.BigInteger.valueOf(long31);
            if (long31 != (long) ulongValue)
                v = v.add(java.math.BigInteger.ONE.shiftLeft(63));
        }

        public BigInteger(float floatValue)
        {
            v = java.math.BigInteger.valueOf((long) (int) floatValue);
        }

        public BigInteger(double doubleValue)
        {
            v = java.math.BigInteger.valueOf((long) doubleValue);
        }

        private BigInteger(string stringValue)
        {
            v = new java.math.BigInteger(stringValue);
        }

        //
        // value methods
        //

        public override bool Equals(object obj)
        {
            if (obj is BigInteger objBigInteger)
                return objBigInteger.v.compareTo(v) == 0;
            return false;
        }

        public override int GetHashCode() => v.GetHashCode();

        public override string ToString() => v.ToString();

        //
        // converters
        //

        // public static double ToDouble(Decimal d) => d.v.doubleValue();

        //
        //
        //

        public static bool TryParse(string value, out BigInteger result)
        {
            try
            {
                // TODO: check if starts with NumberFormatInfo.Positive/NegativeSign
                result = new BigInteger(value);
                return true;
            }
            catch (java.lang.NumberFormatException)
            {
                result = _val0;
                return false;
            }
        }

        //
        //
        //

        public BigInteger One => _val1;
        public BigInteger Zero => _val0;
        public BigInteger MinusOne => _valm1;

        //
        // static constants
        //

        static BigInteger()
        {
            _val1 = new BigInteger(1);
            _val0 = new BigInteger(0);
            _valm1 = new BigInteger(-1);
        }
    }
}

