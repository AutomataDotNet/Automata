using System;

namespace Microsoft.Automata.Grammars
{
    /// <summary>
    /// terminal symbol
    /// </summary>
    public class Terminal<T> : GrammarSymbol
    {
        /// <summary>
        /// the term
        /// </summary>
        public T term;

        /// <summary>
        /// constructs a terminal
        /// </summary>
        /// <param name="term">given term</param>
        /// <param name="name">given name</param>
        public Terminal(T term)
        {
            this.term = term;
        }

        string __name = null;
        /// <summary>
        /// name of the terminal
        /// </summary>
        public override string Name
        {
            get
            {
                if (__name == null)
                {
                    string s = term.ToString();
                    if (s.Length > 0)
                    {
                        char first = s[0];
                        if (('A' <= first && first <= 'Z') || first == Nonterminal.ReservedNonterminalStart)
                            __name = "(" + s + ")";
                        else
                            __name = s;
                    }
                    else
                        __name = "";
                }
                return __name;
            }
        }

        /// <summary></summary>
        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            var t = obj as Terminal<T>;
            if (t == null)
                return false;
            return t.term.Equals(this.term);
        }

        public override int GetHashCode()
        {
            return term.GetHashCode();
        }
    }
}
