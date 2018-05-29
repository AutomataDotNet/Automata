using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Automata
{
    /// <summary>
    /// Helper class of symbolic regex for finding matches
    /// </summary>
    /// <typeparam name="S"></typeparam>
    internal class SymbolicRegexMatcher<S>
    {
        /// <summary>
        /// Original regex.
        /// </summary>
        SymbolicRegex<S> A;

        /// <summary>
        /// Reverse(A).
        /// </summary>
        SymbolicRegex<S> Ar;

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
        internal SymbolicRegexMatcher(SymbolicRegex<S> sr, int StateLimit = 1000)
        {
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

        /// <summary>
        /// Generate all earliest maximal matches. We know that inputStr.Length is at least 2.
        /// TBD: different options.
        /// </summary>
        internal Tuple<int, int>[] FindMatches(string input, bool inlcude_overlaps)
        {
            //char[] input = inputStr.ToCharArray();

            //stores the accumulated matches
            List<Tuple<int, int>> matches = new List<Tuple<int, int>>();

            //find the first accepting state
            //initial start position in the input is i = 0
            int i = 0;
            int k = input.Length;

            //after a match is found the match_start_boundary becomes 
            //the first postion after the last match
            //enforced when inlcude_overlaps == false
            int match_start_boundary = 0;

            //optimize for the case when A starts with a fixed prefix
            if (A.FixedPrefix != string.Empty)
            {
                //specific top level loop that finds all the matches when 
                //A starts with a noenmpty fixed string that must be matched
                while (true)
                {
                    i = FindFinalState1(input, i);
                    if (i == k)
                    {
                        //end of input has been reached without reaching a final state, so no more matches
                        break;
                    }

                    int i_start = FindStartPosition(input, i, match_start_boundary);

                    int i_end = FindEndPosition(input, i_start);

                    var newmatch = new Tuple<int, int>(i_start, i_end + 1 - i_start);
                    matches.Add(newmatch);

                    //continue matching from the position following last match
                    i = i_end + 1;
                    //enforce match_start_boundary when inlcude_overlaps == false
                    if (!inlcude_overlaps)
                        match_start_boundary = i;
                }
            }
            else
            {
                //general top level loop that finds all the matches
                while (true)
                {
                    i = FindFinalState(input, i);
                    if (i == k)
                    {
                        //end of input has been reached without reaching a final state, so no more matches
                        break;
                    }

                    int i_start = FindStartPosition(input, i, match_start_boundary);

                    int i_end = FindEndPosition(input, i_start);

                    var newmatch = new Tuple<int, int>(i_start, i_end + 1 - i_start);
                    matches.Add(newmatch);

                    //continue matching from the position following last match
                    i = i_end + 1;
                    //enforce match_start_boundary when inlcude_overlaps == false
                    if (!inlcude_overlaps)
                        match_start_boundary = i;
                }
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

                if (A2.FixedPrefix != string.Empty)
                {
                    #region prefix optimization
                    var prefix = A2.FixedPrefix;
                    //it is important to use Ordinal/OrdinalIgnoreCase to avoid culture dependent semantics of IndexOf
                    StringComparison comparison = (A2.IgnoreCaseOfFixedPrefix ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                    i = input.IndexOf(prefix, 0, comparison);

                    if (i == -1)
                    {
                        return false;
                    }
                    else
                    {
                        //compute the end state for the prefix
                        for (int j = 0; j < prefix.Length; j++)
                        {
                            q = Delta(input[i], q, out regex);
                            i += 1;
                        }
                    }
                    #endregion
                }

                while (i < k)
                {
                    int c = input[i];
                    int p;

                    p = Delta(c, q, out regex);

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
                if (A1.FixedPrefix != string.Empty)
                {
                    i = FindFinalState1(input, 0);
                }
                else
                {
                    i = FindFinalState(input, 0);
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
                p = Delta(c, q, out regex);

                if (regex.isNullable)
                {
                    //accepting state has been reached
                    //record the position 
                    i_end = i;
                }
                else if (regex.IsNothing)
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
        /// <param name="i">position to start walking back from</param>
        /// <param name="match_start_boundary">do not pass this boundary when walking back</param>
        /// <returns></returns>
        private int FindStartPosition(string input, int i, int match_start_boundary)
        {
            int i_start = -1;
            int q = q0_Ar;
            while (i >= match_start_boundary)
            {
                //observe that the input is reversed 
                //so input[k-1] is the first character 
                //and input[0] is the last character
                //TBD: anchors
                SymbolicRegex<S> regex;
                int c = input[i];
                int p;
                p = Delta(c, q, out regex);

                if (regex.isNullable)
                {
                    //earliest start point so far
                    //TBD: one option is to break here?
                    //this must happen at some point 
                    //or else A1 would not have reached a 
                    //final state after match_start_boundary
                    i_start = i;
                }
                else if (regex.IsNothing)
                {
                    //the previous i_start was in fact the earliest
                    break;
                }
                q = p;
                i -= 1;
            }
            if (i_start == -1)
                throw new AutomataException(AutomataExceptionKind.InternalError);
            return i_start;
        }

        /// <summary>
        /// Return the position of the last character that leads to a final state in A1
        /// </summary>
        /// <param name="input">given input array</param>
        /// <param name="i">start position</param>
        /// <returns></returns>
        private int FindFinalState(string input, int i)
        {
            int k = input.Length;
            int q = q0_A1;
            while (i < k)
            {
                //TBD: anchors
                SymbolicRegex<S> regex;
                int c = input[i];
                int p;

                p = Delta(c, q, out regex);

                if (regex.isNullable)
                {
                    //p is a final state so match has been found
                    break;
                }
                else if (regex == regex.builder.nothing)
                {
                    //p is a deadend state so any further saerch is meaningless
                    return k;
                }

                //continue from the target state
                q = p;
                i += 1;
            }
            return i;
        }

        /// <summary>
        /// FindFinalState optimized for the case when A starts with a fixed prefix
        /// </summary>
        private int FindFinalState1(string input, int i)
        {
            int k = input.Length;
            int q = q0_A1;
            var prefix = A.FixedPrefix;
            //it is important to use Ordinal/OrdinalIgnoreCase to avoid culture dependent semantics of IndexOf
            StringComparison comparison = (A.IgnoreCaseOfFixedPrefix ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
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
                        //compute the end state for the prefix
                        for (int j = 0; j < prefix.Length; j++)
                            q = Delta(prefix[j], q, out regex);
                        //skip the prefix
                        i = i + prefix.Length;
                        if (regex.isNullable)
                            //return the last position of the match
                            return i - 1;
                        if (i == k)
                            return k;
                    }
                }
                #endregion

                //TBD: anchors
                int c = input[i];
                int p;

                p = Delta(c, q, out regex);

                if (regex.isNullable)
                {
                    //p is a final state so match has been found
                    break;
                }
                else if (regex == regex.builder.nothing)
                {
                    //p is a deadend state so any further saerch is meaningless
                    return k;
                }

                //continue from the target state
                q = p;
                i += 1;
            }
            return i;
        }
    }
}
