using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata.BooleanAlgebras
{
    /// <summary>
    /// Implements a disjoint union of two Boolean algebras.
    /// A Tuple(s,t) represents intuitively the union {(1,x) : x in s} U {(2,x) : x in t}.
    /// </summary>
    /// <typeparam name="S">type of the first Boolean algebra</typeparam>
    /// <typeparam name="T">type of the second Boolean algebra</typeparam>
    public class DisjointUnionAlgebra<S, T> : IBooleanAlgebra<Tuple<S, T>>
    {
        IBooleanAlgebra<S> first;
        IBooleanAlgebra<T> second;
        MintermGenerator<Tuple<S, T>> mintermgenerator;
        Tuple<S, T> tt;
        Tuple<S, T> ff;

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
        public DisjointUnionAlgebra(IBooleanAlgebra<S> first, IBooleanAlgebra<T> second)
        {
            this.first = first;
            this.second = second;
            this.mintermgenerator = new MintermGenerator<Tuple<S, T>>(this);
            this.tt = new Tuple<S, T>(first.True, second.True);
            this.ff = new Tuple<S, T>(first.False, second.False);
        }

        /// <summary>
        /// Gets the top element.
        /// </summary>
        public Tuple<S, T> True
        {
            get { return tt; }
        }

        /// <summary>
        /// Gets the bottom element.
        /// </summary>
        public Tuple<S, T> False
        {
            get { return ff; }
        }

        /// <summary>
        /// Returns the pairwise union (disjunction) of the enumerated pairs.
        /// </summary>
        /// <param name="predicates">given enumeration of predicate pairs</param>
        public Tuple<S, T> MkOr(IEnumerable<Tuple<S, T>> predicates)
        {
            var one = first.MkOr(EnumerateFirstComponents(predicates));
            var two = second.MkOr(EnumerateSecondComponents(predicates));
            if (one.Equals(first.False) && second.Equals(second.False))
                return ff;
            else if (one.Equals(first.True) && second.Equals(second.True))
                return tt;
            else
                return new Tuple<S, T>(one, two);
        }

        /// <summary>
        /// Returns the pairwise intersection (conjunction) of the enumerated pairs.
        /// </summary>
        /// <param name="predicates">given enumeration of predicate pairs</param>
        public Tuple<S, T> MkAnd(IEnumerable<Tuple<S, T>> predicates)
        {
            var one = first.MkAnd(EnumerateFirstComponents(predicates));
            var two = second.MkAnd(EnumerateSecondComponents(predicates));
            if (one.Equals(first.False) && second.Equals(second.False))
                return ff;
            else if (one.Equals(first.True) && second.Equals(second.True))
                return tt;
            else
                return new Tuple<S, T>(one, two);
        }

        public Tuple<S, T> MkAnd(params Tuple<S, T>[] predicates)
        {
            var one = first.MkAnd(EnumerateFirstComponents(predicates));
            var two = second.MkAnd(EnumerateSecondComponents(predicates));
            if (one.Equals(first.False) && second.Equals(second.False))
                return ff;
            else if (one.Equals(first.True) && second.Equals(second.True))
                return tt;
            else
                return new Tuple<S, T>(one, two);
        }

        static private IEnumerable<T> EnumerateSecondComponents(IEnumerable<Tuple<S, T>> predicates)
        {
            foreach (var p in predicates)
                yield return p.Item2;
        }

        static private IEnumerable<S> EnumerateFirstComponents(IEnumerable<Tuple<S, T>> predicates)
        {
            foreach (var p in predicates)
                yield return p.Item1;
        }

        /// <summary>
        /// Complement the pair predicate by complementing its components.
        /// </summary>
        public Tuple<S, T> MkNot(Tuple<S, T> predicate)
        {
            return new Tuple<S, T>(first.MkNot(predicate.Item1), second.MkNot(predicate.Item2));
        }

        /// <summary>
        /// Complement the pair predicate by complementing its components.
        /// </summary>
        public Tuple<S, T> Simplify(Tuple<S, T> predicate)
        {
            return new Tuple<S, T>(first.Simplify(predicate.Item1), second.Simplify(predicate.Item2));
        }

        /// <summary>
        /// Returns true iff the first components are equivalent and the second components are equivalent.
        /// </summary>
        public bool AreEquivalent(Tuple<S, T> predicate1, Tuple<S, T> predicate2)
        {
            return first.AreEquivalent(predicate1.Item1, predicate2.Item1) &&
                 second.AreEquivalent(predicate1.Item2, predicate2.Item2);
        }

        /// <summary>
        /// Makes a union of the first components and a union of the second components.
        /// </summary>
        public Tuple<S, T> MkOr(Tuple<S, T> predicate1, Tuple<S, T> predicate2)
        {
            return new Tuple<S, T>(first.MkOr(predicate1.Item1, predicate2.Item1), second.MkOr(predicate1.Item2, predicate2.Item2));
        }

        /// <summary>
        /// Makes an intersection of the first components and an intersection of the second components.
        /// </summary>
        public Tuple<S, T> MkAnd(Tuple<S, T> predicate1, Tuple<S, T> predicate2)
        {
            return new Tuple<S, T>(first.MkAnd(predicate1.Item1, predicate2.Item1), second.MkAnd(predicate1.Item2, predicate2.Item2));
        }

        /// <summary>
        /// Returns true iff the first or the second component of the pair predicate is satisfiable.
        /// </summary>
        public bool IsSatisfiable(Tuple<S, T> predicate)
        {
            return first.IsSatisfiable(predicate.Item1) || second.IsSatisfiable(predicate.Item2);
        }

        /// <summary>
        /// Generate minterms for the constraints.
        /// </summary>
        public IEnumerable<Tuple<bool[], Tuple<S, T>>> GenerateMinterms(params Tuple<S, T>[] constraints)
        {
            return mintermgenerator.GenerateMinterms(constraints);
        }


        public bool IsExtensional
        {
            get { return First.IsExtensional && Second.IsExtensional; }
        }

        public Tuple<S, T> MkSymmetricDifference(Tuple<S, T> p1, Tuple<S, T> p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool CheckImplication(Tuple<S, T> lhs, Tuple<S, T> rhs)
        {
            return first.CheckImplication(lhs.Item1, rhs.Item1) && second.CheckImplication(lhs.Item2, rhs.Item2);
        }

        public bool IsAtomic
        {
            get { return first.IsAtomic && second.IsAtomic; }
        }

        public Tuple<S, T> GetAtom(Tuple<S, T> psi)
        {
            if (!IsAtomic)
                throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);

            var a1 = first.GetAtom(psi.Item1);
            if (a1.Equals(first.False))
                return False;

            var a2 = second.GetAtom(psi.Item2);
            if (a2.Equals(second.False))
                return False;

            return new Tuple<S, T>(a1, a2);
        }


        public bool EvaluateAtom(Tuple<S, T> atom, Tuple<S, T> psi)
        {
            throw new NotImplementedException();
        }


        public Tuple<S, T> MkDiff(Tuple<S, T> predicate1, Tuple<S, T> predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }
    }
}
