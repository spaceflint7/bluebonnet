
namespace system
{

    [System.Serializable]
    public struct Guid : System.IFormattable, System.IComparable,
                         System.IComparable<Guid>, System.IEquatable<Guid>
    {
        [java.attr.RetainType] private java.util.UUID _uuid;
        private java.util.UUID uuid => _uuid ?? (_uuid = new java.util.UUID(0, 0));

        public Guid(string g) => _uuid = java.util.UUID.fromString(g);

        public override string ToString() => uuid.ToString();
        public string ToString(string format) => ToString(format, null);
        public string ToString(string format, System.IFormatProvider provider)
        {
            if (string.IsNullOrEmpty(format) || format == "D")
                return uuid.ToString();
            throw new System.FormatException(format);
        }

        public override int GetHashCode() => uuid.GetHashCode();
        public override bool Equals(object o) => (o is Guid g) && uuid.Equals(g._uuid);

        public bool Equals(Guid g) => uuid.Equals(g._uuid);
        public int CompareTo(Guid g) => uuid.compareTo(g.uuid);
        public int CompareTo(object o)
            => (o is Guid g) ? uuid.compareTo(g.uuid) : throw new System.ArgumentException();
        public static bool operator ==(Guid a, Guid b) => a.uuid.Equals(b._uuid);
        public static bool operator !=(Guid a, Guid b) => !a.uuid.Equals(b._uuid);

        public static readonly Guid Empty = new Guid();
    }

}
