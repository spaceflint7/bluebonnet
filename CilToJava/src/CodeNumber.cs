
using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public static class CodeNumber
    {

        public static void Conversion(JavaCode code, Code cilOp)
        {
            var oldType = (CilType) code.StackMap.PopStack(CilMain.Where);
            var (newType, overflow, unsigned) = ConvertOpCodeToTypeCode(cilOp);
            int op;

            if (oldType.IsReference)
            {
                ConvertReference(code, oldType, newType);
                return;
            }

            if (newType == TypeCode.Single || newType == TypeCode.Double)
                op = ConvertToFloat(code, oldType.PrimitiveType, newType, unsigned);

            else if (newType == TypeCode.Int64 || newType == TypeCode.UInt64)
                op = ConvertToLong(code, oldType.PrimitiveType, newType, overflow);

            else
                op = ConvertToInteger(code, oldType.PrimitiveType, newType, overflow);

            if (op != -1)
                code.NewInstruction((byte) op, null, null);

            code.StackMap.PushStack(CilType.From(new JavaType(newType, 0, null)));
        }



        static void ConvertReference(JavaCode code, JavaType oldType, TypeCode newType)
        {
            if (newType == TypeCode.Int64 || newType == TypeCode.UInt64)
            {
                // in CIL code, it is typical to see a load address instruction
                // followed by 'conv.u'.  we just ignore and keep the original type
                code.NewInstruction(0x00 /* nop */, null, null);
                code.StackMap.PushStack(oldType);
            }
            else
                throw new InvalidProgramException();
        }



        static int ConvertToFloat(JavaCode code, TypeCode oldType, TypeCode newType, bool unsigned)
        {
            if (newType == TypeCode.Single)
            {
                //
                // convert to float
                //

                if (oldType == TypeCode.Single)
                    return 0x00; // nop

                if (oldType == TypeCode.Double)
                    return 0x90; // d2f

                if (oldType == TypeCode.Int64)
                    return 0x89; // l2f

                if (oldType != TypeCode.UInt64)
                    return 0x86; // i2f

                CallUnsignedLongToDouble(code);
                code.NewInstruction(0x8D /* d2f */, null, null);
                return -1; // no output
            }

            else if (! unsigned) // && (newType == TypeCode.Double)
            {
                //
                // convert to double
                //

                if (oldType == TypeCode.Double)
                    return 0x00; // nop

                if (oldType == TypeCode.Single)
                    return 0x8D; // f2d

                if (oldType == TypeCode.Int64)
                    return 0x8A; // l2d

                if (oldType != TypeCode.UInt64)
                    return 0x87; // i2d

                CallUnsignedLongToDouble(code);
                return -1; // no output
            }

            else
            {
                //
                // convert integer, interpreted as unsigned, to a double
                //

                if (oldType == TypeCode.Single || oldType == TypeCode.Double)
                    throw new InvalidProgramException();

                bool fromInt32 = (oldType != TypeCode.Int64 && oldType != TypeCode.UInt64);
                if (fromInt32)
                {
                    code.NewInstruction(0x85 /* i2l */, null, null);
                    code.StackMap.PushStack(JavaType.LongType);

                    if (oldType == TypeCode.UInt32)
                    {
                        code.NewInstruction(0x12 /* ldc */, null, (long) 0xFFFFFFFF);
                        code.StackMap.PushStack(JavaType.LongType);
                        code.NewInstruction(0x7F /* land */, null, null);
                        code.StackMap.PopStack(CilMain.Where);
                    }
                }

                CallUnsignedLongToDouble(code);

                if (fromInt32)
                    code.StackMap.PopStack(CilMain.Where);

                return -1; // no output
            }

            void CallUnsignedLongToDouble(JavaCode code)
            {
                code.NewInstruction(0xB8 /* invokestatic */,
                                    CilType.From(JavaType.DoubleType).AsWritableClass,
                                    new JavaMethodRef("UnsignedLongToDouble",
                                            JavaType.DoubleType, JavaType.LongType));
            }

        }



        static int ConvertToLong(JavaCode code, TypeCode oldType, TypeCode newType, bool overflow)
        {
            if (oldType == TypeCode.Int64 || oldType == TypeCode.UInt64)
                return 0x00; // nop

            if (oldType == TypeCode.Double)
                return 0x8F; // d2l

            if (oldType == TypeCode.Single)
                return 0x8C; // f2l

            if (    newType == TypeCode.UInt64)
            {
                code.NewInstruction(0x85 /* i2l */, null, null);
                code.StackMap.PushStack(JavaType.LongType);
                #if true
                long maskValue = (oldType == TypeCode.Byte) ? 0xFF
                               : (oldType == TypeCode.UInt16) ? 0xFFFF
                               : (oldType == TypeCode.UInt32) ? 0xFFFFFFFF
                               : 0;
                code.NewInstruction(0x12 /* ldc */, null, (long) 0xFFFFFFFF);
                #else
                code.NewInstruction(0x02 /* iconst_m1 */, null, null);
                code.NewInstruction(0x85 /* i2l */, null, null);
                #endif
                code.StackMap.PushStack(JavaType.LongType);
                code.StackMap.PopStack(CilMain.Where);
                code.StackMap.PopStack(CilMain.Where);
                return 0x7F; // land
            }

            return 0x85; // i2l
        }



        static int ConvertToInteger(JavaCode code, TypeCode oldType, TypeCode newType, bool overflow)
        {
            if (oldType == TypeCode.Double)
            {
                if (newType == TypeCode.Int32 || oldType == TypeCode.UInt32)
                    return 0x8E; // d2i
                code.NewInstruction(0x8E /* d2i */, null, null);
            }

            if (oldType == TypeCode.Single)
            {
                if (newType == TypeCode.Int32 || oldType == TypeCode.UInt32)
                    return 0x8B; // f2i
                code.NewInstruction(0x8B /* f2i */, null, null);
            }

            if (oldType == TypeCode.Int64 || oldType == TypeCode.UInt64)
            {
                if (newType == TypeCode.Int32 || oldType == TypeCode.UInt32)
                    return 0x88; // l2i
                code.NewInstruction(0x88 /* l2i */, null, null);
            }

            if (newType == TypeCode.SByte)
                return 0x91; // i2b

            if (newType == TypeCode.Byte)
            {
                code.StackMap.PushStack(JavaType.IntegerType);
                code.NewInstruction(0x12 /* ldc */, null, (int) 0xFF);
                code.StackMap.PushStack(JavaType.IntegerType);
                code.StackMap.PopStack(CilMain.Where);
                code.StackMap.PopStack(CilMain.Where);
                return 0x7E; // iand
            }

            if (newType == TypeCode.Int16)
                return 0x93; // i2s

            if (newType == TypeCode.UInt16)
                return 0x92; // i2c

            return 0x00; // nop
        }



        static (TypeCode, bool, bool) ConvertOpCodeToTypeCode(Code cilOp)
        {
            switch (cilOp)
            {
                case Code.Conv_I1:          return (TypeCode.SByte, false, false);
                case Code.Conv_Ovf_I1:      return (TypeCode.SByte, true, false);
                case Code.Conv_Ovf_I1_Un:   return (TypeCode.SByte, true, true);

                case Code.Conv_U1:          return (TypeCode.Byte, false, false);
                case Code.Conv_Ovf_U1:      return (TypeCode.Byte, true, false);
                case Code.Conv_Ovf_U1_Un:   return (TypeCode.Byte, true, true);

                case Code.Conv_I2:          return (TypeCode.Int16, false, false);
                case Code.Conv_Ovf_I2:      return (TypeCode.Int16, true, false);
                case Code.Conv_Ovf_I2_Un:   return (TypeCode.Int16, true, true);

                case Code.Conv_U2:          return (TypeCode.UInt16, false, false);
                case Code.Conv_Ovf_U2:      return (TypeCode.UInt16, true, false);
                case Code.Conv_Ovf_U2_Un:   return (TypeCode.UInt16, true, true);

                case Code.Conv_I4:          return (TypeCode.Int32, false, false);
                case Code.Conv_Ovf_I4:      return (TypeCode.Int32, true, false);
                case Code.Conv_Ovf_I4_Un:   return (TypeCode.Int32, true, true);

                case Code.Conv_U4:          return (TypeCode.UInt32, false, false);
                case Code.Conv_Ovf_U4:      return (TypeCode.UInt32, true, false);
                case Code.Conv_Ovf_U4_Un:   return (TypeCode.UInt32, true, true);

                case Code.Conv_I8:          return (TypeCode.Int64, false, false);
                case Code.Conv_Ovf_I8:      return (TypeCode.Int64, true, false);
                case Code.Conv_Ovf_I8_Un:   return (TypeCode.Int64, true, true);

                case Code.Conv_I:           return (TypeCode.Int64, false, false);
                case Code.Conv_Ovf_I:       return (TypeCode.Int64, true, false);
                case Code.Conv_Ovf_I_Un:    return (TypeCode.Int64, true, true);

                case Code.Conv_U:           return (TypeCode.UInt64, false, false);
                case Code.Conv_Ovf_U:       return (TypeCode.UInt64, true, false);
                case Code.Conv_Ovf_U_Un:    return (TypeCode.UInt64, true, true);

                case Code.Conv_U8:          return (TypeCode.UInt64, false, true);
                case Code.Conv_Ovf_U8:      return (TypeCode.UInt64, true, true);
                case Code.Conv_Ovf_U8_Un:   return (TypeCode.UInt64, true, true);

                case Code.Conv_R4:          return (TypeCode.Single, false, false);
                case Code.Conv_R8:          return (TypeCode.Double, false, false);
                case Code.Conv_R_Un:        return (TypeCode.Double, false, true);

                default:                    throw new InvalidProgramException();
            }
        }



        public static void Calculation(JavaCode code, Code cilOp)
        {
            var stackTop1 = code.StackMap.PopStack(CilMain.Where);
            var type1 = GetNumericTypeCode(stackTop1);

            if (cilOp == Code.Not)
            {
                BitwiseNot(code, type1);
                return;
            }

            byte op;
            if (cilOp == Code.Neg)
                op = 0x74; // ineg

            else
            {
                var stackTop2 = code.StackMap.PopStack(CilMain.Where);
                if (CodeSpan.AddOffset(stackTop1, stackTop2, code))
                    return;

                char kind;
                var type2 = type1;
                type1 = GetNumericTypeCode(stackTop2);

                switch (cilOp)
                {
                    case Code.Add:      op = 0x60; kind = 'A'; break;   // iadd
                    case Code.Sub:      op = 0x64; kind = 'A'; break;   // isub
                    case Code.Mul:      op = 0x68; kind = 'A'; break;   // imul
                    case Code.Div:      op = 0x6C; kind = 'A'; break;   // idiv
                    case Code.Rem:      op = 0x70; kind = 'A'; break;   // irem

                    case Code.And:      op = 0x7E; kind = 'L'; break;   // iand
                    case Code.Or:       op = 0x80; kind = 'L'; break;   // ior
                    case Code.Xor:      op = 0x82; kind = 'L'; break;   // ixor

                    case Code.Shl:      op = 0x78; kind = 'S'; break;   // ishl
                    case Code.Shr:      op = 0x7A; kind = 'S'; break;   // ishr
                    case Code.Shr_Un:   op = 0x7C; kind = 'S'; break;   // iushr

                    case Code.Div_Un:  case Code.Rem_Un:
                                        op = 0x00; kind = 'U'; break;

                    case Code.Add_Ovf:    case Code.Sub_Ovf:    case Code.Mul_Ovf:
                    case Code.Add_Ovf_Un: case Code.Sub_Ovf_Un: case Code.Mul_Ovf_Un:
                                        op = 0x00; kind = 'A'; break;

                    default:            throw new InvalidProgramException();
                }

                bool ok = true;
                if (kind == 'S')
                {
                    // second operand must be integer shift count
                    if (type2 != TypeCode.Int32)
                        ok = false;
                }
                else
                {
                    // logical and arithmetic operands must be same type
                    if (type1 != type2)
                    {
                        if (kind == 'A' && type1 == TypeCode.Int64 && type2 == TypeCode.Int32)
                        {
                            // special case:  convert the second operand to Int64
                            code.NewInstruction(0x85 /* i2l */, null, null);
                            code.StackMap.PushStack(JavaType.LongType);
                            code.StackMap.PushStack(JavaType.LongType);
                            code.StackMap.PopStack(CilMain.Where);
                            code.StackMap.PopStack(CilMain.Where);
                        }
                        else if (kind == 'A' && type1 == TypeCode.Int32 && type2 == TypeCode.Int64)
                        {
                            // special case:  convert the second operand to Int32
                            code.NewInstruction(0x88 /* l2i */, null, null);
                        }
                        else
                            ok = false;
                    }

                    if (kind == 'L')
                    {
                        // logical operation requires integer operands
                        if (type1 != TypeCode.Int32 && type1 != TypeCode.Int64)
                            ok = false;
                    }
                }

                if (! ok)
                {
                    throw new Exception($"unexpected opcode or operands ({type1} and {type2})");
                }

                if (kind == 'A' && op == 0)
                {
                    OverflowArithmetic(code, type1, cilOp);
                    return;
                }

                if (kind == 'U')
                {
                    UnsignedDivide(code, type1, (cilOp == Code.Rem_Un));
                    return;
                }
            }

            if (type1 == TypeCode.Int64)
                op++;       // ixxx -> lxxx
            else if (type1 == TypeCode.Single)
                op += 2;    // ixxx -> fxxx
            else if (type1 == TypeCode.Double)
                op += 3;    // ixxx -> dxxx

            code.NewInstruction(op, null, null);
            code.StackMap.PushStack(CilType.From(new JavaType(type1, 0, null)));
        }



        static void BitwiseNot(JavaCode code, TypeCode typeCode)
        {
            var jtype = new JavaType(typeCode, 0, null);
            code.StackMap.PushStack(CilType.From(jtype));
            code.StackMap.PushStack(jtype);

            byte op = 0x82; // ixor
            code.NewInstruction(0x02 /* iconst_m1 */, null, null);
            if (typeCode == TypeCode.Int64)
            {
                code.NewInstruction(0x85 /* i2l */, null, null);
                op++; // lxor
            }
            else if (typeCode != TypeCode.Int32)
                throw new InvalidProgramException();

            code.NewInstruction(op, null, null);
            code.StackMap.PopStack(CilMain.Where);
        }



        static void UnsignedDivide(JavaCode code, TypeCode typeCode, bool remainder)
        {
            TypeCode unsignedTypeCode;
            if (typeCode == TypeCode.Int32)
                unsignedTypeCode = TypeCode.UInt32;
            else if (typeCode == TypeCode.Int64)
                unsignedTypeCode = TypeCode.UInt64;
            else
                throw new InvalidProgramException();

            var signedType = CilType.From(new JavaType(typeCode, 0, null));
            code.StackMap.PushStack(signedType);

            code.NewInstruction(0xB8 /* invokestatic */,
                                CilType.From(new JavaType(unsignedTypeCode, 0, null)).AsWritableClass,
                                new JavaMethodRef("Unsigned" + (remainder ? "Remainder" : "Division"),
                                        signedType, signedType, signedType));
        }



        static void OverflowArithmetic(JavaCode code, TypeCode typeCode, Code cilOp)
        {
            bool unsigned = (    cilOp == Code.Add_Ovf_Un
                              || cilOp == Code.Sub_Ovf_Un
                              || cilOp == Code.Mul_Ovf_Un);
            TypeCode callTypeCode;
            if (typeCode == TypeCode.Int32)
                callTypeCode = unsigned ? TypeCode.UInt32 : typeCode;
            else if (typeCode == TypeCode.Int64)
                callTypeCode = unsigned ? TypeCode.UInt64 : typeCode;
            else
                throw new InvalidProgramException();

            var signedType = CilType.From(new JavaType(typeCode, 0, null));
            code.StackMap.PushStack(signedType);

            string verb = (cilOp == Code.Add_Ovf || cilOp == Code.Add_Ovf_Un) ? "Add"
                        : (cilOp == Code.Sub_Ovf || cilOp == Code.Sub_Ovf_Un) ? "Subtract"
                        : (cilOp == Code.Mul_Ovf || cilOp == Code.Mul_Ovf_Un) ? "Multiply"
                        : throw new InvalidProgramException();

            code.NewInstruction(0xB8 /* invokestatic */,
                                CilType.From(new JavaType(callTypeCode, 0, null)).AsWritableClass,
                                new JavaMethodRef("Overflow" + verb,
                                        signedType, signedType, signedType));
        }



        static TypeCode GetNumericTypeCode(JavaType type)
        {
            if (type.IsReference)
            {
                string boxed = (type is BoxedType) ? "boxed " : "";
                throw new ArgumentException($"{boxed}type '{type}' is not compatible with numeric operation");
            }

            switch (type.PrimitiveType)
            {
                case TypeCode.Int32: case TypeCode.UInt32:
                case TypeCode.Int16: case TypeCode.UInt16: case TypeCode.Char:
                case TypeCode.SByte: case TypeCode.Byte: case TypeCode.Boolean:
                    return TypeCode.Int32;

                case TypeCode.Int64: case TypeCode.UInt64:
                    return TypeCode.Int64;

                case TypeCode.Single: case TypeCode.Double:
                    return type.PrimitiveType;

                default:
                    throw new InvalidProgramException();
            }
        }



        public static void Indirection(JavaCode code, Code cilOp)
        {
            var (name, opcodeType) = IndirectOpCodeToNameAndType(cilOp);
            bool isRef = opcodeType.Equals(JavaType.ObjectType);

            bool isLoad;
            if (cilOp >= Code.Ldind_I1 && cilOp <= Code.Ldind_Ref)
                isLoad = true;
            else
            {
                isLoad = false;
                var valueType = code.StackMap.PopStack(CilMain.Where);
                if (valueType.IsReference != isRef)
                    throw new InvalidProgramException();
            }

            var stackTop = (CilType) code.StackMap.PopStack(CilMain.Where);

            if (stackTop.IsGenericParameter)
            {
                if (isLoad)
                {
                    var resultType = GenericUtil.CastMaybeGeneric(stackTop, false, code);
                    if (resultType == stackTop && (! stackTop.Equals(JavaType.ObjectType)))
                    {
                        code.NewInstruction(0xC0 /* checkcast */, stackTop.AsWritableClass, null);
                        resultType = stackTop;
                    }
                    code.StackMap.PushStack(resultType);
                }
                else
                {
                    code.NewInstruction(0x5F /* swap */, null, null);
                    GenericUtil.ValueCopy(stackTop, code);
                }
                return;
            }

            //
            // non-generic object reference
            //

            var boxedType = stackTop as BoxedType;
            if (boxedType == null || boxedType.IsBoxedReference != isRef)
            {
                if (CodeSpan.LoadStore(isLoad, stackTop, opcodeType, code))
                    return;

                if (object.ReferenceEquals(stackTop, CodeArrays.GenericArrayType))
                {
                    // a byref parameter T[] gets translated to java.lang.Object,
                    // so we have to explicitly cast it to system.Reference
                    boxedType = new BoxedType(stackTop, false);
                    code.NewInstruction(0x5F /* swap */, null, null);
                    code.NewInstruction(0xC0 /* checkcast */,
                                        boxedType.AsWritableClass, null);
                    code.NewInstruction(0x5F /* swap */, null, null);
                }
                else
                    throw new ArgumentException($"incompatible type '{stackTop}'");
            }

            var unboxedType = boxedType.UnboxedType;
            var unboxedTypeCode = unboxedType.IsReference ? 0 : unboxedType.PrimitiveType;

            JavaMethodRef method;
            if (CompareIndirectTypes(unboxedTypeCode, opcodeType.PrimitiveType))
            {
                // indirect access to a primitive or reference type, with a
                // reference type that represents the boxed form of the same type.

                if (isLoad)
                {
                    boxedType.GetValue(code);
                    if (unboxedType.IsReference)
                    {
                        // if we know the type of indirected value, cast to it
                        if (! unboxedType.Equals(JavaType.ObjectType))
                        {
                            code.NewInstruction(0xC0 /* checkcast */,
                                                unboxedType.AsWritableClass, null);
                        }
                    }
                    else
                        unboxedType = CilType.From(opcodeType);
                    code.StackMap.PushStack(unboxedType);
                }
                else
                    boxedType.SetValueOV(code);
                return;
            }

            // indirect access to a primitive value from a reference type that
            // represents some other a primitive value;  for example ldind.r4
            // from a system.Int32.  we call the "CodeNumber.Indirection methods"
            // helpers, defined in all baselib primitives, to assist.

            if (opcodeType.IsIntLike)
                opcodeType = JavaType.IntegerType;
            if (isLoad)
            {
                method = new JavaMethodRef("Get_" + name, opcodeType);
                code.StackMap.PushStack(CilType.From(opcodeType));
            }
            else
                method = new JavaMethodRef("Set_" + name, JavaType.VoidType, opcodeType);
            code.NewInstruction(0xB6 /* invokevirtual */, boxedType, method);
        }



        static (string, JavaType) IndirectOpCodeToNameAndType(Code cilOp)
        {
            switch (cilOp)
            {
                case Code.Ldind_Ref: case Code.Stind_Ref:
                    return ("Reference", JavaType.ObjectType);

                case Code.Ldind_I1: case Code.Stind_I1:
                    return ("I8", new JavaType(TypeCode.SByte, 0, null));
                case Code.Ldind_U1:
                    return ("U8", new JavaType(TypeCode.Byte, 0, null));

                case Code.Ldind_I2: case Code.Stind_I2:
                    return ("I16", new JavaType(TypeCode.Int16, 0, null));
                case Code.Ldind_U2:
                    return ("U16", new JavaType(TypeCode.UInt16, 0, null));

                case Code.Ldind_I4: case Code.Ldind_U4: case Code.Stind_I4:
                    return ("I32", JavaType.IntegerType);

                case Code.Ldind_I8: case Code.Ldind_I: case Code.Stind_I8: case Code.Stind_I:
                    return ("I64", JavaType.LongType);

                case Code.Ldind_R4: case Code.Stind_R4:
                    return ("F32", JavaType.FloatType);

                case Code.Ldind_R8: case Code.Stind_R8:
                    return ("F64", JavaType.DoubleType);

                default:
                    throw new InvalidProgramException();
            }
        }



        static bool CompareIndirectTypes(TypeCode boxedType, TypeCode opcodeType)
        {
            if (boxedType == opcodeType)
                return true;

            if (boxedType == TypeCode.UInt64)
                return (opcodeType == TypeCode.Int64);

            if (boxedType == TypeCode.UInt32)
                return (opcodeType == TypeCode.Int32);

            return false;
        }

    }

}
