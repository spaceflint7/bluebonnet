
namespace system.text.regularexpressions
{

    public class Regex : System.Runtime.Serialization.ISerializable
    {

        [java.attr.RetainType] private java.util.regex.Pattern JavaPattern;

        //
        // constructor
        //

        public Regex(string pattern)
        {
            JavaPattern = java.util.regex.Pattern.compile(pattern);
        }

        //
        // Match
        //

        public Match Match(string input)
            => Matches(input).NextMatch();

        public static Match Match(string input, string pattern)
            => new Regex(pattern).Match(input);

        //
        // Matches
        //

        public MatchCollection Matches(string input) => new MatchCollection(JavaPattern, input);

        public static MatchCollection Matches(string input, string pattern)
            => new Regex(pattern).Matches(input);

        //
        // ISerializable
        //

        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
                                          System.Runtime.Serialization.StreamingContext context)
            => throw new System.NotImplementedException();
    }

}
