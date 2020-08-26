
using System;

namespace SpaceFlint.JavaBinary
{

    public class JavaField : JavaFieldRef
    {

        public JavaClass Class;
        public JavaAccessFlags Flags;
        public object Constant;



        public JavaField()
        {
        }



        /*
         * JavaFieldReader
         */



        public JavaField(JavaReader rdr)
        {
            Class = rdr.Class;

            Flags = (JavaAccessFlags) rdr.Read16();

            Name = rdr.ConstUtf8(rdr.Read16());

            rdr.Where.Push($"field '{Name}'");

            Type = new JavaType(rdr.ConstUtf8(rdr.Read16()), rdr.Where);

            var attributes = new JavaAttributeSet(rdr);

            if ((Flags & JavaAccessFlags.ACC_STATIC) != 0)
            {
                var constAttr = attributes.GetAttr<JavaAttribute.ConstantValue>();
                if (constAttr != null)
                {
                    InitConstant(constAttr.value, rdr.Where);
                }
            }

            rdr.Where.Pop();
        }



        public void InitConstant(object value, JavaException.Where Where)
        {
            bool ok = false;
            if (Type.ArrayRank == 0 && (Flags & JavaAccessFlags.ACC_STATIC) != 0)
            {
                if (     (Type.ClassName == "java.lang.String" && value is string)
                     || (Type.PrimitiveType == TypeCode.Double && value is double)
                     || (Type.PrimitiveType == TypeCode.Single && value is float)
                     || (Type.PrimitiveType == TypeCode.Int64 && value is long))
                {
                    ok = true;
                }
                else if (Type.PrimitiveType == TypeCode.UInt64 && value is ulong)
                {
                    value = (long) ((ulong) value);
                    ok = true;
                }
                else
                {
                    switch (value)
                    {
                        case bool boolValue:      value = (int) (boolValue ? 1 : 0);  break;
                        case byte byteValue:      value = (int) byteValue;            break;
                        case sbyte sbyteValue:    value = (int) sbyteValue;           break;
                        case char charValue:      value = (int) charValue;            break;
                        case short shortValue:    value = (int) shortValue;           break;
                        case ushort ushortValue:  value = (int) ushortValue;          break;
                        case uint uintValue:      value = (int) uintValue;            break;
                    }

                    if (value is int)
                    {
                        if (    Type.PrimitiveType == TypeCode.Boolean
                             || Type.PrimitiveType == TypeCode.Byte
                             || Type.PrimitiveType == TypeCode.SByte
                             || Type.PrimitiveType == TypeCode.Char
                             || Type.PrimitiveType == TypeCode.Int16
                             || Type.PrimitiveType == TypeCode.UInt16
                             || Type.PrimitiveType == TypeCode.Int32
                             || Type.PrimitiveType == TypeCode.UInt32)
                        {
                            ok = true;
                        }
                    }
                }
            }
            if (ok)
                Constant = value;
            else
                throw Where.Exception($"bad constant value or non-static field of type '{Type}'");
        }



        /*
         * JavaFieldPrinter
         */



        public void Print(IndentedText txt)
        {
            txt.Write("/* {0} */ {1}{2}{3}{4}{5}{6} {7}",
                /* 0 */ ((ushort) Flags).ToString("X4"),
                /* 1 */ ((Flags & JavaAccessFlags.ACC_PUBLIC) != 0 ? "public " :
                            ((Flags & JavaAccessFlags.ACC_PRIVATE) != 0 ? "private " :
                                ((Flags & JavaAccessFlags.ACC_PROTECTED) != 0 ? "protected " :
                                    string.Empty))),
                /* 2 */ ((Flags & JavaAccessFlags.ACC_STATIC) != 0 ? "static " : string.Empty),
                /* 3 */ ((Flags & JavaAccessFlags.ACC_FINAL) != 0 ? "final " : string.Empty),
                /* 4 */ ((Flags & JavaAccessFlags.ACC_TRANSIENT) != 0 ? "transient " : string.Empty),
                /* 5 */ ((Flags & JavaAccessFlags.ACC_VOLATILE) != 0 ? "volatile " : string.Empty),
                /* 6 */ Type,
                /* 7 */ Name);

            if (Constant != null)
            {
                txt.Write(" = ");
                if (Constant is string)
                    txt.Write("\"{0}\"", Constant);
                else
                    txt.Write(Constant.ToString());
            }

            txt.Write(";");
            txt.NewLine();
        }



        /*
         * JavaFieldWriter
         */



        public void Write(JavaWriter wtr)
        {
            wtr.Where.Push($"field '{Name}'");

            wtr.Write16((ushort) Flags);
            wtr.Write16(wtr.ConstUtf8(Name));
            wtr.Write16(wtr.ConstUtf8(Type.ToDescriptor()));

            if (Constant != null)
            {
                var attributes = new JavaAttributeSet();
                attributes.Put(new JavaAttribute.ConstantValue(Constant));
                attributes.Write(wtr);
            }
            else
                wtr.Write16(0); // attributes

            wtr.Where.Pop();
        }

    }

}

