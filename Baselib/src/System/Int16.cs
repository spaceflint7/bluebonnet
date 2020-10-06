
using JavaUnsafe = system.runtime.interopservices.JavaUnsafe;

namespace system
{

    public class Int16 : system.ValueType, system.ValueMethod, java.lang.Cloneable,
                         System.IComparable, System.IComparable<short>,
                         System.IConvertible, System.IEquatable<short>, System.IFormattable
    {

        [java.attr.RetainType] protected short v;



        public static Int16 Box(int v) => new Int16() { v = (short) v };
        public static Int16 Box(short[] a, int i) => new Int16.InArray(a, i);
        protected virtual ValueType Clone(int v) => Int16.Box(v);

        public virtual int Get() => v;
        public virtual int VolatileGet() =>
            JavaUnsafe.Obj.getIntVolatile(this, ValueOffset);

        public virtual void Set(int v) => this.v = (short) v;
        public virtual void VolatileSet(int v) =>
            JavaUnsafe.Obj.putIntVolatile(this, ValueOffset, v);

        public static void Set(int v, Int16 o) => o.Set(v);
        public static void VolatileSet(int v, Int16 o) => o.VolatileSet(v);



        public override bool Equals(object obj)
        {
            var objInt16 = obj as Int16;
            return (objInt16 != null && objInt16.Get() == Get());
        }

        public override int GetHashCode() => Get();

        public override string ToString() => java.lang.Integer.toString(Get());



        // System.IEquatable<short>
        public bool Equals(short v) => v == Get();

        // System.IFormattable
        public string ToString(string format, System.IFormatProvider provider)
        {
            if (string.IsNullOrEmpty(format))
                return ToString();
            return ParseNumbers.FormatNumber((java.lang.String) (object) format, provider,
                                             java.lang.Short.valueOf((short) Get()));
        }

        public string ToString(string format) => ToString(format, null);

        public string ToString(System.IFormatProvider provider) => ToString();

        // System.IComparable
        public virtual int CompareTo(object obj)
        {
            if (obj is Int16 objInt16)
                return CompareTo((short) objInt16.Get());
            else if (object.ReferenceEquals(obj, null))
                return 1;
            throw new System.ArgumentException();
        }

        // System.IComparable<short>
        public int CompareTo(short b)
        {
            var a = Get();
            return (a < b ? -1 : a > b ? 1 : 0);
        }




        void ValueMethod.Clear() => Set(0);
        void ValueMethod.CopyTo(ValueType into) => ((Int16) into).Set(Get());
        ValueType ValueMethod.Clone() => Clone(Get());



        static long ValueOffset
        {
            get
            {
                if (_ValueOffset == -1)
                {
                    _ValueOffset = JavaUnsafe.FieldOffset(
                                                (java.lang.Class) typeof(Int16),
                                                java.lang.Short.TYPE);
                }
                return _ValueOffset;
            }
        }
        [java.attr.RetainType] static long _ValueOffset = -1;



        //
        // CodeNumber.Indirection methods
        //



        public int Get_U8() => (byte) Get();
        public int Get_I8() => (sbyte) Get();
        public void Set_I8(int v) => Set((int) ((byte) v | ((uint) Get() & (uint) 0xFFFFFF00)));

        public int Get_U32() => throw new System.NotSupportedException();
        public int Get_I32() => throw new System.NotSupportedException();
        public void Set_I32(int v) => throw new System.NotSupportedException();

        public long Get_I64() => throw new System.NotSupportedException();
        public void Set_I64(long v) => throw new System.NotSupportedException();

        public float Get_F32() => throw new System.NotSupportedException();
        public void Set_F32(float v) => throw new System.NotSupportedException();

        public double Get_F64() => throw new System.NotSupportedException();
        public void Set_F64(double v) => throw new System.NotSupportedException();



        //
        // IConvertible
        //



        public virtual System.TypeCode GetTypeCode() => System.TypeCode.Int16;

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

        private sealed class InArray : Int16
        {
            [java.attr.RetainType] private short[] a;
            [java.attr.RetainType] private int i;

            public InArray(short[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override int Get() => a[i];
            public override int VolatileGet() => throw new System.NotSupportedException();

            public override void Set(int v) => a[i] = (short) v;
            public override void VolatileSet(int v) => throw new System.NotSupportedException();
        }

    }



    #pragma warning disable 0659
    public class UInt16 : Int16, System.IComparable<ushort>, System.IEquatable<ushort>
    {

        new public static UInt16 Box(int v) => new UInt16() { v = (short) v };
        public static UInt16 Box(ushort[] a, int i) => new UInt16.InArray(a, i);
        protected override ValueType Clone(int v) => UInt16.Box(v);

        public override bool Equals(object obj)
        {
            var objUInt16 = obj as UInt16;
            return (objUInt16 != null && objUInt16.Get() == Get());
        }

        // System.IEquatable<ushort>
        public bool Equals(ushort v) => v == Get();

        public override System.TypeCode GetTypeCode() => System.TypeCode.UInt16;

        // System.IComparable
        public override int CompareTo(object obj)
        {
            if (obj is UInt16 objUInt16)
                return CompareTo((ushort) objUInt16.Get());
            else if (object.ReferenceEquals(obj, null))
                return 1;
            throw new System.ArgumentException();
        }

        // System.IComparable<ushort>
        public int CompareTo(ushort v) => CompareTo((short) Get(), (short) v);

        public static int CompareTo(short a, short b)
            => a == b ? 0 : (a & 0xFFFF) < (b & 0xFFFF) ? -1 : 1;



        //
        // InArray
        //

        private sealed class InArray : UInt16
        {
            [java.attr.RetainType] private ushort[] a;
            [java.attr.RetainType] private int i;

            public InArray(ushort[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override int Get() => a[i];
            public override int VolatileGet() => throw new System.NotSupportedException();

            public override void Set(int v) => a[i] = (ushort) v;
            public override void VolatileSet(int v) => throw new System.NotSupportedException();
        }

    }

}
