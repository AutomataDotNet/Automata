using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;

namespace Microsoft.Automata
{
    /// <summary>
    /// Provides extension methods for the System.Text.RegularExpressions.Regex class
    /// </summary>
    public static class RegexExtensionMethods
    {
        internal static CharSetSolver context = null;

        /// <summary>
        /// Context is a static field that is reused by the Compile and TryCompile methods and is shared accross regexes.
        /// To forget the context set its value to null.
        /// </summary>
        public static CharSetSolver Context 
        {
            get
            {
                return context;
            }
            set
            {
                context = value;
            }
        }

        /// <summary>
        /// Sets the value of the static Context field to null and allows the solver to be garbage collected.
        /// </summary>
        public static void ResetContext(this Regex regex)
        {
            context = null;
        }

        /// <summary>
        /// Compiles this regex and possibly other regexes into a common symbolic regex representing their intersection
        /// </summary>
        /// <param name="regex">this regex</param>
        /// <param name="regexes">more regexes to intersect with</param>
        /// <returns></returns>
        public static RegexMatcher Compile(this Regex regex, params Regex[] regexes)
        {
            if (regex.ToString() == "")
                throw new AutomataException(AutomataExceptionKind.InvalidRegex);
            return Compile(regex, true, true, false, regexes);
        }

        /// <summary>
        /// Compiles this regex and possibly other regexes into a common symbolic regex representing their intersection
        /// </summary>
        /// <param name="regex">this regex</param>
        /// <param name="regexes">more regexes to intersect with</param>
        /// <param name="keepAnchors">if false missing anchors are replaced by .* else just omitted</param>
        /// <param name="unwindLowerBounds">if true then lower bounds of loops are unwound</param>
        /// <returns></returns>
        public static RegexMatcher Compile(this Regex regex, bool keepAnchors, bool unwindLowerBounds, bool isMatchOnly = false, params Regex[] regexes)
        {
            //first test if this regex is a simple string, i.e., a toplevel multi-node
            RegexTree rt = RegexParser.Parse(regex.ToString(), regex.Options);
            //if (regexes.Length == 0)
            //{
            //    if (rt._root._type == RegexNode.Capture && rt._root.Child(0)._type == RegexNode.Multi)
            //    {
            //        //this is an explicit string
            //        var pattern = rt._root.Child(0)._str;
            //        return new FixedStringMatcher(pattern, (regex.Options & RegexOptions.IgnoreCase) == RegexOptions.IgnoreCase);
            //    }
            //}

            if (context == null)
                context = new CharSetSolver();

            var first = context.RegexConverter.ConvertToSymbolicRegex(rt._root, keepAnchors, unwindLowerBounds);

            if (!isMatchOnly && first.CheckIfContainsLazyLoop() && !first.CheckIfAllLoopsAreLazy())
                throw new AutomataException("Match generation with mixed lazy and eager loops currently not supported.");

            var others = Array.ConvertAll(regexes, r => context.RegexConverter.ConvertToSymbolicRegex(r, keepAnchors, unwindLowerBounds));
            var all = new SymbolicRegexNode<BDD>[1 + regexes.Length];
            all[0] = first;
            for (int i = 1; i <= others.Length; i++)
                all[i] = others[i - 1];
            var srBuilder = context.RegexConverter.srBuilder;
            var conj = srBuilder.MkAnd(all);
            var partition = conj.ComputeMinterms();
            RegexMatcher matcher;
            if (partition.Length > 64)
            {
                //more than 64 bits needed to represent a set
                matcher = new SymbolicRegexBV(conj, context, partition);
            }
            else
            {
                //enough to use 64 bits
                matcher = new SymbolicRegexUInt64(conj, context, partition);
            }
            return matcher;
        }

        /// <summary>
        /// Tries to compile a regex into a symbolic regex
        /// </summary>
        /// <param name="regex">given regex</param>
        /// <param name="result">if the return value is true then this is the result of compilation</param>
        /// <param name="whyfailed">if the return value is false then this is the reason why compilation failed</param>
        /// <param name="regexes">other regexes to be intersected with given regex</param>
        public static bool TryCompile(this Regex regex,  out RegexMatcher result, out string whyfailed, params Regex[] regexes)
        {
            try
            {
                result = Compile(regex, regexes);
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
            if (context == null)
                context = new CharSetSolver();
            try
            {
                var sr_bdd = context.RegexConverter.ConvertToSymbolicRegex(regex, true);
                whynot = "";
                return true;
            }
            catch (AutomataException e)
            {
                whynot = e.Message;
                return false;
            }
        }

        internal static SymbolicRegexNode<BDD> ConvertToSymbolicRegexBDD(this Regex regex, CharSetSolver css, bool simplify = true)
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

            var sampler = new SymbolicRegexSampler<BDD>(solver.RegexConverter.srBuilder, sr, maxUnroll, cornerCaseProb);
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

            var sampler = new SymbolicRegexSampler<BDD>(solver.RegexConverter.srBuilder, sr, maxUnroll, cornerCaseProb);
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
            return new Regex(pattern, regex.Options);
        }
    }
}
