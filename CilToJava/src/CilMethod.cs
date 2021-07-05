
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public class CilMethod : JavaMethodRef
    {

        static ConditionalWeakTable<MethodReference, CilMethod> _Methods =
                                new ConditionalWeakTable<MethodReference, CilMethod>();

        public CilType DeclType;
        JavaMethodRef GenericMethodRef;
        int Flags;



        public static CilMethod From(MethodReference fromMethod)
        {
            if (! _Methods.TryGetValue(fromMethod, out var converted))
            {
                CilMain.Where.Push($"method '{fromMethod.FullName}'");

                MethodDefinition defMethod;
                bool isGenericInstance = fromMethod.IsGenericInstance
                               || fromMethod.DeclaringType.IsGenericInstance;

                if (fromMethod.DeclaringType is ArrayType fromArrayType)
                {
                    defMethod = null;
                    converted = new CilMethod(fromMethod, fromArrayType);

                    if (! isGenericInstance)
                        _Methods.Add(fromMethod, converted);
                }
                else
                {
                    defMethod = AsDefinition(fromMethod);
                    converted = new CilMethod(fromMethod, defMethod);

                    if (! isGenericInstance)
                    {
                        _Methods.Add(fromMethod, converted);

                        if (    defMethod != fromMethod && defMethod != null
                             && (! _Methods.TryGetValue(defMethod, out var converted2)))
                        {
                            _Methods.Add(defMethod, converted);
                        }
                    }
                }

                CilMain.Where.Pop();
            }
            return converted;
        }



        protected CilMethod(MethodReference fromMethod, ArrayType fromArrayType)
        {
            if (! fromArrayType.IsVector)
            {
                foreach (var dim in fromArrayType.Dimensions)
                {
                    if (dim.LowerBound != 0)
                        throw CilMain.Where.Exception("unsupported array with non-zero lower bound");
                }
            }

            DeclType = CilType.From(fromArrayType.ElementType);

            Name = fromMethod.Name;
            if (Name == ".ctor")
                Flags |= CONSTRUCTOR;
            Flags |= THIS_ARG | ARRAY_CALL;

            ImportParameters(fromMethod);
            ImportGenericParameters(fromMethod);
        }



        protected CilMethod(MethodReference fromMethod, MethodDefinition defMethod)
        {
            DeclType = CilType.From(fromMethod.DeclaringType);

            SetMethodType(defMethod);
            bool appendSuffix = ImportParameters(fromMethod);
            TranslateNameClrToJvm(fromMethod, appendSuffix);

            if (IsConstructor)
            {
                if (appendSuffix && (! DeclType.IsDelegate))
                    FixDuplicateConstructor(defMethod);
            }
            else if (! IsRetainName)
            {
                InsertMethodNamePrefix(defMethod);

                if (appendSuffix)
                    AppendMethodNameSuffix(defMethod);
            }

            ImportGenericParameters(fromMethod);
        }



        void SetMethodType(MethodDefinition defMethod)
        {
            if (! defMethod.HasThis)
            {
                Flags |= STATIC;

                if (defMethod.IsConstructor)
                {
                    Flags |= CONSTRUCTOR;

                    if (DeclType.HasGenericParameters)
                    {
                        // a static initializer in a generic class will be made
                        // a regular constructor, so it needs a 'this' argument.
                        // see also CilGenericUtil.MakeGenericClass
                        Flags |= THIS_ARG;
                    }
                }
            }
            else
            {
                Flags |= THIS_ARG;

                if (defMethod.IsConstructor)
                    Flags |= CONSTRUCTOR;

                if (defMethod.IsVirtual)
                    Flags |= VIRTUAL;
            }

            if (defMethod.HasCustomAttribute("RetainName"))
            {
                Flags |= RETAIN_NAME;

                if (defMethod.HasOverrides)
                {
                    throw CilMain.Where.Exception(
                        "[java.attr.RetainName] is incompatible with explicit methods");
                }
            }

            else if (DeclType.IsDelegate)
            {
                Flags |= RETAIN_NAME;
            }

            if ((! defMethod.HasBody) && (! defMethod.IsAbstract))
            {
                Flags |= EXTERNAL;
            }
        }



        bool ImportParameters(MethodReference fromMethod)
        {
            bool appendSuffix = false;

            int n = fromMethod.HasParameters ? fromMethod.Parameters.Count : 0;
            Parameters = new List<JavaFieldRef>(n);

            for (int i = 0; i < n; i++)
            {
                var fromParameterType = fromMethod.Parameters[i].ParameterType;
                var paramType = CilType.From(fromParameterType);

                if (paramType.IsPointer)
                {
                    if (paramType.IsValueClass || (! paramType.IsReference))
                        paramType = CilType.MakeSpanOf(paramType);
                    else
                        throw CilMain.Where.Exception("invalid pointer type in parameter");
                }

                if (paramType.IsGenericParameter)
                {
                    appendSuffix = true;
                    var nm = "-generic-";
                    if (paramType.IsArray)
                        nm += "array-" + paramType.ArrayRank + "-";
                    if (paramType.IsByReference)
                        nm = "-ref" + nm.Replace("&", "");
                    nm += "$" + ((GenericParameter) fromParameterType.GetElementType()).Position;
                    paramType = CilType.WrapMethodGenericParameter(paramType, nm);
                    Flags |= GEN_ARGS;
                }

                if (paramType.IsByReference)
                {
                    if (paramType.IsValueClass)
                    {
                        paramType = paramType.MakeByRef();
                        appendSuffix = true;
                    }
                    else
                    {
                        // byref of any reference type parameter becomes system.Reference,
                        // so add a unique method name suffix
                        var paramBoxedType = new BoxedType(paramType, false);
                        paramType = paramBoxedType;
                        appendSuffix |= paramBoxedType.IsBoxedReference;
                    }
                }

                if (! appendSuffix)
                {
                    appendSuffix |= fromParameterType.IsGenericInstance;

                    if (! paramType.IsReference)
                        appendSuffix |= ShouldRenamePrimitive(paramType, DeclType);
                }

                Parameters.Add(new JavaFieldRef("", paramType));
            }

            var returnType = CilType.From(fromMethod.ReturnType);
            if (returnType.IsGenericParameter)
                returnType = CilType.WrapMethodGenericParameter(returnType);
            else if (returnType.IsByReference)
                returnType = new BoxedType(returnType, false);
            ReturnType = returnType;

            appendSuffix |= fromMethod.ReturnType.IsGenericInstance;

            return appendSuffix;



            static bool ShouldRenamePrimitive(CilType paramType, CilType declType)
            {
                // .Net primitives and Java primitives do not have an exact 1:1
                // correspondence, for example both Int32 and UInt32 translate
                // to 'int'.  to differentiate between multiple methods, we use
                // method name renaming (via AppendMethodNameSuffix) for those
                // primitive types which are not 'real' primitives in Java.
                //
                // the same approach is also useful for enum parameters, which
                // might be translated to an 'int' type, so require the same
                // method renaming, to avoid conflict with other methods.

                var code = paramType.PrimitiveType;
                var name = paramType.JavaName;

                switch (code)
                {
                    case TypeCode.Boolean:  case TypeCode.Single:  case TypeCode.Double:
                    case TypeCode.SByte when name == "system.SByte":
                    case TypeCode.Char when name == "system.Char":
                    case TypeCode.Int16 when name == "system.Int16":
                    case TypeCode.Int32 when name == "system.Int32":
                    case TypeCode.Int64 when name == "system.Int64":
                        return false;
                }

                if (paramType.JavaName == declType.JavaName)
                {
                    // don't rename if the parameter is within its own type,
                    // e.g. system.IntPtr::CompareTo(system.IntPtr)
                    return false;
                }

                return true;
            }
        }



        void TranslateNameClrToJvm(MethodReference fromMethod, bool appendSuffix)
        {
            if (IsConstructor)
            {
                if (IsStatic)
                    Name = "<clinit>";

                else if (DeclType.IsValueClass)
                {
                    Name = "$initValue";
                    Flags &= ~CONSTRUCTOR;
                    Flags |= VALUE_INIT;
                }

                else
                {
                    if (DeclType.IsDelegate)
                    {
                        Delegate.FixConstructor(Parameters, DeclType);
                    }

                    Name = "<init>";
                }

                return;
            }

            var name = CilMain.MakeValidMemberName(fromMethod.Name);

            if ((! appendSuffix) && (! IsRetainName))
            {
                if (name == "MemberwiseClone" && ToDescriptor() == "()Ljava/lang/Object;")
                    name = "clone";

                if (name == "Finalize" && ToDescriptor() == "()V")
                    name = "finalize";

                if (name == "Equals" && ToDescriptor() == "(Ljava/lang/Object;)Z")
                    name = "equals";

                if (name == "GetHashCode" && ToDescriptor() == "()I")
                    name = "hashCode";

                if (name == "ToString" && ToDescriptor() == "()Ljava/lang/String;")
                    name = "toString";
            }

            Name = name;
        }



        // for DotNetImporter
        public static string TranslateNameJvmToClr(JavaMethodRef jmethod)
        {
            string name = jmethod.Name;

            if (name == "clone" && jmethod.ToDescriptor() == "()V")
                return "MemberwiseClone";

            if (name == "equals" && jmethod.ToDescriptor() == "(Ljava/lang/Object;)Z")
                return "Equals";

            if (name == "finalize" && jmethod.ToDescriptor() == "()V")
                return"Finalize";

            if (name == "hashCode" && jmethod.ToDescriptor() == "()I")
                return "GetHashCode";

            if (name == "toString" && jmethod.ToDescriptor() == "()Ljava/lang/String;")
                return "ToString";

            return null;
        }



        void ImportGenericParameters(MethodReference fromMethod)
        {
            List<JavaFieldRef> newParameters = null;
            var newName = Name;

            if (IsConstructor || IsStatic)
            {
                if (DeclType.HasGenericParameters)
                {
                    newParameters = new List<JavaFieldRef>(Parameters);

                    foreach (var arg in DeclType.GenericParameters)
                    {
                        var newParam = new JavaFieldRef(arg.JavaName, CilType.SystemTypeType);
                        newParameters.Add(newParam);
                        newName += CilMain.EXCLAMATION;
                    }
                }
            }

            if (fromMethod.IsGenericInstance)
                fromMethod = ((GenericInstanceMethod) fromMethod).ElementMethod;

            if (fromMethod.HasGenericParameters)
            {
                if (newParameters == null)
                    newParameters = new List<JavaFieldRef>(Parameters);

                foreach (var arg in fromMethod.GenericParameters)
                {
                    var argName = CilType.From(arg).JavaName;
                    var newParam = new JavaFieldRef(argName, CilType.SystemTypeType);
                    newParameters.Add(newParam);
                    newName += CilMain.EXCLAMATION;
                }
            }

            if (newParameters != null)
            {
                if (IsConstructor || IsArrayMethod || IsInterlockedOrVolatile())
                    newName = Name;

                GenericMethodRef = new JavaMethodRef(newName, ReturnType, newParameters);
            }
        }



        void FixDuplicateConstructor(MethodDefinition defMethod)
        {
            //
            // if a method has byref or generic parameters, we rename the
            // method to prevent collisions.  see also AppendMethodNameSuffix
            //
            // for a constructor this is not possible, so instead we add
            // a parameter with the type of a dummy unique inner class
            //
            // this parameter is identified in TypeBuilder::ImportMethods
            // in order to create the actual dummy nested class.
            //
            // it is also identified in various methods in CodeCall so it
            // can correctly push or pop the dummy argument.
            //

            string oldName = Name;
            AppendMethodNameSuffix(defMethod);

            var dummyName = DeclType.ClassName + "$$unique" + Name.GetHashCode().ToString("X4");
            var dummyType = CilType.From(new JavaType(0, 0, dummyName));
            Parameters.Add(new JavaFieldRef("", dummyType));
            Flags |= DUMMY_ARG;

            Name = oldName;
        }



        void InsertMethodNamePrefix(MethodDefinition defMethod)
        {
            if (defMethod.HasOverrides)
            {
                //
                // a method override is an explicit interface implementation
                //

                if (defMethod.Overrides.Count != 1)
                {
                    throw CilMain.Where.Exception(
                                $"multiple explicit interface implementions in '{defMethod}'");
                }

                var interfaceMethod = CilMethod.From(defMethod.Overrides[0]);
                if (interfaceMethod.DeclType.IsRetainName)
                {
                    throw CilMain.Where.Exception(
                        $"cannot declare an explicit method for the [java.attr.RetainName] interface '{interfaceMethod.DeclType}'");
                }

                Name = CilMethod.From(defMethod.Overrides[0]).Name;

                if (defMethod.Overrides[0].DeclaringType is GenericInstanceType genericInterface)
                {
                    // a class that implements a generic interface for multiple types
                    // (e.g. class Cls : Int<bool>, Int<string>) can provide multiple explicit
                    // implementation methods.  to differentiate them, we add the generic type
                    // as method name suffix (e.g. CompareTo--bool, CompareTo--int).  this is
                    // necessary only for concrete types, because .Net does not permit a class
                    // to implement a generic interface more than once for a generic parameter,
                    // see also C# compiler error CS0695
                    foreach (var p in genericInterface.GenericArguments)
                    {
                        if (! (p is GenericParameter))
                            Name += "--" + GenericParameterSuffixName(CilType.From(p));
                    }
                }

                var idx = Name.IndexOf(CilMain.OPEN_PARENS);
                if (idx != -1)
                {
                    // if already processed by AppendMethodNameSuffix, discard suffix
                    Name = Name.Substring(0, idx);
                }

                Flags |= EXPL_IMPL;
            }
            else
            {
                var prefixType = MethodIsShadowing(defMethod);

                if (prefixType == null && DeclType.IsInterface && (! DeclType.IsRetainName))
                {
                    prefixType = DeclType;
                }

                if (prefixType != null)
                {
                    Name = prefixType.ClassName.Replace('.', '-') + "-" + Name;
                }
            }

            //
            // special case:  we need a couple of methods in our system.Enum
            // to overload based on return type, we mark them [SpecialName]
            // so we can identify and fix them here
            //

            if (defMethod.IsSpecialName && DeclType.ClassName == "system.Enum"
                                        && Name.EndsWith("Long"))
            {
                Name = Name.Substring(0, Name.Length - 4);
            }
        }



        static CilType MethodIsShadowing(MethodDefinition method)
        {
            //
            // check if method is 'new', i.e. hides a base method with same signature
            //

            var initialType = method.DeclaringType;
            var type = initialType;

            for (;;)
            {
                var baseTypeRef = type.BaseType;
                if (baseTypeRef == null)
                    return null;

                type = CilType.AsDefinition(baseTypeRef);
                if (type.HasMethods)
                {
                    foreach (var method2 in type.Methods)
                    {
                        if (CompareMethods(method, method2))
                        {
                            // exception objects have a circular chain of inheritance:
                            // SomeException -> system.Exception -> java.lang.Exception ...
                            // ... -> java.lang.Throwable -> System.Exception -> System.Object
                            //
                            // due to this chain, we might end up here, comparing methods from
                            // System.Exception, instead of our system.Exception.  to prevent
                            // this, we make a special rule to stop at System.Exception.

                            if (initialType.Namespace == "System" && initialType.Name == "Exception")
                            {
                                // ... except the special case of System.Exception::GetType(),
                                // which actually is shadowing System.Object::GetType()
                                if (method.Name == "GetType")
                                    return CilType.From(new JavaType(0, 0, "system.Exception"));
                                return null;
                            }

                            if (method.IsVirtual && (! method.IsNewSlot))
                            {
                                // we are overriding some virtual method, but keep checking
                                // backwards to check if that method is itself shadowing
                                return MethodIsShadowing(method2);
                            }

                            return CilType.From(initialType);
                        }
                    }
                }
            }
        }



        void AppendMethodNameSuffix(MethodDefinition defMethod)
        {
            // if a method has byref or generic parameters, we rename the
            // method to prevent collisions.  see also FixDuplicateConstructor

            if (IsInterlockedOrVolatile())
                return;

            string suffix = "";
            if (    defMethod.ReturnType is GenericInstanceType genericReturnType
                 && (! OnlyGenericParameterArguments(genericReturnType)))
            {
                suffix = CilMain.OPEN_PARENS
                       + "-ret-" + GenericInstanceName(genericReturnType)
                       + CilMain.CLOSE_PARENS;
            }

            int n = Parameters.Count;
            for (int i = 0; i < n; i++)
            {
                var s = CilMain.OPEN_PARENS.ToString();

                if (defMethod.Parameters[i].ParameterType is GenericInstanceType genericParameterType)
                    s += GenericInstanceName(genericParameterType);
                else
                {
                    var paramType = (CilType) Parameters[i].Type;
                    if (paramType is BoxedType paramBoxedType)
                    {
                        s += "-ref-";
                        paramType = paramBoxedType.UnboxedType;
                    }
                    else if (paramType.IsByReference && (! paramType.IsGenericParameter))
                        s += "-ref-";
                    if (paramType.IsArray)
                        s += "array-" + paramType.ArrayRank + "-";
                    s += paramType.JavaName.Replace('.', '-');
                }

                s += CilMain.CLOSE_PARENS;
                suffix += s;
            }
            Name += suffix;



            string GenericInstanceName(GenericInstanceType genericInstance)
            {
                string s = CilType.From(genericInstance).JavaName.Replace('.', '-')
                         + CilMain.OPEN_PARENS;
                foreach (var argIterator in genericInstance.GenericArguments)
                {
                    s += "-";
                    var arg = argIterator;
                    if (arg is ArrayType arrayArg)
                    {
                        s += "-array-" + arrayArg.Rank;
                        arg = arrayArg.ElementType;
                    }

                    if (arg is GenericParameter)
                        s += "-generic-$"; // + arg.Name;
                    else if (arg is GenericInstanceType genericInstanceArg)
                        s += "-" + GenericInstanceName(genericInstanceArg);
                    else
                        s += "-" + CilType.From(arg).JavaName.Replace('.', '-');
                }
                return s + CilMain.CLOSE_PARENS;
            }



            bool OnlyGenericParameterArguments(GenericInstanceType genericInstance)
            {
                foreach (var arg in genericInstance.GenericArguments)
                {
                    if (! (arg is GenericParameter))
                        return false;
                }
                return true;
            }
        }



        public static void FixNameForVirtualToStaticCall(JavaMethodRef method, CilType callClass)
        {
            // this method is called from CodeCall.ConvertVirtualToStaticCall,
            // which converts a virtual call to a static call by inserting an
            // initial 'this' parameter.
            // if the target method was altered by AppendMethodNameSuffix,
            // then we also have to insert the name for that initial 'this'.

            string name = method.Name;
            int idx = name.IndexOf(CilMain.OPEN_PARENS);
            if (idx != -1)
            {
                string newName = name.Substring(0, idx)
                               + CilMain.OPEN_PARENS
                               + callClass.JavaName.Replace('.', '-')
                               + CilMain.CLOSE_PARENS
                               + name.Substring(idx);
                method.Name = newName;
            }
        }



        internal static bool CompareMethods(MethodDefinition m1, MethodDefinition m2)
        {
            if (m1.Name != m2.Name)
                return false;
            if (m1.HasThis != m2.HasThis)
                return false;
            if (CilType.AsDefinition(m1.ReturnType) != CilType.AsDefinition(m2.ReturnType))
                return false;
            var p1 = m1.Parameters;
            var p2 = m2.Parameters;
            int n = p1.Count;
            if (n != p2.Count)
                return false;
            for (int i = 0; i < n; i++)
            {
                var p1i = p1[i].ParameterType;
                var p2i = p2[i].ParameterType;
                if (p1i.IsByReference || p2i.IsByReference)
                {
                    if (p1i.IsByReference != p2i.IsByReference)
                        return false;
                    p1i = ((ByReferenceType) p1i).ElementType;
                    p2i = ((ByReferenceType) p2i).ElementType;
                }
                if (p1i.IsGenericParameter != p2i.IsGenericParameter)
                    return false;
                if (CilType.AsDefinition(p1i) != CilType.AsDefinition(p2i))
                    return false;
            }
            return true;
        }



        static Dictionary<MethodReference, MethodDefinition> _MethodDefs =
                                new Dictionary<MethodReference, MethodDefinition>();

        internal static MethodDefinition AsDefinition(MethodReference _ref)
        {
            if (_ref.IsDefinition)
                return _ref as MethodDefinition;
            if (_MethodDefs.TryGetValue(_ref, out var def))
                return def;
            def = _ref.Resolve();
            if (def != null)
            {
                _MethodDefs.Add(_ref, def);
                return def;
            }
            throw CilMain.Where.Exception(
                            $"could not resolve method '{_ref.Name}' from assembly '{_ref.DeclaringType.Scope}'");
        }



        public static JavaCode CreateConstructor(JavaClass theClass, int numGeneric, bool isInstance)
        {
            var name = isInstance ? "<init>" : "<clinit>";
            JavaMethod mth = new JavaMethod(name, JavaType.VoidType);
            mth.Class = theClass;
            mth.Flags = isInstance ? JavaAccessFlags.ACC_PUBLIC : JavaAccessFlags.ACC_STATIC;
            mth.ReturnType = JavaType.VoidType;

            mth.Parameters = new List<JavaFieldRef>();
            for (int i = 0; i < numGeneric; i++)
                mth.Parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));

            mth.Code = new JavaCode(mth);
            mth.Code.MaxLocals = (isInstance ? 1 : 0) + numGeneric;
            mth.Code.StackMap = new JavaStackMap();

            theClass.Methods.Add(mth);
            return mth.Code;
        }



        internal CilMethod(CilType valueType)
        {
            // create a parameterless constructor method reference
            // for a possibly generic non-primitive value type.
            // used by Translate_Newobj in CodeCall

            Flags |= CONSTRUCTOR | THIS_ARG;
            Name = "<init>";
            ReturnType = JavaType.VoidType;

            Parameters = new List<JavaFieldRef>(0);

            if (valueType != null)
            {
                if (! valueType.IsValueClass)
                    throw new ArgumentException();

                int numGeneric = valueType.GenericParametersCount;
                if (numGeneric != 0)
                {
                    var newParameters = new List<JavaFieldRef>(numGeneric);
                    for (int i = 0; i < numGeneric; i++)
                    {
                        newParameters.Add(new JavaFieldRef(
                            valueType.GenericParameters[i].JavaName, CilType.SystemTypeType));
                    }

                    GenericMethodRef = new JavaMethodRef(Name, ReturnType, newParameters);
                }
            }
        }



        public static void MoveCodeFromBottomToTop(JavaCode code, int startingIndex)
        {
            var insts = code.Instructions;
            int numInstsToMove = insts.Count - startingIndex;
            var rangeToMove = insts.GetRange(startingIndex, numInstsToMove);
            insts.RemoveRange(startingIndex, numInstsToMove);
            insts.InsertRange(0, rangeToMove);
        }



        public static CilMethod CreateDelegateConstructor()
        {
            var method = new CilMethod(null);
            var objectParam = new JavaFieldRef("", CilType.From(JavaType.ObjectType));
            method.Parameters.Add(objectParam);   // target
            method.Parameters.Add(objectParam);   // invokable
            method.DeclType = CilType.From(
                        new JavaType(0, 0, "system.FunctionalInterfaceDelegate"));
            return method;
        }



        public JavaMethodRef WithGenericParameters => GenericMethodRef ?? this;



        public static string GenericParameterSuffixName(CilType type)
            => type.JavaName.Replace('.', '-').Replace('/', '$');



        public static void ValueMethod(JavaMethodRef method, JavaCode code) =>
            code.NewInstruction(0xB6 /* invokevirtual */, CilType.SystemValueType, method);



        public bool IsInterlockedOrVolatile()
        {
            // we need to identify several special methods which are used to
            // access volatile data, and remove any suffixes/decoration on them.
            //
            // for example, System.Threading.Interlocked.Exchange(ref int, int)
            // is actually defined in baselib as Exchange(system.Int32, int).
            // this means we need to drop the method name suffix, so callers
            // of Exchange (as declared in .Net) can find our method in baselib.
            return (    DeclType.JavaName == "system.threading.Interlocked"
                     || DeclType.JavaName == "system.threading.Volatile");
        }



        public bool IsStatic => (Flags & STATIC) != 0;
        public bool IsConstructor => (Flags & CONSTRUCTOR) != 0;
        public bool IsValueInit => (Flags & VALUE_INIT) != 0;
        public bool IsRetainName => (Flags & RETAIN_NAME) != 0;
        public bool IsArrayMethod => (Flags & ARRAY_CALL) != 0;
        public bool HasGenericArgs => (Flags & GEN_ARGS) != 0;
        public bool HasThisArg => (Flags & THIS_ARG) != 0;
        public bool IsExplicitImpl => (Flags & EXPL_IMPL) != 0;
        public bool HasDummyClassArg => (Flags & DUMMY_ARG) != 0;
        public bool IsExternal => (Flags & EXTERNAL) != 0;
        public bool IsVirtual => (Flags & VIRTUAL) != 0;



        const int STATIC      = 0x0001;
        const int CONSTRUCTOR = 0x0002;
        const int VALUE_INIT  = 0x0004;
        const int EXPL_IMPL   = 0x0040;
        const int RETAIN_NAME = 0x0080;
        const int ARRAY_CALL  = 0x0100;
        const int GEN_ARGS    = 0x0200;
        const int THIS_ARG    = 0x0400;
        const int DUMMY_ARG   = 0x0800;
        const int EXTERNAL    = 0x1000;
        const int VIRTUAL     = 0x2000;



        internal static readonly JavaMethodRef ValueClear =
                                    new JavaMethod("system-ValueMethod-Clear", JavaType.VoidType);

        internal static readonly JavaMethodRef ValueCopyTo =
                                    new JavaMethod("system-ValueMethod-CopyTo",
                                                   JavaType.VoidType, CilType.SystemValueType);

        internal static readonly JavaMethodRef ValueClone =
                                    new JavaMethod("system-ValueMethod-Clone", CilType.SystemValueType);

    }
}
