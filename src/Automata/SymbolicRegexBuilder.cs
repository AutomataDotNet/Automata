using System.Collections.Generic;
using System;
using System.Runtime.Serialization;

namespace Microsoft.Automata
{
    /// <summary>
    /// Builder of symbolic regexes over S. 
    /// S is the type of elements of an effective Boolean algebra.
    /// Used to convert .NET regexes to symbolic regexes.
    /// </summary>
    internal class SymbolicRegexBuilder<S>
    {
        internal ICharAlgebra<S> solver;

        internal SymbolicRegexNode<S> epsilon;
        internal SymbolicRegexNode<S> nothing;
        internal SymbolicRegexNode<S> startAnchor;
        internal SymbolicRegexNode<S> endAnchor;
        internal SymbolicRegexNode<S> bolAnchor;
        internal SymbolicRegexNode<S> eolAnchor;
        internal SymbolicRegexNode<S> newLine;
        internal SymbolicRegexNode<S> dot;
        internal SymbolicRegexNode<S> dotStar;
        internal SymbolicRegexNode<S> dollar;
        internal SymbolicRegexNode<S> hat;
        internal SymbolicRegexNode<S> bolRegex;
        internal SymbolicRegexNode<S> eolRegex;

        internal SymbolicRegexSet<S> fullSet;
        internal SymbolicRegexSet<S> emptySet;

        Dictionary<S, SymbolicRegexNode<S>> singletonCache = new Dictionary<S, SymbolicRegexNode<S>>();
        Dictionary<SymbolicRegexNode<S>, SymbolicRegexNode<S>> nodeCache = new Dictionary<SymbolicRegexNode<S>, SymbolicRegexNode<S>>();

        private SymbolicRegexBuilder()
        {
            this.epsilon = SymbolicRegexNode<S>.MkEpsilon(this);
            this.startAnchor = SymbolicRegexNode<S>.MkStartAnchor(this);
            this.endAnchor = SymbolicRegexNode<S>.MkEndAnchor(this);
            this.eolAnchor = SymbolicRegexNode<S>.MkEolAnchor(this);
            this.bolAnchor = SymbolicRegexNode<S>.MkBolAnchor(this);
            //----------
            this.nodeCache[this.epsilon] = this.epsilon;
            this.nodeCache[this.startAnchor] = this.startAnchor;
            this.nodeCache[this.endAnchor] = this.endAnchor;
            this.nodeCache[this.eolAnchor] = this.eolAnchor;
            this.nodeCache[this.bolAnchor] = this.bolAnchor;
            //---
            this.fullSet = SymbolicRegexSet<S>.MkFullSet(this);
            this.emptySet = SymbolicRegexSet<S>.MkEmptySet(this);
        }

        /// <summary>
        /// Create a new symbolic regex builder.
        /// </summary>
        /// <param name="solver">Effective Boolean algebra over S.</param>
        internal SymbolicRegexBuilder(ICharAlgebra<S> solver) : this()
        {
            InitilizeFields(solver);
        }

        /// <summary>
        /// Initializer all fields, used also by deserializer of SymbolicRegexMatcher
        /// </summary>
        private void InitilizeFields(ICharAlgebra<S> solver)
        {
            this.solver = solver;
            this.nothing = SymbolicRegexNode<S>.MkFalse(this, solver.False);
            this.dot = SymbolicRegexNode<S>.MkTrue(this, solver.True);
            this.dotStar = SymbolicRegexNode<S>.MkDotStar(this, this.dot);
            this.newLine = SymbolicRegexNode<S>.MkNewline(this, solver.MkCharConstraint('\n'));
            this.bolRegex = SymbolicRegexNode<S>.MkLoop(this, SymbolicRegexNode<S>.MkConcat(this, this.dotStar, this.newLine), 0, 1);
            this.eolRegex = SymbolicRegexNode<S>.MkLoop(this, SymbolicRegexNode<S>.MkConcat(this, this.newLine, this.dotStar), 0, 1);
            // --- initialize caches ---
            this.singletonCache[this.solver.False] = this.nothing;
            this.singletonCache[this.newLine.set] = this.newLine;
            this.singletonCache[this.solver.True] = this.dot;
            //---
            this.nodeCache[this.nothing] = this.nothing;
            this.nodeCache[this.dot] = this.dot;
            this.nodeCache[this.dotStar] = this.dotStar;
            this.nodeCache[this.newLine] = this.newLine;
            this.nodeCache[this.bolRegex] = this.bolRegex;
            this.nodeCache[this.eolRegex] = this.eolRegex;
        }


        internal SymbolicRegexNode<S> Internalize(SymbolicRegexNode<S> node)
        {
            SymbolicRegexNode<S> nodeRef;
            if (!nodeCache.TryGetValue(node, out nodeRef))
            {
                nodeRef = node;
                nodeCache[node] = node;
            }
            return nodeRef;
        }

        /// <summary>
        /// Make a disjunction of given regexes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        public SymbolicRegexNode<S> MkOr(params SymbolicRegexNode<S>[] regexes)
        {
            if (regexes.Length < 1)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var sr = regexes[regexes.Length - 1];

            for (int i = regexes.Length - 2; i >= 0; i--)
            {
                sr = this.MkOr2(regexes[i], sr);
                if (sr == this.dotStar)
                    return this.dotStar;
            }

            return sr;
        }

        /// <summary>
        /// Make a conjunction of given regexes, simplify by eliminating regexes that accept everything
        /// </summary>
        public SymbolicRegexNode<S> MkAnd(params SymbolicRegexNode<S>[] regexes)
        {
            if (regexes.Length < 1)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var sr = regexes[regexes.Length - 1];

            for (int i = regexes.Length - 2; i >= 0; i--)
            {
                var re = sr;
                sr = this.MkAnd2(regexes[i], re);
                if (sr == this.nothing)
                    return this.nothing;
            }

            return sr;
        }

        /// <summary>
        /// Make a disjunction of given regexes, simplify by eliminating any regex that accepts no inputs
        /// </summary>
        public SymbolicRegexNode<S> MkOr(SymbolicRegexSet<S> regexset)
        {
            if (regexset.IsNothing)
                return this.nothing;
            else if (regexset.IsEverything)
                return this.dotStar;
            else if (regexset.IsSigleton)
                return regexset.GetTheElement();
            else
                return SymbolicRegexNode<S>.MkOr(this, regexset);
        }

        SymbolicRegexNode<S> MkOr2(SymbolicRegexNode<S> x, SymbolicRegexNode<S> y)
        {
            if (x == this.dotStar || y == this.dotStar)
                return this.dotStar;
            else if (x == this.nothing)
                return y;
            else if (y == this.nothing)
                return x;
            else
            {
                var or = SymbolicRegexNode<S>.MkOr(this, x, y);
                return or;
            }
        }

        SymbolicRegexNode<S> MkAnd2(SymbolicRegexNode<S> x, SymbolicRegexNode<S> y)
        {
            if (x == this.nothing || y == this.nothing)
                return this.nothing;
            else if (x == this.dotStar)
                return y;
            else if (y == this.dotStar)
                return x;
            else
            {
                var and = SymbolicRegexNode<S>.MkAnd(this, x, y);
                return and;
            }
        }

        /// <summary>
        /// Make a conjunction of given regexes, simplify by eliminating any regex that accepts all inputs, 
        /// returns the empty regex if the regex accepts nothing
        /// </summary>
        public SymbolicRegexNode<S> MkAnd(SymbolicRegexSet<S> regexset)
        {
            if (regexset.IsNothing)
                return this.nothing;
            else if (regexset.IsEverything)
                return this.dotStar;
            else if (regexset.IsSigleton)
                return regexset.GetTheElement();
            else
                return SymbolicRegexNode<S>.MkAnd(this, regexset);
        }

        /// <summary>
        /// Make a concatenation of given regexes, if any regex is nothing then return nothing, eliminate 
        /// intermediate epsilons
        /// </summary>
        public SymbolicRegexNode<S> MkConcat(params SymbolicRegexNode<S>[] regexes)
        {
            if (regexes.Length == 0)
                return this.epsilon;

            var sr = regexes[regexes.Length - 1];
            if (sr == this.nothing)
            {
                return this.nothing;
            }
            else
            {
                //exclude epsilons from the concatenation
                for (int i = regexes.Length - 2; i >= 0; i--)
                {
                    if (regexes[i] == this.nothing)
                    {
                        return this.nothing;
                    }
                    else if (sr.IsEpsilon)
                    {
                        sr = regexes[i];
                    }
                    else if (!regexes[i].IsEpsilon)
                    {
                        sr = SymbolicRegexNode<S>.MkConcat(this, regexes[i], sr);
                    }
                }
                return sr;
            }
        }

        /// <summary>
        /// Make loop regex
        /// </summary>
        public SymbolicRegexNode<S> MkLoop(SymbolicRegexNode<S> regex, int lower = 0, int upper = int.MaxValue)
        {
            if (lower == 1 && upper == 1)
            {
                return regex;
            }
            else if (lower == 0 && upper == 0)
            {
                return this.epsilon;
            }
            else if (lower == 0 && upper == int.MaxValue && regex.kind == SymbolicRegexKind.Singleton && this.solver.AreEquivalent(this.solver.True, regex.set))
            {
                return this.dotStar;
            }
            else
            {
                var loop = SymbolicRegexNode<S>.MkLoop(this, regex, lower, upper);
                return loop;
            }
        }

        public SymbolicRegexNode<S> MkStartAnchor()
        {
            return this.startAnchor;
        }

        public SymbolicRegexNode<S> MkEndAnchor()
        {
            return this.endAnchor;
        }

        /// <summary>
        /// Make a singleton sequence regex
        /// </summary>
        public SymbolicRegexNode<S> MkSingleton(S set)
        {
            SymbolicRegexNode<S> res;
            if (!singletonCache.TryGetValue(set, out res))
            {
                res = SymbolicRegexNode<S>.MkSingleton(this, set);
                singletonCache[set] = res;
            }
            return res;
        }

        /// <summary>
        /// Make an if-then-else regex (?(cond)left|right), 
        /// or create it as conjuction if right is false
        /// </summary>
        /// <param name="cond">condition</param>
        /// <param name="left">true case</param>
        /// <param name="right">false case</param>
        /// <returns></returns>
        public SymbolicRegexNode<S> MkIfThenElse(SymbolicRegexNode<S> cond, SymbolicRegexNode<S> left, SymbolicRegexNode<S> right)
        {
            return SymbolicRegexNode<S>.MkIfThenElse(this, cond, left, right);
        }

        /// <summary>
        /// Goes over the symbolic regex, removes anchors, adds .* if anchors were not present. 
        /// Creates an equivalent regex with implicit start and end anchors.
        /// </summary>
        internal SymbolicRegexNode<S> RemoveAnchors(SymbolicRegexNode<S> sr, bool isBeg, bool isEnd)
        {
            switch (sr.Kind)
            {
                case SymbolicRegexKind.Concat:
                    {
                        #region concat
                        var left = RemoveAnchors(sr.Left, isBeg, false);
                        var right = RemoveAnchors(sr.Right, false, isEnd);
                        //empty language concatenated with anything else reduces to empty language
                        if (left == this.nothing)
                        {
                            return left;
                        }
                        else if (right == this.nothing)
                        {
                            return right;
                        }
                        else if (left == this.dotStar && right == this.dotStar)
                        {
                            //.*.* simplifies to .*
                            return left;
                        }
                        else if (left.Kind == SymbolicRegexKind.Epsilon)
                        {
                            //()r simplifies to r
                            return right;
                        }
                        else if (right.Kind == SymbolicRegexKind.Epsilon)
                        {
                            //l() simplifies to l
                            return left;
                        }
                        else if (left == sr.Left && right == sr.Right)
                        {
                            //there was no change
                            return sr;
                        }
                        else
                        {
                            return this.MkConcat(left, right);
                        }
                        #endregion
                    }
                case SymbolicRegexKind.Epsilon:
                    {
                        #region epsilon
                        if (isBeg || isEnd)
                        {
                            //this is the start or the end but there is no anchor so return .*
                            return this.dotStar;
                        }
                        else
                        {
                            //just return ()
                            return sr;
                        }
                        #endregion
                    }
                case SymbolicRegexKind.IfThenElse:
                    {
                        #region ite
                        var left = RemoveAnchors(sr.Left, isBeg, isEnd);
                        var right = RemoveAnchors(sr.Right, isBeg, isEnd);
                        var cond = RemoveAnchors(sr.IteCond, isBeg, isEnd);
                        if (left == sr.Left && right == sr.Right && sr.IteCond == cond)
                            return sr;
                        else
                        {
                            return this.MkIfThenElse(cond, left, right);
                        }
                        #endregion
                    }
                case SymbolicRegexKind.Loop:
                    {
                        #region loop
                        //this call only verifies absense of start and end anchors inside the loop body (Left)
                        //because any anchor causes an exception
                        RemoveAnchors(sr.Left, false, false);
                        var loop = sr;
                        if (loop == this.dotStar)
                        {
                            return loop;
                        }
                        if (isEnd)
                        {
                            loop = MkConcat(loop, this.dotStar);
                        }
                        if (isBeg)
                        {
                            loop = MkConcat(this.dotStar, loop);
                        }
                        return loop;
                        #endregion
                    }
                case SymbolicRegexKind.Or:
                    {
                        #region or
                        var choices = sr.alts.RemoveAnchors(isBeg, isEnd);
                        return this.MkOr(choices);
                        #endregion
                    }
                case SymbolicRegexKind.And:
                    {
                        #region and
                        var conjuncts = sr.alts.RemoveAnchors(isBeg, isEnd);
                        return this.MkAnd(conjuncts);
                        #endregion
                    }
                case SymbolicRegexKind.StartAnchor:
                    {
                        #region anchor ^
                        if (isBeg) //^ at the beginning
                        {
                            if (isEnd) //^ also at the end
                            {
                                return this.dotStar;
                            }
                            else
                            {
                                if (sr.IsStartOfLineAnchor)
                                {
                                    return this.bolRegex;
                                }
                                else
                                {
                                    return this.epsilon;
                                }
                            }
                        }
                        else
                        {
                            //treat the anchor as a regex that accepts nothing
                            return this.nothing;
                        }
                        #endregion
                    }
                case SymbolicRegexKind.EndAnchor:
                    {
                        #region anchor $
                        if (isEnd) //$ at the end
                        {
                            if (isBeg) //$ also at the beginning
                                return this.dotStar;
                            else
                            {
                                if (sr.IsEndOfLineAnchor)
                                {
                                    return this.eolRegex;
                                }
                                else
                                {
                                    return this.epsilon;
                                }
                            }
                        }
                        else
                        {
                            //treat the anchor as regex that accepts nothing
                            return this.nothing;
                        }
                        #endregion
                    }
                default: // SymbolicRegexKind.Singleton:
                    {
                        #region singleton
                        var res = sr;
                        if (isEnd)
                        {
                            //add .* at the end
                            res = this.MkConcat(res, this.dotStar);
                        }
                        if (isBeg)
                        {
                            //add .* at the beginning
                            res = this.MkConcat(this.dotStar, res);
                        }
                        return res;
                        #endregion
                    }
            }
        }

        internal SymbolicRegexNode<S> MkDerivative(S elem, bool isFirst, bool isLast, SymbolicRegexNode<S> sr)
        {
            if (sr == this.dotStar)
                return this.dotStar;
            else if (sr == this.nothing)
                return this.nothing;
            else
                switch (sr.kind)
                {
                    case SymbolicRegexKind.StartAnchor:
                    case SymbolicRegexKind.EndAnchor:
                    case SymbolicRegexKind.Epsilon:
                        {
                            return this.nothing;
                        }
                    case SymbolicRegexKind.Singleton:
                        {
                            #region d(a,R) = epsilon if (a in R) else nothing
                            if (this.solver.IsSatisfiable(this.solver.MkAnd(elem, sr.set)))
                            {
                                return this.epsilon;
                            }
                            else
                            {
                                return this.nothing;
                            }
                            #endregion
                        }
                    case SymbolicRegexKind.Loop:
                        {
                            #region d(a, R*) = d(a,R)R*
                            var step = MkDerivative(elem, isFirst, isLast, sr.left);
                            if (step == this.nothing)
                            {
                                return this.nothing;
                            }
                            if (sr.IsStar)
                            {
                                var deriv = this.MkConcat(step, sr);
                                return deriv;
                            }
                            else if (sr.IsPlus)
                            {
                                var star = this.MkLoop(sr.left);
                                var deriv = this.MkConcat(step, star);
                                return deriv;
                            }
                            else if (sr.IsMaybe)
                            {
                                return step;
                            }
                            else
                            {
                                //also decrement the upper bound if it was not maximum int
                                //there cannot be a case when upper == lower == 1
                                //such a loop is never created by MkLoop it will just return the first argument
                                //and case upper == 1, lower == 0 is the previous case
                                //so upper > 1 holds here
                                int newupper = (sr.upper == int.MaxValue ? int.MaxValue : sr.upper - 1);
                                int newlower = (sr.lower == 0 ? 0 : sr.lower - 1);
                                var rest = this.MkLoop(sr.left, newlower, newupper);
                                var deriv = this.MkConcat(step, rest);
                                return deriv;
                            }
                            #endregion
                        }
                    case SymbolicRegexKind.Concat:
                        {
                            #region d(a, AB) = d(a,A)B | (if A nullable then d(a,B))
                            var first = this.MkConcat(this.MkDerivative(elem, isFirst, isLast, sr.left), sr.right);
                            if (sr.left.IsNullable(isFirst, isLast))
                            {
                                var second = this.MkDerivative(elem, isFirst, isLast, sr.right);
                                var deriv = this.MkOr2(first, second);
                                return deriv;
                            }
                            else
                            {
                                return first;
                            }
                            #endregion
                        }
                    case SymbolicRegexKind.Or:
                        {
                            #region d(a,A|B) = d(a,A)|d(a,B)
                            var alts_deriv = sr.alts.MkDerivative(elem, isFirst, isLast);
                            return this.MkOr(alts_deriv);
                            #endregion
                        }
                    case SymbolicRegexKind.And:
                        {
                            #region d(a,A & B) = d(a,A) & d(a,B)
                            var derivs = sr.alts.MkDerivative(elem, isFirst, isLast);
                            return this.MkAnd(derivs);
                            #endregion
                        }
                    default: //ITE 
                        {
                            #region d(a,Ite(A,B,C)) = Ite(d(a,A),d(a,B),d(a,C))
                            var condD = this.MkDerivative(elem, isFirst, isLast, sr.iteCond);
                            if (condD == this.nothing)
                            {
                                var rightD = this.MkDerivative(elem, isFirst, isLast, sr.right);
                                return rightD;
                            }
                            else if (condD == this.dotStar)
                            {
                                var leftD = this.MkDerivative(elem, isFirst, isLast, sr.left);
                                return leftD;
                            }
                            else
                            {
                                var leftD = this.MkDerivative(elem, isFirst, isLast, sr.left);
                                var rightD = this.MkDerivative(elem, isFirst, isLast, sr.right);
                                var ite = this.MkIfThenElse(condD, leftD, rightD);
                                return ite;
                            }
                            #endregion
                        }
                }
        }

        internal SymbolicRegexNode<S> Normalize(SymbolicRegexNode<S> sr)
        {
            switch (sr.kind)
            {
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.Epsilon:
                case SymbolicRegexKind.Singleton:
                    return sr;
                case SymbolicRegexKind.Loop:
                    {
                        if (sr.IsStar)
                            return sr;
                        else if (sr.IsMaybe)
                            return MkOr2(sr.left, this.epsilon);
                        else if (sr.IsPlus)
                        {
                            var star = this.MkLoop(sr.left);
                            var plus = this.MkConcat(sr.left, star);
                            return plus;
                        }
                        else if (sr.upper == int.MaxValue)
                        {
                            var fixed_loop = this.MkLoop(sr.left, sr.lower, sr.lower);
                            var star = this.MkLoop(sr.left);
                            var concat = this.MkConcat(fixed_loop, star);
                            return concat;
                        }
                        else
                        {
                            return sr;
                        }
                    }
                case SymbolicRegexKind.Concat:
                    {
                        var left = Normalize(sr.left);
                        var right = Normalize(sr.right);
                        var concat = this.MkConcat(left, right);
                        return concat;
                    }
                case SymbolicRegexKind.Or:
                    {
                        var alts = new List<SymbolicRegexNode<S>>();
                        foreach (var elem in sr.alts)
                            alts.Add(Normalize(elem));
                        var or = this.MkOr(alts.ToArray());
                        return or;
                    }
                default: //ITE 
                    throw new NotSupportedException("Normalize not supported for " + sr.kind);
            }
        }

        internal IEnumerable<ConditionalDerivative<S>> EnumerateConditionalDerivatives(S elem, SymbolicRegexNode<S> node)
        {
            if (node == this.dotStar)
                yield return new ConditionalDerivative<S>(this.dotStar);
            else if (node == this.nothing)
                yield break;
            else
                switch (node.kind)
                {
                    case SymbolicRegexKind.StartAnchor:
                    case SymbolicRegexKind.EndAnchor:
                    case SymbolicRegexKind.Epsilon:
                        {
                            yield break;
                        }
                    case SymbolicRegexKind.Singleton:
                        {
                            #region d(a,R) = epsilon if (a in R) else nothing
                            if (this.solver.IsSatisfiable(this.solver.MkAnd(elem, node.set)))
                            {      
                                yield return new ConditionalDerivative<S>(this.epsilon);
                            }
                            yield break;
                            #endregion
                        }
                    case SymbolicRegexKind.Or:
                        {
                            #region d(a,A|B) = d(a,A) U d(a,B)
                            //the hashset is used to eliminate duplicates
                            //for two different members the derivatives may be the same
                            var derivs = new HashSet<ConditionalDerivative<S>>();
                            foreach (var member in node.alts)
                                foreach (var deriv in this.EnumerateConditionalDerivatives(elem, member))
                                    if (derivs.Add(deriv))
                                        yield return deriv;
                            yield break;
                            #endregion
                        }
                    case SymbolicRegexKind.Concat:
                        {
                            #region d(a, AB) = d(a,A)B U (if A nullable then d(a,B))
                            //rearrange the concat into left-associative form
                            //knowing that the representation initially is in 
                            //right-associative form where none of the elements is itself a sequence
                            //this is crucial for correctness of nullability condition generation
                            var left = node.left;
                            var right = node.right;
                            while (right.kind == SymbolicRegexKind.Concat)
                            {
                                left = left.ConcatWithoutNormalizing(right.left);
                                right = right.right;
                            }

                            var derivs = new HashSet<ConditionalDerivative<S>>();
                            //observe that left will be already in the left-assoc form so the above loop will 
                            //not be used again to normalize left to left-assoc form in the recursive call
                            foreach (var deriv in this.EnumerateConditionalDerivatives(elem, left))
                            {
                                var deriv1 = new ConditionalDerivative<S>(deriv.Condition, 
                                                    this.MkConcat(deriv.PartialDerivative, right));
                                if (derivs.Add(deriv1))
                                    yield return deriv1;
                            }

                            var reset = GetNullabilityCondition_of_left_assoc(left);
                            if (reset != null)
                            {
                                var cd_reset = new ConditionalDerivative<S>(reset, this.epsilon);
                                foreach (var deriv in this.EnumerateConditionalDerivatives(elem, right))
                                {
                                    var deriv1 = cd_reset.Compose(deriv);
                                    if (derivs.Add(deriv1))
                                        yield return deriv1;
                                }
                            }
                            yield break;
                            #endregion
                        }
                    case SymbolicRegexKind.Loop:
                        {
                            #region d(a, R*) = d(a,R)R*
                            if (node.IsStar)
                            {
                                foreach (var step in this.EnumerateConditionalDerivatives(elem, node.left))
                                {
                                    var deriv = this.MkConcat(step.PartialDerivative, node);
                                    var cd = new ConditionalDerivative<S>(step.Condition, deriv);
                                    yield return cd;
                                }
                            }
                            //--- already normalized, this should be dead code
                            //else if (node.IsPlus)
                            //{
                            //    var star = this.MkLoop(node.left);
                            //    var expandedloop = this.MkConcat(node.left, star);
                            //    foreach (var deriv in this.EnumerateConditionalDerivatives(elem, expandedloop))
                            //        yield return deriv;
                            //}
                            //else if (node.IsMaybe)
                            //{
                            //    foreach (var step in this.EnumerateConditionalDerivatives(elem, node.left))
                            //        yield return step;
                            //}
                            //else if (node.UpperBound == int.MaxValue)
                            //{
                            //    //normalize A{k,*} loop into A{k}A*
                            //    var Ak = this.MkLoop(node.left, node.lower, node.lower);
                            //    var Astar = this.MkLoop(node.left, 0, int.MaxValue);
                            //    var expandedloop = this.MkConcat(Ak, Astar);
                            //    foreach (var step in this.EnumerateConditionalDerivatives(elem, expandedloop))
                            //        yield return step;
                            //}
                            else
                            {
                                CounterUpdate ca = new CounterUpdate(node, CounterOp.INCREMENT);
                                foreach (var step in this.EnumerateConditionalDerivatives(elem, node.left))
                                {
                                    var deriv = this.MkConcat(step.PartialDerivative, node);
                                    var cond = step.Condition.Append(ca);
                                    var cd = new ConditionalDerivative<S>(cond, deriv);
                                    yield return cd;
                                }
                            }
                            yield break;
                            #endregion
                        }
                    default: 
                        {
                            throw new NotSupportedException("Conditional derivatives not supported for " + node.kind);
                        }
                }
        }

        Dictionary<object, string> counterIdMap = new Dictionary<object, string>();
        internal string GetCounterName(object id)
        {
            string name;
            if (!counterIdMap.TryGetValue(id, out name))
            {
                name = "c" + counterIdMap.Count;
                counterIdMap[id] = name;
            }
            return name;
        }

        SymbolicRegexNode<S> ToLeftAssocForm(SymbolicRegexNode<S> node)
        {
            if (node.kind != SymbolicRegexKind.Concat || node.right.kind != SymbolicRegexKind.Concat)
                return node;
            else
            {
                var left = node.left;
                var right = node.right;
                while (right.kind == SymbolicRegexKind.Concat)
                {
                    left = left.ConcatWithoutNormalizing(right.left);
                    right = right.right;
                }
                return left.ConcatWithoutNormalizing(right);
            }
        }

        internal Sequence<CounterUpdate> GetNullabilityCondition(SymbolicRegexNode<S> node)
        {
            var node1 = ToLeftAssocForm(node);
            var reset = GetNullabilityCondition_of_left_assoc(node1);
            return reset;
        }

        bool IsCountingLoop(SymbolicRegexNode<S> node)
        {
            return !node.IsMaybe && !node.IsStar && !node.IsPlus;
        }

        /// <summary>
        /// node is assumed to be in left-assoc form if it is a concatenation
        /// </summary>
        Sequence<CounterUpdate> GetNullabilityCondition_of_left_assoc(SymbolicRegexNode<S> node)
        {
            switch (node.kind)
            {
                case SymbolicRegexKind.StartAnchor:
                case SymbolicRegexKind.EndAnchor:
                case SymbolicRegexKind.Epsilon:
                    {
                        return Sequence<CounterUpdate>.Empty;
                    }
                case SymbolicRegexKind.Singleton:
                    {
                        return null;
                    }
                case SymbolicRegexKind.Or:
                    {
                        if (node.isNullable)
                            return Sequence<CounterUpdate>.Empty;
                        else
                            return null;
                    }
                case SymbolicRegexKind.Loop:
                    {
                        if (IsCountingLoop(node))
                            return new Sequence<CounterUpdate>(new CounterUpdate(node, CounterOp.RESET));
                        else if (node.isNullable)
                            return Sequence<CounterUpdate>.Empty;
                        else
                            return null;
                    }
                case SymbolicRegexKind.Concat:
                    {
                        var reset1 = GetNullabilityCondition_of_left_assoc(node.left);
                        if (reset1 == null)
                            return null;
                        else
                        {
                            //we know that right is not a concat
                            var reset2 = GetNullabilityCondition_of_left_assoc(node.right);
                            if (reset2 == null)
                                return null;
                            else
                            {
                                //TBD: this optimization needs to be verified
                                //if reset2 is nonempty it can only be a singleton
                                if (reset1.IsEmpty || reset2.IsEmpty || 
                                    reset1.TrueForAll(x => reset2[0].Counter.ContainsSubCounter(x.Counter)))
                                    return reset1.Append(reset2);
                                else if (reset2[0].Counter.LowerBound == 0)
                                {
                                    return reset1;
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        }
                    }
                default:
                    {
                        throw new NotSupportedException("GetNullabilityCondition not supported for " + node.kind);
                    }
            }
        }
        internal SymbolicRegexNode<T> Transform<T>(SymbolicRegexNode<S> sr, SymbolicRegexBuilder<T> builderT, Func<S, T> predicateTransformer)
        {
            switch (sr.kind)
            {
                case SymbolicRegexKind.StartAnchor:
                    return builderT.startAnchor;
                case SymbolicRegexKind.EndAnchor:
                    return builderT.endAnchor;
                case SymbolicRegexKind.Epsilon:
                    return builderT.epsilon;
                case SymbolicRegexKind.Singleton:
                    return builderT.MkSingleton(predicateTransformer(sr.set));
                case SymbolicRegexKind.Loop:
                    return builderT.MkLoop(Transform(sr.left, builderT, predicateTransformer), sr.lower, sr.upper);
                case SymbolicRegexKind.Or:
                    return builderT.MkOr(sr.alts.Transform(builderT, predicateTransformer));
                case SymbolicRegexKind.And:
                    return builderT.MkAnd(sr.alts.Transform(builderT, predicateTransformer));
                case SymbolicRegexKind.Concat:
                    return builderT.MkConcat(Transform(sr.left, builderT, predicateTransformer),
                        Transform(sr.right, builderT, predicateTransformer));
                default: //ITE
                    return
                        builderT.MkIfThenElse(Transform(sr.IteCond, builderT, predicateTransformer),
                        Transform(sr.left, builderT, predicateTransformer),
                        Transform(sr.right, builderT, predicateTransformer));
            }
        }

        internal SymbolicRegexNode<S> Parse(string s, int i, out int i_next)
        {
            switch (s[i])
            {
                case '.':
                    {
                        #region character class of all characters 
                        i_next = i + 1;
                        return this.dot;
                        #endregion
                    }
                case '[':
                    {
                        #region parse character class
                        if (s[i + 1] == ']')
                        {
                            i_next = i + 2;
                            return this.nothing;
                        }
                        else
                        {
                            int j = s.IndexOf(']', i);
                            int[] atomIds = Array.ConvertAll(s.Substring(i + 1, j - (i + 1)).Split(','), x => int.Parse(x));
                            S[] bva = Array.ConvertAll(atomIds, id => this.solver.GetPartition()[id]);
                            var bv = this.solver.MkOr(bva);

                            SymbolicRegexNode<S> node;
                            if (!this.singletonCache.TryGetValue(bv, out node))
                            {
                                node = SymbolicRegexNode<S>.MkSingleton(this, bv);
                                this.singletonCache[bv] = node;
                            }
                            i_next = j + 1;
                            return node;
                        }
                        #endregion
                    }
                case 'E':
                    {
                        #region Epsilon
                        i_next = i + 1;
                        return this.epsilon;
                        #endregion
                    }
                case 'L': //L(l,u,body) for body{l,u} u may be *
                    {
                        #region Loop
                        int j = s.IndexOf(',', i + 2);
                        int lower = int.Parse(s.Substring(i + 2, j - (i + 2)));
                        int upper = int.MaxValue;
                        if (s[j+1] == '*')
                        {
                            j = j + 3;
                        }
                        else
                        {
                            int k = s.IndexOf(',', j + 1);
                            upper = int.Parse(s.Substring(j + 1, k - (j + 1)));
                            j = k + 1;
                        }
                        int n;
                        var body = Parse(s, j, out n);
                        var node = SymbolicRegexNode<S>.MkLoop(this, body, lower, upper);
                        i_next = n + 1;
                        return node;
                        #endregion
                    }
                case 'S': //binary concat S(R1,R2)
                    {
                        #region concatenation
                        int n;
                        var first = Parse(s, i + 2, out n);
                        int m;
                        var second = Parse(s, n + 1, out m);
                        var seq = SymbolicRegexNode<S>.MkConcat(this, first, second);
                        i_next = m + 1;
                        return seq;
                        #endregion
                    }
                case 'C': //conjunction C(R1,R2,...,Rk)
                    {
                        #region conjunction
                        int n;
                        SymbolicRegexNode<S>[] nodes = ParseSequence(s, i + 2, out n);
                        var conj = SymbolicRegexNode<S>.MkAnd(this, nodes);
                        i_next = n;
                        return conj;
                        #endregion
                    }
                case 'D': //Disjunction D(R1,R2,...,Rk)
                    {
                        #region disjunction
                        int n;
                        SymbolicRegexNode<S>[] nodes = ParseSequence(s, i + 2, out n);
                        var disj = SymbolicRegexNode<S>.MkOr(this, nodes);
                        i_next = n;
                        return disj;
                        #endregion
                    }
                case 'I': //if then else I(x,y,z)
                    {
                        #region ITE
                        int n;
                        var cond = Parse(s, i + 2, out n);
                        int m;
                        var first = Parse(s, n + 1, out m);
                        int k;
                        var second = Parse(s, m + 1, out k);
                        var ite = SymbolicRegexNode<S>.MkIfThenElse(this, cond, first, second);
                        i_next = k + 1;
                        return ite;
                        #endregion
                    }
                case '^':
                    {
                        #region start anchor
                        i_next = i + 1;
                        return this.startAnchor;
                        #endregion
                    }
                case '$':
                    {
                        #region start anchor
                        i_next = i + 1;
                        return this.endAnchor;
                        #endregion
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Deserialize a symbolic regex from its serialized representation 
        /// that was produced by SymbolicRegexNode.Serialize
        /// </summary>
        public SymbolicRegexNode<S> Deserialize(string s)
        {
            int tmp;
            return Parse(s, 0, out tmp);
        }

        private SymbolicRegexNode<S>[] ParseSequence(string s, int i, out int n)
        {
            if (s[i] == ')')
            {
                n = i + 1;
                return new SymbolicRegexNode<S>[] { };
            }
            else
            {
                var nodes = new List<SymbolicRegexNode<S>>();
                int j;
                nodes.Add(Parse(s, i, out j));
                while (s[j] == ',')
                {
                    i = j + 1;
                    nodes.Add(Parse(s, i, out j));
                }
                n = j + 1;
                return nodes.ToArray();
            }
        }
    }
}
