
namespace system
{

    public static class ThrowHelper
    {

        public static void ThrowIfNull(object o)
        {
            if (object.ReferenceEquals(o, null))
                throw new System.ArgumentNullException();
        }

        public static void ThrowIfNull(object o1, object o2)
        {
            if (object.ReferenceEquals(o1, null) || object.ReferenceEquals(o2, null))
                throw new System.ArgumentNullException();
        }

        public static void ThrowKeyNotFoundException()
            => throw new System.Collections.Generic.KeyNotFoundException();
    }

}
