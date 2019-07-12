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
        /// Generate the conjuncts of the predicate in DNF form
        /// </summary>
        public IEnumerable<Tuple<CsConditionSeq, T>> GetSumOfProducts()
        {
            var list = new List<Tuple<CsConditionSeq, T>>(EnumerateCases(pred));
            return list;
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

        IEnumerable<Tuple<CsConditionSeq, T>> EnumerateCases(BDD<T> bdd)
        {
            if (bdd.IsLeaf)
            {
                var val = ((BDD<T>)bdd).Leaf;
                if (bdd.Algebra.Second.IsSatisfiable(val))
                    //note that CsConditionSeq.False means that all counters are disabled
                    yield return new Tuple<CsConditionSeq, T>(alg.__emptyCsConditionSeq, ((BDD<T>)bdd).Leaf);
            }
            else
            {
                //even bit is about exit condition
                bool is_canexit_bit = (bdd.Ordinal % 2 == 0);
                //counter id
                int i = bdd.Ordinal / 2;
                foreach (var path in EnumerateCases((BDD<T>)bdd.Zero))
                {
                    #region Zero branch: means that the bit is 0
                    var v = path.Item1[i];
                    if (is_canexit_bit)
                    {
                        if (v == CsCondition.EMPTY)
                            v = CsCondition.LOW; //cannot exit
                        else
                            v = v & CsCondition.LOW;
                    }
                    else
                    {
                        if (v == CsCondition.EMPTY)
                            v = CsCondition.HIGH; //cannot increment
                        else
                            v = v & CsCondition.HIGH;
                    }
                    if (v != CsCondition.EMPTY && v != CsCondition.FALSE)
                        yield return new Tuple<CsConditionSeq, T>(path.Item1.Update(i, v), path.Item2);
                    #endregion
                }
                foreach (var path in EnumerateCases((BDD<T>)bdd.One))
                {
                    #region One branch: means that the bit is 1
                    var v = path.Item1[i];
                    if (is_canexit_bit)
                    {
                        if (v == CsCondition.EMPTY)
                            v = CsCondition.CANEXIT;
                        else
                            v = v & CsCondition.CANEXIT;
                    }
                    else
                    {
                        if (v == CsCondition.EMPTY)
                            v = CsCondition.CANLOOP;
                        else
                            v = v & CsCondition.CANLOOP;
                    }
                    if (v != CsCondition.EMPTY && v != CsCondition.FALSE)
                        yield return new Tuple<CsConditionSeq, T>(path.Item1.Update(i, v), path.Item2);
                    #endregion
                }
            }
        }

        public override string ToString()
        {
            return pred.ToString();
        }
    }

    /// <summary>
    /// Boolean algebra for CsPred predicates
    /// </summary>
    /// <typeparam name="T">input predicate type</typeparam>
    public class CsAlgebra<T> : IBooleanAlgebra<CsPred<T>>, IPrettyPrinter<CsPred<T>>
    {
        internal BDDAlgebra<T> algebra;
        internal int K;
        CsPred<T> __false;
        CsPred<T> __true;

        internal CsConditionSeq __emptyCsConditionSeq;

        public CsAlgebra(IBooleanAlgebra<T> leafAlgebra, int nrOfCountingSets)
        {
            this.algebra = new BDDAlgebra<T>(leafAlgebra);
            __false = new CsPred<T>(this, (BDD<T>)algebra.False);
            __true = new CsPred<T>(this, (BDD<T>)algebra.True);
            this.K = nrOfCountingSets;
            __emptyCsConditionSeq = CsConditionSeq.MkEmpty(nrOfCountingSets);
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
                return algebra.LeafAlgebra.IsExtensional;
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
            return algebra.AreEquivalent((BDD)pred1.pred, pred2.pred);
        }

        public bool CheckImplication(CsPred<T> lhs, CsPred<T> rhs)
        {
            return algebra.CheckImplication((BDD)lhs.pred, rhs.pred);
        }

        public bool EvaluateAtom(CsPred<T> atom, CsPred<T> psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }

        public IEnumerable<Tuple<bool[], CsPred<T>>> GenerateMinterms(params CsPred<T>[] constraints)
        {
            foreach (var minterm in algebra.GenerateMinterms((BDD[])Array.ConvertAll(constraints, x => x.pred)))
                yield return new Tuple<bool[], CsPred<T>>(minterm.Item1, new CsPred<T>(this, (BDD<T>)minterm.Item2));
        }

        public CsPred<T> GetAtom(CsPred<T> psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }

        public bool IsSatisfiable(CsPred<T> predicate)
        {
            return algebra.IsSatisfiable((BDD)predicate.pred);
        }

        public CsPred<T> MkAnd(params CsPred<T>[] preds)
        {
            if (preds.Length == 0)
                return __true;
            else
            {
                var bdd = preds[0].pred;
                int i = 1;
                while (algebra.IsSatisfiable((BDD)bdd) && i < preds.Length)
                    bdd = (BDD<T>)algebra.MkAnd((BDD)bdd, preds[i++].pred);
                return new CsPred<T>(this, bdd);
            }
        }

        public CsPred<T> MkAnd(IEnumerable<CsPred<T>> preds)
        {
            var bdd = (BDD<T>)algebra.True;
            foreach (var pred in preds)
            {
                bdd = (BDD<T>)algebra.MkAnd((BDD)bdd, pred.pred);
                if (!algebra.IsSatisfiable((BDD)bdd))
                    return __false;
            }
            return new Automata.CsPred<T>(this, bdd);
        }

        public CsPred<T> MkAnd(CsPred<T> pred1, CsPred<T> pred2)
        {
            var bdd = (BDD<T>)algebra.MkAnd((BDD)pred1.pred, (BDD)pred2.pred);
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkDiff(CsPred<T> pred1, CsPred<T> pred2)
        {
            var bdd = (BDD<T>)algebra.MkDiff((BDD)pred1.pred, (BDD)pred2.pred);
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkNot(CsPred<T> pred)
        {
            var bdd = (BDD<T>)algebra.MkNot((BDD)pred.pred);
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkOr(IEnumerable<CsPred<T>> preds)
        {
            var bdd = (BDD<T>)algebra.False;
            foreach (var pred in preds)
            {
                bdd = (BDD<T>)algebra.MkOr((BDD)bdd, pred.pred);
                if (bdd.Equals(algebra.True))
                    return __true;
            }
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkOr(CsPred<T> pred1, CsPred<T> pred2)
        {
            var bdd = (BDD<T>)algebra.MkOr((BDD)pred1.pred, (BDD)pred2.pred);
            return new CsPred<T>(this, bdd);
        }

        public CsPred<T> MkPredicate(T leafpred, CsConditionSeq vals)
        {
            return MkPredicate(leafpred, vals.ToArray());
        }

        public CsPred<T> MkPredicate(T leafpred, params CsCondition[] vals)
        {
            var alg = algebra;
            var node = alg.MkLeaf(leafpred);
            for (int i = 0; i < K; i++)
            {
                int canexit_bit = (i * 2);
                int canloop_bit = (i * 2) + 1;
                if (vals[i] != CsCondition.EMPTY)
                {
                    #region set the bit combinations in the node
                    //exclude 00
                    var canexitorloop = alg.MkOr(alg.MkBitTrue(canexit_bit), alg.MkBitTrue(canloop_bit));
                    node = (BDD<T>)alg.MkAnd(node, canexitorloop);

                    switch (vals[i])
                    {
                        case CsCondition.LOW:
                            {
                                //can increment but not exit
                                node = (BDD<T>)alg.MkAnd(node, alg.MkBitFalse(canexit_bit), alg.MkBitTrue(canloop_bit));
                                break;
                            }
                        case CsCondition.MIDDLE:
                            {
                                //can both increment and exit
                                node = (BDD<T>)alg.MkAnd(node, alg.MkBitTrue(canexit_bit), alg.MkBitTrue(canloop_bit));
                                break;
                            }
                        case CsCondition.HIGH:
                            {
                                //can exit but not increment
                                node = (BDD<T>)alg.MkAnd(node, alg.MkBitTrue(canexit_bit), alg.MkBitFalse(canloop_bit));
                                break;
                            }
                        case CsCondition.CANLOOP:
                            {
                                node = (BDD<T>)alg.MkAnd(node, alg.MkBitTrue(canloop_bit));
                                break;
                            }
                        case CsCondition.CANEXIT:
                            {
                                node = (BDD<T>)alg.MkAnd(node, alg.MkBitTrue(canexit_bit));
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                    #endregion
                }
            }
            return new CsPred<T>(this, node);
        }

        public CsPred<T> MkSymmetricDifference(CsPred<T> pred1, CsPred<T> pred2)
        {
            var bdd = (BDD<T>)algebra.MkSymmetricDifference((BDD)pred1.pred, pred2.pred);
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
                var pp = algebra.Second as IPrettyPrinter<T>;
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
    }
}
