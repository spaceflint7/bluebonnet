
using FileAttributes = System.IO.FileAttributes;

namespace system.io
{

    public abstract class FileSystemInfo
    {
        [java.attr.RetainType] protected java.io.File JavaFile;

        protected FileSystemInfo(java.io.File javaFile)
        {
            JavaFile = javaFile;
        }

        public virtual string FullName => JavaFile.getAbsolutePath();

        public virtual string Name => JavaFile.getName();

        public string Extension
        {
            get
            {
                var name = Name;
                int dot = Name.LastIndexOf('.');
                if (dot != -1)
                {
                    int slash = Name.LastIndexOf('/');
                    if (dot > slash)
                    {
                        return ((java.lang.String) (object) name).substring(dot);
                    }
                }
                return "";
            }
        }

        public FileAttributes Attributes
        {
            get
            {
                FileAttributes attr = (FileAttributes) 0;
                if (JavaFile.isDirectory())
                    attr |= FileAttributes.Directory;
                if (JavaFile.isHidden())
                    attr |= FileAttributes.Hidden;
                if (! JavaFile.canWrite())
                    attr |= FileAttributes.ReadOnly;
                if (attr == (FileAttributes) 0)
                    attr = FileAttributes.Normal;
                return attr;
            }
            set => throw new System.PlatformNotSupportedException();
        }

        public virtual void Delete() => JavaFile.delete();

        public virtual bool Exists => JavaFile.exists();

        public DateTime CreationTimeUtc
        {
            get => throw new System.PlatformNotSupportedException();
            set => throw new System.PlatformNotSupportedException();
        }

        public DateTime LastAccessTimeUtc
        {
            get => throw new System.PlatformNotSupportedException();
            set => throw new System.PlatformNotSupportedException();
        }

        public DateTime LastWriteTimeUtc
        {
            // 10,000 DateTime ticks in a millisecond
            get => new DateTime(JavaFile.lastModified() * 10000);
            set => JavaFile.setLastModified(value.Ticks / 10000);
        }

        public DateTime CreationTime
        {
            get => CreationTimeUtc.ToLocalTime();
            set => CreationTimeUtc = value.ToUniversalTime();
        }

        public DateTime LastAccessTime
        {
            get => LastAccessTimeUtc.ToLocalTime();
            set => LastAccessTimeUtc = value.ToUniversalTime();
        }

        public DateTime LastWriteTime
        {
            get => LastWriteTimeUtc.ToLocalTime();
            set => LastWriteTimeUtc = value.ToUniversalTime();
        }

        public void Refresh() { }

    }

}
