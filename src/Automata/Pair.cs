using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Automata
{
    /// <summary>
    /// Pair of elements of type S and type T.
    /// This is a value class: two pairs are equal iff their 
    /// first elements are equal and their second elements are equal.
    /// Obsolete, extends Tuple of two elements.
    /// </summary>
    /// <typeparam name="S">type of the first element</typeparam>
    /// <typeparam name="T">type of the secod element</typeparam>
    public class Pair<S, T> : Tuple<S,T>
    {
        /// <summary>
        /// The first element of the pair.
        /// </summary>
        public S First { get { return this.Item1; } }
        /// <summary>
        /// The second element of the pair.
        /// </summary>
        public T Second { get { return this.Item2; } }

        public Pair(S first, T second) : base(first, second) { }

        public override string ToString()
        {
            return string.Format("({0},{1})", First, Second);
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    } 

    /// <summary>
    /// Pair of comparable elements
    /// </summary>
    public class ComparablePair<S,T> : Pair<S,T>, IComparable where S:IComparable where T:IComparable
    {
        public int CompareTo(object obj)
        {
            ComparablePair<S, T> op = obj as ComparablePair<S, T>;
            if (op == null)
                return -1;

            var k = First.CompareTo(op.First);
            if (k != 0)
                return k;
            else
                return Second.CompareTo(op.Second);
        }

        public ComparablePair(S first, T second)
            : base(first, second)
        {
        }
    }
}
