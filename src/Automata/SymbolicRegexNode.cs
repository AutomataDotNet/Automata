using System;
using System.Collections;
using System.Collections.Generic;

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
        And = 8
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

        internal SymbolicRegexNode<S> left = null;
        internal SymbolicRegexNode<S> right = null;
        internal SymbolicRegexNode<S> iteCond = null;

        internal SymbolicRegexSet<S> alts = null;

        internal bool isNullable = false;
        public bool containsAnchors = false;

        /// <summary>
        /// fixed string prefix that must be matched by this regex, 
        /// null means that the value has not been computed yet
        /// </summary>
        string fixedPrefix = null;

        bool ignoreCaseOfFixedPrefix = false;

        int hashcode = -1;

        #region serialization

        /// <summary>
        /// Produce the serialized from of this symbolic regex node.
        /// </summary>
        public string Serialize()
        {
            switch (kind)
            {
                case SymbolicRegexKind.Singleton:
                    if (this.Equals(builder.dot))
                        return ".";
                    else
                    {
                        var atoms = this.builder.solver.GetPartition();
                        List<int> atomIds = new List<int>();
                        for (int i = 0; i < atoms.Length; i++)
                            if (this.builder.solver.IsSatisfiable(this.builder.solver.MkAnd(atoms[i], set)))
                                atomIds.Add(i);
                        return "[" + string.Join(",", atomIds.ToArray()) + "]";
                    }
                case SymbolicRegexKind.Loop:
                    return string.Format("L({0},{1},{2})", lower, (this.upper == int.MaxValue ? "*" : upper.ToString()), left.Serialize());
                case SymbolicRegexKind.Concat:
                    return string.Format("S({0},{1})", left.Serialize(), right.Serialize());
                case SymbolicRegexKind.Epsilon:
                    return "E";
                case SymbolicRegexKind.Or:
                    return string.Format("D({0})", alts.Serialize());
                case SymbolicRegexKind.And:
                    return string.Format("C({0})", alts.Serialize());
                case SymbolicRegexKind.EndAnchor:
                    return "$";
                case SymbolicRegexKind.StartAnchor:
                    return "^";
                default: // SymbolicRegexKind.IfThenElse:
                    return string.Format("I({0},{1},{2})", iteCond.Serialize(), left.Serialize(), right.Serialize());
            }
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

        internal static SymbolicRegexNode<S> MkLoop(SymbolicRegexBuilder<S> builder, SymbolicRegexNode<S> body, int lower, int upper)
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
                foreach (SymbolicRegexNode<S> elem in left.EnumerateConcatElementsBackwards())
                {
                    var tmp = new SymbolicRegexNode<S>(builder, SymbolicRegexKind.Concat, elem, concat, -1, -1, default(S), null, null);
                    tmp.isNullable = elem.isNullable && concat.isNullable;
                    tmp.containsAnchors = elem.containsAnchors || concat.containsAnchors;
                    concat = tmp;
                }
            }
            return concat;
        }

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

        [NonSerialized]
        string toString = null;
        /// <summary>
        /// Produce a string representation of the symbolic regex. 
        /// </summary>
        public override string ToString()
        {
            if (toString == null)
            {
                toString = ComputeToString();
            }
            return toString;
        }

        private string ComputeToString()
        {
            switch (kind)
            {
                case SymbolicRegexKind.StartAnchor:
                    return "^";
                case SymbolicRegexKind.EndAnchor:
                    return "$";
                case SymbolicRegexKind.Epsilon:
                    return "()";
                case SymbolicRegexKind.Singleton:
                    {
                        return this.builder.solver.PrettyPrint(set);
                    }
                case SymbolicRegexKind.Loop:
                    string s = left.ToString();
                    if (left.OrCount > 1 || left.kind == SymbolicRegexKind.Concat)
                        s = "(" + s + ")";
                    if (IsStar)
                        return s + "*";
                    else if (IsPlus)
                        return s + "+";
                    else if (IsMaybe)
                        return s + "?";
                    else
                        return string.Format("{0}{{{1},{2}}}", s, this.lower.ToString(),
                            (this.upper == int.MaxValue ? "" : this.upper.ToString()));
                case SymbolicRegexKind.Concat:
                    string a = left.ToString();
                    string b = right.ToString();
                    if (left.OrCount > 1)
                        a = "(" + a + ")";
                    if (right.OrCount > 1)
                        b = "(" + b + ")";
                    return a + b;
                case SymbolicRegexKind.Or:
                case SymbolicRegexKind.And:
                    return alts.ToString();
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
        public SymbolicRegexNode<S> Restrict(S pred)
        {
            switch (kind)
            {
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.EndAnchor:
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
        public SymbolicRegexNode<S> MkDerivative(S elem, bool isFirst = false, bool isLast = false)
        {
            return builder.MkDerivative(elem, isFirst, isLast, this);
        }

        /// <summary>
        /// Takes the Antimirov derivative of the symbolic regex wrt elem. 
        /// Assumes that elem is either a minterm wrt the predicates of the whole regex or a singleton set.
        /// </summary>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <returns></returns>
        internal List<ConditionalDerivative<S>> GetConditinalDerivatives(S elem)
        {
            return new List<ConditionalDerivative<S>>(builder.EnumerateConditionalDerivatives(elem, this));
        }

        /// <summary>
        /// Temporary counter automaton exploration untility
        /// </summary>
        /// <returns></returns>
        internal Automaton<Tuple<Maybe<S>,Sequence<CounterUpdate>>> Explore()
        {
            var stateLookup = new Dictionary<SymbolicRegexNode<S>, int>();
            var regexLookup = new Dictionary<int,SymbolicRegexNode<S>>();
            stateLookup[this] = 0;
            int stateid = 2;
            regexLookup[0] = this;
            SimpleStack<int> frontier = new SimpleStack<int>();
            var moves = new List<Move<Tuple<Maybe<S>, Sequence<CounterUpdate>>>>();
            var finalStates = new HashSet<int>();
            frontier.Push(0);
            while (frontier.IsNonempty)
            {
                var q = frontier.Pop();
                var regex = regexLookup[q];
                //partition corresponds to the alphabet
                foreach (S a in builder.solver.GetPartition())
                {
                    foreach (var cd in builder.EnumerateConditionalDerivatives(a, regex))
                    {
                        int p;
                        if (!stateLookup.TryGetValue(cd.PartialDerivative, out p))
                        {
                            p = stateid++;
                            stateLookup[cd.PartialDerivative] = p;
                            regexLookup[p] = cd.PartialDerivative;
                            if (cd.PartialDerivative.isNullable)
                                finalStates.Add(p);
                            else if (cd.PartialDerivative.kind == SymbolicRegexKind.Loop)
                            {
                                var reset = builder.GetCounterResetConditions(cd.PartialDerivative);
                                moves.Add(Move<Tuple<Maybe<S>, Sequence<CounterUpdate>>>.Create(p, 1,
                                    new Tuple<Maybe<S>, Sequence<CounterUpdate>>(Maybe<S>.Nothing, reset)));
                                finalStates.Add(1);
                            }
                            frontier.Push(p);
                        }
                        moves.Add(Move<Tuple<Maybe<S>, Sequence<CounterUpdate>>>.Create(q, p,
                            new Tuple<Maybe<S>, Sequence<CounterUpdate>>(Maybe<S>.Something(a), cd.Condition)));
                    }
                }
            }
            var aut = Automaton<Tuple<Maybe<S>, Sequence<CounterUpdate>>>.Create(new CABA<S>(builder),
                0, finalStates, moves);
            return aut;
        }


        internal class CABA<S> : IBooleanAlgebra<Tuple<Maybe<S>, Sequence<CounterUpdate>>>, IPrettyPrinter<Tuple<Maybe<S>, Sequence<CounterUpdate>>>
        {
            SymbolicRegexBuilder<S> builder;
            public CABA(SymbolicRegexBuilder<S> builder)
            {
                this.builder = builder;
            }

            public string PrettyPrint(Tuple<Maybe<S>, Sequence<CounterUpdate>> t)
            {
                if (t.Item1.IsSomething)
                    return builder.solver.PrettyPrint(t.Item1.Element) + "/" + t.Item2.ToString();
                else
                    return t.Item2.ToString();
            }

            #region not implemented
            public Tuple<Maybe<S>, Sequence<CounterUpdate>> False
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsAtomic
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsExtensional
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> True
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool AreEquivalent(Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate1, Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate2)
            {
                throw new NotImplementedException();
            }

            public bool CheckImplication(Tuple<Maybe<S>, Sequence<CounterUpdate>> lhs, Tuple<Maybe<S>, Sequence<CounterUpdate>> rhs)
            {
                throw new NotImplementedException();
            }

            public bool EvaluateAtom(Tuple<Maybe<S>, Sequence<CounterUpdate>> atom, Tuple<Maybe<S>, Sequence<CounterUpdate>> psi)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<Tuple<bool[], Tuple<Maybe<S>, Sequence<CounterUpdate>>>> GenerateMinterms(params Tuple<Maybe<S>, Sequence<CounterUpdate>>[] constraints)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> GetAtom(Tuple<Maybe<S>, Sequence<CounterUpdate>> psi)
            {
                throw new NotImplementedException();
            }

            public bool IsSatisfiable(Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> MkAnd(params Tuple<Maybe<S>, Sequence<CounterUpdate>>[] predicates)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> MkAnd(IEnumerable<Tuple<Maybe<S>, Sequence<CounterUpdate>>> predicates)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> MkAnd(Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate1, Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate2)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> MkDiff(Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate1, Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate2)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> MkNot(Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> MkOr(IEnumerable<Tuple<Maybe<S>, Sequence<CounterUpdate>>> predicates)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> MkOr(Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate1, Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate2)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> MkSymmetricDifference(Tuple<Maybe<S>, Sequence<CounterUpdate>> p1, Tuple<Maybe<S>, Sequence<CounterUpdate>> p2)
            {
                throw new NotImplementedException();
            }

            public string PrettyPrint(Tuple<Maybe<S>, Sequence<CounterUpdate>> t, Func<Tuple<Maybe<S>, Sequence<CounterUpdate>>, string> varLookup)
            {
                throw new NotImplementedException();
            }

            public string PrettyPrintCS(Tuple<Maybe<S>, Sequence<CounterUpdate>> t, Func<Tuple<Maybe<S>, Sequence<CounterUpdate>>, string> varLookup)
            {
                throw new NotImplementedException();
            }

            public Tuple<Maybe<S>, Sequence<CounterUpdate>> Simplify(Tuple<Maybe<S>, Sequence<CounterUpdate>> predicate)
            {
                throw new NotImplementedException();
            }
            #endregion
        }


        /// <summary>
        /// true iff epsilon is accepted
        /// </summary>
        public bool IsNullable(bool isFirst = false, bool isLast = false)
        {
            if (isNullable || !(isFirst || isLast))
                return isNullable;
            else
                return IsNullableAtBorder(isFirst, isLast);
        }

        /// <summary>
        /// true if epsilon is accepted at the start or at the end
        /// </summary>
        /// <param name="isStart">if true then returns true for start anchor</param>
        /// <param name="isEnd">if true then returns true for end anchor</param>
        /// <returns></returns>
        bool IsNullableAtBorder(bool isStart, bool isEnd)
        {
            if (isNullable)
                return true;
            else
                switch (kind)
                {
                    case SymbolicRegexKind.StartAnchor:
                        return isStart;
                    case SymbolicRegexKind.EndAnchor:
                        return isEnd;
                    case SymbolicRegexKind.Epsilon:
                        return true;
                    case SymbolicRegexKind.Singleton:
                        return false;
                    case SymbolicRegexKind.Loop:
                        return this.left.IsNullableAtBorder(isStart, isEnd);
                    case SymbolicRegexKind.Or:
                    case SymbolicRegexKind.And:
                        return this.alts.IsNullable(isStart, isEnd);
                    case SymbolicRegexKind.Concat:
                        return this.left.IsNullableAtBorder(isStart, isEnd) && this.right.IsNullableAtBorder(isStart, isEnd);
                    default: //ITE
                        return (this.iteCond.IsNullableAtBorder(isStart, isEnd) ? this.left.IsNullableAtBorder(isStart, isEnd) : this.right.IsNullableAtBorder(isStart, isEnd));
                }
        }

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
                    case SymbolicRegexKind.Loop:
                        hashcode = kind.GetHashCode() ^ left.GetHashCode() ^ lower ^ upper;
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
        /// Enumerates all predicates that occur in the regex
        /// </summary>
        public IEnumerable<S> EnumeratePredicates()
        {
            switch (kind)
            {
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.Epsilon:
                    yield break;
                case SymbolicRegexKind.Singleton:
                    {
                        yield return this.set;
                        yield break;
                    }
                case SymbolicRegexKind.Loop:
                    {
                        foreach (S s in this.left.EnumeratePredicates())
                            yield return s;
                        yield break;
                    }
                case SymbolicRegexKind.Or:
                case SymbolicRegexKind.And:
                    {
                        foreach (SymbolicRegexNode<S> sr in this.alts)
                            foreach (S s in sr.EnumeratePredicates())
                                yield return s;
                        yield break;
                    }
                case SymbolicRegexKind.Concat:
                    {
                        foreach (S s in this.left.EnumeratePredicates())
                            yield return s;
                        foreach (S s in this.right.EnumeratePredicates())
                            yield return s;
                        yield break;
                    }
                default: //ITE
                    {
                        foreach (S s in this.iteCond.EnumeratePredicates())
                            yield return s;
                        foreach (S s in this.left.EnumeratePredicates())
                            yield return s;
                        foreach (S s in this.right.EnumeratePredicates())
                            yield return s;
                        yield break;
                    }
            }
        }

        /// <summary>
        /// Compute all the minterms from the predicates in this regex.
        /// If S implements IComparable then sort the result in increasing order.
        /// </summary>
        public S[] ComputeMinterms()
        {
            var predicates = new List<S>(new HashSet<S>(EnumeratePredicates()));
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
                case SymbolicRegexKind.StartAnchor:
                    return builder.endAnchor;
                case SymbolicRegexKind.EndAnchor:
                    return builder.startAnchor;
                case SymbolicRegexKind.Loop:
                    return builder.MkLoop(this.left.Reverse(), this.lower, this.upper);
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
                        var res = (diff == 0 ? builder.epsilon : builder.MkLoop(body, 0, diff));
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
                                    return builder.MkLoop(this.left, 0, upper1);
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
        internal string GetFixedPrefix(CharSetSolver css)
        {
            if (fixedPrefix == null)
            {
                #region compute fixedPrefix
                S[] prefix = GetPrefix();
                if (prefix.Length == 0)
                {
                    fixedPrefix = string.Empty;
                }
                else
                {
                    BDD[] bdds = Array.ConvertAll(prefix, p => builder.solver.ConvertToCharSet(css, p));
                    if (Array.TrueForAll(bdds, x => css.IsSingleton(x)))
                    {
                        //all elements are singletons
                        char[] chars = Array.ConvertAll(bdds, x => (char)x.GetMin());
                        fixedPrefix = new string(chars);
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
                            fixedPrefix = new string(chars);
                            //set the ignore case flag to true
                            ignoreCaseOfFixedPrefix = true;
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
                            //but ignoreCaseOfFixedPrefix == false is cheaper in IndexOf
                            if (elemsI.Count > elems.Count)
                            {
                                fixedPrefix = new string(elemsI.ToArray());
                                ignoreCaseOfFixedPrefix = true;
                            }
                            else if (elems.Count > 0)
                            {
                                fixedPrefix = new string(elems.ToArray());
                            }
                            else if (elemsI.Count > 0)
                            {
                                fixedPrefix = new string(elemsI.ToArray());
                                ignoreCaseOfFixedPrefix = true;
                            }
                            else
                            {
                                fixedPrefix = string.Empty;
                            }
                        }
                    }
                }
                #endregion
            }
            return fixedPrefix;
        }

        internal S[] GetPrefix()
        {
            return GetPrefixSequence(Sequence<S>.Empty).ToArray();
        }

        Sequence<S> GetPrefixSequence(Sequence<S> pref)
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
                            return this.right.GetPrefixSequence(pref.Append(this.left.set));
                        else
                            return pref;
                    }
                case SymbolicRegexKind.Or:
                case SymbolicRegexKind.And:
                    {
                        var enumerator = alts.GetEnumerator();
                        enumerator.MoveNext();
                        var alts_prefix = enumerator.Current.GetPrefixSequence(Sequence<S>.Empty);
                        while (!alts_prefix.IsEmpty && enumerator.MoveNext())
                        {
                            var p = enumerator.Current.GetPrefixSequence(Sequence<S>.Empty);
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

        /// <summary>
        /// If true then, when FixedPrefix is matched then case is ignored.
        /// </summary>
        public bool IgnoreCaseOfFixedPrefix
        {
            get { return ignoreCaseOfFixedPrefix; }
        }

        /// <summary>
        /// If this is a loop then a name of the associated counter
        /// </summary>
        public string CounterName
        {
            get
            {
                if (this.lower >= 0)
                    return this.builder.GetCounterName(this);
                else
                    return null;
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
                case SymbolicRegexKind.EndAnchor:
                    return algebra.False;
                case SymbolicRegexKind.Singleton:
                    return this.set;
                case SymbolicRegexKind.Loop:
                    return this.left.GetStartSet(algebra);
                case SymbolicRegexKind.Concat:
                    {
                        var startSet = this.left.GetStartSet(algebra);
                        if (left.isNullable)
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

            public IEnumerable<int> GetStates()
            {
                return states.Keys;
            }

            public bool IsFinalState(int state)
            {
                return states[state].isNullable;
            }
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
            if (optimizeLoops)
            { 
                foreach (var elem in elems)
                {
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
                return new SymbolicRegexSet<S>(builder, SymbolicRegexSetKind.Disjunction,  other, loops);
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

        public bool IsNullable(bool isFirst = false, bool isLast = false)
        {
            var e = this.GetEnumerator();
            if (kind == SymbolicRegexSetKind.Disjunction)
            {
                #region some element must be nullable
                while (e.MoveNext())
                {
                    if (e.Current.IsNullable(isFirst, isLast))
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
                    if (!e.Current.IsNullable(isFirst, isLast))
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

        internal SymbolicRegexSet<S> MkDerivative(S elem, bool isFirst, bool isLast)
        {
            if (kind == SymbolicRegexSetKind.Disjunction)
                return CreateDisjunction(builder, MkDerivativesOfElems(elem, isFirst, isLast));
            else
                return CreateConjunction(builder, MkDerivativesOfElems(elem, isFirst, isLast));
        }

        internal IEnumerable<SymbolicRegexNode<S>> MkDerivativesOfElems(S elem, bool isFirst, bool isLast)
        {
            foreach (var s in this)
                yield return s.MkDerivative(elem, isFirst, isLast);
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
                            current = set.builder.MkConcat(set.builder.MkLoop(body, 0, upper), rest);
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
                        current = set.builder.MkConcat(set.builder.MkLoop(body, 0, upper), rest);
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
