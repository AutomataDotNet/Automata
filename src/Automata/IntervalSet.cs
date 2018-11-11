using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents a sorted finite set of finite intervals representing characters
    /// </summary>
    [Serializable]
    public class IntervalSet : ISerializable
    {
        Tuple<uint, uint>[] intervals;

        /// <summary>
        /// Create a new interval set
        /// </summary>
        /// <param name="intervals">given intervals</param>
        public IntervalSet(params Tuple<uint, uint>[] intervals)
        {
            this.intervals = intervals;
        }

        /// <summary>
        /// Gets the index'th element where index is in [0..Count-1]. 
        /// Throws IndexOutOfRangeException() if index is out of range.
        /// </summary>
        public uint this[int index]
        {
            get
            {
                int k = index;
                for (int i = 0; i < intervals.Length; i++)
                {
                    int ith_size = (int)intervals[i].Item2 - (int)intervals[i].Item1 + 1;
                    if (k < ith_size)
                        return intervals[i].Item1 + (uint)k;
                    else
                        k = k - ith_size;
                }
                throw new IndexOutOfRangeException();
            }
        }

        int count = -1;

        /// <summary>
        /// Number of elements in the set
        /// </summary>
        public int Count
        {
            get
            {
                if (count == -1)
                {
                    int s = 0;
                    for (int i = 0; i < intervals.Length; i++)
                    {
                        s += (int)intervals[i].Item2 - (int)intervals[i].Item1 + 1;
                    }
                    count = s;
                }
                return count;
            }
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        /// <summary>
        /// The least element in the set, provided Count != 0.
        /// </summary>
        public uint Min
        {
            get
            {
                return intervals[0].Item1;
            }
        }

        private static int CompareTuples(Tuple<uint, uint> x, Tuple<uint, uint> y)
        {
            return x.Item1.CompareTo(y.Item1);
        }

        internal static IntervalSet Merge(IEnumerable<IntervalSet> sets)
        {
            List<Tuple<uint, uint>> merged = new List<Tuple<uint, uint>>();
            foreach (var set in sets)
                merged.AddRange(set.intervals);

            merged.Sort(CompareTuples);
            return new IntervalSet(merged.ToArray());
        }

        public BDD AsBDD(IBDDAlgebra alg)
        {
            var res = alg.False;
            for (int i = 0; i < intervals.Length; i++)
                res = res | alg.MkSetFromRange(intervals[i].Item1, intervals[i].Item2, 15);
            return res;
        }

        public IEnumerable<uint> Enumerate()
        {
            for (int i = 0; i < intervals.Length; i++)
            {
                for (uint j = intervals[i].Item1; j < intervals[i].Item2; j++)
                    yield return j;
                yield return intervals[i].Item2;
            }
        }

        internal string ToCharacterClass(bool isComplement)
        {
            if (IsEmpty)
                return "[0-[0]]";

            string res = "";
            uint m = intervals[0].Item1;
            uint n = intervals[0].Item2;
            for (int i = 1; i < intervals.Length; i++)
            {
                if (intervals[i].Item1 == n + 1)
                    n = intervals[i].Item2;
                else
                {
                    res += ToCharacterClassInterval(m, n);
                    m = intervals[i].Item1;
                    n = intervals[i].Item2;
                }
            }
            res += ToCharacterClassInterval(m, n);
            if (isComplement || res.Length > 1)
            {
                res = "[" + (isComplement ? "^" : "") + res + "]";
            }
            return res;
        }

        private static string ToCharacterClassInterval(uint m, uint n)
        {
            if (m == 0 && n == 0xFFFF)
                return ".";

            if (m == n)
                return StringUtility.Escape((char)m);

            string res = StringUtility.Escape((char)m);
            if (n > m + 1)
                res += "-";
            res += StringUtility.Escape((char)n);
            return res;
        }

        public override string ToString()
        {
            return ToCharacterClass(false);
        }

        #region serialization
        /// <summary>
        /// Serialize
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("i", intervals);
        }
        /// <summary>
        /// Deserialize
        /// </summary>
        public IntervalSet(SerializationInfo info, StreamingContext context)
        {
            intervals = (Tuple<uint, uint>[])info.GetValue("i", typeof(Tuple<uint, uint>[]));
        }
        #endregion
    }
}
