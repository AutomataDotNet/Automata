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
        T[] elems;

        /// <summary>
        /// Gets the i'th element of the sequence, where i must be between 0 and Length-1.
        /// </summary>
        public T this[int i]
        {
            get { return elems[i]; }
        }

        /// <summary>
        /// The number of elements in the sequence
        /// </summary>
        public int Length { get { return elems.Length; } }

        /// <summary>
        /// Returns true iff the sequence is empty.
        /// </summary>
        public bool IsEmpty { get { return elems.Length == 0; } }

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
            this.elems = elems;
        }

        /// <summary>
        /// Creates a new sequence containing the given elements.
        /// </summary>
        /// <param name="elems">given elements of the sequence</param>
        public Sequence(IEnumerable<T> elems)
        {
            this.elems = new List<T>(elems).ToArray();
        }

        /// <summary>
        /// Creates a new sequence by appending seq at the end of this sequence.
        /// </summary>
        /// <param name="seq">sequence to be appended at the end of this</param>
        public Sequence<T> Append(Sequence<T> seq)
        {
            if (seq.Length == 0)
                return this;
            else
            {
                T[] elems1 = new T[elems.Length + seq.Length];
                Array.Copy(elems, elems1, elems.Length);
                Array.Copy(seq.elems, 0, elems1, elems.Length, seq.Length);
                return new Sequence<T>(elems1);
            }
        }

        /// <summary>
        /// Creates a new sequence by appending seq at the end of this sequence.
        /// </summary>
        /// <param name="seq">elements to be appended</param>
        public Sequence<T> Append(IEnumerable<T> seq)
        {
            var new_elems = new List<T>(elems);
            new_elems.AddRange(seq);
            return new Sequence<T>(new_elems.ToArray());
        }

        /// <summary>
        /// Returns the sequence where the i'th element, starting with 0, has been replaced by e. 
        /// </summary>
        public Sequence<T> Replace(int i, T e)
        {
            var new_elems = new T[this.Length];
            Array.Copy(this.elems, new_elems, this.Length);
            new_elems[i] = e;
            var res = new Sequence<T>(new_elems);
            return res;
        }

        /// <summary>
        /// Creates a new sequence by appending the given elements at the end of this sequence.
        /// </summary>
        /// <param name="seq">elements to be appended at the end of this sequence</param>
        public Sequence<T> Append(params T[] seq)
        {
            if (seq.Length == 0)
                return this;
            else
            {
                T[] elems1 = new T[elems.Length + seq.Length];
                Array.Copy(elems, elems1, elems.Length);
                Array.Copy(seq, 0, elems1, elems.Length, seq.Length);
                return new Sequence<T>(elems1);
            }
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
            if (i >= this.elems.Length)
                return Sequence<T>.Empty;
            else if (0 >= i)
                return this;
            else
            {
                var res = new T[elems.Length - i];
                Array.Copy(elems, i, res, 0, res.Length);
                return new Sequence<T>(res);
            }
        }


        public Sequence<T> ConvertAll(Func<T, T> f)
        {
            var a = Array.ConvertAll(elems, x => f(x));
            return new Sequence<T>(a);
        }

        public T[] ToArray()
        {
            return elems;
        }

        /// <summary>
        /// Two sequences are equal iff they have the same length and their i'th elements are equal for all i.
        /// </summary>
        public override bool Equals(object obj)
        {
            var s = obj as Sequence<T>;
            if (s == null || s.elems.Length != elems.Length)
                return false;
            for (int i = 0; i < elems.Length; i++)
                if (!object.Equals(elems[i], s.elems[i]))
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            int res = elems.Length;
            for (int i = 0; i < elems.Length; i++)
                res += (object.Equals(elems[i], default(T)) ? 0 : (elems[i].GetHashCode() << i));
            return res;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < elems.Length; i++)
            {
                if (i > 0)
                    sb.Append(",");
                if (elems[i] == null)
                    sb.Append("(null)");
                else
                    sb.Append(elems[i]);
            }
            sb.Append("]");
            return sb.ToString();
        }


        public string ToString(Func<T,string> prettyprint)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < elems.Length; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(prettyprint(elems[i]));
            }
            sb.Append("]");
            return sb.ToString();
        }

        public IEnumerator<T> GetEnumerator()
        {
            IEnumerable<T> ie = (IEnumerable<T>)elems;
            return ie.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return elems.GetEnumerator();
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

            for (int i = 0; i < s.Length; i++)
                if (!object.Equals(this.elems[i], s.elems[i]))
                    if (res == -1)
                        res = i;
                    else
                        return -1;

            return res;
        }
    }
}
