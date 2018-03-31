using System;
using System.Text.RegularExpressions;

namespace Microsoft.Automata
{
    /// <summary>
    /// Provides functionality to convert regex patterns to corresponding symbolic finite automata
    /// and symbolic regexes.
    /// </summary>
    public interface IRegexConverter<S>
    {
        /// <summary>
        /// Converts a regex pattern into an eqivalent symbolic automaton.
        /// Same as Convert(regex, RegexOptions.None).
        /// </summary>
        /// <param name="regex">the given regex pattern</param>
        Automaton<S> Convert(string regex);

        /// <summary>
        /// Converts a regex pattern into an equivalent symbolic automaton
        /// </summary>
        /// <param name="regex">the given regex pattern</param>
        /// <param name="options">regular expression options for the pattern</param>
        Automaton<S> Convert(string regex, RegexOptions options);

        /// <summary>
        /// Converts a string s into an automaton that accepts s and no other strings.
        /// </summary>
        /// <param name="s">given input strings</param>
        Automaton<S> ConvertString(string s);

        /// <summary>
        /// Convert given regex to a symbolic regex AST
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="options">regex options</param>
        SymbolicRegex<S> ConvertToSymbolicRegex(string regex, RegexOptions options = RegexOptions.None);

        /// <summary>
        /// Convert a set into a singleton regex
        /// </summary>
        /// <param name="set">given set</param>
        SymbolicRegex<S> MkSingleton(S set);

        /// <summary>
        /// Make a choice regex of given regexes
        /// </summary>
        SymbolicRegex<S> MkOr(params SymbolicRegex<S>[] regexes);

        /// <summary>
        /// Make a concatenation of given regexes
        /// </summary>
        SymbolicRegex<S> MkConcat(params SymbolicRegex<S>[] regexes);

        /// <summary>
        /// Make regex that accepts the empty word
        /// </summary>
        SymbolicRegex<S> MkEpsilon();

        /// <summary>
        /// Make loop regex
        /// </summary>
        SymbolicRegex<S> MkLoop(SymbolicRegex<S> regex, int lower = 0, int upper = int.MaxValue);
    }

}
