using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Extends IBooleanAlgebra with character predicate solving and predicate pretty printing.
    /// </summary>
    /// <typeparam name="PRED">predicates</typeparam>
    public interface ICharAlgebra<PRED> : IBooleanAlgebra<PRED>, IPrettyPrinter<PRED>
    {
        BitWidth Encoding { get; }

        /// <summary>
        /// Make a constraint describing the set of all characters between a (inclusive) and b (inclusive). 
        /// Add both uppercase and lowercase elelements if caseInsensitive is true.
        /// </summary>
        PRED MkRangeConstraint(char lower, char upper, bool caseInsensitive = false);

        /// <summary>
        /// Make a constraint describing a singleton set containing the character c, or
        /// a set containing also the upper and lowercase versions of c if caseInsensitive is true.
        /// </summary>
        /// <param name="caseInsensitive">if true include both the uppercase and the lowercase versions of the given character</param>
        /// <param name="c">the given character</param>
        PRED MkCharConstraint(char c, bool caseInsensitive = false);

        /// <summary>
        /// Make the disjunction of MkRangeConstraint(caseInsensitive, a, b) for all [a,b] in ranges.
        /// </summary>
        PRED MkRangesConstraint(bool caseInsensitive, IEnumerable<char[]> ranges);

        /// <summary>
        /// Make a term that encodes the given character set.
        /// </summary>
        PRED ConvertFromCharSet(BDD set);

        /// <summary>
        /// Compute the number of elements in the set
        /// </summary>
        ulong ComputeDomainSize(PRED set);

        /// <summary>
        /// Enumerate all characters in the set
        /// </summary>
        /// <param name="set">given set</param>
        IEnumerable<char> GenerateAllCharacters(PRED set);

        /// <summary>
        /// Convert a predicate into a set of characters.
        /// </summary>
        BDD ConvertToCharSet(IBDDAlgebra solver, PRED pred);

        /// <summary>
        /// Gets the underlying character set solver.
        /// </summary>
        CharSetSolver CharSetProvider { get; }

        /// <summary>
        /// If named definitions are possible, 
        /// makes a named definition of pred, as a unary relation symbol, 
        /// such that, for all x, name(x) holds iff body(x) holds. Returns the 
        /// atom name(x) that is equivalent to pred(x).
        /// If named definitions are not supported, returns pred.
        /// </summary>
        PRED MkCharPredicate(string name, PRED pred);

        /// <summary>
        /// Makes a singleton set consisting of e.
        /// </summary>
        PRED MkSet(uint e);

        /// <summary>
        /// Chooses a random member of a nonempty set s.
        /// </summary>
        uint Choose(PRED s);

        /// <summary>
        /// Chooses a random character uniformly at random from a nonempty set s.
        /// </summary>
        char ChooseUniformly(PRED s);

        /// <summary>
        /// Returns a partition of the full domain.
        /// </summary>
        PRED[] GetPartition();

        /// <summary>
        /// Serialize the predicate using characters in [0-9a-f\-\.]
        /// </summary>
        /// <param name="s">given predicate</param>
        string SerializePredicate(PRED s);

        /// <summary>
        /// Deserialize the predicate from a string constructed with Serialize
        /// </summary>
        /// <param name="s">given serialized predicate</param>
        PRED DeserializePredicate(string s);
    }
}

