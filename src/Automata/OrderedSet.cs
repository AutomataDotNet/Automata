using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents an ordered set of comparable elements.
    /// </summary>
    /// <typeparam name="T">type of element that implements IComparable</typeparam>
    public class OrderedSet<T> : IComparable, IEnumerable<T> where T : IComparable
    {
        public ConsList<T> elements;
        int count = 0;

        /// <summary>
        /// Creates a new ordered set instance.
        /// </summary>
        public OrderedSet()
        {
            this.elements = null;
        }

        public OrderedSet(IEnumerable<T> ordered_list)
        {

            this.elements = ConsList<T>.Create(ordered_list);
            this.count = (elements == null ? 0 : elements.Count());
        }

        public OrderedSet(params T[] ordered_list)
        {

            this.elements = ConsList<T>.Create(ordered_list);
            this.count = (elements == null ? 0 : elements.Count());
        }

        OrderedSet(IEnumerable<T> ordered_list, int count)
        {

            this.elements = ConsList<T>.Create(ordered_list);
            this.count = count;
        }

        /// <summary>
        /// Add the elements in the enumertion into the set, modifies the set in place.
        /// </summary>
        /// <param name="more_elems">elements to add</param>
        public void AddRange(IEnumerable<T> more_elems)
        {
            foreach (var t in more_elems)
                Add(t);
        }

        /// <summary>
        /// Inserts an element to the set in place, by modifying the set.
        /// </summary>
        /// <param name="elem">the element to add</param>
        public void Add(T elem)
        {
            if (elements == null)
            {
                elements = new ConsList<T>(elem);
                count = 1;
            }
            else if (elem.CompareTo(elements.First) < 0)
            {
                elements = new ConsList<T>(elem, elements);
                count += 1;
            }
            else if (elem.CompareTo(elements.First) > 0)
                Insert(elem, elements);
        }
        //keep the list ordered
        private void Insert(T state, ConsList<T> states1)
        {
            if (states1.Rest == null)
            {
                states1.Rest = new ConsList<T>(state);
                count += 1;
            }
            else if (state.CompareTo(states1.Rest.First) < 0)
            {
                states1.Rest = new ConsList<T>(state, states1.Rest);
                count += 1;
            }
            else if (state.CompareTo(states1.Rest.First) > 0)
                Insert(state, states1.Rest);
        }

        /// <summary>
        /// Returns a new set that contains the new elements and the old elements.
        /// </summary>
        public OrderedSet<T> Union(params T[] new_elems)
        {
            var set = new OrderedSet<T>(this, count);
            set.AddRange(new_elems);
            return set;
        }

        /// <summary>
        /// Returns a new set that contains the new elements and the old elements.
        /// </summary>
        public OrderedSet<T> Union(IEnumerable<T> new_elems)
        {
            var set = new OrderedSet<T>(this, count);
            set.AddRange(new_elems);
            return set;
        }

        /// <summary>
        /// Returns true iff obj is a set with equal set of elements.
        /// </summary>
        public override bool Equals(object obj)
        {
            var ts = obj as OrderedSet<T>;
            if (ts == null)
                return false;

            if (count != ts.count)
                return false;

            var states1 = elements;
            var states2 = ts.elements;
            while (states1 != null)
            {
                if (!object.Equals(states1.First, states2.First))
                    return false;
                states1 = states1.Rest;
                states2 = states2.Rest;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int h = 0;
            var next = elements;
            int i = 1;
            while (next != null)
            {
                h += (object.Equals(next.First, default(T)) ? 0 : next.First.GetHashCode()) + i++;
                next = next.Rest;
            }
            return h;
        }

        /// <summary>
        /// Returs some element from a nonempty set, and returns default(T) when the set is empty.
        /// </summary>
        public T SomeElement
        {
            get
            {
                if (elements == null)
                    return default(T);
                else
                    return elements.First;
            }
        }

        /// <summary>
        /// Returns true iff the set has one element.
        /// </summary>
        public bool IsSingleton
        {
            get
            {
                return (elements != null && elements.Rest == null);
            }
        }
        /// <summary>
        /// Returns true iff the set is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return elements == null;
            }
        }
        /// <summary>
        /// Returns true iff the set is either empty or singleton.
        /// </summary>
        public bool IsEmptyOrSingleton
        {
            get
            {
                return (elements == null || elements.Rest == null);
            }
        }


        /// <summary>
        /// Enumerates all elements in the set.
        /// </summary>
        IEnumerable<T> GetElements()
        {
            var elems = elements;
            while (elems != null)
            {
                yield return elems.First;
                elems = elems.Rest;
            }
        }

        public override string ToString()
        {
            if (elements == null)
                return "[]";
            else
                return elements.ToString();
        }

        public int CompareTo(object obj)
        {
            OrderedSet<T> os = obj as OrderedSet<T>;
            if (obj == null)
                return 1;
            if (count < os.count)
                return -1;
            else if (count > os.count)
                return 1;
            else
            {
                var list1 = elements;
                var list2 = os.elements;
                while (list1 != null)
                {
                    int k = list1.First.CompareTo(list2.First);
                    if (k != 0)
                        return k;
                    list1 = list1.Rest;
                    list2 = list2.Rest;
                }
                return 0;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetElements().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetElements().GetEnumerator();
        }
    }
}
