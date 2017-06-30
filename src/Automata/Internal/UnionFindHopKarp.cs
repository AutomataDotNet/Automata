using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{

    internal class UnionFindHopKarp<S>
    {

        private int numSets;

        private Dictionary<int, int> parents;  // The index of the parent element. An element is a representative iff its parent is itself.
        private Dictionary<int, int> ranks;   // Always in the range [0, floor(log2(numElems))]. Thus has a maximum value of 30.
        private Dictionary<int, int> sizes;    // Positive number if the element is a representative, otherwise zero.
        private Dictionary<int, bool> isFinal;
        private Dictionary<int, List<S>> witness;


        public bool contains(int elem)
        {
            return parents.ContainsKey(elem);
        }

        // Constructs a new set containing the given number of singleton sets.
        // For example, new DisjointSet(3) --> {{0}, {1}, {2}}.
        public UnionFindHopKarp()
        {
            parents = new Dictionary<int, int>();
            ranks = new Dictionary<int, int>();
            sizes = new Dictionary<int, int>();
            isFinal = new Dictionary<int, bool>();
            witness = new Dictionary<int, List<S>>();
            numSets = 0;
        }

        // Constructs a new set containing the given number of singleton sets.
        // For example, new DisjointSet(3) --> {{0}, {1}, {2}}.
        public void add(int elem, bool isFin, List<S> wit)
        {
            if (parents.ContainsKey(elem))
                throw new AutomataException("Element should not be in the set already");
            parents[elem] = elem;
            ranks[elem] = 0;
            sizes[elem] = 1;
            isFinal[elem] = isFin;
            witness[elem] = wit;
            numSets++;
        }

        public int getNumberOfElements()
        {
            return parents.Count;
        }

        public int getNumberOfSets()
        {
            return numSets;
        }

        public int getRepr(int elemIndex)
        {
            // Follow parent pointers until we reach a representative
            int parent = parents[elemIndex];
            if (parent == elemIndex)
                return elemIndex;
            while (true)
            {
                int grandparent = parents[parent];
                if (grandparent == parent)
                    return parent;
                parents[elemIndex] = grandparent; // Partial path compression
                elemIndex = parent;
                parent = grandparent;
            }
        }


        // Returns the size of the set that the given element is a member of. 1 <= result <= getNumberOfElements().
        public int getSizeOfSet(int elemIndex)
        {
            return sizes[getRepr(elemIndex)];
        }

        // Returns the size of the set that the given element is a member of. 1 <= result <= getNumberOfElements().
        public List<S> getWitness(int elemIndex)
        {
            return witness[elemIndex];
        }


        // Tests whether the given two elements are members of the same set. Note that the arguments are orderless.
        public bool areInSameSet(int elemIndex0, int elemIndex1)
        {
            return getRepr(elemIndex0) == getRepr(elemIndex1);
        }


        // Merges together the sets that the given two elements belong to. This method is also known as "union" in the literature.
        // Returns false if the two elements have different final states conditions
        public bool mergeSets(int elemIndex0, int elemIndex1)
        {
            if (isFinal[elemIndex0] != isFinal[elemIndex1])
                return false;

            // Get representatives
            int repr0 = getRepr(elemIndex0);
            int repr1 = getRepr(elemIndex1);
            if (repr0 == repr1)
                return true;

            // Compare ranks
            int cmp = ranks[repr0] - ranks[repr1];
            if (cmp == 0)
            {
                // Increment repr0's rank if both nodes have same rank
                int r = ranks[repr0];
                ranks[repr0] = r + 1;
            }
            else if (cmp < 0)
            {  // Swap to ensure that repr0's rank >= repr1's rank
                int temp = repr0;
                repr0 = repr1;
                repr1 = temp;
            }

            // Graft repr1's subtree onto node repr0
            parents[repr1] = repr0;
            int sizer1 = sizes[repr1];
            sizes[repr0] = sizer1;
            sizes[repr1] = 0;
            numSets--;
            return true;
        }


        // For unit tests. This detects many but not all invalid data structures, throwing an AssertionError
        // if a structural invariant is known to be violated. This always returns silently on a valid object.
        void checkStructure()
        {
            int numRepr = 0;
            for (int i = 0; i < parents.Count; i++)
            {
                int parent = parents[i];
                int rank = ranks[i];
                int size = sizes[i];
                bool isRepr = parent == i;
                if (isRepr)
                    numRepr++;

                bool ok = true;
                ok &= 0 <= parent && parent < parents.Count;
                ok &= 0 <= rank && (isRepr || rank < ranks[parent]);
                ok &= !isRepr && size == 0 || isRepr && size >= (1 << rank);
                if (!ok)
                    throw new AutomataException("error in union find");
            }
            if (!(1 <= numSets && numSets == numRepr && numSets <= parents.Count))
                throw new AutomataException("error in union find");
        }

        public String toString()
        {
            var sets = new Dictionary<int, HashSet<int>>();
            foreach (int i in parents.Keys)
                sets[i] = new HashSet<int>();
            foreach (int i in parents.Keys)
            {
                int p = parents[i];
                HashSet<int> set = sets[p];
                set.Add(i);
                sets[p] = set;
            }
            String s = "";
            foreach (HashSet<int> set in sets.Values)
            {
                if (set.Count > 0)
                    s += set + " ";
            }

            return s;
        }
    }
}
