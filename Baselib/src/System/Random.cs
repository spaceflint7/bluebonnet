
namespace system
{

    public class Random
    {
        [java.attr.RetainType] private java.util.Random _random;

        // if this is an instance of Random itself, rather than a
        // derived class, then don't bother with calls to Sample()
        [java.attr.RetainType] private bool isSystemRandom;

        public Random()
        {
            isSystemRandom = (GetType() == typeof(System.Random));
            _random = new java.util.Random();
        }

        public Random(int Seed)
        {
            isSystemRandom = (GetType() == typeof(System.Random));
            _random = new java.util.Random(Seed);
        }



        // NextBytes(byte[]) does NOT use Sample()
        public virtual void NextBytes(byte[] buffer)
        {
            ThrowHelper.ThrowIfNull(buffer);
            int n = buffer.Length;
            for (int i = 0; i < n; i++)
                buffer[i] = (byte) (_random.nextInt() % 256);
        }

        // Next() does NOT use Sample()
        public virtual int Next() => _random.nextInt();



        // NextBytes(Span<byte>) uses Sample()
        public virtual void NextBytes(Span<byte> buffer)
        {
            throw new System.PlatformNotSupportedException();
            /*int n = buffer.Length;
            for (int i = 0; i < n; i++)
                buffer[i] = (byte) (Sample() * 256);*/
        }



        // Next(int) uses Sample()
        public virtual int Next(int maxValue) => Next(0, maxValue);

        // Next(int,int) uses Sample()
        public virtual int Next(int minValue, int maxValue)
        {
            if (maxValue < minValue)
                ThrowHelper.ThrowArgumentOutOfRangeException();
            var range = (long) maxValue - minValue;
            if (range <= int.MaxValue && isSystemRandom)
                return _random.nextInt((int) range) + minValue;
            return minValue + (int) (Sample() * range);
        }



        // NextInt64 uses Sample()
        public virtual long NextInt64() => NextInt64(0, long.MaxValue);

        // NextInt64(long) uses Sample()
        public virtual long NextInt64(long maxValue) => NextInt64(0, maxValue);

        // NextInt64(long, long) uses Sample()
        public virtual long NextInt64(long minValue, long maxValue)
        {
            if (maxValue < minValue)
                ThrowHelper.ThrowArgumentOutOfRangeException();
            return isSystemRandom ? (_random.nextLong() % maxValue)
                                  : (long) (Sample() * long.MaxValue);
        }



        // NextSingle uses Sample()
        public virtual float NextSingle() => (float) Sample();

        // NextDouble uses Sample()
        public virtual double NextDouble() => Sample();



        protected virtual double Sample() => _random.nextDouble();
    }
}
