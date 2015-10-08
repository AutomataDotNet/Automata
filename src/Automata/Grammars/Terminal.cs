using System;

namespace Microsoft.Automata.Grammars
{
    public class Exprinal<T> : GrammarSymbol
    {
        public T term;
        string name;

        public Exprinal(T term, string name)
        {
            this.term = term;
            this.name = name;
        }

        public override string Name
        {
            get { return name; }
        }

        public override string ToString()
        {
            return name;
        }
    }
}
