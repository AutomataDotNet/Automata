using System;
using System.Collections.Generic;


namespace Microsoft.Automata.MSO
{
    /// <summary>
    /// Simply linked list of elements.
    /// </summary>
    /// <typeparam name="T">element type</typeparam>
    public class Cons<T> : IEnumerable<T>
    {
        T first;
        Cons<T> rest;
        int count;

        /// <summary>
        /// Gets the first element of this list.
        /// </summary>
        public T First
        {
            get
            {
                if (count == 0)
                    throw new InvalidOperationException("Empty list");
                return first;
            }
        }

        /// <summary>
        /// Gets the rest of the list.
        /// </summary>
        public Cons<T> Rest
        {
            get
            {
                if (count == 0)
                    throw new InvalidOperationException("Empty list");
                return rest;
            }
        }

        /// <summary>
        /// Gets the number of elements in the list.
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Gets the i'th element, i must be in the range 0..Count-1.
        /// The operation is linear in the length of the list.
        /// </summary>
        /// <param name="i">index of the element to get</param>
        public T GetElement(int i)
        {
            if (i < 0 || i >= count)
                throw new IndexOutOfRangeException();
            var curr = this;
            for (int j = 0; j < i; j++)
                curr = curr.rest;
            return curr.first;
        }

        /// <summary>
        /// Gets the i'th element, i must be in the range 0..Count-1.
        /// The operation is linear in the length of the list.
        /// </summary>
        /// <param name="i">index of the element to get</param>
        public T this[int i]
        {
            get
            {
                return GetElement(i);
            }
        }

        Cons()
        {
            this.first = default(T);
            this.rest = null;
            this.count = 0;
        }

        readonly static Cons<T> empty = new Cons<T>();

        /// <summary>
        /// The empty list.
        /// </summary>
        public static Cons<T> Empty
        {
            get { return empty; }
        }

        /// <summary>
        /// Returns true if the list is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return count == 0; }
        }

        /// <summary>
        /// Constructs a new Cons list.
        /// </summary>
        /// <param name="first">the first element</param>
        /// <param name="rest">the rest of the list</param>
        public Cons(T first, Cons<T> rest)
        {
            if (rest == null)
                throw new ArgumentNullException("rest");
            this.first = first;
            this.rest = rest;
            this.count = rest.count + 1;
        }

        /// <summary>
        /// Make a new list of the given elements.
        /// </summary>
        /// <param name="elems">given elements</param>
        public static Cons<T> Mk(params T[] elems)
        {
            var res = empty;
            for (int i = elems.Length - 1; i >= 0; i--)
                res = new Cons<T>(elems[i], res);
            return res;
        }
        
        /// <summary>
        /// Returns true if there exists an element that satisfies pred
        /// </summary>
        /// <param name="pred">given predicate</param>
        public bool Exists(Predicate<T> pred)
        {
            var elems = this;
            while (!elems.IsEmpty)
                if (pred(elems.first))
                    return true;
                else
                    elems = elems.rest;
            return false;
        }

        /// <summary>
        /// Gets the enumerator of the elements in this Cons list.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return new ConsEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ConsEnumerator(this);
        }

        class ConsEnumerator : IEnumerator<T>
        {
            Cons<T> orig;
            Cons<T> cons;
            T current;
            internal ConsEnumerator(Cons<T> cons)
            {
                this.cons = cons;
                this.orig = cons;
                this.current = default(T);
            }

            public T Current
            {
                get
                {
                    return current;
                }
            }

            public void Dispose()
            { }

            object System.Collections.IEnumerator.Current
            {
                get { return current; }
            }

            public bool MoveNext()
            {
                if (cons.IsEmpty)
                    return false;
                else
                {
                    current = cons.first;
                    cons = cons.rest;
                    return true;
                }
            }

            public void Reset()
            {
                this.cons = this.orig;
                this.current = default(T);
            }
        }

        //public override void PrettyPrint(StringBuilder sb)
        //{
        //    sb.Append("(");
        //    var curr = this;
        //    bool firstdone = false;
        //    while (!curr.isempty)
        //    {
        //        if (firstdone)
        //            sb.Append(" ");
        //        curr.first.PrettyPrint(sb);
        //        curr = curr.rest;
        //        firstdone = true;
        //    }
        //    sb.Append(")");
        //}

        /// <summary>
        /// Returns true iff the lists have the same length and have equal elements.
        /// </summary>
        /// <param name="list">other list to compare to</param>
        public bool Equals(Cons<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (list.count != count)
                return false;

            var list1 = this;
            var list2 = list;
            for (int j = 0; j < count; j++)
            {
                if (!object.Equals(list1.first, list2.first))
                    return false;
                list1 = list1.rest;
                list2 = list2.rest;
            }
            return true;
        }

        public override string ToString()
        {
            string res = "";
            foreach (T elem in this)
            {
                if (res != "")
                    res += ",";
                res += elem.ToString();
            }
            return res;
        }

        public string ToString(string sep, Func<T, string> elemToString = null, string start = "", string end = "")
        {
            string res = start;
            bool nonmepty = false;
            foreach (T elem in this)
            {
                if (nonmepty)
                    res += sep;
                res += (elemToString == null ? string.Format("{0}", elem) : elemToString(elem));
                nonmepty = true;
            }
            return res + end;
        }

        public Cons<S> Convert<S>(Func<T, S> f)
        {
            if (IsEmpty)
                return Cons<S>.Empty;
            else
                return new Cons<S>(f(first), rest.Convert<S>(f));
        }

        public T[] ToArray()
        {
            T[] res = new T[Count];
            var elems = this;
            int i = 0;
            while (!elems.IsEmpty)
            {
                res[i++] = elems.First;
                elems = elems.Rest;
            }
            return res;
        }
    }
}
