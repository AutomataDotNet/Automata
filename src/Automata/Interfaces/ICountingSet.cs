using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    /// <summary>
    /// A bounded set on integers that supports incrementing all elements adding 0 and 1.
    /// </summary>
    public interface ICountingSet
    {
        /// <summary>
        /// What the maximum value in the set is allowed to be.
        /// </summary>
        int UpperBound { get; }

        /// <summary>
        /// True iff the set is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// True iff the set is a singleton set.
        /// </summary>
        bool IsSingleton { get; }

        /// <summary>
        /// Gets the minimum value in the set. Set must be nonempty.
        /// </summary>
        int Min { get; }

        /// <summary>
        /// Gets the maximum value in the set. Set must be nonempty.
        /// </summary>
        int Max { get; }

        /// <summary>
        /// Set the set to the value [0].
        /// </summary>
        void Set0();

        /// <summary>
        /// Set the set to the value [1].
        /// </summary>
        void Set1();

        /// <summary>
        /// Increment all values in the set.
        /// If Max becomes greater than UpperBound then remove it.
        /// </summary>
        void Incr();

        /// <summary>
        /// Push 0 into the set.
        /// </summary>
        void Push0();

        /// <summary>
        /// Empty the set.
        /// </summary>
        void Clear();

        /// <summary>
        /// Increment all values in the set and push 0 into the set.
        /// If Max becomes greater than UpperBound then remove it.
        /// </summary>
        void IncrPush0();

        /// <summary>
        /// Increment all values in the set and push 1 into the set.
        /// If Max becomes greater than UpperBound then remove it.
        /// </summary>
        void IncrPush1();

        /// <summary>
        /// Increment all values in the set and push 0 and 1 into the set.
        /// If Max becomes greater than UpperBound then remove it.
        /// </summary>
        void IncrPush01();
    }
}
