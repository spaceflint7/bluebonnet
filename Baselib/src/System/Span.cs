
namespace system
{

    public struct Span<T>
    {
        [java.attr.RetainType] private Reference array;
        [java.attr.RetainType] private int shift;
        [java.attr.RetainType] private int count;
        [java.attr.RetainType] private int start;

        public static int Shiftof(java.lang.Class cls = null)
        {
            if (cls == null)
                cls = ((system.RuntimeType) typeof(T)).JavaClassForArray();
            if (cls.isPrimitive())
            {
                if (cls == java.lang.Boolean.TYPE || cls == java.lang.Byte.TYPE)
                    return 0;   // size 1, shift of 0
                if (cls == java.lang.Character.TYPE || cls == java.lang.Short.TYPE)
                    return 1;   // size 2, shift of 1
                if (cls == java.lang.Integer.TYPE || cls == java.lang.Float.TYPE)
                    return 2;   // size 3, shift of 2
                if (cls == java.lang.Long.TYPE || cls == java.lang.Double.TYPE)
                    return 3;   // size 4, shift of 3
            }
            return 4;  // dummy size 16 (shift 4) for all non-primitive types
        }

        public object Array(java.lang.Class cls)
        {
            var array = this.array.Get();
            if (array == null)
            {
                if (cls == null)
                    cls = ((system.RuntimeType) typeof(T)).JavaClassForArray();

                int shift = this.shift;
                if (shift != 0)
                    shift--;
                else
                {
                    shift = Shiftof(cls);
                    this.shift = shift + 1;
                }

                count = count >> shift;
                this.array.Set(array = java.lang.reflect.Array.newInstance(cls, count));
                if (! cls.isPrimitive())
                {
                    for (int i = 0; i < count; i++)
                        java.lang.reflect.Array.set(array, i, cls.newInstance());
                }
            }
            return array;
        }

        //
        // Helper methods for instructions
        //

        public static int Sizeof()
        {
            // this helper method is invoked by code which uses the 'sizeof'
            // instruction.  see also CodeSpan::Sizeof method.
            return 1 << Shiftof();
        }

        [java.attr.RetainName]
        public static Span<ValueType> Localloc(long bytes)
        {
            // this helper method is invoked by code which uses the 'localloc'
            // instruction.  see also CodeSpan::Localloc method.
            int intBytes = (int) bytes;
            if (intBytes != bytes)
                throw new System.ArgumentOutOfRangeException();
            return new Span<ValueType>() { array = new Reference(), count = intBytes };
        }

        [java.attr.RetainName]
        public Span<T> Add(long offset, System.Type spanType)
        {
            var shift = this.shift;
            if (shift != 0)
                shift--;
            else
            {
                if (spanType == null)
                    spanType = typeof(T);
                shift = Shiftof(((system.RuntimeType) spanType).JavaClassForArray());
                this.shift = shift + 1;
            }

            var span = this;

            int shifted_offset = ((int) offset) >> shift;
            if ((shifted_offset << shift) != offset)
            {
                // we require offset to be a multiple of shift
                throw new System.InvalidOperationException();
            }
            span.start = span.start + shifted_offset;
            return span;
        }

        public ValueType Box()
        {
            // box a span element that is
            // - returned as a by-reference result (CodeCall::Translate_Return)
            // - assigned to a by-reference variable (CodeLocals::StoreValue)
            return system.Array.Box(Array(null), start);
        }

        //
        // Helper methods for offsets and loops
        //

        [java.attr.RetainName]
        public long Subtract(Span<T> other)
        {
            var array1 = this.array?.Get();
            var array2 = other.array?.Get();
            if (array1 != array2)
                throw new System.ArgumentException();
            var shift = this.shift;
            if (shift != 0)
                shift--;
            return (this.start - other.start) << shift;
        }

        [java.attr.RetainName]
        public static int CompareTo(Span<T> span1, Span<T> span2)
            => (int) span1.Subtract(span2);

        //
        // Assign
        //
        // this helper method is invoked by code which stores an address into
        // a pointer.  see also CodeSpan::Address method.
        //

        [java.attr.RetainName]
        public static Span<char> Assign(java.lang.String str)
            => new Span<char>(str.toCharArray()) { count = System.SByte.MinValue };

        [java.attr.RetainName]
        public static System.ValueType Assign(ValueType source)
            => new Span<T>((T[]) (object) (new ValueType[1] { source }));

        [java.attr.RetainName]
        public static Span<object> Assign(long zero)
        {
            // we expect an assignment of zero at the end of a 'fixed' block
            if (zero != 0L)
                throw new System.ArgumentException();
            return Span<object>.Empty;
        }

        [java.attr.RetainName]
        public static System.ValueType AssignArray(object array, int index)
        {
            int length = java.lang.reflect.Array.getLength(array) - index;
            return new Span<T>((T[]) (object) array, index, length);
        }

        //
        // helper methods to access a span of a primitive type.
        // see also CodeSpan::LoadStore method.
        //

        public bool LoadZ() => ((bool[]) Array(java.lang.Boolean.TYPE))[start];
        public void StoreZ(bool value) => ((bool[]) Array(java.lang.Boolean.TYPE))[start] = value;

        public sbyte LoadB() => ((sbyte[]) Array(java.lang.Byte.TYPE))[start];
        public void StoreB(sbyte value) => ((sbyte[]) Array(java.lang.Byte.TYPE))[start] = value;

        public char LoadC() => ((char[]) Array(java.lang.Character.TYPE))[start];
        public void StoreC(char value)
        {
            // disallow writing into a Span<char> created via Assign(string) method above
            if (count == System.SByte.MinValue)
                throw new System.InvalidOperationException();
            ((char[]) Array(java.lang.Character.TYPE))[start] = value;
        }

        public short LoadS() => ((short[]) Array(java.lang.Short.TYPE))[start];
        public void StoreS(short value) => ((short[]) Array(java.lang.Short.TYPE))[start] = value;

        public int LoadI() => ((int[]) Array(java.lang.Integer.TYPE))[start];
        public void StoreI(int value) => ((int[]) Array(java.lang.Integer.TYPE))[start] = value;

        public long LoadJ() => ((long[]) Array(java.lang.Long.TYPE))[start];
        public void StoreJ(long value) => ((long[]) Array(java.lang.Long.TYPE))[start] = value;

        public float LoadF() => ((float[]) Array(java.lang.Float.TYPE))[start];
        public void StoreF(float value) => ((float[]) Array(java.lang.Float.TYPE))[start] = value;

        public double LoadD() => ((double[]) Array(java.lang.Double.TYPE))[start];
        public void StoreD(double value) => ((double[]) Array(java.lang.Double.TYPE))[start] = value;

        //
        // helper methods to access a span of a value type.
        // see also CodeSpan::LoadStore method.
        //

        public system.ValueType Load(java.lang.Class cls)
            => (system.ValueType) java.lang.reflect.Array.get(Array(cls), start);

        public void Store(ValueType value, java.lang.Class cls)
            => ((ValueMethod) value).CopyTo(Load(cls));

        public void Clear()
        {
            if (this.array != null && this.array.Get() != null)
            {
                ((ValueMethod) Load(null)).Clear();
            }
        }

        //
        // helper methods for system.runtime.compilerservices.VoidHelper
        //

        public Span<T> CloneIntoCache(out object array)
        {
            var span = this;
            array = span.array?.Get();
            span.array = null;
            return span;
        }

        public System.ValueType CloneFromCache(object array)
        {
            var span = this;
            span.array = Reference.Box(array);
            return (System.ValueType) span;
        }

        //
        // System.Span methods
        //

        [java.attr.RetainName]
        public Span(Span<T> fromSpan, int count)
        {
            int shift = Shiftof();
            if (    (! fromSpan.IsEmpty)
                 || fromSpan.start != 0
                 || fromSpan.count != (1 << shift) * count)
            {
                // input span should be result of 'localloc' instruction,
                // as implemented by the Localloc method above, so the
                // span should have a null array a matching byte count
                throw new System.InvalidOperationException();
            }
            this.array = fromSpan.array;
            this.array.Set(new T[count]);
            this.count = count;
            this.shift = shift + 1;
            this.start = 0;
        }

        public Span(T[] array)
        {
            this.array = Reference.Box(array);
            count = (array != null) ? array.Length : 0;
            shift = Shiftof() + 1;
            start = 0;
        }

        public Span(T[] array, int start, int length)
        {
            this.array = Reference.Box(array);
            if (array == null)
            {
                if (start != 0 || length != 0)
                    throw new System.ArgumentOutOfRangeException();
                count = 0;
            }
            else
            {
                if (start < 0 || length < 0 || (start + length > array.Length))
                    throw new System.ArgumentOutOfRangeException();
                count = length - start;
            }
            this.shift = Shiftof() + 1;
            this.start = start;
        }

        public object this[int index] => system.Array.Box(array.Get(), start + index);

        public int Length => count;

        public bool IsEmpty => array == null || array.Get() == null;
        public static Span<T> Empty => new Span<T>();

    }

}
