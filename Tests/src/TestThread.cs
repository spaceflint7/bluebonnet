
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestThread : BaseTest
    {

        public volatile Mutex mutex0;
        public volatile AutoResetEvent event1;
        public volatile AutoResetEvent event2;
        public volatile Semaphore semaphore;

        [ThreadStatic] static int threadValue = 7;



        public override void TestMain()
        {
            Test1();
            Thread.Sleep(100);
            Test2();
            Thread.Sleep(100);
        }



        void Test1()
        {
            threadValue = 123;

            mutex0 = new Mutex();
            event1 = new AutoResetEvent(false);
            event2 = new AutoResetEvent(false);

            (new Thread(Test1Thread1)).Start();
            (new Thread(Test1Thread2)).Start();

            Thread.Sleep(100);
            Console.WriteLine("Signal Event1");
            event1.Set();
            Thread.Sleep(500);
            Console.WriteLine("Signal Event2");
            event2.Set();

            Thread.Sleep(300);
            Console.WriteLine("Main Thread Value = " + threadValue);
        }

        void Test1Thread1()
        {
            threadValue = 456;

            Thread.Sleep(300);
            try { mutex0.WaitOne(); }
            catch (Exception e) { Console.WriteLine("Exception " + e.GetType()); }
            Console.WriteLine("Mutex acquired in Thread1");
            var waits = new WaitHandle[] { mutex0, event1, event2 };
            var result = WaitHandle.WaitAll(waits, 2000);
            Console.WriteLine("All Waits Satisfied in Thread1: " + result);
            try
            {
                mutex0.ReleaseMutex();
                mutex0.ReleaseMutex();
            }
            catch (Exception e) { Console.WriteLine("Exception " + e.GetType()); }

            Console.WriteLine("Test1 Thread Value = " + threadValue);
        }

        void Test1Thread2()
        {
            threadValue = 789;

            mutex0.WaitOne();
            Console.WriteLine("Mutex acquired in Thread2");
            mutex0.ReleaseMutex();
            event1.WaitOne();
            Console.WriteLine("Event1 signalled in Thread2");
            Console.WriteLine("Test2 Thread Value = " + threadValue);
            event1.Set();
        }



        void Test2()
        {
            /*
            ThreadPool.GetMinThreads(out var count1, out var count2);
            ThreadPool.GetMaxThreads(out var count3, out var count4);
            Console.WriteLine("Thread pool has thread counts " + count1 + "," + count2 + "," + count3 + "," + count4);
            */

            ThreadPool.QueueUserWorkItem(new WaitCallback(
                (x => Console.WriteLine("In Delegate, Argument = " + x))),
                "TestQueueUserWorkItemString");

            var thread = new Thread(Test2Thread);
            thread.Start();
            thread.Join();
        }

        void Test2Thread()
        {
            semaphore = new Semaphore(5, 5);
            var events = new WaitHandle[10];
            for (int i = 0; i < 10; i++)
                events[i] = Test2WorkItem(i);
            WaitHandle.WaitAll(events);
            Console.WriteLine("THREAD POOL TEST OK");
        }

        WaitHandle Test2WorkItem(int workItemNumber)
        {
            var workItemEvent = new AutoResetEvent(false);
            ThreadPool.QueueUserWorkItem(new WaitCallback(x =>
            {
                bool acquired = semaphore.WaitOne(0);
                if (! acquired)
                    acquired = semaphore.WaitOne();
                try
                {
                    for (var i = 0; i < 10; i++)
                    {
                        //Console.WriteLine("iteration " + i + " out of 20 in work item " + workItemNumber);
                        Thread.Sleep(20);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                semaphore.Release();
                workItemEvent.Set();

            }), "TestQueueUserWorkItemString");
            return workItemEvent;
        }

    }
}

