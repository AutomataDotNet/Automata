using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Automata.BooleanAlgebras;

namespace Microsoft.Automata
{
    /// <summary>
    /// Counting-set Automaton
    /// </summary>
    public class CsAutomaton<S> : Automaton<CsLabel<S>>
    {
        PowerSetStateBuilder stateBuilder;
        Dictionary<int, ICounter> countingStates;
        HashSet<int> origFinalStates;

        CsAlgebra<S> productAlgebra;

        /// <summary>
        /// Underlying Cartesian product algebra
        /// </summary>
        public CsAlgebra<S> ProductAlgebra
        {
            get
            {
                return productAlgebra;
            }
        }

        Dictionary<int, HashSet<int>> activeCounterMap;

        HashSet<int> finalCounterSet;


        ICounter[] counters;

        public CsAutomaton(CsAlgebra<S> productAlgebra, Automaton<CsLabel<S>> aut, PowerSetStateBuilder stateBuilder, Dictionary<int, ICounter> countingStates, HashSet<int> origFinalStates) : base(aut)
        {
            this.stateBuilder = stateBuilder;
            this.countingStates = countingStates;
            this.origFinalStates = origFinalStates;
            activeCounterMap = new Dictionary<int, HashSet<int>>();
            finalCounterSet = new HashSet<int>();
            counters = new ICounter[countingStates.Count];
            foreach (var q in aut.States)
            {
                var q_set = new HashSet<int>();
                activeCounterMap[q] = q_set;
                foreach (var mem in stateBuilder.GetMembers(q))
                {
                    if (countingStates.ContainsKey(mem))
                    {
                        var counterId = countingStates[mem].CounterId;
                        q_set.Add(counterId);
                        if (origFinalStates.Contains(mem))
                            finalCounterSet.Add(counterId);
                        counters[counterId] = countingStates[mem];
                    }
                }
            }
            this.productAlgebra = productAlgebra;
            stateDescr[InitialState] = SpecialCharacters.S(0);
        }

        int GetOriginalInitialState()
        {
            foreach (var q0 in stateBuilder.GetMembers(InitialState))
                return q0;
            throw new AutomataException(AutomataExceptionKind.SetIsEmpty);
        }

        bool __hidePowersets = false;
        internal bool __debugmode = false;

        public void ShowGraph(string name = "CsAutomaton", bool debumode = false, bool hidePowersets = false)
        {
            __hidePowersets = hidePowersets;
            __debugmode = debumode;
            base.ShowGraph(name);
        }

        public void SaveGraph(string name = "CsAutomaton", bool debumode = false, bool hidePowersets = false)
        {
            __hidePowersets = hidePowersets;
            __debugmode = debumode;
            base.SaveGraph(name);
        }


        Dictionary<int, string> stateDescr = new Dictionary<int, string>();
        
        /// <summary>
        /// Describe the state information, including original states if determinized, as well as counters.
        /// </summary>
        public override string DescribeState(int state)
        {
            string s;
            if (!stateDescr.TryGetValue(state, out s))
            {
                s = SpecialCharacters.S(stateDescr.Count);
                stateDescr[state] = s;
            }

            var mems = new List<int>(stateBuilder.GetMembers(state));
            mems.Sort();
            if (!__hidePowersets)
            {
                s += "\n" + "{";
                foreach (var q in mems)
                {
                    if (!s.EndsWith("{"))
                        s += ",";
                    s += SpecialCharacters.q(q);
                }
                s += "}";
            }
            var state_counters = GetCountersOfState(state);
            var state_counters_list = new List<int>(state_counters);
            state_counters_list.Sort();
            foreach (var c in state_counters_list)
            {
                s += "\n";
                s += "(" + SpecialCharacters.c(c) + ")";
                //s += "(" + counters[c].LowerBound + 
                //    SpecialCharacters.LEQ + SpecialCharacters.Cntr(c) + SpecialCharacters.LEQ + counters[c].UpperBound + ")";
                if (finalCounterSet.Contains(c))
                {
                    s += SpecialCharacters.XI_LOWERCASE + SpecialCharacters.ToSubscript(c);
                    s += SpecialCharacters.CHECKMARK;
                }
            }
            return s;
        }

        /// <summary>
        /// Describe if the initial state is associuated with a counter, if so then set it to {0}
        /// </summary>
        public override string DescribeStartLabel()
        {
            var initcounters = activeCounterMap[InitialState].GetEnumerator();
            if (initcounters.MoveNext())
            {
                var c = initcounters.Current;
                return string.Format("{0}={{0}}", SpecialCharacters.c(c));
            }
            else
                return "";
        }

        public override string DescribeLabel(CsLabel<S> lab)
        {
            return lab.ToString(__debugmode);
        }

        public static CsAutomaton<S> CreateFrom(CountingAutomaton<S> ca)
        {
            var productmoves = new List<Move<CsPred<S>>>();
            var counters = ca.counters;
            var alg = new CsAlgebra<S>(((CABA<S>)ca.Algebra).builder.solver, counters);
            foreach (var move in ca.GetMoves())
            {
                var ccond = alg.TrueCsConditionSeq;
                if (ca.IsCountingState(move.SourceState))
                {
                    var counter = ca.GetCounter(move.SourceState);
                    var cid = counter.CounterId;
                    if (move.Label.Item2.First.OperationKind == CounterOp.EXIT ||
                        move.Label.Item2.First.OperationKind == CounterOp.EXIT_SET0 ||
                        move.Label.Item2.First.OperationKind == CounterOp.EXIT_SET1)
                    {
                        if (counter.LowerBound == counter.UpperBound && !ca.HasMovesTo(move.SourceState, move.Label.Item1.Element))
                            ccond = ccond.And(cid, CsCondition.HIGH);
                        else if (counter.LowerBound > 0)
                            ccond = ccond.And(cid, CsCondition.CANEXIT);
                        else
                            ccond.And(cid, CsCondition.NONEMPTY);
                    }
                    else
                    {
                        if (move.Label.Item2.First.OperationKind != CounterOp.INCR)
                            throw new AutomataException(AutomataExceptionKind.InternalError);

                        if (counter.LowerBound == counter.UpperBound && !ca.HasMovesTo(move.SourceState, move.Label.Item1.Element))
                            ccond = ccond.And(cid, CsCondition.LOW);
                        else
                            ccond = ccond.And(cid, CsCondition.CANLOOP);
                    }
                }
                if (ccond.IsSatisfiable)
                {
                    var pmove = Move<CsPred<S>>.Create(move.SourceState, move.TargetState, alg.MkPredicate(move.Label.Item1.Element, ccond));
                    productmoves.Add(pmove);
                }
            }
            var prodaut = Automaton<CsPred<S>>.Create(alg, ca.InitialState, ca.GetFinalStates(), productmoves);

            PowerSetStateBuilder sb;
            var det = prodaut.Determinize(out sb);

            //add predicate that all counters associated with the state are nonempty
            var counterFilter = new Dictionary<int, CsConditionSeq>();
            foreach (var state in det.GetStates())
            {
                var stateCounterFilter = alg.TrueCsConditionSeq;
                foreach (var q in sb.GetMembers(state))
                    if (ca.IsCountingState(q))
                        stateCounterFilter = stateCounterFilter.And(ca.GetCounter(q).CounterId, CsCondition.NONEMPTY);
                counterFilter[state] = stateCounterFilter;
            }

            var csmoves = new List<Move<CsLabel<S>>>();

            //make disjunction of the guards of transitions with same update sequence
            var trans = new Dictionary<Tuple<int,int>,Dictionary<CsUpdateSeq, CsPred<S>>>();

            foreach (var dmove in det.GetMoves())
            { 
                foreach (var prodcond in dmove.Label.GetSumOfProducts())
                {
                    var upd = CsUpdateSeq.MkNOOP(ca.NrOfCounters);
                    foreach (var q in sb.GetMembers(dmove.SourceState))
                        upd = upd | ca.GetCounterUpdate(q, prodcond.Item2, prodcond.Item1);
                    //make sure all counter guards are nonempty
                    //determinization may create EMPTY counter conditions that are unreachable
                    //while all counters associated with a state are always nonempty
                    var counterGuard = prodcond.Item1 & counterFilter[dmove.SourceState];
                    if (counterGuard.IsSatisfiable)
                    {
                        #region replace set with incr if possible
                        for (int i = 0; i < upd.Length; i++)
                        {
                            var guard_i = counterGuard[i];
                            if (guard_i == CsCondition.HIGH)
                            {
                                var upd_i = upd[i];
                                switch (upd_i)
                                {
                                    case CsUpdate.SET0:
                                        upd = upd.Set(i, CsUpdate.INCR0);
                                        break;
                                    case CsUpdate.SET1:
                                        upd = upd.Set(i, CsUpdate.INCR1);
                                        break;
                                    case CsUpdate.SET01:
                                        upd = upd.Set(i, CsUpdate.INCR01);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        #endregion

                        var guard = alg.MkPredicate(prodcond.Item2, counterGuard);
                        var statepair = new Tuple<int, int>(dmove.SourceState, dmove.TargetState);
                        Dictionary<CsUpdateSeq, CsPred<S>> labels;
                        if (!trans.TryGetValue(statepair, out labels))
                        {
                            labels = new Dictionary<CsUpdateSeq, CsPred<S>>();
                            trans[statepair] = labels;
                        }
                        CsPred<S> pred;
                        if (!labels.TryGetValue(upd, out pred))
                            pred = guard;
                        else
                            pred = guard | pred;
                        labels[upd] = pred;
                    }
                    else
                    {
                        ;
                    }
                }
            }

            Func<S,string> pp = ((CABA<S>)ca.Algebra).builder.solver.PrettyPrint;
            foreach (var entry in trans)
            {
                var s = entry.Key.Item1;
                var t = entry.Key.Item2;
                foreach (var label in entry.Value)
                {
                    var upd = label.Key;
                    var psi = label.Value;
                    csmoves.Add(Move<CsLabel<S>>.Create(s, t, CsLabel<S>.MkTransitionLabel(psi, upd, pp)));
                }
            }

            var csa_aut = Automaton<CsLabel<S>>.Create(null, det.InitialState, det.GetFinalStates(), csmoves, true, true);

            var fs = new HashSet<int>(ca.GetFinalStates());

            var csa = new CsAutomaton<S>(alg, csa_aut, sb, ca.countingStates, fs);

            return csa;
        }

        /// <summary>
        /// Get the active counters associated with the given state.
        /// The set is empty if this state is not asscociated with any counters.
        /// </summary>
        public HashSet<int> GetCountersOfState(int state)
        {
            return activeCounterMap[state];
        }

        /// <summary>
        /// Get the total number of counters
        /// </summary>
        public int NrOfCounters
        {
            get
            {
                return counters.Length;
            }
        }

        /// <summary>
        /// Get the counter info associated with the given counter id
        /// </summary>
        /// <param name="counterId">must be a number between 0 and NrOfCounters-1</param>
        /// <returns></returns>
        public ICounter GetCounterInfo(int counterId)
        {
            return counters[counterId];
        }

        /// <summary>
        /// Returns true if the given counter is a final counter, thus, in final state 
        /// contributes to the overall final state condition.
        /// </summary>
        /// <param name="counterId">must be a number between 0 and NrOfCounters-1</param>
        /// <returns></returns>
        public bool IsFinalCounter(int counterId)
        {
            return finalCounterSet.Contains(counterId);
        }
    }

    public class CsLabel<S>
    {
        bool isFinalCondition;
        public readonly CsPred<S> Guard;
        public readonly CsUpdateSeq Updates;
        Func<S, string> InputToString;

        public bool IsFinalCondition
        {
            get { return isFinalCondition; }
        }

        CsLabel(bool isFinalCondition, CsPred<S> guard, CsUpdateSeq updates, Func<S, string> inputToString)
        {
            this.isFinalCondition = isFinalCondition;
            this.Guard = guard;
            this.Updates = updates;
            this.InputToString = inputToString;
        }

        public static CsLabel<S> MkFinalCondition(CsPred<S> guard, Func<S, string> inputToString = null)
        {
            return new CsLabel<S>(true, guard, null, inputToString);
        }

        public static CsLabel<S> MkTransitionLabel(CsPred<S> guard, CsUpdateSeq updates, Func<S, string> inputToString = null)
        {
            return new CsLabel<S>(false, guard, updates, inputToString);
        }

        public override string ToString()
        {
            return ToString(false);
        }

        internal string ToString(bool debugmode)
        {
            var cases = Guard.ToArray();
            string cond = "";
            foreach (var psi in cases)
            {
                var pp = Guard.Algebra.LeafAlgebra as ICharAlgebra<S>;
                if (cond != "")
                    cond += SpecialCharacters.OR + "\n";
                cond += (pp != null ? pp.PrettyPrint(psi.Item2) : psi.Item2.ToString());
                var countercond = (debugmode ? psi.Item1.ToString() : psi.Item1.ToString<S>(Guard.Algebra));
                if (countercond != SpecialCharacters.TOP.ToString())
                    cond += SpecialCharacters.MIDDOT + countercond;
            }
            if (isFinalCondition)
            {
                return cond;
            }
            else
            {
                var s = cond;
                var upd = DescribeCounterUpdate(debugmode);
                if (upd != "")
                {
                    s += SpecialCharacters.IMPLIES + upd;
                }
                return s;
            }
        }

        private string DescribeCounterUpdate(bool debugmode)
        {
            return Updates.ToString(debugmode);
        }
    }

    public enum CsUpdate
    {
        /// <summary>
        /// No update
        /// </summary>
        NOOP = 0,
        /// <summary>
        /// Insert 0
        /// </summary>
        SET0 = 1,
        /// <summary>
        /// Insert 1
        /// </summary>
        SET1 = 2,
        /// <summary>
        /// Insert 0 and 1, same as SET0|SET1
        /// </summary>
        SET01 = 3,
        /// <summary>
        /// Increment all elements
        /// </summary>
        INCR = 4,
        /// <summary>
        /// Increment all elements and then insert 0, same as INCR|SET0
        /// </summary>
        INCR0 = 5,
        /// <summary>
        /// Increment all elements and then insert 1, same as INCR|SET1
        /// </summary>
        INCR1 = 6,
        /// <summary>
        /// Increment all elements and then insert 0 and 1, same as INCR|SET0|SET1
        /// </summary>
        INCR01 = 7,
    }

    public enum CsCondition
    {
        /// <summary>
        /// Unsatisfiable condition
        /// </summary>
        FALSE = 0,
        /// <summary>
        /// Nonempty and all elements are below lower bound
        /// </summary>
        LOW = 1,
        /// <summary>
        /// Some element is at least lower bound but it is not the only element if it is the upper bound
        /// </summary>
        MIDDLE = 2,
        /// <summary>
        /// The condition when loop increment is possible, same as LOW|MIDDLE
        /// </summary>
        CANLOOP = 3,
        /// <summary>
        /// Singleton set containing the upper bound
        /// </summary>
        HIGH = 4,
        /// <summary>
        /// All elements are below lower bound, or singleton set containing the upper bound, same as LOW|HIGH
        /// </summary>
        LOWorHIGH = 5,
        /// <summary>
        /// The condition when loop exit is possible, same as MIDDLE|HIGH
        /// </summary>
        CANEXIT = 6,
        /// <summary>
        /// Set is nonempty, same as LOW|MIDDLE|HIGH
        /// </summary>
        NONEMPTY = 7,
        /// <summary>
        /// Set is empty
        /// </summary>
        EMPTY = 8,
        /// <summary>
        /// Same as EMPTY|LOW
        /// </summary>
        CANNOTEXIT = 9,
        /// <summary>
        /// Same as EMPTY|MIDDLE
        /// </summary>
        EMPTYorMIDDLE = 10,
        /// <summary>
        /// Same as EMPTY|MIDDLE|LOW
        /// </summary>
        EMPTYorCANLOOP = 11,
        /// <summary>
        /// Same as EMPTY|HIGH
        /// </summary>
        CANNOTLOOP = 12,
        /// <summary>
        /// Same as EMPTY|HIGH|LOW
        /// </summary>
        EMPTYorHIGHorLOW = 13,
        /// <summary>
        /// Same as EMPTY|HIGH|MIDDLE
        /// </summary>
        EMPTYorCANEXIT = 14,
        /// <summary>
        /// Condition that always holds, same as EMPTY|MIDDLE|HIGH|LOW
        /// </summary>
        TRUE = 15,
    }

    public class CsUpdateSeq
    {
        Tuple<int, ulong, ulong, ulong> vals;

        CsUpdateSeq(int count, ulong set0, ulong set1, ulong incr)
        {
            vals = new Tuple<int, ulong, ulong, ulong>(count, set0, set1, incr);
        }

        public static CsUpdateSeq MkNOOP(int count)
        {
            return new CsUpdateSeq(count, 0, 0, 0);
        }

        public int Length
        {
            get { return vals.Item1; }
        }

        public static CsUpdateSeq Mk(params CsUpdate[] vals)
        {
            ulong set0 = 0;
            ulong set1 = 0;
            ulong incr = 0;
            ulong bit = 1;
            for (int i = 0; i < vals.Length; i++)
            {
                if (vals[i].HasFlag(CsUpdate.SET0))
                    set0 = set0 | bit;
                if (vals[i].HasFlag(CsUpdate.SET1))
                    set1 = set1 | bit;
                if (vals[i].HasFlag(CsUpdate.INCR))
                    incr = incr | bit;
                bit = bit << 1;
            }
            return new CsUpdateSeq(vals.Length, set0, set1, incr);
        }

        public static CsUpdateSeq operator |(CsUpdateSeq left, CsUpdateSeq right)
        {
            if (left.vals.Item1 != right.vals.Item1)
                throw new ArgumentException("Incompatible lenghts");

            return new CsUpdateSeq(left.vals.Item1, left.vals.Item2 | right.vals.Item2, left.vals.Item3 | right.vals.Item3, left.vals.Item4 | right.vals.Item4);
        }

        public CsUpdate this[int i]
        {
            get
            {
                ulong bit = ((ulong)1) << i;
                int val = 0;
                if ((vals.Item2 & bit) != 0)
                    val = val | 1;
                if ((vals.Item3 & bit) != 0)
                    val = val | 2;
                if ((vals.Item4 & bit) != 0)
                    val = val | 4;
                CsUpdate res = (CsUpdate)val;
                return res;
            }
        }

        public CsUpdateSeq Or(int i, CsUpdate upd)
        {
            ulong bit = ((ulong)1) << i;
            ulong set0 = vals.Item2;
            ulong set1 = vals.Item3;
            ulong incr = vals.Item4;
            if (upd.HasFlag(CsUpdate.SET0))
                set0 = set0 | bit;
            if (upd.HasFlag(CsUpdate.SET1))
                set1 = set1 | bit;
            if (upd.HasFlag(CsUpdate.INCR))
                incr = incr | bit;
            CsUpdateSeq res = new CsUpdateSeq(vals.Item1, set0, set1, incr);
            return res;
        }

        public CsUpdateSeq Set(int i, CsUpdate upd)
        {
            ulong bit = ((ulong)1) << i;
            var mask = ~bit;
            ulong set0 = vals.Item2 & mask;
            ulong set1 = vals.Item3 & mask;
            ulong incr = vals.Item4 & mask;
            if (upd.HasFlag(CsUpdate.SET0))
                set0 = set0 | bit;
            if (upd.HasFlag(CsUpdate.SET1))
                set1 = set1 | bit;
            if (upd.HasFlag(CsUpdate.INCR))
                incr = incr | bit;
            CsUpdateSeq res = new CsUpdateSeq(vals.Item1, set0, set1, incr);
            return res;
        }

        /// <summary>
        /// Returns true if all counter operations are NOOP
        /// </summary>
        public bool IsNOOP
        {
            get
            {
                return (vals.Item2 == 0 && vals.Item3 == 0 && vals.Item4 == 0);
            }
        }

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < Length; i++)
                if (this[i] != CsUpdate.NOOP)
                    s += string.Format("{0}({1});", this[i], SpecialCharacters.c(i));
            return s;
        }

        internal string ToString(bool debugmode)
        {
            if (debugmode)
            {
                return ToString();
            }
            else
            {
                string s = "";
                for (int i = 0; i < Length; i++)
                {
                    string c = SpecialCharacters.c(i);
                    char assign = SpecialCharacters.ASSIGN;
                    char union = SpecialCharacters.UNION;
                    switch (this[i])
                    {
                        case CsUpdate.INCR:
                            {
                                s += c + "++";
                                break;
                            }
                        case CsUpdate.INCR0:
                            {
                                s += c + "++" + union + "{0}";
                                break;
                            }
                        case CsUpdate.INCR1:
                            {
                                s += c + "++" + union + "{1}";
                                break;
                            }
                        case CsUpdate.INCR01:
                            {
                                s += c + "++" + union + "{0,1}";
                                break;
                            }
                        case CsUpdate.SET0:
                            {
                                s += c + assign + "{0}";
                                break;
                            }
                        case CsUpdate.SET1:
                            {
                                s += c + assign + "{1}";
                                break;
                            }
                        case CsUpdate.SET01:
                            {
                                s += c + assign + "{0,1}";
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                }
                return s;
            }
        }

        public override bool Equals(object obj)
        {
            return vals.Equals(((CsUpdateSeq)obj).vals);
        }

        public override int GetHashCode()
        {
            return vals.GetHashCode();
        }
    }

    public class CsConditionSeq
    {
        bool isAND;
        /// <summary>
        /// Returns true iff this sequence represents a conjunction
        /// </summary>
        public bool IsAND
        {
            get
            {
                return isAND;
            }
        }
        Tuple<int, ulong, ulong, ulong, ulong, ulong> elems;
        /// <summary>
        /// Number of conditions
        /// </summary>
        public int Length { get { return elems.Item1; } }
        internal ulong Mask { get { return elems.Item2; } }
        internal ulong Empty { get { return elems.Item3; } }
        internal ulong Low { get { return elems.Item4; } }
        internal ulong Middle { get { return elems.Item5; } }
        internal ulong High { get { return elems.Item6; } }

        CsConditionSeq(bool isAND, Tuple<int, ulong, ulong, ulong, ulong, ulong> elems)
        {
            this.isAND = isAND;
            this.elems = elems;
        }

        /// <summary>
        /// Make a sequence that corresponds to the conjunction of the individual counter conditions.
        /// </summary>
        /// <param name="vals">i'th element is the i'th counter condition</param>
        public static CsConditionSeq MkAND(params CsCondition[] vals)
        {
            return MkSeq(true, vals);
        }

        /// <summary>
        /// Make a sequence that corresponds to the disjunction of the individual counter conditions.
        /// </summary>
        /// <param name="vals">i'th element is the i'th counter condition</param>
        public static CsConditionSeq MkOR(params CsCondition[] vals)
        {
            return MkSeq(false, vals);
        }

        static CsConditionSeq MkSeq(bool isOr, CsCondition[] vals)
        {
            if (vals.Length > 64)
                throw new NotImplementedException("More than 64 counter support not implemented");

            int length = vals.Length;
            ulong mask = (length == 64 ? ulong.MaxValue : ((ulong)1 << length) - 1);
            ulong empty = 0;
            ulong low = 0;
            ulong middle = 0;
            ulong high = 0;
            ulong bitmask = 1;
            for (int i = 0; i < length; i++)
            {
                CsCondition cond = vals[i];
                if (cond.HasFlag(CsCondition.LOW))
                    low = low | bitmask;
                if (cond.HasFlag(CsCondition.MIDDLE))
                    middle = middle | bitmask;
                if (cond.HasFlag(CsCondition.HIGH))
                    high = high | bitmask;
                if (cond.HasFlag(CsCondition.EMPTY))
                    empty = empty | bitmask;
                bitmask = bitmask << 1;
            }
            var elems = new Tuple<int, ulong, ulong, ulong, ulong, ulong>(length, mask, empty, low, middle, high);
            return new CsConditionSeq(isOr, elems);
        }

        /// <summary>
        /// Creates a conjunction with all individual counter conditions being TRUE
        /// </summary>
        public static CsConditionSeq MkTrue(int length)
        {
            CsCondition[] vals = new CsCondition[length];
            for (int i = 0; i < length; i++)
                vals[i] = CsCondition.TRUE;
            return MkAND(vals);
        }

        /// <summary>
        /// Creates a disjunction with all individual counter conditions being FALSE
        /// </summary>
        public static CsConditionSeq MkFalse(int length)
        {
            CsCondition[] vals = new CsCondition[length];
            for (int i = 0; i < length; i++)
                vals[i] = CsCondition.FALSE;
            return MkOR(vals);
        }

        public override bool Equals(object obj)
        {
            var cond = obj as CsConditionSeq;
            if (cond == null)
                return false;
            else
                return cond.isAND == isAND && elems.Equals(cond.elems);
        }

        public override int GetHashCode()
        {
            return (isAND ? elems.GetHashCode() : elems.GetHashCode() << 1);
        }

        public static string DescribeCondition<S>(CsAlgebra<S> algebra, CsCondition cond, int i)
        {
            string c = SpecialCharacters.c(i);
            string canExit = SpecialCharacters.XI_LOWERCASE + SpecialCharacters.ToSubscript(i);
            string canIncr = SpecialCharacters.IOTA_LOWERCASE + SpecialCharacters.ToSubscript(i);
            char and = SpecialCharacters.AND;
            char or = SpecialCharacters.OR;
            char not = SpecialCharacters.NOT;
            string empty = string.Format("{0}={1}", c, SpecialCharacters.EMPTYSET);
            string nonempty = string.Format("{0}{1}{2}", c, SpecialCharacters.NEQ, SpecialCharacters.EMPTYSET);
            string _true = SpecialCharacters.TOP.ToString();
            string _false = SpecialCharacters.BOT.ToString();
            switch (cond)
            {
                case CsCondition.TRUE:
                    return _true;
                case CsCondition.FALSE:
                    return _false;
                case CsCondition.EMPTY:
                    return empty;
                case CsCondition.NONEMPTY:
                    return nonempty;
                case CsCondition.LOW:
                    //return string.Format("{0}{2}{3}{4}max({0})<{1}", SpecialCharacters.Cntr(i), algebra.GetCounter(i).LowerBound, SpecialCharacters.NEQ, SpecialCharacters.EMPTYSET, SpecialCharacters.AND);
                    //return canIncr + and + not + canExit;
                    return not + canExit;
                case CsCondition.HIGH:
                    //return string.Format("{0}={{{1}}}", SpecialCharacters.Cntr(i), algebra.GetCounter(i).UpperBound);
                    //return not + canIncr + and + canExit;
                    return not + canIncr;
                case CsCondition.CANEXIT:
                    //return string.Format("{0}{1}{2}", SpecialCharacters.Cntr(i), SpecialCharacters.GEQ, algebra.GetCounter(i).LowerBound);
                    return canExit;
                case CsCondition.CANNOTEXIT:
                    //return string.Format("{0}{1}{2}", SpecialCharacters.Cntr(i), SpecialCharacters.NOTGEQ, algebra.GetCounter(i).LowerBound);
                    return not + canExit;
                case CsCondition.CANLOOP:
                    //return string.Format("{0}+1{1}{2}", SpecialCharacters.Cntr(i), SpecialCharacters.NEQ, SpecialCharacters.EMPTYSET);
                    //return string.Format("{0}x{1}{2}(x<{3})", SpecialCharacters.EXISTS, SpecialCharacters.IN, SpecialCharacters.Cntr(i), algebra.GetCounter(i).UpperBound);
                    return canIncr;
                case CsCondition.CANNOTLOOP:
                    //return string.Format("{0}+1{1}{2}", SpecialCharacters.Cntr(i), "=", SpecialCharacters.EMPTYSET);
                    //return string.Format("{0}x{1}{2}(x<{3})", SpecialCharacters.NOTEXISTS, SpecialCharacters.IN, SpecialCharacters.Cntr(i), algebra.GetCounter(i).UpperBound);
                    return not + canIncr;
                case CsCondition.MIDDLE:
                    //return string.Format("{2}{4}{5}{6}{0}{1}max({2})<{3}", algebra.GetCounter(i).LowerBound, SpecialCharacters.LEQ, SpecialCharacters.Cntr(i), algebra.GetCounter(i).UpperBound, SpecialCharacters.NEQ, SpecialCharacters.EMPTYSET, SpecialCharacters.AND);
                    return canIncr + and + canExit;
                #region these cases should not occur
                case CsCondition.LOWorHIGH:
                    //return string.Format("({0}{1}{2})", DescribeCondition<S>(algebra, CsCondition.LOW, i), SpecialCharacters.OR, DescribeCondition<S>(algebra, CsCondition.HIGH, i));
                    return "(" + not + canIncr + or + not + canExit + ")";
                case CsCondition.EMPTYorCANEXIT:
                    return "(" + empty + or + canExit + ")";
                case CsCondition.EMPTYorCANLOOP:
                    return "(" + empty + or + canIncr + ")";
                case CsCondition.EMPTYorHIGHorLOW:
                    return "(" + empty + or + not + canIncr + or + not + canExit + ")";
                case CsCondition.EMPTYorMIDDLE:
                    return "(" + empty + or + canIncr + and + canExit + ")";
                default:
                    throw new AutomataException(AutomataExceptionKind.UndefinedEnum);
               #endregion
            }
        }

        public override string ToString()
        {
            string s = "";
            if (isAND)
            {
                for (int i=0; i < Length; i++)
                {
                    if (this[i] != CsCondition.TRUE)
                    {
                        if (s != "")
                            s += SpecialCharacters.AND;
                        s += string.Format("{0}({1})", this[i], SpecialCharacters.c(i));
                    }
                }
                if (s == "")
                    s = SpecialCharacters.TOP.ToString();
            }
            else
            {
                for (int i = 0; i < Length; i++)
                {
                    if (this[i] != CsCondition.FALSE)
                    {
                        if (s != "")
                            s += SpecialCharacters.OR;
                        s += string.Format("{0}({1})", this[i], SpecialCharacters.c(i));
                    }
                }
                if (s == "")
                    s = SpecialCharacters.BOT.ToString();
            }
            return s;
        }

        public string ToString<S>(CsAlgebra<S> algebra)
        {
            string s = "";
            if (isAND)
            {
                for (int i = 0; i < Length; i++)
                {
                    if (this[i] != CsCondition.TRUE)
                    {
                        if (s != "")
                            s += SpecialCharacters.AND;
                        s += DescribeCondition<S>(algebra, this[i], i);
                    }
                }
                if (s == "")
                    s = SpecialCharacters.TOP.ToString();
            }
            else
            {
                for (int i = 0; i < Length; i++)
                {
                    if (this[i] != CsCondition.FALSE)
                    {
                        if (s != "")
                            s += SpecialCharacters.OR;
                        s += DescribeCondition<S>(algebra, this[i], i);
                    }
                }
                if (s == "")
                    s = SpecialCharacters.BOT.ToString();
            }
            return s;
        }

        public CsCondition[] ToArray()
        {
            var list = new List<CsCondition>();
            var arr = new CsCondition[Length];
            for (int i = 0; i < Length; i++)
                arr[i] = this[i];
            return arr;
        }

        /// <summary>
        /// Returns the i'th condition
        /// </summary>
        /// <param name="i">must be between 0 and Length-1</param>
        public CsCondition this[int i]
        {
            get
            {
                if (i >= Length || i < 0)
                    throw new ArgumentOutOfRangeException();
                else
                {
                    ulong bitmask = ((ulong)1) << i;
                    int res = 0;
                    if ((Low & bitmask) != 0)
                        res = (int)CsCondition.LOW;
                    if ((Middle & bitmask) != 0)
                        res = res | (int)CsCondition.MIDDLE;
                    if ((High & bitmask) != 0)
                        res = res | (int)CsCondition.HIGH;
                    if ((Empty & bitmask) != 0)
                        res = res | (int)CsCondition.EMPTY;
                    return (CsCondition)res;
                }
            }
        }

        /// <summary>
        /// If conjunction, returns true if all conditions in the sequence are different from FALSE.
        /// If disjunction, returns true if some condition in the sequence is different from FALSE
        /// </summary>
        public bool IsSatisfiable
        {
            get
            {
                if (isAND)
                    return (Empty | Low | Middle | High) == Mask;
                else
                    return (Middle != 0 | Low != 0 | High != 0 | Empty != 0);
            }
        }

        /// <summary>
        /// If conjunction, returns true if all conditions in the sequence are TRUE.
        /// If disjunction, returns true if some condition in the sequence is TRUE.
        /// </summary>
        public bool IsValid
        {
            get
            {
                var mask = Mask;
                if (isAND)
                    return (Empty == mask && Low == mask && Middle == mask && High == Mask);
                else
                    return (mask & (~Empty | ~Low | ~Middle | ~High)) != mask;
            }
        }

        /// <summary>
        /// Create a conjunction sequence of two sequences that represent conjunctions
        /// </summary>
        public static CsConditionSeq operator &(CsConditionSeq left, CsConditionSeq right)
        {
            if (left.Length == right.Length && left.isAND && right.isAND)
            {
                int length = left.Length;
                ulong mask = left.Mask;
                ulong empty = left.Empty & right.Empty;
                ulong low = left.Low & right.Low;
                ulong middle = left.Middle & right.Middle;
                ulong high = left.High & right.High;
                var elems = new Tuple<int, ulong, ulong, ulong, ulong, ulong>(length, mask, empty, low, middle, high);
                var res = new CsConditionSeq(true, elems);
                return res;
            }
            else
                throw new InvalidOperationException("Incompatible arguments, & is only supported between conjunction sequences");
        }

        /// <summary>
        /// Create a disjunction sequence of two sequences that represent disjunctions
        /// </summary>
        public static CsConditionSeq operator |(CsConditionSeq left, CsConditionSeq right)
        {
            if (left.Length == right.Length && !left.isAND && !right.isAND)
            {
                int length = left.Length;
                ulong mask = left.Mask;
                ulong empty = left.Empty | right.Empty;
                ulong low = left.Low | right.Low;
                ulong middle = left.Middle | right.Middle;
                ulong high = left.High | right.High;
                var elems = new Tuple<int, ulong, ulong, ulong, ulong, ulong>(length, mask, empty, low, middle, high);
                var res = new CsConditionSeq(false, elems);
                return res;
            }
            else
                throw new InvalidOperationException("Incompatible arguments, | is only supported between disjunction sequences");
        }

        /// <summary>
        /// Complement the sequence from OR to AND and vice versa, 
        /// individual counter conditions are complemented.
        /// </summary>
        public static CsConditionSeq operator ~(CsConditionSeq arg)
        {
            int length = arg.Length;
            ulong mask = arg.Mask;
            ulong empty = mask & ~arg.Empty;
            ulong low = mask & ~arg.Low;
            ulong middle = mask & ~arg.Middle;
            ulong high = mask & ~arg.High;
            var elems = new Tuple<int, ulong, ulong, ulong, ulong, ulong>(length, mask, empty, low, middle, high);
            var res = new CsConditionSeq(!arg.isAND, elems);
            return res;
        }

        public CsConditionSeq Update(int i, CsCondition cond)
        {
            if (i >= Length)
                throw new ArgumentOutOfRangeException();

            ulong bit = ((ulong)1) << i;
            ulong bitmask = ~bit;
            //clear the bit
            ulong empty = Empty & bitmask;
            ulong low = Low & bitmask;
            ulong mid = Middle & bitmask;
            ulong high = High & bitmask;
            //set the new value
            if (cond.HasFlag(CsCondition.LOW))
                low = low | bit;
            if (cond.HasFlag(CsCondition.MIDDLE))
                mid = mid | bit;
            if (cond.HasFlag(CsCondition.HIGH))
                high = high | bit;
            if (cond.HasFlag(CsCondition.EMPTY))
                empty = empty | bit;

            var elems = new Tuple<int, ulong, ulong, ulong, ulong, ulong>(Length, Mask, empty, low, mid, high);
            return new CsConditionSeq(isAND, elems);
        }

        /// <summary>
        /// Update the i'th element to this[i] | cond
        /// </summary>
        public CsConditionSeq Or(int i, CsCondition cond)
        {
            if (i >= Length)
                throw new ArgumentOutOfRangeException();

            ulong bit = ((ulong)1) << i;
            ulong empty = Empty;
            ulong low = Low;
            ulong mid = Middle;
            ulong high = High;
            //set the new value
            if (cond.HasFlag(CsCondition.LOW))
                low = low | bit;
            if (cond.HasFlag(CsCondition.MIDDLE))
                mid = mid | bit;
            if (cond.HasFlag(CsCondition.HIGH))
                high = high | bit;
            if (cond.HasFlag(CsCondition.EMPTY))
                empty = empty | bit;

            var elems = new Tuple<int, ulong, ulong, ulong, ulong, ulong>(Length, Mask, empty, low, mid, high);
            var res = new CsConditionSeq(isAND, elems);
            return res;
        }

        /// <summary>
        /// Update the i'th element to this[i] & cond
        /// </summary>
        public CsConditionSeq And(int i, CsCondition cond)
        {
            if (i >= Length)
                throw new ArgumentOutOfRangeException();

            ulong bit = ((ulong)1) << i;
            ulong bit_false = ~bit;

            ulong empty = Empty;
            ulong low = Low;
            ulong mid = Middle;
            ulong high = High;
            //set the new value
            if (!cond.HasFlag(CsCondition.LOW))
                low = low & bit_false;
            if (!cond.HasFlag(CsCondition.MIDDLE))
                mid = mid & bit_false;
            if (!cond.HasFlag(CsCondition.HIGH))
                high = high & bit_false;
            if (!cond.HasFlag(CsCondition.EMPTY))
                empty = empty & bit_false;

            var elems = new Tuple<int, ulong, ulong, ulong, ulong, ulong>(Length, Mask, empty, low, mid, high);
            var res = new CsConditionSeq(isAND, elems);
            return res;
        }
    }
}
