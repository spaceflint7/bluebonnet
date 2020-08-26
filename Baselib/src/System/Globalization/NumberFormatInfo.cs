
using System;

namespace system.globalization {

    [Serializable]
    sealed public class NumberFormatInfo : ICloneable, IFormatProvider
    {
        public object GetFormat(Type formatType)
            => formatType == typeof(NumberFormatInfo) ? this : null;

        public object Clone() => this;

    }

}
