
using System;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaMethod
    {

        public void Write(JavaWriter wtr)
        {
            wtr.Where.Push($"method '{Name}'");

            wtr.Write16((ushort) Flags);
            wtr.Write16(wtr.ConstUtf8(Name));
            wtr.Write16(wtr.ConstUtf8(ToDescriptor()));

            var attributes = new JavaAttributeSet();

            if (Code != null)
            {
                var codeAttr = new JavaAttribute.Code();
                Code.Write(wtr, codeAttr);
                attributes.Put(codeAttr);
            }

            attributes.Write(wtr);

            wtr.Where.Pop();
        }

    }
}
