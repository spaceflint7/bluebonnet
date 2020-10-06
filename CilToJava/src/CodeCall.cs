
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public partial class CodeBuilder
    {

        void CallMethod(Code op, object data)
        {
            bool ok = false;

            if (data is MethodReference methodRef)
            {
                if (op == Code.Call && CodeSpan.ExplicitCast(methodRef, code))
                    return;

                var callMethod = CilMain.GenericStack.EnterMethod(methodRef);

                if (op == Code.Call)
                    ok = Translate_Call(callMethod);

                else if (! callMethod.IsStatic)
                {
                    if (op == Code.Newobj)
                        ok = Translate_Newobj(callMethod);

                    else if (op == Code.Callvirt)
                    {
                        if (callMethod.IsConstructor)
                            ok = Translate_Call(callMethod);

                        else if (cilInst.Previous?.OpCode.Code == Code.Constrained)
                            ok = Translate_Constrained(callMethod, cilInst.Previous.Operand);
                        else
                            ok = Translate_Callvirt(callMethod);
                    }
                }
            }

            else if (op == Code.Ret)
                ok = Translate_Return();

            else if (data is CilMethod && op == Code.Newobj)
            {
                // special case from LoadFunction in Delegate module
                ok = Translate_Newobj((CilMethod) data);
            }

            if (! ok)
                throw new InvalidProgramException();
        }



        bool Translate_Newobj(CilMethod callMethod)
        {
            if (callMethod.IsStatic)
                return false;

            var newClass = callMethod.DeclType;

            if (callMethod.IsArrayMethod)
            {
                arrays.New(newClass, callMethod.Parameters.Count);
                return true;
            }

            if (newClass.IsValueClass)
            {
                if (! callMethod.IsValueInit)
                    return false;
            }
            else
            {
                if (! callMethod.IsConstructor)
                    return false;

                if (    newClass.Equals(JavaType.StringType)
                     && code.Method.Class.Name != newClass.JavaName)
                {
                    // redirect the System.String constructor to system.String.New
                    var newMethod = new JavaMethodRef("New", newClass);
                    newMethod.Parameters = callMethod.Parameters;

                    code.NewInstruction(0xB8 /* invokestatic */,
                                        new JavaType(0, 0, newClass.JavaName), newMethod);

                    int n = callMethod.Parameters.Count;
                    while (n-- > 0)
                        stackMap.PopStack(CilMain.Where);
                    stackMap.PushStack(newClass);

                    return true;
                }

                if (newClass.Equals(JavaType.ThrowableType))
                {
                    // generally we translate System.Exception to java.lang.Throwable,
                    // but not when allocating a new object
                    newClass = CilType.From(new JavaType(0, 0, newClass.JavaName));
                }
            }

            //
            // save any parameters pushed for the call to the constructor
            //

            int localIndex = SaveMethodArguments(callMethod);

            code.NewInstruction(0xBB /* new */, newClass.AsWritableClass, null);
            stackMap.PushStack(newClass);
            code.NewInstruction(0x59 /* dup */, null, null);
            stackMap.PushStack(newClass);

            if (newClass.IsValueClass)
            {
                // new value type object.  first call the default constructor,
                // to allocate the object.  then call the constructor provided
                // in the Newobj instruction, to initialize the new object

                var defaultConstructor = new CilMethod(newClass);
                PushGenericArguments(defaultConstructor);
                code.NewInstruction(0xB7 /* invokespecial */, newClass,
                                    defaultConstructor.WithGenericParameters);

                // the defaul constructor accepted generic parameters, if any,
                // and they should be discarded now

                int numGeneric =
                        defaultConstructor.WithGenericParameters.Parameters.Count;

                while (numGeneric-- > 0)
                    stackMap.PopStack(CilMain.Where);

                // if Newobj specifies a non-default constructor, then call it

                int numNonGeneric = callMethod.Parameters.Count;
                if (numNonGeneric != 0)
                {
                    code.NewInstruction(0x59 /* dup */, null, null);

                    LoadMethodArguments(callMethod, localIndex);

                    code.NewInstruction(0xB6 /* invokevirtual */, newClass, callMethod);

                    while (numNonGeneric-- > 0)
                        stackMap.PopStack(CilMain.Where);
                }

                stackMap.PopStack(CilMain.Where);
            }
            else
            {
                //
                // new reference type object
                //

                LoadMethodArguments(callMethod, localIndex);
                PushGenericArguments(callMethod);

                code.NewInstruction(0xB7 /* invokespecial */, newClass.AsWritableClass,
                                    callMethod.WithGenericParameters);

                ClearMethodArguments(callMethod, false);
            }

            return true;
        }



        bool Translate_Call(CilMethod callMethod)
        {
            var currentClass = method.DeclType;
            var callClass = callMethod.DeclType;

            byte op;
            if (callMethod.IsStatic)
            {
                if (ConvertCallToNop())
                    return true;

                if (    callClass.Equals(JavaType.ObjectType)
                     || callClass.Equals(JavaType.StringType))
                {
                    // generally we translate System.Object to java.lang.Object,
                    //                    and System.String to java.lang.String,
                    // but not in the case of a static method call
                    callClass = CilType.From(new JavaType(0, 0, callClass.JavaName));
                }

                else if (IsSystemTypeOperatorOrIsCallToIsInterface(callClass, callMethod))
                {
                    // rename System.Type operators == and != to system.RuntimeType, and
                    // rename call to RuntimeTypeHandle.IsInterface to system.RuntimeType
                    callClass = CilType.SystemRuntimeTypeType;
                }

                else if (callMethod.IsExternal &&
                    NativeMethodClasses.TryGetValue(callClass.ClassName, out var altClassName))
                {
                    // when the call target is a native methods, redirect it
                    callClass = CilType.From(new JavaType(0, 0, altClassName));
                }

                op = 0xB8; // invokestatic
            }
            else if (callMethod.IsConstructor || currentClass.IsDerivedFrom(callClass))
            {
                if (callClass.Equals(JavaType.ThrowableType))
                {
                    // generally we translate System.Exception to java.lang.Throwable,
                    // but not in the case of a super constructor call
                    callClass = CilType.From(new JavaType(0, 0, callClass.JavaName));
                }

                if (ConvertVirtualToStaticCall(callClass, callMethod))
                    return true;

                if (callMethod.IsVirtual && currentClass.Equals(callClass))
                {
                    // Android 'D8' does not support 'invokespecial' on a method
                    // on the same class, unless the method is marked final.
                    // if we know the target method is a virtual method, then
                    // it surely is not marked final, so use 'invokevirtual'.

                    op = 0xB6; // invokevirtual
                }
                else
                {
                    op = 0xB7; // invokespecial
                }

                if (callMethod.Name == "clone" && callMethod.Parameters.Count == 0)
                {
                    // if calling clone on the super object, implement Cloneable
                    code.Method.Class.AddInterface("java.lang.Cloneable");
                }
            }
            else
            {
                if (ConvertVirtualToStaticCall(callClass, callMethod))
                    return true;

                op = 0xB6; // invokevirtual
            }

            if (callMethod.IsArrayMethod)
            {
                arrays.Call(callMethod, cilInst);
            }
            else
            {
                CheckAndBoxArguments(callMethod, (op == 0xB6));
                PushGenericArguments(callMethod);
                code.NewInstruction(op, callClass.AsWritableClass,
                                    callMethod.WithGenericParameters);

                ClearMethodArguments(callMethod, (op == 0xB7));

                PushMethodReturnType(callMethod);
            }

            return true;

            bool IsSystemTypeOperatorOrIsCallToIsInterface(CilType callClass, CilMethod callMethod)
            {
                if (callClass.Equals(CilType.SystemTypeType) && (
                        callMethod.Name == "op_Equality" || callMethod.Name == "op_Inequality"))
                    return true;
                if (callClass.Equals(JavaType.ClassType) && callMethod.Name == "IsInterface")
                    return true;
                return false;
            }
        }



        bool Translate_Callvirt(CilMethod callMethod)
        {
            var callClass = callMethod.DeclType;

            CheckAndBoxArguments(callMethod, true);

            byte op;
            if (callClass.IsInterface)
            {
                if (callClass.ClassName == "system.ValueMethod")
                {
                    // convert calls on interface method from system.ValueMethod
                    // to calls on virtual method from system.ValueType
                    op = 0xB6; // invokevirtual
                    callClass = CilType.From(CilType.SystemValueType);
                }
                else
                    op = 0xB9; // invokeinterface
            }
            else
            {
                if (ConvertVirtualToStaticCall(callClass, callMethod))
                    return true;

                op = 0xB6; // invokevirtual
            }

            PushGenericArguments(callMethod);

            code.NewInstruction(op, callClass.AsWritableClass,
                                callMethod.WithGenericParameters);

            ClearMethodArguments(callMethod, false);
            PushMethodReturnType(callMethod);

            return true;
        }



        bool ConvertVirtualToStaticCall(CilType callClass, CilMethod callMethod)
        {
            if (callClass.ClassName == callClass.JavaName)
            {
                if (    callClass.ClassName == "system.FunctionalInterfaceDelegate"
                     && callMethod.Name == "AsInterface")
                {
                    // a call to system.FunctionalInterfaceDelegate::AsInterface()
                    // should be fixed to expect an 'object' return type, and then
                    // cast that return type to the proper interface
                    var tempMethod = new JavaMethodRef(callMethod.Name, JavaType.ObjectType);
                    code.NewInstruction(0xB6 /* invokevirtual */, callClass, tempMethod);
                    code.NewInstruction(0xC0 /* checkcast */, callMethod.ReturnType, null);
                    stackMap.PopStack(CilMain.Where);
                    stackMap.PushStack(callMethod.ReturnType);
                    return true;
                }

                if (    callMethod.IsExternal
                     && NativeMethodClasses.TryGetValue(callClass.ClassName,
                                                        out var altClassName))
                {
                    // a call to an instance method System.NameSpace.Class::Method,
                    // which is implemented only as a native method, is translated
                    // into a static call to some other class where the method is
                    // implemented.  such implementing classes are listed in the
                    // NativeMethodClasses dictionary the bottom of this file.

                    var altMethodName = callMethod.Name;
                    int altMethodNameIdx = altMethodName.IndexOf(CilMain.OPEN_PARENS);
                    if (altMethodNameIdx != -1)
                        altMethodName = altMethodName.Substring(0, altMethodNameIdx);

                    var tempMethod = new JavaMethodRef(altMethodName, callMethod.ReturnType);
                    var parameters = new List<JavaFieldRef>(callMethod.Parameters);
                    parameters.Insert(0, new JavaFieldRef("this", callClass));
                    tempMethod.Parameters = parameters;

                    code.NewInstruction(0xB8 /* invokestatic */,
                                        new JavaType(0, 0, altClassName), tempMethod);

                    ClearMethodArguments(callMethod, false);
                    PushMethodReturnType(callMethod);
                    return true;
                }

                // don't bother adjusting the call if the program explicitly
                // refers to the java class rather than the simulated wrapper,
                // e.g. to java.lang.Throwable rather than System.Exception
                return false;
            }

            if (    callClass.Equals(JavaType.StringType)
                 || callClass.Equals(JavaType.ThrowableType)
                 || (    callClass.Equals(JavaType.ObjectType)
                      && callMethod.Name == "GetType"
                      && callMethod.ToDescriptor() == "()Lsystem/Type;"))
            {
                // we map some basic .Net types their java counterparts, so we
                // can't invoke .Net instance methods on them directly.  instead,
                // we invoke a static method on our helper class.  for example:
                // ((System.String)x).CompareTo(y) -> system.String.CompareTo(x,y)

                var tempMethod = new JavaMethodRef(callMethod.Name, callMethod.ReturnType);
                var parameters = new List<JavaFieldRef>(callMethod.Parameters);
                parameters.Insert(0, new JavaFieldRef("this", callClass));
                tempMethod.Parameters = parameters;

                if (    callClass.Equals(JavaType.ThrowableType)
                     && callMethod.Name == "system-Exception-GetType")
                {
                    // undo the effect of CilMethod::MethodIsShadowing upon calling
                    // the virtual/overriding GetType() from System.Exception, and
                    // instead call the static system.Object.GetType()
                    tempMethod.Name = "GetType";
                    callClass = CilType.From(new JavaType(0, 0, "system.Object"));
                    parameters[0].Type = JavaType.ObjectType;
                }

                else
                    CilMethod.FixNameForVirtualToStaticCall(tempMethod, callClass);

                code.NewInstruction(0xB8 /* invokestatic */,
                                    new JavaType(0, 0, callClass.JavaName), tempMethod);

                ClearMethodArguments(callMethod, false);
                PushMethodReturnType(callMethod);

                return true;
            }

            if (    callClass.JavaName == null
                 && callClass.Equals(JavaType.ClassType)
                 && callMethod.Name == "GetRuntimeType")
            {
                // convert virtual call to RuntimeTypeHandle.GetRuntimeType
                // to a static call to system.RuntimeType

                code.NewInstruction(0xB8 /* invokestatic */,
                                    CilType.SystemRuntimeTypeType,
                                    new JavaMethodRef(
                                            callMethod.Name, callMethod.ReturnType,
                                            JavaType.ObjectType));

                ClearMethodArguments(callMethod, false);
                PushMethodReturnType(callMethod);

                return true;
            }

            return false;
        }



        bool Translate_Constrained(CilMethod callMethod, object data)
        {
            var typeRef = data as TypeReference;
            if (typeRef == null)
                throw new InvalidProgramException();

            var constrainType = CilType.From(typeRef);

            int localIndex = SaveMethodArguments(callMethod);

            var calledObjectType = (CilType) stackMap.PopStack(CilMain.Where);
            if (calledObjectType is BoxedType boxedType && boxedType.IsBoxedReference)
                boxedType.GetValue(code);
            else if (calledObjectType.IsGenericParameter)
                GenericUtil.ValueLoad(code);

            if (! (constrainType.IsGenericParameter || constrainType.Equals(JavaType.ObjectType)))
                code.NewInstruction(0xC0 /* checkcast */, constrainType.AsWritableClass, null);
            stackMap.PushStack(constrainType);

            var callClass = callMethod.DeclType;
            if (ConvertVirtualToStaticCall(callClass, callMethod))
                return true;

            if (GenericUtil.ShouldCallGenericCast(constrainType, callClass))
            {
                // if we are calling a method of a generic interface, then we need
                // to call GenericType.CallCast to get a reference to the proxy object
                // for the interface (created by InterfaceBuilder.BuildGenericProxy).
                GenericUtil.LoadMaybeGeneric(callClass, code);
                code.NewInstruction(0xB8 /* invokestatic */, GenericUtil.SystemGenericType,
                                    new JavaMethodRef("CallCast", JavaType.ObjectType,
                                            JavaType.ObjectType, CilType.SystemTypeType));
                stackMap.PopStack(CilMain.Where);
            }

            LoadMethodArguments(callMethod, localIndex);
            PushGenericArguments(callMethod);

            byte op;
            if (callClass.IsInterface)
                op = 0xB9; // invokeinterface
            else
                op = 0xB6; // invokevirtual

            code.NewInstruction(op, callClass.AsWritableClass, callMethod.WithGenericParameters);

            ClearMethodArguments(callMethod, false);
            PushMethodReturnType(callMethod);

            return true;
        }



        int SaveMethodArguments(CilMethod callMethod)
        {
            int localIndex = -1;
            int i = callMethod.Parameters.Count - (callMethod.HasDummyClassArg ? 1 : 0);
            while (i-- > 0)
            {
                var paramType = (CilType) callMethod.Parameters[i].Type;
                var stackTop = (CilType) stackMap.PopStack(CilMain.Where);
                var shouldBoxOrCast =
                        ShouldBoxOrCastArgumentForParameter(paramType, stackTop);

                if (shouldBoxOrCast != 0)
                {
                    // if the parameter is generic, the compiler may not bother
                    // boxing the argument, and we have to do it explicitly

                    paramType = BoxOrCastArgumentForParameter(paramType, stackTop,
                                                              shouldBoxOrCast, code);
                }
                else
                {
                    // if a method expects an array interface like IEnumerable
                    // but has an array on the stack, we need to create the proxy
                    if (! CodeArrays.MaybeGetProxy(stackTop, paramType, code))
                    {
                        // if not an array, then it might be a span.
                        // box a span that is passed by-reference
                        CodeSpan.Box(paramType, stackTop, code);
                    }
                }

                localIndex = locals.GetTempIndex(paramType);
                code.NewInstruction(paramType.StoreOpcode, null, (int) localIndex);
            }
            return localIndex;
        }



        void LoadMethodArguments(CilMethod callMethod, int localIndex)
        {
            int i = callMethod.Parameters.Count - (callMethod.HasDummyClassArg ? 1 : 0);
            while (i-- > 0)
            {
                var paramType = callMethod.Parameters[i].Type;

                var savedType = stackMap.GetLocal(localIndex);
                code.NewInstruction(savedType.LoadOpcode, null, (int) localIndex);
                stackMap.PushStack(savedType);

                locals.FreeTempIndex(localIndex);
                localIndex--;
                if (localIndex > 1 && stackMap.GetLocal(localIndex - 1).Category == 2)
                    localIndex--;
            }
        }



        void CheckAndBoxArguments(CilMethod callMethod, bool checkGenericCast)
        {
            var stack = stackMap.StackArray();
            int stackLen = stack.Length;
            int n = callMethod.Parameters.Count - (callMethod.HasDummyClassArg ? 1 : 0);
            if (stackLen < n)
                throw new InvalidProgramException();

            var callClass = callMethod.DeclType;
            int stackTopIndex;
            int idx;

            if (    checkGenericCast && (stackLen >= n + 1)
                 && (stackTopIndex = stackLen - n - 1) >= 0
                 && GenericUtil.ShouldCallGenericCast(
                                        (CilType) stack[stackTopIndex], callClass))
            {
                // if we are calling a method of a generic interface, then we need
                // to call GenericType.CallCast to get a reference to the proxy object
                // for the interface (created by InterfaceBuilder.BuildGenericProxy).
                //
                // this also means that we need to pop all parameters in order to get
                // to the object reference, so we just let SaveMethodArguments handle
                // any boxing of parameters, and can just skip the optimization that
                // is done in the 'else' branch of this 'if' statement.

                idx = SaveMethodArguments(callMethod);
                GenericUtil.LoadMaybeGeneric(callClass, code);
                code.NewInstruction(0xB8 /* invokestatic */, GenericUtil.SystemGenericType,
                                    new JavaMethodRef("CallCast", JavaType.ObjectType,
                                            JavaType.ObjectType, CilType.SystemTypeType));
                if (! callClass.IsInterface)
                {
                    // invokeinterface does not require a cast, but invokevirtual does
                    code.NewInstruction(0xC0 /* checkcast */, callClass, null);
                }
                stackMap.PopStack(CilMain.Where);
                LoadMethodArguments(callMethod, idx);
            }

            else if ( -1 != (idx = ShouldCastArrayArgument(
                                        stack, stackLen, callMethod.Parameters, n)))
            {
                // if any of the pushed arguments is an array, and the corresponding
                // parameter is an array interface, then call SaveMethodArguments
                // to cast the arguments
                //
                // or, if any of the pushed arguments is a value class, and the
                // parameter is System.Object, System.ValueType, or an interface,
                // then call SaveMethodArguments to clone the value argument
                //
                // except if this is the last parameter, in which case we can
                // avoid loading and saving all the parameters

                if (idx == n - 1)
                {
                    var stackTop = (CilType) stack[stackLen - n + idx];
                    var paramType = (CilType) callMethod.Parameters[idx].Type;

                    // if a method expects an array interface like IEnumerable
                    // but has an array on the stack, we need to create the proxy
                    if (! CodeArrays.MaybeGetProxy(stackTop, paramType, code))
                    {
                        // if not an array, then it might be a span.
                        // box a span that is passed by-reference
                        CodeSpan.Box(paramType, stackTop, code);
                    }
                }
                else
                {
                    idx = SaveMethodArguments(callMethod);
                    LoadMethodArguments(callMethod, idx);
                }
            }

            else
            {
                // if any primitive argument was passed to a generic parameter,
                // we need to box.  this is typically done via SaveMethodArguments,
                // which involves popping and pushing all the parameters, so as an
                // optimization, if it is the last parameter, we box it here

                for (idx = 0; idx < n; idx++)
                {
                    var stackTop = (CilType) stack[stackLen - n + idx];
                    var paramType = (CilType) callMethod.Parameters[idx].Type;
                    var shouldBoxOrCast =
                        ShouldBoxOrCastArgumentForParameter(paramType, stackTop);

                    if (shouldBoxOrCast != 0)
                    {
                        if (idx == n - 1)
                        {
                            BoxOrCastArgumentForParameter(paramType, stackTop,
                                                          shouldBoxOrCast, code);
                        }
                        else
                        {
                            idx = SaveMethodArguments(callMethod);
                            LoadMethodArguments(callMethod, idx);
                        }
                        break;
                    }
                }
            }

            int ShouldCastArrayArgument(JavaType[] stack, int stackLen,
                                        List<JavaFieldRef> args, int numArgs)
            {
                for (int i = 0; i < numArgs; i++)
                {
                    var stackType = stack[stackLen - numArgs + i];
                    if (    stackType.ArrayRank != 0
                         || object.ReferenceEquals(stackType, CodeArrays.GenericArrayType)
                         || stackType.Equals(JavaType.StringType))
                    {
                        var paramType = (CilType) args[i].Type;
                        if (GenericUtil.ShouldCallGenericCast((CilType) stackType, paramType))
                            return i;
                    }
                }
                return -1;
            }
        }



        static char ShouldBoxOrCastArgumentForParameter(CilType paramType, CilType stackTop)
        {
            if (stackTop.IsReference)
            {
                if (    stackTop.IsGenericParameter
                     && paramType.IsReference
                     && (! paramType.Equals(JavaType.ObjectType))
                     && (! paramType.Equals(stackTop)))
                {
                    return 'C'; // should cast
                }
            }
            else
            {
                // if a primitive argument is passed to a parameter of type object,
                // we should box the value ourselves.

                // it is also possible for a zero value to be passed to a parameter
                // of type system.Span, when the variable was originally a pointer
                // (see CodeLocals::InitLocalsVars), when the program intent is to
                // pass a null pointer.

                if (    paramType.Equals(JavaType.ObjectType)
                     || paramType.Equals(CodeSpan.SpanType))
                {
                    return 'B'; // should box
                }
            }
            return (char) 0;
        }



        static CilType BoxOrCastArgumentForParameter(CilType paramType, JavaType stackTop,
                                                     char shouldBoxOrCast, JavaCode code)
        {
            if (shouldBoxOrCast == 'C')
            {
                // if we get here, stack top is a generic parameter
                GenericUtil.ValueLoad(code);
                code.NewInstruction(0xC0 /* checkcast */, paramType, null);
                return paramType;
            }

            CilType inputType;

            var genericParameter = paramType.GetMethodGenericParameter();
            if (genericParameter != null)
            {
                var genericMark = CilMain.GenericStack.Mark();
                (inputType, _) = CilMain.GenericStack.Resolve(genericParameter.JavaName);
                CilMain.GenericStack.Release(genericMark);

                if (inputType == null)
                    throw new InvalidProgramException();
            }
            else
            {
                inputType = stackTop as CilType;
                if (inputType == null)
                    inputType = CilType.From(stackTop);
            }

            var boxedType = new BoxedType(inputType, false);
            boxedType.BoxValue(code);
            return boxedType;
        }



        void PushGenericArguments(CilMethod callMethod)
        {
            if (callMethod.HasDummyClassArg)
            {
                code.NewInstruction(0x01 /* aconst_null */, null, null);
                int n = callMethod.Parameters.Count;
                stackMap.PushStack(callMethod.Parameters[n - 1].Type);
            }

            int count1 = callMethod.Parameters.Count;
            var genericMethod = callMethod.WithGenericParameters;
            int count2 = genericMethod.Parameters.Count;

            for (int i = count1; i < count2; i++)
            {
                GenericUtil.LoadGeneric(genericMethod.Parameters[i].Name, code);
            }
        }



        void ClearMethodArguments(CilMethod callMethod, bool updateThis)
        {
            int n = callMethod.WithGenericParameters.Parameters.Count;
            while (n-- > 0)
                stackMap.PopStack(CilMain.Where);

            if (! callMethod.IsStatic)
            {
                var thisRef = stackMap.PopStack(CilMain.Where);
                if (updateThis && thisRef.Equals(JavaStackMap.UninitializedThis))
                {
                    // an 'uninitializedthis' first argument is updated in
                    // the stack frame after calling the super constructor
                    locals.UpdateThis(method.DeclType);
                }
            }
        }



        void PushMethodReturnType(CilMethod callMethod)
        {
            if (! callMethod.ReturnType.Equals(JavaType.VoidType))
            {
                // push the initial return value, which is already on the stack
                // even before we insert any generic casting instructions
                var returnType = (CilType) callMethod.ReturnType;
                stackMap.PushStack(returnType);
                // cast to the final return value, and replace it on the stack
                returnType = GenericUtil.CastMaybeGeneric(
                                    returnType, returnType.IsByReference, code);
                stackMap.PopStack(CilMain.Where);
                stackMap.PushStack(returnType);
            }
        }



        bool Translate_Return()
        {
            var returnType = (CilType) method.ReturnType;
            if (returnType.IsReference)
            {
                var stackTop = (CilType) stackMap.PopStack(CilMain.Where);

                // if a method returns an array interface like IEnumerable
                // but has an array on the stack, we need to create the proxy
                if (! CodeArrays.MaybeGetProxy(stackTop, returnType, code))
                {
                    // if not an array, then it might be a span.
                    // box a span that is returned as a by-reference
                    CodeSpan.Box(returnType, stackTop, code);
                }
            }

            code.NewInstruction(returnType.ReturnOpcode, null, null);

            CilMain.LoadFrameOrClearStack(stackMap, cilInst);

            return true;
        }



        void LoadToken()
        {
            // (1)  this instruction is used to load a reference to a System.Type
            // object, typically in the following sequence --
            //
            // ldtoken type-reference-operand
            // call (System.Type) System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)
            //
            // (2)  DotNetImporter adds a cast operator in java.lang.Class:
            //
            // explicit operator [to] java.lang.Class [from] (System.Type)
            //
            // this lets us write the following C# expression:
            //              (java.lang.Class) typeof(TypeName)
            //
            // which is compiled into the following sequence --
            //
            // ldtoken type-reference-operand
            // call (System.Type) System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)
            // call (java.lang.Class) java.lang.Class::op_Explicit(System.Type)
            //
            // in either case, we want to discard the 'call' instructions
            // (see also ConvertCallToNop() below).  for case (1), we translate
            // into a call to system.Util.GetType() to obtain a System.Type.
            // for case (2), we convert to a simple 'ldc' instruction.
            //
            // (3)  this instruction is used to load a reference to an array
            // initializer, typically in the following sequence --
            //
            // ldtoken field-definition-operand
            // call (System.Void) System.Void System.Runtime.CompilerServices.RuntimeHelpers
            //                    ::InitializeArray(System.Array,System.RuntimeFieldHandle)
            //

            var nextInst = cilInst.Next;
            bool nextInstIsValid = (nextInst != null && nextInst.OpCode.Code == Code.Call);

            if (    nextInstIsValid && cilInst.Operand is TypeReference typeRef
                 && IsCallToGetTypeFromHandle(nextInst))
            {
                //
                // case (1) and case (2), load a TypeReference
                //

                nextInst = nextInst.Next;
                bool loadClass = (nextInst != null && nextInst.OpCode.Code == Code.Call
                                            && IsCallToClassExplicitCast(nextInst));
                if (loadClass)
                {
                    if (    (typeRef is GenericInstanceType)
                         || (typeRef is GenericParameter))
                    {
                        throw CilMain.Where.Exception("generic type cannot be cast to java.lang.Class");
                    }

                    if (! (typeRef.IsDefinition || typeRef.HasGenericParameters))
                    {
                        var typeDef = CilType.AsDefinition(typeRef);
                        if (typeDef.HasGenericParameters)
                        {
                            // the expression typeof (Generic<>) gets a TypeReference with
                            // no generic parameters, we identify this case by looking at
                            // the type definition and comparing generic parameters
                            typeRef = typeDef;
                        }
                    }

                    code.NewInstruction(0x12 /* ldc */, CilType.From(typeRef).AsWritableClass, null);
                    stackMap.PushStack(CilType.From(JavaType.ClassType));
                }
                else if ((! typeRef.IsGenericInstance) && typeRef.HasGenericParameters)
                {
                    // special case of parameterless GenericType<,>
                    GenericUtil.LoadParameterlessGeneric(CilType.From(typeRef), code);
                }
                else
                {
                    var myType = CilMain.GenericStack.EnterType(typeRef);
                    GenericUtil.LoadMaybeGeneric(myType, code);
                    if (myType.IsGenericParameter && myType.ArrayRank != 0)
                    {
                        code.NewInstruction(0x12 /* ldc */, null, myType.ArrayRank);
                        stackMap.PushStack(JavaType.IntegerType);
                        code.NewInstruction(0xB6 /* invokevirtual */, CilType.SystemTypeType,
                                            new JavaMethodRef("MakeArrayType",
                                                CilType.SystemTypeType, JavaType.IntegerType));
                        stackMap.PopStack(CilMain.Where);
                    }
                }
            }

            else if (    nextInstIsValid && cilInst.Operand is FieldDefinition fieldDef
                      && IsCallToInitializeArray(nextInst))
            {
                //
                // case (3), initialize an array using a FieldDefinition
                //

                CodeArrays.InitializeArray(fieldDef.InitialValue, code);
            }

            else if (cilInst.Operand is MethodReference methodRef)
            {
                var calledMethod = CilMethod.From(methodRef);
                if (methodRef.IsGenericInstance || calledMethod.DeclType.HasGenericParameters)
                    throw new Exception("generic type");
                GetReflectMethod(calledMethod, code);
            }

            else
                throw new Exception("not followed by call to GetTypeFromHandle or InitializeArray");
        }



        bool ConvertCallToNop()
        {
            // (1)  convert 'call' instructions to 'nop' in order to discard calls to
            //      System.Type::GetTypeFromHandle  and  java.lang.Class::op_Explicit
            //  for more information, see 'ldtoken' above.
            //
            // but we do make sure that calls to GetTypeFromHandle follow 'ldtoken',
            // and calls to op_Explicit follow call to GetTypeFromHandle.
            //
            // (2)  discard calls to fake methods on system.BooleanUtils which are
            // used for an optimization in system.Boolean in BaseLib.
            //

            bool nop;

            if (IsCallToGetTypeFromHandle(cilInst))
            {
                var prevInst = cilInst.Previous;
                if (prevInst == null || prevInst.OpCode.Code != Code.Ldtoken)
                    throw CilMain.Where.Exception("call to GetTypeFromHandle not following ldtoken");
                nop = true;
            }

            else if (IsCallToInitializeArray(cilInst))
            {
                var prevInst = cilInst.Previous;
                if (prevInst == null || prevInst.OpCode.Code != Code.Ldtoken)
                    throw CilMain.Where.Exception("call to InitializeArray not following ldtoken");
                nop = true;
            }

            else if (IsCallToClassExplicitCast(cilInst))
            {
                var prevInst = cilInst.Previous;
                if (    prevInst == null || prevInst.OpCode.Code != Code.Call
                     || (! IsCallToGetTypeFromHandle(prevInst)))
                {
                    throw CilMain.Where.Exception("cast to java.lang.Class not following call to GetTypeFromHandle");
                }
                nop = true;
            }

            else
                nop = IsCallToBooleanUtils(cilInst);

            if (nop)
            {
                code.NewInstruction(0x00 /* nop */, null, null);
                return true;
            }

            return false;

            //
            // dummy intrinsics in dummy class system.BooleanUtils
            // (see Boolean.cs in BaseLib) which are used to convert
            // a boolean element in an array to a byte
            //

            bool IsCallToBooleanUtils(Instruction inst)
            {
                return (    inst.Operand is MethodReference methodRef
                         && methodRef.DeclaringType.FullName == "system.BooleanUtils");
            }
        }



        static bool IsCallToGetTypeFromHandle(Instruction inst)
        {
            return (    inst.Operand is MethodReference methodRef
                     && methodRef.DeclaringType.FullName == "System.Type"
                     && methodRef.Name == "GetTypeFromHandle");
        }

        static bool IsCallToInitializeArray(Instruction inst)
        {
            return (    inst.Operand is MethodReference methodRef
                     && methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers"
                     && methodRef.Name == "InitializeArray");
        }

        static bool IsCallToClassExplicitCast(Instruction inst)
        {
            return (    inst.Operand is MethodReference methodRef
                     && methodRef.DeclaringType.FullName == "java.lang.Class"
                     && methodRef.Name == "op_Explicit");
        }



        static void GetReflectMethod(CilMethod calledMethod, JavaCode code)
        {
            var callParameters = new List<JavaFieldRef>();

            code.NewInstruction(0x12 /* ldc */, calledMethod.DeclType.AsWritableClass, null);
            code.StackMap.PushStack(JavaType.ClassType);
            callParameters.Add(new JavaFieldRef("", JavaType.ClassType));

            code.NewInstruction(0x12 /* ldc */, null, calledMethod.Name);
            code.StackMap.PushStack(JavaType.StringType);
            callParameters.Add(new JavaFieldRef("", JavaType.StringType));

            PushClass(calledMethod.ReturnType, code);
            callParameters.Add(new JavaFieldRef("", JavaType.ClassType));

            var parameters = calledMethod.WithGenericParameters.Parameters;
            int parametersCount = parameters.Count;

            var arrayOfClass = CilType.From(JavaType.ClassType).AdjustRank(1);
            code.NewInstruction(0x12 /* ldc */, null, parametersCount);
            code.StackMap.PushStack(JavaType.IntegerType);
            code.NewInstruction(0xBD /* anewarray */, JavaType.ClassType, null);
            code.StackMap.PopStack(CilMain.Where);
            code.StackMap.PushStack(arrayOfClass);
            callParameters.Add(new JavaFieldRef("", arrayOfClass));

            for (int i = 0; i < parametersCount; i++)
            {
                code.NewInstruction(0x59 /* dup */, null, null);
                code.StackMap.PushStack(arrayOfClass);

                code.NewInstruction(0x12 /* ldc */, null, i);
                code.StackMap.PushStack(JavaType.IntegerType);

                PushClass(parameters[i].Type, code);

                code.NewInstruction(0x53 /* aastore */, null, null);
                code.StackMap.PopStack(CilMain.Where);
                code.StackMap.PopStack(CilMain.Where);
                code.StackMap.PopStack(CilMain.Where);
            }

            code.NewInstruction(0xB8 /* invokestatic */,
                                new JavaType(0, 0, "system.reflection.RuntimeMethodInfo"),
                    new JavaMethodRef("ReflectMethod", CilType.ReflectMethodType, callParameters));

            code.StackMap.PopStack(CilMain.Where);  // arrayOfClass
            code.StackMap.PopStack(CilMain.Where);  // return type
            code.StackMap.PopStack(CilMain.Where);  // name
            code.StackMap.PopStack(CilMain.Where);  // class

            code.StackMap.PushStack(CilType.ReflectMethodType);

            static void PushClass(JavaType parameterOrReturnType, JavaCode code)
            {
                if (parameterOrReturnType.IsReference)
                    code.NewInstruction(0x12 /* ldc */, parameterOrReturnType, null);
                else
                {
                    #if false
                    string jcls;
                    switch (parameterOrReturnType.PrimitiveType)
                    {
                        case TypeCode.Empty:                        jcls = "Void";      break;
                        case TypeCode.Boolean:                      jcls = "Boolean";   break;
                        case TypeCode.SByte: case TypeCode.Byte:    jcls = "Byte";      break;
                        case TypeCode.Char:                         jcls = "Character"; break;
                        case TypeCode.Int16: case TypeCode.UInt16:  jcls = "Short";     break;
                        case TypeCode.Int32: case TypeCode.UInt32:  jcls = "Integer";   break;
                        case TypeCode.Int64: case TypeCode.UInt64:  jcls = "Long";      break;
                        case TypeCode.Single:                       jcls = "Float";     break;
                        case TypeCode.Double:                       jcls = "Double";    break;
                        default: throw new Exception("bad primitive type");
                    }
                    code.NewInstruction(0xB2 /* getstatic */,
                                        new JavaType(0, 0, "java.lang." + jcls),
                                        new JavaFieldRef("TYPE", JavaType.ClassType));
                    #endif
                    var wrapperClass = parameterOrReturnType.Wrapper;
                    if (wrapperClass == null)
                        throw new Exception("bad primitive type");
                    code.NewInstruction(0xB2 /* getstatic */, wrapperClass,
                                        new JavaFieldRef("TYPE", JavaType.ClassType));
                }

                code.StackMap.PushStack(JavaType.ClassType);
            }
        }



        static Dictionary<string, string> NativeMethodClasses = new Dictionary<string, string>()
        {
            { "java.lang.Class", "system.RuntimeType" },
            { "system.reflection.AssemblyName", "system.reflection.RuntimeAssembly" },
            { "system.TimeSpan", "system.CompatibilitySwitches$TimeSpan" },
        };

    }
}
