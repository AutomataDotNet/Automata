using System;
using System.Collections.Generic;

namespace Microsoft.Automata
{

    /// <summary>
    /// Represents a Boolean cobination of input predicates and counter conditions
    /// </summary>
    public class CsPred<T>
    {
        internal BDD<T> pred;
        CsAlgebra<T> alg;
        internal CsPred(CsAlgebra<T> alg, BDD<T> pred)
        {
            this.alg = alg;
            this.pred = pred;
        }

        /// <summary>
        /// Gets the size of the underlying internal representation of the counter predicate.
        /// </summary>
        public int CounterPredicateSize
        {
            get
            {
                return pred.CountNodes();
            }
        }

        /// <summary>
        /// Gets the algebra of the predicate.
        /// </summary>
        public CsAlgebra<T> Algebra
        {
            get
            {
                return alg;
            }
        }

        /// <summary>
        /// Project the input predicate
        /// </summary>
        public T ProjectSecond()
        {
            return ProjectSecond_helper(new Dictionary<BDD<T>, T>(), pred);
        }

        static T ProjectSecond_helper(Dictionary<BDD<T>, T> map, BDD<T> pred)
        {
            T proj;
            var alg = ((BDDAlgebra<T>)(pred.algebra)).LeafAlgebra;
            if (map.TryGetValue(pred, out proj))
                return proj;
            else
            {
                if (pred.IsLeaf)
                    proj = pred.Leaf;
                else
                {
                    var left = ProjectSecond_helper(map, (BDD<T>)pred.Zero);
                    var right = ProjectSecond_helper(map, (BDD<T>)pred.One);
                    proj = alg.MkOr(left, right);
                }
                map[pred] = proj;
                return proj;
            }
        }

        /// <summary>
        /// Enumerate the individual conjuncts of the predicate in DNF form.
        /// </summary>
        public IEnumerable<Tuple<CsConditionSeq, T>> GetSumOfProducts()
        {
            return EnumerateCases(pred);
        }

        /// <summary>
        /// Return all the conjuncts of the predicate in DNF form as an array.
        /// The DNF may be very large even if CounterPredicateSize is relatively small.
        /// </summary>
        public Tuple<CsConditionSeq, T>[] ToArray()
        {
            var list = new List<Tuple<CsConditionSeq, T>>(EnumerateCases(pred));
            var arr = list.ToArray();
            return arr;
        }

        IEnumerable<Tuple<CsConditionSeq, T>> EnumerateCases(BDD<T> bdd)
        {
            if (bdd.IsLeaf)
            {
                var val = ((BDD<T>)bdd).Leaf;
                if (bdd.Algebra.Second.IsSatisfiable(val))
                    //note that CsConditionSeq.False means that all counters are disabled
                    yield return new Tuple<CsConditionSeq, T>(alg.TrueCsConditionSeq, ((BDD<T>)bdd).Leaf);
            }
            else
            {
                //even bit is about exit condition, odd bit is about increment condition
                bool is_canexit_bit = (bdd.Ordinal % 2 == 0);
                //counter id
                int i = bdd.Ordinal / 2;
                if (bdd.Zero == bdd.One)
                {
                    ; //TBD
                }
                else
                {
                    foreach (var path in EnumerateCases((BDD<T>)bdd.Zero))
                    {
                        #region Zero branch: means that the bit is 0
                        var v = path.Item1[i];
                        if (is_canexit_bit)
                            v = v & CsCondition.CANNOTEXIT; //cannot exit
                        else
                            v = v & CsCondition.CANNOTLOOP; //cannot increment
                        if (v != CsCondition.FALSE) //FALSE means unsatisfiable
                            yield return new Tuple<CsConditionSeq, T>(path.Item1.And(i, v), path.Item2);
                        #endregion
                    }
                    foreach (var path in EnumerateCases((BDD<T>)bdd.One))
                    {
                        #region One branch: means that the bit is 1
                        var v = path.Item1[i];
                        if (is_canexit_bit)
                            v = v & CsCondition.CANEXIT; //can exit
                        else
                            v = v & CsCondition.CANLOOP; //can increment
                        if (v != CsCondition.FALSE) //FALSE means unsatisfiable
                            yield return new Tuple<CsConditionSeq, T>(path.Item1.And(i, v), path.Item2);
                        #endregion
                    }
                }
            }
        }

        public override string ToString()
        {
            return pred.ToString();
        }

        /// <summary>
        /// Make the conjunction of left with right
        /// </summary>
        public static CsPred<T> operator &(CsPred<T> left, CsPred<T> right)
        {
            return left.Algebra.MkAnd(left, right);
        }

        /// <summary>
        /// Make the disjunction of left with right
        /// </summary>
        public static CsPred<T> operator |(CsPred<T> left, CsPred<T> right)
        {
            return left.Algebra.MkOr(left, right);
        }

        /// <summary>
        /// Make the negation of the predicate
        /// </summary>
        public static CsPred<T> operator ~(CsPred<T> predicate)
        {
            return predicate.Algebra.MkNot(predicate);
        }

        /// <summary>
        /// Returns true if the predicate is satisfiable
        /// </summary>
        public bool CheckSat()
        {
            return alg.IsSatisfiable(this);
        }

        /// <summary>
        /// Returns true if the predicate is valid
        /// </summary>
        public bool CheckValidity()
        {
            return !alg.IsSatisfiable(alg.MkNot(this));
        }
    }

    /// <summary>
    /// Boolean algebra for CsPred predicates
    /// </summary>
    /// <typeparam name="T">input predicate type</typeparam>
    public class CsAlgebra<T> : IBooleanAlgebra<CsPred<T>>, IPrettyPrinter<CsPred<T>>
    {
        /// <summary>
        /// Generic BDD algebra use for internal representation
        /// </summary>
        internal readonly BDDAlgebra<T> NodeAlgebra;
        /// <summary>
        /// Algebra of T-predicates used for leaves of the generic BDD algebra
        /// </summary>
        public readonly IBooleanAlgebra<T> LeafAlgebra;
        /// <summary>
        /// Number of counters.
        /// </summary>
        public readonly int K;
        CsPred<T> __false;
        CsPred<T> __true;

        internal ICounter[] counters;
        public ICounter GetCounter(int i)
        {
            return counters[i];
        }

        public readonly CsConditionSeq TrueCsConditionSeq;
        public readonly CsConditionSeq FalseCsConditionSeq;

        public CsAlgebra(IBooleanAlgebra<T> leafAlgebra, ICounter[] counters)
        {
            this.counters = counters;
            this.LeafAlgebra = leafAlgebra;
            this.NodeAlgebra = new BDDAlgebra<T>(leafAlgebra);
            __false = new CsPred<T>(this, (BDD<T>)NodeAlgebra.False);
            __true = new CsPred<T>(this, (BDD<T>)NodeAlgebra.True);
            this.K = counters.Length;
            TrueCsConditionSeq = CsConditionSeq.MkTrue(counters.Length);
            FalseCsConditionSeq = CsConditionSeq.MkFalse(counters.Length);
        }

        public CsPred<T> False
        {
            get
            {
                return __false;
            }
        }

        public bool IsAtomic
        {
            get
            {
                return false;
            }
        }

        public bool IsExtensional
        {
            get
            {
                return NodeAlgebra.LeafAlgebra.IsExtensional;
            }
        }

        public CsPred<T> True
        {
            get
            {
                return __true;
            }
        }

        public bool AreEquivalent(CsPred<T> pred1, CsPred<T> pred2)
        {
            return NodeAlgebra.AreEquivalent((BDD)pred1.pred, pred2.pred);
        }

        public bool CheckImplication(CsPred<T> lhs, CsPred<T> rhs)
        {
            return NodeAlgebra.CheckImplication((BDD)lhs.pred, rhs.pred);
        }

        public bool EvaluateAtom(CsPred<T> atom, CsPred<T> psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }

        public IEnumerable<Tuple<bool[], CsPred<T>>> GenerateMinterms(params CsPred<T>[] constraints)
        {
            foreach (var minterm in NodeAlgebra.GenerateMinterms((BDD[])Array.ConvertAll(constraints, x => x.pred)))
                yield return new Tuple<bool[], CsPred<T>>(minterm.Item1, new CsPred<T>(this, (BDD<T>)minterm.Item2));
        }

        public CsPred<T> GetAtom(CsPred<T> psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }

        public bool IsSatisfiable(CsPred<T> predicate)
        {
            return NodeAlgebra.IsSatisfiable((BDD)predicate.pred);
        }

        public CsPred<T> MkAnd(params CsPred<T>[] preds)
        {
            if (preds.Length == 0)
                return __true;
            else
            {
                var bdd = preds[0].pred;
                int i = 1;
                while (NodeAlgebra.IsSatisfiable((BDD)bdd) && i < preds.Length)
                    bdd = (BDD<T>)NodeAlgebra.MkAnd((BDD)bdd, preds[i++].pred);
                return new CsPred<T>(this, bdd);
            }
        }

        public CsPred<T> MkAnd(IEnumerable<CsPred<T>> preds)
        {
            var bdd = (BDD<T>)NodeAlgebra.True;
            foreach (var pred in preds)
            {
                bdd = (BDD<T>)NodeAlgebra.MkAnd((BDD)bdd, pred.pred);
                if (!NodeAlgebra.IsSatisfiable((BDD)bdd))
                    return __false;
            }
            return new Automata.CsPred<T>(this, bdd);
        }

        public CsPred<T> MkAnd(CsPred<T> pred1, CsPred<T> pred2)
        {
            var bdd = (BDD<T>)NodeAlgebra.MkAnd((BDD)pred1.pred, (BDD)pred2.pred);
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkDiff(CsPred<T> pred1, CsPred<T> pred2)
        {
            var bdd = (BDD<T>)NodeAlgebra.MkDiff((BDD)pred1.pred, (BDD)pred2.pred);
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkNot(CsPred<T> pred)
        {
            var bdd = (BDD<T>)NodeAlgebra.MkNot((BDD)pred.pred);
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkOr(IEnumerable<CsPred<T>> preds)
        {
            var bdd = (BDD<T>)NodeAlgebra.False;
            foreach (var pred in preds)
            {
                bdd = (BDD<T>)NodeAlgebra.MkOr((BDD)bdd, pred.pred);
                if (bdd.Equals(NodeAlgebra.True))
                    return __true;
            }
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkOr(CsPred<T> pred1, CsPred<T> pred2)
        {
            var bdd = (BDD<T>)NodeAlgebra.MkOr((BDD)pred1.pred, (BDD)pred2.pred);
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkSymmetricDifference(CsPred<T> pred1, CsPred<T> pred2)
        {
            var bdd = (BDD<T>)NodeAlgebra.MkSymmetricDifference((BDD)pred1.pred, pred2.pred);
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> Simplify(CsPred<T> predicate)
        {
            return predicate;
        }

        public string PrettyPrint(CsPred<T> t)
        {
            string s = "";
            foreach (var prod in t.GetSumOfProducts())
            {
                if (s != "")
                    s += ";";
                var pp = NodeAlgebra.Second as IPrettyPrinter<T>;
                string inp = "";
                if (pp != null)
                    inp = pp.PrettyPrint(prod.Item2);
                else
                    inp = prod.Item2.ToString();
                string cc = prod.Item1.ToString();
                s += inp;
                if (cc != "")
                    s += "&" + cc;
            }
            return s;
        }

        public string PrettyPrint(CsPred<T> t, Func<CsPred<T>, string> varLookup)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrintCS(CsPred<T> t, Func<CsPred<T>, string> varLookup)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Make a product between leafpred and vals.
        /// </summary>
        /// <param name="leafpred">input predicate</param>
        /// <param name="vals">this is either a conjunction or disjunction of counter conditions</param>
        /// <returns></returns>
        public CsPred<T> MkPredicate(T leafpred, CsConditionSeq vals)
        {
            return MkPredicate(leafpred, vals.IsAND, vals.ToArray());
        }

        /// <summary>
        /// Make a product between leafpred and vals.
        /// </summary>
        /// <param name="leafpred">input predicate</param>
        /// <param name="vals">sequence of counter conditions</param>
        /// <param name="isAND">if true then treat vals as a conjunction, else as a disjunction</param>
        /// <returns></returns>
        public CsPred<T> MkPredicate(T leafpred, bool isAND, params CsCondition[] vals)
        {
            if (vals.Length != K)
                throw new ArgumentOutOfRangeException("vals", "Incompatible number " + vals.Length + " of conditions, expecting " + K);

            var alg = NodeAlgebra;
            var node = alg.MkLeaf(leafpred);
            BDD<T> vals_bdd = (isAND ? (BDD<T>)alg.True : (BDD<T>)alg.False);
            for (int i = 0; i < K; i++)
            {
                int canexit_bit = (i * 2);
                int canloop_bit = (i * 2) + 1;

                CsCondition cond = vals[i];

                BDD<T> union = (BDD<T>)alg.False;
                if (cond == CsCondition.TRUE)
                {
                    union = (BDD<T>)alg.True;
                }
                else
                {
                    if (cond.HasFlag(CsCondition.LOW))
                        union = (BDD<T>)alg.MkOr(union, alg.MkAnd(node, alg.MkBitFalse(canexit_bit), alg.MkBitTrue(canloop_bit)));
                    if (cond.HasFlag(CsCondition.MIDDLE))
                        union = (BDD<T>)alg.MkOr(union, alg.MkAnd(node, alg.MkBitTrue(canexit_bit), alg.MkBitTrue(canloop_bit)));
                    if (cond.HasFlag(CsCondition.HIGH))
                        union = (BDD<T>)alg.MkOr(union, alg.MkAnd(node, alg.MkBitTrue(canexit_bit), alg.MkBitFalse(canloop_bit)));
                    if (cond.HasFlag(CsCondition.EMPTY))
                        union = (BDD<T>)alg.MkOr(union, alg.MkAnd(node, alg.MkBitFalse(canexit_bit), alg.MkBitFalse(canloop_bit)));
                }

                if (isAND)
                {
                    vals_bdd = (BDD<T>)alg.MkAnd((BDD)vals_bdd, union);
                    if (vals_bdd.Equals(alg.False))
                        break;
                }
                else
                {
                    vals_bdd = (BDD<T>)alg.MkOr((BDD)vals_bdd, union);
                    if (vals_bdd.Equals(alg.True))
                        break;
                }
            }
            node = (BDD<T>)alg.MkAnd((BDD)node, vals_bdd);
            return new CsPred<T>(this, node);
        }
    }
}
