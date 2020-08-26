
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;
using SpaceFlint.CilToJava;

public class DotNetImporter
{

    ModuleDefinition module;
    Dictionary<string, TypeReference> typeMap;
    HashSet<TypeReference> typeUse;
    HashSet<string> funcIfcNames;
    HashSet<TypeDefinition> funcIfcTypes;
    TypeReference systemException;
    MethodReference methodRefNotImplementedException;
    MethodBody methodBodyNotImplementedExceptionThrow;
    MethodReference retainNameAttributeConstructor;
    MethodReference asInterfaceAttributeConstructor;
    JavaException.Where Where;



    public DotNetImporter(ModuleDefinition _module)
    {
        module = _module;
        typeMap = new Dictionary<string, TypeReference>();
        typeUse = new HashSet<TypeReference>();
        funcIfcNames = new HashSet<string>();
        funcIfcTypes = new HashSet<TypeDefinition>();

        var typeSystem = module.TypeSystem;
        typeMap["./" + TypeCode.Empty]   = typeSystem.Void;
        typeMap["./" + TypeCode.Boolean] = typeSystem.Boolean;
        typeMap["./" + TypeCode.Byte]    = typeSystem.Byte;
        typeMap["./" + TypeCode.SByte]   = typeSystem.SByte;
        typeMap["./" + TypeCode.Char]    = typeSystem.Char;
        typeMap["./" + TypeCode.Int16]   = typeSystem.Int16;
        typeMap["./" + TypeCode.Int32]   = typeSystem.Int32;
        typeMap["./" + TypeCode.Int64]   = typeSystem.Int64;
        typeMap["./" + TypeCode.UInt16]  = typeSystem.UInt16;
        typeMap["./" + TypeCode.UInt32]  = typeSystem.UInt32;
        typeMap["./" + TypeCode.UInt64]  = typeSystem.UInt64;
        typeMap["./" + TypeCode.Single]  = typeSystem.Single;
        typeMap["./" + TypeCode.Double]  = typeSystem.Double;
        typeMap["java.lang.Object"]      = typeSystem.Object;
        typeMap["java.lang.String"]      = typeSystem.String;

        systemException = new TypeReference(
            "System", "Exception", module, typeSystem.CoreLibrary);

        methodRefNotImplementedException = new MethodReference(
            ".ctor", typeSystem.Void,
            new TypeReference(
                    "System", "NotImplementedException",
                    module, typeSystem.CoreLibrary));
        methodRefNotImplementedException.HasThis = true;

        Where = new JavaException.Where();
        Where.Push($"assembly '{module.Assembly.Name.Name}'");

        ImportCilTypes(module.Types);
    }



    void ImportCilTypes(Mono.Collections.Generic.Collection<TypeDefinition> types)
    {
        foreach (var cilType in types)
        {
            var jvmName = cilType.FullName.Replace('/', '$');
            if (typeMap.ContainsKey(jvmName))
                throw new JavaException($"duplicate class/type {jvmName}", Where);
            typeMap[jvmName] = cilType;

            if (cilType.HasNestedTypes)
                ImportCilTypes(cilType.NestedTypes);
        }

        CreateCustomAttribute("DiscardAttribute", out var discardAttribute, types);
        CreateCustomAttribute("RetainNameAttribute", out var retainNameAttribute, types);
        CreateCustomAttribute("RetainTypeAttribute", out var retainTypeAttribute, types);
        CreateCustomAttribute("AsInterfaceAttribute", out var asInterfaceAttribute, types);

        retainNameAttributeConstructor = GetParameterlessConstructor(retainNameAttribute);
        asInterfaceAttributeConstructor = GetParameterlessConstructor(asInterfaceAttribute);
    }



    void CreateCustomAttribute(string attrName, out TypeReference attrType,
                               Mono.Collections.Generic.Collection<TypeDefinition> types)
    {
        attrName = "java.attr." + attrName;
        if (typeMap.TryGetValue(attrName, out attrType))
            typeMap.Remove(attrName);
        else
        {
            var attrTypeDef = CreateCustomAttribute(attrName);
            types.Add(attrTypeDef);
            attrType = attrTypeDef;
        }
    }



    TypeDefinition CreateCustomAttribute(string typeName)
    {
        int dot = typeName.LastIndexOf('.');
        string nsName = typeName.Substring(0, dot);
        typeName = typeName.Substring(dot + 1);
        var typeSystem = module.TypeSystem;

        // create a custom attribute type
        var newType = new TypeDefinition(nsName, typeName,
                                TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        // which inherits from System.Attribute
        newType.BaseType = new TypeReference("System", "Attribute", module, typeSystem.CoreLibrary);
        // and specifies [System.AttributeUsageAttribute( ...
        var usageConstructor = new MethodReference(".ctor", typeSystem.Void, new TypeReference(
                    "System", "AttributeUsageAttribute", module, typeSystem.CoreLibrary));
        usageConstructor.HasThis = true;
        usageConstructor.Parameters.Add(new ParameterDefinition(
            new TypeReference("System", "AttributeTargets", module, typeSystem.CoreLibrary, true)));
        //  ... System.AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        newType.CustomAttributes.Add(new CustomAttribute(usageConstructor, new byte[] {
            0x01, 0x00, 0xFF, 0x7F, 0x00, 0x00, 0x01, 0x00, 0x54, 0x02, 0x09, 0x49, 0x6E,
            0x68, 0x65, 0x72, 0x69, 0x74, 0x65, 0x64, 0x00, 0x54, 0x02, 0x0D, 0x41, 0x6C,
            0x6C, 0x6F, 0x77, 0x4D, 0x75, 0x6C, 0x74, 0x69, 0x70, 0x6c, 0x65, 0x00 }));

        // now create a constructor for the new custom attribute
        var baseConstructor = new MethodReference(".ctor", typeSystem.Void, newType.BaseType);
        baseConstructor.HasThis = true;

        var newConstructor = new MethodDefinition(".ctor",
                    (MethodAttributes.Public | MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName | MethodAttributes.RTSpecialName),
                    typeSystem.Void);
        var body = new MethodBody(newConstructor);
        body.GetILProcessor().Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        body.GetILProcessor().Emit(Mono.Cecil.Cil.OpCodes.Call, baseConstructor);
        body.GetILProcessor().Emit(Mono.Cecil.Cil.OpCodes.Ret);
        newConstructor.Body = body;
        newType.Methods.Add(newConstructor);

        return newType;
    }



    MethodReference GetParameterlessConstructor(TypeReference typeRef)
    {
        var typeDef = typeRef.Resolve();
        foreach (var method in typeDef.Methods)
        {
            if (method.IsConstructor && method.Parameters.Count == 0)
                return method;
        }
        throw new ArgumentException("Parameterless constructor not found for type " + typeRef);
    }



    public void Merge(List<JavaClass> classes)
    {
        var classes2 = new HashSet<JavaClass>(classes.Count);

        foreach (var jclass in classes)
        {
            if (CreateCilTypeForClass(jclass))
                classes2.Add(jclass);

            if (    jclass.Flags == (   JavaAccessFlags.ACC_INTERFACE
                                      | JavaAccessFlags.ACC_ABSTRACT
                                      | JavaAccessFlags.ACC_PUBLIC)
                 && jclass.Methods != null && jclass.Methods.Count == 1)
            {
                // add interface to set of potentially functional interfaces
                funcIfcNames.Add(jclass.Name);
            }
        }

        foreach (var jclass in classes2)
        {
            Where.Push($"class '{jclass.Name}'");

            var cilType = typeMap[jclass.Name] as TypeDefinition;
            LinkCilTypesByClass(cilType, jclass);
            BuildCilTypeFromClass(cilType, jclass);

            Where.Pop();
        }

        // discard private types that are not referenced

        foreach (var cilType in typeMap.Values)
        {
            var cilTypeDef = cilType.Resolve();
            var visibility = cilTypeDef.Attributes & TypeAttributes.VisibilityMask;
            if (    visibility == TypeAttributes.NotPublic
                 || visibility == TypeAttributes.NestedPrivate)
            {
                if (! typeUse.Contains(cilType))
                {
                    if (cilTypeDef.DeclaringType == null)
                        module.Types.Remove(cilTypeDef);
                    else
                        cilTypeDef.DeclaringType.NestedTypes.Remove(cilTypeDef);
                }
            }
        }

        // create artificial delegate types for functional interfaces

        foreach (var functionalInterface in funcIfcTypes)
        {
            Where.Push($"functional interface '{functionalInterface}'");

            BuildDelegate(functionalInterface);

            Where.Pop();
        }
    }



    public void BuildDelegate(TypeDefinition fromInterface)
    {
        if (! fromInterface.HasMethods)
            return;
        var fromMethod = fromInterface.Methods[0];
        if ((! fromMethod.IsPublic) || (! fromMethod.IsAbstract))
            return;
        MethodAttributes attrs = 0;

        var newType = new TypeDefinition("", "Delegate", TypeAttributes.Public | TypeAttributes.Sealed);
        newType.CustomAttributes.Add(new CustomAttribute(asInterfaceAttributeConstructor));
        newType.BaseType = new TypeReference(
                            "System", "MulticastDelegate", module, module.TypeSystem.CoreLibrary);

        attrs = MethodAttributes.Public | MethodAttributes.HideBySig
              | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        var newConst = new MethodDefinition(".ctor", attrs, module.TypeSystem.Void);
        newConst.Parameters.Add(new ParameterDefinition("object", 0, module.TypeSystem.Object));
        newConst.Parameters.Add(new ParameterDefinition("method", 0, module.TypeSystem.IntPtr));
        SetCommonMethodBody(newConst);

        attrs = MethodAttributes.Public | MethodAttributes.HideBySig
              | MethodAttributes.Virtual | MethodAttributes.NewSlot;
        var newMethod = new MethodDefinition("Invoke", attrs, fromMethod.ReturnType);
        foreach (var fromMethodParm in fromMethod.Parameters)
            newMethod.Parameters.Add(fromMethodParm);
        newMethod.HasThis = true;
        SetCommonMethodBody(newMethod);

        attrs = MethodAttributes.Public | MethodAttributes.HideBySig;
        var newGetter = new MethodDefinition("AsInterface", attrs, fromInterface);
        newGetter.HasThis = true;
        SetCommonMethodBody(newGetter);

        newType.Methods.Add(newConst);
        newType.Methods.Add(newMethod);
        newType.Methods.Add(newGetter);

        fromInterface.NestedTypes.Add(newType);
    }



    public bool CreateCilTypeForClass(JavaClass jclass)
    {
        var name = jclass.Name;

        if (    typeMap.TryGetValue(name, out _)
             || jclass.IsInnerClass() &&
                (    name.StartsWith("java.lang.Object$")
                  || name.StartsWith("java.lang.String$")))
        {
            Console.WriteLine($"skipping duplicate class '{name}'");
            return false;
        }

        string nsName, clsName;
        TypeAttributes attrs = 0;

        if (jclass.IsInnerClass())
        {
            nsName = string.Empty;
            clsName = jclass.OuterAndInnerClasses[0].InnerShortName;

            if (! string.IsNullOrEmpty(clsName))
            {
                int idx = name.LastIndexOf('$');
                if (idx != -1 && idx + 1 < name.Length)
                    clsName = name.Substring(idx + 1);
            }
            if (string.IsNullOrEmpty(clsName) || (! Char.IsLetter(clsName[0])))
                clsName = "autogenerated_" + jclass.GetHashCode().ToString("X8");

            if ((jclass.Flags & JavaAccessFlags.ACC_PUBLIC) != 0)
                attrs = TypeAttributes.NestedPublic;
            else if ((jclass.Flags & JavaAccessFlags.ACC_PRIVATE) != 0)
                attrs = TypeAttributes.NestedPrivate;
            else if ((jclass.Flags & JavaAccessFlags.ACC_PROTECTED) != 0)
                attrs = TypeAttributes.NestedFamORAssem;
            else
                attrs = TypeAttributes.NestedAssembly;
        }
        else
        {
            int n = jclass.PackageNameLength;
            nsName = name.Substring(0, n);
            if (name[n] == '.')
                n++;
            clsName = name.Substring(n);
            if ((jclass.Flags & JavaAccessFlags.ACC_PUBLIC) != 0)
                attrs |= TypeAttributes.Public;
        }

        if ((jclass.Flags & JavaAccessFlags.ACC_ABSTRACT) != 0)
            attrs |= TypeAttributes.Abstract;
        if ((jclass.Flags & JavaAccessFlags.ACC_INTERFACE) != 0)
            attrs |= TypeAttributes.Interface;
        if ((jclass.Flags & JavaAccessFlags.ACC_FINAL) != 0)
            attrs |= TypeAttributes.Sealed;

        var newType = new TypeDefinition(nsName, clsName, attrs);
        newType.CustomAttributes.Add(new CustomAttribute(retainNameAttributeConstructor));

        typeMap[name] = newType;
        return true;
    }



    public void LinkCilTypesByClass(TypeDefinition cilType, JavaClass jclass)
    {
        if (jclass.IsInnerClass())
        {
            var outerName = jclass.OuterAndInnerClasses[0].OuterLongName;
            if (    typeMap.TryGetValue(outerName, out var cilOuterTypeRef)
                 && cilOuterTypeRef is TypeDefinition cilOuterType)
            {
                foreach (var nestedType in cilOuterType.NestedTypes)
                {
                    if (nestedType.Name == cilType.Name)
                    {
                        Console.WriteLine($"skipping duplicate class '{nestedType.FullName}'");
                        return;
                    }
                }
                cilOuterType.NestedTypes.Add(cilType);
            }
            else
            {
                throw new JavaException(
                            $"outer class '{outerName}' not found for inner class", Where);
            }
        }
        else
        {
            module.Types.Add(cilType);
        }

        var superName = jclass.Super;
        if (superName == "java.lang.Enum")
            superName = null;

        if (superName != null)
        {
            if (typeMap.TryGetValue(superName, out var cilSuperTypeRef))
            {
                if (cilType.IsInterface)
                {
                    if (superName != "java.lang.Object")
                    {
                        cilType.Interfaces.Add(
                                new InterfaceImplementation(cilSuperTypeRef));
                    }
                }
                else
                {
                    if (    jclass.Name == "java.lang.Throwable"
                         && superName == "java.lang.Object")
                    {
                        cilSuperTypeRef = systemException;
                    }
                    cilType.BaseType = cilSuperTypeRef;
                }
                CilTypeAddUse(cilSuperTypeRef);
            }
            else
                throw new JavaException($"super class '{superName}' not found", Where);
        }

        if (jclass.Interfaces != null)
        {
            foreach (var interfaceName in jclass.Interfaces)
            {
                if (typeMap.TryGetValue(interfaceName, out var cilInterfaceTypeRef))
                {
                    if (cilInterfaceTypeRef.Resolve()?.IsInterface == true)
                    {
                        cilType.Interfaces.Add(
                                    new InterfaceImplementation(cilInterfaceTypeRef));
                        CilTypeAddUse(cilInterfaceTypeRef);
                    }
                }
                else
                    throw new JavaException($"interface '{interfaceName}' not found", Where);
            }
        }
    }



    void BuildCilTypeFromClass(TypeDefinition cilType, JavaClass jclass)
    {
        if (jclass.Fields != null)
        {
            foreach (var jfield in jclass.Fields)
            {
                if (IsPublicOrProtected(jfield.Flags))
                {
                    Where.Push($"field '{jfield.Name}'");
                    var cilField = BuildCilField(cilType, jfield);
                    cilType.Fields.Add(cilField);
                    Where.Pop();
                }
            }
        }

        if (jclass.Methods != null)
        {
            foreach (var jmethod in jclass.Methods)
            {
                if (IsPublicOrProtected(jmethod.Flags))
                {
                    Where.Push($"method '{jmethod.Name}'");

                    // discard Java 8 default interface methods and static interface methods
                    bool skip =    ((jclass.Flags & JavaAccessFlags.ACC_INTERFACE) != 0)
                                && (    ((jmethod.Flags & JavaAccessFlags.ACC_ABSTRACT) == 0)
                                     || ((jmethod.Flags & JavaAccessFlags.ACC_STATIC) != 0));

                    // discard overriding bridge methods that merely change the return type
                    if (! skip)
                        skip = SkipMethodByReturnType(jmethod);

                    if (! skip)
                    {
                        var cilMethod = BuildCilMethod(cilType, jmethod);
                        if (cilMethod != null)
                            cilType.Methods.Add(cilMethod);
                    }
                    Where.Pop();
                }
            }

            if (jclass.Name == "java.lang.Class")
                JavaLangClassExplicitCast(cilType);
        }
    }



    FieldDefinition BuildCilField(TypeDefinition cilType, JavaField jfield)
    {
        FieldAttributes attrs = 0;

        if ((jfield.Flags & JavaAccessFlags.ACC_PUBLIC) != 0)
            attrs = FieldAttributes.Public;
        else if ((jfield.Flags & JavaAccessFlags.ACC_PRIVATE) != 0)
            attrs = FieldAttributes.Private;
        else if ((jfield.Flags & JavaAccessFlags.ACC_PROTECTED) != 0)
            attrs = FieldAttributes.FamORAssem;
        else
            attrs = FieldAttributes.Assembly;

        if ((jfield.Flags & JavaAccessFlags.ACC_STATIC) != 0)
            attrs |= FieldAttributes.Static;

        if ((jfield.Flags & JavaAccessFlags.ACC_FINAL) != 0)
            attrs |= FieldAttributes.InitOnly;

        var fieldDef = new FieldDefinition(
                jfield.Name, attrs, CilTypeReference(jfield.Type));

        if (cilType.IsInterface && jfield.Constant != null)
        {
            fieldDef.Constant = jfield.Constant;
            fieldDef.IsLiteral = true;
        }

        return fieldDef;
    }



    MethodDefinition BuildCilMethod(TypeDefinition cilType, JavaMethod jmethod)
    {
        MethodAttributes attrs = 0;

        if ((jmethod.Flags & JavaAccessFlags.ACC_PUBLIC) != 0)
            attrs = MethodAttributes.Public;
        else if ((jmethod.Flags & JavaAccessFlags.ACC_PRIVATE) != 0)
            attrs = MethodAttributes.Private;
        else if ((jmethod.Flags & JavaAccessFlags.ACC_PROTECTED) != 0)
            attrs = MethodAttributes.FamORAssem;
        else
            attrs = MethodAttributes.Assembly;

        var methodName = jmethod.Name;
        if (methodName == "<clinit>")
            return null;

        if (! cilType.IsInterface)
        {
            var newMethodName = CilMethod.TranslateNameJvmToClr(jmethod);
            if (newMethodName != null)
                methodName = newMethodName;
        }

        if ((jmethod.Flags & JavaAccessFlags.ACC_STATIC) != 0)
            attrs |= MethodAttributes.Static;
        else if (methodName == "<init>")
        {
            methodName = ".ctor";
            attrs |= MethodAttributes.SpecialName
                  |  MethodAttributes.RTSpecialName;
        }
        else
        {
            attrs |= MethodAttributes.Virtual;
            if ((jmethod.Flags & JavaAccessFlags.ACC_ABSTRACT) != 0)
                attrs |= MethodAttributes.Abstract;
        }

        if ((jmethod.Flags & JavaAccessFlags.ACC_FINAL) != 0)
            attrs |= MethodAttributes.Final;

        if (cilType.IsInterface)
        {
            attrs |= MethodAttributes.Abstract
                  |  MethodAttributes.NewSlot;
        }

        attrs |= MethodAttributes.HideBySig;

        var methodDef = new MethodDefinition(
                methodName, attrs, CilTypeReference(jmethod.ReturnType));

        if ((jmethod.Flags & JavaAccessFlags.ACC_STATIC) == 0)
            methodDef.HasThis = true;

        foreach (var jparam in jmethod.Parameters)
        {
            var paramType = CilTypeReference(jparam.Type);

            var newParamDef = new ParameterDefinition(jparam.Name, 0, paramType);
            methodDef.Parameters.Add(newParamDef);

            var paramClass = jparam.Type.ClassName;
            if (    paramClass != null && funcIfcNames.Contains(paramClass)
                 && paramType is TypeDefinition paramTypeDef)
            {
                // move interface from set of potentially functional interfaces
                // to set of such interfaces actually appearing as parameter
                funcIfcNames.Remove(paramClass);
                funcIfcTypes.Add(paramTypeDef);
            }
        }

        if ((jmethod.Flags & JavaAccessFlags.ACC_ABSTRACT) == 0
                && (! cilType.IsInterface))
        {
            SetCommonMethodBody(methodDef);
        }

        return methodDef;
    }



    void SetCommonMethodBody(MethodDefinition method)
    {
        if (methodBodyNotImplementedExceptionThrow == null)
        {
            var body = new MethodBody(method);

            body.GetILProcessor().Emit(
                    Mono.Cecil.Cil.OpCodes.Newobj, methodRefNotImplementedException);

            body.GetILProcessor().Emit(
                    Mono.Cecil.Cil.OpCodes.Throw);

            methodBodyNotImplementedExceptionThrow = body;
        }

        method.Body = methodBodyNotImplementedExceptionThrow;
    }



    TypeReference CilTypeReference(JavaType jtype)
    {
        string typeName = jtype.ClassName;
        if (typeName == null)
            typeName = "./" + jtype.PrimitiveType;

        if (typeMap.TryGetValue(typeName, out var cilType))
        {
            CilTypeAddUse(cilType);
            if (jtype.ArrayRank != 0)
                cilType = new ArrayType(cilType, jtype.ArrayRank);
            return cilType;
        }

        if (typeName.IndexOf('$') == -1)
        {
            Console.WriteLine($"auto-generating empty class for '{typeName}'");

            string nsName, clsName;
            int dot = typeName.LastIndexOf('.');
            if (dot == -1 || dot + 1 == typeName.Length)
            {
                nsName = null;
                clsName = typeName;
            }
            else
            {
                nsName = typeName.Substring(0, dot - 1);
                clsName = typeName.Substring(dot + 1);
            }

            cilType = new TypeDefinition(nsName, clsName, TypeAttributes.Public);
            CilTypeAddUse(cilType);
            typeMap[typeName] = cilType;
            module.Types.Add(cilType as TypeDefinition);
            return cilType;
        }

        throw new JavaException($"unknown type '{typeName}'", Where);
    }



    void CilTypeAddUse(TypeReference cilType)
    {
        while (cilType != null)
        {
            typeUse.Add(cilType);
            cilType = cilType.DeclaringType;
        }
    }



    static bool IsPublicOrProtected(JavaAccessFlags flags)
    {
        return (flags & (JavaAccessFlags.ACC_PUBLIC | JavaAccessFlags.ACC_PROTECTED)) != 0;
    }



    static bool SkipMethodByReturnType(JavaMethod method)
    {
        // discard bridge methods that only change the return types

        int n = method.Parameters?.Count ?? 0;
        foreach (var otherMethod in method.Class.Methods)
        {
            if (otherMethod != method && otherMethod.Name == method.Name && method.Name != "<init>")
            {
                int n2 = otherMethod.Parameters?.Count ?? 0;
                if (n == n2)
                {
                    bool same = true;
                    for (int i = 0; i < n && same; i++)
                    {
                        same = method.Parameters[i].Type.Equals(otherMethod.Parameters[i].Type);
                    }
                    if (same)
                    {
                        if (    method.ReturnType.ClassName != method.Class.Name
                             && otherMethod.ReturnType.ClassName == method.Class.Name)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }



    void JavaLangClassExplicitCast(TypeDefinition cilType)
    {
        MethodAttributes attrs = MethodAttributes.Public
                               | MethodAttributes.Static
                               | MethodAttributes.HideBySig
                               | MethodAttributes.SpecialName;
        var method = new MethodDefinition(
                "op_Explicit", attrs, CilTypeReference(JavaType.ClassType));
        method.Parameters.Add(new ParameterDefinition(
                    "type", 0, new TypeReference("System", "Type",
                                        module, module.TypeSystem.CoreLibrary)));
        SetCommonMethodBody(method);
        cilType.Methods.Add(method);
    }

}
