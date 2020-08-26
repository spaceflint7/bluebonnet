
namespace system
{

    public class Console
    {

        //
        // TextReader In, TextWriter Out, TextWriter Error
        //

        private static object s_InternalSyncObject;

        private static System.IO.TextReader inputReader;
        private static System.IO.TextWriter outputWriter;
        private static System.IO.TextWriter errorWriter;

        public static System.IO.TextReader In
        {
            get
            {
                bool initialized = false;
                return System.Threading.LazyInitializer.EnsureInitialized<System.IO.TextReader>(
                    ref inputReader, ref initialized, ref s_InternalSyncObject, () =>
                    {
                        var jstream = new java.io.FileInputStream(java.io.FileDescriptor.@in);
                        var nstream = new system.io.FileStream(
                                jstream.getChannel(), system.io.FileStream.CAN_READ);
                        var encoding = System.Text.Encoding.Default;
                        var reader = new System.IO.StreamReader(nstream, encoding, false, 256, true);
                        return System.IO.TextReader.Synchronized(reader);
                    });
            }
        }



        /*
        this is commented out due to encoding issues;  see also system.io.ConsoleWriter
        public static System.IO.TextWriter GetTextWriter(ref System.IO.TextWriter theWriter,
                                                         java.io.FileDescriptor javafd)
        {
            bool initialized = false;
            return System.Threading.LazyInitializer.EnsureInitialized<System.IO.TextWriter>(
                ref theWriter, ref initialized, ref s_InternalSyncObject, () =>
                {
                    var jstream = new java.io.FileOutputStream(javafd);
                    var nstream = new system.io.FileStream(
                            jstream.getChannel(), system.io.FileStream.CAN_WRITE);
                    var encoding = System.Text.Encoding.Default;
                    var writer = new System.IO.StreamWriter(nstream, encoding, 256, true);
                    ((system.io.StreamWriter) (object) writer).HaveWrittenPreamble = true;
                    writer.AutoFlush = true;
                    return System.IO.TextWriter.Synchronized(writer);
                });
        }
        public static System.IO.TextWriter Out
            => GetTextWriter(ref outputWriter, java.io.FileDescriptor.@out);
        public static System.IO.TextWriter Error
            => GetTextWriter(ref errorWriter, java.io.FileDescriptor.@err);
        */



        public static System.IO.TextWriter GetTextWriter(ref System.IO.TextWriter theWriter,
                                                         java.io.PrintStream javastream)
        {
            bool initialized = false;
            return System.Threading.LazyInitializer.EnsureInitialized<System.IO.TextWriter>(
                ref theWriter, ref initialized, ref s_InternalSyncObject, () =>
                {
                    return System.IO.TextWriter.Synchronized(new ConsoleWriter(javastream));
                });
        }

        public static System.IO.TextWriter Out
            => GetTextWriter(ref outputWriter, java.lang.System.@out /*java.io.FileDescriptor.@out*/);

        public static System.IO.TextWriter Error
            => GetTextWriter(ref errorWriter, java.lang.System.@err /*java.io.FileDescriptor.@err*/);

        public static bool IsInputRedirected => true;
        public static bool IsOutputRedirected => true;
        public static bool IsErrorRedirected => true;

        public static System.Text.Encoding InputEncoding => System.Text.Encoding.Default;
        public static System.Text.Encoding OutputEncoding => System.Text.Encoding.Default;

        //
        // No-op methods
        //

        //public static void Beep() { }
        //public static void Beep(int frequency, int duration) { }
        //public static void Clear() { }

        //
        // Read and ReadLine
        //

        public static int Read() => In.Read();

        public static string ReadLine() => In.ReadLine();

        //
        // Write
        //

        public static void Write(object value) => Out.Write(value);
        public static void Write(string value) => Out.Write(value);
        public static void Write(bool value) => Out.Write(value);
        public static void Write(char value) => Out.Write(value);
        public static void Write(int value) => Out.Write(value);
        public static void Write(uint value) => Out.Write(value);
        public static void Write(long value) => Out.Write(value);
        public static void Write(ulong value) => Out.Write(value);
        public static void Write(float value) => Out.Write(value);
        public static void Write(double value) => Out.Write(value);
        public static void Write(decimal value) => Out.Write(value);
        public static void Write(char[] buffer) => Out.Write(buffer);
        public static void Write(char[] buffer, int index, int count) => Out.Write(buffer, index, count);
        public static void Write(string format, object arg0) => Out.Write(format, arg0);
        public static void Write(string format, object arg0, object arg1) => Out.Write(format, arg0, arg1);
        public static void Write(string format, object arg0, object arg1, object arg2) => Out.Write(format, arg0, arg1, arg2);
        public static void Write(string format, object arg0, object arg1, object arg2, object arg3) => Out.Write(format, arg0, arg1, arg2, arg3);
        public static void Write(string format, params object[] arg)
        {
            if (arg == null)
                Out.Write(format, null, null);
            else
                Out.Write(format, arg);
        }

        //
        // WriteLine
        //

        public static void WriteLine() => Out.WriteLine();
        public static void WriteLine(object value) => Out.WriteLine(value);
        public static void WriteLine(string value) => Out.WriteLine(value);
        public static void WriteLine(bool value) => Out.WriteLine(value);
        public static void WriteLine(char value) => Out.WriteLine(value);
        public static void WriteLine(int value) => Out.WriteLine(value);
        public static void WriteLine(uint value) => Out.WriteLine(value);
        public static void WriteLine(long value) => Out.WriteLine(value);
        public static void WriteLine(ulong value) => Out.WriteLine(value);
        public static void WriteLine(float value) => Out.WriteLine(value);
        public static void WriteLine(double value) => Out.WriteLine(value);
        public static void WriteLine(decimal value) => Out.WriteLine(value);
        public static void WriteLine(char[] buffer) => Out.WriteLine(buffer);
        public static void WriteLine(char[] buffer, int index, int count) => Out.WriteLine(buffer, index, count);
        public static void WriteLine(string format, object arg0) => Out.WriteLine(format, arg0);
        public static void WriteLine(string format, object arg0, object arg1) => Out.WriteLine(format, arg0, arg1);
        public static void WriteLine(string format, object arg0, object arg1, object arg2) => Out.WriteLine(format, arg0, arg1, arg2);
        public static void WriteLine(string format, object arg0, object arg1, object arg2, object arg3) => Out.WriteLine(format, arg0, arg1, arg2, arg3);
        public static void WriteLine(string format, params object[] arg)
        {
            if (arg == null)
                Out.WriteLine(format, null, null);
            else
                Out.WriteLine(format, arg);
        }

        /*public static void WriteLine(string format, object arg0)
            => java.lang.System.@out.println(System.String.Format(format, arg0));

        public static void WriteLine(string format, object arg0, object arg1)
            => java.lang.System.@out.println(System.String.Format(format, arg0, arg1));*/

        //
        // Console application runner
        //

        public static void main(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0)
                    throw new System.ArgumentException("must specify main class name");

                var cls = java.lang.Class.forName(args[0]);
                if (cls == null)
                    throw new System.ArgumentException("main class not found");

                #pragma warning disable 0436
                java.lang.reflect.Method meth = null;
                var parameters = new java.lang.Class[1] { (java.lang.Class) typeof(string[]) };
                try
                {
                    meth = (java.lang.reflect.Method) (object)
                                        cls.getDeclaredMethod("Main", parameters);
                }
                catch (java.lang.NoSuchMethodException)
                {
                    try
                    {
                        meth = (java.lang.reflect.Method) (object)
                                        cls.getDeclaredMethod("main", parameters);
                    }
                    catch (java.lang.NoSuchMethodException e)
                    {
                        throw new System.ArgumentException("main method not found", e);
                    }
                }
                #pragma warning restore 0436

                var n = args.Length;
                var newArgs = new string[n - 1];
                for (int i = 1; i < n; i++)
                    newArgs[i - 1] = args[i];

                object result = null;
                try
                {
                    result = meth.invoke(null, new object[] { newArgs });
                }
                catch (java.lang.IllegalAccessException)
                {
                    meth.setAccessible(true);
                    result = meth.invoke(null, new object[] { newArgs });
                }

                if (result is java.lang.Integer intResult)
                {
                    var exitCode = intResult.intValue();
                    if (exitCode != 0)
                        java.lang.System.exit(exitCode);
                }
            }
            catch (System.Exception exception)
            {
                var exc = exception;
                for (;;)
                {
                    Console.WriteLine("Exception " + ((java.lang.Object) (object) exc).getClass()
                                    + "\nMessage: " + exc.Message //((java.lang.Throwable) exc).getMessage()
                                    + "\n" + exc.StackTrace);
                    if ((exc = exc.InnerException) == null)
                        break;
                    Console.Write("Caused by Inner ");
                }
                java.lang.System.exit(-1);
            }
        }
    }

    //
    // java does not provide a reliable way to detect the encoding of the
    // console on Windows.  e.g. in a console configured with cp437,
    // java.nio.charset.Charset.defaultCharset returns "windows-1252",
    // which is wrong.  however, java.lang.System.@out will actually be
    // configured for cp437.
    //
    // this means that having System.Console create a TextWriter with the
    // java-provided encoding will produce wrong results.  so instead, we
    // just use this basic TextWriter implementation.
    //

    class ConsoleWriter : System.IO.TextWriter
    {
        [java.attr.RetainType] public java.io.PrintStream stream;

        public ConsoleWriter(java.io.PrintStream _stream)
        {
            stream = _stream;
        }

        public override System.Text.Encoding Encoding => System.Text.Encoding.Default;

        public override void Write(char ch)
        {
            stream.print(ch);
            stream.flush();
        }

        public override void WriteLine()
        {
            stream.println();
            stream.flush();
        }
    }

}

/*namespace system.io
{
    [java.attr.Discard] // discard in output
    public abstract class StreamWriter
    {
        public bool HaveWrittenPreamble { get; set; }
    }
}*/
