
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [System.Serializable]
    public sealed class RuntimeParameterInfo : ParameterInfo, ISerializable
    {
        public RuntimeParameterInfo(string name, System.Type type, int pos)
        {
            NameImpl = name;
            ClassImpl = type;
            PositionImpl = pos;
        }

        //
        // ISerializable
        //

        public void GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new PlatformNotSupportedException();
    }
}
