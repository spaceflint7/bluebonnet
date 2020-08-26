
namespace system
{

    interface EnumFlags { }

    [System.Serializable]
    public abstract class Enum : system.ValueType, system.ValueMethod, System.IComparable
                                 //System.IFormattable, System.IConvertible
    {

        public static object Box(int v, java.lang.Class cls)
        {
            var obj = (system.Enum) cls.newInstance();
            Enum.Set(v, obj, cls);
            return obj;
        }

        public static object Box(long v, java.lang.Class cls)
        {
            var obj = cls.newInstance();
            Util.GetEnumField(cls).setLong(obj, v);
            return obj;
        }



        public virtual int Get() =>
            Util.GetEnumField(((java.lang.Object) (object) this).getClass()).getInt(this);

        [System.Runtime.CompilerServices.SpecialName] // discard "Long" in name; see CilMethod
        public virtual long GetLong() =>
            Util.GetEnumField(((java.lang.Object) (object) this).getClass()).getLong(this);

        public virtual int VolatileGet() => throw new System.NotSupportedException();

        [System.Runtime.CompilerServices.SpecialName] // discard "Long" in name; see CilMethod
        public virtual long VolatileGetLong() => throw new System.NotSupportedException();

        public virtual void Set(int v) => Set(v, this);
        public virtual void VolatileSet(int v) => throw new System.NotSupportedException();

        public virtual void Set(long v) => Set(v, this);
        public virtual void VolatileSet(long v) => throw new System.NotSupportedException();

        public static void Set(int v, Int32 o) => o.Set(v);
        public static void VolatileSet(int v, Int32 o) => o.VolatileSet(v);



        public static void Set(int v, system.Enum obj)
            => Set(v, obj, ((java.lang.Object) (object) obj).getClass());

        public static void Set(long v, system.Enum obj)
            => Set(v, obj, ((java.lang.Object) (object) obj).getClass());



        public static void Set(int v, system.Enum obj, java.lang.Class cls)
        {
            var fld = Util.GetEnumField(cls);
            cls = fld.getType();

            if (cls == java.lang.Integer.TYPE)
                fld.setInt(obj, v);
            else if (cls == java.lang.Short.TYPE)
                fld.setShort(obj, (short) v);
            else if (cls == java.lang.Byte.TYPE)
                fld.setByte(obj, (sbyte) v);
            else
                fld.setLong(obj, (long) v);
        }



        public static void Set(long v, system.Enum obj, java.lang.Class cls)
        {
            Util.GetEnumField(cls).setLong(obj, v);
        }



        public int CompareTo(object obj)
        {
            if (obj == null)
                return (this != null) ? 1 : 0;

            var cls = ((java.lang.Object) (object) this).getClass();
            if (cls != ((java.lang.Object) obj).getClass())
                throw new System.ArgumentException();

            var fld = Util.GetEnumField(cls);
            var v1 = fld.getLong(this);
            var v2 = fld.getLong(obj);
            return java.lang.Long.signum(v1 - v2);
        }



        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var cls = ((java.lang.Object) (object) this).getClass();
                if (cls == ((java.lang.Object) obj).getClass())
                {
                    var fld = Util.GetEnumField(cls);
                    var v1 = fld.getLong(this);
                    var v2 = fld.getLong(obj);
                    if (v1 == v2)
                        return true;
                }
            }
            return false;
        }



        public override int GetHashCode()
        {
            var cls = ((java.lang.Object) (object) this).getClass();
            return java.lang.Long.hashCode(Util.GetEnumField(cls).getLong(this));
        }



        public override string ToString()
        {
            var cls = ((java.lang.Object) (object) this).getClass();
            var fld = Util.GetEnumField(cls);
            if (fld != null)
            {
                var v = fld.getLong(this);
                string s;

                var flds = cls.getDeclaredFields();
                int n = flds.Length;

                if (this is EnumFlags)
                    s = Util.EnumToStringMulti(v, flds, n);
                else
                    s = Util.EnumToStringSingle(v, flds, n);

                return s ?? java.lang.Long.toString(v);
            }
            Util.BadEnum(cls);
            return default(string);
        }



        void ValueMethod.Clear() => Set(0);
        void ValueMethod.CopyFrom(ValueType from) => Set(((Enum) from).Get());
        void ValueMethod.CopyInto(ValueType into) => ((Enum) into).Set(Get());
        ValueType ValueMethod.Clone() => (ValueType) Box(Get(), ((java.lang.Object) (object) this).getClass());

    }



    /*
     *
     * Enum Util
     *
     */



    public static partial class Util
    {

        [java.attr.RetainType] private static java.util.concurrent.ConcurrentHashMap enumMap =
            new java.util.concurrent.ConcurrentHashMap();



        internal static void BadEnum(java.lang.Class cls)
        {
            throw new System.ArgumentException("Bad enum " + cls);
        }



        internal static java.lang.reflect.Field GetEnumField(java.lang.Class cls)
        {
            var field = enumMap.get(cls);
            if (field != null)
                return (java.lang.reflect.Field) field;

            foreach (var f in cls.getDeclaredFields())
            {
                if (f.getModifiers() == java.lang.reflect.Modifier.PUBLIC)
                {
                    enumMap.put(cls, f);
                    return f;
                }
            }

            BadEnum(cls);
            return null;
        }



        internal static string EnumToStringSingle(long v, java.lang.reflect.Field[] fields, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var f = fields[i];
                if (f.getModifiers() == (   java.lang.reflect.Modifier.PUBLIC
                                          | java.lang.reflect.Modifier.STATIC))
                {
                    if (f.getLong(null) == v)
                        return f.getName();
                }
            }
            return null;
        }



        internal static string EnumToStringMulti(long v, java.lang.reflect.Field[] fields, int n)
        {
            return "???";
            #if false
            var sb = new java.lang.StringBuilder();
            bool comma = false;

            for (int i = 0; i < n; i++)
            {
                var f = fields[i];
                if (f.getModifiers() == (   java.lang.reflect.Modifier.PUBLIC
                                          | java.lang.reflect.Modifier.STATIC))
                {
                    var fv = f.getLong(null);
                    if ((fv & v) == fv)
                    {
                        v &= ~fv;
                        if (comma)
                            sb.append(", ");
                        sb.append(f.getName());
                        comma = true;
                    }
                }
            }

            return (v == 0) ? sb.ToString() : null;
            #endif
        }

    }

}
