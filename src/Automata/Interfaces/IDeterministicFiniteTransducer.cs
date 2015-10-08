using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Deterministic finite state transducer.
    /// </summary>
    public interface IDeterministicFiniteTransducer
    {
        /// <summary>
        /// All states.
        /// </summary>
        ICollection<int> Q { get; }

        /// <summary>
        /// Initial state.
        /// </summary>
        int q0 { get; }

        /// <summary>
        /// All final states.
        /// </summary>
        ICollection<int> F { get; }

        /// <summary>
        /// Alphabet.
        /// </summary>
        IEnumerable<char> Sigma { get; }

        /// <summary>
        /// Transition function. Assumed to be a total function.
        /// </summary>
        /// <param name="state">source state</param>
        /// <param name="c">input character (negative value means end of string)</param>
        /// <returns>target state</returns>
        int Delta(int state, int c);

        /// <summary>
        /// Output function. Assumed to be a total function.
        /// </summary>
        /// <param name="state">source state</param>
        /// <param name="c">input character (negative value means end of string)</param>
        /// <returns>output sequence</returns>
        IEnumerable<char> Psi(int state, int c);
    }
}
