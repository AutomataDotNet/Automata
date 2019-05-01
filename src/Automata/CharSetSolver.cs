using System;
using System.Collections.Generic;
//using RestrictKeyType = System.Int64;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.Automata;

namespace Microsoft.Automata
{
    /// <summary>
    /// Provides functionality to build character sets, to perform boolean operations over character sets,
    /// and to construct an SFA over character sets from a regex.
    /// Character sets are represented by bitvector sets.
    /// </summary>
    public class CharSetSolver : BDDAlgebra,  ICharAlgebra<BDD>
    {

        internal RegexToAutomatonConverterCharSet regexConverter;

        int _bw;

        public BitWidth Encoding
        {
            get { return (BitWidth)_bw; }
        }
        
        /// <summary>
        /// Underlying regex converter.
        /// </summary>
        public RegexToAutomatonConverter<BDD> RegexConverter { get { return regexConverter; } }

        /// <summary>
        /// Construct the solver for BitWidth.BV16
        /// </summary>
        public CharSetSolver() : this(BitWidth.BV16)
        {
        }

        /// <summary>
        /// Construct a character set solver for the given character encoding (nr of bits).
        /// </summary>
        public CharSetSolver(BitWidth bits) : base()
        {
            if (!CharacterEncodingTool.IsSpecified(bits))
                throw new AutomataException(AutomataExceptionKind.CharacterEncodingIsUnspecified);
            _bw = (int)bits;
            regexConverter = new RegexToAutomatonConverterCharSet(this);
        }

        /// <summary>
        /// Choose a member of the set. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="chooser">element chooser</param>
        /// <param name="set">given set</param>
        /// <returns></returns>
        public uint Choose(Chooser chooser, BDD set)
        {
            return (uint)Choose(chooser, set, _bw - 1);
        }

        #region  ICharacterConstraintSolver<BvSet> Members

        #region Creating ranges

        Microsoft.Automata.Utilities.IgnoreCaseTransformer _IgnoreCase = null;
        Microsoft.Automata.Utilities.IgnoreCaseTransformer IgnoreCase
        {
            get
            {
                if (_IgnoreCase == null)
                    _IgnoreCase = new Microsoft.Automata.Utilities.IgnoreCaseTransformer(this);
                return _IgnoreCase;
            }
        }

        BDD[] charPredTable = new BDD[1 << 16];

        /// <summary>
        /// Make a character containing the given character c.
        /// If c is a lower case or upper case character and ignoreCase is true
        /// then add both the upper case and the lower case characters.
        /// </summary>
        public BDD MkCharConstraint(char c, bool ignoreCase = false)
        {
            int i = (int)c;
            if (charPredTable[i] == null)
                charPredTable[i] = MkSetFrom((uint)c, _bw - 1);
            if (ignoreCase)
                return IgnoreCase.Apply(charPredTable[i]);
            return charPredTable[i];
        }

        /// <summary>
        /// Make a CharSet from all the characters in the range from m to n. 
        /// Returns the empty set if n is less than m
        /// </summary>
        public BDD MkCharSetFromRange(char m, char n)
        {
            return MkSetFromRange((uint)m, (uint)n, _bw-1);
        }

        /// <summary>
        /// Make a character set that is the union of the character sets of the given ranges.
        /// </summary>
        public BDD MkCharSetFromRanges(IEnumerable<Tuple<uint, uint>> ranges)
        {
            BDD res = False;
            foreach (var range in ranges)
                res = MkOr(res, MkSetFromRange(range.Item1, range.Item2, _bw -1));
            return res;
        }

        /// <summary>
        /// Make a character set that is the union of the character sets of ranges, there must me an even number of characters definining ranges.
        /// </summary>
        public BDD MkCharSetFromRanges(params char[] chars)
        {
            if ((chars.Length & 1) == 1)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            BDD res = False;
            for (int i = 0; i < chars.Length; i +=2 )
                res = MkOr(res, MkSetFromRange((uint)chars[i], (uint)chars[i+1], _bw - 1));
            return res;
        }

        /// <summary>
        /// Make a character set from a regex character class, e.g., 
        /// "\w" is the character class of word-letter characters.
        /// </summary>
        public BDD MkCharSetFromRegexCharClass(string charClass)
        {
            var aut = Convert("^" + charClass + @"\z");
            if (aut.MoveCount != 1)
                throw new AutomataException(AutomataExceptionKind.InvalidRegexCharacterClass);

            return aut.GetMoveFrom(aut.InitialState).Label;
        }

        //private void CheckBug(Tuple<int, int>[] ranges)
        //{
        //    if (ranges.Length > 2)
        //    {
        //        for (int i = 0; i < ranges.Length - 1; i++)
        //        {
        //            if (ranges[i].Second == ranges[i + 1].First - 1)
        //                throw new Exception("bug");
        //        }
        //    }
        //}


        const int maxChar = 0xFFFF;
        /// <summary>
        /// Make a character set of all the characters in the interval from c to d.
        /// If ignoreCase is true ignore cases for upper and lower case characters by including both versions.
        /// </summary>
        public BDD MkRangeConstraint(char c, char d, bool ignoreCase = false)
        {
            var res = MkSetFromRange((uint)c, (uint)d, _bw - 1);
            if (ignoreCase)
                res = IgnoreCase.Apply(res);
            return res;
        }

        //BDD MkRangeConstraint1(bool ignoreCase, char c, char d)
        //{
        //    if (ignoreCase)
        //    {
        //        BDD bdd = False;
        //        for (char i = c; i <= d; i++) //???
        //            bdd = MkOr(bdd, MkCharConstraint(ignoreCase, i));
        //        return bdd;
        //    }
        //    else
        //    {
        //        return MkCharSetFromRange(c, d);
        //    }

        //}

        /// <summary>
        /// Make a BDD encoding of k least significant bits of all the integers in the ranges
        /// </summary>
        internal BDD MkBddForIntRanges(IEnumerable<int[]> ranges)
        {
            BDD bdd = False;
            foreach (var range in ranges)
                bdd = MkOr(bdd, MkSetFromRange((uint)range[0], (uint)range[1], _bw - 1));
            return bdd;
        }

        /// <summary>
        /// Make a character set constraint of all the characters in the character ranges.
        /// If ignoreCase is true ignore cases for upper and lower case characters by including both versions.
        /// It is assumed that each elemet in ranges is an array of 2 characters.
        /// </summary>
        public BDD MkRangesConstraint(bool ignoreCase, IEnumerable<char[]> ranges)
        {
            BDD bdd = False;
            foreach (var range in ranges)
                bdd = MkOr(bdd, MkRangeConstraint(range[0], range[1], ignoreCase));
            return bdd;
        }
        #endregion

        #endregion

        #region Serialializing and deserializing BDDs

        /// <summary>
        /// Represent the set as an integer array.
        /// Assumes that the bdd has less than 2^14 nodes and at most 16 variables.
        /// </summary>
        internal int[] SerializeCompact(BDD bdd)
        {
            //return SerializeBasedOnRanges(bdd);
            return SerializeCompact2(bdd);
        }

        /// <summary>
        /// Represent the set as an integer array.
        /// Assumes that the bdd has at most 2^14 nodes and at most 16 variables.
        /// </summary>
        int[] SerializeCompact2(BDD bdd)
        {
            // encode the bdd directly
            //
            // the element at index 0 is the false node
            // the element at index 1 is the true node 
            // and entry at index i>1 is node i and has the structure 
            // (ordinal trueNode falseNode)
            // where ordinal uses 4 bits and trueNode and falseNode each use 14 bits
            // Assumes that the bdd has less than 2^14 nodes and at most 16 variables.
            // BDD.False is represented by int[]{0}.
            // BDD.True is represented by int[]{0,0}.
            // The root of the BDD (Other than True or False) is node 2

            if (bdd.IsEmpty)
                return new int[] { 0 };
            if (bdd.IsFull)
                return new int[] { 0, 0 };

            int nrOfNodes = bdd.CountNodes();

            if (nrOfNodes > (1 << 14))
                throw new AutomataException(AutomataExceptionKind.CompactSerializationNodeLimitViolation);

            int[] res = new int[nrOfNodes];


            //here we know that bdd is neither empty nor full
            var done = new Dictionary<BDD, int>();
            done[False] = 0;
            done[True] = 1;

            Stack<BDD> stack = new Stack<BDD>();
            stack.Push(bdd);
            done[bdd] = 2;

            int doneCount = 3;

            while (stack.Count > 0)
            {
                BDD b = stack.Pop();
                if (!done.ContainsKey(b.One))
                {
                    done[b.One] = (doneCount++);
                    stack.Push(b.One);
                }
                if (!done.ContainsKey(b.Zero))
                {
                    done[b.Zero] = (doneCount++);
                    stack.Push(b.Zero);
                }
                int bId = done[b];
                int fId = done[b.Zero];
                int tId = done[b.One];

                if (b.Ordinal > 15)
                    throw new AutomataException(AutomataExceptionKind.CompactSerializationBitLimitViolation);

                res[bId] = (b.Ordinal << 28) | (tId << 14) | fId;
            }
            return res;
        }

        private uint[] SerializeBasedOnRanges(BDD set)
        {
            //use ranges
            var ranges = this.ToRanges(set);
            var res = new uint[ranges.Length];
            for (int i = 0; i < res.Length; i++)
                //uses the fact that only 16 bits are used since integers are character codes
                res[i] = (ranges[i].Item1 << 16) | ranges[i].Item2;

            return res;
        }

        /// <summary>
        /// Recreates a BDD from an int array that has been created using SerializeCompact
        /// </summary>
        internal BDD DeserializeCompact(int[] arcs)
        {
            //return DeserializeBasedOnRanges(arcs);
            return DeserializeCompact2(arcs);
        }

        /// <summary>
        /// Recreates a BDD from an int array that has been created using SerializeCompact
        /// </summary>
        BDD DeserializeCompact2(int[] arcs) 
        {
            if (arcs.Length == 1)
                return False;
            if (arcs.Length == 2)
                return True;

            //organized by order
            //note that all arcs are strictly increasing in levels
            var levels = new List<int>[16];

            BDD[] bddMap = new BDD[arcs.Length];
            bddMap[0] = False;
            bddMap[1] = True;

            for (int i = 2; i < arcs.Length; i++)
            {
                int x = ((arcs[i] >> 28) & 0xF);
                if (levels[x] == null)
                    levels[x] = new List<int>();
                levels[x].Add(i);
            }

            //create the BDD nodes according to the levels x
            //this is to ensure proper internalization
            for (int x = 0; x < 16; x++)
            {
                if (levels[x] != null)
                {
                    foreach (int i in levels[x])
                    {
                        int one = ((arcs[i] >> 14) & 0x3FFF);
                        int zero = (arcs[i] & 0x3FFF);
                        if (one > bddMap.Length || zero > bddMap.Length)
                            throw new AutomataException(AutomataExceptionKind.CompactDeserializationError);
                        var oneBranch = bddMap[one];
                        var zeroBranch = bddMap[zero];
                        var bdd = MkBvSet(x, oneBranch, zeroBranch);
                        bddMap[i] = bdd;
                        if (bdd.Ordinal <= bdd.One.Ordinal || bdd.Ordinal <= bdd.Zero.Ordinal)
                            throw new AutomataException(AutomataExceptionKind.CompactDeserializationError);
                    }
                }
            }

            return bddMap[2];
        }

        private BDD DeserializeBasedOnRanges(uint[] arcs)
        {
            Tuple<uint, uint>[] ranges = new Tuple<uint, uint>[arcs.Length];
            for (int i = 0; i < arcs.Length; i++)
                ranges[i] = new Tuple<uint, uint>((arcs[i] >> 16) & 0xFFFF, arcs[i] & 0xFFFF);
            var set = MkCharSetFromRanges(ranges);
            return set;
        }
        #endregion

        /// <summary>
        /// Provides a regex-character-class view of a character set.
        /// For example if the set contains characters 'a' to 'd' and 'x', and digits '5' to '8' then
        /// PrettyPrint(set) returns the string "[a-dx5-8]".
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        public string PrettyPrint(BDD set)
        {
            if (set.IsFull)
            {
                return ".";
            }
            else if (set.IsEmpty)
            {
                return "[0-[0]]";
            }
            if (ComputeDomainSize(MkNot(set)) <= 3)
            {
                string res = "[^";
                foreach (var c in GenerateAllCharactersInOrder(MkNot(set)))
                {
                    res += StringUtility.Escape((char)c);
                }
                res += "]";
                return res;
            }

            else
                return regexConverter.Describe(set);
        }

        /// <summary>
        /// Provides a regex-character-class view of a character set.
        /// For example if the set contains characters 'a' to 'd' and 'x', and digits '5' to '8' then
        /// PrettyPrint(set) returns the string "[a-dx5-8]".
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        public string PrettyPrint(BDD set, Func<BDD,string> varLookup)
        {
            return regexConverter.Describe(set);
        }

        /// <summary>
        /// Gets the element chooser of this character set solver.
        /// </summary>
        public Chooser Chooser
        {
            get { return regexConverter.Chooser; }
        }

        /// <summary>
        /// Choose a random element from the set. Assumes that the set is not empty.
        /// </summary>
        public uint Choose(BDD set)
        {
            return (uint)Choose(regexConverter.Chooser, set, _bw - 1);
        }

        /// <summary>
        /// Choose a random character from the set uniformly, all characters have equal probability of getting chosen. Assumes that the set is not empty.
        /// </summary>
        public char ChooseUniformly(BDD set)
        {
            return (char)ChooseUniformly(regexConverter.Chooser, set, _bw - 1);
        }

        /// <summary>
        /// Choose a random string such that the i'th character is a member of the 
        /// i'th character set in the enumeration.
        /// </summary>
        public string ChooseString(IEnumerable<BDD> charSets)
        {
            List<char> chars = new List<char>();
            foreach (var cs in charSets)
                chars.Add((char)Choose(cs));
            string s = new String(chars.ToArray());
            return s;
        }

        /// <summary>
        /// Convert an Automaton into a equivalent .NET regex.
        /// </summary>
        /// <param name="automaton">automaton over charsets</param>
        public string ConvertToRegex(Automaton<BDD> automaton, List<int> stateOrder)
        {
            if (automaton.IsEmpty)
                return @"^[^\0-\uFFFF]$"; //regex that accepts nothing

            List<int> removeList = new List<int>();

            int newInitialState = automaton.MaxState + 1;
            int newFinalState = automaton.MaxState + 2;

            var moves = new List<Move<BDD>>(automaton.GetMoves());
            moves.Add(new Move<BDD>(newInitialState, automaton.InitialState, null));
            foreach (var f in automaton.GetFinalStates())
                moves.Add(new Move<BDD>(f, newFinalState, null));

            automaton = Automaton<BDD>.Create(automaton.Algebra, newInitialState, new int[] { newFinalState }, moves);

            Func<int, int, Tuple<int, int>> P = (m, n) => { return new Tuple<int, int>(m, n); };          

            Dictionary<Tuple<int, int>, string> regexes = new Dictionary<Tuple<int, int>, string>();
            Dictionary<int, HashSet<int>> outStates = new Dictionary<int, HashSet<int>>();
            Dictionary<int, HashSet<int>> inStates = new Dictionary<int, HashSet<int>>();

            foreach (int state in automaton.GetStates())
            {
                if (state != newInitialState && state != newFinalState)
                    removeList.Add(state);
                //regexes[P(state, state)] = "";
                outStates[state] = new HashSet<int>();
                inStates[state] = new HashSet<int>();
            }

            var remListCopy = new List<int>();
            foreach (var st in stateOrder)
            {
                if (removeList.Contains(st))
                    remListCopy.Add(st);
            }
            removeList = remListCopy;

            #region initialize the regex map and the outlink and inlink maps
            foreach (var move in automaton.GetMoves())
            {
                if (move.SourceState != move.TargetState)
                {
                    outStates[move.SourceState].Add(move.TargetState);
                    inStates[move.TargetState].Add(move.SourceState);
                }

                var p = P(move.SourceState, move.TargetState);
                string lab = (move.IsEpsilon ? "" : PrettyPrint(move.Label));
                string oldlab;
                if (regexes.TryGetValue(p, out oldlab))
                    lab = MkRegexOr(oldlab, lab);
                regexes[p] = lab;
            }
            #endregion

            foreach (var pstate in removeList)
            {
                //eliminate the state.
                int s = pstate;
                string loopLabel;
                if (regexes.TryGetValue(P(s, s), out loopLabel))
                    loopLabel = MkKleeneClosure(regexes[P(s, s)]);

                foreach (var p in inStates[s])
                {
                    var ps = P(p, s);
                    foreach (var q in outStates[s])
                    {
                        var sq = P(s, q);
                        string lab = regexes[ps] + loopLabel + regexes[sq];

                        var pq = P(p, q);

                        string oldlab;
                        if (regexes.TryGetValue(pq, out oldlab))
                            lab = MkRegexOr(oldlab, lab);

                        regexes[pq] = lab;

                        if (p != q)
                        {
                            outStates[p].Add(q);
                            inStates[q].Add(p);
                        }
                    }
                }

                foreach (var p in inStates[s])
                    outStates[p].Remove(s);
                foreach (var q in outStates[s])
                    inStates[q].Remove(s);
            }

            string res = regexes[P(newInitialState, newFinalState)];

            return "^(" + res + ")$";
        }

        /// <summary>
        /// Convert an Automaton into a equivalent .NET regex.
        /// </summary>
        /// <param name="automaton">automaton over charsets</param>
        public string ConvertToRegex(Automaton<BDD> automaton)
        {
            if (automaton.IsEmpty)
                return @"^[^\0-\uFFFF]$"; //regex that accepts nothing

            List<PrioritizedState> removeList = new List<PrioritizedState>();

            int newInitialState = automaton.MaxState + 1;
            int newFinalState = automaton.MaxState + 2;

            var moves = new List<Move<BDD>>(automaton.GetMoves());
            moves.Add(new Move<BDD>(newInitialState, automaton.InitialState,null));
            foreach (var f in automaton.GetFinalStates())
                moves.Add(new Move<BDD>(f, newFinalState, null));

            automaton = Automaton<BDD>.Create(automaton.Algebra, newInitialState, new int[] { newFinalState }, moves);

            Func<int, int, Tuple<int,int>> P = (m,n) => {return new Tuple<int,int>(m,n);};

            Func<int, PrioritizedState> Prio = s => 
                {
                    int m = automaton.InDegree(s);
                    int n = automaton.OutDegree(s);
                    return new PrioritizedState(s, m * n);
                };

            Dictionary<Tuple<int, int>, string> regexes = new Dictionary<Tuple<int, int>, string>();
            Dictionary<int, HashSet<int>> outStates = new Dictionary<int, HashSet<int>>();
            Dictionary<int, HashSet<int>> inStates = new Dictionary<int, HashSet<int>>();

            foreach (int state in automaton.GetStates())
            {
                if (state != newInitialState && state != newFinalState)
                    removeList.Add(Prio(state));
                //regexes[P(state, state)] = "";
                outStates[state] = new HashSet<int>();
                inStates[state] = new HashSet<int>();
            }

            removeList.Sort();

            #region initialize the regex map and the outlink and inlink maps
            foreach (var move in automaton.GetMoves())
            {
                if (move.SourceState != move.TargetState)
                {
                    outStates[move.SourceState].Add(move.TargetState);
                    inStates[move.TargetState].Add(move.SourceState);
                }

                var p = P(move.SourceState, move.TargetState);
                string lab = (move.IsEpsilon ? "" : PrettyPrint(move.Label));
                string oldlab;
                if (regexes.TryGetValue(p, out oldlab))
                    lab = MkRegexOr(oldlab, lab);
                regexes[p] = lab;
            }
            #endregion

            foreach (var pstate in removeList)
            {
                //eliminate the state.
                int s = pstate.id;
                string loopLabel; 
                if (regexes.TryGetValue(P(s, s), out loopLabel))
                    loopLabel = MkKleeneClosure(regexes[P(s, s)]);

                foreach (var p in inStates[s])
                {
                    var ps = P(p, s);
                    foreach (var q in outStates[s])
                    {
                        var sq = P(s, q);
                        string lab = regexes[ps] + loopLabel + regexes[sq];

                        var pq = P(p, q);

                        string oldlab;
                        if (regexes.TryGetValue(pq, out oldlab))
                            lab = MkRegexOr(oldlab, lab);

                        regexes[pq] = lab;

                        if (p != q)
                        {
                            outStates[p].Add(q);
                            inStates[q].Add(p);
                        }
                    }
                }

                foreach (var p in inStates[s])
                    outStates[p].Remove(s);
                foreach (var q in outStates[s])
                    inStates[q].Remove(s);
            }

            string res = regexes[P(newInitialState, newFinalState)];

            return "^(" + res + ")$";
        }

        private string MkKleeneClosure(string p)
        {
            if (p == "")
                return "";
            return "(" + p + ")*";
        }

        string MkRegexOr(string r1, string r2)
        {
            if (r1 == "" && r2 == "")
                return "";
            if (r1 == "")
                return "(" + r2 + ")?";
            if (r2 == "")
                return "(" + r1 + ")?";
            return
                "(" + r1 + "|" + r2 + ")";
        }

        /// <summary>
        /// Convert a .NET regex into an equivalent automaton whose moves are labeled by character sets.
        /// Uses System.Text.RegularExpressions.RegexOptions.None for interpreting the regex.
        /// </summary>
        /// <param name="regex">.NET regex pattern</param>
        public Automaton<BDD> Convert(string regex)
        {
            var res = regexConverter.Convert(regex, System.Text.RegularExpressions.RegexOptions.None);
            return res;
        }

        /// <summary>
        /// Convert a .NET regex into an equivalent automaton whose moves are labeled by character sets.
        /// </summary>
        /// <param name="regex">.NET regex pattern</param>
        /// <param name="options">regex options for interpreting the regex, default is System.Text.RegularExpressions.RegexOptions.Singleline</param>
        /// <param name="keepBoundaryStates">used for testing purposes, when true boundary states are not removed, default is false</param>
        public Automaton<BDD> Convert(string regex, System.Text.RegularExpressions.RegexOptions options, bool keepBoundaryStates = false)
        {
            var res = regexConverter.Convert(regex, options, keepBoundaryStates);
            return res;
        }

        public Tuple<string, Automaton<BDD>>[] ConvertCaptures(string regex, out bool isLoop)
        {
            isLoop = false;
            var aut = regexConverter.ConvertCaptures(regex, out isLoop);
            var minaut = new Tuple<string, Automaton<BDD>>[aut.Length];
            for (int i = 0; i < aut.Length; i++)
            {
                minaut[i] = new Tuple<string, Automaton<BDD>>(aut[i].Item1, aut[i].Item2.RemoveEpsilons().Determinize().Minimize());
                if (i > 0)
                {
                    if (Overlaps(minaut[i - 1].Item2, minaut[i].Item2))
                        throw new AutomataException(AutomataExceptionKind.OverlappingPatternsNotSupported);
                }
            }
            if (isLoop)
                if (minaut[aut.Length - 1].Item2.HasMoreThanOneFinalState || minaut[aut.Length - 1].Item2.GetMovesCountFrom(minaut[aut.Length - 1].Item2.FinalState) > 0)
                    throw new AutomataException(AutomataExceptionKind.OverlappingPatternsNotSupported);

            return minaut;
        }

        private bool Overlaps(Automaton<BDD> a1, Automaton<BDD> a2)
        {
            foreach (var qf in a1.GetFinalStates())
                foreach (var qf_move in a1.GetMovesFrom(qf))
                    foreach (var q0_move in a2.GetMovesFrom(a2.InitialState))
                    {
                        var psi1 = qf_move.Label;
                        var psi2 = q0_move.Label;
                        if (IsSatisfiable(MkAnd(psi1, psi2)))
                            return true;
                    }
            return false;
        }

        /// <summary>
        /// Generate a random string accepted by the automaton.
        /// Assumes that the automaton accepts at least one string.
        /// </summary>
        public string GenerateMember(Automaton<BDD> aut)
        {
            if (aut.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeNonempty);

            return regexConverter.GenerateMember(aut);
        }

        /// <summary>
        /// Returns true iff the automaton aut accepts the string s.
        /// </summary>
        /// <param name="aut">given automaton</param>
        /// <param name="s">given string</param>
        /// <returns></returns>
        public bool Accepts(Automaton<BDD> aut, string s)
        {
            var s_aut = regexConverter.ConvertString(s);
            var prod = aut.Intersect(s_aut);
            return !prod.IsEmpty;
        }


        /// <summary>
        /// Generates a random string accepted by the automaton.
        /// The distribution is uniform: all strings accepted by the automaton are equally likely to be generated.
        /// </summary>
        /// <param name="aut">loopfree and deterministic automaton</param>
        /// <returns></returns>
        public string GenerateMemberUniformly(Automaton<BDD> aut)
        {
            if (!aut.IsLoopFree || !aut.IsDeterministic || aut.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeLoopFreeAndDeterministic);

            aut.ComputeProbabilities(ComputeDomainSize);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            var move = aut.ChooseTransitionUniformly(Chooser, aut.InitialState);
            while (move != null)
            {
                var c = ChooseUniformly(move.Label);
                sb.Append(c);
                move = aut.ChooseTransitionUniformly(Chooser, move.TargetState);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Identity function, returns s.
        /// </summary>
        public BDD ConvertFromCharSet(BDD s)
        {
            return s;
        }

        /// <summary>
        /// Returns this character set solver.
        /// </summary>
        public CharSetSolver CharSetProvider
        {
            get { return this; }
        }

        /// <summary>
        /// Returns pred.
        /// </summary>
        public BDD MkCharPredicate(string name, BDD pred)
        {
            return pred;
        }

        /// <summary>
        /// Save the automaton in dgml format and open the dgml file in a new process.
        /// The dgml file is opened in a new window in VS.
        /// </summary>
        /// <param name="aut">the automaton</param>
        /// <param name="name">a name for the dgml file</param>
        public void ShowGraph(Automaton<BDD> aut, string name)
        {
            var autwrap = new AutWrapper(aut, this);
            DirectedGraphs.DgmlWriter.ShowGraph<BDD>(-1, autwrap, name);
        }

        /// <summary>
        /// Shows the probabilities of choosing different moves according to uniform distribution of accepted strings.
        /// Saves the automaton in dgml format and opens the dgml file in a new process.
        /// The dgml file is opened in a new window in VS. 
        /// The automaton must be loopfree and deterministic.
        /// </summary>
        /// <param name="aut">the automaton</param>
        /// <param name="name">a name for the dgml file</param>
        public void ShowDAG(Automaton<BDD> aut, string name)
        {
            if (!aut.IsDeterministic || !aut.IsLoopFree || aut.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeLoopFreeAndDeterministic);

            aut.ComputeProbabilities(ComputeDomainSize);

            List<Move<string>> moves = new List<Move<string>>();
            foreach (var move in aut.GetMoves())
                moves.Add(Move<string>.Create(move.SourceState, move.TargetState, PrettyPrint(move.Label) + " (" + aut.GetProbability(move) + ")"));

            var autwrap = Automaton<string>.Create(null, aut.InitialState, aut.GetFinalStates(), moves);
            DirectedGraphs.DgmlWriter.ShowGraph<string>(-1, autwrap, name);
        }

        /// <summary>
        /// Save the automaton in dot format in the given file.
        /// </summary>
        /// <param name="aut">the automaton</param>
        /// <param name="name">a name for the dot file</param>
        public void SaveAsDot(Automaton<BDD> aut, string name, string file)
        {
            var autwrap = new AutWrapper(aut, this);
            DirectedGraphs.DotWriter.AutomatonToDot(this.PrettyPrint, autwrap, name, file, DirectedGraphs.DotWriter.RANKDIR.LR, 12, true);
        }

        /// <summary>
        /// Save the automaton in dgml format in the given file.
        /// </summary>
        /// <param name="aut">the automaton</param>
        /// <param name="name">a name for the dgml file</param>
        public void SaveAsDgml(Automaton<BDD> aut, string name)
        {
            var autwrap = new AutWrapper(aut, this);
            DirectedGraphs.DgmlWriter.AutomatonToDgml(-1, autwrap, name);
        }

        /// <summary>
        /// Create a string encoding of the given automaton with probabilities.
        /// </summary>
        /// <param name="aut">deterministic and loopfree automaton</param>
        public string SerializeDAG(Automaton<BDD> aut)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (!aut.IsDeterministic || !aut.IsLoopFree || aut.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeLoopFreeAndDeterministic);

            aut.ComputeProbabilities(ComputeDomainSize);

            var stateIdMap = new Dictionary<int, int>();

            //write the state probabilities, final states have non-zero probabilities
            //the i'th value is the probability of the i'th state
            for (int i = 0; i < aut.topsort.Count; i++ )
            {
                var st = aut.topsort[i];
                stateIdMap[st] = i;
                if (i > 0)
                    sb.Append(" ");
                sb.Append(aut.GetProbability(st).ToString());
            }

            sb.AppendLine();

            foreach (var st in aut.topsort)
            {

                foreach (var move in aut.GetMovesFrom(st))
                {
                    sb.Append(aut.GetProbability(move));
                    sb.Append(" ");
                    sb.Append(stateIdMap[move.SourceState]);
                    sb.Append(" ");
                    sb.Append(stateIdMap[move.TargetState]);
                    foreach (var range in this.ToRanges(move.Label))
                    {
                        sb.Append(" ");
                        sb.Append(range.Item1);
                        sb.Append(" ");
                        sb.Append(range.Item2);
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Recreate an automaton from a string that has been produced with SerializeDAG.
        /// </summary>
        /// <param name="dag">string encoding of a deterministic and loopfree automaton with probabilities</param>
        public Automaton<BDD> DeserializeDAG(string dag)
        {
            var lines = dag.Trim().Split('\n');
            var stateProbs = Array.ConvertAll(lines[0].Split(' '), double.Parse);
            var moves = new Move<BDD>[lines.Length-1];
            var moveProbs = new Dictionary<Move<BDD>, double>();
            for (int i = 1; i < lines.Length; i++)
            {
                var nums = lines[i].Split(' ');
                int s = int.Parse(nums[1]);
                int t = int.Parse(nums[2]);
                var ranges = new List<Tuple<uint, uint>>();
                for (int j = 3; j < nums.Length; j = j + 2)
                    ranges.Add(new Tuple<uint, uint>(uint.Parse(nums[j]), uint.Parse(nums[j + 1])));
                BDD set = MkCharSetFromRanges(ranges);
                moves[i-1] = Move<BDD>.Create(s, t, set);
                moveProbs[moves[i - 1]] = double.Parse(nums[0]);
            }
            List<int> finalStates = new List<int>();
            List<int> topsort = new List<int>();
            for (int i = 0; i < stateProbs.Length; i++)
            {
                topsort.Add(i);
                if (stateProbs[i] > 0.0)
                    finalStates.Add(i);
            }
            var aut = Automaton<BDD>.Create(this, 0, finalStates, moves);
            aut.isDeterministic = true;
            aut.isEpsilonFree = true;
            aut.didTopSort = true;
            aut.topsort = topsort;
            aut.probabilities = new Dictionary<int, double[]>();
            for (int q = 0; q < topsort.Count; q++)
            {
                var dd = new double[aut.OutDegree(q) + (aut.IsFinalState(q) ? 1 : 0)];
                for (int n = 0; n < aut.OutDegree(q); n++)
                    dd[n] = moveProbs[aut.GetNthMoveFrom(q, n)];
                if (aut.IsFinalState(q))
                    dd[dd.Length - 1] = stateProbs[q];
                aut.probabilities[q] = dd;
            }
            return aut;
        }

        /// <summary>
        /// Create a string encoding of the given automaton with probabilities.
        /// </summary>
        /// <param name="aut">deterministic and loopfree automaton</param>
        public string SerializeAutomaton(Automaton<BDD> aut)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.Append(""+aut.InitialState);
            sb.AppendLine();

            var first = true;
            foreach(var st in aut.GetFinalStates())
                if (first)
                {
                    sb.Append(st);
                    first = false;
                }
                else
                    sb.Append(" " + st);                
            sb.AppendLine();

            foreach (var st in aut.States)
            {
                foreach (var move in aut.GetMovesFrom(st))
                {
                    sb.Append(move.SourceState);
                    sb.Append(" ");
                    sb.Append(move.TargetState);
                    if (!move.IsEpsilon)
                        foreach (var range in this.ToRanges(move.Label))
                        {
                            sb.Append(" ");
                            sb.Append(range.Item1);
                            sb.Append(" ");
                            sb.Append(range.Item2);
                        }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Recreate an automaton from a string that has been produced with SerializeDAG.
        /// </summary>
        /// <param name="serialDesc">string encoding of a deterministic and loopfree automaton with probabilities</param>
        public Automaton<BDD> DeserializeAutomaton(string serialDesc)
        {
            var lines = serialDesc.Replace("\r", "").Split('\n');

            var moves = new Move<BDD>[lines.Length - 3];

            var initialState = int.Parse(lines[0]);
            var finalStates = Array.ConvertAll(lines[1].Split(' '), int.Parse);

            for (int i = 2; i < lines.Length-1; i++)
            {
                var l = lines[i];
                var nums = lines[i].Split(' ');
                int s = int.Parse(nums[0]);
                int t = int.Parse(nums[1]);
                if (nums.Length > 2)
                {
                    var ranges = new List<Tuple<uint, uint>>();
                    for (int j = 2; j < nums.Length; j = j + 2)
                        ranges.Add(new Tuple<uint, uint>(uint.Parse(nums[j]), uint.Parse(nums[j + 1])));
                    BDD set = MkCharSetFromRanges(ranges);
                    moves[i - 2] = Move<BDD>.Create(s, t, set);
                }
                else
                {
                    moves[i - 2] = Move<BDD>.Epsilon(s, t);
                }

            }
            return Automaton<BDD>.Create(this, initialState, finalStates, moves);
        }

        internal class AutWrapper : IAutomaton<BDD>
        {
            Automaton<BDD> aut;
            CharSetSolver solver;
            internal AutWrapper(Automaton<BDD> aut, CharSetSolver solver)
            {
                this.aut = aut;
                this.solver = solver;
            }



            #region IAutomaton<BvSet> Members

            public int InitialState
            {
                get { return aut.InitialState; }
            }

            public bool IsFinalState(int state)
            {
                return aut.IsFinalState(state);
            }

            public IEnumerable<Move<BDD>> GetMoves()
            {
                return aut.GetMoves();
            }

            public IEnumerable<int> GetStates()
            {
                return aut.GetStates();
            }

            public string DescribeState(int state)
            {
                if (aut.IsWordBoundary(state))
                    return "[" + state + "]";
                else
                    return aut.DescribeState(state);
            }

            public string DescribeLabel(BDD lab)
            {
                return solver.PrettyPrint(lab);
            }

            public string DescribeStartLabel()
            {
                return "" ;
            }

            public IEnumerable<Move<BDD>> GetMovesFrom(int state)
            {
                return aut.GetMovesFrom(state);
            }

            #endregion


            public IBooleanAlgebra<BDD> Algebra
            {
                get { return solver; }
            }
        }

        #region IPrettyPrinter<BvSet> Members


        public string PrettyPrintCS(BDD t, Func<BDD, string> varLookup)
        {
            return  PrettyPrint(t, varLookup);
        } 

        #endregion

        public IEnumerable<char> GenerateAllCharacters(BDD bvSet, bool inRevereseOrder = false)
        {
            foreach (var c in GenerateAllElements(bvSet, inRevereseOrder))
                yield return (char)c;
        }

        public IEnumerable<char> GenerateAllCharacters(BDD set)
        {
            return GenerateAllCharacters(set, false);
        }


        /// <summary>
        /// Calculate the number of elements in the set.
        /// </summary>
        /// <param name="set">the given set</param>
        /// <returns>the cardinality of the set</returns>
        public ulong ComputeDomainSize(BDD set)
        {
            var card = ComputeDomainSize(set, _bw - 1);
            return card;
        }

        /// <summary>
        /// Returns true iff the set contains exactly one element.
        /// </summary>
        /// <param name="set">the given set</param>
        /// <returns>true iff the set is a singleton</returns>
        public bool IsSingleton(BDD set)
        {
            var card = ComputeDomainSize(set, _bw - 1);
            return card == (long)1;
        }

        /// <summary>
        /// Convert the set into an equivalent array of ranges. The ranges are nonoverlapping and ordered.
        /// If limit > 0 then returns null if the total number of ranges exceeds limit.
        /// </summary>
        public Tuple<uint, uint>[] ToRanges(BDD set, int limit = 0)
        {
            return ToRanges(set, _bw - 1, limit);
        }

        /// <summary>
        /// Convert the set into an equivalent array of ranges and return the number of such ranges.
        /// </summary>
        public int GetRangeCount(BDD set)
        {
            return GetRangeCount(set, _bw - 1);
        }

        IEnumerable<uint> GenerateAllCharactersInOrder(BDD set)
        {
            var ranges = ToRanges(set);
            foreach (var range in ranges)
                for (uint i = range.Item1; i <= range.Item2; i++)
                    yield return (uint)i;
        }

        IEnumerable<uint> GenerateAllCharactersInReverseOrder(BDD set)
        {
            var ranges = ToRanges(set);
            for (int j = ranges.Length - 1; j >= 0; j--)
                for (uint i = ranges[j].Item2; i >= ranges[j].Item1; i--)
                    yield return (char)i;
        }

        /// <summary>
        /// Generate all characters that are members of the set in alphabetical order, smallest first, provided that inReverseOrder is false.
        /// </summary>
        /// <param name="set">the given set</param>
        /// <param name="inReverseOrder">if true the members are generated in reverse alphabetical order with the largest first, otherwise in alphabetical order</param>
        /// <returns>enumeration of all characters in the set, the enumeration is empty if the set is empty</returns>
        public IEnumerable<uint> GenerateAllElements(BDD set, bool inReverseOrder)
        {
            if (set == False)
                return GenerateNothing();
            else if (inReverseOrder)
                return GenerateAllCharactersInReverseOrder(set);
            else
                return GenerateAllCharactersInOrder(set);
        }

        IEnumerable<uint> GenerateNothing()
        {
            yield break;
        }


        /// <summary>
        /// Get the lexicographically maximum bitvector in the set. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="set">the given nonempty set</param>
        /// <returns>the lexicographically largest bitvector in the set</returns>
        public char GetMax(BDD set)
        {
            return (char)GetMax(set, _bw - 1);
        }

        public bool Contains(BDD set, char c)
        {
            return !MkAnd(set, MkCharSetFromRange(c, c)).IsEmpty;
        }


        public bool TryConvertToCharSet(BDD pred, out BDD set)
        {
            set = pred;
            return true;
        }

        public BDD ConvertToCharSet(IBDDAlgebra alg, BDD pred)
        {
            return pred;
        }

        public void SaveToFile(Automaton<BDD> A, string file)
        {

            System.IO.StreamWriter sw = new System.IO.StreamWriter(file);
            sw.WriteLine(A.InitialState);
            foreach (var s in A.GetFinalStates())
                sw.Write("{0} ", s);
            sw.WriteLine();
            foreach (var move in A.GetMoves())
                if (move.IsEpsilon)
                    sw.WriteLine("{0} {1} {2} {3}", move.SourceState, -1, -1, move.TargetState);
                else
                    foreach (var range in this.ToRanges(move.Label))
                        sw.WriteLine("{0} {1} {2} {3}", move.SourceState, (int)range.Item1, (int)range.Item2, move.TargetState);
            sw.Close();
        }

        public string SaveToString(Automaton<BDD> A)
        {

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(A.InitialState.ToString());
            foreach (var s in A.GetFinalStates())
                sb.AppendLine(string.Format("{0} ", s));
            sb.AppendLine();
            foreach (var move in A.GetMoves())
                if (move.IsEpsilon)
                    sb.AppendLine(string.Format("{0} {1} {2} {3}", move.SourceState, -1, -1, move.TargetState));
                else
                    foreach (var range in this.ToRanges(move.Label))
                        sb.AppendLine(string.Format("{0} {1} {2} {3}", move.SourceState, (int)range.Item1, (int)range.Item2, move.TargetState));
            return sb.ToString();
        }

        /// <summary>
        /// Returns the set of states touched by the input string
        /// </summary>
        public HashSet<int> GetCoveredStates(Automaton<BDD> aut, string input, bool checkContainment = true)
        {
            if (!aut.isDeterministic)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNondeterministic);

            HashSet<int> states = new HashSet<int>();

            int state = aut.InitialState;
            states.Add(state);
            foreach (var c in input.ToCharArray())
            {
                foreach (var move in aut.GetMovesFrom(state))

                    if (IsSatisfiable(MkAnd(move.Label, MkCharConstraint(c))))
                    {
                        states.Add(move.TargetState);
                        state = move.TargetState;
                    }
            }
            var finSt = new HashSet<int>(aut.GetFinalStates());
            if (checkContainment && !finSt.Contains(state))
                throw new AutomataException("Input string not in the language");
            return states;
        }

        /// <summary>
        /// Returns the set of states touched by the input string
        /// </summary>
        public HashSet<Move<BDD>> GetCoveredMoves(Automaton<BDD> aut, string input)
        {
            if (!aut.isDeterministic)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNondeterministic);

            HashSet<Move<BDD>> moves = new HashSet<Move<BDD>>();

            int state = aut.InitialState;
            foreach (var c in input.ToCharArray())
            {
                foreach (var move in aut.GetMovesFrom(state))

                    if (IsSatisfiable(MkAnd(move.Label, MkCharConstraint(c))))
                    {
                        moves.Add(move);
                        state = move.TargetState;
                    }
            }
            var finSt = new HashSet<int>(aut.GetFinalStates());
            if (!finSt.Contains(state))
                throw new AutomataException("Input string not in the language");
            return moves;
        }

        public Automaton<BDD> ReadFromFile(string file)
        {
            return Microsoft.Automata.Utilities.RegexToRangeAutomatonSerializer.Read(this, file);
        }

        public Automaton<BDD> ReadFromString(string automaton)
        {
            return Microsoft.Automata.Utilities.RegexToRangeAutomatonSerializer.ReadFromString(this, automaton);
        }

        /// <summary>
        /// Each transition has the form int[]{fromState, intervalStart, intervalEnd, toState}.
        /// If intervalStart = intervalEnd = -1 then this is an epsilon move.
        /// </summary>
        public Automaton<BDD> ReadFromRanges(int initialState, int[] finalStates, IEnumerable<int[]> transitions)
        {
            return Microsoft.Automata.Utilities.RegexToRangeAutomatonSerializer.ReadFromRanges(this, initialState, finalStates, transitions);
        }

        /// <summary>
        /// Shift right and set the leftmost bit to 0.
        /// </summary>
        public BDD LShiftRight(BDD set)
        {
            return ShiftRight0(set, this._bw - 1);
        }

        public bool TryGetMember(BDD predicate, out BDD member)
        {
            if (predicate.IsEmpty)
            {
                member = null;
                return false;
            }
            else
            {
                var memberVal = GetMin(predicate);
                member = MkSetFrom(memberVal, _bw - 1);
                return true;
            }
        }

        new public bool IsAtomic
        {
            get { return true; }
        }

        new public BDD GetAtom(BDD bdd)
        {
            if (bdd.IsEmpty)
                return bdd;

            var m = GetMin(bdd);
            var res = MkSet(m);
            return res;
        }

        #region code generation 

        public IMatcher ToCS(Automaton<BDD> automaton, bool OptimzeForAsciiInput = true, string classname = null, string namespacename = null)
        {
            return new Microsoft.Automata.Utilities.AutomataCSharpCompiler(automaton, classname, namespacename, OptimzeForAsciiInput).Compile();
        }

        public string ToCpp(Automaton<BDD>[] automata, bool exportIsMatch, bool OptimzeForAsciiInput = true, string classname = null, string namespacename = null)
        {
            var c = (classname == null ? "RegexMatcher" : classname);
            var n = (namespacename == null ? "GeneratedRegexMatchers" : namespacename);
            return new Microsoft.Automata.Utilities.CppCodeGenerator(this, c, n, exportIsMatch, OptimzeForAsciiInput, automata).GenerateMatcher();
        }

        public void ToCppFile(Automaton<BDD>[] automata, string path, bool exportIsMatch, bool OptimzeForAsciiInput = true, string classname = null, string namespacename = null)
        {
            var c = (classname == null ? "RegexMatcher" : classname);
            var n = (namespacename == null ? "GeneratedRegexMatchers" : namespacename);
            new Microsoft.Automata.Utilities.CppCodeGenerator(this, c, n, exportIsMatch, OptimzeForAsciiInput, automata).GenerateMatcherToFile(path);
        }

        public void ToCppFile(Regex[] regexes, string path, bool exportIsMatch, bool OptimzeForAsciiInput = true, string classname = null, string namespacename = null)
        {
            var c = (classname == null ? "RegexMatcher" : classname);
            var n = (namespacename == null ? "GeneratedRegexMatchers" : namespacename);
            new Microsoft.Automata.Utilities.CppCodeGenerator(this, c, n, exportIsMatch, OptimzeForAsciiInput, regexes).GenerateMatcherToFile(path);
        }

        public BDD[] GetPartition()
        {
            throw new NotSupportedException();
        }

        #endregion

        public string SerializePredicate(BDD s)
        {
            throw new NotImplementedException();
        }

        public BDD DeserializePredicate(string s)
        {
            throw new NotImplementedException();
        }
    }
}
