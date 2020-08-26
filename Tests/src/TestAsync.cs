
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestAsync : BaseTest
    {

        public override void TestMain()
        {
            Test1();
        }

        void Test1()
        {
            try
            {
                TestWait(false).Wait();
                Thread.Sleep(200);
                TestWait(true).Wait();
                Thread.Sleep(200);
            }
            catch (AggregateException eAggregateException)
            {
                Exception e = eAggregateException;
                while (e != null)
                {
                    Console.WriteLine("Caught exception " + e.GetType());
                    //Console.WriteLine(e.StackTrace);
                    e = e.InnerException;
                }
            }
        }

        async Task TestWait(bool @throw)
        {
            Console.WriteLine("Before Delay");
            await Task.Delay(100);
            Console.WriteLine("After Delay");
            if (@throw)
                throw new System.Exception("MyException");
        }

        /*void TestCount()
        {
            Console.WriteLine("Begin Count");
            for (uint i = 0; i < int.MaxValue; i++)
            {
                new System.Text.StringBuilder();
                if ((i % 100000000) == 0)
                    Console.WriteLine((int) i);
            }
            Console.WriteLine("Done Counting");
        }*/

    }
}

