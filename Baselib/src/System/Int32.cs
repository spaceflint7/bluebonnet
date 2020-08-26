
namespace system
{

    public class Int32 : system.ValueType, system.ValueMethod,
                         System.IConvertible, System.IEquatable<int>, System.IFormattable
    {

        [java.attr.RetainType] protected int v;



        public static Int32 Box(int v) => new Int32() { v = v };
        public static Int32 Box(int[] a, int i) => new Int32.InArray(a, i);

        public virtual int Get() => v;
        public virtual int VolatileGet() =>
            Util.JavaUnsafe.getIntVolatile(this, ValueOffset);

        public virtual void Set(int v) => this.v = v;
        public virtual void VolatileSet(int v) =>
            Util.JavaUnsafe.putIntVolatile(this, ValueOffset, v);

        public static void Set(int v, Int32 o) => o.Set(v);
        public static void VolatileSet(int v, Int32 o) => o.VolatileSet(v);

        public virtual bool CompareAndSwap(int expect, int update) =>
            Util.JavaUnsafe.compareAndSwapInt(this, ValueOffset, expect, update);



        public override bool Equals(object obj)
        {
            var objInt32 = obj as Int32;
            return (objInt32 != null && objInt32.Get() == Get());
        }

        public override int GetHashCode() => Get();

        public override string ToString() => java.lang.Integer.toString(Get());



        // System.IEquatable<int>
        public bool Equals(int v) => v == Get();

        // System.IFormattable
        public string ToString(string format, System.IFormatProvider provider)
        {
            if (string.IsNullOrEmpty(format))
                return ToString();
            return ParseNumbers.FormatNumber(
                (java.lang.String) (object) format, provider, java.lang.Integer.valueOf(Get()));
        }

        public string ToString(string format) => ToString(format, null);

        public string ToString(System.IFormatProvider provider) => ToString();



        public static int OverflowAdd(int a, int b)
        {
            int c = a + b;
            if (((a ^ c) & (b ^ c)) < 0)
                system.Math.OverflowException();
            return c;
        }

        public static int OverflowSubtract(int a, int b)
        {
            int c = a - b;
            if (((a ^ b) & (a ^ c)) < 0)
                system.Math.OverflowException();
            return c;
        }

        public static int OverflowMultiply(int a, int b)
        {
            long cLong = (long) a * (long) b;
            int cInt = (int) cLong;
            if (cInt != cLong)
                system.Math.OverflowException();
            return (int) cInt;
        }



        void ValueMethod.Clear() => Set(0);
        void ValueMethod.CopyFrom(ValueType from) => Set(((Int32) from).Get());
        void ValueMethod.CopyInto(ValueType into) => ((Int32) into).Set(Get());
        ValueType ValueMethod.Clone() => Box(Get());



        static long ValueOffset
        {
            get
            {
                if (_ValueOffset == -1)
                {
                    var cls = (java.lang.Class) typeof(Int32);
                    _ValueOffset = Util.JavaUnsafe.objectFieldOffset(cls.getDeclaredFields()[0]);
                }
                return _ValueOffset;
            }
        }
        [java.attr.RetainType] static long _ValueOffset = -1;



        //
        //
        //

        public static bool TryParse(string s, out int result)
        {
            try
            {
                // TODO: check if begins with NumberFormatInfo.Positive/NegativeSign
                result = java.lang.Integer.parseInt(s.Trim());
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



        public int Get_U8() => (byte) Get();
        public int Get_I8() => (sbyte) Get();
        public void Set_I8(int v) => Set((int) ((byte) v | ((uint) Get() & (uint) 0xFFFFFF00)));

        public int Get_U16() => (ushort) Get();
        public int Get_I16() => (short) Get();
        public void Set_I16(int v) => Set((int) ((ushort) v | ((uint) Get() & (uint) 0xFFFF0000)));

        public long Get_I64() => throw new System.NotSupportedException();
        public void Set_I64(long v) => throw new System.NotSupportedException();

        public float Get_F32() => java.lang.Float.intBitsToFloat(Get());
        public void Set_F32(float v) => Set(java.lang.Float.floatToRawIntBits(v));

        public double Get_F64() => throw new System.NotSupportedException();
        public void Set_F64(double v) => throw new System.NotSupportedException();



        //
        // IConvertible
        //



        public virtual System.TypeCode GetTypeCode() => System.TypeCode.Int32;

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

        private sealed class InArray : Int32
        {
            [java.attr.RetainType] private int[] a;
            [java.attr.RetainType] private int i;

            public InArray(int[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override int Get() => a[i];
            public override int VolatileGet() =>
                Util.JavaUnsafe.getIntVolatile(a, Util.ElementOffset32(i));

            public override void Set(int v) => a[i] = v;
            public override void VolatileSet(int v) =>
                Util.JavaUnsafe.putIntVolatile(a, Util.ElementOffset32(i), v);

            public override bool CompareAndSwap(int expect, int update) =>
                Util.JavaUnsafe.compareAndSwapInt(a, Util.ElementOffset32(i), expect, update);
        }

    }



    #pragma warning disable 0659
    public class UInt32 : Int32, System.IEquatable<uint>
    {

        new public static UInt32 Box(int v) => new UInt32() { v = v };
        public static UInt32 Box(uint[] a, int i) => new UInt32.InArray(a, i);

        public static void Set(int v, UInt32 o) => o.Set(v);
        public static void VolatileSet(int v, UInt32 o) => o.VolatileSet(v);

        public override bool CompareAndSwap(int expect, int update) =>
            throw new System.NotSupportedException();

        public override bool Equals(object obj)
        {
            var objUInt32 = obj as UInt32;
            return (objUInt32 != null && objUInt32.Get() == Get());
        }

        // System.IEquatable<uint>
        public bool Equals(uint v) => v == Get();

        public override System.TypeCode GetTypeCode() => System.TypeCode.UInt32;

        //public int CompareTo(uint v) => java.lang.Integer.compareUnsigned(Get(), (int) v);

        public int CompareTo(uint v) => CompareTo(Get(), (int) v);

        public static int CompareTo(int a, int b)
            => a == b ? 0 : a + System.Int32.MinValue < b + System.Int32.MinValue ? -1 : 1;

        public static int UnsignedDivision(int dividend, int divisor)
        {
            return (int) (((ulong) (uint) dividend) / ((ulong) (uint) divisor));
        }

        public static int UnsignedRemainder(int dividend, int divisor)
        {
            return (int) (((ulong) (uint) dividend) % ((ulong) (uint) divisor));
        }

        new public static uint OverflowAdd(int a, int b)
        {
            uint c = (uint) (a + b);
            if (c < (uint) a || c < (uint) b)
                system.Math.OverflowException();
            return c;
        }

        new public static uint OverflowSubtract(int a, int b)
        {
            uint c = (uint) (a - b);
            if (c > (uint) a || c > (uint) b)
                system.Math.OverflowException();
            return c;
        }

        new public static uint OverflowMultiply(int a, int b)
        {
            uint c = (uint) (a * b);
            if (c < (uint) a || c < (uint) b)
                system.Math.OverflowException();
            return c;
        }



        //
        // InArray
        //

        private sealed class InArray : UInt32
        {
            [java.attr.RetainType] private uint[] a;
            [java.attr.RetainType] private int i;

            public InArray(uint[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override int Get() => (int) a[i];
            public override int VolatileGet() =>
                Util.JavaUnsafe.getIntVolatile(a, Util.ElementOffset32(i));

            public override void Set(int v) => a[i] = (uint) v;
            public override void VolatileSet(int v) =>
                Util.JavaUnsafe.putIntVolatile(a, Util.ElementOffset32(i), v);
        }

    }

}
