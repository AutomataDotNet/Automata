using System;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    /// <summary>
    /// For accessing the key components of an automaton.
    /// </summary>
    /// <typeparam name="L">type of labels in moves</typeparam>
    public interface IAutomaton<L>
    {
        /// <summary>
        /// The initial state of the automaton.
        /// </summary>
        int InitialState { get; }

        /// <summary>
        /// Returns true iff the state is a final state.
        /// </summary>
        bool IsFinalState(int state);

        /// <summary>
        /// Enumerates all moves of the automaton.
        /// </summary>
        IEnumerable<Move<L>> GetMoves();

        /// <summary>
        /// Enumerates all states of the automaton.
        /// </summary>
        IEnumerable<int> GetStates();

        /// <summary>
        /// Provides a description of the state for visualization purposes.
        /// </summary>
        string DescribeState(int state);

        /// <summary>
        /// Provides a description of the label for visualization purposes.
        /// </summary>
        string DescribeLabel(L lab);

        /// <summary>
        /// Provides a description of the label for visualization purposes.
        /// </summary>
        string DescribeStartLabel();

        /// <summary>
        /// Gets the algebra of the labels.
        /// </summary>
        IBooleanAlgebra<L> Algebra { get; }
    }
}
