using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Automata.BooleanAlgebras;

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
            Microsoft.Automata.DirectedGraphs.DotWriter.CharSetToDot(this, file, fname, DirectedGraphs.DotWriter.RANKDIR.TB, 12);
        }

        protected virtual string PrintLeaf()
        {
            if (this.IsEmpty)
                return "false";
            else if (this.IsFull)
                return "true";
            else
                throw new AutomataException(AutomataExceptionKind.UnexpectedMTBDDTerminal);
        }

        public override string ToString()
        {
            if (IsLeaf)
            {
                return PrintLeaf();
            }
            else
            {

                var sb = new System.Text.StringBuilder();
                sb.Append("{");
                foreach (var s in EnumCases())
                {
                    if (sb.Length > 1)
                        sb.Append("|");
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
                yield return "(" + PrintLeaf() + ")";
            else
            {
                int m1 = (One.IsLeaf ? Ordinal : (Ordinal - One.Ordinal) - 1);
                int m0 = (Zero.IsLeaf ? Ordinal : (Ordinal - Zero.Ordinal) - 1);
                foreach (var s in One.EnumCases())
                    yield return s + MkAny(m1) + "1";
                foreach (var s in Zero.EnumCases())
                    yield return s + MkAny(m0) + "0";
            }
        }

        static string MkAny(int k)
        {
            if (k <= 0)
                return "";
            switch (k)
            {
                case 1:
                    return "-";
                case 2:
                    return "--";
                default:
                    var stars = new char[k];
                    for (int i = 0; i < k; i++)
                        stars[i] = '-';
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

        public BDD Or(BDD other)
        {
            return algebra.MkOr(this,other);
        }

        public BDD And(BDD other)
        {
            return algebra.MkAnd(this, other);
        }

        public BDD Not()
        {
            return algebra.MkNot(this);
        }

        public BDD Diff(BDD other)
        {
            return algebra.MkDiff(this, other);
        }

        public Tuple<uint,uint>[] ToRanges(int bound = 0)
        {
            CharSetSolver solver = algebra as CharSetSolver;
            if (solver == null)
                throw new AutomataException(AutomataExceptionKind.AlgebraMustBeCharSetSolver);
            return solver.ToRanges(this, bound);
        }

        /// <summary>
        /// Decrement the ordinals of all nodes by k, k must be nonnegative.
        /// </summary>
        /// <param name="k">offset</param>
        public BDD ShiftRight(int k = 1)
        {
            return algebra.ShiftRight(this, k);
        }

        /// <summary>
        /// Increment the ordinals of all nodes by k, k must be nonnegative.
        /// </summary>
        /// <param name="k">offset</param>
        public BDD ShiftLeft(int k = 1)
        {
            return algebra.ShiftLeft(this, k);
        }
    }

    public class BDD<T> : BDD, IMonadicPredicate<BDD,T>
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

        public IEnumerable<Tuple<BDD, T>> GetSumOfProducts()
        {
            throw new NotImplementedException();
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

        protected override string PrintLeaf()
        {
            var alg = (BDDAlgebra<T>)Algebra;
            var pp = alg.LeafAlgebra as IPrettyPrinter<T>;
            if (pp != null)
                return pp.PrettyPrint(Leaf);
            else
                return Leaf.ToString();
        }
    }
}

