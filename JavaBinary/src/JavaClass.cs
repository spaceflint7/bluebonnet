
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaClass
    {

        public JavaAccessFlags Flags;
        public short PackageNameLength;
        public string Name;
        public string Super;
        public List<string> Interfaces;
        public List<JavaField> Fields;
        public List<JavaMethod> Methods;
        public string SourceFile;
        public string Signature;

        public InnerClass[] OuterAndInnerClasses;



        public class InnerClass
        {
            public string InnerShortName;
            public string InnerLongName;
            public string OuterLongName;
            public JavaAccessFlags Flags;
        }



        public JavaClass()
        {
        }



        public string FilePath()
        {
            return Name.Replace('.', '/') + ".class";
        }



        public bool IsInnerClass()
        {
            return (OuterAndInnerClasses != null && OuterAndInnerClasses[0] != null);
        }



        public void AddInterface(string newInterface)
        {
            if (Interfaces == null)
                Interfaces = new List<string>();
            else if (Interfaces.Contains(newInterface))
                return;
            Interfaces.Add(newInterface);
        }



        public void AddInnerClass(JavaClass innerClassToAdd)
        {
            if (! innerClassToAdd.Name.StartsWith(Name))
                throw new ArgumentException();

            int n = Name.Length;
            if (innerClassToAdd.Name[n] == '$')
                n++;

            var entryFlags = (innerClassToAdd.Flags & ~JavaAccessFlags.ACC_SUPER)
                             | JavaAccessFlags.ACC_STATIC;

            //
            // append a new entry in this class, linking to innerClassToAdd
            //

            var innerEntry = new JavaClass.InnerClass();

            innerEntry.InnerShortName = innerClassToAdd.Name.Substring(n);
            innerEntry.InnerLongName = innerClassToAdd.Name;
            innerEntry.OuterLongName = Name;
            innerEntry.Flags = entryFlags;

            if (OuterAndInnerClasses == null)
            {
                n = 1;
                OuterAndInnerClasses = new JavaClass.InnerClass[2];
            }
            else
            {
                n = OuterAndInnerClasses.Length;
                var newArray = new JavaClass.InnerClass[n + 1];
                Array.Copy(OuterAndInnerClasses, 0, newArray, 0, n);
                OuterAndInnerClasses = newArray;
            }

            OuterAndInnerClasses[n] = innerEntry;

            //
            // update the first entry in innerClassToAdd, connecting it to this class
            //

            var outerEntry = new JavaClass.InnerClass();

            outerEntry.InnerShortName = innerEntry.InnerShortName;
            outerEntry.InnerLongName = innerEntry.InnerLongName;
            outerEntry.OuterLongName = innerEntry.OuterLongName;
            outerEntry.Flags = entryFlags;

            if (innerClassToAdd.OuterAndInnerClasses == null)
            {
                innerClassToAdd.OuterAndInnerClasses = new JavaClass.InnerClass[1];
            }
            else if (innerClassToAdd.OuterAndInnerClasses[0] != null)
            {
                // the inner class is already connected to a parent
                throw new ArgumentException();
            }

            innerClassToAdd.OuterAndInnerClasses[0] = outerEntry;
        }

    }

}
