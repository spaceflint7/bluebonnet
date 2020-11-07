
namespace system
{

    [System.Serializable]
    public class Exception : java.lang.Exception, System.Runtime.Serialization.ISerializable
    {

        [java.attr.RetainType] private bool fillInStackTraceWasIgnoredOnce;
        [java.attr.RetainType] private int _HResult = unchecked((int)0x80131500); // COR_E_EXCEPTION

        // some derived exceptions (e.g. TypeLoadException) reference this field directly
        public string _message;


        public Exception() : base()
        {
        }



        public Exception(string message) : base(message)
        {
            _message = message;
        }



        public Exception(string message, java.lang.Throwable innerException)
            : base(message, innerException)
        {
            _message = message;
        }



        protected Exception(System.Runtime.Serialization.SerializationInfo info,
                            System.Runtime.Serialization.StreamingContext context)
        {
            throw new System.NotImplementedException();
        }



        //
        // there is a circular dependency where the java.lang.Throwable imported by
        // DotNetImporter.cs is made a subtype of System.Exception, and here we have
        // system.Exception as a subtype of java.lang.Exception.  we have to decorate
        // some methods with [java.attr.RetainName] to prevent method renaming due to
        // shadowing, as done by CilMethod.cs MethodIsShadowing().
        //



        //new public virtual System.Collections.IDictionary Data { [java.attr.RetainName] get; }
        new public virtual string HelpLink { [java.attr.RetainName] get; [java.attr.RetainName] set; }
        new protected int HResult
        {
            [java.attr.RetainName] get => _HResult;
            [java.attr.RetainName] set => _HResult = value;
        }
        new public System.Exception InnerException { [java.attr.RetainName] get => base.getCause(); }



        new public virtual string Message
        {
            [java.attr.RetainName] get
            {
                if (_message != null)
                    return _message;
                // xxx translate to GetResourceString ?
                return base.getMessage() ??
                    ("Exception of type '" + GetClassName() + "' was thrown.");
            }
        }



        new public virtual string Source { [java.attr.RetainName] get; [java.attr.RetainName] set; }



        new public virtual string StackTrace
        {
            [java.attr.RetainName] get => get_StackTrace(this);
        }



        //public System.Reflection.MethodBase TargetSite { get; }



        public static string get_HelpLink(java.lang.Throwable exc)
        {
            if (exc is system.Exception clrExc)
                return clrExc.HelpLink;
            else
                return "";
        }



        public static int get_HResult(java.lang.Throwable exc)
        {
            if (exc is system.Exception clrExc)
                return clrExc.HResult;
            else
                return unchecked((int)0x80131500); // COR_E_EXCEPTION
        }



        public static System.Exception get_InnerException(java.lang.Throwable exc)
        {
            if (exc is system.Exception clrExc)
                return clrExc.InnerException;
            else
                return exc.getCause();
        }



        public static string get_Message(java.lang.Throwable exc)
        {
            if (exc is system.Exception clrExc)
                return clrExc.Message;
            else
                return exc.getMessage();
        }



        public static string get_Source(java.lang.Throwable exc)
        {
            if (exc is system.Exception clrExc)
                return clrExc.Source;
            else
                return "";
        }



        public static string get_StackTrace(java.lang.Throwable exc)
        {
            var elems = exc.getStackTrace();
            int n = elems?.Length ?? 0;
            if (n == 0)
                return null;
            var sb = new java.lang.StringBuilder();
            for (int i = 0; i < n; i++)
            {
                var elem = elems[i];
                if (i > 0)
                    sb.append("\n");
                sb.append("   at ");
                sb.append(elem.getClassName());
                sb.append(".");
                sb.append(elem.getMethodName());
                sb.append("() in ");
                sb.append(elem.getFileName());
                sb.append(":line ");
                sb.append(elem.getLineNumber());
            }
            return sb.ToString();
        }



        /*internal static void SetErrorCode(java.lang.Throwable exc, int hr)
        {
            if (exc is system.Exception clrExc)
                clrExc.HResult = hr;
        }*/
        [java.attr.RetainName] internal void SetErrorCode(int hr) => HResult = hr;



        public override java.lang.Throwable fillInStackTrace()
        {
            //
            // java collects the stack trace when the exception is allocated,
            // from the java.lang.Throwable constructor.  but cil collects the
            // stack trace only when the exception is thrown.
            //
            // our cil exceptions inherit from Throwable, so we need to ignore
            // the initial call to fillInStackTrace.  our implementation of the
            // cil 'throw' instruction calls fillInStackTrace.
            //

            if (fillInStackTraceWasIgnoredOnce)
                return base.fillInStackTrace();

            fillInStackTraceWasIgnoredOnce = true;
            return this;
        }



        public override string ToString()
        {
            string s = GetClassName() + ": " + this.Message;
            string t = this.StackTrace;
            if (t != null)
                s += "\n" + t;
            if (! (((object) this) is System.AggregateException))
            {
                var e = InnerException;
                if (! object.ReferenceEquals(e, null))
                    s += "\nInner exception: " + e;
            }
            return s;
        }



        public static string ToString(java.lang.Throwable exc)
        {
            if (exc is system.Exception clrExc)
                return clrExc.ToString();

            string s = ((java.lang.Object) (object) exc).getClass() + ": " + exc.getMessage();
            string t = get_StackTrace(exc);
            if (t != null)
                s += "\n" + t;

            var innerException = exc.getCause();
            if (! object.ReferenceEquals(innerException, null))
                s += "\nInner exception: " + innerException;

            return s;
        }



        [java.attr.RetainName]
        private string GetClassName() => ((java.lang.Object) (object) this).GetType().FullName;



        //
        // System.Exception includes the following method, to prevent compiler
        // from making System.Object::GetType() virtual.  we need the same
        // workaround, because exception objects have a circular chain of inheritance:
        //
        //      SomeException -> system.Exception -> java.lang.Exception ...
        // ... -> java.lang.Throwable -> System.Exception -> System.Object
        //
        // System.Exception references the System.Runtime.InteropServices::_Exception
        // interface, and our InterfaceBuilder::CollectMethods sees the method
        // System.Exception::GetType() as an appropriate implementation.  so we are
        // required to actually provide such a method as system.Exception::GetType().
        //

        new public System.Type GetType() => ((java.lang.Object) (object) this).GetType();

    }



    /*
     *
     * Util Helpers
     *
     */



    public static partial class Util
    {

        [java.attr.RetainType] private static java.util.concurrent.ConcurrentHashMap exceptionMap =
            new java.util.concurrent.ConcurrentHashMap();

        internal delegate System.Exception ExceptionTranslator(java.lang.Throwable exc);



        internal static void DefineException(java.lang.Class type, ExceptionTranslator dlg)
        {
            exceptionMap.put(type, dlg);
        }



        public static java.lang.Throwable TranslateException(java.lang.Throwable exc)
        {
            System.Exception newExc = null;

            if (exc is system.Exception)
                return exc;

            var dlg = (ExceptionTranslator) exceptionMap.get(
                                                ((java.lang.Object) (object) exc).getClass());
            if (dlg != null)
                newExc = dlg(exc);

            if (newExc == null)
            {
                if (exc is java.lang.NullPointerException)
                    newExc = new System.NullReferenceException();

                /*if (exc is java.lang.IllegalArgumentException)
                    newExc = new System.ArgumentException();*/

                if (exc is java.lang.ArithmeticException && exc.getMessage() == "/ by zero")
                    newExc = new System.DivideByZeroException(); // "Attempted to divide by zero."

                    // also: IndexOutOfRangeException.  define in Array?  not necessarily.
                    // fixme:  move these to specialized exceptionMap ?

                if (newExc == null)
                    return exc;

                /*if (newExc == null)
                {
                    // any throwable not deriving from system.Exception is assumed
                    // to be coming from the JVM or java code.  If it was not mapped
                    // to some known .NET exception, wrap it in an ExternalException
                    // newExc = new Exception("Java exception: " + exc.GetType());
                    // fixme System.Runtime.InteropServices.ExternalException ???
                    Console.WriteLine("Created New Exception: ");
                    Console.WriteLine(newExc);
                    Console.WriteLine("Continuing...");
                }*/
            }

            java.lang.Throwable newThrowable = (java.lang.Throwable) newExc;
            newThrowable.setStackTrace(exc.getStackTrace());
            return newThrowable;
        }

    }



    public class TryCatchLeaveTarget : java.lang.VirtualMachineError
    {
        // this fake-exception is used in the implementation of
        // 'leave' instruction, when there is also a 'finally' clause.
        // see also:  Translate_Leave in CodeExcept.

        private int Target;
        public override java.lang.Throwable fillInStackTrace() => this;

        private TryCatchLeaveTarget(int _target) { Target = _target; }
        public static java.lang.Throwable New(int _target) => new TryCatchLeaveTarget(_target);
        public static int Get(java.lang.Throwable t) => t is TryCatchLeaveTarget t1 ? t1.Target : -1;
    }

}
