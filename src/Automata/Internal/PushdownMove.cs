using System;
using System.Text;

namespace Microsoft.Automata
{
    internal class PushdownLabel<S, T> : Tuple<T, Tuple<S, Sequence<S>>>
    {
        public S PopSymbol { get { return Item2.Item1;} }
        public Sequence<S> PushSymbols { get { return Item2.Item2; } }
        public T Input { get { return Item1; } }
        public Tuple<S, Sequence<S>> PushAndPop { get { return Item2;} }

        public bool InputIsEpsilon
        {
            get
            {
                return object.Equals(Input, default(T));
            }
        }

        public PushdownLabel(T label, S pop, params S[] push) : base(label, new Tuple<S,Sequence<S>>(pop, new Sequence<S>(push)))
        {
        }

        public PushdownLabel(S pop, params S[] push) : base(default(T), new Tuple<S, Sequence<S>>(pop, new Sequence<S>(push)))
        {
        }

        public PushdownLabel(T label, Tuple<S, Sequence<S>> pp) : base(label, pp)
        {
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            sb.Append(object.Equals(Input,default(T)) ? "" : Input.ToString() + ":");
            sb.Append(PopSymbol);
            sb.Append("/");
            sb.Append(PushSymbols);
            sb.Append(")");
            return sb.ToString();
        }
    }
}
