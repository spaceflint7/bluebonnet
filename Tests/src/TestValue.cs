
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestValue : BaseTest
    {

        public struct MyValue : IFormattable
        {
            public int a;
            public string ToString(string format, IFormatProvider formatProvider)
            {
                a++;
                return a.ToString();
            }
        }

        public class MyClass : IFormattable
        {
            public int a;
            public string ToString(string format, IFormatProvider formatProvider)
            {
                a++;
                return a.ToString();
            }
        }

        public override void TestMain()
        {
            TestBoxing();
            TestObject();
        }

        void TestBoxing()
        {
            var myv = new MyValue();
            Console.Write(TestHelper1(myv));
            Console.Write(TestHelper2(myv));
            Console.Write(TestHelper3(myv));
            Console.Write(TestHelper4(myv));
            Console.Write(myv.a);
            Console.Write(";");

            var myv2 = new MyClass();
            Console.Write(TestHelper1(myv2));
            Console.Write(TestHelper2(myv2));
            Console.Write(TestHelper4(myv2));
            Console.Write(myv2.a);
            Console.WriteLine();

            string TestHelper1(IFormattable ifmt) => ifmt.ToString("N", null);
            string TestHelper2(object ifmt) => ((IFormattable) ifmt).ToString("N", null);
            string TestHelper3(MyValue ifmt) => ifmt.ToString("N", null);
            string TestHelper4<T>(T ifmt) where T : IFormattable => ifmt.ToString("N", null);
        }

        void TestObject()
        {
            var myv = default(MyValue);
            TestObject1(ref myv);

            void TestObject1(ref MyValue myv)
            {
                TestObject2(myv);
                var v = myv;
                object o = myv;
                myv = v;
            }

            void TestObject2(MyValue myv)
            {
                var v = myv;
                object o = myv;
            }
        }

    }
}
