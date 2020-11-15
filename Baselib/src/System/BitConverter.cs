
namespace system
{

    public static class BitConverter
    {

        [java.attr.RetainType] private static readonly bool _IsLittleEndian =
            java.nio.ByteOrder.nativeOrder() == java.nio.ByteOrder.LITTLE_ENDIAN;
        public static readonly bool IsLittleEndian = _IsLittleEndian;

        public static int SingleToInt32Bits(float value) => java.lang.Float.floatToRawIntBits(value);
        public static float Int32BitsToSingle(int value) => java.lang.Float.intBitsToFloat(value);

        public static long DoubleToInt64Bits(double value) => java.lang.Double.doubleToRawLongBits(value);
        public static double Int64BitsToDouble(long value) => java.lang.Double.longBitsToDouble(value);

        public static byte[] GetBytes(bool value) => new byte[] { value ? (byte) 1 : (byte) 0 };
        public static byte[] GetBytes(short value)
            => (byte[]) (object) java.util.Arrays.copyOf(
                    GetByteBuffer(2).putShort(0, value).array(), 2);
        public static byte[] GetBytes(ushort value) => GetBytes((short) value);
        public static byte[] GetBytes(char value) => GetBytes((short) value);
        public static byte[] GetBytes(int value)
            => (byte[]) (object) java.util.Arrays.copyOf(
                    GetByteBuffer(4).putInt(0, value).array(), 4);
        public static byte[] GetBytes(uint value) => GetBytes((int) value);
        public static byte[] GetBytes(long value)
            => (byte[]) (object) java.util.Arrays.copyOf(
                    GetByteBuffer(8).putLong(0, value).array(), 8);
        public static byte[] GetBytes(ulong value) => GetBytes((long) value);
        public static byte[] GetBytes(float value)
            => (byte[]) (object) java.util.Arrays.copyOf(
                    GetByteBuffer(4).putFloat(0, value).array(), 4);
        public static byte[] GetBytes(double value)
            => (byte[]) (object) java.util.Arrays.copyOf(
                    GetByteBuffer(8).putDouble(0, value).array(), 8);

        public static short ToInt16(byte[] value, int startIndex)
        {
            ThrowHelper.ThrowIfNull(value);
            if ((uint) startIndex >= value.Length || startIndex > value.Length - 2)
                ThrowHelper.ThrowArgumentOutOfRangeException();
            byte b0 = value[startIndex];
            byte b1 = value[++startIndex];
            if (! _IsLittleEndian)
                (b0, b1) = (b1, b0);
            return (short) (b0 | (b1 << 8));
        }
        public static ushort ToUInt16(byte[] value, int startIndex)
            => (ushort) ToInt16(value, startIndex);
        public static char ToChar(byte[] value, int startIndex)
            => (char) ToInt16(value, startIndex);

        public static int ToInt32(byte[] value, int startIndex)
        {
            ThrowHelper.ThrowIfNull(value);
            if ((uint) startIndex >= value.Length || startIndex > value.Length - 4)
                ThrowHelper.ThrowArgumentOutOfRangeException();
            byte b0 = value[startIndex];
            byte b1 = value[++startIndex];
            byte b2 = value[++startIndex];
            byte b3 = value[++startIndex];
            if (! _IsLittleEndian)
                (b0, b1, b2, b3) = (b3, b2, b1, b0);
            return (b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));
        }
        public static uint ToUInt32(byte[] value, int startIndex)
            => (uint) ToInt32(value, startIndex);
        public static float ToSingle(byte[] value, int startIndex)
            => java.lang.Float.intBitsToFloat(ToInt32(value, startIndex));

        public static long ToInt64(byte[] value, int startIndex)
        {
            ThrowHelper.ThrowIfNull(value);
            if ((uint) startIndex >= value.Length || startIndex > value.Length - 8)
                ThrowHelper.ThrowArgumentOutOfRangeException();
            byte b0 = value[startIndex];
            byte b1 = value[++startIndex];
            byte b2 = value[++startIndex];
            byte b3 = value[++startIndex];
            byte b4 = value[++startIndex];
            byte b5 = value[++startIndex];
            byte b6 = value[++startIndex];
            byte b7 = value[++startIndex];
            if (! _IsLittleEndian)
                (b0, b1, b2, b3, b4, b5, b6, b7) = (b7, b6, b5, b4, b3, b2, b1, b0);
            int i1 = (b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));
            int i2 = (b4 | (b5 << 8) | (b6 << 16) | (b7 << 24));
            return (uint) i1 | ((long) i2 << 32);
        }
        public static ulong ToUInt64(byte[] value, int startIndex)
            => (ulong) ToInt64(value, startIndex);
        public static double ToDouble(byte[] value, int startIndex)
            => java.lang.Double.longBitsToDouble(ToInt64(value, startIndex));

        public static bool ToBoolean(byte[] value, int startIndex)
        {
            ThrowHelper.ThrowIfNull(value);
            if (startIndex < 0 || startIndex > value.Length - 1)
                ThrowHelper.ThrowArgumentOutOfRangeException();
            return (value[startIndex] != 0);
        }

        public static string ToString(byte[] value, int startIndex, int length)
        {
            ThrowHelper.ThrowIfNull(value);
            int valueLength = value.Length;
            if (    startIndex < 0 || length < 0
                 || (startIndex >= valueLength && startIndex > 0)
                 || (startIndex > valueLength - length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }
            if (length == 0)
                return "";

            var output = new char[valueLength * 2];
            int outputIndex = 0;
            while (valueLength --> 0)
            {
                byte v = value[startIndex++];
                output[outputIndex++] = HexChars[v >> 4];
                output[outputIndex++] = HexChars[v & 0x0F];
            }
            return new string(output);
        }

        private static java.nio.ByteBuffer GetByteBuffer(int len)
        {
            var buffer = (java.nio.ByteBuffer) TlsByteBuffer.get();
            if (buffer == null || buffer.limit() < len)
            {
                buffer = java.nio.ByteBuffer.allocate(len)
                                            .order(java.nio.ByteOrder.nativeOrder());
                TlsByteBuffer.set(buffer);
            }
            return buffer;
        }

        [java.attr.RetainType] static java.lang.ThreadLocal TlsByteBuffer =
                                                            new java.lang.ThreadLocal();

        [java.attr.RetainType] private static readonly char[] HexChars =
                                                            "0123456789ABCDEF".ToCharArray();

    }

}
