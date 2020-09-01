
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestPointer : BaseTest
    {
        public override void TestMain()
        {
            Test1();
            Test2("Hello, world");
            Test3(default(Guid));
            Test4(default(AAA));
        }

        unsafe void Test1()
        {
            int* intptr = stackalloc int[16];
            Console.WriteLine(TestInt(intptr, 44));
            ref int refptr = ref RefIndex<int>(intptr, 7);
            refptr *= 2;
            Console.WriteLine(intptr[7]);

            Guid* guidptr = stackalloc Guid[16];
            guidptr[3] = new Guid("00000000-0000-0000-0000-000000000000");
            Console.WriteLine(guidptr[3]);

            int TestInt(int* intptr, int value)
            {
                intptr[7] = value * 2;
                return intptr[7];
            }

            ref T RefIndex<T>(T* tptr, int index) where T : unmanaged
            {
                //return ref tptr[index];
                ref T tmp = ref tptr[index];
                return ref tmp;
            }
        }

        unsafe void Test2(string from)
        {
            int n = from.Length;
            char *into = stackalloc char[n + 1];
            fixed (char* pFrom = from)
            {
                for (int i = 0; i < n; i++)
                {
                    char ch = pFrom[i];
                    if (ch >= 'a' && ch <= 'z')
                        ch = (char) (ch - 'a' + 'A');
                    //pFrom[i] = ch;
                    into[i] = ch;
                }
            }
            into[n] = '\0';
            Console.WriteLine(from + " -> " + new String(into) + "~~~");
        }

        unsafe void Test3(Guid from)
        {
            Guid *pFrom = &from;
            *pFrom = new Guid("00000001-0000-0000-0000-000000000000");
            Console.WriteLine(from);
        }

        struct AAA {}

        unsafe void Test4(AAA from)
        {
            AAA *pFrom = &from;
            *pFrom = new AAA();
            Console.WriteLine(from);
        }

    }
}
