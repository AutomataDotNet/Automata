using System;


namespace Microsoft.Automata.Grammars
{
    public abstract class GrammarSymbol
    {
        public abstract string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
