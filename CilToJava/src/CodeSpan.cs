
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public class CodeSpan
    {

        public static void Sizeof(object data, JavaCode code)
        {
            if (data is TypeReference operandTypeReference)
            {
                var myType = CilType.From(operandTypeReference);
                if (myType.IsValueClass)
                {
                    // compiler generated IL typically calculates the stackalloc
                    // total buffer size, for primitive types, and uses 'sizeof'
                    // for values types.  but it also uses 'sizeof' for generic
                    // types, which may stand for a primitive type.
                    GenericUtil.LoadMaybeGeneric(myType, code);
                    code.NewInstruction(0xB8 /* invokestatic */, SpanType,
                                        new JavaMethodRef("Sizeof" + CilMain.EXCLAMATION,
                                                JavaType.IntegerType, CilType.SystemTypeType));
                    code.StackMap.PopStack(CilMain.Where);  // generic type
                    code.StackMap.PushStack(CilType.From(JavaType.IntegerType));
                    return;
                }
                else if (! myType.IsReference)
                {
                    // if the compiler inserted a typeof(primitive) instruction,
                    // then we need to replace it with a load constant
                    int size = -1;
                    if (myType.Category == 2)   // Double, Int64, UInt64
                        size = 8;
                    else
                    {
                        switch (myType.PrimitiveType)
                        {
                            case TypeCode.Single: case TypeCode.UInt32: case TypeCode.Int32:
                                size = 4;
                                break;
                            case TypeCode.UInt16: case TypeCode.Int16: case TypeCode.Char:
                                size = 2;
                                break;
                            case TypeCode.Byte: case TypeCode.SByte: case TypeCode.Boolean:
                                size = 1;
                                break;
                        }
                    }
                    if (size != -1)
                    {
                        code.NewInstruction(0x12 /* ldc */, null, size);
                        code.StackMap.PushStack(CilType.From(JavaType.IntegerType));
                        return;
                    }
                }
            }
            throw new InvalidProgramException();
        }



        public static void Localloc(JavaCode code)
        {
            // 'localloc' step 1

            // the 'localloc' instruction does not specify the type of the memory
            // to allocate.  in most cases it is followed by a store instruction
            // into a variable of a pointer type, or of type System.Span, but in
            // cases where the result of 'localloc' is passed to a method call,
            // there can be any number of intermediate instruction that push
            // other parameters, and it might not be practial to find the type.

            code.NewInstruction(0x01 /* aconst_null */, null, null);
            code.StackMap.PushStack(CilType.SystemTypeType);

            code.NewInstruction(0xB8 /* invokestatic */, SpanType,
                                new JavaMethodRef("Localloc" + CilMain.EXCLAMATION,
                                    SpanType, JavaType.LongType, CilType.SystemTypeType));

            code.StackMap.PopStack(CilMain.Where);  // null type
            code.StackMap.PopStack(CilMain.Where);  // localloc size
            code.StackMap.PushStack(SpanType);
        }



        public static bool AddOffset(JavaType offsetType, JavaType spanType, JavaCode code)
        {
            if (spanType.Equals(SpanType))
            {
                code.StackMap.PushStack(spanType);

                if (offsetType.Equals(JavaType.IntegerType))
                {
                    code.NewInstruction(0x85 /* i2l */, null, null);
                    offsetType = JavaType.LongType;
                }
                code.StackMap.PushStack(offsetType);

                bool loadedType = false;
                if (spanType is CilType spanType2)
                {
                    if (    (! spanType2.HasGenericParameters)
                         && spanType2.GenericParameters != null
                         && spanType2.GenericParameters[0] is CilType spanPointerType
                         && (! spanPointerType.IsGenericParameter))
                    {
                        GenericUtil.LoadMaybeGeneric(spanPointerType, code);
                        loadedType = true;
                    }
                }
                if (! loadedType)
                {
                    code.NewInstruction(0x01 /* aconst_null */, null, null);
                    code.StackMap.PushStack(CilType.SystemTypeType);
                }

                code.NewInstruction(0xB6 /* invokevirtual */, SpanType,
                                    new JavaMethodRef("Add",
                                            SpanType, offsetType, CilType.SystemTypeType));

                code.StackMap.PopStack(CilMain.Where);  // span type
                code.StackMap.PopStack(CilMain.Where);  // offset

                return true;
            }
            return false;
        }


        public static bool SubOffset(JavaType secondType, JavaCode code)
        {
            // make sure the first operand is a pointer span, not a real Span<T>
            var spanType1 = (CilType) code.StackMap.PopStack(CilMain.Where);
            if (    (! spanType1.HasGenericParameters)
                 && spanType1.GenericParameters != null
                 && spanType1.GenericParameters[0] is CilType span1PointerType
                 && (! span1PointerType.IsGenericParameter))
            {
                var spanType2 = (CilType) secondType;

                // check if subtracting two pointer spans
                if (    (! spanType2.HasGenericParameters)
                     && spanType2.GenericParameters != null
                     && spanType2.GenericParameters[0] is CilType span2PointerType
                     && (! span2PointerType.IsGenericParameter))
                {
                    code.NewInstruction(0xB6 /* invokevirtual */, SpanType,
                                        new JavaMethodRef("Subtract",
                                                JavaType.LongType, spanType2));
                    code.StackMap.PushStack(CilType.From(JavaType.LongType));
                    return true;
                }

                // check if subtracting an offset from a pointer span
                if (spanType2.Equals(JavaType.IntegerType))
                {
                    code.NewInstruction(0xB6 /* invokevirtual */, SpanType,
                                        new JavaMethodRef("Subtract",
                                                SpanType, spanType2));
                    code.StackMap.PushStack(SpanType);
                    return true;
                }
            }

            code.StackMap.PushStack(spanType1);
            return false;
        }



        public static bool Box(CilType intoType, JavaType spanType, JavaCode code)
        {
            if (spanType.Equals(SpanType) && intoType.IsByReference)
            {
                code.NewInstruction(0xB6 /* invokevirtual */, SpanType,
                                    new JavaMethodRef("Box", CilType.SystemValueType));
                code.StackMap.PushStack(CilType.SystemValueType);
                return true;
            }
            return false;
        }



        public static bool Address(CilType fromType, CilType intoType, JavaCode code)
        {
            if (intoType.Equals(SpanType) && (! fromType.Equals(SpanType)))
            {
                // allow assignment of null to clear the pointer
                if (fromType.Equals(JavaStackMap.Null))
                    return true;

                    #if false
                if (    fromType.JavaName == "system.Void.Pointer"
                     && (! intoType.HasGenericParameters)
                     && intoType.GenericParameters != null)
                {
                    code.NewInstruction(0xC0 /* checkcast */, SpanType, null);
                    return true;
                }
                    #endif

                // allow assignment of native int (presumably zero)
                bool callAssign = false;
                bool pushNullType = true;
                JavaType argType = fromType;
                JavaType retType = SpanType;

                if ((! fromType.IsReference) && fromType.PrimitiveType == TypeCode.UInt64)
                    callAssign = true;
                else if (intoType.GenericParameters != null)
                {
                    // allow assignment when the types match
                    callAssign =    intoType.GenericParameters[0].Equals(fromType)
                                 || fromType.JavaName == intoType.GenericParameters[0].JavaName;

                    // for arbitrary value types, call a Assign(ValueType)
                    if (fromType.IsValueClass)
                    {
                        argType = retType = CilType.SystemValueType;
                        GenericUtil.LoadMaybeGeneric(fromType, code);
                        pushNullType = false;
                    }
                }

                if (callAssign)
                {
                    if (pushNullType)
                    {
                        code.NewInstruction(0x01 /* aconst_null */, null, null);
                        code.StackMap.PushStack(CilType.SystemTypeType);
                    }

                    code.NewInstruction(0xB8 /* invokestatic */, SpanType,
                                        new JavaMethodRef("Assign" + CilMain.EXCLAMATION,
                                            retType, argType, CilType.SystemTypeType));

                    code.NewInstruction(0xC0 /* checkcast */, SpanType, null);

                    code.StackMap.PopStack(CilMain.Where);  // type argument
                    return true;
                }

                throw new Exception($"bad assignment of '{fromType.JavaName}' into pointer of '{intoType.GenericParameters[0].JavaName}'");
            }
            return false;
        }



        public static bool AddressArray(CilType elemType, CilType spanType, JavaCode code)
        {
            if (    spanType != null && spanType.Equals(SpanType)
                 && elemType.ArrayRank == 0 && (! elemType.Equals(SpanType)))
            {
                GenericUtil.LoadMaybeGeneric(elemType, code);

                code.NewInstruction(0xB8 /* invokestatic */, SpanType,
                                    new JavaMethodRef("AssignArray" + CilMain.EXCLAMATION,
                                        CilType.SystemValueType, JavaType.ObjectType,
                                            JavaType.IntegerType, CilType.SystemTypeType));

                code.NewInstruction(0xC0 /* checkcast */, SpanType, null);

                code.StackMap.PopStack(CilMain.Where);  // type argument
                code.StackMap.PushStack(spanType);

                return true;
            }
            return false;
        }



        public static bool LoadStore(bool isLoad, CilType stackTop, JavaType opcodeType,
                                     CilType dataType, JavaCode code)
        {
            if (stackTop.Equals(SpanType) && code.Method.Class.Name != SpanType.ClassName)
            {
                string opcodeDescr;
                if (opcodeType == null)
                {
                    opcodeType = CilType.SystemValueType;
                    opcodeDescr = "";

                    // at this point we should have been called from LoadObject or
                    // StoreObject in CodeMisc to handle a ldobj/stobj instruction,
                    // so make sure the pointer-span element is a value type
                    if (dataType.IsGenericParameter || (! dataType.IsValueClass))
                        throw new InvalidProgramException();
                    code.NewInstruction(0x12 /* ldc */, dataType.AsWritableClass, null);

                    // make sure the stack has room for three parameters:
                    // 'this', value reference (in case of Store), and class
                    code.StackMap.PushStack(JavaType.ObjectType);
                    code.StackMap.PushStack(JavaType.ObjectType);
                    code.StackMap.PushStack(JavaType.ObjectType);
                    code.StackMap.PopStack(CilMain.Where);
                    code.StackMap.PopStack(CilMain.Where);
                    code.StackMap.PopStack(CilMain.Where);
                }
                else
                {
                    if (    opcodeType.Equals(JavaType.ShortType)
                         && stackTop.GenericParameters != null
                         && stackTop.GenericParameters[0].Equals(JavaType.CharacterType))
                    {
                        opcodeType = JavaType.CharacterType;
                    }

                    opcodeDescr = opcodeType.ToDescriptor();
                }

                var voidType = JavaType.VoidType;
                var spanMethod = isLoad
                      ? (new JavaMethodRef("Load" + opcodeDescr, opcodeType))
                      : (new JavaMethodRef("Store" + opcodeDescr, voidType, opcodeType));
                if (opcodeDescr == "")
                    spanMethod.Parameters.Add(new JavaFieldRef("", JavaType.ClassType));
                code.NewInstruction(0xB6 /* invokevirtual */, SpanType, spanMethod);
                if (isLoad)
                    code.StackMap.PushStack(CilType.From(opcodeType));

                return true;
            }
            return false;
        }



        public static bool Clear(JavaType stackTop, CilType dataType, JavaCode code)
        {
            if (    stackTop.Equals(SpanType)
                 && dataType.IsValueClass
                 && code.Method.Class.Name != SpanType.ClassName)
            {
                // if initobj is called on a span or pointer, call Span<T>.Clear()
                code.NewInstruction(0xB6 /* invokevirtual */, SpanType,
                                    new JavaMethodRef("Clear", JavaType.VoidType));
                return true;
            }
            return false;
        }



        public static byte CompareEq(JavaType stackTop, JavaType stackTop2,
                                     Mono.Cecil.Cil.Instruction cilInst, JavaCode code)
        {
            if (    stackTop.Equals(SpanType)
                 && (    stackTop2.PrimitiveType == TypeCode.Int64
                      || stackTop2.PrimitiveType == TypeCode.UInt64))
            {
                // compare Span with long
                throw new InvalidProgramException();
            }

            if (    stackTop2.Equals(SpanType)
                 && (    stackTop.PrimitiveType == TypeCode.Int64
                      || stackTop.PrimitiveType == TypeCode.UInt64))
            {
                if (    cilInst.Previous == null
                     || cilInst.Previous.OpCode.Code != Code.Conv_U
                     || cilInst.Previous.Previous == null
                     || cilInst.Previous.Previous.OpCode.Code != Code.Ldc_I4_0)
                {
                    // make sure the program is comparing the span address against
                    // a zero value, which we can convert to a null reference.
                    //      ldarg.1 (span argument)
                    //      ldc.i4.0
                    //      conv.u
                    //      bne.un label
                    throw new InvalidProgramException();
                }
                // compare long with Span
                code.NewInstruction(0x58 /* pop2 */, null, null);
                code.NewInstruction(0x01 /* aconst_null */, null, null);
            }

            else if (CompareGtLt(stackTop, stackTop2, code))
            {
                return 0x99; // ifeq == zero
            }

            return 0;
        }



        public static bool CompareGtLt(JavaType stackTop, JavaType stackTop2, JavaCode code)
        {
            if (stackTop.Equals(SpanType) && stackTop2.Equals(SpanType))
            {
                code.NewInstruction(0x01 /* aconst_null */, null, null);
                code.StackMap.PushStack(CilType.SystemTypeType);

                code.NewInstruction(0xB8 /* invokestatic */, SpanType,
                                    new JavaMethodRef("CompareTo" + CilMain.EXCLAMATION,
                                        JavaType.IntegerType, SpanType, SpanType, CilType.SystemTypeType));

                code.StackMap.PopStack(CilMain.Where);  // null type

                return true;
            }
            return false;
        }



        public static bool ExplicitCast(MethodReference methodRef, JavaCode code)
        {
            if (    methodRef.HasThis || methodRef.Name != "op_Explicit"
                 || methodRef.Parameters.Count != 1)
            {
                // we only handle op_Explicit methods which have one parameter
                return false;
            }

            var stackTop = code.StackMap.PopStack(CilMain.Where);

            if (    stackTop.Equals(SpanType)
                 && methodRef.ReturnType.IsPrimitive
                 && methodRef.ReturnType.Name == "IntPtr"
                 && methodRef.Parameters[0].ParameterType.Name == "Void*")
            {
                // translate call from
                //      System.IntPtr System.IntPtr::op_Explicit(System.Void*)
                // to - long system.runtime.compilerservices.VoidHelpers

                code.NewInstruction(0xB8 /* invokestatic */,
                        new JavaType(0, 0, "system.runtime.compilerservices.VoidHelper"),
                        new JavaMethodRef("SpanToIntPtr",
                                          JavaType.LongType, CilType.SystemValueType));

                code.StackMap.PushStack(CilType.From(JavaType.LongType));
                return true;
            }

            if (    stackTop.Equals(JavaType.LongType)
                 && methodRef.ReturnType.IsPointer
                 && methodRef.ReturnType.Name == "Void*"
                 && methodRef.Parameters[0].ParameterType.Name == "IntPtr")
            {
                // translate call from
                //      System.IntPtr System.IntPtr::op_Explicit(System.Void*)
                // to - long system.runtime.compilerservices.VoidHelpers

                code.NewInstruction(0xB8 /* invokestatic */,
                        new JavaType(0, 0, "system.runtime.compilerservices.VoidHelper"),
                        new JavaMethodRef("IntPtrToSpan",
                                          CilType.SystemValueType, JavaType.LongType));

                code.StackMap.PushStack(SpanType);
                return true;
            }

            if (    stackTop.Equals(JavaType.LongType)
                 && methodRef.ReturnType.IsPrimitive
                 && (    (    methodRef.ReturnType.Name == "Int64"
                           && methodRef.Parameters[0].ParameterType.Name == "IntPtr")
                      || (    methodRef.ReturnType.Name == "IntPtr"
                           && methodRef.Parameters[0].ParameterType.Name == "Int64")))
            {
                // translate call from
                //      long System.IntPtr::op_Explicit(System.IntPtr)
                //      IntPtr System.IntPtr::op_Explicit(long)
                // to - nop

                code.NewInstruction(0x00 /* nop */, null, null);
                code.StackMap.PushStack(stackTop);
                return true;
            }

            code.StackMap.PushStack(stackTop);
            return false;
        }



        internal static readonly CilType SpanType =
                                            CilType.From(new JavaType(0, 0, "system.Span$$1"));
    }

}
