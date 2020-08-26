
namespace system.text.regularexpressions
{

    [System.Serializable]
    public class MatchCollection : System.Collections.ICollection
    {

        [java.attr.RetainType] private java.util.regex.Matcher JavaMatcher;
        [java.attr.RetainType] private string input;

        //
        // constructor
        //

        public MatchCollection(java.util.regex.Pattern javaPattern, string input)
        {
            this.input = input;
            JavaMatcher = javaPattern.matcher((java.lang.CharSequence) (object) input);
        }

        //
        //
        //

        public Match NextMatch()
        {
            if (! JavaMatcher.find())
                return Match.Empty;
            return new Match(JavaMatcher, input);
        }



        //
        // ICollection
        //

        public void CopyTo(System.Array array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
                throw new System.ArgumentException("RankMultiDimNotSupported");
            throw new System.NotImplementedException();
        }

        public int Count => throw new System.NotImplementedException();

        public object SyncRoot => this;
        public bool IsSynchronized => false;


        //
        // Enumerator
        //

        public System.Collections.IEnumerator GetEnumerator() => new Enumerator(this);

        struct Enumerator : System.Collections.IEnumerator
        {
            [java.attr.RetainType] private MatchCollection matchCollection;
            [java.attr.RetainType] private Match current;

            public Enumerator(MatchCollection matchCollection)
            {
                this.matchCollection = matchCollection;
                current = Match.Empty;
            }

            public bool MoveNext()
            {
                current = matchCollection.NextMatch();
                return ! (object.ReferenceEquals(current, Match.Empty));
            }

            public object Current => current;

            public void Reset() => matchCollection.JavaMatcher.reset();
        }

    }

}
