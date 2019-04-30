using System;

namespace Microsoft.Automata.Grammars
{
    public class Nonterminal : GrammarSymbol
    {
        string name;

        public override string Name
        {
            get { return name; }
        }

        Nonterminal(string name)
        {
            this.name = name;
        }

        #region special symbols reserved for PDA construction from CFG
        internal static Nonterminal StartStackSymbol = new Nonterminal("^");
        internal static Nonterminal EndStackSymbol = new Nonterminal("$");
        #endregion

        #region special nonterminal creators
        /// <summary>
        /// Appends '#q' in front of state id represented in decimal.
        /// </summary>
        internal static Nonterminal MkNonterminalForStateId(int stateId)
        {
            return new Nonterminal("#q" + stateId);
        }

        /// <summary>
        /// Appends '#t' in front of terminal name.
        /// </summary>
        internal static Nonterminal MkNonterminalForTerminal(string name)
        {
            return new Nonterminal("#t" + name);
        }

        /// <summary>
        /// Appends '#' in front of id represented in decimal.
        /// </summary>
        internal static Nonterminal MkNonterminalForId(int id)
        {
            return new Nonterminal("#" + id);
        }

        /// <summary>
        /// Uses the name as is
        /// </summary>
        internal static Nonterminal MkNonterminalForZ3Expr(string name)
        {
            return new Nonterminal(name);
        }

        /// <summary>
        /// Appends '#r' in front of id represented in decimal.
        /// </summary>
        internal static Nonterminal MkNonterminalForRegex(int id)
        {
            return new Nonterminal("#r" + id);
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
        internal static Nonterminal CreateByParser(string name)
        {
            return new Nonterminal(name);
        }
        #endregion

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
