
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public sealed class JavaException : Exception
    {

        public JavaException(string reason, Where where) : base(reason + where) {}

        public class Where : Stack<string>
        {
            public override string ToString()
            {
                string s = string.Empty;
                foreach (var w in this)
                    s += " in " + w;
                return s;
            }
            public JavaException Exception(string reason) => new JavaException(reason, this);
            /*public new Where Push(string s)
            {
                base.Push(s);
                return this;
            }*/
            /*public new void Push(string s)
            {
                Console.WriteLine("ENTERING " + s);
                base.Push(s);
            }
            public new string Pop()
            {
                var s = base.Pop();
                Console.WriteLine("LEAVING " + s);
                return s;
            }*/
        }

    }

}
