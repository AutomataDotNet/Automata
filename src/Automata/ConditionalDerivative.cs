using System;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    /// <summary>
    /// Counter update operations. 
    /// Counter opertions other that NOOP (that = 0) can be combined with bitwise-or.
    /// </summary>
    public enum CounterOp
    {
        /// <summary>
        /// Target counter is set to 0
        /// </summary>
        SET0 = 1,
        /// <summary>
        /// Target counter is set to 1
        /// </summary>
        SET1 = 2,
        /// <summary>
        /// Source counter greater or equal lower bound is checked
        /// </summary>
        EXIT = 8,
        /// <summary>
        /// Source counter less than upper bound is checked and the value is incermented by 1
        /// </summary>
        INCR = 4,
        /// <summary>
        /// Source counter greater or equal lower bound is checked and target counter is set to 0
        /// </summary>
        EXIT_SET0 = 16,
        /// <summary>
        /// Source counter greater or equal lower bound is checked and target counter is set to 1
        /// </summary>
        EXIT_SET1 = 32,
    }

    /// <summary>
    /// Operation over counters
    /// </summary>
    public class CounterOperation  
    {
        Tuple<ICounter, CounterOp> elems;
        public CounterOperation(ICounter counter, CounterOp op)
        {
            if (counter == null)
                throw new ArgumentNullException("counter");
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
                    return string.Format("{0}{1}{2}", SpecialCharacters.c(Counter.CounterId), SpecialCharacters.GEQ, Counter.LowerBound);
                    //return SpecialCharacters.XI_LOWERCASE + SpecialCharacters.ToSubscript(Counter.CounterId);
                else
                    return "";
            }
        }

        string UpperBoundCheck
        {
            get
            {
                if (Counter.UpperBound < int.MaxValue)
                    return string.Format("{0}<{1}", SpecialCharacters.c(Counter.CounterId), Counter.UpperBound);
                    //return SpecialCharacters.IOTA_LOWERCASE + SpecialCharacters.ToSubscript(Counter.CounterId);
                else
                    return "";
            }
        }

        public override string ToString()
        {
            switch (elems.Item2)
            {
                case CounterOp.EXIT:
                    return LowerBoundCheck;
                case CounterOp.SET0:
                    return string.Format("{0}{1}0", SpecialCharacters.c(Counter.CounterId), SpecialCharacters.ASSIGN);
                case CounterOp.EXIT_SET0:
                    return string.Format("{2}{3}{0}{1}0", SpecialCharacters.c(Counter.CounterId), SpecialCharacters.ASSIGN, LowerBoundCheck, SpecialCharacters.IMPLIES);
                case CounterOp.SET1:
                    return string.Format("{0}{1}1", SpecialCharacters.c(Counter.CounterId), SpecialCharacters.ASSIGN);
                case CounterOp.EXIT_SET1:
                    return string.Format("{2}{3}{0}{1}1", SpecialCharacters.c(Counter.CounterId), SpecialCharacters.ASSIGN, LowerBoundCheck, SpecialCharacters.IMPLIES);
                case CounterOp.INCR:
                    return string.Format("{2}{3}{0}++", SpecialCharacters.c(Counter.CounterId), SpecialCharacters.ASSIGN, UpperBoundCheck, SpecialCharacters.IMPLIES);
                default:
                    throw new NotImplementedException(elems.Item2.ToString());
            }
        }
    }

    /// <summary>
    /// Represents a counter with finite lower and upper bounds
    /// </summary>
    internal class BoundedCounter : ICounter
    {
        int id;
        int lowerBound;
        int upperBound;

        public BoundedCounter(int id, int lowerBound, int upperBound)
        {
            this.id = id;
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }

        public int CounterId
        {
            get
            {
                return id;
            }
        }

        public int LowerBound
        {
            get
            {
                return lowerBound;
            }
        }

        public int UpperBound
        {
            get
            {
                return upperBound;
            }
        }

        /// <summary>
        /// The counter is intended to be used when subcounters (nested counters) do not occur.
        /// This function returns false always.
        /// </summary>
        public bool ContainsSubCounter(ICounter counter)
        {
            return false;
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
                    //the only valid combination is to first exit and then increment or set to 0 or one
                    if (f_update.OperationKind == CounterOp.EXIT && (g_update.OperationKind == CounterOp.INCR || g_update.OperationKind == CounterOp.SET1))
                        f_o_g_list.Add(new CounterOperation(c, CounterOp.EXIT_SET1));
                    else if (f_update.OperationKind == CounterOp.EXIT && g_update.OperationKind == CounterOp.SET0)
                        f_o_g_list.Add(new CounterOperation(c, CounterOp.EXIT_SET0));
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
