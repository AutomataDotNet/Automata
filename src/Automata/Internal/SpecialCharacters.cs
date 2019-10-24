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
        public static char UNION = '\u222A';
        public static char INTERSECT = '\u2229';
        public static char IN = '\u2208';
        public static char NOTIN = '\u2209';
        public static char PROD = '\u00D7';
        //misc
        public static char ASSIGN = '\u2254';
        public static char SUBSCRIPTMINUS = '\u208B';
        public static char SUBSCRIPTPLUS = '\u208A';
        public static char NEWLINESYMBOL = '\u2424';
        public static char LARGEBULLET = '\u26AB';
        public static char BLACKSQUARE = '\u220E';
        public static char CHECKMARK = '\u2713';
        public static char MIDDOT = '\u00B7';
        public static char AND_DOT_ABOVE = '\u2A51';
        //arithmetic
        public static char NEQ = '\u2260';
        public static char LEQ = '\u2264';
        public static char NOTLEQ = '\u2270';
        public static char NOTLT = '\u226E';
        public static char NOTGT = '\u226F';
        public static char GEQ = '\u2265';
        public static char NOTGEQ = '\u2271';
        //lowercase greek letters
        public static char ALPHA_LOWERCASE = '\u03B1';
        public static char BETA_LOWERCASE = '\u03B2';
        public static char GAMMA_LOWERCASE = '\u03B3';
        public static char DELTA_LOWERCASE = '\u03B4';
        public static char EPSILON_LOWERCASE = '\u03B5';
        public static char ZETA_LOWERCASE = '\u03B6';
        public static char ETA_LOWERCASE = '\u03B7';
        public static char THETA_LOWERCASE = '\u03B8';
        public static char IOTA_LOWERCASE = '\u03B9';
        public static char KAPPA_LOWERCASE = '\u03BA';
        public static char LAMBDA_LOWERCASE = '\u03BB';
        public static char MU_LOWERCASE = '\u03BC';
        public static char NU_LOWERCASE = '\u03BD';
        public static char XI_LOWERCASE = '\u03BE';
        public static char OMICRON_LOWERCASE = '\u03BF';
        public static char PI_LOWERCASE = '\u03C0';
        public static char RHO_LOWERCASE = '\u03C1';
        public static char FINAL_SIGMA_LOWERCASE = '\u03C2';	 
        public static char SIGMA_LOWERCASE = '\u03C3';
        public static char TAU_LOWERCASE = '\u03C4';
        public static char UPSILON_LOWERCASE = '\u03C5';
        public static char PHI_LOWERCASE = '\u03C6';
        public static char CHI_LOWERCASE = '\u03C7';
        public static char PSI_LOWERCASE = '\u03C8';
        public static char OMEGA_LOWERCASE = '\u03C9';
        //--- some common capital greek letters
        public static char GAMMA_CAPITAL = '\u0393';
        public static char DELTA_CAPITAL = '\u0394';
        public static char THETA_CAPITA = '\u0398';
        public static char LAMBDA_CAPITAL = '\u039B';
        public static char XI_CAPITAL = '\u039E';
        public static char PI_CAPITAL = '\u03A0';
        public static char SIGMA_CAPITAL = '\u03A3';
        public static char PHI_CAPITAL = '\u03A6';
        public static char PSI_CAPITAL = '\u03A8';
        public static char OMEGA_CAPITAL = '\u03A9';

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
        /// Produces 'c' with subscript i
        /// </summary>
        public static string c(int i)
        {
            return "c" + SpecialCharacters.ToSubscript(i);
        }

        /// <summary>
        /// Produces 'S' with subscript i
        /// </summary>
        public static string S(int i)
        {
            return "S" + SpecialCharacters.ToSubscript(i);
        }

        /// <summary>
        /// Produces 'q' with subscript i
        /// </summary>
        public static string q(int i)
        {
            return "q" + SpecialCharacters.ToSubscript(i);
        }
    }
}
