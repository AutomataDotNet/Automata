using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata.Grammars
{
    public class Production
    {
        public readonly Nonterminal Lhs;
        public readonly GrammarSymbol[] Rhs;

        /// <summary>
        /// Returns true if Rhs is empty
        /// </summary>
        public bool IsEpsilon
        {
            get { return Rhs.Length == 0; }
        }

        public bool IsUnit
        {
            get
            {
                return Rhs.Length == 1 && Rhs[0] is Nonterminal;
            }
        }

        public bool IsSingleExprinal
        {
            get
            {
                return Rhs.Length == 1 && !(Rhs[0] is Nonterminal);
            }
        }

        /// <summary>
        /// Returns the First symbol in the Rhs.
        /// Assumes that Rhs is nonempty.
        /// </summary>
        public GrammarSymbol First
        {
            get
            {
                return Rhs[0];
            }
        }

        /// <summary>
        /// Returns the symbols in the Rhs except the first.
        /// Assumes that Rhs is nonempty.
        /// </summary>
        public GrammarSymbol[] Rest
        {
            get
            {
                GrammarSymbol[] rest = new GrammarSymbol[Rhs.Length - 1];
                Array.Copy(Rhs, 1, rest, 0, Rhs.Length - 1);
                return rest;
            }
        }

        public bool ContainsNoExprinals
        {
            get
            {
                foreach (GrammarSymbol s in Rhs)
                    if (!(s is Nonterminal))
                        return false;
                return true;
            }
        }

        public bool ContainsNoVariables
        {
            get
            {
                foreach (GrammarSymbol s in Rhs)
                    if (s is Nonterminal)
                        return false;
                return true;
            }
        }

        public IEnumerable<Nonterminal> GetVariables()
        {
            foreach (GrammarSymbol s in Rhs)
                if (s is Nonterminal)
                    yield return (Nonterminal)s;
        }

        public string Description
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Lhs.Name);
                sb.Append(" -> ");
                sb.Append(DescriptionOfRhs);
                return sb.ToString();
            }
        }

        public Production(Nonterminal lhs, params GrammarSymbol[] rhs)
        {
            this.Lhs = lhs;
            this.Rhs = rhs;
        }

        public Production(Nonterminal lhs, GrammarSymbol[] rhsButLast, GrammarSymbol last)
        {
            this.Lhs = lhs;
            this.Rhs = new GrammarSymbol[rhsButLast.Length + 1];
            Array.Copy(rhsButLast, this.Rhs, rhsButLast.Length);
            this.Rhs[rhsButLast.Length] = last;
        }

        //Symbol Last
        //{
        //    get
        //    {
        //        return Rhs[Rhs.Length-1];
        //    }
        //}

        //public static List<Production> MkList(params Production[] prods)
        //{
        //    return new List<Production>(prods);
        //}

        public override string ToString()
        {
            return Description;
        }

        internal bool AreVariablesContainedIn(HashSet<Nonterminal> vars)
        {
            foreach (GrammarSymbol s in Rhs)
                if (s is Nonterminal)
                    if (!vars.Contains((Nonterminal)s))
                        return false;
            return true;
        }

        public string DescriptionOfRhs
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (GrammarSymbol s in Rhs)
                {
                    sb.Append(s.Name);
                    sb.Append(" ");
                }
                return sb.ToString();
            }
        }

        public bool IsCNF
        {
            get
            {
                return ((Rhs.Length == 1 && !(Rhs[0] is Nonterminal)) ||
                    (Rhs.Length == 2 && Rhs[0] is Nonterminal && Rhs[1] is Nonterminal));
            }
        }

        public bool IsGNF
        {
            get
            {
                return ((Rhs.Length > 0 && !(Rhs[0] is Nonterminal)));
            }
        }

        public IEnumerable<GrammarSymbol> GetExprinals()
        {
            foreach (GrammarSymbol s in Rhs)
                if (!(s is Nonterminal))
                    yield return s;
        }

        internal bool RhsContainsSymbol(GrammarSymbol symbol)
        {
            foreach (GrammarSymbol s in Rhs)
                if (s.Equals(symbol))
                    return true;
            return false;
        }
    }
}
