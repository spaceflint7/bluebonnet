
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public class JavaReader
    {

        public class JavaClassEx
        {
            public JavaClass JavaClass;
            public JavaConstantPool Constants;
            public byte[] RawBytes;
        }

        internal JavaClass Class;
        internal JavaException.Where Where;

        Stream stream;
        JavaConstantPool constants;
        internal List<JavaCallSite> callSites;



        public static JavaClass ReadClass(Stream stream, bool withCode = true)
        {
            string whereText;
            if (stream is FileStream fileStream)
                whereText = $"class file '{fileStream.Name}'";
            else
                whereText = "unnamed stream";
            return (new JavaReader(stream, whereText, withCode)).Class;
        }



        public static JavaClassEx ReadClassEx(System.IO.Compression.ZipArchiveEntry entry,
                                              bool withCode = true)
        {
            if (entry.Length > 4 &&
                    (! string.IsNullOrEmpty(Path.GetFileName(entry.FullName))))
            {
                using (var stream = entry.Open())
                {
                    var (b0, b1, b2, b3) = (stream.ReadByte(), stream.ReadByte(),
                                            stream.ReadByte(), stream.ReadByte());
                    if (b0 == 0xCA && b1 == 0xFE && b2 == 0xBA && b3 == 0xBE)
                    {
                        using (var stream2 = new MemoryStream())
                        {
                            stream2.WriteByte((byte) b0);
                            stream2.WriteByte((byte) b1);
                            stream2.WriteByte((byte) b2);
                            stream2.WriteByte((byte) b3);
                            stream.CopyTo(stream2);
                            stream2.Position = 0;

                            var whereText = $"entry '{entry.FullName}' in archive";
                            var rdr = new JavaReader(stream2, whereText, withCode);

                            if (rdr.Class != null)
                            {
                                rdr.Class.PackageNameLength =
                                        (short) Path.GetDirectoryName(entry.FullName).Length;

                                return new JavaClassEx
                                {
                                    JavaClass = rdr.Class,
                                    Constants = rdr.constants,
                                    RawBytes = stream2.ToArray()
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }



        public static JavaClass ReadClass(System.IO.Compression.ZipArchiveEntry entry,
                                          bool withCode = true)
            => ReadClassEx(entry, withCode)?.JavaClass;



        JavaReader(Stream _stream, string whereText, bool withCode)
        {
            Where = new JavaException.Where();
            Where.Push(whereText);

            stream = _stream;

            if (Read32() != 0xCAFEBABE)
                throw Where.Exception("bad class magic number");

            var (minorVersion, majorVersion) = (Read16(), Read16());
            if (majorVersion < 45)
                throw Where.Exception("bad class major version number");

            constants = new JavaConstantPool(this);

            new JavaClass(this, majorVersion, minorVersion, withCode);

            Where.Pop();
        }



        public byte Read8()
        {
            var b = stream.ReadByte();
            if (b == -1)
                throw new EndOfStreamException();
            return (byte) b;
        }



        public ushort Read16()
        {
            var (hi, lo) = (Read8(), Read8());
            return (ushort) (((ushort) hi) << 8 | ((ushort) lo));
        }



        public uint Read32()
        {
            var (hi, lo) = (Read16(), Read16());
            return ((uint) hi) << 16 | ((uint) lo);
        }



        public byte[] ReadBlock(int count)
        {
            var bytes = new byte[count];
            if (stream.Read(bytes, 0, count) != count)
                throw new EndOfStreamException();
            return bytes;
        }



        public string ReadString()
        {
            var count = Read16();
            var bytes = ReadBlock(count);
            return JavaUtf8.Decode(bytes, Where);
        }



        public long StreamPosition
        {
            get
            {
                return stream.Position;
            }
        }



        public Type ConstType(int constantIndex)
        {
            return constants.Get(constantIndex)?.GetType();
        }



        public string ConstUtf8(int constantIndex)
        {
            var utf8Const = constants.Get<JavaConstant.Utf8>(constantIndex, Where);
            return utf8Const.str;
        }



        public int ConstInteger(int constantIndex)
        {
            var integerConst = constants.Get<JavaConstant.Integer>(constantIndex, Where);
            return integerConst.value;
        }



        public float ConstFloat(int constantIndex)
        {
            var floatConst = constants.Get<JavaConstant.Float>(constantIndex, Where);
            return floatConst.value;
        }



        public long ConstLong(int constantIndex)
        {
            var longConst = constants.Get<JavaConstant.Long>(constantIndex, Where);
            return longConst.value;
        }



        public double ConstDouble(int constantIndex)
        {
            var doubleConst = constants.Get<JavaConstant.Double>(constantIndex, Where);
            return doubleConst.value;
        }



        public string ConstString(int constantIndex)
        {
            var stringConst = constants.Get<JavaConstant.String>(constantIndex, Where);

            return stringConst.cached
                ?? (stringConst.cached = ConstUtf8(stringConst.stringIndex));
        }



        public JavaType ConstClass(int constantIndex)
        {
            var classConst = constants.Get<JavaConstant.Class>(constantIndex, Where);

            if (classConst.cached == null)
            {
                var className = ConstUtf8(classConst.stringIndex).Replace('/', '.');
                classConst.cached = (className[0] == '[')
                                  ? new JavaType(className, Where)
                                  : new JavaType(0, 0, className);
            }

            return classConst.cached;
        }



        public (JavaType, JavaFieldRef) ConstField(int constantIndex)
        {
            var fieldConst = constants.Get<JavaConstant.FieldRef>(constantIndex, Where);

            if (   fieldConst.cachedClass == null
                || fieldConst.cachedField == null)
            {
                fieldConst.cachedClass = ConstClass(fieldConst.classIndex);
                fieldConst.cachedField = ConstNameAndTypeField(fieldConst.nameAndTypeIndex);
            }

            return (fieldConst.cachedClass, fieldConst.cachedField);
        }



        public (JavaType, JavaMethodRef) ConstMethod(int constantIndex)
        {
            var methodConst = constants.Get<JavaConstant.MethodRef>(constantIndex, Where);
            return ConstMethod2(methodConst);
        }



        public (JavaType, JavaMethodRef) ConstInterfaceMethod(int constantIndex)
        {
            var interfaceMethodConst =
                    constants.Get<JavaConstant.InterfaceMethodRef>(constantIndex, Where);
            return ConstMethod2(interfaceMethodConst);
        }



        (JavaType, JavaMethodRef) ConstMethod2(JavaConstant.MethodRef methodConst)
        {
            if (   methodConst.cachedClass == null
                || methodConst.cachedMethod == null)
            {
                methodConst.cachedClass = ConstClass(methodConst.classIndex);
                methodConst.cachedMethod = ConstNameAndTypeMethod(methodConst.nameAndTypeIndex);
            }

            return (methodConst.cachedClass, methodConst.cachedMethod);
        }



        public JavaFieldRef ConstNameAndTypeField(int constantIndex)
        {
            var nameAndTypeConst = constants.Get<JavaConstant.NameAndType>(constantIndex, Where);

            return nameAndTypeConst.cachedField ?? (nameAndTypeConst.cachedField =
                    new JavaFieldRef(ConstUtf8(nameAndTypeConst.nameIndex),
                                     ConstUtf8(nameAndTypeConst.descriptorIndex),
                                     Where));
        }



        public JavaMethodRef ConstNameAndTypeMethod(int constantIndex)
        {
            var nameAndTypeConst = constants.Get<JavaConstant.NameAndType>(constantIndex, Where);

            return nameAndTypeConst.cachedMethod ?? (nameAndTypeConst.cachedMethod =
                    new JavaMethodRef(ConstUtf8(nameAndTypeConst.nameIndex),
                                      ConstUtf8(nameAndTypeConst.descriptorIndex),
                                      Where));
        }



        public JavaMethodHandle ConstMethodHandle(int constantIndex)
        {
            var handleConst = constants.Get<JavaConstant.MethodHandle>(constantIndex, Where);

            return handleConst.cached ?? (handleConst.cached =
                    new JavaMethodHandle(this, handleConst.referenceKind, handleConst.referenceIndex));
        }



        public JavaMethodType ConstMethodType(int constantIndex)
        {
            var methodTypeConst = constants.Get<JavaConstant.MethodType>(constantIndex, Where);

            if (methodTypeConst.cached == null)
            {
                string descriptor = ConstUtf8(methodTypeConst.descriptorIndex);
                methodTypeConst.cached = new JavaMethodType(descriptor, Where);
            }

            return methodTypeConst.cached;
        }



        public JavaCallSite ConstInvokeDynamic(int constantIndex)
        {
            var dynamicConst = constants.Get<JavaConstant.InvokeDynamic>(constantIndex, Where);

            if (dynamicConst.cached == null)
            {
                dynamicConst.cached = new JavaCallSite(dynamicConst.bootstrapMethodIndex,
                                            ConstNameAndTypeMethod(dynamicConst.nameAndTypeIndex));
            }

            return dynamicConst.cached;
        }

    }

}
