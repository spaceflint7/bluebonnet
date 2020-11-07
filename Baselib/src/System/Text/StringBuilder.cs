
using System;
using System.Runtime.Serialization;

namespace system.text
{

    [Serializable]
    public sealed class StringBuilder : System.Runtime.Serialization.ISerializable
    {

        [java.attr.RetainType] private readonly java.lang.StringBuilder sb;

        public StringBuilder()
        {
            sb = new java.lang.StringBuilder();
        }

        public StringBuilder(int capacity)
        {
            if (capacity < 0)
                throw new System.ArgumentOutOfRangeException();
            sb = new java.lang.StringBuilder(capacity);
        }

        public StringBuilder(string str, int capacity)
        {
            if (capacity < 0)
                throw new System.ArgumentOutOfRangeException();
            sb = new java.lang.StringBuilder(capacity);
            Append(str);
        }

        public StringBuilder Clear()
        {
            sb.setLength(0);
            return this;
        }

        public StringBuilder Append(string value)
        {
            if (value != null)
                sb.append(value);
            return this;
        }

        public StringBuilder Append(char value)
        {
            sb.append(value);
            return this;
        }

        public StringBuilder Append(char value, int repeatCount)
        {
            var buffer = new char[repeatCount];
            for (int i = 0; i < repeatCount; i++)
                buffer[i] = value;
            sb.append(buffer);
            return this;
        }

        public StringBuilder Append(char[] value, int startIndex, int charCount)
        {
            if (startIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException();
            if (value == null)
            {
                if (startIndex != 0 || charCount != 0)
                    throw new ArgumentOutOfRangeException();
            }
            else
            {
                if (charCount > value.Length - startIndex)
                    throw new ArgumentOutOfRangeException();
                sb.append(value, startIndex, charCount);
            }
            return this;
        }

        public StringBuilder Append(bool value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(byte value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(sbyte value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(short value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(ushort value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(int value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(uint value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(long value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(ulong value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(float value)
        {
            sb.append(value.ToString());
            return this;
        }

        public StringBuilder Append(double value)
        {
            sb.append(value.ToString());
            return this;
        }

        public int Capacity
        {
            get => sb.capacity();
            set
            {
                if (value < 0 || value < sb.length())
                    throw new ArgumentOutOfRangeException();
                sb.ensureCapacity(value);
            }
        }

        public int Length
        {
            get => sb.length();
            set
            {
                if (value < 0) // || value > MaxCapacity
                    throw new ArgumentOutOfRangeException();
                sb.setLength(value);
            }
        }

        public override string ToString() => sb.ToString();

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new PlatformNotSupportedException();

        //
        // Insert
        //

        public StringBuilder Insert(int index, char value)
        {
            if (index < 0 || index > sb.length())
                throw new ArgumentOutOfRangeException();
            sb.insert(index, value);
            return this;
        }

        //
        // AppendFormatHelper
        //

        public static void AppendFormatHelper(java.lang.StringBuilder sb, IFormatProvider provider,
                                              java.lang.String format, object[] args)
        {
            if (format == null || args == null)
                throw new System.ArgumentNullException();

            ICustomFormatter customFormatter = null;
            if (provider != null)
                customFormatter = (ICustomFormatter) provider.GetFormat(typeof(ICustomFormatter));

            try
            {
                int len = format.length();
                int idx = 0;
                for (;;)
                {
                    if (idx == len)
                        return;
                    var ch = format.charAt(idx++);
                    if (ch != '{')
                    {
                        sb.append(ch);
                        if (ch == '}' && format.charAt(idx++) != '}')
                            break;
                        continue;
                    }
                    ch = format.charAt(idx++);
                    if (ch == '{')
                    {
                        sb.append(ch);
                        continue;
                    }

                    int argIndex = 0;
                    for (;;)
                    {
                        if (ch < '0' || ch > '9')
                            break;
                        argIndex = (argIndex * 10) + (int) (ch - '0');
                        ch = format.charAt(idx++);
                    }
                    if (argIndex >= args.Length)
                        break;
                    var argObject = args[argIndex];

                    while (ch == ' ')
                        ch = format.charAt(idx++);

                    int argAlign = 0;
                    if (ch == ',')
                    {
                        do
                            ch = format.charAt(idx++);
                        while (ch == ' ');

                        int multiplier = 1;
                        if (ch == '-')
                        {
                            multiplier = -1;
                            ch = format.charAt(idx++);
                        }
                        if (ch < '0' || ch > '9')
                            break;
                        for (;;)
                        {
                            argAlign = (argAlign * 10) + multiplier * (int) (ch - '0');
                            ch = format.charAt(idx++);
                            if (ch < '0' || ch > '9')
                                break;
                        }

                        while (ch == ' ')
                            ch = format.charAt(idx++);
                    }

                    string argFormat = null;
                    if (ch == ':')
                    {
                        argFormat = "";
                        int idx0 = idx;
                        for (;;)
                        {
                            idx = format.indexOf('}', idx0);
                            if (idx == -1)
                                break;
                            argFormat += format.substring(idx0, idx);
                            if (++idx == len || format.charAt(idx) != '}')
                                break;
                            argFormat += '}';
                            idx0 = idx + 1;
                        }
                        if (idx == -1)
                            break;
                    }
                    else if (ch != '}')
                        break;

                    string argText = null;
                    if (customFormatter != null)
                        argText = customFormatter.Format(argFormat, argObject, provider);
                    if (argText == null)
                    {
                        if (argObject is System.IFormattable argFormattable)
                            argText = argFormattable.ToString(argFormat, provider);
                        else if (argObject != null)
                            argText = argObject.ToString();
                    }

                    if (argText == null)
                        argText = "";
                    int argLength = argText.Length;

                    while (argAlign > 0 && argAlign-- > argLength)
                        sb.append(' ');
                    sb.append(argText);
                    while (argAlign < 0 && argAlign++ < -argLength)
                        sb.append(' ');
                }
            }
            catch (System.Exception e)
            {
                throw new System.FormatException((string) (object) format, e);
            }
            throw new System.FormatException((string) (object) format);
        }

        //
        // Join
        //

        public delegate object JoinIterator(ref bool done);
        public static string Join(string separator, JoinIterator iterator)
        {
            if (separator == null)
                separator = "";
            bool first = true;
            bool done = false;

            var sb = new java.lang.StringBuilder();
            for (;;)
            {
                object objNext = iterator(ref done);
                if (done)
                    break;

                string strNext = objNext as string;
                if (strNext == null)
                {
                    if (objNext == null)
                        continue;
                    strNext = objNext.ToString();
                    if (strNext == null)
                        continue;
                }

                if (first)
                    first = false;
                else
                    sb.append(separator);
                sb.append(strNext);
            }

            return sb.ToString();
        }

    }

}
