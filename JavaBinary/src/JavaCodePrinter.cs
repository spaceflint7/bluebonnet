
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaCode
    {

        static string[] instMnemonics;



        public void Print(IndentedText txt)
        {
            if (Instructions.Count == 0)
                return;

            if (instMnemonics == null)
                InitializeMnemonics();

            var excStrings = PrepareExceptions();

            int line = 0;
            string lineText;
            if (Instructions.Count > 0 && Instructions[0]?.Line != 0)
            {
                line = Instructions[0].Line;
                lineText = "line " + line + ", ";
            }
            else
                lineText = string.Empty;

            txt.Write("        ----------     {0}stack={1}, locals={2}, args_size={3}",
                lineText, MaxStack, MaxLocals, (Method.Parameters?.Count ?? 0));
            txt.NewLine();

            int offset = 0;
            foreach (var inst in Instructions)
            {

                if (inst.Line != 0 && inst.Line != line)
                {
                    line = inst.Line;
                    txt.Write("        ----------     line {0}", line);
                    txt.NewLine();
                }

                if (excStrings.TryGetValue(offset, out var strs))
                {
                    txt.AdjustIndent(+4);
                    foreach (var s in strs)
                    {
                        txt.Write(s);
                        txt.NewLine();
                    }
                    txt.AdjustIndent(-4);
                }

                if (StackMap != null)
                {
                    var (strLocals, strStack) = StackMap.FrameToString((ushort) offset);
                    if (strLocals != null)
                    {
                        txt.Write($"    locals = [ {strLocals} ]");
                        txt.NewLine();
                    }
                    if (strStack != null)
                    {
                        txt.Write($"    stack  = [ {strStack} ]");
                        txt.NewLine();
                    }
                }

                byte op;

                if (inst.Bytes != null)
                {
                    op = inst.Bytes[0];
                    txt.Write("{0:X4}    {1:X2}", offset, op);
                    for (int i = 1; i < inst.Bytes.Length; i++)
                        txt.Write(" {0:X2}", inst.Bytes[i]);
                    for (int i = inst.Bytes.Length; i < 5; i++)
                        txt.Write("   ");
                    txt.Write(" ");
                }
                else
                {
                    op = inst.Opcode;
                    txt.Write("L.{0:X4}  {1:X2}", inst.Label, op);
                    for (int i = 1; i < 5; i++)
                        txt.Write("   ");
                    txt.Write(" ");
                }

                if (op == 0xC4)
                {
                    PrintInstructionWide(txt, inst, offset);
                }
                else
                {
                    switch (instOperandType[op] & 0xF0)
                    {
                        case 0x80:
                            PrintInstructionConst(txt, inst, offset);
                            break;

                        case 0x40:
                            PrintInstructionJump(txt, inst, offset);
                            break;

                        case 0x20:
                            PrintInstructionMisc1(txt, inst, offset);
                            break;

                        case 0x10:
                            PrintInstructionNarrow(txt, inst, offset);
                            break;

                        case 0x00:
                            PrintInstructionMisc0(txt, inst, offset);
                            break;

                        default:
                            txt.Write("unknown opcode kind");
                            break;
                    }
                }

                txt.NewLine();

                if (inst.Bytes != null)
                    offset += inst.Bytes.Length;
            }
        }



        void PrintInstructionConst(IndentedText txt, Instruction inst, int offset)
        {
            txt.Write(instMnemonics[inst.Opcode] ?? "???");

            if (inst.Class != null)
            {
                if (inst.Data == null)
                {
                    txt.Write(" {0}", inst.Class);
                }

                else if (inst.Data is JavaMethodRef mr)
                {
                    txt.Write(" ({0}) {1}.{2}(", mr.ReturnType, inst.Class, mr.Name);
                    int numArgs = mr.Parameters?.Count ?? 0;
                    for (int i = 0; i < numArgs; i++)
                    {
                        txt.Write("{0}{1}",
                            /* 0 */ (i > 0 ? ", " : string.Empty),
                            /* 1 */ mr.Parameters[i].Type);
                    }
                    txt.Write(")");
                }

                else if (inst.Data is JavaFieldRef fr)
                {
                    txt.Write(" ({0}) {1}.{2}", fr.Type, inst.Class, fr.Name);
                }

                else
                    txt.Write("???" + inst.Data + "???");
            }

            else if (inst.Data is JavaCallSite callSite)
            {
                txt.Write(" {0}", callSite.BootstrapMethod);
                txt.NewLine();
                txt.AdjustIndent(+23);
                txt.Write("dynamic invocation name = {0}", callSite.InvokedMethod);
                txt.NewLine();
                if (callSite.BootstrapArgs != null)
                {
                    int n = callSite.BootstrapArgs.Length;
                    txt.Write("bootstrap method arguments = ");
                    for (int i = 0; i < n; i++)
                    {
                        txt.Write("{0}{1}", (i > 0 ? " ; " : ""), callSite.BootstrapArgs[i].ToString());
                    }
                }

                txt.AdjustIndent(-23);
            }

            else if (inst.Data is string strValue)
            {
                txt.Write(" \"{0}\"", strValue);
            }

            else if (inst.Data is int intValue)
            {
                txt.Write(" int {0} ({0:X8})", intValue, intValue);
            }

            else if (inst.Data is long longValue)
            {
                txt.Write(" long {0} ({0:X8})", longValue, longValue);
            }

            else if (inst.Data is float floatValue)
            {
                txt.Write(" float {0:F1}", floatValue);
            }

            else if (inst.Data is double doubeValue)
            {
                txt.Write(" double {0:F1}", doubeValue);
            }

            else
                txt.Write(" unknown constant of type " + inst.Data.GetType());
        }



        void PrintInstructionJump(IndentedText txt, Instruction inst, int offset)
        {
            byte op = inst.Opcode;
            txt.Write(instMnemonics[op] ?? "???");

            switch (instOperandType[op])
            {
                case 0x42 when inst.Data is ushort targetLabel:
                    txt.Write(" L.{0:X4}", targetLabel);
                    break;

                case 0x44 when inst.Data is ushort targetLabelWide:
                    txt.Write(" L.{0:X4}", targetLabelWide);
                    break;

                case 0x4F when inst.Data is int[] switchData:
                    txt.NewLine();
                    txt.AdjustIndent(+23);
                    txt.Write("{0} default @ L.{1:X4}", instMnemonics[op], switchData[0]);
                    int i0 = switchData[1];
                    for (int i = 3; i < switchData.Length; i++)
                        txt.Write(", case {0} @ L.{1:X4}", i + i0 - 3, switchData[i]);
                    txt.AdjustIndent(-23);
                    break;

                default:
                    txt.Write(" unknown jump opcode kind");
                    break;
            }
        }



        void PrintInstructionMisc0(IndentedText txt, Instruction inst, int offset)
        {
            byte op = inst.Opcode;
            byte op0 = 0;

            if (op >= 0x1A && op <= 0x1D)
            {
                op -= 0x1A;
                op0 = 0x15; // iload
            }
            else if (op >= 0x1E && op <= 0x21)
            {
                op -= 0x1E;
                op0 = 0x16; // lload
            }
            else if (op >= 0x22 && op <= 0x25)
            {
                op -= 0x22;
                op0 = 0x17; // fload
            }
            else if (op >= 0x26 && op <= 0x29)
            {
                op -= 0x26;
                op0 = 0x18; // dload
            }
            else if (op >= 0x2A && op <= 0x2D)
            {
                op -= 0x2A;
                op0 = 0x19; // aload
            }
            else if (op >= 0x3B && op <= 0x3E)
            {
                op -= 0x3B;
                op0 = 0x36; // istore
            }
            else if (op >= 0x3F && op <= 0x42)
            {
                op -= 0x3F;
                op0 = 0x37; // lstore
            }
            else if (op >= 0x43 && op <= 0x46)
            {
                op -= 0x43;
                op0 = 0x38; // fstore
            }
            else if (op >= 0x47 && op <= 0x4A)
            {
                op -= 0x47;
                op0 = 0x39; // dstore
            }
            else if (op >= 0x4B && op <= 0x4E)
            {
                op -= 0x4B;
                op0 = 0x3A; // astore
            }

            if (op0 == 0)
                txt.Write(instMnemonics[op] ?? "???");
            else
                txt.Write("{0}_{1}", instMnemonics[op0], op);
        }



        void PrintInstructionWide(IndentedText txt, Instruction inst, int offset)
        {
            byte op = inst.Opcode;
            if (op == 0x84)
            {
                txt.Write("wide iinc?");
            }
            else if (inst.Data is short val)
            {
                txt.Write("{0} {1} (wide)", instMnemonics[op] ?? "???", val);
            }
            else
            {
                txt.Write("unknown wide opcode");
            }
        }



        void PrintInstructionNarrow(IndentedText txt, Instruction inst, int offset)
        {
            byte op = inst.Opcode;
            if (op == 0x84)
            {
                sbyte value = (sbyte) ((int) inst.Data & 0xFF);
                byte index = (byte) ((int) inst.Data >> 8);
                txt.Write("{0} {1} {2}{3}",
                    instMnemonics[op], index,
                    (value >= 0 ? "+" : string.Empty), value);
            }
            else if (inst.Data is int val)
            {
                txt.Write("{0} {1}", instMnemonics[op] ?? "???", (int) inst.Data);
            }
            else
            {
                txt.Write("unknown narrow opcode");
            }
        }



        void PrintInstructionMisc1(IndentedText txt, Instruction inst, int offset)
        {
            if (inst.Data is int val)
            {
                txt.Write(instMnemonics[inst.Opcode] ?? "???");
                txt.Write(" ");

                if (inst.Opcode == 0xBC)        // newarray
                {
                    txt.Write(val == 4 ? "boolean" : val == 5 ? "char" :
                              val == 6 ? "float" : val == 7 ? "double" :
                              val == 8 ? "byte" : val == 9 ? "short" :
                              val == 10 ? "int" : val == 11 ? "long" :
                              "unknown");
                }
                else if (inst.Opcode == 0x11)   // sipush
                {
                    txt.Write("{0} (0x{0:X4})", (short) val);
                }
                else if (inst.Opcode == 0x10)   // bipush
                {
                    txt.Write("{0} (0x{0:X2})", (sbyte) val);
                }
                else
                    txt.Write("unknown misc opcode");
            }
            else
            {
                txt.Write("unknown misc opcode");
            }
        }



        Dictionary<int, List<string>> PrepareExceptions()
        {
            var excStrings = new Dictionary<int, List<string>>();

            var prevBlock = (First: -1, Last: -1);
            var currBlock = prevBlock;
            List<string> strs;

            foreach (var exc in Exceptions)
            {
                currBlock = (exc.start, exc.endPlus1);

                if (currBlock != prevBlock)
                {
                    if (! excStrings.TryGetValue(currBlock.First, out strs))
                        strs = new List<string>();
                    strs.Insert(0, $"begin try block {currBlock.First:X4}..{currBlock.Last:X4} target {exc.handler:X4} catch type {exc.catchType ?? "*"}");
                    excStrings[currBlock.First] = strs;

                    if (! excStrings.TryGetValue(currBlock.Last, out strs))
                        strs = new List<string>();
                    strs.Insert(0, $"end of try block {currBlock.First:X4}..{currBlock.Last:X4}");
                    excStrings[currBlock.Last] = strs;

                    prevBlock = currBlock;
                }

                if (! excStrings.TryGetValue(exc.handler, out strs))
                    strs = new List<string>();
                strs.Add($"catch type {exc.catchType ?? "*"} on range {currBlock.First:X4}..{currBlock.Last:X4}");
                excStrings[exc.handler] = strs;
            }

            return excStrings;
        }



        static void InitializeMnemonics()
        {
            instMnemonics = new string[256];

            instMnemonics[0x00] = "nop";
            instMnemonics[0x01] = "aconst_null";
            instMnemonics[0x02] = "iconst_m1 (int -1)";
            instMnemonics[0x03] = "iconst_0 (int 0)";
            instMnemonics[0x04] = "iconst_1 (int 1)";
            instMnemonics[0x05] = "iconst_2 (int 2)";
            instMnemonics[0x06] = "iconst_3 (int 3)";
            instMnemonics[0x07] = "iconst_4 (int 4)";
            instMnemonics[0x08] = "iconst_5 (int 5)";
            instMnemonics[0x09] = "lconst_0 (long 0)";
            instMnemonics[0x0A] = "lconst_1 (long 1)";
            instMnemonics[0x0B] = "fconst_0 (float 0)";
            instMnemonics[0x0C] = "fconst_1 (float 1)";
            instMnemonics[0x0D] = "fconst_2 (float 2)";
            instMnemonics[0x0E] = "dconst_0 (double 0)";
            instMnemonics[0x0F] = "dconst_1 (double 1)";
            instMnemonics[0x10] = "bipush";
            instMnemonics[0x11] = "sipush";
            instMnemonics[0x12] = "ldc";
            instMnemonics[0x13] = "ldc_w";
            instMnemonics[0x14] = "ldc2_w";
            instMnemonics[0x15] = "iload";
            instMnemonics[0x16] = "lload";
            instMnemonics[0x17] = "fload";
            instMnemonics[0x18] = "dload";
            instMnemonics[0x19] = "aload";
            instMnemonics[0x1E] = "lload";
            instMnemonics[0x2E] = "iaload";
            instMnemonics[0x2F] = "laload";
            instMnemonics[0x30] = "faload";
            instMnemonics[0x31] = "daload";
            instMnemonics[0x32] = "aaload";
            instMnemonics[0x33] = "baload";
            instMnemonics[0x34] = "caload";
            instMnemonics[0x35] = "saload";
            instMnemonics[0x36] = "istore";
            instMnemonics[0x37] = "lstore";
            instMnemonics[0x38] = "fstore";
            instMnemonics[0x39] = "dstore";
            instMnemonics[0x3A] = "astore";
            instMnemonics[0x4F] = "iastore";
            instMnemonics[0x50] = "lastore";
            instMnemonics[0x51] = "fastore";
            instMnemonics[0x52] = "dastore";
            instMnemonics[0x53] = "aastore";
            instMnemonics[0x54] = "bastore";
            instMnemonics[0x55] = "castore";
            instMnemonics[0x56] = "sastore";
            instMnemonics[0x57] = "pop";
            instMnemonics[0x58] = "pop2";
            instMnemonics[0x59] = "dup";
            instMnemonics[0x5A] = "dup_x1";
            instMnemonics[0x5B] = "dup_x2";
            instMnemonics[0x5C] = "dup2";
            instMnemonics[0x5D] = "dup2_x1";
            instMnemonics[0x5E] = "dup2_x2";
            instMnemonics[0x5F] = "swap";
            instMnemonics[0x60] = "iadd";
            instMnemonics[0x61] = "ladd";
            instMnemonics[0x62] = "fadd";
            instMnemonics[0x63] = "dadd";
            instMnemonics[0x64] = "isub";
            instMnemonics[0x65] = "lsub";
            instMnemonics[0x66] = "fsub";
            instMnemonics[0x67] = "dsub";
            instMnemonics[0x68] = "imul";
            instMnemonics[0x69] = "lmul";
            instMnemonics[0x6A] = "fmul";
            instMnemonics[0x6B] = "dmul";
            instMnemonics[0x6C] = "idiv";
            instMnemonics[0x6D] = "ldiv";
            instMnemonics[0x6E] = "fdiv";
            instMnemonics[0x6F] = "ddiv";
            instMnemonics[0x70] = "irem";
            instMnemonics[0x71] = "lrem";
            instMnemonics[0x72] = "frem";
            instMnemonics[0x73] = "drem";
            instMnemonics[0x74] = "ineg";
            instMnemonics[0x75] = "lneg";
            instMnemonics[0x76] = "fneg";
            instMnemonics[0x77] = "dneg";
            instMnemonics[0x78] = "ishl";
            instMnemonics[0x79] = "lshl";
            instMnemonics[0x7A] = "ishr";
            instMnemonics[0x7B] = "lshr";
            instMnemonics[0x7C] = "iushr";
            instMnemonics[0x7D] = "lushr";
            instMnemonics[0x7E] = "iand";
            instMnemonics[0x7F] = "land";
            instMnemonics[0x80] = "ior";
            instMnemonics[0x81] = "lor";
            instMnemonics[0x82] = "ixor";
            instMnemonics[0x83] = "lxor";
            instMnemonics[0x84] = "iinc";
            instMnemonics[0x85] = "i2l";
            instMnemonics[0x86] = "i2f";
            instMnemonics[0x87] = "i2d";
            instMnemonics[0x88] = "l2i";
            instMnemonics[0x89] = "l2f";
            instMnemonics[0x8A] = "l2d";
            instMnemonics[0x8B] = "f2i";
            instMnemonics[0x8C] = "f2l";
            instMnemonics[0x8D] = "f2d";
            instMnemonics[0x8E] = "d2i";
            instMnemonics[0x8F] = "d2l";
            instMnemonics[0x90] = "d2f";
            instMnemonics[0x91] = "i2b";
            instMnemonics[0x92] = "i2c";
            instMnemonics[0x93] = "i2s";
            instMnemonics[0x94] = "lcmp";
            instMnemonics[0x95] = "fcmpl";
            instMnemonics[0x96] = "fcmpg";
            instMnemonics[0x97] = "dcmpl";
            instMnemonics[0x98] = "dcmpg";
            instMnemonics[0x99] = "ifeq_zero";
            instMnemonics[0x9A] = "ifne_zero";
            instMnemonics[0x9B] = "iflt_zero";
            instMnemonics[0x9C] = "ifge_zero";
            instMnemonics[0x9D] = "ifgt_zero";
            instMnemonics[0x9E] = "ifle_zero";
            instMnemonics[0x9F] = "if_icmpeq";
            instMnemonics[0xA0] = "if_icmpne";
            instMnemonics[0xA1] = "if_icmplt";
            instMnemonics[0xA2] = "if_icmpge";
            instMnemonics[0xA3] = "if_icmpgt";
            instMnemonics[0xA4] = "if_icmple";
            instMnemonics[0xA5] = "if_acmpeq";
            instMnemonics[0xA6] = "if_acmpne";
            instMnemonics[0xA7] = "goto";
            instMnemonics[0xAA] = "tableswitch";
            instMnemonics[0xAC] = "ireturn (int)";
            instMnemonics[0xAD] = "lreturn (long)";
            instMnemonics[0xAE] = "freturn (float)";
            instMnemonics[0xAF] = "dreturn (double)";
            instMnemonics[0xB0] = "areturn (reference)";
            instMnemonics[0xB1] = "return (void)";
            instMnemonics[0xB2] = "getstatic";
            instMnemonics[0xB3] = "putstatic";
            instMnemonics[0xB4] = "getfield";
            instMnemonics[0xB5] = "putfield";
            instMnemonics[0xB6] = "invokevirtual";
            instMnemonics[0xB7] = "invokespecial";
            instMnemonics[0xB8] = "invokestatic";
            instMnemonics[0xB9] = "invokeinterface";
            instMnemonics[0xBA] = "invokedynamic";
            instMnemonics[0xBB] = "new";
            instMnemonics[0xBC] = "newarray";
            instMnemonics[0xBD] = "anewarray";
            instMnemonics[0xBE] = "arraylength";
            instMnemonics[0xBF] = "athrow";
            instMnemonics[0xC0] = "checkcast";
            instMnemonics[0xC1] = "instanceof";
            instMnemonics[0xC2] = "monitorenter";
            instMnemonics[0xC3] = "monitorexit";
            instMnemonics[0xC5] = "multianewarray";
            instMnemonics[0xC6] = "ifnull";
            instMnemonics[0xC7] = "ifnonnull";
            instMnemonics[0xC8] = "goto_w";
            instMnemonics[0xCA] = "breakpoint";
        }

    }

}
