
namespace system
{

    public struct Guid
    {
        string vvv;

        public Guid (string g)
        {
            vvv = g;
        }

        public override string ToString() => vvv;

        public static readonly Guid Empty = new Guid();
    }

}
