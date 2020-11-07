
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestArray : BaseTest
    {

        public override void TestMain()
        {
            Console.Write((object)null);
            TestAccess();
            TestGeneric();
            TestLock();
            TestReflect<TestArray>();
            TestLoadStore();
            TestCopy();
            TestInit();
            TestArrayCast();
            TestStoreIntoArrayOfGenericType<int>(new int[10], 7);
            TestValue(true);
            TestGeneric2<int>(new int[2]);
            TestString("Hello, World!");
            TestMisc();
        }

        void TestAccess()
        {
            var arr2 = new int[8,8];
            arr2[0,1] = 99;

            var arr3 = new int[8,8,8];
            arr3[0,1,2] = 99;

            var arr4 = new int[8,8,8,8];
            arr4[0,1,2,3] = 99;

            var arr5 = new int[8,8,8,8,8];
            arr5[0,1,2,3,4] = 99;

            var arr6 = new int[8,8,8,8,8,8];
            arr6[0,1,2,3,4,5] = 99;

            var arr7 = new int[8,8,8,8,8,8,8];
            arr7[0,1,2,3,4,5,6] = 99;

            var arr8 = new int[8,8,8,8,8,8,8,8];
            arr8[0,1,2,3,4,5,6,7] = 99;
            Console.WriteLine("Check Value 99 = " + arr8[0,1,2,3,4,5,6,7]);

            var arr32 = new int[1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1];

            var arr = new int[8,8];
            arr[0,0] = 1; arr[1,0] = 2; arr[2,0] = 3; arr[3,0] = 4;
            arr[4,0] = 5; arr[5,0] = 6; arr[6,0] = 7; arr[7,0] = 8;
            arr[4,4] = 9; WriteIntoArray(arr, 5, 5);
            Console.Write(arr[4,4] + "," + arr[5,5] + ",");
            TestEnum(arr);
        }

        void WriteIntoArray(int[,] arr, int x, int y) => arr[x,y] = 8;

        void TestEnum(object a)
        {
            foreach (var e in (System.Collections.IEnumerable) a)
                Console.Write( e);
            Console.WriteLine();
            Console.WriteLine("Array " + a.GetType().GetElementType() + " of rank " + (a as Array).Rank + " castable to IList? " + (a is System.Collections.IList));
            /*
            Console.WriteLine("Castable to IStructuralComparable? " + (a is System.Collections.IStructuralComparable));
            Console.WriteLine("Castable to IStructuralEquatable? " + (a is System.Collections.IStructuralEquatable));
            */
        }

        void TestGeneric()
        {
            var arr = new int[9]; arr[5] = 5;
            var asListInt = (((object) arr) as System.Collections.Generic.IList<int>);
            Console.Write("Array castable to IList<int> ? " + (asListInt != null) + " and IEnumerable<int> = ");
            foreach (var e in asListInt)
                Console.Write(e);
            Console.WriteLine();
            Console.WriteLine("Castable to IList<int> = " + (((object) asListInt) is System.Collections.Generic.IList<int>)
                        + "\t\tCastable to IList<float> = " + (((object) asListInt) is System.Collections.Generic.IList<float>));
            /*
            Console.WriteLine("Castable to IList<IConvertible> = " + (((object) asListInt) is System.Collections.Generic.IList<IConvertible>));
            Console.WriteLine("Castable to IReadOnlyList<IConvertible> = " + (((object) asListInt) is System.Collections.Generic.IReadOnlyList<IConvertible>));
            */
            /*var asEnumInt  = (((object) arr) as System.Collections.Generic.IEnumerable<int>);
            Console.WriteLine("Castable to IEnumerable<int> = " + (((object) asEnumInt) is System.Collections.Generic.IEnumerable<int>)
                          + "\tCastable to IEnumerable<float> = " + (((object) asEnumInt) is System.Collections.Generic.IEnumerable<float>)
                          + "\nCastable to IEnumerable<IConvertible> = " + (((object) asEnumInt) is System.Collections.Generic.IEnumerable<IConvertible>));
            var asEnumInt  = (((object) arr) as System.Collections.Generic.IEnumerable<IConvertible>);
            */

            Console.Write("IStructuralEquatable Hash = " + (arr as System.Collections.IStructuralEquatable).GetHashCode(System.Collections.StructuralComparisons.StructuralEqualityComparer));
            var asReadOnly = Array.AsReadOnly<int>(arr);
            Console.Write(" AsReadOnly = " + (asReadOnly != null) + " Enumeration = ");
            foreach (var e in asReadOnly)
                Console.Write(e);
            Console.WriteLine();

            var asListRef = ((new RefX[4]) as System.Collections.Generic.IList<RefX>);
            Console.WriteLine("Array of RefX castable to obj? "          + (null != (((object) asListRef) as System.Collections.Generic.IList<object>)));
            Console.WriteLine("Array of RefX castable to IfcX? "         + (null != (((object) asListRef) as System.Collections.Generic.IEnumerable<IfcX>)));
            Console.WriteLine("Array of RefX castable to IConvertible? " + (null != (((object) asListRef) as System.Collections.Generic.IEnumerable<IConvertible>)));
            Console.WriteLine("Array of RefX castable to ReadOnly IfcX? "+ (null != (((object) asListRef) as System.Collections.Generic.IReadOnlyList<IfcX>)));

            var listRef = new System.Collections.Generic.List<RefX>();
            Console.WriteLine("List of RefX castable to ReadOnly IfcX? " + (null != (((object) listRef) as System.Collections.Generic.IReadOnlyList<IfcX>)));

        }

        void TestReflect<T>()
        {
            PrintArrayType("int array", typeof(int[]));
            PrintArrayType("str array", typeof(string[]));
            PrintArrayType("gen array", typeof(T[]));

            CompareTypes(typeof(int[]), (new int[1]).GetType());
            CompareTypes(typeof(int[,]), (new int[1,1]).GetType());
            CompareTypes(typeof(int[][]), (new int[1][]).GetType());
        }

        void PrintArrayType(string title, Type type)
        {
            Console.WriteLine(title + ": "
                                + " IsArray? " + type.IsArray + " Rank? " + type.GetArrayRank()
                                + " Element? " + type.GetElementType());
        }

        void CompareTypes(Type a, Type b)
        {
            if (! object.ReferenceEquals(a, b))
                Console.WriteLine("Different types: " + a + " AND " + b);
        }

        void TestLock()
        {
            var arr = new Version[2];
            var asList = arr as System.Collections.Generic.IList<Version>;
            lock (asList)
            {
                // confirm that locking the proxy object actually locks the array
                Console.WriteLine("Array locked? " + System.Threading.Monitor.IsEntered(arr));
            }
        }

        void TestLoadStore()
        {
            var intArr = new int[5];
            var refArr = new RefX[5];
            var valArr = new ValX[5];

            /*
            Console.WriteLine("RefX castable to IfcArray? " + (refArr is IfcX[])
                            + "," + (refArr is System.Collections.Generic.IList<IfcX>));
            Console.WriteLine("ValX castable to IfcArray? " + (valArr is IfcX[])
                            + "," + (valArr is System.Collections.Generic.IList<IfcX>));
            */

            intArr[3] = 10;
            refArr[3] = new RefX { x = 20 };
            valArr[3] = new ValX { x = 30 };

            var ref3 = refArr[3];
            var val3 = valArr[3];
            Change(refArr[3]);
            Change(valArr[3]);
            ref3.x *= 2;
            val3.x *= 2;

            Console.Write(ref3.x + "," + val3.x + "," + intArr[3] + "," + refArr[3].x + "," + valArr[3].x);
            valArr[4] = val3;
            val3 = default(ValX);
            Console.Write(" AND " + valArr[4].Print());

            valArr[3].Change();
            Console.Write(" AND " + valArr[3].Print());

            IfcX ifc3 = valArr[3];
            ifc3.Change();
            Console.Write(" AND " + valArr[3].Print());

            var ifcArr = new IfcX[3];
            ifcArr[1] = valArr[3];
            ifcArr[1].Change();
            Console.Write(" AND " + valArr[3].Print());

            void Change(IfcX x) => x.Change();

            TestGeneric<RefX>();
            TestGeneric<ValX>();
            Console.WriteLine();

            TestGeneric2<int>();
            TestGeneric2<bool>();
            TestGeneric2<Exception>();
            Console.WriteLine();

            //

            object valBox = valArr[1];
            IfcX valBox2 = (IfcX) valBox;
            IfcX valBox3 = valArr[1];
            ValX valBox4 = (ValX) valBox;
            ((ValX) valBox).Change();
            valBox2.Change();
            Console.WriteLine("BOX " + valArr[1].Print() + " , " + ((IfcX) valBox).Print() + " , " + valBox2.Print() + " , " + valBox3.Print() + " , " + valBox4.Print());

            // test invalid store

            try { ((object[])(new Version[4]))[0] = new ValX(); }
            catch (Exception e) { Console.WriteLine(e.GetType()); }

            void TestGeneric<T>() where T : IfcX, new()
            {
                T[] arr = new T[5];
                arr[3] = new T();
                var x = arr[3];
                x.Change();
                arr[3].Change();
                Console.Write(" ; " + x.Print() + " vs " + arr[3].Print());
            }

            void TestGeneric2<T>()
            {
                T[] arr = new T[5];
                arr[3] = default(T);
                foreach (var e in arr) Console.Write("[" + (e == null ? "null" : e.ToString()) + "]");
            }
        }

        interface IfcX { void Change(); string Print(); }
        class RefX : IfcX {
            public int x;
            public void Change() => x = (x + 1) * 9;
            public string Print() => x.ToString();
        }
        struct ValX : IfcX {
            public int x;
            public void Change() => x = (x + 1) * 9;
            public string Print() => x.ToString();
        }



        void TestStoreIntoArrayOfGenericType<T>(T[] arr, T v)
        {
            var arr2 = new T[10];
            arr[7] = v;
            arr2[7] = v;
            v = default(T);
            Console.WriteLine("Input Array[7] = " + arr[7] + " ; Local Array[7] = " + arr2[7] + " ; and v = " + v);
        }



        void TestCopy()
        {
            var arr2D_int_orig = new int[4,6];
            for (int i = 0; i < 4; i++) { for (int j = 0; j < 6; j++) { arr2D_int_orig[i,j] = (i + 1) * 100 + (j + 1); } }
            var arr2D_int_copy = arr2D_int_orig.Clone() as Array;
            for (int i = 0; i < 4; i++) { for (int j = 0; j < 6; j++) { arr2D_int_orig[i,j] *= 2; } }
            PrintArray(arr2D_int_copy);
        }



        void TestInit()
        {
            var boolArr = new bool[] { true, true, true, true };
            var shortArr = new short[] { 123, 456, 789 };
            var charArr = new char[] { 'X', 'Y', 'Z' };
            var floatArr = new float[] { 1.0F, 2.0F, 3.0F };
        }


        /*string TestSwitch(string str)
        {
            test for other cases of <PrivateImplementationDetails> initializers,
            but the following does not seem to generate such initializers,
            unlike the array initializer in TestInit()

            var dict = new System.Collections.Generic.Dictionary<int,string>()
            {
                { 111, "AAA" },
                { 222, "BBB" },
                { 333, "CCC" },
                { 444, "DDD" },
                { 555, "EEE" },
            };

            string retval;
            switch (str.ToLower())
            {
                case "a": retval = "1"; break;
                case "b": retval = "2"; break;
                case "c": retval = "3"; break;
                default:  retval = "0"; break;
            }
            return retval;
        }*/


        /*void TestCollection()
        {
            var arr = new Exception[] { new Exception(), new Exception(), new Exception() };
            var qqq = (System.Collections.Generic.ICollection<Exception>) arr;
            var qqq2 = (Exception[]) qqq;
            var coll = AsColl(arr);
            var arr2 = AsArray(coll);
            Console.Write("Array as Collection:  Count = " + coll.Count);
            Console.WriteLine(" And as Array:  Count = " + arr2.Length);
            System.Collections.Generic.ICollection<Exception> AsColl(Exception[] arr)
                => (System.Collections.Generic.ICollection<Exception>) arr;
            Exception[] AsArray(System.Collections.Generic.ICollection<Exception> coll)
                => (Exception[]) coll;
        }*/



        void PrintArray(Array array)
        {
            var iterator = array.GetEnumerator();
            int dims = array.Rank;
            var idxs = new int[dims];
            var lims = new int[dims];
            for (int i = 0; i < dims; i++)
            {
                lims[i] = array.GetUpperBound(i);
                if (array.GetLowerBound(i) != 0 || lims[i] + 1 != array.GetLength(i))
                    throw new Exception();
            }
            bool done = false;
            while (! done)
            {
                var elem = array.GetValue(idxs);
                Console.Write(elem);
                char sep = ',';

                if (! (iterator.MoveNext() && iterator.Current.Equals(elem)))
                    throw new Exception();

                for (int i = dims - 1; ;)
                {
                    if (++idxs[i] <= lims[i])
                        break;
                    sep = '\n';
                    if (i == 0)
                    {
                        done = true;
                        break;
                    }
                    idxs[i--] = 0;
                }

                Console.Write(sep);
            }
        }



        void TestArrayCast()
        {
            var intArr = new int[3];
            var refArr = new RefX[3];
            var valArr = new ValX[3];
            Console.WriteLine("Array casts: "
                            + (((object) intArr) is object[])
                      + "," + (((object) intArr) is ValueType[])
                      + "," + (((object) refArr) is object[])
                      + "," + (((object) refArr) is ValueType[])
                      + "," + (((object) valArr) is object[])
                      + "," + (((object) valArr) is ValueType[]));
            try
            {
                var objArr = (IfcX[]) ((object) valArr);
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception " + e.GetType());
            }
        }

        void TestValue(bool selector)
        {
            ValX v1 = new ValX();
            RefX v2 = new RefX();
            Change(selector ? (IfcX) v1 : (IfcX) v2);

            void Change(IfcX x) => x.Change();
        }

        void TestGeneric2<T>(T[] chunk)
        {
            var chunks = new T[5][];
            chunks[2] = chunk;
            Console.WriteLine("Array from Generic Type = " + chunks.GetType());
        }

        void TestString(object s)
        {
            Console.WriteLine("Object = " + s.GetType());
            Console.WriteLine("String Castable to IEnumerable<char>? " + ((s as System.Collections.Generic.IEnumerable<char>) != null)
                                              + " IEnumerable<int>? " + ((s as System.Collections.Generic.IEnumerable<int>)  != null));
            Console.WriteLine("String Castable to IEnumerable? " + ((s as System.Collections.IEnumerable) != null)
                                              + " ICloneable? " + ((s as System.ICloneable) != null));
            Console.WriteLine("String Castable to IComparable<string>? " + ((s as System.IComparable<string>) != null)
                                              + " IEquatable<string>? " + ((s as System.IEquatable<string>) != null));
            /*Console.WriteLine( (s as System.Collections.IEnumerable).GetEnumerator());
            Console.WriteLine("String Castable to IComparable? " + ((s as System.IComparable) != null));
            Console.WriteLine("String Castable to IConvertible? " + ((s as System.IConvertible) != null));*/
        }

        void TestMisc()
        {
            var arr = new (string, int) [10];
            var (item0s, item0i) = arr[0];

            var list = new System.Collections.Generic.List<int[]>();
            var arr1 = new int[] { 1, 2, 3 };
            list.Add(arr1);
            var arr2 = list.ToArray();
            PrintArray(arr2[0]);
        }

    }
}
