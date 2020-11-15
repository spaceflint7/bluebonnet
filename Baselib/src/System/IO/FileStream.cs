
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

        public FileStream(string path, System.IO.FileMode mode)
            : this(path, mode, (mode == System.IO.FileMode.Append
                    ? System.IO.FileAccess.Write : System.IO.FileAccess.ReadWrite)) { }

        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share)
            : this(path, mode, access) { }

        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access)
        {
            ThrowHelper.ThrowIfNull(path);

            if ((int) access < 1 || (int) access > 3)
                throw new System.ArgumentOutOfRangeException();

            switch (mode)
            {
                case System.IO.FileMode.CreateNew:      CreateIfNotExist(); break;
                case System.IO.FileMode.Create:         CreateOrTruncate(); break;
                case System.IO.FileMode.Open:           OpenExisting();     break;
                case System.IO.FileMode.OpenOrCreate:   OpenOrCreate();     break;
                case System.IO.FileMode.Truncate:       TruncateExisting(); break;
                case System.IO.FileMode.Append:         AppendOrCreate();   break;
                default:           throw new System.ArgumentOutOfRangeException();
            }

            void CreateIfNotExist()
            {
                // create a new file; throw IOException if exists
                if (access == System.IO.FileAccess.Read)
                    throw new System.ArgumentException(access.ToString());
                try
                {
                    var stream = new java.io.RandomAccessFile(path, "r");
                    stream.close();
                    throw new System.IO.IOException("Exists: " + path);
                }
                catch (java.io.FileNotFoundException)
                {
                }
                JavaChannel = new java.io.RandomAccessFile(path, "rw").getChannel();
                Flags = CAN_WRITE | CAN_SEEK;
            }

            void CreateOrTruncate()
            {
                // create a new file or truncate an existing file
                if (access == System.IO.FileAccess.Read)
                    throw new System.ArgumentException(access.ToString());
                var stream = new java.io.RandomAccessFile(path, "rw");
                stream.setLength(0);
                JavaChannel = stream.getChannel();
                Flags = CAN_WRITE | CAN_SEEK;
            }

            void OpenExisting()
            {
                // open existing file, throwing FileNotFoundException if cannot
                var stream = new java.io.RandomAccessFile(path, "r");
                if (access != System.IO.FileAccess.Read)
                {
                    // knowing the file exists, reopen for writing if necessary
                    stream.close();
                    stream = new java.io.RandomAccessFile(path, "rw");
                    Flags = CAN_READ | CAN_WRITE | CAN_SEEK;
                }
                else
                    Flags = CAN_READ | CAN_SEEK;
                JavaChannel = stream.getChannel();
            }

            void OpenOrCreate()
            {
                // open existing file, or create a new file
                JavaChannel = new java.io.RandomAccessFile(path, "rw").getChannel();
                if (access != System.IO.FileAccess.Read)
                    Flags = CAN_READ | CAN_WRITE | CAN_SEEK;
                else
                    Flags = CAN_READ | CAN_SEEK;
            }

            void TruncateExisting()
            {
                // truncate existing file, or throws FileNotFoundException
                if (access == System.IO.FileAccess.Read)
                    throw new System.ArgumentException(access.ToString());
                var stream = new java.io.RandomAccessFile(path, "r");
                stream.close();
                stream = new java.io.RandomAccessFile(path, "rw");
                stream.setLength(0);
                JavaChannel = stream.getChannel();
                Flags = CAN_WRITE | CAN_SEEK;
            }

            void AppendOrCreate()
            {
                var stream = new java.io.RandomAccessFile(path, "rw");
                stream.seek(stream.length());
                JavaChannel = stream.getChannel();
                Flags = CAN_WRITE | CAN_SEEK;
            }

        }

        //
        // properties
        //

        public override bool CanRead => (Flags & CAN_READ) != 0;
        public override bool CanWrite => (Flags & CAN_WRITE) != 0;
        public override bool CanSeek => (Flags & CAN_SEEK) != 0;

        public override long Length => JavaChannel.size();

        public override long Position
        {
            get => JavaChannel.position();
            set => JavaChannel.position(value);
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
        {
            if (value < 0)
                throw new System.ArgumentOutOfRangeException();
            JavaChannel.truncate(value);
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            if (origin == System.IO.SeekOrigin.Current)
                offset += JavaChannel.position();
            else if (origin == System.IO.SeekOrigin.End)
                offset = JavaChannel.size() - offset;
            else if (origin != System.IO.SeekOrigin.Begin)
                throw new System.ArgumentException();
            JavaChannel.position(offset);
            return JavaChannel.position();
        }

        //
        // static constructor
        //

        static FileStream()
        {
            system.Util.DefineException(
                (java.lang.Class) typeof(java.io.FileNotFoundException),
                (exc) => new System.IO.FileNotFoundException(exc.getMessage())
            );

            system.Util.DefineException(
                (java.lang.Class) typeof(java.nio.channels.NonWritableChannelException),
                (exc) => new System.NotSupportedException(exc.getMessage())
            );

            system.Util.DefineException(
                (java.lang.Class) typeof(java.nio.channels.ClosedChannelException),
                (exc) => new System.ObjectDisposedException(exc.getMessage())
            );

        }

    }

}
