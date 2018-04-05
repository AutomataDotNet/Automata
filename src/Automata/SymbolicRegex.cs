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
        SymbolicRegexBuilder<S> builder;

        /// <summary>
        /// Underlying solver
        /// </summary>
        public ICharAlgebra<S> Solver
        {
            get { return builder.solver; }
        }

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

        /// <summary>
        /// AST node of a symbolic regex
        /// </summary>
        /// <param name="builder">the buiolder</param>
        /// <param name="kind">what kind of node</param>
        /// <param name="left">left child</param>
        /// <param name="right">right child</param>
        /// <param name="lower">lower bound of a loop</param>
        /// <param name="upper">upper boubd of a loop</param>
        /// <param name="set">sinlgelton set</param>
        /// <param name="toString"></param>
        /// <param name="iteCond"></param>
        internal SymbolicRegex(SymbolicRegexBuilder<S> builder, SymbolicRegexKind kind, SymbolicRegex<S> left, SymbolicRegex<S> right, int lower, int upper, S set, SymbolicRegex<S> iteCond)
        {
            this.builder = builder;
            this.kind = kind;
            this.left = left;
            this.right = right;
            this.lower = lower;
            this.upper = upper;
            this.set = set;
            this.iteCond = iteCond;
        }

        internal static SymbolicRegex<S> MkSingleton(SymbolicRegexBuilder<S> builder, S set)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Singleton, null, null, -1, -1, set,  null);
        }

        internal static SymbolicRegex<S> MkStartAnchor(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Epsilon, null, null, -2, -1, default(S), null);
        }

        internal static SymbolicRegex<S> MkEndAnchor(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Epsilon, null, null, -1, -2, default(S), null);
        }

        internal static SymbolicRegex<S> MkEolAnchor(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Epsilon, null, null, -1, -3, default(S), null);
        }

        internal static SymbolicRegex<S> MkBolAnchor(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Epsilon, null, null, -3, -1, default(S), null);
        }

        internal static SymbolicRegex<S> MkEpsilon(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Epsilon, null, null, -1, -1, default(S), null);
        }

        internal static SymbolicRegex<S> MkLoop(SymbolicRegexBuilder<S> builder, SymbolicRegex<S> body, int lower, int upper)
        {
            if (lower < 0 || upper < lower)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Loop, body, null, lower, upper, default(S), null);
        }

        internal static SymbolicRegex<S> MkOr(SymbolicRegexBuilder<S> builder, SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Or, left, right, -1, -1, default(S), null);
        }

        internal static SymbolicRegex<S> MkConcat(SymbolicRegexBuilder<S> builder, SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Concat, left, right, -1, -1, default(S), null);
        }

        internal static SymbolicRegex<S> MkIfThenElse(SymbolicRegexBuilder<S> builder, SymbolicRegex<S> cond, SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.IfThenElse, left, right, -1, -1, default(S), cond);
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
                    return builder.solver.PrettyPrint(set);
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

        /// <summary>
        /// Produce a string representation of the symbolic regex with explicit start and end anchors. 
        /// </summary>
        public string ToStringWithAnchors()
        {
            return string.Format("^({0})$", this.ToString());
        }

        /// <summary>
        /// Transform the symbolic regex so that all singletons have been intersected with the given predicate pred. 
        /// </summary>
        public SymbolicRegex<S> Restrict(S pred)
        {
            switch (kind)
            {
                case SymbolicRegexKind.Epsilon:
                    return this;
                case SymbolicRegexKind.Singleton:
                    {
                        var newset = builder.solver.MkAnd(this.set, pred);
                        if (this.set.Equals(newset))
                            return this;
                        else
                            return builder.MkSingleton(newset);
                    }
                case SymbolicRegexKind.Loop:
                    {
                        var body = this.left.Restrict(pred);
                        if (body == this.left)
                            return this;
                        else
                            return builder.MkLoop(body, this.lower, this.upper);
                    }
                case SymbolicRegexKind.Concat:
                    {
                        var first = this.left.Restrict(pred);
                        var second = this.right.Restrict(pred);
                        if (first == this.left && second == this.right)
                            return this;
                        else
                            return builder.MkConcat(first, second);
                    }
                case SymbolicRegexKind.Or:
                    {
                        var first = this.left.Restrict(pred);
                        var second = this.right.Restrict(pred);
                        if (first == this.left && second == this.right)
                            return this;
                        else
                            return builder.MkOr(first, second);
                    }
                default: //ITE 
                    {
                        var truecase = this.left.Restrict(pred);
                        var falsecase = this.right.Restrict(pred);
                        var cond = this.iteCond.Restrict(pred);
                        if (truecase == this.left && falsecase == this.right && cond == this.iteCond)
                            return this;
                        else
                            return builder.MkIfThenElse(cond, truecase, falsecase);
                    }
            }
        }
    }
}
