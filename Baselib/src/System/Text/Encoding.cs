
namespace system.text
{

    [System.Serializable]
    public abstract class Encoding : System.ICloneable
    {

        [java.attr.RetainType] protected java.nio.charset.Charset JavaCharset;

        public virtual object Clone()
        {
            Encoding newEncoding = (Encoding) MemberwiseClone();
            return newEncoding;
        }

        //
        // Decoder methods
        //

        public virtual int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                     char[] chars, int charIndex)
        {
            if (chars == null || bytes == null)
                throw new System.ArgumentNullException();

            int charLength = chars.Length - charIndex;
            if (    byteIndex < 0 || byteCount < 0 || charIndex < 0
                 || byteIndex + byteCount > bytes.Length || charLength < 0)
                throw new System.ArgumentOutOfRangeException();

            var charBuffer = java.nio.CharBuffer.wrap(chars, charIndex, charLength);
            var byteBuffer = java.nio.ByteBuffer.wrap((sbyte[]) (object) bytes, byteIndex, byteCount);

            var decoder = PerThreadJavaDecoder;
            var result = PerThreadJavaDecoder.decode(byteBuffer, charBuffer, true);
            if (result.isUnderflow())
                result = decoder.flush(charBuffer);

            if (result.isOverflow())
                throw new System.ArgumentException();
            if (! result.isUnderflow())
                throw new System.InvalidOperationException("Decoder exception " + result);

            return charBuffer.position();
        }

        public virtual int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new System.ArgumentNullException();
            if (index < 0 || count < 0 || index + count > bytes.Length)
                throw new System.ArgumentOutOfRangeException();

            var byteBuffer = java.nio.ByteBuffer.wrap((sbyte[]) (object) bytes, index, count);
            var charBuffer = JavaCharset.decode(byteBuffer);
            return charBuffer.remaining();
        }

        public virtual int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new System.ArgumentOutOfRangeException();
            return (int) (byteCount * PerThreadJavaDecoder.maxCharsPerByte());
        }

        public virtual System.Text.Decoder GetDecoder() => new DefaultDecoder(this);

        //
        // DefaultDecoder
        //

        [System.Serializable]
        public class DefaultDecoder : System.Text.Decoder
        {
            [java.attr.RetainType] protected Encoding encoding;

            public DefaultDecoder(Encoding encoding)
            {
                this.encoding = encoding;
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                         char[] chars, int charIndex)
                => encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

            public override int GetCharCount(byte[] bytes, int index, int count)
                => encoding.GetCharCount(bytes, index, count);
        }

        //
        // Encoder methods
        //

        public virtual int GetBytes(char[] chars, int charIndex, int charCount,
                                    byte[] bytes, int byteIndex)
        {
            if (chars == null || bytes == null)
                throw new System.ArgumentNullException();

            int bytesLength = bytes.Length - byteIndex;
            if (    charIndex < 0 || charCount < 0 || byteIndex < 0
                 || charIndex + charCount > chars.Length || bytesLength < 0)
                throw new System.ArgumentOutOfRangeException();

            var byteBuffer = java.nio.ByteBuffer.wrap((sbyte[]) (object) bytes, byteIndex, bytesLength);
            var charBuffer = java.nio.CharBuffer.wrap(chars, charIndex, charCount);

            var encoder = PerThreadJavaEncoder;
            var result = encoder.encode(charBuffer, byteBuffer, true);
            if (result.isUnderflow())
                result = encoder.flush(byteBuffer);

            if (result.isOverflow())
                throw new System.ArgumentException();
            if (! result.isUnderflow())
                throw new System.InvalidOperationException("Encoder exception " + result);

            return byteBuffer.position();
        }

        public virtual int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new System.ArgumentNullException();
            if (index < 0 || count < 0 || index + count > chars.Length)
                throw new System.ArgumentOutOfRangeException();

            var charBuffer = java.nio.CharBuffer.wrap(chars, index, count);
            var byteBuffer = JavaCharset.encode(charBuffer);
            return byteBuffer.remaining();
        }


        public virtual int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new System.ArgumentOutOfRangeException();
            return (int) (charCount * PerThreadJavaEncoder.maxBytesPerChar());
        }

        public virtual System.Text.Encoder GetEncoder() => new DefaultEncoder(this);

        //
        // DefaultEncoder
        //

        [System.Serializable]
        public class DefaultEncoder : System.Text.Encoder
        {
            [java.attr.RetainType] protected Encoding encoding;

            public DefaultEncoder(Encoding encoding)
            {
                this.encoding = encoding;
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount,
                                         byte[] bytes, int byteIndex, bool flush)
                => encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

            public override int GetByteCount(char[] chars, int index, int count, bool flush)
                => encoding.GetByteCount(chars, index, count);
        }

        //
        //
        //

        [java.attr.RetainType] private static byte[] emptyPreamble = new byte[0];
        public virtual byte[] GetPreamble() => emptyPreamble;

        //
        // PerThreadJavaDecoder / PerThreadJavaEncoder
        //

        [java.attr.RetainType] private static java.util.concurrent.ConcurrentHashMap
                        perThreadJavaCodersMap = new java.util.concurrent.ConcurrentHashMap();

        private object PerThreadJavaCoder(int index)
        {
            var elem = perThreadJavaCodersMap.get(this);
            if (elem == null)
            {
                var elemNew = new java.util.concurrent.atomic.AtomicReferenceArray(2);
                elem = perThreadJavaCodersMap.putIfAbsent(this, elemNew) ?? elemNew;
            }
            var coders = (java.util.concurrent.atomic.AtomicReferenceArray) elem;

            elem = coders.get(index);
            if (elem == null)
            {
                var elemNew = (index == 0)
                            ? (object) JavaCharset.newDecoder()
                                                  .onUnmappableCharacter(java.nio.charset.CodingErrorAction.REPLACE)
                                                  .onMalformedInput(java.nio.charset.CodingErrorAction.REPLACE)
                            : (object) JavaCharset.newEncoder()
                                                  .onUnmappableCharacter(java.nio.charset.CodingErrorAction.REPLACE)
                                                  .onMalformedInput(java.nio.charset.CodingErrorAction.REPLACE);
                elem = coders.getAndSet(index, elemNew) ?? elemNew;
            }
            return elem;
        }

        protected java.nio.charset.CharsetDecoder PerThreadJavaDecoder
            => ((java.nio.charset.CharsetDecoder) PerThreadJavaCoder(0)).reset();

        protected java.nio.charset.CharsetEncoder PerThreadJavaEncoder
            => ((java.nio.charset.CharsetEncoder) PerThreadJavaCoder(1)).reset();

        //
        // well known encodings
        //

        private static Encoding defaultEncoding;
        public static Encoding Default
            => System.Threading.LazyInitializer.EnsureInitialized<Encoding>(
                    ref defaultEncoding, () => new DefaultEncoding());

        private static Encoding asciiEncoding;
        public static Encoding ASCII
            => System.Threading.LazyInitializer.EnsureInitialized<Encoding>(
                    ref asciiEncoding, () => new ASCIIEncoding());

        private static Encoding utf8Encoding;
        public static Encoding UTF8
            => System.Threading.LazyInitializer.EnsureInitialized<Encoding>(
                    ref utf8Encoding, () => new UTF8Encoding(true));

        //
        // utility methods
        //

        public virtual char[] GetChars(byte[] bytes, int index, int count)
        {
            char[] chars = new char[GetCharCount(bytes, index, count)];
            GetChars(bytes, index, count, chars, 0);
            return chars;
        }

        public virtual int GetByteCount(string s)
        {
            ThrowHelper.ThrowIfNull(s);
            var chars = s.ToCharArray();
            return GetByteCount(chars, 0, chars.Length);
        }

        public virtual byte[] GetBytes(string s)
        {
            ThrowHelper.ThrowIfNull(s);
            var chars = s.ToCharArray();
            int length = chars.Length;
            byte[] bytes = new byte[length];
            GetBytes(chars, 0, length, bytes, 0);
            return bytes;
        }

        public virtual int GetBytes(string s, int charIndex, int charCount,
                                    byte[] bytes, int byteIndex)
        {
            ThrowHelper.ThrowIfNull(s);
            return GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);
        }

        public virtual string GetString(byte[] bytes)
        {
            ThrowHelper.ThrowIfNull(bytes);
            return new string(GetChars(bytes, 0, bytes.Length));
        }

        public virtual string GetString(byte[] bytes, int index, int count)
            => new string(GetChars(bytes, index, count));

    }



    [System.Serializable]
    class DefaultEncoding : Encoding
    {
        public DefaultEncoding()
        {
            JavaCharset = java.nio.charset.Charset.defaultCharset();
        }
    }



    [System.Serializable]
    class ASCIIEncoding : Encoding
    {
        public ASCIIEncoding()
        {
            JavaCharset = java.nio.charset.Charset.forName("US-ASCII");
        }
    }



    [System.Serializable]
    public class UTF8Encoding : Encoding
    {
        [java.attr.RetainType] private bool emitUTF8Identifier = false;
        //[java.attr.RetainType] private bool isThrowException = false;

        public UTF8Encoding()                       : this(false, false) { }
        public UTF8Encoding(bool emit)              : this(emit,  false) { }
        public UTF8Encoding(bool emit, bool @throw)
        {
            JavaCharset = java.nio.charset.Charset.forName("UTF-8");
        }

        public override byte[] GetPreamble()
            => emitUTF8Identifier ? new byte[3] { 0xEF, 0xBB, 0xBF } : base.GetPreamble();
    }



    [System.Serializable]
    public class UnicodeEncoding : Encoding
    {
        public bool bigEndian;
        public bool byteOrderMark;
        public const int CharSize = 2;

        public UnicodeEncoding() : this(false, true, false) { }
        public UnicodeEncoding(bool bigEndian, bool byteOrderMark)
            : this(bigEndian, byteOrderMark, false) { }

        public UnicodeEncoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes)
        {
            this.bigEndian = bigEndian;
            this.byteOrderMark = byteOrderMark;
            JavaCharset = java.nio.charset.Charset.forName(
                                    bigEndian ? "UTF-16BE" : "UTF-16LE");
        }

        public override byte[] GetPreamble()
            =>   (! byteOrderMark) ? new byte[0]
               : bigEndian ? new byte[2] { 0xFE, 0xFF } : new byte[2] { 0xFF, 0xfE };
    }



    [System.Serializable]
    public abstract class DecoderFallback
    {
    }

    [System.Serializable]
    public abstract class EncoderFallback
    {
    }

}
