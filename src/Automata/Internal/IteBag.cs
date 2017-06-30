using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Automata;

namespace Microsoft.Automata
{
    internal class IteBag<T>
    {
        IteBagBuilder<T> builder;
        readonly public int Count;
        readonly public T Predicate;
        readonly public IteBag<T> TrueCase;
        readonly public IteBag<T> FalseCase;
        public bool IsLeaf
        {
            get
            {
                return TrueCase == null;
            }
        }
        internal IteBag(IteBagBuilder<T> builder, int count, T predicate, IteBag<T> trueCase, IteBag<T> falseCase)
        {
            this.builder = builder;
            this.Count = count;
            this.Predicate = predicate;
            this.TrueCase = trueCase;
            this.FalseCase = falseCase;
        }

        public IteBag<T> Plus(IteBag<T> bag)
        {
            if (bag.builder != builder)
                throw new AutomataException(AutomataExceptionKind.IteBagError);
            return builder.Op(BagOpertion.PLUS, this, bag);
        }

        public IteBag<T> Minus(IteBag<T> bag)
        {
            if (bag.builder != builder)
                throw new AutomataException(AutomataExceptionKind.IteBagError);
            return builder.Op(BagOpertion.MINUS, this, bag);
        }

        public IteBag<T> SetMinus(IteBag<T> bag)
        {
            if (bag.builder != builder)
                throw new AutomataException(AutomataExceptionKind.IteBagError);
            return builder.Op(BagOpertion.SETMINUS, this, bag);
        }

        public IteBag<T> Min(IteBag<T> bag)
        {
            if (bag.builder != builder)
                throw new AutomataException(AutomataExceptionKind.IteBagError);
            return builder.Op(BagOpertion.MIN, this, bag);
        }

        public bool IntersectsWith(IteBag<T> bag)
        {
            if (bag.builder != builder)
                throw new AutomataException(AutomataExceptionKind.IteBagError);
            return builder.IsNonemptyIntersection(this, bag);
        }

        public T ToSet()
        {
            if (IsLeaf)
                if (Count > 0)
                    return builder.algebra.True;
                else
                    return builder.algebra.False;
            else
                return builder.algebra.MkOr(builder.algebra.MkAnd(Predicate, TrueCase.ToSet()), builder.algebra.MkAnd(builder.algebra.MkNot(Predicate), FalseCase.ToSet()));
        }

    }

    internal enum BagOpertion
    {
        PLUS, MINUS, SETMINUS, MIN, MAX, ISNONEMPTYINTERSECTION
    }

    internal class IteBagBuilder<T>
    {
        internal IBooleanAlgebra<T> algebra;
        Dictionary<int, IteBag<T>> leafCache = new Dictionary<int, IteBag<T>>();
        Dictionary<Tuple<T, IteBag<T>, IteBag<T>>, IteBag<T>> nodeCache = 
            new Dictionary<Tuple<T, IteBag<T>, IteBag<T>>, IteBag<T>>();
        Dictionary<Tuple<BagOpertion, T, IteBag<T>, IteBag<T>>, IteBag<T>> opCache = 
            new Dictionary<Tuple<BagOpertion, T, IteBag<T>, IteBag<T>>, IteBag<T>>();
        Dictionary<Tuple<T, IteBag<T>, IteBag<T>>, bool> intersectionEmptinessTestCache =
            new Dictionary<Tuple<T, IteBag<T>, IteBag<T>>, bool>();

        //profiling info
        Dictionary<BagOpertion, int> count = new Dictionary<BagOpertion, int>();
        Dictionary<BagOpertion, System.Diagnostics.Stopwatch> stopwatch = new Dictionary<BagOpertion, System.Diagnostics.Stopwatch>();

        public int GetCallCount(BagOpertion op)
        {
            return count[op];
        }

        public long GetElapsedMilliseconds(BagOpertion op)
        {
            return stopwatch[op].ElapsedMilliseconds;
        }

        public IteBagBuilder(IBooleanAlgebra<T> algebra)
        {
            this.algebra = algebra;
            leafCache[0] = new IteBag<T>(this, 0, default(T), null, null);
            leafCache[1] = new IteBag<T>(this, 1, default(T), null, null);
            foreach (BagOpertion op in Enum.GetValues(typeof(BagOpertion)))
            {
                count[op] = 0;
                stopwatch[op] = new System.Diagnostics.Stopwatch();
            }
        }

        public IteBag<T> MkSingleton(T predicate)
        {
            return MkNode(predicate, leafCache[1], leafCache[0]);
        }

        IteBag<T>  MkNode(T predicate, IteBag<T> t, IteBag<T> f)
        {
            if (t == f)
                return t;

            IteBag<T> bag;
            var key = new Tuple<T, IteBag<T>, IteBag<T>>(predicate, t, f);
            if (nodeCache.TryGetValue(key, out bag))
                return bag;

            bag = new IteBag<T>(this, -1, predicate, t, f);
            nodeCache[key] = bag;
            return bag;
        }

        IteBag<T> MkLeaf(int count)
        {
            IteBag<T> leaf;
            if (leafCache.TryGetValue(count, out leaf))
                return leaf;

            leaf = new IteBag<T>(this, count, default(T), null, null);
            leafCache[count] = leaf;
            return leaf;
        }

        internal IteBag<T> Op(BagOpertion op, IteBag<T> bag1, IteBag<T> bag2)
        {
            count[op] += 1;
            stopwatch[op].Start();
            var res = OpInContext(op, algebra.True, bag1, bag2);
            stopwatch[op].Stop();
            return res;
        }

        /// <summary>
        /// Invariant: algebra.IsSatisfiable(context), assumes that bag1 has no dead branches
        /// </summary>
        private IteBag<T> OpInContext(BagOpertion op, T context, IteBag<T> bag1, IteBag<T> bag2)
        {
            IteBag<T> bag;
            var key = new Tuple<BagOpertion, T, IteBag<T>, IteBag<T>>(op, context, bag1, bag2);
            if (opCache.TryGetValue(key, out bag))
                return bag;

            if (bag1.IsLeaf)
                return OpInContext2(op, context, bag1, bag2);

            var context1 = algebra.MkAnd(context, bag1.Predicate);
            var context2 = algebra.MkAnd(context, algebra.MkNot(bag1.Predicate));

            var t = OpInContext(op, context1, bag1.TrueCase, bag2);
            var f = OpInContext(op, context2, bag1.FalseCase, bag2);
            bag = MkNode(bag1.Predicate, t, f);
            opCache[key] = bag;
            return bag;
        }

        private IteBag<T> OpInContext2(BagOpertion op, T context, IteBag<T> leaf, IteBag<T> bag2)
        {
            if (bag2.IsLeaf)
            {
                var newleaf = MkLeaf(ApplyOp(op, leaf.Count, bag2.Count));
                return newleaf;
            }

            IteBag<T> bag;
            var key = new Tuple<BagOpertion, T, IteBag<T>, IteBag<T>>(op, context, leaf, bag2);
            if (opCache.TryGetValue(key, out bag))
                return bag;

            var context_t = algebra.MkAnd(context, bag2.Predicate);
            if (algebra.IsSatisfiable(context_t))
            {
                var context_f = algebra.MkAnd(context, algebra.MkNot(bag2.Predicate));
                if (algebra.IsSatisfiable(context_f))
                    bag = MkNode(bag2.Predicate, OpInContext2(op, context_t, leaf, bag2.TrueCase), OpInContext2(op, context_f, leaf, bag2.FalseCase));
                else //~IsSat(context & ~ bag2.Predicate) ---> IsValid(context ==> bag2.Predicate)  
                    bag = OpInContext2(op, context, leaf, bag2.TrueCase);
            }
            else //~IsSat(context & bag2.Predicate) ---> IsValid(context ==> ~ bag2.Predicate)          
                bag = OpInContext2(op, context, leaf, bag2.FalseCase);
            opCache[key] = bag;
            return bag;
        }

        private int ApplyOp(BagOpertion op, int p1, int p2)
        {
            switch (op)
            {
                case BagOpertion.PLUS:
                    return p1 + p2;
                case BagOpertion.MINUS:
                    return Math.Max(0, p1 - p2);
                case BagOpertion.SETMINUS:
                    return Math.Max(0, Math.Min(1, p1) - p2);
                case BagOpertion.MIN:
                    return Math.Min(p1, p2);
                case BagOpertion.MAX:
                    return Math.Max(p1, p2);
                default:
                    throw new AutomataException(AutomataExceptionKind.IteBagError);
            }
        }

        internal bool IsNonemptyIntersection(IteBag<T> bag1, IteBag<T> bag2)
        {
            count[BagOpertion.ISNONEMPTYINTERSECTION] += 1;
            stopwatch[BagOpertion.ISNONEMPTYINTERSECTION].Start();
            var res = IsNonemptyIntersection_1(algebra.True, bag1, bag2);
            stopwatch[BagOpertion.ISNONEMPTYINTERSECTION].Stop();
            return res;
        }

        private bool IsNonemptyIntersection_1(T context, IteBag<T> bag1, IteBag<T> bag2)
        {
            bool res;
            var key = new Tuple<T, IteBag<T>, IteBag<T>>(context, bag1, bag2);
            if (intersectionEmptinessTestCache.TryGetValue(key, out res))
                return res;
            else
            {
                if (bag1.IsLeaf)
                    res = IsNonemptyIntersection_2(context, bag1, bag2);
                else
                {
                    var context1 = algebra.MkAnd(context, bag1.Predicate);
                    if (IsNonemptyIntersection_1(context1, bag1.TrueCase, bag2))
                        res = true;
                    else
                    {
                        var context2 = algebra.MkAnd(context, algebra.MkNot(bag1.Predicate));
                        if (IsNonemptyIntersection_1(context2, bag1.FalseCase, bag2))
                            res = true;
                        else 
                            res = false;
                    }
                }
                intersectionEmptinessTestCache[key] = res;
                return res;
            }
        }

        private bool IsNonemptyIntersection_2(T context, IteBag<T> leaf, IteBag<T> bag2)
        {
            bool res;
            var key = new Tuple<T, IteBag<T>, IteBag<T>>(context, leaf, bag2);
            if (intersectionEmptinessTestCache.TryGetValue(key, out res))
                return res;
            else
            {
                if (bag2.IsLeaf)
                {
                    if (leaf.Count > 0 && bag2.Count > 0)
                        res = true;
                    else
                        res = false;
                }
                else
                {
                    var context_t = algebra.MkAnd(context, bag2.Predicate);
                    if (algebra.IsSatisfiable(context_t))
                    {
                        var context_f = algebra.MkAnd(context, algebra.MkNot(bag2.Predicate));
                        if (algebra.IsSatisfiable(context_f))
                        {
                            if (IsNonemptyIntersection_2(context_t, leaf, bag2.TrueCase))
                                res = true;
                            else if (IsNonemptyIntersection_2(context_f, leaf, bag2.FalseCase))
                                res = true;
                            else 
                                res = false;
                        }
                        else //~IsSat(context & ~ bag2.Predicate) ---> IsValid(context ==> bag2.Predicate)  
                            res = IsNonemptyIntersection_2(context, leaf, bag2.TrueCase);
                    }
                    else //~IsSat(context & bag2.Predicate) ---> IsValid(context ==> ~ bag2.Predicate)          
                        res = IsNonemptyIntersection_2(context, leaf, bag2.FalseCase);
                }
                intersectionEmptinessTestCache[key] = res;
                return res;
            }
        }
    }
}
