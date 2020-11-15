
using FileMode = System.IO.FileMode;
using FileAccess = System.IO.FileAccess;
using FileShare = System.IO.FileShare;

namespace system.io
{

    public static class File
    {

        public static FileStream Open(string path, FileMode mode)
            => new FileStream(path, mode);

        public static FileStream Open(string path, FileMode mode, FileAccess access)
            => new FileStream(path, mode, access);

        public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
            => new FileStream(path, mode, access);
    }

}
