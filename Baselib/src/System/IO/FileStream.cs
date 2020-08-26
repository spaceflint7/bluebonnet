
namespace system.io
{

    public class FileStream : System.IO.Stream
    {

        [java.attr.RetainType] java.nio.channels.FileChannel JavaChannel;
        [java.attr.RetainType] int Flags;

        public const int CAN_READ = 1;
        public const int CAN_WRITE = 2;
        public const int CAN_SEEK = 4;
        public const int SKIP_FLUSH = 128;

        //
        // constructors
        //

        public FileStream(java.nio.channels.FileChannel channel, int flags)
        {
            JavaChannel = channel;
            Flags = flags;
        }

        public override bool CanRead => (Flags & CAN_READ) != 0;
        public override bool CanWrite => (Flags & CAN_WRITE) != 0;
        public override bool CanSeek => (Flags & CAN_SEEK) != 0;

        public override long Length
            => throw new System.PlatformNotSupportedException();

        public override long Position
        {
            get => throw new System.PlatformNotSupportedException();
            set => throw new System.PlatformNotSupportedException();
        }

        public override void Flush()
        {
            if ((Flags & SKIP_FLUSH) == 0)
            {
                try
                {
                    JavaChannel.force(false);
                }
                catch (java.io.IOException)
                {
                    Flags |= SKIP_FLUSH;
                }
            }
        }

        private java.nio.ByteBuffer ReadWriteBuffer(byte[] array, int offset, int count, int flagBit)
        {
            if (array == null)
                throw new System.ArgumentNullException();
            if (offset < 0 || count < 0)
                throw new System.ArgumentOutOfRangeException();
            if (array.Length - offset < count)
                throw new System.ArgumentException();
            if ((Flags & flagBit) == 0)
                throw new System.NotSupportedException();
            return java.nio.ByteBuffer.wrap((sbyte[]) (object) array, offset, count);
        }

        public override int Read(byte[] array, int offset, int count)
        {
            var buffer = ReadWriteBuffer(array, offset, count, CAN_READ);
            count = JavaChannel.read(buffer);
            if (count < 0)
                count = 0;
            return count;
        }

        public override void Write(byte[] array, int offset, int count)
        {
            var buffer = ReadWriteBuffer(array, offset, count, CAN_WRITE);
            JavaChannel.write(buffer);
        }

        public override void SetLength(long value)
            => throw new System.PlatformNotSupportedException();

        public override long Seek(long offset, System.IO.SeekOrigin origin)
            => throw new System.PlatformNotSupportedException();
    }

}
