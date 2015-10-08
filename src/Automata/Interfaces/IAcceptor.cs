using System;

namespace Microsoft.Automata
{
    /// <summary>
    /// Methods for asserting the axiomatic theory and accessing the language acceptor of an SFA or the transduction acceptor of an ST.
    /// </summary>
    /// <typeparam name="FUNC">function declarations, each function declaration has domain and range sorts</typeparam>
    /// <typeparam name="TERM">terms, each term has a fixed sort</typeparam>
    /// <typeparam name="SORT">sorts correspond to different subuniverses of elements</typeparam>
    public interface IAcceptor<FUNC, TERM, SORT>
    {
        /// <summary>
        /// The given SMT solver. 
        /// </summary>
        IContext<FUNC, TERM, SORT> Solver { get; }

        /// <summary>
        /// Assert the theory as an auxiliary background theory to the given SMT solver
        /// </summary>
        void AssertTheory();

        /// <summary>
        /// Relation symbol of the language or transduction relation. 
        /// </summary>
        FUNC Acceptor { get; }
    }
}
