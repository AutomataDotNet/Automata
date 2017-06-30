using System;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents a move of a extended symbolic finite automaton.
    /// Can either go to a state or be final
    /// </summary>
    /// <typeparam name="LAB">the type of the labels of moves</typeparam>
    internal abstract class ExtendedMove<TERM>
    {
        /// <summary>
        /// Source state of the move
        /// </summary>
        public readonly int SourceState;

        /// <summary>
        /// Condition of the move
        /// </summary>
        public readonly ExtendedAction<TERM> action;

        public bool isFinal;

        /// <summary>
        /// Move function
        /// </summary>
        /// <param name="sourceState"></param>
        /// <param name="condition"></param>
        public ExtendedMove(int sourceState, ExtendedAction<TERM> action)
        {
            this.SourceState = sourceState;
            this.action = action;
        }        
    }

    /// <summary>
    /// Represents a final move of a ESFT.
    /// </summary>
    /// <typeparam name="LAB">the type of the labels of moves</typeparam>
    internal class ExtendedFinalMove<TERM> : ExtendedMove<TERM>
    {
        /// <summary>
        /// Final move
        /// </summary>
        /// <param name="sourceState"></param>
        /// <param name="condition"></param>
        public ExtendedFinalMove(int sourceState, ExtendedAction<TERM> action):
            base (sourceState,action)
        {
            isFinal = true;
        }

        /// <summary>
        /// Returns true if obj is a final move with the same source state, and condition.
        /// </summary>
        public override bool Equals(object obj)
        {

            if (!(obj is ExtendedFinalMove<TERM>))
                return false;
            ExtendedFinalMove<TERM> t = (ExtendedFinalMove<TERM>)obj;
            return (t.SourceState == SourceState && t.action.Equals(action));
        }

        /// <summary>
        /// override HahsCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return SourceState + (object.Equals(action, default(TERM)) ? 0 : action.GetHashCode());
        }

        /// <summary>
        /// Override to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "(" + SourceState + "," + (object.Equals(action, default(TERM)) ? "" : action + ",") + "o)";
        }
    }

    /// <summary>
    /// Represents a non-final move of a ESFT.
    /// </summary>
    /// <typeparam name="LAB">the type of the labels of moves</typeparam>
    internal class ExtendedNormalMove<TERM> : ExtendedMove<TERM>
    {
        /// <summary>
        /// Source state of the move
        /// </summary>
        public readonly int TargetState;

        /// <summary>
        /// Final move
        /// </summary>
        /// <param name="sourceState"></param>
        /// <param name="condition"></param>
        internal ExtendedNormalMove(int sourceState, int TargetState, ExtendedAction<TERM> action) :
            base(sourceState, action)
        {
            this.TargetState = TargetState;
            isFinal = false;
        }

        /// <summary>
        /// Returns true if obj is a final move with the same source state, and condition.
        /// </summary>
        public override bool Equals(object obj)
        {

            if (!(obj is ExtendedNormalMove<TERM>))
                return false;
            ExtendedNormalMove<TERM> t = (ExtendedNormalMove<TERM>)obj;
            return (t.SourceState == SourceState && t.TargetState == TargetState && t.action.Equals(action));
        }

        /// <summary>
        /// override HahsCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return SourceState + 2*TargetState + (object.Equals(action, default(TERM)) ? 0 : action.GetHashCode());
        }

        /// <summary>
        /// Override to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "(" + SourceState + "," + (object.Equals(action, default(TERM)) ? "" : action + ",") + TargetState+ ")";
        }
    }
}
