using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    public class FiniteSetAlgebra<S> : IBooleanAlgebra<HashSet<S>>
    {
        readonly HashSet<S> universe;
        readonly HashSet<S> empty;

        MintermGenerator<HashSet<S>> mtg;

        public FiniteSetAlgebra(HashSet<S> universe)
        {
            this.universe = universe;
            this.empty = new HashSet<S>();
            mtg = new MintermGenerator<HashSet<S>>(this);
        }

        public bool AreEquivalent(HashSet<S> a, HashSet<S> b)
        {
            return a.SetEquals(b);
        }

        public bool CheckImplication(HashSet<S> a, HashSet<S> b)
        {
            return a.IsSubsetOf(b);
        }

        public HashSet<S> True
        {
            get { return new HashSet<S>(universe); }
        }

        public HashSet<S> False
        {
            get { return new HashSet<S>(); }
        }

        public HashSet<S> MkOr(IEnumerable<HashSet<S>> sets)
        {
            var res = False;
            foreach (var set in sets)
                res.UnionWith(set);
            return res;
        }

        public HashSet<S> MkAnd(IEnumerable<HashSet<S>> sets)
        {
            var res = True;
            foreach (var set in sets)
                res.IntersectWith(set);
            return res;
        }

        public HashSet<S> MkAnd(params HashSet<S>[] sets)
        {
            var res = True;
            foreach (var set in sets)
                res.IntersectWith(set);
            return res;
        }

        public HashSet<S> MkNot(HashSet<S> set)
        {
            var res = True;
            res.RemoveWhere(el => set.Contains(el));
            return res;
        }

        public HashSet<S> MkOr(HashSet<S> s1, HashSet<S> s2)
        {
            var res = False;
            res.UnionWith(s1);
            res.UnionWith(s2);           
            return res;
        }

        public HashSet<S> MkAnd(HashSet<S> s1, HashSet<S> s2)
        {
            var res = True;
            res.IntersectWith(s1);
            res.IntersectWith(s2);
            return res;
        }

        public bool IsSatisfiable(HashSet<S> s)
        {
            return s.Count>0;
        }

        public IEnumerable<Pair<bool[], HashSet<S>>> GenerateMinterms(params HashSet<S>[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        public HashSet<S> Simplify(HashSet<S> s)
        {
            return s;
        }

        public bool IsExtensional
        {
            get { return false; }
        }

        public HashSet<S> MkSymmetricDifference(HashSet<S> p1, HashSet<S> p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool IsAtomic
        {
            get { return true; }
        }

        public HashSet<S> GetAtom(HashSet<S> set)
        {
            if (!IsSatisfiable(set))
                return set;

            foreach(var v in set){
                var res = new HashSet<S>();
                res.Add(v);
                return res;
            }

            //This shouldn't happen
            return null;
        }

        public HashSet<S> MkAtom(S atom)
        {
            if (!universe.Contains(atom))
                throw new AutomataException("Not a member of universe "+ atom);

            var set = new HashSet<S>();
            set.Add(atom);

            return set;
        }


        public bool EvaluateAtom(HashSet<S> atom, HashSet<S> psi)
        {
            throw new NotImplementedException();
        }


        public HashSet<S> MkDiff(HashSet<S> predicate1, HashSet<S> predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }
    }
}
