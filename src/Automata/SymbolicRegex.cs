using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{
    /// <summary>
    /// Kinds of symbolic regexes
    /// </summary>
    public enum SymbolicRegexKind
    {
        Epsilon,
        Singleton,
        Choice,
        Concat, 
        Loop
    }

    /// <summary>
    /// Base class for symbolic regexes.
    /// </summary>
    public abstract class SymbolicRegex
    {
        /// <summary>
        /// Gets the kind of the regex
        /// </summary>
        abstract public SymbolicRegexKind Kind { get; }

        /// <summary>
        /// Number of unnested alternative branches if this is a choice node. 
        /// If this is not a choice node then the value is 1.
        /// </summary>
        virtual public int ChoiceCount
        {
            get { return 1; }
        }
    }

    /// <summary>
    /// Epsilon regular expression, accepts the empty sequence.
    /// </summary>
    public class SymbolicRegexEpsilon : SymbolicRegex
    {
        /// <summary>
        /// Returns the kind SymbolicRegexKind.Epsilon
        /// </summary>
        public override SymbolicRegexKind Kind
        {
            get
            {
                return SymbolicRegexKind.Epsilon;
            }
        }

        public override string ToString()
        {
            return "()";
        }

        internal SymbolicRegexEpsilon() { }
    }

    /// <summary>
    /// Set or singleton sequence of elements
    /// </summary>
    /// <typeparam name="S">set type</typeparam>
    /// <typeparam name="T">element type</typeparam>
    public class SymbolicRegexSingleton<S> : SymbolicRegex
    {
        /// <summary>
        /// Returns the kind SymbolicRegexKind.Singleton
        /// </summary>
        public override SymbolicRegexKind Kind
        {
            get
            {
                return SymbolicRegexKind.Singleton;
            }
        }

        S set;
        Func<S, string> toString;

        /// <summary>
        /// Set of elements
        /// </summary>
        public S Set
        {
            get { return set; }
        }

        /// <summary>
        /// Creates a regex singleton sequence
        /// </summary>
        /// <param name="set">set of elements</param>
        /// <param name="toString">funtion that displays the set</param>
        internal SymbolicRegexSingleton(S set, Func<S,string> toString)
        {
            this.set = set;
            this.toString = toString;
        }

        /// <summary>
        /// Display the set as a string
        /// </summary>
        public override string ToString()
        {
            return this.toString(set);
        }
    }

    /// <summary>
    /// Choice node between two regexes
    /// </summary>
    public class SymbolicRegexChoice : SymbolicRegex
    {
        SymbolicRegex first;
        SymbolicRegex second;

        /// <summary>
        /// Returns the kind SymbolicRegexKind.Choice
        /// </summary>
        public override SymbolicRegexKind Kind
        {
            get
            {
                return SymbolicRegexKind.Choice;
            }
        }

        /// <summary>
        /// First element
        /// </summary>
        public SymbolicRegex First
        {
            get { return this.first; }
        }

        /// <summary>
        /// Second element
        /// </summary>
        public SymbolicRegex Second
        {
            get { return this.second; }
        }

        /// <summary>
        /// Create choice of first or second
        /// </summary>
        internal SymbolicRegexChoice(SymbolicRegex first, SymbolicRegex second)
        {
            this.first = first;
            this.second = second;
        }

        /// <summary>
        /// Equals First.ChoiceCount + Second.ChoiceCount
        /// </summary>
        override public int ChoiceCount
        {
            get { return first.ChoiceCount + second.ChoiceCount; }
        }

        /// <summary>
        /// Display the choice
        /// </summary>
        public override string ToString()
        {
            return first.ToString() + "|" + second.ToString();
        }
    }

    /// <summary>
    /// Concatenation of two regexes
    /// </summary>
    public class SymbolicRegexConcat : SymbolicRegex
    {
        SymbolicRegex first;
        SymbolicRegex second;

        /// <summary>
        /// Returns the kind SymbolicRegexKind.Concat
        /// </summary>
        public override SymbolicRegexKind Kind
        {
            get
            {
                return SymbolicRegexKind.Concat;
            }
        }


        /// <summary>
        /// First element
        /// </summary>
        public SymbolicRegex First
        {
            get { return this.first; }
        }

        /// <summary>
        /// Second element
        /// </summary>
        public SymbolicRegex Second
        {
            get { return this.second; }
        }

        /// <summary>
        /// Create sequence of first followed by second
        /// </summary>
        internal SymbolicRegexConcat(SymbolicRegex first, SymbolicRegex second)
        {
            this.first = first;
            this.second = second;
        }

        /// <summary>
        /// Display the concatenation
        /// </summary>
        public override string ToString()
        {
            string a = first.ToString();
            string b = second.ToString();
            if (first.ChoiceCount > 1)
                a = "(" + a + ")";
            if (second.ChoiceCount > 1)
                b = "(" + b + ")";
            return a + b;
        }
    }

    /// <summary>
    /// Generalized star operator with upper and lower iteration bounds
    public class SymbolicRegexLoop : SymbolicRegex
    {
        SymbolicRegex body;
        int lower;
        int upper;

        /// <summary>
        /// Returns the kind SymbolicRegexKind.Loop
        /// </summary>
        public override SymbolicRegexKind Kind
        {
            get
            {
                return SymbolicRegexKind.Loop;
            }
        }


        /// <summary>
        /// The body of the loop
        /// </summary>
        public SymbolicRegex Body
        {
            get { return body; }
        }

        /// <summary>
        /// Lower bound of the loop
        /// </summary>
        public int LowerBound { get { return lower; } }

        /// <summary>
        /// Upper bound of the loop
        /// </summary>
        public int UpperBound {  get { return upper; } }


        /// <summary>
        /// Creates a loop
        /// </summary>
        /// <param name="regex">the body of the loop</param>
        /// <param name="lower">lower bound on the number of iterations (default 0)</param>
        /// <param name="upper">upper bound on the number of iterations (default int.MaxValue)</param>
        internal SymbolicRegexLoop(SymbolicRegex regex, int lower = 0, int upper = int.MaxValue)
        {
            if (lower < 0 || upper < lower)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            this.body = regex;
            this.lower = lower;
            this.upper = upper;
        }

        /// <summary>
        /// Returns true iff lower bound is 0 and upper bound is max
        /// </summary>
        public bool IsStar
        {
            get
            {
                return lower == 0 && upper == int.MaxValue;
            }
        }

        /// <summary>
        /// Returns true iff lower bound is 1 and upper bound is max
        /// </summary>
        public bool IsPlus
        {
            get
            {
                return lower == 1 && upper == int.MaxValue;
            }
        }

        /// <summary>
        /// Display the loop
        /// </summary>
        public override string ToString()
        {
            string s = body.ToString();
            if (body.ChoiceCount > 1)
                s = "(" + s + ")";
            if (IsStar)
                return s + "*";
            else if (IsPlus)
                return s + "+";
            else
                return string.Format("{0}{{{1},{2}}}", s, this.lower, this.upper);
        }
    }
}
