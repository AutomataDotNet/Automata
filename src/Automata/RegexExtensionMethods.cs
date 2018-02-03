using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    /// <summary>
    /// Provides extension methods for the System.Text.RegularExpressions.Regex class
    /// </summary>
    public static class RegexExtensionMethods
    {
        /// <summary>
        /// Attempts to compile the regex to the given automaton interface or returns null if the compilation attempt fails.
        /// </summary>
        /// <param name="regex">the regex</param>
        /// <param name="timeout">given compilation timeout in ms</param>
        public static ICompiledStringMatcher Compile(this Regex regex, int timeout = 0)
        {
            var brexman = new BREXManager("Matcher", "FString", 0, timeout);
            var brex = brexman.MkRegex(regex.ToString(), regex.Options);
            if (!brex.CanBeOptimized())
                return null;

            var aut = brex.Optimize();
            return aut.Compile();
        }

        /// <summary>
        /// Display the automaton of the regex in dgml.
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="name">name for the automaton and the dgml file</param>
        /// <param name="minimize">minimize (and determinize) if true</param>
        /// <param name="determinize">determinize if true</param>
        /// <param name="removeepsilons">remove epsilon moves if true</param>
        public static void Display(this Regex regex, string name = "Automaton", bool minimize = false, bool determinize = false, bool removeepsilons = false)
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            var aut = solver.Convert(regex.ToString(), regex.Options);
            if (removeepsilons)
            {
                aut = aut.RemoveEpsilons().Normalize();
            }
            if (determinize)
            {
                aut = aut.RemoveEpsilons();
                aut = aut.Determinize().Normalize();
            }
            if (minimize)
            {
                aut = aut.RemoveEpsilons();
                aut = aut.Determinize();
                aut = aut.Minimize().Normalize();
            }
            aut.ShowGraph(name);
        }

        /// <summary>
        /// Generate c++ matcher code for the given regex. Returns the empty string if the compilation fails.
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="timeout">timeout in ms, 0 means no timeout and is the default</param>
        public static string GenerateCpp(this Regex regex, int timeout = 0)
        {
            var brexman = new BREXManager("Matcher", "FString", 0, timeout);
            var brex = brexman.MkRegex(regex.ToString(), regex.Options);
            if (!brex.CanBeOptimized())
                return string.Empty;
            brexman.AddBoolRegExp(brex);
            var cpp = brexman.GenerateCpp();
            return cpp;
        }
    }
}
