
using JavaUnsafe = system.runtime.interopservices.JavaUnsafe;

namespace system
{

    public class Boolean : system.ValueType, system.ValueMethod, java.lang.Cloneable,
                           System.IComparable, System.IComparable<bool>,
                           System.IConvertible, System.IEquatable<bool>
    {

        [java.attr.RetainType] protected int v;



        public static Boolean Box(int v) => new Boolean() { v = (v != 0) ? 1 : 0 };
        public static Boolean Box(bool[] a, int i) => new Boolean.InArray(a, i);

        public virtual int Get() => v;
        public virtual int VolatileGet()
            => JavaUnsafe.Obj.getIntVolatile(this, ValueOffset);

        public virtual void Set(int v) => this.v = (v != 0) ? 1 : 0;
        public virtual void VolatileSet(int v)
            => JavaUnsafe.Obj.putIntVolatile(this, ValueOffset, (v != 0) ? 1 : 0);

        public static void Set(int v, Boolean o) => o.Set(v);
        public static void VolatileSet(int v, Boolean o) => o.VolatileSet(v);



        public override bool Equals(object obj)
        {
            var objBoolean = obj as Boolean;
            return (objBoolean != null && objBoolean.Get() == Get());
        }

        public override int GetHashCode() => Get() & 1;

        public override string ToString() => Get() != 0 ? "True" : "False";

        public string ToString(System.IFormatProvider provider) => ToString();



        // System.IEquatable<bool>
        public bool Equals(bool v) => (Get() != 0 ? v : (! v));



        // System.IComparable
        public virtual int CompareTo(object obj)
        {
            if (obj is Boolean objBoolean)
                return CompareTo(objBoolean.Get() != 0 ? true : false);
            else if (object.ReferenceEquals(obj, null))
                return 1;
            throw new System.ArgumentException();
        }

        // System.IComparable<bool>
        public int CompareTo(bool b)
        {
            return (Get() == 0) ? (b ? -1 : 0)
                                : (b ? 0  : 1);
        }



        void ValueMethod.Clear() => Set(0);
        void ValueMethod.CopyTo(ValueType into) => ((Boolean) into).Set(Get());
        ValueType ValueMethod.Clone() => Box(Get());



        static long ValueOffset
        {
            get
            {
                if (_ValueOffset == -1)
                {
                    _ValueOffset = JavaUnsafe.FieldOffset(
                                                (java.lang.Class) typeof(Boolean),
                                                java.lang.Integer.TYPE);
                }
                return _ValueOffset;
            }
        }
        [java.attr.RetainType] static long _ValueOffset = -1;



        //
        // CodeNumber.Indirection methods
        //

        public int Get_U8() => (byte) Get();
        public int Get_I8() => Get();
        public void Set_I8(int v) => Set(v);

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

        public System.TypeCode GetTypeCode() => System.TypeCode.Boolean;

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

        private sealed class InArray : Boolean
        {
            [java.attr.RetainType] private bool[] a;
            [java.attr.RetainType] private int i;

            public InArray(bool[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override int Get() => BooleanUtils.BoolToByte(a[i]);
            public override int VolatileGet() => throw new System.NotSupportedException();

            public override void Set(int v) => a[i] = BooleanUtils.ByteToBool(v);
            public override void VolatileSet(int v) => throw new System.NotSupportedException();
        }

    }



    [java.attr.Discard] // discard in output
    public static class BooleanUtils
    {
        public static byte BoolToByte(bool v) => 0;
        public static bool ByteToBool(int v) => false;
    }
}
