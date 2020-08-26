
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public class JavaWriter
    {

        internal JavaException.Where Where;

        Stream stream;
        Stack<Stream> forkedStreams;
        JavaConstantPool constants;
        internal JavaAttribute.BootstrapMethods bootstrapMethods;



        public static void WriteClass(JavaClass jclass, Stream stream)
        {
            new JavaWriter(stream, jclass);
        }



        JavaWriter(Stream _stream, JavaClass _class)
        {
            Where = new JavaException.Where();

            var memoryStream = new MemoryStream();
            stream = memoryStream;
            forkedStreams = new Stack<Stream>();

            constants = new JavaConstantPool();

            _class.Write(this);

            stream = _stream;

            Write32(0xCAFEBABE);
            Write16(0);
            Write16(52);

            constants.Write(this);
            memoryStream.WriteTo(_stream);
            memoryStream.Dispose();
        }



        public void Write8(byte v)
        {
            stream.WriteByte(v);
        }



        public void Write16(ushort v)
        {
            Write8((byte) ((v >> 8) & 0xFF));
            Write8((byte) (v & 0xFF));
        }



        public void Write32(uint v)
        {
            Write16((ushort) ((v >> 16) & 0xFFFF));
            Write16((ushort) (v & 0xFFFF));
        }



        public void WriteBlock(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }



        public void WriteString(string v)
        {
            var bytes = JavaUtf8.Encode(v);
            int count = bytes.Length;
            if (count > 65535)
                throw Where.Exception("string too long");
            Write16((ushort) count);
            WriteBlock(bytes);
        }



        public void Write16(int v)
        {
            var v16 = (ushort) v;
            if (((int) v16) != v)
                throw Where.Exception("invalid 16-bit number");
            Write16(v16);
        }



        public void Fork()
        {
            forkedStreams.Push(stream);
            stream = new MemoryStream();
        }



        public void Join()
        {
            if (forkedStreams.Count == 0)
                throw Where.Exception("cannot join without fork");

            var forkedBuffer = ((MemoryStream) stream).GetBuffer();
            var forkedLength = ((MemoryStream) stream).Length;

            if (forkedLength > int.MaxValue)
                throw Where.Exception("forked stream is too large");

            stream = forkedStreams.Pop();

            Write32((uint) forkedLength);
            stream.Write(forkedBuffer, 0, (int) forkedLength);
        }



        public ushort ConstUtf8(string v)
        {
            if (v == null)
                throw new NullReferenceException();
            return (ushort) constants.Put(new JavaConstant.Utf8(v), Where);
        }



        public ushort ConstInteger(int v)
        {
            return (ushort) constants.Put(new JavaConstant.Integer(v), Where);
        }



        public ushort ConstFloat(float v)
        {
            return (ushort) constants.Put(new JavaConstant.Float(v), Where);
        }



        public ushort ConstLong(long v)
        {
            return (ushort) constants.Put(new JavaConstant.Long(v), Where);
        }



        public ushort ConstDouble(double v)
        {
            return (ushort) constants.Put(new JavaConstant.Double(v), Where);
        }



        public ushort ConstString(string v)
        {
            ushort stringIndex = ConstUtf8(v);
            return (ushort) constants.Put(new JavaConstant.String(stringIndex), Where);
        }



        public ushort ConstClass(JavaType v)
        {
            string nameOrDescriptor;
            if (v.ArrayRank != 0)
                nameOrDescriptor = v.ToDescriptor();
            else if (v.ClassName != null)
                nameOrDescriptor = v.ClassName.Replace('.', '/');
            else
                throw Where.Exception("invalid class name");

            ushort stringIndex = ConstUtf8(nameOrDescriptor);
            return (ushort) constants.Put(new JavaConstant.Class(stringIndex), Where);
        }



        public ushort ConstClass(string className)
        {
            return ConstClass(new JavaType(0, 0, className));
        }



        public ushort ConstNameAndType(string name, string descriptor)
        {
            ushort nameIndex = ConstUtf8(name);
            ushort descriptorIndex = ConstUtf8(descriptor);
            return (ushort) constants.Put(
                new JavaConstant.NameAndType(nameIndex, descriptorIndex), Where);
        }



        public ushort ConstField(JavaType vClass, JavaFieldRef vField)
        {
            ushort classIndex = ConstClass(vClass);
            ushort nameAndTypeIndex = ConstNameAndType(vField.Name, vField.Type.ToDescriptor());
            return (ushort) constants.Put(
                new JavaConstant.FieldRef(classIndex, nameAndTypeIndex), Where);
        }



        public ushort ConstMethod(JavaType vClass, JavaMethodRef vMethod)
        {
            ushort classIndex = ConstClass(vClass);
            ushort nameAndTypeIndex = ConstNameAndType(vMethod.Name, vMethod.ToDescriptor());
            return (ushort) constants.Put(
                new JavaConstant.MethodRef(classIndex, nameAndTypeIndex), Where);
        }



        public ushort ConstInterfaceMethod(JavaType vClass, JavaMethodRef vMethod)
        {
            ushort classIndex = ConstClass(vClass);
            ushort nameAndTypeIndex = ConstNameAndType(vMethod.Name, vMethod.ToDescriptor());
            return (ushort) constants.Put(
                new JavaConstant.InterfaceMethodRef(classIndex, nameAndTypeIndex), Where);
        }



        public ushort ConstMethodHandle(JavaMethodHandle vMethodHandle)
        {
            return (ushort) constants.Put(vMethodHandle.ToConstant(this), Where);
        }



        public ushort ConstMethodType(JavaMethodType vMethodType)
        {
            ushort descriptorIndex = ConstUtf8(vMethodType.ToDescriptor());
            return (ushort) constants.Put(new JavaConstant.MethodType(descriptorIndex), Where);
        }



        public ushort ConstInvokeDynamic(JavaCallSite vCallSite)
        {
            ushort nameAndTypeIndex = ConstNameAndType(
                        vCallSite.InvokedMethod.Name, vCallSite.InvokedMethod.ToDescriptor());

            if (vCallSite.BootstrapMethodIndex != 0xFFFF)
                throw Where.Exception("invalid call site argument");

            if (bootstrapMethods == null)
                bootstrapMethods = new JavaAttribute.BootstrapMethods();

            vCallSite.BootstrapMethodIndex = (ushort) bootstrapMethods.FindOrCreateItem(
                                        vCallSite.BootstrapMethod, vCallSite.BootstrapArgs);

            return (ushort) constants.Put(
                new JavaConstant.InvokeDynamic(
                        vCallSite.BootstrapMethodIndex, nameAndTypeIndex), Where);
        }

    }


}
