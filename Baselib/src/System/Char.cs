
using JavaUnsafe = system.runtime.interopservices.JavaUnsafe;

namespace system
{

    public class Char : system.ValueType, system.ValueMethod, java.lang.Cloneable,
                        System.IComparable, System.IComparable<char>,
                        System.IConvertible, System.IEquatable<char>
    {

        [java.attr.RetainType] protected int v;



        public static Char Box(int v) => new Char() { v = (char) v };
        public static Char Box(char[] a, int i) => new Char.InArray(a, i);

        public virtual int Get() => (char) v;
        public virtual int VolatileGet()
            => (char) JavaUnsafe.Obj.getIntVolatile(this, ValueOffset);

        public virtual void Set(int v) => this.v = (char) v;
        public virtual void VolatileSet(int v)
            => JavaUnsafe.Obj.putIntVolatile(this, ValueOffset, (char) v);

        public static void Set(int v, Char o) => o.Set(v);
        public static void VolatileSet(int v, Char o) => o.VolatileSet(v);



        public override bool Equals(object obj)
        {
            var objChar = obj as Char;
            return (objChar != null && objChar.Get() == Get());
        }

        // System.IEquatable<char>
        public bool Equals(char v) => v == Get();

        public override int GetHashCode()
        {
            int v = Get();
            return v | (v << 16);
        }

        public override string ToString() => java.lang.String.valueOf((char) Get());

        public string ToString(System.IFormatProvider provider) => ToString();

        public static string ToString(char c) => java.lang.String.valueOf(c);



        // System.IComparable
        public virtual int CompareTo(object obj)
        {
            if (obj is Char objChar)
                return CompareTo(objChar.Get());
            else if (object.ReferenceEquals(obj, null))
                return 1;
            throw new System.ArgumentException();
        }

        // System.IComparable<char>
        public int CompareTo(char b) => Get() - ((int) b);



        //public static char ToUpper(char c, system.globalization.CultureInfo cultureInfo)

        public static char ToLowerInvariant(char c) =>
                                (c >= 'A' && c <= 'Z') ? (char)(c + ('a' - 'A')) : c;

        public static char ToUpperInvariant(char c) =>
                                (c >= 'a' && c <= 'z') ? (char)(c - ('a' - 'A')) : c;



        void ValueMethod.Clear() => Set(0);
        void ValueMethod.CopyTo(ValueType into) => ((Char) into).Set(Get());
        ValueType ValueMethod.Clone() => Box(Get());



        static long ValueOffset
        {
            get
            {
                if (_ValueOffset == -1)
                {
                    _ValueOffset = JavaUnsafe.FieldOffset(
                                                (java.lang.Class) typeof(Char),
                                                java.lang.Integer.TYPE);
                }
                return _ValueOffset;
            }
        }
        [java.attr.RetainType] static long _ValueOffset = -1;



        //
        //
        //

        static char StringCharAt(string str, int idx)
        {
            ThrowHelper.ThrowIfNull(str);
            if ((uint) idx >= (uint) str.Length)
                throw new System.ArgumentOutOfRangeException();
            return str[idx];
        }

        public static bool IsWhiteSpace(char c)
        {
            // codes:       CHARACTER TABULATION (U+0009), LINE FEED (U+000A)
            //              LINE TABULATION (U+000B), FORM FEED (U+000C)
            //              CARRIAGE RETURN (U+000D), NEXT LINE (U+0085)
            // categories:  SPACE_SEPARATOR (12), LINE_SEPARATOR(13), PARAGRAPH_SEPARATOR (14)
            if (c == 0x0020 || (c >= 0x0009 && c <= 0x000D) || c == 0x0085)
                return true;
            int cat = java.lang.Character.getType(c);
            return (cat >= 12 && cat <= 14);
        }

        public static bool IsWhiteSpace(string s, int index) => IsWhiteSpace(StringCharAt(s, index));

        public static bool IsDigit(char c) => java.lang.Character.isDigit(c);
        public static bool IsDigit(string s, int index) => java.lang.Character.isDigit(StringCharAt(s, index));

        public static bool IsHighSurrogate(char c) => java.lang.Character.isHighSurrogate(c);
        public static bool IsHighSurrogate(string s, int index) => java.lang.Character.isHighSurrogate(StringCharAt(s, index));

        public static bool IsLowSurrogate(char c) => java.lang.Character.isLowSurrogate(c);
        public static bool IsLowSurrogate(string s, int index) => java.lang.Character.isLowSurrogate(StringCharAt(s, index));



        public static string ConvertFromUtf32(int utf32)
        {
            try
            {
                return new string(java.lang.Character.toChars(utf32));
            }
            catch (java.lang.IllegalArgumentException)
            {
                throw new System.ArgumentOutOfRangeException();
            }
        }

        public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
        {
            if (    java.lang.Character.isHighSurrogate(highSurrogate)
                 && java.lang.Character.isLowSurrogate(lowSurrogate))
            {
                return java.lang.Character.toCodePoint(highSurrogate, lowSurrogate);
            }
            throw new System.ArgumentOutOfRangeException();
        }

        public static int ConvertToUtf32(string s, int index)
        {
            var c = StringCharAt(s, index);
            return   java.lang.Character.isHighSurrogate(c)
                   ? ConvertToUtf32(c, StringCharAt(s, index + 1))
                   : c;
        }



        //
        // CodeNumber.Indirection methods
        //

        public int Get_U8() => (byte) Get();
        public int Get_I8() => Get();
        public void Set_I8(int v) => Set(v);

        public int Get_U16() => Get();
        public int Get_I16() => (int) (short) Get();
        public void Set_I16(int v) => Set((char) v);

        public long Get_I64() => throw new System.NotSupportedException();
        public void Set_I64(long v) => throw new System.NotSupportedException();

        public float Get_F32() => throw new System.NotSupportedException();
        public void Set_F32(float v) => throw new System.NotSupportedException();

        public double Get_F64() => throw new System.NotSupportedException();
        public void Set_F64(double v) => throw new System.NotSupportedException();



        //
        // IConvertible
        //

        public System.TypeCode GetTypeCode() => System.TypeCode.Char;

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

        private sealed class InArray : Char
        {
            [java.attr.RetainType] private char[] a;
            [java.attr.RetainType] private int i;

            public InArray(char[] array, int index)
            {
                if (index < 0 || index >= array.Length)
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override int Get() => a[i];
            public override int VolatileGet() => throw new System.NotSupportedException();

            public override void Set(int v) => a[i] = (char) v;
            public override void VolatileSet(int v) => throw new System.NotSupportedException();
        }

    }

}
