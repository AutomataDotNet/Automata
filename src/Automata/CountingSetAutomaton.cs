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

        Dictionary<int, HashSet<int>> activeCounterMap;

        HashSet<int> finalCounterSet;

        ICounter[] counters;

        public CsAutomaton(IBooleanAlgebra<S> inputAlgebra, Automaton<CsLabel<S>> aut, PowerSetStateBuilder stateBuilder, Dictionary<int, ICounter> countingStates, HashSet<int> origFinalStates) : base(aut)
        {
            this.stateBuilder = stateBuilder;
            this.countingStates = countingStates;
            this.productAlgebra = new CsAlgebra<S>(inputAlgebra, countingStates.Count);
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
        }

        int GetOriginalInitialState()
        {
            foreach (var q0 in stateBuilder.GetMembers(InitialState))
                return q0;
            throw new AutomataException(AutomataExceptionKind.SetIsEmpty);
        }

        bool __hidePowersets = false;
        public void ShowGraph(string name = "CsAutomaton", bool hidePowersets = false)
        {
            __hidePowersets = hidePowersets;
            base.ShowGraph(name);
        }

        /// <summary>
        /// Describe the state information, including original states if determinized, as well as counters.
        /// </summary>
        public override string DescribeState(int state)
        {
            string s = state.ToString();
            var mems = new List<int>(stateBuilder.GetMembers(state));
            mems.Sort();
            if (!__hidePowersets)
            {
                s += "&#13;{";
                foreach (var q in mems)
                {
                    if (!s.EndsWith("{"))
                        s += ",";
                    s += q.ToString();
                }
                s += "}";
            }
            var state_counters = GetCountersOfState(state);
            var state_counters_list = new List<int>(state_counters);
            state_counters_list.Sort();
            foreach (var c in state_counters_list)
            {
                s += "&#13;";
                if (finalCounterSet.Contains(c))
                    s += "(F)";
                s += "c" + c + ":[" + counters[c].LowerBound + "," + counters[c].UpperBound + "]";
            }
            return s;
        }

        /// <summary>
        /// Describe if the initial state is associuated wit a counter, if so then set it to {0}
        /// </summary>
        /// <returns></returns>
        public override string DescribeStartLabel()
        {
            var initcounters = activeCounterMap[InitialState].GetEnumerator();
            if (initcounters.MoveNext())
            {
                var c = initcounters.Current;
                return string.Format("c{0}={{0}}", c);
            }
            else
                return "";
        }

        public static CsAutomaton<S> CreateFrom(CountingAutomaton<S> ca)
        {
            var productmoves = new List<Move<CsPred<S>>>();
            var alg = new CsAlgebra<S>(((CABA<S>)ca.Algebra).builder.solver, ca.NrOfCounters);
            foreach (var move in ca.GetMoves())
            {
                var ccond = CsConditionSeq.MkEmpty(ca.NrOfCounters);
                if (ca.IsCountingState(move.SourceState))
                {
                    var cid = ca.GetCounter(move.SourceState).CounterId;
                    if (move.Label.Item2.First.OperationKind == CounterOp.EXIT ||
                        move.Label.Item2.First.OperationKind == CounterOp.EXIT_SET0 ||
                        move.Label.Item2.First.OperationKind == CounterOp.EXIT_SET1)
                    {
                        ccond = ccond.Update(cid, CsCondition.CANEXIT);
                    }
                    else
                    {
                        if (move.Label.Item2.First.OperationKind != CounterOp.INCR)
                            throw new AutomataException(AutomataExceptionKind.InternalError);

                        ccond = ccond.Update(cid, CsCondition.CANLOOP);
                    }
                }
                var pmove = Move<CsPred<S>>.Create(move.SourceState, move.TargetState, alg.MkPredicate(move.Label.Item1.Element, ccond));
                productmoves.Add(pmove);
            }
            var prodaut = Automaton<CsPred<S>>.Create(alg, ca.InitialState, ca.GetFinalStates(), productmoves);

            PowerSetStateBuilder sb;
            var det = prodaut.Determinize(out sb);

            var csmoves = new List<Move<CsLabel<S>>>();

            foreach (var dmove in det.GetMoves())
            { 
                foreach (var prodcond in dmove.Label.GetSumOfProducts())
                {
                    var upd = CsUpdateSeq.MkNOOP(ca.NrOfCounters);
                    foreach (var q in sb.GetMembers(dmove.SourceState))
                        upd = upd | ca.GetCounterUpdate(q, prodcond.Item2, prodcond.Item1);
                    csmoves.Add(Move<CsLabel<S>>.Create(dmove.SourceState, dmove.TargetState, CsLabel<S>.MkTransitionLabel(prodcond.Item2, prodcond.Item1, upd, ((CABA<S>)ca.Algebra).builder.solver.PrettyPrint)));
                }
            }

            var csa_aut = Automaton<CsLabel<S>>.Create(null, det.InitialState, det.GetFinalStates(), csmoves);

            var fs = new HashSet<int>(ca.GetFinalStates());

            var csa = new CsAutomaton<S>(((CABA<S>)ca.Algebra).builder.solver, csa_aut, sb, ca.countingStates, fs);

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
        S input;
        bool isFinalCondition;
        public readonly CsConditionSeq Conditions;
        public readonly CsUpdateSeq Updates;
        Func<S, string> InputToString;

        public bool IsFinalCondition
        {
            get { return isFinalCondition; }
        }

        public S InputGuard
        {
            get
            {
                if (isFinalCondition)
                    throw new AutomataException(AutomataExceptionKind.InvalidCall);

                return input;
            }
        }

        CsLabel(bool isFinalCondition, S input, CsConditionSeq conditions, CsUpdateSeq updates, Func<S, string> inputToString)
        {
            this.input = input;
            this.isFinalCondition = isFinalCondition;
            this.Conditions = conditions;
            this.Updates = updates;
            this.InputToString = inputToString;
        }

        public static CsLabel<S> MkFinalCondition(CsConditionSeq conditions, Func<S, string> inputToString = null)
        {
            return new CsLabel<S>(true, default(S), conditions, CsUpdateSeq.MkNOOP(conditions.Length), inputToString);
        }

        public static CsLabel<S> MkTransitionLabel(S input, CsConditionSeq conditions, CsUpdateSeq updates, Func<S, string> inputToString = null)
        {
            return new CsLabel<S>(false, input, conditions, updates, inputToString);
        }

        public override string ToString()
        {
            if (isFinalCondition)
            {
                return DescribeCounterCondition();
            }
            else
            {
                var s = (InputToString == null ? input.ToString() : InputToString(input));

                var cond = DescribeCounterCondition();
                var upd = DescribeCounterUpdate();
                if (cond != "")
                {
                    s += "/" + cond;
                }
                if (upd != "")
                {
                    s += ":" + upd;
                }
                return s;
            }
        }

        private string DescribeCounterCondition()
        {
            string s = "";
            for (int i = 0; i < Conditions.Length; i++)
            {
                if (Conditions[i] != CsCondition.EMPTY && Conditions[i] != CsCondition.NONEMPTY)
                {
                    if (s != "")
                        s += "&";
                    s += CsCondition_ToString(Conditions[i]) + "(c" + i.ToString() + ")";
                }
            }
            return s;
        }

        static string CsCondition_ToString(CsCondition cond)
        {
            switch (cond)
            {
                case CsCondition.LOW:
                    return "L";
                case CsCondition.MIDDLE:
                    return "M";
                case CsCondition.HIGH:
                    return "H";
                default:
                    return cond.ToString();
            }
        }

        private string DescribeCounterUpdate()
        {
            string s = "";
            for (int i = 0; i < Updates.Length; i++)
            {
                if (Updates[i] != CsUpdate.NOOP)
                {
                    if (s != "")
                        s += "&";
                    s += Updates[i].ToString() + "(c" + i.ToString() + ")";
                }
            }
            //for (int i = 0; i < Updates.Length; i++)
            //{
            //    switch (Updates[i])
            //    {
            //        case CsUpdate.NOOP:
            //            break;
            //        case CsUpdate.INCR:
            //            s += string.Format("c{0}++;", i);
            //            break;
            //        case CsUpdate.INCR0:
            //            s += string.Format("c{0}+U{{0}};", i);
            //            break;
            //        case CsUpdate.INCR1:
            //            s += string.Format("c{0}+U{{1}};", i);
            //            break;
            //        case CsUpdate.INCR01:
            //            s += string.Format("c{0}+U{{0,1}};", i);
            //            break;
            //        case CsUpdate.SET0:
            //            s += string.Format("c{0}:={{0}};", i);
            //            break;
            //        case CsUpdate.SET1:
            //            s += string.Format("c{0}:={{1}};", i);
            //            break;
            //        case CsUpdate.SET01:
            //            s += string.Format("c{0}:={{0,1}};", i);
            //            break;
            //    }
            //}
            return s;
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
        LOWHIGH = 5,
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
        /// Any set, same as LOW|MIDDLE|HIGH|EMPTY
        /// </summary>
        TRUE = 15,
        /// <summary>
        /// No set
        /// </summary>
        FALSE = 0,
    }

    public class CsUpdateSeq
    {
        int length;
        ulong elems = 0;

        public static readonly CsUpdateSeq False = new CsUpdateSeq(0, 0);

        public static CsUpdateSeq MkNOOP(int count)
        {
            return new CsUpdateSeq(0, count);
        }

        public int Length
        {
            get { return length; }
        }

        CsUpdateSeq(ulong elems, int count)
        {
            this.elems = elems;
            this.length = count;
        }

        public static CsUpdateSeq Mk(int i, CsUpdate update, int length)
        {
            if (length > 21)
                throw new NotImplementedException();

            return new CsUpdateSeq(((ulong)update) << (3 * i), length);
        }

        public static CsUpdateSeq Mk(params CsUpdate[] vals)
        {
            if (vals.Length > 21)
                throw new NotImplementedException();

            ulong x = 0;
            int k = 0;
            for (int i = 0; i < vals.Length; i++)
            {
                x = x | (((ulong)(vals[i])) << k);
                k += 3;
            }
            return new CsUpdateSeq(x, vals.Length);
        }


        public static CsUpdateSeq operator |(CsUpdateSeq left, CsUpdateSeq right)
        {
            return new CsUpdateSeq(left.elems | right.elems, left.length);
        }

        public static CsUpdateSeq operator &(CsUpdateSeq left, CsUpdateSeq right)
        {
            return new CsUpdateSeq(left.elems & right.elems, left.length);
        }

        public CsUpdate this[int i]
        {
            get
            {
                return (CsUpdate)((elems >> (3 * i)) & 7);
            }
        }

        public override bool Equals(object obj)
        {
            return elems == ((CsUpdateSeq)obj).elems;
        }

        public override int GetHashCode()
        {
            return elems.GetHashCode();
        }

        public CsUpdateSeq Update(int i, CsUpdate upd)
        {
            ulong mask = ~(((ulong)7) << (3 * i));
            var conds1 = (elems & mask) | (((ulong)upd) << (3 * i));
            return new CsUpdateSeq(conds1, length);
        }

        public CsUpdateSeq Or(int i, CsUpdate upd)
        {
            var conds1 = elems | (((ulong)upd) << (3 * i));
            return new CsUpdateSeq(conds1, length);
        }

        public bool IsEmpty(int i)
        {
            return ((elems >> (3 * i)) & 7) == 0;
        }
    }

    //public class CsConditionSeq
    //{
    //    internal int length;
    //    ulong conds = 0;

    //    public static CsConditionSeq MkFalse(int length)
    //    {
    //        return new CsConditionSeq(0, length);
    //    }

    //    CsConditionSeq(ulong conds, int count)
    //    {
    //        this.conds = conds;
    //        this.length = count;
    //    }

    //    public static CsConditionSeq Mk(int i, CsCondition cond, int length)
    //    {
    //        if (length > 0 && length > 21)
    //            throw new NotImplementedException();

    //        return new CsConditionSeq(((ulong)cond) << (3 * i), length);
    //    }

    //    public static CsConditionSeq Mk(params CsCondition[] vals)
    //    {
    //        if (vals.Length > 21)
    //            throw new NotImplementedException();

    //        ulong x = 0;
    //        int k = 0;
    //        for (int i = 0; i < vals.Length; i++)
    //        {
    //            x = x | (((ulong)(vals[i])) << k);
    //            k += 3;
    //        }
    //        return new CsConditionSeq(x, vals.Length);
    //    }

    //    public CsCondition this[int i]
    //    {
    //        get
    //        {
    //            return (CsCondition)((conds >> (3 * i)) & 7);
    //        }
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        return conds == ((CsConditionSeq)obj).conds;
    //    }

    //    public override int GetHashCode()
    //    {
    //        return conds.GetHashCode();
    //    }

    //    public CsConditionSeq Update(int i, CsCondition cond)
    //    {
    //        ulong mask = ~(((ulong)7) << (3 * i));
    //        var conds1 = (conds & mask) | (((ulong)cond) << (3 * i));
    //        return new CsConditionSeq(conds1, length);
    //    }

    //    public CsConditionSeq Or(int i, CsCondition cond)
    //    {
    //        var conds1 = conds | (((ulong)cond) << (3 * i));
    //        return new CsConditionSeq(conds1, length);
    //    }

    //    public bool IsEmpty(int i)
    //    {
    //        return ((conds >> (3 * i)) & 7) == 0;
    //    }

    //    public CsCondition[] ToArray()
    //    {
    //        var list = new List<CsCondition>();
    //        var arr = new CsCondition[length];
    //        for (int i = 0; i < length; i++)
    //            arr[i] = this[i];
    //        return arr;
    //    }

    //    public override string ToString()
    //    {
    //        string s = "";
    //        for (int i = 0; i < length; i++)
    //        {
    //            if (s != "")
    //                s += "&";
    //            if (this[i] == CsCondition.LOW)
    //                s += string.Format("{0}(c{1})", "L", i);
    //            else if (this[i] == CsCondition.MIDDLE)
    //                s += string.Format("{0}(c{1})", "M", i);
    //            else if (this[i] == CsCondition.HIGH)
    //                s += string.Format("{0}(c{1})", "H", i);
    //            else
    //                s += string.Format("{0}(c{1})", this[i], i);
    //        }
    //        return s;
    //    }

    //    public static CsConditionSeq operator &(CsConditionSeq left, CsConditionSeq right)
    //    {
    //        return new CsConditionSeq(left.conds & right.conds, left.length);
    //    }

    //    public static CsConditionSeq operator |(CsConditionSeq left, CsConditionSeq right)
    //    {
    //        return new CsConditionSeq(left.conds | right.conds, left.length);
    //    }
    //}

    public class CsConditionSeq
    {
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

        CsConditionSeq(Tuple<int, ulong, ulong, ulong, ulong, ulong> elems)
        {
            this.elems = elems;
        }

        public static CsConditionSeq Mk(params CsCondition[] vals)
        {
            if (vals.Length > 64)
                throw new NotImplementedException();

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
            return new CsConditionSeq(elems);
        }

        public static CsConditionSeq MkEmpty(int length)
        {
            CsCondition[] vals = new CsCondition[length];
            for (int i = 0; i < length; i++)
                vals[i] = CsCondition.EMPTY;
            return Mk(vals);
        }

        public override bool Equals(object obj)
        {
            return elems.Equals(((CsConditionSeq)obj).elems);
        }

        public override int GetHashCode()
        {
            return elems.GetHashCode();
        }

        public override string ToString()
        {
            if (IsSatisfiable)
                return new Sequence<CsCondition>(ToArray()).ToString();
            else
                return "false";
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
        /// Returns true if all conditions in the sequence are different from FALSE
        /// </summary>
        public bool IsSatisfiable
        {
            get
            {
                return (Empty | Low | Middle | High) == Mask;
            }
        }

        public static CsConditionSeq operator &(CsConditionSeq left, CsConditionSeq right)
        {
            int length = left.Length;
            ulong mask = left.Mask;
            ulong empty = left.Empty & right.Empty;
            ulong low = left.Low & right.Low;
            ulong middle = left.Middle & right.Middle;
            ulong high = left.High & right.High;
            var elems = new Tuple<int, ulong, ulong, ulong, ulong, ulong>(length, mask, empty, low, middle, high);
            var res = new CsConditionSeq(elems);
            return res;
        }

        public CsConditionSeq Update(int i, CsCondition cond)
        {
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
            return new CsConditionSeq(elems);
        }

        public CsConditionSeq Or(int i, CsCondition cond)
        {
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
            return new CsConditionSeq(elems);
        }
    }
}
