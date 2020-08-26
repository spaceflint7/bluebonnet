
namespace system
{

    public static class Buffer
    {

        public static void InternalBlockCopy(Array src, int srcOffsetBytes,
                                             Array dst, int dstOffsetBytes, int byteCount)
        {
            if (src.SyncRoot is char[] srcChars && dst.SyncRoot is char[] dstChars)
            {
                int srcIndex = srcOffsetBytes >> 1;
                int dstIndex = dstOffsetBytes >> 1;
                int count = byteCount >> 1;
                if (    (srcIndex << 1) == srcOffsetBytes
                     && (dstIndex << 1) == dstOffsetBytes
                     && (count << 1) == byteCount)
                {
                    java.lang.System.arraycopy(srcChars, srcIndex, dstChars, dstIndex, count);
                    return;
                }
            }
            throw new System.PlatformNotSupportedException(
                                "InternalBlockCopy/" + src.GetType() + "/" + dst.GetType());
        }

    }

}
