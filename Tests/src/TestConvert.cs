
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class TestConvert : BaseTest
    {

        private const System.Byte m_byteValue = (System.Byte) 255;
        private const System.SByte m_sbyteValue = (System.SByte) (-1);

        private const System.Int32 m_int32Value = (System.Int32) (-1);
        private const System.UInt32 m_uint32Value = unchecked ( (System.UInt32) (-1) );

        public override void TestMain()
        {
            TestDivide();
            TestOverflow();
            TestDecimal();
            //TestByte();
            //TestFloat();
            //TestDouble();
        }

        void TestDivide()
        {
            ulong dividend64 = (ulong) 0xFFFFFFFFFFFFFFFF;
            var div64 = (int)(uint)(dividend64 / 10000);
            var rem64 = (int)(uint)(dividend64 % 10000);

            uint dividend32 = (uint) 0xFFFFFFFF;
            var div32 = (int)(uint)(dividend32 / 10000);
            var rem32 = (int)(uint)(dividend32 % 10000);

            Console.WriteLine("DIVIDE    UNSIGNED " + div32 + " , " + div64
                           + " REMAINDER UNSIGNED " + rem32 + " , " + rem64);
        }

        void TestOverflow()
        {
            TestOverflowAdd();
            TestOverflowSub();
            TestOverflowMul();

            void TestOverflowAdd()
            {
                try {
                    Console.Write("Int32 Add: ");
                    int a = Int16.MaxValue;
                    int b = a;
                    int c = checked (a + b);
                    Console.Write("OK " + c);
                    a = Int32.MaxValue - 1;
                    b = Int32.MaxValue - 2;
                    c = checked (a + b);
                    Console.WriteLine("OK " + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }

                try {
                    Console.Write("UInt32 Add: ");
                    uint a = UInt16.MaxValue;
                    uint b = a;
                    uint c = checked (a + b);
                    Console.Write("OK " + c);
                    a = UInt32.MaxValue - 1;
                    b = UInt32.MaxValue - 2;
                    c = checked (a + b);
                    Console.WriteLine("OK " + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }

                try {
                    Console.Write("Int64 Add: ");
                    long a = Int32.MaxValue - 1;
                    long b = Int32.MaxValue - 2;
                    long c = checked (a + b);
                    Console.Write("OK " + c);
                    a = Int64.MaxValue - 1;
                    b = Int64.MaxValue - 2;
                    c = checked (a + b);
                    Console.WriteLine("OK" + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }

                try {
                    Console.Write("UInt64 Add: ");
                    ulong a = Int32.MaxValue - 1;
                    ulong b = Int32.MaxValue - 2;
                    ulong c = checked (a + b);
                    Console.Write("OK " + c);
                    a = UInt64.MaxValue - 1;
                    b = UInt64.MaxValue - 2;
                    c = checked (a + b);
                    Console.WriteLine("OK" + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }
            }

            void TestOverflowSub()
            {
                try {
                    Console.Write("Int32 Sub: ");
                    int a = 0;
                    int b = Int16.MaxValue;
                    int c = checked (a - b);
                    Console.Write("OK " + c);
                    a = Int32.MinValue;
                    b = Int32.MaxValue;
                    c = checked (a - b);
                    Console.WriteLine("OK " + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }

                try {
                    Console.Write("UInt32 Sub: ");
                    uint a = 2;
                    uint b = 1;
                    uint c = checked (a - b);
                    Console.Write("OK " + c);
                    a = 0;
                    b = 1;
                    c = checked (a - b);
                    Console.WriteLine("OK" + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }

                try {
                    Console.Write("Int64 Sub: ");
                    long a = 0;
                    long b = Int32.MaxValue;
                    long c = checked (a - b);
                    Console.Write("OK " + c);
                    a = Int64.MinValue;
                    b = Int64.MaxValue;
                    c = checked (a - b);
                    Console.WriteLine("OK " + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }

                try {
                    Console.Write("UInt64 Sub: ");
                    ulong a = 2;
                    ulong b = 1;
                    ulong c = checked (a - b);
                    Console.Write("OK " + c);
                    a = 0;
                    b = 1;
                    c = checked (a - b);
                    Console.WriteLine("OK " + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }
            }

            void TestOverflowMul()
            {
                try {
                    Console.Write("Int32 Mul: ");
                    int a = Int16.MaxValue;
                    int b = a;
                    int c = checked (a * b);
                    Console.Write("OK " + c);
                    a = Int32.MaxValue - 1;
                    b = Int32.MaxValue - 2;
                    c = checked (a * b);
                    Console.WriteLine("OK " + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }

                try {
                    Console.Write("UInt32 Mul: ");
                    uint a = UInt16.MaxValue;
                    uint b = a / 2;
                    uint c = checked (a * b);
                    Console.Write("OK " + c);
                    a = UInt32.MaxValue - 1;
                    b = UInt32.MaxValue - 2;
                    c = checked (a * b);
                    Console.WriteLine("OK " + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }

                try {
                    Console.Write("Int64 Mul: ");
                    long a = Int32.MaxValue - 1;
                    long b = Int32.MaxValue - 2;
                    long c = checked (a * b);
                    Console.Write("OK " + c);
                    a = Int64.MaxValue - 1;
                    b = Int64.MaxValue - 2;
                    c = checked (a * b);
                    Console.WriteLine("OK" + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }

                try {
                    Console.Write("UInt64 Mul: ");
                    ulong a = Int32.MaxValue - 1;
                    ulong b = Int32.MaxValue - 2;
                    ulong c = checked (a * b);
                    Console.Write("OK " + c);
                    a = UInt64.MaxValue - 1;
                    b = UInt64.MaxValue - 2;
                    c = checked (a * b);
                    Console.WriteLine("OK" + c);
                } catch (Exception e) { Console.WriteLine(" Caught " + e.GetType()); }
            }
        }

        void TestByte()
        {
            byte b = 255;
            Console.Write(b); Console.Write("\t"); Console.WriteLine(m_byteValue);
            Console.Write(m_int32Value); Console.Write("\t"); Console.WriteLine(m_uint32Value);
        }

        static void TestFloat()
        {
            float r;

            Console.WriteLine("FLOAT");

            Console.WriteLine("U8");
            byte   u8  = byte.MaxValue;   r = u8;   Console.WriteLine(r);
                   u8  = byte.MinValue;   r = u8;   Console.WriteLine(r);

            Console.WriteLine("S8");
            sbyte  s8  = sbyte.MaxValue;  r = s8;   Console.WriteLine(r);
                   s8  = sbyte.MinValue;  r = s8;   Console.WriteLine(r);

            Console.WriteLine("U16");
            ushort u16 = ushort.MaxValue; r = u16;  Console.WriteLine(r);
                   u16 = ushort.MinValue; r = u16;  Console.WriteLine(r);

            Console.WriteLine("S16");
            short  s16 = short.MaxValue;  r = s16;  Console.WriteLine(r);
                   s16 = short.MinValue;  r = s16;  Console.WriteLine(r);

            Console.WriteLine("U32");
            uint   u32 = uint.MaxValue;   r = u32;  Console.WriteLine(r);
                   u32 = uint.MinValue;   r = u32;  Console.WriteLine(r);

            Console.WriteLine("S32");
            int    s32 = int.MaxValue;    r = s32;  Console.WriteLine(r);
                   s32 = int.MinValue;    r = s32;  Console.WriteLine(r);

            Console.WriteLine("U64");
            ulong  u64 = ulong.MaxValue;  r = u64;  Console.WriteLine(r);
                   u64 = ulong.MinValue;  r = u64;  Console.WriteLine(r);

            Console.WriteLine("S64");
            long   s64 = long.MaxValue;   r = s64;  Console.WriteLine(r);
                   s64 = long.MinValue;   r = s64;  Console.WriteLine(r);

            Console.WriteLine("F32");
            float  f32 = float.MaxValue;  r = f32;  Console.WriteLine(r);
                   f32 = float.MinValue;  r = f32;  Console.WriteLine(r);

            Console.WriteLine("D64");
            double d64 = double.MaxValue; r = (float) d64;  Console.WriteLine(r);
                   d64 = double.MinValue; r = (float) d64;  Console.WriteLine(r);
        }

        static void TestDouble()
        {
            double r;

            Console.WriteLine("DOUBLE");

            Console.WriteLine("U8");
            byte   u8  = byte.MaxValue;   r = u8;  Console.WriteLine(r);
                   u8  = byte.MinValue;   r = u8;  Console.WriteLine(r);

            Console.WriteLine("S8");
            sbyte  s8  = sbyte.MaxValue;  r = s8;  Console.WriteLine(r);
                   s8  = sbyte.MinValue;  r = s8;  Console.WriteLine(r);

            Console.WriteLine("U16");
            ushort u16 = ushort.MaxValue; r = u16; Console.WriteLine(r);
                   u16 = ushort.MinValue; r = u16; Console.WriteLine(r);

            Console.WriteLine("S16");
            short  s16 = short.MaxValue;  r = s16; Console.WriteLine(r);
                   s16 = short.MinValue;  r = s16; Console.WriteLine(r);

            Console.WriteLine("U32");
            uint   u32 = uint.MaxValue;   r = u32; Console.WriteLine(r);
                   u32 = uint.MinValue;   r = u32; Console.WriteLine(r);

            Console.WriteLine("S32");
            int    s32 = int.MaxValue;    r = s32; Console.WriteLine(r);
                   s32 = int.MinValue;    r = s32; Console.WriteLine(r);

            Console.WriteLine("U64");
            ulong  u64 = ulong.MaxValue;  r = u64; Console.WriteLine(r);
                   u64 = ulong.MinValue;  r = u64; Console.WriteLine(r);

            Console.WriteLine("S64");
            long   s64 = long.MaxValue;   r = s64; Console.WriteLine(r);
                   s64 = long.MinValue;   r = s64; Console.WriteLine(r);

            Console.WriteLine("F32");
            float  f32 = float.MaxValue;  r = f32; Console.WriteLine(r);
                   f32 = float.MinValue;  r = f32; Console.WriteLine(r);

            Console.WriteLine("D64");
            double d64 = double.MaxValue; r = d64; Console.WriteLine(r);
                   d64 = double.MinValue; r = d64; Console.WriteLine(r);
        }



        void TestDecimal()
        {
            Dump(new Decimal(1234.5678));
            Dump(new Decimal(-1234.5678));
            Dump(new Decimal(0x1234567812345678));
            Dump(new Decimal(-0x1234567812345678));
            Dump(new Decimal((uint) 0xFFFFFFFF));
            Dump(new Decimal((ulong) 0xFFFFFFFFFFFFFFFF));

            void Dump(decimal v)
            {
                var bytes = Decimal.GetBits(v);
                Console.WriteLine("Decimal " + v + " lo " + bytes[0] + " mid " + bytes[1] + " hi " + bytes[2] + " flags " + bytes[3] + " hash " + v.GetHashCode());
            }
        }

    }
}
