using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata.BooleanAlgebras
{
    /// <summary>
    /// Boolean algebra over an atomic universe.
    /// </summary>
    public class TrivialBooleanAlgebra : IBooleanAlgebra<bool>
    {
        public TrivialBooleanAlgebra()
        {
        }

        public IEnumerable<Tuple<bool[], bool>> GenerateMinterms(params bool[] constraints)
        {
            yield return new Tuple<bool[], bool>(constraints, true);
        }

        public bool True
        {
            get { return true; }
        }

        public bool False
        {
            get { return false; }
        }

        public bool MkOr(IEnumerable<bool> predicates)
        {
            foreach (var v in predicates)
                if (v)
                    return true;
            return false;
        }

        public bool MkAnd(IEnumerable<bool> predicates)
        {
            foreach (var v in predicates)
                if (!v)
                    return false;
            return true;
        }

        public bool MkAnd(params bool[] predicates)
        {
            for (int i = 0; i < predicates.Length; i++)
                if (!predicates[i])
                    return false;
            return true;
        }

        public bool MkNot(bool predicate)
        {
            return !predicate;
        }

        public bool AreEquivalent(bool predicate1, bool predicate2)
        {
            return predicate1 == predicate2;
        }

        public bool IsExtensional
        {
            get { return true; }
        }

        public bool Simplify(bool predicate)
        {
            return predicate;
        }

        public bool IsSatisfiable(bool predicate)
        {
            return predicate;
        }

        public bool MkAnd(bool predicate1, bool predicate2)
        {
            return predicate1 && predicate2;
        }

        public bool MkOr(bool predicate1, bool predicate2)
        {
            return predicate1 || predicate2;
        }


        public bool MkSymmetricDifference(bool p1, bool p2)
        {
            return p1 != p2;
        }

        public bool CheckImplication(bool lhs, bool rhs)
        {
            return !lhs || rhs;
        }

        public bool IsAtomic
        {
            get { return true; }
        }

        public bool GetAtom(bool psi)
        {
            return psi;
        }

        public bool EvaluateAtom(bool atom, bool psi)
        {
            return atom && psi;
        }


        public bool MkDiff(bool predicate1, bool predicate2)
        {
            return predicate1 && !predicate2;
        }
    }
}
