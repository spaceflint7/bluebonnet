
using System;

namespace SpaceFlint.JavaBinary
{

    public partial class JavaMethod
    {

        public JavaMethod(JavaReader rdr, bool withCode = true)
        {
            Class = rdr.Class;

            Flags = (JavaAccessFlags) rdr.Read16();

            Name = rdr.ConstUtf8(rdr.Read16());

            rdr.Where.Push($"method '{Name}'");

            var tmpMethod = new JavaMethodRef(rdr.ConstUtf8(rdr.Read16()), rdr.Where);

            ReturnType = tmpMethod.ReturnType;
            Parameters = tmpMethod.Parameters;

            var attributes = new JavaAttributeSet(rdr, withCode);

            Exceptions = attributes.GetAttr<JavaAttribute.Exceptions>()?.classes;

            var codeAttr = attributes.GetAttr<JavaAttribute.Code>();
            if (withCode && codeAttr != null)
            {
                #if ! DEBUG
                try
                {
                #endif
                    Code = new JavaCode(rdr, this, codeAttr);
                #if ! DEBUG
                }
                catch (IndexOutOfRangeException)
                {
                    throw rdr.Where.Exception("unexpected end of code");
                }
                #endif
            }

            InitParameterNames(attributes, codeAttr);

            rdr.Where.Pop();
        }



        void InitParameterNames(JavaAttributeSet attributes, JavaAttribute.Code codeAttr)
        {
            int numArgs = Parameters.Count;

            var parmsAttr = attributes.GetAttr<JavaAttribute.MethodParameters>();
            if (parmsAttr != null)
            {
                //
                // MethodParameters attributes lists parameter names one by one
                //

                int numParms = parmsAttr.parms.Length;
                if (numParms > numArgs)
                    numParms = numArgs;

                for (int i = 0; i < numParms; i++)
                    Parameters[i].Name = parmsAttr.parms[i].name;
            }

            else if (codeAttr != null)
            {
                var localsAttrs =
                    codeAttr.attributes.GetAttrs<JavaAttribute.LocalVariableTable>();

                if (localsAttrs != null)
                {
                    //
                    // LocalVariableTables specify local variables by index,
                    // so we also need to keep track of the index of each parameter
                    //

                    int index = 0;
                    if ((Flags & JavaAccessFlags.ACC_STATIC) == 0)
                        index++;

                    for (int i = 0; i < numArgs; i++)
                    {
                        foreach (var localsAttr in localsAttrs)
                        {
                            for (int j = 0; j < localsAttr.vars.Length; j++)
                            {
                                if (localsAttr.vars[j].index == index)
                                    Parameters[i].Name = localsAttr.vars[j].nameAndType.Name;
                            }
                        }

                        index += Parameters[i].Type.Category;
                    }
                }
            }

            int i0 = ((Flags & JavaAccessFlags.ACC_STATIC) != 0) ? 0 : 1;
            for (int i = 0; i < numArgs; i++)
            {
                if (Parameters[i].Name == null)
                    Parameters[i].Name = "$arg" + (i + i0);
            }
        }

    }
}
