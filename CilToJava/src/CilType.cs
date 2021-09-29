
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public class CilType : JavaType
    {

        static ConditionalWeakTable<TypeReference, CilType> _Types =
                                new ConditionalWeakTable<TypeReference, CilType>();

        public string JavaName;
        public List<CilType> SuperTypes;
        public List<CilType> GenericParameters;
        int Flags;



        public static CilType From(TypeReference fromType)
        {
            if (! _Types.TryGetValue(fromType, out var converted))
            {
                converted = GetCachedPrimitive(fromType);
                if (converted != null)
                    return converted;

                CilMain.Where.Push($"type '{fromType.FullName}'");

                converted = new CilType();
                converted.Import(fromType);

                if ((converted.Flags & DONT_CACHE) == 0)
                {
                    // do not cache GenericInstanceMethod objects,
                    // as each instance stands for a single use of the generic type
                    _Types.Add(fromType, converted);
                }

                CilMain.Where.Pop();
            }
            return converted;
        }



        public static CilType From(JavaType oldType)
        {
            var newType = new CilType();
            newType.CopyFrom(oldType);
            if (newType.IsReference)
                newType.JavaName = newType.ClassName;
            else
            {
                string s;
                switch (newType.PrimitiveType)
                {
                    case TypeCode.Empty:    s = "system.Void";      break;
                    case TypeCode.Boolean:  s = "system.Boolean";   break;
                    case TypeCode.Char:     s = "system.Char";      break;
                    case TypeCode.SByte:    s = "system.SByte";     break;
                    case TypeCode.Byte:     s = "system.Byte";      break;
                    case TypeCode.Int16:    s = "system.Int16";     break;
                    case TypeCode.UInt16:   s = "system.UInt16";    break;
                    case TypeCode.Int32:    s = "system.Int32";     break;
                    case TypeCode.UInt32:   s = "system.UInt32";    break;
                    case TypeCode.Int64:    s = "system.Int64";     break;
                    case TypeCode.UInt64:   s = "system.UInt64";    break;
                    case TypeCode.Single:   s = "system.Single";    break;
                    case TypeCode.Double:   s = "system.Double";    break;
                    default:                throw new ArgumentException();
                }
                newType.JavaName = s;
            }
            return newType;
        }



        void Import(TypeReference fromType)
        {
            if (fromType is TypeSpecification fromTypeSpec)
            {
                ImportTypeSpec(fromTypeSpec);
            }
            else if (fromType.IsGenericParameter)
            {
                ImportGenericParameter((GenericParameter) fromType);
            }
            else
            {
                var defType = AsDefinition(fromType);
                var metadataType = defType.MetadataType;
                bool isValueType = (metadataType == MetadataType.ValueType);
                if (isValueType || (metadataType == MetadataType.Class))
                {
                    ImportClass(fromType, defType, isValueType);
                }
                else
                {
                    ImportPrimitive(metadataType, fromType.Name);
                }
            }
        }



        void ImportTypeSpec(TypeSpecification fromType)
        {
            Import(fromType.ElementType);

            if (fromType.IsByReference)
                Flags |= BYREF;

            else if (fromType.IsPointer || fromType.IsPinned)
            {
                if (fromType.FullName == "System.Void*")
                    throw CilMain.Where.Exception($"Void* pointers are not supported");
                Flags |= POINTER;
            }

            else if (fromType.IsArray)
            {
                ArrayRank += (fromType as ArrayType).Rank;
                if (fromType.ElementType.IsArray)
                    Flags |= JAGGED;
            }

            else if (fromType.IsRequiredModifier || fromType.IsOptionalModifier)
            {
                var modifier = (fromType as IModifierType).ModifierType.FullName;
                if (modifier == "System.Runtime.CompilerServices.IsVolatile")
                    Flags |= VOLATILE;
                else if (modifier != "System.Runtime.CompilerServices.IsExternalInit")
                    throw CilMain.Where.Exception($"unknown modifier '{modifier}'");
            }

            else if (fromType.IsGenericInstance)
                Flags |= DONT_CACHE;

            else
                throw CilMain.Where.Exception("unknown type");
        }



        void ImportClass(TypeReference fromType, TypeDefinition defType, bool isValue)
        {
            int numGeneric = ImportGenericParameters(fromType);

            if (defType.HasCustomAttribute(
                            "System.Runtime.Remoting.Contexts.SynchronizationAttribute", true))
                throw CilMain.Where.Exception($"attribute [Synchronization] is not supported");

            if (defType.HasCustomAttribute("RetainName"))
                Flags |= RETAIN_NAME;

            if (isValue)
            {
                Flags |= VALUE;
                if (defType.IsEnum)
                {
                    if (defType.HasFields && numGeneric == 0)
                    {
                        var type = From(defType.Fields[0].FieldType);
                        if ((! type.IsReference) && type.PrimitiveType >= TypeCode.Boolean
                                                 && type.PrimitiveType <= TypeCode.UInt64)
                        {
                            JavaName = ImportName(defType, 0);
                            CopyFrom(type);
                            Flags |= ENUM;
                            return;
                        }
                    }
                    throw CilMain.Where.Exception("bad enum");
                }
                if (fromType.Namespace == "System")
                {
                    if (fromType.Name == "RuntimeTypeHandle")
                    {
                        CopyFrom(JavaType.ClassType);
                        Flags &= ~VALUE;
                        return;
                    }
                    /*
                    if (fromType.Name == "RuntimeFieldHandle")
                    {
                        CopyFrom(ReflectFieldType);
                        JavaName = ClassName;
                        Flags &= ~VALUE;
                        return;
                    }
                    if (fromType.Name == "RuntimeMethodHandle")
                    {
                        CopyFrom(ReflectMethodType);
                        JavaName = ClassName;
                        Flags &= ~VALUE;
                        return;
                    }
                    */
                }
            }
            else
            {
                if (fromType.Namespace == "System")
                {
                    if (fromType.Name == "Exception")
                    {
                        CopyFrom(JavaType.ThrowableType);
                        JavaName = "system.Exception";
                        return;
                    }
                    if (fromType.Name == "MarshalByRefObject")
                    {
                        CopyFrom(JavaType.ObjectType);
                        JavaName = "system.MarshalByRefObject";
                        return;
                    }
                }

                if (defType.IsInterface)
                    Flags |= INTERFACE;

                else if (defType.IsAbstract && defType.HasCustomAttribute("AsInterface"))
                    Flags |= INTERFACE;

                else if (IsDelegateClass(defType))
                    Flags |= DELEGATE;
            }

            PrimitiveType = TypeCode.Empty;
            ArrayRank = 0;
            ClassName = ImportName(fromType, numGeneric);
            JavaName = ClassName;

            LinkSuperTypes(defType);

            if (IsDelegate && defType.HasCustomAttribute("AsInterface")
                           && ClassName.EndsWith("$Delegate"))
            {
                // check for an artificial delegate, generated to represent a
                // java functional interface: (BuildDelegate in DotNetImporter),
                // and change to our baselib type -- FunctionalInterfaceDelegate.
                // see also:  LoadFunction in Delegate module.
                JavaName = ClassName = "system.FunctionalInterfaceDelegate";
            }
        }



        static string ImportName(TypeReference fromType, int numGeneric)
        {
            string name = CilMain.MakeValidTypeName(fromType.Name);

            if (fromType.IsNested)
            {
                var parent = CilType.From(AsDefinition(fromType.DeclaringType));
                name = parent.ClassName + "$" + name;
            }
            else if (! string.IsNullOrEmpty(fromType.Namespace))
            {
                name = CilMain.MakeValidTypeName(fromType.Namespace.ToLowerInvariant())
                     + "." + name;
            }

            if (numGeneric != 0)
            {
                // the apostrophe character may get replaced in MakeValidTypeName(),
                // so we look for it in the original name string
                int index = fromType.Name.IndexOf('`');
                if (index != -1)
                {
                    index = index - fromType.Name.Length + name.Length;
                    name = name.Substring(0, index) + "$$" + numGeneric;
                }
            }

            return name;
        }



        int ImportGenericParameters(TypeReference fromType)
        {
            if (! fromType.HasGenericParameters)
                return 0;
            int numGeneric = fromType.GenericParameters.Count;
            if (numGeneric != 0)
            {
                GenericParameters = new List<CilType>(numGeneric);
                for (int i = 0; i < numGeneric; i++)
                {
                    var genericParameter = CilType.From(fromType.GenericParameters[i]);
                    GenericParameters.Add(genericParameter);
                }
                Flags |= HAS_GEN_PRM;
            }
            return numGeneric;
        }



        void ImportGenericParameter(GenericParameter fromType)
        {
            CopyFrom(SystemValueType);
            JavaName = GenericParameterFullName(fromType);
            Flags |= IS_GEN_PRM | VALUE;
        }



        public static CilType WrapMethodGenericParameter(CilType fromType, string newJavaName = null)
        {
            var newType = new CilType();
            newType.CopyFrom(JavaType.ObjectType);
            newType.Flags = fromType.Flags | IS_GEN_PRM;
            newType.JavaName = newJavaName ?? fromType.JavaName;
            newType.GenericParameters = new List<CilType>();
            newType.GenericParameters.Add(fromType);
            return newType;
        }



        public CilType GetMethodGenericParameter()
        {
            if (IsGenericParameter)
            {
                // if wrapped by WrapMethodGenericParameter, then will have
                // a single entry in GenericParameters, without flag HAS_GEN_PRM
                if ((! HasGenericParameters) && GenericParameters != null)
                    return GenericParameters[0];
            }
            return null;
        }



        public static string GenericParameterFullName(GenericParameter genericParameter)
        {
            string name = genericParameter.Name;
            string prefix;
            if (genericParameter.Type == GenericParameterType.Method)
            {
                prefix = "M!" + genericParameter.DeclaringMethod.FullName;
                if (name.StartsWith("!!"))
                {
                    name = CilMethod.AsDefinition(genericParameter.DeclaringMethod)
                                    .GenericParameters[genericParameter.Position].Name;
                }
                else if (name.StartsWith("!"))
                {
                    throw new ArgumentException();
                }
            }
            else
            {
                prefix = "T!" + genericParameter.DeclaringType.FullName;
                if (name.StartsWith("!"))
                {
                    name = CilType.AsDefinition(genericParameter.DeclaringType)
                                  .GenericParameters[genericParameter.Position].Name;
                }
            }
            return (prefix + "<" + name + ">");
        }



        void ImportPrimitive(MetadataType meta, string name)
        {
            TypeCode primitiveType;

            switch (meta)
            {
                case MetadataType.Void:     primitiveType = TypeCode.Empty;     break;
                case MetadataType.Boolean:  primitiveType = TypeCode.Boolean;   break;
                case MetadataType.Char:     primitiveType = TypeCode.Char;      break;
                case MetadataType.SByte:    primitiveType = TypeCode.SByte;     break;
                case MetadataType.Byte:     primitiveType = TypeCode.Byte;      break;
                case MetadataType.Int16:    primitiveType = TypeCode.Int16;     break;
                case MetadataType.UInt16:   primitiveType = TypeCode.UInt16;    break;
                case MetadataType.Int32:    primitiveType = TypeCode.Int32;     break;
                case MetadataType.UInt32:   primitiveType = TypeCode.UInt32;    break;
                case MetadataType.Int64:    primitiveType = TypeCode.Int64;     break;
                case MetadataType.UInt64:   primitiveType = TypeCode.UInt64;    break;
                case MetadataType.Single:   primitiveType = TypeCode.Single;    break;
                case MetadataType.Double:   primitiveType = TypeCode.Double;    break;

                case MetadataType.String:

                    CopyFrom(JavaType.StringType);
                    JavaName = "system.String";
                    return;

                default:

                    if (meta == MetadataType.Object || meta == MetadataType.TypedByReference)
                    {
                        CopyFrom(JavaType.ObjectType);
                        JavaName = "system.Object";
                        return;
                    }
                    else if (meta == MetadataType.IntPtr)
                        primitiveType = TypeCode.Int64;
                    else if (meta == MetadataType.UIntPtr)
                        primitiveType = TypeCode.UInt64;
                    else
                        throw CilMain.Where.Exception($"bad metadata '{meta}'");
                    break;
            }

            PrimitiveType = primitiveType;
            ArrayRank = 0;
            ClassName = null;
            JavaName = "system." + name;
        }



        void LinkSuperTypes(TypeDefinition defType)
        {
            int n = defType.HasInterfaces ? defType.Interfaces.Count : 0;

            SuperTypes = new List<CilType>(n + 1);
            SuperTypes.Add(CilType.From(
                                defType.BaseType ?? defType.Module.TypeSystem.Object));

            for (int i = 0; i < n; i++)
                SuperTypes.Add(CilType.From(defType.Interfaces[i].InterfaceType));

            for (int i = 0; i <= n; i++)
            {
                //if (SuperTypes[i].HasGenericParameters || SuperTypes[i].HasGenericSuperType)
                if (SuperTypes[i].IsGenericThisOrSuper)
                    Flags |= HAS_GEN_SUP;
            }
        }



        CilType Clone()
        {
            var newType = new CilType();
            newType.CopyFrom(this);
            newType.JavaName = JavaName;
            newType.SuperTypes = SuperTypes;
            newType.GenericParameters = GenericParameters;
            newType.Flags = Flags;
            return newType;
        }



        public CilType AdjustRank(int rankAdjust)
        {
            var newType = Clone();
            newType.ArrayRank += rankAdjust;
            return newType;
        }



        public CilType MakeByRef()
        {
            if (! IsValueClass)
                throw new ArgumentException();
            var newType = Clone();
            newType.Flags = Flags | BYREF;
            return newType;
        }



        public CilType MakeClonedAtTop()
        {
            // indicates that a local is cloned at the top of a method
            if (! IsValueClass)
                throw new ArgumentException();
            var newType = Clone();
            newType.Flags = Flags | CLONED_TOP;
            return newType;
        }



        public CilType MakeLiteral()
        {
            if (IsReference && (! Equals(JavaType.StringType)))
                throw new ArgumentException();
            var newType = Clone();
            newType.Flags = Flags | LITERAL;
            return newType;
        }



        public static CilType MakeSpanOf(CilType fromType)
        {
            if (! fromType.IsPointer)
                throw new ArgumentException();
            var newType = CodeSpan.SpanType.Clone();
            newType.Flags |= VALUE;
            // we add the original buffer type as a generic parameter,
            // but we do not mark with HAS_GEN_PRM HasGenericParameters,
            // similar to WrapMethodGenericParameter
            newType.GenericParameters = new List<CilType>();
            newType.GenericParameters.Add(fromType);
            return newType;
        }



        bool IsDelegateClass(TypeDefinition def) =>
               (! def.HasFields) && (def.BaseType != null) && (def.BaseType.Namespace == "System")
            && (def.BaseType.Name == "Delegate" || def.BaseType.Name == "MulticastDelegate");



        public bool IsDerivedFrom(CilType otherBaseType)
        {
            var obj = this;
            for (;;)
            {
                if (obj.JavaName.Equals(otherBaseType.JavaName))
                    return true;
                if (obj.SuperTypes == null || obj.SuperTypes.Count == 0)
                    return false;
                obj = obj.SuperTypes[0];
            }
        }



        public override bool AssignableTo(JavaType other)
        {
            if (base.AssignableTo(other))
                return true;

            if (SuperTypes != null)
            {
                foreach (var sup in SuperTypes)
                {
                    if (    (! sup.Equals(JavaType.ObjectType))
                         && (other.Equals(sup) || sup.AssignableTo(other)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override JavaType ResolveConflict(JavaType other)
        {
            // the ternary operator ?: may produce code that causes a conflict:
            //      object x = flag ? (object) new A() : (object) new B();
            // if we detect such a conflict at a branch target, we assume this
            // is the cause, and set the stack elements to a common denominator
            // or to the lowest common denominator, java.lang.Object
            if (IsReference && other.IsReference && other is CilType other2)
            {
                return FindCommonSuperType(this, other2)
                                    ?? CilType.From(JavaType.ObjectType);
            }
            return null;

            static CilType FindCommonSuperType(CilType type1, CilType type2)
            {
                if (type1.SuperTypes != null && type2.SuperTypes != null)
                {
                    foreach (var sup1 in type1.SuperTypes)
                    {
                        if (! sup1.Equals(JavaType.ObjectType))
                        {
                            if (FindCommonSuperTypeRecursive(sup1, type2))
                                return sup1;
                        }
                    }
                }
                return null;
            }

            static bool FindCommonSuperTypeRecursive(CilType sup1, CilType type2)
            {
                if (type2.SuperTypes != null)
                {
                    foreach (var sup2 in type2.SuperTypes)
                    {
                        if (sup2.Equals(sup1))
                            return true;
                        if (FindCommonSuperTypeRecursive(sup1, sup2))
                            return true;
                    }
                }
                return false;
            }
        }



        public CilType AsWritableClass
            => (IsReference ? this : CilType.From(new JavaType(0, 0, JavaName)));



        static Dictionary<TypeReference, TypeDefinition> _TypeDefs =
                                new Dictionary<TypeReference, TypeDefinition>();

        internal static TypeDefinition AsDefinition(TypeReference _ref)
        {
            if (_ref.IsDefinition)
                return _ref as TypeDefinition;
            if (_TypeDefs.TryGetValue(_ref, out var def))
                return def;
            def = _ref.Resolve();
            if (def == null && _ref.GetElementType() is GenericParameter)
                def = AsDefinition(_ref.Module.TypeSystem.Object);
            if (def != null)
            {
                _TypeDefs.Add(_ref, def);
                return def;
            }
            throw CilMain.Where.Exception(
                        $"could not resolve type '{_ref}' from assembly '{_ref.Scope}'");
        }



        static CilType[] _Primitives = new CilType[32];

        private static CilType GetCachedPrimitive(TypeReference fromType)
        {
            var metadataType = fromType.MetadataType;
            switch (metadataType)
            {
                case MetadataType.Void:
                case MetadataType.Boolean:
                case MetadataType.Char:
                case MetadataType.SByte:
                case MetadataType.Byte:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                case MetadataType.Int32:
                case MetadataType.UInt32:
                case MetadataType.Int64:
                case MetadataType.UInt64:
                case MetadataType.Single:
                case MetadataType.Double:
                case MetadataType.String:
                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
                case MetadataType.Object:

                    var resultType = _Primitives[(int) metadataType];
                    if (resultType == null)
                    {
                        resultType = new CilType();
                        resultType.Import(AsDefinition(fromType));
                        _Primitives[(int) metadataType] = resultType;
                    }
                    return resultType;
            }
            return null;
        }


        protected void SetBoxedFlags(bool clonedAtTop)
            => Flags |= (VALUE | BYREF) | (clonedAtTop ? CLONED_TOP : 0);



        public bool IsByReference => (Flags & BYREF) != 0;
        public bool IsPointer => (Flags & POINTER) != 0;
        public bool IsVolatile => (Flags & VOLATILE) != 0;
        public bool IsValue => (Flags & VALUE) != 0;
        public bool IsValueClass => (Flags & VALUE) != 0 && ClassName != null && ArrayRank == 0;
        public bool IsEnum => (Flags & ENUM) != 0;
        public bool IsInterface => (Flags & INTERFACE) != 0;
        public bool IsDelegate => (Flags & DELEGATE) != 0;
        public bool IsRetainName => (Flags & RETAIN_NAME) != 0;
        public bool IsLiteral => (Flags & LITERAL) != 0;
        public bool IsJaggedArray => (Flags & JAGGED) != 0;
        public bool IsGenericParameter => (Flags & IS_GEN_PRM) != 0;
        public bool HasGenericParameters => (Flags & HAS_GEN_PRM) != 0;
        public bool HasGenericSuperType => (Flags & HAS_GEN_SUP) != 0;
        public bool IsGenericThisOrSuper => (Flags & (HAS_GEN_PRM | HAS_GEN_SUP)) != 0;
        public bool IsClonedAtTop => (Flags & CLONED_TOP) != 0;
        public int GenericParametersCount => (Flags & HAS_GEN_PRM) != 0 ? GenericParameters.Count : 0;



        const int BYREF       = 0x0001;
        const int POINTER     = 0x0002;
        const int VOLATILE    = 0x0004;
        const int VALUE       = 0x0008;
        const int ENUM        = 0x0010;
        const int INTERFACE   = 0x0020;
        const int DELEGATE    = 0x0040;
        const int RETAIN_NAME = 0x0080;
        const int IS_GEN_PRM  = 0x0100;
        const int HAS_GEN_PRM = 0x0200;
        const int HAS_GEN_SUP = 0x0400;
        const int LITERAL     = 0x0800;
        const int CLONED_TOP  = 0x1000;
        const int DONT_CACHE  = 0x4000;
        const int JAGGED      = 0x8000;



        internal static readonly CilType SystemEnumType = CilType.From(new JavaType(0, 0, "system.Enum"));
        internal static readonly CilType SystemTypeType = CilType.From(new JavaType(0, 0, "system.Type"));
        internal static readonly CilType SystemRuntimeTypeType =
                                            CilType.From(new JavaType(0, 0, "system.RuntimeType"));
        internal static readonly JavaType SystemUtilType = new JavaType(0, 0, "system.Util");
        internal static readonly JavaType SystemValueType = new JavaType(0, 0, "system.ValueType");

        /*internal static readonly CilType ReflectFieldType =
                                            CilType.From(new JavaType(0, 0, "java.lang.reflect.Field"));*/
        internal static readonly CilType ReflectMethodType =
                                            CilType.From(new JavaType(0, 0, "java.lang.reflect.Method"));
    }



    public class BoxedType : CilType
    {
        public CilType UnboxedType;
        public bool IsLocal;

        public BoxedType(CilType fromType, bool isLocal)
        {
            if (fromType is BoxedType)
                throw CilMain.Where.Exception("already boxed");

            IsLocal = isLocal;
            UnboxedType = fromType;

            if (fromType.IsReference)
                ClassName = "system.Reference";
            else
                ClassName = fromType.JavaName;  // e.g., system.Int32

            JavaName = ClassName;

            SetBoxedFlags(isLocal);
        }

        protected virtual string BoxedClassName => ClassName;

        public bool IsBoxedReference => (BoxedClassName == "system.Reference");

        public bool IsBoxedIntPtr => (    BoxedClassName == "system.IntPtr"
                                       || BoxedClassName == "system.UIntPtr");

        protected JavaType GetBaseClass(JavaType cls)
        {
            var name = cls.ClassName;
            if (IsBoxedIntPtr || name == "system.UInt64")
            {
                // IntPtr, UIntPtr, UInt64 -> Int64
                name = "system.Int64";
            }
            else if (name == "system.UInt32")
                name = "system.Int32";
            else if (name == "system.UInt16")
                name = "system.Int16";
            else if (name == "system.Byte")
                name = "system.SByte";
            else
                return cls;
            return new JavaType(0, 0, name);
        }

        protected bool UnboxedTypeIsEnum
            => (UnboxedType.IsEnum && UnboxedType.ArrayRank == 0);

        private JavaType ThisOrEnum
            => (UnboxedTypeIsEnum ? SystemEnumType : GetBaseClass(this));

        protected JavaType UnboxedTypeInMethod =>
              UnboxedType.IsIntLike ? JavaType.IntegerType
            : UnboxedType.IsReference ? JavaType.ObjectType
            : UnboxedType;

        public virtual void BoxValue(JavaCode code)
        {
            if (UnboxedTypeIsEnum)
            {
                code.NewInstruction(0x12 /* ldc */, this, null);
                code.StackMap.PushStack(JavaType.ClassType);
                code.NewInstruction(0xB8 /* invokestatic */, SystemEnumType,
                                    new JavaMethodRef("Box", JavaType.ObjectType,
                                            UnboxedTypeInMethod, JavaType.ClassType));
                code.NewInstruction(0xC0 /* checkcast */, this, null);
                code.StackMap.PopStack(CilMain.Where);
            }
            else
            {
                code.NewInstruction(0xB8 /* invokestatic */, this,
                                    new JavaMethodRef("Box", this, UnboxedTypeInMethod));
            }
        }

        private string VolatileName(string nm, bool isVolatile)
            => ((UnboxedType.IsVolatile || isVolatile) ? ("Volatile" + nm) : nm);

        public virtual void GetValue(JavaCode code, bool isVolatile = false)
        {
            code.NewInstruction(0xB6 /* invokevirtual */, ThisOrEnum,
                                new JavaMethodRef(VolatileName("Get", isVolatile),
                                                  UnboxedTypeInMethod));
            if (UnboxedTypeInMethod.Category == 2)
                CilMain.MakeRoomForCategory2ValueOnStack(code);
        }

        public virtual void SetValueOV(JavaCode code, bool isVolatile = false)
        {
            code.NewInstruction(0xB6 /* invokevirtual */, ThisOrEnum,
                                new JavaMethodRef(VolatileName("Set", isVolatile),
                                                    JavaType.VoidType, UnboxedTypeInMethod));
        }

        public virtual void SetValueVO(JavaCode code, bool isVolatile = false)
        {
            var thisOrEnum = ThisOrEnum;
            code.NewInstruction(0xB8 /* invokestatic */, thisOrEnum,
                                new JavaMethodRef(VolatileName("Set", isVolatile),
                                    JavaType.VoidType, UnboxedTypeInMethod, thisOrEnum));
        }
    }



    public class ThreadBoxedType : BoxedType
    {
        string OldClassName;

        public ThreadBoxedType(CilType fromType)
            : base(fromType, false)
        {
            OldClassName = ClassName;
            ClassName = JavaName = "system.threading.ThreadLocal";
        }

        protected override string BoxedClassName => OldClassName;

        public override void BoxValue(JavaCode code) => throw new NotImplementedException();

        public JavaType GetInnerObject(JavaCode code)
        {
            var innerType = new JavaType(0, 0, OldClassName);
            code.NewInstruction(0xB6 /* invokevirtual */, this,
                                new JavaMethodRef("get", JavaType.ObjectType));
            code.NewInstruction(0xC0 /* checkcast */, innerType, null);
            return (UnboxedTypeIsEnum ? SystemEnumType : GetBaseClass(innerType));
        }

        public override void GetValue(JavaCode code, bool isVolatile = false)
        {
            var innerOrEnum = GetInnerObject(code);
            code.NewInstruction(0xB6 /* invokevirtual */, innerOrEnum,
                                new JavaMethodRef("Get", UnboxedTypeInMethod));
            if (UnboxedTypeInMethod.Category == 2)
                CilMain.MakeRoomForCategory2ValueOnStack(code);
        }

        public override void SetValueOV(JavaCode code, bool isVolatile = false)
        {
            var innerOrEnum = GetInnerObject(code);
            code.NewInstruction(0xB6 /* invokevirtual */, innerOrEnum,
                                new JavaMethodRef("Set", JavaType.VoidType, UnboxedTypeInMethod));
        }

        public override void SetValueVO(JavaCode code, bool isVolatile = false)
        {
            var innerOrEnum = GetInnerObject(code);
            code.NewInstruction(0xB8 /* invokestatic */, innerOrEnum,
                                new JavaMethodRef("Set",
                                        JavaType.VoidType, UnboxedTypeInMethod, innerOrEnum));
        }

    }

}
