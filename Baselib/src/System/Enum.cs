
namespace system
{

    interface EnumFlags { }

    [System.Serializable]
    public abstract class Enum : system.ValueType, system.ValueMethod, System.IComparable,
                                 System.IFormattable //System.IConvertible
    {

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
        // GetUnderlyingType
        //

        public static System.Type GetUnderlyingType(System.Type enumType)
        {
            ThrowHelper.ThrowIfNull(enumType);
            return enumType.GetEnumUnderlyingType();
            #if false
            if (enumType.IsEnum && enumType is RuntimeType enumRuntimeType)
            {
                var fields = enumRuntimeType.JavaClassForArray().getDeclaredFields();
                if (fields.Length > 1)
                {
                    var f = fields[0];
                    if ((f.getModifiers() & java.lang.reflect.Modifier.STATIC) == 0)
                    {
                        var fldType = system.RuntimeType.GetType(f.getType());
                        var typeCode = (int) System.Type.GetTypeCode(fldType);
                        if (typeCode >= 4 && typeCode <= 12)
                            return fldType;
                    }
                }
            }
            throw new System.ArgumentException();
            #endif
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
                bool asFlags = false;
                switch (format[0])
                {
                    case 'f': case 'F':
                        asFlags = true;
                        goto case 'G';

                    case 'g': case 'G':
                        if (! asFlags)
                        {
                            asFlags = ((java.lang.Class) typeof (EnumFlags))
                                            .isAssignableFrom(cls);
                        }
                        return FormatNames(cls, asFlags, value);

                    case 'd': case 'D':
                        return java.lang.Long.toString(value);

                    case 'x': case 'X':
                        return java.lang.Long.toHexString(value);
                }
            }
            throw new System.FormatException();



            static string FormatNames(java.lang.Class cls, bool asFlags, long v)
            {
                var fields = cls.getDeclaredFields();
                int n = fields.Length;

                var s = asFlags
                      ? ToStringMulti(v, fields, n)
                      : ToStringSingle(v, fields, n);
                return s ?? java.lang.Long.toString(v);
            }

            static string ToStringSingle(long v, java.lang.reflect.Field[] fields, int n)
            {
                for (int i = 0; i < n; i++)
                {
                    var f = fields[i];
                    if (f.getModifiers() == (   java.lang.reflect.Modifier.PUBLIC
                                              | java.lang.reflect.Modifier.STATIC))
                    {
                        f.setAccessible(true);
                        if (f.getLong(null) == v)
                            return f.getName();
                    }
                }
                return null;
            }

            static string ToStringMulti(long v, java.lang.reflect.Field[] fields, int n)
            {
                var v0 = v;
                var sb = new java.lang.StringBuilder();
                bool comma = false;

                for (int i = 0; i < n; i++)
                {
                    var f = fields[i];
                    if (f.getModifiers() == (   java.lang.reflect.Modifier.PUBLIC
                                              | java.lang.reflect.Modifier.STATIC))
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
        }



        void ValueMethod.Clear() => SetLong(0L);
        void ValueMethod.CopyTo(ValueType into) => ((Enum) into).SetLong(GetLong());
        ValueType ValueMethod.Clone() => (ValueType) this.MemberwiseClone();

    }

}
