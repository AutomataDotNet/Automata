using System;
using System.Collections.Generic;
using System.Text;


namespace Microsoft.Automata
{
    /// <summary>
    /// Simply linked list of elements of type E. 
    /// </summary>
    public class ConsList<E> : IEnumerable<E>
    {
        /// <summary>
        /// The first element in the list
        /// </summary>
        E first;

        /// <summary>
        /// Gets the first element in the list
        /// </summary>
        public E First
        {
            get { return first; }
        }


        /// <summary>
        /// The rest of the list (null if the rest is empty)
        /// </summary>
        ConsList<E> rest;

        /// <summary>
        /// Gets or sets the rest of the list. Value is null if the rest is empty.
        /// </summary>
        public ConsList<E> Rest
        {
            get { return rest; }
            set
            {
                rest = value;
            }
        }

        /// <summary>
        /// Make a new list whose first element is first and whose rest is rest (rest may be null)
        /// </summary>
        public ConsList(E first, ConsList<E> rest)
        {
            this.first = first;
            this.rest = rest;
        }

        /// <summary>
        /// Counts the number of elements in the list.
        /// Assumes that the list is not circular, or else the method will not terminate.
        /// </summary>
        public int Count()
        {
            int k = 1;
            var x = rest;
            while (x != null)
            {
                k += 1;
                x = x.rest;
            }
            return k;
        }

        /// <summary>
        /// Returns the nonempty array of the elements in the list.
        /// Assumes that the list is not circular, or else the method will not terminate.
        /// </summary>
        /// <param name="inreverse">if true the elements are in reverse order</param>
        public E[] ToArray(bool inreverse = false)
        {
            E[] res = new E[Count()];
            if (inreverse)
            {
                int k = res.Length;
                res[k - 1] = first;
                var x = rest;
                int i = k - 2;
                while (x != null)
                {
                    res[i--] = x.first;
                    x = x.rest;
                }
                return res;
            }
            else
            {
                res[0] = first;
                var x = rest;
                int i = 1;
                while (x != null)
                {
                    res[i++] = x.first;
                    x = x.rest;
                }
                return res;
            }
        }

        /// <summary>
        /// Creates a singleton list.
        /// </summary>
        /// <param name="elem">the element of the list</param>
        public ConsList(E elem) : this(elem, null) { }

        /// <summary>
        /// Creates a list of the elements in the enumeration.
        /// Returns null if the enumeration is empty.
        /// </summary>
        public static ConsList<E> Create(IEnumerable<E> elems)
        {
            ConsList<E> tl = null;
            ConsList<E> last = null;
            foreach (E elem in elems)
                if (tl == null)
                {
                    tl = new ConsList<E>(elem);
                    last = tl;
                }
                else
                {
                    last.rest = new ConsList<E>(elem);
                    last = last.rest;
                }
            return tl;
        }

        /// <summary>
        /// Tests if there exists an element e in the list such that check(e) is true
        /// </summary>
        /// <param name="check">Boolean function to perform the test</param>
        /// <returns>true iff there exists an element e in the list such that check(e) is true</returns>
        public bool Exists(Func<E, bool> check)
        {
            if (check(first))
                return true;
            if (rest == null)
                return false;
            return rest.Exists(check);
        }

        ///// <summary>
        ///// Returns a new list of elements that satisfy the check
        ///// Returns null if there are no elements that satisfy the check.
        ///// Will not terminate if the list is circular.
        ///// </summary>
        //public static ConsList<E> Filter(ConsList<E> list, Func<E, bool> check)
        //{
        //    if (list == null)
        //        return null;
        //    else
        //        if (!check(list.first))
        //            return Filter(list.rest, check);
        //        else
        //            return new ConsList<E>(list.first, Filter(list.rest, check));
        //}

        ///// <summary>
        ///// Modify the list by deleting all elements from the rest of the list that satisfy the given check.
        ///// Will not terminate if the list is circular.
        ///// </summary>
        //internal void DeleteAllFromRest(Func<E, bool> check)
        //{
        //    ConsList<E> p = this;
        //    ConsList<E> q = this.rest;
        //    while (q != null)
        //    {
        //        if (check(q.first))
        //        {
        //            p.rest = q.rest;
        //            q = q.rest;
        //        }
        //        else
        //        {
        //            p = q;
        //            q = q.rest;
        //        }
        //    }
        //}

        /// <summary>
        /// Display the list as [elem_0,elem_1,...,elem_k]. Ends with "..." if the list is circular.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            HashSet<ConsList<E>> hs = new HashSet<ConsList<E>>();
            var list = this;
            while (list != null)
            {
                if (!hs.Add(list))
                {
                    sb.Append("...");
                    return sb.ToString();
                }
                sb.Append(list.First.ToString());
                list = list.rest;
                if (list != null)
                    sb.Append(",");
            }
            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Returns a new list that is the reverse of the list.
        /// Assumes that this list is not circular, or else the method will not terminate.
        /// </summary>
        /// <returns></returns>
        public ConsList<E> Reverse()
        {
            ConsList<E> reversed = new ConsList<E>(first);
            ConsList<E> l = rest;
            while (l != null)
            {
                reversed = new ConsList<E>(l.first, reversed);
                l = l.rest;
            }
            return reversed;
        }

        #region IEnumerable<E> Members

        public IEnumerator<E> GetEnumerator()
        {
            return new SimpleListEnumerator<E>(this);
        }
        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private class SimpleListEnumerator<E1> : IEnumerator<E1>
        {
            ConsList<E1> tl;
            bool initialized;

            internal SimpleListEnumerator(ConsList<E1> tl)
            {
                this.tl = tl;
            }


            #region IEnumerator<E1> Members

            public E1 Current
            {
                get
                {
                    if (tl == null || !initialized)
                        throw new InvalidOperationException("Current is undefined");
                    return tl.first;
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                tl = null;
            }

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public bool MoveNext()
            {
                if (tl == null)
                    return false;

                if (!initialized)
                {
                    initialized = true;
                    return true;
                }

                tl = tl.rest;
                return (tl != null);
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            #endregion
        }


        //public static IEnumerable<ConsList<Tuple<bool, E>>> GenerateChoiceLists(ConsList<E> tl)
        //{
        //    if (tl != null)
        //        foreach (var tl1 in GenerateChoiceLists(tl.rest))
        //        {
        //            yield return new ConsList<Tuple<bool, E>>(new Tuple<bool, E>(true, tl.first), tl1);
        //            yield return new ConsList<Tuple<bool, E>>(new Tuple<bool, E>(false, tl.first), tl1);
        //        }
        //    else
        //        yield return null;
        //}
    }

    /// <summary>
    /// A simple list of elements of type E. 
    /// </summary>
    public class SimpleList<E> : IEnumerable<E>
    {
        readonly  int k;
        readonly E last;
        readonly SimpleList<E> butlast;

        SimpleList()
        {
            this.butlast = null;
            this.k = 0;
            this.last = default(E);
        }

        static SimpleList<E> empty = new SimpleList<E>();
        /// <summary>
        /// Empty simple list.
        /// </summary>
        public static SimpleList<E> Empty
        {
            get { return empty; }
        }

        /// <summary>
        /// Returns true iff the list is empty.
        /// </summary>
        public bool IsNonempty
        {
            get { return k > 0; }
        }

        /// <summary>
        /// Returns the number of elements in this list.
        /// </summary>
        public int Count
        {
            get { return k; }
        }

        SimpleList(E e, SimpleList<E> r)
        {
            this.last = e;
            this.butlast = r;
            this.k = 1 + r.k;
        }

        /// <summary>
        /// Returns the position of the last occurrence of elem, or -1 if elem does not occur in the list.
        /// Indexing starts with 0.
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public int IndexOf(E elem)
        {
            if (k == 0)
                return -1;
            else if (object.Equals(last, elem))
                return k - 1;
            else
                return butlast.IndexOf(elem);
        }

        /// <summary>
        /// Returns true iff the list contains elem.
        /// </summary>
        public bool Contains(E elem)
        {
            return IndexOf(elem) >= 0;
        }

        /// <summary>
        /// Creates a new simple list by appending elem at the end of this list.
        /// </summary>
        /// <param name="elem">element to append</param>
        public SimpleList<E> Append(E elem)
        {
            var res = new SimpleList<E>(elem, this);
            return res;
        }

        /// <summary>
        /// Creates a new simple list by appending elems at the end of this list.
        /// </summary>
        /// <param name="elems">elements to append</param>
        public SimpleList<E> Append(params E[] elems)
        {
            var res = this;
            for (int i = 0; i < elems.Length; i++)
                res = new SimpleList<E>(elems[i], res);
            return res;
        }

        /// <summary>
        /// Creates a new simple list by appending elems at the end of this list.
        /// </summary>
        /// <param name="elems">elements to append</param>
        public SimpleList<E> Append(IEnumerable<E> elems)
        {
            var res = this;
            foreach (var e in elems)
                res = new SimpleList<E>(e, res);
            return res;
        }

        /// <summary>
        /// Convert the list into an array
        /// </summary>
        public E[] ToArray()
        {
            var res = new E[k];
            int i = k - 1;
            var list = this;
            while (list.butlast != null)
            {
                res[i--] = list.last;
                list = list.butlast;
            }
            return res;
        }

        public override string ToString()
        {
            return new Sequence<E>(ToArray()).ToString();
        }

        public IEnumerator<E> GetEnumerator()
        {
            IEnumerable<E> ie = (IEnumerable<E>)ToArray();
            return ie.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            System.Collections.IEnumerable ie = ToArray();
            return ie.GetEnumerator();
        }

        /// <summary>
        /// Returns the last element. Returns default(E) if the list is empty.
        /// </summary>
        public E Last
        {
            get { return last; }
        }

        /// <summary>
        /// Returns the list without the last element. Returns null if the list is empty.
        /// </summary>
        public SimpleList<E> Butlast
        {
            get { return butlast; }
        }
    }

    public class SimpleStack<E> : IEnumerable<E>
    {

        ConsList<E> elems = null;

        /// <summary>
        /// Creates a new empty stack.
        /// </summary>
        public SimpleStack(){}

        /// <summary>
        /// Returns true iff the stack is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return elems == null; }
        }

        /// <summary>
        /// Returns true iff the stack is nonempty.
        /// </summary>
        public bool IsNonempty
        {
            get { return elems != null; }
        }

        /// <summary>
        /// Pushes a new element to the top of the stack.
        /// </summary>
        /// <param name="elem">element to be pushed</param>
        public void Push(E elem)
        {
            elems = new ConsList<E>(elem, elems);
        }

        /// <summary>
        /// Pushes all elements to the top of the stack.
        /// </summary>
        /// <param name="newelems">elements to be pushed</param>
        public void PushAll(IEnumerable<E> newelems)
        {
            foreach (E elem in newelems)
                elems = new ConsList<E>(elem, elems);
        }

        /// <summary>
        /// Pops the top element of the stack and returns it.
        /// </summary>
        /// <returns></returns>
        public E Pop()
        {
            var e = elems.First;
            elems = elems.Rest;
            return e;
        }

        private static IEnumerable<E> emptyEnumerable = (IEnumerable<E>)new E[] { };

        public IEnumerator<E> GetEnumerator()
        {
            if (elems == null)
                return emptyEnumerable.GetEnumerator();
            else
                return elems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (elems == null)
                return emptyEnumerable.GetEnumerator();
            else
                return elems.GetEnumerator();
        }
    }
}

