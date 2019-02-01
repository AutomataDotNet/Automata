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

        private Sequence<CounterUpdate> ComposeCounterUpdates(Sequence<CounterUpdate> f, Sequence<CounterUpdate> g)
        {
            var f_o_g_list = new List<CounterUpdate>();
            for (int i = 0; i < f.Length; i++)
            {
                var f_update = f[i];
                var c = f_update.Counter;
                CounterUpdate g_update;
                if (g.TryGetElement(x => x.Counter.Equals(c), out g_update))
                {
                    //the only valid combination for same counter is first reset then increment
                    if (f_update.UpdateKind == CounterOp.RESET && g_update.UpdateKind == CounterOp.INCREMENT)
                        f_o_g_list.Add(new CounterUpdate(c, CounterOp.ASSIGN1));
                    else
                        //TBD: the counter updates cannot be combined unless the second counters lower bound is 0
                        return null;
                }
                else
                {
                    f_o_g_list.Add(f_update);
                }
            }
            for (int i = 0; i < g.Length; i++)
            {
                if (!f.Exists(x => x.Counter.Equals(g[i].Counter)))
                    f_o_g_list.Add(g[i]);
            }
            return new Sequence<CounterUpdate>(f_o_g_list.ToArray());
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
