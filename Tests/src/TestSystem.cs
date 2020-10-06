
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestSystem : BaseTest
    {

        public override void TestMain()
        {
            TestSuppressGC();
            TestUnhandledException();
        }

        //
        // TestSuppressGC
        //

        public class MyFinalizable
        {
            public string which;
            public MyFinalizable(string which) { this.which = which; }
            ~MyFinalizable() => Console.WriteLine("In Finalizer of " + which);
        }

        void TestSuppressGC()
        {
            new MyFinalizable("One");
            GC.SuppressFinalize(new MyFinalizable("Two"));
            GC.Collect();
            System.Threading.Thread.Sleep(100);
        }

        void TestUnhandledException()
        {
            AppDomain.CurrentDomain.UnhandledException += Handler;
            //throw new Exception();
            void Handler(object sender, UnhandledExceptionEventArgs args)
                => Console.WriteLine("In Unhandled Exception Handler");
        }

    }
}

