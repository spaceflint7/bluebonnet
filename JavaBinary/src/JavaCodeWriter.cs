
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaCode
    {

        public void Write(JavaWriter wtr, JavaAttribute.Code codeAttr)
        {
            wtr.Where.Push("method body");

            PerformOptimizations();
            EliminateNops();

            int codeLength = FillInstructions(wtr);
            if (codeLength > 0xFFFE)
                throw wtr.Where.Exception("output method is too large");

            var labelToOffsetMap = FillJumpTargets(wtr);

            codeAttr.code = new byte[codeLength];
            FillCodeAttr(codeAttr.code);

            codeAttr.maxStack = (ushort) MaxStack;
            codeAttr.maxLocals = (ushort) MaxLocals;

            WriteExceptionData(codeAttr, labelToOffsetMap);

            if (StackMap != null)
            {
                var attr = StackMap.ToAttribute(wtr, labelToOffsetMap);
                if (attr != null)
                    codeAttr.attributes.Put(attr);
            }

            if (DebugLocals != null)
            {
                var attr = new JavaAttribute.LocalVariableTable(DebugLocals, codeLength);
                codeAttr.attributes.Put(attr);
            }

            FillLineNumbers(codeAttr);

            wtr.Where.Pop();
        }



        int FillInstructions(JavaWriter wtr)
        {
            int codeLength = 0;

            foreach (var inst in Instructions)
            {
                if (inst.Bytes != null)
                {
                    codeLength += inst.Bytes.Length;
                    continue;
                }

                byte op = inst.Opcode;
                byte kind = instOperandType[op];
                bool ok;

                wtr.Where.Push($"opcode 0x{op:X2} label 0x{inst.Label:X4}");

                if ((kind & 0x80) != 0)
                    ok = FillInstruction_Const(wtr, inst, op);

                else if ((kind & 0x40) != 0)
                    ok = FillInstruction_Jump(wtr, inst, op, codeLength);

                else if ((kind & 0x10) != 0)
                    ok = FillInstruction_Local(wtr, inst, op);

                else
                    ok = FillInstruction_Special(wtr, inst, op);

                if (! ok)
                    throw wtr.Where.Exception($"unsupported instruction {op:X2} (or operand) at offset {codeLength:X4} (line {inst.Line})");

                if (inst.Bytes != null)
                    codeLength += inst.Bytes.Length;
                else
                    codeLength++;

                wtr.Where.Pop();
            }

            return codeLength;
        }



        bool FillInstruction_Const(JavaWriter wtr, Instruction inst, byte op)
        {
            int length = 3;
            int count = 0;

            if (op >= 0x12 && op <= 0x14)
            {
                FillInstruction_ConstLoad(wtr, inst);
                return true;
            }

            int constantIndex = -1;

            if (op >= 0xB2 && op <= 0xB5)
            {
                // getstatic/putstatic/getfield/putfield
                if (inst.Data is JavaFieldRef vField)
                    constantIndex = wtr.ConstField(inst.Class, vField);
            }

            else if (op >= 0xB6 && op <= 0xB8)
            {
                // invokevirtual/invokespecial/invokestatic
                if (inst.Data is JavaMethodRef vMethod)
                    constantIndex = wtr.ConstMethod(inst.Class, vMethod);
            }

            else if (op == 0xB9)
            {
                // invokeinterface
                if (inst.Data is JavaMethodRef vMethod)
                {
                    constantIndex = wtr.ConstInterfaceMethod(inst.Class, vMethod);
                    length = 5;
                    count = 1; // 'this' argument
                    int numArgs = vMethod.Parameters.Count;
                    for (int i = 0; i < numArgs; i++)
                        count += vMethod.Parameters[i].Type.Category;
                }
            }

            else if (op == 0xBA)
            {
                // invokedynamic
                if (inst.Data is JavaCallSite vCallSite)
                {
                    constantIndex = wtr.ConstInvokeDynamic(vCallSite);
                    length = 5;
                }
            }

            else if (op >= 0xBB)
            {
                // new/anewarray/checkcast/instanceof/multianewarray
                constantIndex = wtr.ConstClass(inst.Class);

                if (op == 0xC5)
                {
                    length++;
                    count = (int) inst.Data;
                }
            }

            if (constantIndex == -1)
                return false;

            inst.Bytes = new byte[length];
            inst.Bytes[0] = op;
            inst.Bytes[1] = (byte) (constantIndex >> 8);
            inst.Bytes[2] = (byte) constantIndex;

            if (op == 0xB9 || op == 0xC5)
                inst.Bytes[3] = (byte) count;

            return true;
        }



        void FillInstruction_ConstLoad(JavaWriter wtr, Instruction inst)
        {
            int constantIndex = -1;
            byte op = 0;

            if (inst.Data is long vLong)
            {
                if (FillInstruction_ConstLoad_Long(inst, vLong))
                    return;
                constantIndex = wtr.ConstLong(vLong);
                op = 0x14;
            }

            else if (inst.Data is double vDouble)
            {
                constantIndex = wtr.ConstDouble(vDouble);
                op = 0x14;
            }

            else
            {
                if (inst.Class != null)
                {
                    if (inst.Data is JavaFieldRef vField)
                        constantIndex = wtr.ConstField(inst.Class, vField);

                    else if (inst.Data is JavaMethodRef vMethod)
                        constantIndex = wtr.ConstMethod(inst.Class, vMethod);

                    else if (inst.Data == null)
                        constantIndex = wtr.ConstClass(inst.Class);
                }

                else if (inst.Data is int vInteger)
                {
                    if (FillInstruction_ConstLoad_Integer(inst, vInteger))
                        return;
                    constantIndex = wtr.ConstInteger(vInteger);
                }

                else if (inst.Data is float vFloat)
                    constantIndex = wtr.ConstFloat(vFloat);

                else if (inst.Data is string vString)
                    constantIndex = wtr.ConstString(vString);

                op = (byte) ((constantIndex <= 255) ? 0x12 : 0x13);
                inst.Opcode = op;
            }

            if (constantIndex == -1)
                throw wtr.Where.Exception("invalid constant in ldc/ldc_w/ldc2_w instruction");

            if (op == 0x12)
            {
                inst.Bytes = new byte[2];
                inst.Bytes[1] = (byte) constantIndex;
            }
            else
            {
                inst.Bytes = new byte[3];
                inst.Bytes[1] = (byte) (constantIndex >> 8);
                inst.Bytes[2] = (byte) constantIndex;
            }
            inst.Bytes[0] = op;
        }



        bool FillInstruction_ConstLoad_Integer(Instruction inst, int value)
        {
            if (value >= -1 && value <= 5)
            {
                inst.Bytes = new byte[1];
                inst.Bytes[0] = (byte) (3 + value); // iconst_m1 .. iconst_5
            }
            else if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                inst.Bytes = new byte[2];
                inst.Bytes[0] = 0x10;               // bipush
                inst.Bytes[1] = (byte) value;
            }
            else if (value >= short.MinValue && value <= short.MaxValue)
            {
                inst.Bytes = new byte[3];
                inst.Bytes[0] = 0x11;               // sipush
                inst.Bytes[1] = (byte) (value >> 8);
                inst.Bytes[2] = (byte) value;
            }
            else
                return false;
            return true;
        }



        bool FillInstruction_ConstLoad_Long(Instruction inst, long value)
        {
            if (value >= 0 && value <= 1)
            {
                inst.Bytes = new byte[1];
                inst.Bytes[0] = (byte) (9 + value); // lconst_0, lconst_1
            }
            else
                return false;
            return true;
        }



        bool FillInstruction_Local(JavaWriter wtr, Instruction inst, byte op)
        {
            if (op == 0x84)
            {
                FillInstruction_LocalIncrement(wtr, inst, op);
                return true;
            }

            ushort var = (ushort) (int) inst.Data;

            if (var <= 3 && op != 0xA9)         // if not 'ret' and small var
            {
                if (op == 0x15)                 // iload
                    inst.Opcode = (byte) (0x1A + var);

                else if (op == 0x16)            // lload
                    inst.Opcode = (byte) (0x1E + var);

                else if (op == 0x17)            // fload
                    inst.Opcode = (byte) (0x22 + var);

                else if (op == 0x18)            // dload
                    inst.Opcode = (byte) (0x26 + var);

                else if (op == 0x19)            // aload
                    inst.Opcode = (byte) (0x2A + var);

                else if (op == 0x36)            // istore
                    inst.Opcode = (byte) (0x3B + var);

                else if (op == 0x37)            // lstore
                    inst.Opcode = (byte) (0x3F + var);

                else if (op == 0x38)            // fstore
                    inst.Opcode = (byte) (0x43 + var);

                else if (op == 0x39)            // dstore
                    inst.Opcode = (byte) (0x47 + var);

                else if (op == 0x3A)            // astore
                    inst.Opcode = (byte) (0x4B + var);

                else
                    return false;
            }

            else if (var <= 255)
            {
                inst.Bytes = new byte[2];
                inst.Bytes[0] = op;
                inst.Bytes[1] = (byte) var;
            }

            else
            {
                inst.Bytes = new byte[4];
                inst.Bytes[0] = 0xC4; // wide
                inst.Bytes[1] = op;
                inst.Bytes[2] = (byte) (var >> 8);
                inst.Bytes[3] = (byte) var;
            }

            return true;
        }



        void FillInstruction_LocalIncrement(JavaWriter wtr, Instruction inst, byte op)
        {
            ushort var = (ushort) ((uint) inst.Data & 0xFFFF);
            ushort inc = (ushort) ((uint) inst.Data >> 16);

            if (var <= 255 && inc <= 255)
            {
                inst.Bytes = new byte[3];
                inst.Bytes[0] = op;
                inst.Bytes[1] = (byte) var;
                inst.Bytes[2] = (byte) inc;
            }
            else
            {
                inst.Bytes = new byte[5];
                inst.Bytes[0] = op;
                inst.Bytes[1] = (byte) (var >> 8);
                inst.Bytes[2] = (byte) var;
                inst.Bytes[3] = (byte) (inc >> 8);
                inst.Bytes[4] = (byte) inc;
            }
        }



        bool FillInstruction_Special(JavaWriter wtr, Instruction inst, byte op)
        {
            if (op == 0xBC)
            {
                inst.Bytes = new byte[2];
                inst.Bytes[0] = op;
                inst.Bytes[1] = (byte) inst.Data;
            }

            return true;
        }



        bool FillInstruction_Jump(JavaWriter wtr, Instruction inst, byte op, int offset)
        {
            if (inst.Data is ushort && op < 0xC8) // insts with 2-byte offsets
            {
                inst.Bytes = new byte[3];
                inst.Bytes[0] = op;
            }

            else if (inst.Data is ushort && op == 0xC8)
            {
                inst.Bytes = new byte[5];
                inst.Bytes[0] = op;
            }

            else if (inst.Data is int[] words && op >= 0xAA && op <= 0xAB)
            {
                int n = words.Length;
                if (    n > 16384
                     || (op == 0xAA && (n <= 3 || n != 3 + words[2] - words[1] + 1))
                     || (op == 0xAB && (n <= 2 || n != 2 + words[1] * 2)))
                {
                    throw wtr.Where.Exception("invalid data in switch instruction");
                }
                inst.Bytes = new byte[4 - (offset & 3) + n * 4];
                inst.Bytes[0] = op;
            }

            else
                return false;
            return true;
        }



        Dictionary<ushort, ushort> FillJumpTargets(JavaWriter wtr)
        {
            var jumpTuples = new List<ValueTuple<Instruction, ushort>>();
            var labelToOffsetMap = new Dictionary<ushort, ushort>();
            ushort offset = 0;
            foreach (var inst in Instructions)
            {
                byte kind = instOperandType[inst.Opcode];
                if ((kind & 0x40) == 0x40)
                {
                    if (inst.Data is int intOffset)
                    {
                        // int data is a jump offset that can be calculated immediately
                        // note that this prevents nop elimination; see EliminateNops
                        intOffset -= offset;
                        inst.Bytes[1] = (byte) (offset >> 8);
                        inst.Bytes[2] = (byte) offset;
                    }
                    else if (inst.Data is ushort || inst.Data is int[])
                    {
                        // ushort data is a label, should be converted to a jump offset
                        jumpTuples.Add(new ValueTuple<Instruction, ushort>(inst, offset));
                    }
                }

                if (! labelToOffsetMap.ContainsKey(inst.Label))
                    labelToOffsetMap[inst.Label] = offset;

                if (inst.Bytes == null)
                    offset++;
                else
                    offset += (ushort) inst.Bytes.Length;
            }

            foreach (var jumpTuple in jumpTuples)
            {
                var inst = jumpTuple.Item1;
                bool ok;

                if (inst.Data is int[])
                    ok = FillJumpTargets_Switch(inst, jumpTuple.Item2, labelToOffsetMap);

                else if (labelToOffsetMap.TryGetValue((ushort) inst.Data, out offset))
                {
                    var offset32 = (int) offset - (int) jumpTuple.Item2;
                    if (inst.Bytes[0] == 0xC8) // goto_w
                    {
                        inst.Bytes[1] = (byte) (offset32 >> 24);
                        inst.Bytes[2] = (byte) (offset32 >> 16);
                        inst.Bytes[3] = (byte) (offset32 >> 8);
                        inst.Bytes[4] = (byte) offset32;
                    }
                    else
                    {
                        if (offset32 < Int16.MinValue || offset32 > Int16.MaxValue)
                            throw wtr.Where.Exception($"jump offset too far (in label {inst.Label:X4}, source line {inst.Line})");
                        offset -= jumpTuple.Item2;
                        inst.Bytes[1] = (byte) (offset >> 8);
                        inst.Bytes[2] = (byte) offset;
                    }
                    ok = true;
                }

                else
                    ok = false;

                if (! ok)
                    throw wtr.Where.Exception($"jump to undefined label (in label {inst.Label:X4}, source line {inst.Line})");
            }

            return labelToOffsetMap;
        }



        bool FillJumpTargets_Switch(Instruction inst, int instOffset,
                                    Dictionary<ushort, ushort> labelToOffsetMap)
        {
            if (inst.Opcode != 0xAA)
                return false;

            var words = (int[]) inst.Data;
            if (! labelToOffsetMap.TryGetValue((ushort) words[0], out var jumpOffset))
                return false;
            int o = 4 - (instOffset & 3);
            WriteInt32(inst.Bytes, o, jumpOffset - instOffset);

            WriteInt32(inst.Bytes, o += 4, words[1]);
            WriteInt32(inst.Bytes, o += 4, words[2]);

            int n = words.Length;
            for (int i = 3; i < n; i++)
            {
                if (! labelToOffsetMap.TryGetValue((ushort) words[i], out jumpOffset))
                    return false;
                WriteInt32(inst.Bytes, o += 4, jumpOffset - instOffset);
            }

            return true;

            void WriteInt32(byte[] bytes, int ofs, int val)
            {
                bytes[ofs + 0] = (byte) (val >> 24);
                bytes[ofs + 1] = (byte) (val >> 16);
                bytes[ofs + 2] = (byte) (val >> 8);
                bytes[ofs + 3] = (byte) val;
            }
        }



        void FillCodeAttr(byte[] codeText)
        {
            int offset = 0;

            foreach (var inst in Instructions)
            {
                if (inst.Bytes == null)
                {
                    codeText[offset++] = inst.Opcode;
                }
                else
                {
                    for (int i = 0; i < inst.Bytes.Length; i++)
                        codeText[offset++] = inst.Bytes[i];
                }
            }
        }



        void FillLineNumbers(JavaAttribute.Code codeAttr)
        {
            var lines = new List<JavaAttribute.LineNumberTable.Item>();
            JavaAttribute.LineNumberTable.Item item;
            item.lineNumber = 0xFFFF;
            ushort offset = 0;
            foreach (var inst in Instructions)
            {
                if (inst.Line != 0 && inst.Line != item.lineNumber)
                {
                    item.offset = offset;
                    item.lineNumber = inst.Line;
                    lines.Add(item);
                }
                offset += (ushort) ((inst.Bytes == null) ? 1 : inst.Bytes.Length);
            }
            if (lines.Count != 0)
            {
                codeAttr.attributes.Put(
                            new JavaAttribute.LineNumberTable(lines.ToArray()));
            }
        }



        void WriteExceptionData(JavaAttribute.Code codeAttr, Dictionary<ushort, ushort> labelToOffsetMap)
        {
            int n;
            if ((Exceptions != null) && (n = Exceptions.Count) > 0)
            {
                codeAttr.exceptions = new JavaAttribute.Code.Exception[n];

                for (int i = 0; i < n; i++)
                {
                    JavaAttribute.Code.Exception exceptionItem = Exceptions[i];

                    if (labelToOffsetMap.TryGetValue(exceptionItem.start, out var startOffset))
                        exceptionItem.start = startOffset;

                    if (labelToOffsetMap.TryGetValue(exceptionItem.endPlus1, out var endPlus1Offset))
                        exceptionItem.endPlus1 = endPlus1Offset;

                    if (labelToOffsetMap.TryGetValue(exceptionItem.handler, out var handlerOffset))
                        exceptionItem.handler = handlerOffset;

                    codeAttr.exceptions[i] = exceptionItem;
                }
            }
        }



        public static int LengthOfLocalVariableInstruction(int index)
        {
            // based on the given index value, determine the minimum length
            // needed for an xload/xstore instruction (e.g. astore).
            // one byte:    index <= 3 translates to xstore_N
            // two bytes:   index <= 255 translates to standard xstore
            // four bytes:  with a wide prefix and two bytes for index

            return (index <= 3) ? 1 : ((index <= 255) ? 2 : 4);
        }



        void PerformOptimizations()
        {
            int n = Instructions.Count;
            var prevInst = Instructions[0];
            for (int i = 1; i < n; i++)
            {
                var currInst = Instructions[i];

                // find occurrences of iconst_0 or iconst_1 followed by i2l
                // (which must not be a branch target), and convert to lconst_xx

                if (currInst.Opcode == 0x85 /* i2l */)
                {
                    if (    prevInst.Opcode == 0x12 /* ldc */
                         && prevInst.Data is int intValue
                         && (intValue == 0 || intValue == 1)
                         && (! StackMap.HasBranchFrame(currInst.Label)))
                    {
                        // convert ldc of 0 or 1 into lconst_0 or lconst_1
                        prevInst.Opcode = (byte) (intValue + 0x09);
                        currInst.Opcode = 0x00; // nop
                    }
                }
                prevInst = currInst;
            }
        }



        void EliminateNops()
        {
            // remove any nop instructions that are not a branch target,
            // and only if there are no exception tables.  note that
            // removing nops that are a branch target, or nops in a method
            // with exception tables, would require updating the stack map,
            // branch instructions and exception tables.

            if (Exceptions != null && Exceptions.Count != 0)
                return;
            var nops = new List<int>();

            int n = Instructions.Count;
            for (int i = 0; i < n; i++)
            {
                byte op = Instructions[i].Opcode;
                if (op == 0x00 /* nop */)
                {
                    // collect this nop only if it is not a branch target
                    if (! StackMap.HasBranchFrame(Instructions[i].Label))
                    {
                        nops.Add(i);
                    }
                }
                else if ((instOperandType[op] & 0x40) == 0x40) // jump inst
                {
                    if (Instructions[i].Data is int)
                    {
                        // if jump instruction has an explicit byte offset,
                        // we can't do nop elimination; see FillJumpTargets
                        return;
                    }
                }
            }

            for (int i = nops.Count; i-- > 0; )
                Instructions.RemoveAt(nops[i]);
        }

    }

}
