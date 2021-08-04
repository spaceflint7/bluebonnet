
using System;
using System.Collections.Generic;
using Mono.Cecil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public static class InterfaceBuilder
    {

        public static int CastableInterfaceCount(List<CilInterface> allInterfaces)
        {
            // a castable interface is an interface with generic parameters,
            // except those that are already implemented by a base type,
            // because those were already counted as castable in the base type.
            // see also: InitInterfaceArrayField and BuildTryCastMethod.

            int num = 0;
            foreach (var ifc in allInterfaces)
            {
                if (ifc.GenericTypes != null && (! ifc.SuperImplements))
                    num++;
            }
            return num;
        }



        public static List<JavaClass> BuildProxyMethods(List<CilInterface> allInterfaces,
                                                        TypeDefinition fromType, CilType intoType,
                                                        JavaClass theClass)
        {
            //
            // process only if the class (or interface) has any methods or super interfaces
            //

            var classMethods = theClass.Methods;
            if (classMethods.Count == 0)
                return null;

            bool isInterface = intoType.IsInterface;
            if ((! isInterface) && theClass.Interfaces == null)
                return null;

            var theMethods = CilInterfaceMethod.CollectAll(fromType);

            //
            // if any interfaces are marked [RetainName], make sure that
            // all corresponding methods are also marked [RetainName]
            //

            CheckRetainNameMethods(theMethods, allInterfaces, intoType);

            //
            // if this is an abstract class but forced to an interface via [AddInterface]
            // decoration, then we need to remove all constructors generated for the class
            //

            if (intoType.IsInterface)
            {
                if (! fromType.IsInterface)
                {
                    for (int i = classMethods.Count; i-- > 0; )
                    {
                        if (classMethods[i].Name == "<init>")
                            classMethods.RemoveAt(i);
                    }
                }

                if (intoType.HasGenericParameters)
                {
                    // the RuntimeType constructor in baselib uses IGenericEntity
                    // marker interface to identify generic classes.  note that
                    // real generic types implement IGenericObject -> IGenericEntity.
                    theClass.AddInterface("system.IGenericEntity");
                }

                return null;
            }

            //
            // for each implemented interface, build proxy methods
            //

            List<JavaClass> output = null;

            int ifcNumber = 0;
            foreach (var ifc in allInterfaces)
            {
                if ((! ifc.DirectReference) && ifc.SuperImplements)
                {
                    // we don't have to build proxy for an interface if it is
                    // implemented by a super type and not by our primary type
                    continue;
                }
                if (ifc.GenericTypes == null)
                {
                    foreach (var ifcMethod in ifc.Methods)
                    {
                        // build proxy methods:  interface$method -> method
                        var newMethod = BuildPlainProxy(ifcMethod, intoType, theMethods);
                        if (newMethod != null)
                        {
                            newMethod.Class = theClass;
                            theClass.Methods.Add(newMethod);
                        }
                    }
                }
                else
                {
                    var ifcClass = CreateInnerClass(theClass, intoType, ++ifcNumber);
                    ifcClass.AddInterface(ifc.InterfaceType.JavaName);

                    if (output == null)
                    {
                        output = new List<JavaClass>();
                        CreateInterfaceArrayField(theClass);
                    }
                    output.Add(ifcClass);

                    // if the class implements a generic interface for multiple types,
                    // then we need a method suffix to differentiate between the methods.
                    // see also:  CilMethod::InsertMethodNamePrefix
                    /*string methodSuffix = "";
                    foreach (var genericType in ifc.GenericTypes)
                        methodSuffix += "--" + CilMethod.GenericParameterSuffixName(genericType);*/

                    foreach (var ifcMethod in ifc.Methods)
                    {
                        // build proxy classes:  proxy sub-class -> this class
                        BuildGenericProxy(ifcMethod, /*methodSuffix,*/ intoType, theMethods, ifcClass);
                    }
                }
            }

            return output;

            JavaClass CreateInnerClass(JavaClass parentClass, CilType parentType, int ifcNumber)
            {
                // generic interfaces are implemented as proxy sub-classes which
                // call methods on the parent class object.  we need to define
                // an inner class.  this class has one instance field which is a
                // reference to the parent class.  the constructor takes this
                // reference as a parameter and initializes the instance field.

                var newClass = CilMain.CreateInnerClass(parentClass,
                                    parentClass.Name + "$$generic" + ifcNumber.ToString(),
                                    markGenericEntity: true);

                var fld = new JavaField();
                fld.Name = ParentFieldName;
                fld.Type = parentType;
                fld.Class = newClass;
                fld.Flags = JavaAccessFlags.ACC_PRIVATE;
                newClass.Fields.Add(fld);

                var code = CilMain.CreateHelperMethod(newClass,
                                new JavaMethodRef("<init>", JavaType.VoidType, JavaType.ObjectType),
                                2, 2);
                code.Method.Flags &= ~JavaAccessFlags.ACC_BRIDGE;   // invalid for constructor

                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                code.NewInstruction(0xB7 /* invokespecial */, JavaType.ObjectType,
                                                new JavaMethodRef("<init>", JavaType.VoidType));
                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                code.NewInstruction(0x19 /* aload */, null, (int) 1);
                code.NewInstruction(0xC0 /* checkcast */, parentType, null);
                code.NewInstruction(0xB5 /* putfield */, new JavaType(0, 0, newClass.Name),
                                                    new JavaFieldRef(ParentFieldName, parentType));
                code.NewInstruction(JavaType.VoidType.ReturnOpcode, null, null);

                return newClass;
            }

            void CreateInterfaceArrayField(JavaClass parentClass)
            {
                // the parent class has a helper array field that is used to track
                // the proxy objects generated for implemented generic interfaces.
                // see also: InitInterfaceArrayField, below.

                var fld = new JavaField();
                fld.Name = InterfaceArrayField.Name;
                fld.Type = InterfaceArrayField.Type;
                fld.Class = parentClass;
                fld.Flags = JavaAccessFlags.ACC_PRIVATE;

                if (parentClass.Fields == null)
                    parentClass.Fields = new List<JavaField>(1);
                parentClass.Fields.Add(fld);
            }
        }



        //
        // InitInterfaceArrayField
        //

        public static void InitInterfaceArrayField(CilType toType, int numCastableInterfaces,
                                                  JavaCode code, int objectIndex)
        {
            // if a type has castable interface (as counted by CastableInterfaceCount),
            // then we need to initialize the helper array field, for use by the
            // implementation of the IGenericObject.TryCast helper method

            if (numCastableInterfaces == 0)
                return;

            // objectIndex specifies the local index for the object reference,
            // e.g. 0 for the 'this' object, or -1 for top of stack.
            if (objectIndex == -1)
                code.NewInstruction(0x59 /* dup */, null, null);
            else
                code.NewInstruction(0x19 /* aload */, null, objectIndex);
            code.StackMap.PushStack(toType);

            code.NewInstruction(0xBB /* new */, AtomicReferenceArrayType, null);
            code.StackMap.PushStack(AtomicReferenceArrayType);
            code.NewInstruction(0x59 /* dup */, null, null);
            code.StackMap.PushStack(AtomicReferenceArrayType);
            code.NewInstruction(0x12 /* ldc */, null, numCastableInterfaces);
            code.StackMap.PushStack(JavaType.IntegerType);
            code.NewInstruction(0xB7 /* invokespecial */, AtomicReferenceArrayType,
                                new JavaMethodRef("<init>", JavaType.VoidType, JavaType.IntegerType));
            code.NewInstruction(0xB5 /* putfield */, toType, InterfaceArrayField);
            code.StackMap.PopStack(CilMain.Where);
            code.StackMap.PopStack(CilMain.Where);
            code.StackMap.PopStack(CilMain.Where);
            code.StackMap.PopStack(CilMain.Where);
        }



        //
        // if any interfaces are marked [RetainName], make sure that
        // all corresponding methods are also marked [RetainName]
        //

        static void CheckRetainNameMethods(List<CilInterfaceMethod> theMethods,
                                           List<CilInterface> theInterfaces,
                                           CilType checkedType)
        {
            List<CilInterface> retainNameInterfaces = null;
            foreach (var myInterface in theInterfaces)
            {
                if (myInterface.InterfaceType.IsRetainName)
                {
                    if (retainNameInterfaces == null)
                        retainNameInterfaces = new List<CilInterface>();
                    retainNameInterfaces.Add(myInterface);
                }
            }

            if (retainNameInterfaces == null)
                return;

            foreach (var myMethod in theMethods)
            {
                // if the method was not declared on the type itself, skip it.

                if (myMethod.Method.DeclType != checkedType)
                    continue;

                // methods may be marked with [RetainName] to avoid shadow renaming
                // (via CilMethod::MethodIsShadowing), as well as to match methods
                // from [RetainName] interfaces.  so if the method is marked so,
                // then we can just skip it.

                if (myMethod.Method.IsRetainName)
                    continue;

                // if the method is an explicit method implementation, then it cannot
                // implement a method from a [RetainName] interface (checked in
                // CilMethod::InsertMethodNamePrefix) and cannot be marked [RetainName]
                // (checked in CilMethod::SetMethodType).

                if (myMethod.Method.IsExplicitImpl)
                    continue;

                // if the method is not marked [RetainName], then make sure it is not
                // overriding an interface method marked [RetainName]

                var (foundInterface, foundMethod) =
                                        FindMethod(retainNameInterfaces, myMethod);

                if (foundMethod != null)
                {
                    var interfaceName = foundInterface.InterfaceType.JavaName;

                    throw CilMain.Where.Exception(
                        $"method '{myMethod}' (for interface '{interfaceName}') "
                      + $"should be decorated with [java.attr.RetainName]");
                }
            }

            (CilInterface, CilInterfaceMethod) FindMethod(List<CilInterface> haystack,
                                                          CilInterfaceMethod needle)
            {
                foreach (var ifc in haystack)
                {
                    var mth = CilInterfaceMethod.FindMethod(ifc.Methods, needle);
                    if (mth != null)
                        return (ifc, mth);
                }
                return (null, null);
            }
        }



        public static JavaMethod BuildPlainProxy(CilInterfaceMethod ifcMethod, CilType intoType,
                                                 List<CilInterfaceMethod> classMethods)
        {
            CilMethod targetMethod = null;

            foreach (var clsMethod in classMethods)
            {
                if (clsMethod.Method.IsExplicitImpl)
                {
                    // no need for a proxy if we already have an override method,
                    // which has the same name as the proxy:  interface$method
                    if (ifcMethod.Method.Name == clsMethod.Method.Name)
                        return null;
                }
                else if (ifcMethod.PlainCompare(clsMethod))
                {
                    // more than one method may match, if a derived type overrides
                    // or hides a method that also exists in a base type.  but the
                    // derived (primary) type methods always come first.
                    if (targetMethod == null)
                        targetMethod = clsMethod.Method;
                }
            }

            if (targetMethod == null)
            {
                throw CilMain.Where.Exception(
                    $"missing method '{ifcMethod.Method}' "
                  + $"(for interface '{ifcMethod.Method.DeclType}')");
            }

            if (targetMethod.IsRetainName)
            {
                // method retains is name, so it doesn't require a proxy bridge
                return null;
            }

            //
            // create proxy method
            //

            var newMethod = new JavaMethod(null, targetMethod);
            newMethod.Name = ifcMethod.Method.Name;
            newMethod.Flags = JavaAccessFlags.ACC_PUBLIC | JavaAccessFlags.ACC_BRIDGE;

            var code = newMethod.Code = new JavaCode();
            code.Method = newMethod;
            code.Instructions = new List<JavaCode.Instruction>();

            //
            // push 'this' and all other parameters
            //

            code.NewInstruction(0x19 /* aload */, null, (int) 0);
            int numArgs = newMethod.Parameters.Count;
            int index = 1;
            for (int i = 0; i < numArgs; i++)
            {
                var arg = targetMethod.Parameters[i].Type;
                code.NewInstruction(arg.LoadOpcode, null, (int) index);
                index += arg.Category;
            }

            //
            // invoke proxy target method and return
            //

            code.NewInstruction(0xB6 /* invokevirtual */, intoType, targetMethod);
            code.NewInstruction(targetMethod.ReturnType.ReturnOpcode, null, null);

            code.MaxLocals = code.MaxStack = index;

            return newMethod;
        }



        public static void BuildGenericProxy(CilInterfaceMethod ifcMethod, /*string methodSuffix,*/
                                             CilType intoType, List<CilInterfaceMethod> classMethods,
                                             JavaClass ifcClass)
        {
            CilMethod targetMethod = null;

            //Console.WriteLine("\n***** LOOKING FOR INTERFACE METHOD " + ifcMethod + " (SUFFIX " + methodSuffix + ") AKA " + ifcMethod.Method);

            foreach (var clsMethod in classMethods)
            {
                /*Console.WriteLine("> LOOKING AT "
                        + (clsMethod.Method.IsExplicitImpl ? "EXPLICIT " : "")
                        + "CLASS METHOD " + clsMethod + " AKA " + clsMethod.Method);*/

                if (ifcMethod.GenericCompare(clsMethod))
                {
                    if (clsMethod.Method.IsExplicitImpl)
                    {
                        // a matching explicit override method is the best match,
                        // and we can immediately stop searching
                        targetMethod = clsMethod.Method;
                        break;
                    }
                    else
                    {
                        // more than one method may match, if a derived type overrides
                        // or hides a method that also exists in a base type.  but the
                        // derived (primary) type methods always come first
                        if (targetMethod == null)
                            targetMethod = clsMethod.Method;

                        // if a second method matches, and the set of generic types
                        // in its signature exactly matches the interface method we
                        // are looking for, then prefer this method.  when a class
                        // implements same-name methods from multiple interfaces,
                        // this is needed to pick the right method.
                        // see also ResolvedGenericTypes in CilInterfaceMethod.

                        else if (    clsMethod.ResolvedGenericTypes.Length != 0
                                  && clsMethod.ResolvedGenericTypes
                                            == ifcMethod.ResolvedGenericTypes)
                        {
                            targetMethod = clsMethod.Method;
                        }
                    }
                }
            }

            if (targetMethod == null)
            {
                throw CilMain.Where.Exception(
                    $"missing method '{ifcMethod.Method}' "
                  + $"(for interface '{ifcMethod.Method.DeclType}')");
            }

            // Console.WriteLine("INTERFACE METHOD " + ifcMethod.Method + " TARGET " + targetMethod);

            BuildGenericProxy2(ifcMethod, targetMethod, true, intoType, ifcClass);
        }



        public static void BuildGenericProxy2(CilInterfaceMethod ifcMethod, CilMethod targetMethod,
                                              bool parentField, CilType intoType, JavaClass ifcClass)
        {
            //
            // create proxy method
            //

            var targetMethod2 = targetMethod.WithGenericParameters;
            var ifcMethod2 = ifcMethod.Method.WithGenericParameters;

            var newMethod = new JavaMethod(ifcClass, targetMethod2);
            newMethod.Name = ifcMethod2.Name;
            newMethod.Flags = JavaAccessFlags.ACC_PUBLIC | JavaAccessFlags.ACC_BRIDGE;

            var code = newMethod.Code = new JavaCode();
            code.Method = newMethod;
            code.Instructions = new List<JavaCode.Instruction>();

            //
            // push a reference to the parent object
            //

            code.NewInstruction(0x19 /* aload */, null, (int) 0);
            if (parentField)
            {
                code.NewInstruction(0xB4 /* getfield */, new JavaType(0, 0, ifcClass.Name),
                                                new JavaFieldRef(ParentFieldName, intoType));
            }

            //
            // push all other parameters
            //

            int numArgs = newMethod.Parameters.Count;
            int index = 1;
            int maxStack = 1;
            for (int i = 0; i < numArgs; i++)
            {
                var ifcArg = ifcMethod2.Parameters[i].Type;
                code.NewInstruction(ifcArg.LoadOpcode, null, (int) index);
                index += ifcArg.Category;

                var clsArg = (CilType) targetMethod2.Parameters[i].Type;
                if (JavaType.ObjectType.Equals(ifcArg))
                {
                    if (! clsArg.IsReference)
                    {
                        var boxedArg = new BoxedType(clsArg, false);
                        code.NewInstruction(0xC0 /* checkcast */, boxedArg, null);
                        boxedArg.GetValue(code);
                    }
                    else if (! JavaType.ObjectType.Equals(clsArg))
                    {
                        code.NewInstruction(0xC0 /* checkcast */, clsArg, null);
                    }
                    // a parameter in the target method may be a concrete type,
                    // but if it is a generic java.lang.Object in the interface,
                    // then it must be a generic java.lang.Object in the proxy
                    newMethod.Parameters[i] = new JavaFieldRef("", ifcArg);
                }
                maxStack += clsArg.Category;
            }

            //
            // invoke proxy target method
            //

            code.NewInstruction(0xB6 /* invokevirtual */, intoType, targetMethod2);

            //
            // return value from method
            //

            var clsRet = (CilType) targetMethod2.ReturnType;
            var ifcRet = ifcMethod2.ReturnType;
            if (JavaType.ObjectType.Equals(ifcRet))
            {
                if (! clsRet.IsReference)
                {
                    var boxedArg = new BoxedType(clsRet, false);
                    boxedArg.BoxValue(code);
                }
                // the return value in the target method may be a concrete type,
                // but if it is a generic java.lang.Object in the interface,
                // then it must also be a generic java.lang.Object in the proxy
                newMethod.ReturnType = ifcRet;
                code.NewInstruction(ifcRet.ReturnOpcode, null, null);
            }
            else
            {
                code.NewInstruction(clsRet.ReturnOpcode, null, null);
            }

            code.MaxLocals = index;
            code.MaxStack = maxStack;

            ifcClass.Methods.Add(newMethod);
        }



        public static void BuildOverloadProxy(TypeDefinition fromType, MethodDefinition fromMethod,
                                              CilMethod targetMethod, JavaClass intoClass)
        {
            // create a proxy bridge to forward invocations of a virtual method from
            // the base class, to an implementation in a derived class.  this is needed
            // where the base virtual method has generic parameters or return type,
            // for example:  virtual T SomeMethod(T arg)   will be translated as:
            //      java.lang.Object SomeMethod(-generic-$)(java.lang.Object arg)
            // and in a derived class that specializes T as String:
            //      java.lang.String SomeMethod(java.lang.String arg)
            // so the derived class must also include a proxy bridge method with the
            // same name as in the base class, and forward the call.

            //Console.WriteLine($"CHECK OVERRIDE {fromMethod}");
            int targetMethodCount = targetMethod.Parameters.Count;

            for (;;)
            {
                var baseType = fromType.BaseType;
                if (baseType == null)
                    break;
                fromType = CilType.AsDefinition(baseType);
                if (! baseType.IsGenericInstance)
                    continue;

                foreach (var fromMethod2 in fromType.Methods)
                {
                    //Console.WriteLine($"\tCOMPARE {fromMethod2} {fromMethod2.IsVirtual} {fromMethod2.Name == fromMethod.Name} {targetMethodCount == fromMethod2.Parameters.Count}");
                    if (    fromMethod2.IsVirtual && fromMethod2.Name == fromMethod.Name
                         && targetMethodCount == fromMethod2.Parameters.Count)
                    {
                        if (CilMethod.CompareMethods(fromMethod, fromMethod2))
                        {
                            //Console.WriteLine(">>>>>>>>>>>>> WARNING SAME: " + fromMethod.ToString() + " AND " + fromType);
                            continue;
                        }

                        var genericMark = CilMain.GenericStack.Mark();
                        CilMain.GenericStack.EnterType(baseType);
                        var baseMethod = new CilInterfaceMethod(
                                                CilMain.GenericStack.EnterMethod(fromMethod2));
                        CilMain.GenericStack.Release(genericMark);

                        //Console.WriteLine($"\twould compare with {fromMethod2} in class {baseType}");
                        //Console.WriteLine($"\t\t{baseMethod}");

                        if (targetMethodCount != baseMethod.Parameters.Count)
                            continue;
                        if (! IsGenericOrEqual(targetMethod.ReturnType, baseMethod.ReturnType))
                            continue;
                        bool sameParameters = true;
                        for (int i = 0; i < targetMethodCount; i++)
                        {
                            if (! IsGenericOrEqual(targetMethod.Parameters[i].Type,
                                                   baseMethod.Parameters[i]))
                            {
                                bool equalsAfterUnboxing = (
                                            targetMethod.Parameters[i].Type is BoxedType boxedType
                                         && boxedType.UnboxedType.Equals(baseMethod.Parameters[i]));

                                if (! equalsAfterUnboxing)
                                {
                                    //Console.WriteLine($"\tMISMATCH {targetMethod.Parameters[i].Type} vs {baseMethod.Parameters[i]}/{baseMethod.Parameters[i].IsGenericParameter}");
                                    sameParameters = false;
                                    break;
                                }
                            }
                        }
                        if (sameParameters)
                        {
                            if (baseMethod.Method.WithGenericParameters.ToString()
                                    != targetMethod.WithGenericParameters.ToString())
                            {
                                // the proxy method may have the same signature as the
                                // target method.  for example in a generic class that
                                // inherits from a generic class:
                                //     class A<T> { virtual T Method(T arg); }
                                // and class B<T> : A<T> { override T Method(T arg); }
                                // in such a case, we should not generate a specialized
                                // proxy for the generic method in class B.
                                //

                                //Console.WriteLine($"proxying {targetMethod} in class {targetMethod.DeclType}");
                                BuildGenericProxy2(baseMethod, targetMethod,
                                                   false, targetMethod.DeclType, intoClass);
                            }
                            return;
                        }
                    }
                }
            }

            static bool IsGenericOrEqual(JavaType pThis, CilType pBase)
                => pBase.IsGenericParameter || pThis.Equals(pBase);
        }



        //
        // implement the method IGenericObject.TryCast(Type)
        //

        public static void BuildTryCastMethod(List<CilInterface> allInterfaces, CilType intoType,
                                              int numCastableInterfaces, JavaClass intoClass)
        {
            var code = CilMain.CreateHelperMethod(intoClass,
                            new JavaMethodRef("system-IGenericObject-TryCast",
                                    JavaType.ObjectType, CilType.SystemTypeType),
                            3, 0);

            code.StackMap = new JavaStackMap();
            code.StackMap.SetLocal(0, intoType);
            code.StackMap.SetLocal(1, CilType.SystemTypeType);
            code.StackMap.SaveFrame(0, false, CilMain.Where);

            if (intoType.HasGenericParameters)
            {
                // if this type is a generic type, compare it to the input.
                // see also:  built GenericUtil::BuildGetTypeMethod()

                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                code.NewInstruction(0xB4 /* getfield */,
                                    intoType, GenericUtil.ConcreteTypeField);
                code.NewInstruction(0x19 /* aload */, null, (int) 1);
                code.NewInstruction(0xA5 /* if_acmpeq */, null, (ushort) 0xFFF1);
                // branch to label 0xFFF1 to return 'this' reference

                if (code.MaxStack < 2)
                    code.MaxStack = 2;
            }

            if (intoType.SuperTypes[0].IsGenericThisOrSuper)
            {
                // if the base type has IGenericObject::TryCast, call that.

                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                code.NewInstruction(0x19 /* aload */, null, (int) 1);
                code.NewInstruction(0xB7 /* invokespecial */,
                                    intoType.SuperTypes[0], code.Method);
                code.NewInstruction(0x59 /* dup */, null, null);
                code.NewInstruction(0xC7 /* ifnonnull */, null, (ushort) 0xFFF2);
                // branch to label 0xFFF2 to return result from super TryCast
                code.NewInstruction(0x57 /* pop */, null, null);

                if (code.MaxStack < 2)
                    code.MaxStack = 2;
            }

            if (numCastableInterfaces != 0)
            {
                // compare the input type to any implemented generic interface,
                // unless that interface is also implemented by a super type.

                int ifcNumber = 0;
                foreach (var ifc in allInterfaces)
                {
                    if (ifc.GenericTypes != null && (! ifc.SuperImplements))
                    {
                        // this is a generic interface, not implemented by a super.

                        if (ifcNumber == 0)
                        {
                            // before processing the first such interface, make an
                            // easy to access reference to the array of interface
                            // types (see also CreateInterfaceArrayField).

                            code.NewInstruction(0x19 /* aload */, null, (int) 0);
                            code.NewInstruction(0xB4 /* getfield */,
                                                intoType, InterfaceArrayField);
                            code.NewInstruction(0x3A /* astore */, null, (int) 2);
                        }

                        // set up parameters to call the GenericType::TryCast helper

                        code.NewInstruction(0x19 /* aload */, null, (int) 0);
                        code.NewInstruction(0x19 /* aload */, null, (int) 1);
                        code.NewInstruction(0x19 /* aload */, null, (int) 2);
                        code.NewInstruction(0x12 /* ldc */, null, (int) ifcNumber++);

                        var proxyClassName = intoType.JavaName
                                           + "$$generic" + ifcNumber.ToString();
                        code.NewInstruction(0x12 /* ldc */,
                                            new JavaType(0, 0, proxyClassName), null);

                        foreach (var inst in ifc.LoadTypeCode.Instructions)
                            code.Instructions.Add(inst);

                        code.NewInstruction(0xB8 /* invokestatic */,
                                            GenericUtil.SystemGenericType, TryCastHelperMethod);

                        code.NewInstruction(0x59 /* dup */, null, null);
                        code.NewInstruction(0xC7 /* ifnonnull */, null, (ushort) 0xFFF2);
                        code.NewInstruction(0x57 /* pop */, null, null);
                        // branch to label 0xFFF2 to return proxy object reference

                        int minStack = ifc.LoadTypeCode.MaxStack + 5;
                        if (code.MaxStack < minStack)
                            code.MaxStack = minStack;
                    }
                }
            }

            // exit points.  if control reaches this point, we return null,  because
            // no matching class or interface type was found.  otherwise if there was
            // a branch to label 0xFFF1, then we need to return the 'this' reference.
            // and finally, a branch to label 0xFFF2, with a reference on the stack.

            // label 0xFFF0 - return null
            code.StackMap.SaveFrame(0xFFF0, true, CilMain.Where);
            code.NewInstruction(0x01 /* aconst_null */, null, null, 0xFFF0);
            code.NewInstruction(JavaType.ObjectType.ReturnOpcode, null, null);

            // label 0xFFF1 - return 'this' reference
            code.StackMap.SaveFrame(0xFFF1, true, CilMain.Where);
            code.NewInstruction(0x19 /* aload */, null, (int) 0, 0xFFF1);

            // label 0xFFF2 - return reference on the stack
            code.StackMap.PushStack(JavaType.ObjectType);
            code.StackMap.SaveFrame(0xFFF2, true, CilMain.Where);
            code.NewInstruction(JavaType.ObjectType.ReturnOpcode, null, null, 0xFFF2);
        }



        //
        // static variables
        //



        internal static readonly JavaType AtomicReferenceArrayType =
                            new JavaType(0, 0, "java.util.concurrent.atomic.AtomicReferenceArray");

        internal static readonly JavaFieldRef InterfaceArrayField =
                                new JavaFieldRef("-generic-interfaces", AtomicReferenceArrayType);

        internal static readonly string ParentFieldName = "-generic-parent";

        internal static readonly JavaMethodRef TryCastHelperMethod;



        static InterfaceBuilder()
        {
            var parameters = new List<JavaFieldRef>(6);
            parameters.Add(new JavaFieldRef("", JavaType.ObjectType));
            parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));
            parameters.Add(new JavaFieldRef("", AtomicReferenceArrayType));
            parameters.Add(new JavaFieldRef("", JavaType.IntegerType));
            parameters.Add(new JavaFieldRef("", JavaType.ClassType));
            parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));
            TryCastHelperMethod = new JavaMethodRef("TryCast", JavaType.ObjectType, parameters);
        }

    }

}
