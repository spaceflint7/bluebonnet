
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system
{

    //
    // System/RuntimeType.cs contains methods required by the type
    // system.  System/Reflection/RuntimeType2.cs (this file) contains
    // methods to provide run-time reflection support.
    //

    public partial class RuntimeType
    {

        //
        // FullName
        //

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

        //
        // Name
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

        //
        // Namespace
        //

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

        //
        // DeclaringType
        //

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

        //
        // ReflectedType
        //

        public override Type ReflectedType => DeclaringType;

        //
        // DeclaringMethod
        //

        public override MethodBase DeclaringMethod
            => throw new PlatformNotSupportedException();

        //
        // BaseType
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

        //
        // Module
        //

        public override Module Module => Assembly.GetModules(false)[0];

        //
        // Assembly
        //

        public override Assembly Assembly => system.reflection.RuntimeAssembly.CurrentAssembly;

        //
        // AssemblyQualifiedName
        //

        public override string AssemblyQualifiedName
        {
            get
            {
                var name = FullName;
                if (name != null)
                    name = Assembly.CreateQualifiedName(Assembly.FullName, name);
                return name;
            }
        }

        //
        // GetCustomAttributes
        //

        public override object[] GetCustomAttributes(bool inherit) => EmptyObjectArray;

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType is system.RuntimeType attributeRuntimeType)
            {
                // we don't yet translate .Net attributes to Java annotations,
                // so we have to fake some attributes to support F# ToString()
                var attrs = system.reflection.FSharpCompat.GetCustomAttributes_Type(
                                    this.JavaClass, attributeRuntimeType.JavaClass);
                if (attrs != null)
                    return attrs;
            }
            return EmptyObjectArray;
        }

        //
        // IsDefined
        //

        public override bool IsDefined(Type attributeType, bool inherit) => false;

        //
        // GetInterfaces
        //

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

        //
        // GetInterface
        //

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

        //
        // GetNestedTypes
        //

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            bool takePublic    = (bindingAttr & BindingFlags.Public)    != 0;
            bool takeNonPublic = (bindingAttr & BindingFlags.NonPublic) != 0;
            if (takePublic || takeNonPublic)
            {
                var innerClasses = JavaClass.getDeclaredClasses();
                if (innerClasses.Length > 0)
                {
                    var list = new java.util.ArrayList();
                    for (int i = 0; i < innerClasses.Length; i++)
                    {
                        var innerCls = innerClasses[i];
                        var isPublic = (0 != (innerCls.getModifiers()
                                                & java.lang.reflect.Modifier.PUBLIC));

                        if (takePublic == isPublic || takeNonPublic != isPublic)
                        {
                            var innerType = GetType(innerCls);
                            var generic = ((RuntimeType) innerType).Generic;
                            list.add(generic != null ? generic.PrimaryType : innerType);
                        }
                    }

                    return (Type[]) list.toArray(system.RuntimeType.EmptyTypeArray);
                }
            }

            return system.RuntimeType.EmptyTypeArray;
        }

        //
        // GetNestedType
        //

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            ThrowHelper.ThrowIfNull(name);
            if ((bindingAttr & BindingFlags.IgnoreCase) != 0)
                throw new PlatformNotSupportedException();

            var innerCls = FindInnerClass(JavaClass, name);
            if (innerCls != null)
            {
                bool takePublic    = (bindingAttr & BindingFlags.Public)    != 0;
                bool takeNonPublic = (bindingAttr & BindingFlags.NonPublic) != 0;
                var isPublic = (0 != (innerCls.getModifiers()
                                        & java.lang.reflect.Modifier.PUBLIC));

                if (takePublic == isPublic || takeNonPublic != isPublic)
                {
                    var innerType = GetType(innerCls);
                    var generic = ((RuntimeType) innerType).Generic;
                    return (generic != null ? generic.PrimaryType : innerType);
                }
            }

            return null;
        }

        //
        // FindInnerClass (public helper)
        //

        public static java.lang.Class FindInnerClass(
                                        java.lang.Class javaClass, string name)
        {
            var innerClasses = javaClass.getDeclaredClasses();
            for (int i = 0; i < innerClasses.Length; i++)
            {
                var innerCls = innerClasses[i];
                if (innerCls.getSimpleName() == name)
                    return innerCls;
            }
            return null;
        }

        //
        // Reflection on members of the type
        //

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
            => throw new PlatformNotSupportedException();

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new PlatformNotSupportedException();
        }

        protected override PropertyInfo GetPropertyImpl(
            string name, BindingFlags bindingAttr, Binder binder, Type returnType,
            Type[] types, ParameterModifier[] modifiers)
        {
            if (JavaClass == null)      // if generic parameter
                return null;
            return system.reflection.RuntimePropertyInfo.GetProperty(
                        name, bindingAttr, binder, returnType, types, modifiers, this);
        }

        protected override MethodInfo GetMethodImpl(
            string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            if (JavaClass == null)      // if generic parameter
                return null;
            return system.reflection.RuntimeMethodInfo.GetMethod(
                        name, bindingAttr, binder, callConvention, types, modifiers, this);
        }

        protected override ConstructorInfo GetConstructorImpl(
            BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            if (JavaClass == null)      // if generic parameter
                return null;
            return system.reflection.RuntimeConstructorInfo.GetConstructor(
                        bindingAttr, binder, callConvention, types, modifiers, this);
        }

        //
        //
        //

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
            => throw new PlatformNotSupportedException();

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
            => throw new PlatformNotSupportedException();

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
            => system.reflection.RuntimeFieldInfo.GetFields(bindingAttr, this);

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
            => system.reflection.RuntimePropertyInfo.GetProperties(bindingAttr, this);

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
            => throw new PlatformNotSupportedException();

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
            => throw new PlatformNotSupportedException();

        //
        //
        //

        public override object InvokeMember(
            string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
            ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new PlatformNotSupportedException();
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
        // UnboxJavaReturnValue
        //

        public static object UnboxJavaReturnValue(object value)
        {
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
            return value;
        }

    }

}
