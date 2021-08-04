
using System;
using System.IO;
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
            TestUsing();
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

        //
        // TestUnhandledException
        //

        void TestUnhandledException()
        {
            AppDomain.CurrentDomain.UnhandledException += Handler;
            //throw new Exception();
            void Handler(object sender, UnhandledExceptionEventArgs args)
                => Console.WriteLine("In Unhandled Exception Handler");
        }

        //
        // TestUnhandledException
        //

        public class MyDisposable : System.IDisposable
        {
            public void Dispose() => Console.Write("Disposed ");
            public void close() => Console.Write("Closing ");
        }

        void TestUsing()
        {
            using (var myDisposable = new MyDisposable())
            {
                myDisposable.Dispose();
                myDisposable.close();
            }
            Console.WriteLine();
        }

    }
}

