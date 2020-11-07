
using system.runtime.compilerservices;

namespace system.runtime.interopservices
{

    public struct GCHandle
    {

        [java.attr.RetainType] private object objref;
        [java.attr.RetainType] private int handle;
        [java.attr.RetainType] private bool pinned;
        //[java.attr.RetainType] private java.util.concurrent.atomic.AtomicLong pinned;

        public static GCHandle Alloc(object value, System.Runtime.InteropServices.GCHandleType type)
        {
            var h = new GCHandle() { objref = value };
            if (type == System.Runtime.InteropServices.GCHandleType.Pinned)
            {
                h.pinned = true;
                if (value != null)
                {
                    var trackedObject =
                            IdentifierWeakTable.Global.GetTrackedObject(value);

                    h.handle = (trackedObject != null) ? trackedObject.id
                             : IdentifierWeakTable.GlobalGenerate(value);

                    //h.pinned = new java.util.concurrent.atomic.AtomicLong();
                }
            }
            else if (type != System.Runtime.InteropServices.GCHandleType.Normal)
                throw new System.PlatformNotSupportedException();
            return h;
        }

        public long AddrOfPinnedObject()
        {
            if (pinned)
            {
                // shift left 32 to allow offsetting, see also FromIntPtr()
                // and system.runtime.compilerservices.VoidHelper
                return ((long) handle) << 32;
                /*
                if (objref == null)
                    return 0;
                long id = pinned.get();
                if (id == 0)
                {
                    // tracks the object in a global table,
                    // see also system.runtime.compilerservices.VoidHelper
                    id = ((long) IdentifierWeakTable.GlobalGenerate(objref)) << 32;
                    pinned.compareAndSet(0, id);
                    id = pinned.get();
                }
                return id;
                */
            }
            throw new System.InvalidOperationException();
        }

        public static GCHandle FromIntPtr(System.IntPtr value)
        {
            object obj = null;
            long intptr;
            if ((intptr = (long) value) != 0)
            {
                int id = (int) (intptr >> 32);
                /*
                if (intptr != ((long) id) << 32)
                    throw new System.PlatformNotSupportedException("FromIntPtr");
                */
                obj = IdentifierWeakTable.GlobalGetObject(id);
                if (obj == null)
                    throw new System.PlatformNotSupportedException("FromIntPtr");
            }
            return new GCHandle() { objref = obj };
        }

        public void Free()
        {
            /*if (pinned != null)
                pinned.set(0);*/
            objref = null;
            handle = 0;
            pinned = false;
        }

        public object Target
        {
            get => objref;
            set => throw new System.PlatformNotSupportedException();
        }

    }

}
