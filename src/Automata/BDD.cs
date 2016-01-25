using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents a Binary Decision Diagram.
    /// </summary>
    public class BDD
    {
        /// <summary>
        /// The encoding of the set for lower ordinals for the case when the current bit is 1.
        /// The value is null iff IsLeaf is true.
        /// </summary>
        public readonly BDD One;

        /// <summary>
        /// The encoding of the set for lower ordinals for the case when the current bit is 0.
        /// The value is null iff IsLeaf is true.
        /// </summary>
        public readonly BDD Zero;


        protected IBDDAlgebra algebra;

        /// <summary>
        /// Ordinal of this bit if nonleaf
        /// </summary>
        public readonly int Ordinal;

        private BDD() { }

        internal BDD(IBDDAlgebra algebra, int ordinal, BDD one, BDD zero)
        {
            this.One = one;
            this.Zero = zero;
            this.Ordinal = ordinal;
            this.algebra = algebra;
        }

        internal BDD(IBDDAlgebra algebra, int ordinal)
        {
            this.One = null;
            this.Zero = null;
            this.Ordinal = ordinal;
            this.algebra = algebra;
        }

        /// <summary>
        /// True iff the node is a terminal (One and Zero are null).
        /// </summary>
        public bool IsLeaf
        {
            get { return One == null; }
        }

        /// <summary>
        /// True iff the set is full.
        /// </summary>
        public bool IsFull
        {
            get { return this == algebra.True; }
        }

        /// <summary>
        /// True iff the set is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return this == algebra.False; }
        }

        /// <summary>
        /// Counts the number of nodes (both terminals and nonterminals) in the BDD.
        /// </summary>
        public int CountNodes()
        {
            if (IsLeaf)
                return 1;

            HashSet<BDD> visited = new HashSet<BDD>();
            SimpleStack<BDD> stack = new SimpleStack<BDD>();
            stack.Push(this);
            visited.Add(this);
            while (stack.IsNonempty)
            {
                BDD a = stack.Pop();
                if (!a.IsLeaf)
                {
                    if (visited.Add(a.One))
                        stack.Push(a.One);
                    if (visited.Add(a.Zero))
                        stack.Push(a.Zero);
                }
            }
            return visited.Count;
        }

        /// <summary>
        /// Store the BDD as a graph in the given file.
        /// </summary>
        public void ToDot(string file)
        {
            string fname = (file.EndsWith(".dot") ? file : file + ".dot");
            Microsoft.Automata.Internal.DirectedGraphs.DotWriter.CharSetToDot(this, file, fname, Internal.DirectedGraphs.DotWriter.RANKDIR.TB, 12);
        }

        public override string ToString()
        {
            if (IsLeaf)
            {
                return algebra.PrettyPrintTerminal(Ordinal);
            }
            else
            {

                var sb = new System.Text.StringBuilder();
                sb.Append("{");
                foreach (var s in EnumCases())
                {
                    if (sb.Length > 1)
                        sb.AppendLine("|");
                    if (sb.Length > 1000)
                    {
                        sb.Append("...");
                        break;
                    }
                    sb.Append(s);
                }
                sb.Append("}");
                return sb.ToString();
            }
        }

        IEnumerable<string> EnumCases()
        {
            if (IsFull)
                yield return "";
            else if (IsEmpty)
                yield break;
            else if (IsLeaf)
                yield return "(" + algebra.PrettyPrintTerminal(Ordinal) + ")";
            else
            {
                foreach (var s in One.EnumCases())
                    yield return "1" + MkStars(Ordinal - One.Ordinal - 1) + s;
                foreach (var s in Zero.EnumCases())
                    yield return "0" + MkStars(Ordinal - Zero.Ordinal - 1) + s;
            }
        }

        static string MkStars(int k)
        {
            if (k <= 0)
                return "";
            switch (k)
            {
                case 1:
                    return "*";
                case 2:
                    return "**";
                default:
                    var stars = new char[k];
                    for (int i = 0; i < k; i++)
                        stars[i] = '*';
                    return new String(stars);
            }
        }

        /// <summary>
        /// Gets the lexicographically minimum bitvector in this BDD as a ulong.
        /// Assumes that this BDD is nonempty and that its ordinal is at most 63.
        /// </summary>
        public ulong GetMin()
        {
            var set = this;

            if (set.IsFull)
                return (ulong)0;

            if (set.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.SetIsEmpty);

            if (set.Ordinal > 63)
                throw new AutomataException(AutomataExceptionKind.OrdinalIsTooLarge);

            ulong res = 0;

            while (!set.IsLeaf)
            {
                if (set.Zero.IsEmpty) //the bit must be set to 1
                {
                    res = res | ((ulong)1 << set.Ordinal);
                    set = set.One;
                }
                else
                    set = set.Zero;
            }

            return res;
        }
    }

    public class BDD<T> : BDD, ISumOfProducts<BDD,T>
    {
        public readonly T Leaf;
        internal BDD(BDDAlgebra<T> algebra, int ordinal, T leaf)
            : base(algebra, ordinal)
        {
            this.Leaf = leaf;
        }

        internal BDD(BDDAlgebra<T> algebra, int ordinal, BDD one, BDD zero)
            : base(algebra, ordinal, one, zero)
        {
            this.Leaf = default(T);
        }

        public IEnumerable<Tuple<BDD, T>> EnumerateProducts()
        {
            throw new NotImplementedException();
        }

        public ISumOfProducts<BDD, T> TransformFirst(Func<BDD, BDD> f)
        {
            return (BDD<T>)f(this);
        }

        public T ProjectSecond()
        {
            BDDAlgebra<T> alg = (BDDAlgebra<T>)algebra;
            return alg.ProjectLeaves(this);
        }

        public ICartesianAlgebra<BDD, T> Algebra
        {
            get { return (BDDAlgebra<T>)this.algebra; }
        }
    }

    /*
    /// <summary>
    /// Represents a bounded set of bit-vectors in form of a Binary Decision Diagram and a fixed depth bound.
    /// </summary>
    public class BBDD
    {
        internal BBDDAlgebra algebra;
        internal Tuple<int, BDD> pair;

        internal BBDD(BBDDAlgebra alg, int depth, BDD bdd)
        {
            this.algebra = alg;
            this.pair = new Tuple<int, BDD>(depth, bdd);
        }

        public override bool Equals(object obj)
        {
            var that = obj as BBDD;
            if (that == null)
                return false;
            else
                return this.pair.Equals(that.pair);
        }

        public override int GetHashCode()
        {
            return pair.GetHashCode();
        }

        /// <summary>
        /// The number of variables in the BDD.
        /// </summary>
        public int Depth
        {
            get { return pair.Item1; }
        }

        /// <summary>
        /// Number of nodes in the underlying BDD
        /// </summary>
        public int Size
        {
            get { return pair.Item2.CountNodes(); }
        }
    }

    public class BBDDAlgebra : IBoolAlgMinterm<BBDD>
    {
        BDDAlgebra bdda;
        MintermGenerator<BBDD> mtg;
        BBDD _True;
        BBDD _False;

        public BBDDAlgebra()
        {
            bdda = new BDDAlgebra();
            mtg = new MintermGenerator<BBDD>(this);
            _True = new BBDD(this, 0, bdda.True);
            _False = new BBDD(this, 0, bdda.False);
        }

        public IEnumerable<Pair<bool[], BBDD>> GenerateMinterms(params BBDD[] constraints)
        {
            return mtg.GenerateMinterms(constraints);
        }

        /// <summary>
        /// True BDD with no variables. Has depth 0. Denotes a singleton set containing an empty tuple.
        /// </summary>
        public BBDD True
        {
            get { return _True; }
        }

        /// <summary>
        /// False BDD with no variables. Has depth 0. Denotes an empty set.
        /// </summary>
        public BBDD False
        {
            get { return _False; }
        }

        public BBDD MkOr(IEnumerable<BBDD> predicates)
        {
            bool isempty = true;
            BBDD res = _False;
            foreach (var pred in predicates)
            {
                if (isempty)
                {
                    res = pred;
                    isempty = false;
                }
                else
                {
                    res = MkOr(res, pred);
                }
            }
            return res;
        }

        public BBDD MkAnd(IEnumerable<BBDD> predicates)
        {
            bool isempty = true;
            BBDD res = _True;
            foreach (var pred in predicates)
            {
                if (isempty)
                {
                    res = pred;
                    isempty = false;
                }
                else
                {
                    res = MkAnd(res, pred);
                }
            }
            return res;
        }

        public BBDD MkAnd(params BBDD[] predicates)
        {
            if (predicates.Length == 0)
                return _True;

            var res = predicates[0];
            for (int i = 1; i < predicates.Length; i++)
                res = MkAnd(res, predicates[i]);

            return res;
        }

        public BBDD MkNot(BBDD predicate)
        {
            if (predicate.algebra != this)
                throw new AutomataException(AutomataExceptionKind.IncompatibleAlgebras);

            var bdd = predicate.algebra.bdda.MkNot(predicate.pair.Item2);
            var res = new BBDD(this, predicate.pair.Item1, bdd);
            return res;
        }

        public bool AreEquivalent(BBDD predicate1, BBDD predicate2)
        {
            if (predicate1.algebra != this || predicate2.algebra != this)
                throw new AutomataException(AutomataExceptionKind.IncompatibleAlgebras);

            if (predicate1.pair.Item1 != predicate2.pair.Item1)
                throw new AutomataException(AutomataExceptionKind.IncompatibleBounds);

            var res = predicate1.algebra.bdda.AreEquivalent(predicate1.pair.Item2, predicate2.pair.Item2);
            return res;
        }

        public BBDD Simplify(BBDD predicate)
        {
            return predicate;
        }

        public BBDD MkAnd(BBDD predicate1, BBDD predicate2)
        {
            if (predicate1.algebra != this || predicate2.algebra != this)
                throw new AutomataException(AutomataExceptionKind.IncompatibleAlgebras);

            if (predicate1.pair.Item1 != predicate2.pair.Item1)
                throw new AutomataException(AutomataExceptionKind.IncompatibleBounds);


            var bdd = predicate1.algebra.bdda.MkAnd(predicate1.pair.Item2, predicate2.pair.Item2);
            var res = new BBDD(this, predicate1.pair.Item1, bdd);
            return res;
        }

        public BBDD MkOr(BBDD predicate1, BBDD predicate2)
        {
            if (predicate1.algebra != this || predicate2.algebra != this)
                throw new AutomataException(AutomataExceptionKind.IncompatibleAlgebras);

            if (predicate1.pair.Item1 != predicate2.pair.Item1)
                throw new AutomataException(AutomataExceptionKind.IncompatibleBounds);


            var bdd = predicate1.algebra.bdda.MkOr(predicate1.pair.Item2, predicate2.pair.Item2);
            var res = new BBDD(this, predicate1.pair.Item1, bdd);
            return res;
        }

        public bool IsSatisfiable(BBDD predicate)
        {
            return !predicate.pair.Item2.IsEmpty;
        }

        public BBDD MkSetWithBitTrue(int k, int depth)
        {
            if (k < 0 || k >= depth)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);
            var bdd = bdda.MkSetWithBitTrue(k);
            var res = new BBDD(this, depth, bdd);
            return res;
        }

        public BBDD MkSetWithBitFalse(int k, int depth)
        {
            if (k < 0 || k >= depth)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);
            var bdd = bdda.MkSetWithBitFalse(k);
            var res = new BBDD(this, depth, bdd);
            return res;
        }

        public BBDD MkTrue(int depth)
        {
            if (depth < 0)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);
            if (depth == 0)
                return _True;
            else
                return new BBDD(this, depth, bdda.True);
        }

        public BBDD RemoveMaxBit(BBDD predicate)
        {
            if (predicate.pair.Item1 == 0)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            if (predicate.pair.Item2.IsTrivial)
                return new BBDD(this, predicate.pair.Item1 - 1, predicate.pair.Item2);
            else
            {
                var bdd0 = predicate.pair.Item2.Zero;
                var bdd1 = predicate.pair.Item2.One; 
                var bdd = bdda.MkOr(bdd0, bdd1);
                return new BBDD(this, predicate.pair.Item1 - 1, bdd);
            }
        }
    }*/
}

