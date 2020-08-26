
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaClass
    {

        public void Write(JavaWriter wtr)
        {
            wtr.Where.Push($"writing class '{Name}'");

            if (Name == null)
                throw wtr.Where.Exception("missing class name");

            wtr.Write16((ushort) Flags);
            wtr.Write16(wtr.ConstClass(Name));
            wtr.Write16(wtr.ConstClass(Super));

            if (Interfaces == null)
                wtr.Write16(0);
            else
            {
                wtr.Write16(Interfaces.Count);
                for (int i = 0; i < Interfaces.Count; i++)
                    wtr.Write16(wtr.ConstClass(Interfaces[i]));
            }

            if (Fields == null)
                wtr.Write16(0);
            else
            {
                wtr.Write16(Fields.Count);
                for (int i = 0; i < Fields.Count; i++)
                    Fields[i].Write(wtr);
            }

            if (Methods == null)
                wtr.Write16(0);
            else
            {
                wtr.Write16(Methods.Count);
                for (int i = 0; i < Methods.Count; i++)
                    Methods[i].Write(wtr);
            }

            var attributes = new JavaAttributeSet();

            if (SourceFile != null)
                attributes.Put(new JavaAttribute.SourceFile(SourceFile));

            if (Signature != null)
                attributes.Put(new JavaAttribute.Signature(Signature));

            WriteInnerClasses(attributes);

            if (wtr.bootstrapMethods != null)
                attributes.Put(wtr.bootstrapMethods);

            attributes.Write(wtr);

            wtr.Where.Pop();
        }



        void WriteInnerClasses(JavaAttributeSet attributes)
        {
            if (OuterAndInnerClasses != null)
            {
                int n = 0;
                for (int i = 0; i < OuterAndInnerClasses.Length; i++)
                {
                    if (OuterAndInnerClasses[i] != null)
                        n++;
                }
                if (n != 0)
                {
                    var innerClasses = new JavaClass.InnerClass[n];
                    n = 0;
                    for (int i = 0; i < OuterAndInnerClasses.Length; i++)
                    {
                        if (OuterAndInnerClasses[i] != null)
                            innerClasses[n++] = OuterAndInnerClasses[i];
                    }
                    attributes.Put(new JavaAttribute.InnerClasses(innerClasses));
                }
            }
        }

     }
}
