
using System;
using System.Runtime.Serialization;

namespace system.globalization {

    [Serializable]
    public class TextInfo : ICloneable, IDeserializationCallback
    {
        [java.attr.RetainType] CultureInfo CultureInfoRef;
        [java.attr.RetainType] java.util.Locale JavaLocale;

        //
        // called by CultureInfo
        //

        public TextInfo(CultureInfo cultureInfo, java.util.Locale locale)
        {
            CultureInfoRef = cultureInfo;
            JavaLocale = locale;
        }

        //
        //
        //

        public static TextInfo CurrentTextInfo
            => system.threading.Thread.CurrentThread.CurrentCulture.TextInfo;

        public static TextInfo InvariantTextInfo
            => system.globalization.CultureInfo.InvariantCulture.TextInfo;

        //
        // ToLower, ToUpper, ToTitleCase
        //

        public virtual char ToLower(char c)
            => ((java.lang.String) (object)
                        ((java.lang.String) (object) java.lang.String.valueOf(c))
                                .toLowerCase(JavaLocale)).charAt(0);

        public virtual string ToLower(string s)
            => ((java.lang.String) (object) s).toLowerCase(JavaLocale);

        public virtual char ToUpper(char c)
            => ((java.lang.String) (object)
                        ((java.lang.String) (object) java.lang.String.valueOf(c))
                                .toUpperCase(JavaLocale)).charAt(0);

        public virtual string ToUpper(string s)
            => ((java.lang.String) (object) s).toUpperCase(JavaLocale);

        public virtual char ToTitleCase(string s)
        {
            // java.lang.Character.toTitleCase(java.lang.String.toUpperCase(locale))
            throw new System.PlatformNotSupportedException();
        }

        //
        // Equals
        //

        public override bool Equals(object other)
            => other is TextInfo otherTextInfo && CultureName == otherTextInfo.CultureName;

        public override int GetHashCode() => CultureName.GetHashCode();

        public string CultureName => CultureInfoRef.Name;

        public override string ToString() => "TextInfo - " + CultureInfoRef.ToString();

        public bool IsReadOnly => true;

        public virtual object Clone() => MemberwiseClone();

        //
        // private methods called by system.OrdinalComparer
        // and system.OrdinalRandomizedComparer
        //

        public static int GetHashCodeOrdinalIgnoreCase(string s)
            => ((java.lang.String) (object) s).toLowerCase(java.util.Locale.ROOT).GetHashCode();

        public static int GetHashCodeOrdinalIgnoreCase(string s,
                                            bool forceRandomizedHashing, long additionalEntropy)
            => GetHashCodeOrdinalIgnoreCase(s);

        //
        //
        //

        void IDeserializationCallback.OnDeserialization(object sender)
            => throw new PlatformNotSupportedException();

    }

}
