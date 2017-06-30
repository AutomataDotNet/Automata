using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Automata
{
    /// <summary>
    /// Pair of comparable elements
    /// </summary>
    internal class ComparablePair<S, T> : Tuple<S, T>, IComparable where S : IComparable where T : IComparable
    {
        public int CompareTo(object obj)
        {
            ComparablePair<S, T> op = obj as ComparablePair<S, T>;
            if (op == null)
                return -1;

            var k = Item1.CompareTo(op.Item1);
            if (k != 0)
                return k;
            else
                return Item2.CompareTo(op.Item2);
        }

        public ComparablePair(S first, T second)
            : base(first, second)
        {
        }
    }
}

