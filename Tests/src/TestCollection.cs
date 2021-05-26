
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestCollection : BaseTest
    {

        public override void TestMain()
        {
            var c0 = new Dictionary<string, string>();

            var c1 = ArrayOfInt();
            TestList<int>(c1,                true);
            TestList<int>(new List<int>(c1), true);

            TestList<ExceptionDispatchInfo>(ListOfException(), false);

            //TestDictionary();
            TestSet();
            TestStack();
            TestHash();
            TestArrayList();
        }



        void TestList<T>(IList<T> list, bool compare)
        {
            int n = list.Count;
            int i = 0;
            foreach (var elem in list)
            {
                if (compare)
                    Console.Write(elem + " & " + list[i] + " == " + elem.Equals(list[i]) + " ; ");
                else
                    Console.Write(elem.GetType().Name + (! elem.Equals(list[i]) ? " BAD " : "") + " ; ");
                if (    (! list.Contains(elem)) || (list.IndexOf(elem) != i)    )
                    Console.Write("(Bad) ");
                i++;
            }
            Console.WriteLine();
            ((IList)list).Clear();
        }



        IList<int> ArrayOfInt()
        {
            var array = new int[10];
            int index = 0;
            for (int j = 0; index < 10; j++)
            {
                if ((j % 7) == 0)
                {
                    array[index++] = j;
                    // test GetValue(int) and GetValue(long)
                    if (! array.GetValue((int) (index - 1)).Equals(j))
                        Console.WriteLine("Bad");
                    if (! array.GetValue((long) (index - 1)).Equals(j))
                        Console.WriteLine("Bad");
                }
            }
            return array;
        }



        IList<ExceptionDispatchInfo> ListOfException()
        {
            var list = new List<ExceptionDispatchInfo>();
            for (int i = 0; i < 10; i++)
                list.Add(ExceptionDispatchInfo.Capture(new RankException()));
            return list;
        }



        public class CaseInsensitiveComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => x.ToUpper().Equals(y.ToUpper());
            public int GetHashCode(string x) => x.ToUpper().GetHashCode();
        }



        public void TestDictionary()
        {
            var myDict = new Dictionary<string, bool>();
            myDict.Add("test", true);
            try { myDict.Add("test", false); } catch (Exception e) { Console.WriteLine(e.GetType()); }
            foreach (var kv in myDict) { Console.WriteLine(kv.Key + " = " + kv.Value); }

            myDict = new Dictionary<string, bool>(myDict, new CaseInsensitiveComparer());
            Console.WriteLine(myDict["TEST"]);
        }



        public void TestSet()
        {
            var a = new HashSet<string>();
            a.Add("hello");
            a.Add("world");
            a.IntersectWith(new string[] { "world", "wide", "web" });
            foreach (var e in a) Console.WriteLine(e);
        }



        public void TestStack()
        {
            var stk = new System.Collections.Generic.Stack<string>();
            stk.Push("hello");
            Test<string>("world");
            Test<object>("object");

            System.Collections.Generic.Stack<T> Test<T>(T elem)
            {
                var stk = new System.Collections.Generic.Stack<T>();
                stk.Push(elem);
                return stk;
            }
        }



        public void TestHash()
        {
            var dict = new System.Collections.Hashtable();
            dict["int1"] = (int) 10;
            dict["int2"] = (int) 20;
            int sum = 0;
            foreach (var key in dict.Keys)
                sum += (int) dict[key];
            Console.WriteLine(sum);
        }


        private class ListElement
        {
            [java.attr.RetainType] public int v1;
            [java.attr.RetainType] public int v2;
        }

        public void TestArrayList()
        {
            var list = new System.Collections.ArrayList(
                new ListElement[] {
                    new ListElement { v1 = 1, v2 = 2 }
                });
            foreach (var e in list) System.Console.WriteLine(e);
        }

    }
}
