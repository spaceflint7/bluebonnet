
using System;

namespace SpaceFlint.JavaBinary
{

    public abstract class JavaAttribute
    {

        public abstract void Write(JavaWriter wtr);



        public static int ReadLength(JavaReader rdr, string name)
        {
            var length = rdr.Read32();
            if (length >= int.MaxValue)
                throw rdr.Where.Exception($"{name} attribute too large");
            return (int) length;
        }



        public static void Write(JavaWriter wtr, string name, int length)
        {
            wtr.Write16(wtr.ConstUtf8(name));
            wtr.Write32((uint) length);
        }



        public class Generic : JavaAttribute
        {
            public string name;
            public byte[] data;

            public Generic(JavaReader rdr, string name, int length)
            {
                this.name = name;
                data = rdr.ReadBlock(length);
            }

            public override void Write(JavaWriter wtr)
            {
                Write(wtr, name, data.Length);
                wtr.WriteBlock(data);
            }
        }



        public class SourceFile : JavaAttribute
        {
            public const string tag = "SourceFile";
            public string fileName;

            public SourceFile(JavaReader rdr)
            {
                fileName = rdr.ConstUtf8(rdr.Read16());
            }

            public SourceFile(string _fileName)
            {
                fileName = _fileName;
            }

            public override void Write(JavaWriter wtr)
            {
                Write(wtr, tag, sizeof(ushort));
                wtr.Write16(wtr.ConstUtf8(fileName));
            }
        }



        public class Signature : JavaAttribute
        {
            public const string tag = "Signature";
            public string descriptor;

            public Signature(JavaReader rdr)
            {
                descriptor = rdr.ConstUtf8(rdr.Read16());
            }

            public Signature(string _descriptor)
            {
                descriptor = _descriptor;
            }

            public override void Write(JavaWriter wtr)
            {
                Write(wtr, tag, sizeof(ushort));
                wtr.Write16(wtr.ConstUtf8(descriptor));
            }
        }



        public class Exceptions : JavaAttribute
        {
            public const string tag = "Exceptions";
            public JavaType[] classes;

            public Exceptions(JavaReader rdr)
            {
                classes = new JavaType[rdr.Read16()];
                for (int i = 0; i < classes.Length; i++)
                    classes[i] = rdr.ConstClass(rdr.Read16());
            }

            public override void Write(JavaWriter wtr)
            {
                int length = sizeof(ushort) // classes.Length
                           + classes.Length * sizeof(ushort);
                Write(wtr, tag, length);
                wtr.Write16(classes.Length);
                for (int i = 0; i < classes.Length; i++)
                    wtr.Write16(wtr.ConstClass(classes[i]));
            }
        }



        public class InnerClasses : JavaAttribute
        {
            public const string tag = "InnerClasses";
            public JavaClass.InnerClass[] classes;

            public InnerClasses(JavaReader rdr)
            {
                classes = new JavaClass.InnerClass[rdr.Read16()];
                for (int i = 0; i < classes.Length; i++)
                {
                    classes[i] = new JavaClass.InnerClass();
                    classes[i].InnerLongName = rdr.ConstClass(rdr.Read16()).ClassName;
                    ushort v = rdr.Read16();
                    if (v != 0)
                        classes[i].OuterLongName = rdr.ConstClass(v).ClassName;
                    v = rdr.Read16();
                    if (v != 0)
                        classes[i].InnerShortName = rdr.ConstUtf8(v);
                    classes[i].Flags = (JavaAccessFlags) rdr.Read16();
                }
            }

            public InnerClasses(JavaClass.InnerClass[] _classes)
            {
                classes = _classes;
            }

            public override void Write(JavaWriter wtr)
            {
                int length = sizeof(ushort) // classes.Length
                           + classes.Length * sizeof(ushort) * 4;
                Write(wtr, tag, length);
                wtr.Write16(classes.Length);
                for (int i = 0; i < classes.Length; i++)
                {
                    wtr.Write16(wtr.ConstClass(classes[i].InnerLongName));
                    ushort v = (classes[i].OuterLongName == null) ? (ushort) 0
                             : wtr.ConstClass(classes[i].OuterLongName);
                    wtr.Write16(v);
                    v = (classes[i].InnerShortName == null) ? (ushort) 0
                      : wtr.ConstUtf8(classes[i].InnerShortName);
                    wtr.Write16(v);
                    wtr.Write16((ushort) classes[i].Flags);
                }
            }
        }



        public class EnclosingMethod : JavaAttribute
        {
            public const string tag = "EnclosingMethod";
            public string className;
            public ushort methodConst;  // partial implementation

            public EnclosingMethod(JavaReader rdr)
            {
                className = rdr.ConstClass(rdr.Read16()).ClassName;
                methodConst = rdr.Read16();
            }

            public override void Write(JavaWriter wtr)
            {
                Write(wtr, tag, sizeof(ushort) * 2);
                wtr.Write16(wtr.ConstClass(className));
                wtr.Write16(methodConst);
            }
        }



        public class ConstantValue : JavaAttribute
        {
            public const string tag = "ConstantValue";
            public object value;

            public ConstantValue(object _value)
            {
                value = _value;
            }

            public ConstantValue(JavaReader rdr)
            {
                var constantIndex = rdr.Read16();
                var constantType = rdr.ConstType(constantIndex);

                if (constantType == typeof(JavaConstant.Integer))
                    value = rdr.ConstInteger(constantIndex);

                else if (constantType == typeof(JavaConstant.Float))
                    value = rdr.ConstFloat(constantIndex);

                else if (constantType == typeof(JavaConstant.Long))
                    value = rdr.ConstLong(constantIndex);

                else if (constantType == typeof(JavaConstant.Double))
                    value = rdr.ConstDouble(constantIndex);

                else if (constantType == typeof(JavaConstant.String))
                    value = rdr.ConstString(constantIndex);

                else
                    throw rdr.Where.Exception($"invalid constant value");
            }

            public override void Write(JavaWriter wtr)
            {
                Write(wtr, tag, sizeof(ushort));
                ushort constantIndex;
                if (value is int intValue)
                    constantIndex = wtr.ConstInteger(intValue);
                else if (value is float floatValue)
                    constantIndex = wtr.ConstFloat(floatValue);
                else if (value is long longValue)
                    constantIndex = wtr.ConstLong(longValue);
                else if (value is double doubleValue)
                    constantIndex = wtr.ConstDouble(doubleValue);
                else if (value is string stringValue)
                    constantIndex = wtr.ConstString(stringValue);
                else
                    throw wtr.Where.Exception($"invalid constant value");
                wtr.Write16(constantIndex);
            }
        }



        public class Code : JavaAttribute
        {
            public const string tag = "Code";
            public ushort maxStack;
            public ushort maxLocals;
            public byte[] code;
            public struct Exception
            {
                public ushort start;
                public ushort endPlus1;
                public ushort handler;
                public string catchType;
            }
            public Exception[] exceptions;
            public JavaAttributeSet attributes;

            public Code()
            {
                attributes = new JavaAttributeSet();
            }

            public Code(JavaReader rdr)
            {
                rdr.Where.Push(tag);
                (maxStack, maxLocals) = (rdr.Read16(), rdr.Read16());
                var codeLength = ReadLength(rdr, "code");
                code = rdr.ReadBlock(codeLength);

                exceptions = new Exception[rdr.Read16()];
                for (int i = 0; i < exceptions.Length; i++)
                {
                    exceptions[i].start = rdr.Read16();
                    exceptions[i].endPlus1 = rdr.Read16();
                    exceptions[i].handler = rdr.Read16();
                    ushort catchType = rdr.Read16();
                    exceptions[i].catchType =
                        (catchType == 0 ? null : rdr.ConstClass(catchType).ClassName);
                }

                attributes = new JavaAttributeSet(rdr);
                rdr.Where.Pop();
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Where.Push(tag);
                wtr.Write16(wtr.ConstUtf8(tag));
                wtr.Fork();
                wtr.Write16(maxStack);
                wtr.Write16(maxLocals);
                wtr.Write32((uint) code.Length);
                wtr.WriteBlock(code);

                if (exceptions == null)
                    wtr.Write16(0);
                else
                {
                    wtr.Write16(exceptions.Length);
                    for (int i = 0; i < exceptions.Length; i++)
                    {
                        wtr.Write16(exceptions[i].start);
                        wtr.Write16(exceptions[i].endPlus1);
                        wtr.Write16(exceptions[i].handler);
                        wtr.Write16((exceptions[i].catchType == null) ? 0 :
                                        wtr.ConstClass(exceptions[i].catchType));
                    }
                }

                attributes.Write(wtr);
                wtr.Join();
                wtr.Where.Pop();
            }
        }



        public class LocalVariableTable : JavaAttribute
        {
            public const string tag = "LocalVariableTable";
            public struct Item
            {
                public ushort offset;
                public ushort length;
                public ushort index;
                public JavaFieldRef nameAndType;
            }
            public Item[] vars;

            public LocalVariableTable(JavaReader rdr)
            {
                vars = new Item[rdr.Read16()];
                for (int i = 0; i < vars.Length; i++)
                {
                    vars[i].offset = rdr.Read16();
                    vars[i].length = rdr.Read16();
                    vars[i].nameAndType = new JavaFieldRef(
                                            rdr.ConstUtf8(rdr.Read16()),
                                            rdr.ConstUtf8(rdr.Read16()),
                                            rdr.Where);
                    vars[i].index = rdr.Read16();
                }
            }

            public LocalVariableTable(
                        System.Collections.Generic.List<JavaFieldRef> varList, int codeSize)
            {
                int localIndex = 0;
                int varCount = varList.Count;
                vars = new Item[varCount];
                for (int varIndex = 0; varIndex < varCount; varIndex++)
                {
                    vars[varIndex].offset = 0;
                    vars[varIndex].length = (ushort) codeSize;
                    vars[varIndex].index = (ushort) localIndex;
                    vars[varIndex].nameAndType = varList[varIndex];
                    localIndex += varList[varIndex].Type.Category;
                }
            }

            public override void Write(JavaWriter wtr)
            {
                int length = sizeof(ushort) // number of entries
                           + vars.Length * sizeof(ushort) * 5;
                Write(wtr, tag, length);
                wtr.Write16(vars.Length);
                for (int i = 0; i < vars.Length; i++)
                {
                    wtr.Write16(vars[i].offset);
                    wtr.Write16(vars[i].length);
                    wtr.Write16(wtr.ConstUtf8(vars[i].nameAndType.Name));
                    wtr.Write16(wtr.ConstUtf8(vars[i].nameAndType.Type.ToDescriptor()));
                    wtr.Write16(vars[i].index);
                }
            }
        }



        public class LineNumberTable : JavaAttribute
        {
            public const string tag = "LineNumberTable";
            public struct Item
            {
                public ushort offset;
                public ushort lineNumber;
            }
            public Item[] lines;

            public LineNumberTable(Item[] _lines)
            {
                lines = _lines;
            }

            public LineNumberTable(JavaReader rdr)
            {
                lines = new Item[rdr.Read16()];
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i].offset = rdr.Read16();
                    lines[i].lineNumber = rdr.Read16();
                }
            }

            public override void Write(JavaWriter wtr)
            {
                int numLines = lines.Length;
                Write(wtr, tag, sizeof(ushort) * (1 + 2 * numLines));
                wtr.Write16(numLines);
                for (int i = 0; i < numLines; i++)
                {
                    wtr.Write16(lines[i].offset);
                    wtr.Write16(lines[i].lineNumber);
                }
            }
        }



        public class StackMapTable : JavaAttribute
        {
            public const string tag = "StackMapTable";
            public struct Slot
            {
                public byte type;
                public ushort extra;
            }
            public struct Item
            {
                public byte type;
                public ushort deltaOffset;
                public Slot[] locals;
                public Slot[] stack;
            }
            public Item[] frames;

            public StackMapTable(Item[] _frames)
            {
                frames = _frames;
            }

            public StackMapTable(JavaReader rdr)
            {
                frames = new Item[rdr.Read16()];
                for (int i = 0; i < frames.Length; i++)
                {
                    byte type = rdr.Read8();
                    frames[i].type = type;
                    if (type >= 247)
                        frames[i].deltaOffset = rdr.Read16();
                    else if (type <= 127)
                    {
                        if (type < 64)
                            frames[i].deltaOffset = type;
                        else
                            frames[i].deltaOffset = (ushort)(type - 64);
                    }

                    if (type >= 252)
                    {
                        int numLocals = (type == 255) ? rdr.Read16() : (type - 251);
                        var locals = new Slot[numLocals];
                        for (int j = 0; j < numLocals; j++)
                        {
                            byte localType = rdr.Read8();
                            locals[j].type = localType;
                            if (localType >= 7 && localType <= 8)
                                locals[j].extra = rdr.Read16();
                        }
                        frames[i].locals = locals;
                    }

                    if ((type >= 64 && type <= 127) || type == 247 || type == 255)
                    {
                        int numStack = (type == 255) ? rdr.Read16() : 1;
                        var stack = new Slot[numStack];
                        for (int j = 0; j < numStack; j++)
                        {
                            byte stackType = rdr.Read8();
                            stack[j].type = stackType;
                            if (stackType >= 7 && stackType <= 8)
                                stack[j].extra = rdr.Read16();
                        }
                        frames[i].stack = stack;
                    }
                }
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Write16(wtr.ConstUtf8(tag));
                wtr.Fork();
                wtr.Write16(frames.Length);

                for (int i = 0; i < frames.Length; i++)
                {
                    byte type = frames[i].type;
                    wtr.Write8(type);
                    if (type >= 247)
                        wtr.Write16(frames[i].deltaOffset);

                    var locals = frames[i].locals;
                    if (locals == null)
                        wtr.Write16(0);
                    else
                    {
                        wtr.Write16(locals.Length);
                        for (int j = 0; j < locals.Length; j++)
                        {
                            byte localType = locals[j].type;
                            wtr.Write8(localType);
                            if (localType >= 7 && localType <= 8)
                                wtr.Write16(locals[j].extra);
                        }
                    }

                    var stack = frames[i].stack;
                    if (stack == null)
                        wtr.Write16(0);
                    else
                    {
                        wtr.Write16(stack.Length);
                        for (int j = 0; j < stack.Length; j++)
                        {
                            byte stackType = stack[j].type;
                            wtr.Write8(stackType);
                            if (stackType >= 7 && stackType <= 8)
                                wtr.Write16(stack[j].extra);
                        }
                    }
                }

                wtr.Join();
            }
        }



        public class MethodParameters : JavaAttribute
        {
            public const string tag = "MethodParameters";
            public struct Item
            {
                public string name;
                public ushort flags;
            }
            public Item[] parms;

            public MethodParameters(JavaReader rdr)
            {
                parms = new Item[rdr.Read8()];
                for (int i = 0; i < parms.Length; i++)
                {
                    parms[i].name = rdr.ConstUtf8(rdr.Read16());
                    parms[i].flags = rdr.Read16();
                }
            }

            public override void Write(JavaWriter wtr)
            {
                throw wtr.Where.Exception(tag);
            }
        }



        public class BootstrapMethods : JavaAttribute
        {
            public const string tag = "BootstrapMethods";

            public struct Item
            {
                public JavaMethodHandle mh;
                public object[] args;
            }
            public Item[] methods;

            public BootstrapMethods()
            {
            }

            public BootstrapMethods(JavaReader rdr)
            {
                methods = new Item[rdr.Read16()];
                for (int i = 0; i < methods.Length; i++)
                {
                    methods[i].mh = rdr.ConstMethodHandle(rdr.Read16());
                    var args = new object[rdr.Read16()];
                    for (int j = 0; j < args.Length; j++)
                    {
                        var constantIndex = rdr.Read16();
                        var constantType = rdr.ConstType(constantIndex);
                        object value;

                        if (constantType == typeof(JavaConstant.Integer))
                            value = rdr.ConstInteger(constantIndex);

                        else if (constantType == typeof(JavaConstant.Float))
                            value = rdr.ConstFloat(constantIndex);

                        else if (constantType == typeof(JavaConstant.Long))
                            value = rdr.ConstLong(constantIndex);

                        else if (constantType == typeof(JavaConstant.Double))
                            value = rdr.ConstDouble(constantIndex);

                        else if (constantType == typeof(JavaConstant.Class))
                            value = rdr.ConstClass(constantIndex);

                        else if (constantType == typeof(JavaConstant.String))
                            value = rdr.ConstString(constantIndex);

                        else if (constantType == typeof(JavaConstant.MethodHandle))
                            value = rdr.ConstMethodHandle(constantIndex);

                        else if (constantType == typeof(JavaConstant.MethodType))
                            value = rdr.ConstMethodType(constantIndex);

                        else
                            throw rdr.Where.Exception($"invalid bootstrap method argument");

                        args[j] = value;
                    }
                    methods[i].args = args;
                }
            }

            public override void Write(JavaWriter wtr)
            {
                wtr.Write16(wtr.ConstUtf8(tag));
                wtr.Fork();
                wtr.Write16(methods.Length);
                for (int i = 0; i < methods.Length; i++)
                {
                    wtr.Write16(wtr.ConstMethodHandle(methods[i].mh));
                    var args = methods[i].args;
                    wtr.Write16(args.Length);
                    for (int j = 0; j < args.Length; j++)
                    {
                        ushort constantIndex;
                        var value = args[j];

                        if (value is int intValue)
                            constantIndex = wtr.ConstInteger(intValue);

                        else if (value is float floatValue)
                            constantIndex = wtr.ConstFloat(floatValue);

                        else if (value is long longValue)
                            constantIndex = wtr.ConstLong(longValue);

                        else if (value is double doubleValue)
                            constantIndex = wtr.ConstDouble(doubleValue);

                        else if (value is JavaType classValue)
                            constantIndex = wtr.ConstClass(classValue);

                        else if (value is string stringValue)
                            constantIndex = wtr.ConstString(stringValue);

                        else if (value is JavaMethodHandle methodHandleValue)
                            constantIndex = wtr.ConstMethodHandle(methodHandleValue);

                        else if (value is JavaMethodType methodTypeValue)
                            constantIndex = wtr.ConstMethodType(methodTypeValue);
                        else
                            throw wtr.Where.Exception($"invalid constant value");

                        wtr.Write16(constantIndex);
                    }
                }
                wtr.Join();
            }

            public int FindOrCreateItem(JavaMethodHandle mh, object[] args)
            {
                if (methods == null)
                {
                    methods = new Item[1];
                    methods[0] = new Item { mh = mh, args = args };
                    return 0;
                }

                int n = methods.Length;

                if (mh == methods[0].mh)
                {
                    // we expect that new entry will always point to the same method,
                    // the lambda metafactory, and we scan old entries to see if we
                    // have a duplicate.  if the new entry is for a different method,
                    // we skip the scan and always add a new entry.

                    for (int i = 0; i < n; i++)
                    {
                        if (CompareBootstrapArgs(methods[i].args, args))
                            return i;
                    }
                }

                var newMethods = new Item[n + 1];
                Array.Copy(methods, 0, newMethods, 0, n);
                newMethods[n] = new Item { mh = mh, args = args };
                methods = newMethods;

                return n;

                static bool CompareBootstrapArgs(object[] one, object[] two)
                {
                    int n = one.Length;
                    if (n != two.Length)
                        return false;
                    for (int i = 0; i < n; i++)
                    {
                        var obj1 = one[i];
                        var obj2 = two[i];
                        if (obj1 is System.ValueType)
                        {
                            if (! obj1.Equals(obj2))
                                return false;
                        }
                        else if (obj1.ToString() != obj2.ToString())
                            return false;
                    }
                    return true;
                }
            }
        }

    }
}
