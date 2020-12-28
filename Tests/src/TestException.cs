
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class TestException : BaseTest
    {

        public class MyException<T> : Exception
        {
            public string MyType() => typeof(T).FullName;
        }


        public override void TestMain()
        {
            TestNestedTry1();
            TestNestedTry2();
            TestNestedTry3(true);
            TestNestedTry3(false);
            TestNestedTry4();
            TestNestedTry5();
            TestNestedTry6();
            TestNestedTry7();
            Console.WriteLine("----------");

            TestFaultHandler();
            TestGenericException(null);

            var eq = new Exception();
            Console.WriteLine($"EXCEPTION: {eq.HResult}/{eq}");

            try
            {
                int b = 0;
                int a = 5 / b;
            }
            catch (ArithmeticException e)
            {
                try
                {
                    object x = null;
                    x.ToString();
                }
                catch (NullReferenceException)
                {
                    //throw;
                }

                Console.WriteLine($"EXCEPTION: {e.HResult}/{e.GetType()}");
                //throw;
            }

            bool ok1 = false;
            bool ok2 = false;
            try
            {
                Test("", true);
            }
            catch (Exception e)
            {
                if (e.Message == "TEST_FILTER")
                    ok1 = true;
                else
                    Console.WriteLine("=> EXCEPTION " + e.Message);
            }

            try
            {
                ok2 = Test("", false);
            }
            catch (Exception e)
            {
                Console.WriteLine("=> EXCEPTION " + e.Message);
            }
            Console.WriteLine("OK1 = " + ok1 + " OK2 = " + ok2);
        }

        public static bool Test(string args, bool testFilter)
        {
            bool ok = false;
            try
            {
                try
                {
                    try
                    {
                        var args2 = args;
                        if (args2 == args)
                        {
                            throw new InvalidCastException(
                                        testFilter ? "TEST_FILTER" : "TEST_CATCH");
                        }
                    }
                    finally
                    {
                        Console.WriteLine("START");
                    }
                    Console.WriteLine("NOT REACHED 2");
                }
                finally
                {
                }
            }
            catch (InvalidProgramException)
            {
                Fail();
            }
            catch (InvalidCastException e) when (e.Message == "TEST_FILTER")
            {
                if (testFilter)
                    ok = true;
                Console.WriteLine("GOOD FILTER <" + e.Message + ">");
                throw e;
            }
            catch (InvalidCastException e) when (e.Message == "DUMMY_FILTER")
            {
                Console.WriteLine("DUMMY FILTER <" + e.Message + ">");
                throw e;
            }
            catch (InvalidCastException e)
            {
                if (! testFilter)
                    ok = true;
                Console.WriteLine("GOOD CATCH <" + e.Message + ">");
            }
            catch
            {
                Fail();
            }
            finally
            {
                Console.WriteLine("FINALLY");
            }
            Console.WriteLine("THE END");
            return ok;
        }

        static void Fail([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            Console.WriteLine("Fail @ " + lineNumber);
        }

        static void TestGenericException(Exception nullArg)
        {
            try
            {
                throw new MyException<int>();
            }
            catch (MyException<float> e) // when (e != null)
            {
                Console.WriteLine("Caught Float");
                nullArg = e;
            }
            catch (MyException<int> e) // when (e != null)
            {
                Console.WriteLine("Caught Int " + e.MyType());
                nullArg = e;
            }
            //Console.WriteLine("RESULT = " + nullArg.GetType().ToString());
        }

        //
        // test fault exception clause.  it is generated as part of the class that
        // is generated to implement the yielding method TestFaultHandler.
        //

        static void TestFaultHandler()
        {
            try
            {
                var numbers = new int[3]; numbers[0] = 123; numbers[1] = 456; numbers[2] = 789;
                foreach (var num in TestFaultHandler(numbers))
                    ;
                /*foreach (var str in TestFaultHandler(new string[] { "Hello", "World" }))
                    ;*/
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught Exception Of Type: " + e + "\n" + e.StackTrace);
            }
        }

        static IEnumerable<int> TestFaultHandler(int[] numbers)
        {
            try
            {
                //foreach (var str in strings) { throw new Exception(); yield return str; }
                foreach (var num in numbers) { yield return num; }
            }
            finally
            {
                Console.WriteLine("TestFaultHandler Finally");
            }
        }



        void TestNestedTry1()
        {
            Console.WriteLine("-----TestNestedTry1");
            try
            {
                try
                {
                    throw new Exception("Try1Exception");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Inner Catch - " + e.Message);
                }
                finally
                {
                    Console.WriteLine("In Inner Finally");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Outer Catch - " + e.Message);
            }
            finally
            {
                Console.WriteLine("In Outer Finally");
            }
        }



        void TestNestedTry2()
        {
            Console.WriteLine("-----TestNestedTry2");
            try
            {
                try
                {
                    throw new Exception("Try2Exception");
                }
                finally
                {
                    Console.WriteLine("In Inner Finally");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Outer Catch - " + e.Message);
            }
            finally
            {
                Console.WriteLine("In Outer Finally");
            }
        }



        void TestNestedTry3(bool innerCatch)
        {
            Console.WriteLine("-----TestNestedTry3 " + innerCatch);
            try
            {
                try
                {
                    throw new Exception("Try3Exception/" + innerCatch.ToString());
                }
                catch (Exception e) when (innerCatch)
                {
                    Console.WriteLine("Inner Catch - " + e.Message);
                }
                finally
                {
                    Console.WriteLine("In Inner Finally");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Outer Catch - " + e.Message);
            }
            finally
            {
                Console.WriteLine("In Outer Finally");
            }
        }



        void TestNestedTry4()
        {
            Console.WriteLine("-----TestNestedTry4");
            try
            {
                try
                {
                    throw new ArgumentException("ARG");
                }
                finally
                {
                    try
                    {
                        throw new NotSupportedException("SUP");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Caught " + e.GetType());
                    }
                    Console.WriteLine("End of TestNestedTry4 Finally");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught " + e.GetType());
            }
        }



        void TestNestedTry5()
        {
            Console.WriteLine("-----TestNestedTry5");
            try
            {
                try
                {
                    throw new ArgumentException("ARG");
                }
                finally
                {
                    throw new NotSupportedException("SUP");
                    #pragma warning disable 0162
                    Console.WriteLine("End of TestNestedTry5 Finally");
                    #pragma warning restore 0162
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught " + e.GetType());
            }
        }



        void TestNestedTry6()
        {
            var str = "A";
            try
            {
                if (str == "A")
                {
                    str = "B";
                }
            }
            finally
            {
                str = "C";
                if (str == "B")
                {
                    try
                    {
                        if (str == "C")
                        {
                            str = "D";
                        }
                    }
                    finally
                    {
                        str = "E";
                    }
                }
            }
        }




        void TestNestedTry7()
        {
            try
            {
                Console.WriteLine("Try1.Stmt1");
                try
                {
                    Console.WriteLine("Try2.Stmt2");
                    return;
                }
                finally
                {
                    Console.WriteLine("Try2.Finally");
                }
                #pragma warning disable 0162
                Console.WriteLine("Try1.Stmt2");
            }
            finally
            {
                Console.WriteLine("Try1.Finally");
            }
        }
    }
}
