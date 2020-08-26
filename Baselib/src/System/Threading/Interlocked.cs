
namespace system.threading
{

    public static class Interlocked
    {

        //
        // Int32
        //

        public static int CompareExchange(system.Int32 data, int update, int expect)
        {
            int current = data.VolatileGet();
            data.CompareAndSwap(expect, update);
            return current;
        }

        public static int Exchange(system.Int32 data, int update)
        {
            for (;;)
            {
                int current = data.VolatileGet();
                if (data.CompareAndSwap(current, update))
                    return current;
            }
        }

        public static int Increment(system.Int32 data)
        {
            for (;;)
            {
                int current = data.VolatileGet();
                int next = current + 1;
                if (data.CompareAndSwap(current, next))
                    return next;
            }
        }

        public static int Decrement(system.Int32 data)
        {
            for (;;)
            {
                int current = data.VolatileGet();
                int next = current - 1;
                if (data.CompareAndSwap(current, next))
                    return next;
            }
        }

        public static int Add(system.Int32 data, int v)
        {
            for (;;)
            {
                int current = data.VolatileGet();
                int next = current + v;
                if (data.CompareAndSwap(current, next))
                    return next;
            }
        }


        //
        // Object/Reference
        //

        // public static T CompareExchange<T> (ref T location1, T value, T comparand) where T : class;
        public static object CompareExchange(object data, object update, object expect, System.Type genericType)
        {
            // the 'class' constraint lets us assume the parameters are always boxed references
            var data_ = (system.Reference) data;
            object current = data_.VolatileGet();
            data_.CompareAndSwap(system.GenericType.Load(expect), system.GenericType.Load(update));
            return current;
        }

        public static object CompareExchange(system.Reference data, object update, object expect)
        {
            // the real CompareExchange takes a 'ref object' first parameter,
            // but we take system.Reference so we can access its CompareAndSwap method.
            // this depends on CilMethod::IsInterlockedOrVolatile() -- see there.
            object current = data.VolatileGet();
            data.CompareAndSwap(expect, update);
            return current;
        }

        public static object Exchange(system.Reference data, object update)
        {
            for (;;)
            {
                object current = data.VolatileGet();
                if (data.CompareAndSwap(current, update))
                    return current;
            }
        }

    }

}
