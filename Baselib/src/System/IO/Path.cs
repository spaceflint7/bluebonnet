
namespace system.io
{

    public static class Path
    {

        private static string CheckPath(string path, bool checkWildcard = false)
        {
            ThrowHelper.ThrowIfNull(path);
            return path.Replace('\\', '/');
        }

        public static bool IsPathRooted(string path) => false;

        public static string Combine(string path1, string path2)
        {
            path1 = CheckPath(path1);
            path2 = CheckPath(path2);
            if (path2.Length == 0)
                return path1;
            if (path1.Length == 0)
                return path2;

            char ch = path1[path1.Length - 1];
            return (ch != '/') ? (path1 + "/" + path2) : (path1 + path2);
        }

        public static string Combine(string path1, string path2, string path3)
            => Combine(Combine(path1, path2), path3);
    }

    public static class PathInternal
    {
    }

}
