using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Automata.Utilities;
using System.Numerics;
using System.Linq;

namespace Microsoft.Automata
{
    /// <summary>
    /// Helper class of symbolic regex for finding matches
    /// </summary>
    /// <typeparam name="S"></typeparam>
    internal class SymbolicRegexMatcher<S>
    {
        SymbolicRegexBuilder<S> builder;

        /// <summary>
        /// Original regex.
        /// </summary>
        SymbolicRegex<S> A;

        /// <summary>
        /// Set of elements that matter as first element of A. 
        /// Characters that matter are mapped to 1. 
        /// Characters that dont matter are mapped to 0.
        /// </summary>
        BooleanDecisionTree A_StartSet;

        /// <summary>
        /// Number of elements in A_StartSet
        /// </summary>
        int A_StartSet_Size;

        /// <summary>
        /// Expilcit characters in A_StartSet if defined
        /// </summary>
        char[] A_StartSet_array = null;

        Vector<ushort> A_First_Vec;

        Vector<ushort> A_Second_Vec;

        Vector<ushort>[] A_StartSet_Vec;

        /// <summary>
        /// Set of first byte of UTF8 encoded characters.
        /// Characters that matter are mapped to true. 
        /// Characters that dont matter are mapped to false.
        /// This array has size 256.
        /// </summary>
        bool[] A_StartSetAsByteArray = new bool[256];

        /// <summary>
        /// if nonempty then A has that fixed prefix
        /// </summary>
        string A_prefix;

        /// <summary>
        /// predicate array corresponding to fixed prefix of A
        /// </summary>
        S[] A_prefix_array;

        /// <summary>
        /// if true then the fixed prefix of A is idependent of case
        /// </summary>
        bool A_fixedPrefix_ignoreCase;

        /// <summary>
        /// precomputed state of A1 that is reached after the fixed prefix of A
        /// </summary>
        int A1_skipState;

        /// <summary>
        /// precomputed regex of A1 that is reached after the fixed prefix of A
        /// </summary>
        SymbolicRegex<S> A1_skipStateRegex;

        /// <summary>
        /// Reverse(A).
        /// </summary>
        SymbolicRegex<S> Ar;

        /// <summary>
        /// if nonempty then Ar has that fixed prefix of predicates
        /// </summary>
        S[] Ar_prefix;

        /// <summary>
        /// precomputed state that is reached after the fixed prefix of Ar
        /// </summary>
        int Ar_skipState;

        /// <summary>
        /// precomputed regex that is reached after the fixed prefix of Ar
        /// </summary>
        SymbolicRegex<S> Ar_skipStateRegex;

        /// <summary>
        /// .*A
        /// </summary>
        SymbolicRegex<S> A1;

        /// <summary>
        /// Variant of A1 for matching.
        /// In A2 anchors have been removed. 
        /// Used only by IsMatch and when A contains anchors.
        /// </summary>
        SymbolicRegex<S> A2 = null;

        /// <summary>
        /// Used only by IsMatch and if A2 is used.
        /// </summary>
        int q0_A2 = 0;

        /// <summary>
        /// Initial state of A1 (0 is not used).
        /// </summary>
        int q0_A1 = 1;

        /// <summary>
        /// Initial state of Ar (0 is not used).
        /// </summary>
        int q0_Ar = 2;

        /// <summary>
        /// Initial state of A (0 is not used).
        /// </summary>
        int q0_A = 3;

        /// <summary>
        /// Next available state id.
        /// </summary>
        int nextStateId = 4;

        /// <summary>
        /// Initialized to atoms.Length.
        /// </summary>
        readonly int K;

        /// <summary>
        /// Partition of the input space of predicates.
        /// Length of atoms is K.
        /// </summary>
        S[] atoms;

        /// <summary>
        /// Maps each character into a partition id in the range 0..K-1.
        /// </summary>
        DecisionTree dt;

        /// <summary>
        /// Maps regexes to state ids
        /// </summary>
        Dictionary<SymbolicRegex<S>, int> regex2state = new Dictionary<SymbolicRegex<S>, int>();

        /// <summary>
        /// Maps states >= StateLimit to regexes.
        /// </summary>
        Dictionary<int, SymbolicRegex<S>> state2regexExtra = new Dictionary<int, SymbolicRegex<S>>();

        /// <summary>
        /// Maps states 1..(StateLimit-1) to regexes. 
        /// State 0 is not used but is reserved for denoting UNDEFINED value.
        /// Length of state2regex is StateLimit. Entry 0 is not used.
        /// </summary>
        SymbolicRegex<S>[] state2regex;

        /// <summary>
        /// Overflow from delta. Transitions with source state over the limit.
        /// Each entry (q, [p_0...p_n]) has n = atoms.Length-1 and represents the transitions q --atoms[i]--> p_i.
        /// All defined states are strictly positive, p_i==0 means that q --atoms[i]--> p_i is still undefined.
        /// </summary>
        Dictionary<int, int[]> deltaExtra = new Dictionary<int, int[]>();

        /// <summary>
        /// Bound on the maximum nr of states stored in array.
        /// </summary>
        internal readonly int StateLimit;

        /// <summary>
        /// Bound on the maximum nr of chars that trigger vectorized IndexOf.
        /// </summary>
        internal readonly int StartSetSizeLimit;

        /// <summary>
        /// Holds all transitions for states 1..MaxNrOfStates-1.
        /// each transition q ---atoms[i]---> p is represented by entry p = delta[(q * K) + i]. 
        /// Length of delta is K*StateLimit.
        /// </summary>
        int[] delta;

        /// <summary>
        /// Constructs matcher for given symbolic regex
        /// </summary>
        /// <param name="sr">given symbolic regex</param>
        /// <param name="StateLimit">limit on the number of states kept in a preallocated array (default is 1000)</param>
        internal SymbolicRegexMatcher(SymbolicRegex<S> sr, int StateLimit = 1000, int startSetSizeLimit = 256)
        {
            this.StartSetSizeLimit = startSetSizeLimit;
            this.builder = sr.builder;
            this.StateLimit = StateLimit;
            if (sr.Solver is BVAlgebra)
            {
                BVAlgebra bva = sr.Solver as BVAlgebra;
                atoms = bva.atoms as S[];
                dt = bva.dtree;
            }
            else if (sr.Solver is CharSetSolver)
            {
                atoms = sr.ComputeMinterms();
                dt = DecisionTree.Create(sr.Solver as CharSetSolver, atoms as BDD[]);
            }
            else
            {
                throw new NotSupportedException(string.Format("only {0} or {1} solver is supported", typeof(BVAlgebra), typeof(CharSetSolver)));
            }

            this.A = sr;
            this.Ar = sr.Reverse();
            this.A1 = sr.builder.MkConcat(sr.builder.dotStar, sr);
            this.regex2state[A1] = q0_A1;
            this.regex2state[Ar] = q0_Ar;
            this.regex2state[A] = q0_A;
            this.K = atoms.Length;
            this.delta = new int[K * StateLimit];
            this.state2regex = new SymbolicRegex<S>[StateLimit];
            if (q0_A1 < StateLimit)
            {
                this.state2regex[q0_A1] = A1;
            }
            else
            {
                this.state2regexExtra[q0_A1] = A1;
                this.deltaExtra[q0_A1] = new int[K];
            }

            if (q0_Ar < StateLimit)
            {
                this.state2regex[q0_Ar] = Ar;
            }
            else
            {
                this.state2regexExtra[q0_Ar] = Ar;
                this.deltaExtra[q0_Ar] = new int[K];
            }

            if (q0_A < StateLimit)
            {
                this.state2regex[q0_A] = A;
            }
            else
            {
                this.state2regexExtra[q0_A] = A;
                this.deltaExtra[q0_A] = new int[K];
            }

            BDD A_startSet_BDD = builder.solver.ConvertToCharSet(A.GetStartSet());
            var A_startSet_ranges = A_startSet_BDD.ToRanges();
            uint A_startSet_count = 0;
            for (int i = 0; i < A_startSet_ranges.Length; i++)
                A_startSet_count += A_startSet_ranges[i].Item2 - A_startSet_ranges[i].Item1 + 1;
            this.A_StartSet_Size = (int)A_startSet_count;
            if (A_StartSet_Size <= startSetSizeLimit)
            {
                this.A_StartSet_array = new char[A_StartSet_Size];
                int i = 0;
                foreach (char c in builder.solver.CharSetProvider.GenerateAllCharacters(A_startSet_BDD))
                    this.A_StartSet_array[i++] = c;
                A_StartSet_Vec = A_StartSet_array.Select(x => new Vector<ushort>(x)).ToArray();
            }
            this.A_StartSet = BooleanDecisionTree.Create(builder.solver.CharSetProvider, A_startSet_BDD);

            //consider the UTF8 encoded first byte
            for (ushort i = 0; i < 128; i++)
            {
                //relevant ASCII characters
                this.A_StartSetAsByteArray[i] = this.A_StartSet.Contains(i);
            }
            //to be on the safe side, set all other bytes to be relevant
            //TBD: set only those bytes to be relevant 
            //that are potentially the first byte encoding of a relevant character
            for (ushort i = 128; i < 256; i++)
            {
                //ASCII is not encoded
                this.A_StartSetAsByteArray[i] = true;
            }

            SymbolicRegex<S> tmp = A;
            this.A_prefix_array = A.GetPrefix();
            this.A_prefix = A.FixedPrefix;
            this.A_fixedPrefix_ignoreCase = A.IgnoreCaseOfFixedPrefix;
            this.A1_skipState = DeltaPlus(A_prefix, q0_A1, out tmp);
            this.A1_skipStateRegex = tmp;

            this.Ar_prefix = Ar.GetPrefix();
            var Ar_prefix_repr = new string(Array.ConvertAll(this.Ar_prefix, x => (char)sr.Solver.CharSetProvider.GetMin(sr.Solver.ConvertToCharSet(x))));
            this.Ar_skipState = DeltaPlus(Ar_prefix_repr, q0_Ar, out tmp);
            this.Ar_skipStateRegex = tmp;


            if (this.A_prefix.Length > 1)
            {
                var first = new List<char>(builder.solver.CharSetProvider.GenerateAllCharacters(
                    builder.solver.ConvertToCharSet(this.A_prefix_array[0])));
                var second = new List<char>(builder.solver.CharSetProvider.GenerateAllCharacters(
                   builder.solver.ConvertToCharSet(this.A_prefix_array[1])));

                ushort[] chars1 = new ushort[Vector<ushort>.Count];
                int i1 = 0;
                foreach (var c in first)
                    chars1[i1++] = c;
                //fill out the rest of the array with the first element
                if (i1 < Vector<ushort>.Count - 1)
                    while (i1 < Vector<ushort>.Count)
                        chars1[i1++] = chars1[0];
                this.A_First_Vec = new Vector<ushort>(chars1);

                ushort[] chars2 = new ushort[Vector<ushort>.Count];
                int i2 = 0;
                foreach (var c in second)
                    chars2[i2++] = c;
                //fill out the rest of the array with the first element
                if (i2 < Vector<ushort>.Count - 1)
                    while (i2 < Vector<ushort>.Count)
                        chars2[i2++] = chars2[0];
                this.A_Second_Vec = new Vector<ushort>(chars2);
            }
        }

        /// <summary>
        /// Return the state after the given input.
        /// </summary>
        /// <param name="input">given input</param>
        /// <param name="q">given start state</param>
        /// <param name="regex">regex of returned state</param>
        int DeltaPlus(string input, int q, out SymbolicRegex<S> regex)
        {
            if (string.IsNullOrEmpty(input))
            {
                regex = (q < StateLimit ? state2regex[q] : state2regexExtra[q]);
                return q;
            }
            else
            {
                char c = input[0];
                q = Delta(c, q, out regex);
                for (int i = 1; i < input.Length; i++)
                {
                    c = input[i];
                    q = Delta(c, q, out regex);
                }
                return q;
            }
        }


        /// <summary>
        /// Compute the target state for source state q and input character c.
        /// All uses of Delta must be inlined for efficiency. 
        /// This is the purpose of the MethodImpl(MethodImplOptions.AggressiveInlining) attribute.
        /// </summary>
        /// <param name="c">input character</param>
        /// <param name="q">state id of source regex</param>
        /// <param name="regex">target regex</param>
        /// <returns>state id of target regex</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Delta(int c, int q, out SymbolicRegex<S> regex)
        {
            int p;
            #region copy&paste region of the definition of Delta being inlined
            int atom_id = (dt.precomputed.Length > c ? dt.precomputed[c] : dt.bst.Find(c));
            S atom = atoms[atom_id];
            if (q < StateLimit)
            {
                #region use delta
                int offset = (q * K) + atom_id;
                p = delta[offset];
                if (p == 0)
                {
                    //p is undefined
                    var q_regex = state2regex[q];
                    var deriv = q_regex.MkDerivative(atom);
                    if (!regex2state.TryGetValue(deriv, out p))
                    {
                        p = nextStateId++;
                        regex2state[deriv] = p;
                        if (p < StateLimit)
                            state2regex[p] = deriv;
                        else
                            state2regexExtra[p] = deriv;
                        if (p >= StateLimit)
                            deltaExtra[p] = new int[K];
                    }
                    delta[offset] = p;
                    regex = deriv;
                }
                else
                {
                    regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                }
                #endregion
            }
            else
            {
                #region use deltaExtra
                int[] q_trans = deltaExtra[q];
                p = q_trans[atom_id];
                if (p == 0)
                {
                    //p is undefined
                    var q_regex = state2regexExtra[q];
                    var deriv = q_regex.MkDerivative(atom);
                    if (!regex2state.TryGetValue(deriv, out p))
                    {
                        p = nextStateId++;
                        regex2state[deriv] = p;
                        // we know at this point that p >= MaxNrOfStates
                        state2regexExtra[p] = deriv;
                        deltaExtra[p] = new int[K];
                    }
                    q_trans[atom_id] = p;
                    regex = deriv;
                }
                else
                {
                    regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                }
                #endregion
            }
            #endregion
            return p;
        }

        #region safe version of Matches and IsMatch for string input

        /// <summary>
        /// Generate all earliest maximal matches. We know that k is at least 2.
        /// <paramref name="input">pointer to input string</paramref>
        /// <paramref name="k">length of input string</paramref>
        /// </summary>
        internal Tuple<int, int>[] Matches(string input)
        {
            int k = input.Length;

            //stores the accumulated matches
            List<Tuple<int, int>> matches = new List<Tuple<int, int>>();

            //find the first accepting state
            //initial start position in the input is i = 0
            int i = 0;

            //after a match is found the match_start_boundary becomes 
            //the first postion after the last match
            //enforced when inlcude_overlaps == false
            int match_start_boundary = 0;

            //TBD: dont enforce match_start_boundary when match overlaps are allowed
            bool A_has_nonempty_prefix = (A.FixedPrefix != string.Empty);
            while (true)
            {
                int i_q0_A1;
                //optimize for the case when A starts with a fixed prefix
                i = (A_has_nonempty_prefix ?
                        FindFinalStatePositionOpt(input, i, out i_q0_A1) :
                        FindFinalStatePosition(input, i, out i_q0_A1));

                if (i == k)
                {
                    //end of input has been reached without reaching a final state, so no more matches
                    break;
                }

                int i_start = FindStartPosition(input, i, i_q0_A1);

                int i_end = FindEndPosition(input, i_start);

                var newmatch = new Tuple<int, int>(i_start, i_end + 1 - i_start);
                matches.Add(newmatch);

                //continue matching from the position following last match
                i = i_end + 1;
                match_start_boundary = i;
            }

            return matches.ToArray();
        }

        /// <summary>
        /// Returns true iff the input string matches A.
        /// </summary>
        internal bool IsMatch(string input)
        {
            int k = input.Length;

            if (this.A.containsAnchors)
            {
                //separate case when A contains anchors
                //TBD prefix optimization  ay still be important here 
                //but the prefix needs to be computed based on A ... but with start anchors removed or treated specially
                if (A2 == null)
                {
                    #region initialize A2 to A.RemoveAnchors()
                    this.A2 = A.ReplaceAnchors();
                    int qA2;
                    if (!regex2state.TryGetValue(this.A2, out qA2))
                    {
                        //the regex does not yet exist
                        qA2 = this.nextStateId++;
                        this.regex2state[this.A2] = qA2;
                    }
                    this.q0_A2 = qA2;
                    if (qA2 >= this.StateLimit)
                    {
                        this.deltaExtra[qA2] = new int[this.K];
                        this.state2regexExtra[qA2] = this.A2;
                    }
                    else
                    {
                        this.state2regex[qA2] = this.A2;
                    }



                    #endregion
                }

                int q = this.q0_A2;
                SymbolicRegex<S> regex = this.A2;
                int i = 0;

                while (i < k)
                {
                    int c = input[i];
                    int p;

                    //p = Delta(c, q, out regex);
                    #region copy&paste region of the definition of Delta being inlined
                    int atom_id = (dt.precomputed.Length > c ? dt.precomputed[c] : dt.bst.Find(c));
                    S atom = atoms[atom_id];
                    if (q < StateLimit)
                    {
                        #region use delta
                        int offset = (q * K) + atom_id;
                        p = delta[offset];
                        if (p == 0)
                        {
                            //p is undefined
                            var q_regex = state2regex[q];
                            var deriv = q_regex.MkDerivative(atom);
                            if (!regex2state.TryGetValue(deriv, out p))
                            {
                                p = nextStateId++;
                                regex2state[deriv] = p;
                                if (p < StateLimit)
                                    state2regex[p] = deriv;
                                else
                                    state2regexExtra[p] = deriv;
                                if (p >= StateLimit)
                                    deltaExtra[p] = new int[K];
                            }
                            delta[offset] = p;
                            regex = deriv;
                        }
                        else
                        {
                            regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                        }
                        #endregion
                    }
                    else
                    {
                        #region use deltaExtra
                        int[] q_trans = deltaExtra[q];
                        p = q_trans[atom_id];
                        if (p == 0)
                        {
                            //p is undefined
                            var q_regex = state2regexExtra[q];
                            var deriv = q_regex.MkDerivative(atom);
                            if (!regex2state.TryGetValue(deriv, out p))
                            {
                                p = nextStateId++;
                                regex2state[deriv] = p;
                                // we know at this point that p >= MaxNrOfStates
                                state2regexExtra[p] = deriv;
                                deltaExtra[p] = new int[K];
                            }
                            q_trans[atom_id] = p;
                            regex = deriv;
                        }
                        else
                        {
                            regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                        }
                        #endregion
                    }
                    #endregion

                    if (regex == regex.builder.dotStar) //(regex.IsEverything)
                    {
                        //the input is accepted no matter how the input continues
                        return true;
                    }
                    if (regex == regex.builder.nothing) //(regex.IsNothing)
                    {
                        //the input is rejected no matter how the input continues
                        return false;
                    }

                    //continue from the target state
                    q = p;
                    i += 1;
                }
                return regex.isNullable;
            }
            else
            {
                //reuse A1
                int i;
                int i_q0;
                if (A.FixedPrefix != string.Empty)
                {
                    i = FindFinalStatePositionOpt(input, 0, out i_q0);
                }
                else
                {
                    i = FindFinalStatePosition(input, 0, out i_q0);
                }
                if (i == k)
                {
                    //the search for final state exceeded the input, so final state was not found
                    return false;
                }
                else
                {
                    //since A has no anchors the pattern is really .*A.*
                    //thus if input[0...i] is in L(.*A) then input is in L(.*A.*)
                    return true;
                }
            }
        }

        /// <summary>
        /// Find match end position using A, end position is known to exist.
        /// </summary>
        /// <param name="input">input array</param>
        /// <param name="i">start position</param>
        /// <returns></returns>
        private int FindEndPosition(string input, int i)
        {
            int k = input.Length;
            int i_end = k;
            int q = q0_A;
            while (i < k)
            {
                SymbolicRegex<S> regex;
                int c = input[i];
                int p;
                //TBD: anchors
                //p = Delta(c, q, out regex);
                #region copy&paste region of the definition of Delta being inlined
                int atom_id = (dt.precomputed.Length > c ? dt.precomputed[c] : dt.bst.Find(c));
                S atom = atoms[atom_id];
                if (q < StateLimit)
                {
                    #region use delta
                    int offset = (q * K) + atom_id;
                    p = delta[offset];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regex[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            if (p < StateLimit)
                                state2regex[p] = deriv;
                            else
                                state2regexExtra[p] = deriv;
                            if (p >= StateLimit)
                                deltaExtra[p] = new int[K];
                        }
                        delta[offset] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                else
                {
                    #region use deltaExtra
                    int[] q_trans = deltaExtra[q];
                    p = q_trans[atom_id];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regexExtra[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            // we know at this point that p >= MaxNrOfStates
                            state2regexExtra[p] = deriv;
                            deltaExtra[p] = new int[K];
                        }
                        q_trans[atom_id] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                #endregion

                if (regex.isNullable)
                {
                    //accepting state has been reached
                    //record the position 
                    i_end = i;
                }
                else if (regex == builder.nothing)
                {
                    //nonaccepting sink state (deadend) has been reached in A
                    //so the match ended when the last i_end was updated
                    break;
                }
                q = p;
                i += 1;
            }
            if (i_end == k)
                throw new AutomataException(AutomataExceptionKind.InternalError);
            return i_end;
        }

        /// <summary>
        /// Walk back in reverse using Ar to find the start position of match, start position is known to exist.
        /// </summary>
        /// <param name="input">the input array</param>
        /// <param name="i">position to start walking back from, i points at the last character of the match</param>
        /// <param name="match_start_boundary">do not pass this boundary when walking back</param>
        /// <returns></returns>
        private int FindStartPosition(string input, int i, int match_start_boundary)
        {
            int q = q0_Ar;
            SymbolicRegex<S> regex = null;
            //A_r may have a fixed sequence
            if (this.Ar_prefix.Length > 0)
            {
                //skip back the prefix portion of Ar
                q = this.Ar_skipState;
                regex = this.Ar_skipStateRegex;
                i = i - this.Ar_prefix.Length;
            }
            if (i == -1)
            {
                //we reached the beginning of the input, thus the state q must be accepting
                if (!regex.isNullable)
                    throw new AutomataException(AutomataExceptionKind.InternalError);
                return 0;
            }

            int last_start = -1;
            if (regex != null && regex.isNullable)
            {
                //the whole prefix of Ar was in reverse a prefix of A
                last_start = i + 1;
            }

            //walk back to the accepting state of Ar
            int p;
            int c;
            while (i >= match_start_boundary)
            {
                //observe that the input is reversed 
                //so input[k-1] is the first character 
                //and input[0] is the last character
                //TBD: anchors
                c = input[i];

                //p = Delta(c, q, out regex);
                #region copy&paste region of the definition of Delta being inlined
                int atom_id = (dt.precomputed.Length > c ? dt.precomputed[c] : dt.bst.Find(c));
                S atom = atoms[atom_id];
                if (q < StateLimit)
                {
                    #region use delta
                    int offset = (q * K) + atom_id;
                    p = delta[offset];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regex[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            if (p < StateLimit)
                                state2regex[p] = deriv;
                            else
                                state2regexExtra[p] = deriv;
                            if (p >= StateLimit)
                                deltaExtra[p] = new int[K];
                        }
                        delta[offset] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                else
                {
                    #region use deltaExtra
                    int[] q_trans = deltaExtra[q];
                    p = q_trans[atom_id];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regexExtra[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            // we know at this point that p >= MaxNrOfStates
                            state2regexExtra[p] = deriv;
                            deltaExtra[p] = new int[K];
                        }
                        q_trans[atom_id] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                #endregion

                if (regex.isNullable)
                {
                    //earliest start point so far
                    //this must happen at some point 
                    //or else A1 would not have reached a 
                    //final state after match_start_boundary
                    last_start = i;
                    //TBD: under some conditions we can break here
                    //break;
                }
                else if (regex == this.builder.nothing)
                {
                    //the previous i_start was in fact the earliest
                    break;
                }
                q = p;
                i -= 1;
            }
            if (last_start == -1)
                throw new AutomataException(AutomataExceptionKind.InternalError);
            return last_start;
        }

        /// <summary>
        /// Return the position of the last character that leads to a final state in A1
        /// </summary>
        /// <param name="input">given input array</param>
        /// <param name="i">start position</param>
        /// <param name="i_q0">last position the initial state of A1 was visited</param>
        /// <returns></returns>
        private int FindFinalStatePosition(string input, int i, out int i_q0)
        {
            int k = input.Length;
            int q = q0_A1;
            int i_q0_A1 = i;

            while (i < k)
            {
                if (q == q0_A1)
                {
                    i = IndexOfStartset(input, i);

                    if (i == -1)
                    {
                        i_q0 = i_q0_A1;
                        return k;
                    }
                    i_q0_A1 = i;
                }

                //TBD: anchors
                SymbolicRegex<S> regex;
                int c = input[i];
                int p;

                //p = Delta(c, q, out regex);
                #region copy&paste region of the definition of Delta being inlined
                int atom_id = (dt.precomputed.Length > c ? dt.precomputed[c] : dt.bst.Find(c));
                S atom = atoms[atom_id];
                if (q < StateLimit)
                {
                    #region use delta
                    int offset = (q * K) + atom_id;
                    p = delta[offset];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regex[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            if (p < StateLimit)
                                state2regex[p] = deriv;
                            else
                                state2regexExtra[p] = deriv;
                            if (p >= StateLimit)
                                deltaExtra[p] = new int[K];
                        }
                        delta[offset] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                else
                {
                    #region use deltaExtra
                    int[] q_trans = deltaExtra[q];
                    p = q_trans[atom_id];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regexExtra[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            // we know at this point that p >= MaxNrOfStates
                            state2regexExtra[p] = deriv;
                            deltaExtra[p] = new int[K];
                        }
                        q_trans[atom_id] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                #endregion

                if (regex.isNullable)
                {
                    //p is a final state so match has been found
                    break;
                }
                else if (regex == regex.builder.nothing)
                {
                    //p is a deadend state so any further search is meaningless
                    i_q0 = i_q0_A1;
                    return k;
                }

                //continue from the target state
                q = p;
                i += 1;
            }
            i_q0 = i_q0_A1;
            return i;
        }

        /// <summary>
        /// FindFinalState optimized for the case when A starts with a fixed prefix
        /// </summary>
        private int FindFinalStatePositionOpt(string input, int i, out int i_q0)
        {
            int k = input.Length;
            int q = q0_A1;
            int i_q0_A1 = i;
            var prefix = this.A_prefix;
            //it is important to use Ordinal/OrdinalIgnoreCase to avoid culture dependent semantics of IndexOf
            StringComparison comparison = (this.A_fixedPrefix_ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            while (i < k)
            {
                SymbolicRegex<S> regex = null;

                // ++++ the following prefix optimization can be commented out without affecting correctness ++++
                // but this optimization has a huge perfomance boost when fixed prefix exists .... in the order of 10x
                //
                #region prefix optimization 
                //stay in the initial state if the prefix does not match
                //thus advance the current position to the 
                //first position where the prefix does match
                if (q == q0_A1)
                {
                    i_q0_A1 = i;

                    //i = IndexOf(input, prefix, i, this.A_fixedPrefix_ignoreCase);
                    i = input.IndexOf(prefix, i, comparison);

                    if (i == -1)
                    {
                        //if a matching position does not exist then IndexOf returns -1
                        //so set i = k to match the while loop behavior
                        i = k;
                        break;
                    }
                    else
                    {
                        //compute the end state for the A prefix
                        //skip directly to the resulting state
                        // --- i.e. does the loop ---
                        //for (int j = 0; j < prefix.Length; j++)
                        //    q = Delta(prefix[j], q, out regex);
                        // ---
                        q = this.A1_skipState;
                        regex = this.A1_skipStateRegex;

                        //skip the prefix
                        i = i + prefix.Length;
                        if (regex.isNullable)
                        {
                            i_q0 = i_q0_A1;
                            //return the last position of the match
                            return i - 1;
                        }
                        if (i == k)
                        {
                            i_q0 = i_q0_A1;
                            return k;
                        }
                    }
                }
                #endregion

                //TBD: anchors
                int c = input[i];
                int p;

                //p = Delta(c, q, out regex);
                #region copy&paste region of the definition of Delta being inlined
                int atom_id = (dt.precomputed.Length > c ? dt.precomputed[c] : dt.bst.Find(c));
                S atom = atoms[atom_id];
                if (q < StateLimit)
                {
                    #region use delta
                    int offset = (q * K) + atom_id;
                    p = delta[offset];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regex[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            if (p < StateLimit)
                                state2regex[p] = deriv;
                            else
                                state2regexExtra[p] = deriv;
                            if (p >= StateLimit)
                                deltaExtra[p] = new int[K];
                        }
                        delta[offset] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                else
                {
                    #region use deltaExtra
                    int[] q_trans = deltaExtra[q];
                    p = q_trans[atom_id];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regexExtra[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            // we know at this point that p >= MaxNrOfStates
                            state2regexExtra[p] = deriv;
                            deltaExtra[p] = new int[K];
                        }
                        q_trans[atom_id] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                #endregion

                if (regex.isNullable)
                {
                    //p is a final state so match has been found
                    break;
                }
                else if (regex == regex.builder.nothing)
                {
                    i_q0 = i_q0_A1;
                    //p is a deadend state so any further saerch is meaningless
                    return k;
                }

                //continue from the target state
                q = p;
                i += 1;
            }
            i_q0 = i_q0_A1;
            return i;
        }

        /// <summary>
        ///  Find first occurrence of s in input starting from index i.
        /// </summary>
        /// <param name="input">input string to search in</param>
        /// <param name="k">length of input string</param>
        /// <param name="s">the substring that is searched for</param>
        /// <param name="i">the start index in input</param>
        /// <param name="caseInsensitive">if true then the search is case insensitive</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IndexOf(string input, string s, int i, bool caseInsensitive)
        {
            //TBD: StringComparison.OrdinalIgnoreCase works incorrectly when s includes a unicode case-equivalent I, or K
            //and the pattern s includes I or K, the match in s will then not be found
            return input.IndexOf(s, i, (caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        }

        /// <summary>
        ///  Find first occurrence of startset element in input starting from index i.
        /// </summary>
        /// <param name="input">input string to search in</param>
        /// <param name="i">the start index in input to search from</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IndexOfStartset(string input, int i)
        {
            int k = input.Length;
            int start_pos = -1;
            while (i < k)
            {
                char c = input[i];
                bool res = this.A_StartSet.Contains(c);
                if (res)
                {
                    start_pos = i;
                    break;
                }
                i += 1;
            }
            return start_pos;
        }

        #endregion

        #region unsafe version of Matches for string input

        /// <summary>
        /// Generate all earliest maximal matches. We know that k is at least 2.
        /// <paramref name="input">pointer to input string</paramref>
        /// <paramref name="k">length of input string</paramref>
        /// </summary>
        internal Tuple<int, int>[] Matches_(string input)
        {
            int k = input.Length;

            //stores the accumulated matches
            List<Tuple<int, int>> matches = new List<Tuple<int, int>>();

            //find the first accepting state
            //initial start position in the input is i = 0
            int i = 0;

            //after a match is found the match_start_boundary becomes 
            //the first postion after the last match
            //enforced when inlcude_overlaps == false
            int match_start_boundary = 0;

            //TBD: dont enforce match_start_boundary when match overlaps are allowed
            bool A_has_nonempty_prefix = (A.FixedPrefix != string.Empty);
            while (true)
            {
                int i_q0_A1;
                if (A_has_nonempty_prefix)
                {
                    i = FindFinalStatePositionOpt_(input, i, out i_q0_A1);
                }
                else
                {
                    i = FindFinalStatePosition_(input, i, out i_q0_A1);
                }

                if (i == k)
                {
                    //end of input has been reached without reaching a final state, so no more matches
                    break;
                }

                int i_start = FindStartPosition(input, i, i_q0_A1);

                int i_end = FindEndPosition(input, i_start);

                var newmatch = new Tuple<int, int>(i_start, i_end + 1 - i_start);
                matches.Add(newmatch);

                //continue matching from the position following last match
                i = i_end + 1;
                match_start_boundary = i;
            }

            return matches.ToArray();
        }

        /// <summary>
        /// Return the position of the last character that leads to a final state in A1
        /// </summary>
        /// <param name="input">given input array</param>
        /// <param name="i">start position</param>
        /// <param name="i_q0">last position the initial state of A1 was visited</param>
        /// <returns></returns>
        private int FindFinalStatePosition_(string input, int i, out int i_q0)
        {
            int lastI = 0;
            int k = input.Length;
            int q = q0_A1;
            int i_q0_A1 = i;

            while (i < k)
            {
                if (q == q0_A1)
                {
                    if (this.A_StartSet_array == null)
                        i = IndexOfStartset(input, i);
                    else
                        i = VectorizedIndexOf.UnsafeIndexOf(input, i, this.A_StartSet, A_StartSet_Vec);

                    if (i == -1)
                    {
                        i_q0 = i_q0_A1;
                        return k;
                    }
                    i_q0_A1 = i;
                }

                //TBD: anchors
                SymbolicRegex<S> regex;
                int c = input[i];
                int p;

                //p = Delta(c, q, out regex);
                #region copy&paste region of the definition of Delta being inlined
                int atom_id = (dt.precomputed.Length > c ? dt.precomputed[c] : dt.bst.Find(c));
                S atom = atoms[atom_id];
                if (q < StateLimit)
                {
                    #region use delta
                    int offset = (q * K) + atom_id;
                    p = delta[offset];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regex[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            if (p < StateLimit)
                                state2regex[p] = deriv;
                            else
                                state2regexExtra[p] = deriv;
                            if (p >= StateLimit)
                                deltaExtra[p] = new int[K];
                        }
                        delta[offset] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                else
                {
                    #region use deltaExtra
                    int[] q_trans = deltaExtra[q];
                    p = q_trans[atom_id];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regexExtra[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            // we know at this point that p >= MaxNrOfStates
                            state2regexExtra[p] = deriv;
                            deltaExtra[p] = new int[K];
                        }
                        q_trans[atom_id] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                #endregion

                if (regex.isNullable)
                {
                    //p is a final state so match has been found
                    break;
                }
                else if (regex == regex.builder.nothing)
                {
                    //p is a deadend state so any further search is meaningless
                    i_q0 = i_q0_A1;
                    return k;
                }

                //continue from the target state
                q = p;
                i += 1;
            }
            i_q0 = i_q0_A1;
            return i;
        }

        /// <summary>
        /// FindFinalState optimized for the case when A starts with a fixed prefix
        /// </summary>
        private int FindFinalStatePositionOpt_(string input, int i, out int i_q0)
        {
            int k = input.Length;
            int q = q0_A1;
            int i_q0_A1 = i;
            var prefix = this.A_prefix;
            //it is important to use Ordinal/OrdinalIgnoreCase to avoid culture dependent semantics of IndexOf
            StringComparison comparison = (this.A_fixedPrefix_ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            while (i < k)
            {
                SymbolicRegex<S> regex = null;

                // ++++ the following prefix optimization can be commented out without affecting correctness ++++
                // but this optimization has a huge perfomance boost when fixed prefix exists .... in the order of 10x
                //
                #region prefix optimization 
                //stay in the initial state if the prefix does not match
                //thus advance the current position to the 
                //first position where the prefix does match
                if (q == q0_A1)
                {
                    i_q0_A1 = i;

                    i = IndexOf_(input, prefix, i, this.A_fixedPrefix_ignoreCase);

                    if (i == -1)
                    {
                        //if a matching position does not exist then IndexOf returns -1
                        //so set i = k to match the while loop behavior
                        i = k;
                        break;
                    }
                    else
                    {
                        //compute the end state for the A prefix
                        //skip directly to the resulting state
                        // --- i.e. does the loop ---
                        //for (int j = 0; j < prefix.Length; j++)
                        //    q = Delta(prefix[j], q, out regex);
                        // ---
                        q = this.A1_skipState;
                        regex = this.A1_skipStateRegex;

                        //skip the prefix
                        i = i + prefix.Length;
                        if (regex.isNullable)
                        {
                            i_q0 = i_q0_A1;
                            //return the last position of the match
                            return i - 1;
                        }
                        if (i == k)
                        {
                            i_q0 = i_q0_A1;
                            return k;
                        }
                    }
                }
                #endregion

                //TBD: anchors
                int c = input[i];
                int p;

                //p = Delta(c, q, out regex);
                #region copy&paste region of the definition of Delta being inlined
                int atom_id = (dt.precomputed.Length > c ? dt.precomputed[c] : dt.bst.Find(c));
                S atom = atoms[atom_id];
                if (q < StateLimit)
                {
                    #region use delta
                    int offset = (q * K) + atom_id;
                    p = delta[offset];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regex[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            if (p < StateLimit)
                                state2regex[p] = deriv;
                            else
                                state2regexExtra[p] = deriv;
                            if (p >= StateLimit)
                                deltaExtra[p] = new int[K];
                        }
                        delta[offset] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                else
                {
                    #region use deltaExtra
                    int[] q_trans = deltaExtra[q];
                    p = q_trans[atom_id];
                    if (p == 0)
                    {
                        //p is undefined
                        var q_regex = state2regexExtra[q];
                        var deriv = q_regex.MkDerivative(atom);
                        if (!regex2state.TryGetValue(deriv, out p))
                        {
                            p = nextStateId++;
                            regex2state[deriv] = p;
                            // we know at this point that p >= MaxNrOfStates
                            state2regexExtra[p] = deriv;
                            deltaExtra[p] = new int[K];
                        }
                        q_trans[atom_id] = p;
                        regex = deriv;
                    }
                    else
                    {
                        regex = (p < StateLimit ? state2regex[p] : state2regexExtra[p]);
                    }
                    #endregion
                }
                #endregion

                if (regex.isNullable)
                {
                    //p is a final state so match has been found
                    break;
                }
                else if (regex == regex.builder.nothing)
                {
                    i_q0 = i_q0_A1;
                    //p is a deadend state so any further saerch is meaningless
                    return k;
                }

                //continue from the target state
                q = p;
                i += 1;
            }
            i_q0 = i_q0_A1;
            return i;
        }

        #endregion

        #region Specialized IndexOf
        /// <summary>
        ///  Find first occurrence of value in input starting from index i.
        /// </summary>
        /// <param name="input">input array to search in</param>
        /// <param name="value">nonempty subarray that is searched for</param>
        /// <param name="i">the search start index in input</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IndexOf(byte[] input, byte[] value, int i)
        {
            int n = value.Length;
            int k = (input.Length - n) + 1;
            while (i < k)
            {
                i = Array.IndexOf<byte>(input, value[0], i);
                if (i == -1)
                    return -1;
                int j = 1;
                while (j < n && input[i + j] == value[j])
                    j += 1;
                if (j == n)
                    return i;
                i += 1;
            }
            return -1;
        }

        /// <summary>
        ///  Find first occurrence of byte in input starting from index i that maps to true by the predicate.
        /// </summary>
        /// <param name="input">input array to search in</param>
        /// <param name="pred">boolean array of size 256 telling which bytes to match</param>
        /// <param name="i">the search start index in input</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IndexOf(byte[] input, bool[] pred, int i)
        {
            int k = input.Length;
            while (i < k && !pred[input[i]])
                i += 1;
            return (i == k ? -1 : i);
        }

        /// <summary>
        ///  Find first occurrence of s in input starting from index i.
        /// </summary>
        /// <param name="input">input string to search in</param>
        /// <param name="k">length of input string</param>
        /// <param name="substring">the substring that is searched for</param>
        /// <param name="i">the start index in input</param>
        /// <param name="caseInsensitive">if true then the search is case insensitive</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IndexOf_(string input, string substring, int i, bool caseInsensitive)
        {
            int k = input.Length;
            int l = substring.Length;
            int k1 = k - l + 1;
            ushort firstchar = substring[0];
            while (i < k1)
            {
                if (substring.Length == 1)
                    i = VectorizedIndexOf.UnsafeIndexOf1(input, i, firstchar, A_StartSet_Vec[0]);
                else
                    i = VectorizedIndexOf.UnsafeIndexOf2(input, i, substring, A_First_Vec, A_Second_Vec);
                if (i == -1)
                    return -1;
                int j = 1;
                while (j < l && input[i + j] == substring[j])
                    j += 1;
                if (j == l)
                    return i;

                i += 1;
            }
            return -1;

            //TBD: StringComparison.OrdinalIgnoreCase works incorrectly when s includes a unicode case-equivalent I, or K
            //and the pattern s includes I or K, the match in s will then not be found
            //return input.IndexOf(substring, i, (caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        }
        #endregion
    }
}
