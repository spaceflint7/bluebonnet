
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;
using Instruction = SpaceFlint.JavaBinary.JavaCode.Instruction;

namespace SpaceFlint.CilToJava
{

    public static class Delegate
    {

        public static JavaClass FixClass(JavaClass fromClass, CilType fromType)
        {
            JavaType interfaceType = InterfaceType(fromType);
            JavaClass interfaceClass = null;

            foreach (var method in fromClass.Methods)
            {
                if ((method.Flags & JavaAccessFlags.ACC_STATIC) == 0 && method.Code == null)
                {
                    if (method.Name == "<init>")
                    {
                        GenerateConstructor(method, fromType);
                    }
                    else if (method.Name == "Invoke")
                    {
                        if (interfaceClass != null)
                            break;

                        interfaceClass = MakeInterface(fromClass, method, interfaceType.ClassName);
                        GenerateInvoke(method, fromType, interfaceType);
                    }
                    else if (method.Name == "BeginInvoke")
                    {
                        if (interfaceClass == null)
                            break;

                        GenerateBeginInvoke();
                        continue;
                    }
                    else if (method.Name == "EndInvoke")
                    {
                        GenerateEndInvoke();
                        continue;
                    }
                    else
                        break;

                    method.Flags &= ~JavaAccessFlags.ACC_ABSTRACT;
                }
            }

            if (interfaceClass == null)
                throw CilMain.Where.Exception("invalid delegate class");

            return interfaceClass;

            //
            //
            //

            JavaClass MakeInterface(JavaClass fromClass, JavaMethod fromMethod, string newName)
            {
                var newClass = CilMain.CreateInnerClass(fromClass, newName,
                                        JavaAccessFlags.ACC_PUBLIC
                                      | JavaAccessFlags.ACC_ABSTRACT
                                      | JavaAccessFlags.ACC_INTERFACE);

                var newMethod = new JavaMethod(newClass, fromMethod);
                newMethod.Flags = JavaAccessFlags.ACC_PUBLIC
                                | JavaAccessFlags.ACC_ABSTRACT;
                if (IsPrimitive(newMethod.ReturnType))
                {
                    // primitive return value is translated to java.lang.Object,
                    // because the delegate target may return a generic type that
                    // is specialized for the primitive type in the delegate.
                    // see also GenerateInvoke and LoadFunction
                    newMethod.ReturnType = JavaType.ObjectType;
                }
                newClass.Methods.Add(newMethod);

                return newClass;
            }

            //
            //
            //

            void GenerateConstructor(JavaMethod method, CilType dlgType)
            {
                var code = method.Code = new JavaCode();
                code.Method = method;
                code.Instructions = new List<Instruction>();

                if (dlgType.HasGenericParameters)
                {
                    code.StackMap = new JavaStackMap();
                    var genericMark = CilMain.GenericStack.Mark();
                    CilMain.GenericStack.EnterMethod(dlgType, method, true);

                    // initialize the generic type field
                    GenericUtil.InitializeTypeField(dlgType, code);

                    CilMain.GenericStack.Release(genericMark);
                    code.MaxStack = code.StackMap.GetMaxStackSize(CilMain.Where);
                }

                code.NewInstruction(0x19 /* aload_0 */, null, (int) 0);
                code.NewInstruction(0x19 /* aload_1 */, null, (int) 1);
                code.NewInstruction(0x19 /* aload_2 */, null, (int) 2);

                code.NewInstruction(0xB7 /* invokespecial */,
                                    new JavaType(0, 0, method.Class.Super),
                                    new JavaMethodRef("<init>", JavaType.VoidType,
                                                      JavaType.ObjectType, JavaType.ObjectType));

                code.NewInstruction(0xB1 /* return (void) */, null, null);

                if (code.MaxStack < 3)
                    code.MaxStack = 3;
                code.MaxLocals = 3 + dlgType.GenericParametersCount;
            }

            //
            //
            //

            void GenerateInvoke(JavaMethod method, CilType dlgType, JavaType ifcType)
            {
                var delegateType = new JavaType(0, 0, "system.Delegate");

                var code = method.Code = new JavaCode();
                code.Method = method;
                code.Instructions = new List<Instruction>();
                code.StackMap = new JavaStackMap();

                code.StackMap.SetLocal(0, dlgType);
                int index = 1;
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var paramType = (CilType) method.Parameters[i].Type;
                    code.StackMap.SetLocal(index, paramType);
                    index += paramType.Category;
                }
                code.StackMap.SaveFrame((ushort) 0, false, CilMain.Where);

                //
                // step 1, call the target method through the interface reference
                // stored in the 'invokable' field of the system.Delegate class
                //

                code.NewInstruction(0x19 /* aload_0 */, null, (int) 0);
                code.NewInstruction(0xB4 /* getfield */, delegateType,
                                    new JavaFieldRef("invokable", JavaType.ObjectType));

                code.NewInstruction(0xC0 /* checkcast */, ifcType, null);
                code.StackMap.PushStack(JavaType.ObjectType);

                // push method parameters 'invokeinterface'

                index = 1;
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var paramType = (CilType) method.Parameters[i].Type;
                    code.NewInstruction(paramType.LoadOpcode, null, index);
                    code.StackMap.PushStack(paramType);

                    if (paramType.IsGenericParameter && (! paramType.IsByReference))
                    {
                        // invoke the helper method which identifies our boxed
                        // primitives and re-boxes them as java boxed values.
                        // see also: GenericType::DelegateParameterin baselib.
                        GenericUtil.LoadMaybeGeneric(
                                        paramType.GetMethodGenericParameter(), code);
                        code.NewInstruction(0xB8 /* invokestatic */,
                                            SystemDelegateUtilType, DelegateParameterMethod);
                        code.StackMap.PopStack(CilMain.Where);
                    }
                    index += paramType.Category;
                }

                var returnType = (CilType) method.ReturnType;
                if (IsPrimitive(returnType))
                {
                    // primitive return value is translated to java.lang.Object,
                    // because the delegate target may return a generic type that
                    // is specialized for the primitive type in the delegate.
                    // see also GenerateInvoke and LoadFunction
                    var adjustedMethod = new JavaMethodRef(
                            method.Name, JavaType.ObjectType, method.Parameters);
                    code.NewInstruction(0xB9 /* invokeinterface */, ifcType, adjustedMethod);
                }
                else
                    code.NewInstruction(0xB9 /* invokeinterface */, ifcType, method);

                code.StackMap.ClearStack();
                code.StackMap.PushStack(returnType);

                // check if this delegate has a 'following' delegate,
                // attached via system.MulticastDelegate.CombineImpl

                code.NewInstruction(0x19 /* aload_0 */, null, (int) 0);
                code.NewInstruction(0xB4 /* getfield */, delegateType,
                                    new JavaFieldRef("following", delegateType));
                code.NewInstruction(0xC7 /* ifnonnull */, null, (ushort) 1);

                if (returnType.IsGenericParameter && (! returnType.IsByReference))
                {
                    GenericUtil.LoadMaybeGeneric(
                                        returnType.GetMethodGenericParameter(), code);
                    code.NewInstruction(0xB8 /* invokestatic */,
                                        SystemDelegateUtilType, DelegateReturnValueMethod);
                    code.StackMap.PopStack(CilMain.Where);
                }
                else if (IsPrimitive(returnType))
                {
                    // if the delegate returns a primitive type, we need to unbox
                    // the object returned by the method we just called.  it will be
                    // a java boxed primitive (e.g. java.lang.Integer) if the called
                    // method actually returns a primitive, due to the boxing done
                    // by the invokedynamic mechanism.  if the called method returns
                    // a generic type, it will be our boxed type, e.g. system.Int32.
                    //
                    // see also DelegateReturnValueX helper methods in baselib,
                    // and MakeInterface and LoadFunction in this file.
                    var helperMethod = new JavaMethodRef(
                        DelegateReturnValueMethod.Name + returnType.ToDescriptor(),
                        returnType, JavaType.ObjectType);
                    code.NewInstruction(0xB8 /* invokestatic */,
                                        SystemDelegateUtilType, helperMethod);
                }

                code.NewInstruction(returnType.ReturnOpcode, null, index);
                code.StackMap.PopStack(CilMain.Where);

                //
                // step 2
                //
                // if this delegate has a 'following' delegate, then we need to
                // call it, but first get rid of the return value on the stack
                //

                byte popOpcode;
                if (returnType.Equals(JavaType.VoidType))
                    popOpcode = 0x00; // nop
                else // select 0x57 pop, or 0x58 pop2
                {
                    var adjustedReturnType =
                            IsPrimitive(returnType) ? JavaType.ObjectType : returnType;
                    code.StackMap.PushStack(adjustedReturnType);
                    popOpcode = (byte) (0x56 + returnType.Category);
                }
                code.NewInstruction(popOpcode, null, null, (ushort) 1);
                code.StackMap.SaveFrame((ushort) 1, true, CilMain.Where);

                // now call the Invoke method on the 'following' delegate

                code.NewInstruction(0x19 /* aload_0 */, null, (int) 0);
                code.NewInstruction(0xB4 /* getfield */, delegateType,
                                    new JavaFieldRef("following", delegateType));
                code.NewInstruction(0xC0 /* checkcast */, dlgType, null);

                // push all method parameters for 'invokevirtual'

                index = 1;
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var paramType = (CilType) method.Parameters[i].Type;
                    code.NewInstruction(paramType.LoadOpcode, null, index);

                    if (paramType.IsGenericParameter && (! paramType.IsByReference))
                    {
                        // invoke the helper method which identifies our boxed
                        // primitives and re-boxes them as java boxed values.
                        // see also: GenericType::DelegateParameterin baselib.
                        GenericUtil.LoadMaybeGeneric(
                                        paramType.GetMethodGenericParameter(), code);
                        code.NewInstruction(0xB8 /* invokestatic */,
                                            SystemDelegateUtilType, DelegateParameterMethod);
                    }
                    index += paramType.Category;
                }

                code.NewInstruction(0xB6 /* invokevirtual */, dlgType, method);

                code.NewInstruction(returnType.ReturnOpcode, null, index);
                code.StackMap.ClearStack();

                code.MaxStack = code.StackMap.GetMaxStackSize(CilMain.Where);
                code.MaxLocals = index;
            }

            //
            //
            //

            void GenerateBeginInvoke()
            {
            }

            //
            //
            //

            void GenerateEndInvoke()
            {
            }
        }



        public static void FixConstructor(List<JavaFieldRef> parameters, CilType dlgType)
        {
            //
            // the constructor of a delegate class is declared with a
            // second parameter of type native integer, so convert to
            // the interface type for the delegate
            //

            if (parameters.Count >= 2 && parameters[0].Type.Equals(JavaType.ObjectType))
            {
                var p1Type = parameters[1].Type;
                if (! p1Type.IsReference)
                {
                    if (    p1Type.PrimitiveType == TypeCode.Int64
                         || p1Type.PrimitiveType == TypeCode.UInt64)
                    {
                        parameters[1].Type = InterfaceType(dlgType);
                    }
                }
            }
        }



        public static void LoadFunction(JavaCode code, Mono.Cecil.Cil.Instruction cilInst)
        {
            if (cilInst.Operand is MethodReference implMethodRef)
            {
                var implMethod = CilMethod.From(implMethodRef);

                var (declType, declMethod, dlgMethodName) = FindInterfaceType(cilInst);

                if (dlgMethodName != null)
                {
                    // if this is an artificial delegate for a functional interface,
                    // then we can't actually instantiate this particular delegate,
                    // because its definition only exists in the DLL created by
                    // DotNetImporter;  see BuildDelegate there.  we fix the Newobj
                    // instruction to instantiate system.FunctionalInterfaceDelegate.

                    cilInst.Next.Operand = DelegateConstructor(cilInst.Next.Operand);

                    declMethod = new JavaMethodRef(dlgMethodName,
                                                   implMethod.ReturnType,
                                                   implMethod.Parameters);
                }

                if (IsPrimitive(declMethod.ReturnType))
                {
                    // primitive return value is translated to java.lang.Object,
                    // because the delegate target may return a generic type that
                    // is specialized for the primitive type in the delegate.
                    // see also MakeInterface and GenerateInvoke
                    declMethod = new JavaMethodRef(
                        declMethod.Name, JavaType.ObjectType, declMethod.Parameters);
                }

                // select the method handle kind for the dynamic call site.
                // for a non-static invocation, we need to push an object reference.

                JavaMethodHandle.HandleKind callKind;
                if (implMethod.DeclType.IsInterface)
                    callKind = JavaMethodHandle.HandleKind.InvokeInterface;
                else
                    callKind = JavaMethodHandle.HandleKind.InvokeVirtual;

                if (cilInst.OpCode.Code == Code.Ldftn)
                {
                    if (implMethod.IsStatic)
                        callKind = JavaMethodHandle.HandleKind.InvokeStatic;

                    else
                    {
                        var implMethodDef = CilMethod.AsDefinition(implMethodRef);
                        bool isVirtual = implMethodDef.IsVirtual;
                        if (isVirtual && implMethodDef.DeclaringType.IsSealed)
                            isVirtual = false;

                        if (! isVirtual)
                        {
                            // non-virtual instance method
                            code.NewInstruction(0x59 /* dup */, null, null);
                            code.StackMap.PushStack(JavaType.ObjectType);
                        }
                        else
                        {
                            // virtual method should only be specified in 'ldvirtftn'
                            throw new Exception("virtual method referenced");
                        }
                    }
                }

                //
                // the method may take generic type parameters, i.e. the System.Type
                // parameters that are added at the end of the parameter list, by
                // CilMethod::ImportGenericParameters.  but when the delegate is
                // called, these parameters will not be pushed.  therefore we do
                // the following when assigning such a method to a delegate:
                //
                // (1) we select a different implementing method, i.e. the bridge
                // method generated by CreateCapturingBridgeMethod (see below),
                // which moves the type arguments to the head of the parameter list.
                // (2) we generate and push the type arguments at this time, via
                // calls to GenericUtil.LoadGeneric.
                // (3) we specify the type arguments as capture parameters of the
                // call site.  (see also JavaCallSite.)  this means the generated
                // proxy will inject these parameters when it calls the bridge
                // method from step (1), and that bridge method will push these
                // parameters at the end of the parameter list, when it invokes
                // the actual target method.
                //

                List<JavaFieldRef> TypeArgs = null;
                JavaMethodRef implMethod2 = implMethod;

                var implGenericMethod = implMethod.WithGenericParameters;
                if (implGenericMethod != implMethod)
                {
                    implMethod2 = new JavaMethodRef(
                        "delegate-bridge-" + implGenericMethod.Name,
                        implMethod.ReturnType, implMethod.Parameters);

                    var count1 = implMethod.Parameters.Count;
                    var count2 = implGenericMethod.Parameters.Count;
                    TypeArgs = implGenericMethod.Parameters.GetRange(count1, count2 - count1);

                    var callMethod = CilMain.GenericStack.EnterMethod(implMethodRef);
                    for (int i = count1; i < count2; i++)
                        GenericUtil.LoadGeneric(implGenericMethod.Parameters[i].Name, code);
                    for (int i = count1; i < count2; i++)
                        code.StackMap.PopStack(CilMain.Where);
                }

                // create a CallSite that implements the method signature
                // declMethod, in the functional interface declType, in order
                // to proxy-invoke the method implMethod.DeclType::implMethod.

                var callSite = new JavaCallSite(declType, declMethod,
                                                implMethod.DeclType, implMethod2,
                                                TypeArgs, callKind);

                code.NewInstruction(0xBA /* invokedynamic */, null, callSite);

                if (callKind != JavaMethodHandle.HandleKind.InvokeStatic)
                    code.StackMap.PopStack(CilMain.Where);

                code.StackMap.PushStack(CilType.From(declType));
            }
            else
                throw new InvalidProgramException();

            //
            // ldftn or ldvirtftn should be followed by newobj, which we can use
            // to identify the delegate, and therefore, the interface
            //

            (JavaType, JavaMethodRef, string) FindInterfaceType(
                                                        Mono.Cecil.Cil.Instruction cilInst)
            {
                cilInst = cilInst.Next;
                if (    cilInst != null && cilInst.OpCode.Code == Code.Newobj
                     && cilInst.Operand is MethodReference constructorRef)
                {
                    var dlgType = CilType.From(constructorRef.DeclaringType);
                    if (dlgType.IsDelegate)
                    {
                        //
                        // check for an artificial delegate, generated to represent a
                        // java functional interface: (BuildDelegate in DotNetImporter)
                        //
                        // delegate type is marked [java.lang.attr.AsInterface],
                        // and is child of an interface type.
                        // interface type is marked [java.lang.attr.RetainName],
                        // and has one method.
                        //

                        var dlgType0 = CilType.AsDefinition(constructorRef.DeclaringType);
                        if (dlgType0.HasCustomAttribute("AsInterface"))
                        {
                            var ifcType0 = dlgType0.DeclaringType;
                            var ifcType = CilType.From(ifcType0);
                            if (    ifcType.IsInterface && ifcType.IsRetainName
                                 && ifcType0.HasMethods && ifcType0.Methods.Count == 1)
                            {
                                return (ifcType, null, ifcType0.Methods[0].Name);
                            }
                        }

                        //
                        // otherwise, a normal delegate, which may be generic, or plain.
                        // interface name is DelegateType$interface.
                        // we look for a method named Invoke.
                        //

                        foreach (var method in dlgType0.Methods)
                        {
                            if (method.Name == "Invoke")
                            {
                                return (InterfaceType(dlgType), CilMethod.From(method), null);
                            }
                        }
                    }
                }
                throw new InvalidProgramException();
            }

            //
            // create a method reference to delegate constructor from baselib:
            // system.MulticastDelegate::.ctor(object, object)
            //

            CilMethod DelegateConstructor(object Operand)
            {
                var baseType = CilType.AsDefinition(((MethodReference) Operand)
                                       .DeclaringType).BaseType;
                if (baseType.Namespace != "System" || baseType.Name != "MulticastDelegate")
                {
                    throw CilMain.Where.Exception(
                                $"delegate base type is '{baseType.Name}', "
                               + "but expected 'System.MulticastDelegate'");
                }

                return CilMethod.CreateDelegateConstructor();
            }
        }



        //
        // a method that takes one or more generic parameters, e.g. T Method<T>(T arg1)
        // will take one or more parameters of type System.Type, and these will be the
        // last parameters.  such a method cannot be directly assigned to a non-generic
        // delegate, such as "int Delegate(int)", because the delegate caller will not
        // pass the System.Type parameters.  however, java.lang.invoke.LambdaMetafactory
        // lets us pass "capture variables" into the generated proxy, which will be
        // passed as the first parameters to the implementing methods.  (in the
        // LambdaMetafactory documentation, these are the first 'K' parameters of
        // 'invokedType'.)  to take advantage of this, we just need a bridge method,
        // which takes the System.Type parameters as the first parameters, and then
        // calls the actual implementing method.  this function creates such a bridge.
        //

        public static JavaMethod CreateCapturingBridgeMethod(JavaMethod targetMethod,
                                                             List<JavaFieldRef> realArgs,
                                                             bool isInterface)
        {
            JavaMethod bridgeMethod = new JavaMethod(targetMethod.Class, targetMethod);
            bridgeMethod.Name = "delegate-bridge-" + bridgeMethod.Name;
            bridgeMethod.Flags = targetMethod.Flags & (~JavaAccessFlags.ACC_ABSTRACT);

            var code = bridgeMethod.Code = new JavaCode();
            code.Method = bridgeMethod;
            code.Instructions = new List<JavaCode.Instruction>();

            int numRealArgs = realArgs.Count;
            var typeArgs = targetMethod.Parameters;
            int numTypeArgs = typeArgs.Count - numRealArgs;

            int firstIndex;
            byte opcode;
            if ((targetMethod.Flags & JavaAccessFlags.ACC_STATIC) != 0)
            {
                firstIndex = 0;
                opcode = 0xB8; // invokestatic
            }
            else
            {
                // push 'this' argument
                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                firstIndex = 1;
                if (isInterface)
                    opcode = 0xB9; // invokeinterface
                if ((targetMethod.Flags & JavaAccessFlags.ACC_FINAL) != 0)
                {
                    // see note about Android 'D8' in CodeCall::Translate_Call()
                    opcode = 0xB7; // invokespecial
                }
                else
                    opcode = 0xB6; // invokevirtual
            }

            //
            // push all other non-Type parameters,
            // followed by all System.Type parameters
            //

            int stack = firstIndex;
            int index = firstIndex + numTypeArgs;
            for (int i = 0; i < numRealArgs; i++)
            {
                var arg = realArgs[i].Type;
                code.NewInstruction(arg.LoadOpcode, null, (int) index);
                index += arg.Category;
                stack += arg.Category;
            }

            index = firstIndex;
            for (int i = 0; i < numTypeArgs; i++)
            {
                var arg = typeArgs[numRealArgs + i];
                bridgeMethod.Parameters.RemoveAt(numRealArgs + i);
                bridgeMethod.Parameters.Insert(i, arg);

                code.NewInstruction(arg.Type.LoadOpcode, null, (int) index);
                index += arg.Type.Category;
                stack += arg.Type.Category;
            }

            //
            // invoke proxy target method and return
            //

            code.NewInstruction(opcode, new JavaType(0, 0, targetMethod.Class.Name),
                                        targetMethod);
            code.NewInstruction(targetMethod.ReturnType.ReturnOpcode, null, null);

            code.MaxLocals = code.MaxStack = stack;

            return bridgeMethod;
        }



        static string InterfaceName(CilType dlgType) => dlgType.ClassName + "$interface";

        static JavaType InterfaceType(CilType dlgType) =>
            CilType.From(new JavaType(0, 0, InterfaceName(dlgType)));

        static bool IsPrimitive(JavaType type)
            => (type.PrimitiveType != 0 && type.ArrayRank == 0);


        internal static readonly JavaType SystemDelegateUtilType =
                                    new JavaType(0, 0, "system.DelegateUtil");

        internal static readonly JavaMethodRef DelegateParameterMethod =
                        new JavaMethod("DelegateParameter", JavaType.ObjectType,
                                       JavaType.ObjectType, CilType.SystemTypeType);

        internal static readonly JavaMethodRef DelegateReturnValueMethod =
                        new JavaMethod("DelegateReturnValue", JavaType.ObjectType,
                                       JavaType.ObjectType, CilType.SystemTypeType);

    }
}
