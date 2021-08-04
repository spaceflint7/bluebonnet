
namespace system.reflection
{

    public static class FSharpCompat
    {

        // we don't yet translate .Net attributes to Java annotations,
        // so we have to fake some attributes to support F# ToString()

        public static object[] GetCustomAttributes_Type(java.lang.Class scanClass,
                                                        java.lang.Class attrClass)
        {
            // we manufacture a CompilationMapping attribute for a type
            // type implements at least three specific interfaces.
            // if it has a nested Tags class, it is a (discriminated union)
            // SumType; otherwise it is a plain RecordType.

            if (IsFSharpAttr(attrClass) && IsFSharpType(scanClass))
            {
                int sourceConstructFlags = IsFSharpSumType(scanClass)
                       ? /* SumType */ 1 : /* RecordType */ 2;

                return CreateAttr(attrClass, sourceConstructFlags);
            }

            return null;
        }



        public static object[] GetCustomAttributes_Property(java.lang.Class declClass,
                                                            java.lang.Class attrClass)
        {
            if (IsFSharpAttr(attrClass))
            {
                // we manufacture a CompilationMapping attribute for a property:
                // - if declared directly in a class that is an "F# type"
                //      and is not also a SumType
                // - if declared in an inner class, and the outer class
                //      is an "F# type" that is a SumType

                bool isFSharp = IsFSharpType(declClass);
                if (isFSharp)
                {
                    if (IsFSharpSumType(declClass))
                        isFSharp = false;
                }
                else
                {
                    var outerClass = declClass.getDeclaringClass();
                    if (outerClass != null)
                        isFSharp = IsFSharpType(outerClass);
                }

                if (isFSharp)
                {
                    return CreateAttr(attrClass, /* Field */ 4);
                }

                return RuntimeType.EmptyObjectArray;
            }
            return null;
        }


        private static bool IsFSharpType(java.lang.Class scanClass)
        {
            int requiredInterfaceCount = 0;
            foreach (var ifc in scanClass.getInterfaces())
            {
                if (    ifc == (java.lang.Class)
                                    typeof(System.Collections.IStructuralEquatable)
                     || ifc == (java.lang.Class)
                                    typeof(System.Collections.IStructuralComparable)
                     || ifc == (java.lang.Class) typeof(System.IComparable))
                    requiredInterfaceCount++;
            }
            return (requiredInterfaceCount >= 3);
        }


        private static bool IsFSharpSumType(java.lang.Class scanClass)
            => system.RuntimeType.FindInnerClass(scanClass, "Tags") != null;


        private static bool IsFSharpAttr(java.lang.Class attrClass)
            => attrClass == (java.lang.Class)
                    typeof(Microsoft.FSharp.Core.CompilationMappingAttribute);


        private static object[] CreateAttr(java.lang.Class attrClass,
                                           int sourceConstructFlags)
        {
            foreach (var _constr in attrClass.getConstructors())
            {
                #pragma warning disable 0436
                var constr = (java.lang.reflect.Constructor) (object) _constr;
                #pragma warning restore 0436
                var parameters = constr.getParameterTypes();
                if (    parameters.Length == 2
                     && parameters[0] == java.lang.Integer.TYPE
                     && parameters[1].isMemberClass())
                {
                    var created = constr.newInstance(new object[] {
                        java.lang.Integer.valueOf(sourceConstructFlags),
                        null
                    });
                    return new object[] { created };
                }
            }
            return null;
        }

    }

}


namespace Microsoft.FSharp.Core
{
    [java.attr.Discard] // discard in output
    public class CompilationMappingAttribute { }
}
