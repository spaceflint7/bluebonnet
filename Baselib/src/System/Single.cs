
namespace system
{

    public class Single : system.ValueType, system.ValueMethod,
                          System.IConvertible, System.IEquatable<float>, System.IFormattable
    {

        [java.attr.RetainType] protected float v;



        public static Single Box(float v) => new Single() { v = v };
        public static Single Box(float[] a, int i) => new Single.InArray(a, i);

        public virtual float Get() => v;
        public virtual float VolatileGet() =>
            java.lang.Float.intBitsToFloat(Util.JavaUnsafe.getIntVolatile(this, ValueOffset));

        public virtual void Set(float v) => this.v = v;
        public virtual void VolatileSet(float v) =>
            Util.JavaUnsafe.putIntVolatile(this, ValueOffset, java.lang.Float.floatToRawIntBits(v));

        public static void Set(float v, Single o) => o.Set(v);
        public static void VolatileSet(float v, Single o) => o.VolatileSet(v);



        public override bool Equals(object obj)
        {
            var objSingle = obj as Single;
            return (objSingle != null && objSingle.Get() == Get());
        }

        public override int GetHashCode()
        {
            int v = java.lang.Float.floatToRawIntBits(Get());
            if (((v - 1) & 0x7FFFFFFF) >= 0x7F800000)
                v &= 0x7F800000;
            return v;
        }

        public override string ToString() => java.lang.Float.toString(Get());



        // System.IEquatable<double>
        public bool Equals(float v) => v == Get();

        // System.IFormattable
        public string ToString(string format, System.IFormatProvider provider)
        {
            if (string.IsNullOrEmpty(format))
                return ToString();
            return ParseNumbers.FormatNumber(
                (java.lang.String) (object) format, provider, java.lang.Float.valueOf(Get()));
        }



        void ValueMethod.Clear() => Set(0);
        void ValueMethod.CopyFrom(ValueType from) => Set(((Single) from).Get());
        void ValueMethod.CopyInto(ValueType into) => ((Single) into).Set(Get());
        ValueType ValueMethod.Clone() => Box(Get());



        static long ValueOffset
        {
            get
            {
                if (_ValueOffset == -1)
                {
                    var cls = (java.lang.Class) typeof(Single);
                    _ValueOffset = Util.JavaUnsafe.objectFieldOffset(cls.getDeclaredFields()[0]);
                }
                return _ValueOffset;
            }
        }
        [java.attr.RetainType] static long _ValueOffset = -1;



        //
        // IConvertible
        //



        public System.TypeCode GetTypeCode() => System.TypeCode.Single;

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

        private sealed class InArray : Single
        {
            [java.attr.RetainType] private float[] a;
            [java.attr.RetainType] private int i;

            public InArray(float[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override float Get() => a[i];
            public override float VolatileGet() =>
                java.lang.Float.intBitsToFloat(
                    Util.JavaUnsafe.getIntVolatile(a, Util.ElementOffset32(i)));

            public override void Set(float v) => a[i] = v;
            public override void VolatileSet(float v) =>
                Util.JavaUnsafe.putIntVolatile(a, Util.ElementOffset32(i),
                                                        java.lang.Float.floatToRawIntBits(v));
        }

    }

}
