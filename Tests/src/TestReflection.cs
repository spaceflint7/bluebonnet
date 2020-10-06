
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Tests
{
    [TestClass]
    public class TestReflection : BaseTest
    {

        public override void TestMain()
        {
            TestAssembly();
            //TestAttributes();
            TestReflection1();
            TestGenericMethod();
            TestFindType();
            TestPrimitives();
        }



        //
        // TestAssembly
        //

        void TestAssembly()
        {
            var asmName = new System.Reflection.AssemblyName("NAME");
            Console.WriteLine(object.ReferenceEquals(asmName.Version, null));
        }



        //
        // TestAttributes
        //

        /*void TestAttributes()
        {
            //[assembly: AssemblyProduct("BluebonnetTest")]
            //[assembly: AssemblyVersion("12.34.56.78")]

            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)
                Attribute.GetCustomAttribute
                (assembly, typeof(AssemblyProductAttribute));
            var version = assembly.GetName().Version;
            Console.WriteLine("Assembly Product " + product.Product + " Version " + version);
            Console.WriteLine();
        }*/



        //
        // TestReflection1
        //

        public class Base<T, U> {}

        public class Derived<V> : Base<string, V>
        {
            public G<Derived <V>> F;

            public class Nested {}
        }

        public class G<T> {}

        public void TestReflection1()
        {
            // Get the generic type definition for Derived, and the base
            // type for Derived.
            //
            Type tDerived = typeof(Derived<>);
            Type tDerivedBase = tDerived.BaseType;

            // Declare an array of Derived<int>, and get its type.
            //
            Derived<int>[] d = new Derived<int>[0];
            Type tDerivedArray = d.GetType();

            // Get a generic type parameter, the type of a field, and a
            // type that is nested in Derived. Notice that in order to
            // get the nested type it is necessary to either (1) specify
            // the generic type definition Derived<>, as shown here,
            // or (2) specify a type parameter for Derived.
            //
            Type tT = typeof(Base<,>).GetGenericArguments()[0];
            //Type tF = tDerived.GetField("F").FieldType;
            Type tNested = typeof(Derived<>.Nested);

            DisplayGenericType(tDerived, "Derived<V>");
            DisplayGenericType(tDerivedBase, "Base type of Derived<V>");
            //DisplayGenericType(tDerivedArray, "Array of Derived<int>");
            DisplayGenericType(tT, "Type parameter T from Base<T>");
            //DisplayGenericType(tF, "Field type, G<Derived<V>>");
            DisplayGenericType(tNested, "Nested type in Derived<V>");
        }

        public static void DisplayGenericType(Type t, string caption)
        {
            Console.WriteLine(caption + " --- Type: " + t + " --- Name: " + t.Name);

            Console.WriteLine("\tIsGenericType: " + t.IsGenericType
                            + "\tIsGenericTypeDefinition: " + t.IsGenericTypeDefinition
                            + "\tIsConstructedGenericType: " + t.IsConstructedGenericType);
            Console.WriteLine("\tIsGenericParameter: " + t.IsGenericParameter
                            + "\tContainsGenericParameters: " + t.ContainsGenericParameters);
        }



        static string intToString(int value)
            => value.ToString();

        static string Call_AToString<A>(Func<A,string> aToString, A value)
            => "[" + aToString(value) + "]";

        public class GenericWithStatic<T>
        {
            public static string TypeArg() => typeof(T).ToString();
            public string Instance1() => typeof(T).ToString();
            public string Instance2<U>() => typeof(U).ToString();
        }

        void TestGenericMethod()
        {
            // static generic method in a non-generic type
            var method0 = GetType().GetMethod("Call_AToString",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            // static generic method in a non-generic type, with type argument
            var method1 = method0.MakeGenericMethod(typeof(int));

            // static non-generic method in a concrete generic type
            var method2 = typeof(GenericWithStatic<Version>).GetMethod("TypeArg",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            // static non-generic method in a generic type
            var method3 = typeof(GenericWithStatic<>).GetMethod("TypeArg",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            // instance method in a concrete generic type
            var method4 = typeof(GenericWithStatic<Version>).GetMethod("Instance1",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            // instance method in a concrete generic type
            var method5 = typeof(GenericWithStatic<>).GetMethod("Instance1",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            // instance generic method in a concrete generic type
            var method6 = typeof(GenericWithStatic<Version>).GetMethod("Instance2",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            // instance generic method in a generic type
            var method7 = typeof(GenericWithStatic<>).GetMethod("Instance2",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            PrintGenericMethod(method0);
            PrintGenericMethod(method1);
            PrintGenericMethod(method2);
            PrintGenericMethod(method3);
            PrintGenericMethod(method4);
            PrintGenericMethod(method5);
            PrintGenericMethod(method6);
            PrintGenericMethod(method7);

            Console.Write(Call_AToString(intToString, 1234));
            Func<int,string> dlg = intToString;
            var result = method1.Invoke(null, new object[] { dlg, 1234 });
            Console.Write(result);
            result = method2.Invoke(null, null);
            Console.WriteLine(result);
        }

        void PrintGenericMethod(System.Reflection.MethodInfo method)
        {
            Console.WriteLine($"Method {method.Name} IsGenericMethod {method.IsGenericMethod} "
                            + $"IsGenericMethodDefinition {method.IsGenericMethodDefinition} "
                            + $"ContainsGenericParameters {method.ContainsGenericParameters}");
        }



        void TestFindType()
        {
            Console.WriteLine("Find Types: "
                            + $"{Type.GetType("Tests.TestReflection")},"
                            //+ $"{Type.GetType("Tests.testreflection", false, true)},"
                            //+ $"{Type.GetType("System.Action`1[System.Int32]")},"
                            );
        }



        void TestPrimitives()
        {
            char sep = '\0';
            foreach (var ty in new Type[] { typeof(Boolean), typeof(Char), typeof(Int16), typeof(UInt16),
                                        typeof(Int32), typeof(UInt32), typeof(Int64), typeof(UInt64),
                                        typeof(Single), typeof(Double), typeof(IntPtr), typeof(UIntPtr),
                                        typeof(Decimal), typeof(DateTime) })
            {
                if (sep == '\t') sep = '\n'; else sep = '\t';
                Console.Write($"{sep}Type {ty,-16}{ty.IsPrimitive,5}, {Type.GetTypeCode(ty),-15}");
            }
            Console.WriteLine();
        }

    }
}
