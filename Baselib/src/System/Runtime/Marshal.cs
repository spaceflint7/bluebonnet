
namespace system.runtime.interopservices
{

    public static class Marshal
    {

        public static int SizeOf(System.Type t)
        {
            var j = ((system.RuntimeType) t).JavaClassForArray();
            return   (j == java.lang.Boolean.TYPE   || j == java.lang.Byte.TYPE)   ? 1
                   : (j == java.lang.Character.TYPE || j == java.lang.Short.TYPE)  ? 2
                   : (j == java.lang.Integer.TYPE   || j == java.lang.Float.TYPE)  ? 4
                   : (j == java.lang.Long.TYPE      || j == java.lang.Double.TYPE) ? 8
                   : throw new System.PlatformNotSupportedException();
        }

    }

}
