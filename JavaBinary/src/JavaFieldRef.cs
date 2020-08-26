
using System;

namespace SpaceFlint.JavaBinary
{

    public class JavaFieldRef
    {

        public string Name;
        public JavaType Type;



        public JavaFieldRef()
        {
        }



        public JavaFieldRef(string name, JavaType type)
        {
            Name = name;
            Type = type;
        }



        public JavaFieldRef(string name, string descriptor, JavaException.Where Where)
        {
            Name = name;
            Type = new JavaType(descriptor, Where);
        }

    }

}
