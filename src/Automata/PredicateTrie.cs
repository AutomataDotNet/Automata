using System;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    /// <summary>
    /// For a given atomic Boolean algebra uses a trie of atoms to map all predicates to unique equivalent representatives.
    /// </summary>
    public class PredicateTrie<T>
    {
        Dictionary<T, T> idCache = new Dictionary<T, T>();
        IBooleanAlgebra<T> algebra;
        List<T> atoms = new List<T>();
        TrieTree tree = null;
        int count = 0;

        /// <summary>
        /// Gets the depth of the trie, that is the number of atoms.
        /// </summary>
        public int Depth
        {
            get
            {
                return atoms.Count;
            }
        }

        /// <summary>
        /// Gets the number of leaves in the trie. All the leaves are pairwise inequivalent predicates.
        /// </summary>
        public int LeafCount
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Calculates the sum of all leaf depths.
        /// </summary>
        public int TotalLeafDepth()
        {
            int tot = 0;
            if (tree != null)
                foreach (int n in tree.EnumerateLeafDepths())
                    tot += n;
            return tot;
        }

        /// <summary>
        /// Calculates the average depth of a non-null node in the tree of the trie.
        /// </summary>
        public double AverageNodeDepth() 
        {
            int nodeCount = 0;
            int tot = 0;
            if (tree != null)
                foreach (int n in tree.EnumerateAllNodeDepths())
                {
                    tot += n;
                    nodeCount += 1;
                }
            return ((double)tot) / ((double)nodeCount);
        }

        /// <summary>
        /// Creates internally an empty trie of atoms to distinguish predicates. Throws AutomataException if algebra.IsAtomic is false.
        /// </summary>
        /// <param name="algebra">given atomic Boolean algebra</param>
        public PredicateTrie(IBooleanAlgebra<T> algebra)
        {
            if (!algebra.IsAtomic)
                throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);

            this.algebra = algebra;
        }

        /// <summary>
        /// For all p: p is equivalent to Search(p).
        /// For all p and q: if p is equivalent to q then Search(p) equals Search(q).
        /// </summary>
        /// <param name="p">given predicate</param>
        public T Search(T p)
        {
            if (tree == null)
            {
                tree = new TrieTree(0, p, null, null);
                idCache[p] = p;
                count = 1;
                return p;
            }
            else
            {
                T id;
                if (!idCache.TryGetValue(p, out id))
                {
                    id = Insert(tree, p);
                    if (!idCache.ContainsKey(id))
                    {
                        //then p == id and p is in a new leaf
                        count += 1;
                    }
                    idCache[p] = id;
                }
                return id;
            }
        }

        T Insert(TrieTree tr, T pred)
        {
            if (tr.IsLeaf)
            {
                var leaf = tr.leaf;
                if (tr.k < atoms.Count)
                {
                    #region extend the trie using atoms[tr.k]
                    var vk = atoms[tr.k];
                    tr.leaf = default(T);
                    if (algebra.EvaluateAtom(vk, leaf))
                    {
                        tr.t1 = new TrieTree(tr.k + 1, leaf, null, null);
                        if (algebra.EvaluateAtom(vk, pred))
                            return Insert(tr.t1, pred);
                        else
                        {
                            //k is smallest such that vk distinguishes leaf and pred
                            tr.t0 = new TrieTree(tr.k + 1, pred, null, null);
                            return pred; //pred is new
                        }
                    }
                    else
                    {
                        tr.t0 = new TrieTree(tr.k + 1, leaf, null, null);
                        if (algebra.EvaluateAtom(vk, pred))
                        {
                            //k is smallest such that vk distinguishes leaf and pred
                            tr.t1 = new TrieTree(tr.k + 1, pred, null, null);
                            return pred; //pred is new
                        }
                        else
                            return Insert(tr.t0, pred);
                    }
                    #endregion
                }
                else
                {
                    #region the existing atoms did not distinguish pred from leaf
                    var symdiff = algebra.MkSymmetricDifference(leaf, pred);
                    var atom = algebra.GetAtom(symdiff);
                    if (atom.Equals(algebra.False))
                        return leaf;  //pred is equivalent to leaf
                    else
                    {
                        //split the leaf based on the new atom
                        atoms.Add(atom);
                        if (algebra.EvaluateAtom(atom, leaf))
                        {
                            tr.t0 = new TrieTree(tr.k + 1, pred, null, null);
                            tr.t1 = new TrieTree(tr.k + 1, leaf, null, null);
                        }
                        else
                        {
                            tr.t0 = new TrieTree(tr.k + 1, leaf, null, null);
                            tr.t1 = new TrieTree(tr.k + 1, pred, null, null);
                        }
                        tr.leaf = default(T);
                        return pred; //pred is new
                    }
                    #endregion
                }
            }
            else
            {
                #region in a nonleaf the invariant holds: tr.k < atoms.Count
                if (algebra.EvaluateAtom(atoms[tr.k], pred))
                {
                    if (tr.t1 == null)
                    {
                        tr.t1 = new TrieTree(tr.k + 1, pred, null, null);
                        return pred;
                    }
                    else
                        return Insert(tr.t1, pred);
                }
                else
                {
                    if (tr.t0 == null)
                    {
                        tr.t0 = new TrieTree(tr.k + 1, pred, null, null);
                        return pred;
                    }
                    else
                        return Insert(tr.t0, pred);
                }
                #endregion
            }
        }

        private class TrieTree
        {
            /// <summary>
            /// the case when the kth atom does not imply the predicate
            /// </summary>
            internal TrieTree t0;
            /// <summary>
            /// the case when the kth atom implies the predicate
            /// </summary>
            internal TrieTree t1;
            /// <summary>
            /// distance from the root, atom identifier for a leaf
            /// </summary>
            internal readonly int k;
            /// <summary>
            /// leaf predicate
            /// </summary>
            internal T leaf;

            internal bool IsLeaf
            {
                get { return t0 == null && t1 == null; }
            }

            internal TrieTree(int k, T leaf, TrieTree t0, TrieTree t1)
            {
                this.k = k;
                this.leaf = leaf;
                this.t0 = t0;
                this.t1 = t1;
            }

            internal static TrieTree MkInitialTree(IBooleanAlgebra<T> algebra)
            {
                var t0 = new TrieTree(1, algebra.False, null, null);
                var t1 = new TrieTree(1, algebra.True, null, null);
                var tree = new TrieTree(0, default(T), t0, t1);    // any element implies True and does not imply False
                return tree;
            }

            internal IEnumerable<int> EnumerateLeafDepths()
            {
                if (IsLeaf)
                    yield return k;
                else
                {
                    if (t0 != null)
                    {
                        foreach (var n in t0.EnumerateLeafDepths())
                            yield return n;
                    }
                    if (t1 != null)
                    {
                        foreach (var n in t1.EnumerateLeafDepths())
                            yield return n;
                    }
                }
            }

            internal IEnumerable<int> EnumerateAllNodeDepths()
            {
                yield return k;
                if (!IsLeaf)
                {
                    if (t0 != null)
                    {
                        foreach (var n in t0.EnumerateAllNodeDepths())
                            yield return n;
                    }
                    if (t1 != null)
                    {
                        foreach (var n in t1.EnumerateAllNodeDepths())
                            yield return n;
                    }
                }
            }
        }
    }

}
