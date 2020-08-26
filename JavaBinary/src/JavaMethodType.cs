
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public class JavaMethodType
    {
        public JavaType ReturnType;
        public List<JavaFieldRef> Parameters;



        public JavaMethodType()
        {
        }



        public JavaMethodType(JavaType returnType, List<JavaFieldRef> parameters)
        {
            ReturnType = returnType;
            Parameters = parameters;
        }



        public JavaMethodType(string descriptor, JavaException.Where Where)
        {
            if (descriptor[0] == '(')
            {
                var list = new List<JavaFieldRef>();

                var index = 1;
                while (index < descriptor.Length)
                {
                    if (descriptor[index] == ')')
                        break;
                    var nextType = new JavaType();
                    if ((index = nextType.ParseType(descriptor, index)) == -1)
                        break;
                    else
                        list.Add(new JavaFieldRef(null, nextType));
                }

                if (index > 0 && index + 1 < descriptor.Length)
                {
                    Parameters = list;
                    ReturnType = new JavaType();

                    if (descriptor[++index] == 'V')
                        return;

                    if (ReturnType.ParseType(descriptor, index) == descriptor.Length)
                        return;
                }
            }

            throw Where.Exception($"bad method descriptor '{descriptor}'");
        }



        public string ToDescriptor()
        {
            string str = "(";
            foreach (var param in Parameters)
                str += param.Type.ToDescriptor();
            str += ")";

            if (ReturnType.PrimitiveType == TypeCode.Empty && ReturnType.ClassName == null)
                str += "V";
            else
                str += ReturnType.ToDescriptor();

            return str;
        }



        public string ParametersToString()
        {
            string s = "(";
            bool comma = false;
            foreach (var p in Parameters)
            {
                if (comma)
                    s += ",";
                else
                    comma = true;
                s += p.Type;
            }
            return s + ")";
        }



        public override string ToString() => $"{ReturnType} {ParametersToString()}";

    }

}
