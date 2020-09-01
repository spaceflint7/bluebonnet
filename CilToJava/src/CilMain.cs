
using System;
using System.Collections.Generic;
using Mono.Cecil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public static class CilMain
    {

        static JavaException.Where _Where;
        public static List<JavaClass> JavaClasses;
        public static GenericStack GenericStack;



        public static JavaException.Where Where => _Where;



        internal static bool HasCustomAttribute(this ICustomAttributeProvider obj, string attrName,
                                                bool fullAttrNameSpecified = false)
        {
            if (! fullAttrNameSpecified)
                attrName = "java.attr." + attrName + "Attribute";
            foreach (var ca in obj.CustomAttributes)
            {
                if (ca.Constructor.DeclaringType.FullName == attrName)
                    return true;
            }
            return false;
        }



        internal static void LoadFrameOrClearStack(JavaStackMap stackMap,
                                                   Mono.Cecil.Cil.Instruction inst)
        {
            // following any instruction that breaks the normal flow of execution,
            // we need to load an stack frame already recorded for some following
            // instruction, if any (e.g. as part of a conditional branch sequence).
            // if there isn't such a frame, just clear and reset the stack frame.

            for (;;)
            {
                inst = inst.Next;
                if (inst == null)
                    break;
                if (stackMap.LoadFrame((ushort) inst.Offset, true, null))
                    return;
                var flowControl = inst.OpCode.FlowControl;
                if (    flowControl == Mono.Cecil.Cil.FlowControl.Branch
                     || flowControl == Mono.Cecil.Cil.FlowControl.Break
                     || flowControl == Mono.Cecil.Cil.FlowControl.Cond_Branch
                     || flowControl == Mono.Cecil.Cil.FlowControl.Return
                     || flowControl == Mono.Cecil.Cil.FlowControl.Throw)
                {
                    break;
                }
            }

            stackMap.ClearStack();
        }



        internal static JavaClass CreateInnerClass(JavaClass outerClass, string innerName,
                                                   JavaAccessFlags innerFlags = 0)
        {
            if (innerFlags == 0)
            {
                innerFlags = JavaAccessFlags.ACC_PUBLIC
                           | JavaAccessFlags.ACC_FINAL
                           | JavaAccessFlags.ACC_SUPER;
            }
            innerFlags |= JavaAccessFlags.ACC_SYNTHETIC;

            var innerClass = new JavaClass();
            innerClass.Name = innerName;
            innerClass.Super = JavaType.ObjectType.ClassName;
            innerClass.PackageNameLength = outerClass.PackageNameLength;
            innerClass.Flags = innerFlags;
            innerClass.Fields = new List<JavaField>();
            innerClass.Methods = new List<JavaMethod>();

            outerClass.AddInnerClass(innerClass);
            return innerClass;
        }



        internal static JavaCode CreateHelperMethod(JavaClass theClass, JavaMethodRef methodType,
                                                    int maxLocals, int maxStack)
        {
            var newMethod = new JavaMethod(theClass, methodType);
            newMethod.Flags = JavaAccessFlags.ACC_PUBLIC | JavaAccessFlags.ACC_BRIDGE;

            var code = newMethod.Code = new JavaCode();
            code.Method = newMethod;
            code.Instructions = new List<JavaCode.Instruction>();

            code.MaxLocals = maxLocals;
            code.MaxStack = maxStack;

            theClass.Methods.Add(newMethod);
            return code;
        }



        internal static void MakeRoomForCategory2ValueOnStack(JavaCode code)
        {
            // ensure the stack has enough space for a category-2 value
            if (code.StackMap != null)
            {
                code.StackMap.PushStack(JavaType.IntegerType);
                code.StackMap.PushStack(JavaType.LongType);
                code.StackMap.PopStack(CilMain.Where);
                code.StackMap.PopStack(CilMain.Where);
            }
        }



        public static List<JavaClass> Import(List<TypeDefinition> cilTypes)
        {
            if (_Where != null)
                throw new InvalidOperationException();

            _Where = new JavaException.Where();
            JavaClasses = new List<JavaClass>();
            GenericStack = new GenericStack();

            foreach (var cilType in cilTypes)
            {
                if (cilType.HasCustomAttribute("Discard"))
                    continue; // if decorated with [java.attr.Discard], don't export to java

                if (cilType.FullName == "<PrivateImplementationDetails>")
                    continue; // discard the private class for array initializers

                Where.Push($"assembly {cilType.Module.FileName}");

                TypeBuilder.BuildJavaClass(cilType, null);

                Where.Pop();
            }

            _Where = null;
            return JavaClasses;
        }



        #if true

        // Android 'D8' tool rejects some identifier characters:
        // parentheses, angle brackets, verticals bars
        // see isSimpleNameChar() method, somewhere in D8/R8 source

        internal const char EXCLAMATION  = '\u00A1'; // U+00A1 Inverted Exclamation Mark
        internal const char OPEN_PARENS  = '\u00AB'; // U+00AB Left-Pointing Double Angle Quotation Mark
        internal const char CLOSE_PARENS = '\u00BB'; // U+00BB Right-Pointing Double Angle Quotation Mark

        internal static string MakeValidTypeName(string name)
        {
            return name.Replace('(', OPEN_PARENS)
                       .Replace(')', CLOSE_PARENS)

                       .Replace('<', OPEN_PARENS)
                       .Replace('>', CLOSE_PARENS)

                       .Replace('|',  '\u00A6')   // U+00A6 Broken Bar
                       .Replace('\'', '\uFF07')   // U+FF07 Fullwidth Apostrophe
                       .Replace(',',  '\uFF0C')   // U+FF0C Fullwidth Comma
                       .Replace('=',  '\uFF1D')   // U+FF1D Fullwidth Equals Sign
                       .Replace('@',  '\uFF20')   // U+FF20 Fullwidth Commercial At
                       .Replace('[',  '\uFF3B')   // U+FF3B Fullwidth Left Square Bracket
                       .Replace(']',  '\uFF3D')   // U+FF3D Fullwidth Right Square Bracket
                       .Replace('`',  '\uFF40')   // U+FF40 Fullwidth Grave Accent
                       ;
        }

        internal static string MakeValidMemberName(string name)
            => MakeValidTypeName(name).Replace('.', '$');

        #else

        // for easier debugging on the jvm, we can relax some of the
        // constraints imposed by Android, and have simpler identifiers

        internal const char EXCLAMATION  = '!';
        internal const char OPEN_PARENS  = '(';
        internal const char CLOSE_PARENS = ')';

        internal static string MakeValidTypeName(string name)
            => name.Replace('<', '(').Replace('>', ')').Replace('|', 'I');

        internal static string MakeValidMemberName(string name)
            => name.Replace('<', '(').Replace('>', ')').Replace('.', '$');

        #endif

    }

}
