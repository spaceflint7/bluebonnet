
namespace system
{

    [System.Serializable]
    public abstract class ValueType : ValueMethod, java.lang.Cloneable
    {

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var type = GetType();
            if (! object.ReferenceEquals(type, obj.GetType()))
                return false;

            var fields = ((java.lang.Object) (object) this).getClass().getDeclaredFields();
            int numFields = fields.Length;

            if (numFields != 0)
            {
                for (int i = 0; i < numFields; i++)
                {
                    var fld = fields[i];
                    var mod = fld.getModifiers();
                    if ((mod & java.lang.reflect.Modifier.STATIC) == 0)
                    {
                        if ((mod & java.lang.reflect.Modifier.PUBLIC) == 0)
                            fld.setAccessible(true);

                        var v1 = fld.get(this);
                        var v2 = fld.get(obj);

                        if (v1 == null)
                        {
                            if (v2 != null)
                                return false;
                        }
                        else if (! v1.Equals(v2))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }



        public override int GetHashCode()
        {

            var fields = ((java.lang.Object) (object) this).getClass().getDeclaredFields();
            int numFields = fields.Length;

            if (numFields != 0)
            {
                for (int i = 0; i < numFields; i++)
                {
                    var fld = fields[i];
                    var mod = fld.getModifiers();
                    if ((mod & java.lang.reflect.Modifier.STATIC) == 0)
                    {
                        if ((mod & java.lang.reflect.Modifier.PUBLIC) == 0)
                            fld.setAccessible(true);

                        return fld.get(this).GetHashCode();
                    }
                }
            }

            return GetType().GetHashCode();
        }



        void ValueMethod.Clear() {}
        void ValueMethod.CopyFrom(ValueType from) {}
        void ValueMethod.CopyInto(ValueType into) {}
        ValueType ValueMethod.Clone() => null;
    }



    [java.attr.Discard] // discard in output
    interface ValueMethod
    {
        void Clear();
        void CopyFrom(ValueType from);
        void CopyInto(ValueType into);
        ValueType Clone();
    }

}
