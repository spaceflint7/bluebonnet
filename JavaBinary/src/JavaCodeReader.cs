
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaCode
    {

        public JavaCode(JavaReader rdr, JavaMethod method, JavaAttribute.Code codeAttr)
        {
            rdr.Where.Push("method body");

            Method = method;
            MaxStack = codeAttr.maxStack;
            MaxLocals = codeAttr.maxLocals;
            var codeText = codeAttr.code;
            Instructions = new List<Instruction>();

            int offset = 0;
            while (offset < codeText.Length)
            {
                var inst = new Instruction();
                var offset0 = offset;
                byte op = codeText[offset++];
                byte kind;

                if (op == 0xC4)
                {
                    // wide prefix doubles operand size
                    op = codeText[offset++];
                    kind = instOperandType[op];
                    if ((kind & 0x10) != 0x10)
                        throw rdr.Where.Exception($"invalid use of wide opcode prefix at {(offset - 2).ToString("X4")}");
                    kind = (byte) (0x10 | ((kind & 0x0F) << 1));
                    if (op == 0x84)
                    {
                        inst.Data = (uint) (   (codeText[offset] << 24)
                                             | (codeText[offset + 1] << 16)
                                             | (codeText[offset + 2] << 8)
                                             | codeText[offset + 3]);
                    }
                    else
                        inst.Data = (short) ((codeText[offset] << 8) | codeText[offset + 1]);
                }
                else if (op == 0xAA || op == 0xAB)
                {
                    offset = ReadSwitch(codeText, op, offset, inst);
                    if (offset == -1)
                        throw rdr.Where.Exception($"switch opcode {op:X2} not supported at offset {offset0:X4}");
                    kind = 0;
                }
                else
                {
                    kind = instOperandType[op];

                    if ((kind & 0x80) == 0x80)
                    {
                        // constant reference
                        int constantIndex = codeText[offset];
                        if (kind >= 0x82 && kind <= 0x84)
                            constantIndex = (constantIndex << 8) | codeText[offset + 1];

                        var constantType = rdr.ConstType(constantIndex);

                        if (constantType == typeof(JavaConstant.String))
                            inst.Data = rdr.ConstString(constantIndex);

                        else if (constantType == typeof(JavaConstant.Integer))
                            inst.Data = rdr.ConstInteger(constantIndex);

                        else if (constantType == typeof(JavaConstant.Float))
                            inst.Data = rdr.ConstFloat(constantIndex);

                        else if (constantType == typeof(JavaConstant.Long))
                            inst.Data = rdr.ConstLong(constantIndex);

                        else if (constantType == typeof(JavaConstant.Double))
                            inst.Data = rdr.ConstDouble(constantIndex);

                        else if (constantType == typeof(JavaConstant.Class))
                            inst.Class = rdr.ConstClass(constantIndex);

                        else if (constantType == typeof(JavaConstant.FieldRef))
                            (inst.Class, inst.Data) = rdr.ConstField(constantIndex);

                        else if (constantType == typeof(JavaConstant.MethodRef))
                            (inst.Class, inst.Data) = rdr.ConstMethod(constantIndex);

                        else if (constantType == typeof(JavaConstant.InterfaceMethodRef))
                            (inst.Class, inst.Data) = rdr.ConstInterfaceMethod(constantIndex);

                        else if (constantType == typeof(JavaConstant.InvokeDynamic))
                        {
                            var callSite = rdr.ConstInvokeDynamic(constantIndex);
                            inst.Data = callSite;

                            if (rdr.callSites == null)
                                rdr.callSites = new List<JavaCallSite>();
                            rdr.callSites.Add(callSite);
                        }

                        else
                        {
                            rdr.Where.Push($"opcode {op:X2} offset {offset0:X4}");
                            throw rdr.Where.Exception( (constantType == null)
                                    ? $"bad constant index '{constantIndex}'"
                                    : $"unsupported constant of type '{constantType.Name}'");
                        }
                    }
                    else if ((kind & 0x40) == 0x40)
                    {
                        // control transfer, 32-bit or 16-bit offset
                        /*if ((kind & 0x4F) == 0x44)
                            throw rdr.Where.Exception($"32-bit branch offsets not supported at offset {(offset - 2).ToString("X4")}");*/
                        int jumpOffset = ((codeText[offset] << 8) | codeText[offset + 1]);
                        if ((kind & 0x4F) == 0x44)
                        {
                            // 32-bit branch offset in 'goto_w' instruction
                            jumpOffset = (jumpOffset << 8) | codeText[offset + 2];
                            jumpOffset = (jumpOffset << 8) | codeText[offset + 3];
                        }
                        inst.Data = (ushort) (offset0 + jumpOffset);
                    }
                    else if ((kind & 0x30) != 0)
                    {
                        // 0x10 - one or two bytes of immediate data
                        // 0x20 - local variable index
                        if ((kind & 0x0F) == 0x01)
                            inst.Data = (int) codeText[offset];
                        else
                            inst.Data = (int) ((codeText[offset] << 8) | codeText[offset + 1]);
                    }
                    else if (kind != 0)
                        throw rdr.Where.Exception("unknown opcode");
                }

                offset += (kind & 0x0F);

                int n = offset - offset0;
                inst.Bytes = new byte[offset - offset0];
                for (int i = 0; i < n; i++)
                    inst.Bytes[i] = codeText[offset0 + i];

                inst.Opcode = op;

                inst.Label = (ushort) offset0;

                Instructions.Add(inst);
            }

            ReadLineNumberTable(codeAttr);

            Exceptions = new List<JavaAttribute.Code.Exception>(codeAttr.exceptions);

            var stackMapAttr = codeAttr.attributes.GetAttr<JavaAttribute.StackMapTable>();
            if (stackMapAttr != null)
                StackMap = new JavaStackMap(stackMapAttr, rdr);

            rdr.Where.Pop();
        }



        void ReadLineNumberTable(JavaAttribute.Code codeAttr)
        {
            var linesAttrs =
                codeAttr?.attributes.GetAttrs<JavaAttribute.LineNumberTable>();

            if (linesAttrs == null)
                return;

            int instCount = Instructions.Count;
            foreach (var linesAttr in linesAttrs)
            {
                int instOffset = 0;
                int instIndex = 0;

                foreach (var line in linesAttr.lines)
                {
                    while (instOffset < line.offset && instIndex < instCount)
                        instOffset += Instructions[instIndex++].Bytes.Length;

                    if (instOffset == line.offset && instIndex < instCount)
                    {
                        Instructions[instIndex].Line = line.lineNumber;
                        instOffset += Instructions[instIndex++].Bytes.Length;
                    }
                }
            }
        }



        int ReadSwitch(byte[] codeText, byte op, int offset, Instruction inst)
        {
            int offset0 = offset - 1;

            if ((offset & 3) != 0)
                offset += 4 - (offset & 3);

            int v0 = (int) (   (codeText[offset]     << 24) | (codeText[offset + 1] << 16)
                             | (codeText[offset + 2] << 8)  |  codeText[offset + 3]);
            offset += 4;

            int v1 = (int) (   (codeText[offset]     << 24) | (codeText[offset + 1] << 16)
                             | (codeText[offset + 2] << 8)  |  codeText[offset + 3]);
            offset += 4;

            if (op == 0xAA)
            {
                int v2 = (int) (   (codeText[offset]     << 24) | (codeText[offset + 1] << 16)
                                 | (codeText[offset + 2] << 8)  |  codeText[offset + 3]);
                offset += 4;

                if (v2 < v1)
                    return -1;
                int n = v2 - v1 + 1;
                var words = new int[3 + n];
                words[0] = v0 + offset0;        // word 0, default jump label
                words[1] = v1;                  // word 1, lowest case number
                words[2] = v2;                  // word 2, highest case number
                for (int i = 0; i < n; i++)
                {
                    int vi = (int) (   (codeText[offset]     << 24) | (codeText[offset + 1] << 16)
                                     | (codeText[offset + 2] << 8)  |  codeText[offset + 3]);
                    words[i + 3] = vi + offset0;
                    offset += 4;
                }
                inst.Data = words;
            }
            else if (op == 0xAB)
                offset += v1 * 4 * 2;   // v1 is npairs
            else
                return -1;

            return offset;
        }

    }

}
