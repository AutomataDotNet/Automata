using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    public class Maybe<S>
    {
        Tuple<bool, S> t;

        public bool IsSomething
        {
            get { return t.Item1; }
        }

        public S Element
        {
            get { return t.Item2; }
        }

        public static readonly Maybe<S> Nothing = 
            new Maybe<S>(new Tuple<bool, S>(false, default(S)));

        Maybe(Tuple<bool, S> t)
        {
            this.t = t;
        }

        public static Maybe<S> Something(S elem)
        {
            return new Maybe<S>(new Tuple<bool, S>(true, elem));
        }

        public override bool Equals(object obj)
        {
            Maybe<S> x = obj as Maybe<S>;
            if (x == null)
                return false;
            return t.Equals(x.t);
        }

        public override int GetHashCode()
        {
            return t.GetHashCode();
        }

        public override string ToString()
        {
            if (t.Item1)
                return t.Item2.ToString();
            else
                return "[]";
        }
    }
}
