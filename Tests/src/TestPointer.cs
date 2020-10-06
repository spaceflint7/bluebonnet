
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
            Test5();
            Test6();
            Test7();
            Test8();
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

        //
        // Test5
        //

        struct Wrapper { public int v; }
        struct Element {
            public int v;
            public Wrapper w;
            public override string ToString() => $"{v},{w.v}";
        }

        unsafe void Test5()
        {
            var array = new Element[10];
            fixed (Element* ptr = &array[0])
            {
                int v1 = ptr->v;
                int v2 = ptr->w.v;
                ptr->v = 1;
                ptr->w = new Wrapper() { v = 2 };
                Console.WriteLine(ptr->v + "," + ptr->w.v);
            }
        }

        unsafe void Test6()
        {
            var array = new Element[10];
            fixed (Element* ptr = &array[0])
            {
                for (int i = 0; i < array.Length; i++)
                {
                    ptr[i].v = i;
                    ptr[i].w.v = i;
                }
            }
            Console.WriteLine(string.Join(",", array));

            var intArray = stackalloc int[10];
            for (int *intPtr = &intArray[0]; intPtr != &intArray[10]; intPtr++)
            {
                *intPtr = (int) (intPtr - &intArray[0]);
                Console.Write(*intPtr);
            }
            Console.WriteLine();
        }

        unsafe void Test7()
        {
            var array = new Element[10];
            fixed (Element* ptr0 = &array[5])
            {
                InnerMethod((IntPtr) ptr0);
            }
            Console.WriteLine(string.Join(",", array));
            void InnerMethod(IntPtr ptr)
            {
                Element* myElement = (Element*) ptr;
                myElement[0].v = 5;
                myElement[1].v = 6;
            }
        }

        unsafe void Test8()
        {
            if (true)
            {
                var array = new UInt64[10];
                fixed (UInt64* ptr = &array[0])
                {
                    for (int i = 0; i < array.Length; i++)
                        ptr[i] = (UInt64) 1;
                }
            }
            if (true)
            {
                var array = new IntPtr[10];
                fixed (IntPtr* ptr = &array[0])
                {
                    for (int i = 0; i < array.Length; i++)
                        ptr[i] = (IntPtr) 1;
                }
            }
        }

    }
}
