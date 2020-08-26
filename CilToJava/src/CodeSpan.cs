
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
                    code.StackMap.PushStack(JavaType.IntegerType);
                    return;
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
            if (intoType.Equals(CodeSpan.SpanType) && (! fromType.Equals(CodeSpan.SpanType)))
            {
                if (    fromType.Equals(JavaType.StringType)
                     && intoType.GenericParameters != null
                     && intoType.GenericParameters[0].Equals(fromType))
                {
                    code.NewInstruction(0x01 /* aconst_null */, null, null);
                    code.StackMap.PushStack(CilType.SystemTypeType);

                    code.NewInstruction(0xB8 /* invokestatic */, SpanType,
                                        new JavaMethodRef("String" + CilMain.EXCLAMATION,
                                            SpanType, JavaType.StringType, CilType.SystemTypeType));

                    code.StackMap.PopStack(CilMain.Where);  // null type
                    return true;
                }

                if (fromType.Equals(JavaStackMap.Null))
                    return true;

                throw new Exception($"bad assignment of '{fromType.JavaName}' into pointer");
            }
            return false;
        }



        public static bool LoadStore(bool isLoad, CilType stackTop, JavaType opcodeType, JavaCode code)
        {
            if (stackTop.Equals(SpanType) && code.Method.Class.Name != SpanType.ClassName)
            {
                string opcodeDescr;
                if (opcodeType == null)
                {
                    opcodeType = JavaType.ObjectType;
                    opcodeDescr = "";
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
                code.NewInstruction(0xB6 /* invokevirtual */, SpanType, spanMethod);
                if (isLoad)
                    code.StackMap.PushStack(CilType.From(opcodeType));

                return true;
            }
            return false;
        }



        public static void Compare(JavaType stackTop, JavaType stackTop2,
                                   Mono.Cecil.Cil.Instruction cilInst, JavaCode code)
        {
            if (    stackTop.Equals(CodeSpan.SpanType)
                 && (    stackTop2.PrimitiveType == TypeCode.Int64
                      || stackTop2.PrimitiveType == TypeCode.UInt64))
            {
                // compare Span with long
                throw new InvalidProgramException();
            }

            if (    stackTop2.Equals(CodeSpan.SpanType)
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
        }



        internal static readonly CilType SpanType =
                                            CilType.From(new JavaType(0, 0, "system.Span$$1"));
    }

}
