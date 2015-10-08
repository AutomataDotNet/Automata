using System;
using System.Collections.Generic;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents a set of bit-vectors in form of a Binary Decision Diagram.
    /// </summary>
    public class BDD
    {
        //represents bit=1 case
        internal BDD one;
        //represents bit=0 case
        internal BDD zero;

        /// <summary>
        /// bit nr of the node 
        /// </summary>
        internal readonly int bit;

        private BDD() { }

        internal BDD(int bit, BDD one, BDD zero)
        {
            this.one = one;
            this.zero = zero;
            this.bit = bit;
        }
        /// <summary>
        /// True iff either the set is empty or full.
        /// </summary>
        public bool IsTrivial
        {
            get { return one == null; }
        }

        /// <summary>
        /// The full set.
        /// </summary>
        public readonly static BDD Full = new BDD(-1, null, null);
        /// <summary>
        /// True iff the set is full.
        /// </summary>
        public bool IsFull
        {
            get { return bit == -1; }
        }

        /// <summary>
        /// The empty set.
        /// </summary>
        public readonly static BDD Empty = new BDD(-2, null, null);

        /// <summary>
        /// True iff the set is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return bit == -2; }
        }

        /// <summary>
        /// The encoding of the set for higher ordinals for the case when the current bit is 1.
        /// The value is null iff IsTrivial is true.
        /// </summary>
        public BDD One
        {
            get { return one; }
        }
        /// <summary>
        /// The encoding of the set for higher ordinals for the case when the current bit is 0.
        /// The value is null iff IsTrivial is true.
        /// </summary>
        public BDD Zero
        {
            get { return zero; }
        }

        /// <summary>
        /// Returns the bit position. Returns a negative value when the set is empty or full.
        /// </summary>
        public int Bit
        {
            get
            {
                return bit;
            }
        }

        /// <summary>
        /// Counts the number of nodes in the underlying BDD representation of the set.
        /// </summary>
        public int CountNodes()
        {
            if (IsTrivial)
                return 1; //either True or False

            HashSet<BDD> visited = new HashSet<BDD>();
            Stack<BDD> stack = new Stack<BDD>();
            stack.Push(this);
            visited.Add(this);
            while (stack.Count > 0)
            {
                BDD a = stack.Pop();
                if (!a.one.IsTrivial && visited.Add(a.one))
                    stack.Push(a.one);
                if (!a.zero.IsTrivial && visited.Add(a.zero))
                    stack.Push(a.zero);
            }
            return visited.Count + 2;
        }

        /// <summary>
        /// Store the character set BDD as a graph in the given file.
        /// </summary>
        public void ToDot(string file)
        {
            string fname = (file.EndsWith(".dot") ? file : file + ".dot");
            Microsoft.Automata.Internal.DirectedGraphs.DotWriter.CharSetToDot(this, file, fname, Internal.DirectedGraphs.DotWriter.RANKDIR.TB, 12);
        }

    }
}

