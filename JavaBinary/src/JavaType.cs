
using System;

namespace SpaceFlint.JavaBinary
{

    public class JavaType : IEquatable<JavaType>
    {

        public TypeCode PrimitiveType { get; protected set; }
        public int ArrayRank { get; protected set; }
        public string ClassName { get; protected set; }



        public JavaType()
        {
        }



        public JavaType(TypeCode primitiveType, int arrayRank, string className)
        {
            (PrimitiveType, ArrayRank, ClassName) =
                (primitiveType, arrayRank, className);
        }



        public JavaType(string descriptor, JavaException.Where Where)
        {
            if (ParseType(descriptor, 0) != descriptor.Length)
                throw Where.Exception($"bad field descriptor {descriptor}");
        }



        protected void CopyFrom(JavaType other)
        {
            (PrimitiveType, ArrayRank, ClassName) =
                (other.PrimitiveType, other.ArrayRank, other.ClassName);
        }



        internal int ParseType(string descriptor, int index)
        {
            switch (descriptor[index++])
            {
                case 'B':
                    PrimitiveType = TypeCode.SByte;
                    break;

                case 'C':
                    PrimitiveType = TypeCode.Char;
                    break;

                case 'D':
                    PrimitiveType = TypeCode.Double;
                    break;

                case 'F':
                    PrimitiveType = TypeCode.Single;
                    break;

                case 'I':
                    PrimitiveType = TypeCode.Int32;
                    break;

                case 'J':
                    PrimitiveType = TypeCode.Int64;
                    break;

                case 'S':
                    PrimitiveType = TypeCode.Int16;
                    break;

                case 'Z':
                    PrimitiveType = TypeCode.Boolean;
                    break;

                case '[':
                    ArrayRank = 1;
                    while (index < descriptor.Length && descriptor[index++] == '[')
                        ArrayRank++;
                    index = ParseType(descriptor, index - 1);
                    break;

                case 'L':
                    var index2 = descriptor.IndexOf(';', index);
                    if (index2 != -1)
                    {
                        ClassName = descriptor.Substring(index, index2 - index).Replace('/', '.');
                        index = index2 + 1;
                    }
                    break;

                default:
                    index = -1;
                    break;
            }

            return index;
        }



        public override string ToString()
        {
            string str = ClassName;
            if (str == null)
                str = typeInfo[(int) PrimitiveType].Name;

            if (PrimitiveType == TypeCode.DBNull && str == JavaStackMap.UninitializedNew.ClassName)
                return $"{str}({ArrayRank:X4})";

            int rank = ArrayRank;
            while (rank-- > 0)
                str += "[]";

            return str;
        }



        public string ToDescriptor()
        {
            var prefix = (ArrayRank != 0) ? new string('[', ArrayRank) : "";

            if (ClassName == null)
            {
                char ch = typeInfo[(int) PrimitiveType].Descriptor;
                return prefix + ch;
            }
            else
            {
                return prefix + 'L' + ClassName.Replace('.', '/') + ';';
            }
        }



        public bool Equals(JavaType other)
        {
            return (    PrimitiveType == other.PrimitiveType
                     && ArrayRank == other.ArrayRank
                     && ClassName == other.ClassName);
        }



        public override int GetHashCode()
        {
            return ClassName?.GetHashCode() ?? (int) PrimitiveType;
        }



        public virtual bool AssignableTo(JavaType other)
        {
            // used by JavaStackMap::MergeFrame to determine if conflicting types
            // in two frames can be merged to the same type.  note that the caller
            // already tried equality comparison, so we don't repeat it here

            if (! IsReference)
                return (InitOpcode == other.InitOpcode);        // if both are int-like

            if (other.Equals(ObjectType))
                return true;            // any reference is assignable to java.lang.Object

            if (ArrayRank != 0)
            {
                if (other.Equals(CloneableInterface) || other.Equals(SerializableInterface))
                    return true;        // arrays implicitly implement these two interfaces
            }

            return false;
        }

        public virtual JavaType ResolveConflict(JavaType other)
        {
            // the ternary operator ?: may produce code that causes a conflict:
            //      object x = flag ? (object) new A() : (object) new B();
            // if we detect such a conflict at a branch target, we assume this
            // is the cause, and set the stack elements to java.lang.Object
            return (IsReference && other.IsReference) ? JavaType.ObjectType : null;
        }



        //public bool IsPrimitive => (ClassName == null && ArrayRank == 0);
        public bool IsReference => (ClassName != null || ArrayRank != 0);
        public bool IsArray     => (ArrayRank != 0);



        public int Category
        {
            get
            {
                return ((! IsReference) && (    PrimitiveType == TypeCode.Int64
                                             || PrimitiveType == TypeCode.UInt64
                                             || PrimitiveType == TypeCode.Double))
                        ? 2 : 1;    // per JVMS Table 2.11.1-B
            }
        }



        public bool IsIntLike
        {
            get
            {
                return (InitOpcode == 0x03);
            }
        }



        public byte InitOpcode
        {
            get
            {
                return IsReference ? (byte) 0x01 // aconst_null (reference)
                                   : typeInfo[(int) PrimitiveType].InitOpcode;
            }
        }



        public byte LoadOpcode
        {
            get
            {
                return IsReference ? (byte) 0x19 // aload (reference)
                                   : typeInfo[(int) PrimitiveType].LoadOpcode;
            }
        }



        public byte StoreOpcode
        {
            get
            {
                return IsReference ? (byte) 0x3A // astore (reference)
                                   : typeInfo[(int) PrimitiveType].StoreOpcode;
            }
        }



        public byte ReturnOpcode
        {
            get
            {
                return IsReference ? (byte) 0xB0 // areturn (reference)
                                   : typeInfo[(int) PrimitiveType].ReturnOpcode;
            }
        }



        public byte NewArrayType
        {
            get
            {
                return IsReference ? throw new ArgumentException()
                                   : typeInfo[(int) PrimitiveType].NewArrayCode;
            }
        }



        public byte LoadArrayOpcode
        {
            get
            {
                return IsReference ? (byte) 0x32 // aaload (reference)
                                   : typeInfo[(int) PrimitiveType].LoadArrayCode;
            }
        }



        public byte StoreArrayOpcode
        {
            get
            {
                return IsReference ? (byte) 0x53 // aastore (reference)
                                   : typeInfo[(int) PrimitiveType].StoreArrayCode;
            }
        }



        public JavaType Wrapper
            => IsReference ? null : new JavaType(0, 0,
                                "java.lang." + typeInfo[(int) PrimitiveType].Wrapper);



        static JavaType()
        {
            typeInfo = new TypeInfo[14 + 1];

            typeInfo[(int) TypeCode.Empty] = new TypeInfo
            {
                Name = "void", Wrapper = "Void", Descriptor = '?',
                ReturnOpcode = 0xB1,    // return (void)
            };

            typeInfo[(int) TypeCode.Boolean] = new TypeInfo
            {
                Name = "boolean", Wrapper = "Boolean", Descriptor = 'Z',
                InitOpcode = 0x03,      // iconst_0 (int)
                LoadOpcode = 0x15,      // iload (int)
                StoreOpcode = 0x36,     // istore (int)
                ReturnOpcode = 0xAC,    // ireturn (int)
                NewArrayCode = 4,       // boolean array
                LoadArrayCode = 0x33,   // baload
                StoreArrayCode = 0x54,  // bastore
            };

            typeInfo[(int) TypeCode.Char] = typeInfo[(int) TypeCode.UInt16] = new TypeInfo
            {   Name = "char", Wrapper = "Character", Descriptor = 'C',
                InitOpcode = 0x03,      // iconst_0 (int)
                LoadOpcode = 0x15,      // iload (int)
                StoreOpcode = 0x36,     // istore (int)
                ReturnOpcode = 0xAC,    // ireturn (int)
                NewArrayCode = 5,       // char array
                LoadArrayCode = 0x34,   // caload
                StoreArrayCode = 0x55,  // castore
            };

            typeInfo[(int) TypeCode.SByte] = typeInfo[(int) TypeCode.Byte] = new TypeInfo
            {
                Name = "byte", Wrapper = "Byte", Descriptor = 'B',
                InitOpcode = 0x03,      // iconst_0 (int)
                LoadOpcode = 0x15,      // iload (int)
                StoreOpcode = 0x36,     // istore (int)
                ReturnOpcode = 0xAC,    // ireturn (int)
                NewArrayCode = 8,       // byte array
                LoadArrayCode = 0x33,   // baload
                StoreArrayCode = 0x54,  // bastore
            };

            typeInfo[(int) TypeCode.Int16] = new TypeInfo
            {
                Name = "short", Wrapper = "Short", Descriptor = 'S',
                InitOpcode = 0x03,      // iconst_0 (int)
                LoadOpcode = 0x15,      // iload (int)
                StoreOpcode = 0x36,     // istore (int)
                ReturnOpcode = 0xAC,    // ireturn (int)
                NewArrayCode = 9,       // short array
                LoadArrayCode = 0x35,   // saload
                StoreArrayCode = 0x56,  // sastore
            };

            typeInfo[(int) TypeCode.Int32] = typeInfo[(int) TypeCode.UInt32] = new TypeInfo
            {
                Name = "int", Wrapper = "Integer", Descriptor = 'I',
                InitOpcode = 0x03,      // iconst_0 (int)
                LoadOpcode = 0x15,      // iload (int)
                StoreOpcode = 0x36,     // istore (int)
                ReturnOpcode = 0xAC,    // ireturn (int)
                NewArrayCode = 10,      // int array
                LoadArrayCode = 0x2E,   // iaload
                StoreArrayCode = 0x4F,  // iastore
            };

            typeInfo[(int) TypeCode.Int64] = typeInfo[(int) TypeCode.UInt64] = new TypeInfo
            {
                Name = "long", Wrapper = "Long", Descriptor = 'J',
                InitOpcode = 0x09,      // lconst_0 (long)
                LoadOpcode = 0x16,      // lload (long)
                StoreOpcode = 0x37,     // lstore (long)
                ReturnOpcode = 0xAD,    // lreturn (long)
                NewArrayCode = 11,      // long array
                LoadArrayCode = 0x2F,   // laload
                StoreArrayCode = 0x50,  // lastore
            };

            typeInfo[(int) TypeCode.Single] = new TypeInfo
            {   Name = "float", Wrapper = "Float", Descriptor = 'F',
                InitOpcode = 0x0B,      // fconst_0 (float)
                LoadOpcode = 0x17,      // fload (float)
                StoreOpcode = 0x38,     // fstore (float)
                ReturnOpcode = 0xAE,    // freturn (float)
                NewArrayCode = 6,       // float array
                LoadArrayCode = 0x30,   // faload
                StoreArrayCode = 0x51,  // fastore
            };

            typeInfo[(int) TypeCode.Double] = new TypeInfo
            {
                Name = "double", Wrapper = "Double", Descriptor = 'D',
                InitOpcode = 0x0E,      // dconst_0 (double)
                LoadOpcode = 0x18,      // dload (double)
                StoreOpcode = 0x39,     // dstore (double)
                ReturnOpcode = 0xAF,    // dreturn (double)
                NewArrayCode = 7,       // double array
                LoadArrayCode = 0x31,   // daload
                StoreArrayCode = 0x52,  // dastore
            };

        }



        private class TypeInfo
        {
            internal string Name;
            internal string Wrapper;
            internal char Descriptor;
            internal byte InitOpcode;
            internal byte LoadOpcode;
            internal byte StoreOpcode;
            internal byte ReturnOpcode;
            internal byte NewArrayCode;
            internal byte LoadArrayCode;
            internal byte StoreArrayCode;
        }



        private static readonly TypeInfo[] typeInfo;
        public static readonly JavaType VoidType = new JavaType();
        public static readonly JavaType ObjectType = new JavaType(0, 0, "java.lang.Object");
        public static readonly JavaType ClassType = new JavaType(0, 0, "java.lang.Class");
        public static readonly JavaType StringType = new JavaType(0, 0, "java.lang.String");
        public static readonly JavaType CloneableInterface = new JavaType(0, 0, "java.lang.Cloneable");
        public static readonly JavaType SerializableInterface = new JavaType(0, 0, "java.io.Serializable");
        public static readonly JavaType ThrowableType = new JavaType(0, 0, "java.lang.Throwable");
        public static readonly JavaType BooleanType = new JavaType(TypeCode.Boolean, 0, null);
        public static readonly JavaType CharacterType = new JavaType(TypeCode.Char, 0, null);
        public static readonly JavaType ShortType = new JavaType(TypeCode.Int16, 0, null);
        public static readonly JavaType IntegerType = new JavaType(TypeCode.Int32, 0, null);
        public static readonly JavaType LongType = new JavaType(TypeCode.Int64, 0, null);
        public static readonly JavaType FloatType = new JavaType(TypeCode.Single, 0, null);
        public static readonly JavaType DoubleType = new JavaType(TypeCode.Double, 0, null);

    }

}
