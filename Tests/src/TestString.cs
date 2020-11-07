
using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestString : BaseTest
    {

        public override void TestMain()
        {
            TestSplit();
            TestIndexOf();
            TestCompare();
            TestCast("Test");
            TestEncoding();
        }



        void TestSplit()
        {
            // This example demonstrates the String() methods that use
            // the StringSplitOptions enumeration.
            string s1 = ",ONE,,TWO,,,THREE,,";
            string s2 = "[stop]" +
                        "ONE[stop][stop]" +
                        "TWO[stop][stop][stop]" +
                        "THREE[stop][stop]";
            char[] charSeparators = new char[] {','};
            string[] stringSeparators = new string[] {"[stop]"};
            string[] result;
            // ------------------------------------------------------------------------------
            // Split a string delimited by characters.
            // ------------------------------------------------------------------------------
            // Display the original string and delimiter characters.
            Console.WriteLine("String = \"{0}\"   Delimiter = '{1}'", s1, charSeparators[0]);

            // Split a string delimited by characters and return all elements.
            Console.Write("Split by char, omit=0: ");
            result = s1.Split(charSeparators, StringSplitOptions.None);
            Show(result);

            // Split a string delimited by characters and return all non-empty elements.
            Console.Write("Split by char, omit=1: ");
            result = s1.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
            Show(result);

            // Split the original string into the string and empty string before the
            // delimiter and the remainder of the original string after the delimiter.
            Console.Write("Split by char, omit=0, lim=2: ");
            result = s1.Split(charSeparators, 2, StringSplitOptions.None);
            Show(result);

            // Split the original string into the string after the delimiter and the
            // remainder of the original string after the delimiter.
            Console.Write("Split by char, omit=1, lim=2: ");
            result = s1.Split(charSeparators, 2, StringSplitOptions.RemoveEmptyEntries);
            Show(result);

            // ------------------------------------------------------------------------------
            // Split a string delimited by another string.
            // ------------------------------------------------------------------------------
            // Display the original string and delimiter string.
            Console.WriteLine("String = \"{0}\"   Delimiter = \"{1}\"", s2, stringSeparators[0]);

            // Split a string delimited by another string and return all elements.
            Console.Write("Split by str, omit=0: ");
            result = s2.Split(stringSeparators, StringSplitOptions.None);
            Show(result);

            // Split the original string at the delimiter and return all non-empty elements.
            Console.Write("Split by str, omit=1: ");
            result = s2.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            Show(result);

            // Split the original string into the empty string before the
            // delimiter and the remainder of the original string after the delimiter.
            Console.Write("Split by str, omit=0, lim=2: ");
            result = s2.Split(stringSeparators, 2, StringSplitOptions.None);
            Show(result);

            // Split the original string into the string after the delimiter and the
            // remainder of the original string after the delimiter.
            Console.Write("Split by str, omit=1, lim=2: ");
            result = s2.Split(stringSeparators, 2, StringSplitOptions.RemoveEmptyEntries);
            Show(result);

            // Display the array of separated strings using a local function
            void Show(string[] entries)
            {
                Console.Write("{0} results: ", entries.Length);
                foreach (string entry in entries)
                {
                    Console.Write("<{0}>", entry);
                }
                Console.WriteLine();
            }
        }



        static void TestIndexOf()
        {
            // based on examples in documentation page for
            // System.Globalization.CompareInfo.IndexOf methods

            CompareInfo myComp = CultureInfo.InvariantCulture.CompareInfo;
            int iS = 20;
            String myT1;
            String myStr = "Is AE or ae the same as Æ or æ?";
            myT1 = new String( '-', iS );
            //Console.WriteLine( "IndexOf( String, *, {0}, * )", iS );
            //Console.WriteLine( "Original      : {0}", myStr );
            //Console.WriteLine( "No options    : {0}{1}", myT1, myStr.Substring( iS ) );
            PrintMarker( "           AE : ", myComp.IndexOf( myStr, "AE", iS ), -1 );
            PrintMarker( "           ae : ", myComp.IndexOf( myStr, "ae", iS ), -1 );
            PrintMarker( "            Æ : ", myComp.IndexOf( myStr, 'Æ', iS ), -1 );
            PrintMarker( "            æ : ", myComp.IndexOf( myStr, 'æ', iS ), -1 );
            Console.WriteLine(/* "Ordinal       : {0}{1}", myT1, myStr.Substring( iS ) */);
            PrintMarker( "           AE : ", myComp.IndexOf( myStr, "AE", iS, CompareOptions.Ordinal ), -1 );
            PrintMarker( "           ae : ", myComp.IndexOf( myStr, "ae", iS, CompareOptions.Ordinal ), -1 );
            PrintMarker( "            Æ : ", myComp.IndexOf( myStr, 'Æ', iS, CompareOptions.Ordinal ), -1 );
            PrintMarker( "            æ : ", myComp.IndexOf( myStr, 'æ', iS, CompareOptions.Ordinal ), -1 );
            Console.WriteLine(/* "IgnoreCase    : {0}{1}", myT1, myStr.Substring( iS ) */);
            PrintMarker( "           AE : ", myComp.IndexOf( myStr, "AE", iS, CompareOptions.IgnoreCase ), -1 );
            PrintMarker( "           ae : ", myComp.IndexOf( myStr, "ae", iS, CompareOptions.IgnoreCase ), -1 );
            PrintMarker( "            Æ : ", myComp.IndexOf( myStr, 'Æ', iS, CompareOptions.IgnoreCase ), -1 );
            PrintMarker( "            æ : ", myComp.IndexOf( myStr, 'æ', iS, CompareOptions.IgnoreCase ), -1 );
            Console.WriteLine();

            myT1 = new String( '-', myStr.Length - iS - 1 );
            //Console.WriteLine( "LastIndexOf( String, *, {0}, * )", iS );
            //Console.WriteLine( "Original      : {0}", myStr );
            //Console.WriteLine( "No options    : {0}{1}", myStr.Substring( 0, iS + 1 ), myT1 );
            PrintMarker( "           AE : ", -1, myComp.LastIndexOf( myStr, "AE", iS ) );
            PrintMarker( "           ae : ", -1, myComp.LastIndexOf( myStr, "ae", iS ) );
            PrintMarker( "            Æ : ", -1, myComp.LastIndexOf( myStr, 'Æ', iS ) );
            PrintMarker( "            æ : ", -1, myComp.LastIndexOf( myStr, 'æ', iS ) );
            Console.WriteLine(/* "Ordinal       : {0}{1}", myStr.Substring( 0, iS + 1 ), myT1 */);
            PrintMarker( "           AE : ", -1, myComp.LastIndexOf( myStr, "AE", iS, CompareOptions.Ordinal ) );
            PrintMarker( "           ae : ", -1, myComp.LastIndexOf( myStr, "ae", iS, CompareOptions.Ordinal ) );
            PrintMarker( "            Æ : ", -1, myComp.LastIndexOf( myStr, 'Æ', iS, CompareOptions.Ordinal ) );
            PrintMarker( "            æ : ", -1, myComp.LastIndexOf( myStr, 'æ', iS, CompareOptions.Ordinal ) );
            Console.WriteLine(/* "IgnoreCase    : {0}{1}", myStr.Substring( 0, iS + 1 ), myT1 */);
            PrintMarker( "           AE : ", -1, myComp.LastIndexOf( myStr, "AE", iS, CompareOptions.IgnoreCase ) );
            PrintMarker( "           ae : ", -1, myComp.LastIndexOf( myStr, "ae", iS, CompareOptions.IgnoreCase ) );
            PrintMarker( "            Æ : ", -1, myComp.LastIndexOf( myStr, 'Æ', iS, CompareOptions.IgnoreCase ) );
            PrintMarker( "            æ : ", -1, myComp.LastIndexOf( myStr, 'æ', iS, CompareOptions.IgnoreCase ) );
            Console.WriteLine();

            // Searches for the combining character sequence Latin capital letter U with diaeresis or Latin small letter u with diaeresis.
            myStr = "Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?";

            myT1 = new String( '-', iS );
            //Console.WriteLine( "IndexOf( String, *, {0}, * )", iS );
            //Console.WriteLine( "Original      : {0}", myStr );
            //Console.WriteLine( "No options    : {0}{1}", myT1, myStr.Substring( iS ) );
            PrintMarker( "           U\u0308 : ", myComp.IndexOf( myStr, "U\u0308", iS ), -1 );
            PrintMarker( "           u\u0308 : ", myComp.IndexOf( myStr, "u\u0308", iS ), -1 );
            PrintMarker( "            Ü : ", myComp.IndexOf( myStr, 'Ü', iS ), -1 );
            PrintMarker( "            ü : ", myComp.IndexOf( myStr, 'ü', iS ), -1 );
            Console.WriteLine(/* "Ordinal       : {0}{1}", myT1, myStr.Substring( iS ) */);
            PrintMarker( "           U\u0308 : ", myComp.IndexOf( myStr, "U\u0308", iS, CompareOptions.Ordinal ), -1 );
            PrintMarker( "           u\u0308 : ", myComp.IndexOf( myStr, "u\u0308", iS, CompareOptions.Ordinal ), -1 );
            PrintMarker( "            Ü : ", myComp.IndexOf( myStr, 'Ü', iS, CompareOptions.Ordinal ), -1 );
            PrintMarker( "            ü : ", myComp.IndexOf( myStr, 'ü', iS, CompareOptions.Ordinal ), -1 );
            Console.WriteLine(/* "IgnoreCase    : {0}{1}", myT1, myStr.Substring( iS ) */);
            PrintMarker( "           U\u0308 : ", myComp.IndexOf( myStr, "U\u0308", iS, CompareOptions.IgnoreCase ), -1 );
            PrintMarker( "           u\u0308 : ", myComp.IndexOf( myStr, "u\u0308", iS, CompareOptions.IgnoreCase ), -1 );
            PrintMarker( "            Ü : ", myComp.IndexOf( myStr, 'Ü', iS, CompareOptions.IgnoreCase ), -1 );
            PrintMarker( "            ü : ", myComp.IndexOf( myStr, 'ü', iS, CompareOptions.IgnoreCase ), -1 );
            Console.WriteLine();

            myT1 = new String( '-', myStr.Length - iS - 1 );
            //Console.WriteLine( "LastIndexOf( String, *, {0}, * )", iS );
            //Console.WriteLine( "Original      : {0}", myStr );
            //Console.WriteLine( "No options    : {0}{1}", myStr.Substring( 0, iS + 1 ), myT1 );
            PrintMarker( "           U\u0308 : ", -1, myComp.LastIndexOf( myStr, "U\u0308", iS ) );
            PrintMarker( "           u\u0308 : ", -1, myComp.LastIndexOf( myStr, "u\u0308", iS ) );
            PrintMarker( "            Ü : ", -1, myComp.LastIndexOf( myStr, 'Ü', iS ) );
            PrintMarker( "            ü : ", -1, myComp.LastIndexOf( myStr, 'ü', iS ) );
            Console.WriteLine(/* "Ordinal       : {0}{1}", myStr.Substring( 0, iS + 1 ), myT1 */);
            PrintMarker( "           U\u0308 : ", -1, myComp.LastIndexOf( myStr, "U\u0308", iS, CompareOptions.Ordinal ) );
            PrintMarker( "           u\u0308 : ", -1, myComp.LastIndexOf( myStr, "u\u0308", iS, CompareOptions.Ordinal ) );
            PrintMarker( "            Ü : ", -1, myComp.LastIndexOf( myStr, 'Ü', iS, CompareOptions.Ordinal ) );
            PrintMarker( "            ü : ", -1, myComp.LastIndexOf( myStr, 'ü', iS, CompareOptions.Ordinal ) );
            Console.WriteLine(/* "IgnoreCase    : {0}{1}", myStr.Substring( 0, iS + 1 ), myT1 */);
            PrintMarker( "           U\u0308 : ", -1, myComp.LastIndexOf( myStr, "U\u0308", iS, CompareOptions.IgnoreCase ) );
            PrintMarker( "           u\u0308 : ", -1, myComp.LastIndexOf( myStr, "u\u0308", iS, CompareOptions.IgnoreCase ) );
            PrintMarker( "            Ü : ", -1, myComp.LastIndexOf( myStr, 'Ü', iS, CompareOptions.IgnoreCase ) );
            PrintMarker( "            ü : ", -1, myComp.LastIndexOf( myStr, 'ü', iS, CompareOptions.IgnoreCase ) );
            Console.WriteLine();

            static void PrintMarker( String Prefix, int First, int Last )  {
                if (First > -1) Console.Write($"\t\tFirst({First})");
                if (Last > -1)  Console.Write($"\t\tLast({First})");
                if (First < 0 && Last < 0)
                                Console.Write($"\t\tNone\t");
            }
        }



        void TestCompare()
        {
            // Defines the strings to compare.
            String myStr1 = "My Uncle Bill's clients";
            String myStr2 = "My uncle bill's clients";

            // Creates a CompareInfo that uses the InvariantCulture.
            CompareInfo myComp = CultureInfo.InvariantCulture.CompareInfo;

            // Compares two strings using myComp.
            Console.WriteLine( "Comparing \"{0}\" and \"{1}\"", myStr1, myStr2 );
            Console.WriteLine( "   With no CompareOptions            : {0}", myComp.Compare( myStr1, myStr2 ) );
            Console.WriteLine( "   With None                         : {0}", myComp.Compare( myStr1, myStr2, CompareOptions.None ) );
            Console.WriteLine( "   With Ordinal                      : {0}", myComp.Compare( myStr1, myStr2, CompareOptions.Ordinal ) );
            //Console.WriteLine( "   With StringSort                   : {0}", myComp.Compare( myStr1, myStr2, CompareOptions.StringSort ) );
            Console.WriteLine( "   With IgnoreCase                   : {0}", myComp.Compare( myStr1, myStr2, CompareOptions.IgnoreCase ) );
            //Console.WriteLine( "   With IgnoreSymbols                : {0}", myComp.Compare( myStr1, myStr2, CompareOptions.IgnoreSymbols ) );
            //Console.WriteLine( "   With IgnoreCase and IgnoreSymbols : {0}", myComp.Compare( myStr1, myStr2, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols ) );
        }



        void TestCast(object str)
        {
            Console.WriteLine("Convertible? " + ((str as IConvertible) != null)
                            + " Comparable? " + ((str as IComparable) != null));
        }



        void TestEncoding()
        {
            string aString = "0123456789";
            foreach (var c in aString) Console.Write($"{c}={((int)c):X2} ");
            var aBytes = System.Text.Encoding.ASCII.GetBytes(aString);
            foreach (var b in aBytes) Console.Write($"  {((int)b):X2} ");
            string bString = System.Text.Encoding.ASCII.GetString(aBytes);
            Console.WriteLine(string.CompareOrdinal(aString, bString));
        }

    }
}
