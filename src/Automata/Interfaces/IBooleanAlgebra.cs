using System;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    /// <summary>
    /// Boolean Algebra Positive operations.
    /// Provides elements for true and false.
    /// Provides operations for conjunction, disjunction, and satisfiability checking of predicates.
    /// </summary>
    public interface IBooleanAlgebraPositive<S>
    {
        /// <summary>
        /// Make a conjunction of predicate1 and predicate2.
        /// </summary>
        S MkAnd(S predicate1, S predicate2);

        /// <summary>
        /// Make a disjunction of predicate1 and predicate2. 
        /// </summary>
        S MkOr(S predicate1, S predicate2);

        /// <summary>
        /// Returns true iff the predicate is satisfiable.
        /// </summary>
        bool IsSatisfiable(S predicate);

        /// <summary>
        /// Top element of the Boolean algebra, corresponds to the value true.
        /// </summary>
        S True { get; }

        /// <summary>
        /// Bottom element of the Boolean algebra, corresponds to the value false.
        /// </summary>
        S False { get; }
    }

    /// <summary>
    /// Generic Boolean Algebra solver.
    /// Provides operations for conjunction, disjunction, and negation.
    /// Allows to decide if a predicate is satisfiable and if two predicates are equivalent.
    /// </summary>
    /// <typeparam name="S">predicates</typeparam>
    public interface IBooleanAlgebra<S> : IBooleanAlgebraPositive<S>
    {
        /// <summary>
        /// Make a disjunction of all the predicates in the enumeration. 
        /// Must return False if the enumeration is empty.
        /// </summary>
        S MkOr(IEnumerable<S> predicates);

        /// <summary>
        /// Make a conjunction of all the predicates in the enumeration. 
        /// Returns True if the enumeration is empty.
        /// </summary>
        S MkAnd(IEnumerable<S> predicates);

        /// <summary>
        /// Make a conjunction of all the predicates. 
        /// Returns True if the enumeration is empty.
        /// </summary>
        S MkAnd(params S[] predicates);

        /// <summary>
        /// Negate the predicate.
        /// </summary>
        S MkNot(S predicate);


        /// <summary>
        /// Compute the predicate and(predicate1,not(predicate2))
        /// </summary>
        S MkDiff(S predicate1, S predicate2);

        /// <summary>
        /// Returns true iff predicate1 is equivalent to predicate2.
        /// </summary>
        bool AreEquivalent(S predicate1, S predicate2);

        /// <summary>
        /// Returns a predicate that is equivalent to (p1&amp;!p2)|(p2&amp;!p1).
        /// </summary>
        S MkSymmetricDifference(S p1, S p2);

        /// <summary>
        /// Returns true iff lhs implies rhs.
        /// </summary>
        bool CheckImplication(S lhs, S rhs); 

        /// <summary>
        /// True iff any two equivalent predicates are identical.
        /// </summary>
        bool IsExtensional { get; }

        /// <summary>
        /// Simplifies the predicate. 
        /// </summary>
        S Simplify(S predicate);

        /// <summary>
        /// Returns true iff the algebra is atomic:
        /// each satisfiable predicate is implied by an atomic predicate. 
        /// A predicate p is atomic means: p is satisfiable and if q =&gt; p then q &lt;=&gt; p.
        /// </summary>
        bool IsAtomic { get; }

        /// <summary>
        /// If the algebra is atomic and psi is satisfiable 
        /// then returns an atomic predicate that implies psi.
        /// Returns False if psi is unsatisfiable.
        /// Throws AutomataException if the algebra is not atomic.
        /// </summary>
        S GetAtom(S psi);

        /// <summary>
        /// The predicate atom must represent a singleton set {v} for some value v.
        /// Returns true iff v is accepted by psi.
        /// </summary>
        /// <param name="atom">predicate denoting a singleton set</param>
        /// <param name="psi">some predicate</param>
        /// <returns></returns>
        bool EvaluateAtom(S atom, S psi);

        /// <summary>
        /// Given an array of constraints {c_1, c_2, ..., c_n} where n&gt;=0.
        /// Enumerate all satisfiable Boolean combinations Tuple({b_1, b_2, ..., b_n}, c)
        /// where c is satisfisable and equivalent to c'_1 &amp; c'_2 &amp; ... &amp; c'_n, 
        /// where c'_i = c_i if b_i = true and c'_i is Not(c_i) otherwise.
        /// If n=0 return Tuple({},True)
        /// </summary>
        /// <param name="constraints">array of constraints</param>
        /// <returns>Booolean combinations that are satisfiable</returns>
        IEnumerable<Tuple<bool[], S>> GenerateMinterms(params S[] constraints);
    }

    /*
//sample 
public class A32 : IBooleanAlgebra<UInt32>
{
    public uint True
    {
        get { return 0xFFFFFFFF; }
    }

    public uint False
    {
        get { return 0; }
    }

    public uint MkOr(IEnumerable<uint> predicates)
    {
        uint res = 0;
        foreach (var p in predicates)
        {
            res = res | p;
            if (res == 0xFFFFFFFF) { return res; }
        }
        return res;
    }

    public uint MkAnd(IEnumerable<uint> predicates)
    {
        uint res = 0xFFFFFFFF;
        foreach (var p in predicates)
        {
            res = res & p;
            if (res == 0) { return res; }
        }
        return res;
    }

    public uint MkAnd(params uint[] predicates)
    {
        uint res = 0xFFFFFFFF;
        foreach (var p in predicates)
        {
            res = res & p;
            if (res == 0) { return res; }
        }
        return res;
    }

    public uint MkNot(uint predicate)
    {
        return ~predicate;
    }

    public bool AreEquivalent(uint predicate1, uint predicate2)
    {
        return predicate1 == predicate2;
    }

    public uint Simplify(uint predicate)
    {
        return predicate;
    }

    public uint MkOr(uint predicate1, uint predicate2)
    {
        return predicate1 | predicate2;
    }

    public uint MkAnd(uint predicate1, uint predicate2)
    {
        return predicate1 & predicate2;
    }

    public bool IsSatisfiable(uint predicate)
    {
        return predicate != 0;
    }
}
 */
}
