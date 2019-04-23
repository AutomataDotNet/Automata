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

        public Nonterminal(string name)
        {
            this.name = name;
        }

        public Nonterminal(int id)
        {
            this.name = "<" + id + ">";
        }

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
