
namespace system.runtime.compilerservices
{

    public class IdentifierWeakTable
    {

        sealed class TrackedObject : java.lang.@ref.WeakReference
        {
            [java.attr.RetainType] public int id;
            public TrackedObject(int id, object obj, java.lang.@ref.ReferenceQueue refq)
                : base(obj, refq) => this.id = id;
        }



        public int Generate(object trackedObject)
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
                        map.put(key, new TrackedObject(id, trackedObject, refq));
                        return id;
                    }
                }
            }
            finally
            {
                javaLock.@unlock();
            }
        }



        public static int GlobalGenerate(object trackedObject) => GlobalInstance.Generate(trackedObject);



        [java.attr.RetainType] readonly java.util.concurrent.atomic.AtomicInteger nextId =
                        new java.util.concurrent.atomic.AtomicInteger(
                                ((int) java.lang.System.currentTimeMillis()) & int.MaxValue);

        [java.attr.RetainType] readonly java.util.concurrent.locks.ReentrantLock javaLock =
                        new java.util.concurrent.locks.ReentrantLock();

        [java.attr.RetainType] readonly java.util.HashMap map = new java.util.HashMap();

        [java.attr.RetainType] readonly java.lang.@ref.ReferenceQueue refq = new java.lang.@ref.ReferenceQueue();



        [java.attr.RetainType] static IdentifierWeakTable GlobalInstance = new IdentifierWeakTable();

    }

}
