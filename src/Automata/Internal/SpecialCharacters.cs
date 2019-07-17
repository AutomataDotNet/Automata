using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    internal static class SpecialCharacters
    {
        //Logical connectives
        public static char NOT = '\u00AC';
        public static char AND = '\u2227';
        public static char OR = '\u2228';
        public static char IFF = '\u21D4';
        public static char IMPLIES = '\u21D2';
        public static char MODELS = '\u22A8';
        public static char TOP = '\u22A4';
        public static char BOT = '\u22A5';
        public static char FORALL = '\u2200';
        public static char EXISTS = '\u2203';
        public static char NOTEXISTS = '\u2204';
        //set operations
        public static char EMPTYSET = '\u2205';
        //misc
        public static char ASSIGN = '\u2254';
        public static char SUBSCRIPTMINUS = '\u208B';
        public static char SUBSCRIPTPLUS = '\u208A';
        public static char NEWLINESYMBOL = '\u2424';
        public static char LARGEBULLET = '\u26AB';
        public static char BLACKSQUARE = '\u220E';
        public static char CHECKMARK = '\u2713';
        //arithmetic
        public static char NEQ = '\u2260';
        public static char LEQ = '\u2264';
        public static char NOTLEQ = '\u2270';
        public static char NOTLT = '\u226E';
        public static char NOTGT = '\u226F';
        public static char GEQ = '\u2265';
        public static char NOTGEQ = '\u2271';

        /// <summary>
        /// Produces subscript index
        /// </summary>
        public static string ToSubscript(int index)
        {
            var s = index.ToString();
            for (int i = 0; i < 10; i++)
                s = s.Replace((char)((int)'0' + i), (char)(0x2080 + i));
            return s.Replace('-', SUBSCRIPTMINUS);
        }

        /// <summary>
        /// Produces counter 'c' with subscript i
        /// </summary>
        public static string Cntr(int i)
        {
            return "c" + SpecialCharacters.ToSubscript(i);
        }
    }
}
