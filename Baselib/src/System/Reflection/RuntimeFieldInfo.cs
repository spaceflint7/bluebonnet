
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [System.Serializable]
    public sealed class RuntimeFieldInfo : FieldInfo, ISerializable
    {
        [java.attr.RetainType] public java.lang.reflect.Field JavaField;
        [java.attr.RetainType] public system.RuntimeType reflectedType;

        //
        // constructor
        //

        private RuntimeFieldInfo(java.lang.reflect.Field javaField,
                                 system.RuntimeType reflectedType)
        {
            this.JavaField = javaField;
            this.reflectedType = reflectedType;
        }

        //
        // GetFields (called by system.RuntimeType.GetFields(GetFields)
        //

        public static FieldInfo[] GetFields(BindingFlags bindingAttr, RuntimeType initialType)
        {
            var list = new System.Collections.Generic.List<FieldInfo>();

            BindingFlagsIterator.Run(bindingAttr, initialType, MemberTypes.Field,
                                     (javaAccessibleObject) =>
            {
                list.Add(new RuntimeFieldInfo((java.lang.reflect.Field) javaAccessibleObject,
                                              initialType));
                return true;
            });

            return list.ToArray();
        }

        public override System.Type FieldType
            => system.RuntimeType.GetType(JavaField.getType());

        //
        //
        //

        public override object GetValue(object obj)
            => throw new PlatformNotSupportedException();

        public override void SetValue(object obj, object value, BindingFlags invokeAttr,
                                      Binder binder, CultureInfo culture)
            => throw new PlatformNotSupportedException();

        public override System.Reflection.FieldAttributes Attributes
            => throw new PlatformNotSupportedException();

        public override System.Type DeclaringType
            => throw new PlatformNotSupportedException();

        public override System.Type ReflectedType
            => throw new PlatformNotSupportedException();

        public override string Name
            => throw new PlatformNotSupportedException();

        public override System.RuntimeFieldHandle FieldHandle
            => throw new PlatformNotSupportedException();

        //
        // custom attributes
        //

        public override bool IsDefined(Type attributeType, bool inherit)
            => throw new PlatformNotSupportedException();

        public override object[] GetCustomAttributes(bool inherit)
            => throw new PlatformNotSupportedException();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            => throw new PlatformNotSupportedException();

        //
        // ISerializable
        //

        public void GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new PlatformNotSupportedException();

    }

}
