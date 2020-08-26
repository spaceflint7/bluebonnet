
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SpaceFlint.JavaBinary
{

    public class JavaAttributeSet
    {

        List<JavaAttribute> set;



        public JavaAttributeSet(JavaReader rdr, bool withCode = true)
        {
            var count = rdr.Read16();
            set = new List<JavaAttribute>(count);

            for (int i = 0; i < count; i++)
            {
                var name = rdr.ConstUtf8(rdr.Read16());
                rdr.Where.Push($"attribute '{name}'");
                bool popped = false;

                var length = JavaAttribute.ReadLength(rdr, name);
                var position = rdr.StreamPosition;

                JavaAttribute attr;
                switch (name)
                {
                    case JavaAttribute.SourceFile.tag:
                        attr = new JavaAttribute.SourceFile(rdr);
                        break;

                    case JavaAttribute.Signature.tag:
                        attr = new JavaAttribute.Signature(rdr);
                        break;

                    case JavaAttribute.Exceptions.tag:
                        attr = new JavaAttribute.Exceptions(rdr);
                        break;

                    case JavaAttribute.InnerClasses.tag:
                        attr = new JavaAttribute.InnerClasses(rdr);
                        break;

                    case JavaAttribute.EnclosingMethod.tag:
                        attr = new JavaAttribute.EnclosingMethod(rdr);
                        break;

                    case JavaAttribute.ConstantValue.tag:
                        attr = new JavaAttribute.ConstantValue(rdr);
                        break;

                    case JavaAttribute.MethodParameters.tag:
                        attr = new JavaAttribute.MethodParameters(rdr);
                        break;

                    //
                    // Code attributes
                    //

                    case JavaAttribute.Code.tag:
                        attr = withCode ? (JavaAttribute) new JavaAttribute.Code(rdr)
                             : (JavaAttribute) new JavaAttribute.Generic(rdr, name, length);
                        break;

                    case JavaAttribute.BootstrapMethods.tag:
                        attr = withCode ? (JavaAttribute) new JavaAttribute.BootstrapMethods(rdr)
                             : (JavaAttribute) new JavaAttribute.Generic(rdr, name, length);
                        break;

                    case JavaAttribute.StackMapTable.tag:
                        attr = new JavaAttribute.StackMapTable(rdr);
                        break;

                    case JavaAttribute.LineNumberTable.tag:
                        attr = new JavaAttribute.LineNumberTable(rdr);
                        break;

                    case JavaAttribute.LocalVariableTable.tag:
                        attr = new JavaAttribute.LocalVariableTable(rdr);
                        break;

                    //
                    // Unhandled attributes
                    //

                    case "Synthetic":
                    case "AnnotationDefault":
                    case "LocalVariableTypeTable":
                    case "RuntimeVisibleAnnotations":
                    case "RuntimeInvisibleAnnotations":
                    case "RuntimeInvisibleParameterAnnotations":
                    case "Deprecated":
                        attr = new JavaAttribute.Generic(rdr, name, length);
                        break;

                    default:
                        rdr.Where.Pop();
                        popped = true;
                        Console.WriteLine($"skipping unknown attribute '{name}'{rdr.Where}");
                        attr = new JavaAttribute.Generic(rdr, name, length);
                        break;
                }

                set.Add(attr);

                if (rdr.StreamPosition != position + length)
                    throw rdr.Where.Exception("attribute too short");

                if (! popped)
                    rdr.Where.Pop();
            }
        }



        public JavaAttributeSet()
        {
            set = new List<JavaAttribute>();
        }



        public T GetAttr<T>() where T : JavaAttribute
        {
            for (int i = 0; i < set.Count; i++)
            {
                if (set[i] is T attr)
                    return attr;
            }
            return null;
        }



        public ReadOnlyCollection<T> GetAttrs<T>() where T : JavaAttribute
        {
            var list = new List<T>();
            for (int i = 0; i < set.Count; i++)
            {
                if (set[i] is T attr)
                    list.Add(attr);
            }
            return list.AsReadOnly();
        }



        public void Put(JavaAttribute attr)
        {
            set.Add(attr);
        }



        public void Write(JavaWriter wtr)
        {
            wtr.Write16(set.Count);
            foreach (var attr in set)
            {
                wtr.Where.Push($"attribute '{attr.GetType().Name}'");
                attr.Write(wtr);
                wtr.Where.Pop();
            }
        }

    }

}
