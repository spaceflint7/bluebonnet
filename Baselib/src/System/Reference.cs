
using JavaUnsafe = system.runtime.interopservices.JavaUnsafe;

namespace system
{

    public class Reference : system.ValueType, system.ValueMethod
    {

        [java.attr.RetainType] protected object v;



        public static Reference Box(object v) => new Reference() { v = v };
        public static Reference Box(object a, int i) => new Reference.InArray(a, i);

        public virtual object Get() => v;
        public virtual object VolatileGet() =>
            JavaUnsafe.Obj.getObjectVolatile(this, ValueOffset);

        public virtual void Set(object v) => this.v = v;
        public virtual void VolatileSet(object v) =>
            JavaUnsafe.Obj.putObjectVolatile(this, ValueOffset, v);

        public static void Set(object v, Reference o) => o.Set(v);
        public static void VolatileSet(object v, Reference o) => o.VolatileSet(v);

        public virtual bool CompareAndSwap(object expect, object update) =>
            JavaUnsafe.Obj.compareAndSwapObject(this, ValueOffset, expect, update);



        public override bool Equals(object obj)
        {
            var objReference = obj as Reference;
            return (objReference != null && objReference.Get() == Get());
        }

        public override int GetHashCode() => Get().GetHashCode();

        public override string ToString()
        {
            object v = Get();
            if (v != null)
                v = v.ToString();
            return (string) v;
        }



        void ValueMethod.Clear() => Set(null);
        void ValueMethod.CopyTo(ValueType into) => ((Reference) into).Set(Get());
        ValueType ValueMethod.Clone() => Box(Get());



        static long ValueOffset
        {
            get
            {
                if (_ValueOffset == -1)
                {
                    _ValueOffset = JavaUnsafe.FieldOffset(
                                                (java.lang.Class) typeof(Reference),
                                                (java.lang.Class) typeof(java.lang.Object));
                }
                return _ValueOffset;
            }
        }
        [java.attr.RetainType] static long _ValueOffset = -1;



        //
        // InArray
        //

        private sealed class InArray : Reference
        {
            [java.attr.RetainType] private object a;
            [java.attr.RetainType] private int i;

            public InArray(object array, int index)
            {
                if (index < 0 || index >= java.lang.reflect.Array.getLength(array))
                    throw new System.IndexOutOfRangeException();
                a = array;
                i = index;
            }

            public override object Get() => java.lang.reflect.Array.get(a, i);
            public override object VolatileGet()
                => JavaUnsafe.Obj.getObjectVolatile(a, JavaUnsafe.ElementOffsetObj(i));

            public override void Set(object v) => java.lang.reflect.Array.set(a, i, v);
            public override void VolatileSet(object v)
                => JavaUnsafe.Obj.putObjectVolatile(a, JavaUnsafe.ElementOffsetObj(i), v);

            public override bool CompareAndSwap(object expect, object update)
                => JavaUnsafe.Obj.compareAndSwapObject(a, JavaUnsafe.ElementOffsetObj(i), expect, update);
        }

    }

}
