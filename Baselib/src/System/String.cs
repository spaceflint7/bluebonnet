
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
        // ToLower, ToUpper
        //

        public static string ToLower(string self)
            => system.globalization.TextInfo.CurrentTextInfo.ToLower(self);

        public static string ToLowerInvariant(string self)
            => system.globalization.TextInfo.InvariantTextInfo.ToLower(self);

        public static string ToUpper(string self)
            => system.globalization.TextInfo.CurrentTextInfo.ToUpper(self);

        public static string ToUpperInvariant(string self)
            => system.globalization.TextInfo.InvariantTextInfo.ToUpper(self);

        //
        // Concat object
        //

        static void AppendIfNotNull(java.lang.StringBuilder sb, object obj)
        {
            if (obj is string)
            {
                sb.append((string) obj);
            }
            else if (obj != null)
            {
                var str = obj.ToString();
                if (str != null)
                    sb.append(str);
            }
        }

        public static string Concat(object[] objs)
        {
            ThrowHelper.ThrowIfNull(objs);
            var sb = new java.lang.StringBuilder();
            foreach (var obj in objs)
                AppendIfNotNull(sb, obj);
            return sb.ToString();
        }

        public static string Concat(object objA, object objB, object objC, object objD)
        {
            var sb = new java.lang.StringBuilder();
            AppendIfNotNull(sb, objA);
            AppendIfNotNull(sb, objB);
            AppendIfNotNull(sb, objC);
            AppendIfNotNull(sb, objD);
            return sb.ToString();
        }

        public static string Concat(object objA, object objB, object objC)
            => Concat(objA, objB, objC, null);

        public static string Concat(object objA, object objB) => Concat(objA, objB, null, null);

        public static string Concat(object objA) => (objA != null ? objA.ToString() : "");

        //
        // Concat string
        //

        public static string Concat(string[] strs)
        {
            ThrowHelper.ThrowIfNull(strs);
            var sb = new java.lang.StringBuilder();
            foreach (var str in strs)
                AppendIfNotNull(sb, str);
            return sb.ToString();
        }

        public static string Concat(string strA, string strB, string strC, string strD)
        {
            var sb = new java.lang.StringBuilder();
            AppendIfNotNull(sb, strA);
            AppendIfNotNull(sb, strB);
            AppendIfNotNull(sb, strC);
            AppendIfNotNull(sb, strD);
            return sb.ToString();
        }

        public static string Concat(string strA, string strB, string strC)
            => Concat(strA, strB, strC, null);

        public static string Concat(string strA, string strB) => Concat(strA, strB, null, null);

        //
        // Concat enumerator
        //

        public static string Concat<T>(System.Collections.Generic.IEnumerable<T> enumerable)
        {
            ThrowHelper.ThrowIfNull(enumerable);
            var sb = new java.lang.StringBuilder();
            foreach (var obj in enumerable)
                AppendIfNotNull(sb, obj);
            return sb.ToString();
        }

        public static string Concat(System.Collections.Generic.IEnumerable<string> enumerable)
            => Concat<string>(enumerable);

        //
        // Split
        //

        public static string[] Split(string str, char[] separator)
            => InternalSplit((java.lang.String) (object) str, separator, System.Int32.MaxValue, System.StringSplitOptions.None);

        public static string[] Split(string str, string[] separator)
            => InternalSplit((java.lang.String) (object) str, separator, System.Int32.MaxValue, System.StringSplitOptions.None);

        public static string[] Split(string str, char[] separator, int count)
            => InternalSplit((java.lang.String) (object) str, separator, count, System.StringSplitOptions.None);

        public static string[] Split(string str, string[] separator, int count)
            => InternalSplit((java.lang.String) (object) str, separator, count, System.StringSplitOptions.None);

        public static string[] Split(string str, char[] separator, System.StringSplitOptions options)
            => InternalSplit((java.lang.String) (object) str, separator, System.Int32.MaxValue, options);

        public static string[] Split(string str, string[] separator, System.StringSplitOptions options)
            => InternalSplit((java.lang.String) (object) str, separator, System.Int32.MaxValue, options);

        public static string[] Split(string str, char[] separator, int count, System.StringSplitOptions options)
            => InternalSplit((java.lang.String) (object) str, separator, count, options);

        public static string[] Split(string str, string[] separator, int count, System.StringSplitOptions options)
            => InternalSplit((java.lang.String) (object) str, separator, count, options);

        private static string[] InternalSplit(java.lang.String str,
                                              object separator, int count,
                                              System.StringSplitOptions options)
        {
            if (count < 0)
                throw new System.ArgumentOutOfRangeException();

            bool omit = (options == System.StringSplitOptions.None) ? false
                      : (options == System.StringSplitOptions.RemoveEmptyEntries) ? true
                      : throw new System.ArgumentException();

            var emptyArray = new string[0];
            if (count == 0)
                return emptyArray;

            int length = str.length();
            var list = new java.util.ArrayList();

            if (separator is char[] charSeparator)
                SplitCharacter(str, length, list, count, omit, charSeparator);
            else if (separator is java.lang.String[] strSeparator)
                SplitString(str, length, list, count, omit, strSeparator);
            else // assuming separator is null
                SplitWhiteSpace(str, length, list, count, omit);

            return (string[]) list.toArray(emptyArray);
        }

        private static void SplitWhiteSpace(java.lang.String str, int length,
                                            java.util.ArrayList list, int maxCount,
                                            bool omit)
        {
            int listCount = 0;
            int lastIndex = -1;
            for (int index = 0; index < length; index++)
            {
                if (! Char.IsWhiteSpace(str.charAt(index)))
                    continue;
                if (index == ++lastIndex && omit)
                    continue;
                if (++listCount == maxCount)
                {
                    list.add(str.substring(lastIndex));
                    return;
                }
                list.add(str.substring(lastIndex, index));
                lastIndex = index;
            }
            if (length != ++lastIndex || ! omit)
                list.add(str.substring(lastIndex));
        }

        private static void SplitCharacter(java.lang.String str, int length,
                                           java.util.ArrayList list, int maxCount,
                                           bool omit, char[] anyOf)
        {
            int listCount = 0;
            int lastIndex = -1;
            for (int index = 0; index < length; index++)
            {
                if (! CharAtIsAnyOf(str, index, anyOf))
                    continue;
                if (index == ++lastIndex && omit)
                    continue;
                if (++listCount == maxCount)
                {
                    list.add(str.substring(lastIndex));
                    return;
                }
                list.add(str.substring(lastIndex, index));
                lastIndex = index;
            }
            if (length != ++lastIndex || ! omit)
                list.add(str.substring(lastIndex));
        }

        private static void SplitString(java.lang.String str, int length,
                                        java.util.ArrayList list, int maxCount,
                                        bool omit, java.lang.String[] anyOfStr)
        {
            int anyOfNum = anyOfStr.Length;
            char[] anyOfChar = new char[anyOfNum];
            for (int index = 0; index < anyOfNum; index++)
            {
                var s = anyOfStr[index];
                anyOfChar[index] =
                    (s != null && s.length() > 0) ? s.charAt(index) : '\0';
            }

            int listCount = 0;
            int lastIndex = 0;
            for (int index = 0; index < length; )
            {
                int len = StrAtIsAnyOf(str, index, anyOfNum, anyOfChar, anyOfStr);
                if (len == -1)
                    index++;
                else
                {
                    if (index != lastIndex || ! omit)
                    {
                        if (++listCount == maxCount)
                        {
                            list.add(str.substring(lastIndex));
                            return;
                        }
                        list.add(str.substring(lastIndex, index));
                    }
                    lastIndex = (index += len);
                }
            }
            if (length != lastIndex || ! omit)
                list.add(str.substring(lastIndex));
        }

        private static bool CharAtIsAnyOf(java.lang.String str, int idx, char[] anyOf)
        {
            char ch1 = str.charAt(idx);
            foreach (var ch2 in anyOf)
                if (ch2 == ch1)
                    return true;
            return false;
        }

        private static int StrAtIsAnyOf(java.lang.String str, int idx, int anyOfNum,
                                        char[] anyOfChar, java.lang.String[] anyOfStr)
        {
            char ch = str.charAt(idx);
            for (int i = 0; i < anyOfNum; i++)
            {
                if (anyOfChar[i] == ch)
                {
                    int len = anyOfStr[i].length();
                    if (str.regionMatches(idx, anyOfStr[i], 0, len))
                        return len;
                }
            }
            return -1;
        }

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
        // Replace
        //

        public static string Replace(java.lang.String str, char oldChar, char newChar)
            => str.replace(oldChar, newChar);

        public static string Replace(java.lang.String str, string oldStr, string newStr)
        {
            ThrowHelper.ThrowIfNull(oldStr);
            int lenOld = ((java.lang.String) (object) oldStr).length();
            if (lenOld == 0)
                throw new System.ArgumentException();

            int idx1 = str.indexOf(oldStr, 0);
            if (idx1 == -1)
                return (string) (object) str;
            int idx0 = 0;
            int len = str.length();
            var sb = new java.lang.StringBuilder();
            for (;;)
            {
                sb.append(str.substring(idx0, idx1));
                if (newStr != null)
                    sb.append(newStr);
                if (    (idx0 = idx1 + lenOld) >= len
                     || (idx1 = str.indexOf(oldStr, idx0)) == -1)
                {
                    sb.append(str.substring(idx0));
                    return sb.ToString();
                }
            }
        }

        //
        // Copy
        //

        public static string Copy(java.lang.String str)
        {
            ThrowHelper.ThrowIfNull(str);
            return java.lang.String.valueOf(str.toCharArray());
        }

        public static string Intern(java.lang.String str)
        {
            ThrowHelper.ThrowIfNull(str);
            return str.intern();
        }

        //
        // CopyTo
        //

        public static void CopyTo(java.lang.String str, int sourceIndex,
                                  char[] destination, int destinationIndex, int count)
        {
            ThrowHelper.ThrowIfNull(destination);
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

        public static bool op_Equality(java.lang.String strA, java.lang.String strB)
            => Equals(strA, strB);

        public static bool op_Inequality(java.lang.String strA, java.lang.String strB)
            => ! Equals(strA, strB);

        //
        // Equals
        //

        public static bool Equals(java.lang.String a, java.lang.String b)
            => (a == null) ? (b == null) : a.Equals(b);

        public static bool Equals(string a, string b, System.StringComparison comparisonType)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(comparisonType);
            return 0 == system.globalization.CompareInfo.CompareString(
                                (string) (object) a, b, compareOption, compareInfo);
        }

        //
        // CompareOrdinal
        //

        public static int CompareOrdinal(java.lang.String strA, java.lang.String strB)
        {
            if (object.ReferenceEquals(strA, strB))
                return 0;
            if (strA == null)
                return -1;
            if (strB == null)
                return 1;
            return strA.compareTo(strB);
        }

        public static int CompareOrdinal(java.lang.String strA, int indexA,
                                         java.lang.String strB, int indexB, int length)
        {
            if (object.ReferenceEquals(strA, strB))
                return 0;
            if (strA == null)
                return -1;
            if (strB == null)
                return 1;
            int endIndexA, endIndexB;
            if (    indexA < 0 || (endIndexA = indexA + length) > strA.length()
                 || indexB < 0 || (endIndexB = indexB + length) > strB.length())
                throw new System.ArgumentOutOfRangeException();
            return           ((java.lang.String) (object) strA.substring(indexA, endIndexA))
                   .compareTo((java.lang.String) (object) strB.substring(indexB, endIndexB));
        }



        //
        // Compare without length
        //

        public static int Compare(string strA, string strB)
        {
            return system.globalization.CompareInfo.CompareString(strA, strB,
                        System.Globalization.CompareOptions.None,
                        system.globalization.CompareInfo.CurrentCompareInfo);
        }

        public static int Compare(string strA, string strB, bool ignoreCase)
        {
            return system.globalization.CompareInfo.CompareString(strA, strB,
                        ignoreCase ? System.Globalization.CompareOptions.IgnoreCase
                                   : System.Globalization.CompareOptions.None,
                        system.globalization.CompareInfo.CurrentCompareInfo);
        }

        public static int Compare(string strA, string strB, System.StringComparison option)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(option);
            return system.globalization.CompareInfo.CompareString(strA, strB, compareOption, compareInfo);
        }

        public static int Compare(string strA, string strB,
                                  bool ignoreCase, system.globalization.CultureInfo culture)
        {
            return system.globalization.CompareInfo.CompareString(strA, strB,
                        ignoreCase ? System.Globalization.CompareOptions.IgnoreCase
                                   : System.Globalization.CompareOptions.None,
                        culture.CompareInfo);
        }

        public static int Compare(string strA, string strB,
                                  system.globalization.CultureInfo culture,
                                  System.Globalization.CompareOptions options)
        {
            return system.globalization.CompareInfo.CompareString(strA, strB,
                        options, culture.CompareInfo);
        }

        //
        // Compare with length
        //

        public static int Compare(string strA, int indexA, string strB, int indexB, int length)
            => Compare(strA, indexA, strB, indexB, length,
                       system.globalization.CompareInfo.CurrentCompareInfo,
                       System.Globalization.CompareOptions.None);

        public static int Compare(string strA, int indexA, string strB, int indexB, int length,
                                  bool ignoreCase)
            => Compare(strA, indexA, strB, indexB, length,
                       system.globalization.CompareInfo.CurrentCompareInfo,
                       System.Globalization.CompareOptions.IgnoreCase);

        public static int Compare(string strA, int indexA, string strB, int indexB, int length,
                                  System.StringComparison comparisonType)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(comparisonType);
            return Compare(strA, indexA, strB, indexB, length, compareInfo, compareOption);
        }

        public static int Compare(string strA, int indexA, string strB, int indexB, int length,
                                  bool ignoreCase, system.globalization.CultureInfo culture)
            => Compare(strA, indexA, strB, indexB, length, culture.CompareInfo,
                        ignoreCase ? System.Globalization.CompareOptions.IgnoreCase
                                   : System.Globalization.CompareOptions.None);

        public static int Compare(string strA, int indexA, string strB, int indexB, int length,
                                  system.globalization.CultureInfo culture,
                                  System.Globalization.CompareOptions options)
            => Compare(strA, indexA, strB, indexB, length, culture.CompareInfo, options);

        private static int Compare(string strA, int indexA, string strB, int indexB, int length,
                                  system.globalization.CompareInfo compareInfo,
                                  System.Globalization.CompareOptions compareOptions)
        {
            int lengthA = length;
            if (strA != null)
            {
                int maxlenA = ((java.lang.String) (object) strA).length() - indexA;
                if (maxlenA < lengthA)
                    lengthA = maxlenA;
            }

            int lengthB = length;
            if (strB != null)
            {
                int maxlenB = ((java.lang.String) (object) strB).length() - indexB;
                if (maxlenB < lengthB)
                    lengthB = maxlenB;
            }

            return compareInfo.Compare(
                        strA, indexA, lengthA, strB, indexB, lengthB, compareOptions);
        }



        //
        // IndexOfAny, LastIndexOfAny
        //

        public static int IndexOfAny(java.lang.String str, char[] anyOf)
            => InternalIndexOfAny(str, anyOf,  1, /* index */ true,  0,
                                                  /* count */ false, 0);

        public static int IndexOfAny(java.lang.String str, char[] anyOf, int startIndex)
            => InternalIndexOfAny(str, anyOf,  1, /* index */ true,  startIndex,
                                                  /* count */ false, 0);

        public static int IndexOfAny(java.lang.String str, char[] anyOf, int startIndex, int count)
            => InternalIndexOfAny(str, anyOf,  1, /* index */ true,  startIndex,
                                                  /* count */ true,  count);

        public static int LastIndexOfAny(java.lang.String str, char[] anyOf)
            => InternalIndexOfAny(str, anyOf, -1, /* index */ false, 0,
                                                  /* count */ false, 0);

        public static int LastIndexOfAny(java.lang.String str, char[] anyOf, int startIndex)
            => InternalIndexOfAny(str, anyOf, -1, /* index */ true,  startIndex,
                                                  /* count */ false, 0);

        public static int LastIndexOfAny(java.lang.String str, char[] anyOf, int startIndex, int count)
            => InternalIndexOfAny(str, anyOf, -1, /* index */ true,  startIndex,
                                                  /* count */ true,  count);

        private static int InternalIndexOfAny(java.lang.String str, char[] anyOf, int dir,
                                              bool haveStartIndex, int startIndex,
                                              bool haveCount, int count)
        {
            ThrowHelper.ThrowIfNull(anyOf);

            int length = str.length();
            if (haveCount && count < 0)
                throw new System.ArgumentOutOfRangeException();
            int endIndex;

            if (dir == 1)
            {
                // IndexOfAny
                endIndex = haveCount ? (startIndex + count) : length;
                if (startIndex < 0 || endIndex > length)
                    throw new System.ArgumentOutOfRangeException();
            }
            else
            {
                // LastIndexOfAny
                if (! haveStartIndex)
                    startIndex = length - 1;
                endIndex = haveCount ? (startIndex - count + 1) : 0;
                if (endIndex < 0 || startIndex >= length)
                    throw new System.ArgumentOutOfRangeException();
            }

            for (int index = startIndex; index != endIndex; index += dir)
            {
                if (CharAtIsAnyOf(str, index, anyOf))
                    return index;
            }

            return -1;
        }



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
            ThrowHelper.ThrowIfNull(pfx);
            return str.startsWith(pfx);
        }

        public static bool EndsWith(java.lang.String str, string sfx)
        {
            ThrowHelper.ThrowIfNull(sfx);
            return str.endsWith(sfx);
        }



        //
        // Contains
        //



        public static bool Contains(java.lang.String large, char small)
            => large.indexOf(small, 0) >= 0;

        public static bool Contains(java.lang.String large, string small)
            => large.indexOf(small, 0) >= 0;

        public static bool Contains(string large, char small, System.StringComparison option)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(option);
            return system.globalization.CompareInfo.IndexOfChar(
                        large, small, 0, compareOption, compareInfo) >= 0;
        }

        public static bool Contains(string large, string small, System.StringComparison option)
        {
            var (compareInfo, compareOption) = StringComparisonToCompareArguments(option);
            return system.globalization.CompareInfo.IndexOfString(
                        large, small, 0, compareOption, compareInfo) >= 0;
        }



        //
        //
        //

        public static int get_Length(java.lang.String str) => str.length();

        public static bool IsNullOrEmpty(java.lang.String str) => (str == null || str.length() == 0);

        public static bool IsNullOrWhiteSpace(java.lang.String str)
        {
            if (str != null)
            {
                int n = str.length();
                for (int i = 0; i < n; i++)
                    if (! Char.IsWhiteSpace(str.charAt(i)))
                        return false;
            }
            return true;
        }

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
            if (    maybeString is string reallyString
                 && (    object.ReferenceEquals(castToType, cachedComparableString)
                      || object.ReferenceEquals(castToType, cachedEquatableString)
                      || object.ReferenceEquals(castToType, cachedIComparable)
                      || object.ReferenceEquals(castToType, cachedIConvertible)))
            {
                return new Wrapper(reallyString);
            }
            return null;
        }

        [java.attr.RetainType] private static readonly System.Type cachedComparableString;
        [java.attr.RetainType] private static readonly System.Type cachedEquatableString;
        [java.attr.RetainType] private static readonly System.Type cachedIComparable;
        [java.attr.RetainType] private static readonly System.Type cachedIConvertible;

        class Wrapper : System.IComparable<string>, System.IEquatable<string>,
                        System.IComparable, System.IConvertible
        {
            [java.attr.RetainType] private string str;
            public Wrapper(string _s) => str = _s;
            // generic CompareTo implementation is culture sensitive
            public int CompareTo(string other)
                => system.String.Compare(str, other);
            // Equals is not sensitive to culture or case
            public bool Equals(string other)
                => ((java.lang.String) (object) str).@equals(
                                        (java.lang.String) (object) other);
            // non-generic CompareTo implementation is culture sensitive
            public int CompareTo(object other)
            {
                return (other is string ostr) ? system.String.Compare(str, ostr)
                     : (other == null) ? 1
                     : throw new System.ArgumentException();
            }
            // IConvertible
            public string ToString(System.IFormatProvider provider) => str;
            public System.TypeCode GetTypeCode() => System.TypeCode.String;
            bool System.IConvertible.ToBoolean(System.IFormatProvider provider)
                => System.Convert.ToBoolean(str, provider);
            char System.IConvertible.ToChar(System.IFormatProvider provider)
                => System.Convert.ToChar(str, provider);
            sbyte System.IConvertible.ToSByte(System.IFormatProvider provider)
                => System.Convert.ToSByte(str, provider);
            byte System.IConvertible.ToByte(System.IFormatProvider provider)
                => System.Convert.ToByte(str, provider);
            short System.IConvertible.ToInt16(System.IFormatProvider provider)
                => System.Convert.ToInt16(str, provider);
            ushort System.IConvertible.ToUInt16(System.IFormatProvider provider)
                => System.Convert.ToUInt16(str, provider);
            int System.IConvertible.ToInt32(System.IFormatProvider provider)
                => System.Convert.ToInt16(str, provider);
            uint System.IConvertible.ToUInt32(System.IFormatProvider provider)
                => System.Convert.ToUInt32(str, provider);
            long System.IConvertible.ToInt64(System.IFormatProvider provider)
                => System.Convert.ToInt64(str, provider);
            ulong System.IConvertible.ToUInt64(System.IFormatProvider provider)
                => System.Convert.ToUInt64(str, provider);
            float System.IConvertible.ToSingle(System.IFormatProvider provider)
                => System.Convert.ToSingle(str, provider);
            double System.IConvertible.ToDouble(System.IFormatProvider provider)
                => System.Convert.ToDouble(str, provider);
            System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider provider)
                => System.Convert.ToDecimal(str, provider);
            System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider)
                => System.Convert.ToDateTime(str, provider);
            object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider)
                => system.Convert.DefaultToType((System.IConvertible) this, type, provider);
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
            cachedIComparable = typeof(System.IComparable);
            cachedIConvertible = typeof(System.IConvertible);
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
        public abstract int compareToIgnoreCase(java.lang.String other);
        public abstract bool equals(java.lang.String other);
        public abstract string intern();
        public abstract string replace(char oldChar, char newChar);
        [java.attr.Discard] extern public static string format(string fmt, object[] args);
        [java.attr.Discard] extern public static string valueOf(char c);
        [java.attr.Discard] extern public static string valueOf(char[] data);
    }
}
