
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestDelegate : BaseTest
    {

        public override void TestMain()
        {
            Test1();
            Test2();
            Test3();
            TestInterfaceDelegate();
            TestGenericClassWithDelegate();
            Test4();
        }



        public delegate bool TheDelegate(long theLong, object theObject);

        public static bool MyStaticDelegate(long theLong, object theObject)
        {
            //Console.WriteLine($"long {theLong} object {theObject}");
            Console.WriteLine("STATIC           - long " + theLong + " object " + theObject);
            //Console.WriteLine("STATIC DELEGATE");
            return true;
        }

        public virtual bool MyVirtualDelegate(long theLong, object theObject)
        {
            //Console.WriteLine($"long {theLong} object {theObject} this {this}");
            Console.WriteLine("VIRTUAL          - long " + theLong + " object " + theObject + " this " + this);
            //Console.WriteLine("VIRTUAL DELEGATE");
            return true;
        }

        static bool StaticGenericDelegate<TX,TY>(TX tx, TY ty)
        {
            Console.WriteLine("STATIC GENERIC   - argX " + tx + " argY   " + ty);
            return true;
        }

        bool InstanceGenericDelegate<TX,TY>(TX tx, TY ty)
        {
            Console.WriteLine("STATIC GENERIC   - argX " + tx + " argY   " + ty);
            return true;
        }

        public class MyDelegateClass<TX,TY,TZ>
        {
            public virtual TZ GenericDelegate(TX tx, TY ty)
            {
                Console.WriteLine("INSTANCE GENERIC - argX " + tx + " argY   " + ty);
                return default(TZ);
            }
        }


        void Test1()
        {
            var dlg = new TheDelegate(MyStaticDelegate);
            dlg(1, new Exception("STATIC"));

            var inst = new TestDelegate();
            dlg = new TheDelegate(inst.MyVirtualDelegate);
            dlg(2, new Exception("VIRTUAL"));

            var inst2 = new MyDelegateClass<long,object,bool>();
            dlg = new TheDelegate(inst2.GenericDelegate);
            bool b3 = dlg(3, new Exception("INSTANCE GENERIC"));

            dlg = new TheDelegate(StaticGenericDelegate<long, object>);
            dlg(4, new Exception("STATIC GENERIC"));

            dlg = new TheDelegate(this.InstanceGenericDelegate<long, object>);
            dlg(5, new Exception("INSTANCE GENERIC"));
        }


        static void Test2()
        {
            Action<string, TimeSpan> action1 =
                (string a, TimeSpan b) => Console.WriteLine("String = " + a + "TimeSpan = " + b);

            action1("Hello", new TimeSpan());

            Action<int> action2 = (int c) => Console.WriteLine("Integer " + c);

            action2(11);

            Func<int, int, bool, int> myFunc = (int a, int b, bool c) => (c ? (a + b) : (a - b));

            Console.WriteLine("Adding 10 and 5 = " + myFunc(10, 5, true));
            Console.WriteLine("Subtracting 5 from 10 = " + myFunc(10, 5, false));

            Action<(string, string)> alterStringTuple =
                ((string, string) t) => {
                    (t.Item1, t.Item2) = (t.Item2, t.Item1);
                    Console.WriteLine("REVERSED: " + t);
                };

            var v1 = ("hello", "world");
            Console.WriteLine(v1);
            alterStringTuple(v1);
            Console.WriteLine(v1);

            Action<int[]> alterArray = (int[] a) => a[1] = 9;
            var myArray = new int[3];
            Console.WriteLine(myArray[0] + " , " + myArray[1] + " , " + myArray[2]);
            alterArray(myArray);
            Console.WriteLine(myArray[0] + " , " + myArray[1] + " , " + myArray[2]);
        }

        delegate void RegularDelegateWithRef_Int(ref int myInt);
        delegate void RegularDelegateWithRef_Str(ref string myString);
        delegate void GenericDelegateWithRef<TQ>(ref TQ obj);

        static void Test3()
        {
            RegularDelegateWithRef_Str myDlgStr = (ref string sDlg) => sDlg = "ALTERED";
            GenericDelegateWithRef<string> myDlgStr2 = (ref string sDlg) => sDlg = "BACK TO ORIGINAL";

            var myStr = "ORIGINAL";
            Console.WriteLine(myStr);
            myDlgStr(ref myStr);
            Console.WriteLine(myStr);
            myDlgStr2(ref myStr);
            Console.WriteLine(myStr);

            RegularDelegateWithRef_Int myDlgInt = (ref int sDlg) => sDlg = 7;
            GenericDelegateWithRef<int> myDlgInt2 = (ref int sDlg) => sDlg = 3;
            int myInt = 4;
            Console.WriteLine(myInt);
            myDlgInt(ref myInt);
            Console.WriteLine(myInt);
            myDlgInt2(ref myInt);
            Console.WriteLine(myInt);
        }



        interface OneMethodInterface
        {
            void Run(object o);
        }

        class ClassForOneMethodInterface : OneMethodInterface
        {
            public void Run(object o) => Console.WriteLine("OneMethodInterface::Invoked");
        }

        void TestInterfaceDelegate()
        {
            var dlg = TestInterfaceDelegate_Internal(new ClassForOneMethodInterface());

            System.Delegate[] TestInterfaceDelegate_Internal(object theObject)
            {
                OneMethodInterface theInterface = theObject as OneMethodInterface;
                return new System.Delegate[] { new Action<object>(theInterface.Run) };
            }
        }



        static class GenericClassWithDelegate<T>
        {
            public static Action<T> TheDelegate = new Action<T>(DelegateMethod);
            static void DelegateMethod(T obj) => Console.WriteLine("Invoked for type " + typeof(T) + " in object " + obj);
        }
        void TestGenericClassWithDelegate()
        {
            GenericClassWithDelegate<int>.TheDelegate(123);
        }



        public class Test4ClassA
        {
            public virtual bool TheMethod(long theLong, object theObject)
            {
                Console.WriteLine("Test3ClassA");
                return true;
            }
        }

        public class Test4ClassB : Test4ClassA
        {
            public override bool TheMethod(long theLong, object theObject)
            {
                Console.WriteLine("Test3ClassB");
                return true;
            }
        }

        void Test4()
        {
            var cls = new Test4ClassB();
            var dlg = new TheDelegate(((Test4ClassA)cls).TheMethod);
            dlg(1, null);
        }

    }

}
