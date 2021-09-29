
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaCode
    {

        static readonly byte[] instOperandType;

        public JavaMethod Method;

        public int MaxStack;
        public int MaxLocals;
        public List<Instruction> Instructions;
        public List<JavaFieldRef> DebugLocals;

        public ushort DefaultLabel;



        public class Instruction
        {
            public byte[] Bytes;
            public JavaType Class;
            public object Data;
            public ushort Label;
            public ushort Line;
            public byte Opcode;

            public Instruction() {}
            public Instruction(byte _opcode, JavaType _class, object _data, ushort _label)
            {
                Opcode = _opcode;
                Class = _class;
                Data = _data;
                Label = _label;
            }
        }

        public JavaStackMap StackMap;

        public List<JavaAttribute.Code.Exception> Exceptions;



        public JavaCode()
        {
        }



        public JavaCode(JavaMethod _Method)
        {
            Method = _Method;
            Instructions = new List<Instruction>();
        }



        public void NewInstruction(byte _opcode, JavaType _class, object _data)
        {
            Instructions.Add(new Instruction(_opcode, _class, _data, DefaultLabel));
        }



        public void NewInstruction(byte _opcode, JavaType _class, object _data, ushort _label)
        {
            Instructions.Add(new Instruction(_opcode, _class, _data, _label));
        }



        public ushort SetLabel(ushort newLabel)
        {
            ushort oldLabel = DefaultLabel;
            DefaultLabel = newLabel;
            return oldLabel;
        }



        public static bool IsBranchOpcode (byte op)
            => (instOperandType[op] & 0x40) == 0x40;



        static JavaCode()
        {
            instOperandType = new byte[256];

            instOperandType[0x12] = 0x81; // ldc
            instOperandType[0x13] = 0x82; // ldc_w
            instOperandType[0x14] = 0x82; // ldc2_w
            instOperandType[0xB2] = 0x82; // getstatic
            instOperandType[0xB3] = 0x82; // putstatic
            instOperandType[0xB4] = 0x82; // getfield
            instOperandType[0xB5] = 0x82; // putfield
            instOperandType[0xB6] = 0x82; // invokevirtual
            instOperandType[0xB7] = 0x82; // invokespecial
            instOperandType[0xB8] = 0x82; // invokestatic
            instOperandType[0xB9] = 0x84; // invokeinterface
            instOperandType[0xBA] = 0x84; // invokedynamic
            instOperandType[0xBB] = 0x82; // new
            instOperandType[0xBD] = 0x82; // anewarray
            instOperandType[0xC0] = 0x82; // checkcast
            instOperandType[0xC1] = 0x82; // instanceof
            instOperandType[0xC5] = 0x83; // multianewarray

            instOperandType[0x99] = 0x42; // ifeq
            instOperandType[0x9A] = 0x42; // ifne
            instOperandType[0x9B] = 0x42; // iflt
            instOperandType[0x9C] = 0x42; // ifge
            instOperandType[0x9D] = 0x42; // ifgt
            instOperandType[0x9E] = 0x42; // ifle
            instOperandType[0x9F] = 0x42; // if_icmpeq
            instOperandType[0xA0] = 0x42; // if_icmpne
            instOperandType[0xA1] = 0x42; // if_icmplt
            instOperandType[0xA2] = 0x42; // if_icmpge
            instOperandType[0xA3] = 0x42; // if_icmpgt
            instOperandType[0xA4] = 0x42; // if_icmple
            instOperandType[0xA5] = 0x42; // if_acmpeq
            instOperandType[0xA6] = 0x42; // if_acmpne
            instOperandType[0xA7] = 0x42; // goto
            instOperandType[0xA8] = 0x42; // jsr
            instOperandType[0xC6] = 0x42; // ifnull
            instOperandType[0xC7] = 0x42; // ifnonnull
            instOperandType[0xC8] = 0x44; // goto_w
            instOperandType[0xC9] = 0x44; // jsr_w
            instOperandType[0xAA] = 0x4F; // tableswitch
            instOperandType[0xAB] = 0x4F; // lookupswitch

            instOperandType[0x10] = 0x21; // bipush
            instOperandType[0x11] = 0x22; // sipush
            instOperandType[0xBC] = 0x21; // newarray

            instOperandType[0x15] = 0x11; // iload
            instOperandType[0x16] = 0x11; // lload
            instOperandType[0x17] = 0x11; // fload
            instOperandType[0x18] = 0x11; // dload
            instOperandType[0x19] = 0x11; // aload
            instOperandType[0x36] = 0x11; // istore
            instOperandType[0x37] = 0x11; // lstore
            instOperandType[0x38] = 0x11; // fstore
            instOperandType[0x39] = 0x11; // dstore
            instOperandType[0x3A] = 0x11; // astore
            instOperandType[0xA9] = 0x11; // ret
            instOperandType[0x84] = 0x12; // iinc
        }

    }
}
