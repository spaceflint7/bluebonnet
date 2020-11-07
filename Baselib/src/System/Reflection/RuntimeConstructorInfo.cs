
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [System.Serializable]
    public sealed class RuntimeConstructorInfo : ConstructorInfo, ISerializable
    {
        #pragma warning disable 0436
        [java.attr.RetainType] private java.lang.reflect.Constructor JavaConstructor;
        #pragma warning restore 0436
        [java.attr.RetainType] private system.RuntimeType reflectedType;
        [java.attr.RetainType] private bool HasGenericArgs;
        [java.attr.RetainType] private bool HasUniqueArg;

        //
        // GetConstructor (called by system.RuntimeType.GetConstructorImpl)
        //

        public static ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder,
                                                     CallingConventions callConvention,
                                                     Type[] types, ParameterModifier[] modifiers,
                                                     RuntimeType initialType)
        {
            RuntimeMethodInfo.ValidateGetMethod(binder, callConvention, types, modifiers);

            if (initialType.IsValueType)
                return null;
            var numGenericArgsInInitialType = initialType.GetGenericArguments().Length;

            RuntimeConstructorInfo foundConstructor = null;

            BindingFlagsIterator.Run(bindingAttr, initialType, MemberTypes.Constructor,
                                     (javaAccessibleObject) =>
            {
                #pragma warning disable 0436
                var javaConstructor = (java.lang.reflect.Constructor) javaAccessibleObject;
                #pragma warning restore 0436

                var constructorTypes = DiscardClasses(initialType.JavaClassForArray(),
                                                      numGenericArgsInInitialType,
                                                      javaConstructor.getParameterTypes(),
                                                      out var hasGenericArgs,
                                                      out var hasUniqueArg);
                if (constructorTypes.Length == 0)
                {
                    javaConstructor.setAccessible(true);
                    foundConstructor = new RuntimeConstructorInfo(javaConstructor, initialType)
                    {
                        HasGenericArgs = hasGenericArgs,
                        HasUniqueArg   = hasUniqueArg,
                    };
                    return false; // stop iteration
                }

                return true; // continue iteration
            });

            return foundConstructor;



            static java.lang.Class[] DiscardClasses(java.lang.Class initialClass,
                                                    int numGenericArgsInInitialType,
                                                    java.lang.Class[] parameters,
                                                    out bool hasGenericArgs,
                                                    out bool hasUniqueArg)
            {
                int numParameters0 = parameters.Length;
                int numParameters = numParameters0;

                hasGenericArgs = false;
                while (    numGenericArgsInInitialType > 0 && numParameters > 0
                        && parameters[numParameters - 1] ==
                                (java.lang.Class) typeof(System.Type))
                {
                    numParameters--;
                    hasGenericArgs = true;
                }

                hasUniqueArg = false;
                if (numParameters > 0)
                {
                    var uniqueArg = parameters[numParameters - 1];
                    if (    uniqueArg.getDeclaringClass() == initialClass
                         && uniqueArg.getModifiers() == 0x1019
                         && uniqueArg.getDeclaredFields().Length == 0
                         && uniqueArg.getDeclaredMethods().Length == 0)
                    {
                        numParameters--;
                        hasUniqueArg = true;
                    }
                }

                if (numParameters != numParameters0)
                {
                    parameters = (java.lang.Class[])
                        java.util.Arrays.copyOf(parameters, numParameters);
                }

                return parameters;
            }
        }

        //
        // constructor
        //

        #pragma warning disable 0436
        private RuntimeConstructorInfo(java.lang.reflect.Constructor javaConstructor,
                                       system.RuntimeType reflectedType)
        #pragma warning restore 0436
        {
            this.JavaConstructor = javaConstructor;
            this.reflectedType = reflectedType;
        }

        //
        // Invoke
        //

        public override object Invoke(BindingFlags invokeAttr, Binder binder,
                                      object[] parameters, CultureInfo culture)
        {
            if (parameters == null)
                parameters = new object[0];
            int numParameters = parameters.Length;

            if (HasUniqueArg)
                parameters = java.util.Arrays.copyOf(parameters, ++numParameters);

            if (HasGenericArgs)
            {
                var typeArgs = reflectedType.GetGenericArguments();
                int n = typeArgs.Length;
                parameters = java.util.Arrays.copyOf(parameters, numParameters + n);
                for (int i = 0; i < n; i++)
                    parameters[numParameters++] = typeArgs[i];
            }

            return JavaConstructor.newInstance(parameters);
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder,
                                      object[] parameters, CultureInfo culture)
        {
            throw new PlatformNotSupportedException();
        }

        //
        //
        //

        public override MethodAttributes Attributes
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        //
        //
        //

        public override ParameterInfo[] GetParameters()
        {
            var classes = JavaConstructor.getParameterTypes();
            var infos = new ParameterInfo[classes.Length];
            for (int i = 0; i < classes.Length; i++)
            {
                var name = "arg" + i;
                var type = system.RuntimeType.GetType(classes[i]);
                infos[i] = new RuntimeParameterInfo(name, type, i);
            }
            return infos;
        }

        //
        //
        //

        public override Type ReflectedType => reflectedType;

        public override Type DeclaringType
            => system.RuntimeType.GetType(JavaConstructor.getDeclaringClass());

        public override string Name => ".ctor";

        public override System.RuntimeMethodHandle MethodHandle
            => throw new PlatformNotSupportedException();

        public override MethodImplAttributes GetMethodImplementationFlags()
            => throw new PlatformNotSupportedException();

        //
        //
        //

        public override string ToString() => $"Void {Name}()";

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

//
// declaration of java.lang.reflect.Constructor.
// this is needed because java 1.8 inserts a new java.lang.reflect.Executable
// class as the base class for Method and Constructor.
// this causes an error on Android, because the Executable class is missing.
//

namespace java.lang.reflect
{
    [java.attr.Discard] // discard in output
    public abstract class Constructor : AccessibleObject
    {
        public abstract object newInstance(object[] initargs);
        public abstract Class getDeclaringClass();
        public abstract Class[] getParameterTypes();
        public abstract int getModifiers();
        public abstract string getName();
    }
}
