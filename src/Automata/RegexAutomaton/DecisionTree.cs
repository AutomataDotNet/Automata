using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.Automata
{
    /// <summary>
    /// Decision tree for mapping character ranges into corresponding partition block ids
    /// </summary>
    [Serializable]
    internal class DecisionTree : ISerializable
    {
        internal int[] precomputed;
        internal BST bst;

        internal BST Tree
        {
            get
            {
                return bst;
            }
        }

        public DecisionTree(int[] precomputed, BST bst)
        {
            this.precomputed = precomputed;
            this.bst = bst;
        }

        /// <summary>
        /// Crteate a decision tree that maps a character into a partion block id
        /// </summary>
        /// <param name="solver">character alberbra</param>
        /// <param name="partition">partition of the whole set of all characters into pairwise disjoint nonempty sets</param>
        /// <param name="precomputeLimit">upper limit for block ids for characters to be precomputed in an array (default is 0xFF, i.e. extended ASCII)</param>
        /// <returns></returns>
        internal static DecisionTree Create(CharSetSolver solver, BDD[] partition, ushort precomputeLimit = 0xFF)
        {
            if (partition.Length == 1)
                //there is no partition, everything maps to one symbol e.g. in .*
                return new DecisionTree(new int[] { }, new BST(0, null, null));

            if (precomputeLimit == 0)
                return new DecisionTree(new int[] { }, MkBST(new PartitionCut(solver, partition), 0, 0xFFFF));

            int[] precomp = Precompute(solver, partition, precomputeLimit);
            BST bst = null;
            if (precomputeLimit < ushort.MaxValue)
                bst = MkBST(new PartitionCut(solver, partition), precomputeLimit + 1, ushort.MaxValue);

            return new DecisionTree(precomp, bst);
        }

        private static int[] Precompute(CharSetSolver solver, BDD[] partition, int precomputeLimit)
        {
            int[] precomp = new int[precomputeLimit + 1];
            Func<int, int> GetPartitionId = i =>
            {
                for (int j = 0; j < partition.Length; j++)
                {
                    var i_bdd = solver.MkCharConstraint((char)i);
                    if (solver.IsSatisfiable(solver.MkAnd(i_bdd, partition[j])))
                    {
                        return j;
                    }
                }
                return -1;
            };
            for (int c = 0; c <= precomputeLimit; c++)
            {
                int id = GetPartitionId(c);
                if (id < 0)
                    throw new AutomataException(AutomataExceptionKind.InternalError);
                precomp[c] = id;
            }
            return precomp;
        }

        private static BST MkBST(PartitionCut partition, int from, int to)
        {
            var cut = partition.Cut(from, to);
            if (cut.IsEmpty)
                return null;
            else
            {
                int block_id = cut.GetSigletonId();
                if (block_id >= 0)
                    //there is precisely one block remaining
                    return new BST(block_id, null, null);
                else
                {
                    //it must be that 'from < to'
                    //or else there could only have been one block
                    int mid = (from + to) / 2;
                    var left = MkBST(cut, from, mid);
                    var right = MkBST(cut, mid + 1, to);
                    //it must be that either left != null or right != null
                    if (left == null)
                        return right;
                    else if (right == null)
                        return left;
                    else
                        return new BST(mid + 1, left, right);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetId(ushort c)
        {
            if (c < precomputed.Length)
            {
                return precomputed[c];
            }
            else
            {
                return bst.Find(c);
            }
        }

        /// <summary>
        /// Used in the decision tree to locate minterm ids of nonascii characters
        /// </summary>
        [Serializable]
        internal class BST : ISerializable
        {
            int node;
            BST left;
            BST right;

            internal BST Left
            {
                get
                {
                    return left;
                }
            }

            internal BST Right
            {
                get
                {
                    return right;
                }
            }

            internal bool IsLeaf
            {
                get
                {
                    return left == null;
                }
            }

            internal int Node
            {
                get
                {
                    return node;
                }
            }

            internal int Find(int charCode)
            {
                if (left == null)
                    return node; //return the leaf
                else if (charCode < node)
                    return left.Find(charCode);
                else
                    return right.Find(charCode);
            }

            public BST(int node, BST left, BST right)
            {
                this.node = node;
                this.left = left;
                this.right = right;
            }

            #region serialization
            /// <summary>
            /// Serialize
            /// </summary>
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("n", node);
                info.AddValue("l", left);
                info.AddValue("r", right);
            }
            /// <summary>
            /// Deserialize
            /// </summary>
            public BST(SerializationInfo info, StreamingContext context)
            {
                node = info.GetInt32("n");
                left = info.GetValue("l", typeof(BST)) as BST;
                right = info.GetValue("r", typeof(BST)) as BST;
            }
            #endregion

            public override string ToString()
            {
                if (IsLeaf)
                    return string.Format("leaf({0})", node);
                else
                    return string.Format("tree({0},{1},{2})", node, left, right);
            }
        }

        /// <summary>
        /// Represents a cut of the original partition wrt some interval
        /// </summary>
        internal class PartitionCut
        {
            BDD[] blocks;
            CharSetSolver solver;
            internal PartitionCut(CharSetSolver solver, BDD[] blocks)
            {
                this.blocks = blocks;
                this.solver = solver;
            }

            internal bool IsEmpty
            {
                get
                {
                    return Array.TrueForAll(blocks, b => b.IsEmpty);
                }
            }

            internal int GetSigletonId()
            {
                int id = -1;
                for (int i = 0; i < blocks.Length; i++)
                {
                    if (!blocks[i].IsEmpty)
                    {
                        if (id >= 0)
                            //there is more than one nonempty block
                            return -1;
                        else
                            id = i;
                    }
                }
                return id;
            }

            internal PartitionCut Cut(int lower, int upper)
            {
                var set = solver.MkCharSetFromRange((char)lower, (char)upper);
                var newblocks = Array.ConvertAll(blocks, b => solver.MkAnd(b, set));
                return new PartitionCut(solver, newblocks);
            }

            public override string ToString()
            {
                string res = "";
                for (int i = 0; i < blocks.Length; i++)
                    res += solver.PrettyPrint(blocks[i]) + (i < blocks.Length - 1 ? "," : "");
                return res;
            }
        }

        #region serialization
        /// <summary>
        /// Serialize
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("p", precomputed);
            info.AddValue("b", bst);
        }
        /// <summary>
        /// Deserialize
        /// </summary>
        public DecisionTree(SerializationInfo info, StreamingContext context)
        {
            precomputed = (int[])info.GetValue("p", typeof(int[]));
            bst = info.GetValue("b", typeof(BST)) as BST;
        }
        #endregion

        public override string ToString()
        {
            return string.Format("DecisionTree({0},{1})", new Sequence<int>(precomputed).ToString(), bst);
        }
    }

    /// <summary>
    /// Decision tree for mapping character ranges into corresponding partition block ids
    /// </summary>
    [Serializable]
    internal class BooleanDecisionTree
    {
        internal bool[] precomputed;
        internal DecisionTree.BST bst;

        internal DecisionTree.BST Tree
        {
            get
            {
                return bst;
            }
        }

        internal BooleanDecisionTree(bool[] precomputed, DecisionTree.BST bst)
        {
            this.precomputed = precomputed;
            this.bst = bst;
        }

        /// <summary>
        /// Crteate a decision tree that maps a character into a partion block id.
        /// References to solver and domain are not saved in the resulting decision tree.
        /// </summary>
        /// <param name="solver">character alberbra</param>
        /// <param name="domain">elements that map to true</param>
        /// <param name="precomputeLimit">upper limit for block ids for characters to be precomputed in an array (default is 0xFF, i.e. extended ASCII)</param>
        /// <returns></returns>
        internal static BooleanDecisionTree Create(CharSetSolver solver, BDD domain, ushort precomputeLimit = 0xFF)
        {
            BDD domain_compl = solver.MkNot(domain);
            var partition = (domain_compl.IsEmpty ? new BDD[] { domain } : new BDD[] { domain_compl, domain });
            if (precomputeLimit == 0)
                return new BooleanDecisionTree(new bool[] { }, MkBST(new DecisionTree.PartitionCut(solver, partition), 0, 0xFFFF));

            bool[] precomp = Precompute(solver, domain, precomputeLimit);
            DecisionTree.BST bst = null;
            if (precomputeLimit < ushort.MaxValue)
                bst = MkBST(new DecisionTree.PartitionCut(solver, partition), precomputeLimit + 1, ushort.MaxValue);

            return new BooleanDecisionTree(precomp, bst);
        }

        private static bool[] Precompute(CharSetSolver solver, BDD domain, int precomputeLimit)
        {
            bool[] precomp = new bool[precomputeLimit + 1];
            Func<int, bool> F = i =>
            {
                var bdd = solver.MkCharConstraint((char)i);
                if (solver.IsSatisfiable(solver.MkAnd(bdd, domain)))
                    return true;
                else
                    return false;
            };
            for (int c = 0; c <= precomputeLimit; c++)
            {
                precomp[c] = F(c);
            }
            return precomp;
        }

        private static DecisionTree.BST MkBST(DecisionTree.PartitionCut partition, int from, int to)
        {
            var cut = partition.Cut(from, to);
            if (cut.IsEmpty)
                return null;
            else
            {
                int block_id = cut.GetSigletonId();
                if (block_id >= 0)
                    //there is precisely one block remaining
                    return new DecisionTree.BST(block_id, null, null);
                else
                {
                    //it must be that 'from < to'
                    //or else there could only have been one block
                    int mid = (from + to) / 2;
                    var left = MkBST(cut, from, mid);
                    var right = MkBST(cut, mid + 1, to);
                    //it must be that either left != null or right != null
                    if (left == null)
                        return right;
                    else if (right == null)
                        return left;
                    else
                        return new DecisionTree.BST(mid + 1, left, right);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(ushort c)
        {
            return (c < precomputed.Length ? precomputed[c] : bst.Find(c) == 1);
        }

        #region serialization
        /// <summary>
        /// Serialize
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("p", precomputed);
            info.AddValue("b", bst);
        }
        /// <summary>
        /// Deserialize
        /// </summary>
        public BooleanDecisionTree(SerializationInfo info, StreamingContext context)
        {
            precomputed = (bool[])info.GetValue("p", typeof(bool[]));
            bst = info.GetValue("b", typeof(DecisionTree.BST)) as DecisionTree.BST;
        }
        #endregion

        public override string ToString()
        {
            return string.Format("DecisionTree({0},{1})", new Sequence<bool>(precomputed).ToString(), bst);
        }
    }
}
