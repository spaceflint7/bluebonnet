
using JavaUnsafe = system.runtime.interopservices.JavaUnsafe;

namespace system
{

    public class Double : system.ValueType, system.ValueMethod, java.lang.Cloneable,
                          System.IComparable, System.IComparable<double>,
                          System.IConvertible, System.IEquatable<double>, System.IFormattable
    {

        [java.attr.RetainType] protected double v;



        public static Double Box(double v) => new Double() { v = v };
        public static Double Box(double[] a, int i) => new Double.InArray(a, i);

        public virtual double Get() => v;
        public virtual double VolatileGet() =>
            java.lang.Double.longBitsToDouble(JavaUnsafe.Obj.getLongVolatile(this, ValueOffset));

        public virtual void Set(double v) => this.v = v;
        public virtual void VolatileSet(double v) =>
            JavaUnsafe.Obj.putLongVolatile(this, ValueOffset, java.lang.Double.doubleToRawLongBits(v));

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

        public override string ToString() => ParseNumbers.DoubleToString(Get());



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

        public string ToString(string format) => ToString(format, null);

        public string ToString(System.IFormatProvider provider) => ToString();



        // System.IComparable
        public virtual int CompareTo(object obj)
        {
            if (obj is Double objDouble)
                return CompareTo(objDouble.Get());
            else if (object.ReferenceEquals(obj, null))
                return 1;
            throw new System.ArgumentException();
        }

        // System.IComparable<double>
        public int CompareTo(double b)
        {
            var a = Get();
            return (a < b ? -1 : a > b ? 1 : 0);
        }



        void ValueMethod.Clear() => Set(0);
        void ValueMethod.CopyTo(ValueType into) => ((Double) into).Set(Get());
        ValueType ValueMethod.Clone() => Box(Get());



        static long ValueOffset
        {
            get
            {
                if (_ValueOffset == -1)
                {
                    _ValueOffset = JavaUnsafe.FieldOffset(
                                                (java.lang.Class) typeof(Double),
                                                java.lang.Double.TYPE);
                }
                return _ValueOffset;
            }
        }
        [java.attr.RetainType] static long _ValueOffset = -1;



        //
        // Is
        //

        public static bool IsNaN(double d) => java.lang.Double.isNaN(d);
        public static bool IsFinite(double d) => ! java.lang.Double.isInfinite(d);
        public static bool IsInfinity(double d) => java.lang.Double.isInfinite(d);
        public static bool IsPositiveInfinity (double d) => d == java.lang.Double.POSITIVE_INFINITY;
        public static bool IsNegativeInfinity(double d) => d == java.lang.Double.NEGATIVE_INFINITY;
        public static bool IsNegative(double d) => d < 0.0;
        public static bool IsNormal(double d) => d >= java.lang.Double.MIN_NORMAL;
        public static bool IsSubnormal(double d) => d < java.lang.Double.MIN_NORMAL;



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
        // CodeNumber.Indirection methods
        //

        public int Get_U8() => throw new System.NotSupportedException();
        public int Get_I8() => throw new System.NotSupportedException();
        public void Set_I8(int v) => throw new System.NotSupportedException();

        public int Get_U16() => throw new System.NotSupportedException();
        public int Get_I16() => throw new System.NotSupportedException();
        public void Set_I16(int v) => throw new System.NotSupportedException();

        public int Get_I32() => throw new System.NotSupportedException();
        public void Set_I32(int v) => throw new System.NotSupportedException();

        public long Get_I64() => java.lang.Double.doubleToRawLongBits(v);
        public void Set_I64(long v) => Set(java.lang.Double.longBitsToDouble(v));

        public double Get_F64() => throw new System.NotSupportedException();
        public void Set_F64(double v) => throw new System.NotSupportedException();



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
            => throw new System.InvalidCastException();
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider)
            => system.Convert.DefaultToType((System.IConvertible) this, type, provider);



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
                    JavaUnsafe.Obj.getLongVolatile(a, JavaUnsafe.ElementOffset64(i)));

            public override void Set(double v) => a[i] = v;
            public override void VolatileSet(double v) =>
                JavaUnsafe.Obj.putLongVolatile(a, JavaUnsafe.ElementOffset64(i),
                                                        java.lang.Double.doubleToRawLongBits(v));
        }

    }

}
