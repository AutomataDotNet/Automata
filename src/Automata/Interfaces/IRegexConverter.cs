using System;
using System.Text.RegularExpressions;

namespace Microsoft.Automata
{
    /// <summary>
    /// Provides functionality to convert regex patterns to corresponding symbolic finite automata.
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
    }

}
