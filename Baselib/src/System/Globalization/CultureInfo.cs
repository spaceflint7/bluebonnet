
using System;

namespace system.globalization {

    [Serializable]
    public class CultureInfo : ICloneable, IFormatProvider {

        [java.attr.RetainType] java.util.Locale JavaLocale;
        [java.attr.RetainType] volatile CompareInfo CompareInfoRef;
        [java.attr.RetainType] volatile TextInfo TextInfoRef;



        public CultureInfo(string name) : this(name, true)
        {
        }



        public CultureInfo(string name, bool useUserOverride)
        {
            if (name == null)
                throw new System.ArgumentNullException();
            var locale = (java.util.Locale) _LocaleCache.get(name);
            if (locale == null)
            {
                int sep = name.IndexOf('-');
                if (sep == -1)
                {
                    if (name == "")
                        locale = java.util.Locale.ROOT;
                    locale = new java.util.Locale(name);
                }
                else
                {
                    var language = name.Substring(0, sep);
                    var country = name.Substring(sep + 1);
                    locale = new java.util.Locale(language, country);
                }
                locale = (java.util.Locale)
                            _LocaleCache.putIfAbsent(name, locale) ?? locale;
            }
            JavaLocale = locale;
            CompareInfoRef = new CompareInfo(this, JavaLocale);
            TextInfoRef = new TextInfo(this, JavaLocale);
        }



        public static CultureInfo GetCultureInfo(string name)
        {
            var culture = (CultureInfo) _CultureCache.get(name);
            if (culture == null)
            {
                culture = new CultureInfo(name, false);
                culture = (CultureInfo)
                            _CultureCache.putIfAbsent(name, culture) ?? culture;
            }
            return culture;
        }



        public virtual string TwoLetterISOLanguageName
        {
            get
            {
                var language = JavaLocale.getLanguage();
                if (language == "iw")
                    language = "he";
                else if (language == "ji")
                    language = "yi";
                else if (language == "in")
                    language = "id";
                return language;
            }
        }

        public virtual string Name
        {
            get
            {
                var country = JavaLocale.getCountry();
                if (country != "")
                    country = "-" + country;
                return TwoLetterISOLanguageName + country;
            }
        }

        public override string ToString() => Name;



        public virtual CompareInfo CompareInfo => CompareInfoRef;

        public virtual TextInfo TextInfo => TextInfoRef;

        public static CultureInfo CurrentCulture
            => system.threading.Thread.CurrentThread.CurrentCulture;

        public static CultureInfo InvariantCulture => s_InvariantCultureInfo;

        public virtual object Clone() => throw new System.NotImplementedException();

        public virtual object GetFormat(Type formatType) => null;

        //
        // static initialization
        //

        static CultureInfo()
        {
            _LocaleCache = new java.util.concurrent.ConcurrentHashMap();
            _LocaleCache.put("", java.util.Locale.ROOT);

            _CultureCache = new java.util.concurrent.ConcurrentHashMap();
            s_InvariantCultureInfo = new CultureInfo("", false);
            _CultureCache.put("", s_InvariantCultureInfo);
        }

        [java.attr.RetainType] private static java.util.concurrent.ConcurrentHashMap _LocaleCache;
        [java.attr.RetainType] private static java.util.concurrent.ConcurrentHashMap _CultureCache;
        [java.attr.RetainType] private static volatile CultureInfo s_InvariantCultureInfo;

    }

}