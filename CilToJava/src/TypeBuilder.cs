
using System.Collections.Generic;
using Mono.Cecil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public static class TypeBuilder
    {

        internal static void BuildJavaClass(TypeDefinition cilType, JavaClass parentClass)
        {
            CilMain.Where.Push($"class '{cilType.FullName}'");

            var genericMark = CilMain.GenericStack.Mark();
            var myType = CilMain.GenericStack.EnterType(cilType);

            var jclass = new JavaClass();
            jclass.Name = myType.JavaName;
            jclass.Flags = AttributesToAccessFlags(cilType.Attributes, myType.IsInterface);

            if (myType.IsInterface)
                jclass.Super = JavaType.ObjectType.ClassName; // java.lang.Object
            else if (cilType.BaseType != null)
            {
                var myBaseType = CilType.From(cilType.BaseType);
                jclass.Super = myBaseType.Equals(JavaType.ObjectType)
                             ? JavaType.ObjectType.ClassName // java.lang.Object
                             : myBaseType.JavaName;
            }
            else
                throw CilMain.Where.Exception("missing base class");

            var myInterfaces = ImportInterfaces(jclass, myType, cilType);

            int numCastableInterfaces = myType.IsGenericThisOrSuper
                                      ? InterfaceBuilder.CastableInterfaceCount(myInterfaces)
                                      : 0;

            ImportFields(jclass, cilType, myType.IsRetainName);

            ImportMethods(jclass, cilType, numCastableInterfaces);

            ValueUtil.InitializeStaticFields(jclass, myType);

            if (myType.IsValueClass)
            {
                ValueUtil.MakeValueClass(jclass, myType, numCastableInterfaces);
            }

            else if (myType.IsEnum)
            {
                ValueUtil.MakeEnumClass(jclass, myType,
                                cilType.HasCustomAttribute("System.FlagsAttribute", true));
            }

            else if (myType.IsDelegate)
            {
                var delegateInterface = Delegate.FixClass(jclass, myType);
                CilMain.JavaClasses.Add(delegateInterface);
            }

            // if derives directly from object, and does not implement ToString
            CodeBuilder.CreateToStringMethod(jclass);

            ResetFieldReferences(jclass);

            LinkClasses(jclass, parentClass, cilType);

            var interfaceClasses = InterfaceBuilder.BuildProxyMethods(
                                            myInterfaces, cilType, myType, jclass);
            if (interfaceClasses != null)
            {
                foreach (var childClass in interfaceClasses)
                    CilMain.JavaClasses.Add(childClass);
            }

            if (myType.HasGenericParameters)
            {
                JavaClass dataClass;

                if (! myType.IsInterface)
                {
                    dataClass = GenericUtil.MakeGenericClass(jclass, myType);
                    if (dataClass != null)
                        CilMain.JavaClasses.Add(dataClass);
                }
                else
                    dataClass = null;

                JavaClass infoClass = jclass;
                if (myType.IsInterface)
                {
                    // Android 'D8' desugars static methods on an interface by
                    // moving into a separate class, so we do it ourselves.
                    // see also system.RuntimeType.CreateGeneric() in baselib
                    infoClass = CilMain.CreateInnerClass(jclass, jclass.Name + "$$info");
                    CilMain.JavaClasses.Add(infoClass);
                }

                GenericUtil.CreateGenericInfoMethod(infoClass, dataClass, myType);

                GenericUtil.CreateGenericVarianceField(infoClass, myType, cilType);
            }

            if (myType.IsGenericThisOrSuper)
            {
                jclass.Signature = GenericUtil.MakeGenericSignature(cilType, jclass.Super);

                if (! myInterfaces.Exists(x => x.InterfaceType.JavaName == "system.IGenericObject"))
                {
                    if (! myType.IsInterface)
                    {
                        // create IGenericObject methods GetType and TryCast
                        // only if class did not already implement IGenericObject
                        if (! myType.HasGenericParameters)
                        {
                            GenericUtil.BuildGetTypeMethod(jclass, myType);
                        }

                        InterfaceBuilder.BuildTryCastMethod(
                                            myInterfaces, myType, numCastableInterfaces, jclass);
                    }
                }
            }

            CilMain.GenericStack.Release(genericMark);
            CilMain.Where.Pop();
        }



        static JavaAccessFlags AttributesToAccessFlags(TypeAttributes attrs, bool isInterface)
        {
            var attrs0 = attrs;
            JavaAccessFlags flags = 0;

            var visibilityMask = attrs & TypeAttributes.VisibilityMask;
            if (visibilityMask == TypeAttributes.NestedPrivate)
            {
                flags |= JavaAccessFlags.ACC_PRIVATE;
            }
            else if (    visibilityMask == TypeAttributes.NestedFamily
                      || visibilityMask == TypeAttributes.NestedFamANDAssem)
            {
                flags |= JavaAccessFlags.ACC_PROTECTED;
            }
            else
            {
                // an assembly can include more than one package namespace,
                // so even assembly-private-internal types must be made public
                flags |= JavaAccessFlags.ACC_PUBLIC;
            }

            attrs &= ~TypeAttributes.VisibilityMask;

            if (isInterface)
            {
                flags |= JavaAccessFlags.ACC_INTERFACE
                      |  JavaAccessFlags.ACC_ABSTRACT;
            }
            else
            {
                flags |= JavaAccessFlags.ACC_SUPER;

                if ((attrs & TypeAttributes.Abstract) != 0)
                {
                    flags |= JavaAccessFlags.ACC_ABSTRACT;
                }
                else if ((attrs & TypeAttributes.Sealed) != 0)
                {
                    flags |= JavaAccessFlags.ACC_FINAL;
                }
            }

            attrs &= ~( TypeAttributes.Abstract
                      | TypeAttributes.Sealed
                      | TypeAttributes.SpecialName
                      | TypeAttributes.Interface
                      | TypeAttributes.LayoutMask
                      | TypeAttributes.Serializable
                      | TypeAttributes.StringFormatMask
                      | TypeAttributes.BeforeFieldInit
                      | TypeAttributes.HasSecurity);

            if (attrs != 0)
                throw CilMain.Where.Exception($"unrecognized attributes {attrs0:X}");

            return flags;
        }



        public static List<CilInterface> ImportInterfaces(JavaClass jclass, CilType myType,
                                                          TypeDefinition cilType)
        {
            var myInterfaces = CilInterface.CollectAll(cilType);

            int n = myInterfaces.Count;
            if (n > 0)
            {
                jclass.Interfaces = new List<string>(n);
                for (int i = 0; i < n; i++)
                {
                    if (myInterfaces[i].DirectReference)
                    {
                        var ifcJavaName = myInterfaces[i].InterfaceType.JavaName;
                        if (ifcJavaName != "system.ValueMethod")
                        {
                            jclass.AddInterface(ifcJavaName);
                        }
                    }
                }
            }

            if ((cilType.Attributes & TypeAttributes.Serializable) != 0)
            {
                jclass.AddInterface("java.io.Serializable");
            }

            if ((cilType.Attributes & TypeAttributes.Interface) != 0)
            {
                // super of interface is always java.lang.Object, per JLS 4.1
                jclass.Super = JavaType.ObjectType.ClassName;
            }

            return myInterfaces;
        }



        public static void ImportFields(JavaClass jclass, TypeDefinition cilType, bool isRetainName)
        {
            if (cilType.HasFields)
            {
                int n = cilType.Fields.Count;
                if (n > 0)
                {
                    if (isRetainName)
                        throw CilMain.Where.Exception("fields not supported in a [RetainName] type");

                    jclass.Fields = new List<JavaField>(n);
                    for (int i = 0; i < n; i++)
                    {
                        var cilField = cilType.Fields[i];
                        CilMain.Where.Push($"field '{cilField.Name}'");

                        if (cilField.InitialValue.Length != 0)
                            throw CilMain.Where.Exception("unsupported InitialValue in field");

                        var myField = new JavaField();
                        myField.Name = CilMain.MakeValidMemberName(cilField.Name);
                        myField.Class = jclass;
                        myField.Flags = AttributesToAccessFlags(
                                                cilField.Attributes,
                                            (cilType.HasNestedTypes || cilType.HasGenericParameters));

                        if (cilType.IsEnum)
                        {
                            myField.Type = CilType.From(cilField.FieldType);

                            if (cilField.Constant != null)
                                myField.InitConstant(cilField.Constant, CilMain.Where);
                        }
                        else
                        {
                            myField.Type = ValueUtil.GetBoxedFieldType(null, cilField);

                            if (((CilType) myField.Type).IsValueClass)
                                myField.Constant = cilField;

                            else
                            {
                                if (cilField.Constant != null)
                                    myField.InitConstant(cilField.Constant, CilMain.Where);

                                if (((CilType) myField.Type).IsVolatile)
                                    myField.Flags |= JavaAccessFlags.ACC_VOLATILE;
                            }
                        }

                        jclass.Fields.Add(myField);

                        CilMain.Where.Pop();
                    }
                }
            }
        }



        public static void ResetFieldReferences(JavaClass jclass)
        {
            // reset all references to FieldDefinition, which were stored by
            // ImportFields (see above) in the Constant field of each JavaField

            if (jclass.Fields != null)
            {
                foreach (var fld in jclass.Fields)
                {
                    if (fld.Constant is FieldDefinition)
                        fld.Constant = null;
                }
            }
        }


        static JavaAccessFlags AttributesToAccessFlags(FieldAttributes attrs, bool hasInnerOrIsGeneric)
        {
            var attrs0 = attrs;
            JavaAccessFlags flags = 0;

            var fieldAccessMask = attrs & FieldAttributes.FieldAccessMask;
            attrs &= ~FieldAttributes.FieldAccessMask;
            switch (fieldAccessMask)
            {
                case FieldAttributes.Private:
                    // .Net nested types can access private fields of parent type,
                    // to emulate this in Java we use the default access modifier
                    // of package-private, rather than ACC_PRIVATE
                    if (! hasInnerOrIsGeneric)
                        flags |= JavaAccessFlags.ACC_PRIVATE;
                    break;

                case FieldAttributes.Family:
                case FieldAttributes.FamANDAssem:
                    flags |= JavaAccessFlags.ACC_PROTECTED;
                    break;

                case FieldAttributes.Assembly:
                case FieldAttributes.FamORAssem:
                    // an assembly can include more than one package namespace
                default:
                    flags |= JavaAccessFlags.ACC_PUBLIC;
                    break;
            }

            if ((attrs & FieldAttributes.Static) != 0)
            {
                flags |= JavaAccessFlags.ACC_STATIC;
                attrs &= ~FieldAttributes.Static;
            }

            if ((attrs & FieldAttributes.InitOnly) != 0)
            {
                flags |= JavaAccessFlags.ACC_FINAL;
                attrs &= ~FieldAttributes.InitOnly;
            }

            if ((attrs & FieldAttributes.NotSerialized) != 0)
            {
                flags |= JavaAccessFlags.ACC_TRANSIENT;
                attrs &= ~FieldAttributes.NotSerialized;
            }

            attrs &= ~(   FieldAttributes.Literal
                        | FieldAttributes.HasFieldRVA
                        | FieldAttributes.HasDefault
                        | FieldAttributes.SpecialName
                        | FieldAttributes.RTSpecialName);

            if (attrs != 0)
                throw CilMain.Where.Exception($"unrecognized attributes {attrs0:X}");

            return flags;
        }



        public static void ImportMethods(JavaClass jclass, TypeDefinition cilType,
                                         int numCastableInterfaces)
        {
            if (cilType.HasMethods)
            {
                int n = cilType.Methods.Count;
                if (n > 0)
                {
                    jclass.Methods = new List<JavaMethod>(n);
                    for (int i = 0; i < n; i++)
                    {
                        var defMethod = cilType.Methods[i];
                        /*
                        if (defMethod.HasCustomAttribute("Discard"))
                            continue; // if decorated with [java.attr.Discard], don't export to java
                        */

                        var genericMark = CilMain.GenericStack.Mark();
                        var myMethod = CilMain.GenericStack.EnterMethod(defMethod);

                        var newMethod = new JavaMethod(jclass, myMethod.WithGenericParameters);
                        newMethod.Flags = AttributesToAccessFlags(
                                                defMethod.Attributes, defMethod.HasOverrides,
                                            (cilType.HasNestedTypes || cilType.HasGenericParameters));

                        if (myMethod.IsStatic & myMethod.IsConstructor)
                        {
                            newMethod.Flags &= ~(   JavaAccessFlags.ACC_PUBLIC
                                                  | JavaAccessFlags.ACC_PRIVATE
                                                  | JavaAccessFlags.ACC_PROTECTED);
                        }

                        if (defMethod.HasBody)
                        {
                            CilMain.Where.Push($"method '{defMethod.Name}'");
                            CodeBuilder.BuildJavaCode(newMethod, myMethod, defMethod,
                                                      numCastableInterfaces);

                            if ((defMethod.ImplAttributes & MethodImplAttributes.Synchronized) != 0)
                            {
                                // if method is decorated with [MethodImplOptions.Synchronized],
                                // create a wrapper method that locks appropriately
                                jclass.Methods.Add(
                                        CodeBuilder.CreateSyncWrapper(newMethod, myMethod.DeclType));
                            }
                            else if (    defMethod.Name == "Finalize"
                                      && (! defMethod.HasParameters) && defMethod.IsVirtual)
                            {
                                // if method is a finalizer, create a wrapper method that
                                // checks if finalization was suppressed for the object

                                CodeBuilder.CreateSuppressibleFinalize(
                                                            newMethod, myMethod.DeclType, jclass);
                            }

                            if (defMethod.IsVirtual)
                            {
                                InterfaceBuilder.BuildOverloadProxy(
                                                    cilType, defMethod, myMethod, jclass);
                            }
                            else if (! myMethod.IsConstructor)
                            {
                                newMethod.Flags |= JavaAccessFlags.ACC_FINAL;
                            }

                            CilMain.Where.Pop();
                        }
                        else
                        {
                            // clear ACC_STATIC and access, set ACC_ABSTRACT and ACC_PUBLIC
                            newMethod.Flags = (newMethod.Flags | JavaAccessFlags.ACC_ABSTRACT
                                                               | JavaAccessFlags.ACC_PUBLIC)
                                           & ~(    JavaAccessFlags.ACC_STATIC
                                                 | JavaAccessFlags.ACC_PRIVATE
                                                 | JavaAccessFlags.ACC_PROTECTED);
                        }

                        jclass.Methods.Add(newMethod);

                        CilMain.GenericStack.Release(genericMark);

                        if (myMethod.IsConstructor)
                        {
                            var dummyClass = CreateDummyClassForConstructor(myMethod, jclass);
                            if (dummyClass != null)
                                CilMain.JavaClasses.Add(dummyClass);
                        }
                        else if (    myMethod.WithGenericParameters != myMethod
                                  && (! myMethod.IsRetainName))
                        {
                            jclass.Methods.Add(
                                Delegate.CreateCapturingBridgeMethod(
                                            newMethod, myMethod.Parameters, cilType.IsInterface));
                        }
                    }
                }
            }
            else
                jclass.Methods = new List<JavaMethod>(0);

            JavaClass CreateDummyClassForConstructor(CilMethod theMethod, JavaClass theClass)
            {
                if (! theMethod.HasDummyClassArg)
                    return null;
                return CilMain.CreateInnerClass(
                            theClass,
                            theMethod.Parameters[
                                        theMethod.Parameters.Count - 1].Type.ClassName);
            }
        }



        static JavaAccessFlags AttributesToAccessFlags(MethodAttributes attrs,
                                                       bool hasOverrides, bool hasInnerOrIsGeneric)
        {
            var attrs0 = attrs;
            JavaAccessFlags flags = 0;

            var methodAccessMask = attrs & MethodAttributes.MemberAccessMask;
            attrs &= ~MethodAttributes.MemberAccessMask;

            if (hasOverrides)
            {
                // explicit interface implementation is private in cil,
                // but must be made public callable in the jvm
                flags |= JavaAccessFlags.ACC_PUBLIC;
            }
            else
            {
                switch (methodAccessMask)
                {
                    case MethodAttributes.Private:
                        // .Net nested types can access private methods of parent type,
                        // to emulate this in Java we use the default access modifier
                        // of package-private, rather than ACC_PRIVATE
                        if (! hasInnerOrIsGeneric)
                            flags |= JavaAccessFlags.ACC_PRIVATE;
                        break;

                    case MethodAttributes.Family:
                    case MethodAttributes.FamANDAssem:
                        flags |= JavaAccessFlags.ACC_PROTECTED;
                        break;

                    case MethodAttributes.Assembly:
                    case MethodAttributes.FamORAssem:
                        // an assembly can include more than one package namespace
                    default:
                        flags |= JavaAccessFlags.ACC_PUBLIC;
                        break;
                }
            }

            if ((attrs & MethodAttributes.Static) != 0)
            {
                flags |= JavaAccessFlags.ACC_STATIC;
                attrs &= ~MethodAttributes.Static;
            }

            if ((attrs & MethodAttributes.PInvokeImpl) != 0)
            {
                // note that an extern (PInvoke) method would not have a body,
                // so ImportMethods would set ACC_ABSTRACT and clear ACC_STATIC
                attrs &= ~MethodAttributes.PInvokeImpl;
            }

            attrs &= ~(   MethodAttributes.Final
                        | MethodAttributes.Virtual
                        | MethodAttributes.NewSlot
                        | MethodAttributes.HideBySig
                        | MethodAttributes.Abstract
                        | MethodAttributes.CheckAccessOnOverride
                        | MethodAttributes.SpecialName
                        | MethodAttributes.RTSpecialName
                        | MethodAttributes.HasSecurity
                        | MethodAttributes.RequireSecObject);

            if (attrs != 0)
                throw CilMain.Where.Exception($"unrecognized attributes {attrs0:X}");

            return flags;
        }



        static void LinkClasses(JavaClass thisClass, JavaClass parentClass, TypeDefinition cilType)
        {
            CilMain.JavaClasses.Add(thisClass);

            if (parentClass == null)
            {
                int lastDot = thisClass.Name.LastIndexOf('.');
                if (lastDot != -1)
                    thisClass.PackageNameLength = (short) lastDot;
            }
            else
            {
                thisClass.PackageNameLength = parentClass.PackageNameLength;

                parentClass.AddInnerClass(thisClass);
            }

            if (cilType.HasNestedTypes)
            {
                foreach (var nestedCilType in cilType.NestedTypes)
                {
                    if (    nestedCilType.Name == "SynchronizedList"
                         && cilType.Name == "List`1"
                         && cilType.Namespace == "System.Collections.Generic")
                    {
                        // skip internal class SynchronizedList which is never used
                        continue;
                    }
                    BuildJavaClass(nestedCilType, thisClass);
                }
            }
        }

    }
}
