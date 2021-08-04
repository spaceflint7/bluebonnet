
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [System.Serializable]
    public sealed class RuntimeMethodInfo : MethodInfo, ISerializable
    {
        #pragma warning disable 0436
        [java.attr.RetainType] private java.lang.reflect.Method JavaMethod;
        #pragma warning restore 0436
        [java.attr.RetainType] private string originalName;
        [java.attr.RetainType] private string strippedName;
        [java.attr.RetainType] private system.RuntimeType reflectedType;
        [java.attr.RetainType] private object[] typeArguments;

        [java.attr.RetainType] private MethodAttributes cachedAttrs = 0;

        [java.attr.RetainType] private int genericFlags;
        const int flgGenericMethod             = 0x10;
        const int flgGenericMethodDefinition   = 0x20;
        const int flgContainsGenericParameters = 0x40;
        const int flgCombineGenericArguments   = 0x80;

        //
        // GetMethod (called by system.RuntimeType.GetMethodImpl)
        //

        public static MethodInfo GetMethod(string name, BindingFlags bindingAttr,
                                           Binder binder, CallingConventions callConvention,
                                           Type[] types, ParameterModifier[] modifiers,
                                           RuntimeType initialType)
        {
            ThrowHelper.ThrowIfNull(name);
            RuntimeMethodInfo.ValidateGetMethod(binder, callConvention, types, modifiers);

            RuntimeMethodInfo foundMethod = null;

            BindingFlagsIterator.Run(bindingAttr, initialType, MemberTypes.Method,
                                     (javaAccessibleObject) =>
            {
                #pragma warning disable 0436
                var javaMethod = (java.lang.reflect.Method) javaAccessibleObject;
                #pragma warning restore 0436

                string originalName = javaMethod.getName();
                var compareName = RuntimeMethodInfo.AdjustedMethodName(originalName);

                if (name == compareName && CompareParameters(javaMethod, types))
                {
                    javaMethod.setAccessible(true);
                    var jmodifiers = javaMethod.getModifiers();
                    foundMethod = new RuntimeMethodInfo(javaMethod, jmodifiers, initialType,
                                                        originalName, compareName);
                    return false; // stop iteration
                }

                return true; // continue iteration
            });

            return foundMethod;



            #pragma warning disable 0436
            static bool CompareParameters(java.lang.reflect.Method javaMethod, Type[] types)
            {
                if (types == null)
                    return true;
                var paramTypes = javaMethod.getParameterTypes();
                if (types.Length != paramTypes.Length)
                    return false;
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    if (! object.ReferenceEquals(
                            types[i], system.RuntimeType.GetType(paramTypes[i])))
                        return false;
                }
                return true;
            }
            #pragma warning restore 0436
        }

        //
        // ValidateGetMethod
        //

        public static void ValidateGetMethod(Binder binder, CallingConventions callConvention,
                                             Type[] types, ParameterModifier[] modifiers)
        {
            if (binder != null)
                throw new PlatformNotSupportedException("non-null binder");
            if (callConvention != CallingConventions.Any)
                throw new PlatformNotSupportedException("calling convention must be Any");
            if (modifiers != null)
                throw new PlatformNotSupportedException("non-null modifiers");
        }

        //
        // AdjustedMethodName
        //

        public static string AdjustedMethodName(string name)
        {
            // note the actual suffix character below is configured
            // in CilMain.cs, with special considerations for Android.
            // we list all possible characters here, just in case.
            int idx = name.IndexOf('\u00AB'); // U+00AB Left-Pointing Double Angle Quotation Mark
            if (idx == -1)
                idx = name.IndexOf('\u00A1'); // U+00A1 Inverted Exclamation Mark
            if (idx == -1)
                idx = name.IndexOf('(');
            if (idx == -1)
                idx = name.IndexOf('!');

            return (idx == -1) ? name : name.Substring(0, idx);
        }

        //
        // constructor
        //

        #pragma warning disable 0436
        private RuntimeMethodInfo(java.lang.reflect.Method javaMethod, int modifiers,
                                  system.RuntimeType reflectedType,
                                  string originalName, string strippedName)
        #pragma warning restore 0436
        {
            this.JavaMethod = javaMethod;
            this.reflectedType = reflectedType;
            this.originalName = originalName;
            this.strippedName = strippedName;

            if (modifiers == -1)    // if called from MakeGenericMethod
                return;

            // analyze the method and the declaring type in order to decide what
            // type of a generic method this is.  the general idea is:
            // if the method takes more generic parameters, than the number of
            // arguments in the type, then it is a generic method definition.

            int originalNameLen = originalName.Length;
            char lastChar = (originalNameLen > 0)
                          ? originalName[originalNameLen - 1]
                          : (char) 0;
            // the actual suffix character is configured in CilMain.cs
            if (lastChar == '\u00A1' || lastChar == '!') // U+00A1 Inverted Exclamation Mark
            {
                if ((modifiers & java.lang.reflect.Modifier.STATIC) == 0)
                {
                    // if an instance method takes any type arguments at all,
                    // then it must be a generic method definition
                    genericFlags |= flgGenericMethod
                                 |  flgGenericMethodDefinition
                                 |  flgContainsGenericParameters;
                }
                else
                {
                    // count the number of type arguments in the declaring type
                    var typeArgsInType = reflectedType.GetGenericArguments();
                    int numTypeArgsInType = typeArgsInType.Length;

                    // count the number of type parameters in the method signature
                    int numTypeArgsInMethod = 0;
                    var paramTypes = javaMethod.getParameterTypes();
                    int paramIndex = paramTypes.Length;
                    while (paramIndex-- > 0)
                    {
                        var paramType = paramTypes[paramIndex];
                        if (paramType != (java.lang.Class) typeof(System.Type))
                            break;
                        numTypeArgsInMethod++;
                    }

                    if (numTypeArgsInMethod == numTypeArgsInType)
                    {
                        // a static method that takes a number of type parameters
                        // equal to the number of arguments in the declaring type.
                        // this means it is not a generic method, but it may not
                        // be invokable, if the declaring type is not concrete.

                        if (reflectedType.ContainsGenericParameters)
                            genericFlags |= flgContainsGenericParameters;

                        else if (reflectedType.IsGenericType)
                            genericFlags |= flgCombineGenericArguments;
                    }

                    else if (numTypeArgsInMethod > numTypeArgsInType)
                    {
                        // a static method that takes more parameters than the
                        // declaring type, i.e. it is a generic method definition.
                        genericFlags |= flgGenericMethod
                                     |  flgGenericMethodDefinition
                                     |  flgContainsGenericParameters;
                    }

                    else
                        throw new TypeLoadException(originalName);
                }
            }
            else
            {
                // a method that does not take any type argument may still be
                // not invokable, if the reflected type is not a concrete type.
                // note that this is true only for instance methods, as static
                // methods in a generic type will always take type arguments.

                if ((modifiers & java.lang.reflect.Modifier.STATIC) == 0)
                {
                    if (reflectedType.ContainsGenericParameters)
                        genericFlags |= flgContainsGenericParameters;

                    else if (reflectedType.IsGenericType)
                        genericFlags |= flgCombineGenericArguments;
                }
            }
        }

        //
        // MakeGenericMethod
        //

        public override MethodInfo MakeGenericMethod(Type[] typeArguments)
        {
            if (! IsGenericMethodDefinition)
                throw new InvalidOperationException();
            if (typeArguments == null)
                throw new ArgumentNullException();

            var implicitTypeArgs      = reflectedType.GetGenericArguments();
            int implicitTypeArgsCount = implicitTypeArgs.Length;
            var explicitTypeArgsCount = typeArguments.Length;
            int combinedTypeArgsCount = implicitTypeArgsCount + explicitTypeArgsCount;

            var combinedTypeArgs = new object[combinedTypeArgsCount];
            java.lang.System.arraycopy(
                            /* from  */ implicitTypeArgs, 0,
                            /* into  */ combinedTypeArgs, 0,
                            /* count */ implicitTypeArgsCount);
            java.lang.System.arraycopy(
                            /* from */ typeArguments, 0,
                            /* into */ combinedTypeArgs, implicitTypeArgsCount,
                            /* count */ explicitTypeArgsCount);

            var newMethod = new RuntimeMethodInfo(JavaMethod, -1, reflectedType,
                                                  originalName, strippedName);
            newMethod.typeArguments = combinedTypeArgs;
            newMethod.genericFlags = flgGenericMethod;

            for (int i = 0; i < combinedTypeArgsCount; i++)
            {
                var arg = combinedTypeArgs[i] as RuntimeType;
                if (arg == null)
                    throw new ArgumentNullException();
                if (arg.IsGenericParameter || arg.ContainsGenericParameters)
                    newMethod.genericFlags |= flgContainsGenericParameters;
            }

            return newMethod;
        }

        //
        // Invoke
        //

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder,
                                      object[] parameters, CultureInfo culture)
        {
            if (ContainsGenericParameters)
                throw new InvalidOperationException();
            if (invokeAttr != BindingFlags.Default)
                throw new PlatformNotSupportedException("bad binding flags " + invokeAttr);
            if (binder != null)
                throw new PlatformNotSupportedException("non-null binder");
            if (culture != null)
                throw new PlatformNotSupportedException("non-null culture");

            // combine the provided parameters and any suffix type arguments
            // that were previously injected by MakeGenericMethod

            var typeArgs = typeArguments;
            if (typeArgs == null && (genericFlags & flgCombineGenericArguments) != 0)
                typeArgs = typeArguments = reflectedType.GetGenericArguments();

            if (typeArgs != null)
            {
                if (parameters == null)
                    parameters = typeArgs;
                else
                {
                    int nParams = parameters.Length;
                    int nTypes = typeArgs.Length;
                    var newParams = new object[nParams + nTypes];
                    java.lang.System.arraycopy(parameters, 0, newParams, 0,       nParams);
                    java.lang.System.arraycopy(typeArgs,   0, newParams, nParams, nTypes);
                    parameters = newParams;
                }
            }

            int numParameters = (parameters != null) ? parameters.Length : 0;
            if (JavaMethod.getParameterTypes().Length != numParameters)
                throw new TargetParameterCountException();

            return JavaMethod.invoke(obj, parameters);
        }

        //
        //
        //

        public override bool IsGenericMethod
            => (genericFlags & flgGenericMethod) != 0;

        public override bool IsGenericMethodDefinition
            => (genericFlags & flgGenericMethodDefinition) != 0;

        public override bool ContainsGenericParameters
            => (genericFlags & flgContainsGenericParameters) != 0;

        //
        //
        //

        public override MethodAttributes Attributes
        {
            get
            {
                var attrs = cachedAttrs;
                if (attrs == 0)
                {
                    var modifiers = JavaMethod.getModifiers();
                    if ((modifiers & java.lang.reflect.Modifier.ABSTRACT) != 0)
                        attrs |= MethodAttributes.Abstract;
                    if ((modifiers & java.lang.reflect.Modifier.FINAL) != 0)
                        attrs |= MethodAttributes.Final;
                    if ((modifiers & java.lang.reflect.Modifier.STATIC) != 0)
                        attrs |= MethodAttributes.Static;

                    if ((modifiers & java.lang.reflect.Modifier.PUBLIC) != 0)
                        attrs |= MethodAttributes.Public;
                    else if ((modifiers & java.lang.reflect.Modifier.PROTECTED) != 0)
                        attrs |= MethodAttributes.Family;
                    else
                        attrs |= MethodAttributes.Private;

                    cachedAttrs = attrs;
                }
                return attrs;
            }
        }

        //
        // GetParameters
        //

        public override ParameterInfo[] GetParameters()
        {
            var classes = JavaMethod.getParameterTypes();
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
            => system.RuntimeType.GetType(JavaMethod.getDeclaringClass());

        public override string Name => strippedName;

        public override System.RuntimeMethodHandle MethodHandle
            => throw new PlatformNotSupportedException();

        public override MethodInfo GetBaseDefinition()
            => throw new PlatformNotSupportedException();

        public override MethodImplAttributes GetMethodImplementationFlags()
            => throw new PlatformNotSupportedException();

        //
        //
        //

        public override string ToString() => strippedName;

        //
        // custom attributes
        //

        public override bool IsDefined(Type attributeType, bool inherit)
            => throw new PlatformNotSupportedException();

        public override object[] GetCustomAttributes(bool inherit)
            => throw new PlatformNotSupportedException();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            => throw new PlatformNotSupportedException();

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
            => throw new PlatformNotSupportedException();

        //
        // ISerializable
        //

        public void GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new PlatformNotSupportedException();

        //
        //
        //

        static RuntimeMethodInfo()
        {
            system.Util.DefineException(
                    (java.lang.Class) typeof(java.lang.reflect.InvocationTargetException),
                    (exc) => new System.Reflection.TargetInvocationException(exc.getMessage(),
                                                    Util.TranslateException(exc.getCause()))
            );

            system.Util.DefineException(
                    (java.lang.Class) typeof(java.lang.NoSuchMethodException),
                    (exc) => new System.MissingMethodException(exc.getMessage())
            );
        }

    }

}

//
// declaration of java.lang.reflect.Method.
// this is needed because java 1.8 inserts a new java.lang.reflect.Executable
// class as the base class for Method and Constructor.
// this causes an error on Android, because the Executable class is missing.
//

namespace java.lang.reflect
{
    [java.attr.Discard] // discard in output
    public abstract class Method : AccessibleObject
    {
        public abstract Class getDeclaringClass();
        public abstract string getName();
        public abstract Class[] getParameterTypes();
        public abstract Class getReturnType();
        public abstract int getModifiers();
        public abstract object invoke(object obj, object[] args);
    }
}
