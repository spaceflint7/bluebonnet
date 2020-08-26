
namespace system
{

    public static class String
    {

        public static string Empty = "";

        //
        // New
        //

        [java.attr.RetainName]
        public static string New(char[] value)
        {
            ThrowHelper.ThrowIfNull(value);
            return new string(value);
        }

        [java.attr.RetainName]
        public static string New(char[] value, int startIndex, int length)
        {
            ThrowHelper.ThrowIfNull(value);
            if (startIndex < 0 || length < 0 || startIndex + length > value.Length)
                throw new System.ArgumentOutOfRangeException();
            return new string(value, startIndex, length);
        }

        [java.attr.RetainName]
        public static string New(system.Span<string> span)
        {
            if (span.Array((java.lang.Class) typeof(char[])) is char[] charArray)
            {
                // system.String(char*) constructor takes a pointer to
                // a null terminated string, and discards the null
                var n = charArray.Length;
                while (--n > 0 && charArray[n] == 0)
                    ;
                n++;
                return new string(charArray, 0, n);
            }
            throw new System.ArgumentException();
        }

        [java.attr.RetainName]
        public static string New(char value, int repeatCount)
        {
            var buffer = new char[repeatCount];
            for (int i = 0; i < repeatCount; i++)
                buffer[i] = value;
            return new string(buffer);
        }

        //
        // Concat object
        //

        public static string Concat(object[] objs)
        {
            if (objs == null)
                throw new System.ArgumentNullException();
            var sb = new java.lang.StringBuilder();
            int n = objs.Length;
            for (int i = 0; i < n; i++)
            {
                var o = objs[i];
                if (o != null)
                    sb.append(o.ToString());
            }
            return sb.ToString();
        }

        public static string Concat(object objA, object objB, object objC, object objD)
        {
            var sb = new java.lang.StringBuilder();
            if (objA != null)
                sb.append(objA.ToString());
            if (objB != null)
                sb.append(objB.ToString());
            if (objC != null)
                sb.append(objC.ToString());
            if (objC != null)
                sb.append(objD.ToString());
            return sb.ToString();
        }

        public static string Concat(object objA, object objB, object objC)
            => Concat(objA, objB, objC, null);

        public static string Concat(object objA, object objB) => Concat(objA, objB, null, null);

        public static string Concat(object objA) => (objA != null ? objA.ToString() : "");

        //
        // Concat string
        //

        public static string Concat(params string[] strs)
        {
            if (strs == null)
                throw new System.ArgumentNullException();
            var sb = new java.lang.StringBuilder();
            int n = strs.Length;
            for (int i = 0; i < n; i++)
            {
                var s = strs[i];
                if (s != null)
                    sb.append(s);
            }
            return sb.ToString();
        }

        public static string Concat(string strA, string strB, string strC, string strD)
        {
            var sb = new java.lang.StringBuilder();
            if (strA != null)
                sb.append(strA);
            if (strB != null)
                sb.append(strB);
            if (strC != null)
                sb.append(strC);
            if (strD != null)
                sb.append(strD);
            return sb.ToString();
        }

        public static string Concat(string strA, string strB, string strC)
            => Concat(strA, strB, strC, null);

        public static string Concat(string strA, string strB) => Concat(strA, strB, null, null);

        //
        // Concat enumerator
        //

        #if false
        [java.attr.RetainName]
        public static string Concat<T>(System.Collections.Generic.IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                throw new System.ArgumentNullException();
            var sb = new java.lang.StringBuilder();
            using (var enumerator = enumerable.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var e = enumerator.Current;
                    if (e != null)
                    {
                        string s = e.ToString();
                        if (s != null)
                            sb.append(s);
                    }
                }
            }
            return sb.ToString();
        }

        //[java.attr.RetainName]
        public static string Concat(System.Collections.Generic.IEnumerable<string> enumerable)
        {
            if (enumerable == null)
                throw new System.ArgumentNullException();
            var sb = new java.lang.StringBuilder();
            using (var enumerator = enumerable.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var s = enumerator.Current;
                    if (s != null)
                        sb.append(s);
                }
            }
            return sb.ToString();
        }
        #endif

        //
        // Join
        //

        public static string Join(string separator, System.Collections.Generic.IEnumerable<string> values)
        {
            var @enum = values.GetEnumerator();
            return system.text.StringBuilder.Join(separator, (ref bool done) =>
            {
                if (@enum.MoveNext())
                    return @enum.Current;
                done = true;
                return null;
            });
        }

        public static string Join<T>(string separator, System.Collections.Generic.IEnumerable<T> values)
        {
            var @enum = values.GetEnumerator();
            return system.text.StringBuilder.Join(separator, (ref bool done) =>
            {
                if (@enum.MoveNext())
                {
                    string value = @enum.Current.ToString();
                    if (value != null)
                        return value;
                }
                done = true;
                return null;
            });
        }

        //
        // CopyTo
        //

        public static void CopyTo(java.lang.String str, int sourceIndex,
                                  char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
                throw new System.ArgumentNullException();
            if (    count < 0 || sourceIndex < 0 || destinationIndex < 0
                 || count > str.length() - sourceIndex
                 || destinationIndex > destination.Length - count)
                throw new System.ArgumentOutOfRangeException();
            str.getChars(sourceIndex, sourceIndex + count, destination, destinationIndex);
        }

        //
        // Format
        //

        public static string Format(System.IFormatProvider provider,
                                    java.lang.String format, object[] args)
        {
            var sb = new java.lang.StringBuilder();
            system.text.StringBuilder.AppendFormatHelper(sb, provider, format, args);
            return sb.ToString();
        }

        public static string Format(System.IFormatProvider provider, java.lang.String format, object arg0)
            => Format(provider, format, new object[] { arg0 });

        public static string Format(System.IFormatProvider provider, java.lang.String format, object arg0, object arg1)
            => Format(provider, format, new object[] { arg0, arg1 });

        public static string Format(System.IFormatProvider provider, java.lang.String format, object arg0, object arg1, object arg2)
            => Format(provider, format, new object[] { arg0, arg1, arg2 });

        public static string Format(java.lang.String format, object arg0)
            => Format(null, format, new object[] { arg0 });

        public static string Format(java.lang.String format, object arg0, object arg1)
            => Format(null, format, new object[] { arg0, arg1 });

        public static string Format(java.lang.String format, object arg0, object arg1, object arg2)
            => Format(null, format, new object[] { arg0, arg1, arg2 });

        public static string Format(java.lang.String format, object[] args)
            => Format(null, format, args);

        //
        // operators
        //

        public static bool op_Equality(string strA, string strB)
            => ((object) strA).Equals(strB);

        public static bool op_Inequality(string strA, string strB)
            => (! ((object) strA).Equals(strB));

        //
        // Equals
        //

        public static bool Equals(java.lang.String a, java.lang.String b)
            => (a == null) ? (b == null) : a.Equals(b);



        //
        // IndexOf (char, ordinal)
        //

        public static int IndexOf(java.lang.String str, char ch) => str.indexOf(ch, 0);

        public static int IndexOf(java.lang.String str, char ch, int idx)
        {
            ThrowIfBadIndex(str, idx);
            return str.indexOf(ch, idx);
        }

        public static int IndexOf(string str, char ch, int idx, int len)
        {
            return system.globalization.CompareInfo.IndexOfChar(str, ch, idx, len,
                        System.Globalization.CompareOptions.Ordinal, null);
        }



        //
        // IndexOf (string, culture-sensitive)
        //

        public static int IndexOf(string str, string val)
        {
            return system.globalization.CompareInfo.IndexOfString(
                        str, val, /* startIndex */ 0,
                        System.Globalization.CompareOptions.None,
                        system.globalization.CompareInfo.CurrentCompareInfo);
        }

        public static int IndexOf(string str, string val, int idx)
        {
            return system.globalization.CompareInfo.IndexOfString(
                        str, val, /* startIndex */ idx,
                        System.Globalization.CompareOptions.None,
                        system.globalization.CompareInfo.CurrentCompareInfo);
        }

        public static int IndexOf(string str, string val, int idx, int len)
        {
            return system.globalization.CompareInfo.IndexOfString(
                        str, val, /* startIndex */ idx, /* count */ len,
                        System.Globalization.CompareOptions.None,
                        system.globalization.CompareInfo.CurrentCompareInfo);
        }



        //
        // IndexOf (string, with string comparison option)
        //

        public static int IndexOf(string str, string val, System.StringComparison option)
            => IndexOf(str, val, /* startIndex */ 0, option);

        public static int IndexOf(string str, string val, int startIndex,
                                  System.StringComparison option)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(option);
            return system.globalization.CompareInfo.IndexOfString(
                        str, val, startIndex, compareOption, compareInfo);
        }

        public static int IndexOf(string str, string val, int startIndex, int count,
                                  System.StringComparison option)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(option);
            return system.globalization.CompareInfo.IndexOfString(
                        str, val, startIndex, count, compareOption, compareInfo);
        }



        //
        // LastIndexOf (char, ordinal)
        //

        public static int LastIndexOf(java.lang.String str, char ch) => str.lastIndexOf(ch);

        public static int LastIndexOf(java.lang.String str, char ch, int idx)
        {
            ThrowIfBadIndex(str, idx);
            return str.lastIndexOf(ch, idx);
        }

        public static int LastIndexOf(string str, char ch, int idx, int len)
        {
            return system.globalization.CompareInfo.LastIndexOfChar(str, ch, idx, len,
                        System.Globalization.CompareOptions.Ordinal, null);
        }



        //
        // LastIndexOf (string, culture-sensitive)
        //

        public static int LastIndexOf(string str, string val)
        {
            return system.globalization.CompareInfo.LastIndexOfString(
                        str, val, /* startIndex */ false, 0,
                        System.Globalization.CompareOptions.None,
                        system.globalization.CompareInfo.CurrentCompareInfo);
        }

        public static int LastIndexOf(string str, string val, int idx)
        {
            return system.globalization.CompareInfo.LastIndexOfString(
                        str, val, /* startIndex */ true, idx,
                        System.Globalization.CompareOptions.None,
                        system.globalization.CompareInfo.CurrentCompareInfo);
        }

        public static int LastIndexOf(string str, string val, int idx, int len)
        {
            return system.globalization.CompareInfo.LastIndexOfString(
                        str, val, /* startIndex */ idx, /* count */ len,
                        System.Globalization.CompareOptions.None,
                        system.globalization.CompareInfo.CurrentCompareInfo);
        }



        //
        // IndexOf (string, with string comparison option)
        //

        public static int LastIndexOf(string str, string val, System.StringComparison option)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(option);
            return system.globalization.CompareInfo.LastIndexOfString(
                        str, val, /* startIndex */ false, 0, compareOption, compareInfo);
        }

        public static int LastIndexOf(string str, string val, int startIndex,
                                      System.StringComparison option)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(option);
            return system.globalization.CompareInfo.LastIndexOfString(
                        str, val, /* startIndex */ true, startIndex, compareOption, compareInfo);
        }

        public static int LastIndexOf(string str, string val, int startIndex, int count,
                                      System.StringComparison option)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(option);
            return system.globalization.CompareInfo.LastIndexOfString(
                        str, val, startIndex, count, compareOption, compareInfo);
        }



        //
        // StringComparisonToCompareArguments
        //

        static (system.globalization.CompareInfo, System.Globalization.CompareOptions)
                    StringComparisonToCompareArguments(System.StringComparison option)
        {
            system.globalization.CompareInfo compareInfo;
            System.Globalization.CompareOptions compareOption;

            switch (option)
            {
                case System.StringComparison.CurrentCulture:
                    compareInfo = system.globalization.CompareInfo.CurrentCompareInfo;
                    compareOption = System.Globalization.CompareOptions.None;
                    break;
                case System.StringComparison.CurrentCultureIgnoreCase:
                    compareInfo = system.globalization.CompareInfo.CurrentCompareInfo;
                    compareOption = System.Globalization.CompareOptions.IgnoreCase;
                    break;
                case System.StringComparison.InvariantCulture:
                    compareInfo = system.globalization.CompareInfo.InvariantCompareInfo;
                    compareOption = System.Globalization.CompareOptions.None;
                    break;
                case System.StringComparison.InvariantCultureIgnoreCase:
                    compareInfo = system.globalization.CompareInfo.InvariantCompareInfo;
                    compareOption = System.Globalization.CompareOptions.IgnoreCase;
                    break;
                case System.StringComparison.Ordinal:
                    compareInfo = null;
                    compareOption = System.Globalization.CompareOptions.Ordinal;
                    break;
                case System.StringComparison.OrdinalIgnoreCase:
                    compareInfo = null;
                    compareOption = System.Globalization.CompareOptions.OrdinalIgnoreCase;
                    break;
                default:
                    throw new System.ArgumentException();
            }

            return (compareInfo, compareOption);
        }



        //
        // IndexOfAny
        //



        //
        //
        //

        // throws java.lang.StringIndexOutOfBounds -> System.IndexOutOfRangeException
        public static char get_Chars(java.lang.String str, int idx) => str.charAt(idx);

        public static string Substring(java.lang.String str, int idx)
        {
            ThrowIfBadIndex(str, idx);
            return str.substring(idx);
        }
        public static string Substring(java.lang.String str, int idx, int len)
        {
            ThrowIfBadIndexOrCount(str, idx, len);
            return str.substring(idx, idx + len);
        }

        public static bool StartsWith(java.lang.String str, string pfx)
        {
            if (pfx == null)
                throw new System.ArgumentNullException();
            return str.startsWith(pfx);
        }

        public static bool EndsWith(java.lang.String str, string sfx)
        {
            if (sfx == null)
                throw new System.ArgumentNullException();
            return str.endsWith(sfx);
        }

        //
        //
        //

        public static int get_Length(java.lang.String str) => str.length();

        public static bool IsNullOrEmpty(java.lang.String str) => (str == null || str.length() == 0);

        public static char[] ToCharArray(java.lang.String str) => str.toCharArray();

        //
        // Trim
        //

        public static string Trim(java.lang.String str)      => TrimWhiteSpace(str, true, true);
        public static string TrimEnd(java.lang.String str)   => TrimWhiteSpace(str, false, true);
        public static string TrimStart(java.lang.String str) => TrimWhiteSpace(str, true, false);

        public static string TrimWhiteSpace(java.lang.String str, bool trimStart, bool trimEnd)
        {
            int n = str.length();
            int i = 0;
            if (trimStart)
            {
                while (i < n && Char.IsWhiteSpace(str.charAt(i)))
                    i++;
            }
            if (trimEnd)
            {
                while (n-- > i && Char.IsWhiteSpace(str.charAt(n)))
                    ;
                n++;
            }
            return str.substring(i, n);
        }

        //
        // internal helper methods
        //

        public static bool UseRandomizedHashing() => false;



        //
        // throw helpers
        //

        static void ThrowIfBadIndex(java.lang.String str, int idx)
        {
            if (idx < 0 || idx >= str.length())
                throw new System.ArgumentOutOfRangeException();
        }

        static void ThrowIfBadIndexOrCount(java.lang.String str, int idx, int num)
        {
            if (idx < 0 || num < 0 || idx + num > str.length())
                throw new System.ArgumentOutOfRangeException();
        }



        //
        // create a wrapper that can cast string to
        // IComparable<string> and IEquatable<string>
        //

        public static object CreateWrapper(object maybeString, System.Type castToType)
        {
            if (    maybeString is java.lang.String
                 && (    object.ReferenceEquals(castToType, cachedComparableString)
                      || object.ReferenceEquals(castToType, cachedEquatableString)))
            {
                return new Wrapper(maybeString);
            }
            return null;
        }

        [java.attr.RetainType] private static readonly System.Type cachedComparableString;
        [java.attr.RetainType] private static readonly System.Type cachedEquatableString;

        class Wrapper : System.IComparable<string>, System.IEquatable<string>
        {
            [java.attr.RetainType] private java.lang.String str;
            public Wrapper(object _s) => str = (java.lang.String) (object) _s;
            // CompareTo implementation is wrong, should be culture sensitive
            public int CompareTo(string other)
                => str.compareTo((java.lang.String) (object) other);
            // Equals is not sensitive to culture or case
            public bool Equals(string other)
                => str.@equals((java.lang.String) (object) other);
        }



        //
        //
        //



        static String()
        {
            system.Util.DefineException(
                (java.lang.Class) typeof(java.lang.StringIndexOutOfBoundsException),
                (exc) => new System.IndexOutOfRangeException(exc.getMessage())
            );

            cachedComparableString = typeof(System.IComparable<string>);
            cachedEquatableString = typeof(System.IEquatable<string>);
        }

    }
}



namespace java.lang
{
    [java.attr.Discard] // discard in output
    public abstract class String
    {
        public abstract int length();
        public abstract int indexOf(int c);
        public abstract int indexOf(int c, int idx);
        public abstract int indexOf(string s);
        public abstract int indexOf(string s, int idx);
        public abstract int lastIndexOf(int c);
        public abstract int lastIndexOf(int c, int idx);
        public abstract int lastIndexOf(string s);
        public abstract int lastIndexOf(string s, int idx);
        public abstract char charAt(int idx);
        public abstract string substring(int idx);
        public abstract string substring(int idx1, int idx2);
        public abstract bool startsWith(string s);
        public abstract bool startsWith(string s, int idx);
        public abstract bool startsWith(String s, int idx);
        public abstract bool endsWith(string s);
        public abstract bool endsWith(String s);
        public abstract bool regionMatches(int toffset, string other, int ooffset, int len);
        public abstract bool regionMatches(int toffset, java.lang.String other, int ooffset, int len);
        public abstract bool regionMatches(bool ignoreCase, int toffset, string other, int ooffset, int len);
        public abstract bool regionMatches(bool ignoreCase, int toffset, java.lang.String other, int ooffset, int len);
        public abstract char[] toCharArray();
        public abstract void getChars(int srcBegin, int srcEnd, char[] dst, int dstBegin);
        public abstract string toUpperCase(java.util.Locale locale);
        public abstract string toLowerCase(java.util.Locale locale);
        public abstract int compareTo(java.lang.String other);
        public abstract bool equals(java.lang.String other);
        [java.attr.Discard] extern public static string format(string fmt, object[] args);
        [java.attr.Discard] extern public static string valueOf(char c);
    }
}
