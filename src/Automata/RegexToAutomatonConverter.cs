using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Automata;

namespace Microsoft.Automata
{
    /// <summary>
    /// Provides functionality to convert .NET regex patterns to corresponding symbolic finite automata and symbolic regexes
    /// </summary>
    public class RegexToAutomatonConverter<S> : IRegexConverter<S>
    {
        ICharAlgebra<S> solver;
        internal IUnicodeCategoryTheory<S> categorizer;
        //records friendly descriptions of conditions for visualization purposes
        internal Dictionary<S, string> description = new Dictionary<S, string>();

        RegexToAutomatonBuilder<RegexNode, S> automBuilder;

        internal SymbolicRegexBuilder<S> srBuilder;

        /// <summary>
        /// The character solver associated with the regex converter
        /// </summary>
        public ICharAlgebra<S> Solver
        {
            get
            {
                return solver;
            }
        }


        //public SymbolicRegexBuilder<S> SRBuilder
        //{
        //    get
        //    {
        //        return srBuilder;
        //    }
        //}

        /// <summary>
        /// Constructs a regex to symbolic finite automata converter
        /// </summary>
        /// <param name="solver">solver for character constraints</param>
        /// <param name="categorizer">maps unicode categories to corresponding character conditions</param>
        internal RegexToAutomatonConverter(ICharAlgebra<S> solver, IUnicodeCategoryTheory<S> categorizer = null)
        {
            this.solver = solver;
            this.categorizer = (categorizer == null ? new UnicodeCategoryTheory<S>(solver) : categorizer);
            description.Add(solver.True, ".");
            //"[]" does not unfortunately parse as a valid regex
            //description.Add(solver.False, "[0-[0]]");
            description.Add(solver.False, "[]");
            this.automBuilder = new RegexToAutomatonBuilder<RegexNode, S>(solver, ConvertNode);
            this.srBuilder = new SymbolicRegexBuilder<S>((ICharAlgebra<S>)solver);
            //this.converterHelper.Callback = (node, start, end) => ConvertNode(node, start, end);
        }

        /// <summary>
        /// Converts a .NET regex pattern into an eqivalent symbolic automaton.
        /// Same as Convert(regex, RegexOptions.None).
        /// </summary>
        public Automaton<S> Convert(string regex)
        {
            return Convert(regex, RegexOptions.None);
        }

        /// <summary>
        /// Converts a single string into an automaton that accepts that string and no other strings.
        /// </summary>
        public Automaton<S> ConvertString(string symbol)
        {
            List<Move<S>> moves = new List<Move<S>>();
            for (int i = 0; i < symbol.Length; i++)
                moves.Add(Move<S>.Create(i, i + 1, solver.MkCharConstraint(symbol[i])));
            Automaton<S> autom = Automaton<S>.Create(solver, 0, new int[] { symbol.Length }, moves);
            return autom;
        }

        /// <summary>
        /// Convert a regex pattern to an equivalent symbolic finite automaton
        /// </summary>
        /// <param name="regex">the given .NET regex pattern</param>
        /// <param name="options">regular expression options for the pattern</param>
        public Automaton<S> Convert(string regex, RegexOptions options = RegexOptions.None)
        {
            return Convert(regex, options, false);
        }

        /// <summary>
        /// Convert a regex pattern to an equivalent symbolic finite automaton
        /// </summary>
        /// <param name="regex">the given .NET regex pattern</param>
        /// <param name="options">regular expression options for the pattern</param>
        /// <param name="keepBoundaryStates">used for testing purposes, when true boundary states are not eliminated</param>
        public Automaton<S> Convert(string regex, RegexOptions options, bool keepBoundaryStates)
        {
            automBuilder.Reset();
            //filter out the RightToLeft option that turns around the parse tree
            //but has no semantical meaning regarding the regex
            var options1 = (options & ~RegexOptions.RightToLeft);

            RegexTree tree = RegexParser.Parse(regex, options1);
            var aut = ConvertNode(tree._root);
            //delay accessing the condition
            Func<bool, S> getWordLetterCondition = (b => categorizer.WordLetterCondition);
            if (!keepBoundaryStates)
                aut.EliminateWordBoundaries(getWordLetterCondition);
            return aut;
        }

        internal Tuple<string,Automaton<S>>[] ConvertCaptures(string regex, out bool isLoop)
        {
            //automBuilder.Reset();
            automBuilder.isBeg = false;
            automBuilder.isEnd = false;
            var options = RegexOptions.Singleline | RegexOptions.ExplicitCapture;
            RegexTree tree = RegexParser.Parse(regex, options);
            List<Tuple<string, Automaton<S>>> automata = new List<Tuple<string, Automaton<S>>>();
            //delay accessing the condition
            Func<bool, S> getWordLetterCondition = (b => categorizer.WordLetterCondition);
            var rootnode = tree._root._children[0];
            isLoop = (rootnode._type == RegexNode.Loop);
            if (isLoop)
                rootnode = rootnode._children[0];
            foreach (var aut in ConvertCaptures(rootnode, id => tree._capslist[id]))
            {
                aut.Item2.EliminateWordBoundaries(getWordLetterCondition);
                automata.Add(aut);
            }
            return automata.ToArray();
        }

        /// <summary>
        /// Must be called if aut was created with keepBoundaryStates=true.
        /// </summary>
        public void EliminateBoundaryStates(Automaton<S> aut)
        {
            Func<bool, S> getWordLetterCondition = (b => categorizer.WordLetterCondition);
            aut.EliminateWordBoundaries(getWordLetterCondition);
        }

        static string DescribeRegexNodeType(int node_type)
        {
            switch (node_type)
            {
                case RegexNode.Alternate:
                    return "Alternate";
                case RegexNode.Beginning:
                    return "Beginning";
                case RegexNode.Bol:
                    return "Bol";
                case RegexNode.Capture:  // (?<name> regex)
                    return "Capture";
                case RegexNode.Concatenate:
                    return "Concatenate";
                case RegexNode.Empty:
                    return "Empty";
                case RegexNode.End:
                    return "End";
                case RegexNode.EndZ:
                    return "EndZ";
                case RegexNode.Eol:
                    return "Eol";
                case RegexNode.Loop:
                    return "Loop";
                case RegexNode.Multi:
                    return "Multi";
                case RegexNode.Notone:
                    return "Notone";
                case RegexNode.Notoneloop:
                    return "Notoneloop";
                case RegexNode.One:
                    return "One";
                case RegexNode.Oneloop:
                    return "Oneloop";
                case RegexNode.Set:
                    return "Set";
                case RegexNode.Setloop:
                    return "Setloop";
                case RegexNode.ECMABoundary:
                    return "ECMABoundary";
                case RegexNode.Boundary:
                    return "Boundary";
                case RegexNode.Nothing:
                    return "Nothing";
                case RegexNode.Nonboundary:
                    return "Nonboundary";
                case RegexNode.NonECMABoundary:
                    return "NonECMABoundary";
                case RegexNode.Greedy:
                    return "Greedy";
                case RegexNode.Group:
                    return "Group";
                case RegexNode.Lazyloop:
                    return "Lazyloop";
                case RegexNode.Prevent:
                    return "Prevent";
                case RegexNode.Require:
                    return "Require";
                case RegexNode.Testgroup:
                    return "Testgroup";
                case RegexNode.Testref:
                    return "Testref";
                case RegexNode.Notonelazy:
                    return "Notonelazy";
                case RegexNode.Onelazy:
                    return "Onelazy";
                case RegexNode.Setlazy:
                    return "Setlazy";
                case RegexNode.Ref:
                    return "Ref";
                case RegexNode.Start:
                    return "Start";
                default:
                    throw new AutomataException(AutomataExceptionKind.UnrecognizedRegex);
            }
        }

        internal Tuple<string,Automaton<S>>[] ConvertCaptures(RegexNode node, Func<int,string> getCaptureName)
        {
            if (node._type == RegexNode.Capture)//single capture
                return new Tuple<string, Automaton<S>>[] { new Tuple<string, Automaton<S>>(getCaptureName(node._m), ConvertNode(node)) };

            if (node._type != RegexNode.Concatenate)
                return new Tuple<string,Automaton<S>>[]{new Tuple<string,Automaton<S>>("",ConvertNode(node))};

            List<Tuple<string,Automaton<S>>> res = new List<Tuple<string,Automaton<S>>>();
            List<RegexNode> between_captures = new List<RegexNode>();
            foreach (var n in node._children) 
            {
                if (n._type != RegexNode.Capture)
                    between_captures.Add(n);
                else
                {
                    if (between_captures.Count > 0)
                    {
                        var aut = this.automBuilder.MkConcatenate(between_captures.ToArray());
                        res.Add(new Tuple<string, Automaton<S>>("",aut));
                        between_captures.Clear();
                    }
                    var capture = ConvertNode(n);
                    string capture_name = getCaptureName(n._m);
                    res.Add(new Tuple<string, Automaton<S>>(capture_name, capture));
                }
            }
            if (between_captures.Count > 0)
            {
                var aut = this.automBuilder.MkConcatenate(between_captures.ToArray());
                res.Add(new Tuple<string, Automaton<S>>("", aut));
                between_captures.Clear();
            }
            return res.ToArray();
        }

        internal Automaton<S> ConvertNode(RegexNode node)
        {
            //node = node.Reduce();
            switch (node._type)
            {
                case RegexNode.Alternate:
                    return this.automBuilder.MkUnion(node._children.ToArray());
                case RegexNode.Beginning: 
                    return this.automBuilder.MkBeginning();
                case RegexNode.Bol:
                    return this.automBuilder.MkBol(solver.MkCharConstraint('\n'));
                case RegexNode.Capture:  //paranthesis (...)
                    return ConvertNode(node.Child(0));
                case RegexNode.Concatenate:
                    return this.automBuilder.MkConcatenate(node._children.ToArray());
                case RegexNode.Empty:
                    return this.automBuilder.MkEmptyWord();
                case RegexNode.End:
                case RegexNode.EndZ: 
                    return this.automBuilder.MkEnd();
                case RegexNode.Eol: 
                    return this.automBuilder.MkEol(solver.MkCharConstraint('\n'));
                case RegexNode.Lazyloop:
                case RegexNode.Loop: 
                    return automBuilder.MkLoop(node._children[0], node._m, node._n); 
                case RegexNode.Multi: 
                    return ConvertNodeMulti(node);
                case RegexNode.Notonelazy:
                case RegexNode.Notone: 
                    return ConvertNodeNotone(node);
                case RegexNode.Notoneloop: 
                    return ConvertNodeNotoneloop(node);
                case RegexNode.Onelazy:
                case RegexNode.One: 
                    return ConvertNodeOne(node);
                case RegexNode.Oneloop: 
                    return ConvertNodeOneloop(node);
                case RegexNode.Setlazy:
                case RegexNode.Set:
                    return ConvertNodeSet(node);
                case RegexNode.Setloop: 
                    return ConvertNodeSetloop(node);
                case RegexNode.ECMABoundary:
                case RegexNode.Boundary:
                    return automBuilder.MkWordBoundary();
                case RegexNode.Nothing:
                    return automBuilder.MkEmptyAutomaton();
                //currently not supported cases
                //case RegexNode.Lazyloop:
                    //throw new AutomataException("Regex construct not supported: lazy constructs *? +? ?? {,}?");
                //case RegexNode.Notonelazy:
                //    throw new AutomataException("Regex construct not supported: lazy construct .*?");
                //case RegexNode.Onelazy:
                //    throw new AutomataException("Regex construct not supported: lazy construct a*?");
                //case RegexNode.Setlazy:
                //    throw new AutomataException(@"Regex construct not supported: lazy construct \d*?");
                case RegexNode.Nonboundary:
                case RegexNode.NonECMABoundary:
                    throw new AutomataException(@"Regex construct not supported: \B");
                case RegexNode.Greedy:
                    throw new AutomataException("Regex construct not supported: greedy constructs (?>) (?<)");
                case RegexNode.Group:
                    throw new AutomataException("Regex construct not supported: grouping (?:)");
                case RegexNode.Prevent:
                    throw new AutomataException("Regex construct not supported: prevent constructs (?!) (?<!)");
                case RegexNode.Require:
                    throw new AutomataException("Regex construct not supported: require constructs (?=) (?<=)"); 
                case RegexNode.Testgroup:
                    throw new AutomataException("Regex construct not supported: test construct (?(...) | )");
                case RegexNode.Testref:
                    throw new AutomataException("Regex construct not supported: test cosntruct (?(n) | )");
                case RegexNode.Ref:
                    throw new AutomataException(@"Regex construct not supported: references \1");
                case RegexNode.Start:
                    throw new AutomataException(@"Regex construct not supported: \G");
                default:
                    throw new AutomataException(AutomataExceptionKind.UnrecognizedRegex);
            }
        }

        #region Character sequences
        /// <summary>
        /// Sequence of characters in node._str
        /// </summary>
        private Automaton<S> ConvertNodeMulti(RegexNode node)
        {
            //sequence of characters
            string sequence = node._str;
            int count = sequence.Length;
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            S[] conds = new S[count];

            for (int i = 0; i < count; i++)
            {
                List<char[]> ranges = new List<char[]>();
                char c = sequence[i];
                ranges.Add(new char[] { c, c });
                S cond = solver.MkRangesConstraint(ignoreCase, ranges);
                //TBD: fix the following description
                if (!description.ContainsKey(cond))
                    description[cond] = Rex.RexEngine.Escape(c);
                conds[i] = cond;
            }

            return automBuilder.MkSeq(conds);
        }

        /// <summary>
        /// Matches chacter any character except node._ch
        /// </summary>
        private Automaton<S> ConvertNodeNotone(RegexNode node)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            S cond = solver.MkNot(solver.MkCharConstraint(node._ch, ignoreCase));

            if (!description.ContainsKey(cond))
                description[cond] = string.Format("[^{0}]", Rex.RexEngine.Escape(node._ch));

            return automBuilder.MkSeq(cond);
        }

        /// <summary>
        /// Matches only node._ch
        /// </summary>
        private Automaton<S> ConvertNodeOne(RegexNode node)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            S cond = solver.MkCharConstraint(node._ch,ignoreCase);
            if (!description.ContainsKey(cond))
                description[cond] = Rex.RexEngine.Escape(node._ch);

            return automBuilder.MkSeq(cond);
        }

        #endregion

        #region Character sets

        private Automaton<S> ConvertNodeSet(RegexNode node)
        {
            //ranges and categories are encoded in set
            string set = node._str;

            S moveCond = CreateConditionFromSet((node._options & RegexOptions.IgnoreCase) != 0, set);

            if (moveCond.Equals(solver.False))
                return this.automBuilder.empty; //the condition is unsatisfiable

            if (!description.ContainsKey(moveCond))
                description[moveCond] = RegexCharClass.SetDescription(set);

            return automBuilder.MkSeq(moveCond);
        }

        private const int SETLENGTH = 1;
        private const int CATEGORYLENGTH = 2;
        private const int SETSTART = 3;
        private const char Lastchar = '\uFFFF';

        internal S CreateConditionFromSet(bool ignoreCase, string set)
        {
            //char at position 0 is 1 iff the set is negated
            //bool negate = ((int)set[0] == 1);
            bool negate = RegexCharClass.IsNegated(set);

            //following are conditions over characters in the set
            //these will become disjuncts of a single disjunction
            //or conjuncts of a conjunction in case negate is true
            //negation is pushed in when the conditions are created
            List<S> conditions = new List<S>();

            #region ranges
            var ranges = ComputeRanges(set);

            foreach (var range in ranges)
            {
                S cond = solver.MkRangeConstraint(range.Item1, range.Item2, ignoreCase);
                conditions.Add(negate ? solver.MkNot(cond) : cond);
            }
            #endregion

            #region categories
            int setLength = set[SETLENGTH];
            int catLength = set[CATEGORYLENGTH];
            //int myEndPosition = SETSTART + setLength + catLength;

            int catStart = setLength + SETSTART;
            int j = catStart;
            while (j < catStart + catLength)
            {
                //singleton categories are stored as unicode characters whose code is 
                //1 + the unicode category code as a short
                //thus - 1 is applied to exctarct the actual code of the category
                //the category itself may be negated e.g. \D instead of \d
                short catCode = (short)set[j++];
                if (catCode != 0)
                {
                    //note that double negation cancels out the negation of the category
                    S cond = MapCategoryCodeToCondition(Math.Abs(catCode) - 1);
                    conditions.Add(catCode < 0 ^ negate ? solver.MkNot(cond) : cond);
                }
                else
                {
                    //special case for a whole group G of categories surrounded by 0's
                    //essentially 0 C1 C2 ... Cn 0 ==> G = (C1 | C2 | ... | Cn)
                    catCode = (short)set[j++];
                    if (catCode == 0)
                        continue; //empty set of categories

                    //collect individual category codes into this set
                    var catCodes = new HashSet<int>();
                    //if the first catCode is negated, the group as a whole is negated
                    bool negGroup = (catCode < 0);

                    while (catCode != 0)
                    {
                        catCodes.Add(Math.Abs(catCode) - 1);
                        catCode = (short)set[j++];
                    }

                    // C1 | C2 | ... | Cn
                    S catCondDisj = MapCategoryCodeSetToCondition(catCodes);

                    S catGroupCond = (negate ^ negGroup ? solver.MkNot(catCondDisj) : catCondDisj);
                    conditions.Add(catGroupCond);
                }
            }
            #endregion

            #region Subtractor
            S subtractorCond = default(S);
            if (set.Length > j)
            {
                //the set has a subtractor-set at the end
                //all characters in the subtractor-set are excluded from the set
                //note that the subtractor sets may be nested, e.g. in r=[a-z-[b-g-[cd]]]
                //the subtractor set [b-g-[cd]] has itself a subtractor set [cd]
                //thus r is the set of characters between a..z except b,e,f,g
                var subtractor = set.Substring(j);
                subtractorCond = CreateConditionFromSet(ignoreCase, subtractor);
            }

            #endregion

            S moveCond;
            //if there are no ranges and no groups then there are no conditions 
            //this situation arises for SingleLine regegex option and .
            //and means that all characters are accepted
            if (conditions.Count == 0)
                moveCond = (negate ? solver.False : solver.True);
            else
                moveCond = (negate ? solver.MkAnd(conditions) : solver.MkOr(conditions));

            //Subtelty of regex sematics:
            //note that the subtractor is not within the scope of the negation (if there is a negation)
            //thus the negated subtractor is conjuncted with moveCond after the negation has been 
            //performed above
            if (!object.Equals(subtractorCond, default(S)))
            {
                moveCond = solver.MkAnd(moveCond, solver.MkNot(subtractorCond));
            }

            return moveCond;
        }

        private static List<Tuple<char, char>> ComputeRanges(string set)
        {
            int setLength = set[SETLENGTH];

            var ranges = new List<Tuple<char, char>>(setLength);
            int i = SETSTART;
            int end = i + setLength;
            while (i < end)
            {
                char first = set[i];
                i++;

                char last;
                if (i < end)
                    last = (char)(set[i] - 1);
                else
                    last = Lastchar;
                i++;
                ranges.Add(new Tuple<char, char>(first, last));
            }
            return ranges;
        }

        private S MapCategoryCodeSetToCondition(HashSet<int> catCodes)
        {
            //TBD: perhaps other common cases should be specialized similarly 
            //check first if all word character category combinations are covered
            //which is the most common case, then use the combined predicate \w
            //rather than a disjunction of the component category predicates
            //the word character class \w covers categories 0,1,2,3,4,8,18
            S catCond = default(S);
            if (catCodes.Contains(0) && catCodes.Contains(1) && catCodes.Contains(2) && catCodes.Contains(3) &&
                catCodes.Contains(4) && catCodes.Contains(8) && catCodes.Contains(18))
            {
                catCodes.Remove(0);
                catCodes.Remove(1);
                catCodes.Remove(2);
                catCodes.Remove(3);
                catCodes.Remove(4);
                catCodes.Remove(8);
                catCodes.Remove(18);
                catCond = categorizer.WordLetterCondition;
            }
            foreach (var cat in catCodes)
            {
                S cond = MapCategoryCodeToCondition(cat);
                catCond = (object.Equals(catCond, default(S)) ? cond : solver.MkOr(catCond, cond));
            }
            return catCond;
        }

        private S MapCategoryCodeToCondition(int code)
        {
            //whitespace has special code 99
            if (code == 99)
                return categorizer.WhiteSpaceCondition;

            //other codes must be valid UnicodeCategory codes
            if (code < 0 || code > 29)
                throw new ArgumentOutOfRangeException("code", "Must be in the range 0..29 or equal to 99");

            return categorizer.CategoryCondition(code);
        }

        #endregion

        #region Loops

        private Automaton<S> ConvertNodeNotoneloop(RegexNode node)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            S cond = solver.MkNot(solver.MkCharConstraint(node._ch, ignoreCase));
            if (!description.ContainsKey(cond))
                description[cond] = string.Format("[^{0}]", Rex.RexEngine.Escape(node._ch));

            Automaton<S> loop = automBuilder.MkOneLoop(cond, node._m, node._n);
            return loop;
        }

        private Automaton<S> ConvertNodeOneloop(RegexNode node)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            S cond = solver.MkCharConstraint(node._ch, ignoreCase);
            if (!description.ContainsKey(cond))
                description[cond] = string.Format("{0}", Rex.RexEngine.Escape(node._ch));

            Automaton<S> loop = automBuilder.MkOneLoop(cond, node._m, node._n);
            return loop;
        }

        private Automaton<S> ConvertNodeSetloop(RegexNode node)
        {
            //ranges and categories are encoded in set
            string set = node._str;

            S moveCond = CreateConditionFromSet((node._options & RegexOptions.IgnoreCase) != 0, set);

            if (moveCond.Equals(solver.False))
            {
                if (node._m == 0)
                    return this.automBuilder.epsilon; // (Empty)* = Epsilon
                else
                    return this.automBuilder.empty;
            }

            if (!description.ContainsKey(moveCond))
                description[moveCond] = RegexCharClass.SetDescription(set);


            Automaton<S> loop = automBuilder.MkOneLoop(moveCond, node._m, node._n);
            return loop;
        }

        #endregion

        #region IDescribe<S> Members

        public string Describe(S label)
        {
            if (!description.ContainsKey(label))
            {
                string descr = solver.PrettyPrint(label);
                description[label] = descr;
                return descr;
            }

            return description[label];
        }

        #endregion

        #region Symbolic regex conversion
        /// <summary>
        /// Convert a regex pattern to an equivalent symbolic regex
        /// </summary>
        /// <param name="regex">the given .NET regex pattern</param>
        /// <param name="options">regular expression options for the pattern (default is RegexOptions.None)</param>
        /// <param name="keepAnchors">if false (default) then anchors are replaced by equivalent regexes</param>
        public SymbolicRegexNode<S> ConvertToSymbolicRegex(string regex, RegexOptions options = RegexOptions.None, bool keepAnchors = false)
        {
            //filter out the RightToLeft option that turns around the parse tree
            //but has no semantical meaning regarding the regex
            var options1 = (options & ~RegexOptions.RightToLeft);

            RegexTree tree = RegexParser.Parse(regex, options1);
            return ConvertToSymbolicRegex(tree._root, keepAnchors);
        }

        internal SymbolicRegexNode<S> ConvertToSymbolicRegex(RegexNode root, bool keepAnchors = false, bool unwindlowerbounds = false)
        {
            var sregex = ConvertNodeToSymbolicRegex(root, true);
            if (keepAnchors)
            {
                if (unwindlowerbounds)
                    return sregex.Simplify();
                else
                    return sregex;
            }
            else
            {
                var res = this.srBuilder.RemoveAnchors(sregex, true, true);
                if (unwindlowerbounds)
                    return res.Simplify();
                else
                    return res;
            }
        }

        /// <summary>
        /// Convert a .NET regex into an equivalent symbolic regex
        /// </summary>
        /// <param name="regex">the given .NET regex</param>
        /// <param name="keepAnchors">if false (default) then anchors are replaced by equivalent regexes</param>
        public SymbolicRegexNode<S> ConvertToSymbolicRegex(Regex regex, bool keepAnchors = false, bool unwindlowerbounds = false)
        {
            var node = ConvertToSymbolicRegex(regex.ToString(), regex.Options, keepAnchors);
            if (unwindlowerbounds)
                node = node.Simplify();
            return node;
        }

        internal SymbolicRegexNode<S> ConvertNodeToSymbolicRegex(RegexNode node, bool topLevel)
        {
            switch (node._type)
            {
                case RegexNode.Alternate:
                    return this.srBuilder.MkOr(Array.ConvertAll(node._children.ToArray(), x => ConvertNodeToSymbolicRegex(x, topLevel)));
                case RegexNode.Beginning:
                    return this.srBuilder.startAnchor;
                case RegexNode.Bol:
                    return this.srBuilder.bolAnchor;
                case RegexNode.Capture:  //paranthesis (...)
                    return ConvertNodeToSymbolicRegex(node.Child(0), topLevel);
                case RegexNode.Concatenate:
                    return this.srBuilder.MkConcat(Array.ConvertAll(node._children.ToArray(), x => ConvertNodeToSymbolicRegex(x, false)), topLevel);
                case RegexNode.Empty:
                    return this.srBuilder.epsilon;
                case RegexNode.End:
                case RegexNode.EndZ:
                    return this.srBuilder.endAnchor;
                case RegexNode.Eol:
                    return this.srBuilder.eolAnchor;
                case RegexNode.Loop:
                    return this.srBuilder.MkLoop(ConvertNodeToSymbolicRegex(node._children[0], false), false, node._m, node._n);
                case RegexNode.Lazyloop:
                    return this.srBuilder.MkLoop(ConvertNodeToSymbolicRegex(node._children[0], false), true, node._m, node._n);
                case RegexNode.Multi:
                    return ConvertNodeMultiToSymbolicRegex(node, topLevel);
                case RegexNode.Notone:
                    return ConvertNodeNotoneToSymbolicRegex(node);
                case RegexNode.Notoneloop:
                    return ConvertNodeNotoneloopToSymbolicRegex(node, false);
                case RegexNode.Notonelazy:
                    return ConvertNodeNotoneloopToSymbolicRegex(node, true);
                case RegexNode.One:
                    return ConvertNodeOneToSymbolicRegex(node);
                case RegexNode.Oneloop:
                    return ConvertNodeOneloopToSymbolicRegex(node, false);
                case RegexNode.Onelazy:
                    return ConvertNodeOneloopToSymbolicRegex(node, true);
                case RegexNode.Set:
                    return ConvertNodeSetToSymbolicRegex(node);
                case RegexNode.Setloop:
                    return ConvertNodeSetloopToSymbolicRegex(node, false);
                case RegexNode.Setlazy:
                    return ConvertNodeSetloopToSymbolicRegex(node, true);
                case RegexNode.Testgroup:
                    return MkIfThenElse(ConvertNodeToSymbolicRegex(node._children[0], false), ConvertNodeToSymbolicRegex(node._children[1], false), ConvertNodeToSymbolicRegex(node._children[2], false));
                case RegexNode.ECMABoundary:
                case RegexNode.Boundary:
                    throw new AutomataException(@"Not implemented: word-boundary \b");
                case RegexNode.Nonboundary:
                case RegexNode.NonECMABoundary:
                    throw new AutomataException(@"Not implemented: non-word-boundary \B");
                case RegexNode.Nothing:
                    throw new AutomataException(@"Not implemented: Nothing");
                case RegexNode.Greedy:
                    throw new AutomataException("Not implemented: greedy constructs (?>) (?<)");
                case RegexNode.Start:
                    throw new AutomataException(@"Not implemented: \G");
                case RegexNode.Group:
                    throw new AutomataException("Not supported: grouping (?:)");
                case RegexNode.Prevent:
                    throw new AutomataException("Not supported: prevent constructs (?!) (?<!)");
                case RegexNode.Require:
                    throw new AutomataException("Not supported: require constructs (?=) (?<=)");
                case RegexNode.Testref:
                    throw new AutomataException("Not supported: test construct (?(n) | )");
                case RegexNode.Ref:
                    throw new AutomataException(@"Not supported: references \1");
                default:
                    throw new AutomataException(@"Unexpected regex construct");
            }
        }

        #region Character sequences to symbolic regexes
        /// <summary>
        /// Sequence of characters in node._str
        /// </summary>
        private SymbolicRegexNode<S> ConvertNodeMultiToSymbolicRegex(RegexNode node, bool topLevel)
        {
            //sequence of characters
            string sequence = node._str;
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            S[] conds = Array.ConvertAll(sequence.ToCharArray(), c => solver.MkCharConstraint(c, ignoreCase));
            var seq = this.srBuilder.MkSequence(conds, topLevel);
            return seq;
        }

        /// <summary>
        /// Matches chacter any character except node._ch
        /// </summary>
        private SymbolicRegexNode<S> ConvertNodeNotoneToSymbolicRegex(RegexNode node)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            S cond = solver.MkNot(solver.MkCharConstraint(node._ch, ignoreCase));

            if (!description.ContainsKey(cond))
                description[cond] = string.Format("[^{0}]", Rex.RexEngine.Escape(node._ch));

            return this.srBuilder.MkSingleton(cond);
        }

        /// <summary>
        /// Matches only node._ch
        /// </summary>
        private SymbolicRegexNode<S> ConvertNodeOneToSymbolicRegex(RegexNode node)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            S cond = solver.MkCharConstraint(node._ch, ignoreCase);
            if (!description.ContainsKey(cond))
                description[cond] = Rex.RexEngine.Escape(node._ch);

            return this.srBuilder.MkSingleton(cond);
        }

        #endregion

        #region special loops
        private SymbolicRegexNode<S> ConvertNodeSetToSymbolicRegex(RegexNode node)
        {
            //ranges and categories are encoded in set
            string set = node._str;

            S moveCond = CreateConditionFromSet((node._options & RegexOptions.IgnoreCase) != 0, set);

            if (!description.ContainsKey(moveCond))
                description[moveCond] = RegexCharClass.SetDescription(set);

            return this.srBuilder.MkSingleton(moveCond);
        }

        private SymbolicRegexNode<S> ConvertNodeNotoneloopToSymbolicRegex(RegexNode node, bool isLazy)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            S cond = solver.MkNot(solver.MkCharConstraint(node._ch, ignoreCase));
            if (!description.ContainsKey(cond))
                description[cond] = string.Format("[^{0}]", Rex.RexEngine.Escape(node._ch));

            SymbolicRegexNode<S> body = this.srBuilder.MkSingleton(cond);
            SymbolicRegexNode<S> loop = this.srBuilder.MkLoop(body, isLazy, node._m, node._n);
            return loop;
        }

        private SymbolicRegexNode<S> ConvertNodeOneloopToSymbolicRegex(RegexNode node, bool isLazy)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            S cond = solver.MkCharConstraint(node._ch, ignoreCase);
            if (!description.ContainsKey(cond))
                description[cond] = string.Format("{0}", Rex.RexEngine.Escape(node._ch));

            SymbolicRegexNode<S> body = this.srBuilder.MkSingleton(cond);
            SymbolicRegexNode<S> loop = this.srBuilder.MkLoop(body, isLazy, node._m, node._n);
            return loop;
        }

        private SymbolicRegexNode<S> ConvertNodeSetloopToSymbolicRegex(RegexNode node, bool isLazy)
        {
            //ranges and categories are encoded in set
            string set = node._str;

            S moveCond = CreateConditionFromSet((node._options & RegexOptions.IgnoreCase) != 0, set);

            if (!description.ContainsKey(moveCond))
                description[moveCond] = RegexCharClass.SetDescription(set);

            SymbolicRegexNode<S> body = this.srBuilder.MkSingleton(moveCond);
            SymbolicRegexNode<S> loop = this.srBuilder.MkLoop(body, isLazy, node._m, node._n);
            return loop;
        }

        #endregion

        /// <summary>
        /// Make an if-then-else regex (?(cond)left|right)
        /// </summary>
        /// <param name="cond">condition</param>
        /// <param name="left">true case</param>
        /// <param name="right">false case</param>
        /// <returns></returns>
        public SymbolicRegexNode<S> MkIfThenElse(SymbolicRegexNode<S> cond, SymbolicRegexNode<S> left, SymbolicRegexNode<S> right)
        {
            return this.srBuilder.MkIfThenElse(cond, left, right);
        }

        /// <summary>
        /// Make a singleton sequence regex
        /// </summary>
        public SymbolicRegexNode<S> MkSingleton(S set)
        {
            return this.srBuilder.MkSingleton(set);
        }

        public SymbolicRegexNode<S> MkOr(params SymbolicRegexNode<S>[] regexes)
        {
            return this.srBuilder.MkOr(regexes);
        }

        public SymbolicRegexNode<S> MkConcat(params SymbolicRegexNode<S>[] regexes)
        {
            return this.srBuilder.MkConcat(regexes, false);
        }

        public SymbolicRegexNode<S> MkEpsilon()
        {
            return this.srBuilder.epsilon;
        }

        public SymbolicRegexNode<S> MkLoop(SymbolicRegexNode<S> regex, int lower = 0, int upper = int.MaxValue, bool isLazy = false)
        {
            return this.srBuilder.MkLoop(regex, isLazy, lower, upper);
        }

        public SymbolicRegexNode<S> MkStartAnchor()
        {
            return this.srBuilder.MkStartAnchor();
        }

        public SymbolicRegexNode<S> MkEndAnchor()
        {
            return this.srBuilder.MkEndAnchor();
        }



        #endregion
    }


    #region more specialized converters
    internal class RegexToAutomatonConverterCharSet : RegexToAutomatonConverter<BDD>
    {
        CharSetSolver bddBuilder;
        Chooser chooser;
        public Chooser Chooser
        {
            get { return chooser; }
        }

        public RegexToAutomatonConverterCharSet(CharSetSolver solver) : base(solver, new UnicodeCategoryToCharSetProvider(solver))
        {
            this.bddBuilder = solver;
            this.chooser = new Chooser();
        }

        //public static RegexToAutomatonConverterCharSet Create(BitWidth encoding)
        //{
        //    var solver = new CharSetSolver(encoding);
        //    return new RegexToAutomatonConverterCharSet(solver);
        //}

        /// <summary>
        /// Describe the bdd as a regex character set
        /// </summary>
        new public string Describe(BDD label)
        {

            string res;
            //if (description.TryGetValue(label, out res))
            //{
            //    string res1 = res.Replace(@"\p{Nd}", @"\d").Replace(@"\P{Nd}", @"\D");
            //    return res1;
            //}
            if (this.categorizer.CategoryCondition(8) == label)
                return @"\d";
            if (this.Solver.MkNot(this.categorizer.CategoryCondition(8)) == label)
                return @"\D";
            if (this.categorizer.WordLetterCondition == label)
                return @"\w";
            if (this.Solver.MkNot(this.categorizer.WordLetterCondition) == label)
                return @"\W";
            if (this.categorizer.WhiteSpaceCondition == label)
                return @"\s";
            if (this.Solver.MkNot(this.categorizer.WhiteSpaceCondition) == label)
                return @"\S";
            for (int i = 0; i < this.categorizer.UnicodeCategoryStandardAbbreviations.Length; i++)
                if (this.categorizer.CategoryCondition(i) == label)
                {
                    return @"\P{" + this.categorizer.UnicodeCategoryStandardAbbreviations[i] + "}";
                }

            var ranges = bddBuilder.ToRanges(label);
            if (ranges.Length == 1 && ranges[0].Item1 == ranges[0].Item2)
            {
                string res1 = StringUtility.Escape((char)ranges[0].Item1);
                description[label] = res1;
                return res1;
            }

            res = "[";
            for (int i = 0; i < ranges.Length; i++ )
            {
                var range = ranges[i];
                if (range.Item1 == range.Item2)
                    res += StringUtility.EscapeWithNumericSpace((char)range.Item1);
                else if (range.Item1 == range.Item2 - 1)
                {
                    res += StringUtility.EscapeWithNumericSpace((char)range.Item1);
                    res += StringUtility.EscapeWithNumericSpace((char)range.Item2);
                }
                else
                {
                    res += StringUtility.EscapeWithNumericSpace((char)range.Item1);
                    res += "-";
                    res += StringUtility.EscapeWithNumericSpace((char)range.Item2);
                }
            }
            res += "]";
            description[label] = res;
            return res;
        }

        /// <summary>
        /// Generates a random member accepted by fa. 
        /// Assumes that fa has no dead states, or else termination is not guaranteed.
        /// </summary>
        public string GenerateMember(Automaton<BDD> fa)
        {
            if (fa.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeNonempty);
            var sb = new System.Text.StringBuilder();
            int state = fa.InitialState;
            while (!fa.IsFinalState(state) || (fa.OutDegree(state) > 0 && chooser.ChooseTrueOrFalse()))
            {
                var move = fa.GetNthMoveFrom(state, chooser.Choose(fa.GetMovesCountFrom(state)));
                if (!move.IsEpsilon)
                    sb.Append((char)(bddBuilder.Choose(move.Label)));
                state = move.TargetState;
            }
            return sb.ToString();
        }
    }

    internal class RegexToAutomatonConverterRanges : RegexToAutomatonConverter<HashSet<Tuple<char,char>>>
    {
        CharRangeSolver solver;
        Chooser chooser;
        public Chooser Chooser
        {
            get { return chooser; }
        }

        public RegexToAutomatonConverterRanges(CharRangeSolver solver)
            : base(solver, new UnicodeCategoryToRangesProvider(solver))
        {
            this.solver = solver;
            this.chooser = new Chooser();
        }

        //public static RegexToAutomatonConverterBDD Create(CharacterEncoding encoding)
        //{
        //    var solver = new CharSetSolver(encoding);
        //    return new RegexToAutomatonConverterBDD(solver);
        //}

        /// <summary>
        /// Describe range set
        /// </summary>
        new public string Describe(HashSet<Tuple<char, char>> label)
        {

            string res;
            res = "";
            List<Tuple<char, char>> ranges = new List<Tuple<char, char>>(label);
            for (int i = 0; i < ranges.Count; i++)
            {
                var range = ranges[i];
                if (range.Item1 == range.Item2)
                    res += Rex.RexEngine.Escape(range.Item1);
                else
                {
                    res += "[";
                    res += Rex.RexEngine.Escape(range.Item1);
                    res += "-";
                    res += Rex.RexEngine.Escape(range.Item2);
                    res += "]";
                }
                if (i < ranges.Count - 1)
                    res += "|";
            }
            return res;
        }

        /// <summary>
        /// Generates a random member accepted by fa. 
        /// Assumes that fa has no dead states, or else termination is not guaranteed.
        /// </summary>
        public string GenerateMember(Automaton<HashSet<Tuple<char, char>>> fa)
        {
            if (fa.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeNonempty);
            var sb = new System.Text.StringBuilder();
            int state = fa.InitialState;
            while (!fa.IsFinalState(state) || (fa.OutDegree(state) > 0 && chooser.ChooseTrueOrFalse()))
            {
                var move = fa.GetNthMoveFrom(state, chooser.Choose(fa.GetMovesCountFrom(state)));
                if (!move.IsEpsilon)
                {
                    Tuple<char, char> someRange = new Tuple<char,char>('\0','\0');
                    foreach (var range in move.Label)
                    {
                        someRange = range;
                        break;
                    }
                    int offset = chooser.Choose(Math.Max(1,(int)someRange.Item2 - (int)someRange.Item1));
                    char someChar = (char)((int)someRange.Item1 + offset);
                    sb.Append(someChar);
                }
                state = move.TargetState;
            }
            return sb.ToString();
        }
    }

    internal class RegexToAutomatonConverterHashSet : RegexToAutomatonConverter<HashSet<char>>
    {
        HashSetSolver solver;
        Chooser chooser;
        public Chooser Chooser
        {
            get { return chooser; }
        }

        public RegexToAutomatonConverterHashSet(HashSetSolver solver)
            : base(solver, new UnicodeCategoryToHashSetProvider(solver))
        {
            this.solver = solver;
            this.chooser = new Chooser();
        }

        /// <summary>
        /// Describe hash set
        /// </summary>
        new public string Describe(HashSet<char> label)
        {
            var ranges = new Utilities.Ranges();
            foreach (char c in label)
                ranges.Add((int)c);

            string res = "";
            for (int i = 0; i < ranges.ranges.Count; i++)
            {
                var range = ranges.ranges[i];
                if (range[0] == range[1])
                    res += Rex.RexEngine.Escape((char)range[0]);
                else
                {
                    res += "[";
                    res += Rex.RexEngine.Escape((char)range[0]);
                    res += "-";
                    res += Rex.RexEngine.Escape((char)range[1]);
                    res += "]";
                }
                if (i < ranges.ranges.Count - 1)
                    res += "|";
            }
            description[label] = res;
            return res;
        }

        /// <summary>
        /// Generates a random member accepted by fa. 
        /// Assumes that fa has no dead states, or else termination is not guaranteed.
        /// </summary>
        public string GenerateMember(Automaton<HashSet<char>> fa)
        {
            if (fa.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeNonempty);
            var sb = new System.Text.StringBuilder();
            int state = fa.InitialState;
            while (!fa.IsFinalState(state) || (fa.OutDegree(state) > 0 && chooser.ChooseTrueOrFalse()))
            {
                var move = fa.GetNthMoveFrom(state, chooser.Choose(fa.GetMovesCountFrom(state)));
                if (!move.IsEpsilon)
                {
                    char someChar = '\0';
                    foreach (var c in move.Label)
                    {
                        someChar = c;
                        break;
                    }
                    sb.Append(someChar);
                }
                state = move.TargetState;
            }
            return sb.ToString();
        }
    }

    #endregion
}

