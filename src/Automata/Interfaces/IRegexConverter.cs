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

        ///// <summary>
        ///// Convert given regex to a symbolic regex AST
        ///// </summary>
        ///// <param name="regex">given regex</param>
        ///// <param name="options">regex options</param>
        ///// <param name="keepAnchors">if true then start and end anchors are not eliminated (default is false)</param>
        //SymbolicRegex<S> ConvertToSymbolicRegex(string regex, RegexOptions options = RegexOptions.None, bool keepAnchors = false);

        ///// <summary>
        ///// Convert given regex to a symbolic regex AST
        ///// </summary>
        ///// <param name="regex">given .NET regex</param>
        ///// <param name="keepAnchors">if true then start and end anchors are not eliminated, else (default) anchors are eliminated and replaced by equivalent regexes and resulting symbolic regex will have implicit start and end anchors</param>
        //SymbolicRegex<S> ConvertToSymbolicRegex(Regex regex, bool keepAnchors = false);

        ///// <summary>
        ///// Convert a set into a singleton regex
        ///// </summary>
        ///// <param name="set">given set</param>
        //SymbolicRegex<S> MkSingleton(S set);

        ///// <summary>
        ///// Make a choice regex of given regexes
        ///// </summary>
        //SymbolicRegex<S> MkOr(params SymbolicRegexNode<S>[] regexes);

        ///// <summary>
        ///// Make a concatenation of given regexes
        ///// </summary>
        //SymbolicRegex<S> MkConcat(params SymbolicRegexNode<S>[] regexes);

        ///// <summary>
        ///// Make regex that accepts the empty word
        ///// </summary>
        //SymbolicRegex<S> MkEpsilon();

        ///// <summary>
        ///// Make a loop with lower and upper bound, 
        ///// lower=0 and upper=int.MaxValue means (...)*, 
        ///// lower=1 and upper=int.MaxValue means (...)+,
        ///// lower=0 and upper=1 means (...)?
        ///// </summary>
        //SymbolicRegex<S> MkLoop(SymbolicRegex<S> regex, int lower = 0, int upper = int.MaxValue);

        ///// <summary>
        ///// Make start anchor
        ///// </summary>
        //SymbolicRegex<S> MkStartAnchor();

        ///// <summary>
        ///// Make end anchor
        ///// </summary>
        //SymbolicRegex<S> MkEndAnchor();

        ///// <summary>
        ///// Gets the builder of derivatives
        ///// </summary>
        //SymbolicRegexBuilder<S> SRBuilder { get; }
    }

}
