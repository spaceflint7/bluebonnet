
namespace system.text.regularexpressions
{

    [System.Serializable]
    public class Match : Group
    {

        //[java.attr.RetainType] private java.util.regex.Matcher JavaMatcher;
        [java.attr.RetainType] GroupCollection groupCollection;

        public Match(java.util.regex.Matcher matcher, string input)
            : base(0, input, matcher.start(), matcher.end())
        {
            groupCollection = new GroupCollection(this, matcher, input);
        }

        private Match() : base(0, "", 0, 0)
        {
            groupCollection = new GroupCollection(this, null, null);
        }

        public virtual GroupCollection Groups => groupCollection;

        [java.attr.RetainType] private static Match _empty = new Match();
        public static Match Empty => _empty;
    }

}
