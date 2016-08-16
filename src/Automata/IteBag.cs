using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Automata.Internal;

namespace Microsoft.Automata
{
    public class IteBag<T>
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

        public T ToSet()
        {
            throw new NotImplementedException();
        }
    }

    internal enum BagOpertion
    {
        PLUS, MINUS
    }

    public class IteBagBuilder<T>
    {
        internal IBooleanAlgebra<T> algebra;
        Dictionary<int, IteBag<T>> leafCache = new Dictionary<int, IteBag<T>>();
        Dictionary<Tuple<T, IteBag<T>, IteBag<T>>, IteBag<T>> nodeCache = 
            new Dictionary<Tuple<T, IteBag<T>, IteBag<T>>, IteBag<T>>();
        Dictionary<Tuple<BagOpertion, IteBag<T>, IteBag<T>>, IteBag<T>> opCache = 
            new Dictionary<Tuple<BagOpertion, IteBag<T>, IteBag<T>>, IteBag<T>>();

        public IteBagBuilder(IBooleanAlgebra<T> algebra)
        {
            this.algebra = algebra;
            leafCache[0] = new IteBag<T>(this, 0, default(T), null, null);
            leafCache[1] = new IteBag<T>(this, 1, default(T), null, null);
        }

        public IteBag<T> MkSingleton(T predicate)
        {
            IteBag<T> bag;
            var key = new Tuple<T, IteBag<T>, IteBag<T>>(predicate, leafCache[1], leafCache[0]);
            if (nodeCache.TryGetValue(key, out bag))
                return bag;

            bag = new IteBag<T>(this, -1, predicate, leafCache[1], leafCache[0]);
            nodeCache[key] = bag;
            return bag;
        }

        internal IteBag<T> Op(BagOpertion op, IteBag<T> bag1, IteBag<T> bag2)
        {
            IteBag<T> bag;
            var key = new Tuple<BagOpertion, IteBag<T>, IteBag<T>>(op, bag1, bag2);
            if (opCache.TryGetValue(key, out bag))
                return bag;

            bag = OpInContext(op, algebra.True, bag1, bag2);
            opCache[key] = bag;
            return bag;
        }

        private IteBag<T> OpInContext(BagOpertion op, T context, IteBag<T> bag1, IteBag<T> bag2)
        {
            throw new NotImplementedException();
        }
    }
}
