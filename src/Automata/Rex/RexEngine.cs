using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Automata;

namespace Microsoft.Automata.Rex
{
    /// <summary>
    /// Provides a member generator for regexes
    /// </summary>
    public class RexEngine
    {
        const int tryLimitMin = 100;
        const int tryLimitMax = 200;

        Chooser chooser;
        CharSetSolver solver;
        public CharSetSolver Solver
        {
            get { return solver; }
        }

        RegexToAutomatonConverterCharSet converter;
        internal RexEngine(CharSetSolver solver)
        {
            this.solver = solver;
            converter = solver.regexConverter; //new RegexToAutomatonConverterCharSet(solver);
            chooser = converter.Chooser;
        }

        BitWidth encoding;

        ///// <summary>
        ///// Create a regex member generator for the given character encoding and the given random seed
        ///// </summary>
        ///// <param name="encoding">character encoding</param>
        ///// <param name="randomSeed">if less than 0 then a randomly chosen random seed is used</param>
        //public RexEngine(BitWidth encoding, int randomSeed)
        //{
        //    //int t = System.Environment.TickCount;
        //    this.encoding = encoding;
        //    solver = new CharSetSolver(encoding);
        //    chooser = (randomSeed < 0 ? new Chooser() : new Chooser(randomSeed));
        //    converter = solver.regexConverter;
        //}

        /// <summary>
        /// Create a regex member generator for the given character encoding.
        /// </summary>
        /// <param name="encoding">character encoding</param>
        public RexEngine(BitWidth encoding)
        {
            //int t = System.Environment.TickCount;
            this.encoding = encoding;
            solver = new CharSetSolver(encoding);
            chooser = new Chooser();
            converter = solver.regexConverter;
        }

        ///// <summary>
        ///// Gets the random seed of the generator
        ///// </summary>
        //public int RandomSeed
        //{
        //    get { return chooser.RandomSeed; }
        //}

        /// <summary>
        /// Generates a random member accepted by fa. 
        /// Assumes that fa has no dead states, or else termination is not guaranteed.
        /// </summary>
        public string GenerateMember(Automaton<BDD> fa)
        {
            var sb = new System.Text.StringBuilder();
            int state = fa.InitialState;
            while (!fa.IsFinalState(state) || (fa.OutDegree(state) > 0 && chooser.ChooseTrueOrFalse()))
            {
                var move = fa.GetNthMoveFrom(state, chooser.Choose(fa.GetMovesCountFrom(state)));
                if (!move.IsEpsilon)
                    sb.Append((char)(solver.Choose(move.Label)));
                state = move.TargetState;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates a random member accepted by fa with uniform distribution among strings accepted by the sfa. 
        /// Assumes that the fa is deterministic and loopfree.
        /// </summary>
        public string GenerateMemberUniformly(Automaton<BDD> fa)
        {
            return solver.GenerateMemberUniformly(fa);
        }

        /// <summary>
        /// Generates at most k distinct strings that match all the given regexes.
        /// The enumeration is empty if there are no strings that match all the regexes.
        /// </summary>
        /// <param name="options">regular expression options</param>
        /// <param name="k"> number of members to be generated</param>
        /// <param name="regexes">given regexes</param>
        /// <returns>enumeration of strings each of which matches all the regexes</returns>
        public IEnumerable<string> GenerateMembers(RegexOptions options, int k, params string[] regexes)
        {
            Automaton<BDD> sfa = CreateFromRegexes(options, regexes);
            return GenerateMembers(sfa, k);
        }

        /// <summary>
        /// Generates at most k distinct strings in the language of the sfa.
        /// The enumeration is empty if the language is empty.
        /// </summary>
        public IEnumerable<string> GenerateMembers(Automaton<BDD> sfa, int k)
        {
            if (sfa == null || sfa.IsEmpty)
                yield break; //there are no members to generate


            var old = new HashSet<string>();

            for (int i = 0; i < k; i++)
            {
                string member = GenerateMember(sfa);
                int tryCount = Math.Min(tryLimitMin + old.Count, tryLimitMax);
                while (old.Contains(member) && tryCount-- > 0)
                    member = GenerateMember(sfa);
                if (tryCount < 0 && old.Contains(member))
                    break; //give up, the language has less than k elements with high probability
                old.Add(member);
                yield return member;
            }
        }

        /// <summary>
        /// Create a product of the automata of the given regexes.
        /// </summary>
        public Automaton<BDD> CreateFromRegexes(RegexOptions options, params string[] regexes)
        {
            Automaton<BDD> sfa = null;
            foreach (var regex in regexes)
            {
                var sfa1 = converter.Convert(regex, options);
                sfa = (sfa == null ? sfa1 : Automaton<BDD>.MkProduct(sfa, sfa1));
                if (sfa.IsEmpty)
                    break;
            }
            return sfa;
        }

        /// <summary>
        /// Create a product of the automata of the given regexes.
        /// </summary>
        public Automaton<BDD> CreateFromRegexes(params string[] regexes)
        {
            return CreateFromRegexes(RegexOptions.None, regexes);
        }

        /// <summary>
        /// Invokes System.Text.RegularExpressions.Regex.IsMatch(input, regex, options)
        /// </summary>
        public static bool IsMatch(string input, string regex, RegexOptions options)
        {
            return Regex.IsMatch(input, regex, options);
        }

        /// <summary>
        /// Create a string encoding of the given automaton with probabilities.
        /// </summary>
        /// <param name="aut">deterministic and loopfree automaton</param>
        public string SerializeDAG(Automaton<BDD> aut)
        {
            return solver.SerializeDAG(aut);
        }

        /// <summary>
        /// Recreate an automaton from a string that has been produced with SerializeDAG.
        /// </summary>
        /// <param name="dag">string encoding of a deterministic and loopfree automaton with probabilities</param>
        public Automaton<BDD> DeserializeDAG(string dag)
        {
            return solver.DeserializeDAG(dag);
        }

        /// <summary>
        /// Determinize and then minimize the automaton
        /// </summary>
        public Automaton<BDD> Minimize(Automaton<BDD> aut)
        {
            return aut.Determinize().Minimize();
        }

        /// <summary>
        /// Complement the automaton
        /// </summary>
        public Automaton<BDD> Complement(Automaton<BDD> aut)
        {
            return aut.Determinize().Complement();
        }

        /// <summary>
        /// Intersect two (or more) automata. 
        /// </summary>
        public Automaton<BDD> Intersect(params Automaton<BDD>[] automata)
        {
            if (automata.Length == 0)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var res = automata[0];
            for (int i = 1; i < automata.Length; i++)
            {
                res = res.Intersect(automata[i]);
            }
            return res;
        }

        /// <summary>
        /// Returns true iff aut1 and aut2 accept the same set of strings.
        /// </summary>
        public bool AreEquivalent(Automaton<BDD> aut1, Automaton<BDD> aut2)
        {
            return aut1.IsEquivalentWith(aut2);
        }


        #region Escaping strings
        /// <summary>
        /// Make an escaped string from a character
        /// </summary>
        public static string Escape(char c)
        {
            int code = (int)c;
            if (code > 126)
                return ToUnicodeRepr(code);

            if (code < 32)
                return string.Format("\\x{0:X}", code);

            switch (c)
            {
                case '\0':
                    return @"\0";
                case '\a':
                    return @"\a";
                case '\b':
                    return @"\b";
                case '\t':
                    return @"\t";
                case '\r':
                    return @"\r";
                case '\v':
                    return @"\v";
                case '\f':
                    return @"\f";
                case '\n':
                    return @"\n";
                case '\u001B':
                    return @"\e";
                case '\"':
                    return "\\\"";
                case '\'':
                    return "\\\'";
                case ' ':
                    return " ";
                default:
                    if (code < 32)
                        return string.Format("\\x{0:X}", code);
                    else
                        return c.ToString();
            }
        }

        static string ToUnicodeRepr(int i)
        {
            string s = string.Format("{0:X}", i);
            if (s.Length == 1)
                s = "\\u000" + s;
            else if (s.Length == 2)
                s = "\\u00" + s;
            else if (s.Length == 3)
                s = "\\u0" + s;
            else
                s = "\\u" + s;
            return s;
        }

        /// <summary>
        /// Make an escaped string from a string
        /// </summary>
        public static string Escape(string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\"");
            foreach (char c in s)
            {
                sb.Append(Escape(c));
            }
            sb.Append("\"");
            return sb.ToString();
        }
        #endregion

        public string Describe(BDD label)
        {
            return converter.Describe(label);
        }
    }

    /*
    /// <summary>
    /// Convert regex expressions to corresponding SFAs
    /// </summary>
    internal class RegexToSFAGeneric<S>
    {
        ICharacterConstraintSolver<S> solver;
        IUnicodeCategoryTheory<S> categorizer;
        //records friendly descriptions of conditions for visualization purposes
        Dictionary<S, string> description = new Dictionary<S, string>();

        public RegexToSFAGeneric(ICharacterConstraintSolver<S> solver,
                          IUnicodeCategoryTheory<S> categorizer)
        {
            this.solver = solver;
            this.categorizer = categorizer;
            description.Add(solver.True, "");
        }

        public SFA<S> Convert(string regex, RegexOptions options)
        {
            //filter out the RightToLeft option that turns around the parse tree
            //but has no semantical meaning regarding the regex
            var options1 = (options & ~RegexOptions.RightToLeft);

            RegexTree tree = RegexParser.Parse(regex, options1);
            return ConvertNode(tree._root, 0, true, true);
        }

        private SFA<S> ConvertNode(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            //node = node.Reduce();
            switch (node._type)
            {
                case RegexNode.Alternate:
                    return ConvertNodeAlternate(node, minStateId, isStart, isEnd);
                case RegexNode.Beginning:
                    return ConvertNodeBeginning(node, minStateId, isStart, isEnd); ;
                case RegexNode.Bol:
                    return ConvertNodeBol(node, minStateId, isStart, isEnd);
                case RegexNode.Boundary:
                    return ConvertNodeBoundary(node, minStateId, isStart, isEnd);
                case RegexNode.Capture:  // (...)
                    return ConvertNode(node.Child(0), minStateId, isStart, isEnd);
                case RegexNode.Concatenate:
                    return ConvertNodeConcatenate(node, minStateId, isStart, isEnd);
                case RegexNode.ECMABoundary:
                    return ConvertNodeECMABoundary(node, minStateId, isStart, isEnd);
                case RegexNode.Empty:
                    return ConvertNodeEmpty(node, minStateId, isStart, isEnd);
                case RegexNode.End:
                    return ConvertNodeEnd(node, minStateId, isStart, isEnd);
                case RegexNode.EndZ:
                    return ConvertNodeEndZ(node, minStateId, isStart, isEnd);
                case RegexNode.Eol:
                    return ConvertNodeEol(node, minStateId, isStart, isEnd);
                case RegexNode.Greedy:
                    return ConvertNodeGreedy(node, minStateId, isStart, isEnd);
                case RegexNode.Group:
                    return ConvertNodeGroup(node, minStateId, isStart, isEnd);
                case RegexNode.Lazyloop:
                    return ConvertNodeLazyloop(node, minStateId, isStart, isEnd);
                case RegexNode.Loop:
                    return ConvertNodeLoop(node, minStateId, isStart, isEnd);
                case RegexNode.Multi:
                    return ConvertNodeMulti(node, minStateId, isStart, isEnd);
                case RegexNode.Nonboundary:
                    return ConvertNodeNonboundary(node, minStateId, isStart, isEnd);
                case RegexNode.NonECMABoundary:
                    return ConvertNodeNonECMABoundary(node, minStateId, isStart, isEnd);
                case RegexNode.Nothing:
                    return ConvertNodeNothing(node, minStateId, isStart, isEnd);
                case RegexNode.Notone:
                    return ConvertNodeNotone(node, minStateId, isStart, isEnd);
                case RegexNode.Notonelazy:
                    return ConvertNodeNotonelazy(node, minStateId, isStart, isEnd);
                case RegexNode.Notoneloop:
                    return ConvertNodeNotoneloop(node, minStateId, isStart, isEnd);
                case RegexNode.One:
                    return ConvertNodeOne(node, minStateId, isStart, isEnd);
                case RegexNode.Onelazy:
                    return ConvertNodeOnelazy(node, minStateId, isStart, isEnd);
                case RegexNode.Oneloop:
                    return ConvertNodeOneloop(node, minStateId, isStart, isEnd);
                case RegexNode.Prevent:
                    return ConvertNodePrevent(node, minStateId, isStart, isEnd);
                case RegexNode.Ref:
                    return ConvertNodeRef(node, minStateId, isStart, isEnd);
                case RegexNode.Require:
                    return ConvertNodeRequire(node, minStateId, isStart, isEnd);
                case RegexNode.Set:
                    return ConvertNodeSet(node, minStateId, isStart, isEnd);
                case RegexNode.Setlazy:
                    return ConvertNodeSetlazy(node, minStateId, isStart, isEnd);
                case RegexNode.Setloop:
                    return ConvertNodeSetloop(node, minStateId, isStart, isEnd);
                case RegexNode.Start:
                    return ConvertNodeStart(node, minStateId, isStart, isEnd);
                case RegexNode.Testgroup:
                    return ConvertNodeTestgroup(node, minStateId, isStart, isEnd);
                case RegexNode.Testref:
                    return ConvertNodeTestref(node, minStateId, isStart, isEnd);
                default:
                    throw new AutomataException(AutomataException.UnrecognizedRegex);
            }
        }

        private SFA<S> ConvertNodeEmpty(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            if (isStart || isEnd) //may start or end with any characters since no anchor is used
                return SFA<S>.Create(minStateId, new int[] { minStateId }, new Move<S>[] { Move<S>.T(minStateId, minStateId, solver.True) });
            else //an intermediate empty string is just an epsilon transition
                return SFA<S>.Epsilon;
        }

        #region Character sequences
        /// <summary>
        /// Sequence of characters in node._str
        /// </summary>
        private SFA<S> ConvertNodeMulti(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            //sequence of characters
            string sequence = node._str;
            int count = sequence.Length;

            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            int initialstate = minStateId;
            int finalstate = initialstate + count;
            int[] finalstates = new int[] { finalstate };

            var moves = new List<Move<S>>();
            for (int i = 0; i < count; i++)
            {
                List<char[]> ranges = new List<char[]>();
                char c = sequence[i];
                ranges.Add(new char[] { c, c });
                S cond = solver.MkRangesConstraint(ignoreCase, ranges);
                if (!description.ContainsKey(cond))
                    description[cond] = RexEngine.Escape(c);
                moves.Add(Move<S>.T(initialstate + i, initialstate + i + 1, cond));
            }

            SFA<S> res = SFA<S>.Create(initialstate, finalstates, moves);
            res.isDeterministic = true;
            if (isStart) //may start with any characters
            {
                res.AddMove(Move<S>.T(initialstate, initialstate, solver.True));
                res.isDeterministic = false;
            }
            if (isEnd) //may end with any characters
                res.AddMove(Move<S>.T(finalstate, finalstate, solver.True));
            res.isEpsilonFree = true;
            return res;
        }

        /// <summary>
        /// Matches chacter any character except node._ch
        /// </summary>
        private SFA<S> ConvertNodeNotone(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            S cond = solver.MkNot(solver.MkCharConstraint(ignoreCase, node._ch));
            if (!description.ContainsKey(cond))
                description[cond] = string.Format("[^{0}]", RexEngine.Escape(node._ch));
            var res = SFA<S>.Create(minStateId,
                             new int[] { minStateId + 1 }, new Move<S>[] { Move<S>.T(minStateId, minStateId + 1, cond) });
            res.isEpsilonFree = true;
            if (isStart) //may start with any characters
            {
                res.AddMove(Move<S>.T(minStateId, minStateId, solver.True));
                res.isDeterministic = false;
            }
            if (isEnd) //may end with any characters
                res.AddMove(Move<S>.T(minStateId + 1, minStateId + 1, solver.True));
            return res;
        }

        /// <summary>
        /// Matches only node._ch
        /// </summary>
        private SFA<S> ConvertNodeOne(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            S cond = solver.MkCharConstraint(ignoreCase, node._ch);
            if (!description.ContainsKey(cond))
                description[cond] = RexEngine.Escape(node._ch);
            var res = SFA<S>.Create(minStateId,
                             new int[] { minStateId + 1 }, new Move<S>[] { Move<S>.T(minStateId, minStateId + 1, cond) });
            res.isEpsilonFree = true;
            if (isStart) //may start with any characters
            {
                res.AddMove(Move<S>.T(minStateId, minStateId, solver.True));
                res.isDeterministic = false;
            }
            if (isEnd) //may end with any characters
                res.AddMove(Move<S>.T(minStateId + 1, minStateId + 1, solver.True));
            return res;
        }

        #endregion

        #region Character sets

        private SFA<S> ConvertNodeSet(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            //ranges and categories are encoded in set
            string set = node._str;

            S moveCond = CreateConditionFromSet((node._options & RegexOptions.IgnoreCase) != 0, set);

            if (moveCond.Equals(solver.False))
                return SFA<S>.Empty; //the condition is unsatisfiable

            if (!description.ContainsKey(moveCond))
                description[moveCond] = RegexCharClass.SetDescription(set);

            //create an SFA with two states: minStateId and minStateId+1 (as the final state)
            //and add a move (minStateId, moveCond, minStateId+1)
            int finalState = minStateId + 1;
            SFA<S> sfa =
                SFA<S>.Create(minStateId,
                    new int[] { finalState }, new Move<S>[] { Move<S>.T(minStateId, finalState, moveCond) });
            sfa.isDeterministic = true;
            if (isStart) //may start with any characters
            {
                sfa.AddMove(Move<S>.T(minStateId, minStateId, solver.True));
                sfa.isDeterministic = false;
            }
            if (isEnd) //may end with any characters
                sfa.AddMove(Move<S>.T(finalState, finalState, solver.True));

            sfa.isEpsilonFree = true;
            return sfa;
        }

        private const int SETLENGTH = 1;
        private const int CATEGORYLENGTH = 2;
        private const int SETSTART = 3;
        private const char Lastchar = '\uFFFF';

        private S CreateConditionFromSet(bool ignoreCase, string set)
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
                S cond = solver.MkRangeConstraint(ignoreCase, range.First, range.Second);
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
            if (subtractorCond != null)
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
                catCond = (catCond == null ? cond : solver.MkOr(catCond, cond));
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

        #region Anchors

        private SFA<S> ConvertNodeEnd(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            if (!isEnd)
                throw new AutomataException(AutomataException.MisplacedEndAnchor);
            if (isStart) //allow any charaters at the beginning
                return SFA<S>.Create(minStateId, new int[] { minStateId }, new Move<S>[] { Move<S>.T(minStateId, minStateId, solver.True) });
            return SFA<S>.Epsilon; //must end without additional characters
        }

        private SFA<S> ConvertNodeEndZ(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            if (!isEnd)
                throw new AutomataException(AutomataException.MisplacedEndAnchor);

            if (isStart)
                //allow any characters in the prefix
                return SFA<S>.Create(minStateId, new int[] { minStateId }, new Move<S>[] { Move<S>.T(minStateId, minStateId, solver.True) });

            return SFA<S>.Epsilon; //must end without additional characters
        }

        private SFA<S> ConvertNodeBeginning(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            if (!isStart)
                throw new AutomataException(AutomataException.MisplacedStartAnchor);
            if (isEnd)
                //otherwise, allow any trailing characters
                return SFA<S>.Create(minStateId, new int[] { minStateId }, new Move<S>[] { Move<S>.T(minStateId, minStateId, solver.True) });

            return SFA<S>.Epsilon;
        }

        private SFA<S> ConvertNodeBol(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            if (!isStart)
                throw new AutomataException(AutomataException.MisplacedStartAnchor);

            //some beginning of a line in multiline mode
            SFA<S> fa = SFA<S>.Create(minStateId, new int[] { minStateId + 2 },
                new Move<S>[]{Move<S>.Epsilon(minStateId, minStateId + 2),
                           Move<S>.Epsilon(minStateId, minStateId + 1),
                           Move<S>.T(minStateId+1,minStateId+1, solver.True),
                           Move<S>.T(minStateId+1,minStateId+2, solver.MkCharConstraint('\n'))});

            if (isEnd) //allow any trailing characters
                fa.AddMove(Move<S>.T(fa.FinalState, fa.FinalState, solver.True));

            return fa;
        }

        private SFA<S> ConvertNodeEol(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            if (!isEnd)
                throw new AutomataException(AutomataException.MisplacedEndAnchor);

            //some end of a line in multiline mode
            SFA<S> fa = SFA<S>.Create(minStateId, new int[] { minStateId + 2 },
                    new Move<S>[]{Move<S>.Epsilon(minStateId, minStateId + 2),
                           Move<S>.Epsilon(minStateId+1, minStateId + 2),
                           Move<S>.T(minStateId+1,minStateId+1, solver.True),
                           Move<S>.T(minStateId,minStateId+1, solver.MkCharConstraint('\n'))});

            if (isStart) //allow any characters at the beginning
                fa.AddMove(Move<S>.T(fa.InitialState, fa.InitialState, solver.True));

            return fa;
        }

        #endregion

        #region Alternation

        private SFA<S> ConvertNodeAlternate(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            var sfas = new List<SFA<S>>();
            int start = minStateId + 1;
            bool includesEmptyWord = false;
            foreach (var child in node._children)
            {
                var sfa = ConvertNode(child, start, isStart, isEnd);
                if (sfa == SFA<S>.Empty)        //may happen if a move condition is unsat
                    continue;               //just ignore the empty SFA
                if (sfa == SFA<S>.Epsilon)      //may happen if some branch is ()
                {
                    includesEmptyWord = true;
                    continue;
                }
                if (sfa.IsFinalState(sfa.InitialState))
                    includesEmptyWord = true;
                sfas.Add(sfa);
                start = sfa.MaxState + 1;
            }

            //ShowAsGraphs(sfas);

            return AlternateSFAs(minStateId, sfas, includesEmptyWord);
        }

        private SFA<S> AlternateSFAs(int start, List<SFA<S>> sfas, bool addEmptyWord)
        {

            #region special cases for sfas.Count == 0 or sfas.Count == 1
            if (sfas.Count == 0)
            {
                if (addEmptyWord)
                    return SFA<S>.Epsilon;
                else
                    return SFA<S>.Empty;
            }
            if (sfas.Count == 1)
            {
                if (addEmptyWord && !sfas[0].IsFinalState(sfas[0].InitialState))
                {
                    if (sfas[0].InitialStateIsSource)
                        sfas[0].MakeInitialStateFinal();
                    else
                        sfas[0].AddNewInitialStateThatIsFinal(start);
                }
                return sfas[0];
            }
            #endregion //special cases for sfas.Count == 0 or sfas.Count == 1

            bool allSingleSource = true;
            #region check if all sfas have a single source
            foreach (var sfa in sfas)
                if (!sfa.InitialStateIsSource)
                {
                    allSingleSource = false;
                    break;
                }
            #endregion //check if all sfas have a single source

            bool isDeterministic = !sfas.Exists(IsNonDeterministic);
            bool isEpsilonFree = !sfas.Exists(HasEpsilons);

            bool allFinalSink = true;
            int sinkId;
            #region check if all sfas have a single final sink and calulate a representative sinkId as the maximum of the ids
            sinkId = int.MinValue;
            foreach (var sfa in sfas)
                if (!sfa.HasSingleFinalSink)
                {
                    allFinalSink = false;
                    break;
                }
                else
                    sinkId = Math.Max(sfa.FinalState, sinkId);
            #endregion

            var finalStates = new List<int>();
            if (addEmptyWord)
                finalStates.Add(start); //epsilon is accepted so initial state is also final
            var conditionMap = new Dictionary<Tuple<int, int>, S>();//for normalization of move conditions
            var eMoves = new HashSet<Tuple<int, int>>();

            if (!allSingleSource)
            {
                isDeterministic = false;
                isEpsilonFree = false;
                foreach (var sfa in sfas) //add initial epsilon transitions
                    eMoves.Add(new Tuple<int, int>(start, sfa.InitialState));
            }
            else if (isDeterministic)
            {
                //check if determinism is preserved
                for (int i = 0; i < sfas.Count - 1; i++)
                {
                    for (int j = i + 1; j < sfas.Count; j++)
                    {
                        S cond1 = solver.False;
                        foreach (var move in sfas[i].GetMovesFrom(sfas[i].InitialState))
                            cond1 = solver.MkOr(cond1, move.Condition);
                        S cond2 = solver.False;
                        foreach (var move in sfas[j].GetMovesFrom(sfas[j].InitialState))
                            cond2 = solver.MkOr(cond2, move.Condition);
                        S checkCond = solver.MkAnd(cond1, cond2);
                        isDeterministic = (checkCond.Equals(solver.False));
                        if (!isDeterministic)
                            break;
                    }
                    if (!isDeterministic)
                        break;
                }
            }

            if (allFinalSink)
                finalStates.Add(sinkId); //this will be the final state 

            Dictionary<int, int> stateRenamingMap = new Dictionary<int, int>();

            foreach (var sfa in sfas)
            {
                foreach (var move in sfa.GetMoves())
                {
                    int source = (allSingleSource && sfa.InitialState == move.SourceState ? start : move.SourceState);
                    int target = (allFinalSink && sfa.FinalState == move.TargetState ? sinkId : move.TargetState);
                    var p = new Tuple<int, int>(source, target);
                    stateRenamingMap[move.SourceState] = source;
                    stateRenamingMap[move.TargetState] = target;
                    if (move.IsEpsilon)
                    {
                        if (source != target)
                            eMoves.Add(new Tuple<int, int>(source, target));
                        continue;
                    }

                    S cond;
                    if (conditionMap.TryGetValue(p, out cond))
                        conditionMap[p] = solver.MkOr(cond, move.Condition);  //join the conditions into a disjunction
                    else
                        conditionMap[p] = move.Condition;
                }
                if (!allFinalSink)
                    foreach (int s in sfa.GetFinalStates())
                    {
                        int s1 = stateRenamingMap[s];
                        if (!finalStates.Contains(s1))
                            finalStates.Add(s1);
                    }
            }

            SFA<S> res = SFA<S>.Create(start, finalStates, GenerateMoves(conditionMap, eMoves));
            res.isDeterministic = isDeterministic;
            return res;
        }

        #endregion //Alternation

        #region Concatenation

        private SFA<S> ConvertNodeConcatenate(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            var children = node._children;
            var sfas = new List<SFA<S>>();
            int start = minStateId;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var sfa = ConvertNode(child, start, isStart && i == 0, isEnd && i == children.Count - 1);
                if (sfa == SFA<S>.Empty)
                    return SFA<S>.Empty; //the whole concatenation becomes empty
                if (sfa == SFA<S>.Epsilon)
                    continue;  //the epsilon is a noop in concatenation, just ignore it
                sfas.Add(sfa);
                start = sfa.MaxState + 1;
            }
            //ShowAsGraphs(sfas);
            return ConcatenateSFAs(sfas);
        }

        private SFA<S> ConcatenateSFAs(List<SFA<S>> sfas)
        {
            if (sfas.Count == 0)
                return SFA<S>.Epsilon; //all were SFAS were epsilons, so the result is Epsilon

            if (sfas.Count == 1)   //concatenation is trivial
                return sfas[0];

            //desctructively update the first sfa
            SFA<S> result = sfas[0];
            for (int i = 1; i < sfas.Count; i++)
                result.Concat(sfas[i]);
            return result;
        }

        #endregion //Concatenation

        #region Loops

        private SFA<S> ConvertNodeLoop(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            SFA<S> sfa = ConvertNode(node._children[0], minStateId, false, false);
            int m = node._m;
            int n = node._n;
            SFA<S> loop;

            //Display(sfa, "tmp", RANKDIR.LR, 18, true, "gif");

            if (m == 0 && sfa.IsEmpty)
                loop = SFA<S>.Epsilon;
            else if (m == 0 && n == int.MaxValue) //case: *
            {
                loop = MakeKleeneClosure(sfa);
            }
            else if (m == 0 && n == 1) //case: ?
            {
                ;
                if (sfa.IsFinalState(sfa.InitialState))
                    return sfa;
                else if (sfa.InitialStateIsSource)
                    sfa.MakeInitialStateFinal();
                else
                    sfa.AddNewInitialStateThatIsFinal(sfa.MaxState + 1);
                loop = sfa;
            }
            else if (m == 1 && n == 1) //trivial case: r{1,1} = r
            {
                if (sfa.IsEmpty)
                    return SFA<S>.Empty;
                loop = sfa;
            }
            else if (n == int.MaxValue) //case: + or generally {m,} for m >= 1
            {
                if (sfa.IsEmpty)
                    return SFA<S>.Empty;

                if (sfa.IsFinalState(sfa.InitialState))
                    loop = MakeKleeneClosure(sfa); //the repetition is a loop
                else
                {
                    List<SFA<S>> sfas = new List<SFA<S>>();
                    for (int i = 0; i < m; i++)
                    {
                        //make m fresh copies
                        sfas.Add(sfa);
                        sfa = sfa.MakeCopy(sfa.MaxState + 1);
                    }
                    //the last one is made into a Kleene closure
                    sfas.Add(MakeKleeneClosure(sfa));
                    //concatenate them all
                    loop = ConcatenateSFAs(sfas);
                }
            }
            else //general case {m,n} 
            {
                List<SFA<S>> sfas = new List<SFA<S>>();
                //List<int> newFinals = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    sfas.Add(sfa);
                    if (i < n - 1)
                    {
                        sfa = sfa.MakeCopy(sfa.MaxState + 1);
                        if (i >= m - 1)
                        {
                            if (sfa.InitialStateIsSource && !sfa.IsFinalState(sfa.InitialState))
                                sfa.MakeInitialStateFinal();
                            else
                                sfa.AddNewInitialStateThatIsFinal(sfa.MaxState + 1);
                        }
                    }

                }
                loop = ConcatenateSFAs(sfas);
                //loop.SetFinalStates(newFinals);
            }
            loop = ExtendLoop(minStateId, isStart, isEnd, loop);
            //Display(loop, "tmp", RANKDIR.LR, 18, true, "gif");
            return loop;
        }

        private SFA<S> ExtendLoop(int minStateId, bool isStart, bool isEnd, SFA<S> loop)
        {
            if (isStart)
            {
                if (loop != SFA<S>.Epsilon)
                {
                    SFA<S> prefix = SFA<S>.Create(loop.MaxState + 1, new int[] { loop.MaxState + 1 },
                        new Move<S>[] { Move<S>.T(loop.MaxState + 1, loop.MaxState + 1, solver.True) });
                    prefix.Concat(loop);
                    loop = prefix;
                }
                else
                    loop = SFA<S>.Create(minStateId, new int[] { minStateId },
                        new Move<S>[] { Move<S>.T(minStateId, minStateId, solver.True) });

            }
            if (isEnd)
            {
                if (loop != SFA<S>.Epsilon)
                    loop.Concat(SFA<S>.Create(loop.MaxState + 1, new int[] { loop.MaxState + 1 },
                        new Move<S>[] { Move<S>.T(loop.MaxState + 1, loop.MaxState + 1, solver.True) }));
                else
                    loop = SFA<S>.Create(minStateId, new int[] { minStateId },
                        new Move<S>[] { Move<S>.T(minStateId, minStateId, solver.True) });
            }
            return loop;
        }

        private SFA<S> MakeKleeneClosure(SFA<S> sfa)
        {
            if (sfa == SFA<S>.Empty || sfa == SFA<S>.Epsilon)
                return SFA<S>.Epsilon;

            if (sfa.IsKleeneClosure())
                return sfa;

            if (sfa.InitialStateIsSource && sfa.HasSingleFinalSink)
            {
                //common case, avoid epsilons in this case
                //just make the final state to be the initial state
                sfa.RenameInitialState(sfa.FinalState);
                return sfa;
            }

            int origInitState = sfa.InitialState;

            if (!sfa.IsFinalState(sfa.InitialState))//the initial state is not final
            {
                if (sfa.InitialStateIsSource)
                    //make the current initial state final
                    sfa.MakeInitialStateFinal();
                else
                    //add a new initial state that is also final
                    sfa.AddNewInitialStateThatIsFinal(sfa.MaxState + 1);
            }

            //add epsilon transitions from final states to the original initial state
            foreach (int state in sfa.GetFinalStates())
                if (state != sfa.InitialState && state != origInitState)
                    sfa.AddMove(Move<S>.Epsilon(state, origInitState));

            //epsilon loops might have been created, remove them
            return sfa.RemoveEpsilonLoops(solver.MkOr);
        }

        private SFA<S> ConvertNodeNotoneloop(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            S cond = solver.MkNot(solver.MkCharConstraint(ignoreCase, node._ch));
            if (!description.ContainsKey(cond))
                description[cond] = string.Format("[^{0}]", RexEngine.Escape(node._ch));
            SFA<S> loop = CreateLoopFromCondition(minStateId, cond, node._m, node._n);
            loop = ExtendLoop(minStateId, isStart, isEnd, loop);
            return loop;
        }

        private SFA<S> ConvertNodeOneloop(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);
            S cond = solver.MkCharConstraint(ignoreCase, node._ch);
            if (!description.ContainsKey(cond))
                description[cond] = string.Format("{0}", RexEngine.Escape(node._ch));
            SFA<S> loop = CreateLoopFromCondition(minStateId, cond, node._m, node._n);
            loop = ExtendLoop(minStateId, isStart, isEnd, loop);
            return loop;
        }

        private SFA<S> ConvertNodeSetloop(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            //ranges and categories are encoded in set
            string set = node._str;

            S moveCond = CreateConditionFromSet((node._options & RegexOptions.IgnoreCase) != 0, set);

            if (moveCond.Equals(solver.False))
            {
                if (node._m == 0)
                    return SFA<S>.Epsilon; // (Empty)* = Epsilon
                else
                    return SFA<S>.Empty;
            }

            if (!description.ContainsKey(moveCond))
                description[moveCond] = RegexCharClass.SetDescription(set);

            SFA<S> loop = CreateLoopFromCondition(minStateId, moveCond, node._m, node._n);
            loop = ExtendLoop(minStateId, isStart, isEnd, loop);
            return loop;
        }

        private static SFA<S> CreateLoopFromCondition(int minStateId, S cond, int m, int n)
        {
            if (m == 0 && n == int.MaxValue) //case : *
            {
                var res = SFA<S>.Create(minStateId,
                                 new int[] { minStateId }, new Move<S>[] { Move<S>.T(minStateId, minStateId, cond) });
                res.isEpsilonFree = true;
                res.isDeterministic = true;
                return res;
            }
            else if (m == 0 && n == 1) //case : ?
            {
                var res = SFA<S>.Create(minStateId,
                                 new int[] { minStateId, minStateId + 1 }, new Move<S>[] { Move<S>.T(minStateId, minStateId + 1, cond) });
                res.isEpsilonFree = true;
                res.isDeterministic = true;
                return res;
            }
            else if (n == int.MaxValue) //case : + or {m,}
            {
                var moves = new List<Move<S>>();
                for (int i = 0; i < m; i++)
                    moves.Add(Move<S>.T(minStateId + i, minStateId + i + 1, cond));
                moves.Add(Move<S>.T(minStateId + m, minStateId + m, cond));
                var res = SFA<S>.Create(minStateId, new int[] { minStateId + m }, moves);
                res.isDeterministic = true;
                res.isEpsilonFree = true;
                return res;
            }
            else //general case {m,n}
            {
                Move<S>[] moves = new Move<S>[n];
                for (int i = 0; i < n; i++)
                    moves[i] = Move<S>.T(minStateId + i, minStateId + i + 1, cond);
                int[] finalstates = new int[n + 1 - m];
                for (int i = m; i <= n; i++)
                    finalstates[i - m] = i + minStateId;
                var res = SFA<S>.Create(minStateId, finalstates, moves);
                res.isEpsilonFree = true;
                res.isDeterministic = true;
                return res;
            }
        }

        #endregion

        #region Constructs that are not supported

        private SFA<S> ConvertNodeECMABoundary(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeGreedy(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeGroup(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeLazyloop(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeBoundary(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeNothing(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeNonboundary(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeNonECMABoundary(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeNotonelazy(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeOnelazy(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodePrevent(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeRef(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeRequire(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeSetlazy(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeStart(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeTestgroup(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        private SFA<S> ConvertNodeTestref(RegexNode node, int minStateId, bool isStart, bool isEnd)
        {
            throw new AutomataException(AutomataException.NotSupported);
        }

        #endregion

        #region Misc helper functions related to SFAs
        static bool IsNonDeterministic(SFA<S> sfa)
        {
            return !sfa.isDeterministic;
        }

        static bool HasEpsilons(SFA<S> sfa)
        {
            return !sfa.isEpsilonFree;
        }

        IEnumerable<Move<S>> GenerateMoves(Dictionary<Tuple<int, int>, S> condMap, IEnumerable<Tuple<int, int>> eMoves)
        {
            foreach (var kv in condMap)
                yield return Move<S>.T(kv.Key.First, kv.Key.Second, kv.Value);
            foreach (var emove in eMoves)
                yield return Move<S>.Epsilon(emove.First, emove.Second);
        }

        #endregion

        #region Visualization

        /// <summary>
        /// Write the FSA in dot format.
        /// </summary>
        /// <param name="fa">the FSA to write</param>
        /// <param name="faName">the name of the FSA</param>
        /// <param name="filename">the name of the output file</param>
        /// <param name="rankdir">the main direction of the arrows</param>
        /// <param name="fontsize">the size of the font in labels</param>
        public void ToDot(SFA<S> fa, string faName, string filename, RANKDIR rankdir, int fontsize)
        {
            System.IO.StreamWriter sw = new System.IO.StreamWriter(filename);
            ToDot(fa, faName, sw, rankdir, fontsize);
            sw.Close();
        }

        /// <summary>
        /// Write the FSA in dot format.
        /// </summary>
        /// <param name="fa">the FSA to write</param>
        /// <param name="faName">the name of the FSA</param>
        /// <param name="tw">text writer for the output</param>
        /// <param name="rankdir">the main direction of the arrows</param>
        /// <param name="fontsize">the size of the font in labels</param>
        public void ToDot(SFA<S> fa, string faName, System.IO.TextWriter tw, RANKDIR rankdir, int fontsize)
        {
            tw.WriteLine("digraph \"" + faName + "\" {");
            tw.WriteLine(string.Format("rankdir={0};", rankdir.ToString()));
            tw.WriteLine();
            tw.WriteLine("//Initial state");
            tw.WriteLine(string.Format("node [style = filled, shape = ellipse, peripheries = {0}, fillcolor = \"#d3d3d3ff\", fontsize = {1}]",
                (fa.IsFinalState(fa.InitialState) ? "2" : "1"), fontsize));
            tw.WriteLine(fa.InitialState);
            tw.WriteLine();
            tw.WriteLine("//Final states");
            tw.WriteLine(string.Format("node [style = filled, shape = ellipse, peripheries = 2, fillcolor = white, fontsize = {0}]", fontsize));
            foreach (int state in fa.GetFinalStates())
                if (state != fa.InitialState)
                    tw.WriteLine(state);
            tw.WriteLine();
            tw.WriteLine("//Other states");
            tw.WriteLine(string.Format("node [style = filled, shape = ellipse, peripheries = 1, fillcolor = white, fontsize = {0}]", fontsize));
            foreach (int state in fa.States)
                if (state != fa.InitialState && !fa.IsFinalState(state))
                    tw.WriteLine(state);
            tw.WriteLine();
            tw.WriteLine("//Transitions");
            foreach (Move<S> t in fa.GetMoves())//???
                tw.WriteLine(string.Format("{0} -> {1} [label = \"{2}\"{3}, fontsize = {4} ];", t.SourceState, t.TargetState,
                    t.IsEpsilon ? "" : description[t.Condition],
                    t.IsEpsilon ? ", style = dashed" : "", fontsize));
            tw.WriteLine("}");
        }

        /// <summary>
        /// View the given SFA as a graph. Requires that dot.exe is installed.
        /// Uses dot.exe to create a file name.dot and produces a layout in name.format.
        /// If showgraph is true, starts a process to view the graph.
        /// For example if name = "foo" and format = "gif", creates a file 
        /// foo.dot with the dot output and a file foo.gif as a picture.
        /// Uses the current working directory.
        /// </summary>
        /// <param name="fa">the SFA to be viewed</param>
        /// <param name="name">name of the file where the graph is stored</param>
        /// <param name="dir">direction of the arrows</param>
        /// <param name="fontsize">size of the font in node and edge labels</param>
        /// <param name="showgraph">id true, the graph is viewed</param>
        /// <param name="format">format of the figure</param>
        public void Display(SFA<S> fa, string name, RANKDIR dir, int fontsize, bool showgraph, string format)
        {
            string currentDirectory = System.Environment.CurrentDirectory;
            string dotFile = string.Format("{1}\\{0}.dot", name, currentDirectory);
            string outFile = string.Format("{2}\\{0}.{1}", name, format, currentDirectory);
            System.IO.FileInfo fi = new System.IO.FileInfo(dotFile);
            if (fi.Exists)
                fi.IsReadOnly = false;
            fi = new System.IO.FileInfo(outFile);
            if (fi.Exists)
                fi.IsReadOnly = false;
            ToDot(fa, name, dotFile, dir, fontsize);
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo("dot.exe", string.Format("-T{2} {0} -o {1}", dotFile, outFile, format));
            try
            {
                p.Start();
                p.WaitForExit();
                if (showgraph)
                {
                    p.StartInfo = new System.Diagnostics.ProcessStartInfo(outFile);
                    p.Start();
                }
            }
            catch (Exception)
            {
                throw new AutomataException(AutomataException.MissingDotViewer);
            }
        }
        #endregion
    }

    */
}
