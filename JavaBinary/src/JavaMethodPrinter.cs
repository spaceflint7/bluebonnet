
using System;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaMethod
    {

        public void Print(IndentedText txt)
        {
            txt.Write("/* {0} */ {1}{2}{3}",
                /* 0 */ ((ushort) Flags).ToString("X4"),
                /* 1 */ ((Flags & JavaAccessFlags.ACC_PUBLIC) != 0 ? "public " :
                            ((Flags & JavaAccessFlags.ACC_PRIVATE) != 0 ? "private " :
                                ((Flags & JavaAccessFlags.ACC_PROTECTED) != 0 ? "protected " :
                                    string.Empty))),
                /* 2 */ ((Flags & JavaAccessFlags.ACC_STATIC) != 0 ? "static " :
                            ((Flags & JavaAccessFlags.ACC_ABSTRACT) != 0 ? "abstract " :
                                    string.Empty)),
                /* 3 */ ((Flags & JavaAccessFlags.ACC_FINAL) != 0 ? "final " : string.Empty));

            if (Name == "<init>" || Name == "<clinit>")
                txt.Write("{0}", Class.Name);
            else
                txt.Write("{0} {1}", ReturnType, Name);

            txt.Write("(");

            for (int i = 0; i < Parameters.Count; i++)
            {
                txt.Write("{0}{1} {2}",
                    /* 0 */ (i > 0 ? ", " : string.Empty),
                    /* 1 */ Parameters[i].Type,
                    /* 2 */ Parameters[i].Name);
            }

            txt.Write(")");

            if (Exceptions != null)
            {
                for (int i = 0; i < Exceptions.Length; i++)
                {
                    txt.Write("{0}{1}",
                        /* 0 */ (i > 0 ? ", " : " throws "),
                        /* 1 */ Exceptions[i].ClassName);
                }
            }

            if (Code != null)
            {
                txt.Write(" {");
                txt.NewLine();
                txt.AdjustIndent(true);

                Code.Print(txt);

                txt.AdjustIndent(false);
                txt.Write("}");
            }
            else
                txt.Write(";");

            txt.NewLine();
            txt.NewLine();
        }

    }

}
