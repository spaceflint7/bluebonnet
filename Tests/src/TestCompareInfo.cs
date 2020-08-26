
using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestCompareInfo : BaseTest
    {

        public override void TestMain()
        {
            TestIndexOf1();
        }

        //
        //
        //

        static void TestIndexOf1()
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
        }

        public static void PrintMarker( String Prefix, int First, int Last )  {
            if (First > -1) Console.Write($"\t\tFirst({First})");
            if (Last > -1)  Console.Write($"\t\tLast({First})");
            if (First < 0 && Last < 0)
                            Console.Write($"\t\tNone\t");
        }
    }
}

