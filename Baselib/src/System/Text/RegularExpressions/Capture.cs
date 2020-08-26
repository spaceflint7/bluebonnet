
namespace system.text.regularexpressions
{

    [System.Serializable]
    public class Capture
    {

        [java.attr.RetainType] protected string input;
        [java.attr.RetainType] protected int start, end;

        public Capture(string input, int start, int end)
        {
            this.input = input;
            this.start = start;
            this.end = end;
        }

        public int Index => (start == -1) ? 0 : start;
        public int Length => end - start;
        public string Value
            => (start == -1) ? "" : ((java.lang.String) (object) input).substring(start, end);

        override public string ToString() => Value;
    }

}
