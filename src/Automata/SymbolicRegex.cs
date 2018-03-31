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
        Or,
        Concat,
        Loop,
        IfThenElse
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
        /// Number of unnested alternative branches if this is an or-node. 
        /// If this is not an or-node then the value is 1.
        /// </summary>
        public int OrCount
        {
            get
            {
                if (kind == SymbolicRegexKind.Or)
                    return left.OrCount + right.OrCount;
                else
                    return 1;
            }
        }

        SymbolicRegex<S> left;
        /// <summary>
        /// Left child of a binary node (the child of a unary node, the true-branch of an Ite-node)
        /// </summary>
        public SymbolicRegex<S> Left
        {
            get { return left; }
        }

        SymbolicRegex<S> right;
        /// <summary>
        /// Right child of a binary node (the false-branch of an Ite-node)
        /// </summary>
        public SymbolicRegex<S> Right
        {
            get { return right; }
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


        SymbolicRegex<S> iteCond;
        /// <summary>
        /// IfThenElse condition
        /// </summary>
        public SymbolicRegex<S> IteCond
        {
            get { return iteCond; }
        }

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
        public bool IsQM
        {
            get
            {
                return lower == 0 && upper == 1;
            }
        }

        internal bool IsSartAnchor
        {
            get { return this.lower == -2; }
        }

        internal bool IsBolAnchor
        {
            get { return this.lower == -3; }
        }

        internal bool IsEndAnchor
        {
            get { return this.upper == -2; }
        }

        internal bool IsEolAnchor
        {
            get { return this.upper == -3; }
        }

        internal bool IsAnchor
        {
            get { return IsSartAnchor || IsBolAnchor || IsEndAnchor || IsEolAnchor; }
        }

        private SymbolicRegex(SymbolicRegexKind kind, SymbolicRegex<S> left, SymbolicRegex<S> right, int lower, int upper, S set, Func<S, string> toString, SymbolicRegex<S> iteCond)
        {
            this.kind = kind;
            this.left = left;
            this.right = right;
            this.lower = lower;
            this.upper = upper;
            this.set = set;
            this.toString = toString;
            this.iteCond = iteCond;
        }
        internal static SymbolicRegex<S> MkSingleton(S set, Func<S, string> toString)
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Singleton, null, null, -1, -1, set, toString, null);
        }

        internal static SymbolicRegex<S> MkStartAnchor()
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Epsilon, null, null, -2, -1, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkEndAnchor()
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Epsilon, null, null, -1, -2, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkEolAnchor()
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Epsilon, null, null, -1, -3, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkBolAnchor()
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Epsilon, null, null, -3, -1, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkEpsilon()
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Epsilon, null, null, -1, -1, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkLoop(SymbolicRegex<S> body, int lower, int upper)
        {
            if (lower < 0 || upper < lower)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            return new SymbolicRegex<S>(SymbolicRegexKind.Loop, body, null, lower, upper, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkOr(SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Or, left, right, -1, -1, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkConcat(SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.Concat, left, right, -1, -1, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkIfThenElse(SymbolicRegex<S> cond, SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return new SymbolicRegex<S>(SymbolicRegexKind.IfThenElse, left, right, -1, -1, default(S), null, cond);
        }

        /// <summary>
        /// Produce a string representation of the symbolic regex. 
        /// </summary>
        public override string ToString()
        {
            switch (kind)
            {
                case SymbolicRegexKind.Epsilon:
                    if (IsSartAnchor || IsBolAnchor)
                        return "^";
                    else if (IsEndAnchor || IsEolAnchor)
                        return "$";
                    else
                        return "()";
                case SymbolicRegexKind.Singleton:
                    return toString(set);
                case SymbolicRegexKind.Loop:
                    string s = left.ToString();
                    if (left.OrCount > 1 || left.kind == SymbolicRegexKind.Concat)
                        s = "(" + s + ")";
                    if (IsStar)
                        return s + "*";
                    else if (IsPlus)
                        return s + "+";
                    else if (IsQM)
                        return s + "?";
                    else
                        return string.Format("{0}{{{1},{2}}}", s, this.lower, this.upper);
                case SymbolicRegexKind.Concat:
                    string a = left.ToString();
                    string b = right.ToString();
                    if (left.OrCount > 1)
                        a = "(" + a + ")";
                    if (right.OrCount > 1)
                        b = "(" + b + ")";
                    return a + b;
                case SymbolicRegexKind.Or:
                    return left.ToString() + "|" + right.ToString();
                default: //ITE 
                    return string.Format("(?({0}){1}|{2})", iteCond, left, right);
            }
        }

    }
}
