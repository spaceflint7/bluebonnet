
namespace system.runtime.interopservices
{

    public static class Marshal
    {

        [java.attr.RetainType] private static java.util.concurrent.ConcurrentHashMap MarshalTypes =
            new java.util.concurrent.ConcurrentHashMap();


        public static int SizeOf(System.Type t)
        {
            var j = ((system.RuntimeType) t).JavaClassForArray();
            return   (j == java.lang.Boolean.TYPE   || j == java.lang.Byte.TYPE)   ? 1
                   : (j == java.lang.Character.TYPE || j == java.lang.Short.TYPE)  ? 2
                   : (j == java.lang.Integer.TYPE   || j == java.lang.Float.TYPE)  ? 4
                   : (j == java.lang.Long.TYPE      || j == java.lang.Double.TYPE) ? 8
                   : MarshalTypes.get(t) is int sz                                 ? sz
                   : throw new System.PlatformNotSupportedException();
        }


        public static bool SetComObjectData(object obj, object key, object data)
        {
            // this method is used to record extra types that Marshal.SizeOf
            // should recognize, and the size returned for each such type

            #pragma warning disable 0252
            if (obj == typeof(Marshal) && key is System.Type && data is int)
            {
                MarshalTypes.put(key, data);
                return true;
            }
            throw new System.PlatformNotSupportedException();
            //return false;
        }

    }

}
