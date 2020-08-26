
namespace system.collections
{

    [java.attr.AsInterface]
    public abstract class IComparer : java.lang.Comparable, java.util.Comparator
    {
        [java.attr.RetainName]
        public int compareTo(object obj)
            => ((System.Collections.IComparer) this).Compare(this, obj);

        [java.attr.RetainName]
        public int compare(object obj1, object obj2)
            => ((System.Collections.IComparer) this).Compare(obj1, obj2);

        [java.attr.RetainName]
        public bool equals(object obj)
            => ((System.Collections.IComparer) this).Compare(this, obj) == 0;
    }

}

namespace system.collections.generic
{

    [java.attr.AsInterface]
    public abstract class IComparer<T> : java.lang.Comparable, java.util.Comparator
    {
        [java.attr.RetainName]
        public int compareTo(object obj)
            => ((System.Collections.Generic.IComparer<T>) this).Compare((T) (object) this, (T) obj);

        [java.attr.RetainName]
        public int compare(object obj1, object obj2)
            => ((System.Collections.Generic.IComparer<T>) this).Compare((T) obj1, (T) obj2);

        [java.attr.RetainName]
        public bool equals(object obj)
            => ((System.Collections.Generic.IComparer<T>) this).Compare((T) (object) this, (T) obj) == 0;
    }

}

