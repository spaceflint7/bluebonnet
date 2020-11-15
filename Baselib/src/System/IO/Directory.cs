
namespace system.io
{

    public static class Directory
    {

        public static bool Exists(string path)
        {
            bool exists = false;
            if (path != null && path.Length != 0)
            {
                var file = new java.io.File(path);
                exists = file.exists() && file.isDirectory();
            }
            return exists;
        }

        public static DirectoryInfo CreateDirectory(string path)
        {
            ThrowHelper.ThrowIfNull(path);
            path = path.Trim();
            if (path.Length == 0)
                throw new System.ArgumentException();
            var file = new java.io.File(path);
            file.mkdirs();
            return new DirectoryInfo(file);
        }

    }

}
