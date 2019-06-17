using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    /// <summary>
    /// Implements a bounded set on integers that supports incermenting all elements adding 0 and 1.
    /// </summary>
    public class BasicCountingSet : ICountingSet
    {
        /// <summary>
        /// Upper limit on what the maximum value in the set can be.
        /// </summary>
        readonly int upperBound;

        /// <summary>
        /// Upper limit on what the maximum value in the set can be.
        /// </summary>
        public int UpperBound
        {
            get
            {
                return upperBound;
            }
        }

        ConsList<int> queue;
        ConsList<int> queue_end;
        int offset;

        /// <summary>
        /// True iff the counting set is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return queue == null;
            }
        }

        /// <summary>
        /// True iff the counting set is a singleton set.
        /// </summary>
        public bool IsSingleton
        {
            get
            {
                return queue != null && queue == queue_end;
            }
        }

        /// <summary>
        /// Create an empty counting set, max is the maximum element size, max must be at least 2.
        /// </summary>
        public BasicCountingSet(int max)
        {
            if (max < 2)
                throw new ArgumentOutOfRangeException("max", "value must be at least 2");
            this.upperBound = max;
            this.queue = null;
            this.queue_end = null;
            this.offset = 0;
        }

        /// <summary>
        /// Gets the maximum value in the set. Set must be nonempty.
        /// </summary>
        public int Max
        {
            get
            {
                return offset - queue.First;
            }
        }

        /// <summary>
        /// Gets the minimum value in the set. Set must be nonempty.
        /// </summary>
        public int Min
        {
            get
            {
                return offset - queue_end.First;
            }
        }

        /// <summary>
        /// Set the counting set to the value [0].
        /// </summary>
        public void Set0()
        {
            this.queue = new ConsList<int>(0);
            this.queue_end = this.queue;
            this.offset = 0;
        }

        /// <summary>
        /// Set the counting set to the value [1].
        /// </summary>
        public void Set1()
        {
            this.queue = new ConsList<int>(0);
            this.queue_end = this.queue;
            this.offset = 1;
        }

        /// <summary>
        /// Increment all values in the set.
        /// If Max becomes greater than UpperBound then remove it.
        /// </summary>
        public void Incr()
        {
            if (queue != null)
            {
                if (Max == upperBound)
                {
                    //remove the first element
                    queue = queue.Rest;
                    //queue may have become empty
                    if (queue == null)
                    {
                        queue_end = null;
                        offset = 0;
                    }
                    else
                        offset += 1;
                }
                else
                    offset += 1;
            }
        }

        /// <summary>
        /// Push 0 into the set.
        /// </summary>
        public void Push0()
        {
            if (queue == null)
            {
                queue = new ConsList<int>(0);
                queue_end = queue;
                offset = 0;
            }
            else
            {
                //check first that 0 is not alredy in the set
                if (queue_end.First != offset)
                {
                    var last = new ConsList<int>(offset);
                    queue_end.Rest = last;
                    queue_end = last;
                }
            }
        }

        /// <summary>
        /// Empty the set.
        /// </summary>
        public void Clear()
        {
            queue = null;
            queue_end = null;
            offset = 0;
        }

        /// <summary>
        /// Increment all values in the set and push 0 into the set.
        /// If Max becomes greater than UpperBound then remove it.
        /// </summary>
        public void IncrPush0()
        {
            Incr();
            Push0();
            //if (queue == null)
            //{
            //    queue = new ConsList<int>(0);
            //    queue_end = queue;
            //    offset = 0;
            //}
            //else
            //{
            //    if (Max == upperBound)
            //        queue = queue.Rest; //remove the max
            //    //queue may have become empty
            //    if (queue == null)
            //    { 
            //        queue = new ConsList<int>(0);
            //        queue_end = queue;
            //        offset = 0;
            //    }
            //    else
            //    {
            //        //there was more than one element, 
            //        offset += 1;
            //        //append the value that represents 0 at the end
            //        //observe that all values at this point are strictly less
            //        //than offset so offset - offset == 0 is a new 0 element
            //        var last = new ConsList<int>(offset);
            //        queue_end.Rest = last;
            //        queue_end = last;
            //    }
            //}
        }

        /// <summary>
        /// Increment all values in the set and push 1 into the set.
        /// If Max becomes greater than UpperBound then remove it.
        /// </summary>
        public void IncrPush1()
        {
            Push0();
            Incr();
            //if (queue == null)
            //{
            //    queue = new ConsList<int>(0);
            //    queue_end = queue;
            //    offset = 1;
            //}
            //else
            //{
            //    if (Max == upperBound)
            //        queue = queue.Rest; //remove the max
            //    //queue may have become empty
            //    if (queue == null)
            //    {
            //        queue = new ConsList<int>(0);
            //        queue_end = queue;
            //        offset = 1;
            //    }
            //    else
            //    {
            //        //more than one element, 
            //        offset += 1;
            //        //append the value that represents 1 at the end
            //        //unless 1 is already there
            //        //all elements are at least 1 at this point
            //        if (Min > 1)
            //        {
            //            var last = new ConsList<int>(offset - 1);
            //            queue_end.Rest = last;
            //            queue_end = last;
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Increment all values in the set and push 0 and 1 into the set.
        /// If Max becomes greater than UpperBound then remove it.
        /// </summary>
        public void IncrPush01()
        {
            Push0();
            Incr();
            Push0();
            //if (queue == null)
            //{
            //    //set to [1,0]
            //    queue_end = new ConsList<int>(1);
            //    queue = new ConsList<int>(0, queue_end);
            //    offset = 1;
            //}
            //else
            //{
            //    if (Max == upperBound)
            //        queue = queue.Rest; //remove the max

            //    //queue may have become empty
            //    if (queue == null)
            //    {
            //        //set to [1,0]
            //        queue_end = new ConsList<int>(1);
            //        queue = new ConsList<int>(0, queue_end);
            //        offset = 1;
            //    }
            //    else
            //    {
            //        offset += 1;
            //        //observe that 1 may already be present, 
            //        //in which case only add 0
            //        if (Min == 1)
            //        {
            //            //only add 0
            //            var zero = new ConsList<int>(offset);
            //            queue_end.Rest = zero;
            //            queue_end = zero;
            //        }
            //        else
            //        {
            //            //add both 1 and 0
            //            var zero = new ConsList<int>(offset);
            //            var one = new ConsList<int>(offset - 1, zero);
            //            queue_end.Rest = one;
            //            queue_end = zero;
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Returns decimal representation of the elements in the set in decreasing order.
        /// </summary>
        public override string ToString()
        {
            var s = "[";
            var next = queue;
            while (next != null)
            {
                if (s != "[")
                    s += ",";
                s += (offset - next.First).ToString();
                next = next.Rest;
            }
            s += "]";
            return s;
        }
    }
}
