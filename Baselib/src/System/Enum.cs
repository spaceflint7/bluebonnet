
using System.Globalization;

namespace system
{

    interface EnumFlags { }

    [System.Serializable]
    public abstract class Enum : system.ValueType, system.ValueMethod, System.IComparable,
                                 System.IFormattable, System.IConvertible
    {

        // warning, do not add fields in this class.
        // doing so will interfere with enumeration of constants.

        //
        // getters and setters
        //

        [System.Runtime.CompilerServices.SpecialName] // discard "Long" in name; see CilMethod
        public abstract long GetLong();

        [System.Runtime.CompilerServices.SpecialName] // discard "Long" in name; see CilMethod
        public abstract void SetLong(long v);

        public virtual int Get() => (int) GetLong();
        public virtual void Set(int v) => SetLong((long) v);
        public static void Set(int v, Enum e) => e.SetLong((long) v);
        public static void Set(long v, Enum e) => e.SetLong(v);

        public virtual int VolatileGet() => throw new System.NotSupportedException();
        public virtual void VolatileSet(int v) => throw new System.NotSupportedException();
        public virtual void VolatileSet(long v) => throw new System.NotSupportedException();
        public static void VolatileSet(int v, Enum e) => throw new System.NotSupportedException();
        public static void VolatileSet(long v, Enum e) => throw new System.NotSupportedException();


        public static object Box(long v, java.lang.Class cls)
        {
            var constructor = cls.getConstructor(null);
            constructor.setAccessible(true);
            var obj = (system.Enum) constructor.newInstance(null);
            obj.SetLong(v);
            return obj;
        }

        public static object Box(int v, java.lang.Class cls) => Box((long) v, cls);

        public static object Box(long v, System.Type type)
            => Box(v, ((system.RuntimeType) type).JavaClassForArray());


        public int CompareTo(object other)
        {
            if (other == null)
                return (this != null) ? 1 : 0;

            var cls = ((java.lang.Object) (object) this).getClass();
            if (cls != ((java.lang.Object) other).getClass())
                throw new System.ArgumentException();

            return java.lang.Long.signum(
                                GetLong() - ((Enum) other).GetLong());
        }



        public override bool Equals(object other)
        {
            if (other != null)
            {
                var cls = ((java.lang.Object) (object) this).getClass();
                if (cls == ((java.lang.Object) other).getClass())
                {
                    if (GetLong() == ((Enum) other).GetLong())
                        return true;
                }
            }
            return false;
        }



        public override int GetHashCode() => java.lang.Long.hashCode(GetLong());



        //
        // ToObject
        //

        public static object ToObject(System.Type enumType, long value)
        {
            ThrowHelper.ThrowIfNull(enumType);
            if (! enumType.IsEnum)
                throw new System.ArgumentException();
            var enumRuntimeType = enumType as RuntimeType;
            if (enumRuntimeType == null)
                throw new System.ArgumentException();
            return Box(value, enumRuntimeType.JavaClassForArray());
        }

        public static object ToObject(System.Type enumType, ulong value)
            => ToObject(enumType, (long) value);

        public static object ToObject(System.Type enumType, uint value)
            => ToObject(enumType, (long) value);

        public static object ToObject(System.Type enumType, int value)
            => ToObject(enumType, (long) value);

        public static object ToObject(System.Type enumType, ushort value)
            => ToObject(enumType, (long) value);

        public static object ToObject(System.Type enumType, short value)
            => ToObject(enumType, (long) value);

        public static object ToObject(System.Type enumType, char value)
            => ToObject(enumType, (long) value);

        public static object ToObject(System.Type enumType, byte value)
            => ToObject(enumType, (long) value);

        public static object ToObject(System.Type enumType, sbyte value)
            => ToObject(enumType, (long) value);

        public static object ToObject(System.Type enumType, bool value)
            => ToObject(enumType, value ? 1L : 0L);



        //
        // HasFlag
        //

        public bool HasFlag(Enum flag)
        {
            ThrowHelper.ThrowIfNull(flag);
            if (! this.GetType().IsEquivalentTo(flag.GetType()))
                throw new System.ArgumentException();
            var v = flag.GetLong();
            return (GetLong() & v) == v;
        }



        //
        // implemented via System.Type:  GetUnderlyingType, GetTypeCode,
        // IsDefined, GetName, GetNames, GetValues
        //

        public static System.Type GetUnderlyingType(System.Type enumType)
        {
            ThrowHelper.ThrowIfNull(enumType);
            return enumType.GetEnumUnderlyingType();
        }

        public System.TypeCode GetTypeCode()
        {
            var typeCode = System.Type.GetTypeCode(GetType().GetEnumUnderlyingType());
            if ((int) typeCode >= 3 && (int) typeCode <= 12) // boolean .. uint64
                return typeCode;
            throw new System.InvalidOperationException();
        }

        public static bool IsDefined(System.Type enumType, object value)
        {
            ThrowHelper.ThrowIfNull(enumType);
            return enumType.IsEnumDefined(value);
        }

        public static string GetName(System.Type enumType, object value)
        {
            ThrowHelper.ThrowIfNull(enumType);
            return enumType.GetEnumName(value);
        }

        public static string[] GetNames(System.Type enumType)
        {
            ThrowHelper.ThrowIfNull(enumType);
            return enumType.GetEnumNames();
        }

        public static System.Array GetValues(System.Type enumType)
        {
            ThrowHelper.ThrowIfNull(enumType);
            return enumType.GetEnumValues();
        }



        //
        // ToString and Format
        //

        public override string ToString() => ToString((string) null);

        public string ToString(System.IFormatProvider provider)
            => ToString((string) null);

        public string ToString(string format, System.IFormatProvider formatProvider)
            => ToString(format);

        public string ToString(string format)
        {
            if (format == null || format.Length == 0)
                format = "G";
            return Format(format, GetLong(), ((java.lang.Object) (object) this).getClass());
        }

        public static string Format(System.Type enumType, object value, string format)
        {
            ThrowHelper.ThrowIfNull(enumType);
            if (! enumType.IsEnum)
                throw new System.ArgumentException();
            ThrowHelper.ThrowIfNull(value, format);

            if (enumType is RuntimeType enumRuntimeType)
            {
                var enumUnderlyingType = GetUnderlyingType(enumType);
                var enumCls = enumRuntimeType.JavaClassForArray();
                var valueType = value.GetType();

                if (value is Enum valueEnum)
                {
                    var valueUnderlyingType = GetUnderlyingType(valueType);
                    if (object.ReferenceEquals(enumUnderlyingType, valueUnderlyingType))
                        return Format(format, valueEnum.GetLong(), enumCls);
                }
                else if (object.ReferenceEquals(enumUnderlyingType, valueType))
                {
                    switch (value)
                    {
                        case byte  byteValue:   return Format(format, (long) byteValue,  enumCls);
                        case char  charValue:   return Format(format, (long) charValue,  enumCls);
                        case short shortValue:  return Format(format, (long) shortValue, enumCls);
                        case int   intValue:    return Format(format, (long) intValue,   enumCls);
                        case long  longValue:   return Format(format,        longValue,  enumCls);
                    }
                }
            }
            throw new System.ArgumentException();
        }

        private static string Format(string format, long value, java.lang.Class cls)
        {
            if (format.Length == 1)
            {
                switch (Char.ToUpperInvariant(format[0]))
                {
                    case 'F': return FormatNames(cls, value, true);
                    case 'G': return FormatNames(cls, value,
                                                 ((java.lang.Class) typeof (EnumFlags))
                                                         .isAssignableFrom(cls));
                    case 'D': return java.lang.Long.toString(value);
                    case 'X': return FormatHex(cls, value);
                };
            }
            throw new System.FormatException();



            static string FormatNames(java.lang.Class cls, long v, bool asFlags)
            {
                var enumLiteralFlags = (   java.lang.reflect.Modifier.PUBLIC
                                         | java.lang.reflect.Modifier.STATIC
                                         | java.lang.reflect.Modifier.FINAL);

                var fields = cls.getDeclaredFields();
                int n = fields.Length;

                var s = asFlags
                      ? ToStringMulti(v, fields, n, enumLiteralFlags)
                      : ToStringSingle(v, fields, n, enumLiteralFlags);
                return s ?? java.lang.Long.toString(v);
            }

            static string ToStringSingle(long v, java.lang.reflect.Field[] fields, int n,
                                         int enumLiteralFlags)
            {
                for (int i = 0; i < n; i++)
                {
                    var f = fields[i];
                    if (f.getModifiers() == enumLiteralFlags)
                    {
                        f.setAccessible(true);
                        if (f.getLong(null) == v)
                            return f.getName();
                    }
                }
                return null;
            }

            static string ToStringMulti(long v, java.lang.reflect.Field[] fields, int n,
                                        int enumLiteralFlags)
            {
                var v0 = v;
                var sb = new java.lang.StringBuilder();
                bool comma = false;

                for (int i = 0; i < n; i++)
                {
                    var f = fields[i];
                    if (f.getModifiers() == enumLiteralFlags)
                    {
                        f.setAccessible(true);
                        var fv = f.getLong(null);

                        if ((fv & v) == fv)
                        {
                            if (fv == 0 && v0 != 0)
                            {
                                // skip field for zero, if value was not zero
                                continue;
                            }
                            v &= ~fv;
                            if (comma)
                                sb.append(", ");
                            sb.append(f.getName());
                            comma = true;
                        }
                    }
                }

                return (v == 0) ? sb.ToString() : null;
            }

            static string FormatHex(java.lang.Class cls, long v)
            {
                var typeCode = System.Type.GetTypeCode(
                                system.RuntimeType.GetType(cls).GetEnumUnderlyingType());
                var hexfmt = typeCode switch
                {
                    System.TypeCode.Boolean => "%02X",
                    System.TypeCode.Char    => "%04X",
                    System.TypeCode.SByte   => "%02X",
                    System.TypeCode.Byte    => "%02X",
                    System.TypeCode.Int16   => "%04X",
                    System.TypeCode.UInt16  => "%04X",
                    System.TypeCode.Int32   => "%08X",
                    System.TypeCode.UInt32  => "%08X",
                    System.TypeCode.Int64   => "%016X",
                    System.TypeCode.UInt64  => "%016X",
                    _                       => throw new System.InvalidOperationException()
                };
                return java.lang.String.format(
                                hexfmt, new object[] { java.lang.Long.valueOf(v) });
            }
        }



        //
        // IConvertible
        //

        bool System.IConvertible.ToBoolean(System.IFormatProvider provider)
            => System.Convert.ToBoolean(GetLong(), CultureInfo.CurrentCulture);

        char System.IConvertible.ToChar(System.IFormatProvider provider)
            => System.Convert.ToChar(GetLong(), CultureInfo.CurrentCulture);

        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider)
            => System.Convert.ToSByte(GetLong(), CultureInfo.CurrentCulture);

        byte System.IConvertible.ToByte(System.IFormatProvider provider)
            => System.Convert.ToByte(GetLong(), CultureInfo.CurrentCulture);

        short System.IConvertible.ToInt16(System.IFormatProvider provider)
            => System.Convert.ToInt16(GetLong(), CultureInfo.CurrentCulture);

        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider)
            => System.Convert.ToUInt16(GetLong(), CultureInfo.CurrentCulture);

        int System.IConvertible.ToInt32(System.IFormatProvider provider)
            => System.Convert.ToInt32(GetLong(), CultureInfo.CurrentCulture);

        uint System.IConvertible.ToUInt32(System.IFormatProvider provider)
            => System.Convert.ToUInt32(GetLong(), CultureInfo.CurrentCulture);

        long System.IConvertible.ToInt64(System.IFormatProvider provider)
            => System.Convert.ToInt64(GetLong(), CultureInfo.CurrentCulture);

        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider)
            => System.Convert.ToUInt64(GetLong(), CultureInfo.CurrentCulture);

        float System.IConvertible.ToSingle(System.IFormatProvider provider)
            => System.Convert.ToSingle(GetLong(), CultureInfo.CurrentCulture);

        double System.IConvertible.ToDouble(System.IFormatProvider provider)
            => System.Convert.ToDouble(GetLong(), CultureInfo.CurrentCulture);

        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider)
            => System.Convert.ToDecimal(GetLong(), CultureInfo.CurrentCulture);

        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider)
            => throw new System.InvalidCastException();

        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider)
            => system.Convert.DefaultToType((System.IConvertible) this, type, provider);

        //
        // value methods
        //

        void ValueMethod.Clear() => SetLong(0L);
        void ValueMethod.CopyTo(ValueType into) => ((Enum) into).SetLong(GetLong());
        ValueType ValueMethod.Clone() => (ValueType) this.MemberwiseClone();

    }

}
