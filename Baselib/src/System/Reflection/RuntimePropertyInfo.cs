
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [System.Serializable]
    public sealed class RuntimePropertyInfo : PropertyInfo, ISerializable
    {
        #pragma warning disable 0436
        [java.attr.RetainType] private java.lang.reflect.Method JavaGetMethod;
        [java.attr.RetainType] private java.lang.reflect.Method JavaSetMethod;
        #pragma warning restore 0436
        [java.attr.RetainType] private string propertyName;
        [java.attr.RetainType] private system.RuntimeType propertyType;
        [java.attr.RetainType] private system.RuntimeType reflectedType;
        [java.attr.RetainType] private system.RuntimeType declaringType;

        //
        // GetMethod (called by system.RuntimeType.GetPropertyImpl)
        //

        public static PropertyInfo GetProperty(string name, BindingFlags bindingAttr,
                                               Binder binder, Type returnType,
                                               Type[] types, ParameterModifier[] modifiers,
                                               RuntimeType initialType)
        {
            ThrowHelper.ThrowIfNull(name);

            if (types != null && types.Length != 0) // if indexed property
                throw new PlatformNotSupportedException();

            #pragma warning disable 0436
            java.lang.reflect.Method getMethod = null;
            java.lang.Class          getClass  = null;
            java.lang.reflect.Method setMethod = null;
            java.lang.Class          setClass  = null;
            #pragma warning restore 0436

            BindingFlagsIterator.Run(bindingAttr & ~BindingFlags.GetProperty,
                                     initialType, MemberTypes.Method,
                                     (javaAccessibleObject) =>
            {
                #pragma warning disable 0436
                var javaMethod = (java.lang.reflect.Method) javaAccessibleObject;
                #pragma warning restore 0436

                var cls = IsGetMethod(javaMethod, name, returnType);
                if (cls != null)
                {
                    getMethod = javaMethod;
                    getClass = cls;
                }
                else
                {
                    cls = IsSetMethod(javaMethod, name, returnType);
                    if (cls != null)
                    {
                        setMethod = javaMethod;
                        setClass = cls;
                    }
                }

                return true;
            });

            if (getClass != null)
            {
                if (setClass != null && setClass != getClass)
                    setMethod = null;
            }
            else if (setClass != null)
            {
                getClass = setClass;
            }
            else // neither get nor set methods
                return null;

            return new RuntimePropertyInfo(getMethod, setMethod, getClass, initialType);



            #pragma warning disable 0436
            static java.lang.Class IsGetMethod(java.lang.reflect.Method javaMethod,
                                               string propertyName, Type propertyType)
            {
                if (javaMethod.getName() == "get_" + propertyName)
                {
                    javaMethod.setAccessible(true);
                    var returnClass = javaMethod.getReturnType();
                    if (    object.ReferenceEquals(propertyType, null)
                         || object.ReferenceEquals(propertyType,
                                system.RuntimeType.GetType(returnClass)))
                    {
                        return returnClass;
                    }
                }
                return null;
            }

            static java.lang.Class IsSetMethod(java.lang.reflect.Method javaMethod,
                                               string propertyName, Type propertyType)
            {
                if (javaMethod.getName() == "set_" + propertyName)
                {
                    javaMethod.setAccessible(true);
                    var paramClasses = javaMethod.getParameterTypes();
                    if (paramClasses.Length == 1)
                    {
                        var paramClass = paramClasses[0];
                        if (    object.ReferenceEquals(propertyType, null)
                             || object.ReferenceEquals(propertyType,
                                    system.RuntimeType.GetType(paramClass)))
                        {
                            return paramClass;
                        }
                    }
                }
                return null;
            }
            #pragma warning restore 0436
        }

        //
        // GetProperties (called by system.RuntimeType.GetProperties()
        //

        public static PropertyInfo[] GetProperties(BindingFlags bindingAttr,
                                                   RuntimeType initialType)
        {
            var list = new java.util.ArrayList();

            BindingFlagsIterator.Run(bindingAttr & ~BindingFlags.GetProperty,
                                     initialType, MemberTypes.Method,
                                     (javaAccessibleObject) =>
            {
                #pragma warning disable 0436
                var javaMethod = (java.lang.reflect.Method) javaAccessibleObject;
                #pragma warning restore 0436
                if (javaMethod.getName().StartsWith("get_"))
                {
                    javaMethod.setAccessible(true);
                    var returnClass = javaMethod.getReturnType();
                    if (    returnClass != java.lang.Void.TYPE
                         && javaMethod.getParameterTypes().Length == 0)
                    {
                        list.add(new RuntimePropertyInfo(
                                javaMethod, null, returnClass, initialType));
                    }
                }
                return true;
            });

            return (RuntimePropertyInfo[]) list.toArray(new RuntimePropertyInfo[0]);
        }

        //
        // constructor
        //

        #pragma warning disable 0436
        private RuntimePropertyInfo(java.lang.reflect.Method javaGetMethod,
                                    java.lang.reflect.Method javaSetMethod,
                                    java.lang.Class javaClass,
                                    system.RuntimeType reflectedType)
        #pragma warning restore 0436
        {
            var getOrSetMethod = javaGetMethod ?? javaSetMethod;
            propertyName = getOrSetMethod.getName().Substring(4);
            propertyType = (system.RuntimeType) system.RuntimeType.GetType(javaClass);
            this.JavaGetMethod = javaGetMethod;
            this.JavaSetMethod = javaSetMethod;
            this.reflectedType = reflectedType;
            this.declaringType = (system.RuntimeType) system.RuntimeType.GetType(
                                        getOrSetMethod.getDeclaringClass());
        }

        //
        //
        //

        public override PropertyAttributes Attributes
            => throw new PlatformNotSupportedException();

        public override MethodInfo[] GetAccessors(bool nonPublic)
            => throw new PlatformNotSupportedException();

        public override MethodInfo GetGetMethod(bool nonPublic)
            => throw new PlatformNotSupportedException();

        public override MethodInfo GetSetMethod(bool nonPublic)
            => throw new PlatformNotSupportedException();

        public override bool CanRead  => (JavaGetMethod != null);
        public override bool CanWrite => (JavaSetMethod != null);

        //
        //
        //

        public override Type PropertyType => propertyType;

        public override ParameterInfo[] GetIndexParameters()
          => throw new PlatformNotSupportedException();

        public override object GetValue(object obj, BindingFlags invokeAttr,
                                        Binder binder, object[] index, CultureInfo culture)
        {
            invokeAttr &= ~(   BindingFlags.Public | BindingFlags.NonPublic
                             | BindingFlags.Instance | BindingFlags.GetProperty);
            if (invokeAttr != BindingFlags.Default)
                throw new PlatformNotSupportedException("bad binding flags " + invokeAttr);
            if (binder != null)
                throw new PlatformNotSupportedException("non-null binder");
            if (culture != null)
                throw new PlatformNotSupportedException("non-null culture");

            if (index != null || JavaGetMethod == null)
                throw new ArgumentException();

            return system.RuntimeType.UnboxJavaReturnValue(
                            JavaGetMethod.invoke(obj, null));
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr,
                                      Binder binder, object[] index, CultureInfo culture)
          => throw new PlatformNotSupportedException();

        //
        //
        //

        public override Type ReflectedType => reflectedType;

        public override Type DeclaringType => declaringType;

        public override string Name => propertyName;

        //
        // custom attributes
        //

        public override bool IsDefined(Type attributeType, bool inherit)
            => throw new PlatformNotSupportedException();

        public override object[] GetCustomAttributes(bool inherit)
            => throw new PlatformNotSupportedException();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType is system.RuntimeType attributeRuntimeType)
            {
                // we don't yet translate .Net attributes to Java annotations,
                // so we have to fake some attributes to support F# ToString()
                var attrs = system.reflection.FSharpCompat.GetCustomAttributes_Property(
                                declaringType.JavaClassForArray(),
                                attributeRuntimeType.JavaClassForArray());
                if (attrs != null)
                    return attrs;
            }
            throw new PlatformNotSupportedException();
        }

        //
        // ISerializable
        //

        public void GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new PlatformNotSupportedException();

    }
}
