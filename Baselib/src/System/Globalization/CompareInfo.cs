
using System;
using System.Runtime.Serialization;
using CompareOptions = System.Globalization.CompareOptions;

namespace system.globalization {

    [Serializable]
    public class CompareInfo : IDeserializationCallback
    {

        [java.attr.RetainType] java.text.RuleBasedCollator JavaCollator;
        [java.attr.RetainType] java.text.BreakIterator JavaBreakIter;
        [java.attr.RetainType] CultureInfo CultureInfoRef;
        [java.attr.RetainType] static bool isAndroidICU;

        //
        // called by CultureInfo
        //

        public CompareInfo(CultureInfo cultureInfo, java.util.Locale locale)
        {
            CultureInfoRef = cultureInfo;

            var collator = (java.text.RuleBasedCollator)
                                        java.text.Collator.getInstance(locale);

            JavaCollator = new java.text.RuleBasedCollator(
                            collator.getRules() + _OverridingRules);

            // enable FULL_DECOMPOSITION to permit the collator to return
            // multiple elements at the same offset, which then allows
            // Iterator to collect elements into a sequence.
            //
            // note that Android does not support FULL_DECOMPOSITION, see:
            // android.icu.text.Collator

            JavaCollator.setDecomposition(isAndroidICU
                                    ? java.text.Collator.CANONICAL_DECOMPOSITION
                                    : java.text.Collator.FULL_DECOMPOSITION);

            CheckIgnorableChars();

            JavaBreakIter = java.text.BreakIterator.getCharacterInstance(locale);
        }

        //
        // Equals
        //

        public override bool Equals(object other)
            => other is CompareInfo otherCompareInfo && Name == otherCompareInfo.Name;

        public override int GetHashCode() => Name.GetHashCode();

        public string Name => CultureInfoRef.Name;

        public override string ToString() => "CompareInfo - " + CultureInfoRef.ToString();



        //
        // GetCompareInfo
        //

        public static CompareInfo GetCompareInfo(string cultureName)
            => CultureInfo.GetCultureInfo(cultureName).CompareInfo;

        public static CompareInfo CurrentCompareInfo
            => system.threading.Thread.CurrentThread.CurrentCulture.CompareInfo;

        public static CompareInfo InvariantCompareInfo
            => system.globalization.CultureInfo.InvariantCulture.CompareInfo;



        //
        // IsSortable
        //

        public static bool IsSortable(char ch)
            => IsSortable((java.lang.String) (object) java.lang.Character.toString(ch));

        public static bool IsSortable(java.lang.String str)
        {
            ThrowHelper.ThrowIfNull(str);
            int n = str.length();
            if (n == 0)
                return false;
            int cp = 0;
            for (int i = 0; i < n; i++)
            {
                var ch = str.charAt(i);
                if (java.lang.Character.isHighSurrogate(ch))
                {
                    var ch1 = str.charAt(++i);
                    cp = java.lang.Character.toCodePoint(ch, ch1);
                }
                else
                    cp = (int) ch;
                if (! java.lang.Character.isDefined(cp))
                    return false;
            }
            return true;
        }



        //
        // Compare (public API)
        //

        public virtual int Compare(string string1, string string2)
        {
            return CompareStringInternal(string1, 0, -1, string2, 0, -1,
                                         CompareOptions.None, this);
        }

        public virtual int Compare(string string1, string string2, CompareOptions options)
        {
            return CompareStringInternal(string1, 0, -1, string2, 0, -1,
                                         options, this);
        }

        public virtual int Compare(string string1, int offset1, string string2, int offset2)
        {
            return CompareStringInternal(string1, offset1, -1, string2, offset2, -1,
                                        CompareOptions.None, this);
        }

        public virtual int Compare(string string1, int offset1, string string2, int offset2,
                                   CompareOptions options)
        {
            return CompareStringInternal(string1, offset1, -1, string2, offset2, -1,
                                         options, this);
        }

        public virtual int Compare(string string1, int offset1, int length1,
                                   string string2, int offset2, int length2)
        {
            if (length1 < 0 || length2 < 0)
                throw new ArgumentOutOfRangeException();
            return CompareStringInternal(string1, offset1, length1, string2, offset2, length2,
                                         CompareOptions.None, this);
        }

        public virtual int Compare(string string1, int offset1, int length1,
                                   string string2, int offset2, int length2,
                                   CompareOptions options)
        {
            if (length1 < 0 || length2 < 0)
                throw new ArgumentOutOfRangeException();
            return CompareStringInternal(string1, offset1, length1, string2, offset2, length2,
                                         options, this);
        }

        //
        // CompareString (semi-public method)
        //

        public static int CompareString(string string1, string string2,
                                        CompareOptions options, CompareInfo compareInfo)
        {
            return CompareStringInternal(string1, 0, -1, string2, 0, -1,
                                         options, compareInfo);
        }

        public static int CompareString(string string1, int offset1,
                                        string string2, int offset2,
                                        CompareOptions options, CompareInfo compareInfo)
        {
            return CompareStringInternal(string1, offset1, -1, string2, offset2, -1,
                                         options, compareInfo);
        }

        public static int CompareString(string string1, int offset1, int length1,
                                        string string2, int offset2, int length2,
                                        CompareOptions options, CompareInfo compareInfo)
        {
            if (length1 < 0 || length2 < 0)
                throw new ArgumentOutOfRangeException();
            return CompareStringInternal(string1, offset1, length1, string2, offset2, length2,
                                         options, compareInfo);
        }

        //
        // CompareStringInternal (private method)
        //

        public static int CompareStringInternal(string string1, int offset1, int length1,
                                                string string2, int offset2, int length2,
                                                CompareOptions options, CompareInfo compareInfo)
        {
            if (offset1 < 0 || offset2 < 0)
                throw new ArgumentOutOfRangeException();
            int endOffset1 = GetEndOffset(string1, offset1, length1);
            int endOffset2 = GetEndOffset(string2, offset2, length2);

            if (string1 == null)
                return (string2 == null) ? 0 : -1;
            else if (string2 == null)
                return 1;

            if ((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) != 0)
            {
                bool ignoreCase;
                if (options == CompareOptions.Ordinal)
                    ignoreCase = false;
                else if (options == CompareOptions.OrdinalIgnoreCase)
                    ignoreCase = true;
                else
                    throw new System.ArgumentException();

                return CompareStringOrdinal(string1, offset1, endOffset1,
                                            string2, offset2, endOffset2,
                                            ignoreCase);
            }
            else
            {
                return CompareStringCulture(string1, offset1, endOffset1,
                                            string2, offset2, endOffset2,
                                        compareInfo, CompareOptionsToCollatorMask(options));
            }

            static int GetEndOffset(string str, int ofs, int len)
            {
                if (str == null)
                {
                    if (ofs == 0 && len <= 0)
                        return -1;
                }
                else
                {
                    int strLen = ((java.lang.String) (object) str).length();
                    if (len == -1)
                    {
                        if (ofs < strLen)
                            return strLen;
                    }
                    else if ((ofs += len) <= strLen)
                        return ofs;
                }
                throw new ArgumentOutOfRangeException();
            }
        }

        //
        // CompareStringOrdinal (internal method)
        //

        private static int CompareStringOrdinal(string string1, int index1, int endIndex1,
                                                string string2, int index2, int endIndex2,
                                                bool ignoreCase)
        {
            var str1 = (java.lang.String) (object)
                (((java.lang.String) (object) string1).substring(index1, endIndex1));
            var str2 = (java.lang.String) (object)
                (((java.lang.String) (object) string2).substring(index2, endIndex2));
            return ignoreCase ? str1.compareToIgnoreCase(str2) : str1.compareTo(str2);
        }

        //
        // CompareStringCulture (internal method)
        //

        private static int CompareStringCulture(string string1, int index1, int endIndex1,
                                                string string2, int index2, int endIndex2,
                                                CompareInfo compareInfo, uint mask)
        {
            // create iterators for the two strings. see also IndexOfStringCulture.
            // see note in IndexOfStringCulture regarding starting index.

            var iterator1 = new Iterator(compareInfo, mask,
                                         new java.text.StringCharacterIterator(
                                             string1, 0, endIndex1, index1));
            iterator1.Index = index1;

            var iterator2 = new Iterator(compareInfo, mask,
                                         new java.text.StringCharacterIterator(
                                             string2, 0, endIndex2, index2));
            iterator2.Index = index2;

            for (;;)
            {
                var order1 = iterator1.Next();
                var order2 = iterator2.Next();

                if (order1 == null)
                    return (order2 == null) ? 0 : -1;
                else if (order2 == null)
                    return 1;

                int n1 = order1.Length;
                int n2 = order2.Length;
                int n = (n1 <= n2) ? n1 : n2;
                for (int i = 0; i < n; i++)
                {
                    int o1 = order1[i];
                    int o2 = order2[i];
                    if (o1 != o2)
                        return o1 - o2;
                    if (o1 == 0)
                        break;
                }

                if (n1 != n2)
                    return n1 - n2;
            }
        }



        //
        // IndexOf (char, public API)
        //

        public virtual int IndexOf(string source, char value)
            => IndexOfChar(source, value, /* startIndex */ 0, CompareOptions.None, this);

        public virtual int IndexOf(string source, char value, CompareOptions options)
            => IndexOfChar(source, value, /* startIndex */ 0, options, this);

        public virtual int IndexOf(string source, char value, int startIndex)
            => IndexOfChar(source, value, startIndex, CompareOptions.None, this);

        public virtual int IndexOf(string source, char value, int startIndex, CompareOptions options)
            => IndexOfChar(source, value, startIndex, options, this);

        public virtual int IndexOf(string source, char value, int startIndex, int count)
            => IndexOfChar(source, value, startIndex, count, CompareOptions.None, this);

        public virtual int IndexOf(string source, char value, int startIndex, int count, CompareOptions options)
            => IndexOfChar(source, value, startIndex, count, options, this);



        //
        // IndexOfChar (semi-public method)
        //

        public static int IndexOfChar(string source, char value, int startIndex,
                                      CompareOptions options, CompareInfo compareInfo)
        {
            ThrowHelper.ThrowIfNull(source);

            int sourceLength = ((java.lang.String) (object) source).length();
            if (startIndex >= sourceLength)
                throw new System.ArgumentOutOfRangeException();

            if (options == CompareOptions.Ordinal)
            {
                return ((java.lang.String) (object) source).indexOf(value, startIndex);
            }
            else if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return IndexOfCharOrdinal((java.lang.String) (object) source, value,
                                          startIndex, sourceLength, true);
            }
            else
            {
                return compareInfo.IndexOfStringCulture(
                                        source, startIndex, sourceLength,
                                        java.lang.Character.toString(value), options);
            }
        }

        public static int IndexOfChar(string source, char value, int startIndex, int count,
                                      CompareOptions options, CompareInfo compareInfo)
        {
            ThrowHelper.ThrowIfNull(source);

            int sourceLength = ((java.lang.String) (object) source).length();
            int endIndex = startIndex + count;
            if (startIndex < 0 || count < 0 || endIndex >= sourceLength)
                throw new System.ArgumentOutOfRangeException();

            if ((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) != 0)
            {
                bool ignoreCase;
                if (options == CompareOptions.Ordinal)
                {
                    if (endIndex == sourceLength)
                        return ((java.lang.String) (object) source).indexOf(value, startIndex);

                    ignoreCase = false;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                    ignoreCase = true;
                else
                    throw new System.ArgumentException();

                return IndexOfCharOrdinal((java.lang.String) (object) source, value,
                                          startIndex, endIndex, ignoreCase);
            }
            else
            {
                return compareInfo.IndexOfStringCulture(
                                        source, startIndex, endIndex,
                                        java.lang.Character.toString(value), options);
            }
        }



        //
        // IndexOfCharOrdinal
        //

        private static int IndexOfCharOrdinal(java.lang.String large, char small,
                                              int largeIndex, int largeEndIndex,
                                              bool ignoreCase)
        {
            int largeDelta = (largeEndIndex >= largeIndex) ? 1 : -1;

            if (ignoreCase)
            {
                char small_L = java.lang.Character.toLowerCase(small);
                char small_U = java.lang.Character.toUpperCase(small);

                for (; largeIndex != largeEndIndex; largeIndex += largeDelta)
                {
                    char largeCh = large.charAt(largeIndex);
                    if (    small_L == java.lang.Character.toLowerCase(largeCh)
                         && small_U == java.lang.Character.toUpperCase(largeCh))
                    {
                        return largeIndex;
                    }
                }
            }
            else
            {
                for (; largeIndex != largeEndIndex; largeIndex += largeDelta)
                {
                    if (small == large.charAt(largeIndex))
                        return largeIndex;
                }
            }

            return -1;
        }



        //
        // IndexOf (string, public API)
        //

        public virtual int IndexOf(string source, string value)
            => IndexOfString(source, value, /* startIndex */ 0, CompareOptions.None, this);

        public virtual int IndexOf(string source, string value, CompareOptions options)
            => IndexOfString(source, value, /* startIndex */ 0, options, this);

        public virtual int IndexOf(string source, string value, int startIndex)
            => IndexOfString(source, value, startIndex, CompareOptions.None, this);

        public virtual int IndexOf(string source, string value, int startIndex, CompareOptions options)
            => IndexOfString(source, value, startIndex, options, this);

        public virtual int IndexOf(string source, string value, int startIndex, int count)
            => IndexOfString(source, value, startIndex, count, CompareOptions.None, this);

        public virtual int IndexOf(string source, string value, int startIndex, int count, CompareOptions options)
            => IndexOfString(source, value, startIndex, count, options, this);



        //
        // IndexOfString (semi-public method)
        //

        public static int IndexOfString(string source, string value, int startIndex,
                                        CompareOptions options, CompareInfo compareInfo)
        {
            ThrowHelper.ThrowIfNull(source);

            int sourceLength = ((java.lang.String) (object) source).length();
            if (startIndex < 0 || startIndex >= sourceLength)
                throw new System.ArgumentOutOfRangeException();

            if (options == CompareOptions.Ordinal)
            {
                return ((java.lang.String) (object) source).indexOf(value, startIndex);
            }
            else if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return IndexOfStringOrdinal((java.lang.String) (object) source,
                                            (java.lang.String) (object) value,
                                            startIndex, sourceLength, true);
            }
            else
            {
                return compareInfo.IndexOfStringCulture(
                                    source, startIndex, sourceLength, value, options);
            }
        }

        public static int IndexOfString(string source, string value, int startIndex, int count,
                                        CompareOptions options, CompareInfo compareInfo)
        {
            ThrowHelper.ThrowIfNull(source, value);

            int sourceLength = ((java.lang.String) (object) source).length();
            int endIndex = startIndex + count;
            if (startIndex < 0 || count < 0 || endIndex > sourceLength)
                throw new System.ArgumentOutOfRangeException();

            if ((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) != 0)
            {
                bool ignoreCase;
                if (options == CompareOptions.Ordinal)
                {
                    if (endIndex == sourceLength)
                        return ((java.lang.String) (object) source).indexOf(value, startIndex);

                    ignoreCase = false;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                    ignoreCase = true;
                else
                    throw new System.ArgumentException();

                return IndexOfStringOrdinal((java.lang.String) (object) source,
                                            (java.lang.String) (object) value,
                                            startIndex, endIndex, ignoreCase);
            }
            else
            {
                return compareInfo.IndexOfStringCulture(
                                        source, startIndex, endIndex, value, options);
            }
        }



        //
        // IndexOfStringCulture (internal method)
        //

        private int IndexOfStringCulture(string large, int largeIndex, int largeEndIndex,
                                         string small, CompareOptions options)
        {
            uint mask = CompareOptionsToCollatorMask(options);

            // get the first non-ignorable character in 'value'

            var smallIterator = new Iterator(this, mask, small);

            var smallOrder0 = smallIterator.Next();
            if (smallOrder0 == null)
            {
                // an empty or ignorable pattern is always found at the beginning
                return largeIndex;
            }
            int smallIndex0 = smallIterator.Save();

            if (largeIndex == largeEndIndex)
            {
                // the pattern cannot be found if there is no text to scan
                return -1;
            }

            int rangeStart, rangeEnd;
            bool switchDirection;
            if (largeIndex > largeEndIndex)
            {
                // LastIndexOf mode, iterating backward
                rangeStart = largeEndIndex;
                rangeEnd = largeIndex + 1;
                switchDirection = true;
            }
            else
            {
                // IndexOf mode, iterating forward
                rangeStart = largeIndex;
                rangeEnd = largeEndIndex;
                switchDirection = false;
            }

            // must always pass a starting range of zero, otherwise Android ICU
            // throws an IndexOutOfBoundsException exception when the setText()
            // method of CollationElementIterator calls setToStart() on the
            // character iterator with a zero offset
            var largeIterator = new Iterator(this, mask,
                                        new java.text.StringCharacterIterator(
                                            large, 0, rangeEnd, rangeStart));

            if (switchDirection)
            {
                largeIterator.Index = rangeEnd;
                largeIterator.SwitchDirection();
            }
            else
                largeIterator.Index = rangeStart;

            // select main loop variant, depending on the first characters
            // in the search string (that is not ignorable).  if it has an
            // order value with a non-zero primary strength, we select the
            // primary variant, otherwise we assume the first character is
            // a combining punctuation, and use the secondary variant.

            if ((smallOrder0[0] & 0xFFFF0000) != 0)
            {
                return IndexOfStringPrimary(
                            largeIterator, smallIterator, smallOrder0[0]);
            }
            else
            {
                return IndexOfStringSecondary(
                            largeIterator, smallIterator, smallOrder0[0],
                            ((java.lang.String) (object) small).charAt(smallIndex0),
                            ((java.lang.String) (object) large));
            }
        }

        //
        // IndexOfStringPrimary (internal method)
        //

        private static int IndexOfStringPrimary(Iterator largeIterator,
                                                Iterator smallIterator,
                                                int smallOrder0)
        {
            // in both main loop variants (this method and the one below),
            // we iterate the strings by sequences, not characters.
            // sequences generated by our nested Iterator class do not
            // necessarily map directly to characters.  for example:
            // - when the (AE) symbol is decomposed into two elements,
            // - when a non-Latin letter is decomposed into a Latin letter
            // followed by a combining mark,
            // - when collecting accents following a Latin letter.
            // for more information, see Iterator.Next() method, below.

            // because we know the search string starts with a primary
            // symbol (i.e. not a combining mark), we can compare just the
            // first element in each sequence, to decide if we need to do
            // compare the sequence further.  this also lets us provide
            // a better emulation of the .Net IndexOf, where searching for
            // "EF" in the text "(AE)F" will fail to match (note that (AE)
            // here stands for the AE symbol, not A followed by E.)

            for (;;)
            {
                var largeOrder = largeIterator.Next();
                if (largeOrder == null)
                {
                    // the pattern was not found if we hit the end of the text
                    return -1;
                }

                if (largeOrder[0] == smallOrder0)
                {
                    int matchIndex = largeIterator.Save();

                    if (Iterator.Compare(largeIterator, smallIterator, 0))
                    {
                        return matchIndex;
                    }

                    smallIterator.RestoreForCompare();
                    largeIterator.RestoreForIterate();
                }
            }
        }

        //
        // IndexOfStringSecondary (internal method)
        //

        private static int IndexOfStringSecondary(Iterator largeIterator,
                                                  Iterator smallIterator,
                                                  int smallOrder0, char smallChar0,
                                                  java.lang.String largeString)
        {
            // the sequences generated by our nested Iterator class do not
            // generally begin with a combining symbol (see discussion in
            // IndexOfStringPrimary method, above.)  so if we know the
            // search string does start with such a symbol, we cannot use
            // the primary variant above.

            // in this secondary variant, we compare every element of every
            // sequence, because a match might start anywhere, for example
            // a search for U+030A (ring) can match the second element in
            // the sequence of A followed by U+030A.

            // one last thing we must do in case of a match, is to identify
            // the character index of match, in a scenario where
            // (1) each sequence element does not necessarily map directly
            // to a character, and (2) the starting element can potentially
            // be found more than once within the sequence.

            // for this, we keep a count of the number of times we hit the
            // starting element within the sequence, and then skip the same
            // number of times when searching for the character by value.

            for (;;)
            {
                var largeOrder = largeIterator.Next();
                if (largeOrder == null)
                {
                    // the pattern was not found if we hit the end of the text
                    return -1;
                }

                int matchCount = 0;
                int matchIndex = -1;

                for (int i = 0; largeOrder[i] != 0; i++)
                {
                    if (largeOrder[i] == smallOrder0)
                    {
                        if (matchIndex == -1)
                            matchIndex = largeIterator.Save();

                        if (Iterator.Compare(largeIterator, smallIterator, i))
                        {
                            for (;;)
                            {
                                matchIndex = largeString.indexOf(smallChar0, matchIndex);
                                if (matchCount-- == 0 || matchIndex == -1)
                                    break;
                                matchIndex++;
                            }

                            return matchIndex;
                        }

                        matchCount++;
                        smallIterator.RestoreForCompare();
                        largeIterator.RestoreForCompare();
                    }
                }

                if (matchIndex != -1)
                    largeIterator.RestoreForIterate();
            }
        }



        //
        // IndexOfStringOrdinal (internal method)
        //

        public static int IndexOfStringOrdinal(java.lang.String largeString,
                                               java.lang.String smallString,
                                               int largeIndex, int largeEndIndex,
                                               bool ignoreCase)
        {
            int smallLength;
            if ((smallLength = smallString.length()) == 0)
                return largeIndex;
            char small0 = smallString.charAt(0);

            int largeDelta;
            if (largeEndIndex >= largeIndex)
            {   // IndexOf
                largeDelta = 1;
                largeEndIndex -= (smallLength - 1);
                if (largeEndIndex < largeIndex)
                    return -1;
            }
            else
            {   // LastIndexOf
                largeDelta = -1;
                largeIndex -= (smallLength - 1);
                if (largeIndex < largeEndIndex)
                    return -1;
                largeEndIndex--;
            }

            if (ignoreCase)
            {
                char small0L = java.lang.Character.toLowerCase(small0);
                char small0U = java.lang.Character.toUpperCase(small0);

                for (; largeIndex != largeEndIndex; largeIndex += largeDelta)
                {
                    char largeCh = largeString.charAt(largeIndex);
                    if (    small0L == java.lang.Character.toLowerCase(largeCh)
                         && small0U == java.lang.Character.toUpperCase(largeCh)
                         && largeString.regionMatches(true, largeIndex, smallString, 0, smallLength))
                    {
                        return largeIndex;
                    }
                }
            }
            else
            {
                for (; largeIndex != largeEndIndex; largeIndex += largeDelta)
                {
                    if (    small0 == largeString.charAt(largeIndex)
                         && largeString.regionMatches(largeIndex, smallString, 0, smallLength))
                    {
                        return largeIndex;
                    }
                }
            }

            return -1;
        }



        //
        // LastIndexOf (char, public API)
        //

        public virtual int LastIndexOf(string source, char value)
            => LastIndexOfChar(source, value, /* startIndex */ false, 0, CompareOptions.None, this);

        public virtual int LastIndexOf(string source, char value, CompareOptions options)
            => LastIndexOfChar(source, value, /* startIndex */ false, 0, options, this);

        public virtual int LastIndexOf(string source, char value, int startIndex)
            => LastIndexOfChar(source, value, /* startIndex */ true, startIndex, CompareOptions.None, this);

        public virtual int LastIndexOf(string source, char value, int startIndex, CompareOptions options)
            => LastIndexOfChar(source, value, /* startIndex */ true, startIndex, options, this);

        public virtual int LastIndexOf(string source, char value, int startIndex, int count)
            => LastIndexOfChar(source, value, startIndex, count, CompareOptions.None, this);

        public virtual int LastIndexOf(string source, char value, int startIndex, int count, CompareOptions options)
            => LastIndexOfChar(source, value, startIndex, count, options, this);



        //
        // LastIndexOfChar (semi-public method)
        //

        public static int LastIndexOfChar(string source, char value,
                                          bool haveStartIndex, int startIndex,
                                          CompareOptions options, CompareInfo compareInfo)
        {
            ThrowHelper.ThrowIfNull(source);

            int sourceLength = ((java.lang.String) (object) source).length();
            if (! haveStartIndex)
                startIndex = sourceLength - 1;
            else if (startIndex < 0 || startIndex >= sourceLength)
                throw new System.ArgumentOutOfRangeException();

            if (options == CompareOptions.Ordinal)
            {
                return ((java.lang.String) (object) source).lastIndexOf(value, startIndex);
            }
            else if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return IndexOfCharOrdinal((java.lang.String) (object) source, value,
                                          startIndex, 0, true);
            }
            else
            {
                return compareInfo.IndexOfStringCulture(
                                        source, startIndex, 0,
                                        java.lang.Character.toString(value), options);
            }
        }

        public static int LastIndexOfChar(string source, char value, int startIndex, int count,
                                          CompareOptions options, CompareInfo compareInfo)
        {
            ThrowHelper.ThrowIfNull(source);

            int sourceLength = ((java.lang.String) (object) source).length();
            int endIndex = startIndex - count + 1;
            if (endIndex < 0 || count < 0 || startIndex >= sourceLength)
                throw new System.ArgumentOutOfRangeException();

            if ((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) != 0)
            {
                bool ignoreCase;
                if (options == CompareOptions.Ordinal)
                {
                    if (endIndex == 0)
                        return ((java.lang.String) (object) source).lastIndexOf(value, startIndex);

                    ignoreCase = false;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                    ignoreCase = true;
                else
                    throw new System.ArgumentException();

                return IndexOfCharOrdinal((java.lang.String) (object) source, value,
                                          startIndex, endIndex, ignoreCase);
            }
            else
            {
                return compareInfo.IndexOfStringCulture(
                                        source, startIndex, endIndex,
                                        java.lang.Character.toString(value), options);
            }
        }



        //
        // LastIndexOf (string, public API)
        //

        public virtual int LastIndexOf(string source, string value)
            => LastIndexOfString(source, value, /* startIndex */ false, 0, CompareOptions.None, this);

        public virtual int LastIndexOf(string source, string value, CompareOptions options)
            => LastIndexOfString(source, value, /* startIndex */ false, 0, options, this);

        public virtual int LastIndexOf(string source, string value, int startIndex)
            => LastIndexOfString(source, value, /* startIndex */ true, startIndex, CompareOptions.None, this);

        public virtual int LastIndexOf(string source, string value, int startIndex, CompareOptions options)
            => LastIndexOfString(source, value, /* startIndex */ true, startIndex, options, this);

        public virtual int LastIndexOf(string source, string value, int startIndex, int count)
            => LastIndexOfString(source, value, startIndex, count, CompareOptions.None, this);

        public virtual int LastIndexOf(string source, string value, int startIndex, int count, CompareOptions options)
            => LastIndexOfString(source, value, startIndex, count, options, this);



        //
        // LastIndexOfString (semi-public method)
        //

        public static int LastIndexOfString(string source, string value,
                                            bool haveStartIndex, int startIndex,
                                            CompareOptions options, CompareInfo compareInfo)
        {
            ThrowHelper.ThrowIfNull(source, value);

            int sourceLength = ((java.lang.String) (object) source).length();
            if (! haveStartIndex)
                startIndex = sourceLength - 1;
            else if (startIndex >= sourceLength)
                throw new System.ArgumentOutOfRangeException();

            if (options == CompareOptions.Ordinal)
            {
                int offset = ((java.lang.String) (object) value).length() - 1;
                if (offset < 0)
                    return startIndex;
                return ((java.lang.String) (object) source).lastIndexOf(value, startIndex - offset);
            }
            else if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return IndexOfStringOrdinal((java.lang.String) (object) source,
                                            (java.lang.String) (object) value,
                                            startIndex, 0, true);
            }
            else
            {
                return compareInfo.IndexOfStringCulture(
                                    source, startIndex, 0, value, options);
            }
        }

        public static int LastIndexOfString(string source, string value, int startIndex, int count,
                                            CompareOptions options, CompareInfo compareInfo)
        {
            ThrowHelper.ThrowIfNull(source, value);

            int sourceLength = ((java.lang.String) (object) source).length();
            int endIndex = startIndex - count + 1;
            if (endIndex < 0 || count < 0 || startIndex >= sourceLength)
                throw new System.ArgumentOutOfRangeException();

            if ((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) != 0)
            {
                bool ignoreCase;
                if (options == CompareOptions.Ordinal)
                {
                    if (endIndex == 0)
                    {
                        int offset = ((java.lang.String) (object) value).length() - 1;
                        if (offset < 0)
                            return startIndex;
                        return ((java.lang.String) (object) source).lastIndexOf(value, startIndex - offset);
                    }

                    ignoreCase = false;
                }
                else if (options == CompareOptions.OrdinalIgnoreCase)
                    ignoreCase = true;
                else
                    throw new System.ArgumentException();

                return IndexOfStringOrdinal((java.lang.String) (object) source,
                                            (java.lang.String) (object) value,
                                            startIndex, endIndex, ignoreCase);
            }
            else
            {
                return compareInfo.IndexOfStringCulture(
                                        source, startIndex, endIndex, value, options);
            }
        }



        //
        // IsPrefix
        //

        public virtual bool IsPrefix(string large, string small) => IsPrefix(large, small, 0);

        public virtual bool IsPrefix(string large, string small, CompareOptions options)
        {
            ThrowHelper.ThrowIfNull(large, small);

            if (options == CompareOptions.Ordinal)
                return IsPrefixOrSuffixOrdinal(large, small, 'p');
            if (options == CompareOptions.OrdinalIgnoreCase)
                return IsPrefixOrSuffixOrdinal(large, small, 'P');

            uint mask = CompareOptionsToCollatorMask(options);

            var smallIterator = new Iterator(this, mask, small);
            var smallOrder = smallIterator.Next();
            if (smallOrder == null)
            {
                // an empty or ignorable pattern is always found at the beginning
                return true;
            }

            var largeIterator = new Iterator(this, mask, large);
            var largeOrder = largeIterator.Next();

            if (largeOrder != null && largeOrder[0] == smallOrder[0])
            {
                return Iterator.Compare(largeIterator, smallIterator, 0);
            }

            return false;
        }



        //
        // IsSuffix
        //

        public virtual bool IsSuffix(string large, string small) => IsSuffix(large, small, 0);

        public virtual bool IsSuffix(string large, string small, CompareOptions options)
        {
            ThrowHelper.ThrowIfNull(large, small);

            if (options == CompareOptions.Ordinal)
                return IsPrefixOrSuffixOrdinal(large, small, 's');
            if (options == CompareOptions.OrdinalIgnoreCase)
                return IsPrefixOrSuffixOrdinal(large, small, 'S');

            uint mask = CompareOptionsToCollatorMask(options);

            var smallIterator = new Iterator(this, mask, small);
            var order = smallIterator.Next();
            if (order == null)
            {
                // an empty or ignorable pattern is always found at the end
                return true;
            }

            var largeIterator = new Iterator(this, mask, large);

            largeIterator.Index = ((java.lang.String) (object) large).length();
            largeIterator.SwitchDirection();

            int i;

            // collect all sequences from the small search string
            var smallOrders = new System.Collections.Generic.List<int>();
            for (;;)
            {
                for (i = 0; order[i] != 0; i++)
                    smallOrders.Add(order[i]);

                order = smallIterator.Next();
                if (order == null)
                    break;
            }
            int numSmallOrders = smallOrders.Count;

            // collect sequences from the large search string
            var largeOrders = new System.Collections.Generic.List<int>();
            int numLargeOrders = 0;
            for (;;)
            {
                order = largeIterator.Next();
                if (order == null)
                    break;

                int j = 0;
                for (i = 0; order[i] != 0; i++)
                {
                    largeOrders.Insert(j++, order[i]);
                    numLargeOrders++;
                }

                if (numLargeOrders >= numSmallOrders)
                    break;
            }

            // the contents of the collected orders must equal
            if (numLargeOrders == numSmallOrders)
            {
                for (i = 0; i < numLargeOrders; i++)
                {
                    if (smallOrders[i] != largeOrders[i])
                        break;
                    i++;
                }
                if (i == numLargeOrders)
                    return true;
            }

            return false;
        }



        //
        // IsPrefixOrSuffixOrdinal
        //

        public static bool IsPrefixOrSuffixOrdinal(string large, string small, char mode)
        {
            switch (mode)
            {
                case 'p':   // prefix, equal case
                    return ((java.lang.String) (object) large).startsWith(small, 0);

                case 's':   // suffix, equal case
                    return ((java.lang.String) (object) large).endsWith(small);

                case 'P':   // prefix, ignore case
                    return ((java.lang.String) (object) large).regionMatches(
                        true, 0, small, 0, ((java.lang.String) (object) small).length());

                case 'S':   // suffix, ignore case
                    int largeLength = ((java.lang.String) (object) large).length();
                    int smallLength = ((java.lang.String) (object) small).length();
                    return ((java.lang.String) (object) large).regionMatches(
                                true, largeLength - smallLength, small, 0, smallLength);
            }
            return false;
        }

        //
        //
        //

        void IDeserializationCallback.OnDeserialization(object sender)
            => throw new PlatformNotSupportedException();



        //
        // Ignorable characters
        //
        // Windows/.NET ignores the following characters in every non-Ordinal
        // string comparison/indexing operation, regardless of culture.  this
        // is table B.1 from RFC 3454, plus several additional characters, as
        // defined in the Windows Sorting Weight Tables, downloadable from
        // Microsoft.
        //
        // we implement this by creating a rule for RuleBasedCollator, which
        // defines those ignorable characters as equal to unicode U+0000.
        //

        [java.attr.RetainType] private static string _OverridingRules;

        static CompareInfo()
        {
            //
            // check if we are running with the Android ICU library
            //

            try
            {
                if (null != java.lang.Class.forName(
                                "android.icu.impl.coll.CollationRuleParser", false,
                                ((java.lang.Class) typeof(CompareInfo)).getClassLoader()))
                {
                    isAndroidICU = true;
                }
            }
            catch (System.Exception) { }

            //
            // create the overriding rules for the RuleBasedCollator
            //

            char ch0 = (char) 0;
            string rules = "&'" + ch0 + "'";
            foreach (var ch in new char[]
            {
                '\u00AD', // U+00AD Soft Hyphen
                '\u034F', // U+034F Combining Grapheme Joiner
                '\u0640', // U+0640 Arabic Tatweel
                '\u0ECC', // U+0ECC Lao Cancellation Mark
                '\u1806', // U+1806 Mongolian Todo Soft Hyphen
                '\u180B', // U+180B Mongolian Free Variation Selector One
                '\u180C', // U+180C Mongolian Free Variation Selector Two
                '\u180D', // U+180D Mongolian Free Variation Selector Three
                '\u200C', // U+200C Zero Width Non-Joiner
                '\u200D', // U+200D Zero Width Joiner
                '\u200E', // U+200E Left-to-Right Mark
                '\u200F', // U+200F Right-to-Left Mark
                '\u202A', // U+202A Left-to-Right Embedding
                '\u202B', // U+202B Right-to-Left Embedding
                '\u202C', // U+202C Pop Directional Formatting
                '\u202D', // U+202D Left-to-Right Override
                '\u202E', // U+202E Right-to-Left Override
                '\u2060', // U+2060 Word Joiner
                '\u2061', // U+2061 Function Application
                '\u2062', // U+2062 Invisible Times
                '\u2063', // U+2063 Invisible Separator
                '\u2064', // U+2064 Invisible Plus
                '\u206A', // U+206A Inhibit Symmetric Swapping
                '\u206B', // U+206B Activate Symmetric Swapping
                '\u206C', // U+206C Inhibit Arabic Form Shaping
                '\u206D', // U+206D Activate Arabic Form Shaping
                '\u206E', // U+206E National Digit Shapes
                '\u206F', // U+206F Nominal Digit Shapes
                '\u3190', // U+3190 Ideographic Annotation Linking Mark
                '\u3191', // U+3191 Ideographic Annotation Reverse Mark
                '\uDB40', // U+DB40 High Surrogates
                '\uFE00', // U+FE00 Variation Selector-1
                '\uFE01', // U+FE00 Variation Selector-2
                '\uFE02', // U+FE00 Variation Selector-3
                '\uFE03', // U+FE00 Variation Selector-4
                '\uFE04', // U+FE00 Variation Selector-5
                '\uFE05', // U+FE00 Variation Selector-6
                '\uFE06', // U+FE00 Variation Selector-7
                '\uFE07', // U+FE00 Variation Selector-8
                '\uFE08', // U+FE00 Variation Selector-9
                '\uFE09', // U+FE00 Variation Selector-10
                '\uFE0A', // U+FE00 Variation Selector-11
                '\uFE0B', // U+FE00 Variation Selector-12
                '\uFE0C', // U+FE00 Variation Selector-13
                '\uFE0D', // U+FE00 Variation Selector-14
                '\uFE0E', // U+FE00 Variation Selector-15
                '\uFE0F', // U+FE0F Variation Selector-16
                '\uFEFF', // U+FEFF Zero Width No-Break Space / Byte Order Mark
                '\uFFF9', // U+FFF9 Interlinear Annotation Anchor
                '\uFFFA', // U+FFFA Interlinear Annotation Separator
                '\uFFFB', // U+FFFB Interlinear Annotation Terminator
                '\uFFFC', // U+FFFC Object Replacement Character
                '\uFFFD', // U+FFFD Replacement Character
            })
            {
                if (isAndroidICU && (ch == '\uDB40' || ch == '\uFFFD'))
                {
                    // Android ICU doesn't like some characters, see also:
                    // android.icu.impl.coll.CollationRuleParser.parseString()
                    continue;
                }
                rules += "='" + ch + "'";
            }

            // the default rules in RuleBasedCollator assign a different
            // priority to pre-composed symbols (e.g. the (AE) symbol)
            // than the decomposed version (e.g. A followed by E).
            // in .Net, they are considered equal.
            char ch_AE = '\u00C6';
            char ch_ae = '\u00E6';
            rules += "&AE=" + ch_AE + "&ae=" + ch_ae;

            _OverridingRules = rules;
        }

        //
        // our ignorable characters overriding rule, as generated in the
        // static initializer above, maps the ignorable characters to the
        // character U+0000, under the assumption that it will have a zero
        // ordering value, so make sure that is actually so.
        //

        private void CheckIgnorableChars()
        {
            char ch0 = (char) 0;
            var iterator = JavaCollator.getCollationElementIterator("" + ch0);
            if (iterator.next() != 0)
            {
                throw new PlatformNotSupportedException(
                                        "unexpected value for UNICODE NULL");
            }
        }



        //
        // CompareOptionsToCollatorMask
        //

        static uint CompareOptionsToCollatorMask(CompareOptions options)
        {
            uint mask = 0xFFFFFFFF;
            if (options == CompareOptions.IgnoreCase)
            {
                // if we are ignoring case, then mask out the tertiary order.
                // see also RuleBasedCollator and CollationElementIterator.
                mask &= ~ (uint) 0xFF;
            }
            else if (options != CompareOptions.None)
                throw new System.ArgumentException("CompareOptions");
            return mask;
        }



        //
        // Iterator
        //

        class Iterator
        {
            [java.attr.RetainType] java.text.CollationElementIterator JavaElemIter;
            [java.attr.RetainType] java.text.BreakIterator JavaBreakIter;
            [java.attr.RetainType] uint orderMask;
            [java.attr.RetainType] int direction;
            [java.attr.RetainType] int saveDirection;
            [java.attr.RetainType] int currIndex;
            [java.attr.RetainType] int lastIndex;
            [java.attr.RetainType] int saveIndexForCompare;
            [java.attr.RetainType] int saveIndexForIterate;
            [java.attr.RetainType] int[] sequence;
            [java.attr.RetainType] int[] saveSequence;



            private Iterator(CompareInfo compareInfo, uint mask)
            {
                JavaBreakIter = (java.text.BreakIterator) compareInfo.JavaBreakIter.clone();
                orderMask = mask;
                direction = 1;
                sequence = new int[4];
            }



            public Iterator(CompareInfo compareInfo, uint mask, string text)
                : this(compareInfo, mask)
            {
                JavaElemIter = compareInfo.JavaCollator.getCollationElementIterator(text);
                JavaBreakIter.setText(text);
            }



            public Iterator(CompareInfo compareInfo, uint mask, java.text.CharacterIterator text)
                : this(compareInfo, mask)
            {
                JavaElemIter = compareInfo.JavaCollator.getCollationElementIterator(text);
                JavaBreakIter.setText(text);
            }



            public int Index
            {
                get => currIndex;
                set => currIndex = value;
            }



            public void SwitchDirection() => direction = -direction;



            public int[] Next()
            {
                // we use a collator configured with FULL_DECOMPOSITION, which
                // does NFKD normalization of text during iteration, instead
                // of normalizing the entire text in advance.  this also lets
                // us make a distinction between composed characters, e.g.:
                // U+00C5 (A with ring) vs A followed by U+030A (ring)
                // U+00C6 (AE symbol) vs A followed by E
                //
                // unfortunately, the collation iterator reports inconsistent
                // offsets when dealing with composed characters, making it
                // impossible to figure out where a decomposed sequence ends.
                //
                // the only reliable workaround seems to be to employ a break
                // iterator to identify where the following character starts,
                // then walk the collation iterator backwards.

                int nextIndex, stopIndex;
                for (;;)
                {
                    if (direction > 0)
                    {
                        if ((nextIndex = JavaBreakIter.following(currIndex)) == -1)
                            return null;
                        JavaElemIter.setOffset(nextIndex);
                        stopIndex = currIndex;
                    }
                    else
                    {
                        if ((nextIndex = JavaBreakIter.preceding(currIndex)) == -1)
                            return null;
                        JavaElemIter.setOffset(currIndex);
                        stopIndex = nextIndex;
                    }

                    int n = 0;
                    int nMax = sequence.Length;
                    bool restart = false;
                    bool malformed = false;

                    for (;;)
                    {
                        int order = JavaElemIter.previous();
                        if (order == -1)
                        {
                            // walked back beyond the start of the sequence
                            break;
                        }

                        int previousIndex = JavaElemIter.getOffset();
                        if (previousIndex < stopIndex)
                        {
                            if (n == 0 && previousIndex < stopIndex)
                            {
                                // if the next element is a malformed combining
                                // symbol.  e.g., U+030A (ring) without a symbol
                                // to combine with.
                                malformed = true;
                            }
                            else
                            {
                                // walked back beyond the start of the sequence
                                break;
                            }
                        }

                        if (order == 0 || order == 0x7FFF0000)
                        {
                            // ignorable characters return a zero order value
                            // (due to our _OverridingRules), and they should
                            // be the only element in the sequence.
                            // unmappable characters should return 0x7FFF0000
                            // as in CollationElementIterator.next(), but due
                            // to a bug, also return a zero order.
                            // which means that if we detect a zero result,
                            // we can just discard this sequence.
                            restart = true;
                            break;
                        }

                        if (n + 1 >= nMax)
                        {
                            // grow the sequence array if necessary
                            nMax += 4;
                            var newSeq = new int[nMax];
                            java.lang.System.arraycopy(sequence, 0, newSeq, 0, n);
                            sequence = newSeq;
                        }

                        sequence[n++] = (int) (order & orderMask);

                        if (malformed)
                            break;
                    }

                    lastIndex = currIndex;
                    currIndex = nextIndex;

                    if (! restart)
                    {
                        // reverse the order of the elements in the sequence
                        int half_n = n / 2;
                        int j = n;
                        for (int i = 0; i < half_n; i++)
                        {
                            int k = sequence[--j];
                            sequence[j] = sequence[i];
                            sequence[i] = k;
                        }
                        sequence[n++] = 0;
                        return sequence;
                    }
                }
            }



            public int Save()
            {
                int n = sequence.Length;
                if (saveSequence == null || saveSequence.Length < n)
                    saveSequence = java.util.Arrays.copyOf(sequence, n);
                else
                    java.lang.System.arraycopy(sequence, 0, saveSequence, 0, n);

                saveDirection = direction;
                if (direction > 0)
                {
                    // when iterating forwards, the current position is now
                    // just beyond the last sequence, so Next() calls during
                    // Compare() -- which are used to load more sequences --
                    // continue from the same place that iteration stopped.
                    saveIndexForCompare = currIndex;
                    saveIndexForIterate = currIndex;
                    // the last position is the sequence starting position
                    return lastIndex;
                }
                else
                {
                    // when iterating backwards, the current position is now
                    // just before the last sequence, so we need to adjust
                    // it -- by setting it to the last position, which is
                    // position just beyond the last sequence.
                    direction = 1;
                    saveIndexForCompare = lastIndex;
                    saveIndexForIterate = currIndex;
                    // the current position is the sequence start position
                    int returnIndex = currIndex;
                    currIndex = lastIndex;
                    return returnIndex;
                }
            }



            public void RestoreForCompare()
            {
                // when iterating forwards, there is no practical difference
                // between RestoreForCompare and RestoreForIterate, because,
                // as mentioned in Save() above, the iteration order is the
                // same.  but when iterating backwards, just like in Save(),
                // we need to make a distinction between restoring state in
                // order to continue iteration, or restoring state in order
                // to do another comparison.  see also RestoreForIterate()

                currIndex = saveIndexForCompare;
                java.lang.System.arraycopy(saveSequence, 0, sequence, 0, saveSequence.Length);
            }



            public void RestoreForIterate()
            {
                // see also RestoreForCompare() and Save()
                currIndex = saveIndexForIterate;
                direction = saveDirection;
            }



            public static bool Compare(Iterator large, Iterator small, int largeIndex0)
            {
                int S = 1;
                int L = largeIndex0 + 1;
                for (;;)
                {
                    if (large.sequence[L] == 0)
                    {
                        if (small.sequence[S] == 0)
                        {
                            // if the small sequence ends at the same time as
                            // the large sequence, then we found a match
                            if (small.Next() == null)
                                return true;
                            S = 0;
                        }
                        if (large.Next() == null)
                        {
                            // reached the end of the large string, so no match
                            return false;
                        }
                        L = 0;
                    }
                    else if (small.sequence[S] == 0)
                    {
                        // if the small sequence ends before the large sequence,
                        // this cannot be a match.  this is to correctly emulate
                        // the .Net comparison, where, for example, a search for
                        // just "A" cannot match the (AE) symbol.

                        if (small.Next() == null)
                            return false;
                        S = 0;
                    }
                    if (large.sequence[L++] != small.sequence[S++])
                    {
                        // elements in the large and small strings do not match
                        return false;
                    }
                }
            }
        }

    }
}
