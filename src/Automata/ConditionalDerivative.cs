using System;
using System.Collections.Generic;

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
        SET0,
        /// <summary>
        /// Counter is initialized to 1
        /// </summary>
        SET1,
        /// <summary>
        /// Lower bound is checked
        /// </summary>
        EXIT,
        /// <summary>
        /// Lower bound is checked and the counter is set to 0
        /// </summary>
        EXIT_SET0,
        /// <summary>
        /// Upper bound is checked and the counter is incermented by 1
        /// </summary>
        INCR,
        /// <summary>
        /// Lower bound is checked and the counter is set to 1
        /// </summary>
        EXIT_SET1,
    }

    /// <summary>
    /// Operation over counters
    /// </summary>
    public class CounterOperation  
    {
        Tuple<ICounter, CounterOp> elems;
        public CounterOperation(ICounter counter, CounterOp op)
        {
            elems = new Tuple<ICounter, CounterOp>(counter, op);
        }

        /// <summary>
        /// counter reference
        /// </summary>
        public ICounter Counter
        {
            get { return elems.Item1; }
        }

        /// <summary>
        /// what kind of operation 
        /// </summary>
        public CounterOp OperationKind
        {
            get { return elems.Item2; }
        }

        public override bool Equals(object obj)
        {
            var cu = obj as CounterOperation;
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
            if (elems.Item2 == CounterOp.SET0)
            {
                return string.Format("{0}:=0", Counter.CounterName);
            }
            else if (elems.Item2 == CounterOp.EXIT_SET0)
            {
                return LowerBoundCheck + string.Format("{0}:=0", Counter.CounterName);
            }
            else if (elems.Item2 == CounterOp.INCR)
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
    /// Conditional derivative
    /// </summary>
    /// <typeparam name="S">input predicate type</typeparam>
    public class ConditionalDerivative<S> : Tuple<Sequence<CounterOperation>, SymbolicRegexNode<S>>
    {
        public ConditionalDerivative(SymbolicRegexNode<S> derivative)
            : base(Sequence<CounterOperation>.Empty, derivative)
        {
        }

        public ConditionalDerivative(Sequence<CounterOperation> condition, SymbolicRegexNode<S> derivative)
            : base(condition, derivative)
        {
        }

        /// <summary>
        /// If null is returned then the composition is not enabled
        /// </summary>
        public ConditionalDerivative<S> Compose(ConditionalDerivative<S> that)
        {
            var f = this.Condition;
            var g = that.Condition;
            var builder = this.PartialDerivative.builder;
            var f_o_g = ComposeCounterUpdates(f, g);
            if (f_o_g == null)
                return null;
            else
            {
                var pd = builder.MkConcat(this.PartialDerivative, that.PartialDerivative);
                var cd = new ConditionalDerivative<S>(f_o_g, pd);
                return cd;
            }
        }

        private Sequence<CounterOperation> ComposeCounterUpdates(Sequence<CounterOperation> f, Sequence<CounterOperation> g)
        {
            var f_o_g_list = new List<CounterOperation>();
            for (int i = 0; i < f.Length; i++)
            {
                var f_update = f[i];
                var c = f_update.Counter;
                CounterOperation g_update;
                if (g.TryGetElement(x => x.Counter.Equals(c), out g_update))
                {
                    //the only valid combination for same counter is first reset then increment
                    if (f_update.OperationKind == CounterOp.EXIT_SET0 && g_update.OperationKind == CounterOp.INCR)
                        f_o_g_list.Add(new CounterOperation(c, CounterOp.EXIT_SET1));
                    else
                        //composition is not enabled
                        return null;
                }
                else
                {
                    f_o_g_list.Add(f_update);
                }
            }
            for (int i = 0; i < g.Length; i++)
            {
                //ignore the counters already processed 
                if (!f.Exists(x => x.Counter.Equals(g[i].Counter)))
                    f_o_g_list.Add(g[i]);
            }
            return new Sequence<CounterOperation>(f_o_g_list.ToArray());
        }

        public Sequence<CounterOperation> Condition
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
