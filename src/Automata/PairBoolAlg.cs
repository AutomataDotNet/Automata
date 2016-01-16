using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Implements a direct product (disjoint union) of two Boolean algebras.
    /// A Pair(s,t) represents intuitively the union {(1,x) : x in s} U {(2,x) : x in t}.
    /// </summary>
    /// <typeparam name="S">type of the first Boolean algebra</typeparam>
    /// <typeparam name="T">type of the second Boolean algebra</typeparam>
    public class PairBoolAlg<S,T> : IBoolAlgMinterm<Pair<S,T>>
    {
        IBooleanAlgebra<S> first;
        IBooleanAlgebra<T> second;
        MintermGenerator<Pair<S, T>> mintermgenerator;
        Pair<S, T> tt;
        Pair<S, T> ff;

        /// <summary>
        /// Gets the first algebra.
        /// </summary>
        IBooleanAlgebra<S> First { get { return first; } }

        /// <summary>
        /// Gets the second algebra.
        /// </summary>
        IBooleanAlgebra<T> Second { get { return second; } }

        /// <summary>
        /// Constructs a pair of Boolean algebras that itself is a Boolean algebra over the disjoint union of the domains.
        /// </summary>
        /// <param name="first">first algebra</param>
        /// <param name="second">second algebra</param>
        public PairBoolAlg(IBooleanAlgebra<S> first, IBooleanAlgebra<T> second)
        {
            this.first = first;
            this.second = second;
            this.mintermgenerator = new MintermGenerator<Pair<S, T>>(this);
            this.tt = new Pair<S, T>(first.True, second.True);
            this.ff = new Pair<S, T>(first.False, second.False);
        }
       
        /// <summary>
        /// Gets the top element.
        /// </summary>
        public Pair<S, T> True
        {
            get { return tt; }
        }

        /// <summary>
        /// Gets the bottom element.
        /// </summary>
        public Pair<S, T> False
        {
            get { return ff; }
        }

        /// <summary>
        /// Returns the pairwise union (disjunction) of the enumerated pairs.
        /// </summary>
        /// <param name="predicates">given enumeration of predicate pairs</param>
        public Pair<S, T> MkOr(IEnumerable<Pair<S, T>> predicates)
        {
            var one = first.MkOr(EnumerateFirstComponents(predicates));
            var two = second.MkOr(EnumerateSecondComponents(predicates));
            if (one.Equals(first.False) && second.Equals(second.False))
                return ff;
            else if (one.Equals(first.True) && second.Equals(second.True))
                return tt;
            else
                return new Pair<S, T>(one, two);
        }

        /// <summary>
        /// Returns the pairwise intersection (conjunction) of the enumerated pairs.
        /// </summary>
        /// <param name="predicates">given enumeration of predicate pairs</param>
        public Pair<S, T> MkAnd(IEnumerable<Pair<S, T>> predicates)
        {
            var one = first.MkAnd(EnumerateFirstComponents(predicates));
            var two = second.MkAnd(EnumerateSecondComponents(predicates));
            if (one.Equals(first.False) && second.Equals(second.False))
                return ff;
            else if (one.Equals(first.True) && second.Equals(second.True))
                return tt;
            else
                return new Pair<S, T>(one, two);
        }

        public Pair<S, T> MkAnd(params Pair<S, T>[] predicates)
        {
            var one = first.MkAnd(EnumerateFirstComponents(predicates));
            var two = second.MkAnd(EnumerateSecondComponents(predicates));
            if (one.Equals(first.False) && second.Equals(second.False))
                return ff;
            else if (one.Equals(first.True) && second.Equals(second.True))
                return tt;
            else
                return new Pair<S, T>(one, two);
        }

        static private IEnumerable<T> EnumerateSecondComponents(IEnumerable<Pair<S, T>> predicates)
        {
            foreach (var p in predicates)
                yield return p.Second;
        }

        static private IEnumerable<S> EnumerateFirstComponents(IEnumerable<Pair<S, T>> predicates)
        {
            foreach (var p in predicates)
                yield return p.First;
        }

        /// <summary>
        /// Complement the pair predicate by complementing its components.
        /// </summary>
        public Pair<S, T> MkNot(Pair<S, T> predicate)
        {
            return new Pair<S, T>(first.MkNot(predicate.First), second.MkNot(predicate.Second));
        }

        /// <summary>
        /// Complement the pair predicate by complementing its components.
        /// </summary>
        public Pair<S, T> Simplify(Pair<S, T> predicate)
        {
            return new Pair<S, T>(first.Simplify(predicate.First), second.Simplify(predicate.Second));
        }

        /// <summary>
        /// Returns true iff the first components are equivalent and the second components are equivalent.
        /// </summary>
        public bool AreEquivalent(Pair<S, T> predicate1, Pair<S, T> predicate2)
        {
            return first.AreEquivalent(predicate1.First, predicate2.First) &&
                 second.AreEquivalent(predicate1.Second, predicate2.Second);
        }

        /// <summary>
        /// Makes a union of the first components and a union of the second components.
        /// </summary>
        public Pair<S, T> MkOr(Pair<S, T> predicate1, Pair<S, T> predicate2)
        {
            return new Pair<S, T>(first.MkOr(predicate1.First, predicate2.First), second.MkOr(predicate1.Second, predicate2.Second));
        }

        /// <summary>
        /// Makes an intersection of the first components and an intersection of the second components.
        /// </summary>
        public Pair<S, T> MkAnd(Pair<S, T> predicate1, Pair<S, T> predicate2)
        {
            return new Pair<S, T>(first.MkAnd(predicate1.First, predicate2.First), second.MkAnd(predicate1.Second, predicate2.Second));
        }

        /// <summary>
        /// Returns true iff the first or the second component of the pair predicate is satisfiable.
        /// </summary>
        public bool IsSatisfiable(Pair<S, T> predicate)
        {
            return first.IsSatisfiable(predicate.First) || second.IsSatisfiable(predicate.Second);
        }

        /// <summary>
        /// Generate minterms for the constraints.
        /// </summary>
        public IEnumerable<Pair<bool[], Pair<S, T>>> GenerateMinterms(params Pair<S, T>[] constraints)
        {
            return mintermgenerator.GenerateMinterms(constraints);
        }


        public bool IsExtensional
        {
            get { return First.IsExtensional && Second.IsExtensional; }
        }

        public Pair<S, T> MkSymmetricDifference(Pair<S, T> p1, Pair<S, T> p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool CheckImplication(Pair<S, T> lhs, Pair<S, T> rhs)
        {
            return first.CheckImplication(lhs.Item1, rhs.Item1) && second.CheckImplication(lhs.Item2, rhs.Item2);
        }

        public bool IsAtomic
        {
            get { return first.IsAtomic && second.IsAtomic; }
        }

        public Pair<S, T> GetAtom(Pair<S, T> psi)
        {
            if (!IsAtomic)
                throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);

            var a1 = first.GetAtom(psi.Item1);
            if (a1.Equals(first.False))
                return False;

            var a2 = second.GetAtom(psi.Item2);
            if (a2.Equals(second.False))
                return False;

            return new Pair<S, T>(a1, a2);
        }
    }
}
