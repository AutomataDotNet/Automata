using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Kinds of symbolic regexes
    /// </summary>
    public enum SymbolicRegexKind
    {
        StartAnchor = 0,
        EndAnchor = 1,
        Epsilon = 2,
        Singleton = 3,
        Or = 4,
        Concat = 5,
        Loop = 6,
        IfThenElse = 7,
        And = 8,
        WatchDog = 9,
        //Sequence = 9
    }

    /// <summary>
    /// Represents an AST node of a symbolic regex.
    /// </summary>
    public class SymbolicRegexNode<S> : ICounter
    {
        internal SymbolicRegexBuilder<S> builder;
        internal SymbolicRegexKind kind;
        internal int lower = -1;
        internal int upper = -1;
        internal S set = default(S);
        internal Sequence<S> sequence = null;

        internal SymbolicRegexNode<S> left = null;
        internal SymbolicRegexNode<S> right = null;
        internal SymbolicRegexNode<S> iteCond = null;

        internal SymbolicRegexSet<S> alts = null;

        internal bool isNullable = false;
        public bool containsAnchors = false;

        int hashcode = -1;

        #region serialization

        /// <summary>
        /// Produce the serialized from of this symbolic regex node.
        /// </summary>
        public string Serialize()
        {
            var sb = new System.Text.StringBuilder();
            Serialize(this, sb);
            return sb.ToString();
        }

        /// <summary>
        /// Append the serialized form of this symbolic regex node to the stringbuilder
        /// </summary>
        public static void Serialize(SymbolicRegexNode<S> node, System.Text.StringBuilder sb)
        {
            var solver = node.builder.solver;
            SymbolicRegexNode<S> next = node;
            while (next != null)
            {
                node = next;
                next = null;
                switch (node.kind)
                {
                    case SymbolicRegexKind.Singleton:
                        {
                            if (node.set.Equals(solver.True))
                                sb.Append(".");
                            else
                            {
                                sb.Append("[");
                                sb.Append(solver.SerializePredicate(node.set));
                                sb.Append("]");
                            }
                            return;
                        }
                    case SymbolicRegexKind.Loop:
                        {
                            if (node.isLazyLoop)
                                sb.Append("Z(");
                            else
                                sb.Append("L(");
                            sb.Append(node.lower.ToString());
                            sb.Append(",");
                            sb.Append(node.upper == int.MaxValue ? "*" : node.upper.ToString());
                            sb.Append(",");
                            Serialize(node.left, sb);
                            sb.Append(")");
                            return;
                        }
                    case SymbolicRegexKind.Concat:
                        {
                            var elems = node.ToArray();
                            var elems_str = Array.ConvertAll(elems, x => x.Serialize());
                            var str = string.Join(",", elems_str);
                            sb.Append("S(");
                            sb.Append(str);
                            sb.Append(")");
                            return;
                        }
                    case SymbolicRegexKind.Epsilon:
                        {
                            sb.Append("E");
                            return;
                        }
                    case SymbolicRegexKind.Or:
                        {
                            sb.Append("D(");
                            node.alts.Serialize(sb);
                            sb.Append(")");
                            return;
                        }
                    case SymbolicRegexKind.And:
                        {
                            sb.Append("C(");
                            node.alts.Serialize(sb);
                            sb.Append(")");
                            return;
                        }
                    case SymbolicRegexKind.EndAnchor:
                        {
                            sb.Append("$");
                            return;
                        }
                    case SymbolicRegexKind.StartAnchor:
                        {
                            sb.Append("^");
                            return;
                        }
                    case SymbolicRegexKind.WatchDog:
                        {
                            sb.Append("#(" + node.lower + ")");
                            return;
                        }
                    default: // SymbolicRegexKind.IfThenElse:
                        {
                            sb.Append("I(");
                            Serialize(node.iteCond, sb);
                            sb.Append(",");
                            Serialize(node.left, sb);
                            sb.Append(",");
                            Serialize(node.right, sb);
                            sb.Append(")");
                            return;
                        }
                }
            }
        }

        /// <summary>
        /// Converts a concatenation into an array, 
        /// returns a non-concatenation in a singleton array.
        /// </summary>
        public SymbolicRegexNode<S>[] ToArray()
        {
            var list = new List<SymbolicRegexNode<S>>();
            AppendToList(this, list);
            return list.ToArray();
        }

        /// <summary>
        /// should only be used only if this is a concatenation node
        /// </summary>
        /// <returns></returns>
        static void AppendToList(SymbolicRegexNode<S> concat, List<SymbolicRegexNode<S>> list)
        {
            var node = concat;
            while (node.kind == SymbolicRegexKind.Concat)
            {
                if (node.left.kind == SymbolicRegexKind.Concat)
                    AppendToList(node.left, list);
                else
                    list.Add(node.left);
                node = node.right;
            }
            list.Add(node);
        }


        #endregion

        #region various properties
        /// <summary>
        /// Returns true if this is equivalent to .*
        /// </summary>
        public bool IsDotStar
        {
            get
            {
                return this.IsStar && this.left.kind == SymbolicRegexKind.Singleton &&
                    this.builder.solver.AreEquivalent(this.builder.solver.True, this.left.set);
            }
        }

        /// <summary>
        /// Returns true if this is equivalent to [0-[0]]
        /// </summary>
        public bool IsNothing
        {
            get
            {
                return this.kind == SymbolicRegexKind.Singleton &&
                    !this.builder.solver.IsSatisfiable(this.set);
            }
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
        /// Returns true iff this loop has an upper bound
        /// </summary>
        public bool HasUpperBound
        {
            get
            {
                return upper < int.MaxValue;
            }
        }

        /// <summary>
        /// Returns true iff this loop has a lower bound
        /// </summary>
        public bool HasLowerBound
        {
            get
            {
                return lower > 0;
            }
        }

        /// <summary>
        /// Returns true iff this is a loop whose lower bound is 0 and upper bound is 1
        /// </summary>
        public bool IsMaybe
        {
            get
            {
                return lower == 0 && upper == 1;
            }
        }

        /// <summary>
        /// Returns true if this is Epsilon
        /// </summary>
        public bool IsEpsilon
        {
            get
            {
                return this.kind == SymbolicRegexKind.Epsilon;
            }
        }
        #endregion

        /// <summary>
        /// Alternatives of an OR
        /// </summary>
        public IEnumerable<SymbolicRegexNode<S>> Alts
        {
            get { return alts; }
        }

        /// <summary>
        /// Gets the kind of the regex
        /// </summary>
        public SymbolicRegexKind Kind
        {
            get { return kind; }
        }

        /// <summary>
        /// Number of alternative branches if this is an or-node. 
        /// If this is not an or-node then the value is 1.
        /// </summary>
        public int OrCount
        {
            get
            {
                if (kind == SymbolicRegexKind.Or)
                    return alts.Count;
                else
                    return 1;
            }
        }

        /// <summary>
        /// Left child of a binary node (the child of a unary node, the true-branch of an Ite-node)
        /// </summary>
        public SymbolicRegexNode<S> Left
        {
            get { return left; }
        }

        /// <summary>
        /// Right child of a binary node (the false-branch of an Ite-node)
        /// </summary>
        public SymbolicRegexNode<S> Right
        {
            get { return right; }
        }

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

        /// <summary>
        /// Returns the number of top-level concatenation nodes.
        /// </summary>
        int _ConcatCount = -1;
        public int ConcatCount
        {
            get
            {
                if (_ConcatCount == -1)
                {
                    if (this.kind == SymbolicRegexKind.Concat)
                        _ConcatCount = left.ConcatCount + right.ConcatCount + 1;
                    else
                        _ConcatCount = 0;
                }
                return _ConcatCount;
            }
        }

        /// <summary>
        /// IfThenElse condition
        /// </summary>
        public SymbolicRegexNode<S> IteCond
        {
            get { return iteCond; }
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
        /// Returns true iff this is a start-anchor
        /// </summary>
        public bool IsSartAnchor
        {
            get { return this.kind == SymbolicRegexKind.StartAnchor; }
        }

        /// <summary>
        /// Returns true iff this is an anchor for detecting start of line (including first line or start of input)
        /// </summary>
        public bool IsStartOfLineAnchor
        {
            get { return this.lower == -2; }
        }

        /// <summary>
        /// Returns true iff this is an anchor for detecting end of input
        /// </summary>
        public bool IsEndAnchor
        {
            get { return this.kind == SymbolicRegexKind.EndAnchor; }
        }

        /// <summary>
        /// Returns true iff this is an anchor for detecting end of line (including last line or end of input)
        /// </summary>
        public bool IsEndOfLineAnchor
        {
            get { return this.upper == -2; }
        }

        /// <summary>
        /// Returns true iff this is either a start-anchor or an end-anchor
        /// </summary>
        public bool IsAnchor
        {
            get { return IsSartAnchor || IsEndAnchor; }
        }

        /// <summary>
        /// AST node of a symbolic regex
        /// </summary>
        /// <param name="builder">the builder</param>
        /// <param name="kind">what kind of node</param>
        /// <param name="left">left child</param>
        /// <param name="right">right child</param>
        /// <param name="lower">lower bound of a loop</param>
        /// <param name="upper">upper boubd of a loop</param>
        /// <param name="set">singelton set</param>
        /// <param name="iteCond">if-then-else condition</param>
        /// <param name="alts">alternatives set of a disjunction</param>
        /// <param name="seq">sequence of singleton sets</param>
        private SymbolicRegexNode(SymbolicRegexBuilder<S> builder, SymbolicRegexKind kind, SymbolicRegexNode<S> left, SymbolicRegexNode<S> right, int lower, int upper, S set, SymbolicRegexNode<S> iteCond, SymbolicRegexSet<S> alts)
        {
            this.builder = builder;
            this.kind = kind;
            this.left = left;
            this.right = right;
            this.lower = lower;
            this.upper = upper;
            this.set = set;
            this.iteCond = iteCond;
            this.alts = alts;
        }

        internal SymbolicRegexNode<S> ConcatWithoutNormalizing(SymbolicRegexNode<S> next)
        {
            var concat = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Concat, this, next, -1, -1, default(S), null, null);
            return concat;
        }

        #region called only once, in the constructor of SymbolicRegexBuilder

        internal static SymbolicRegexNode<S> MkFalse(SymbolicRegexBuilder<S> builder, S f)
        {
            return new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Singleton, null, null, -1, -1, f, null, null);
        }

        internal static SymbolicRegexNode<S> MkTrue(SymbolicRegexBuilder<S> builder, S t)
        {
            return new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Singleton, null, null, -1, -1, t, null, null);
        }

        internal static SymbolicRegexNode<S> MkNewline(SymbolicRegexBuilder<S> builder, S nl)
        {
            return new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Singleton, null, null, -1, -1, nl, null, null);
        }

        //internal static SymbolicRegexNode<S> MkSequence(SymbolicRegexBuilder<S> builder, Sequence<S> seq)
        //{
        //    return new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Sequence, null, null, -1, -1, default(S), null, null, seq);
        //}

        internal static SymbolicRegexNode<S> MkWatchDog(SymbolicRegexBuilder<S> builder, int length)
        {
            var wd = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.WatchDog, null, null, length, -1, default(S), null, null);
            wd.isNullable = true;
            return wd;
        }

        internal static SymbolicRegexNode<S> MkEpsilon(SymbolicRegexBuilder<S> builder)
        {
            var eps = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Epsilon, null, null, -1, -1, default(S), null, null);
            eps.isNullable = true;
            return eps;
        }

        internal static SymbolicRegexNode<S> MkStartAnchor(SymbolicRegexBuilder<S> builder)
        {
            var anchor = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.StartAnchor, null, null, -1, -1, default(S), null, null);
            anchor.containsAnchors = true;
            return anchor;
        }

        internal static SymbolicRegexNode<S> MkEndAnchor(SymbolicRegexBuilder<S> builder)
        {
            var anchor = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.EndAnchor, null, null, -1, -1, default(S), null, null);
            anchor.containsAnchors = true;
            return anchor;
        }

        internal static SymbolicRegexNode<S> MkEolAnchor(SymbolicRegexBuilder<S> builder)
        {
            var anchor = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.EndAnchor, null, null, -1, -2, default(S), null, null);
            anchor.containsAnchors = true;
            return anchor;
        }

        internal static SymbolicRegexNode<S> MkBolAnchor(SymbolicRegexBuilder<S> builder)
        {
            var anchor = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.StartAnchor, null, null, -2, -1, default(S), null, null);
            anchor.containsAnchors = true;
            return anchor;
        }

        internal static SymbolicRegexNode<S> MkDotStar(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> body)
        {
            var loop = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Loop, body, null, 0, int.MaxValue, default(S), null, null);
            loop.isNullable = true;
            return loop;
        }

        #endregion

        internal static SymbolicRegexNode<S> MkSingleton(SymbolicRegexBuilder<S> builder, S set)
        {
            return new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Singleton, null, null, -1, -1, set, null, null);
        }

        internal static SymbolicRegexNode<S> MkLoop(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> body, int lower, int upper, bool isLazy)
        {
            if (lower < 0 || upper < lower)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var loop = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Loop, body, null, lower, upper, default(S), null, null);
            if (loop.lower == 0)
            {
                loop.isNullable = true;
            }
            else
            {
                loop.isNullable = body.isNullable;
            }
            loop.isLazyLoop = isLazy;
            loop.containsAnchors = body.containsAnchors;
            return loop;
        }

        internal static SymbolicRegexNode<S> MkOr(SymbolicRegexBuilder<S> builder, params SymbolicRegexNode<S>[] choices)
        {
            return MkOr(builder, SymbolicRegexSet<S>.CreateDisjunction(builder, choices));
        }

        internal static SymbolicRegexNode<S> MkAnd(SymbolicRegexBuilder<S> builder, params SymbolicRegexNode<S>[] conjuncts)
        {
            var elems = SymbolicRegexSet<S>.CreateConjunction(builder, conjuncts);
            return MkAnd(builder, elems);
        }

        internal static SymbolicRegexNode<S> MkOr(SymbolicRegexBuilder<S> builder, SymbolicRegexSet<S> alts)
        {
            if (alts.IsNothing)
                return builder.nothing;
            else if (alts.IsEverything)
                return builder.dotStar;
            else if (alts.IsSigleton)
                return alts.GetTheElement();
            else
            {
                var or = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Or, null, null, -1, -1, default(S), null, alts);
                or.isNullable = alts.IsNullable();
                or.containsAnchors = alts.ContainsAnchors();
                return or;
            }
        }

        internal static SymbolicRegexNode<S> MkAnd(SymbolicRegexBuilder<S> builder, SymbolicRegexSet<S> alts)
        {
            if (alts.IsNothing)
                return builder.nothing;
            else if (alts.IsEverything)
                return builder.dotStar;
            else if (alts.IsSigleton)
                return alts.GetTheElement();
            else
            {
                var and = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.And, null, null, -1, -1, default(S), null, alts);
                and.isNullable = alts.IsNullable();
                and.containsAnchors = alts.ContainsAnchors();
                return and;
            }
        }

        /// <summary>
        /// Only call MkConcat when left and right are flat, the resulting concat(left,right) is then also flat,
        /// </summary>
        internal static SymbolicRegexNode<S> MkConcat(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> left, SymbolicRegexNode<S> right)
        {
            SymbolicRegexNode<S> concat;
            if (left == builder.nothing || right == builder.nothing)
                return builder.nothing;
            else if (left.IsEpsilon)
                return right;
            else if (right.IsEpsilon)
                return left;
            else if (left.kind != SymbolicRegexKind.Concat)
            {
                concat = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Concat, left, right, -1, -1, default(S), null, null);
                concat.isNullable = left.isNullable && right.isNullable;
                concat.containsAnchors = left.containsAnchors || right.containsAnchors;
            }
            else
            {
                concat = right;
                var left_elems = left.ToArray();
                for (int i = left_elems.Length - 1; i >= 0; i = i - 1)
                {
                    var tmp = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Concat, left_elems[i], concat, -1, -1, default(S), null, null);
                    tmp.isNullable = left_elems[i].isNullable && concat.isNullable;
                    tmp.containsAnchors = left_elems[i].containsAnchors || concat.containsAnchors;
                    concat = tmp;
                }
            }
            return concat;
        }


        //internal SymbolicRegexNode<S> MkConcatWith(SymbolicRegexNode<S> that)
        //{
        //    switch (this.kind)
        //    {
        //        case SymbolicRegexKind.Concat:
        //            {
        //                var concat = that;
        //                foreach (var node in this.EnumerateConcatElementsBackwards())
        //                {
        //                    var tmp = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Concat, node, concat, -1, -1, default(S), null, null);
        //                    tmp.isNullable = node.isNullable && concat.isNullable;
        //                    tmp.containsAnchors = node.containsAnchors || concat.containsAnchors;
        //                    concat = tmp;
        //                }
        //                return concat;
        //            }
        //        case SymbolicRegexKind.Singleton:
        //            {
        //                if (that.kind == SymbolicRegexKind.Singleton)
        //                {
        //                    var seq = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Sequence, null, null, -1, -1, default(S), null, null, new Sequence<S>(this.set, that.set));
        //                    seq.isNullable = false;
        //                    seq.containsAnchors = false;
        //                    return seq;
        //                }
        //                else if (that.kind == SymbolicRegexKind.Sequence)
        //                {
        //                    var seq = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Sequence, null, null, -1, -1, default(S), null, null, new Sequence<S>(this.set).Append(that.sequence));
        //                    seq.isNullable = false;
        //                    seq.containsAnchors = false;
        //                    return seq;
        //                }
        //                else if (that.kind == SymbolicRegexKind.Concat)
        //                {
        //                    if (that.left.kind == SymbolicRegexKind.Singleton)
        //                    {
        //                        var seq = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Sequence, null, null, -1, -1, default(S), null, null, new Sequence<S>(this.set, that.left.set));
        //                        seq.isNullable = false;
        //                        seq.containsAnchors = false;
        //                        var concat = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Concat, seq, that.right, -1, -1, default(S), null, null);
        //                    }
        //                }
        //            }
        //        default:
        //            {
        //                var concat = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Concat, this, that, -1, -1, default(S), null, null);
        //                concat.isNullable = this.isNullable && that.isNullable;
        //                concat.containsAnchors = this.containsAnchors || that.containsAnchors;
        //                return concat;
        //            }
        //    }
        //}

        private IEnumerable<SymbolicRegexNode<S>> EnumerateConcatElementsBackwards()
        {
            switch (this.kind)
            {
                case SymbolicRegexKind.Concat:
                    foreach (var elem in right.EnumerateConcatElementsBackwards())
                        yield return elem;
                    yield return left;
                    yield break;
                default:
                    yield return this;
                    yield break;
            }
        }

        internal static SymbolicRegexNode<S> MkIfThenElse(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> cond, SymbolicRegexNode<S> left, SymbolicRegexNode<S> right)
        {
            if (right == builder.nothing)
            {
                return SymbolicRegexNode<S>.MkAnd(builder, cond, left);
            }
            else
            {
                var ite = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.IfThenElse, left, right, -1, -1, default(S), cond, null);
                ite.isNullable = (cond.isNullable ? left.isNullable : right.isNullable);
                ite.containsAnchors = (cond.containsAnchors || left.containsAnchors || right.containsAnchors);
                return ite;
            }
        }

        /// <summary>
        /// Produce a string representation of the symbolic regex. 
        /// </summary>
        public override string ToString()
        {

            switch (this.kind)
            {
                case SymbolicRegexKind.StartAnchor:
                    {
                        return "^";
                    }
                case SymbolicRegexKind.EndAnchor:
                    {
                        return "$";
                    }
                case SymbolicRegexKind.WatchDog:
                    {
                        return "()" + SpecialCharacters.ToSubscript(lower);
                    }
                case SymbolicRegexKind.Epsilon:
                    {
                        return "()";
                    }
                case SymbolicRegexKind.Singleton:
                    {
                        return builder.solver.PrettyPrint(this.set);
                    }
                case SymbolicRegexKind.Loop:
                    {
                        var body = this.left.ToString();
                        if (this.left.kind != SymbolicRegexKind.Singleton)
                            body = "(" + body + ")";
                        if (this.IsMaybe)
                            return body + "?";
                        else if (this.IsStar)
                            return body + "*";
                        else if (this.IsPlus)
                            return body + "+";
                        else
                        {
                            string bounds = this.lower.ToString();
                            if (this.upper > this.lower)
                            {
                                bounds += ",";
                                if (this.upper < int.MaxValue)
                                    bounds += this.upper.ToString();
                            }
                            return body + "{" + bounds + "}";
                        }
                    }
                case SymbolicRegexKind.Concat:
                    {
                        string s = "";
                        var node = this;
                        while (node.kind == SymbolicRegexKind.Concat)
                        {
                            if (node.left.kind == SymbolicRegexKind.Or)
                            {
                                s += "(" + node.left.ToString() + ")";
                            }
                            else
                            {
                                s += node.left.ToString();
                            }
                            node = node.right;
                        }
                        if (node.kind == SymbolicRegexKind.Or)
                        {
                            s += "(" + node.ToString() + ")";
                        }
                        else
                        {
                            s += node.ToString();
                        }
                        return s;
                    }
                case SymbolicRegexKind.Or:
                case SymbolicRegexKind.And:
                    {
                        return this.alts.ToString();
                    }
                default: //if-then-else
                    {
                        return "(?(" + this.iteCond.ToString() + ")(" + this.left.ToString() + ")|(" + this.right.ToString() + "))";
                    }
            }
        }

        /// <summary>
        /// Produces a string in smtlib format of this regex. Assumes there are no anchors.
        /// </summary>
        /// <returns></returns>
        public string ToSMTlibFormat(bool ascii = true)
        {
            var filter = (ascii ? this.builder.solver.CharSetProvider.MkCharSetFromRange('\0', '\x7F') : 
                this.builder.solver.CharSetProvider.True);
            return ToSMT_(filter);
        }

        string ToSMT_(BDD filter)
        {
            string epsilon = "(str.to_re \"\")";
            switch (this.kind)
            {
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.StartAnchor:
                    {
                        throw new AutomataException(AutomataExceptionKind.RegexConstructNotSupported);
                    }
                case SymbolicRegexKind.WatchDog:
                case SymbolicRegexKind.Epsilon:
                    {
                        return epsilon;
                    }
                case SymbolicRegexKind.Singleton:
                    {
                        var bdd = this.builder.solver.ConvertToCharSet(this.builder.solver.CharSetProvider, this.set);
                        if (bdd.IsFull)
                            return "(re.allchar)";
                        else if (bdd.IsEmpty)
                            return "(re.none)";
                        else
                        {
                            bdd = this.builder.solver.CharSetProvider.MkAnd(bdd, filter);
                            var ranges = bdd.ToRanges();
                            string res = "";
                            for (int i = 0; i < ranges.Length; i++)
                            {
                                string l_i = getSMTlibChar(ranges[i].Item1);
                                string h_i = getSMTlibChar(ranges[i].Item2);
                                string range_i;
                                if (l_i == h_i)
                                {
                                    range_i = "(str.to_re " + l_i + ")";
                                }
                                else
                                {
                                    range_i = "(re.range " + l_i + " " + h_i + ")";
                                }
                                if (res == "")
                                    res = range_i;
                                else
                                    res = "(re.union " + res + " " + range_i + ")";
                            }
                            return res;
                        }
                    }
                case SymbolicRegexKind.Loop:
                    {
                        var body = this.left.ToSMT_(filter);
                        if (this.IsDotStar)
                            return "(re.all)";
                        else if (this.IsMaybe)
                            return "(re.opt " + body + ")";
                        else if (this.IsStar)
                            return "(re.* " + body + ")";
                        else if (this.IsPlus)
                            return "(re.+ " + body + ")";
                        else if (this.upper == int.MaxValue)
                            //only lower bound > 1
                            return string.Format("(re.++ ((_ re.^ {0}) {1}) (re.* {1}))", this.lower, body);
                        else if (this.lower == this.upper)
                            //lower'th power
                            return string.Format("((_ re.^ {0}) {1})", this.lower, body);
                        else
                            //general bounded loop
                            return string.Format("((_ re.loop {0} {1}) {2})", this.lower, this.upper, body);
                    }
                case SymbolicRegexKind.Concat:
                    {
                        string lhs = this.left.ToSMT_(filter);
                        string rhs = this.right.ToSMT_(filter);
                        //skip rhs epsilons
                        if (rhs == epsilon)
                            return lhs;
                        string res;
                        if (lhs.StartsWith("(str.to_re \"") && rhs.StartsWith("(str.to_re \""))
                        {
                            //extract the relevant string parts
                            string lhs_str = lhs.Substring(0, lhs.Length - 2);
                            string rhs_str = rhs.Substring(12);
                            //append the strings into one string
                            res = lhs_str + rhs_str;
                        }
                        else
                        {
                            res = string.Format("(re.++ {0} {1})", lhs, rhs);
                        }
                        return res;
                    }
                case SymbolicRegexKind.Or:
                    {
                        var enumerator = this.alts.GetEnumerator();
                        string res = "(re.none)";
                        while (enumerator.MoveNext())
                        {
                            string str = enumerator.Current.ToSMT_(filter);
                            if (res == "(re.none)")
                                res = str;
                            else
                                res = string.Format("(re.union {0} {1})", res, str);
                        }
                        return res;
                    }
                case SymbolicRegexKind.And:
                    {
                        var enumerator = this.alts.GetEnumerator();
                        string res = "(re.all)";
                        while (enumerator.MoveNext())
                        {
                            string str = enumerator.Current.ToSMT_(filter);
                            if (res == "(re.all)")
                                res = str;
                            else
                                res = string.Format("(re.inter {0} {1})", res, str);
                        }
                        return res;
                    }
                default: //if-then-else
                    {
                        string cond = this.iteCond.ToSMT_(filter);
                        string truecase = this.left.ToSMT_(filter);
                        string falsecase = this.right.ToSMT_(filter);
                        string res;
                        if ((truecase == "(re.none)") && (falsecase == "(re.all)"))
                            //the ite represents a complement
                            res = string.Format("(re.comp {0})", cond);
                        else if (falsecase == "(re.none)")
                        {
                            res = string.Format("(re.inter {0} {1})", cond, truecase);
                        }
                        else if (truecase == "(re.none)")
                        {
                            res = string.Format("(re.inter (re.comp {0}) {1})", cond, falsecase);
                        }
                        else
                        {
                            res = string.Format("(re.union (re.inter {0} {1}) (re.inter (re.comp {0}) {2}))", cond, truecase, falsecase);
                        }
                        return res;
                    }
            }
        }

        private IEnumerable<SymbolicRegexNode<S>> EnumerateConcatElems()
        {
            if (this.kind != SymbolicRegexKind.Concat)
                yield return this;
            else
            {
                foreach (var elem in left.EnumerateConcatElems())
                    yield return elem;
                foreach (var elem in right.EnumerateConcatElems())
                    yield return elem;
            }
        }

        string getSMTlibChar(uint code)
        {
            string res;
            //view the printable character as singleton string with that character
            if ((35 <= code) && (code <= 122))
            {
                 res = "\"" + ((char)code).ToString() + "\"";
            }
            else
            {
                res = "(_ char #x" + code.ToString("X") + ")";
            }
            return res;
        }

        private static void ComputeToString(SymbolicRegexNode<S> top, StringBuilder sb)
        {
            var solver = top.builder.solver;
            SymbolicRegexNode<S> next = top;
            while (next != null)
            {
                var node = next;
                next = null;
                switch (node.kind)
                {
                    case SymbolicRegexKind.StartAnchor:
                        {
                            sb.Append("^");
                            continue;
                        }
                    case SymbolicRegexKind.EndAnchor:
                        {
                            sb.Append("$");
                            continue;
                        }
                    case SymbolicRegexKind.Epsilon:
                        {
                            sb.Append("()");
                            continue;
                        }
                    case SymbolicRegexKind.WatchDog:
                        {
                            sb.Append("()" + SpecialCharacters.ToSubscript(node.lower));
                            continue;
                        }
                    case SymbolicRegexKind.Singleton:
                        {
                            sb.Append(solver.PrettyPrint(node.set));
                            continue;
                        }
                    case SymbolicRegexKind.Loop:
                        {
                            sb.Append("(");
                            node.left.ComputeToString_LoopBody_AddExtraParentesis(sb);
                            if (node.IsMaybe)
                                sb.Append("?");
                            else if (node.IsStar)
                                sb.Append("*");
                            else if (node.IsPlus)
                                sb.Append("+");
                            else
                            {
                                sb.Append("{");
                                sb.Append(node.lower.ToString());
                                sb.Append(",");
                                if (node.upper < int.MaxValue)
                                    sb.Append(node.upper.ToString());
                                sb.Append("}");
                            }
                            sb.Append(")");
                            continue;
                        }
                    case SymbolicRegexKind.Concat:
                        {
                            node.left.ComputeToString_LoopBody_AddExtraParentesis(sb);
                            next = node.right;
                            continue;
                        }
                    case SymbolicRegexKind.Or:
                    case SymbolicRegexKind.And:
                        {
                            sb.Append(node.alts.ToString());
                            continue;
                        }
                    default: //if-then-else
                        {
                            sb.Append("(?(");
                            ComputeToString(node.iteCond, sb);
                            sb.Append(")");
                            ComputeToString(node.left, sb);
                            sb.Append("|");
                            ComputeToString(node.right, sb);
                            sb.Append(")");
                            continue;
                        }
                }
            }
        }

        private void ComputeToString_LoopBody_AddExtraParentesis(StringBuilder sb)
        {
            if ((this.kind == SymbolicRegexKind.Loop && this.left.kind != SymbolicRegexKind.Singleton) || 
                //this.OrCount > 1 ||
                this.kind == SymbolicRegexKind.Concat)
            {
                sb.Append("(");
                ComputeToString(this, sb);
                sb.Append(")");
            }
            else
            {
                ComputeToString(this, sb);
            }
        }

        private void ComputeToString_(StringBuilder sb)
        {
            var stack = new SimpleStack<SymbolicRegexNode<S>>();
            stack.Push(this);
            while (stack.IsNonempty)
            {
            }
            switch (kind)
            {
                case SymbolicRegexKind.StartAnchor:
                    {
                        sb.Append("^");
                        return;
                    }
                case SymbolicRegexKind.EndAnchor:
                    {
                        sb.Append("$");
                        return;
                    }
                case SymbolicRegexKind.Epsilon:
                    {
                        sb.Append("()");
                        return;
                    }
                case SymbolicRegexKind.WatchDog:
                    {
                        return;
                    }
                case SymbolicRegexKind.Singleton:
                    {
                        sb.Append(this.builder.solver.PrettyPrint(set));
                        return;
                    }
                case SymbolicRegexKind.Loop:
                    {
                        if (left.OrCount > 1 || left.kind == SymbolicRegexKind.Concat)
                        {
                            sb.Append("(");
                            left.ComputeToString_(sb);
                            sb.Append(")");
                        }
                        else if (IsStar)
                        {
                            left.ComputeToString_(sb);
                            sb.Append("*");
                        }
                        else if (IsPlus)
                        {
                            left.ComputeToString_(sb);
                            sb.Append("+");
                        }
                        else if (IsMaybe)
                        {
                            if (left.kind == SymbolicRegexKind.Loop)
                            {
                                sb.Append("(");
                                left.ComputeToString_(sb);
                                sb.Append(")?");
                            }
                            else
                            {
                                left.ComputeToString_(sb);
                                sb.Append("?");
                            }
                        }
                        else
                        {
                            if (left.kind == SymbolicRegexKind.Loop)
                            {
                                sb.Append("(");
                                left.ComputeToString_(sb);
                                sb.Append("){");
                                sb.Append(this.lower.ToString());
                                sb.Append(",");
                                if (this.upper < int.MaxValue)
                                    sb.Append(this.upper.ToString());
                                sb.Append("}");
                            }
                            else
                            {
                                left.ComputeToString_(sb);
                                sb.Append("{");
                                sb.Append(this.lower.ToString());
                                sb.Append(",");
                                if (this.upper < int.MaxValue)
                                    sb.Append(this.upper.ToString());
                                sb.Append("}");
                            }
                        }
                        return;
                    }
                case SymbolicRegexKind.Concat:
                    {
                        if (left.OrCount > 1)
                        {
                            sb.Append("(");
                            left.ComputeToString_(sb);
                            sb.Append(")");
                        }
                        else
                        {
                            left.ComputeToString_(sb);
                        }
                        if (right.OrCount > 1)
                        {
                            sb.Append("(");
                            right.ComputeToString_(sb);
                            sb.Append(")");
                        }
                        else
                        {
                            right.ComputeToString_(sb);
                        }
                        return;
                    }
                case SymbolicRegexKind.Or:
                case SymbolicRegexKind.And:
                    {
                        sb.Append(alts.ToString());
                        return;
                    }
                default:
                    {
                        sb.Append("(?(");
                        iteCond.ComputeToString_(sb);
                        sb.Append(")");
                        left.ComputeToString_(sb);
                        sb.Append("|");
                        right.ComputeToString_(sb);
                        sb.Append(")");
                        return;
                    }
            }
        }

        /// <summary>
        /// Transform the symbolic regex so that all singletons have been intersected with the given predicate pred. 
        /// </summary>
        public SymbolicRegexNode<S> Restrict(S pred)
        {
            switch (kind)
            {
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.WatchDog:
                    return this;
                case SymbolicRegexKind.Singleton:
                    {
                        var newset = builder.solver.MkAnd(this.set, pred);
                        if (this.set.Equals(newset))
                            return this;
                        else
                            return builder.MkSingleton(newset);
                    }
                //case SymbolicRegexKind.Sequence:
                //    {
                //        var newseq = new Sequence<S>(Array.ConvertAll(this.sequence.ToArray(), x => builder.solver.MkAnd(x, pred)));
                //        if (this.sequence.Equals(newseq))
                //            return this;
                //        else
                //            return builder.MkSequence(newseq);
                //    }
                case SymbolicRegexKind.Loop:
                    {
                        var body = this.left.Restrict(pred);
                        if (body == this.left)
                            return this;
                        else
                            return builder.MkLoop(body, isLazyLoop, this.lower, this.upper);
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
                        var choices = alts.Restrict(pred);
                        return builder.MkOr(choices);
                    }
                case SymbolicRegexKind.And:
                    {
                        var conjuncts = alts.Restrict(pred);
                        return builder.MkAnd(conjuncts);
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

        /// <summary>
        /// Returns the fixed matching length of the regex or -1 if the regex does not have a fixed matching length.
        /// </summary>
        public int GetFixedLength()
        {
            switch (kind)
            {
                case SymbolicRegexKind.WatchDog:
                case SymbolicRegexKind.Epsilon:
                    return 0;
                case SymbolicRegexKind.Singleton:
                    return 1;
                case SymbolicRegexKind.Loop:
                    {
                        if (this.lower == this.upper)
                        {
                            var body_length = this.left.GetFixedLength();
                            if (body_length >= 0)
                                return this.lower * body_length;
                            else
                                return -1;
                        }
                        else
                            return -1;
                    }
                case SymbolicRegexKind.Concat:
                    {
                        var left_length = this.left.GetFixedLength();
                        if (left_length >= 0)
                        {
                            var right_length = this.right.GetFixedLength();
                            if (right_length >= 0)
                                return left_length + right_length;
                        }
                        return -1;
                    }
                case SymbolicRegexKind.Or:
                    {
                        return alts.GetFixedLength();
                    }
                default: 
                    {
                        return -1;
                    }
            }
        }

        //bool IsNullableInitially
        //{
        //    get
        //    {
        //        if (isNullable)
        //            return true;
        //        else if (kind == SymbolicRegexKind.Loop)
        //            return lower == 0;
        //        else if (kind == SymbolicRegexKind.Concat)
        //            return left.IsNullableInitially && right.IsNullableInitially;
        //        else if (kind == SymbolicRegexKind.Or)
        //        {
        //            foreach (var choice in alts)
        //                if (choice.IsNullableInitially)
        //                    return true;
        //            return false;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //}

        /// <summary>
        /// Replace all anchors (^ and $) in the symbolic regex with () and missing anchors with .*
        /// </summary>
        /// <param name="isBeg">if true (default) then this is the beginning borderline and missing ^ is replaced with .*</param>
        /// <param name="isEnd">if true (default) then this is the end borderline and missing $ is replaced with .*</param>
        /// <returns></returns>
        public SymbolicRegexNode<S> ReplaceAnchors(bool isBeg = true, bool isEnd = true)
        {
            return builder.RemoveAnchors(this, isBeg, isEnd);
        }

        /// <summary>
        /// Takes the derivative of the symbolic regex wrt elem. 
        /// Assumes that elem is either a minterm wrt the predicates of the whole regex or a singleton set.
        /// </summary>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <returns></returns>
        public SymbolicRegexNode<S> MkDerivative(S elem)
        {
            return builder.MkDerivative(elem, this);
        }

        /// <summary>
        /// Takes the Antimirov derivative of the symbolic regex wrt elem. 
        /// Assumes that elem is either a minterm wrt the predicates of the whole regex or a singleton set.
        /// </summary>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <returns></returns>
        internal List<ConditionalDerivative<S>> GetConditinalDerivatives(S elem)
        {
            return new List<ConditionalDerivative<S>>(builder.EnumerateConditionalDerivatives(elem, this, true));
        }

        ///// <summary>
        ///// Temporary counter automaton exploration untility
        ///// </summary>
        ///// <returns></returns>
        //internal Automaton<Tuple<Maybe<S>,Sequence<CounterOperation>>> Explore()
        //{
        //    var this_normalized = this.builder.NormalizeGeneralLoops(this);
        //    var stateLookup = new Dictionary<SymbolicRegexNode<S>, int>();
        //    var regexLookup = new Dictionary<int,SymbolicRegexNode<S>>();
        //    stateLookup[this_normalized] = 0;
        //    int stateid = 2;
        //    regexLookup[0] = this_normalized;
        //    SimpleStack<int> frontier = new SimpleStack<int>();
        //    var moves = new List<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>>();
        //    var finalStates = new HashSet<int>();
        //    frontier.Push(0);
        //    var reset0 = this_normalized.GetNullabilityCondition(false, true);
        //    if (reset0 != null)
        //    {
        //        if (reset0.TrueForAll(x => x.Counter.LowerBound == 0))
        //            reset0 = Sequence<CounterOperation>.Empty;
        //        moves.Add(Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(0, 1,
        //            new Tuple<Maybe<S>, Sequence<CounterOperation>>(Maybe<S>.Nothing, reset0)));
        //        finalStates.Add(1);
        //    }
        //    while (frontier.IsNonempty)
        //    {
        //        var q = frontier.Pop();
        //        var regex = regexLookup[q];
        //        //partition corresponds to the alphabet
        //        foreach (S a in builder.solver.GetPartition())
        //        {
        //            foreach (var cd in builder.EnumerateConditionalDerivatives(a, regex, false))
        //            {
        //                int p;
        //                if (!stateLookup.TryGetValue(cd.PartialDerivative, out p))
        //                {
        //                    p = stateid++;
        //                    stateLookup[cd.PartialDerivative] = p;
        //                    regexLookup[p] = cd.PartialDerivative;

        //                    var reset = cd.PartialDerivative.GetNullabilityCondition(false, true);
        //                    if (reset != null)
        //                    {
        //                        if (reset.TrueForAll(x => x.Counter.LowerBound == 0))
        //                            reset = Sequence<CounterOperation>.Empty;
        //                        moves.Add(Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(p, 1,
        //                            new Tuple<Maybe<S>, Sequence<CounterOperation>>(Maybe<S>.Nothing, reset)));
        //                        finalStates.Add(1);
        //                    }
        //                    frontier.Push(p);
        //                }
        //                moves.Add(Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(q, p,
        //                    new Tuple<Maybe<S>, Sequence<CounterOperation>>(Maybe<S>.Something(a), cd.Condition)));
        //            }
        //        }
        //    }
        //    var aut = Automaton<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(new CABA<S>(builder),
        //        0, finalStates, moves);
        //    return aut;
        //}

        internal Sequence<CounterOperation> GetNullabilityCondition(bool top)
        {
            if (!top)
            {
                if (this.isNullable)
                    return Sequence<CounterOperation>.Empty;
                else
                    return null;
            }
            else
            {
                var node = this;
                //find the leftmost node in the concatenation such that 
                //all the ones after it are nullable in initial mode
                while (node.kind == SymbolicRegexKind.Concat && node.right.isNullable)
                    node = node.left;
                //if this node is a bounded loop return its exit condition
                if (node.IsBoundedLoop)
                    return new Sequence<CounterOperation>(new CounterOperation(node, CounterOp.EXIT));
                else if (node.isNullable)
                    return Sequence<CounterOperation>.Empty;
                else
                    return null;
            }
        }

        /// <summary>
        /// Counter automaton exploration utility
        /// </summary>
        /// <returns></returns>
        Automaton<Tuple<Maybe<S>, Sequence<CounterOperation>>> Explore(out Dictionary<int, ICounter> countingStates, out Dictionary<int, SymbolicRegexNode<S>> stateMap, bool monadic)
        {
            var node = (monadic ? this : this.builder.NormalizeGeneralLoops(this));
            var stateLookup = new Dictionary<SymbolicRegexNode<S>, int>();
            var regexLookup = new Dictionary<int, SymbolicRegexNode<S>>();
            stateLookup[node] = 0;
            int stateid = 1;
            regexLookup[0] = node;
            SimpleStack<int> frontier = new SimpleStack<int>();
            var moves = new List<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>>();
            var finalStates = new HashSet<int>();
            frontier.Push(0);
            var reset0 = node.GetNullabilityCondition(true);
            if (reset0 != null)
            {
                moves.Add(Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(0, 0,
                    new Tuple<Maybe<S>, Sequence<CounterOperation>>(Maybe<S>.Nothing, reset0)));
                finalStates.Add(0);
            }
            while (frontier.IsNonempty)
            {
                var q = frontier.Pop();
                var regex = regexLookup[q];
                //partition corresponds to the alphabet
                foreach (S a in builder.solver.GetPartition())
                {
                    var a_derivs = regex.GetConditinalDerivatives(a);
                    foreach (var cd in a_derivs)
                    {
                        int p;
                        if (!stateLookup.TryGetValue(cd.PartialDerivative, out p))
                        {
                            p = stateid++;
                            stateLookup[cd.PartialDerivative] = p;
                            regexLookup[p] = cd.PartialDerivative;

                            var reset = cd.PartialDerivative.GetNullabilityCondition(true);
                            if (reset != null)
                            {
                                moves.Add(Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(p, p,
                                    new Tuple<Maybe<S>, Sequence<CounterOperation>>(Maybe<S>.Nothing, reset)));
                                finalStates.Add(p);
                            }
                            frontier.Push(p);
                        }
                        moves.Add(Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(q, p,
                            new Tuple<Maybe<S>, Sequence<CounterOperation>>(Maybe<S>.Something(a), cd.Condition)));
                    }
                }
            }
            stateMap = regexLookup;

            var moves1 = moves;
            if (monadic)
            {
                #region move initialization to start of counting states
                //remap counters to fresh ones
                Dictionary<int, ICounter> newCounters1 = new Dictionary<int, ICounter>();
                Dictionary<Tuple<int, int>, ICounter> freshCounterMap = new Dictionary<Tuple<int, int>, ICounter>();
                foreach (var q_r in regexLookup)
                {
                    var c = q_r.Value.GetBoundedCounter();
                    if (c != null)
                    {
                        var bc = new BoundedCounter(freshCounterMap.Count, c.LowerBound, c.UpperBound);
                        var old = new Tuple<int, int>(q_r.Key, c.CounterId);
                        freshCounterMap[old] = bc;
                        newCounters1[q_r.Key] = bc;
                    }
                }

                moves1 = new List<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>>();

                Func<int, int, Sequence<CounterOperation>, Sequence<CounterOperation>> F = (s, t, ops) =>
                {
                    var sc = regexLookup[s].GetBoundedCounter();
                    var tc = regexLookup[t].GetBoundedCounter();
                    var sc1 = (newCounters1.ContainsKey(s) ? newCounters1[s] : null);
                    var tc1 = (newCounters1.ContainsKey(t) ? newCounters1[t] : null);
                    var ops1 = new List<CounterOperation>();

                    if (s == t)
                    {
                        //sc1 == null means that this is a non-counting state loop
                        if (sc1 != null)
                        {
                            if (ops.Length == 1 && (ops[0].OperationKind == CounterOp.EXIT || ops[0].OperationKind == CounterOp.EXIT_SET0))
                                ops1.Add(new CounterOperation(sc1, CounterOp.EXIT_SET0));
                            else if (ops.Length == 1 && ops[0].OperationKind == CounterOp.EXIT_SET1)
                                ops1.Add(new CounterOperation(sc1, CounterOp.EXIT_SET1));
                            else if (ops.Length == 1 && ops[0].OperationKind == CounterOp.INCR)
                                ops1.Add(new CounterOperation(sc1, CounterOp.INCR));
                            else
                                throw new AutomataException(AutomataExceptionKind.InternalError);
                        }
                    }
                    else if (sc != null && tc == null)
                    {
                        //this is a nonmonadic counting loop
                        if (ops.Length == 1 && ops[0].OperationKind == CounterOp.INCR)
                            ops1.Add(new CounterOperation(sc1, CounterOp.INCR));
                        else if (ops.Length == 1 && ops[0].OperationKind == CounterOp.EXIT)
                            ops1.Add(new CounterOperation(sc1, CounterOp.EXIT));
                        else
                            throw new AutomataException(AutomataExceptionKind.InternalError);
                    }
                    else
                    {
                        foreach (var op in ops)
                        {
                            if (op.OperationKind == CounterOp.EXIT)
                            {
                                if (!op.Counter.Equals(sc))
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                ops1.Add(new CounterOperation(sc1, CounterOp.EXIT));
                            }
                            else if (op.OperationKind == CounterOp.SET0)
                            {
                                if (!op.Counter.Equals(tc))
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                ops1.Add(new CounterOperation(tc1, CounterOp.SET0));
                            }
                            else if (op.OperationKind == CounterOp.SET1)
                            {
                                if (!op.Counter.Equals(tc))
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                ops1.Add(new CounterOperation(tc1, CounterOp.SET1));
                            }
                            else if (op.OperationKind == CounterOp.INCR)
                            {
                                if (!op.Counter.Equals(tc))
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                ops1.Add(new CounterOperation(tc1, CounterOp.SET1));
                            }
                            else if (op.OperationKind == CounterOp.EXIT_SET0)
                            {
                                if (ops.Length > 1)
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                ops1.Add(new CounterOperation(sc1, CounterOp.EXIT));
                                ops1.Add(new CounterOperation(tc1, CounterOp.SET0));
                            }
                            else if (op.OperationKind == CounterOp.EXIT_SET1)
                            {
                                if (ops.Length > 1)
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                ops1.Add(new CounterOperation(sc1, CounterOp.EXIT));
                                ops1.Add(new CounterOperation(tc1, CounterOp.SET1));
                            }
                            else
                            {
                                throw new AutomataException(AutomataExceptionKind.InternalError);
                            }
                        }
                    }
                    var seq = new Sequence<CounterOperation>(ops1.ToArray());
                    if (s != t && tc1 != null)
                    {
                        if (!seq.Exists(x => x.Counter.CounterId == tc1.CounterId))
                        {
                            //t is a counting state but its counter is not being initialized
                            seq = seq.Append(new CounterOperation(tc1, CounterOp.SET0));
                        }
                    }

                    return seq;
                };

                foreach (var move in moves)
                {
                    var ops = F(move.SourceState, move.TargetState, move.Label.Item2);
                    var lab = new Tuple<Maybe<S>, Sequence<CounterOperation>>(move.Label.Item1, ops);
                    var move1 = Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(move.SourceState, move.TargetState, lab);
                    moves1.Add(move1);
                }
                #endregion
                countingStates = newCounters1;
            }
            else
            {
                countingStates = null;
            }
            var aut = Automaton<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(new CABA<S>(builder), 0, finalStates, moves1);
            return aut;
        }

        private static Move<Tuple<Maybe<S>, Sequence<CounterOperation>>> MkMove(Move<Tuple<Maybe<S>, Sequence<CounterOperation>>> move, params CounterOperation[] ops)
        {
            var lab = new Tuple<Maybe<S>, Sequence<CounterOperation>>(move.Label.Item1, new Sequence<CounterOperation>(ops));
            var move1 = Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(move.SourceState, move.TargetState, lab);
            return move1;
        }

        //internal Sequence<CounterOperation> GetCounterInitConditions()
        //{
        //    if (kind == SymbolicRegexKind.Loop && lower > 0 && !IsPlus && !IsMaybe)
        //    {
        //        var bodyinit = left.GetCounterInitConditions();
        //        var init = bodyinit.Append(new CounterOperation(this, CounterOp.SET0));
        //        return init;
        //    }
        //    else
        //    {
        //        return Sequence<CounterOperation>.Empty;
        //    }
        //}

        /// <summary>
        /// Convert the regex to a counting automaton
        /// </summary>
        /// <param name="makeMonadic">if true then nonmonadic loops are removed</param>
        public CountingAutomaton<S> CreateCountingAutomaton(bool makeMonadic = true)
        {
            SymbolicRegexNode<S> node = (makeMonadic ? this.MkMonadic() : this);
            Dictionary<int, ICounter> countingStates;
            Dictionary<int, SymbolicRegexNode<S>> stateMap;
            var aut = node.Explore(out countingStates, out stateMap, makeMonadic);
            var ca = new CountingAutomaton<S>(aut, stateMap, countingStates);
            return ca;
        }

        /// <summary>
        /// true iff epsilon is accepted
        /// </summary>
        public bool IsNullable
        {
            get
            {
                return isNullable;
            }
        }

        ///// <summary>
        ///// true if epsilon is accepted at the start or at the end
        ///// </summary>
        ///// <param name="isStart">if true then returns true for start anchor</param>
        ///// <param name="isEnd">if true then returns true for end anchor</param>
        ///// <returns></returns>
        //bool IsNullableAtBorder(bool isStart, bool isEnd)
        //{
        //    if (isNullable)
        //        return true;
        //    else
        //        switch (kind)
        //        {
        //            case SymbolicRegexKind.StartAnchor:
        //                return isStart;
        //            case SymbolicRegexKind.EndAnchor:
        //                return isEnd;
        //            case SymbolicRegexKind.Epsilon:
        //                return true;
        //            case SymbolicRegexKind.WatchDog:
        //                return true;
        //            case SymbolicRegexKind.Singleton:
        //                return false;
        //            case SymbolicRegexKind.Loop:
        //                return this.left.IsNullableAtBorder(isStart, isEnd);
        //            case SymbolicRegexKind.Or:
        //            case SymbolicRegexKind.And:
        //                return this.alts.IsNullable(isStart, isEnd);
        //            case SymbolicRegexKind.Concat:
        //                return this.left.IsNullableAtBorder(isStart, isEnd) && this.right.IsNullableAtBorder(isStart, isEnd);
        //            default: //ITE
        //                return (this.iteCond.IsNullableAtBorder(isStart, isEnd) ? this.left.IsNullableAtBorder(isStart, isEnd) : this.right.IsNullableAtBorder(isStart, isEnd));
        //        }
        //}

        [NonSerialized]
        static int prime = 31;
        public override int GetHashCode()
        {
            if (hashcode == -1)
            {
                switch (kind)
                {
                    case SymbolicRegexKind.EndAnchor:
                    case SymbolicRegexKind.StartAnchor:
                    case SymbolicRegexKind.Epsilon:
                        hashcode = kind.GetHashCode();
                        break;
                    case SymbolicRegexKind.WatchDog:
                        hashcode = kind.GetHashCode() + lower;
                        break;
                    case SymbolicRegexKind.Loop:
                        hashcode = kind.GetHashCode() ^ left.GetHashCode() ^ lower ^ upper ^ isLazyLoop.GetHashCode();
                        break;
                    case SymbolicRegexKind.Or:
                    case SymbolicRegexKind.And:
                        hashcode = kind.GetHashCode() ^ alts.GetHashCode();
                        break;
                    case SymbolicRegexKind.Concat:
                        hashcode = left.GetHashCode() + (prime * right.GetHashCode());
                        break;
                    case SymbolicRegexKind.Singleton:
                        hashcode = kind.GetHashCode() ^ set.GetHashCode();
                        break;
                    //case SymbolicRegexKind.Sequence:
                    //    hashcode = kind.GetHashCode() ^ sequence.GetHashCode();
                    //    break;
                    default: //if-then-else
                        hashcode = kind.GetHashCode() ^ iteCond.GetHashCode() ^ (left.GetHashCode() << 1) ^ (right.GetHashCode() << 2);
                        break;
                }
            }
            return hashcode;
        }

        public override bool Equals(object obj)
        {
            SymbolicRegexNode<S> that = obj as SymbolicRegexNode<S>;
            if (that == null)
            {
                return false;
            }
            else if (this == that)
            {
                return true;
            }
            else
            {
                if (this.kind != that.kind)
                    return false;
                switch (this.kind)
                {
                    case SymbolicRegexKind.Concat:
                        return this.left.Equals(that.left) && this.right.Equals(that.right);
                    case SymbolicRegexKind.Singleton:
                        return object.Equals(this.set, that.set);
                    //case SymbolicRegexKind.Sequence:
                    //    return object.Equals(this.sequence, that.sequence);
                    case SymbolicRegexKind.Or:
                    case SymbolicRegexKind.And:
                        return this.alts.Equals(that.alts);
                    case SymbolicRegexKind.Loop:
                        return this.lower == that.lower && this.upper == that.upper && this.left.Equals(that.left);
                    case SymbolicRegexKind.IfThenElse:
                        return this.iteCond.Equals(that.iteCond) && this.left.Equals(that.left) && this.right.Equals(that.right);
                    default: //otherwsie this.kind == that.kind implies they must be the same
                        return true;
                }
            }
        }

        /// <summary>
        /// Returns the set of all predicates that occur in the regex
        /// </summary>
        public HashSet<S> GetPredicates()
        {
            var predicates = new HashSet<S>();
            CollectPredicates_helper(predicates);
            return predicates;
        }

        /// <summary>
        /// Collects all predicates that occur in the regex into the given set predicates
        /// </summary>
        void CollectPredicates_helper(HashSet<S> predicates)
        {
            switch (kind)
            {
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.WatchDog:
                    return;
                case SymbolicRegexKind.Singleton:
                    {
                        predicates.Add(this.set);
                        return;
                    }
                case SymbolicRegexKind.Loop:
                    {
                        this.left.CollectPredicates_helper(predicates);
                        return;
                    }
                case SymbolicRegexKind.Or:
                case SymbolicRegexKind.And:
                    {
                        foreach (SymbolicRegexNode<S> sr in this.alts)
                            sr.CollectPredicates_helper(predicates);
                        return;
                    }
                case SymbolicRegexKind.Concat:
                    {
                        left.CollectPredicates_helper(predicates);
                        right.CollectPredicates_helper(predicates);
                        return;
                    }
                default: //ITE
                    {
                        this.iteCond.CollectPredicates_helper(predicates);
                        this.left.CollectPredicates_helper(predicates);
                        this.right.CollectPredicates_helper(predicates);
                        return;
                    }
            }
        }

        /// <summary>
        /// Compute all the minterms from the predicates in this regex.
        /// If S implements IComparable then sort the result in increasing order.
        /// </summary>
        public S[] ComputeMinterms()
        {
            var predicates = new List<S>(GetPredicates());
            var mt = new List<S>(EnumerateMinterms(predicates.ToArray()));
            //there must be at least one minterm
            if (mt.Count == 0)
                throw new AutomataException(AutomataExceptionKind.InternalError_SymbolicRegex);
            if (mt[0] is IComparable)
                mt.Sort();
            var minterms = mt.ToArray();
            return minterms;
        }

        IEnumerable<S> EnumerateMinterms(S[] preds)
        {
            foreach (var pair in builder.solver.GenerateMinterms(preds))
                yield return pair.Item2;
        }

        /// <summary>
        /// Create the reverse of this regex
        /// </summary>
        public SymbolicRegexNode<S> Reverse()
        {
            switch (kind)
            {
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.Singleton:
                    return this;
                case SymbolicRegexKind.WatchDog:
                    return builder.epsilon;
                case SymbolicRegexKind.StartAnchor:
                    return builder.endAnchor;
                case SymbolicRegexKind.EndAnchor:
                    return builder.startAnchor;
                case SymbolicRegexKind.Loop:
                    return builder.MkLoop(this.left.Reverse(), this.isLazyLoop, this.lower, this.upper);
                case SymbolicRegexKind.Concat:
                    {
                        var rev = left.Reverse();
                        var rest = this.right;
                        while (rest.kind == SymbolicRegexKind.Concat)
                        {
                            var rev1 = rest.left.Reverse();
                            rev = builder.MkConcat(rev1, rev);
                            rest = rest.right;
                        }
                        var restr = rest.Reverse();
                        rev = builder.MkConcat(restr, rev);
                        return rev;
                    }
                case SymbolicRegexKind.Or:
                    {
                        var rev = builder.MkOr(alts.Reverse());
                        return rev;
                    }
                case SymbolicRegexKind.And:
                    {
                        var rev = builder.MkAnd(alts.Reverse());
                        return rev;
                    }
                default: //if-then-else
                    return builder.MkIfThenElse(iteCond.Reverse(), left.Reverse(), right.Reverse());
            }
        }

        internal string GenerateRandomMember(int maxLoopUnrol = 10, int cornerCaseProb = 5, int maxSamplingIter = 3)
        {
            var sampler = new SymbolicRegexSampler<S>(this.builder, this, maxLoopUnrol, cornerCaseProb, maxSamplingIter);
            return sampler.GenerateRandomMember();
        }

        internal HashSet<string> GetPositiveDataset(SymbolicRegexBuilder<S> builder, int size, /* string charClassRestriction = null, */ int maxLoopUnrol = 10, int cornerCaseProb = 5, int maxSamplingIter = 3)
        {
            var sr = this;
            //if (charClassRestriction != null)
            //{
            //    var pred_bdd = this.builder.solver.CharSetProvider.MkCharSetFromRegexCharClass(charClassRestriction);
            //    var pred_S = this.builder.solver.ConvertFromCharSet(pred_bdd);
            //    sr = this.Restrict(pred_S);
            //}
            var sampler = new SymbolicRegexSampler<S>(builder, sr, maxLoopUnrol, cornerCaseProb, maxSamplingIter);
            return sampler.GetPositiveDataset(size);
        }

        internal bool StartsWithLoop(int upperBoundLowestValue = 1)
        {
            switch (kind)
            {
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.Singleton: 
                case SymbolicRegexKind.WatchDog:
                case SymbolicRegexKind.Epsilon:
                    return false;
                case SymbolicRegexKind.Loop:
                    return (this.upper < int.MaxValue) && (this.upper > upperBoundLowestValue);
                case SymbolicRegexKind.Concat:
                    return (this.left.StartsWithLoop(upperBoundLowestValue) ||
                        (this.left.isNullable && this.right.StartsWithLoop(upperBoundLowestValue)));
                case SymbolicRegexKind.Or:
                    return alts.StartsWithLoop(upperBoundLowestValue);
                default:
                    throw new NotImplementedException();
            }
        }

        int enabledBoundedLoopCount = -1;

        internal int EnabledBoundedLoopCount
        {
            get
            {
                if (enabledBoundedLoopCount == -1)
                {
                    switch (kind)
                    {
                        case SymbolicRegexKind.EndAnchor:
                        case SymbolicRegexKind.StartAnchor:
                        case SymbolicRegexKind.Singleton:
                        case SymbolicRegexKind.WatchDog:
                        case SymbolicRegexKind.Epsilon:
                            {
                                enabledBoundedLoopCount = 0;
                                break;
                            }
                        case SymbolicRegexKind.Loop:
                            {
                                //nr of loops in the body
                                int n = this.left.EnabledBoundedLoopCount;
                                if ((this.upper < int.MaxValue) && (this.upper > 0))
                                    n += 1;
                                enabledBoundedLoopCount = n;
                                break;
                            }
                        case SymbolicRegexKind.Concat:
                            {
                                int n = this.left.EnabledBoundedLoopCount;
                                //if (this.left.IsNullable())
                                //    n += this.right.EnabledBoundedLoopCount;
                                enabledBoundedLoopCount = n;
                                break;
                            }
                        case SymbolicRegexKind.Or:
                            {
                                enabledBoundedLoopCount = alts.EnabledBoundedLoopCount;
                                break;
                            }
                        default:
                            throw new NotImplementedException(kind.ToString());
                    }
                }
                return enabledBoundedLoopCount;
            }
        }

        internal int EnabledBoundedLoopValue()
        {

            switch (kind)
            {
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.Singleton:
                case SymbolicRegexKind.WatchDog:
                case SymbolicRegexKind.Epsilon:
                    {
                        return 0;
                    }
                case SymbolicRegexKind.Loop:
                    {
                        if (this.upper < int.MaxValue)
                            return this.upper;
                        else
                            return 0;
                    }
                case SymbolicRegexKind.Concat:
                    {
                        return this.left.EnabledBoundedLoopValue();
                    }
                case SymbolicRegexKind.Or:
                    {
                        foreach (var alt in this.alts)
                        {
                            var k = alt.EnabledBoundedLoopValue();
                            if (k > 0)
                                return k;
                        }
                        return 0;
                    }
                default:
                    throw new NotImplementedException(kind.ToString());
            }
        }

        /// <summary>
        /// Unwind lower loop boundaries
        /// </summary>
        internal SymbolicRegexNode<S> Simplify()
        {
            switch (kind)
            {
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.Singleton:
                case SymbolicRegexKind.WatchDog:
                    return this;
                case SymbolicRegexKind.Concat:
                    return builder.MkConcat(left.Simplify(), right.Simplify());
                case SymbolicRegexKind.Or:
                    return builder.MkOr(alts.Simplify());
                case SymbolicRegexKind.And:
                    return builder.MkAnd(alts.Simplify());
                case SymbolicRegexKind.Loop:
                    {
                        var body = this.left.Simplify();
                        //we know that lower <= upper
                        //so diff >= 0
                        int diff = (this.upper == int.MaxValue ? int.MaxValue : upper - lower);
                        var res = (diff == 0 ? builder.epsilon : builder.MkLoop(body, isLazyLoop, 0, diff));
                        for (int i = 0; i < lower; i++)
                            res = builder.MkConcat(body, res);
                        return res;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Only valid to call if there is a single bounded loop
        /// </summary>
        internal SymbolicRegexNode<S> DecrementBoundedLoopCount(bool makeZero = false)
        {
            if (EnabledBoundedLoopCount != 1)
                return this;
            else
            {
                switch (kind)
                {
                    case SymbolicRegexKind.EndAnchor:
                    case SymbolicRegexKind.StartAnchor:
                    case SymbolicRegexKind.Singleton:
                    case SymbolicRegexKind.WatchDog:
                    case SymbolicRegexKind.Epsilon:
                        {
                            return this;
                        }
                    case SymbolicRegexKind.Loop:
                        {
                            if ((lower == 0) && (upper > 0) && (upper < int.MaxValue))
                            {
                                //must be this loop
                                if (makeZero)
                                    return builder.epsilon;
                                else
                                {
                                    int upper1 = upper - 1;
                                    return builder.MkLoop(this.left, this.isLazyLoop, 0, upper1);
                                }
                            }
                            else
                            {
                                return this;
                            }
                        }
                    case SymbolicRegexKind.Concat:
                        {
                            return builder.MkConcat(left.DecrementBoundedLoopCount(makeZero), right);
                        }
                    case SymbolicRegexKind.Or:
                        {
                            return builder.MkOr(alts.DecrementBoundedLoopCount(makeZero));
                        }
                    default:
                        throw new NotImplementedException(kind.ToString());
                }
            }
        }

        /// <summary>
        /// Gets the string prefix that the regex must match or the empty string if such a prefix does not exist.
        /// </summary>
        internal string GetFixedPrefix(CharSetSolver css, out bool ignoreCase)
        {
            var pref = GetFixedPrefix_(css, out ignoreCase);
            int i = pref.IndexOf('I');
            int k = pref.IndexOf('K');
            if (ignoreCase && (i != -1 || k != -1))
            {
                //eliminate I and K to avoid possible semantic discrepancy with later search
                //due to \u0130 (capital I with dot above, İ,  in regex same as i modulo ignore case)
                //due to \u212A (Kelvin sign, in regex same as k under ignore case)
                //but these do not match with string.IndexOf modulo ignore case
                if (k == -1)
                    return pref.Substring(0, i);
                else if (i == -1)
                    return pref.Substring(0, k);
                else
                    return pref.Substring(0, (i < k ? i : k));
            }
            else
            {
                return pref;
            }
        }

        string GetFixedPrefix_(CharSetSolver css, out bool ignoreCase)
        {
            #region compute fixedPrefix
            S[] prefix = GetPrefix();
            if (prefix.Length == 0)
            {
                ignoreCase = false;
                return string.Empty;
            }
            else
            {
                BDD[] bdds = Array.ConvertAll(prefix, p => builder.solver.ConvertToCharSet(css, p));
                if (Array.TrueForAll(bdds, x => css.IsSingleton(x)))
                {
                    //all elements are singletons
                    char[] chars = Array.ConvertAll(bdds, x => (char)x.GetMin());
                    ignoreCase = false;
                    return new string(chars);
                }
                else
                {
                    //maps x to itself if x is invariant under ignoring case
                    //maps x to False otherwise
                    Func<BDD, BDD> F = x =>
                    {
                        char c = (char)x.GetMin();
                        var y = css.MkCharConstraint(c, true);
                        if (x == y)
                            return x;
                        else
                            return css.False;
                    };
                    BDD[] bdds1 = Array.ConvertAll(bdds, x => F(x));
                    if (Array.TrueForAll(bdds1, x => !x.IsEmpty))
                    {
                        //all elements are singletons up-to-ignoring-case
                        //choose representatives
                        char[] chars = Array.ConvertAll(bdds, x => (char)x.GetMin());
                        ignoreCase = true;
                        return new string(chars);
                    }
                    else
                    {
                        List<char> elems = new List<char>();
                        //extract prefix of singletons
                        for (int i = 0; i < bdds.Length; i++)
                        {
                            if (css.IsSingleton(bdds[i]))
                                elems.Add((char)bdds[i].GetMin());
                            else
                                break;
                        }
                        List<char> elemsI = new List<char>();
                        //extract prefix up-to-ignoring-case 
                        for (int i = 0; i < bdds1.Length; i++)
                        {
                            if (bdds1[i].IsEmpty)
                                break;
                            else
                                elemsI.Add((char)bdds1[i].GetMin());
                        }
                        //TBD: these heuristics should be evaluated more
                        #region different cases of fixed prefix
                        if (elemsI.Count > elems.Count)
                        {
                            ignoreCase = true;
                            return new string(elemsI.ToArray());
                        }
                        else if (elems.Count > 0)
                        {
                            ignoreCase = false;
                            return new string(elems.ToArray());
                        }
                        else if (elemsI.Count > 0)
                        {
                            ignoreCase = true;
                            return new string(elemsI.ToArray());
                        }
                        else
                        {
                            ignoreCase = false;
                            return string.Empty;
                        }
                        #endregion
                    }
                }
            }
            #endregion
        }

        internal const int maxPrefixLength = 5;
        internal S[] GetPrefix()
        {
            return GetPrefixSequence(Sequence<S>.Empty, maxPrefixLength).ToArray();
        }

        Sequence<S> GetPrefixSequence(Sequence<S> pref, int lengthBound)
        {
            if (lengthBound == 0)
            {
                return pref;
            }
            else
            {
                switch (this.kind)
                {
                    case SymbolicRegexKind.Singleton:
                        {
                            return pref.Append(this.set);
                        }
                    case SymbolicRegexKind.Concat:
                        {
                            if (this.left.kind == SymbolicRegexKind.Singleton)
                                return this.right.GetPrefixSequence(pref.Append(this.left.set), lengthBound - 1);
                            else
                                return pref;
                        }
                    case SymbolicRegexKind.Or:
                    case SymbolicRegexKind.And:
                        {
                            var enumerator = alts.GetEnumerator();
                            enumerator.MoveNext();
                            var alts_prefix = enumerator.Current.GetPrefixSequence(Sequence<S>.Empty, lengthBound);
                            while (!alts_prefix.IsEmpty && enumerator.MoveNext())
                            {
                                var p = enumerator.Current.GetPrefixSequence(Sequence<S>.Empty, lengthBound);
                                alts_prefix = alts_prefix.MaximalCommonPrefix(p);
                            }
                            return alts_prefix;
                        }
                    default:
                        {
                            return pref;
                        }
                }
            }
        }

        ///// <summary>
        ///// If this node starts with a loop other than star or plus then 
        ///// returns the nonnegative id of the associated counter else returns -1
        ///// </summary>
        //public int CounterId
        //{
        //    get
        //    {
        //        if (this.kind == SymbolicRegexKind.Loop && !this.IsStar && !this.IsPlus)
        //            return this.builder.GetCounterId(this);
        //        else if (this.kind == SymbolicRegexKind.Concat)
        //            return left.CounterId;
        //        else
        //            return -1;
        //    }
        //}

        //0 means value is not computed, 
        //-1 means this is not a sequence of singletons
        //1 means it is a sequence of singletons
        internal int sequenceOfSingletons_count = 0;

        /// <summary>
        /// true if this node is a lazy loop
        /// </summary>
        internal bool isLazyLoop = false;

        internal bool IsSequenceOfSingletons
        {
            get
            {
                if (sequenceOfSingletons_count == 0)
                {
                    var node = this;
                    int k = 1;
                    while (node.kind == SymbolicRegexKind.Concat && node.left.kind == SymbolicRegexKind.Singleton)
                    {
                        node = node.right;
                        k += 1;
                    }
                    if (node.kind == SymbolicRegexKind.Singleton)
                    {
                        node.sequenceOfSingletons_count = 1;
                        node = this;
                        while (node.kind == SymbolicRegexKind.Concat)
                        {
                            node.sequenceOfSingletons_count = k;
                            node = node.right;
                            k = k - 1;
                        }
                    }
                    else
                    {
                        node.sequenceOfSingletons_count = -1;
                        node = this;
                        while (node.kind == SymbolicRegexKind.Concat && node.left.kind == SymbolicRegexKind.Singleton)
                        {
                            node.sequenceOfSingletons_count = -1;
                            node = node.right;
                        }
                    }
                }
                return sequenceOfSingletons_count > 0;
            }
        }

        /// <summary>
        /// Gets the predicate that covers all elements that make some progress. 
        /// </summary>
        public S GetStartSet(ICharAlgebra<S> algebra)
        {
            switch (kind)
            {
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.WatchDog:
                case SymbolicRegexKind.EndAnchor:
                    return algebra.False;
                case SymbolicRegexKind.Singleton:
                    return this.set;
                //case SymbolicRegexKind.Sequence:
                //    return this.sequence.First;
                case SymbolicRegexKind.Loop:
                    return this.left.GetStartSet(algebra);
                case SymbolicRegexKind.Concat:
                    {
                        var startSet = this.left.GetStartSet(algebra);
                        if (left.isNullable || left.kind == SymbolicRegexKind.StartAnchor)
                        {
                            var set2 = this.right.GetStartSet(algebra);
                            startSet = algebra.MkOr(startSet, set2);
                        }
                        return startSet;
                    }
                case SymbolicRegexKind.Or:
                    {
                        S startSet = algebra.False;
                        foreach (var alt in alts)
                            startSet = algebra.MkOr(startSet, alt.GetStartSet(algebra));
                        return startSet;
                    }
                case SymbolicRegexKind.And:
                    {
                        S startSet = algebra.True;
                        foreach (var alt in alts)
                            startSet = algebra.MkAnd(startSet, alt.GetStartSet(algebra));
                        return startSet;
                    }
                default: //if-then-else
                    {
                        S startSet = algebra.MkOr(iteCond.GetStartSet(algebra), algebra.MkOr(left.GetStartSet(algebra), right.GetStartSet(algebra)));
                        return startSet;
                    }
            }
        }

        /// <summary>
        /// Unwind the regex and show the resulting state graph
        /// </summary>
        /// <param name="bound">roughly the maximum number of states</param>
        /// <param name="name">name for the graph, used also as .dgml file name</param>
        /// <param name="matchAnywhere">if true then pretend that there is a .* at the beginning</param>
        /// <param name="hideDerivatives">if true then hide derivatives in state labels</param>
        public void ShowGraph(int bound = 0, string name = "DFA", bool matchAnywhere = false, bool hideDerivatives = false)
        {
            DirectedGraphs.DgmlWriter.ShowGraph<S>(-1, Unwind(bound, matchAnywhere, hideDerivatives), name);
        }

        /// <summary>
        /// Unwind the regex into an automaton that can be displayed as a graph.
        /// </summary>
        /// <param name="bound">roughly the maximum number of states, 0 or negative value means no bound</param>
        public IAutomaton<S> Unwind(int bound = 0, bool dotStarAtStart = false, bool hideDerivatives = false)
        {
            return new SymbolicRegexGraph(builder, this, bound, dotStarAtStart, hideDerivatives);
        }

        //public bool ContainsSubCounter(ICounter subcounter)
        //{
        //    return !subcounter.Equals(this) &&
        //        this.ExistsNode(x => x.kind == SymbolicRegexKind.Loop && x.Equals(subcounter));
        //}

        internal class SymbolicRegexGraph : IAutomaton<S>
        {
            SymbolicRegexNode<S> sr;
            Dictionary<SymbolicRegexNode<S>, int> stateIdMap = new Dictionary<SymbolicRegexNode<S>, int>();
            Dictionary<int, SymbolicRegexNode<S>> states = new Dictionary<int, SymbolicRegexNode<S>>();
            Dictionary<Tuple<int, int>, S> normalizedmoves = new Dictionary<Tuple<int, int>, S>();
            ICharAlgebra<S> bva;
            SymbolicRegexBuilder<S> builder;
            bool hideDerivatives;

            internal SymbolicRegexGraph(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> s, int bound, bool addDotStar, bool hideDerivatives)
            {
                this.builder = builder;
                this.hideDerivatives = hideDerivatives;
                if (addDotStar)
                    this.sr = builder.MkConcat(builder.dotStar, s);
                else
                    this.sr = s;
                if (!(builder.solver is ICharAlgebra<S>))
                    throw new AutomataException(AutomataExceptionKind.NotSupported);
                this.bva = builder.solver as ICharAlgebra<S>;

                Func<int, S, Tuple<int, S>> Pair = (x, y) => new Tuple<int, S>(x, y);

                stateIdMap[sr] = 0;
                states[0] = sr;
                var stack = new Stack<int>();
                stack.Push(0);
                int nextstate = 1;
                while (stack.Count > 0 && (bound <= 0 || nextstate <= bound))
                {
                    var q = stack.Pop();
                    foreach (var c in bva.GetPartition())
                    {
                        var q_deriv = states[q].MkDerivative(c);
                        //ignore the deadend state
                        if (q_deriv != builder.nothing)
                        {
                            int p;
                            if (!stateIdMap.TryGetValue(q_deriv, out p))
                            {
                                p = nextstate++;
                                stateIdMap[q_deriv] = p;
                                states[p] = q_deriv;
                                stack.Push(p);
                            }
                            var qp = new Tuple<int, int>(q,p);
                            if (normalizedmoves.ContainsKey(qp))
                                normalizedmoves[qp] = bva.MkOr(normalizedmoves[qp], c);
                            else
                                normalizedmoves[qp] = c;
                        }
                    }
                }
            }

            public IBooleanAlgebra<S> Algebra
            {
                get
                {
                    return builder.solver;
                }
            }

            public int InitialState
            {
                get
                {
                    return 0;
                }
            }

            public string DescribeLabel(S lab)
            {
                return builder.solver.PrettyPrint(lab);
            }

            public string DescribeStartLabel()
            {
                return "";
            }

            public string DescribeState(int state)
            {
                if (this.hideDerivatives)
                    return state.ToString();
                else
                    return states[state].ToString();
            }

            public IEnumerable<Move<S>> GetMoves()
            {
                foreach (var entry in normalizedmoves)
                    yield return Move<S>.Create(entry.Key.Item1, entry.Key.Item2, entry.Value);
            }

            public IEnumerable<Move<S>> GetMovesFrom(int state)
            {
                foreach (var entry in normalizedmoves)
                    if (entry.Key.Item1 == state)
                        yield return Move<S>.Create(entry.Key.Item1, entry.Key.Item2, entry.Value);
            }

            public IEnumerable<int> GetStates()
            {
                return states.Keys;
            }

            public bool IsFinalState(int state)
            {
                return states[state].isNullable;
            }
        }

        /// <summary>
        /// Returns true iff there exists a node that satisfies the predicate
        /// </summary>
        public bool ExistsNode(Predicate<SymbolicRegexNode<S>> pred)
        {
            if (pred(this))
                return true;
            else
                switch (kind)
                {
                    case SymbolicRegexKind.Concat:
                        return left.ExistsNode(pred) || right.ExistsNode(pred);
                    case SymbolicRegexKind.Or:
                    case SymbolicRegexKind.And:
                        foreach (var node in this.alts)
                            if (node.ExistsNode(pred))
                                return true;
                        return false;
                    case SymbolicRegexKind.Loop:
                        return left.ExistsNode(pred);
                    default:
                        return false;
                }
        }

        /// <summary>
        /// Returns true if this node represents a bounded loop whose body is nonmonadic.
        /// </summary>
        public bool IsNonmonadicBoundedLoop
        {
            get
            {
                return (this.kind == SymbolicRegexKind.Loop
                     && !this.IsStar && !this.IsMaybe
                     && this.left.kind != SymbolicRegexKind.Singleton);
            }
        }

        /// <summary>
        /// Returns true if this node is a monadic loop with lower bound but no upper bound.
        /// </summary>
        public bool IsMonadicLoopWithLowerButWihtoutUpperBound
        {
            get
            {
                return (this.kind == SymbolicRegexKind.Loop
                    && this.left.kind == SymbolicRegexKind.Singleton
                    && this.HasLowerBound && !this.HasUpperBound);
            }
        }

        public int CounterId
        {
            get
            {
                return builder.GetCounterId(this);
            }
        }

        ICounter GetBoundedCounter()
        {
            var node = this;
            while (node.Kind == SymbolicRegexKind.Concat)
                node = node.left;
            if (node.IsBoundedLoop)
                return node;
            else
                return null;
        }


        /// <summary>
        /// Unwinds nonmonadic bounded quantifiers, and splits monadic bounded quantifiers without upper bounds to a fixed loop followed by Kleene star
        /// </summary>
        public SymbolicRegexNode<S> MkMonadic()
        {
            if (ExistsNode(x => (x.IsNonmonadicBoundedLoop || x.IsMonadicLoopWithLowerButWihtoutUpperBound)))
                return UnwindNonmonadic_();
            else
                return this;
        }

        SymbolicRegexNode<S> UnwindNonmonadic_()
        {
            switch (kind)
            {
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.Singleton:
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.WatchDog:
                    {
                        return this;
                    }
                case SymbolicRegexKind.Loop:
                    {
                        if (left.kind == SymbolicRegexKind.Singleton)
                        {
                            #region monadic loop
                            if (upper == int.MaxValue)
                            {
                                //unbounded: split the loop into two parts
                                var firstLoop = builder.MkLoop(left, false, lower, lower);
                                var secondLoop = builder.MkLoop(left, isLazyLoop);
                                return builder.MkConcat(firstLoop, secondLoop);
                            }
                            else
                                return this;
                            #endregion
                        }
                        else
                        {
                            #region nonmonadic loop
                            var body = this.left.UnwindNonmonadic_();
                            if (IsStar)
                            {
                                if (body == this.left)
                                    return this;
                                else
                                    return builder.MkLoop(body, isLazyLoop);
                            }
                            else
                            {
                                //the actual unwinding of nonmonadic loops
                                var nodes = new SymbolicRegexNode<S>[upper < int.MaxValue ? upper : lower + 1];
                                for (int i = 0; i < lower; i++)
                                    nodes[i] = body;
                                if (upper < int.MaxValue)
                                { 
                                    var body_or_eps = builder.MkOr(body, builder.epsilon);
                                    for (int i = lower; i < upper; i++)
                                        nodes[i] = body_or_eps;
                                }
                                else
                                    nodes[lower] = builder.MkLoop(body, isLazyLoop);
                                return builder.MkConcat(nodes, false);
                            }
                            #endregion
                        }
                    }
                case SymbolicRegexKind.Concat:
                    {
                        var l = left.UnwindNonmonadic_();
                        var r = right.UnwindNonmonadic_();
                        if (l == left && r == right)
                            return this;
                        else
                            return builder.MkConcat(l, r);
                    }
                case SymbolicRegexKind.Or:
                    {
                        var members = new List<SymbolicRegexNode<S>>();
                        bool someNodeWasUnwinded = false;
                        foreach (var member in this.alts)
                        {
                            var member1 = member.UnwindNonmonadic_();
                            members.Add(member1);
                            if (member1 != member)
                                someNodeWasUnwinded = true;
                        }
                        if (someNodeWasUnwinded)
                            return builder.MkOr(members.ToArray());
                        else
                            return this;
                    }
                default:
                    throw new NotImplementedException(kind.ToString());
            }
        }

        /// <summary>
        /// Returns true if this is a loop with an upper bound
        /// </summary>
        public bool IsBoundedLoop
        {
            get
            {
                return (this.kind == SymbolicRegexKind.Loop && this.upper < int.MaxValue);
            }
        }

        public bool ContainsSubCounter(ICounter counter)
        {
            var node = this;
            if (this.IsBoundedLoop)
            {
                if (this.left.Equals(counter))
                    return true;
                else
                    return this.left.ContainsSubCounter(counter);
            }
            else
                return false;
        }

        /// <summary>
        /// Returns true if the match-end of this regex can be determined with a 
        /// single pass from the start. 
        /// </summary>
        public bool IsSinglePass
        {
            get
            {
                if (this.IsSequenceOfSingletons)
                    return true;
                else
                {
                    switch (kind)
                    {
                        case SymbolicRegexKind.Or:
                            {
                                foreach (var member in alts)
                                    if (!member.IsSinglePass)
                                        return false;
                                return true;
                            }
                        case SymbolicRegexKind.Concat:
                            {
                                return left.IsSinglePass && right.IsSinglePass;
                            }
                        default:
                            return false;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the regex contains a lazy loop
        /// </summary>
        public bool CheckIfContainsLazyLoop()
        {
            return this.ExistsNode(node => (node.kind == SymbolicRegexKind.Loop && node.isLazyLoop));
        }

        /// <summary>
        /// Returns true if there are no loops or if all loops are lazy. 
        /// </summary>
        public bool CheckIfAllLoopsAreLazy()
        {
            bool existsEagerLoop =  this.ExistsNode(node => (node.kind == SymbolicRegexKind.Loop && !node.IsMaybe && !node.isLazyLoop));
            return !existsEagerLoop;
        }

        /// <summary>
        /// Returns true if there is a loop
        /// </summary>
        public bool CheckIfLoopExists()
        {
            bool existsLoop = this.ExistsNode(node => (node.kind == SymbolicRegexKind.Loop));
            return existsLoop;
        }
    }

    /// <summary>
    /// The kind of a symbolic regex set
    /// </summary>
    public enum SymbolicRegexSetKind { Conjunction, Disjunction };

    /// <summary>
    /// Represents a set of symbolic regexes that is either a disjunction or a conjunction
    /// </summary>
    public class SymbolicRegexSet<S> : IEnumerable<SymbolicRegexNode<S>>
    {
        internal static bool optimizeLoops = true;
        internal SymbolicRegexBuilder<S> builder;

        HashSet<SymbolicRegexNode<S>> set;
        //if the set kind is disjunction then
        //symbolic regex A{0,k}B is stored as (A,B) -> k
        //symbolic regex A{0,k} is stored as (A,()) -> k
        Dictionary<Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>, int> loops;

        internal SymbolicRegexSetKind kind;

        int hashCode = 0;

        //#region serialization
        ///// <summary>
        ///// Serialize
        ///// </summary>
        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    //var ctx = context.Context as SymbolicRegexBuilder<S>;
        //    //if (ctx == null || ctx != builder)
        //    //    throw new AutomataException(AutomataExceptionKind.InvalidSerializationContext);
        //    info.AddValue("loops", loops);
        //    info.AddValue("set", set);
        //    info.AddValue("kind", kind);
        //}

        ///// <summary>
        ///// Deserialize
        ///// </summary>
        //public SymbolicRegexSet(SerializationInfo info, StreamingContext context)
        //{
        //    builder = context.Context as SymbolicRegexBuilder<S>;
        //    if (builder == null)
        //        throw new AutomataException(AutomataExceptionKind.SerializationNotSupported);

        //    kind = (SymbolicRegexSetKind)info.GetValue("kind", typeof(SymbolicRegexSetKind));
        //    set = (HashSet<SymbolicRegexNode<S>>)info.GetValue("set", typeof(HashSet<SymbolicRegexNode<S>>));
        //    loops = (Dictionary<Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>, int>)info.GetValue("loops", typeof(Dictionary<Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>, int>));
        //}
        //#endregion

        public SymbolicRegexSetKind Kind
        {
            get { return kind; }
        }

        /// <summary>
        /// if >= 0 then the maximal length of a watchdog in the set
        /// </summary>
        internal int watchdog = -1;

        /// <summary>
        /// Denotes the empty conjunction
        /// </summary>
        public bool IsEverything
        {
            get { return this.kind == SymbolicRegexSetKind.Conjunction && this.set.Count == 0 && this.loops.Count == 0; }
        }

        /// <summary>
        /// Denotes the empty disjunction
        /// </summary>
        public bool IsNothing
        {
            get { return this.kind == SymbolicRegexSetKind.Disjunction && this.set.Count == 0 && this.loops.Count == 0; }
        }

        private SymbolicRegexSet(SymbolicRegexBuilder<S> builder, SymbolicRegexSetKind kind)
        {
            this.builder = builder;
            this.kind = kind;
            this.set = new HashSet<SymbolicRegexNode<S>>();
            this.loops = new Dictionary<Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>, int>();
        }

        private SymbolicRegexSet(SymbolicRegexBuilder<S> builder, SymbolicRegexSetKind kind, HashSet<SymbolicRegexNode<S>> set, Dictionary<Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>, int> loops)
        {
            this.builder = builder;
            this.kind = kind;
            this.set = set;
            this.loops = loops;
        }

        internal static SymbolicRegexSet<S> MkFullSet(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegexSet<S>(builder, SymbolicRegexSetKind.Conjunction);
        }

        internal static SymbolicRegexSet<S> MkEmptySet(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegexSet<S>(builder, SymbolicRegexSetKind.Disjunction);
        }

        static internal SymbolicRegexSet<S> CreateDisjunction(SymbolicRegexBuilder<S> builder, IEnumerable<SymbolicRegexNode<S>> elems)
        {
            var loops = new Dictionary<Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>, int>();
            var other = new HashSet<SymbolicRegexNode<S>>();
            int watchdog = -1;
            if (optimizeLoops)
            { 
                foreach (var elem in elems)
                {
                    //keep track of maximal watchdog in the set
                    if (elem.kind == SymbolicRegexKind.WatchDog && elem.lower > watchdog)
                        watchdog = elem.lower;

                    #region start foreach
                    if (elem == builder.dotStar)
                        return builder.fullSet;
                    else if (elem != builder.nothing)
                    {
                        switch (elem.kind)
                        {
                            case SymbolicRegexKind.Or:
                                {
                                    foreach (var alt in elem.alts)
                                    {
                                        if (alt.kind == SymbolicRegexKind.Loop && alt.lower == 0)
                                        {
                                            var pair = new Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>(alt.left, builder.epsilon);
                                            //map to the maximal of the upper bounds
                                            int cnt;
                                            if (loops.TryGetValue(pair, out cnt))
                                            {
                                                if (cnt < alt.upper)
                                                    loops[pair] = alt.upper;
                                            }
                                            else
                                            {
                                                loops[pair] = alt.upper;
                                            }
                                        }
                                        else if (alt.kind == SymbolicRegexKind.Concat && alt.left.kind == SymbolicRegexKind.Loop && alt.left.lower == 0)
                                        {
                                            var pair = new Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>(alt.left.left, alt.right);
                                            //map to the maximal of the upper bounds
                                            int cnt;
                                            if (loops.TryGetValue(pair, out cnt))
                                            {
                                                if (cnt < alt.left.upper)
                                                    loops[pair] = alt.left.upper;
                                            }
                                            else
                                            {
                                                loops[pair] = alt.left.upper;
                                            }
                                        }
                                        else
                                        {
                                            other.Add(alt);
                                        }
                                    }
                                    break;
                                }
                            case SymbolicRegexKind.Loop:
                                {
                                    if (elem.kind == SymbolicRegexKind.Loop && elem.lower == 0)
                                    {
                                        var pair = new Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>(elem.left, builder.epsilon);
                                        //map the body of the loop (elem.left) to the maximal of the upper bounds
                                        int cnt;
                                        if (loops.TryGetValue(pair, out cnt))
                                        {
                                            if (cnt < elem.upper)
                                                loops[pair] = elem.upper;
                                        }
                                        else
                                        {
                                            loops[pair] = elem.upper;
                                        }
                                    }
                                    else
                                    {
                                        other.Add(elem);
                                    }
                                    break;
                                }
                            case SymbolicRegexKind.Concat:
                                {
                                    if (elem.kind == SymbolicRegexKind.Concat && elem.left.kind == SymbolicRegexKind.Loop && elem.left.lower == 0)
                                    {
                                        var pair = new Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>(elem.left.left, elem.right);
                                        //map to the maximal of the upper bounds
                                        int cnt;
                                        if (loops.TryGetValue(pair, out cnt))
                                        {
                                            if (cnt < elem.left.upper)
                                                loops[pair] = elem.left.upper;
                                        }
                                        else
                                        {
                                            loops[pair] = elem.left.upper;
                                        }
                                    }
                                    else
                                    {
                                        other.Add(elem);
                                    }
                                    break;
                                }
                            default:
                                {
                                    other.Add(elem);
                                    break;
                                }
                        }
                    }
                    #endregion
                }
                //if any element of other is covered in loops then omit it
                var others1 = new HashSet<SymbolicRegexNode<S>>();
                foreach (var sr in other)
                {
                    var key = new Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>(sr, builder.epsilon);
                    if (loops.ContainsKey(key))
                        others1.Add(sr);
                }
                foreach (var pair in loops)
                {
                    if (other.Contains(pair.Key.Item2))
                        others1.Add(pair.Key.Item2);
                }
                other.ExceptWith(others1);
            }
            else
            {
                foreach (var elem in elems)
                {
                    if (elem.kind == SymbolicRegexKind.Or)
                    {
                        other.UnionWith(elem.alts);
                    }
                    else
                    {
                        other.Add(elem);
                    }
                }
            }
            if (other.Count == 0 && loops.Count == 0)
                return builder.emptySet;
            else
            {
                var disj = new SymbolicRegexSet<S>(builder, SymbolicRegexSetKind.Disjunction, other, loops);
                disj.watchdog = watchdog;
                return disj;
            }
        }

        static internal SymbolicRegexSet<S> CreateConjunction(SymbolicRegexBuilder<S> builder, IEnumerable<SymbolicRegexNode<S>> elems)
        {
            var loops = new Dictionary<Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>, int>();
            var conjuncts = new HashSet<SymbolicRegexNode<S>>();
            foreach (var elem in elems)
            {
                if (elem == builder.nothing)
                    return builder.emptySet;
                if (elem == builder.dotStar)
                    continue;
                if (elem.kind == SymbolicRegexKind.And)
                {
                    conjuncts.UnionWith(elem.alts);
                }
                else
                {
                    conjuncts.Add(elem);
                }
            }
            if (conjuncts.Count == 0)
                return builder.fullSet;
            else
                return new SymbolicRegexSet<S>(builder, SymbolicRegexSetKind.Conjunction, conjuncts, loops);
        }

        IEnumerable<SymbolicRegexNode<S>> RestrictElems(S pred)
        {
            foreach (var elem in this)
                yield return elem.Restrict(pred);
        }

        public SymbolicRegexSet<S> Restrict(S pred)
        {
            if (kind == SymbolicRegexSetKind.Disjunction)
                return CreateDisjunction(builder, RestrictElems(pred));
            else
                return CreateConjunction(builder, RestrictElems(pred));
        }

        /// <summary>
        /// How many elements are there in this set
        /// </summary>
        public int Count
        {
            get
            {
                return set.Count + loops.Count;
            }
        }

        /// <summary>
        /// True iff the set is a singleton
        /// </summary>
        public bool IsSigleton
        {
            get
            {
                return this.Count == 1;
            }
        }

        internal bool IsNullable()
        {
            var e = this.GetEnumerator();
            if (kind == SymbolicRegexSetKind.Disjunction)
            {
                #region some element must be nullable
                while (e.MoveNext())
                {
                    if (e.Current.IsNullable)
                        return true;
                }
                return false;
                #endregion
            }
            else
            {
                #region  all elements must be nullable
                while (e.MoveNext())
                {
                    if (!e.Current.IsNullable)
                        return false;
                }
                return true;
                #endregion
            }
        }

        public override int GetHashCode()
        {
            if (hashCode == 0)
            {
                hashCode = this.kind.GetHashCode();
                var e = set.GetEnumerator();
                while (e.MoveNext())
                {
                    hashCode = hashCode ^ e.Current.GetHashCode();
                }
                e.Dispose();
                var e2 = loops.GetEnumerator();
                while (e2.MoveNext())
                {
                    hashCode = (hashCode ^ (e2.Current.Key.GetHashCode() + e2.Current.Value));
                }
            }
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            var that = obj as SymbolicRegexSet<S>;
            if (that == null)
                return false;
            if (this.kind != that.kind)
                return false;
            if (this.set.Count != that.set.Count)
                return false;
            if (this.loops.Count != that.loops.Count)
                return false;
            if (this.set.Count > 0 && !this.set.SetEquals(that.set))
                return false;
            var e1 = this.loops.GetEnumerator();
            while (e1.MoveNext())
            {
                int cnt;
                if (!that.loops.TryGetValue(e1.Current.Key, out cnt))
                    return false;
                if (cnt != e1.Current.Value)
                    return false;
            }
            e1.Dispose();
            return true;
        }

        public override string ToString()
        {
            string res = "";
            var e = this.GetEnumerator();
            var R = new List<string>();
            while (e.MoveNext())
                R.Add(e.Current.ToString());
            R.Sort();
            if (R.Count == 0)
                return res;
            if (kind == SymbolicRegexSetKind.Disjunction)
            {
                #region display as R[0]|R[1]|...
                for (int i = 0; i < R.Count; i++)
                {
                    if (res != "")
                        res += "|";
                    res += R[i].ToString();
                }
                #endregion
            }
            else
            {
                #region display using if-then-else construct: (?(A)(B)|[0-[0]]) to represent intersect(A,B)
                res = R[R.Count - 1].ToString();
                for (int i = R.Count - 2; i >= 0; i--)
                {
                    //unfortunately [] is an invalid character class expression, using [0-[0]] instead
                    res = string.Format("(?({0})({1})|{2})", R[i].ToString(), res, "[0-[0]]");
                }
                #endregion
            }
            //if (this.Count > 1 && kind == SymbolicRegexSetKind.Disjunction)
            //    //add extra parentesis to enclose the disjunction
            //    return "(" + res + ")";
            //else
            return res;
        }

        internal SymbolicRegexNode<S>[] ToArray(SymbolicRegexBuilder<S> builder)
        {
            List<SymbolicRegexNode<S>> elemsL = new List<SymbolicRegexNode<S>>(this);
            SymbolicRegexNode<S>[] elems = elemsL.ToArray();
            return elems;
        }

        IEnumerable<SymbolicRegexNode<S>> RemoveAnchorsElems(SymbolicRegexBuilder<S> builder, bool isBeg, bool isEnd)
        {
            foreach (var elem in this)
                yield return elem.ReplaceAnchors(isBeg, isEnd);
        }

        public SymbolicRegexSet<S> RemoveAnchors(bool isBeg, bool isEnd)
        {
            if (kind == SymbolicRegexSetKind.Disjunction)
                return CreateDisjunction(builder, RemoveAnchorsElems(builder, isBeg, isEnd));
            else
                return CreateConjunction(builder, RemoveAnchorsElems(builder, isBeg, isEnd));
        }

        internal SymbolicRegexSet<S> MkDerivative(S elem)
        {
            if (kind == SymbolicRegexSetKind.Disjunction)
                return CreateDisjunction(builder, MkDerivativesOfElems(elem));
            else
                return CreateConjunction(builder, MkDerivativesOfElems(elem));
        }

        internal SymbolicRegexSet<S> MkDerivative_StartOfLine()
        {
            if (kind == SymbolicRegexSetKind.Disjunction)
                return CreateDisjunction(builder, MkDerivatives_StartOfLine_OfElems());
            else
                return CreateConjunction(builder, MkDerivatives_StartOfLine_OfElems());
        }

        IEnumerable<SymbolicRegexNode<S>> MkDerivativesOfElems(S elem)
        {
            foreach (var s in this)
                yield return s.MkDerivative(elem);
        }

        IEnumerable<SymbolicRegexNode<S>> MkDerivatives_StartOfLine_OfElems()  
        {
            foreach (var s in this)
                yield return s.builder.MkDerivative_StartOfLine(s);
        }

        IEnumerable<SymbolicRegexNode<T>> TransformElems<T>(SymbolicRegexBuilder<T> builderT, Func<S, T> predicateTransformer)
        {
            foreach (var sr in this)
                yield return builder.Transform(sr, builderT, predicateTransformer);
        }

        internal SymbolicRegexSet<T> Transform<T>(SymbolicRegexBuilder<T> builderT, Func<S, T> predicateTransformer)
        {
            if (kind == SymbolicRegexSetKind.Disjunction)
                return SymbolicRegexSet<T>.CreateDisjunction(builderT, TransformElems(builderT, predicateTransformer));
            else
                return SymbolicRegexSet<T>.CreateConjunction(builderT, TransformElems(builderT, predicateTransformer));
        }

        internal SymbolicRegexNode<S> GetTheElement()
        {
            var en = this.GetEnumerator();
            en.MoveNext();
            var elem = en.Current;
            en.Dispose();
            return elem;
        }

        internal SymbolicRegexSet<S> Reverse()
        {
            if (kind == SymbolicRegexSetKind.Disjunction)
                return CreateDisjunction(builder, ReverseElems());
            else
                return CreateConjunction(builder, ReverseElems());
        }

        IEnumerable<SymbolicRegexNode<S>> ReverseElems()
        {
            foreach (var elem in this)
                yield return elem.Reverse();
        }

        internal bool StartsWithLoop(int upperBoundLowestValue)
        {
            bool res = false;
            var e = this.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.StartsWithLoop(upperBoundLowestValue))
                {
                    res = true;
                    break;
                }
            }
            e.Dispose();
            return res;
        }

        internal SymbolicRegexSet<S> Simplify()
        {
            if (kind == SymbolicRegexSetKind.Disjunction)
                return CreateDisjunction(builder, SimplifyElems());
            else
                return CreateConjunction(builder, SimplifyElems());
        }

        IEnumerable<SymbolicRegexNode<S>> SimplifyElems()
        {
            foreach (var elem in this)
                yield return elem.Simplify();
        }

        internal SymbolicRegexSet<S> DecrementBoundedLoopCount(bool makeZero = false)
        {
            if (kind == SymbolicRegexSetKind.Disjunction)
                return CreateDisjunction(builder, DecrementBoundedLoopCountElems(makeZero));
            else
                return CreateConjunction(builder, DecrementBoundedLoopCountElems(makeZero));
        }

        IEnumerable<SymbolicRegexNode<S>> DecrementBoundedLoopCountElems(bool makeZero = false)
        {
            foreach (var elem in this)
                yield return elem.DecrementBoundedLoopCount(makeZero);
        }

        internal bool ContainsAnchors()
        {
            foreach (var elem in this)
                if (elem.containsAnchors)
                    return true;
            return false;
        }

        int enabledBoundedLoopCount = -1;
        internal int EnabledBoundedLoopCount
        {
            get
            {
                if (enabledBoundedLoopCount == -1)
                {
                    int res = 0;
                    var en = this.GetEnumerator();
                    while (en.MoveNext())
                    {
                        res += en.Current.EnabledBoundedLoopCount;
                    }
                    en.Dispose();
                    enabledBoundedLoopCount = res;
                }
                return enabledBoundedLoopCount;
            }
        }

        public IEnumerator<SymbolicRegexNode<S>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        internal string Serialize()
        {
            var list = new List<SymbolicRegexNode<S>>(this);
            var arr = list.ToArray();
            var ser = Array.ConvertAll(arr, x => x.Serialize());
            var str = new List<string>(ser);
            str.Sort();
            return string.Join(",", str);
        }

        internal void Serialize(StringBuilder sb)
        {
            sb.Append(Serialize());
        }

        internal int GetFixedLength()
        {
            if (loops.Count > 0)
                return -1;
            else
            {
                int length = -1;
                foreach (var node in this.set)
                {
                    var node_length = node.GetFixedLength();
                    if (node_length == -1)
                        return -1;
                    else if (length == -1)
                        length = node_length;
                    else if (length != node_length)
                        return -1;
                }
                return length;
            }
        }

        /// <summary>
        /// Enumerates all symbolic regexes in the set
        /// </summary>
        public class Enumerator : IEnumerator<SymbolicRegexNode<S>>
        {
            SymbolicRegexSet<S> set;
            bool set_next;
            HashSet<SymbolicRegexNode<S>>.Enumerator set_en;
            bool loops_next;
            Dictionary<Tuple<SymbolicRegexNode<S>, SymbolicRegexNode<S>>, int>.Enumerator loops_en;
            SymbolicRegexNode<S> current;

            internal Enumerator(SymbolicRegexSet<S> symbolicRegexSet)
            {
                this.set = symbolicRegexSet;
                set_en = symbolicRegexSet.set.GetEnumerator();
                loops_en = symbolicRegexSet.loops.GetEnumerator();
                set_next = true;
                loops_next = true;
                current = null;
            }

            public SymbolicRegexNode<S> Current
            {
                get
                {
                    return current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return current;
                }
            }

            public void Dispose()
            {
                set_en.Dispose();
                loops_en.Dispose();
            }

            public bool MoveNext()
            {
                if (set_next)
                {
                    set_next = set_en.MoveNext();
                    if (set_next)
                    {
                        current = set_en.Current;
                        return true;
                    }
                    else
                    {
                        loops_next = loops_en.MoveNext();
                        if (loops_next)
                        {
                            var body = loops_en.Current.Key.Item1;
                            var rest = loops_en.Current.Key.Item2;
                            var upper = loops_en.Current.Value;
                            //recreate the symbolic regex from (body,rest)->k to body{0,k}rest
                            //TBD:lazy
                            current = set.builder.MkConcat(set.builder.MkLoop(body, false, 0, upper), rest);
                            return true;
                        }
                        else
                        {
                            current = null;
                            return false;
                        }
                    }
                }
                else if (loops_next)
                {
                    loops_next = loops_en.MoveNext();
                    if (loops_next)
                    {
                        var body = loops_en.Current.Key.Item1;
                        var rest = loops_en.Current.Key.Item2;
                        var upper = loops_en.Current.Value;
                        //recreate the symbolic regex from (body,rest)->k to body{0,k}rest
                        current = set.builder.MkConcat(set.builder.MkLoop(body, false, 0, upper), rest);
                        return true;
                    }
                    else
                    {
                        current = null;
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
