
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

        public Regex(string pattern, System.Text.RegularExpressions.RegexOptions options)
        {
            int flags = 0;

            if ((options & System.Text.RegularExpressions.RegexOptions.CultureInvariant) != 0)
                options &= ~System.Text.RegularExpressions.RegexOptions.CultureInvariant;
            else
            {
                flags   |= java.util.regex.Pattern.UNICODE_CASE
                        |  java.util.regex.Pattern.CANON_EQ;
            }

            if ((options & System.Text.RegularExpressions.RegexOptions.IgnoreCase) != 0)
            {
                options &= ~System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                flags   |= java.util.regex.Pattern.CASE_INSENSITIVE;
            }

            if ((options & System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace) != 0)
            {
                options &= ~System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace;
                flags   |= java.util.regex.Pattern.COMMENTS;
            }

            if ((options & System.Text.RegularExpressions.RegexOptions.Multiline) != 0)
            {
                options &= ~System.Text.RegularExpressions.RegexOptions.Multiline;
                flags   |= java.util.regex.Pattern.MULTILINE;
            }

            if ((options & System.Text.RegularExpressions.RegexOptions.Singleline) != 0)
            {
                options &= ~System.Text.RegularExpressions.RegexOptions.Singleline;
                flags   |= java.util.regex.Pattern.DOTALL;
            }

            options &= ~System.Text.RegularExpressions.RegexOptions.Compiled;
            if (options != 0)
                throw new System.ArgumentOutOfRangeException();

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
        // IsMatch
        //

        public bool IsMatch(string input)
            => Match(input).Success;

        //
        // Replace
        //

        public static string Replace(string input, string pattern, string replacement)
            => java.util.regex.Pattern.compile(pattern)
                                      .matcher((java.lang.CharSequence) (object) input)
                                      .replaceAll(replacement);

        //
        // Escape
        //

        public static string Escape(string str)
        {
            int idx1 = str.IndexOfAny(EscapeChars);
            if (idx1 == -1)
                return str;
            int idx0 = 0;
            int len = ((java.lang.String) (object) str).length();
            var sb = new java.lang.StringBuilder();
            for (;;)
            {
                sb.append(((java.lang.String) (object) str).substring(idx0, idx1));
                sb.append('\\');
                var ch = ((java.lang.String) (object) str).charAt(idx1);
                     if (ch == '\u0009') ch = 't';
                else if (ch == '\u000A') ch = 'n';
                else if (ch == '\u000C') ch = 'f';
                else if (ch == '\u000D') ch = 'r';
                sb.append(ch);

                if (    (idx0 = idx1 + 1) >= len
                     || (idx1 = str.IndexOfAny(EscapeChars, idx0)) == -1)
                {
                    sb.append(((java.lang.String) (object) str).substring(idx0));
                    return sb.ToString();
                }
            }
        }

        [java.attr.RetainType] private static readonly char[] EscapeChars = {
            '\u0009', '\u000A', '\u000C', '\u000D', '\u0020',   // spaces
            '#', '$', '(', ')', '*', '+', '.', '?', '[', '\\', '^', '{', '|' };
        //   |{^\[?.+*)($#  SPC(32)TAB(9)0x000D,0x000C, 0x000A,0x0009

        //
        // ISerializable
        //

        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
                                          System.Runtime.Serialization.StreamingContext context)
            => throw new System.NotImplementedException();
    }

}
