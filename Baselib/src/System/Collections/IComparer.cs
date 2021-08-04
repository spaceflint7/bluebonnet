
namespace system.collections
{

    [java.attr.AsInterface]
    public abstract class IComparer : java.util.Comparator
    {
        public abstract int Compare(object x, object y);

        [java.attr.RetainName]
        public int compare(object obj1, object obj2)
            => this.Compare(obj1, obj2);

        [java.attr.RetainName]
        public bool equals(object otherComparer)
            => system.Object.Equals(this, otherComparer);
    }



    public class GenericComparerProxy<T> : java.util.Comparator
    {
        System.Collections.Generic.IComparer<T> comparer;

        public GenericComparerProxy(System.Collections.Generic.IComparer<T> _comparer)
            => comparer = _comparer;

        [java.attr.RetainName]
        public int compare(object obj1, object obj2)
            => comparer.Compare((T) obj1, (T) obj2);

        [java.attr.RetainName]
        public bool equals(object otherComparer)
            => system.Object.Equals(this, otherComparer);
    }

}
/*
removing custom IComparer<T>.  the idea was to be able to send an
object implementing this interface to a Java method that takes a
java.util.Comparator.  this works for the non-generic IComparer,
but will not work for the generic counterpart.  instead we have
a bridge (GenericComparerProxy above) which takes an IComparer<T>
object and returns a java.util.Comparator.  for an example usage,
see SortGeneric<T> in system.Array.

namespace system.collections.generic
{

    [java.attr.AsInterface]
    public abstract class IComparer<T> : java.util.Comparator
    {
        // a generic variance field is created for this abstract class,
        // see also GenericUtil::CreateGenericVarianceField()

        public abstract int Compare(T x, T y);

        [java.attr.RetainName]
        public int compare(object obj1, object obj2) => Compare(obj1, obj2);

        [java.attr.RetainName]
        public bool equals(object obj)
            => throw new System.PlatformNotSupportedException();
    }

}
*/
