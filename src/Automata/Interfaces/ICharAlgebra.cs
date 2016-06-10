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
        /// Try to convert a predicate into a set of characters.
        /// </summary>
        bool TryConvertToCharSet(PRED pred, out BDD set);

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
        /// Chooses a member of a nonempty set s.
        /// </summary>
        uint Choose(PRED s);
    }
}

