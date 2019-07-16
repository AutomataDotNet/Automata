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
        public static char ASSIGN = '\u2254';
        public static char SUBSCRIPT0 = '\u2080';
        public static char SUBSCRIPT1 = '\u2081';
        public static char SUBSCRIPT2 = '\u2082';
        public static char SUBSCRIPT3 = '\u2083';
        public static char SUBSCRIPT4 = '\u2084';
        public static char SUBSCRIPT5 = '\u2085';
        public static char SUBSCRIPT6 = '\u2086';
        public static char SUBSCRIPT7 = '\u2087';
        public static char SUBSCRIPT8 = '\u2088';
        public static char SUBSCRIPT9 = '\u2089';
        public static char SUBSCRIPTMINUS = '\u208B';
        public static char SUBSCRIPTPLUS = '\u208A';
        public static char FORALL = '\u2200';
        public static char EXISTS = '\u2203';
        public static char NOTEXISTS = '\u2204';
        public static char LEQ = '\u2264';
        public static char NOTLEQ = '\u2270';
        public static char NOTLT = '\u226E';
        public static char NOTGT = '\u226F';
        public static char GEQ = '\u2265';
        public static char NOTGEQ = '\u2271';
        public static char TOP = '\u22A4';
        public static char BOT = '\u22A5';
        public static char EMPTYSET = '\u2205';
        public static char NEQ = '\u2260';
        public static char NEWLINESYMBOL = '\u2424';

        public static string ToSubscript(int index)
        {
            var s = index.ToString();
            for (int i = 0; i < 10; i++)
                s = s.Replace((char)((int)'0' + i), (char)(0x2080 + i));
            return s.Replace('-', SUBSCRIPTMINUS);
        }
    }
}
