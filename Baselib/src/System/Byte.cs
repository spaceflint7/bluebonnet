
namespace system
{

    public class SByte : system.ValueType, system.ValueMethod,
                         System.IConvertible, System.IEquatable<sbyte>, System.IFormattable
    {

        [java.attr.RetainType] protected sbyte v;



        public static SByte Box(int v) => new SByte() { v = (sbyte) v };
        public static SByte Box(sbyte[] a, int i) => new SByte.InArray(a, i);

        public virtual int Get() => v;
        public virtual int VolatileGet() => throw new System.NotSupportedException();

        public virtual void Set(int v) => this.v = (sbyte) v;
        public virtual void VolatileSet(int v) => throw new System.NotSupportedException();

        public static void Set(int v, SByte o) => o.Set(v);
        public static void VolatileSet(int v, SByte o) => throw new System.NotSupportedException();



        public override bool Equals(object obj)
        {
            var objSByte = obj as SByte;
            return (objSByte != null && objSByte.Get() == Get());
        }

        public override int GetHashCode() => Get();

        public override string ToString() => java.lang.Integer.toString(Get());



        // System.IEquatable<sbyte>
        public bool Equals(sbyte v) => v == Get();

        // System.IFormattable
        public string ToString(string format, System.IFormatProvider provider)
        {
            if (string.IsNullOrEmpty(format))
                return ToString();
            return ParseNumbers.FormatNumber(
                (java.lang.String) (object) format, provider, java.lang.Integer.valueOf(Get()));
        }



        void ValueMethod.Clear() => Set(0);
        void ValueMethod.CopyFrom(ValueType from) => Set(((SByte) from).Get());
        void ValueMethod.CopyInto(ValueType into) => ((SByte) into).Set(Get());
        ValueType ValueMethod.Clone() => Box(Get());



        static long ValueOffset
        {
            get
            {
                if (_ValueOffset == -1)
                {
                    var cls = (java.lang.Class) typeof(SByte);
                    _ValueOffset = Util.JavaUnsafe.objectFieldOffset(cls.getDeclaredFields()[0]);
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

        public int Get_U16() => throw new System.NotSupportedException();
        public int Get_I16() => throw new System.NotSupportedException();
        public void Set_I16(int v) => throw new System.NotSupportedException();

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



        public virtual System.TypeCode GetTypeCode() => System.TypeCode.SByte;

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

        private sealed class InArray : SByte
        {
            [java.attr.RetainType] private sbyte[] a;
            [java.attr.RetainType] private int i;

            public InArray(sbyte[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override int Get() => a[i];
            public override int VolatileGet() => throw new System.NotSupportedException();

            public override void Set(int v) => a[i] = (sbyte) v;
            public override void VolatileSet(int v) => throw new System.NotSupportedException();
        }

    }



    #pragma warning disable 0659
    public class Byte : SByte, System.IEquatable<byte>
    {

        new public static Byte Box(int v) => new Byte() { v = (sbyte) v };
        public static Byte Box(byte[] a, int i) => new Byte.InArray(a, i);

        public static void Set(int v, Byte o) => o.Set(v);
        public static void VolatileSet(int v, Byte o) => throw new System.NotSupportedException();

        public override bool Equals(object obj)
        {
            var objByte = obj as Byte;
            return (objByte != null && objByte.Get() == Get());
        }

        // System.IEquatable<sbyte>
        public bool Equals(byte v) => v == Get();

        public override System.TypeCode GetTypeCode() => System.TypeCode.Byte;




        //
        // InArray
        //

        private sealed class InArray : Byte
        {
            [java.attr.RetainType] private byte[] a;
            [java.attr.RetainType] private int i;

            public InArray(byte[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override int Get() => a[i];
            public override int VolatileGet() => throw new System.NotSupportedException();

            public override void Set(int v) => a[i] = (byte) v;
            public override void VolatileSet(int v) => throw new System.NotSupportedException();
        }

    }

}
