
namespace system
{

    public static class Object
    {

        public static bool Equals(java.lang.Object objA, java.lang.Object objB)
        {
            if (objA == objB)
                return true;
            else if (objA == null || objB == null)
                return false;
            else
                return objA.Equals(objB);
        }

        public static bool ReferenceEquals(java.lang.Object objA, java.lang.Object objB)
        {
            return objA == objB;
        }

        public static System.Type GetType(java.lang.Object obj)
        {
            if (obj is IGenericObject genericObject)
                return genericObject.GetType();
            return system.RuntimeType.GetType(obj.getClass());
        }

    }



    //
    // Void type
    //



    public struct Void { }

}



namespace java.lang
{
    [java.attr.Discard] // discard in output
    public abstract class Object
    {
        public abstract java.lang.Class getClass();
    }
}
