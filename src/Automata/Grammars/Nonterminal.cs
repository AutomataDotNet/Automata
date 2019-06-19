using System;

namespace Microsoft.Automata.Grammars
{
    public class Nonterminal : GrammarSymbol
    {
        string name;

        public const char ReservedNonterminalStart = '#';

        public override string Name
        {
            get { return name; }
        }

        Nonterminal(string name)
        {
            this.name = name;
        }

        #region special symbols reserved for PDA construction from CFG
        /// <summary>
        /// Nonterminal "^"
        /// </summary>
        internal static Nonterminal StartStackSymbol = new Nonterminal("^");
        /// <summary>
        /// Nonterminal "$"
        /// </summary>
        internal static Nonterminal EndStackSymbol = new Nonterminal("$");
        #endregion

        #region special nonterminal creators
        /// <summary>
        /// Appends ReservedNonterminalStart + "q" in front of state id represented in decimal.
        /// </summary>
        internal static Nonterminal MkNonterminalForStateId(int stateId)
        {
            return new Nonterminal(ReservedNonterminalStart.ToString() + "q" + stateId.ToString());
        }

        /// <summary>
        /// Appends ReservedNonterminalStart + "r" in front of id represented in decimal.
        /// </summary>
        internal static Nonterminal MkNonterminalForRegex(int id)
        {
            return new Nonterminal(ReservedNonterminalStart.ToString() + "r" + id.ToString());
        }

        /// <summary>
        /// Appends ReservedNonterminalStart in front of id represented in decimal.
        /// </summary>
        public static Nonterminal MkNonterminalForId(int id)
        {
            return new Nonterminal(ReservedNonterminalStart.ToString() + id.ToString());
        }

        /// <summary>
        /// Uses the name as is
        /// </summary>
        internal static Nonterminal MkNonterminalForZ3Expr(string name)
        {
            return new Nonterminal(name);
        }

        /// <summary>
        /// Produces the nonterminal regexId + '_' + stateId
        /// </summary>
        internal static Nonterminal MkNonterminalForAutomatonState(string regexId, int stateId)
        {
            return new Nonterminal(regexId + "_" + stateId);
        }

        /// <summary>
        /// Creates the nonterminal with the name as is during parsing.
        /// </summary>
        public static Nonterminal CreateByParser(string name)
        {
            return new Nonterminal(name);
        }
        #endregion

        public override bool Equals(object obj)
        {
            Nonterminal nt = obj as Nonterminal;
            if (nt == null)
                return false;
            else
                return nt.name.Equals(this.name);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() + 1;
        }
    }
}
