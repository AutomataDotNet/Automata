using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata.Z3.Internal
{
    internal class Escaper
    {
        #region Escaping strings
        /// <summary>
        /// Make an escaped string from a character
        /// </summary>
        public static string Escape(char c)
        {
            int code = (int)c;

            if (code > 126)
                return ToUnicodeRepr(code);

            if (code <= 32)
                return string.Format("\\x{0:X}", code);

            switch (c)
            {
                case '\\':
                    return "\\\\";
                case '\0':
                    return @"\0";
                case '\a':
                    return @"\a";
                case '\b':
                    return @"\b";
                case '\t':
                    return @"\t";
                case '\r':
                    return @"\r";
                case '\v':
                    return @"\v";
                case '\f':
                    return @"\f";
                case '\n':
                    return @"\n";
                case '\u001B':
                    return @"\e";
                case '\"':
                    return "\\\"";
                case '\'':
                    return "\\\'";
                case ' ':
                    return @"\s";
                default:
                    return c.ToString();
            }
        }


        public static string EscapeHex(int n)
        {
            if (0 <= n && n <= 9)
                return n.ToString();
            else
                return string.Format("0x{0:X}", n);
        }

        static string ToUnicodeRepr(int i)
        {
            string s = string.Format("{0:X}", i);
            if (s.Length == 1)
                s = "\\u000" + s;
            else if (s.Length == 2)
                s = "\\u00" + s;
            else if (s.Length == 3)
                s = "\\u0" + s;
            else
                s = "\\u" + s;
            return s;
        }

        /// <summary>
        /// Make an escaped string from a string
        /// </summary>
        public static string Escape(string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\"");
            foreach (char c in s)
            {
                sb.Append(Escape(c));
            }
            sb.Append("\"");
            return sb.ToString();
        }
        #endregion
    }
}
