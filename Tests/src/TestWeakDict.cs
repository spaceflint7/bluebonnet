
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;


namespace Tests
{
    [TestClass]
    public class TestWeakDict : BaseTest
    {

        static ConditionalWeakTable<object, object> Dict = new ConditionalWeakTable<object, object>();
        static object globalKey4;

        public override void TestMain()
        {
            Test1();

            GC.Collect();
            System.Threading.Thread.Sleep(700);

            object v;
            Dict.TryGetValue("dummy", out v);

            GC.Collect();
            System.Threading.Thread.Sleep(700);

            Console.WriteLine("Actual End");
        }

        static void Test1()
        {
            var k = Test2();

            Console.WriteLine("Collection Main");
            GC.Collect();
            System.Threading.Thread.Sleep(700);

            object v;
            Dict.TryGetValue(k, out v);
            //Console.WriteLine("Value = " + ((Item) v).name);

            Console.WriteLine("Clean Up");
            //k = null;
        }

        static object Test2()
        {
            Dict.Add(new Item("Key1"), new Item("Value1"));

            var kv2 = new Item("Key2", new Item("Value2"));
            Dict.Add(kv2, kv2.value);

            var kv3 = new Item("Key3", new Item("Value3"));
            Dict.Add(kv3, kv3.value);

            var k4 = new Item("Key4");
            var v4 = new Item("Value4", k4);
            Dict.Add(k4, v4);
            globalKey4 = k4;

            Console.WriteLine("Collection Test");
            GC.Collect();
            System.Threading.Thread.Sleep(700);

            return kv2;
        }



        public class Item
        {
            public string name;
            public object value;

            public Item(string nm)
            {
                name = nm;
            }

            public Item(string nm, object vl)
            {
                name = nm;
                value = vl;
            }

            ~Item()
            {
                #if STANDALONE
                Console.WriteLine("Finalization for " + name);
                #endif
            }
        }

    }
}
