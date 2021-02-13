
using System;

namespace system
{

    public static class ParseNumbers
    {

        public static string IntToString(int l, int radix, int width, char paddingChar, int flags)
        {
            if (flags != 0)
                throw new ArgumentException(nameof(flags));
            if (paddingChar != '0')
                throw new ArgumentException(nameof(paddingChar));
            if (width <= 0)
                throw new ArgumentException(nameof(width));

            return java.lang.String.format("%0" + width.ToString() + "d",
                                           new object[] { java.lang.Integer.valueOf(l) });
        }

        public static string FormatNumber(java.lang.String format, IFormatProvider provider,
                                          java.lang.Number number)
        {
            if (provider != null)
            {
                if (    (provider is System.Globalization.NumberFormatInfo)
                     || null != provider.GetFormat(
                                    typeof(System.Globalization.NumberFormatInfo)))
                {
                    throw new PlatformNotSupportedException("provider with a NumberFormatInfo");
                }
            }

            int len = format.length();

            bool explicit_width = false;
            int width = -1;
            if (len == 1)
                width = 0;
            else if (len > 1 && len <= 3)
            {
                int n10 = 0;
                int n01 = (int) (format.charAt(1) - '0');
                if (len > 2)
                {
                    n10 = n01;
                    n01 = (int) (format.charAt(2) - '0');
                }
                if (n01 >= 0 && n01 <= 9 && n10 >= 0 && n10 <= 9)
                    width = n10 * 10 + n01;
            }

            char c = format.charAt(0);
            bool isInt = (    number is java.lang.Byte    || number is java.lang.Short
                           || number is java.lang.Integer || number is java.lang.Long);
            char pfx = (char) 0;
            string sfx = "";

            switch (Char.ToUpperInvariant(c))
            {
                case 'X':
                    if (width != 0)
                        pfx = '0';
                    if (! isInt)
                        c = (char) 0;
                    else if (number is java.lang.Byte)
                        number = java.lang.Integer.valueOf(number.byteValue() & 0xFF);
                    else if (number is java.lang.Short)
                        number = java.lang.Integer.valueOf(number.shortValue() & 0xFFFF);
                    break;

                case 'D':
                    c = isInt ? 'd' : (char) 0;
                    pfx = '0';
                    break;

                case 'N':
                    if (isInt)
                    {
                        c = 'd';
                        sfx = "." + new string('0', width);
                        width = 0;
                    }
                    else
                    {
                        c = 'f';
                        explicit_width = true;  // set width even if zero
                    }
                    pfx = ',';
                    break;

                case 'P':
                    number = java.lang.Double.valueOf(number.doubleValue() * 100.0);
                    pfx = ',';
                    sfx = "%%";
                    if (len == 1)
                        width = 2;
                    goto case 'F';

                case 'F':
                    c = 'f';
                    goto case 'G';

                case 'E': case 'G':
                    if (isInt)
                    {
                        number = java.lang.Double.valueOf(number.doubleValue());
                        isInt = false;
                    }
                    break;

                default:
                    c = (char) 0;
                    break;
            }

            if (c != 0 && width != -1)
            {
                var format1 = "%";
                if (pfx != 0)
                    format1 += pfx;
                if (explicit_width || width != 0)
                {
                    if (! isInt)
                        format1 += ".";
                    format1 += width.ToString();
                }
                format1 += c + sfx;
                return java.lang.String.format(format1, new object[] { number });
            }

            return (string) (object) format;
        }

        public static string DoubleToString(double v)
        {
            var formatter = (java.text.DecimalFormat) TlsDecimalFormatter.get();
            if (formatter == null)
            {
                formatter = new java.text.DecimalFormat("0");
                // 340 is DecimalFormat.DOUBLE_FRACTION_DIGITS
                formatter.setMaximumFractionDigits(340);
                TlsDecimalFormatter.set(formatter);
            }
            return formatter.format(v);
        }

        [java.attr.RetainType] static java.lang.ThreadLocal TlsDecimalFormatter =
                                                            new java.lang.ThreadLocal();

    }

}

