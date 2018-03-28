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
    /// Represents an AST node of a symbolic regex.
    /// </summary>
    public class SymbolicRegex<S>
    {
        SymbolicRegexKind kind;
        /// <summary>
        /// Gets the kind of the regex
        /// </summary>
        public SymbolicRegexKind Kind
        {
            get { return kind; }
        }

        /// <summary>
        /// Number of unnested alternative branches if this is a choice node. 
        /// If this is not a choice node then the value is 1.
        /// </summary>
        public int ChoiceCount
        {
            get
            {
                if (kind == SymbolicRegexKind.Choice)
                    return left.ChoiceCount + right.ChoiceCount;
                else
                    return 1;
            }
        }

        SymbolicRegex<S> left;
        /// <summary>
        /// Left child of a binary node or the child of a unary node
        /// </summary>
        public SymbolicRegex<S> Left
        {
            get { return left; }
        }

        SymbolicRegex<S> right;
        /// <summary>
        /// Right child of a binary node
        /// </summary>
        public SymbolicRegex<S> Right
        {
            get{ return right; }
        }

        int lower; 
        /// <summary>
        /// The lower bound of a loop
        /// </summary>
        public int LowerBound
        {
            get
            {
                return lower;
            }
        }

        int upper;
        /// <summary>
        /// The upper bound of a loop
        /// </summary>
        public int UpperBound
        {
            get
            {
                return upper;
            }
        }

        S set;
        /// <summary>
        /// The set of a singleton
        /// </summary>
        public S Set
        {
            get
            {
                return set;
            }
        }
        Func<S, string> toString;

        /// <summary>
        /// Returns true iff this is a loop whose lower bound is 0 and upper bound is max
        /// </summary>
        public bool IsStar
        {
            get
            {
                return lower == 0 && upper == int.MaxValue;
            }
        }

        /// <summary>
        /// Returns true iff this is a loop whose lower bound is 1 and upper bound is max
        /// </summary>
        public bool IsPlus
        {
            get
            {
                return lower == 1 && upper == int.MaxValue;
            }
        }

        /// <summary>
        /// Returns true iff this is a loop whose lower bound is 0 and upper bound is 1
        /// </summary>
        public bool IsOptional
        {
            get
            {
                return lower == 0 && upper == 1;
            }
        }

        private SymbolicRegex(SymbolicRegexKind kind, SymbolicRegex<S> left, SymbolicRegex<S> right, int lower, int upper, S set, Func<S, string> toString)
        {
            this.kind = kind;
            this.left = left;
            this.right = right;
            this.lower = lower;
            this.upper = upper;
            this.set = set;
            this.toString = toString;
        }

        internal static SymbolicRegex<S> MkSingleton(S set, Func<S, string> toString)
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Singleton, null, null, -1, -1, set, toString);
        }

        internal static SymbolicRegex<S> MkEpsilon()
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Epsilon, null, null, -1, -1, default(S), null);
        }

        internal static SymbolicRegex<S> MkLoop(SymbolicRegex<S> body, int lower, int upper)
        {
            if (lower < 0 || upper < lower)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            return new SymbolicRegex<S>(SymbolicRegexKind.Loop, body, null, lower, upper, default(S), null);
        }

        internal static SymbolicRegex<S> MkChoice(SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Choice, left, right, -1, -1, default(S), null);
        }

        internal static SymbolicRegex<S> MkConcat(SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Concat, left, right, -1, -1, default(S), null);
        }

        public override string ToString()
        {
            switch (kind)
            {
                case SymbolicRegexKind.Epsilon:
                    return "()";
                case SymbolicRegexKind.Singleton:
                    return toString(set);
                case SymbolicRegexKind.Loop:
                    string s = left.ToString();
                    if (left.ChoiceCount > 1)
                        s = "(" + s + ")";
                    if (IsStar)
                        return s + "*";
                    else if (IsPlus)
                        return s + "+";
                    else if (IsOptional)
                        return s + "?";
                    else
                        return string.Format("{0}{{{1},{2}}}", s, this.lower, this.upper);
                case SymbolicRegexKind.Concat:
                    string a = left.ToString();
                    string b = right.ToString();
                    if (left.ChoiceCount > 1)
                        a = "(" + a + ")";
                    if (right.ChoiceCount > 1)
                        b = "(" + b + ")";
                    return a + b;
                default:
                    return left.ToString() + "|" + right.ToString();
            }
        }
    }

}
