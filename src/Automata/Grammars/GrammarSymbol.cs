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

        public override bool Equals(object obj)
        {
            var gs = obj as GrammarSymbol;
            return ((gs != null) && Name.Equals(gs.Name));
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
