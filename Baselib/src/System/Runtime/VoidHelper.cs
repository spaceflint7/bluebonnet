
namespace system.runtime.compilerservices {

    public static class VoidHelper
    {

        public static long SpanToIntPtr(system.ValueType span)
        {
            // CodeSpan::ExplicitCast inserts a call to this method
            // when processing a call to
            //      System.IntPtr System.IntPtr::op_Explicit(System.Void*)
            //
            // note that a void* pointer should always handled as a Span<T>
            // in CodeSpan, so we make sure the input is really a span.

            if (span == null)
                return 0;
            if (SpanClass != ((java.lang.Object) (object) span).getClass())
                throw new System.PlatformNotSupportedException("SpanToIntPtr");
            return (long) IdentifierWeakTable.GlobalGenerate(span);
        }

        public static system.ValueType IntPtrToSpan(long intptr)
        {
            // as above, when casting the IntPtr back to a Void* pointer,
            // returns the Span that was cached for the particular IntPtr
            // value.  if casting a non-zero IntPtr value that has not
            // been cached earlier, then report an exception.

            if (intptr == 0)
                return null;
            var span = IdentifierWeakTable.GlobalGetObject((int) intptr);
            if (span == null)
                throw new System.PlatformNotSupportedException("IntPtrToSpan");
            return (system.ValueType) span;
        }

        static ConditionalWeakTable PointerTable = new ConditionalWeakTable();

        static java.lang.Class SpanClass =
                    ((system.RuntimeType) typeof(Span<object>)).JavaClassForArray();
    }

}
