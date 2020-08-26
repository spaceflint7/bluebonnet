
namespace system.threading
{

    //
    // helper class for the implementation of [System.ThreadStatic] attribute.
    // this attribute is handled at the JIT/IL level to make a static field
    // into a thread local storage field.
    //
    // see also:  ThreadBoxedType class in CilType
    //            ConstructValue method in ValueUtil
    //            GetBoxedFieldType method in ValueUtil
    //            LoadFieldAddress in CodeField
    //            java.lang.ThreadLocal in JDK
    //

    public sealed class ThreadLocal : java.lang.ThreadLocal
    {
        [java.attr.RetainType] private system.ValueType model;

        public ThreadLocal(system.ValueType _model) => model = _model;

        protected override object initialValue() => ((ValueMethod) model).Clone();
    }

}
