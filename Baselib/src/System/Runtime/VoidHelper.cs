
namespace system.runtime.compilerservices {

    public static class VoidHelper
    {

        // this helper class assists in casting a Span<T> to an IntPtr.
        // this is accomplished by caching a reference to the target object
        // and returning the table key id as an IntPtr, which may then be
        // casted back to a Span<T>.
        //
        // important:  a weak reference is cached, to make it possible to
        // discard 'pointers' for objects that were garbage collected.
        // do not pass a Span/pointer to memory that is not referenced
        // by anything except via the passed Span object!

        class TrackedSpan : IdentifierWeakTable.TrackedObject
        {
            [java.attr.RetainType] public Span<object> span;
            public TrackedSpan(int id, object obj, java.lang.@ref.ReferenceQueue refq)
                : base(id, obj, refq) {}
        }

        public static long SpanToIntPtr(System.ValueType span)
        {
            // CodeSpan::ExplicitCast inserts a call to this method
            // when processing a call to
            //      System.IntPtr System.IntPtr::op_Explicit(System.Void*)
            //
            // note that a void* pointer should always handled as a Span<T>
            // in CodeSpan, so we make sure the input is really a span.

            if (span == null)
                return 0;

            if ((object) span is system.Span<object> span_)
            {
                object array;
                var copy = span_.CloneIntoCache(out array);

                if (array != null)
                {
                    var trackedObject =
                            IdentifierWeakTable.Global.GetTrackedObject(array);

                    int id = (trackedObject != null) ? trackedObject.id
                           : IdentifierWeakTable.Global.Generate(array,
                                (id, obj, @ref) =>
                                    new TrackedSpan(id, obj, @ref) { span = copy });

                    // left-shift the 'address' by 32-bits, so we can detect
                    // if an offset was added to the returned 'pointer'
                    return ((long) id) << 32;
                }
            }

            // this helper class only manages Span<T>
            throw new System.PlatformNotSupportedException("SpanToIntPtr");
        }

        public static System.ValueType IntPtrToSpan(long intptr)
        {
            // as above, when casting the IntPtr back to a Void* pointer,
            // returns the Span that was cached for the particular IntPtr
            // value.  if casting a non-zero IntPtr value that has not
            // been cached earlier, then report an exception.

            if (intptr == 0)
                return null;
            int id = (int) (intptr >> 32);
            if (intptr != ((long) id) << 32)
                throw new System.PlatformNotSupportedException("IntPtrToSpan non-zero offset");

            var trackedObject = IdentifierWeakTable.Global.GetTrackedObject(id);
            if (trackedObject is TrackedSpan trackedSpan)
            {
                var array = trackedSpan.get();
                if (array != null)
                {
                    return trackedSpan.span.CloneFromCache(array);
                }
            }

            throw new System.PlatformNotSupportedException("IntPtrToSpan not in table");
        }
    }

}
