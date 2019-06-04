using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// A value class representing a sequence of elements of type T.
    /// </summary>
    public class Sequence<T> : IEnumerable<T>
    {
        int offset;
        T[] elems;

        /// <summary>
        /// Gets the i'th element of the sequence, where i must be between 0 and Length-1.
        /// </summary>
        public T this[int i]
        {
            get { return elems[i + offset]; }
        }

        /// <summary>
        /// The number of elements in the sequence
        /// </summary>
        public int Length { get { return elems.Length - offset; } }

        /// <summary>
        /// Returns true iff the sequence is empty.
        /// </summary>
        public bool IsEmpty { get { return elems.Length == offset; } }

        /// <summary>
        /// The empty sequence.
        /// </summary>
        public static Sequence<T> Empty = new Sequence<T>();

        /// <summary>
        /// Creates a new sequence containing the given elements.
        /// </summary>
        /// <param name="elems">given elements of the sequence</param>
        public Sequence(params T[] elems)
        {
            this.offset = 0;
            this.elems = elems;
        }

        /// <summary>
        /// Creates a new sequence containing the given elements.
        /// </summary>
        /// <param name="elems">given elements of the sequence</param>
        public Sequence(IEnumerable<T> elems)
        {
            this.offset = 0;
            this.elems = new List<T>(elems).ToArray();
        }

        /// <summary>
        /// Append seq at the end of this sequence
        /// </summary>
        public Sequence<T> Append(Sequence<T> seq)
        {
            var k = this.Length;
            var m = seq.Length;
            var newelems = new T[k + m];
            Array.Copy(this.elems, this.offset, newelems, 0, k);
            Array.Copy(seq.elems, seq.offset, newelems, k, m);
            return new Sequence<T>(newelems);
        }

        /// <summary>
        /// Creates a new sequence by appending the given elements at the end of this sequence.
        /// </summary>
        /// <param name="seq">elements to be appended at the end of this sequence</param>
        public Sequence<T> Append(params T[] seq)
        {
            return this.Append(new Sequence<T>(seq));
        }

        /// <summary>
        /// Creates a new sequence by appending seq at the end of this sequence.
        /// </summary>
        /// <param name="seq">elements to be appended</param>
        public Sequence<T> Append(IEnumerable<T> seq)
        {
            return this.Append(new Sequence<T>(seq));
        }

        /// <summary>
        /// Returns the sequence where the i'th element, starting with 0, has been replaced by e. 
        /// </summary>
        public Sequence<T> Replace(int i, T e)
        {
            var new_elems = new T[this.Length];
            Array.Copy(this.elems, this.offset, new_elems, 0, this.Length);
            new_elems[i] = e;
            var res = new Sequence<T>(new_elems);
            return res;
        }

        /// <summary>
        /// Returns the suffix of the sequence starting from the i'th element. Indexing starts with 0.
        /// 
        /// Returns the empty sequence if i is >= the length of this sequence.
        /// 
        /// Returns this sequence if 0 >= i.
        /// </summary>
        /// <param name="i">index of the start position</param>
        public Sequence<T> Suffix(int i)
        {
            if (i >= this.Length)
                return Sequence<T>.Empty;
            else if (0 >= i)
                return this;
            else
            {
                var seq = new Sequence<T>(elems);
                seq.offset = this.offset + i;
                return seq;
            }
        }

        public Sequence<S> ConvertAll<S>(Func<T, S> f)
        {
            if (offset == 0)
                return new Sequence<S>(Array.ConvertAll(elems, x => f(x)));
            else
                return new Sequence<S>(Array.ConvertAll(this.ToArray(), x => f(x)));
        }

        public T[] ToArray()
        {
            if (offset == 0)
                return elems;
            else
            {
                var elems1 = new T[Length];
                Array.Copy(elems, offset, elems1, 0, Length);
                return elems1;
            }
        }

        /// <summary>
        /// Two sequences are equal iff they have the same length and their i'th elements are equal for all i.
        /// </summary>
        /// <summary>
        /// Two sequences are equal iff they have the same length and their i'th elements are equal for all i.
        /// </summary>
        public override bool Equals(object obj)
        {
            var that = obj as Sequence<T>;
            int k = Length;
            if (that == null || k != that.Length)
                return false;
            for (int i = 0; i < k; i++)
                if (!object.Equals(this.elems[i + this.offset], that.elems[i + that.offset]))
                    return false;
            return true;
        }

        int hashcode = 0;
        public override int GetHashCode()
        {
            if (hashcode == 0)
            {
                int k = elems.Length - offset;
                int res = k;
                for (int i = 0; i < k; i++)
                    res += (object.Equals(this[i], default(T)) ? 0 : (this[i].GetHashCode() << i));
                hashcode = res;
            }
            return hashcode;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < this.Length; i++)
            {
                if (i > 0)
                    sb.Append(",");
                if (this[i] == null)
                    sb.Append("(null)");
                else
                    sb.Append(this[i]);
            }
            sb.Append("]");
            return sb.ToString();
        }

        public string ToString(Func<T,string> prettyprint)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < this.Length; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(prettyprint(this[i]));
            }
            sb.Append("]");
            return sb.ToString();
        }

        public IEnumerator<T> GetEnumerator()
        {
            IEnumerable<T> ie;
            if (offset == 0)
                ie = (IEnumerable<T>)elems;
            else
                ie = (IEnumerable<T>)ToArray();
            return ie.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            IEnumerable<T> ie;
            if (offset == 0)
                ie = (IEnumerable<T>)elems;
            else
                ie = (IEnumerable<T>)ToArray();
            return ie.GetEnumerator();
        }

        /// <summary>
        /// Returns the index i if there exists i such that !this[i].Equals(s[i]) but this[j].Equals(s[j]) for all j != i.
        /// Returns -1 otherwise.
        /// </summary>
        public int EqAllButOne(Sequence<T> s)
        {
            if (this.Length != s.Length || this.Length == 0)
                return -1;

            int res = -1;

            for (int i = 0; i < Length; i++)
                if (!object.Equals(this[i], s[i]))
                    if (res == -1)
                        res = i;
                    else
                        return -1;

            return res;
        }

        /// <summary>
        /// Reuturns the maximal common prefix between this sequence and another sequence
        /// </summary>
        /// <typeparam name="S">element type</typeparam>
        /// <param name="that">the other sequence</param>
        /// <returns></returns>
        public Sequence<T> MaximalCommonPrefix(Sequence<T> that)
        {
            if (that.IsEmpty || this.IsEmpty)
                return Sequence<T>.Empty;
            else
            {
                int k = (this.Length <= that.Length ? this.Length : that.Length);
                int i = 0;
                while (i < k)
                {
                    if (object.Equals(this[i], that[i]))
                        i += 1;
                    else
                        break;
                }
                if (i == 0)
                    return Sequence<T>.Empty;
                else
                {
                    T[] common_prefix = new T[i];
                    Array.Copy(this.elems, this.offset, common_prefix, 0, i);
                    var pref = new Sequence<T>(common_prefix);
                    return pref;
                }
            }
        }

        /// <summary>
        /// Try to find an element in the sequence for which pred is true.
        /// If such an element exists then true is returned and elem is the first such element 
        /// else false is retured and elem is assigned default(T).
        /// </summary>
        /// <param name="pred">given predicate</param>
        /// <param name="elem">the found element if true is returned</param>
        /// <returns></returns>
        public bool TryGetElement(Predicate<T> pred, out T elem)
        {
            for (int i=offset; i < elems.Length; i++)
            {
                if (pred(elems[i]))
                {
                    elem = elems[i];
                    return true;
                }
            }
            elem = default(T);
            return false;
        }

        /// <summary>
        /// Returns true if there exists an element in the sequence for which pred is true.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="pred">given predicate</param>
        public bool Exists(Predicate<T> pred)
        {
            return Array.Exists(ToArray(), pred);
        }

        /// <summary>
        /// Returns true if the predicate holds for all elements in the sequence.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="pred">given predicate</param>
        public bool TrueForAll(Predicate<T> pred)
        {
            return Array.TrueForAll(ToArray(), pred);
        }

        /// <summary>
        /// The first element of the sequence
        /// </summary>
        public T First
        {
            get { return elems[offset]; }
        }

        /// <summary>
        /// The rest of the sequence
        /// </summary>
        public Sequence<T> Rest()
        {
            return Suffix(1);
        }

        /// <summary>
        /// Reverse the sequence
        /// </summary>
        public Sequence<T> Reverse()
        {
            if (this.Length <= 1)
                return this;
            else
            {
                int last = elems.Length - 1;
                int k = last - offset + 1;
                var elems_rev = new T[k];
                for (int i = 0; i < k; i++)
                {
                    elems_rev[i] = this.elems[last - i];
                }
                return new Sequence<T>(elems_rev);
            }
        }

        /// <summary>
        /// Append all the sequences into a single serquence
        /// </summary>
        /// <param name="seqs">given sequences to be appended</param>
        public static Sequence<T> AppendAll(params Sequence<T>[] seqs)
        {
            List<T> elems = new List<T>();
            for (int i = 0; i < seqs.Length; i++)
            {
                var subseq = seqs[i];
                for (int j = 0; j < subseq.Length; j++)
                    elems.Add(subseq[j]);
            }
            var seq = new Sequence<T>(elems.ToArray());
            return seq;
        }
    }
}
