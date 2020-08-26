
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public class JavaMethodRef : JavaMethodType
    {

        public string Name;



        public JavaMethodRef()
        {
        }



        public JavaMethodRef(string name, JavaType returnType)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = new List<JavaFieldRef>();
        }



        public JavaMethodRef(string name, JavaType returnType, List<JavaFieldRef> parameters)
            : base(returnType, parameters)
        {
            Name = name;
        }



        public JavaMethodRef(string name, JavaType returnType, JavaType singleParameter)
            : this(name, returnType, singleParameter, null, null)
        {
        }



        public JavaMethodRef(string name, JavaType returnType,
                             JavaType firstParameter, JavaType secondParameter)
            : this(name, returnType, firstParameter, secondParameter, null)
        {
        }



        public JavaMethodRef(string name, JavaType returnType,
                             JavaType firstParameter, JavaType secondParameter, JavaType thirdParameter)
        {
            Name = name;
            ReturnType = returnType;
            if (firstParameter == null)
                Parameters = new List<JavaFieldRef>(0);
            else
            {
                int n = (thirdParameter != null) ? 3 : ((secondParameter != null) ? 2 : 1);
                Parameters = new List<JavaFieldRef>(n);
                Parameters.Add(new JavaFieldRef("arg0", firstParameter));
                if (secondParameter != null)
                {
                    Parameters.Add(new JavaFieldRef("arg1", secondParameter));
                    if (thirdParameter != null)
                        Parameters.Add(new JavaFieldRef("arg2", thirdParameter));
                }
            }
        }



        public JavaMethodRef(string name, string descriptor, JavaException.Where Where)
            : base(descriptor, Where)
        {
            Name = name;
        }



        public JavaMethodRef(string descriptor, JavaException.Where Where)
            : base(descriptor, Where)
        {
        }




        public override string ToString() => $"{ReturnType} {Name}{ParametersToString()}";



        /*public static readonly JavaMethodRef InstanceConstructor =
                                    new JavaMethodRef("<init>", JavaType.VoidType);*/
    }

}
