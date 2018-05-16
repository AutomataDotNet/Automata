using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata.BooleanAlgebras
{
    internal class FiniteSetAlgebra<S> : IBooleanAlgebra<UIntW>
    {        
        readonly Dictionary<S,UIntW> alph;

        readonly int size;
        readonly UIntW full;
        readonly UIntW empty;

        MintermGenerator<UIntW> mtg;

        public FiniteSetAlgebra(HashSet<S> universe)
        {
            if (universe.Count > 63)
                throw new AutomataException("for now only supports alphabets of size<=63");
            var alphDic = new Dictionary<S,UIntW>();
            foreach (var v in universe)
                alphDic[v] = new UIntW(((UInt64)1) << alphDic.Count);
            alph = alphDic;
            this.size=universe.Count;
            this.empty = new UIntW(0);
            this.full = new UIntW((((UInt64)1) << size) - 1);

            mtg = new MintermGenerator<UIntW>(this);
        }

        public bool AreEquivalent(UIntW a, UIntW b)
        {
            return a==b;
        }

        public bool CheckImplication(UIntW a, UIntW b)
        {
            return MkDiff(a,b).val==0;
        }

        public UIntW True
        {
            get { return full; }
        }

        public UIntW False
        {
            get { return empty; }
        }

        public UIntW MkOr(IEnumerable<UIntW> sets)
        {
            var res = False.val;
            foreach (var set in sets)
                res = res | set.val;
            return new UIntW(res);
        }

        public UIntW MkAnd(IEnumerable<UIntW> sets)
        {
            var res = True.val;
            foreach (var set in sets)
                res = res & set.val;
            return new UIntW(res);
        }

        public UIntW MkAnd(params UIntW[] sets)
        {
            var res = True.val;
            foreach (var set in sets)
                res = res & set.val;
            return new UIntW(res);
        }

        public UIntW MkNot(UIntW set)
        {
            return new UIntW(True.val & ~(set.val));
        }

        public UIntW MkOr(UIntW s1, UIntW s2)
        {
            return new UIntW(s1.val | s2.val);
        }

        public UIntW MkAnd(UIntW s1, UIntW s2)
        {
            return new UIntW(s1.val & s2.val);
        }

        public bool IsSatisfiable(UIntW s)
        {
            return s.val > 0;
        }

        public IEnumerable<Tuple<bool[], UIntW>> GenerateMinterms(params UIntW[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        public UIntW Simplify(UIntW s)
        {
            return s;
        }

        public bool IsExtensional
        {
            get { return false; }
        }

        public UIntW MkSymmetricDifference(UIntW p1, UIntW p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool IsAtomic
        {
            get { return true; }
        }

        private UIntW IthOnly(UIntW var, int pos){
            return new UIntW((UInt64)(var.val & (uint)(1<<pos)));
        }

        public UIntW GetAtom(UIntW set)
        {
            if (!IsSatisfiable(set))
                return set;

            for(int i=0;i<size;i++){
                var ith = IthOnly(set,i);
                if(ith.val>0)
                    return ith;
            }                

            //This shouldn't happen
            return new UIntW(0);
        }

        public UIntW MkAtom(S atom)
        {
            if (!alph.ContainsKey(atom))
                throw new AutomataException("Not a member of universe "+ atom);

            return alph[atom];
        }


        public bool EvaluateAtom(UIntW atom, UIntW psi)
        {
            throw new NotImplementedException();
        }


        public UIntW MkDiff(UIntW predicate1, UIntW predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }
    }

    internal class UIntW
    {
        public UInt64 val;
        public UIntW(UInt64 val)
        {
            this.val = val; 
        }
        public override string ToString()
        {
            return Convert.ToString((int)val, 2);
        }
    }
}
