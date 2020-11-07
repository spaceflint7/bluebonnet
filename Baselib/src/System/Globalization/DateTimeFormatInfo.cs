
using System;

namespace system.globalization {

    [Serializable]
    sealed public class DateTimeFormatInfo : ICloneable, IFormatProvider
    {
        public object GetFormat(Type formatType)
            => formatType == typeof(DateTimeFormatInfo) ? this : null;

        public object Clone() => this;

    }

}
