using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Serialization;
using System.IO;

namespace Microsoft.Automata
{
    /// <summary>
    /// Provides IsMatch and Matches methods and extends ISerializable.
    /// </summary>
    public interface IMatcher : ISerializable
    {
        /// <summary>
        /// Returns true iff the input string matches. 
        /// <param name="input">given iput string</param>
        /// <param name="startat">start position in the input, default is 0</param>
        /// <param name="endat">end position in the input, -1 means that the value is unspecified and taken to be input.Length-1</param>
        /// </summary>
        bool IsMatch(string input, int startat = 0, int endat = -1);

        /// <summary>
        /// Returns all matches as pairs (startindex, length) in the input string.
        /// </summary>
        /// <param name="input">given iput string</param>
        /// <param name="limit">as soon as this many matches have been found the search terminates, 0 or negative value means that there is no bound, default is 0</param>
        /// <param name="startat">start position in the input, default is 0</param>
        /// <param name="endat">end position in the input, -1 means that the value is unspecified and taken to be input.Length-1</param>
        Tuple<int, int>[] Matches(string input, int limit = 0, int startat = 0, int endat = -1);

        /// <summary>
        /// Generate a random string that is a complete match.
        /// </summary>
        string GenerateRandomMatch(int maxLoopUnrol = 10, int cornerCaseProb = 5, int maxSamplingIter = 3);

        /// <summary>
        /// Serialize this symbolic regex matcher to the given stream.
        /// If formatter is null then an instance of 
        /// System.Runtime.Serialization.Formatters.Binary.BinaryFormatter is used.
        /// </summary>
        /// <param name="stream">stream where the serialization is stored</param>
        /// <param name="formatter">given formatter</param>
        void Serialize(Stream stream, IFormatter formatter = null);

        /// <summary>
        /// Serialize this symbolic regex matcher to the given file.
        /// If formatter is null then an instance of 
        /// System.Runtime.Serialization.Formatters.Binary.BinaryFormatter is used.
        /// </summary>
        /// <param name="file">file where the serialization is stored</param>
        /// <param name="formatter">given formatter</param>
        void Serialize(string file, IFormatter formatter = null);
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

        /// <summary>
        /// Returns true iff the automaton accepts the input
        /// </summary>
        bool IsMatch(string input);
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

        /// <summary>
        /// Returns the number of matches, and fills in the array with those matches (startindex, length) 
        /// </summary>
        int GenerateMatches(string input, Tuple<int,int>[] mathces);
    }
}
