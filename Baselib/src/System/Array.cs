
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace system
{

    [System.Serializable]
    public abstract class Array : System.ICloneable, IList, IStructuralComparable, IStructuralEquatable
    {

        //
        // Array helper proxy used when casting an array to an interface
        // or to System.Array
        //

        [java.attr.RetainType] private object arr;
        [java.attr.RetainType] private int len;
        [java.attr.RetainType] private int rank;

        protected Array(object _arr)
        {
            arr = _arr;
            len = java.lang.reflect.Array.getLength(_arr);
            var s = ((java.lang.Object) arr).getClass().getName();
            for (; rank < s.Length && s[rank] == '['; rank++) ;
        }



        static void ThrowIfNull(object checkObject)
        {
            if (checkObject == null)
                throw new System.ArgumentNullException();
        }



        //
        // ICloneable
        //

        public object Clone() => Clone(arr, len);

        public static object Clone(object arr, int len)
        {
            switch (arr)
            {
                case bool[] boolArray:
                    return java.util.Arrays.copyOf(boolArray, len);
                case sbyte[] byteArray:
                    return java.util.Arrays.copyOf(byteArray, len);
                case char[] charArray:
                    return java.util.Arrays.copyOf(charArray, len);
                case short[] shortArray:
                    return java.util.Arrays.copyOf(shortArray, len);
                case int[] intArray:
                    return java.util.Arrays.copyOf(intArray, len);
                case long[] longArray:
                    return java.util.Arrays.copyOf(longArray, len);
                case float[] floatArray:
                    return java.util.Arrays.copyOf(floatArray, len);
                case double[] doubleArray:
                    return java.util.Arrays.copyOf(doubleArray, len);
            }

            var type = ((java.lang.Object) arr).getClass().getComponentType();
            var copy = java.util.Arrays.copyOf((object[]) arr, len);
            int idx;
            object obj;

            if (type.isArray())
            {
                for (idx = 0; idx < len; idx++)
                {
                    obj = java.lang.reflect.Array.get(copy, idx);
                    if (obj != null)
                    {
                        obj = Clone(obj, java.lang.reflect.Array.getLength(obj));
                        java.lang.reflect.Array.set(copy, idx, obj);
                    }
                }
            }
            else if (system.RuntimeType.IsValueClass(type))
            {
                var copyArray = (ValueType[]) copy;
                for (idx = 0; idx < len; idx++)
                    copyArray[idx] = ((ValueMethod) copyArray[idx]).Clone();
            }

            return copy;
        }



        //
        // ConstrainedCopy, Copy, CopyTo, ICollection.CopyTo, ICollection.Count
        //

        public static void ConstrainedCopy(Array sourceArray, int sourceIndex,
                                           Array destinationArray, int destinationIndex,
                                           int length)
        {
            object copy = null;
            if (destinationArray != null)
                copy = Clone(destinationArray.arr, destinationArray.len);
            try
            {
                sourceArray.CopyTo((System.Array) (object) destinationArray, 0);
                copy = null;
            }
            finally
            {
                if (copy != null)
                    destinationArray.arr = copy;
            }
        }

        public void CopyTo(System.Array array, long index)
        {
            int intIndex = (int) index;
            if (intIndex != index)
                throw new System.ArgumentOutOfRangeException();
            CopyTo(array, intIndex);
        }

        public void CopyTo(System.Array array, int index)
            => Copy(this, 0, (system.Array) (object) array, index, len);

        public static void Copy(Array sourceArray, Array destinationArray, int length)
            => Copy(sourceArray, 0, destinationArray, 0, length);

        public static void Copy(Array sourceArray, Array destinationArray, long length)
            => Copy(sourceArray, 0, destinationArray, 0, length);

        public static void Copy(Array sourceArray, long sourceIndex,
                                Array destinationArray, long destinationIndex,
                                long length)
        {
            int intSourceIndex = (int) sourceIndex;
            int intDestinationIndex = (int) destinationIndex;
            int intLength = (int) length;
            if (intSourceIndex != sourceIndex || intDestinationIndex != destinationIndex)
                throw new System.ArgumentOutOfRangeException("index");
            if (intLength != length)
                throw new System.ArgumentOutOfRangeException("length");
            Copy(sourceArray, intSourceIndex, destinationArray, intDestinationIndex, intLength);
        }

        public static void Copy(Array sourceArray, int sourceIndex,
                                Array destinationArray, int destinationIndex,
                                int length)
        {
            ThrowIfNull(sourceArray);
            ThrowIfNull(destinationArray);
            if (sourceArray.rank != 1 || sourceArray.rank != destinationArray.rank)
                throw new System.RankException("Rank_MultiDimNotSupported");
            if (sourceIndex < 0 || destinationIndex < 0)
                throw new System.ArgumentOutOfRangeException("index");
            if (    sourceIndex + length > sourceArray.len
                 || destinationIndex + length > destinationArray.len)
                throw new System.ArgumentException("length");

            var srcArr = sourceArray.arr;
            var dstArr = destinationArray.arr;

            var srcType = ((java.lang.Object) srcArr).getClass().getComponentType();
            var dstType = ((java.lang.Object) dstArr).getClass().getComponentType();
            if (srcType != dstType)
                throw new System.ArrayTypeMismatchException();

            if (system.RuntimeType.IsValueClass(srcType))
            {
                ValueType srcObj, dstObj;
                if (    object.ReferenceEquals(srcArr, dstArr)
                     && destinationIndex > sourceIndex
                     && destinationIndex < sourceIndex + length)
                {
                    // copy backwards to prevent smearing
                    while (length-- > 0)
                    {
                        srcObj = (ValueType) java.lang.reflect.Array.get(srcArr, sourceIndex + length);
                        dstObj = (ValueType) java.lang.reflect.Array.get(dstArr, destinationIndex + length);
                        ((ValueMethod) ((ValueType) srcObj)).CopyTo((ValueType) dstObj);
                    }
                }
                else
                {
                    for (int idx = 0; idx < length; idx++)
                    {
                        srcObj = (ValueType) java.lang.reflect.Array.get(srcArr, sourceIndex + idx);
                        dstObj = (ValueType) java.lang.reflect.Array.get(dstArr, destinationIndex + idx);
                        ((ValueMethod) ((ValueType) srcObj)).CopyTo((ValueType) dstObj);
                    }
                }
            }
            else
            {
                // for an array of primitives or references, use built-in arraycopy
                java.lang.System.arraycopy(srcArr, sourceIndex, dstArr, destinationIndex, length);
            }
        }

        int ICollection.Count => len;



        //
        // System.Array methods
        //

        public static ReadOnlyCollection<T> AsReadOnly<T>(T[] array)
        {
            ThrowIfNull(array);
            return new ReadOnlyCollection<T>(array);
        }

        public int Rank => rank;



        //
        // GetLength, GetLongLength, GetLowerBound, GetUpperBound
        //

        public int GetLength(int dimension)
        {
            if (dimension < 0 || dimension >= rank)
                throw new System.IndexOutOfRangeException();
            if (dimension == 0)
                return len;
            object sub = arr;
            for (int i = 0; i < dimension; i++)
                sub = java.lang.reflect.Array.get(sub, 0);
            return java.lang.reflect.Array.getLength(sub);
        }

        public long GetLongLength(int dimension) => GetLength(dimension);
        public int GetLowerBound(int dimension) => 0;
        public int GetUpperBound(int dimension) => GetLength(dimension) - 1;

        //
        // GetValue (integer)
        //

        public object GetValue(int index) => Load(arr, index);

        public object GetValue(int index1, int index2)
        {
            if (rank != 2)
                throw new System.ArgumentException();
            var sub = java.lang.reflect.Array.get(arr, index1);
            return Load(sub, index2);
        }

        public object GetValue(int index1, int index2, int index3)
        {
            if (rank != 3)
                throw new System.ArgumentException();
            var sub = java.lang.reflect.Array.get(arr, index1);
            sub = java.lang.reflect.Array.get(sub, index2);
            return Load(sub, index3);
        }

        public object GetValue(params int[] indices)
        {
            ThrowIfNull(indices);
            int n = indices.Length;
            if (rank != n--)
                throw new System.ArgumentException();
            object sub = arr;
            for (int i = 0; i < n; i++)
                sub = java.lang.reflect.Array.get(sub, indices[i]);
            return Load(sub, indices[n]);
        }

        //
        // GetValue (long)
        //

        public object GetValue(long index)
        {
            var intIndex = (int) index;
            if (intIndex != index)
                throw new System.ArgumentOutOfRangeException();
            return GetValue(intIndex);
        }

        public object GetValue(long index1, long index2)
        {
            int intIndex1 = (int) index1;
            int intIndex2 = (int) index2;
            if (intIndex1 != index1 || intIndex2 != index2)
                throw new System.ArgumentOutOfRangeException();
            return GetValue(intIndex1, intIndex2);
        }

        public object GetValue(long index1, long index2, long index3)
        {
            int intIndex1 = (int) index1;
            int intIndex2 = (int) index2;
            int intIndex3 = (int) index3;
            if (intIndex1 != index1 || intIndex2 != index2 || intIndex3 != index3)
                throw new System.ArgumentOutOfRangeException();
            return GetValue(intIndex1, intIndex2, intIndex3);
        }

        public object GetValue(params long[] indices)
        {
            int[] intIndices = null;
            if (indices != null)
            {
                int n = indices.Length;
                intIndices = new int[n];
                for (int i = 0; i < n; i++)
                {
                    long longIndex = indices[i];
                    int intIndex = (int) longIndex;
                    if (intIndex != longIndex)
                        throw new System.ArgumentOutOfRangeException();
                    intIndices[i] = intIndex;
                }
            }
            return GetValue(intIndices);
        }

        //
        // SetValue (integer)
        //

        public void SetValue(object value, int index) => Store(arr, index, value);

        public void SetValue(object value, int index1, int index2)
        {
            if (rank != 2)
                throw new System.ArgumentException();
            var sub = java.lang.reflect.Array.get(arr, index1);
            Store(sub, index2, value);
        }

        public void SetValue(object value, int index1, int index2, int index3)
        {
            if (rank != 3)
                throw new System.ArgumentException();
            var sub = java.lang.reflect.Array.get(arr, index1);
            sub = java.lang.reflect.Array.get(sub, index2);
            Store(sub, index3, value);
        }

        public void SetValue(object value, params int[] indices)
        {
            ThrowIfNull(indices);
            int n = indices.Length;
            if (rank != n--)
                throw new System.ArgumentException();
            object sub = arr;
            for (int i = 0; i < n; i++)
                sub = java.lang.reflect.Array.get(sub, indices[i]);
            Store(sub, indices[n], value);
        }

        public void SetValue(object value, long index)
        {
            var intIndex = (int) index;
            if (intIndex != index)
                throw new System.ArgumentOutOfRangeException();
            SetValue(value, intIndex);
        }

        public void SetValue(object value, long index1, long index2)
        {
            int intIndex1 = (int) index1;
            int intIndex2 = (int) index2;
            if (intIndex1 != index1 || intIndex2 != index2)
                throw new System.ArgumentOutOfRangeException();
            SetValue(value, intIndex1, intIndex2);
        }

        public void SetValue(object value, long index1, long index2, long index3)
        {
            int intIndex1 = (int) index1;
            int intIndex2 = (int) index2;
            int intIndex3 = (int) index3;
            if (intIndex1 != index1 || intIndex2 != index2 || intIndex3 != index3)
                throw new System.ArgumentOutOfRangeException();
            SetValue(value, intIndex1, intIndex2, intIndex3);
        }

        public void SetValue(object value, params long[] indices)
        {
            int[] intIndices = null;
            if (indices != null)
            {
                int n = indices.Length;
                intIndices = new int[n];
                for (int i = 0; i < n; i++)
                {
                    long longIndex = indices[i];
                    int intIndex = (int) longIndex;
                    if (intIndex != longIndex)
                        throw new System.ArgumentOutOfRangeException();
                    intIndices[i] = intIndex;
                }
            }
            SetValue(value, intIndices);
        }

        //
        // IndexOf
        //

        public static int IndexOf(Array array, object value, int startIndex, int count)
        {
            ThrowIfNull(array);
            if (array.rank != 1)
                throw new System.RankException();
            if (startIndex < 0 || startIndex > array.len)
                throw new System.ArgumentOutOfRangeException();
            if (count < 0 || count > array.len - startIndex)
                throw new System.ArgumentOutOfRangeException();

            int endIndex = startIndex + count;

            switch (array.arr)
            {
                case bool[] boolArray:
                    var boolValue = (bool) value;
                    for (; startIndex < endIndex; startIndex++)
                        if (boolArray[startIndex] == boolValue)
                            return startIndex;
                    break;

                case sbyte[] byteArray:
                    var byteValue = (sbyte) value;
                    for (; startIndex < endIndex; startIndex++)
                        if (byteArray[startIndex] == byteValue)
                            return startIndex;
                    break;

                case char[] charArray:
                    var charValue = (char) value;
                    for (; startIndex < endIndex; startIndex++)
                        if (charArray[startIndex] == charValue)
                            return startIndex;
                    break;

                case short[] shortArray:
                    var shortValue = (short) value;
                    for (; startIndex < endIndex; startIndex++)
                        if (shortArray[startIndex] == shortValue)
                            return startIndex;
                    break;

                case int[] intArray:
                    var intValue = (int) value;
                    for (; startIndex < endIndex; startIndex++)
                        if (intArray[startIndex] == intValue)
                            return startIndex;
                    break;

                case long[] longArray:
                    var longValue = (long) value;
                    for (; startIndex < endIndex; startIndex++)
                        if (longArray[startIndex] == longValue)
                            return startIndex;
                    break;

                case float[] floatArray:
                    var floatValue = (float) value;
                    for (; startIndex < endIndex; startIndex++)
                        if (floatArray[startIndex] == floatValue)
                            return startIndex;
                    break;

                case double[] doubleArray:
                    var doubleValue = (double) value;
                    for (; startIndex < endIndex; startIndex++)
                        if (doubleArray[startIndex] == doubleValue)
                            return startIndex;
                    break;

                case object[] objectArray:
                    if (value == null)
                    {
                        for (; startIndex < endIndex; startIndex++)
                            if (objectArray[startIndex] == null)
                                return startIndex;
                    }
                    else
                    {
                        for (; startIndex < endIndex; startIndex++)
                        {
                            var objectAtIndex = objectArray[startIndex];
                            if (objectAtIndex != null && objectAtIndex.Equals(value))
                                return startIndex;
                        }
                    }
                    break;

                default:
                    throw new System.InvalidOperationException();
            }

            return -1;
        }

        public static int IndexOf(Array array, object value, int startIndex)
        {
            ThrowIfNull(array);
            return IndexOf(array, value, startIndex, array.len - startIndex);
        }

        public static int IndexOf(Array array, object value) => IndexOf(array, value, 0);

        public static int IndexOf<T>(T[] array, T value)
            => IndexOf((Array) (object) array, (object) value, 0);

        public static int IndexOf<T>(T[] array, T value, int startIndex)
            => IndexOf((Array) (object) array, (object) value, startIndex);

        public static int IndexOf<T>(T[] array, T value, int startIndex, int count)
            => IndexOf((Array) (object) array, (object) value, startIndex, count);

        //
        // IndexOf
        //

        public static int LastIndexOf(Array array, object value, int startIndex, int count)
        {
            ThrowIfNull(array);
            if (array.rank != 1)
                throw new System.RankException("Rank_MultiDimNotSupported");
            if (startIndex < 0 || startIndex > array.len)
                throw new System.ArgumentOutOfRangeException("startIndex");
            if (count < 0 || count > array.len - startIndex)
                throw new System.ArgumentOutOfRangeException("count");

            int endIndex = startIndex + count;

            switch (array.arr)
            {
                case bool[] boolArray:
                    var boolValue = (bool) value;
                    while (endIndex-- > startIndex)
                        if (boolArray[endIndex] == boolValue)
                            return endIndex;
                    break;

                case sbyte[] byteArray:
                    var byteValue = (sbyte) value;
                    while (endIndex-- > startIndex)
                        if (byteArray[endIndex] == byteValue)
                            return endIndex;
                    break;

                case char[] charArray:
                    var charValue = (char) value;
                    while (endIndex-- > startIndex)
                        if (charArray[endIndex] == charValue)
                            return endIndex;
                    break;

                case short[] shortArray:
                    var shortValue = (short) value;
                    while (endIndex-- > startIndex)
                        if (shortArray[endIndex] == shortValue)
                            return endIndex;
                    break;

                case int[] intArray:
                    var intValue = (int) value;
                    while (endIndex-- > startIndex)
                        if (intArray[endIndex] == intValue)
                            return endIndex;
                    break;

                case long[] longArray:
                    var longValue = (long) value;
                    while (endIndex-- > startIndex)
                        if (longArray[endIndex] == longValue)
                            return endIndex;
                    break;

                case float[] floatArray:
                    var floatValue = (float) value;
                    while (endIndex-- > startIndex)
                        if (floatArray[endIndex] == floatValue)
                            return endIndex;
                    break;

                case double[] doubleArray:
                    var doubleValue = (double) value;
                    while (endIndex-- > startIndex)
                        if (doubleArray[endIndex] == doubleValue)
                            return endIndex;
                    break;

                case object[] objectArray:
                    if (value == null)
                    {
                        while (endIndex-- > startIndex)
                            if (objectArray[endIndex] == null)
                                return endIndex;
                    }
                    else
                    {
                        while (endIndex-- > startIndex)
                        {
                            var objectAtIndex = objectArray[endIndex];
                            if (objectAtIndex != null && objectAtIndex.Equals(value))
                                return endIndex;
                        }
                    }
                    break;

                default:
                    throw new System.InvalidOperationException();
            }

            return -1;
        }

        public static int LastIndexOf(Array array, object value, int startIndex)
        {
            ThrowIfNull(array);
            return LastIndexOf(array, value, startIndex, array.len - startIndex);
        }

        public static int LastIndexOf(Array array, object value) => LastIndexOf(array, value, 0);

        public static int LastIndexOf<T>(T[] array, T value)
            => LastIndexOf((Array) (object) array, (object) value, 0);

        public static int LastIndexOf<T>(T[] array, T value, int startIndex)
            => LastIndexOf((Array) (object) array, (object) value, startIndex);

        public static int LastIndexOf<T>(T[] array, T value, int startIndex, int count)
            => LastIndexOf((Array) (object) array, (object) value, startIndex, count);

        //
        // Sort
        //

        public static void Sort(Array array) => Sort(array, (system.collections.IComparer) null);
        public static void Sort(Array array, int index, int length) => Sort(array, null, index, length, null);
        public static void Sort(Array array, int index, int length, system.collections.IComparer comparer) => Sort(array, null, index, length, comparer);
        public static void Sort(Array array, system.collections.IComparer comparer)
        {
            ThrowIfNull(array);
            Sort(array, null, 0, array.len, comparer);
        }

        public static void Sort(Array keys, Array items) => Sort(keys, items, 0, 0, null);
        public static void Sort(Array keys, Array items, int index, int length) => Sort(keys, items, 0, 0, null);
        public static void Sort(Array keys, Array items, system.collections.IComparer comparer) => Sort(keys, items, 0, 0, null);

        public static void Sort(Array keys, Array items, int index, int length,
                                system.collections.IComparer comparer)
        {
            ThrowIfNull(keys);
            if (! object.ReferenceEquals(keys, items))              // sorting different arrays
                throw new System.PlatformNotSupportedException();   //    not yet supported
            if (keys.rank != 1)
                throw new System.RankException();
            if (index < 0 || length < 0)
                throw new System.ArgumentOutOfRangeException();
            if (keys.len - index < length)
                throw new System.ArgumentException();
            if (length <= 1)
                return;
            int endIndex = index + length;
            if (comparer == null)
            {
                switch (keys.arr)
                {
                    case sbyte[] byteArray:     java.util.Arrays.sort(byteArray, index, endIndex); return;
                    case char[] charArray:      java.util.Arrays.sort(charArray, index, endIndex); return;
                    case short[] shortArray:    java.util.Arrays.sort(shortArray, index, endIndex); return;
                    case int[] intArray:        java.util.Arrays.sort(intArray, index, endIndex); return;
                    case long[] longArray:      java.util.Arrays.sort(longArray, index, endIndex); return;
                    case float[] floatArray:    java.util.Arrays.sort(floatArray, index, endIndex); return;
                    case double[] doubleArray:  java.util.Arrays.sort(doubleArray, index, endIndex); return;
                    case object[] objectArray:  java.util.Arrays.sort(objectArray, index, endIndex); return;
                }
            }
            else
            {
                var objectArray = keys.arr as object[];
                if (objectArray == null)
                {
                    // sorting a primitive array using a comparer, not yet supported
                    throw new System.PlatformNotSupportedException();
                }
                java.util.Arrays.sort(objectArray, index, endIndex, comparer);
            }
        }

        /*public static void Sort<T>(T[] array)
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items)
        public static void Sort<T>(T[] array, int index, int length)
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, int index, int length)
        public static void Sort<T>(T[] array, System.Collections.Generic.IComparer<T> comparer)
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, System.Collections.Generic.IComparer<TKey> comparer)
        public static void Sort<T>(T[] array, int index, int length, System.Collections.Generic.IComparer<T> comparer)
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, int index, int length, System.Collections.Generic.IComparer<TKey> comparer)
        public static void Sort<T>(T[] array, Comparison<T> comparison)*/

        //
        // BinarySearch
        //

        public static int BinarySearch(Array array, object value)
        {
            ThrowIfNull(array);
            return BinarySearch(array, 0, array.len, value, null);
        }

        public static int BinarySearch(Array array, int index, int length, object value)
            => BinarySearch(array, index, length, value, null);

        public static int BinarySearch(Array array, object value,
                                       System.Collections.IComparer comparer)
        {
            ThrowIfNull(array);
            return BinarySearch(array, 0, array.len, value, comparer);
        }

        public static int BinarySearch(Array array, int index, int length, object value,
                                       System.Collections.IComparer comparer)
        {
            ThrowIfNull(array);
            if (index < 0 || length < 0)
                throw new System.ArgumentOutOfRangeException();
            if (array.len - index < length)
                throw new System.ArgumentException();
            if (array.rank != 1)
                throw new System.RankException();

            /*not supported
            if (comparer == null || comparer == System.Collections.Comparer.Default)
            {
            }*/
            throw new System.PlatformNotSupportedException();
        }

        public static int BinarySearch<T>(T[] array, T value) => BinarySearch(array, value);

        public static int BinarySearch<T>(T[] array, T value,
                                          System.Collections.Generic.IComparer<T> comparer)
        {
            throw new System.PlatformNotSupportedException();
        }

        public static int BinarySearch<T>(T[] array, int index, int length, T value)
        {
            throw new System.PlatformNotSupportedException();
        }

        public static int BinarySearch<T>(T[] array, int index, int length, T value,
                                          System.Collections.Generic.IComparer<T> comparer)
        {
            throw new System.PlatformNotSupportedException();
        }

        //
        // Clear
        //

        public static void Clear(Array array, int index, int length)
        {
            ThrowIfNull(array);
            if (index < 0 || length < 0)
                throw new System.ArgumentOutOfRangeException();
            if (array.len - index < length)
                throw new System.ArgumentException();

            switch (array.arr)
            {
                case bool[] boolArray:
                    for (; length-- > 0; index++)
                        boolArray[index] = default(bool);
                    break;

                case sbyte[] byteArray:
                    for (; length-- > 0; index++)
                        byteArray[index] = default(sbyte);
                    break;

                case char[] charArray:
                    for (; length-- > 0; index++)
                        charArray[index] = default(char);
                    break;

                case short[] shortArray:
                    for (; length-- > 0; index++)
                        shortArray[index] = default(short);
                    break;

                case int[] intArray:
                    for (; length-- > 0; index++)
                        intArray[index] = default(int);
                    break;

                case long[] longArray:
                    for (; length-- > 0; index++)
                        longArray[index] = default(long);
                    break;

                case float[] floatArray:
                    for (; length-- > 0; index++)
                        floatArray[index] = default(float);
                    break;

                case double[] doubleArray:
                    for (; length-- > 0; index++)
                        doubleArray[index] = default(double);
                    break;

                case object[] objectArray:
                    if (system.RuntimeType.IsValueClass(
                            ((java.lang.Object) array.arr).getClass().getComponentType()))
                    {
                        for (; length-- > 0; index++)
                            ((ValueMethod) (ValueType) objectArray[index]).Clear();
                    }
                    else
                    {
                        for (; length-- > 0; index++)
                            objectArray[index] = null;
                    }
                    break;

                default:
                    throw new System.InvalidOperationException();
            }
        }

        public void Initialize()
        {
            var type = ((java.lang.Object) arr).getClass().getComponentType();
            if (system.RuntimeType.IsValueClass(type))
            {
                var objectArray = (object[]) arr;
                for (int idx = 0; idx < len; idx++)
                    ((ValueMethod) (ValueType) objectArray[idx]).Clear();
            }
        }

        /*public static bool TrueForAll<T>(T[] array, Predicate<T> match)
        public static void Reverse(Array array)
        public static void Reverse(Array array, int index, int length)
        public static void Resize<T> (ref T[] array, int newSize)
        public static void ForEach<T> (T[] array, Action<T> action);
        public static bool Exists<T> (T[] array, Predicate<T> match);
        public static T[] Empty<T> ();
        public static TOutput[] ConvertAll<TInput,TOutput> (TInput[] array, Converter<TInput,TOutput> converter);
        //LastIndexOf, Find, FindAll, FindIndex, FindLast, CreateInstance*/

        //
        // IList interface
        //

        bool IList.Contains(object value) => IndexOf(this, value, 0, len) >= 0;
        int IList.IndexOf(object value) => IndexOf(this, value);

        void IList.Clear() => Clear(this, 0, len);

        // Array does not support addition and removal
        int IList.Add(object value) => throw new System.NotSupportedException();
        void IList.Insert(int index, object value) => throw new System.NotSupportedException();
        void IList.Remove(object value) => throw new System.NotSupportedException();
        void IList.RemoveAt(int index) => throw new System.NotSupportedException();

        object IList.this[int index] {
            get => Load(arr, index);
            set => SetValue(value, index);
        }

        public bool IsReadOnly => false;
        public bool IsFixedSize => true;
        public bool IsSynchronized => false;
        public object SyncRoot => arr;



        //
        // IStructuralComparable, IStructuralEquatable
        //


        int IStructuralComparable.CompareTo(object other, IComparer comparer)
        {
            if (other == null)
                return 1;
            Array otherArray = other as Array;
            if (otherArray == null || len != otherArray.len)
                throw new System.ArgumentException();
            for (int i = 0; i < len; i++)
            {
                var c = comparer.Compare(Load(arr, i), Load(otherArray.arr, i));
                if (c != 0)
                    return c;
            }
            return 0;
        }

        bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
        {
            if (other == null)
                return false;
            if (object.ReferenceEquals(this, other))
                return true;
            Array otherArray = other as Array;
            if (otherArray == null || len != otherArray.len)
                return false;
            for (int i = 0; i < len; i++)
            {
                if (! comparer.Equals(Load(arr, i), Load(otherArray.arr, i)))
                    return false;
            }
            return true;
        }

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            ThrowIfNull(comparer);
            int hash = 0;
            for (int i = (len >= 8 ? len - 8 : 0); i < len; i++)
            {
                // same calculation as in reference implementation of System.Array
                hash = ((hash << 5) + hash) ^ (comparer.GetHashCode(Load(arr, i)));
            }
            return hash;
        }



        //
        // Helper methods for array initialization and access
        //

        public static object New(int count, System.Type type)
        {
            var runtimeType = (system.RuntimeType) type;
            var cls = runtimeType.JavaClassForArray();
            var array = java.lang.reflect.Array.newInstance(cls, count);
            if (system.RuntimeType.IsValueClass(cls))
            {
                Initialize(array, runtimeType,
                           (system.ValueType) runtimeType.CallConstructor(true),
                           1);
            }
            return array;
        }



        public static void Initialize(object array, System.Type type, system.ValueType model, int dims)
        {
            if (model == null)
                model = (system.ValueType) (((system.RuntimeType) type).CallConstructor(true));

            if (dims == 1)
                InitSingle((object[]) array, model);
            else
                InitMulti((object[]) array, model, dims);

            void InitSingle(object[] array, system.ValueType model)
            {
                int n = array.Length;
                if (n > 0)
                {
                    java.lang.reflect.Array.set(array, 0, model);
                    for (int i = 1; i < n; i++)
                        java.lang.reflect.Array.set(array, i, ((ValueMethod) model).Clone());
                }
            }

            void InitMulti(object[] array, system.ValueType model, int dims)
            {
                int n = array.Length;
                if (dims == 1 && n > 0)
                {
                    for (int i = 0; i < n; i++)
                        java.lang.reflect.Array.set(array, i, ((ValueMethod) model).Clone());
                }
                else if (dims > 1)
                {
                    for (int i = 0; i < n; i++)
                        InitMulti((object[]) array[i], model, dims - 1);
                }
            }
        }

        public static void Store(object array, int index, object value)
        {
            try
            {
                switch (array)
                {
                    case bool[] boolArray:
                        boolArray[index] = ((Boolean) value).Get() != 0 ? true : false;
                        break;
                    case sbyte[] byteArray:
                        byteArray[index] = (sbyte) ((SByte) value).Get();
                        break;
                    case char[] charArray:
                        charArray[index] = (char) ((Char) value).Get();
                        break;
                    case short[] shortArray:
                        shortArray[index] = (short) ((Int16) value).Get();
                        break;
                    case int[] intArray:
                        intArray[index] = (int) ((Int32) value).Get();
                        break;
                    case long[] longArray:
                        longArray[index] = (long) ((Int64) value).Get();
                        break;
                    case float[] floatArray:
                        floatArray[index] = (float) ((Single) value).Get();
                        break;
                    case double[] doubleArray:
                        doubleArray[index] = (double) ((Double) value).Get();
                        break;

                    case ValueType[] valueArray:
                        // this will throw invalid cast if the types do not match.
                        // but note that it will succeed with generic instance types
                        // even when the type arguments do not match.
                        ((ValueMethod) ((ValueType) valueArray[index])).CopyTo((ValueType) value);
                        break;

                    case object[] objectArray:
                        // note that if a value type is being stored, we assume
                        // that it was already boxed, and we don't clone it here
                        if (value is system.Reference valueRef)
                            value = valueRef.Get();
                        objectArray[index] = value;
                        break;
                }
            }
            catch (System.Exception exc)
            {
                throw new System.ArrayTypeMismatchException("", exc);
            }
        }

        public static object Load(object array, int index)
        {
            // if array of a primitive type, returns a boxed copy of array[index].
            // if array of a value type or reference type, returns array[index].

            switch (array)
            {
                case bool[] boolArray:      return Boolean.Box(boolArray[index] ? 1 : 0);
                case sbyte[] byteArray:     return SByte.Box(byteArray[index]);
                case char[] charArray:      return Char.Box(charArray[index]);
                case short[] shortArray:    return Int16.Box(shortArray[index]);
                case int[] intArray:        return Int32.Box(intArray[index]);
                case long[] longArray:      return Int64.Box(longArray[index]);
                case float[] floatArray:    return Single.Box(floatArray[index]);
                case double[] doubleArray:  return Double.Box(doubleArray[index]);
                case object[] objectArray:  return objectArray[index];
            }

            throw new System.IndexOutOfRangeException();
        }

        public static ValueType Box(object array, int index)
        {
            // called from CodeArrays::Address(),
            // array parameter is always a single-dim array
            // returns a boxed reference into an array

            switch (array)
            {
                case bool[] boolArray:          return Boolean.Box(boolArray, index);
                case sbyte[] byteArray:         return SByte.Box(byteArray, index);
                case char[] charArray:          return Char.Box(charArray, index);
                case short[] shortArray:        return Int16.Box(shortArray, index);
                case int[] intArray:            return Int32.Box(intArray, index);
                case long[] longArray:          return Int64.Box(longArray, index);
                case float[] floatArray:        return Single.Box(floatArray, index);
                case double[] doubleArray:      return Double.Box(doubleArray, index);
                case ValueType[] valueArray:    return valueArray[index];
                case object[] objectArray:      return Reference.Box(objectArray, index);
            }

            throw new System.IndexOutOfRangeException();
        }

        //
        // CheckCast
        //

        public static object CheckCast(object array, java.lang.Class castToClass, bool @throw)
        {
            if (array is object[])
            {
                var elemClass = ((java.lang.Object) array).getClass().getComponentType();
                if (system.RuntimeType.IsValueClass(elemClass))
                {
                    if (elemClass == castToClass)
                        return array;
                }
                else if (castToClass.isAssignableFrom(elemClass))
                    return array;
                if (@throw)
                    GenericType.ThrowInvalidCastException(array,
                                    RuntimeType.GetType(castToClass).MakeArrayType());
            }
            return null;
        }

        public static object CheckCast(object array, System.Type castToType, bool @throw)
        {
            if (array is object[])
            {
                var elemClass = ((java.lang.Object) array).getClass().getComponentType();
                var elemType = RuntimeType.GetType(elemClass);
                if (system.RuntimeType.IsValueClass(elemClass))
                {
                    if (object.ReferenceEquals(elemType, castToType))
                        return array;
                }
                else if (castToType.IsAssignableFrom(elemType))
                    return array;
                if (@throw)
                    GenericType.ThrowInvalidCastException(array, castToType.MakeArrayType());
            }
            return null;
        }

        //
        // IEnumerator
        //

        public IEnumerator GetEnumerator() => new Enumerator(arr, len, rank);

        [System.Serializable]
        public class Enumerator : IEnumerator, System.ICloneable
        {
            [java.attr.RetainType] private object arr;
            [java.attr.RetainType] private int len;
            [java.attr.RetainType] private int rank;
            [java.attr.RetainType] private int idx;
            [java.attr.RetainType] private Enumerator sub;

            public Enumerator(object _arr, int _len, int _rank)
            {
                arr = _arr;
                len = _len;
                rank = _rank;
                idx = -1;
            }

            public bool MoveNext()
            {
                for (;;)
                {
                    if (sub != null)
                    {
                        if (sub.MoveNext())
                            return true;
                        sub = null;
                    }

                    int next = idx + 1;
                    if (next >= len)
                        return false;
                    idx = next;

                    if (rank > 1)
                    {
                        var subArr = java.lang.reflect.Array.get(arr, idx);
                        sub = new Enumerator(subArr,
                                             java.lang.reflect.Array.getLength(subArr),
                                             rank - 1);
                        continue;
                    }

                    return true;
                }
            }

            public object Current
            {
                get
                {
                    if (sub != null)
                        return sub.Current;
                    if (idx < 0)
                        throw new System.InvalidOperationException();
                    if (idx >= len)
                        throw new System.InvalidOperationException();
                    return Load(arr, idx);
                }
            }

            public void Reset()
            {
                idx = -1;
                if (sub != null)
                    sub = null;
            }

            public object Clone() => MemberwiseClone();
        }



        //
        // GetProxy
        //

        public static object GetProxy(object obj, System.Type castToType, bool callCast)
        {
            var objClass = ((java.lang.Object) obj).getClass();
            if (objClass.isArray())
            {
                int ok = 0;

                // check if asking to cast the object to System.Array or one of
                // the non-generic interfaces that an array should implement
                if (    object.ReferenceEquals(castToType, cachedArrayType)
                     || object.ReferenceEquals(castToType, cachedICloneable)
                     || object.ReferenceEquals(castToType, cachedPlainIEnumerable)
                     || object.ReferenceEquals(castToType, cachedPlainICollection)
                     || object.ReferenceEquals(castToType, cachedPlainIList)
                     || object.ReferenceEquals(castToType, cachedIStructuralComparable)
                     || object.ReferenceEquals(castToType, cachedIStructuralEquatable))
                {
                    ok = 1; // non-generic
                }

                else if (castToType.IsConstructedGenericType)
                {
                    // otherwise check if asking to cast the object to one of the
                    // generic interfaces that an array should implement.  later on,
                    // the call to IGenericObject.TryCast() verifies the actual
                    // generic parameters types (see the check for ok == 2, below).
                    var genericDef = castToType.GetGenericTypeDefinition();
                    if (    object.ReferenceEquals(genericDef, cachedGenericIEnumerable)
                         || object.ReferenceEquals(genericDef, cachedGenericICollection)
                         || object.ReferenceEquals(genericDef, cachedGenericIList)
                         || object.ReferenceEquals(genericDef, cachedGenericIReadOnlyCollection)
                         || object.ReferenceEquals(genericDef, cachedGenericIReadOnlyList))
                    {
                        ok = 2; // generic
                    }
                }

                if (ok != 0)
                {
                    var proxy = ArrayProxyCache.GetOrAdd(obj, null);
                    if (object.ReferenceEquals(proxy, null))
                    {
                        // create the Array.Proxy<T> object, where T is the array element.
                        // note that we use reflection to call the constructor, which takes
                        // one additional hidden parameter for the generic type.
                        var elementType =
                                system.RuntimeType.GetType(objClass.getComponentType());
                        var newProxyObject =
                                ProxyConstructor.newInstance(new object[] { obj, elementType });
                        proxy = ArrayProxyCache.GetOrAdd(obj, newProxyObject);
                    }
                    if (ok == 2)
                    {
                        // if we are getting a proxy for a generic interface, we must now
                        // make sure the proxy object supports the specific generic type.
                        var genericProxy = ((IGenericObject) proxy).TryCast(castToType);
                        if (callCast)
                            proxy = genericProxy;
                        else if (genericProxy == null)
                            proxy = null;
                    }
                    return proxy;
                }
            }

            else if (objClass == (java.lang.Class) typeof(java.lang.String))
            {
                // string can be cast to system.collections.IEnumerable,
                // system.ICloneable, and, specifically in this implementation,
                // also to system.Array (see CodeCall::ShouldCastArrayArgument
                // and CodeArrays::MaybeGetProxy methods)
                if (    object.ReferenceEquals(castToType, cachedArrayType)
                     || object.ReferenceEquals(castToType, cachedICloneable)
                     || object.ReferenceEquals(castToType, cachedPlainIEnumerable)
                     || object.ReferenceEquals(castToType, cachedGenericIEnumerable))
                {
                    return GetProxy(    ((java.lang.String) obj).toCharArray(),
                                        castToType, callCast);
                }
                if (castToType.IsConstructedGenericType)
                {
                    // castable to system.collections.generic.IEnumerable<char>
                    var genericDef = castToType.GetGenericTypeDefinition();
                    if (object.ReferenceEquals(genericDef, cachedGenericIEnumerable))
                    {
                        return GetProxy(    ((java.lang.String) obj).toCharArray(),
                                            castToType, callCast);
                    }
                }
                return String.CreateWrapper(obj, castToType);
            }

            return null;
        }

        public static Array GetProxy(object obj)
        {
            var proxy = GetProxy(obj, cachedArrayType, false);
            if (proxy == null)
                GenericType.ThrowInvalidCastException(obj, cachedArrayType);

            // because 'proxy' is of type 'object', casting it to directly to
            // System.Array would generate code that calls GetProxy (see also
            // CodeCall.Translate_Return and GenericUtil.ShouldCallGenericCast),
            // we avoid this by casting it first to ProxySyncRoot
            return (Array) (Array.ProxySyncRoot) proxy;
        }

        [java.attr.RetainType] private static readonly System.Type cachedArrayType =
                system.RuntimeType.GetType((java.lang.Class) typeof(System.Array));
        [java.attr.RetainType] private static readonly System.Type cachedICloneable =
                system.RuntimeType.GetType((java.lang.Class) typeof(System.ICloneable));
        [java.attr.RetainType] private static System.Type cachedPlainIEnumerable =
                system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.IEnumerable));
        [java.attr.RetainType] private static System.Type cachedPlainICollection =
                system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.ICollection));
        [java.attr.RetainType] private static System.Type cachedPlainIList =
                system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.IList));
        [java.attr.RetainType] private static System.Type cachedIStructuralComparable =
                system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.IStructuralComparable));
        [java.attr.RetainType] private static System.Type cachedIStructuralEquatable =
                system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.IStructuralEquatable));

        [java.attr.RetainType] private static System.Type cachedGenericIEnumerable =
            system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.Generic.IEnumerable<>));
        [java.attr.RetainType] private static System.Type cachedGenericICollection =
            system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.Generic.ICollection<>));
        [java.attr.RetainType] private static System.Type cachedGenericIList =
                system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.Generic.IList<>));
        [java.attr.RetainType] private static System.Type cachedGenericIReadOnlyCollection =
            system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.Generic.IReadOnlyCollection<>));
        [java.attr.RetainType] private static System.Type cachedGenericIReadOnlyList =
                system.RuntimeType.GetType((java.lang.Class) typeof(System.Collections.Generic.IReadOnlyList<>));

        #pragma warning disable 0436
        [java.attr.RetainType] private static readonly java.lang.reflect.Constructor ProxyConstructor =
                (java.lang.reflect.Constructor) (object)
                    (((java.lang.Class) typeof(Array.Proxy<>)).getDeclaredConstructors())[0];
        #pragma warning restore 0436

        [java.attr.RetainType] private static readonly system.runtime.compilerservices.ConditionalWeakTable ArrayProxyCache
            = new system.runtime.compilerservices.ConditionalWeakTable();


        public interface ProxySyncRoot
        {
            // this interface is used by system.threading.Monitor to identify
            // an array proxy object, and get a reference to the actual array
            object SyncRoot { get; }
        }



        public sealed class Proxy<T> : Array, IList<T>, IReadOnlyList<T>, ProxySyncRoot
        {
            public Proxy(object _arr) : base(_arr) {}

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => new GenericEnumerator(arr, len, rank);

            [System.Serializable]
            public class GenericEnumerator : system.Array.Enumerator, IEnumerator<T>
            {
                public GenericEnumerator(object _arr, int _len, int _rank) : base(_arr, _len, _rank) {}
                T IEnumerator<T>.Current => (T) base.Current;
                void System.IDisposable.Dispose() {}
            }

            bool ICollection<T>.Contains(T value) => IndexOf(this, value, 0, len) >= 0;
            int IList<T>.IndexOf(T value) => IndexOf(this, value);

            void ICollection<T>.CopyTo(T[] array, int index)
            {
                if (rank != 1)
                    throw new System.ArgumentException("Rank_MultiDimNotSupported");
                base.CopyTo(array, index);
            }

            // Array does not support addition and removal
            void ICollection<T>.Add(T value) => throw new System.NotSupportedException("Array");
            void ICollection<T>.Clear() => throw new System.NotSupportedException("Array");
            bool ICollection<T>.Remove (T value) => throw new System.NotSupportedException("Array");
            void IList<T>.Insert(int index, T value) => throw new System.NotSupportedException("Array");
            void IList<T>.RemoveAt(int index) => throw new System.NotSupportedException("Array");

            public int Count => len;            // implements ICollection, IReadOnlyCollection

            public T this[int index] {          // implements IList, IReadOnlyList
                get => (T) Load(arr, index);
                set => SetValue(value, index);
            }
        }



        //
        // static initializer
        //

        static Array()
        {
            // translate exception  java.lang.NegativeArraySizeException
            //                into  System.ArgumentOutOfRangeException

            system.Util.DefineException(
                (java.lang.Class) typeof(java.lang.NegativeArraySizeException),
                (exc) => new System.ArgumentOutOfRangeException(exc.getMessage())
            );

            system.Util.DefineException(
                (java.lang.Class) typeof(java.lang.IndexOutOfBoundsException),
                (exc) => new System.IndexOutOfRangeException(exc.getMessage())
            );

            system.Util.DefineException(
                (java.lang.Class) typeof(java.util.ConcurrentModificationException),
                (exc) => new System.InvalidOperationException(exc.getMessage())
            );

            system.Util.DefineException(
                (java.lang.Class) typeof(java.lang.ArrayStoreException),
                (exc) => new System.ArrayTypeMismatchException(exc.getMessage())
            );

        }
    }
}
