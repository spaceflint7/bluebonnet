
namespace system
{

    public class Double : system.ValueType, system.ValueMethod,
                          System.IConvertible, System.IEquatable<double>, System.IFormattable
    {

        [java.attr.RetainType] protected double v;



        public static Double Box(double v) => new Double() { v = v };
        public static Double Box(double[] a, int i) => new Double.InArray(a, i);

        public virtual double Get() => v;
        public virtual double VolatileGet() =>
            java.lang.Double.longBitsToDouble(Util.JavaUnsafe.getLongVolatile(this, ValueOffset));

        public virtual void Set(double v) => this.v = v;
        public virtual void VolatileSet(double v) =>
            Util.JavaUnsafe.putLongVolatile(this, ValueOffset, java.lang.Double.doubleToRawLongBits(v));

        public static void Set(double v, Double o) => o.Set(v);
        public static void VolatileSet(double v, Double o) => o.VolatileSet(v);



        public override bool Equals(object obj)
        {
            var objDouble = obj as Double;
            return (objDouble != null && objDouble.Get() == Get());
        }

        public override int GetHashCode()
        {
            long v = java.lang.Double.doubleToRawLongBits(Get());
            if (((v - 1) & 0x7FFFFFFFFFFFFFFF) >= 0x7FF0000000000000)
                v &= 0x7FF0000000000000;
            return ((int) v) ^ (int) (v >> 32);
        }

        public override string ToString() => java.lang.Double.toString(Get());



        // System.IEquatable<double>
        public bool Equals(double v) => v == Get();

        // System.IFormattable
        public string ToString(string format, System.IFormatProvider provider)
        {
            if (string.IsNullOrEmpty(format))
                return ToString();
            return ParseNumbers.FormatNumber(
                (java.lang.String) (object) format, provider, java.lang.Double.valueOf(Get()));
        }



        void ValueMethod.Clear() => Set(0);
        void ValueMethod.CopyFrom(ValueType from) => Set(((Double) from).Get());
        void ValueMethod.CopyInto(ValueType into) => ((Double) into).Set(Get());
        ValueType ValueMethod.Clone() => Box(Get());



        static long ValueOffset
        {
            get
            {
                if (_ValueOffset == -1)
                {
                    var cls = (java.lang.Class) typeof(Double);
                    _ValueOffset = Util.JavaUnsafe.objectFieldOffset(cls.getDeclaredFields()[0]);
                }
                return _ValueOffset;
            }
        }
        [java.attr.RetainType] static long _ValueOffset = -1;



        //
        // UnsignedLongToDouble
        //

        public static double UnsignedLongToDouble(long from)
        {
            if (from < 0)
                return ((double) (-from)) + 18446744073709551616.0D; // 2^64
            else
                return (double) from;
        }



        //
        //
        //

        public static bool TryParse(string s, out double result)
        {
            try
            {
                // TODO: check if begins with NumberFormatInfo.Positive/NegativeSign
                result = java.lang.Double.parseDouble(s.Trim());
                return true;
            }
            catch (java.lang.NumberFormatException)
            {
                result = 0;
                return false;
            }
        }



        //
        // IConvertible
        //



        public System.TypeCode GetTypeCode() => System.TypeCode.Double;

        bool System.IConvertible.ToBoolean(System.IFormatProvider provider)
            => System.Convert.ToBoolean(Get());
        char System.IConvertible.ToChar(System.IFormatProvider provider)
            => System.Convert.ToChar(Get());
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider)
            => System.Convert.ToSByte(Get());
        byte System.IConvertible.ToByte(System.IFormatProvider provider)
            => System.Convert.ToByte(Get());
        short System.IConvertible.ToInt16(System.IFormatProvider provider)
            => System.Convert.ToInt16(Get());
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider)
            => System.Convert.ToUInt16(Get());
        int System.IConvertible.ToInt32(System.IFormatProvider provider)
            => System.Convert.ToInt32(Get());
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider)
            => System.Convert.ToUInt32(Get());
        long System.IConvertible.ToInt64(System.IFormatProvider provider)
            => System.Convert.ToInt64(Get());
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider)
            => System.Convert.ToUInt64(Get());
        float System.IConvertible.ToSingle(System.IFormatProvider provider)
            => System.Convert.ToSingle(Get());
        double System.IConvertible.ToDouble(System.IFormatProvider provider)
            => System.Convert.ToDouble(Get());
        System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider provider)
            => System.Convert.ToDecimal(Get());
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider)
            => throw new System.InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo_Int32_DateTime"));
        string System.IConvertible.ToString(System.IFormatProvider provider)
            => ToString();
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider)
            => null;//System.Convert.DefaultToType((System.IConvertible) this, type, provider);



        //
        // InArray
        //

        private sealed class InArray : Double
        {
            [java.attr.RetainType] private double[] a;
            [java.attr.RetainType] private int i;

            public InArray(double[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override double Get() => a[i];
            public override double VolatileGet() =>
                java.lang.Double.longBitsToDouble(
                    Util.JavaUnsafe.getLongVolatile(a, Util.ElementOffset64(i)));

            public override void Set(double v) => a[i] = v;
            public override void VolatileSet(double v) =>
                Util.JavaUnsafe.putLongVolatile(a, Util.ElementOffset64(i),
                                                        java.lang.Double.doubleToRawLongBits(v));
        }

    }

}
