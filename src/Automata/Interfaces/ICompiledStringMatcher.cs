using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Microsoft.Automata
{
    /// <summary>
    /// Provides source code, IsMatch method, and IFiniteAutomaton interface of the source code.
    /// </summary>
    public interface ICompiledStringMatcher
    {
        /// <summary>
        /// Source code implementing the matcher.
        /// </summary>
        string SourceCode { get; }

        /// <summary>
        /// Returns true iff the automaton accepts the input string. 
        /// Semantically equivalent to Automaton.IsFinalState(Automaton.Transition(0, input.ToCharArray())).
        /// </summary>
        bool IsMatch(string input);

        /// <summary>
        /// Incremental automaton interface to the compiled source code.
        /// </summary>
        IFiniteAutomaton Automaton { get; }
    }

    /// <summary>
    /// Represents a compiled finite state automaton interface. Initial state is 0.
    /// </summary>
    public interface IFiniteAutomaton
    {
        /// <summary>
        /// Returns true iff q is a final state.
        /// </summary>
        bool IsFinalState(int q);

        /// <summary>
        /// Returns true iff q loops for all characters.
        /// </summary>
        bool IsSinkState(int q);

        /// <summary>
        /// Returns the target state reached after reading the input characters from the source state q.
        /// </summary>
        int Transition(int q, params char[] input);
    }

    /// <summary>
    /// Extends the IFiniteAutomaton interface, exposes the transition function
    /// </summary>
    public interface IDeterministicFiniteAutomaton : IFiniteAutomaton
    {
        /// <summary>
        /// The transition function. 
        /// The set of states is {0,...,Delta.Length-1}.
        /// </summary>
        Func<char, int>[] Delta { get; }
    }
}
