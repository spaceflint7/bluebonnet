
namespace system.text.regularexpressions
{

    [System.Serializable]
    public class GroupCollection : System.Collections.ICollection
    {
        [java.attr.RetainType] private Match match;
        [java.attr.RetainType] private Group[] elements;

        public GroupCollection(Match match, java.util.regex.Matcher matcher, string input)
        {
            this.match = match;
            if (matcher == null)
                elements = new Group[0];
            else
            {
                int n = matcher?.groupCount() ?? 0;
                elements = new Group[n + 1];
                for (int i = 0; i <= n; i++)
                    elements[i] = new Group(i, input, matcher.start(i), matcher.end(i));
            }
        }

        public int Count => elements.Length;

        public Group this[int groupnum] => elements[groupnum];
        public Group this[string groupname] => throw new System.PlatformNotSupportedException();

        public void CopyTo(System.Array array, int index)
        {
            if (array == null)
                throw new System.ArgumentNullException();
            elements.CopyTo(array, index);
        }

        public bool IsReadOnly => true;
        public bool IsSynchronized => false;
        public object SyncRoot => match;

        public System.Collections.IEnumerator GetEnumerator()
            => ((System.Array) elements).GetEnumerator();
    }

}
