
namespace system.runtime.compilerservices
{

    public class IdentifierWeakTable
    {

        public class TrackedObject : java.lang.@ref.WeakReference
        {
            [java.attr.RetainType] public int id;
            public TrackedObject(int id, object obj, java.lang.@ref.ReferenceQueue refq)
                : base(obj, refq) => this.id = id;
        }



        public delegate TrackedObject TrackedObjectMaker(
                            int id, object objref, java.lang.@ref.ReferenceQueue refq);



        public int Generate(object target, TrackedObjectMaker maker)
        {
            javaLock.@lock();
            try
            {
                for (;;)
                {
                    var weakref = (TrackedObject) refq.poll();
                    if (weakref == null)
                        break;
                    map.remove(java.lang.Integer.valueOf(weakref.id));
                }
                for (;;)
                {
                    var id = nextId.getAndIncrement();
                    if (id <= 0)
                    {
                        nextId.set(1);
                        continue;
                    }
                    var key = java.lang.Integer.valueOf(id);
                    if (! map.containsKey(key))
                    {
                        map.put(key, maker(id, target, refq));
                        return id;
                    }
                }
            }
            finally
            {
                javaLock.@unlock();
            }
        }



        public TrackedObject GetTrackedObject(int id)
        {
            TrackedObject obj = null;
            javaLock.@lock();
            try
            {
                obj = (TrackedObject) map.get(java.lang.Integer.valueOf(id));
            }
            finally
            {
                javaLock.@unlock();
            }
            return obj;
        }



        public TrackedObject GetTrackedObject(object target)
        {
            TrackedObject obj = null;
            javaLock.@lock();
            try
            {
                var it = map.values().iterator();
                while (it.hasNext())
                {
                    var trackedObject = (TrackedObject) it.next();
                    if (object.ReferenceEquals(trackedObject.get(), target))
                    {
                        obj = trackedObject;
                        break;
                    }
                }
            }
            finally
            {
                javaLock.@unlock();
            }
            return obj;
        }



        //
        // helpers for the common case
        //

        [java.attr.RetainType] public readonly static IdentifierWeakTable Global =
                                                                new IdentifierWeakTable();

        public static int GlobalGenerate(object trackedObject)
            => Global.Generate(trackedObject,
                    (id, objref, refq) => new TrackedObject(id, trackedObject, refq));

        public static object GlobalGetObject(int id) => Global.GetTrackedObject(id)?.get();



        //
        // data
        //

        [java.attr.RetainType] readonly java.util.concurrent.atomic.AtomicInteger nextId =
                        new java.util.concurrent.atomic.AtomicInteger(
                                ((int) java.lang.System.currentTimeMillis()) & int.MaxValue);

        [java.attr.RetainType] readonly java.util.concurrent.locks.ReentrantLock javaLock =
                        new java.util.concurrent.locks.ReentrantLock();

        [java.attr.RetainType] readonly java.util.HashMap map = new java.util.HashMap();

        [java.attr.RetainType] readonly java.lang.@ref.ReferenceQueue refq = new java.lang.@ref.ReferenceQueue();

    }

}
