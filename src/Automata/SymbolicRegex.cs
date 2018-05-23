using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace Microsoft.Automata
{
    /// <summary>
    /// Kinds of symbolic regexes
    /// </summary>
    public enum SymbolicRegexKind
    {
        StartAnchor,
        EndAnchor,
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
        internal SymbolicRegexBuilder<S> builder;

        internal SymbolicRegexSet<S> alts;

        /// <summary>
        /// Alternatives of an OR
        /// </summary>
        public SymbolicRegexSet<S> Alts
        {
            get { return alts; }
        }

        /// <summary>
        /// Underlying solver
        /// </summary>
        public ICharAlgebra<S> Solver
        {
            get { return builder.solver; }
        }

        internal SymbolicRegexKind kind;
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

        public IEnumerable<SymbolicRegex<S>> EnumerateAlts()
        {
            if (this.kind == SymbolicRegexKind.Or)
                return this.alts;
            else
                return new SymbolicRegex<S>[] { this };
        }

        internal SymbolicRegex<S> left;
        /// <summary>
        /// Left child of a binary node (the child of a unary node, the true-branch of an Ite-node)
        /// </summary>
        public SymbolicRegex<S> Left
        {
            get { return left; }
        }

        internal SymbolicRegex<S> right;
        /// <summary>
        /// Right child of a binary node (the false-branch of an Ite-node)
        /// </summary>
        public SymbolicRegex<S> Right
        {
            get { return right; }
        }

        internal int lower;
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

        internal int upper;
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

        internal S set;
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

        internal SymbolicRegex<S> iteCond;
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
        public bool IsMaybe
        {
            get
            {
                return lower == 0 && upper == 1;
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
        /// Returns true if this is a .*
        /// </summary>
        public bool IsEverything
        {
            get
            {
                return this.IsStar && this.Left.Kind == SymbolicRegexKind.Singleton && this.builder.solver.True.Equals(this.Left.Set);
            }
        }

        /// <summary>
        /// Returns true if this is as singleton whose set is empty
        /// </summary>
        public bool IsNothing
        {
            get
            {
                return this.kind == SymbolicRegexKind.Singleton && this.builder.solver.False.Equals(this.Set);
            }
        }

        /// <summary>
        /// Returns true if this kind is Epsilon
        /// </summary>
        public bool IsEpsilon
        {
            get
            {
                return this.kind == SymbolicRegexKind.Epsilon;
            }
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
        private SymbolicRegex(SymbolicRegexBuilder<S> builder, SymbolicRegexKind kind, SymbolicRegex<S> left, SymbolicRegex<S> right, int lower, int upper, S set, SymbolicRegex<S> iteCond, SymbolicRegexSet<S> alts)
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
            //this.seq = seq;
        }

        internal static SymbolicRegex<S> MkSingleton(SymbolicRegexBuilder<S> builder, S set)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.Singleton, null, null, -1, -1, set, null, null);
        }

        internal static SymbolicRegex<S> MkStartAnchor(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.StartAnchor, null, null, -1, -1, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkEndAnchor(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.EndAnchor, null, null, -1, -1, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkEolAnchor(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.EndAnchor, null, null, -1, -2, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkBolAnchor(SymbolicRegexBuilder<S> builder)
        {
            return new SymbolicRegex<S>(builder, SymbolicRegexKind.StartAnchor, null, null, -2, -1, default(S), null, null);
        }

        internal static SymbolicRegex<S> MkEpsilon(SymbolicRegexBuilder<S> builder)
        {
            var eps = new SymbolicRegex<S>(builder, SymbolicRegexKind.Epsilon, null, null, -1, -1, default(S), null, null);
            eps.isNullable = true;
            return eps;
        }

        internal static SymbolicRegex<S> MkLoop(SymbolicRegexBuilder<S> builder, SymbolicRegex<S> body, int lower, int upper)
        {
            if (lower < 0 || upper < lower)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var loop = new SymbolicRegex<S>(builder, SymbolicRegexKind.Loop, body, null, lower, upper, default(S), null, null);
            if (loop.lower == 0)
            {
                loop.isNullable = true;
            }
            else
            {
                loop.isNullable = body.isNullable;
            }
            return loop;
        }

        internal static SymbolicRegex<S> MkOr(SymbolicRegexBuilder<S> builder, params SymbolicRegex<S>[] choices)
        {
            return MkOr(builder, SymbolicRegexSet<S>.Create(choices));
        }

        internal static SymbolicRegex<S> MkOr(SymbolicRegexBuilder<S> builder, SymbolicRegexSet<S> alts)
        {
            if (alts.IsNothing)
                return builder.nothing;
            else if (alts.IsEverything)
                return builder.dotStar;
            else if (alts.IsSigleton)
                return alts.GetTheElement();
            else
            {
                var or = new SymbolicRegex<S>(builder, SymbolicRegexKind.Or, null, null, -1, -1, default(S), null, alts);
                or.isNullable = alts.IsNullable();
                return or;
            }
        }

        /// <summary>
        /// We know that left and right are flat, make sure that concat(left,right) is also flat
        /// </summary>
        internal static SymbolicRegex<S> MkConcat(SymbolicRegexBuilder<S> builder, SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            SymbolicRegex<S> concat;
            if (left.IsEpsilon)
                return right;
            else if (right.IsEpsilon)
                return left;
            else if (left.kind != SymbolicRegexKind.Concat)
            {
                concat = new SymbolicRegex<S>(builder, SymbolicRegexKind.Concat, left, right, -1, -1, default(S), null, null);
                concat.isNullable = left.isNullable && right.isNullable;
            }
            else
            {
                concat = right;
                foreach (SymbolicRegex<S> elem in left.EnumerateConcatElementsBackwards())
                {
                    var tmp = new SymbolicRegex<S>(builder, SymbolicRegexKind.Concat, elem, concat, -1, -1, default(S), null, null);
                    tmp.isNullable = elem.isNullable && concat.isNullable;
                    concat = tmp;
                }
            }
            return concat;
        }

        private IEnumerable<SymbolicRegex<S>> EnumerateConcatElementsBackwards()
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

        internal static SymbolicRegex<S> MkIfThenElse(SymbolicRegexBuilder<S> builder, SymbolicRegex<S> cond, SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            var ite = new SymbolicRegex<S>(builder, SymbolicRegexKind.IfThenElse, left, right, -1, -1, default(S), cond, null);
            ite.isNullable = (cond.isNullable ? left.isNullable : right.isNullable);
            return ite;
        }

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
                    return builder.solver.PrettyPrint(set);
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
        public SymbolicRegex<S> Restrict(S pred)
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
        public SymbolicRegex<S> RemoveAnchors(bool isBeg = true, bool isEnd = true)
        {
            return this.builder.RemoveAnchors(this, isBeg, isEnd);
        }

        /// <summary>
        /// Creates the derivative of the symbolic regex wrt elem. 
        /// Assumes that elem is either a minterm wrt the predicates of the whole regex or a singleton set.
        /// </summary>
        /// <param name="elem">given element wrt which the derivative is taken</param>
        /// <returns></returns>
        public SymbolicRegex<S> MkDerivative(S elem, bool isFirst = false, bool isLast = false)
        {
            return this.builder.MkDerivative(elem, isFirst, isLast, this);
        }

        bool isNullable = false;
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
                        return this.alts.IsNullable(isStart, isEnd);
                    case SymbolicRegexKind.Concat:
                        return this.left.IsNullableAtBorder(isStart, isEnd) && this.right.IsNullableAtBorder(isStart, isEnd);
                    default: //ITE
                        return (this.iteCond.IsNullableAtBorder(isStart, isEnd) ? this.left.IsNullableAtBorder(isStart, isEnd) : this.right.IsNullableAtBorder(isStart, isEnd));
                }
        }

        /// <summary>
        /// Checks if the given input string is accepted.
        /// </summary>
        public bool IsMatch(string input)
        {
            S[] atoms;
            DecisionTree dt;
            if (this.Solver is BVAlgebra)
            {
                BVAlgebra bva = this.Solver as BVAlgebra;
                atoms = bva.atoms as S[];
                dt = bva.dtree;
            }
            else if (this.Solver is CharSetSolver)
            {
                //compute the minterms 
                atoms = ComputeMinterms();
                dt = DecisionTree.Create(this.Solver as CharSetSolver, atoms as BDD[]);
            }
            else
            { 
                throw new NotSupportedException(string.Format("only {0} or {1} solver is supported", typeof(BVAlgebra), typeof(CharSetSolver)));
            }
            var regex2state = new Dictionary<SymbolicRegex<S>, int>();
            var state2regex = new Dictionary<int, SymbolicRegex<S>>();
            var transitions = new Dictionary<int, int[]>();

            //use 1 as the initial state, transitions[q][s]=0 means uninitialized entry
            regex2state[this] = 1;
            state2regex[1] = this;
            transitions[1] = new int[atoms.Length];
            int nextStateId = 2;

            var k = input.Length;
            if (k == 0)
                return this.IsNullable(true, true);
            else
            {
                var q = 1;
                for (int i = 0; i < k; i++)
                {
                    int[] q_transitions = transitions[q];
                    char c = input[i];
                    int c_id = dt.GetId(c);
                    //a is the corresponding atom or minterm of c
                    S a = atoms[c_id];
                    int p = q_transitions[c_id];
                    if (p == 0)
                    {
                        //p==0 means that the transition q--a-->p is undefined
                        //so compute the derivative for atom a 
                        var regex = state2regex[q];
                        //TBD: anchors
                        var d = regex.MkDerivative(a);
                        if (d.IsEverything)
                            //everything is accepted from this point on
                            return true; 
                        else if (d.IsNothing)
                            //everything is rejected from this point on
                            return false;
                        if (!regex2state.TryGetValue(d, out p))
                        {
                            //the derivative has not been seen before
                            //assign the next avaliable state id to it
                            //p is the new state
                            p = nextStateId++;
                            regex2state[d] = p;
                            state2regex[p] = d;
                            transitions[p] = new int[atoms.Length];
                        }
                        q_transitions[c_id] = p;
                    }
                    q = p;
                }
                return state2regex[q].IsNullable(false, true);
            }
        }

        static int prime = 31;
        int hashcode = -1;
        public override int GetHashCode()
        {
            if (hashcode == -1)
            {
                switch (kind)
                {
                    case SymbolicRegexKind.EndAnchor:
                    case SymbolicRegexKind.StartAnchor:
                    case SymbolicRegexKind.Epsilon:
                        return kind.GetHashCode();
                    case SymbolicRegexKind.Loop:
                        return kind.GetHashCode() ^ left.GetHashCode() ^ lower ^ upper;
                    case SymbolicRegexKind.Or:
                        return kind.GetHashCode() ^ alts.GetHashCode();
                    case SymbolicRegexKind.Concat:
                        return left.GetHashCode() + (prime * right.GetHashCode());
                    case SymbolicRegexKind.Singleton:
                        return kind.GetHashCode() ^ set.GetHashCode();
                    default: //if-then-else
                        return kind.GetHashCode() ^ iteCond.GetHashCode() ^ (left.GetHashCode() << 1) ^ (right.GetHashCode() << 2);
                }
            }
            return hashcode;
        }

        public override bool Equals(object obj)
        {
            SymbolicRegex<S> that = obj as SymbolicRegex<S>;
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
                    {
                        foreach (SymbolicRegex<S> sr in this.alts)
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
            foreach (var pair in Solver.GenerateMinterms(preds))
                yield return pair.Item2;
        }

        /// <summary>
        /// Create the reverse of this regex
        /// </summary>
        public SymbolicRegex<S> Reverse()
        {
            switch (kind)
            {
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.Singleton:
                    return this;
                case SymbolicRegexKind.StartAnchor:
                    return this.builder.endAnchor;
                case SymbolicRegexKind.EndAnchor:
                    return this.builder.startAnchor;
                case SymbolicRegexKind.Loop:
                    return this.builder.MkLoop(this.left.Reverse(), this.lower, this.upper);
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
                default: //if-then-else
                    return builder.MkIfThenElse(iteCond.Reverse(), left.Reverse(), right.Reverse());
            }
        }

        public string GenerateRandomMember(int maxLoopUnrol = 10, int cornerCaseProb = 5, int maxSamplingIter = 3)
        {
            var sampler = new SymbolicRegexSampler<S>(this, maxLoopUnrol, cornerCaseProb, maxSamplingIter);
            return sampler.GenerateRandomMember();
        }

        public HashSet<string> GetPositiveDataset(int size, /* string charClassRestriction = null, */ int maxLoopUnrol = 10, int cornerCaseProb = 5, int maxSamplingIter = 3)
        {
            var sr = this;
            //if (charClassRestriction != null)
            //{
            //    var pred_bdd = this.builder.solver.CharSetProvider.MkCharSetFromRegexCharClass(charClassRestriction);
            //    var pred_S = this.builder.solver.ConvertFromCharSet(pred_bdd);
            //    sr = this.Restrict(pred_S);
            //}
            var sampler = new SymbolicRegexSampler<S>(sr, maxLoopUnrol, cornerCaseProb, maxSamplingIter);
            return sampler.GetPositiveDataset(size);
        }

        /// <summary>
        /// Try to explore the symbolic regex into a DFA. 
        /// If maxStateCount >= 0 then returns null if the DFA state count exceeds maxStateCount.
        /// </summary>
        public RegexAutomaton Explore(int maxStateCount = 1024)
        {
            S[] minterms;
            BDD[] partition;
            int K;
            BVAlgebra bva;
            if (this.Solver is BVAlgebra)
            {
                bva = this.Solver as BVAlgebra;
                minterms = bva.atoms as S[];
                partition = bva.minterms;
                K = minterms.Length;
            }
            else if (this.Solver is CharSetSolver)
            {
                var css = this.Solver as CharSetSolver;
                //compute the partition
                minterms = ComputeMinterms();
                bva = new BVAlgebra(css, minterms as BDD[]);
                K = minterms.Length;
                partition = minterms as BDD[];
                //partition = new BDD[K];
                //#region compute corresponding BDD partition from minterms
                //for (int i = 0; i < K; i++)
                //{
                //    BDD bdd;
                //    if (!Solver.TryConvertToCharSet(minterms[i], out bdd))
                //        throw new AutomataException(AutomataExceptionKind.InternalError_SymbolicRegex);
                //    partition[i] = bdd;
                //}
                //#endregion
            }
            else
            {
                throw new NotSupportedException(string.Format("only {0} or {1} solver is supported", typeof(BVAlgebra), typeof(CharSetSolver)));
            }

            Dictionary<SymbolicRegex<S>, int> regex2state = new Dictionary<SymbolicRegex<S>, int>(maxStateCount);
            Dictionary<int, SymbolicRegex<S>> state2regex = new Dictionary<int, SymbolicRegex<S>>(maxStateCount);

            //there are at most maxStateCount * K transitions
            //a transtion q --i--> p is represented by the entry delta[(q*K)+i]==p
            int[] delta = new int[maxStateCount * K];
            HashSet<int> finalstates = new HashSet<int>();

            //initial state is 0
            regex2state[this] = 0;
            state2regex[0] = this;

            int stateCount = 1;

            var stack = new SimpleStack<int>();
            stack.Push(0);
            if (this.IsNullable())
                finalstates.Add(0);

            int nonfinalSinkState = -1;
            int finalSinkState = -1;

            var q_derivs = new SymbolicRegex<S>[K];

            #region use DFS to discover the transitions, return null if state bound goes beyond maxStateCount
            while (stack.IsNonempty)
            {
                var q = stack.Pop();
                int qK = q * K;
                var r = state2regex[q];
                for (int i = 0; i < K; i++)
                    //TBD: anchors
                    q_derivs[i] = r.MkDerivative(minterms[i]);

                #region tmp: analyze the derivatives for counter extraction
                if (false) // (r.EnabledBoundedLoopCount > 0)
                {
                    //group inputs leading to same target state
                    var targets = new Dictionary<SymbolicRegex<S>, S>();
                    for (int i = 0; i < K; i++)
                    {
                        S s;
                        if (targets.TryGetValue(q_derivs[i], out s))
                            targets[q_derivs[i]] = Solver.MkOr(s, minterms[i]);
                        else
                            targets[q_derivs[i]] = minterms[i];
                    }
                    var r1 = r.DecrementBoundedLoopCount();
                    var k = r.EnabledBoundedLoopValue();
                    if (targets.ContainsKey(r1))
                    {
                        //the bounded loop predicate
                        var loop_pred = targets[r1];
                        //check that r is an invariant loop state
                        var targets1 = new Dictionary<SymbolicRegex<S>, S>();
                        var r1_derivs = new SymbolicRegex<S>[K];
                        for (int i = 0; i < K; i++)
                            //TBD: anchors
                            r1_derivs[i] = r1.MkDerivative(minterms[i]);
                        for (int i = 0; i < K; i++)
                        {
                            S s;
                            if (targets1.TryGetValue(r1_derivs[i], out s))
                                targets1[r1_derivs[i]] = Solver.MkOr(s, minterms[i]);
                            else
                                targets1[r1_derivs[i]] = minterms[i];
                        }
                        bool aresame = true;
                        if (targets1.Count != targets.Count)
                            aresame = false;
                        else
                            foreach (var key in targets1.Keys)
                            {
                                if (!targets1[key].Equals(loop_pred))
                                    if (!(targets.ContainsKey(key) && targets[key].Equals(targets1[key])))
                                    {
                                        aresame = false;
                                        break;
                                    }
                            }
                        Console.WriteLine(aresame);
                        targets1.Clear();
                        targets.Clear();
                        //try to detect a loop
                    }

                }
                #endregion

                for (int i = 0; i < K; i++)
                {
                    var c = minterms[i];
                    var d = q_derivs[i];

                    //if (r.EnabledBoundedLoopCount == 1)
                    //{
                    //    var r1 = r.DecrementBoundedLoopCount();
                    //    if (r1.Equals(d))
                    //        Console.WriteLine("YES: loop decrement: on deriv({0}, {1})", c, r);
                    //    else if (!regex2state.ContainsKey(d))
                    //        Console.WriteLine("NO: loop decrement: on deriv({0}, {1}) = {2} ", c, r, d);
                    //}

                    int p;
                    if (!regex2state.TryGetValue(d, out p))
                    {
                        //check if state limit has been reached
                        if ((maxStateCount > 0) && (stateCount == maxStateCount))
                            return null;

                        p = stateCount;
                        regex2state[d] = p;
                        state2regex[p] = d;
                        stateCount += 1;
                        if (d.IsNullable())
                        {
                            finalstates.Add(p);
                        }
                        if (d.IsEverything)
                        {
                            finalSinkState = p;
                            int pK = p * K;
                            for (int j = 0; j < K; j++)
                                delta[pK + j] = p;
                        }
                        else if (d.IsNothing)
                        {
                            nonfinalSinkState = p;
                            int pK = p * K;
                            for (int j = 0; j < K; j++)
                                delta[pK + j] = p;
                        }
                        else
                        {
                            stack.Push(p);
                        }
                    }
                    delta[qK + i] = p;
                }
            }
            #endregion

            var dt = DecisionTree.Create(this.builder.solver.CharSetProvider, partition);

            var aut = new RegexAutomaton(bva, new System.Text.RegularExpressions.Regex(this.ToString()), K, stateCount,
                delta, finalstates, dt, nonfinalSinkState, finalSinkState);

            return aut;
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
        internal SymbolicRegex<S> Simplify()
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
        internal SymbolicRegex<S> DecrementBoundedLoopCount(bool makeZero = false)
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


        SymbolicRegexMatcher<S> matcher = null;

        /// <summary>
        /// Find all matches in the given input string.
        /// Returns arrays of pairs (index, length) such that input.Substring(index, length) matches the regex
        /// </summary>
        public Tuple<int, int>[] Matches(string input, bool inlcude_overlaps = false)
        {
            if (string.IsNullOrEmpty(input))
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);
            else if (input.Length == 1)
            {
                if (IsMatch(input))
                    return new Tuple<int, int>[] { new Tuple<int, int>(0, 1) };
                else
                    return new Tuple<int, int>[] {};
            }
            else
            {
                if (matcher == null)
                    matcher = new SymbolicRegexMatcher<S>(this);
                return matcher.FindMatches(input, inlcude_overlaps);
            }
        }

        public bool ContainsAnchors()
        {
            switch (kind)
            {
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.Singleton:
                    return false;
                case SymbolicRegexKind.StartAnchor:
                    return true;
                case SymbolicRegexKind.EndAnchor:
                    return true;
                case SymbolicRegexKind.Loop:
                    return this.left.ContainsAnchors();
                case SymbolicRegexKind.Concat:
                    {
                        return (this.left.ContainsAnchors() || this.right.ContainsAnchors());
                    }
                case SymbolicRegexKind.Or:
                    {
                        return this.alts.ContainsAnchors();
                    }
                default: //if-then-else
                    return (iteCond.ContainsAnchors() || left.ContainsAnchors() || right.ContainsAnchors());
            }
        }
    }

    /// <summary>
    /// Represents a choice of a symboic Or-regex
    /// </summary>
    public class SymbolicRegexSet<S> : IEnumerable<SymbolicRegex<S>>
    {
        internal static bool optimizeLoops = true;
        static SymbolicRegexSet<S> everything = new SymbolicRegexSet<S>();
        static SymbolicRegexSet<S> nothing = new SymbolicRegexSet<S>();
        HashSet<SymbolicRegex<S>> set;
        //symbolic regex A{0,k}B is stored as (A,B) -> k
        //symbolic regex A{0,k} is stored as (A,()) -> k
        Dictionary<Tuple<SymbolicRegex<S>, SymbolicRegex<S>>, int> loops;

        public bool IsEverything
        {
            get { return this == everything; }
        }

        public bool IsNothing
        {
            get { return this == nothing; }
        }

        private SymbolicRegexSet()
        {
            this.set = null;
            this.loops = null;
        }

        private SymbolicRegexSet(HashSet<SymbolicRegex<S>> set, Dictionary<Tuple<SymbolicRegex<S>, SymbolicRegex<S>>, int> loops)
        {
            this.set = set;
            this.loops = loops;
        }

        static internal SymbolicRegexSet<S> Create(IEnumerable<SymbolicRegex<S>> elems)
        {
            var loops = new Dictionary<Tuple<SymbolicRegex<S>, SymbolicRegex<S>>, int>();
            var other = new HashSet<SymbolicRegex<S>>();
            if (optimizeLoops)
            {
                foreach (var elem in elems)
                {
                    #region start foreach
                    if (elem.IsEverything)
                        return everything;
                    else if (!elem.IsNothing)
                    {
                        switch (elem.kind)
                        {
                            case SymbolicRegexKind.Or:
                                {
                                    foreach (var alt in elem.alts)
                                    {
                                        if (alt.kind == SymbolicRegexKind.Loop && alt.lower == 0)
                                        {
                                            var pair = new Tuple<SymbolicRegex<S>, SymbolicRegex<S>>(alt.left, alt.builder.epsilon);
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
                                            var pair = new Tuple<SymbolicRegex<S>, SymbolicRegex<S>>(alt.left.left, alt.right);
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
                                        var pair = new Tuple<SymbolicRegex<S>, SymbolicRegex<S>>(elem.left, elem.builder.epsilon);
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
                                        var pair = new Tuple<SymbolicRegex<S>, SymbolicRegex<S>>(elem.left.left, elem.right);
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
                var others1 = new HashSet<SymbolicRegex<S>>();
                foreach (var sr in other)
                {
                    var key = new Tuple<SymbolicRegex<S>, SymbolicRegex<S>>(sr, sr.builder.epsilon);
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
                return nothing;
            else
                return new SymbolicRegexSet<S>(other, loops);
        }

        IEnumerable<SymbolicRegex<S>> RestrictElems(S pred)
        {
            foreach (var elem in this)
                yield return elem.Restrict(pred);
        }

        public SymbolicRegexSet<S> Restrict(S pred)
        {
            return Create(RestrictElems(pred));
        }

        int hashCode = 0;

        public int Count
        {
            get
            {
                return set.Count + loops.Count;
            }
        }

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
            while (e.MoveNext())
            {
                if (e.Current.IsNullable(isFirst, isLast))
                    return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (hashCode == 0)
            {
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
            if (!this.set.SetEquals(that.set))
                return false;
            if (this.loops.Count != that.loops.Count)
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
            var choices = new List<string>();
            while (e.MoveNext())
                choices.Add(e.Current.ToString());
            choices.Sort();
            for (int i = 0; i < choices.Count; i++)
            {
                if (res != "")
                    res += "|";
                res += choices[i];
            }
            return res;
        }

        public IEnumerator<SymbolicRegex<S>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public SymbolicRegex<S>[] ToArray()
        {
            List<SymbolicRegex<S>> elemsL = new List<SymbolicRegex<S>>(this);
            SymbolicRegex<S>[] elems = elemsL.ToArray();
            return elems;
        }

        IEnumerable<SymbolicRegex<S>> RemoveAnchorsElems(bool isBeg, bool isEnd)
        {
            foreach (var elem in this)
                yield return elem.RemoveAnchors(isBeg, isEnd);
        }

        public SymbolicRegexSet<S> RemoveAnchors(bool isBeg, bool isEnd)
        {
            return Create(RemoveAnchorsElems(isBeg, isEnd));
        }

        internal SymbolicRegexSet<S> MkDerivative(S elem, bool isFirst, bool isLast)
        {
            return Create(MkDerivativesOfElems(elem, isFirst, isLast));
        }

        internal IEnumerable<SymbolicRegex<S>> MkDerivativesOfElems(S elem, bool isFirst, bool isLast)
        {
            foreach (var s in this)
                yield return s.MkDerivative(elem, isFirst, isLast);
        }

        IEnumerable<SymbolicRegex<T>> TransformElems<T>(SymbolicRegexBuilder<T> builderT, Func<S, T> predicateTransformer)
        {
            foreach (var sr in this)
                yield return sr.builder.Transform(sr, builderT, predicateTransformer);
        }

        internal SymbolicRegexSet<T> Transform<T>(SymbolicRegexBuilder<T> builderT, Func<S, T> predicateTransformer)
        {
            return SymbolicRegexSet<T>.Create(TransformElems(builderT, predicateTransformer));
        }

        internal SymbolicRegex<S> GetTheElement()
        {
            var en = this.GetEnumerator();
            en.MoveNext();
            var elem = en.Current;
            en.Dispose();
            return elem;
        }

        internal SymbolicRegexSet<S> Reverse()
        {
            return Create(ReverseElems());
        }

        IEnumerable<SymbolicRegex<S>> ReverseElems()
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
            return Create(SimplifyElems());
        }

        IEnumerable<SymbolicRegex<S>> SimplifyElems()
        {
            foreach (var elem in this)
                yield return elem.Simplify();
        }

        internal SymbolicRegexSet<S> DecrementBoundedLoopCount(bool makeZero = false)
        {
            return Create(DecrementBoundedLoopCountElems(makeZero));
        }

        IEnumerable<SymbolicRegex<S>> DecrementBoundedLoopCountElems(bool makeZero = false)
        {
            foreach (var elem in this)
                yield return elem.DecrementBoundedLoopCount(makeZero);
        }

        internal bool ContainsAnchors()
        {
            foreach (var elem in this)
                if (elem.ContainsAnchors())
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

        /// <summary>
        /// Enumerates all symbolic regexes in the set
        /// </summary>
        public class Enumerator : IEnumerator<SymbolicRegex<S>>
        {
            SymbolicRegexSet<S> symbolicRegexSet;
            bool set_next;
            HashSet<SymbolicRegex<S>>.Enumerator set_en;
            bool loops_next;
            Dictionary<Tuple<SymbolicRegex<S>, SymbolicRegex<S>>, int>.Enumerator loops_en;
            SymbolicRegex<S> current;

            internal Enumerator(SymbolicRegexSet<S> symbolicRegexSet)
            {
                this.symbolicRegexSet = symbolicRegexSet;
                set_en = symbolicRegexSet.set.GetEnumerator();
                loops_en = symbolicRegexSet.loops.GetEnumerator();
                set_next = true;
                loops_next = true;
                current = null;
            }

            public SymbolicRegex<S> Current
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
                            current = body.builder.MkConcat(body.builder.MkLoop(body, 0, upper), rest);
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
                        current = body.builder.MkConcat(body.builder.MkLoop(body, 0, upper), rest);
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

    /// <summary>
    /// Helper class of symbolic regex for finding matches
    /// </summary>
    /// <typeparam name="S"></typeparam>
    internal class SymbolicRegexMatcher<S>
    {
        /// <summary>
        /// original regex
        /// </summary>
        SymbolicRegex<S> A;

        /// <summary>
        /// reverse(A)
        /// </summary>
        SymbolicRegex<S> Ar;

        /// <summary>
        /// .*A
        /// </summary>
        SymbolicRegex<S> A1;

        /// <summary>
        /// Initial state of A1
        /// </summary>
        int q0_A1 = 1;

        /// <summary>
        /// Initial state of Ar
        /// </summary>
        int q0_Ar = 2;

        /// <summary>
        /// Initial state of A
        /// </summary>
        int q0_A = 3;

        /// <summary>
        /// new state id generator
        /// </summary>
        int nextStateId = 4;

        /// <summary>
        /// partition of the input space of predicates
        /// </summary>
        S[] atoms;

        /// <summary>
        /// maps a character into a partition id in the range 0 .. atoms.Length-1
        /// </summary>
        DecisionTree dt;

        /// <summary>
        /// maps regexes to state ids
        /// </summary>
        Dictionary<SymbolicRegex<S>, int> regex2state = new Dictionary<SymbolicRegex<S>, int>();

        /// <summary>
        /// Maps states to regexes. 
        /// TBD: optimization as a large preallocated array
        /// </summary>
        Dictionary<int, SymbolicRegex<S>> state2regex = new Dictionary<int, SymbolicRegex<S>>();

        /// <summary>
        /// State transition map.
        /// Each entry (q, [p_0...p_n]) has n = atoms.Length-1 and represents the transitions q --atoms[i]--> p_i.
        /// All defined states are strictly positive, p_i==0 means that q --atoms[i]--> p_i is still undefined.
        /// TBD: optimization as a large flat array.
        /// </summary>
        Dictionary<int, int[]> transitions = new Dictionary<int, int[]>();

        /// <summary>
        /// Constructs matcher for given symbolic regex
        /// </summary>
        /// <param name="sr">given symbolic regex</param>
        internal SymbolicRegexMatcher(SymbolicRegex<S> sr)
        {
            if (sr.Solver is BVAlgebra)
            {
                BVAlgebra bva = sr.Solver as BVAlgebra;
                atoms = bva.atoms as S[];
                dt = bva.dtree;
            }
            else if (sr.Solver is CharSetSolver)
            {
                atoms = sr.ComputeMinterms();
                dt = DecisionTree.Create(sr.Solver as CharSetSolver, atoms as BDD[]);
            }
            else
            {
                throw new NotSupportedException(string.Format("only {0} or {1} solver is supported", typeof(BVAlgebra), typeof(CharSetSolver)));
            }

            this.A = sr;
            this.Ar = sr.Reverse();
            this.A1 = sr.builder.MkConcat(sr.builder.dotStar, sr);
            this.regex2state[A1] = q0_A1;
            this.state2regex[q0_A1] = A1;
            this.regex2state[Ar] = q0_Ar;
            this.state2regex[q0_Ar] = Ar;
            this.regex2state[A] = q0_A;
            this.state2regex[q0_A] = A;
            transitions[q0_A1] = new int[atoms.Length];
            transitions[q0_Ar] = new int[atoms.Length];
            transitions[q0_A] = new int[atoms.Length];
        }

        /// <summary>
        /// Compute the target state for source state q and input character c.
        /// </summary>
        /// <param name="c">input character</param>
        /// <param name="q">state id of source regex</param>
        /// <param name="regex">target regex (derivative of source regex wrt c)</param>
        /// <returns>state id of target regex</returns>
        int Delta(char c, int q, out SymbolicRegex<S> regex)
        {
            int atom_id = dt.GetId(c);
            S atom = atoms[atom_id];
            int[] q_trans = transitions[q];
            int p = q_trans[atom_id];
            if (p == 0)
            {
                //p is undefined
                var q_regex = state2regex[q];
                var deriv = q_regex.MkDerivative(atom);
                if (!regex2state.TryGetValue(deriv, out p))
                {
                    p = nextStateId++;
                    regex2state[deriv] = p;
                    state2regex[p] = deriv;
                    transitions[p] = new int[atoms.Length];
                }
                q_trans[atom_id] = p;
                regex = deriv;
            }
            else
            {
                regex = state2regex[p];
            }
            return p;
        }

        /// <summary>
        /// Generate all earliest maximal matches. We know that inputStr.Length is at least 2.
        /// TBD: should different variants of semantics be considered?
        /// </summary>
        internal Tuple<int, int>[] FindMatches(string inputStr, bool inlcude_overlaps)
        {
            char[] input = inputStr.ToCharArray();

            //stores the accumulated matches
            List<Tuple<int, int>> matches = new List<Tuple<int, int>>();

            //find the first accepting state
            //initial start position in the input is i = 0
            int i = 0;
            int k = input.Length;

            //after a match is found the match_start_boundary becomes 
            //the first postion after the last match
            //enforced when inlcude_overlaps == false
            int match_start_boundary = 0;

            //top level loop that finds all the matches
            while (true)
            {
                #region  find the first accepting state in A1 at position i
                int q_A1 = q0_A1;
                while (i < k)
                {
                    //TBD: anchors
                    SymbolicRegex<S> deriv;
                    q_A1 = Delta(input[i], q_A1, out deriv);
                    if (deriv.IsNullable())
                    {
                        //q_A1 is a final state
                        //so match has been found
                        break;
                    }
                    i += 1;
                }
                #endregion

                if (i == k)
                {
                    //end of input has been reached without reaching a final state, so no more matches
                    break;
                }

                int i_start = -1;

                #region  walk back to find the start position i_start according to reverse(A)
                int q_Ar = q0_Ar;
                //TBD: enforcing match_start_boundary could be turned on/off conditionally 
                while (i >= match_start_boundary)
                {
                    //observe that the input is reversed 
                    //so input[k-1] is the first character 
                    //and input[0] is the last character
                    //TBD: anchors
                    SymbolicRegex<S> deriv;
                    q_Ar = Delta(input[i], q_Ar, out deriv);
                    if (deriv.IsNullable())
                    {
                        //earliest start point so far
                        //TBD: one option is to break here?
                        //this must happen at some point 
                        //or else A1 would not have reached a 
                        //final state after match_start_boundary
                        i_start = i;
                    }
                    else if (deriv.IsNothing)
                    {
                        //the previous i_start was in fact the earliest
                        break;
                    }
                    i -= 1;
                }
                #endregion

                if (i_start == -1)
                {
                    throw new AutomataException(AutomataExceptionKind.InternalError);
                }
                i = i_start;

                #region  maximize the match from the point of view of A
                //TBD: can this be avoided, eg. by continuing with A1 from i_end instead of using A ?
                int i_end = i;
                int q_A = q0_A;
                while (i < k)
                {
                    SymbolicRegex<S> deriv;
                    //TBD: anchors
                    q_A = Delta(input[i], q_A, out deriv);
                    if (deriv.IsNullable())
                    {
                        //accepting state has been reached
                        //record the position 
                        i_end = i;
                    }
                    else if (deriv.IsNothing)
                    {
                        //nonaccepting sink state (deadend) has been reached in A
                        //so the match ended when the last i_end was updated
                        break;
                    }
                    i += 1;
                }
                #endregion

                //save the match (i_start, i_end)
                var newmatch = new Tuple<int, int>(i_start, i_end + 1 - i_start);
                matches.Add(newmatch);
                //continue matching from the position following last match
                i = i_end + 1;
                //enforce match_start_boundary when inlcude_overlaps == false
                if (!inlcude_overlaps)
                    match_start_boundary = i;
            }
            return matches.ToArray();
        }
    }
}
