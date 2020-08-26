
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestSwitch : BaseTest
    {
        public override void TestMain()
        {
            Test(1);
            Test(2);
            Test(33);

            Test("Hello");
            Test(123.456);
            Test(new Exception());
        }

        static void Test(int arg)
        {
            switch (arg)
            {
                case 1:
                    Console.WriteLine("1");
                    break;
                case 2:
                    Console.WriteLine("2");
                    break;
                case 33:
                    Console.WriteLine("33");
                    break;
            }
        }

        static void Test(object arg)
        {
            switch (arg)
            {
                case string strArg: Console.WriteLine("string " + strArg); break;
                case double dblArg: Console.WriteLine("double " + dblArg); break;
                default:            Console.WriteLine("unknown " + arg); break;
            }
        }
    }
}
