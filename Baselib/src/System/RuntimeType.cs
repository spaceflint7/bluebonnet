
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system
{

    [Serializable]
    public class RuntimeType : system.reflection.TypeInfo, ISerializable, ICloneable
    {

        private sealed class GenericData
        {
            [java.attr.RetainType] public RuntimeType PrimaryType;
            [java.attr.RetainType] public Type[] ArgumentTypes;
            [java.attr.RetainType] public string Variance;
            [java.attr.RetainType] public object StaticData;
        }

        [java.attr.RetainType] private readonly java.lang.Class JavaClass;
        [java.attr.RetainType] private java.lang.Class JavaClassForArrayCached;
        [java.attr.RetainType] private readonly GenericData Generic;

        [java.attr.RetainType] private static GenericData GenericDataForSetStatic;

        [java.attr.RetainType] public TypeAttributes CachedAttrs;
        const TypeAttributes AttrInitialized = (TypeAttributes) 0x40000000;
        const TypeAttributes AttrTypeCode    = (TypeAttributes) 0x1F000000;
        const TypeAttributes AttrPrivateMask = unchecked ((TypeAttributes) 0xFF000000);

        //
        // construction methods
        //

        private RuntimeType(java.lang.Class _cls, object genericData)
        {
            JavaClass = _cls;

            if (genericData != null)
            {
                Generic = (GenericData) genericData;
            }
            else if (JavaClass != null)
            {
                var javaClass = JavaClass;
                if (javaClass.isArray())
                    javaClass = javaClass.getComponentType();

                if (((java.lang.Class) typeof (system.IGenericEntity)).isAssignableFrom(javaClass))
                {
                    try
                    {
                        Generic = CreateGeneric(javaClass);
                    }
                    catch (System.Exception e)
                    {
                        throw PrintException(new TypeInitializationException(JavaClass.getName(), e));
                    }
                }
            }
            else
                throw PrintException(new TypeLoadException("null type"));

            GenericData CreateGeneric(java.lang.Class javaClass)
            {
                // a generic type should include a method of the form:
                //      (class) -generic-info-method(system.GenericType t1,...)
                // the return value (class) is the static data class, or void.
                // the number of parameters determines the number of type arguments.
                // all parameters should be of the type system.GenericType.

                var searchClass = javaClass;
                if (javaClass.isInterface())
                {
                    // Android 'D8' desugars static methods on an interface,
                    // by moving them to a separate class, which we might not
                    // be able to locate here.  so TypeBuilder (see also there)
                    // moves the generic info method to a static inner class.
                    foreach (var nestedClass in javaClass.getDeclaredClasses())
                    {
                        // nested class:  public, final, static, synthetic
                        if (nestedClass.getModifiers() == 0x1019)
                        {
                            searchClass = nestedClass;
                            break;
                        }
                    }
                }

                #pragma warning disable 0436
                foreach (java.lang.reflect.Method method in
                            (java.lang.reflect.Method[]) (object) searchClass.getDeclaredMethods())
                {
                    // search for a method:  public static final synthetic bridge
                    if (method.getModifiers() != 0x1059)
                        continue;

                    int genericCount = 0;
                    foreach (var parameter in method.getParameterTypes())
                    {
                        if (parameter.Equals((java.lang.Class) typeof (system.GenericType)))
                            genericCount++;
                        else
                        {
                            genericCount = 0;
                            break;
                        }
                    }
                    if (genericCount == 0)
                        continue;

                    // create an array of generic parameter types

                    var generic = new GenericData();
                    generic.PrimaryType = this;

                    var argTypes = new Type[genericCount];
                    while (genericCount-- > 0)
                        argTypes[genericCount] = new RuntimeType(null, generic);

                    // create the generic data for the new type

                    generic = new GenericData();
                    generic.PrimaryType = this;
                    generic.ArgumentTypes = argTypes;

                    generic.StaticData = method.getReturnType();
                    if (generic.StaticData.Equals(java.lang.Void.TYPE))
                        generic.StaticData = null;

                    // an interface or delegate may include a variance string.
                    // note that any static fields in a generic class have been
                    // moved to the static data class, the one remaining field
                    // should be a string type that indicates the variance.

                    var superClass = javaClass.getSuperclass();

                    if (    javaClass.isInterface()
                         || superClass == DelegateClass
                         || superClass == MulticastDelegateClass)
                    {
                        foreach (var field in searchClass.getDeclaredFields())
                        {
                            // search for field:  public static final synthetic transient
                            if (    field.getModifiers() == 0x1099
                                 && field.getType() == (java.lang.Class) typeof(java.lang.String))
                            {
                                generic.Variance = (string) field.get(null);
                                break;
                            }
                        }
                    }

                    return generic;
                }
                #pragma warning restore 0436

                return null;
            }
        }



        static RuntimeType MakeGenericType(RuntimeType primaryType, Type[] typeArguments, TypeKey typeKey)
        {
            var oldGeneric = primaryType.Generic;
            if (oldGeneric == null)
            {
                // primary type is not generic?
                throw PrintException(new InvalidOperationException(
                    primaryType.JavaClass != null ? primaryType.JavaClass.getName() : "null type"));
            }

            var newGeneric = new GenericData();
            newGeneric.PrimaryType = primaryType;
            newGeneric.Variance = oldGeneric.Variance;

            var javaClass = primaryType.JavaClass;

            FillNameAndArgs(newGeneric, oldGeneric, typeArguments);

            // the static initializer that we call in FillStaticData can access its
            // own type, so we must make that type accessible even before returning

            var newType = new RuntimeType(javaClass, newGeneric);
            TypeCache.put(typeKey, newType);

            FillStaticData(newGeneric, oldGeneric, javaClass);

            return newType;

            void FillNameAndArgs(GenericData newGeneric, GenericData oldGeneric,
                                 Type[] typeArguments)
            {
                Type[] args = null;
                bool throwArgumentNullException = false;
                bool throwArgumentException = false;

                if (typeArguments == null)
                    throwArgumentNullException = true;
                else
                {
                    int n = typeArguments.Length;
                    if (n != oldGeneric.ArgumentTypes.Length || n == 0)
                        throwArgumentException = true;
                    else
                    {
                        args = new Type[n];
                        for (int i = 0; i < n; i++)
                        {
                            var typeArg = typeArguments[i];
                            if (object.ReferenceEquals(typeArg, null))
                            {
                                throwArgumentNullException = true;
                                break;
                            }
                            if (typeArg.IsByRef || object.ReferenceEquals(typeArg, VoidType))
                            {
                                throwArgumentException = true;
                                break;
                            }
                            args[i] = typeArg;
                        }
                    }
                }

                if (throwArgumentNullException)
                    throw PrintException(new ArgumentNullException("typeArguments"));
                else if (throwArgumentException)
                    throw PrintException(new ArgumentException("typeArguments"));

                newGeneric.ArgumentTypes = args;
            }

            void FillStaticData(GenericData newGeneric, GenericData oldGeneric, java.lang.Class javaClass)
            {
                try
                {
                    var staticClass = (java.lang.Class) oldGeneric.StaticData;
                    if (staticClass != null)
                    {
                        // we expect a constructor accepting arguments of type System.Type
                        int n = oldGeneric.ArgumentTypes.Length;
                        var argsArray = new java.lang.Class[n];
                        while (n-- > 0)
                            argsArray[n] = (java.lang.Class) typeof(System.Type);
                        var constructor = staticClass.getDeclaredConstructor(argsArray);

                        // during the call to the constructor for the generic static data
                        // object, we may get a call to GetStatic to get a reference for
                        // the same data object -- for example the constructor might call
                        // some other static method in the class.  so we must make the
                        // data object available to GetStatic even before the constructor
                        // returns.  we do this by having the constructor call SetStatic.
                        // see also GenericUtil::FixConstructorInData(),

                        if (GenericDataForSetStatic != null)
                            throw PrintException(new InvalidOperationException());

                        GenericDataForSetStatic = newGeneric;
                        constructor.newInstance(newGeneric.ArgumentTypes);

                        if (    (GenericDataForSetStatic != null)
                             || (newGeneric.StaticData == null))
                        {
                            throw PrintException(new InvalidOperationException());
                        }
                    }
                }
                catch (System.Exception e)
                {
                    throw PrintException(new TypeInitializationException(javaClass.getName(), e));
                }
            }
        }



        public static void SetStatic(object staticData)
        {
            // we expect to only ever be called from the constructor of the
            // static data class, as invoked by FillStaticData.  in this context,
            // the type lock must be held by the current thread, and the
            // GenericDataForSetStatic variable should be non-null.

            if (    (! TypeLock.isHeldByCurrentThread())
                 || (GenericDataForSetStatic == null)
                 || (GenericDataForSetStatic.StaticData != null))
            {
                throw PrintException(new InvalidOperationException());
            }
            GenericDataForSetStatic.StaticData = staticData;
            GenericDataForSetStatic = null;
        }



        static System.Exception PrintException(System.Exception exception)
        {
            // usually we just throw the error, but since it is difficult to
            // diagnose exceptions that occur before the type system has finished
            // initialization, then we may also print the exception here

            if (! TypeSystemInitialized)
            {
                Console.WriteLine("\n!!! Exception occured during initialization of the type system:\n");
                var exc = exception;
                for (;;)
                {
                    Console.WriteLine("Exception " + ((java.lang.Object) (object) exc).getClass()
                                    + "\nMessage: " + ((java.lang.Throwable) exc).getMessage()
                                    + "\n" + exc.StackTrace);
                    if ((exc = exc.InnerException) == null)
                        break;
                    Console.Write("Caused by Inner ");
                }
            }
            return exception;
        }



        // invoked by code generated by GenericUtil::LoadGeneric
        public Type Argument(int index) => Generic.ArgumentTypes[index];



        //
        // System.Object methods
        //



        public override string ToString()
        {
            var name = FullName;

            var generic = Generic;
            if (generic != null)
            {
                string suffix = null;
                if (IsArrayImpl())
                {
                    var idx = name.IndexOf('[');
                    if (idx != -1)
                    {
                        suffix = name.Substring(idx);
                        name = name.Substring(0, idx);
                    }
                }

                var genericArgs = generic.ArgumentTypes;
                if (genericArgs == null)    // generic parameter
                    name = Name;
                else
                {
                    int n = genericArgs.Length;
                    for (int i = 0; i < n; i++)
                    {
                        name += ((i == 0) ? "[" : ",") + genericArgs[i].ToString();
                    }
                    name += "]";
                }

                if (suffix != null)
                    name += suffix;
            }

            return name;
        }



        public override int GetHashCode()
        {
            if (JavaClass != null)
                return JavaClass.GetHashCode();
            if (Generic != null)
                return Generic.PrimaryType.GetHashCode();
            return 0;
        }



        //
        // the non-overloadable System.Type::IsInterface calls the static method
        // System.RuntimeTypeHandle.IsInterface, we have CodeCall redirect it here
        //

        new public static bool IsInterface(RuntimeType type)
            => (type.JavaClass != null) ? type.JavaClass.isInterface() : false;



        //
        // Equality
        //
        // System.Type may have native versions of the following operators,
        // so we have to implement them here.
        //

        [java.attr.RetainName]
        public static bool op_Equality(Type obj1, Type obj2)
            => object.ReferenceEquals(obj1, obj2);

        [java.attr.RetainName]
        public static bool op_Inequality(Type obj1, Type obj2)
            => ! object.ReferenceEquals(obj1, obj2);

        [java.attr.RetainName]
        public static bool op_Equality(RuntimeType obj1, RuntimeType obj2)
            => object.ReferenceEquals(obj1, obj2);

        [java.attr.RetainName]
        public static bool op_Inequality(RuntimeType obj1, RuntimeType obj2)
            => ! object.ReferenceEquals(obj1, obj2);



        //
        // MemberInfo
        //

        public override string Name
        {
            get
            {
                var generic = Generic;

                if (JavaClass == null)
                    return GenericParameterName(generic);
                if (JavaClass.isArray())
                    return (GetType(JavaClass.getComponentType()).Name) + "[]";
                else
                    return ClassName(generic);

                string ClassName(GenericData generic)
                {
                    var name = JavaClass.getSimpleName();
                    int numArgsInDeclType = 0;

                    var declType = DeclaringType;
                    if (declType is RuntimeType declRuntimeType && declRuntimeType.Generic != null)
                    {
                        // a nested generic type duplicates the parameters of the parent
                        numArgsInDeclType = declRuntimeType.Generic.ArgumentTypes.Length;
                    }

                    if (generic != null)
                    {
                        int numArgs = generic.ArgumentTypes.Length;
                        int index = name.LastIndexOf("$$" + numArgs);
                        if (index != -1)
                            name = name.Substring(0, index);
                        numArgs -= numArgsInDeclType;
                        if (numArgs > 0)
                            name += "`" + numArgs.ToString();
                    }

                    return name;
                }

                string GenericParameterName(GenericData generic)
                {
                    if (generic != null)
                    {
                        // generic parameter
                        var argTypes = generic.PrimaryType.Generic.ArgumentTypes;
                        for (int i = 0; i < argTypes.Length; i++)
                        {
                            if (object.ReferenceEquals(argTypes[i], this))
                            {
                                var typeNames = generic.PrimaryType.JavaClass.getTypeParameters();
                                if (i < typeNames.Length)
                                {
                                    // if we have a valid generic signature, extract parameter name
                                    var nm = typeNames[i].getName();
                                    if (! string.IsNullOrEmpty(nm))
                                        return nm;
                                }
                                return "T" + i.ToString();
                            }
                        }
                    }
                    return null;    // invalid combination
                }

            }
        }

        public override Type DeclaringType
        {
            get
            {
                var javaClass = JavaClass;
                if (javaClass == null)
                    return null;
                if (javaClass.isArray())
                    javaClass = javaClass.getComponentType();
                var declClass = javaClass.getDeclaringClass();
                return (declClass == null) ? null : GetType(declClass);
            }
        }

        public override Type ReflectedType => DeclaringType;

        public override object[] GetCustomAttributes(bool inherit) => null;
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => null;
        public override bool IsDefined(Type attributeType, bool inherit) => false;

        //
        // _Type
        //

        public override Type BaseType
        {
            get
            {
                if (    JavaClass == null
                     || object.ReferenceEquals(this, ObjectType)
                     || IsInterface(this))
                {
                    return null;
                }
                if (object.ReferenceEquals(this, ExceptionType))
                {
                    // CilType translates System.Exception to java.lang.Throwable,
                    // and here we map java.lang.Throwable to system.Exception,
                    // thereby creating an infinite loop in which system.Exception
                    // inherits from java.lang.Exception, which inherits from
                    // java.lang.Throwable, which is mapped to system.Exception.
                    // the workaround breaks the infinite loop.
                    return ObjectType;
                }
                return FromJavaGenericType(JavaClass.getGenericSuperclass());
            }
        }

        public override string Namespace
        {
            get
            {
                var javaClass = JavaClass;
                if (javaClass == null)
                    return null;

                if (javaClass.isArray())
                {
                    do
                    {
                        javaClass = javaClass.getComponentType();
                    }
                    while (javaClass.isArray());
                    return GetType(javaClass).Namespace;
                }

                string dottedName = javaClass.getName();
                int lastIndex = dottedName.LastIndexOf('.');
                if (lastIndex <= 0)
                    return null;

                int firstIndex = 0;
                string resultName = "";
                for (;;)
                {
                    int nextIndex = dottedName.IndexOf('.', firstIndex);
                    var component = Char.ToUpperInvariant(dottedName[firstIndex])
                                  + dottedName.Substring(firstIndex + 1, nextIndex - firstIndex - 1);
                    resultName += component;
                    if (nextIndex == lastIndex)
                        return resultName;
                    resultName += ".";
                    firstIndex = nextIndex + 1;
                }
            }
        }

        public override string FullName
        {
            get
            {
                string name;
                var declType = DeclaringType;
                if (! object.ReferenceEquals(declType, null))
                    name = declType.FullName + "+";
                else if (IsGenericParameter)
                    return null;
                else
                {
                    name = Namespace;
                    if (name != null)
                        name += ".";
                }
                name += Name;
                return name;
            }
        }

        public override Assembly Assembly => null;
        public override string AssemblyQualifiedName => "?";
        public override System.Guid GUID => System.Guid.Empty;
        public override Module Module => null;
        public override Type UnderlyingSystemType => this;

        // Attributes property
        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            if ((CachedAttrs & AttrInitialized) == 0)
            {
                TypeAttributes attrs = TypeAttributes.UnicodeClass | AttrInitialized;

                int modifiers = JavaClass.getModifiers();
                if ((modifiers & 0x0400) != 0)
                    attrs |= TypeAttributes.Abstract;
                if ((modifiers & 0x0200) != 0)
                    attrs |= TypeAttributes.Interface;
                if ((modifiers & 0x0010) != 0)
                    attrs |= TypeAttributes.Sealed;

                if (((java.lang.Class) typeof (java.io.Serializable)).isAssignableFrom(JavaClass))
                    attrs |= TypeAttributes.Serializable;

                modifiers &= 7;
                if (JavaClass.getDeclaringClass() == null)
                {
                    if (modifiers == 1)
                        attrs |= TypeAttributes.Public;
                }
                else
                {
                    if (modifiers == 1)
                        attrs |= TypeAttributes.NestedPublic;
                    else if ((modifiers & 7) == 2)
                        attrs |= TypeAttributes.NestedPrivate;
                    else if ((modifiers & 7) == 4)
                        attrs |= TypeAttributes.NestedFamily;
                    else
                        attrs |= TypeAttributes.NestedAssembly;
                }

                CachedAttrs |= attrs;
            }

            return CachedAttrs & ~AttrPrivateMask;
        }



        public override bool IsSerializable
            => ((GetAttributeFlagsImpl() & TypeAttributes.Serializable) != 0);



        public override Type[] GetInterfaces()
        {
            var output = new java.util.HashSet();
            GetInterfacesInternal(JavaClass, output);
            return (Type[]) output.toArray(System.Type.EmptyTypes);

            void GetInterfacesInternal(java.lang.Class input, java.util.HashSet output)
            {
                foreach (var javaType in input.getGenericInterfaces())
                {
                    output.add(FromJavaGenericType(javaType));
                    if (javaType is java.lang.Class classType)
                        GetInterfacesInternal(classType, output);
                }
            }
        }



        public override Type GetInterface(string name, bool ignoreCase)
        {
            if (name == null)
                throw new ArgumentNullException();
            StringComparison cmp = ignoreCase
                                 ? System.StringComparison.InvariantCultureIgnoreCase
                                 : System.StringComparison.InvariantCulture;
            foreach (var ifc in GetInterfaces())
            {
                if (string.Compare(name, ifc.FullName, cmp) == 0)
                    return ifc;
            }
            return null;
        }



        protected override bool IsCOMObjectImpl() => false;
        protected override bool IsByRefImpl() => false; // always false?

        protected override bool HasElementTypeImpl() => IsArrayImpl();

        public override Type GetElementType()
        {
            var javaClass = JavaClass;
            if (javaClass == null)
                return null;
            javaClass = javaClass.getComponentType();
            if (javaClass == null)
                return null;
            for (;;)
            {
                var nextClass = javaClass.getComponentType();
                if (nextClass == null)
                    return GetType(javaClass);
                javaClass = nextClass;
            }
        }

        protected override bool IsPointerImpl() => false;

        protected override bool IsArrayImpl()
            => (JavaClass != null) ? JavaClass.isArray() : false;

        protected override bool IsValueTypeImpl() => IsValueClass(JavaClass);

        public override int GetArrayRank()
        {
            var javaClass = JavaClass;
            if (javaClass == null || (! javaClass.isArray()))
                throw new ArgumentException();
            for (int rank = 0; ; rank++)
            {
                if ((javaClass = javaClass.getComponentType()) == null)
                    return rank;
            }
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new PlatformNotSupportedException();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new PlatformNotSupportedException();
        }

        public override Type[] GetGenericArguments()
        {
            if (Generic != null)
            {
                return (Type[]) java.util.Arrays.copyOf(
                    Generic.ArgumentTypes, Generic.ArgumentTypes.Length);
            }
            return new Type[0];
        }

        public override Type GetGenericTypeDefinition()
        {
            var generic = Generic;
            if (generic != null && JavaClass != null)
                return generic.PrimaryType;
            throw new System.InvalidOperationException("InvalidOperation_NotGenericType");
        }

        public override bool IsGenericParameter => (JavaClass == null && Generic != null);
        public override bool IsGenericType => (JavaClass != null && Generic != null);
        public override bool IsGenericTypeDefinition
            => (Generic != null && object.ReferenceEquals(Generic.PrimaryType, this));
        public override bool IsConstructedGenericType
            => IsGenericType && (! IsGenericTypeDefinition);

        //
        // ISerializable
        //

        public void GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new PlatformNotSupportedException();

        //
        // ICloneable
        //

        public object Clone() => this;



        //
        // Primitives types
        //



        protected override TypeCode GetTypeCodeImpl()
            => (TypeCode) (((int) (CachedAttrs & AttrTypeCode) >> 24) + 1);

        protected override bool IsPrimitiveImpl()
        {
            // the primitive types are Boolean, Byte, SByte, Int16, UInt16,
            // Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single
            int typeCodeMinus1;
            if ((typeCodeMinus1 = (int) (CachedAttrs & AttrTypeCode)) != 0)
            {
                typeCodeMinus1 >>= 24;
                if (typeCodeMinus1 <= 13) // Double == 14, but minus 1 */
                    return true;
            }
            return false;
        }



        //
        // Utility methods for System.Activator and System.Collections.Generic.EqualityComparer
        //



        public object CreateInstanceDefaultCtor(bool publicOnly, bool skipCheckThis, bool fillCache,
                                                ref system.threading.StackCrawlMark stackMark)
        {
            if (ContainsGenericParameters)
                throw new ArgumentException(Environment.GetResourceString("Acc_CreateGenericEx", this));

            Type rootElementType = this;
            while (rootElementType.HasElementType)
                rootElementType = rootElementType.GetElementType();

            if (object.ReferenceEquals(rootElementType, VoidType))
                throw new NotSupportedException(Environment.GetResourceString("Acc_CreateVoid"));

            return CallConstructor(publicOnly);
        }



        public static object CreateInstanceForAnotherGenericParameter(
                                system.RuntimeType type, system.RuntimeType genericParameter)
        {
            var newType = (RuntimeType) GetType(type.JavaClass, genericParameter);
            return newType.CallConstructor(true);
        }



        //
        // Utility methods for GenericType
        //



        public object CallConstructor(bool publicOnly)
        {
            object[] args = (Generic == null) ? new object[0] : Generic.ArgumentTypes;
            try
            {
                #pragma warning disable 0436
                foreach (java.lang.reflect.Constructor constructor in
                            (java.lang.reflect.Constructor[]) (object) JavaClass.getConstructors())
                {
                    if (constructor.getParameterTypes().Length == args.Length)
                    {
                        bool canCallConstructor = (! publicOnly)
                            || ((constructor.getModifiers() & java.lang.reflect.Modifier.PUBLIC) != 0);

                        if (canCallConstructor)
                        {
                            // setAccessible is required for a private constructor, and
                            // also for a public constructor in a private inner class
                            constructor.setAccessible(true);
                            return constructor.newInstance(args);
                        }
                    }
                }
                #pragma warning restore 0436
            }
            catch (System.Exception e)
            {
                throw new TypeLoadException(FullName, e);
            }
            throw new TypeLoadException(FullName);
        }



        public java.lang.Class JavaClassForArray()
        {
            // given a RuntimeType object for an array, e.g. system.Int32[][][],
            // we want to return a corresponding java class, e.g. int[][][].
            // note that this method is used on both array and non-array types.
            // see also: ReplaceArrayType()

            var cls = JavaClassForArrayCached;
            if (cls == null)
            {
                cls = JavaClass;
                if (! cls.isArray())
                    cls = JavaClassForArray1();
                else
                    cls = JavaClassForArray3(cls);
                JavaClassForArrayCached = cls;
            }
            return cls;

            java.lang.Class JavaClassForArray1()
            {
                switch (GetTypeCodeImpl())
                {
                    case TypeCode.Boolean:                      return java.lang.Boolean.TYPE;
                    case TypeCode.SByte: case TypeCode.Byte:    return java.lang.Byte.TYPE;
                    case TypeCode.Char:                         return java.lang.Character.TYPE;
                    case TypeCode.Int16: case TypeCode.UInt16:  return java.lang.Short.TYPE;
                    case TypeCode.Int32: case TypeCode.UInt32:  return java.lang.Integer.TYPE;
                    case TypeCode.Int64: case TypeCode.UInt64:  return java.lang.Long.TYPE;
                    case TypeCode.Single:                       return java.lang.Float.TYPE;
                    case TypeCode.Double:                       return java.lang.Double.TYPE;
                    case TypeCode.String:   return (java.lang.Class) typeof(java.lang.String);
                    default:
                        if (object.ReferenceEquals(this, ObjectType))
                            return (java.lang.Class) typeof(java.lang.Object);
                        if (object.ReferenceEquals(this, ExceptionType))
                            return (java.lang.Class) typeof(java.lang.Throwable);
                        return JavaClass;
                }
            }

            static java.lang.Class JavaClassForArray2(java.lang.Class cls)
            {
                if (object.ReferenceEquals(cls, ((RuntimeType) ObjectType).JavaClass))
                    return (java.lang.Class) typeof(java.lang.Object);
                if (object.ReferenceEquals(cls, ((RuntimeType) ExceptionType).JavaClass))
                    return (java.lang.Class) typeof(java.lang.Throwable);
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(String)).JavaClass))
                    return (java.lang.Class) typeof(java.lang.String);

                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(Double)).JavaClass))
                    return java.lang.Double.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(Single)).JavaClass))
                    return java.lang.Float.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(UInt64)).JavaClass))
                    return java.lang.Long.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(Int64)).JavaClass))
                    return java.lang.Long.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(UInt32)).JavaClass))
                    return java.lang.Integer.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(Int32)).JavaClass))
                    return java.lang.Integer.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(UInt16)).JavaClass))
                    return java.lang.Short.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(Int16)).JavaClass))
                    return java.lang.Short.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(Char)).JavaClass))
                    return java.lang.Character.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(Byte)).JavaClass))
                    return java.lang.Byte.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(SByte)).JavaClass))
                    return java.lang.Byte.TYPE;
                if (object.ReferenceEquals(cls, ((RuntimeType) typeof(Boolean)).JavaClass))
                    return java.lang.Boolean.TYPE;

                return cls;
            }

            static java.lang.Class JavaClassForArray3(java.lang.Class elemClass)
            {
                int rank = 0;
                for (;;)
                {
                    var nextClass = elemClass.getComponentType();
                    if (nextClass == null)
                        break;
                    elemClass = nextClass;
                    rank++;
                }
                var newArray = java.lang.reflect.Array.newInstance(
                                    JavaClassForArray2(elemClass), new int[rank]);
                return ((java.lang.Object) newArray).getClass();
            }
        }



        public static bool IsValueClass(java.lang.Class javaClass)
        {
            var superClass = javaClass.getSuperclass();
            return (superClass == ValueTypeClass || superClass == EnumClass);
        }



        //
        // check if assignment of B to A (or, A from B) is valid
        //

        public override bool IsAssignableFrom(Type other)
        {
            if (object.ReferenceEquals(other, null))
                return false;

            // valid if A and B are the same type
            if (object.ReferenceEquals(other, this))
                return true;

            // valid if B is derived from (or, A is a base class of B)
            if (other.IsSubclassOf(this))
                return true;

            if (IsInterface(this)) // if A is an interface
            {
                if (other is RuntimeType _other)
                {
                    // valid if I<A> is assignable from I<B>,
                    // taking generic variance into account
                    if (_other.IsCastableToGenericInterface(this, IsArrayImpl()))
                        return true;
                }

                // valid if B implements A.  that is, if any of the
                // interfaces implemented in B are assignable from A
                foreach (var otherIfc in other.GetInterfaces())
                {
                    if (otherIfc.IsAssignableFrom(this))
                        return true;
                }
                return false;
            }

            // valid if both are generic parameters.
            // real .Net also checks for constraints, which we do not.
            return IsGenericParameter;
        }



        public bool IsCastableToGenericInterface(Type castToType, bool isArray)
        {
            GenericData genericThis, genericCast;
            java.lang.String variance;

            if (    // 'this' and 'castToType' must be the same java class
                    // (note that we assume 'this' is an interface class)
                    castToType is RuntimeType castToRuntimeType
                 && castToRuntimeType.JavaClass == JavaClass
                    // both must be generic types
                 && (genericThis = Generic) != null
                 && (genericCast = castToRuntimeType.Generic) != null
                    // neither must be a generic type definition
                 && (! object.ReferenceEquals(genericThis.PrimaryType, this))
                 && (! object.ReferenceEquals(genericCast.PrimaryType, castToType))     )
            {
                variance = (java.lang.String) (object) genericThis.Variance;
                if (variance == null)
                {
                    if (isArray)
                    {
                        // IList<T> and ICollection<T> do not specify variance,
                        // but when implemented for an array, are covariant
                        variance = ((java.lang.String) (object) "O");
                    }
                }
                int n = genericThis.ArgumentTypes.Length;
                if (variance == null)
                    return false;
                if (n == genericCast.ArgumentTypes.Length && variance != null)
                {
                    for (int i = 0; i < n; i++)
                    {
                        var argThis = (RuntimeType) genericThis.ArgumentTypes[i];
                        var argCast = (RuntimeType) genericCast.ArgumentTypes[i];
                        if (! object.ReferenceEquals(argThis, argCast))
                        {
                            if (argThis.IsValueTypeImpl())
                                return false;
                            char varianceChar = variance.charAt(i);
                            if (varianceChar == 'O')        // covariance
                            {
                                if (! argCast.IsAssignableFrom(argThis))
                                    return false;
                            }
                            else if (varianceChar == 'I')   // contravariance
                            {
                                if (! argThis.IsAssignableFrom(argCast))
                                    return false;
                            }
                            else
                                return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }



        //
        // reflection support - extract information from Java generic signatures
        // (generated by MakeGenericSignature in GenericUtil)
        //

        Type FromJavaGenericType(java.lang.reflect.Type javaType)
        {
            if (javaType is java.lang.reflect.ParameterizedType parameterizedType)
            {
                // a ParameterizedType is GenericType<GenericArg1,...GenericArgN>
                // where each GenericArg would be a TypeVariable or a Class

                var primaryClass = (java.lang.Class) parameterizedType.getRawType();
                var javaTypeArgs = parameterizedType.getActualTypeArguments();
                int n = javaTypeArgs.Length;
                var typeArgs = new Type[n];
                for (int i = 0; i < n; i++)
                    typeArgs[i] = FromJavaGenericType(javaTypeArgs[i]);
                return GetType(primaryClass, typeArgs);
            }

            if (    javaType is java.lang.reflect.TypeVariable varType
                 && varType.getGenericDeclaration() is java.lang.Class varClass)
            {
                // a TypeVariable is a GenericArg with a reference to the type
                // it belongs to.  if that type (in the java generics mechanism)
                // corresponds to 'this' type, and if 'this' type is a concrete
                // generic instance (as opposed to a generic definition), then
                // we grab a concrete generic type from 'this' type instance.
                //
                // for example, with A<T1,T2> and B<T1,T2> : A<T2,T1>, then
                // for generic instance B<int,bool>, we are able to resolve
                // the base type as A<bool,int>.

                var varName = varType.getName();
                var primaryType = GetType(varClass) as RuntimeType;
                var argTypes = primaryType?.Generic?.ArgumentTypes;
                if (argTypes != null)
                {
                    int n = argTypes.Length;
                    for (int i = 0; i < n; i++)
                    {
                        if (argTypes[i].Name == varName)
                        {
                            if (    varClass == JavaClass
                                 && Generic != null && Generic.ArgumentTypes != null
                                 && (! object.ReferenceEquals(Generic.PrimaryType, this)))
                            {
                                return Generic.ArgumentTypes[i];
                            }
                            return argTypes[i];
                        }
                    }
                }
            }

            if (javaType is java.lang.Class plainClass)
            {
                // a java generic Type may also be a java.lang.Class
                return GetType(plainClass);
            }

            // if this is a java concept like GenericArrayType or WildcardType,
            // or anything else we don't recognize, return System.Object

            return ObjectType;
        }



        //
        // Methods to create and retrieve type objects
        //



        public static Type GetType(java.lang.Class cls, Type[] argTypes)
        {
            var key = new TypeKey(cls, argTypes);
            var type = (RuntimeType) TypeCache.get(key);
            if (object.ReferenceEquals(type, null))
            {
                // we don't use a monitor with lock (obj) { ... } syntax,
                // because we must never be interrupted when locking types,
                // and system.threading.Monitor calls lockInterruptibly()
                TypeLock.@lock();
                try
                {
                    type = (RuntimeType) TypeCache.get(key);
                    if (object.ReferenceEquals(type, null))
                    {
                        if (argTypes == null)
                        {
                            type = new RuntimeType(cls, null);
                            type = ReplaceArrayType(cls, type);
                            TypeCache.put(key, type);
                        }
                        else
                        {
                            type = (RuntimeType) GetType(cls);
                            type = MakeGenericType(type, argTypes, key);
                        }
                    }
                }
                finally
                {
                    TypeLock.unlock();
                }
            }
            return type;
        }

        public static Type GetType(java.lang.Class cls)
            => GetType(cls, (Type[]) null);

        public static Type GetType(java.lang.Class cls, Type arg)
            => GetType(cls, new Type[] { arg });

        public static Type GetType(java.lang.Class cls, java.lang.Class arg)
            => GetType(cls, new Type[] { GetType(arg) });



        public override Type MakeGenericType(params Type[] typeArguments)
            => GetType(JavaClass, typeArguments);



        public override Type MakeArrayType()
            => MakeArrayType(1);

        public override Type MakeArrayType(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();
            if (JavaClass.isArray())
            {
                var s = JavaClass.getName();
                for (int i = 0; i < s.Length && s[i] == '['; i++)
                    rank++;
            }
            if (rank > 32)
                throw new IndexOutOfRangeException();
            if (IsByRefImpl())
                throw new TypeLoadException(FullName);

            var newArray = java.lang.reflect.Array.newInstance(JavaClass, new int[rank]);
            var javaClass = ((java.lang.Object) newArray).getClass();
            var typeArgs = (Generic != null) ? Generic.ArgumentTypes : null;
            return GetType(javaClass, typeArgs);
        }



        static RuntimeType ReplaceArrayType(java.lang.Class javaClass, RuntimeType type)
        {
            // given a RuntimeType object created for a class which is an array of
            // a java primitive, e.g. int[], we want to return a RuntimeType object
            // for a corresponding array e.g. system.Int32[]
            // see also: JavaClassForArray()

            if (javaClass.isArray() && type.GetElementType() is RuntimeType elemType)
            {
                for (int rank = 0; ; rank++)
                {
                    var nextClass = javaClass.getComponentType();
                    if (nextClass == null)
                    {
                        if (javaClass != elemType.JavaClass)
                        {
                            // the basic element would be a different class only for
                            // a builtin type, for example, int -> System.Int32, or
                            // java.lang.String -> System.String.
                            type = (RuntimeType) elemType.MakeArrayType(rank);
                        }
                        break;
                    }
                    javaClass = nextClass;
                }
            }
            return type;
        }



        public static object GetStatic(Type forType)
        {
            if (forType is RuntimeType runtimeType)
            {
                var generic = runtimeType.Generic;
                if (generic != null)
                {
                    var primary = generic.PrimaryType;
                    if (! (    object.ReferenceEquals(primary, null)
                            || object.ReferenceEquals(primary, forType)))
                    {
                        return generic.StaticData;
                    }
                }
            }
            throw new InvalidOperationException();
        }



        public static void StaticInit()
        {
            // the static initializer of System.Type itself uses the type system,
            // by referencing the generic type EmptyArray`1, and causes GetType()
            // above to be called, before the type cache is initialized.
            //
            // to work around this, InitializeStaticFields in ValueUtil injects
            // a call to this method at the top of that static initializer.

            if (TypeCache == null)
            {
                TypeCache = new java.util.concurrent.ConcurrentHashMap();
                TypeLock = new java.util.concurrent.locks.ReentrantLock();

                ValueTypeClass = (java.lang.Class) typeof(ValueType);
                EnumClass = (java.lang.Class) typeof(Enum);
                DelegateClass = (java.lang.Class) typeof(Delegate);
                MulticastDelegateClass = (java.lang.Class) typeof(MulticastDelegate);

                AddBuiltinType((java.lang.Class) typeof(Object),
                               (java.lang.Class) typeof(java.lang.Object),
                               TypeCode.Object);
                AddBuiltinType((java.lang.Class) typeof(Void), java.lang.Void.TYPE,
                               TypeCode.Object);
                AddBuiltinType((java.lang.Class) typeof(String),
                               (java.lang.Class) typeof(java.lang.String),
                               TypeCode.String);
                AddBuiltinType((java.lang.Class) typeof(Exception),
                               (java.lang.Class) typeof(java.lang.Throwable),
                               TypeCode.Object);

                ObjectType = typeof(Object);
                VoidType = typeof(void);
                ExceptionType = typeof(Exception);

                // note that at this time, it does not seem necessary to add the
                // InArray nested classes for the following primitive type classes.

                AddBuiltinType((java.lang.Class) typeof(Boolean), java.lang.Boolean.TYPE,
                               TypeCode.Boolean);
                AddBuiltinType((java.lang.Class) typeof(Char), java.lang.Character.TYPE,
                               TypeCode.Char);

                AddBuiltinType((java.lang.Class) typeof(SByte), java.lang.Byte.TYPE,
                               TypeCode.SByte);
                AddBuiltinType((java.lang.Class) typeof(Byte), null, TypeCode.Byte);

                AddBuiltinType((java.lang.Class) typeof(Int16), java.lang.Short.TYPE,
                               TypeCode.Int16);
                AddBuiltinType((java.lang.Class) typeof(UInt16), null, TypeCode.UInt16);

                AddBuiltinType((java.lang.Class) typeof(Int32), java.lang.Integer.TYPE,
                               TypeCode.Int32);
                AddBuiltinType((java.lang.Class) typeof(UInt32), null, TypeCode.UInt32);

                AddBuiltinType((java.lang.Class) typeof(Int64), java.lang.Long.TYPE,
                               TypeCode.Int64);
                AddBuiltinType((java.lang.Class) typeof(UInt64), null, TypeCode.UInt64);

                AddBuiltinType((java.lang.Class) typeof(Single), java.lang.Float.TYPE,
                               TypeCode.Single);
                AddBuiltinType((java.lang.Class) typeof(Double), java.lang.Double.TYPE,
                               TypeCode.Double);

                system.Util.DefineException(
                    (java.lang.Class) typeof(java.lang.ClassCastException),
                    (exc) => new System.InvalidCastException(exc.getMessage())
                );
            }

            void AddBuiltinType(java.lang.Class primary, java.lang.Class secondary, TypeCode typeCode)
            {
                var type = new RuntimeType(primary, null);
                if (typeCode != TypeCode.Empty)
                {
                    type.CachedAttrs |= (TypeAttributes) ((((int) typeCode - 1) & 0x1F) << 24);
                }
                TypeCache.put(new TypeKey(primary, null), type);
                if (secondary != null)
                    TypeCache.put(new TypeKey(secondary, null), type);
            }
        }



        [java.attr.RetainType] private static java.util.concurrent.ConcurrentHashMap TypeCache;
        [java.attr.RetainType] private static java.util.concurrent.locks.ReentrantLock TypeLock;
        [java.attr.RetainType] private static java.lang.Class ValueTypeClass;
        [java.attr.RetainType] private static java.lang.Class EnumClass;
        [java.attr.RetainType] private static java.lang.Class DelegateClass;
        [java.attr.RetainType] private static java.lang.Class MulticastDelegateClass;
        [java.attr.RetainType] private static Type ObjectType;
        [java.attr.RetainType] private static Type VoidType;
        [java.attr.RetainType] private static Type ExceptionType;

        // the following initialization occurs after the StaticInit method has completed
        public static RuntimeType EnumType = (RuntimeType) typeof(Enum);

        // this should be the last static variable that we initialize
        [java.attr.RetainType] private static bool TypeSystemInitialized = true;



        private class TypeKey
        {
            java.lang.Class JavaClass;
            Type[] ArgumentTypes;

            public TypeKey(java.lang.Class cls, Type[] args)
            {
                JavaClass = cls;
                ArgumentTypes = args;
            }

            public override bool Equals(object obj)
            {
                var objKey = obj as TypeKey;
                if (objKey == null)
                    return false;
                return object.ReferenceEquals(JavaClass, objKey.JavaClass)
                    && java.util.Arrays.equals(ArgumentTypes, objKey.ArgumentTypes);
            }

            public override int GetHashCode() =>
                JavaClass.GetHashCode() ^ java.util.Arrays.hashCode(ArgumentTypes);
        }



        // called by System.Linq.Parallel.Scheduling.GetDefaultChunkSize<T>
        public override System.Runtime.InteropServices.StructLayoutAttribute StructLayoutAttribute
            => GetRuntimeModule().StructLayoutAttribute;

        public system.reflection.RuntimeModule GetRuntimeModule()
            => _RuntimeModule ?? (_RuntimeModule = new system.reflection.RuntimeModule());
        [java.attr.RetainType] private system.reflection.RuntimeModule _RuntimeModule;



        #pragma warning disable 0436
        public override System.Array GetEnumValues()
        {
            // the default implementation of System.Type::GetEnumValues()
            // throws NotImplementedException, so we have to use reflection
            // to invoke the underlying private method that does the work

            if (! IsEnum)
                throw new ArgumentException();

            // the default implementation of System.Type::GetEnumValues()
            // throws NotImplementedException, so we have to use reflection
            // to invoke the underlying private method that does the work

            if (GetEnumRawConstantValues == null)
            {
                var searchClass = (java.lang.Class) typeof(System.Type);
                var modifierMask  = java.lang.reflect.Modifier.PRIVATE
                                  | java.lang.reflect.Modifier.FINAL
                                  | java.lang.reflect.Modifier.STATIC;
                var modifierValue = java.lang.reflect.Modifier.PRIVATE
                                  | java.lang.reflect.Modifier.FINAL;

                foreach (java.lang.reflect.Method method in
                            (java.lang.reflect.Method[]) (object) searchClass.getDeclaredMethods())
                {
                    if (    (method.getModifiers() & modifierMask) == modifierValue
                         && method.getParameterTypes().Length == 0
                         && method.getReturnType() == (java.lang.Class) typeof(System.Array))
                    {
                        method.setAccessible(true);
                        GetEnumRawConstantValues = method;
                    }
                }
            }

            if (GetEnumRawConstantValues != null)
            {
                return (System.Array) GetEnumRawConstantValues.invoke(this, null);
            }

            throw new InvalidOperationException();
        }
        [java.attr.RetainType] private static java.lang.reflect.Method GetEnumRawConstantValues;
        #pragma warning restore 0436



        //
        // Reflection on members of the type
        //

        public override MethodBase DeclaringMethod
            => throw new PlatformNotSupportedException();

        protected override MethodInfo GetMethodImpl(
            string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            if (JavaClass == null)      // if generic parameter
                return null;
            return system.reflection.RuntimeMethodInfo.GetMethod(
                        name, bindingAttr, binder, callConvention, types, modifiers, this);
        }

        public override object InvokeMember(
            string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
            ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new PlatformNotSupportedException();
        }

        protected override ConstructorInfo GetConstructorImpl(
            BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new PlatformNotSupportedException();
        }

        protected override PropertyInfo GetPropertyImpl(
            string name, BindingFlags bindingAttr, Binder binder, Type returnType,
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new PlatformNotSupportedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw new PlatformNotSupportedException();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new PlatformNotSupportedException();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new PlatformNotSupportedException();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return system.reflection.RuntimeFieldInfo.GetFields(bindingAttr, this);
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new PlatformNotSupportedException();
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new PlatformNotSupportedException();
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
            => throw new PlatformNotSupportedException();

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
            => throw new PlatformNotSupportedException();



        //
        // unimplemented methods from System.Type:
        // base implementations throw NotSupportedException
        /*

        virtual internal bool IsWindowsRuntimeObjectImpl()
        virtual internal bool IsExportedToWindowsRuntimeImpl()
        public virtual bool IsSecurityCritical
        public virtual bool IsSecuritySafeCritical
        public virtual bool IsSecurityTransparent

        public virtual Type MakePointerType()
        public virtual Type MakeByRefType()
        public virtual RuntimeTypeHandle TypeHandle

        virtual public MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr);
        public virtual MemberInfo[] GetDefaultMembers()

        public virtual GenericParameterAttributes GenericParameterAttributes
        public virtual int GenericParameterPosition
        public virtual Type[] GetGenericParameterConstraints()

        internal virtual string FormatTypeName(bool serialization)
        public virtual InterfaceMapping GetInterfaceMap(Type interfaceType)
        */



        //
        // unimplemented methods from System.Type:
        // base implementations is adequate
        /*

        public virtual bool IsEnum
        internal virtual bool IsSzArray
        protected virtual bool IsContextfulImpl()
        protected virtual bool IsMarshalByRefImpl()
                -> always true due to System.MarshalByRefObject == java.lang.Object
        internal virtual bool HasProxyAttributeImpl()
                -> always returns false

        public virtual Type[] FindInterfaces(TypeFilter filter,Object filterCriteria)
        virtual public EventInfo[] GetEvents()
        virtual public MemberInfo[] GetMember(String name, BindingFlags bindingAttr)
        public virtual MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, Object filterCriteria)
        public virtual bool ContainsGenericParameters
        public virtual Type[] GenericTypeArguments

        public virtual bool IsSubclassOf(Type c)
        public virtual bool IsInstanceOfType(Object o)
        public virtual bool IsEquivalentTo(Type other)

        public virtual Type GetEnumUnderlyingType()
        public virtual string[] GetEnumNames()
        public virtual bool IsEnumDefined(object value)
        public virtual string GetEnumName(object value)

        public override bool Equals(Object o)
        public virtual bool Equals(Type o)
        */
    }
}


namespace system.reflection
{
    // because the real System.Reflection.TypeInfo has an inaccessible constructor.
    // we use this dummy class (discarded in output) to make compilation possible.

    [java.attr.Discard] // discard in output
    public abstract class TypeInfo : Type, IReflectableType
    {
        System.Reflection.TypeInfo IReflectableType.GetTypeInfo()
        {
            throw new NotImplementedException();
        }
    }
}
