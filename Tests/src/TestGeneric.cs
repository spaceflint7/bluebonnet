
using System;
using System.Threading;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestGeneric : BaseTest
    {
        volatile Version m_version;
        volatile EventHandler m_eventHandler;

        public override void TestMain()
        {
            TestGenericReplace();
            TestGenericRecursive();
            TestGenericVariance();
            TestGenericOverload();
            TestByRefValue();
            TestByRefArray();
            TestGenericOverload2();
            TestGenericInterface2();
            TestGenericOverload3();
            TestGenericMethodWithEnum();
        }

        //
        // TestGenericReplace
        //

        static Func<Version> CreateVersionObject = () => new Version();

        static void EventCallback(object sender, EventArgs e) {}

        void TestGenericReplace()
        {
            #pragma warning disable 0420
            Interlocked.CompareExchange(ref m_eventHandler, EventCallback, null);
            var version1 = LazyInitializer.EnsureInitialized<Version>(ref m_version, CreateVersionObject);
            var version2 = LazyInitializer.EnsureInitialized<Version>(ref m_version, CreateVersionObject);
            Console.WriteLine("TestGenericReplace? " + object.ReferenceEquals(version1, version2));
            #pragma warning restore 0420
        }

        //
        // TestGenericRecursive
        //

        public class Base<T>
        {
            protected void PrintType() { Console.WriteLine(typeof(T)); }
        }

        public class Base2<T> : Base<T> { }

        private sealed class Recursive<U> : Base<Base<U>[]>
        {
            private readonly Base<U>[] _items;

            internal Recursive(Base<U>[] itemsCopy) : base()
            {
                _items = itemsCopy;
                base.PrintType();
            }
        }

        void TestGenericRecursive()
        {
            new Recursive<string>(new Base<string>[2]);
        }

        //
        // TestGenericVariance
        //

        public interface InterfaceVariant<in TIn, out TOut>
        {
            TOut Method(TIn arg);
        }

        void TestGenericVariance()
        {
            var t0 = typeof(InterfaceVariant<Exception, Exception>);
            int ti = 0;
            foreach (var t1 in new Type[] {
                typeof(InterfaceVariant<object, Exception>),
                typeof(InterfaceVariant<Exception, Exception>),
                typeof(InterfaceVariant<SystemException, Exception>),
                typeof(InterfaceVariant<ISerializable, Exception>),
                typeof(InterfaceVariant<Exception, object>),
                typeof(InterfaceVariant<Exception, SystemException>),
                typeof(InterfaceVariant<Exception, ISerializable>),
                typeof(InterfaceVariant<object, object>),
                typeof(InterfaceVariant<SystemException, SystemException>),
                typeof(InterfaceVariant<ISerializable, ISerializable>),
                })
            {
                Console.Write((ti++) + "=" + t1.IsAssignableFrom(t0) + ";");
            }
            Console.WriteLine();
            Console.WriteLine("Exception is ISerializable? " + ((new Exception()) is ISerializable));
        }

        //
        // TestGenericOverload
        //

        public abstract class Base5<TInput,TOutput>
        {
            public abstract TOutput Invoke(TInput value);
        }

        public class Derived5 : Base5<Version,string>
        {
            public override string Invoke(Version value) => value.ToString();
        }

        void TestGenericOverload()
        {
            var result = (new Derived5()).Invoke(new Version());
            Console.WriteLine($"Result type {result.GetType()} value {result}");
        }


        //
        // TestByRef
        //

        void TestByRefValue()
        {
            int a = 1;
            Console.Write(a);
            f1(ref a);
            Console.Write(a);
            f2<int>(ref a);
            Console.Write(a);

            void f1(ref int a) => a *= 2;
            void f2<T>(ref T a) => a = (T) (object) (((int) (object) a) * 2);
        }

        void TestByRefArray()
        {
            var a = new int[1];
            Console.Write(a.Length);
            f1(ref a);
            Console.Write(a.Length);
            f2<int>(ref a);
            Console.WriteLine(a.Length);

            void f1(ref int[] a) => a = new int[2];
            void f2<T>(ref T[] a) => a = new T[3];
        }

        //
        // test generic resolution
        //

        class MyTest<T1,T2>
        {
            static void F1<S1>()
            {
                new MyTest<S1,ValueTuple<T1,T2>>();
            }
        }

        //
        //
        //

        public interface I1<T> { int Get(T v); }
        public interface I2<T> { int Get(T v); }

        class MyOv1<T1,T2> : I1<T2>, I2<T1>
        {
            public virtual int Get(T1 v) => 1;
            public virtual int Get(T2 v) => 2;
        }

        class MyOv2<T1,T2> : MyOv1<T1,T2>
        {
            public override int Get(T1 v) => 3;
            public override int Get(T2 v) => 4;
        }

        void TestGenericOverload2()
        {
            var a = new MyOv1<int,bool>();
            Console.Write(a.Get(1));
            Console.Write(a.Get(true));
            var b = new MyOv2<int,bool>();
            Console.Write(b.Get(1));
            Console.WriteLine(b.Get(true));
        }

        //
        // TestGenericInterface
        //

        interface AaaIfc<T>
        {
            void Method<S>(T a);
        }

        class AaaImpl<U,T> : AaaIfc<T>
        {
            public void Method<S>(T a) => Console.WriteLine(typeof(T) + "," + typeof(S));
        }

        void TestGenericInterface2()
        {
            AaaIfc<bool> c = new AaaImpl<char,bool>();
            c.Method<int>(false);
        }

        //
        // TestGenericOverload3
        //

        public abstract class CccB1<T1,T2> { public abstract void DoIt(ref T1 a, ref T2 b); }

        public class CccC1<T1> : CccB1<T1,int>     { public override void DoIt(ref T1 a, ref int b)     => Console.Write("OK1 "); }
        public class CccC2<T1> : CccB1<T1,Version> { public override void DoIt(ref T1 a, ref Version b) => Console.Write("OK2 "); }
        public class CccC3<T1> : CccB1<T1,object>  { public override void DoIt(ref T1 a, ref object b)  => Console.Write("OK3 "); }

        void TestGenericOverload3()
        {
            bool bFalse = false;
            int iZero = 0;
            Version vZero = null;
            object oZero = null;

            CccB1<bool,int>     c1  = new CccC1<bool>();
            CccB1<bool,Version> c2  = new CccC2<bool>();
            CccB1<bool,object>  c3  = new CccC3<bool>();

            c1.DoIt(ref bFalse, ref iZero);
            c2.DoIt(ref bFalse, ref vZero);
            c3.DoIt(ref bFalse, ref oZero);
            Console.WriteLine();
        }

        //
        // TestGenericMethodWithEnum
        //

        void TestGenericMethodWithEnum()
        {
            Console.WriteLine(TestGenericMethodWithEnum_<TypeCode>(TypeCode.Int32));

            T TestGenericMethodWithEnum_<T>(T a)
            {
                Console.Write(a);
                Console.Write(",");
                return a;
            }
        }

    }
}
