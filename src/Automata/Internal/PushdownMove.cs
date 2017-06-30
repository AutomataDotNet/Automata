using System;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Represents a move of a symbolic pushdown automaton.
    /// The value default(T) is reserved to represent the condition of an epsilon move.
    /// Thus if T is a reference type the condition of an epsilon transition is null.
    /// </summary>
    /// <typeparam name="T">the type of the move condition</typeparam>
    internal class PushdownMove<T> : Move<T>
    {
        //public readonly int SourceState;
        public readonly int PopSymbol;
        //public readonly int TargetState;
        public readonly int[] PushSymbols; //may be null

        ///// <summary>
        ///// Condition is a Boolean term containing at most one variable #0 of character sort
        ///// or null, meaning that the move is an epsilon move
        ///// </summary>
        //public readonly T Condition;

        //string conditionName;

        //public string ConditionName
        //{
        //    get { return conditionName; }
        //}

        //public bool IsEpsilon
        //{
        //    get
        //    {
        //        return Condition == null;
        //    }
        //}

        /// <summary>
        /// Creates a symbolic move with a condition
        /// </summary>
        public PushdownMove(int source, int pop, T condition, int target, params int[] pushes) : base(source,target,condition)
        {
            //this.SourceState = source;
            //this.TargetState = target;
            this.PopSymbol = pop;
            this.PushSymbols = pushes;
            //this.Condition = condition;
            //this.conditionName = (condition == null ? "epsilon" : condition.ToString());
        }

        ///// <summary>
        ///// Creates a symbolic move with a condition and a name for the condition that 
        ///// is used for pretty printing
        ///// </summary>
        //public PushdownMove(int source, int pop, T condition, string conditionName, int target, params int[] pushes)
        //{
        //    this.SourceState = source;
        //    this.TargetState = target;
        //    this.PopSymbol = pop;
        //    this.PushSymbols = pushes;
        //    this.Condition = condition;
        //    this.conditionName = conditionName;
        //}

        /// <summary>
        /// Creates an epsilon move
        /// </summary>
        public PushdownMove(int source, int pop, int target, params int[] pushes) : base(source, target, default(T))
        {
            //this.SourceState = source;
            //this.TargetState = target;
            this.PopSymbol = pop;
            this.PushSymbols = pushes;
            //this.conditionName = "epsilon";
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            sb.Append(SourceState);
            sb.Append(",");
            sb.Append(PopSymbol);
            sb.Append(IsEpsilon ? "" : "," + Label.ToString());
            sb.Append(") --> (");
            sb.Append(TargetState);
            sb.Append(",[");
            if (PushSymbols.Length > 0)
            {
                sb.Append(PushSymbols[0]);
                for (int i = 1; i < PushSymbols.Length; i++)
                {
                    sb.Append(",");
                    sb.Append(PushSymbols[i]);
                }
            }
            sb.Append("])");
            return sb.ToString();
        }
    }
}
