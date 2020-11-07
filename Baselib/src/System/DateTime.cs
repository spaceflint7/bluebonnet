
using System;
using System.Runtime.Serialization;

namespace system
{

    public struct DateTime : System.IComparable, IFormattable, IConvertible, ISerializable,
                             IComparable<DateTime>, IEquatable<DateTime>
    {
        [java.attr.RetainType] private java.util.Calendar JavaCalendar;
        [java.attr.RetainType] private DateTimeKind kind;
        [java.attr.RetainType] private static java.util.TimeZone TimeZoneUTC =
                                        java.util.TimeZone.getTimeZone("UTC");

        //
        // Constants
        //

        // number of ticks in a millisecond
        const long TicksPerMillisecond = 10000L;
        const long TicksPerDay = 86400L * 1000L * TicksPerMillisecond;
        // number of days between 1/1/0001 and 12/31/1969 is 719,162
        const long MinTicks = 719162L * TicksPerDay;
        // number of days between 1/1/0001 and 1/1/10000 is 3,652,059
        const long MaxTicks = 3652059 * TicksPerDay - 1;

        //
        // Constructors (Now)
        //

        private DateTime(java.util.Calendar javaCalendar, DateTimeKind kind)
        {
            JavaCalendar = javaCalendar;
            this.kind = kind;
        }

        public static DateTime Now
            => new DateTime(java.util.Calendar.getInstance(), DateTimeKind.Local);

        public static DateTime UtcNow
            => new DateTime(java.util.Calendar.getInstance(TimeZoneUTC), DateTimeKind.Utc);

        //
        // Constructors (Ticks)
        //

        public DateTime(long ticks)
        {
            JavaCalendar = CalendarFromTicks(ticks);
            kind = DateTimeKind.Unspecified;
        }

        public DateTime(long ticks, DateTimeKind kind)
        {
            JavaCalendar = CalendarFromTicks(ticks);
            if (kind < DateTimeKind.Unspecified || kind > DateTimeKind.Local)
                throw new ArgumentException();
            this.kind = kind;
        }

        private static java.util.Calendar CalendarFromTicks(long ticks)
        {
            if (ticks < MinTicks || ticks > MaxTicks)
                throw new ArgumentOutOfRangeException();
            var cal = java.util.Calendar.getInstance(TimeZoneUTC);
            cal.setTimeInMillis((ticks - MinTicks) / TicksPerMillisecond);
            return cal;
        }

        //
        // Properties
        //

        public DateTimeKind Kind => kind;

        public long Ticks =>   MinTicks + TicksPerMillisecond *
                                          (   JavaCalendar.getTimeInMillis()
                                            + JavaCalendar.getTimeZone().getRawOffset());

        //
        // ToString
        //

        public override string ToString() => JavaCalendar.ToString();

        public string ToString(IFormatProvider provider) => ToString(null, provider);

        public string ToString(string format, IFormatProvider provider)
        {
            if (provider != null)
            {
                if (    (provider is System.Globalization.DateTimeFormatInfo)
                     || null != provider.GetFormat(
                            typeof(System.Globalization.DateTimeFormatInfo)))
                {
                    throw new PlatformNotSupportedException("provider with a DateTimeFormatInfo");
                }
                if (! object.ReferenceEquals(
                            provider, system.globalization.CultureInfo.CurrentCulture))
                {
                    throw new PlatformNotSupportedException("provider with CultureInfo != current");
                }
            }
            var formatter = java.text.DateFormat.getInstance();
            formatter.setTimeZone(JavaCalendar.getTimeZone());
            return formatter.format(JavaCalendar.getTime());
        }

        //
        // Equality
        //

        public static bool Equals(DateTime t1, DateTime t2)
            => t1.JavaCalendar.Equals(t2.JavaCalendar);

        public bool Equals(DateTime value) => Equals(this, value);

        public override bool Equals(object value)
            => value is DateTime valueAsDateTime
                    ? JavaCalendar.Equals(valueAsDateTime.JavaCalendar) : false;

        public override int GetHashCode() => JavaCalendar.GetHashCode();

        //
        // Compare and CompareTo
        //

        /*public static int Compare(DateTime t1, DateTime t2)
            => value is DateTime valueAsDateTime*/

        public int CompareTo(DateTime value)
            => JavaCalendar.compareTo(value.JavaCalendar);

        public int CompareTo(object value)
            => (value is DateTime valueAsDateTime) ? CompareTo(valueAsDateTime)
                    : (value == null) ? 1 : throw new ArgumentException();

        //
        // IConvertible
        //

        public System.TypeCode GetTypeCode() => System.TypeCode.DateTime;

        bool System.IConvertible.ToBoolean(System.IFormatProvider provider)
            => throw new InvalidCastException();
        char System.IConvertible.ToChar(System.IFormatProvider provider)
            => throw new InvalidCastException();
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider)
            => throw new InvalidCastException();
        byte System.IConvertible.ToByte(System.IFormatProvider provider)
            => throw new InvalidCastException();
        short System.IConvertible.ToInt16(System.IFormatProvider provider)
            => throw new InvalidCastException();
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider)
            => throw new InvalidCastException();
        int System.IConvertible.ToInt32(System.IFormatProvider provider)
            => throw new InvalidCastException();
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider)
            => throw new InvalidCastException();
        long System.IConvertible.ToInt64(System.IFormatProvider provider)
            => throw new InvalidCastException();
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider)
            => throw new InvalidCastException();
        float System.IConvertible.ToSingle(System.IFormatProvider provider)
            => throw new InvalidCastException();
        double System.IConvertible.ToDouble(System.IFormatProvider provider)
            => throw new InvalidCastException();
        System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider provider)
            => throw new InvalidCastException();
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider)
            => (System.DateTime) (object) this;
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider)
            => system.Convert.DefaultToType((System.IConvertible) this, type, provider);

        //
        // Operators
        //

        public static DateTime operator -(DateTime d, TimeSpan t)
        {
            var dTicks = d.Ticks;
            var tTicks = t.Ticks;
            if (dTicks < tTicks || dTicks - MaxTicks > tTicks)
                throw new ArgumentOutOfRangeException();
            return new DateTime(dTicks - tTicks, d.kind);
        }

        public static TimeSpan operator - (DateTime d1, DateTime d2)
            => new TimeSpan(d1.Ticks - d2.Ticks);

        public static bool operator == (DateTime d1, DateTime d2)
            => d1.Ticks == d2.Ticks;

        public static bool operator != (DateTime d1, DateTime d2)
            => d1.Ticks != d2.Ticks;

        public static bool operator < (DateTime d1, DateTime d2)
            => d1.Ticks < d2.Ticks;

        public static bool operator <= (DateTime d1, DateTime d2)
            => d1.Ticks <= d2.Ticks;

        public static bool operator > (DateTime d1, DateTime d2)
            => d1.Ticks > d2.Ticks;

        public static bool operator >= (DateTime d1, DateTime d2)
            => d1.Ticks >= d2.Ticks;

        //
        // ISerializable
        //

        public void GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new PlatformNotSupportedException();

    }

}
