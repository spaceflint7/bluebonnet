
namespace system.threading
{

    public static class Volatile
    {

        public static bool Read(system.Boolean refBool)
            => refBool.VolatileGet() != 0 ? true : false;

        public static void Write(system.Boolean refBool, bool value)
            => refBool.VolatileSet(value ? 1 : 0);

        // public static void Write<T> (ref T location, T value) where T : class;
        public static void Write(object data, object value, System.Type genericType)
        {
            // the 'class' constraint lets us assume data is always a boxed reference
            ((system.Reference) data).VolatileSet(value);
        }

        // public static T Read<T> (ref T location) where T : class;
        public static object Read(object data, System.Type genericType)
        {
            // the 'class' constraint lets us assume data is always a boxed reference
            return ((system.Reference) data).VolatileGet();
        }

    }

}

