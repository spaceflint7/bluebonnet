
namespace system
{

    [java.attr.AsInterface]
    public abstract class IComparable : java.lang.Comparable
    {

        public abstract int CompareTo(object obj);

        [java.attr.RetainName]
        public int compareTo(object obj) => this.CompareTo(obj);

    }

}

