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

        CsAlgebra<S> productAlgebra;

        public CsAutomaton(IBooleanAlgebra<S> inputAlgebra, Automaton<CsLabel<S>> aut, PowerSetStateBuilder stateBuilder, Dictionary<int, ICounter> countingStates) : base(aut)
        {
            this.stateBuilder = stateBuilder;
            this.countingStates = countingStates;
            this.productAlgebra = new CsAlgebra<S>(inputAlgebra, countingStates.Count);
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
            foreach (var q in mems)
            {
                if (countingStates.ContainsKey(q))
                {
                    var c = countingStates[q];
                    s += "&#13;c" + c.CounterId + ":[" + c.LowerBound + "," + c.UpperBound + "]";
                }
            }
            return s;
        }

        public override string DescribeStartLabel()
        {
            var q0 = GetOriginalInitialState();
            if (countingStates.ContainsKey(q0))
            {
                var c = countingStates[q0];
                return string.Format("c{0}:=0", c.CounterId);
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
                var ccond = CsConditionSeq.MkFalse(ca.NrOfCounters);
                if (ca.IsCountingState(move.SourceState))
                {
                    var cid = ca.GetCounter(move.SourceState).CounterId;
                    if (move.Label.Item2.First.OperationKind == CounterOp.EXIT ||
                        move.Label.Item2.First.OperationKind == CounterOp.EXIT_SET0 ||
                        move.Label.Item2.First.OperationKind == CounterOp.EXIT_SET1)
                    {
                        ccond = ccond.Or(cid, CsCondition.CANEXIT);
                    }
                    else
                    {
                        if (move.Label.Item2.First.OperationKind != CounterOp.INCR)
                            throw new AutomataException(AutomataExceptionKind.InternalError);

                        ccond = ccond.Or(cid, CsCondition.CANLOOP);
                    }
                }
                productmoves.Add(Move<CsPred<S>>.Create(move.SourceState, move.TargetState, alg.MkPredicate(move.Label.Item1.Element, ccond)));
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

            var csa = new CsAutomaton<S>(((CABA<S>)ca.Algebra).builder.solver, csa_aut, sb, ca.countingStates);

            return csa;
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
            return new CsLabel<S>(true, default(S), conditions, CsUpdateSeq.MkNOOP(conditions.length), inputToString);
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
                    s += "/" + upd;
                }
                return s;
            }
        }

        private string DescribeCounterCondition()
        {
            string s = "";
            for (int i = 0; i < Conditions.length; i++)
            {
                if (Conditions[i] != CsCondition.EMPTY && Conditions[i] != CsCondition.NONEMPTY)
                {
                    if (s != "")
                        s += "&";
                    s += Conditions[i].ToString() + "(c" + i.ToString() + ")";
                }
            }
            return s;
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
        /// Set is empty
        /// </summary>
        EMPTY = 0,
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

    public class CsConditionSeq
    {
        internal int length;
        ulong conds = 0;

        public static CsConditionSeq MkFalse(int lenth)
        {
            return new CsConditionSeq(0, lenth);
        }

        CsConditionSeq(ulong conds, int count)
        {
            this.conds = conds;
            this.length = count;
        }

        public static CsConditionSeq Mk(int i, CsCondition cond, int length)
        {
            if (length > 0 && length > 21)
                throw new NotImplementedException();

            return new CsConditionSeq(((ulong)cond) << (3 * i), length);
        }

        public static CsConditionSeq Mk(params CsCondition[] vals)
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
            return new CsConditionSeq(x, vals.Length);
        }

        public CsCondition this[int i]
        {
            get
            {
                return (CsCondition)((conds >> (3 * i)) & 7);
            }
        }

        public override bool Equals(object obj)
        {
            return conds == ((CsConditionSeq)obj).conds;
        }

        public override int GetHashCode()
        {
            return conds.GetHashCode();
        }

        public CsConditionSeq Update(int i, CsCondition cond)
        {
            ulong mask = ~(((ulong)7) << (3 * i));
            var conds1 = (conds & mask) | (((ulong)cond) << (3 * i));
            return new CsConditionSeq(conds1, length);
        }

        public CsConditionSeq Or(int i, CsCondition cond)
        {
            var conds1 = conds | (((ulong)cond) << (3 * i));
            return new CsConditionSeq(conds1, length);
        }

        public bool IsEmpty(int i)
        {
            return ((conds >> (3 * i)) & 7) == 0;
        }

        public CsCondition[] ToArray()
        {
            var list = new List<CsCondition>();
            var arr = new CsCondition[length];
            for (int i = 0; i < length; i++)
                arr[i] = this[i];
            return arr;
        }

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < length; i++)
            {
                if (this[i] != CsCondition.EMPTY && this[i] != CsCondition.NONEMPTY)
                {
                    if (s != "")
                        s += "&";
                    if (this[i] == CsCondition.LOW)
                        s += string.Format("{0}(c{1})", "L", i);
                    else if (this[i] == CsCondition.MIDDLE)
                        s += string.Format("{0}(c{1})", "M", i);
                    else if (this[i] == CsCondition.HIGH)
                        s += string.Format("{0}(c{1})", "H", i);
                    else 
                        s += string.Format("{0}(c{1})", this[i], i);
                }
            }
            return s;
        }
    }
}
