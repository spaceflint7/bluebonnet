
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public partial class CodeBuilder
    {

        void PopOrDupStack(Code cilOp)
        {
            var stackTop = stackMap.PopStack(CilMain.Where);

            byte op;
            if (cilOp == Code.Dup)
            {
                stackMap.PushStack(stackTop);
                stackMap.PushStack(stackTop);
                // select 0x59 dup, or 0x5C dup2
                op = (byte) (0x56 + stackTop.Category * 3);
            }
            else if (cilOp == Code.Pop)
            {
                // select 0x57 pop, or 0x58 pop2
                op = (byte) (0x56 + stackTop.Category);
            }
            else
                throw new InvalidProgramException();

            code.NewInstruction(op, null, null);
        }



        void LoadConstant(Code op, Mono.Cecil.Cil.Instruction inst)
        {
            JavaType pushType;
            object pushValue;
            byte pushOpcode;

            if (op == Code.Ldnull)
            {
                pushValue = null;
                pushOpcode = 0x01; // aconst_null
                pushType = JavaStackMap.Null;
            }
            else
            {
                var data = inst.Operand;
                pushOpcode = 0x12; // ldc

                if (data is string stringVal) // Code.Ldstr
                {
                    pushValue = stringVal;
                    pushType = JavaType.StringType;
                }
                else if (data is double doubleVal) // Code.Ldc_R8
                {
                    pushValue = doubleVal;
                    pushType = JavaType.DoubleType;
                }
                else if (data is float floatVal) // Code.Ldc_R4
                {
                    pushValue = floatVal;
                    pushType = JavaType.FloatType;
                }
                else if (data is long longVal) // Code.Ldc_I8
                {
                    pushValue = longVal;
                    pushType = JavaType.LongType;
                }
                else if (IsAndBeforeShift(inst, code))
                {
                    // jvm shift instructions mask the shift count, so
                    // eliminate AND-ing with 31 and 63 prior to a shift
                    code.NewInstruction(0x00 /* nop */, null, null);
                    return;
                }
                else
                {
                    pushType = JavaType.IntegerType;

                    if (data is int intVal) // Code.Ldc_I4
                        pushValue = intVal;

                    else if (data is sbyte sbyteVal) // Code.Ldc_I4_S
                        pushValue = (int) sbyteVal;

                    else if (op >= Code.Ldc_I4_M1 && op <= Code.Ldc_I4_8)
                        pushValue = (int) (op - Code.Ldc_I4_0);

                    else
                        throw new InvalidProgramException();
                }
            }

            code.NewInstruction(pushOpcode, null, pushValue);
            stackMap.PushStack(CilType.From(pushType));
        }



        public static int? IsLoadConstant(Mono.Cecil.Cil.Instruction inst)
        {
            if (inst != null)
            {
                var op = inst.OpCode.Code;
                var data = inst.Operand;
                if (op == Code.Ldc_I4 && data is int intVal)
                    return intVal;
                if (op == Code.Ldc_I4_S && data is sbyte sbyteVal)
                    return sbyteVal;
            }
            return null;
        }



        public static bool IsAndBeforeShift(Mono.Cecil.Cil.Instruction inst, JavaCode code)
        {
            // jvm shift instructions mask the shift count, so
            // eliminate AND-ing with 31 and 63 prior to a shift.

            // the input inst should point to the first of three
            // instructions, and here we check if the sequence is:
            // ldc_i4 31 or 63; and; shift

            // used by LoadConstant (see above), CodeNumber::Calculation

            var next1 = inst.Next;
            if (next1 != null && next1.OpCode.Code == Code.And)
            {
                var next2 = next1.Next;
                if (next2 != null && (    next2.OpCode.Code == Code.Shl
                                       || next2.OpCode.Code == Code.Shr
                                       || next2.OpCode.Code == Code.Shr_Un))
                {
                    var stackArray = code.StackMap.StackArray();
                    if (    stackArray.Length >= 2
                         && IsLoadConstant(inst) is int shiftMask
                         && (   (    shiftMask == 31
                                  && stackArray[0].Equals(JavaType.IntegerType))
                             || (    shiftMask == 63
                                  && stackArray[0].Equals(JavaType.LongType))))
                    {
                        return true;
                    }
                }
            }
            return false;
        }



        void CastToClass(object data)
        {
            var srcType = (CilType) code.StackMap.PopStack(CilMain.Where);
            if (! (srcType.IsReference && data is TypeReference))
                throw new InvalidProgramException();

            byte op;
            var dstType = (CilType) CilType.From((TypeReference) data);

            if (GenericUtil.ShouldCallGenericCast(srcType, dstType))
            {
                code.StackMap.PushStack(srcType);
                // casting to a generic type is done via GenericType.TestCast
                GenericUtil.CastToGenericType((TypeReference) data, 1, code);
                code.StackMap.PopStack(CilMain.Where);  // srcType
                op = 0xC0; // checkcast
            }
            else
            {
                // cast to a non-generic type
                if (dstType.Equals(srcType) || dstType.Equals(JavaType.ObjectType))
                    op = 0x00; // nop
                else if (dstType.IsReference)
                {
                    CodeArrays.CheckCast(dstType, true, code);

                    if (    srcType.ArrayRank != 0
                         && srcType.ArrayRank == dstType.ArrayRank
                         && srcType.PrimitiveType != 0
                         && dstType.PrimitiveType != 0
                         && srcType.AdjustRank(-srcType.ArrayRank).NewArrayType
                                == dstType.AdjustRank(-dstType.ArrayRank).NewArrayType)
                    {
                        // casting to same java array type, e.g. byte[] to sbyte[]
                        op = 0x00; // nop
                    }
                    else if (dstType.IsGenericParameter)
                        op = 0x00; // nop
                    else
                        op = 0xC0; // checkcast
                }
                else
                    throw new InvalidProgramException();
            }

            code.NewInstruction(op, dstType, null);
            code.StackMap.PushStack(dstType);
        }



        void LoadObject(Code cilOp, object data)
        {
            if (data is TypeReference typeRef)
            {
                var dataType = CilType.From(typeRef);
                var fromType = (CilType) code.StackMap.PopStack(CilMain.Where);

                if (CodeSpan.LoadStore(true, fromType, null, dataType, code))
                    return;

                if (    (! dataType.IsReference) && cilOp == Code.Ldobj
                     && fromType is BoxedType fromBoxedType
                     && dataType.PrimitiveType == fromBoxedType.UnboxedType.PrimitiveType)
                {
                    // 'ldobj primitive' with a corresponding boxed type on the stack.
                    // we implement by unboxing the boxed type into a primitive value.
                    fromBoxedType.GetValue(code);
                    stackMap.PushStack(fromBoxedType.UnboxedType);
                    return;
                }

                if (    dataType.IsGenericParameter
                     || (dataType.IsValueClass && dataType.Equals(fromType)))
                {
                    code.StackMap.PushStack(dataType);

                    if (SkipClone(cilInst.Next, fromType))
                    {
                        // see below for the several cases where we determine
                        // that we can safely avoid making a clone of the value
                        code.NewInstruction(0x00 /* nop */, null, null);
                    }
                    else if (dataType.IsGenericParameter)
                    {
                        GenericUtil.ValueClone(code);
                    }
                    else if (cilOp == Code.Ldobj)
                    {
                        code.NewInstruction(0x00 /* nop */, null, null);
                    }
                    else
                    {
                        CilMethod.ValueMethod(CilMethod.ValueClone, code);
                    }
                    return;
                }

                if (dataType.IsReference && cilOp == Code.Box)
                {
                    // 'box' is permitted on reference types, we treat it as a cast
                    code.StackMap.PushStack(fromType);
                    CastToClass(data);
                    return;
                }

                if (! dataType.IsReference)
                {
                    var boxedType = new BoxedType(dataType, false);
                    code.StackMap.PushStack(boxedType);
                    boxedType.BoxValue(code);
                    return;
                }
            }
            throw new InvalidProgramException();


            bool SkipClone(Mono.Cecil.Cil.Instruction next, CilType checkType)
            {
                if (checkType.IsByReference)
                    return true;

                if (next == null)
                    return false;
                var op = next.OpCode.Code;

                if (IsBrTrueBrFalseIsInst(op))
                {
                    // if 'ldobj' or 'box' is followed by a check for null,
                    // we don't actually need to clone just for the test
                    return true;
                }

                if (op == Code.Box)
                {
                    if (next.Operand is TypeReference nextTypeRef)
                    {
                        // 'ldobj' may be followed by 'box', to load the value
                        // of a byref value type, and then box it into an object.
                        // effectively we only need to clone once, in such a case.
                        return CilType.From(nextTypeRef).Equals(checkType);
                    }
                }

                var (storeType, _) = locals.GetLocalFromStoreInst(op, next.Operand);
                if (storeType != null)
                {
                    // 'ldobj' or 'box' may be followed by a store instruction.
                    // if storing into a variable of the same value type, the
                    // next instruction will copy the value, so skip clone.
                    return storeType.Equals(checkType);
                }

                if (op == Code.Ret && checkType.IsClonedAtTop)
                {
                    // if the value on the stack was cloned/boxed at the top of
                    // the method, then we can avoid clone and return it directly.
                    // see also:  CilType.MakeClonedAtTop and its callers.
                    return true;
                }

                if (op == Code.Unbox_Any)
                {
                    if (next.Operand is TypeReference nextTypeRef)
                    {
                        // 'ldobj' or 'box' may be followed by 'unbox' and
                        // then one of the instructions above, e.g. 'brtrue'
                        // or 'stloc'.  we still want to detect such a case
                        // and prevent a needless clone.
                        return SkipClone(next.Next, CilType.From(nextTypeRef));
                    }
                }

                return false;
            }
        }



        void StoreObject(object data)
        {
            if (data is TypeReference typeRef)
            {
                var dataType = CilType.From(typeRef);

                var valueType = (CilType) code.StackMap.PopStack(CilMain.Where);
                var intoType = (CilType) code.StackMap.PopStack(CilMain.Where);
                if (CodeSpan.LoadStore(false, intoType, null, dataType, code))
                    return;

                if (    (! dataType.IsReference)
                     && intoType is BoxedType intoBoxedType
                     && dataType.PrimitiveType == intoBoxedType.UnboxedType.PrimitiveType)
                {
                    // 'stobj primitive' with a primitive value on the stack
                    intoBoxedType.SetValueOV(code);
                    return;
                }

                code.StackMap.PushStack(intoType);
                code.StackMap.PushStack(valueType);

                GenericUtil.ValueCopy(dataType, code, true);
                code.StackMap.PopStack(CilMain.Where);
                code.StackMap.PopStack(CilMain.Where);
            }
            else
                throw new InvalidProgramException();
        }



        void InitObject(object data)
        {
            if (data is TypeReference typeRef)
            {
                var dataType = CilType.From(typeRef);
                var fromType = code.StackMap.PopStack(CilMain.Where);

                if (CodeSpan.Clear(fromType, dataType, code))
                    return;

                if (    dataType.IsGenericParameter
                     || (dataType.IsValueClass && dataType.Equals(fromType)))
                {
                    if (dataType.IsGenericParameter)
                        code.NewInstruction(0xC0 /* checkcast */, CilType.SystemValueType, null);

                    code.NewInstruction(0xB6 /* invokevirtual */,
                                        CilType.SystemValueType, CilMethod.ValueClear);
                    return;
                }
            }
            throw new InvalidProgramException();
        }



        void NullCheck()
        {
            // calling java.lang.Object.getClass() to check for a null reference
            // is the approach used by the java compiler

            code.NewInstruction(0x59 /* dup */, null, null);
            code.StackMap.PushStack(JavaType.ObjectType);

            code.NewInstruction(0xB6 /* invokevirtual */, JavaType.ObjectType,
                                new JavaMethodRef("getClass", JavaType.ClassType));

            code.NewInstruction(0x57 /* pop */, null, null);
            code.StackMap.PopStack(CilMain.Where);
        }



        void UnboxObject(Code cilOp, object data)
        {
            if (data is TypeReference typeRef)
            {
                var dataType = CilType.From(typeRef);

                if (dataType.IsValueClass)
                {
                    if (dataType.IsGenericParameter)
                    {
                        if (cilOp == Code.Unbox_Any)
                        {
                            // unboxing (casting) a concrete type as a generic type
                            var stackTop = (CilType) code.StackMap.PopStack(CilMain.Where);
                            if (stackTop.Equals(JavaType.ObjectType) || dataType.Equals(stackTop))
                                code.NewInstruction(0x00 /* nop */, null, null);
                            else
                                code.NewInstruction(0xC0 /* checkcast */, dataType, null);
                            code.StackMap.PushStack(dataType);
                        }
                        else // plain Unbox
                            throw new InvalidProgramException();
                    }
                    else
                    {
                        code.StackMap.PopStack(CilMain.Where);
                        code.NewInstruction(0xC0 /* checkcast */, dataType, null);
                        code.StackMap.PushStack(dataType);
                    }
                }

                else if (! dataType.IsReference)
                {
                    var boxedType = new BoxedType(dataType, false);

                    var fromType = code.StackMap.PopStack(CilMain.Where);
                    if (! fromType.Equals(boxedType))
                        code.NewInstruction(0xC0 /* checkcast */, boxedType, null);

                    boxedType.GetValue(code);
                    code.StackMap.PushStack(dataType);
                }

                else
                {
                    if (cilOp == Code.Unbox_Any)
                    {
                        NullCheck();
                        CastToClass(data);
                    }
                    else // plain Unbox
                        throw new InvalidProgramException();
                }
            }
        }



        public static bool IsBrTrueBrFalseIsInst(Code op)
            => (    op == Code.Brtrue  || op == Code.Brtrue_S
                 || op == Code.Brfalse || op == Code.Brfalse_S
                 || op == Code.Isinst);



        public static void CreateToStringMethod(JavaClass theClass)
        {
            // we add an explicit ToString method if it is missing in a
            // non-interface class, which derives directly from java.lang.Object

            if ((theClass.Flags & JavaAccessFlags.ACC_INTERFACE) != 0)
                return;
            if (! theClass.Super.Equals(JavaType.ObjectType.ClassName))
                return;
            foreach (var m in theClass.Methods)
            {
                if (    m.Name == "toString"
                     && (m.Flags & JavaAccessFlags.ACC_STATIC) == 0
                     && m.ReturnType.Equals(JavaType.StringType)
                     && m.Parameters.Count == 0)
                {
                    return;
                }
            }

            // create method:   string ToString() => GetType().ToString();

            var toStringMethod = new JavaMethodRef("toString", JavaType.StringType);
            var code = CilMain.CreateHelperMethod(theClass, toStringMethod, 1, 1);
            code.Method.Flags &= ~JavaAccessFlags.ACC_BRIDGE;

            code.NewInstruction(0x19 /* aload */, null, (int) 0);

            code.NewInstruction(0xB8 /* invokestatic */,
                                new JavaType(0, 0, "system.Object"),
                                new JavaMethodRef("GetType",
                                        CilType.SystemTypeType, JavaType.ObjectType));

            code.NewInstruction(0xB6 /* invokevirtual */, JavaType.ObjectType, toStringMethod);

            code.NewInstruction(JavaType.StringType.ReturnOpcode, null, null);
        }



        public static JavaMethod CreateSyncWrapper(JavaMethod innerMethod, CilType declType)
        {
            //
            // if method is decorated with [MethodImplOptions.Synchronized],
            // create a wrapper method that locks the object (for instance methods)
            // or the type (for static methods), and then calls the original method,
            // which we make private and rename to have a unique suffix
            //

            if (innerMethod.Name == "<init>")
                throw CilMain.Where.Exception("[Synchronized] is not supported on constructors");

            var outerMethod = new JavaMethod(innerMethod.Class, innerMethod);

            outerMethod.Flags = innerMethod.Flags;
            innerMethod.Flags &= ~(JavaAccessFlags.ACC_PUBLIC | JavaAccessFlags.ACC_PROTECTED);
            innerMethod.Flags |= JavaAccessFlags.ACC_PRIVATE;

            innerMethod.Name += "---inner";

            // count the size of locals in the parameters, plus one.  we have to
            // add one for an instance method, to account for the 'this' argument.
            // and if a static method, we have to add one for the lock object.

            var numLocals = 1;
            for (int i = outerMethod.Parameters.Count; i-- > 0; )
                numLocals += outerMethod.Parameters[i].Type.Category;

            // prepare to generate instructions

            var code = outerMethod.Code = new JavaCode();
            code.Method = outerMethod;
            code.Instructions = new List<JavaCode.Instruction>();
            code.MaxLocals = numLocals + 1;

            var exception = new JavaAttribute.Code.Exception();
            exception.start = /* label */ 1;
            exception.endPlus1 = /* label */ 2;
            exception.handler = /* label */ 3;
            exception.catchType = CodeExceptions.ThrowableType.ClassName;

            code.Exceptions = new List<JavaAttribute.Code.Exception>();
            code.Exceptions.Add(exception);

            code.StackMap = new JavaStackMap();
            code.StackMap.SaveFrame((ushort) 0, false, CilMain.Where);

            // get a reference to 'this' (for instance methods)
            // or to the type object (for static methods),
            // then lock on the reference pushed on the stack

            int lockedObjectIndex;
            if ((outerMethod.Flags & JavaAccessFlags.ACC_STATIC) == 0)
            {
                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                code.StackMap.PushStack(JavaType.ObjectType);
                code.StackMap.SetLocal(0, JavaType.ObjectType);

                lockedObjectIndex = 0;
            }
            else
            {
                GenericUtil.LoadMaybeGeneric(declType, code);
                code.NewInstruction(0x59 /* dup */, null, null);
                code.StackMap.PushStack(JavaType.ObjectType);
                code.StackMap.PopStack(CilMain.Where);
                code.NewInstruction(0x3A /* astore */, null, (int) numLocals);
                code.StackMap.SetLocal((int) numLocals, JavaType.ObjectType);

                lockedObjectIndex = (int) numLocals;
            }

            code.NewInstruction(0xB8 /* invokestatic */,
                                new JavaType(0, 0, "system.threading.Monitor"),
                                new JavaMethodRef(
                                        "Enter", JavaType.VoidType, JavaType.ObjectType));
            code.StackMap.PopStack(CilMain.Where);

            code.NewInstruction(0x00 /* nop */, null, null, /* label */ 1);
            code.StackMap.SaveFrame((ushort) 1, false, CilMain.Where);

            // push all arguments for the call to the inner method

            byte callOpcode;
            int localIndex = 0;
            if ((outerMethod.Flags & JavaAccessFlags.ACC_STATIC) == 0)
            {
                code.StackMap.PushStack(JavaType.ObjectType);
                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                localIndex++;
                callOpcode = 0xB7; // invokespecial
            }
            else
                callOpcode = 0xB8; // invokestatic

            for (int i = 0; i < outerMethod.Parameters.Count; i++)
            {
                var paramType = outerMethod.Parameters[i].Type;
                code.StackMap.PushStack(paramType);
                code.NewInstruction(paramType.LoadOpcode, null, (int) localIndex);
                localIndex += paramType.Category;
            }

            code.NewInstruction(callOpcode, declType, innerMethod);

            // if we get here, then no exception was thrown in the
            // inner method, we need to unlock and return the result

            code.StackMap.ClearStack();
            if (! innerMethod.ReturnType.Equals(JavaType.VoidType))
                code.StackMap.PushStack(innerMethod.ReturnType);

            code.NewInstruction(0x00 /* nop */, null, null, /* label */ 2);
            code.StackMap.SaveFrame((ushort) 2, false, CilMain.Where);

            code.NewInstruction(0x19 /* aload */, null, lockedObjectIndex);
            code.StackMap.PushStack(JavaType.ObjectType);

            code.NewInstruction(0xB8 /* invokestatic */,
                                new JavaType(0, 0, "system.threading.Monitor"),
                                new JavaMethodRef(
                                        "Exit", JavaType.VoidType, JavaType.ObjectType));

            code.NewInstruction(innerMethod.ReturnType.ReturnOpcode, null, null);

            // if we get here, then an exception was thrown, unlock
            // and rethrow the exception

            code.StackMap.ClearStack();
            code.StackMap.PushStack(CodeExceptions.ThrowableType);

            code.NewInstruction(0x00 /* nop */, null, null, /* label */ 3);
            code.StackMap.SaveFrame((ushort) 3, true, CilMain.Where);

            code.NewInstruction(0x19 /* aload */, null, lockedObjectIndex);
            code.StackMap.PushStack(JavaType.ObjectType);

            code.NewInstruction(0xB8 /* invokestatic */,
                                new JavaType(0, 0, "system.threading.Monitor"),
                                new JavaMethodRef(
                                        "Exit", JavaType.VoidType, JavaType.ObjectType));

            code.StackMap.PopStack(CilMain.Where);
            code.NewInstruction(0xBF /* athrow */, null, null);

            code.StackMap.ClearStack();
            code.MaxStack = code.StackMap.GetMaxStackSize(CilMain.Where);

            return outerMethod;
        }



        public static void CreateSuppressibleFinalize(JavaMethod innerMethod, CilType declType,
                                                      JavaClass theClass)
        {
            //
            // if the class defines a finalizer method Finalize() then:
            //
            // - create a flag field that tracks whether finalization is suppressed
            //
            // - implement interface system.GC.FinalizeSuppressible, and its Set()
            // method, which sets the flag field
            //
            // - create a wrapper method that checks the flag field and possibly
            // invokes the original finalizer
            //
            // see also: system.GC in baselib
            //

            var flagField = new JavaField();
            flagField.Name = "-finalize-suppressed";
            flagField.Type = CilType.From(JavaType.BooleanType);
            flagField.Class = theClass;
            flagField.Flags = JavaAccessFlags.ACC_PRIVATE | JavaAccessFlags.ACC_VOLATILE;

            if (theClass.Fields == null)
                theClass.Fields = new List<JavaField>();
            theClass.Fields.Add(flagField);

            //
            // implement the interface method
            //

            var ifcMethod = new JavaMethod("system-GC$SuppressibleFinalize-Set",
                                           JavaType.VoidType);
            ifcMethod.Class = theClass;
            ifcMethod.Flags = JavaAccessFlags.ACC_PUBLIC;

            var code = ifcMethod.Code = new JavaCode();
            code.Method = ifcMethod;
            code.Instructions = new List<JavaCode.Instruction>();
            code.MaxLocals = code.MaxStack = 2;

            code.NewInstruction(0x19 /* aload */, null, (int) 0);
            code.NewInstruction(0x12 /* ldc */, null, (int) 1);
            code.NewInstruction(0xB5 /* putfield */, declType, flagField);
            code.NewInstruction(JavaType.VoidType.ReturnOpcode, null, null);

            theClass.Methods.Add(ifcMethod);
            theClass.AddInterface("system.GC$SuppressibleFinalize");

            //
            // create the wrapper method
            //

            var outerMethod = new JavaMethod(theClass, innerMethod);

            outerMethod.Flags = JavaAccessFlags.ACC_PROTECTED;
            innerMethod.Flags = JavaAccessFlags.ACC_PRIVATE;

            innerMethod.Name += "---inner";

            // prepare to generate instructions

            code = outerMethod.Code = new JavaCode();
            code.Method = outerMethod;
            code.Instructions = new List<JavaCode.Instruction>();
            code.StackMap = new JavaStackMap();
            code.StackMap.SaveFrame((ushort) 0, false, CilMain.Where);
            code.MaxLocals = code.MaxStack = 1;

            //
            // check the flag field to determine if suppressed
            //

            code.NewInstruction(0x19 /* aload */, null, (int) 0);

            code.NewInstruction(0xB4 /* getfield */, declType, flagField);

            code.NewInstruction(0x9A /* ifne != zero */, null, (ushort) 0xFFFE);

            code.NewInstruction(0x19 /* aload */, null, (int) 0);

            code.NewInstruction(0xB7 /* invokespecial */, declType, innerMethod);

            code.NewInstruction(JavaType.VoidType.ReturnOpcode, null, null,
                                /* label */ 0xFFFE);

            code.StackMap.SaveFrame((ushort) 0xFFFE, true, CilMain.Where);

            theClass.Methods.Add(outerMethod);
        }

    }

}
