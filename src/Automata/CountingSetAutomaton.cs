using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    /// <summary>
    /// Counting-set Automaton
    /// </summary>
    public class CsAutomaton<S> : Automaton<CsLabel<S>>
    {
        PowerSetStateBuilder stateBuilder;
        Dictionary<int, ICounter> countingStates;

        public CsAutomaton(Automaton<CsLabel<S>> aut, PowerSetStateBuilder stateBuilder, Dictionary<int, ICounter> countingStates) : base(aut)
        {
            this.stateBuilder = stateBuilder;
            this.countingStates = countingStates;
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
    }

    public class CsLabel<S>
    {
        S input;
        bool isFinalCondition;
        public readonly CsConditionSeq Conditions;
        public readonly CsUpdateSeq Updates;

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

        CsLabel(bool isFinalCondition, S input, CsConditionSeq conditions, CsUpdateSeq updates)
        {
            this.input = input;
            this.isFinalCondition = isFinalCondition;
            this.Conditions = conditions;
            this.Updates = updates;
        }

        public static CsLabel<S> MkFinalCondition(CsConditionSeq conditions)
        {
            return new CsLabel<S>(true, default(S), conditions, CsUpdateSeq.False);
        }

        public static CsLabel<S> MkTransitionLabel(S input, CsConditionSeq conditions, CsUpdateSeq updates)
        {
            return new CsLabel<S>(false, input, conditions, updates);
        }

        public override string ToString()
        {
            if (isFinalCondition)
            {
                return DescribeCounterCondition();
            }
            else
            {
                var s = input.ToString();
                var cond = DescribeCounterCondition();
                var upd = DescribeCounterUpdate();
                if (cond != "")
                {
                    s += "&amp;" + cond;
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
            for (int i = 0; i < Conditions.Length; i++)
            {
                if (Conditions[i] != CsCondition.EMPTY && Conditions[i] != CsCondition.NONEMPTY)
                {
                    if (s != "")
                        s += "&amp;";
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
                        s += "&amp;";
                    s += Conditions[i].ToString() + "(c" + i.ToString() + ")";
                }
            }
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
        int count;
        ulong elems = 0;

        public static readonly CsUpdateSeq False = new CsUpdateSeq(0, 0);
        public int Length
        {
            get { return count; }
        }

        CsUpdateSeq(ulong elems, int count = 0)
        {
            this.elems = elems;
            this.count = count;
        }

        public static CsUpdateSeq Mk(int i, CsUpdate update, int length = 0)
        {
            if (length > 0 && length > 21)
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
            return new CsUpdateSeq(left.elems | right.elems);
        }

        public static CsUpdateSeq operator &(CsUpdateSeq left, CsUpdateSeq right)
        {
            return new CsUpdateSeq(left.elems & right.elems);
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

        public CsUpdateSeq Update(int i, CsUpdate cond)
        {
            ulong mask = ~(((ulong)7) << (3 * i));
            var conds1 = (elems & mask) | (((ulong)cond) << (3 * i));
            return new CsUpdateSeq(conds1, count);
        }

        public CsUpdateSeq Or(int i, CsCondition cond)
        {
            var conds1 = elems | (((ulong)cond) << (3 * i));
            return new CsUpdateSeq(conds1);
        }

        public bool IsEmpty(int i)
        {
            return ((elems >> (3 * i)) & 7) == 0;
        }
    }

    public class CsConditionSeq
    {
        int count;
        ulong conds = 0;

        public int Length
        {
            get
            {
                return count;
            }
        }

        public static readonly CsConditionSeq False = new CsConditionSeq(0, 21);

        CsConditionSeq(ulong conds, int count = 0)
        {
            this.conds = conds;
            this.count = count;
        }

        public static CsConditionSeq Mk(int i, CsCondition cond, int length = 21)
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


        public static CsConditionSeq operator |(CsConditionSeq left, CsConditionSeq right)
        {
            return new CsConditionSeq(left.conds | right.conds, left.count);
        }

        public static CsConditionSeq operator &(CsConditionSeq left, CsConditionSeq right)
        {
            return new CsConditionSeq(left.conds & right.conds, left.count);
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
            return new CsConditionSeq(conds1, count);
        }

        public CsConditionSeq Or(int i, CsCondition cond)
        {
            var conds1 = conds | (((ulong)cond) << (3 * i));
            return new CsConditionSeq(conds1, count);
        }

        public bool IsEmpty(int i)
        {
            return ((conds >> (3 * i)) & 7) == 0;
        }
    }
}
