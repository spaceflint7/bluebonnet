
using System;
using System.IO;
using System.Text;

namespace SpaceFlint.JavaBinary
{
    public class IndentedText
    {

        StringBuilder sb;
        int indent;
        bool sol;


        public IndentedText()
        {
            sb = new StringBuilder();
            indent = 0;
            sol = true;
        }



        public void AdjustIndent(int delta)
        {
            indent += delta;
            if (indent < 0)
                indent = 0;
        }



        public void AdjustIndent(bool increase)
        {
            if (increase)
                indent += 4;
            else if (indent > 4)
                indent -= 4;
            else
                indent = 0;
        }



        public void Write(string str)
        {
            if (sol)
            {
                sb.Append(new string(' ', indent));
                sol = false;
            }
            sb.Append(str);
        }



        public void Write(string fmt, params object[] args)
        {
            if (sol)
            {
                sb.Append(new string(' ', indent));
                sol = false;
            }
            sb.AppendFormat(fmt, args);
        }



        public void NewLine()
        {
            sb.AppendLine();
            sol = true;
        }



        public override string ToString() => sb.ToString();
    }

}
