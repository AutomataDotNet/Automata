using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata.BooleanAlgebras
{
    public interface IMonadicPredicate<T1, T2>
    {
        IEnumerable<Tuple<T1, T2>> GetSumOfProducts();
        T2 ProjectSecond();
        ICartesianAlgebra<T1, T2> Algebra { get; }
    }

    public interface ICartesianAlgebra<T1, T2> : IBooleanAlgebra<IMonadicPredicate<T1, T2>>
    {
        IMonadicPredicate<T1, T2> MkCartesianProduct(T1 first, T2 second);
        IBooleanAlgebra<T2> Second { get; }
    }

    public interface ICartesianAlgebraBDD<T> : ICartesianAlgebra<BDD, T>
    {
        IBDDAlgebra BDDAlgebra { get; }
        IMonadicPredicate<BDD, T> Omit(int bit, IMonadicPredicate<BDD, T> pred);
    }

    /// <summary>
    /// Cartesian product algebra of two Boolean algebras.
    /// </summary>
    public class CartesianAlgebra<T, S> : ICartesianAlgebra<T, S>
    {
        internal readonly IBooleanAlgebra<S> nodeAlgebra;
        internal readonly IBooleanAlgebra<T> leafAlgebra;

        public IBooleanAlgebra<S> Second
        {
            get
            {
                return nodeAlgebra;
            }
        }

        public IBooleanAlgebra<T> First
        {
            get
            {
                return leafAlgebra;
            }
        }

        internal Dictionary<Tuple<S, T, BDG<T, S>>, BDG<T, S>> restrictCache =
            new Dictionary<Tuple<S, T, BDG<T, S>>, BDG<T, S>>();
        internal Dictionary<Tuple<T, BDG<T, S>>, BDG<T, S>> restrictLeafCache =
            new Dictionary<Tuple<T, BDG<T, S>>, BDG<T, S>>();
        internal Dictionary<Tuple<S, BDG<T, S>, BDG<T, S>>, BDG<T, S>> MkAndCache =
            new Dictionary<Tuple<S, BDG<T, S>, BDG<T, S>>, BDG<T, S>>();
        internal Dictionary<BDG<T, S>, BDG<T, S>> MkNotCache = new Dictionary<BDG<T, S>, BDG<T, S>>();

        Dictionary<Tuple<S, BDG<T, S>, BDG<T, S>>, BDG<T, S>>
            MkNodeCache = new Dictionary<Tuple<S, BDG<T, S>, BDG<T, S>>, BDG<T, S>>();

        Dictionary<T, BDG<T, S>> MkLeafCache1 = new Dictionary<T, BDG<T, S>>();
        Dictionary<T, BDG<T, S>> MkLeafCache2 = new Dictionary<T, BDG<T, S>>();

        MintermGenerator<IMonadicPredicate<T, S>> mintermGenerator;

        //internal Dictionary<Tuple<Func<T, T>, BDG<T, S>>, BDG<T, S>> TransformLeavesCache =
        //    new Dictionary<Tuple<Func<T, T>, BDG<T, S>>, BDG<T, S>>();

        internal BDG<T, S> _True;
        internal BDG<T, S> _False;

        public CartesianAlgebra(IBooleanAlgebra<T> leafAlg, IBooleanAlgebra<S> nodeAlg)
        {
            this.leafAlgebra = leafAlg;
            this.nodeAlgebra = nodeAlg;
            this._True = new BDG<T, S>(this, default(S), leafAlg.True, null, null);
            this._False = new BDG<T, S>(this, default(S), leafAlg.False, null, null);
            MkLeafCache1[leafAlg.True] = _True;
            MkLeafCache2[leafAlg.True] = _True;
            MkLeafCache1[leafAlg.False] = _False;
            MkLeafCache2[leafAlg.False] = _False;
            MkNotCache[_True] = _False;
            MkNotCache[_False] = _True;
            this.mintermGenerator = new MintermGenerator<IMonadicPredicate<T, S>>(this);
        }

        /// <summary>
        /// Assumes that pred is sat and ~pred is sat
        /// </summary>
        internal BDG<T, S> MkNode(S pred, BDG<T, S> t, BDG<T, S> f)
        {
            if (t == f)
                return t;

            BDG<T, S> val;
            var key = new Tuple<S, BDG<T, S>, BDG<T, S>>(pred, t, f);
            if (!MkNodeCache.TryGetValue(key, out val))
            {
                val = new BDG<T, S>(this, pred, default(T), t, f);
                MkNodeCache[key] = val;
            }
            return val;
        }

        /// <summary>
        /// Creates a leaf with LeafCondition pred.
        /// If simplify=true, checks if pred is unsat (returns False) or valid (returns True).
        /// Assumes that if simplify=false then pred is neither unsat nor valid.
        /// </summary>
        public BDG<T, S> MkLeaf(T pred, bool simplify = false)
        {
            BDG<T, S> val;
            if (simplify)
            {
                if (!MkLeafCache1.TryGetValue(pred, out val))
                {
                    if (!leafAlgebra.IsSatisfiable(pred))
                        val = _False;
                    else if (!leafAlgebra.IsSatisfiable(leafAlgebra.MkNot(pred)))
                        val = _True;
                    else
                        val = new BDG<T, S>(this, default(S), pred, null, null);
                    MkLeafCache1[pred] = val;
                }
            }
            else
            {
                if (!MkLeafCache2.TryGetValue(pred, out val))
                {
                    val = new BDG<T, S>(this, default(S), pred, null, null);
                    MkLeafCache2[pred] = val;
                }
            }
            return val;
        }


        #region IBooleanAlgebra members

        public IEnumerable<Tuple<bool[], IMonadicPredicate<T, S>>> GenerateMinterms(params IMonadicPredicate<T, S>[] constraints)
        {
            return mintermGenerator.GenerateMinterms(constraints);
        }

        public IMonadicPredicate<T, S> True
        {
            get { return _True; }
        }

        public IMonadicPredicate<T, S> False
        {
            get { return _False; }
        }

        public IMonadicPredicate<T, S> MkOr(IEnumerable<IMonadicPredicate<T, S>> predicates)
        {
            var res = True;
            foreach (var pred in predicates)
                res = MkAnd(res, MkNot(pred));

            return MkNot(res);
        }

        public IMonadicPredicate<T, S> MkAnd(IEnumerable<IMonadicPredicate<T, S>> predicates)
        {
            var res = True;
            foreach (var pred in predicates)
                res = MkAnd(res, pred);
            return res;
        }

        public IMonadicPredicate<T, S> MkAnd(params IMonadicPredicate<T, S>[] predicates)
        {
            var res = True;
            foreach (var pred in predicates)
                res = MkAnd(res, pred);
            return res;
        }

        public IMonadicPredicate<T, S> MkNot(IMonadicPredicate<T, S> predicate)
        {
            return ((BDG<T, S>)predicate).MkNot();
        }

        public bool AreEquivalent(IMonadicPredicate<T, S> predicate1, IMonadicPredicate<T, S> predicate2)
        {
            //check if predicate1 does not imply predicate2
            if (IsSatisfiable(MkAnd(predicate1, MkNot(predicate2))))
                return false;

            //check the other direction
            if (IsSatisfiable(MkAnd(predicate2, MkNot(predicate1))))
                return false;

            return true;
        }

        public IMonadicPredicate<T, S> Simplify(IMonadicPredicate<T, S> predicate)
        {
            return predicate; //TBD, not clear what this means here
        }

        public IMonadicPredicate<T, S> MkAnd(IMonadicPredicate<T, S> predicate1, IMonadicPredicate<T, S> predicate2)
        {
            //using Depth as a heuristic
            BDG<T, S> p1 = (BDG<T, S>)predicate1;
            BDG<T, S> p2 = (BDG<T, S>)predicate2;
            if (p1.Depth <= p2.Depth)
                return p2.MkAnd(p1);
            else
                return p1.MkAnd(p2);
        }

        public IMonadicPredicate<T, S> MkOr(IMonadicPredicate<T, S> predicate1, IMonadicPredicate<T, S> predicate2)
        {
            //using DeMorgan
            return MkNot(MkAnd(MkNot(predicate1), MkNot(predicate2)));
        }

        public bool IsSatisfiable(IMonadicPredicate<T, S> predicate)
        {
            return predicate != False; //assuming no unsatisfiable leafs are ever created
        }

        #endregion

        #region ICartesianAlgebra members
        public IMonadicPredicate<T, S> MkSumOfProducts(IEnumerable<Tuple<T, S>> products)
        {
            var conj = _True;
            foreach (var row in products)
                conj = conj.MkAnd(MkNode(row.Item1, row.Item2).MkNot());
            var res = conj.MkNot();
            return res;
        }


        public IMonadicPredicate<T, S> MkCartesianProduct(T first, S second)
        {
            return MkNode(first, second);
        }
        #endregion

        /// <summary>
        /// Creates a phi-node with leaves. 
        /// Represents the set [[t]]x[[s]].
        /// Assumes that s is satisfiable.
        /// If s equals True, then creates MkLeaf(t).
        /// </summary>
        public BDG<T, S> MkNode(T t, S s)
        {
            if (s.Equals(nodeAlgebra.True))
                return MkLeaf(t);
            return MkNode(s, MkLeaf(t), _False);
        }


        public bool IsExtensional
        {
            get { return false; }
        }


        public IMonadicPredicate<T, S> MkSymmetricDifference(IMonadicPredicate<T, S> p1, IMonadicPredicate<T, S> p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool CheckImplication(IMonadicPredicate<T, S> lhs, IMonadicPredicate<T, S> rhs)
        {
            return !IsSatisfiable(MkAnd(lhs, MkNot(rhs)));
        }

        public bool IsAtomic
        {
            get { return nodeAlgebra.IsAtomic && leafAlgebra.IsAtomic; }
        }

        public IMonadicPredicate<T, S> GetAtom(IMonadicPredicate<T, S> psi)
        {
            if (!IsAtomic)
                throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);


            foreach (var tuple in psi.GetSumOfProducts())
            {
                var a2 = nodeAlgebra.GetAtom(tuple.Item2);
                var a1 = leafAlgebra.GetAtom(tuple.Item1);
                var a = MkNode(a1, a2);
                return a;
            }

            return _False;
        }


        public bool EvaluateAtom(IMonadicPredicate<T, S> atom, IMonadicPredicate<T, S> psi)
        {
            throw new NotImplementedException();
        }



        public IMonadicPredicate<T, S> MkDiff(IMonadicPredicate<T, S> predicate1, IMonadicPredicate<T, S> predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }
    }

    public class CartesianAlgebraBDD<T> : CartesianAlgebra<BDD, T>, ICartesianAlgebraBDD<T>
    {
        public CartesianAlgebraBDD(BDDAlgebra bddAlg, IBooleanAlgebra<T> nodeAlg) :
            base(bddAlg, nodeAlg)
        {
        }

        public CartesianAlgebraBDD(IBooleanAlgebra<T> nodeAlg) :
            base(new BDDAlgebra(), nodeAlg)
        {
        }

        public IBDDAlgebra BDDAlgebra
        {
            get { return (BDDAlgebra)leafAlgebra; }
        }


        public IMonadicPredicate<BDD, T> Omit(int bit, IMonadicPredicate<BDD, T> pred)
        {
            return ((BDG<BDD, T>)pred).TransformLeaves(bdd => BDDAlgebra.OmitBit(bdd, bit));
        }
    }

    /// <summary>
    /// Binary Decision Graph. 
    /// Used as a predidate in CartesianAlgebra.
    /// </summary>
    public class BDG<T, S> : IMonadicPredicate<T, S>
    {
        /// <summary>
        /// Underlying Cartesian algebra
        /// </summary>
        public readonly CartesianAlgebra<T, S> algebra;
        /// <summary>
        /// Underlying Cartesian algebra
        /// </summary>
        public ICartesianAlgebra<T, S> Algebra
        {
            get { return algebra; }
        }
        /// <summary>
        /// Branch condition is a predicate of Algebra.NodeAlgebra (if IsLeaf is false else default(S))
        /// </summary>
        public readonly S BranchCondition;
        /// <summary>
        /// Leaf condition is a predicate of Algebra.LeafAlgebra (if IsLeaf is true else default(T))
        /// </summary>
        public readonly T LeafCondition;
        /// <summary>
        /// The case BranchCondition is true (null if IsLeaf is true)
        /// </summary>
        public readonly BDG<T, S> TrueCase;
        /// <summary>
        /// The case BranchCondition is false (null if IsLeaf is true)
        /// </summary>
        public readonly BDG<T, S> FalseCase;
        /// <summary>
        /// Depth is 0 for a leaf and 1 + max(TrueCase.Depth,FalseCase.Depth) for a node
        /// </summary>
        public readonly int Depth;
        /// <summary>
        /// Returns true iff depth is 0
        /// </summary>
        public bool IsLeaf
        {
            get { return Depth == 0; }
        }
        /// <summary>
        /// Returns true if this is Alg.True
        /// </summary>
        public bool IsTrue
        {
            get { return algebra.True == this; }
        }
        /// <summary>
        /// Returns true if this is Alg.False
        /// </summary>
        public bool IsFalse
        {
            get { return algebra.False == this; }
        }
        /// <summary>
        /// No public constructor.
        /// </summary>
        private BDG() { }

        internal BDG(CartesianAlgebra<T, S> alg,
            S nodePred, T leafPred, BDG<T, S> tCase, BDG<T, S> fCase)
        {
            this.algebra = alg;
            this.BranchCondition = nodePred;
            this.LeafCondition = leafPred;
            this.TrueCase = tCase;
            this.FalseCase = fCase;
            this.Depth = (tCase == null ? 0 : Math.Max(tCase.Depth, fCase.Depth) + 1);
        }

        /// <summary>
        /// Makes the conjunction of this and that
        /// </summary>
        public BDG<T, S> MkAnd(BDG<T, S> that)
        {
            return MkAnd(algebra.nodeAlgebra.True, that);
        }

        /// <summary>
        /// Maintains the node invariant: sat(path &amp; nodePred) and sat(path &amp; ~nodePred)
        /// </summary>
        BDG<T, S> MkAnd(S path, BDG<T, S> that)
        {
            var key = new Tuple<S, BDG<T, S>, BDG<T, S>>(path, this, that);
            BDG<T, S> val;
            if (!algebra.MkAndCache.TryGetValue(key, out val))
            {
                if (this.IsLeaf)
                    if (path.Equals(algebra.nodeAlgebra.True))
                        val = that.RestrictLeaves(this.LeafCondition);
                    else
                        val = that.Restrict(path, this.LeafCondition);
                else if (that.IsLeaf)
                    val = this.RestrictLeaves(that.LeafCondition); //path is not relevant
                else
                {
                    var path_and_thatCond = algebra.nodeAlgebra.MkAnd(path, that.BranchCondition);
                    if (!algebra.nodeAlgebra.IsSatisfiable(path_and_thatCond))
                    {
                        //path implies ~that.BranchCondition
                        var t = this.TrueCase.MkAnd(algebra.nodeAlgebra.MkAnd(path, this.BranchCondition), that.FalseCase);
                        var f = this.FalseCase.MkAnd(algebra.nodeAlgebra.MkAnd(path, algebra.nodeAlgebra.MkNot(this.BranchCondition)), that.FalseCase);
                        if (t == this.TrueCase && f == this.FalseCase)
                            val = this;
                        else
                            val = this.algebra.MkNode(this.BranchCondition, t, f);
                    }
                    else
                    {
                        var path_and_not_thatCond = algebra.nodeAlgebra.MkAnd(path, algebra.nodeAlgebra.MkNot(that.BranchCondition));
                        if (!algebra.nodeAlgebra.IsSatisfiable(path_and_not_thatCond))
                        {
                            //path implies that.BranchCondition
                            var t = this.TrueCase.MkAnd(algebra.nodeAlgebra.MkAnd(path, this.BranchCondition), that.TrueCase);
                            var f = this.FalseCase.MkAnd(algebra.nodeAlgebra.MkAnd(path, algebra.nodeAlgebra.MkNot(this.BranchCondition)), that.TrueCase);
                            if (t == this.TrueCase && f == this.FalseCase)
                                val = this;
                            else
                                val = this.algebra.MkNode(this.BranchCondition, t, f);
                        }
                        else
                        {  //both cases are possible
                            var t = this.TrueCase.MkAnd(algebra.nodeAlgebra.MkAnd(path, this.BranchCondition), that);
                            var f = this.FalseCase.MkAnd(algebra.nodeAlgebra.MkAnd(path, algebra.nodeAlgebra.MkNot(this.BranchCondition)), that);
                            if (t == this.TrueCase && f == this.FalseCase)
                                val = this;
                            else
                                val = this.algebra.MkNode(this.BranchCondition, t, f);
                        }
                    }
                }
                algebra.MkAndCache[key] = val;
            }
            return val;
        }

        /// <summary>
        /// Negates the predicate by negating the leaves
        /// </summary>
        /// <returns></returns>
        public BDG<T, S> MkNot()
        {
            BDG<T, S> val;
            if (!algebra.MkNotCache.TryGetValue(this, out val))
            {
                if (this.IsLeaf)
                {
                    //if (this == Algebra.False)
                    //    val = Algebra._True;
                    //else if (this == Algebra.True)
                    //    val = Algebra._False;
                    //else
                    val = algebra.MkLeaf(algebra.leafAlgebra.MkNot(this.LeafCondition));
                }
                else
                {
                    var tNot = this.TrueCase.MkNot();
                    var fNot = this.FalseCase.MkNot();
                    val = algebra.MkNode(this.BranchCondition, tNot, fNot);
                }
                algebra.MkNotCache[this] = val;
            }
            return val;
        }

        /// <summary>
        /// Restrict the leaf predicates with psi
        /// </summary>
        BDG<T, S> RestrictLeaves(T psi)
        {
            var key = new Tuple<T, BDG<T, S>>(psi, this);
            BDG<T, S> val;
            if (!algebra.restrictLeafCache.TryGetValue(key, out val))
            {
                if (this.IsLeaf)
                    val = this.algebra.MkLeaf(this.algebra.leafAlgebra.MkAnd(this.LeafCondition, psi), true);
                else
                {
                    var t = TrueCase.RestrictLeaves(psi);
                    var f = FalseCase.RestrictLeaves(psi);
                    if (t == TrueCase && f == FalseCase)
                        val = this;
                    else
                        val = this.algebra.MkNode(this.BranchCondition, t, f);
                }
                algebra.restrictLeafCache[key] = val;
            }
            return val;
        }

        /// <summary>
        /// Restrict top down wrt the path condition and strengthen the leaf predicates with psi
        /// </summary>
        BDG<T, S> Restrict(S path, T psi)
        {
            var key = new Tuple<S, T, BDG<T, S>>(path, psi, this);
            BDG<T, S> val;
            if (!algebra.restrictCache.TryGetValue(key, out val))
            {
                if (this.IsLeaf)
                    val = this.algebra.MkLeaf(this.algebra.leafAlgebra.MkAnd(this.LeafCondition, psi), true);
                else
                {
                    #region restrict the children
                    var path_and_not_nodePred = algebra.nodeAlgebra.MkAnd(path, algebra.nodeAlgebra.MkNot(BranchCondition));
                    if (algebra.nodeAlgebra.IsSatisfiable(path_and_not_nodePred))
                    {
                        var f = this.FalseCase.Restrict(path_and_not_nodePred, psi);
                        var path_and_nodePred = algebra.nodeAlgebra.MkAnd(path, BranchCondition);
                        if (algebra.nodeAlgebra.IsSatisfiable(path_and_nodePred))
                        {
                            var t = this.TrueCase.Restrict(path_and_nodePred, psi);
                            if (f == this.FalseCase && t == this.TrueCase)
                                val = this; //nothing changed
                            else
                                val = this.algebra.MkNode(BranchCondition, t, f);
                        }
                        else //path implies not(nodePred)
                            val = f;
                    }
                    else //path implies nodePred 
                        val = TrueCase.Restrict(path, psi);
                    #endregion
                }
                algebra.restrictCache[key] = val;
            }
            return val;
        }

        /// <summary>
        /// Compute all pairs (s_1,t_1),...,(s_k,t_k) such that 
        /// the BDG represents ([[s_1]]x[[t_1]]) U ... U ([[s_k]]x[[t_k]])
        /// </summary>
        public IEnumerable<Tuple<T, S>> GetSumOfProducts()
        {
            foreach (var kv in GetRowsDictionary())
                if (!(this.algebra.First.False.Equals(kv.Key)))
                    yield return new Tuple<T, S>(kv.Key, kv.Value);
        }
        public Dictionary<T, S> GetRowsDictionary()
        {
            return GetCases1(new Dictionary<BDG<T, S>, Dictionary<T, S>>());
        }

        private Dictionary<T, S> GetCases1(Dictionary<BDG<T, S>, Dictionary<T, S>> done)
        {
            Dictionary<T, S> d;
            if (!done.TryGetValue(this, out d))
            {
                if (this.IsLeaf)
                {
                    d = new Dictionary<T, S>();
                    d[this.LeafCondition] = algebra.nodeAlgebra.True;
                }
                else
                {
                    var t = this.TrueCase.GetCases1(done);
                    var f = this.FalseCase.GetCases1(done);
                    d = new Dictionary<T, S>();
                    foreach (var kv in t)
                    {
                        d[kv.Key] = (kv.Value.Equals(algebra.nodeAlgebra.True) ? this.BranchCondition : algebra.nodeAlgebra.MkAnd(kv.Value, this.BranchCondition));
                    }
                    foreach (var kv in f)
                    {
                        S psi2 = (kv.Value.Equals(algebra.nodeAlgebra.True) ? algebra.nodeAlgebra.MkNot(this.BranchCondition) :
                            algebra.nodeAlgebra.MkAnd(kv.Value, algebra.nodeAlgebra.MkNot(this.BranchCondition)));
                        S psi;
                        if (d.TryGetValue(kv.Key, out psi))
                            d[kv.Key] = algebra.nodeAlgebra.MkOr(psi, psi2);
                        else
                            d[kv.Key] = psi2;
                    }
                }
                done[this] = d;
            }
            return d;
        }

        /// <summary>
        /// Computes (number of nonterminals, number of terminals) in the underlying directed acyclic graph.
        /// </summary>
        public Tuple<int, int> GetSize()
        {
            int nrNodes = 0;
            int nrLeaves = 0;
            HashSet<BDG<T, S>> done = new HashSet<BDG<T, S>>();
            SimpleStack<BDG<T, S>> stack = new SimpleStack<BDG<T, S>>();
            stack.Push(this);
            done.Add(this);
            while (stack.IsNonempty)
            {
                var bdg = stack.Pop();
                if (bdg.IsLeaf)
                    nrLeaves += 1;
                else
                {
                    if (done.Add(bdg.TrueCase))
                        stack.Push(bdg.TrueCase);
                    if (done.Add(bdg.FalseCase))
                        stack.Push(bdg.FalseCase);
                    nrNodes += 1;
                }
            }
            return new Tuple<int, int>(nrNodes, nrLeaves);
        }

        public override string ToString()
        {
            if (this.IsLeaf)
            {
                IPrettyPrinter<T> printer = this.algebra.leafAlgebra as IPrettyPrinter<T>;
                if (printer != null)
                    return printer.PrettyPrint(this.LeafCondition);
                else
                    return this.LeafCondition.ToString();
            }
            else
            {
                IPrettyPrinter<S> printer = this.algebra.nodeAlgebra as IPrettyPrinter<S>;
                if (printer != null)
                    return "ITE(" + printer.PrettyPrint(this.BranchCondition) + "," + TrueCase.ToString() + "," + FalseCase.ToString() + ")";
                else
                    return "ITE(" + this.BranchCondition.ToString() + "," + TrueCase.ToString() + "," + FalseCase.ToString() + ")";
            }
        }

        /// <summary>
        /// Apply the transformation f to all leaves.
        /// </summary>
        internal BDG<T, S> TransformLeaves(Func<T, T> func)
        {
            BDG<T, S> val = null;
            var key = new Tuple<Func<T, T>, BDG<T, S>>(func, this);
            //if (!Algebra.TransformLeavesCache.TryGetValue(key, out val))
            //{
            if (this.IsLeaf)
            {
                var newPred = func(LeafCondition);
                val = algebra.MkLeaf(newPred, true);
            }
            else
            {
                var t = this.TrueCase.TransformLeaves(func);
                var f = this.FalseCase.TransformLeaves(func);
                val = algebra.MkNode(this.BranchCondition, t, f);
            }
            //Algebra.TransformLeavesCache[key] = val;
            //}
            return val;
        }


        public T ProjectFirst()
        {
            T res = algebra.First.False;
            foreach (var p in GetSumOfProducts())
                res = algebra.First.MkOr(res, p.Item1);
            return res;
        }

        public S ProjectSecond()
        {
            S res = algebra.nodeAlgebra.False;
            foreach (var p in GetSumOfProducts())
                if (!p.Item1.Equals(algebra.First.False))
                {
                    if (res.Equals(algebra.leafAlgebra.False))
                        res = p.Item2;
                    else
                        res = algebra.nodeAlgebra.MkOr(res, p.Item2);
                }
            return res;
        }

        internal IEnumerable<Tuple<S, T>> EnumerateBranches()
        {
            throw new NotImplementedException();
        }
    }
}
