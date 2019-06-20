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
    public class CsAutomaton<S> : IAutomaton<CsLabel<S>>
    {
        Automaton<CsLabel<S>> aut;
        public Automaton<CsLabel<S>> Automaton
        {
            get { return aut; }
        }

        public int InitialState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IBooleanAlgebra<CsLabel<S>> Algebra
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public CsAutomaton(Automaton<CsLabel<S>> aut)
        {
            this.aut = aut;
        }

        public void ShowGraph()
        {
            //TBD
        }

        #region TBD

        public IEnumerable<Move<CsLabel<S>>> GetMoves()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> GetStates()
        {
            throw new NotImplementedException();
        }

        public string DescribeState(int state)
        {
            throw new NotImplementedException();
        }

        public string DescribeLabel(CsLabel<S> lab)
        {
            throw new NotImplementedException();
        }

        public string DescribeStartLabel()
        {
            throw new NotImplementedException();
        }

        public bool IsFinalState(int state)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Move<CsLabel<S>>> GetMovesFrom(int state)
        {
            throw new NotImplementedException();
        }
        #endregion

    }

    public class CsLabel<S>
    {
        S input;
        bool isFinalCondition;
        public readonly CsConditionSeq conditions;
        public readonly CsUpdateSeq updates;

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

        //public CsCondition GetCondition(int counterid)
        //{
        //    return CsAlgebra.GetCondition(this.condUpdates[counterid]);
        //}

        //public CsUpdate GetUpdate(int counterid)
        //{
        //    if (isFinalCondition)
        //        throw new AutomataException(AutomataExceptionKind.InvalidCall);

        //    return CsAlgebra.GetUpdate(this.condUpdates[counterid]);
        //}


        CsLabel(bool isFinalCondition, S input, CsConditionSeq conditions, CsUpdateSeq updates)
        {
            this.input = input;
            this.isFinalCondition = isFinalCondition;
            this.conditions = conditions;
            this.updates = updates;
        }

        public static CsLabel<S> MkFinalCondition(CsConditionSeq conditions)
        {
            return new CsLabel<S>(true, default(S), conditions, CsUpdateSeq.False);
        }

        public static CsLabel<S> MkTransitionLabel(S input, CsConditionSeq conditions, CsUpdateSeq updates)
        {
            return new CsLabel<S>(false, input, conditions, updates);
        }

        //public override string ToString()
        //{
        //    if (isFinalCondition)
        //    {
        //        var conds = Array.ConvertAll(condUpdates, CsAlgebra.GetCondition);
        //        string s = new Sequence<CsCondition>(conds).ToString();
        //        return s;
        //    }
        //    else
        //    {
        //        var arr = Array.ConvertAll(condUpdates, CsAlgebra.ToString);
        //        string s = string.Join(",", arr);
        //        return s;
        //    }
        //}
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

        public static readonly CsConditionSeq False = new CsConditionSeq(0, 0);

        CsConditionSeq(ulong conds, int count = 0)
        {
            this.conds = conds;
            this.count = count;
        }

        public static CsConditionSeq Mk(int i, CsCondition cond, int length = 0)
        {
            if (length > 0 && length > 21)
                throw new NotImplementedException();

            return new CsConditionSeq(((ulong)cond) << (3 * i), length);
        }

        public static CsConditionSeq operator |(CsConditionSeq left, CsConditionSeq right)
        {
            return new CsConditionSeq(left.conds | right.conds);
        }

        public static CsConditionSeq operator &(CsConditionSeq left, CsConditionSeq right)
        {
            return new CsConditionSeq(left.conds & right.conds);
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
            return new CsConditionSeq(conds1);
        }

        public CsConditionSeq Or(int i, CsCondition cond)
        {
            var conds1 = conds | (((ulong)cond) << (3 * i));
            return new CsConditionSeq(conds1);
        }

        public bool IsEmpty(int i)
        {
            return ((conds >> (3 * i)) & 7) == 0;
        }
    }
}
