
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public static class CodeCompare
    {

        public static void Straight(JavaCode code, Code cilOp, CodeLocals locals,
                                    Mono.Cecil.Cil.Instruction cilInst)
        {
            //
            // a straight branch maps to the corresponding compare logic
            // (beq = ceq, bgt = cgt, blt = clt), plus logic for brtrue.
            //

            byte op;
            ushort nextInstOffset = 0;

            if (cilOp == Code.Br || cilOp == Code.Br_S)
            {
                if (cilInst.Operand is Mono.Cecil.Cil.Instruction inst)
                {
                    if (inst == cilInst.Next)
                        op = 0x00; // nop
                    else
                    {
                        op = 0xA7; // goto
                        if (cilInst.Next != null)
                            nextInstOffset = (ushort) cilInst.Next.Offset;
                    }
                }
                else
                    throw new InvalidProgramException();
            }
            else
            {
                var stackTop = code.StackMap.PopStack(CilMain.Where);
                op = Common(code, cilOp, cilInst, stackTop);
            }

            Finish(code, locals, cilInst, op);

            if (nextInstOffset != 0)
            {
                if (! code.StackMap.LoadFrame(nextInstOffset, true, null))
                {
                    if (op == 0xA7 /* goto */)
                    {
                        locals.TrackUnconditionalBranch(cilInst);
                    }
                }
            }
        }



        public static void Opposite(JavaCode code, Code cilOp, CodeLocals locals,
                                    Mono.Cecil.Cil.Instruction cilInst)
        {
            //
            // an opposite branch works by generating logic/opcode for
            // the opposite test (e.g. clt for bge), and an opposite
            // branch opcode (e.g. for bge, branch if clt returns zero)
            //

            var stackTop = code.StackMap.PopStack(CilMain.Where);

            bool isFloat = (    stackTop.PrimitiveType == TypeCode.Single
                             || stackTop.PrimitiveType == TypeCode.Double);

            if (cilOp == Code.Brfalse || cilOp == Code.Brfalse_S)
            {
                cilOp = Code.Brtrue;
            }
            else if (cilOp == Code.Bne_Un || cilOp == Code.Bne_Un_S)
            {
                cilOp = Code.Beq;
            }
            else if (cilOp == Code.Ble || cilOp == Code.Ble_S)
            {
                // for integer, this is the opposite of BGT
                // for float, this is the opposite of BGT.UN
                cilOp = isFloat ? Code.Bgt_Un : Code.Bgt;
            }
            else if (cilOp == Code.Ble_Un || cilOp == Code.Ble_Un_S)
            {
                // for integer, this is the opposite of BGT.UN
                // for float, this is the opposite of BGT
                cilOp = isFloat ? Code.Bgt : Code.Bgt_Un;
            }
            else if (cilOp == Code.Bge || cilOp == Code.Bge_S)
            {
                // for integer, this is the opposite of BLT
                // for float, this is the opposite of BLT.UN
                cilOp = isFloat ? Code.Blt_Un : Code.Blt;
            }
            else if (cilOp == Code.Bge_Un || cilOp == Code.Bge_Un_S)
            {
                // for integer, this is the opposite of BLT.UN
                // for float, this is the opposite of BLT
                cilOp = isFloat ? Code.Blt : Code.Blt_Un;
            }
            else
                throw new InvalidProgramException();

            //
            // change the branch test to the opposite result
            //

            byte op = Common(code, cilOp, cilInst, stackTop);

            op = NegateCondition(op);

            Finish(code, locals, cilInst, op);
        }



        static void Finish(JavaCode code, CodeLocals locals,
                           Mono.Cecil.Cil.Instruction cilInst, byte op)
        {
            var inst = (Mono.Cecil.Cil.Instruction) cilInst.Operand;

            bool isNop;
            if (op == 0x00 || op == 0x57 || op == 0x58) // nop, pop, pop2
                isNop = true;
            else
            {
                isNop = false;
                int diff = cilInst.Offset - inst.Offset;
                if (diff < -0x2000 || diff > 0x2000)
                {
                    // branch instructions use 16-bits for the signed offset.
                    // for farther branches, we have to negate the condition,
                    // and insert a 32-bit 'goto_w' instruction.

                    if (cilInst.Next != null)
                    {
                        op = NegateCondition(op);
                        var nextOffset = (ushort) cilInst.Next.Offset;
                        code.NewInstruction(op, null, nextOffset);
                        code.StackMap.SaveFrame(nextOffset, true, CilMain.Where);
                    }

                    op = 0xC8;
                }
            }

            code.NewInstruction(op, null, (ushort) inst.Offset);

            if (! isNop) // no stack frame for 'nop' or 'pop'
            {
                var resetLocals =
                        code.StackMap.SaveFrame((ushort) inst.Offset, true, CilMain.Where);

                if (resetLocals != null)
                {
                    ResetLocalsOutOfScope(code.StackMap, locals, resetLocals, cilInst, inst);
                }
            }
        }



        static byte NegateCondition(byte op)
        {
            switch (op)
            {
                case 0x99:  return 0x9A;    // ifeq == zero  -->  ifne != zero
                case 0x9A:  return 0x99;    // ifne != zero  -->  ifeq == zero

                case 0x9B:  return 0x9C;    // iflt < zero   -->  ifge >= zero
                case 0x9C:  return 0x9B;    // ifge >= zero  -->  iflt < zero

                case 0x9D:  return 0x9E;    // ifgt > zero   -->  ifle <= zero
                case 0x9E:  return 0x9D;    // ifle <= zero  -->  ifgt > zero

                case 0x9F:  return 0xA0;    // if_icmpeq ==  -->  if_icmpne !=
                case 0xA0:  return 0x9F;    // if_icmpne !=  -->  if_icmpeq ==

                case 0xA1:  return 0xA2;    // if_icmplt <   -->  if_icmpge >=
                case 0xA2:  return 0xA1;    // if_icmpge >=  -->  if_icmplt <

                case 0xA3:  return 0xA4;    // if_icmpgt >   -->  if_icmple <=
                case 0xA4:  return 0xA3;    // if_icmple <=  -->  if_icmpgt >

                case 0xA5:  return 0xA6;    // if_acmpeq ==  -->  if_acmpne !=
                case 0xA6:  return 0xA5;    // if_acmpne !=  -->  if_acmpeq ==

                case 0xC6:  return 0xC7;    // ifnull        -->  ifnonnull
                case 0xC7:  return 0xC6;    // ifnonnull     -->  ifnull

                case 0x00:                  // nop remains nop
                case 0x57:                  // pop remains pop
                case 0x58:                  // pop2 remains pop2
                case 0xA7:                  // goto remains goto
                    return op;

                default:
                    throw new InvalidProgramException();
            }
        }



        static byte Common(JavaCode code, Code cilOp, Mono.Cecil.Cil.Instruction cilInst, JavaType stackTop)
        {
            if (cilInst.Operand is Mono.Cecil.Cil.Instruction inst)
            {
                bool branchToNext = (inst == cilInst.Next);

                if (cilOp == Code.Brtrue || cilOp == Code.Brtrue_S)
                {
                    if (branchToNext)
                        return PopOpCode(stackTop);
                    else
                        return TestBool(code, stackTop);
                }

                // all other conditionals pop a second value off the stack
                var stackTop2 = code.StackMap.PopStack(CilMain.Where);
                if (branchToNext)
                {
                    code.NewInstruction(PopOpCode(stackTop2), null, null);
                    return PopOpCode(stackTop);
                }

                if (cilOp == Code.Beq || cilOp == Code.Beq_S)
                {
                    return TestEq(code, stackTop, stackTop2, cilInst);
                }
                else
                {
                    return TestGtLt(code, stackTop,
             /* if greater than */ (    cilOp == Code.Bgt
              /* (vs less than) */   || cilOp == Code.Bgt_S
                                     || cilOp == Code.Bgt_Un
                                     || cilOp == Code.Bgt_Un_S),
                 /* if unsigned */ (    cilOp == Code.Bgt_Un
                /* or unordered */   || cilOp == Code.Bgt_Un_S
                                     || cilOp == Code.Blt_Un
                                     || cilOp == Code.Blt_Un_S));
                }
            }
            else
                throw new InvalidProgramException();

            byte PopOpCode(JavaType forType) => (byte) (0x56 + forType.Category);
        }



        public static void Compare(JavaCode code, Code cilOp, Mono.Cecil.Cil.Instruction cilInst)
        {
            //
            // each cil comparison instruction checks one relation (equals,
            // grater than, or less than) and pushes integer 0 or 1.
            // in contrast, jvm compares two values and returns -1, 0, or 1.
            // we simulate ceq/cgt/clt as a compare and branch sequence:
            //
            // call to System.UInt{16,32}.CompareTo (for unsigned compare)
            // comparison like lcmp/fcmpl/dcmpl (for long/float/double)
            // branch like if_acmpeq/if_icmplt/ifne (depends on test)
            // iconst_0
            // goto next instruction
            // branch label:
            // iconst_1
            //

            if (cilInst.Next == null)
                throw new InvalidProgramException();

            //
            // first, the comparison test.  this is done in a helper method
            // which returns the opcode for the branch following the test.
            //

            var stackTop = code.StackMap.PopStack(CilMain.Where);
            var stackTop2 = code.StackMap.PopStack(CilMain.Where);

            byte op = (cilOp == Code.Ceq)
                    ? TestEq(code, stackTop, stackTop2, cilInst)
                    : TestGtLt(code, stackTop,
             /* if greater than */ (    cilOp == Code.Cgt
              /* (vs less than) */   || cilOp == Code.Cgt_Un),
                 /* if unsigned */ (    cilOp == Code.Cgt_Un
                /* or unordered */   || cilOp == Code.Clt_Un));
            //
            // as branch labels, we generally use the offset of the source
            // cil instruction;  in this case, we need a temporary label
            // which does not correspond to any cil instruction, so we will
            // use offset+1 (knowing that ceq/cgt/clt is two bytes long)
            //

            ushort label1 = (ushort) (cilInst.Offset + 1);

            code.NewInstruction(op, null, label1);
            code.StackMap.SaveFrame(label1, true, CilMain.Where);

            //
            // if the test failed and we fall through, push 0 and branch
            // to the next cil instruction.  otherwise push 1.
            //

            ushort label2 = (ushort) cilInst.Next.Offset;

            code.NewInstruction(0x03 /* iconst_0 */, null, null);
            code.StackMap.PushStack(CilType.From(JavaType.IntegerType));

            code.NewInstruction(0xA7 /* goto */, null, label2);
            code.StackMap.SaveFrame(label2, true, CilMain.Where);

            code.NewInstruction(0x04 /* iconst_1 */, null, null, (ushort) label1);
        }



        static byte TestEq(JavaCode code, JavaType stackTop, JavaType stackTop2,
                           Mono.Cecil.Cil.Instruction cilInst)
        {
            if (stackTop.IsReference || stackTop2.IsReference)
            {
                CodeSpan.Compare(stackTop, stackTop2, cilInst, code);
                return 0xA5; // if_acmpeq (reference)
            }

            if (stackTop2.IsIntLike && (    stackTop.PrimitiveType == TypeCode.Int32
                                         || stackTop.PrimitiveType == TypeCode.UInt32
                                         || stackTop.PrimitiveType == TypeCode.Int16
                                         || stackTop.PrimitiveType == TypeCode.UInt16
                                         || stackTop.PrimitiveType == TypeCode.SByte
                                         || stackTop.PrimitiveType == TypeCode.Byte
                                         || stackTop.PrimitiveType == TypeCode.Char
                                         || stackTop.PrimitiveType == TypeCode.Boolean))
            {
                return 0x9F; // if_icmpeq
            }

            byte op;

            if (    (     stackTop.PrimitiveType == TypeCode.Int64
                      ||  stackTop.PrimitiveType == TypeCode.UInt64)
                 && (    stackTop2.PrimitiveType == TypeCode.Int64
                      || stackTop2.PrimitiveType == TypeCode.UInt64))
            {
                op = 0x94; // lcmp (long)
            }

            else if (    stackTop.PrimitiveType == TypeCode.Single
                      && stackTop2.PrimitiveType == TypeCode.Single)
            {
                op = 0x95; // fcmpl (float)
            }

            else if (    stackTop.PrimitiveType == TypeCode.Double
                      && stackTop2.PrimitiveType == TypeCode.Double)
            {
                op = 0x97; // dcmpl (double)
            }

            else
                throw new Exception($"incompatible types '{stackTop}' and '{stackTop2}'");

            code.NewInstruction(op, null, null);
            return 0x99; // ifeq == zero
        }



        static byte TestGtLt(JavaCode code, JavaType stackTop, bool greater, bool unsigned_unordered)
        {
            byte op;

            //
            // for floating point, normal comparison returns 0 if either value
            // is NaN, while unordered comparison returns 1.  we have fcmp/dcmp
            // variants which return either 1 or -1.  following the comparison,
            // we have ifgt/iflt to check if the result is either greater than
            // or less than 0.  we consider all this when picking the opcode:
            //
            //                  test    normal compare  unordered compare
            //  greater than    ifgt        xcmpl              xcmpg
            //     less than    iflt        xcmpg              xcmpl
            //

            if (stackTop.PrimitiveType == TypeCode.Single)
            {
                op = (byte) ((greater != unsigned_unordered) ? 0x95  // fcmpl
                                                             : 0x96); // fcmpg
            }

            else if (stackTop.PrimitiveType == TypeCode.Double)
            {
                op = (byte) ((greater != unsigned_unordered) ? 0x97   // dcmpl
                                                             : 0x98); // dcmpg
            }

            //
            // for unsigned integer comparison, we use library function
            // system.UInt{32,64}.CompareTo, followed by ifgt/iflt
            //

            else if (unsigned_unordered && stackTop.PrimitiveType != TypeCode.Char)
            {
                char typeChar;
                int typeBits;
                string typeName = null;

                     if (    stackTop.PrimitiveType == TypeCode.SByte
                          || stackTop.PrimitiveType == TypeCode.Byte)
                {
                    typeChar = 'B';
                    typeBits = 0;
                    typeName = "Byte";
                }
                else if (    stackTop.PrimitiveType == TypeCode.Int16
                          || stackTop.PrimitiveType == TypeCode.UInt16)
                {
                    typeChar = 'S';
                    typeBits = 16;
                }
                else if (    stackTop.PrimitiveType == TypeCode.Int32
                     || stackTop.PrimitiveType == TypeCode.UInt32)
                {
                    typeChar = 'I';
                    typeBits = 32;
                }
                else if (    stackTop.PrimitiveType == TypeCode.Int64
                          || stackTop.PrimitiveType == TypeCode.UInt64)
                {
                    typeChar = 'J';
                    typeBits = 64;
                }
                else if (greater && stackTop.Equals(JavaStackMap.Null))
                {
                    // per MS CLI Partition III table 4 note 2, 'cgt.un'
                    // can be used to check for a non-null reference
                    return 0xA6; // if_acmpne
                }
                else
                    throw new InvalidProgramException();

                if (typeBits != 0)
                    typeName = "UInt" + typeBits.ToString();

                code.NewInstruction(0xB8 /* invokestatic */,
                                    new JavaType(0, 0, $"system.{typeName}"),
                                    new JavaMethodRef("CompareTo",
                                                     $"({typeChar}{typeChar})I", CilMain.Where));
                                    /*
                                    new JavaType(0, 0, $"java.lang.{typeName}"),
                                    new JavaMethodRef("compareUnsigned",
                                                     $"({typeChar}{typeChar})I", CilMain.Where));*/
                op = 0;
            }

            //
            // for signed long comparison, we use lcmp followed by ifgt/iflt
            //

            else if (    stackTop.PrimitiveType == TypeCode.Int64
                      || stackTop.PrimitiveType == TypeCode.UInt64)
            {
                op = 0x94; // lcmp (long)
            }

            //
            // for signed integer comparison, we have if_icmplt/if_icmpgt
            // which directly compare two integers and branch
            //

            else if (    stackTop.PrimitiveType == TypeCode.Int32
                      || stackTop.PrimitiveType == TypeCode.UInt32
                      || stackTop.PrimitiveType == TypeCode.Int16
                      || stackTop.PrimitiveType == TypeCode.UInt16
                      || stackTop.PrimitiveType == TypeCode.SByte
                      || stackTop.PrimitiveType == TypeCode.Byte
                      || stackTop.PrimitiveType == TypeCode.Char
                      || stackTop.PrimitiveType == TypeCode.Boolean)
            {
                return (byte) (greater ? 0xA3   // if_icmpgt
                                       : 0xA1); // if_icmplt
            }

            else
                throw new InvalidProgramException();

            //
            // return the selected opcode
            //

            if (op != 0)
                code.NewInstruction(op, null, null);

            return (byte) (greater ? 0x9D   // ifgt
                                   : 0x9B); // iflt
        }



        static byte TestBool(JavaCode code, JavaType stackTop)
        {
            if (stackTop.IsReference)
            {
                return 0xC7; // ifnonnull
            }

            if (    stackTop.PrimitiveType == TypeCode.Int64
                 || stackTop.PrimitiveType == TypeCode.UInt64
                 || stackTop.PrimitiveType == TypeCode.Int32
                 || stackTop.PrimitiveType == TypeCode.UInt32
                 || stackTop.PrimitiveType == TypeCode.Int16
                 || stackTop.PrimitiveType == TypeCode.UInt16
                 || stackTop.PrimitiveType == TypeCode.SByte
                 || stackTop.PrimitiveType == TypeCode.Byte
                 || stackTop.PrimitiveType == TypeCode.Char
                 || stackTop.PrimitiveType == TypeCode.Boolean)
            {
                if (    stackTop.PrimitiveType == TypeCode.Int64
                     || stackTop.PrimitiveType == TypeCode.UInt64)
                {
                    code.NewInstruction(0x09 /* lconst_0 (long) */, null, null);
                    code.NewInstruction(0x94 /* lcmp (long) */, null, null);
                }

                return 0x9A; // ifne != zero
            }

            throw new InvalidProgramException();
        }



        public static void Switch(JavaCode code, Mono.Cecil.Cil.Instruction cilInst)
        {
            if (cilInst.Operand is Mono.Cecil.Cil.Instruction[] targets)
            {
                if (    (targets.Length > Int32.MaxValue - 1)
                     || cilInst.Next == null
                     || (! code.StackMap.PopStack(CilMain.Where).IsIntLike))
                {
                    throw new InvalidProgramException();
                }

                ushort offset = (ushort) cilInst.Next.Offset;
                code.StackMap.SaveFrame(offset, true, CilMain.Where);

                int n = targets.Length;
                var instdata = new int[3 + n];
                instdata[0] = offset;
                instdata[2] = n - 1;

                for (int i = 0; i < n; i++)
                {
                    offset = (ushort) targets[i].Offset;
                    code.StackMap.SaveFrame(offset, true, CilMain.Where);
                    instdata[i + 3] = offset;
                }

                code.NewInstruction(0xAA /* tableswitch */, null, instdata);
            }
        }



        public static void Instance(JavaCode code, CodeLocals locals, Mono.Cecil.Cil.Instruction cilInst)
        {
            if (cilInst.Operand is TypeReference cilType && cilInst.Next != null)
            {
                var stackTop = (CilType) code.StackMap.PopStack(CilMain.Where);
                if (! stackTop.IsReference)
                    throw new InvalidProgramException(); // top of stack is a primitive type

                var castType = (CilType) CilType.From(cilType);
                JavaType castClass = CilType.From(cilType).AsWritableClass;

                if (GenericUtil.ShouldCallGenericCast(stackTop, castType))
                {
                    code.StackMap.PushStack(stackTop);
                    // casting to a generic type is done via GenericType.TestCast
                    GenericUtil.CastToGenericType(cilType, 0, code);
                    code.StackMap.PopStack(CilMain.Where);  // stackTop
                    code.NewInstruction(0xC0 /* checkcast */, castClass, null);
                    code.StackMap.PushStack(castClass);
                }

                else if (CodeArrays.CheckCast(castType, false, code))
                {
                    // if casting to Object[], ValueType[], to an array of
                    // interface type, or to an array of a generic parameter,
                    // then CodeArrays.CheckCast already generated a call to
                    // system.Array.CheckCast in baselib, and we are done here

                    code.NewInstruction(0xC0 /* checkcast */, castClass, null);
                    code.StackMap.PushStack(castClass);
                }

                //
                // the cil 'isinst' casts the operand to the requested class,
                // but the jvm 'instanceof' only returns zero or one.  so we
                // also use 'checkcast' to get the jvm to acknowledge the cast
                //
                // however, if the cil 'isinst' is immediately followed by
                // 'brtrue' or 'brfalse' then we don't have to actually cast
                //

                else if (! TestForBranch(code, castClass, cilInst.Next))
                {
                    ushort nextLabel = (ushort) cilInst.Next.Offset;
                    int localIndex = locals.GetTempIndex(stackTop);

                    TestAndCast(code, castClass, stackTop, nextLabel, localIndex);

                    locals.FreeTempIndex(localIndex);
                }
            }
            else
                throw new InvalidProgramException();

            //
            // if the cil 'isinst' is immediately followed by 'brtrue'
            // or 'brfalse' then we don't have to actually cast the result
            //

            bool TestForBranch(JavaCode code, JavaType castClass,
                               Mono.Cecil.Cil.Instruction nextInst)
            {
                var op = nextInst.OpCode.Code;
                if (    op != Code.Brtrue   && op != Code.Brfalse
                     && op != Code.Brtrue_S && op != Code.Brfalse_S)
                {
                    return false;
                }

                code.NewInstruction(0xC1 /* instanceof */, castClass, null);
                code.StackMap.PushStack(CilType.From(JavaType.IntegerType));

                return true;
            }

            //
            // the cil 'isinst' casts the operand to the requested class,
            // but the jvm 'instanceof' only returns zero or one.  so we
            // also use 'checkcast' to get the jvm to acknowledge the cast
            //

            void TestAndCast(JavaCode code, JavaType castClass, JavaType stackTop,
                             ushort nextLabel, int localIndex)
            {
                code.NewInstruction(stackTop.StoreOpcode, null, (int) localIndex);

                code.NewInstruction(0x01 /* aconst_null */, null, null);
                code.StackMap.PushStack(castClass);

                code.NewInstruction(stackTop.LoadOpcode, null, (int) localIndex);
                code.StackMap.PushStack(stackTop);

                code.NewInstruction(0xC1 /* instanceof */, castClass, null);
                code.StackMap.PopStack(CilMain.Where);

                code.NewInstruction(0x99 /* ifeq == zero */, null, nextLabel);
                code.StackMap.SaveFrame(nextLabel, true, CilMain.Where);

                code.NewInstruction(0x57 /* pop */, null, null);
                code.StackMap.PopStack(CilMain.Where);

                code.NewInstruction(stackTop.LoadOpcode, null, (int) localIndex);
                code.NewInstruction(0xC0 /* checkcast */, castClass, null);

                code.StackMap.PushStack(castClass);
            }
        }



        static void ResetLocalsOutOfScope(JavaStackMap stackMap, CodeLocals locals,
                                          int[] resetLocals,
                                          Mono.Cecil.Cil.Instruction branchInst,
                                          Mono.Cecil.Cil.Instruction targetInst)
        {
            // branches forward and backward, combined with out-of-scope locals,
            // might cause java verification errors.  for example:
            //
            //          test cond with branch forward to :L.C
            //          store into some local
            //   L.A:   ...
            //          branch forward to :L.B
            //   L.B:   java verification error here
            //   L.C:   branch back to :L.A
            //
            // initially, the stack frame at L.A and L.B have the local, due to
            // normal flow of execution.  the frame at L.C does not include it,
            // due to the branch at the very top.  when the instruction at L.C
            // is processed, the active frame (without the local) is merged with
            // the branch target frame, i.e. L.A.  but the frame at L.B is not
            // modified, and still includes the local.  when java looks at the
            // branch below L.A, to branch target L.B, it sees a branch from a
            // frame that does not include the local, to a frame that does, and
            // this causes a verification error.
            //
            // to avoid such an error, we first need the set of locals that were
            // reset by the merging of the branch origin frame (i.e. L.C) into
            // the branch target frame (i.e. L.A), this is handled in Finish(),
            // see above.  in this method, we go through all the frames between
            // the target (L.A) the origin (L.C), and reset all references to
            // those locals in all intermediate frames.

            // make sure this is backwards branch
            if (targetInst.Offset >= branchInst.Offset)
                return;

            /*var (indexLocalVars0, indexLocalVars1) = locals.GetLocalsIndexAndCount();
            indexLocalVars1 += indexLocalVars0;*/
            int resetLength = resetLocals.Length;

            var inst = targetInst;
            while (inst != branchInst)
            {
                var (localType, localIndex) =
                        locals.GetLocalFromStoreInst(inst.OpCode.Code, inst.Operand);

                //if (localIndex >= indexLocalVars0 && localIndex <= indexLocalVars1)
                if (localIndex != -1)
                {
                    for (int i = 0; i < resetLength; i++)
                    {
                        if (resetLocals[i] == localIndex)
                        {
                            // if we detect a store into an out-of-scope local that
                            // we are processing, we have to take it off the list,
                            // because it is no longer out-of-scope.
                            resetLocals[i] = Int32.MaxValue;
                        }
                    }
                }
                else
                {
                    stackMap.ResetLocalsInFrame((ushort) inst.Offset, resetLocals);
                }

                inst = inst.Next;
            }
        }

    }

}
