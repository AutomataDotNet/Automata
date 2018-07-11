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
        /// Copmiles a regex into a symbolic regex
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="css">given solver, if null a new one is created</param>
        /// <param name="simplify">if true then lower loop bounds are unwound (default is true)</param>
        /// <returns></returns>
        public static SymbolicRegex<BV> Compile(this Regex regex, CharSetSolver css = null, bool simplify = true)
        {
            if (css == null)
                css = new CharSetSolver();
            var sr_bdd = css.RegexConverter.ConvertToSymbolicRegex(regex, true);
            BVAlgebra bva = new BVAlgebra(css, sr_bdd.ComputeMinterms());
            SymbolicRegexBuilder<BV> builder = new SymbolicRegexBuilder<BV>(bva);
            var sr_bv = sr_bdd.builder.Transform<BV>(sr_bdd, builder, builder.solver.ConvertFromCharSet);
            if (simplify)
                sr_bv = sr_bv.Simplify();
            sr_bv.InitializeMatcher();
            return sr_bv;
        }

        /// <summary>
        /// Tries to compile a regex into a symbolic regex
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="result">if the return value is true then this is the result of compilation</param>
        /// <param name="whyfailed">if the return value is false then this is the reason why compilation failed</param>
        /// <param name="css">given solver, if null a new one is created</param>
        /// <param name="simplify">if true then lower loop bounds are unwound (default is true)</param>
        public static bool TryCompile(this Regex regex, out SymbolicRegex<BV> result, out string whyfailed, CharSetSolver css = null, bool simplify = true)
        {
            if (css == null)
                css = new CharSetSolver();
            try
            {
                result = Compile(regex, css, simplify);
                whyfailed = "";
                return true;
            }
            catch (AutomataException e)
            {
                result = null;
                whyfailed = e.Message;
                return false;
            }
        }

        /// <summary>
        /// Returns true if the regex can be compiled into a symbolic regex.
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="whynot">if the return value is false, reason why compilation is not supported</param>
        /// <returns></returns>
        public static bool IsCompileSupported(this Regex regex, out string whynot)
        {
            var css = new CharSetSolver();
            try
            {
                var sr_bdd = css.RegexConverter.ConvertToSymbolicRegex(regex, true);
                whynot = "";
                return true;
            }
            catch (AutomataException e)
            {
                whynot = e.Message;
                return false;
            }
            finally
            {
                css.Dispose();
            }
        }

        internal static SymbolicRegex<BDD> ConvertToSymbolicRegexBDD(this Regex regex, CharSetSolver css, bool simplify = true)
        {
            var sr_bdd = css.RegexConverter.ConvertToSymbolicRegex(regex, true);
            if (simplify)
                sr_bdd = sr_bdd.Simplify();
            return sr_bdd;
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

        /// <summary>
        /// Generate a random member accepted by the regex
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="maxUnroll">maximum nr of times a loop is unrolled</param>
        /// <param name="cornerCaseProb">inverse of pobability of taking a corner case (lower/upper bound) of the number of iterations a loop may be unrolled.</param>
        /// <param name="charClassRestriction">restrict all generated members to this character class (null means no restriction)</param>
        public static string GenerateRandomMember(this Regex regex, string charClassRestriction = null, int maxUnroll = 10, int cornerCaseProb = 5)
        {
            var solver = new CharSetSolver();
            var sr = solver.RegexConverter.ConvertToSymbolicRegex(regex);
            if (charClassRestriction != null)
                sr = sr.Restrict(solver.MkCharSetFromRegexCharClass(charClassRestriction));

            var sampler = new SymbolicRegexSampler<BDD>(sr, maxUnroll, cornerCaseProb);
            return sampler.GenerateRandomMember();
        }

        /// <summary>
        /// Generate a dataset of random members accepted by the regex
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="size">number of members</param>
        /// <param name="maxUnroll">maximum nr of times a loop is unrolled</param>
        /// <param name="cornerCaseProb">inverse of pobability of taking a corner case (lower/upper bound) of the number of iterations a loop may be unrolled.</param>
        /// <param name="charClassRestriction">restrict all generated members to this character class (null means no restriction)</param>
        /// <param name="maxSamplingIter">Maximum number of iterations in order to collect the requested number of samples</param>
        public static HashSet<string> GenerateRandomDataSet(this Regex regex, int size = 10, string charClassRestriction = null, int maxUnroll = 10, int cornerCaseProb = 5, int maxSamplingIter = 3)
        {
            var solver = new CharSetSolver();
            var sr = solver.RegexConverter.ConvertToSymbolicRegex(regex);
            if (charClassRestriction != null)
                sr = sr.Restrict(solver.MkCharSetFromRegexCharClass(charClassRestriction));

            var sampler = new SymbolicRegexSampler<BDD>(sr, maxUnroll, cornerCaseProb);
            return sampler.GetPositiveDataset(size);
        }

        /// <summary>
        /// Returns a regex that accepts the complement of this regex.
        /// </summary>
        /// <param name="regex">this regex</param>
        /// <param name="timeout">timeout to complement in ms</param>
        /// <returns></returns>
        public static Regex Complement(this Regex regex, int timeout = 10000)
        {
            var solver = new CharSetSolver();
            var aut = solver.Convert(regex.ToString(), regex.Options).RemoveEpsilons();
            var aut_det = aut.Determinize(timeout);
            var aut_min_c = aut_det.Minimize().Complement();
            var pattern = solver.ConvertToRegex(aut_min_c);
            return new Regex(pattern);
        }
    }
}
