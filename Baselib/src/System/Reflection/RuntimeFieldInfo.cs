
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
        // GetFields (called by system.RuntimeType.GetFields()
        //

        public static FieldInfo[] GetFields(BindingFlags bindingAttr, RuntimeType initialType)
        {
            var list = new System.Collections.Generic.List<FieldInfo>();

            BindingFlagsIterator.Run(bindingAttr, initialType, MemberTypes.Field,
                                     (javaAccessibleObject) =>
            {
                var javaField = (java.lang.reflect.Field) javaAccessibleObject;
                javaField.setAccessible(true);
                list.Add(new RuntimeFieldInfo(javaField, initialType));
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

        public override string Name => JavaField.getName();

        public override object GetRawConstantValue()
        {
            if ((JavaField.getModifiers() & java.lang.reflect.Modifier.STATIC) != 0)
            {
                var value = JavaField.get(null);
                switch (value)
                {
                    case java.lang.Boolean boolBox:
                        return system.Boolean.Box(boolBox.booleanValue() ? 1 : 0);
                    case java.lang.Byte byteBox:
                        return system.SByte.Box(byteBox.byteValue());
                    case java.lang.Character charBox:
                        return system.Char.Box(charBox.charValue());
                    case java.lang.Short shortBox:
                        return system.Int16.Box(shortBox.shortValue());
                    case java.lang.Integer intBox:
                        return system.Int32.Box(intBox.intValue());
                    case java.lang.Long longBox:
                        return system.Int64.Box(longBox.longValue());
                    case java.lang.Float floatBox:
                        return system.Single.Box(floatBox.floatValue());
                    case java.lang.Double doubleBox:
                        return system.Double.Box(doubleBox.doubleValue());
                }
            }
            throw new System.NotSupportedException();
        }

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
