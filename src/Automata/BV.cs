using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;


namespace Microsoft.Automata
{
    /// <summary>
    /// Represents a bitvector
    /// </summary>
    [Serializable]
    public class BV : IComparable, ISerializable
    {
        int nrOfBits;
        internal ulong first;
        internal ulong[] more;

        /// <summary>
        /// Constructs a bitvector
        /// </summary>
        /// <param name="first">first 64 bits</param>
        /// <param name="more">remaining bits in 64 increments</param>
        /// <param name="nrOfBits">total number of bits in the bitvector</param>
        public BV(int nrOfBits, ulong first, params ulong[] more)
        {
            this.nrOfBits = nrOfBits;
            this.first = first;
            this.more = more;
        }

        /// <summary>
        /// Bitwise AND
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BV operator &(BV x, BV y)
        {
            int k = (x.more.Length <= y.more.Length ? x.more.Length : y.more.Length);
            var first = x.first & y.first;
            var more = new ulong[k];
            for (int i = 0; i < k; i++)
            {
                more[i] = x.more[i] & y.more[i];
            }
            return new BV(x.nrOfBits, first, more);
        }

        /// <summary>
        /// Bitwise OR
        /// </summary>
        public static BV operator |(BV x, BV y)
        {
            int k = (x.more.Length <= y.more.Length ? x.more.Length : y.more.Length);
            var first = x.first | y.first;
            var more = new ulong[k];
            for (int i = 0; i < k; i++)
            {
                more[i] = x.more[i] | y.more[i];
            }
            return new BV(x.nrOfBits, first, more);
        }

        /// <summary>
        /// Bitwise XOR
        /// </summary>
        public static BV operator ^(BV x, BV y)
        {
            int k = (x.more.Length <= y.more.Length ? x.more.Length : y.more.Length);
            var first = x.first ^ y.first;
            var more = new ulong[x.more.Length];
            for (int i = 0; i < x.more.Length; i++)
            {
                more[i] = x.more[i] ^ y.more[i];
            }
            return new BV(x.nrOfBits, first, more);
        }

        /// <summary>
        /// Bitwise NOT
        /// </summary>
        public static BV operator ~(BV x)
        {
            var first = ~x.first;
            if (x.nrOfBits < 64)
                first = first & (((ulong)1 << x.nrOfBits) - 1);
            var more = new ulong[x.more.Length];
            int remNrOfBits = x.nrOfBits;
            for (int i = 0; i < x.more.Length; i++)
            {
                remNrOfBits = x.nrOfBits - 64;
                more[i] = ~x.more[i];
                if (remNrOfBits < 64)
                    more[i] = more[i] & (((ulong)1 << x.nrOfBits) - 1);
            }
            var notx = new BV(x.nrOfBits, first, more);
            return notx;
        }

        /// <summary>
        /// less than
        /// </summary>
        public static bool operator <(BV x, BV y)
        {
            return x.CompareTo(y) < 0;
        }

        /// <summary>
        /// greater than
        /// </summary>
        public static bool operator >(BV x, BV y)
        {
            return x.CompareTo(y) > 0;
        }

        /// <summary>
        /// less than or equal
        /// </summary>
        public static bool operator <=(BV x, BV y)
        {
            return x.CompareTo(y) <= 0;
        }

        /// <summary>
        /// greater than or equal
        /// </summary>
        public static bool operator >=(BV x, BV y)
        {
            return x.CompareTo(y) >= 0;
        }

        /// <summary>
        /// Shows which bits are true
        /// </summary>
        public override string ToString()
        {
            List<int> bits = new List<int>();
            for (int i = 0; i < (nrOfBits < 64 ? nrOfBits : 64); i++)
            {
                if ((first & ((ulong)1 << i)) != 0)
                {
                    bits.Add(i);
                }
            }
            for (int j = 0; j < more.Length; j++)
            {
                for (int i = 0; i < 64; i++)
                {
                    if ((j + 1) * 64 + i < nrOfBits)
                    {
                        if ((more[j] & ((ulong)1 << i)) != 0)
                        {
                            bits.Add(((j + 1) * 64) + i);
                        }
                    }
                }
            }
            return DisplayIntervals(bits);
        }

        internal static string DisplayIntervals(List<int> bits)
        {
            List<Tuple<int, int>> intervals = new List<Tuple<int, int>>();
            int last = -1;
            foreach (var b in bits)
            {
                if (last == -1)
                {
                    intervals.Add(new Tuple<int, int>(b, b));
                    last = 0;
                }
                else if (intervals[last].Item2 == b - 1)
                {
                    intervals[last] = new Tuple<int, int>(intervals[last].Item1, b);
                }
                else
                {
                    intervals.Add(new Tuple<int, int>(b, b));
                    last += 1;
                }
            }
            string res = "";
            foreach (var pair in intervals)
            {
                if (res != "")
                    res += ",";
                if (pair.Item1 == pair.Item2)
                    res += pair.Item1;
                else if (pair.Item2 == pair.Item1 + 1)
                    res += pair.Item1 + "," + pair.Item2;
                else
                    res += pair.Item1 + ".." + pair.Item2;
            }
            return "[" + res + "]";
        }

        public override bool Equals(object obj)
        {
            BV bv = obj as BV;
            if (bv == null)
                return false;
            if (this == bv)
                return true;
            if (this.first != bv.first)
                return false;
            if (bv.more.Length != this.more.Length)
                return false;
            for (int i = 0; i < more.Length; i++)
            {
                if (more[i] != bv.more[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int h = first.GetHashCode();
            for (int i = 0; i < more.Length; i++)
            {
                h = (h << 5) ^ more[i].GetHashCode();
            }
            return h;
        }

        public int CompareTo(object obj)
        {
            BV that = obj as BV;
            if (that == null)
                return 1;
            else if (this.nrOfBits != that.nrOfBits)
                return (this.nrOfBits.CompareTo(that.nrOfBits));
            else
            {
                int k = this.more.Length;
                if (k > 0)
                {
                    int i = k - 1;
                    while (i >= 0)
                    {
                        var comp = this.more[i].CompareTo(that.more[i]);
                        if (comp == 0)
                            i = i - 1;
                        else
                            return comp;
                    }
                }
                return this.first.CompareTo(that.first);
            }
        }

        #region serialization
        /// <summary>
        /// Serialize
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("n", nrOfBits);
            info.AddValue("f", first);
            info.AddValue("m", more);
        }
        /// <summary>
        /// Deserialize
        /// </summary>
        public BV(SerializationInfo info, StreamingContext context)
        {
            nrOfBits = info.GetInt32("n");
            first = info.GetUInt64("f");
            more = (ulong[])info.GetValue("m", typeof(ulong[]));
        }
        #endregion

    }
}
