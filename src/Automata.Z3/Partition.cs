using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata.Z3
{
    public class Partition
    {
        Dictionary<int, int> B;
        List<HashSet<int>> P;

        public Partition(HashSet<int> states)
        {
            if (states.Count == 0)
                throw new Exception("Partition can't be empty");
            else
            {
                P = new List<HashSet<int>>();
                B = new Dictionary<int, int>();
                P.Add(states);
                foreach (var i in states)
                    B[i] = 0;
            }
        }

        /**
         * Return the representative of the given element.
         * @param k an element in the partitioned set.
         * @return the smallest integer in same class as k
         */
        public int first(int k)
        {
            return P[B[k]].First();
        }

        /**
         * Return next element in class (cyclic).
         * @param k an element in the partitioned set.
         * @return the smallest integer larger than k if there is one in the class
         * and the smallest integer in class otherwise.
         */
        public int next(int k)
        {
            int n = B[k];
            var found = false;
            foreach (var vi in P[n])
            {
                if (found)
                    return vi;

                if (vi == k)
                    found = true;
            }
            foreach (var vi in P[n])
                return vi;

            throw new Exception("Should not be empty");
        }

        public HashSet<int> all(int k)
        {
            return P[B[k]];
        }

        /* Name:	equiv
         * Class:	Partition.
         * Space:	None.
         * Purpose:	Returns true if i and j are equivalent.
         * Parameters:	i 		- (i) state number.
                        j               - (i) state number
         * Returns:	True if i and j are equivalent false otherwise.
         * Globals:	None.
         * Remarks:	Does not check limits.
         */
        public bool equiv(int i, int j)
        {
            return B[i] == B[j];
        }

        /* Name:	refine
         * Class:	Partition.
         * Space:	None.
         * Purpose:	Splits container class into two subclasses: the input subset and the remainder.
         * Parameters:	subset 		- (i) subset of states.
         * Returns:	Nothing.
         * Globals:	None.
         * Remarks:	Input subset must be properly contained in a class. 
                        Input subset cannot contain a class representive.
         */
        public void refine(HashSet<int> subset)
        {
            foreach (var i in subset)
            {
                P[B[i]].Remove(i);
                B[i] = P.Count;
            }
            P.Add(subset);
        }

        /**
         * Return equivalence class.
         * 
         */
        public HashSet<int> block(int k)
        {
            return P[B[k]];
        }

        public int getSize() { return P.Count; }
    }

    public class Triple
    {
        public int v1, v2, v3;

        public Triple(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Triple p = obj as Triple;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (v1 == p.v1) && (v2 == p.v2) && (v3 == p.v3);
        }


        public override int GetHashCode()
        {
            return v1 ^ v2 ^ v3;
        }

        public override string ToString()
        {
            return v1 + "," + v2 + "," + v3;
        }
    }

    public class HashSetComparer : IEqualityComparer<HashSet<Triple>>
    {
        public bool Equals(HashSet<Triple> left, HashSet<Triple> right)
        {
            return left.SetEquals(right);
        }

        public int GetHashCode(HashSet<Triple> obj)
        {
            int hashcode = 0;
            foreach (var v in obj)
                hashcode += v.GetHashCode();
            return hashcode;
        }
    }

    public class ListComparer<T> : IEqualityComparer<List<T>>
    {
        public bool Equals(List<T> left, List<T> right)
        {
            return left.SequenceEqual(right);
        }

        public int GetHashCode(List<T> obj)
        {
            int hashcode = 0;
            foreach (var v in obj)
                hashcode += v.GetHashCode();
            return hashcode;
        }
    }
}