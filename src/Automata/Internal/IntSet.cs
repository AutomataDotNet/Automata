using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents a set of integers. This is a value class.
    /// </summary>
    internal class IntSet
    {
        public static readonly IntSet Empty = new IntSet();

        internal HashSet<int> elems;
        int choice = int.MaxValue;
        string repr = null;
        string Repr
        {
            get
            {
                if (repr == null)
                    repr = MkString();
                return repr;
            }
        }
        public IntSet(params int[] elems)
        {
            this.elems = new HashSet<int>();
            foreach (int elem in elems)
            {
                this.elems.Add(elem);
                this.choice = Math.Min(elem, this.choice);
            }
        }
        public IntSet(IEnumerable<int> elems)
        {
            this.elems = new HashSet<int>();
            foreach (int elem in elems)
            {
                this.elems.Add(elem);
                this.choice = Math.Min(elem, this.choice);
            }
        }
        public override int GetHashCode()
        {
            return Repr.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return Repr.Equals(((IntSet)obj).Repr);
        }
        public override string ToString()
        {
            return Repr;
        }

        internal int Choice
        {
            get { return choice; }
        }

        private string MkString()
        {
            if (this.elems.Count == 0) return "{}";
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            bool addComma = false;
            List<int> elemsList = new List<int>(elems);
            elemsList.Sort();
            foreach (int elem in elemsList)
            {
                if (addComma)
                    sb.Append(",");
                else
                    addComma = true;
                sb.Append(elem);
            }
            sb.Append("}");
            return sb.ToString();
        }

        public bool Contains(int elem)
        {
            return elems.Contains(elem);
        }

        public IntSet Intersect(IEnumerable<int> other)
        {
            var elemsInBoth = new HashSet<int>(elems);
            elemsInBoth.IntersectWith(other);
            return new IntSet(elemsInBoth);
        }

        public IntSet Union(params int[] other)
        {
            var elemsInBoth = new HashSet<int>(elems);
            elemsInBoth.UnionWith(other);
            return new IntSet(elemsInBoth);
        }

        public bool IsSingleton
        {
            get { return elems.Count == 1; }
        }

        public bool IsEmpty
        {
            get { return elems.Count == 0; }
        }

        public IEnumerable<int> EnumerateMembers()
        {
            return elems;
        }
    }
}
