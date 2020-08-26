
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaMethod : JavaMethodRef
    {

        public JavaClass Class;
        public JavaAccessFlags Flags;
        public JavaType[] Exceptions;
        public JavaCode Code;



        public JavaMethod()
        {
        }



        public JavaMethod(string name, JavaType returnType)
            : base(name, returnType)
        {
        }



        public JavaMethod(string name, JavaType returnType, JavaType singleParameter)
            : base(name, returnType, singleParameter)
        {
        }



        public JavaMethod(string name, JavaType returnType,
                          JavaType firstParameter, JavaType secondParameter)
            : base(name, returnType, firstParameter, secondParameter)
        {
        }



        public JavaMethod(JavaClass jclass, JavaMethodRef methodRef)
        {
            Name = methodRef.Name;
            ReturnType = methodRef.ReturnType;
            Parameters = new List<JavaFieldRef>(methodRef.Parameters);
            Class = jclass;
        }

    }
}
