
using System;

namespace SpaceFlint.JavaBinary
{

    public abstract class JavaConstant : IEquatable<JavaConstant>
    {


        public abstract bool Equals(JavaConstant other);

        public abstract void Write(JavaWriter wrt);



        public class Utf8 : JavaConstant
        {
            public const byte tag = 1;
            public string str;

            public Utf8(JavaReader rdr)
            {
                str = rdr.ReadString();
            }

            public Utf8(string _str)
            {
                str = _str;
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.Utf8;
                return (str == other?.str);
            }

            public override string ToString()
            {
                return str;
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Write8(tag);
                wtr.WriteString(str);
            }
        }



        public class Integer : JavaConstant
        {
            public const byte tag = 3;
            public int value;

            public Integer(JavaReader rdr)
            {
                value = (int) rdr.Read32();
            }

            public Integer(int _value)
            {
                value = _value;
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.Integer;
                return (value == other?.value);
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Write8(tag);
                wtr.Write32((uint) value);
            }
        }



        public class Float : JavaConstant
        {
            public const byte tag = 4;
            public float value;

            public Float(JavaReader rdr)
            {
                var blk = rdr.ReadBlock(4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(blk);
                value = BitConverter.ToSingle(blk, 0);
            }

            public Float(float _value)
            {
                value = _value;
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.Float;
                return (value == other?.value);
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override void Write(JavaWriter wtr)
            {
                var blk = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(blk);
                wtr.Write8(tag);
                wtr.WriteBlock(blk);
            }
        }



        public class Long : JavaConstant
        {
            public const byte tag = 5;
            public long value;

            public Long(JavaReader rdr)
            {
                var (hi, lo) = (rdr.Read32(), rdr.Read32());
                value = (((long) hi) << 32) | lo;
            }

            public Long(long _value)
            {
                value = _value;
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.Long;
                return (value == other?.value);
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Write8(tag);
                wtr.Write32((uint) (value >> 32));
                wtr.Write32((uint) (value & 0xFFFFFFFF));
            }
        }



        public class Double : JavaConstant
        {
            public const byte tag = 6;
            public double value;

            public Double(JavaReader rdr)
            {
                var blk = rdr.ReadBlock(8);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(blk);
                value = BitConverter.ToDouble(blk, 0);
            }

            public Double(double _value)
            {
                value = _value;
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.Double;
                return (value == other?.value);
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override void Write(JavaWriter wtr)
            {
                var blk = BitConverter.GetBytes(value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(blk);
                wtr.Write8(tag);
                wtr.WriteBlock(blk);
            }
        }



        public abstract class StringRef : JavaConstant
        {
            public ushort stringIndex;

            public StringRef(JavaReader rdr)
            {
                stringIndex = rdr.Read16();
            }

            public StringRef(ushort _index)
            {
                stringIndex = _index;
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.StringRef;
                return (stringIndex == other?.stringIndex);
            }

            public void Write(JavaWriter wtr, byte tag)
            {
                wtr.Write8(tag);
                wtr.Write16(stringIndex);
            }
        }



        public class Class : StringRef
        {
            public const byte tag = 7;
            public JavaType cached;

            public Class(JavaReader rdr)
                : base(rdr)
            {
            }

            public Class(ushort _index)
                : base(_index)
            {
            }

            public override void Write(JavaWriter wtr)
            {
                base.Write(wtr, tag);
            }
        }



        public class String : StringRef
        {
            public const byte tag = 8;
            public string cached;

            public String(JavaReader rdr)
                : base(rdr)
            {
            }

            public String(ushort _index)
                : base(_index)
            {
            }

            public override void Write(JavaWriter wtr)
            {
                base.Write(wtr, tag);
            }
        }



        public abstract class MemberRef : JavaConstant
        {
            public ushort classIndex, nameAndTypeIndex;
            public JavaType cachedClass;

            public MemberRef(JavaReader rdr)
            {
                (classIndex, nameAndTypeIndex) = (rdr.Read16(), rdr.Read16());
            }

            public MemberRef(ushort _classIndex, ushort _nameAndTypeIndex)
            {
                (classIndex, nameAndTypeIndex) = (_classIndex, _nameAndTypeIndex);
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.MemberRef;
                return (classIndex == other?.classIndex
                     && nameAndTypeIndex == other?.nameAndTypeIndex);
            }

            public void Write(JavaWriter wtr, byte tag)
            {
                wtr.Write8(tag);
                wtr.Write16(classIndex);
                wtr.Write16(nameAndTypeIndex);
            }
        }



        public class FieldRef : MemberRef
        {
            public const byte tag = 9;
            public JavaFieldRef cachedField;

            public FieldRef(JavaReader rdr)
                : base(rdr)
            {
            }

            public FieldRef(ushort _classIndex, ushort _nameAndTypeIndex)
                : base(_classIndex, _nameAndTypeIndex)
            {
            }

            public override void Write(JavaWriter wtr)
            {
                base.Write(wtr, tag);
            }
        }



        public class MethodRef : MemberRef
        {
            public const byte tag = 10;
            public JavaMethodRef cachedMethod;

            public MethodRef(JavaReader rdr)
                : base(rdr)
            {
            }

            public MethodRef(ushort _classIndex, ushort _nameAndTypeIndex)
                : base(_classIndex, _nameAndTypeIndex)
            {
            }

            public override void Write(JavaWriter wtr)
            {
                base.Write(wtr, tag);
            }
        }



        public class InterfaceMethodRef : MethodRef
        {
            public new const byte tag = 11;

            public InterfaceMethodRef(JavaReader rdr)
                : base(rdr)
            {
            }

            public InterfaceMethodRef(ushort _classIndex, ushort _nameAndTypeIndex)
                : base(_classIndex, _nameAndTypeIndex)
            {
            }

            public override void Write(JavaWriter wtr)
            {
                base.Write(wtr, tag);
            }
        }



        public class NameAndType : JavaConstant
        {
            public const byte tag = 12;
            public ushort nameIndex, descriptorIndex;
            public JavaFieldRef cachedField;
            public JavaMethodRef cachedMethod;

            public NameAndType(JavaReader rdr)
            {
                (nameIndex, descriptorIndex) = (rdr.Read16(), rdr.Read16());
            }

            public NameAndType(ushort _nameIndex, ushort _descriptorIndex)
            {
                (nameIndex, descriptorIndex) = (_nameIndex, _descriptorIndex);
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.NameAndType;
                return (nameIndex == other?.nameIndex
                     && descriptorIndex == other?.descriptorIndex);
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Write8(tag);
                wtr.Write16(nameIndex);
                wtr.Write16(descriptorIndex);
            }
        }



        public class MethodHandle : JavaConstant
        {
            public const byte tag = 15;
            public byte referenceKind;
            public ushort referenceIndex;
            public JavaMethodHandle cached;

            public MethodHandle(JavaReader rdr)
            {
                referenceKind = rdr.Read8();
                referenceIndex = rdr.Read16();
            }

            public MethodHandle(byte _referenceKind, ushort _referenceIndex)
            {
                (referenceKind, referenceIndex) = (_referenceKind, _referenceIndex);
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.MethodHandle;
                return (referenceKind == other?.referenceKind
                        && referenceIndex == other?.referenceIndex);
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Write8(tag);
                wtr.Write8(referenceKind);
                wtr.Write16(referenceIndex);
            }
        }



        public class MethodType : JavaConstant
        {
            public const byte tag = 16;
            public ushort descriptorIndex;
            public JavaMethodType cached;

            public MethodType(JavaReader rdr)
            {
                descriptorIndex = rdr.Read16();
            }

            public MethodType(ushort _descriptorIndex)
            {
                descriptorIndex = _descriptorIndex;
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.MethodType;
                return (descriptorIndex == other?.descriptorIndex);
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Write8(tag);
                wtr.Write16(descriptorIndex);
            }
        }



        public class InvokeDynamic : JavaConstant
        {
            public const byte tag = 18;
            public ushort bootstrapMethodIndex;
            public ushort nameAndTypeIndex;
            public JavaCallSite cached;

            public InvokeDynamic(JavaReader rdr)
            {
                bootstrapMethodIndex = rdr.Read16();
                nameAndTypeIndex = rdr.Read16();
            }

            public InvokeDynamic(ushort _bootstrapMethodIndex, ushort _nameAndTypeIndex)
            {
                (bootstrapMethodIndex, nameAndTypeIndex) = (_bootstrapMethodIndex, _nameAndTypeIndex);
            }

            public override bool Equals(JavaConstant _other)
            {
                var other = _other as JavaConstant.InvokeDynamic;
                return (bootstrapMethodIndex == other?.bootstrapMethodIndex
                        && nameAndTypeIndex == other?.nameAndTypeIndex);
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Write8(tag);
                wtr.Write16(bootstrapMethodIndex);
                wtr.Write16(nameAndTypeIndex);
            }
        }

    }

}
