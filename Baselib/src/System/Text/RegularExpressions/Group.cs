
namespace system.text.regularexpressions
{

    [System.Serializable]
    public class Group : Capture
    {
        private int number;

        public Group(int number, string input, int start, int end)
            : base(input, start, end)
        {
            this.number = number;
        }

        public string Name => number.ToString();

        public bool Success => (number != -1 && start != -1);

        public CaptureCollection Captures => throw new System.PlatformNotSupportedException();
    }

}
