
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaClass
    {


        public void PrintJava(IndentedText txt)
        {
            if (SourceFile != null)
            {
                txt.Write("//");
                txt.NewLine();
                txt.Write("// {0}", SourceFile);
                txt.NewLine();
                txt.Write("//");
                txt.NewLine();
                txt.NewLine();
            }

            PrintClassPrefix(txt, Flags, Name);

            if (Super != null)
                txt.Write(" extends {0}", Super);

            if (Interfaces != null)
            {
                txt.Write(" implements ");
                for (int i = 0; i < Interfaces.Count; i++)
                    txt.Write("{0}{1}", (i > 0 ? ", " : string.Empty), Interfaces[i]);
            }

            txt.Write(" {");
            txt.NewLine();
            txt.NewLine();
            txt.AdjustIndent(true);

            if (Signature != null)
            {
                txt.Write("// signature ");
                txt.Write(Signature);
                txt.NewLine();
                txt.NewLine();
            }

            if (OuterAndInnerClasses != null)
            {
                if (OuterAndInnerClasses[0] != null)
                {
                    if (OuterAndInnerClasses[0].InnerShortName != null)
                    {
                        txt.Write("// declared as {0} {{...}} in {1}",
                                    OuterAndInnerClasses[0].InnerShortName,
                                    OuterAndInnerClasses[0].OuterLongName);
                    }
                    else
                    {
                        txt.Write("// anonymous class {{...}} in {0}",
                                    OuterAndInnerClasses[0].OuterLongName);
                    }
                    txt.NewLine();
                    txt.NewLine();
                }

                for (int i = 1; i < OuterAndInnerClasses.Length; i++)
                {
                    PrintClassPrefix(txt,
                                     OuterAndInnerClasses[i].Flags,
                                     OuterAndInnerClasses[i].InnerShortName);
                    txt.Write("{0} {{...}} is {1};",
                            (OuterAndInnerClasses[i].InnerShortName == null
                                    ? "(anonymous)" : string.Empty),
                            OuterAndInnerClasses[i].InnerLongName);
                    txt.NewLine();
                    txt.NewLine();
                }
            }

            if (Fields != null)
            {
                for (int i = 0; i < Fields.Count; i++)
                {
                    Fields[i].Print(txt);
                    txt.NewLine();
                }
            }

            if (Methods != null)
            {
                for (int i = 0; i < Methods.Count; i++)
                    Methods[i].Print(txt);
            }

            txt.AdjustIndent(false);
            txt.Write("}");
            txt.NewLine();
            txt.NewLine();
        }



        static void PrintClassPrefix(IndentedText txt, JavaAccessFlags Flags, string Name)
        {
            if (Name == "interface")
                Name = '\"' + Name + '\"';
            txt.Write("/* {0:X} */ {1}{2}{3}{4}{5} {6}",
                /* 0 */ ((ushort) Flags).ToString("X4"),
                /* 1 */ ((Flags & JavaAccessFlags.ACC_PUBLIC)    != 0 ? "public " : string.Empty),
                /* 2 */ ((Flags & JavaAccessFlags.ACC_ABSTRACT)  != 0 ? "abstract " : string.Empty),
                /* 3 */ ((Flags & JavaAccessFlags.ACC_STATIC)    != 0 ? "static " : string.Empty),
                /* 4 */ ((Flags & JavaAccessFlags.ACC_FINAL)     != 0 ? "final " : string.Empty),
                /* 5 */ ((Flags & JavaAccessFlags.ACC_INTERFACE) != 0 ? "interface" :
                        ((Flags & JavaAccessFlags.ACC_ENUM)      != 0 ? "enum" : "class")),
                /* 6 */ Name);
        }

    }

}
