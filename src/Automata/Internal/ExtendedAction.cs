using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents the action of a move of a Extended Symbolic Finite Transducer (ESFT).
    /// </summary>
    internal class ExtendedAction<TERM>
    {
        internal TERM guard;
        internal TERM[] yields;
        internal int lookahead;

        /// <summary>
        /// Gets the guard of the rule.
        /// </summary>
        public TERM Guard
        {
            get { return guard; }
        }

        /// <summary>
        /// Gets the array of yielded outputs, that is the empty array if there are no outputs.
        /// </summary>
        public TERM[] Yields
        {
            get { return yields; }
        }

        /// <summary>
        /// Gets the lookahead size of the Guard
        /// </summary>
        public int Lookahead
        {
            get { return lookahead; }
        }

        /// <summary>
        /// Must not be used.
        /// </summary>
        ExtendedAction() { }

        /// <summary>
        /// Creates a new rule
        /// </summary>
        /// <param name="final">true iff the rule represents a final output</param>
        /// <param name="guard">predicate over input and registers, or registers only when the rule is a final output</param>
        /// <param name="yields">output elements yielded by the rule</param>
        ExtendedAction(TERM guard, TERM[] yields)
        {
            this.guard = guard;
            this.yields = yields;
        }

        internal ExtendedAction(int lookahead, TERM guard, TERM[] yields)
        {
            this.guard = guard;
            this.yields = yields;
            this.lookahead = lookahead;
        }


        /// <summary>
        /// Creates a new rule representing a guarded register update.
        /// </summary>
        /// <param name="guard">predicate over input and registers</param>
        /// <param name="update">register update</param>
        /// <param name="yields">(possibly empty) array of output terms yielded by the rule</param>
        static public ExtendedAction<TERM> Mk(TERM guard, params TERM[] yields)
        {
            return new ExtendedAction<TERM>(guard, yields);
        }

    }
}
