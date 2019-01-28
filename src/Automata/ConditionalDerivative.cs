using System;

namespace Microsoft.Automata
{
    /// <summary>
    /// Counter update operations
    /// </summary>
    public enum CounterOp
    {
        /// <summary>
        /// Counter is initialized to 0
        /// </summary>
        INIT,
        /// <summary>
        /// Counter is reset to 0
        /// </summary>
        RESET,
        /// <summary>
        /// Counter is incermented by 1
        /// </summary>
        INCREMENT,
        /// <summary>
        /// Counter is first reset and then incremented
        /// </summary>
        ASSIGN1,
    }

    /// <summary>
    /// Update to counter with given reference
    /// </summary>
    public class CounterUpdate
    {
        Tuple<ICounter, CounterOp> elems;
        public CounterUpdate(ICounter counter, CounterOp update)
        {
            elems = new Tuple<ICounter, CounterOp>(counter, update);
        }


        /// <summary>
        /// counter reference
        /// </summary>
        public ICounter Counter
        {
            get { return elems.Item1; }
        }

        /// <summary>
        /// update operation 
        /// </summary>
        public CounterOp UpdateKind
        {
            get { return elems.Item2; }
        }

        public override bool Equals(object obj)
        {
            var cu = obj as CounterUpdate;
            if (cu == null)
                return false;
            return elems.Equals(cu.elems);
        }

        public override int GetHashCode()
        {
            return elems.GetHashCode();
        }

        string LowerBoundCheck
        {
            get
            {
                if (Counter.LowerBound > 0)
                    return string.Format("{1}<={0}:", Counter.CounterName, Counter.LowerBound);
                else
                    return "";
            }
        }

        string UpperBoundCheck
        {
            get
            {
                if (Counter.UpperBound < int.MaxValue)
                    return string.Format("{0}<{1}:", Counter.CounterName, Counter.UpperBound);
                else
                    return "";
            }
        }

        public override string ToString()
        {
            if (elems.Item2 == CounterOp.INIT)
            {
                return string.Format("{0}:=0", Counter.CounterName);
            }
            else if (elems.Item2 == CounterOp.RESET)
            {
                return LowerBoundCheck + string.Format("{0}:=0", Counter.CounterName);
            }
            else if (elems.Item2 == CounterOp.INCREMENT)
            {
                return UpperBoundCheck + string.Format("{0}++", Counter.CounterName);
            }
            else
            {
                return LowerBoundCheck + string.Format("{0}:=1", Counter.CounterName);
            }
        }
    }

    /// <summary>
    /// conditional derivative
    /// </summary>
    /// <typeparam name="S">input predicate type</typeparam>
    public class ConditionalDerivative<S> : Tuple<Sequence<CounterUpdate>, SymbolicRegexNode<S>>
    {
        public ConditionalDerivative(SymbolicRegexNode<S> derivative)
            : base(Sequence<CounterUpdate>.Empty, derivative)
        {
        }

        public ConditionalDerivative(Sequence<CounterUpdate> condition, SymbolicRegexNode<S> derivative)
            : base(condition, derivative)
        {
        }

        public Sequence<CounterUpdate> Condition
        {
            get { return Item1; }
        }

        public SymbolicRegexNode<S> PartialDerivative
        {
            get { return Item2; }
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
