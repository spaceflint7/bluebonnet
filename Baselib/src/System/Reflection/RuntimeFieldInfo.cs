
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
        [java.attr.RetainType] private FieldAttributes cachedAttrs = 0;

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
            var list = new java.util.ArrayList();

            BindingFlagsIterator.Run(bindingAttr & ~BindingFlags.GetField,
                                     initialType, MemberTypes.Field,
                                     (javaAccessibleObject) =>
            {
                var javaField = (java.lang.reflect.Field) javaAccessibleObject;
                javaField.setAccessible(true);
                list.add(new RuntimeFieldInfo(javaField, initialType));
                return true;
            });

            return (RuntimeFieldInfo[]) list.toArray(new RuntimeFieldInfo[0]);
        }

        //
        //
        //

        public override System.Type FieldType
            => system.RuntimeType.GetType(JavaField.getType());

        public override object GetValue(object obj)
            => system.RuntimeType.UnboxJavaReturnValue(JavaField.get(obj));

        public override void SetValue(object obj, object value, BindingFlags invokeAttr,
                                      Binder binder, CultureInfo culture)
            => throw new PlatformNotSupportedException();

        public override FieldAttributes Attributes
        {
            get
            {
                var attrs = cachedAttrs;
                if (attrs == 0)
                {
                    int modifiers = JavaField.getModifiers();
                    if ((modifiers & java.lang.reflect.Modifier.PUBLIC) != 0)
                        attrs |= FieldAttributes.Public;
                    if ((modifiers & java.lang.reflect.Modifier.PRIVATE) != 0)
                        attrs |= FieldAttributes.Private;
                    if ((modifiers & java.lang.reflect.Modifier.PROTECTED) != 0)
                        attrs |= FieldAttributes.Family;
                    if ((modifiers & java.lang.reflect.Modifier.TRANSIENT) != 0)
                        attrs |= FieldAttributes.NotSerialized;

                    if ((modifiers & java.lang.reflect.Modifier.STATIC) != 0)
                    {
                        attrs |= FieldAttributes.Static;
                        if (    ((modifiers & java.lang.reflect.Modifier.FINAL) != 0)
                             && JavaField.getType().isPrimitive()
                             && JavaField.get(null) != null)
                        {
                            attrs |= FieldAttributes.Literal;
                        }
                    }

                    cachedAttrs = attrs;
                }
                return attrs;
            }
        }

        public override Type ReflectedType => reflectedType;

        public override Type DeclaringType
            => system.RuntimeType.GetType(JavaField.getDeclaringClass());

        public override string Name => JavaField.getName();

        public override object GetRawConstantValue()
        {
            if (0 != (JavaField.getModifiers() & (   java.lang.reflect.Modifier.STATIC
                                                   | java.lang.reflect.Modifier.FINAL))
                && JavaField.getType().isPrimitive())
            {
                return GetValue(null);
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
