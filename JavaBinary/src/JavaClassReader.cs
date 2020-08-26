
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaClass
    {

        public JavaClass(JavaReader rdr, int majorVersion, int minorVersion, bool withCode = true)
        {
            rdr.Class = this;

            Flags = (JavaAccessFlags) rdr.Read16();

            Name = rdr.ConstClass(rdr.Read16()).ClassName;

            rdr.Where.Push($"reading class '{Name}' version {majorVersion}.{minorVersion}");

            ushort superConstIndex = rdr.Read16();
            if (! (superConstIndex == 0 && Name == "java.lang.Object"))
                Super = rdr.ConstClass(superConstIndex).ClassName;

            var interfaceCount = rdr.Read16();
            if (interfaceCount != 0)
            {
                Interfaces = new List<string>(interfaceCount);
                for (int i = 0; i < interfaceCount; i++)
                    Interfaces.Add(rdr.ConstClass(rdr.Read16()).ClassName);
            }

            var fieldCount = rdr.Read16();
            if (fieldCount != 0)
            {
                Fields = new List<JavaField>(fieldCount);
                for (int i = 0; i < fieldCount; i++)
                    Fields.Add(new JavaField(rdr));
            }

            var methodCount = rdr.Read16();
            if (methodCount != 0)
            {
                Methods = new List<JavaMethod>(methodCount);
                for (int i = 0; i < methodCount; i++)
                    Methods.Add(new JavaMethod(rdr, withCode));
            }

            var attributes = new JavaAttributeSet(rdr, withCode);

            var sourceAttr = attributes.GetAttr<JavaAttribute.SourceFile>();
            SourceFile = sourceAttr?.fileName;

            var signatureAttr = attributes.GetAttr<JavaAttribute.Signature>();
            Signature = signatureAttr?.descriptor;

            var innerAttr = attributes.GetAttr<JavaAttribute.InnerClasses>();
            if (innerAttr != null)
            {
                var enclosingClass =
                        attributes.GetAttr<JavaAttribute.EnclosingMethod>()
                                 ?.className;
                ParseInnerClasses(innerAttr.classes, enclosingClass);
            }

            ReadCallSites(rdr, attributes);

            if ((Flags & JavaAccessFlags.ACC_ENUM) != 0)
            {
                bool superOk = (Super == "java.lang.Enum");
                if (IsInnerClass() && Super == OuterAndInnerClasses[0].OuterLongName)
                    superOk = true;
                if (! superOk)
                    throw rdr.Where.Exception($"bad super class '{Super}' for enum");
            }

            rdr.Where.Pop();
        }



        void ParseInnerClasses(InnerClass[] classes, string enclosingClass)
        {
            int parentIndex = -1;
            int childCount = 0;
            for (int i = 0; i < classes.Length; i++)
            {
                if (classes[i].InnerLongName == Name)
                {
                    parentIndex = i;
                    if (classes[i].OuterLongName == null)
                        classes[i].OuterLongName = enclosingClass;
                }
                else if (classes[i].OuterLongName == Name || classes[i].OuterLongName == null)
                    childCount++;
            }

            if (parentIndex != -1 || childCount != 0)
            {
                OuterAndInnerClasses = new InnerClass[1 + childCount];
                int j = 1;
                for (int i = 0; i < classes.Length; i++)
                {
                    if (i == parentIndex)
                        OuterAndInnerClasses[0] = classes[i];
                    else if (classes[i].OuterLongName == Name || classes[i].OuterLongName == null)
                        OuterAndInnerClasses[j++] = classes[i];
                }
            }
        }



        void ReadCallSites(JavaReader rdr, JavaAttributeSet attributes)
        {
            if (rdr.callSites != null)
            {
                var attr = attributes.GetAttr<JavaAttribute.BootstrapMethods>();
                if (attr == null)
                    throw rdr.Where.Exception($"missing bootstrap methods attribute");

                var methods = attr.methods;
                foreach (var callSite in rdr.callSites)
                {
                    int i = callSite.BootstrapMethodIndex;
                    if (i >= methods.Length)
                        throw rdr.Where.Exception($"invalid bootstrap method index");

                    callSite.BootstrapMethod = methods[i].mh;
                    callSite.BootstrapArgs = methods[i].args;
                }
            }
        }

    }
}
