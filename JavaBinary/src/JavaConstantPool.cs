
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{


    public class JavaConstantPool
    {

        List<JavaConstant> pool;
        bool editable;



        public JavaConstantPool()
        {
            pool = new List<JavaConstant>();
            pool.Add(null);
            editable = true;
        }



        public JavaConstantPool(JavaReader rdr)
        {
            var count = rdr.Read16();
            pool = new List<JavaConstant>(count + 1);
            pool.Add(null);
            for (int i = 1; i < count; i++)
            {
                var c = ReadConstant(rdr);
                pool.Add(c);
                if (c is JavaConstant.Long || c is JavaConstant.Double)
                {
                    i++;    // section 4.4.5
                    pool.Add(null);
                }
            }
        }



        JavaConstant ReadConstant(JavaReader rdr)
        {
            var tag = rdr.Read8();
            switch (tag)
            {
                case JavaConstant.Utf8.tag:
                    return new JavaConstant.Utf8(rdr);

                case JavaConstant.Integer.tag:
                    return new JavaConstant.Integer(rdr);

                case JavaConstant.Float.tag:
                    return new JavaConstant.Float(rdr);

                case JavaConstant.Long.tag:
                    return new JavaConstant.Long(rdr);

                case JavaConstant.Double.tag:
                    return new JavaConstant.Double(rdr);

                case JavaConstant.Class.tag:
                    return new JavaConstant.Class(rdr);

                case JavaConstant.String.tag:
                    return new JavaConstant.String(rdr);

                case JavaConstant.FieldRef.tag:
                    return new JavaConstant.FieldRef(rdr);

                case JavaConstant.MethodRef.tag:
                    return new JavaConstant.MethodRef(rdr);

                case JavaConstant.InterfaceMethodRef.tag:
                    return new JavaConstant.InterfaceMethodRef(rdr);

                case JavaConstant.NameAndType.tag:
                    return new JavaConstant.NameAndType(rdr);

                case JavaConstant.MethodHandle.tag:
                    return new JavaConstant.MethodHandle(rdr);

                case JavaConstant.MethodType.tag:
                    return new JavaConstant.MethodType(rdr);

                case JavaConstant.InvokeDynamic.tag:
                    return new JavaConstant.InvokeDynamic(rdr);

                default:
                    throw rdr.Where.Exception($"invalid tag {tag} in constant pool");
            }
        }



        public JavaConstant Get(int index)
        {
            return ((index > 0 && index < pool.Count) ? pool[index] : null);
        }



        public T Get<T>(int index, JavaException.Where Where)
        {
            if (   index > 0 && index < pool.Count
                && pool[index] is T o && o != null)
            {
                return o;
            }
            throw Where.Exception($"expected constant of type '{typeof(T).Name}' at index {index}");
        }



        public int Put(JavaConstant newConst, JavaException.Where Where)
        {
            var newConstType = newConst.GetType();
            int n = pool.Count;
            for (int i = 1; i < n; i++)
            {
                var oldConst = pool[i];
                if (oldConst?.GetType() == newConstType && oldConst.Equals(newConst))
                {
                    return (ushort) i;
                }
            }

            if (! editable)
                throw Where.Exception("new constants not allowed");
            pool.Add(newConst);

            if (newConst is JavaConstant.Long || newConst is JavaConstant.Double)
                pool.Add(null);     // section 4.4.5

            return n;
        }



        public void Write(JavaWriter wtr)
        {
            int n = pool.Count;
            wtr.Write16((ushort) n);
            for (int i = 1; i < n; i++)
                pool[i]?.Write(wtr);
            editable = false;
        }

    }
}
