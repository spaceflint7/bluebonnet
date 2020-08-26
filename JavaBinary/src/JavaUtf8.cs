
using System;

namespace SpaceFlint.JavaBinary
{

    public static class JavaUtf8
    {

        public static byte[] Encode(string s)
        {
            int j = 0;
            foreach (var ch in s)
            {
                if (ch >= 0x800)
                    j += 3;
                else if (ch == 0 || ch >= 0x80)
                    j += 2;
                else
                    j += 1;
            }

            var blk = new byte[j];
            j = 0;
            foreach (var ch in s)
            {
                if (ch >= 0x800)
                {
                    // three-byte sequence
                    blk[j++] = (byte) (0xE0 | ((ch >> 12) & 0x0F)); // bits 15-12
                    blk[j++] = (byte) (0x80 | ((ch >> 6) & 0x3F));  // bits 11-6
                    blk[j++] = (byte) (0x80 | (ch & 0x3F));         // bits 5-0
                }
                else if (ch == 0 || ch >= 0x80)
                {
                    // two-byte sequence
                    blk[j++] = (byte) (0xC0 | ((ch >> 6) & 0x1F));  // bits 10-6
                    blk[j++] = (byte) (0x80 | (ch & 0x3F));         // bits 5-0
                }
                else
                {
                    // one-byte sequence
                    blk[j++] = (byte) ch;                           // bits 6-0
                }
            }

            return blk;
        }



        public static string Decode(byte[] blk, JavaException.Where Where)
        {
            int n = blk.Length;
            var ch = new char[n];
            int i = 0;
            int j = 0;
            while (i < n)
            {
                byte b0 = blk[i++];
                if ((b0 & 0x80) == 0)
                {
                    // one-byte sequence
                    ch[j++] = (char) b0;
                    continue;
                }
                if (i < n)
                {
                    byte b1 = blk[i++];
                    if ((b1 & 0xC0) == 0x80)
                    {
                        if ((b0 & 0xE0) == 0xC0)
                        {
                            // two-byte sequence
                            ch[j++] = (char) (((b0 & 0x1F) << 6) + (b1 & 0x3F));
                            continue;
                        }
                        else if (i < n && (b0 & 0xF0) == 0xE0)
                        {
                            byte b2 = blk[i++];
                            if ((b1 & 0xC0) == 0x80)
                            {
                                // three-byte sequence
                                ch[j++] = (char)
                                    (((b0 & 0x0F) << 12) + ((b1 & 0x3F) << 6) + (b2 & 0x3F));
                                continue;
                            }
                        }
                    }
                }
                throw Where.Exception("invalid UTF-8 data (at offset " + i + " in data)");
            }

            return new string(ch, 0, j);
        }

    }

}
