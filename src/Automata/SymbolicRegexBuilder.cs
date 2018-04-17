

namespace Microsoft.Automata
{
    /// <summary>
    /// Builder of symbolic regexes over S. 
    /// S is the type of elements of an effective Boolean algebra.
    /// Used to convert .NET regexes to symbolic regexes.
    /// </summary>
    internal class SymbolicRegexBuilder<S>
    {
        //empty string only
        internal SymbolicRegex<S> epsilon;
        //empty language, complement of dotStar
        internal SymbolicRegex<S> nothing;
        internal SymbolicRegex<S> startAnchor;
        internal SymbolicRegex<S> endAnchor;
        internal SymbolicRegex<S> bolAnchor;
        internal SymbolicRegex<S> eolAnchor;
        internal SymbolicRegex<S> newLine;
        internal SymbolicRegex<S> dot;
        internal SymbolicRegex<S> dotStar;
        internal SymbolicRegex<S> dollar;
        internal SymbolicRegex<S> hat;
        internal SymbolicRegex<S> bolRegex;
        internal SymbolicRegex<S> eolRegex;
        internal ICharAlgebra<S> solver;
        /// <summary>
        /// Create a new incremental symbolic regex builder.
        /// </summary>
        /// <param name="solver">Effective Boolean algebra over S.</param>
        public SymbolicRegexBuilder(ICharAlgebra<S> solver)
        {
            this.solver = solver;
            this.epsilon = SymbolicRegex<S>.MkEpsilon(this);
            this.nothing = SymbolicRegex<S>.MkSingleton(this, solver.False);
            this.startAnchor = SymbolicRegex<S>.MkStartAnchor(this);
            this.endAnchor = SymbolicRegex<S>.MkEndAnchor(this);
            this.eolAnchor = SymbolicRegex<S>.MkEolAnchor(this);
            this.bolAnchor = SymbolicRegex<S>.MkBolAnchor(this);
            this.newLine = SymbolicRegex<S>.MkSingleton(this, solver.MkCharConstraint('\n'));
            this.dollar = SymbolicRegex<S>.MkSingleton(this, solver.MkCharConstraint('$'));
            this.hat = SymbolicRegex<S>.MkSingleton(this, solver.MkCharConstraint('^'));
            this.dot = SymbolicRegex<S>.MkSingleton(this, solver.True);
            this.dotStar = SymbolicRegex<S>.MkLoop(this, this.dot, 0, int.MaxValue);
            this.bolRegex = SymbolicRegex<S>.MkLoop(this, SymbolicRegex<S>.MkConcat(this, this.dotStar, this.newLine), 0, 1);
            this.eolRegex = SymbolicRegex<S>.MkLoop(this, SymbolicRegex<S>.MkConcat(this, this.newLine, this.dotStar), 0, 1);
        }

        /// <summary>
        /// Make a disjunction of given regexes, eliminate nothing
        /// </summary>
        public SymbolicRegex<S> MkOr(params SymbolicRegex<S>[] regexes)
        {
            if (regexes.Length < 1)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var sr = regexes[regexes.Length - 1];
            for (int i = regexes.Length - 2; i >= 0; i--)
            {
                if (!regexes[i].IsNothing)
                {
                    sr = SymbolicRegex<S>.MkOr(this, regexes[i], sr);
                }
            }

            return sr;
        }

        /// <summary>
        /// Make a concatenation of given regexes, if any regex is nothing then return nothing
        /// </summary>
        public SymbolicRegex<S> MkConcat(params SymbolicRegex<S>[] regexes)
        {
            if (regexes.Length < 1)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument);

            var sr = regexes[regexes.Length - 1];
            if (sr.IsNothing)
            {
                return sr;
            }
            else
            {
                //exclude epsilons from the concatenation
                for (int i = regexes.Length - 2; i >= 0; i--)
                {
                    if (regexes[i].IsNothing)
                    {
                        return regexes[i];
                    }
                    else if (sr.IsEpsilon)
                    {
                        sr = regexes[i];
                    }
                    else if (!regexes[i].IsEpsilon)
                    {
                        sr = SymbolicRegex<S>.MkConcat(this, regexes[i], sr);
                    }
                }
                return sr;
            }
        }

        /// <summary>
        /// Make loop regex
        /// </summary>
        public SymbolicRegex<S> MkLoop(SymbolicRegex<S> regex, int lower = 0, int upper = int.MaxValue)
        {
            return SymbolicRegex<S>.MkLoop(this, regex, lower, upper);
        }

        public SymbolicRegex<S> MkStartAnchor()
        {
            return this.startAnchor;
        }

        public SymbolicRegex<S> MkEndAnchor()
        {
            return this.endAnchor;
        }

        /// <summary>
        /// Make a singleton sequence regex
        /// </summary>
        public SymbolicRegex<S> MkSingleton(S set)
        {
            var s = new SymbolicRegex<S>(this, SymbolicRegexKind.Singleton, null, null, -1, -1, set, null);
            return s;
        }

        /// <summary>
        /// Make an if-then-else regex (?(cond)left|right)
        /// </summary>
        /// <param name="cond">condition</param>
        /// <param name="left">true case</param>
        /// <param name="right">false case</param>
        /// <returns></returns>
        public SymbolicRegex<S> MkIfThenElse(SymbolicRegex<S> cond, SymbolicRegex<S> left, SymbolicRegex<S> right)
        {
            return SymbolicRegex<S>.MkIfThenElse(this, cond, left, right);
        }

        /// <summary>
        /// Goes over the symbolic regex, removes anchors, adds .* if anchors were not present. 
        /// Creates an equivalent regex with implicit start and end anchors.
        /// </summary>
        internal SymbolicRegex<S> RemoveAnchors(SymbolicRegex<S> sr, bool isBeg, bool isEnd)
        {
            switch (sr.Kind)
            {
                case SymbolicRegexKind.Concat:
                    {
                        #region concat
                        var left = RemoveAnchors(sr.Left, isBeg, false);
                        var right = RemoveAnchors(sr.Right, false, isEnd);
                        //empty language concatenated with anything else reduces to empty language
                        if (left.IsNothing)
                        {
                            return left;
                        }
                        else if (right.IsNothing)
                        {
                            return right;
                        }
                        else if (left.IsAll && right.IsAll)
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
                        if (loop.IsAll)
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
                        var left = RemoveAnchors(sr.Left, isBeg, isEnd);
                        var right = RemoveAnchors(sr.Right, isBeg, isEnd);
                        if (left == sr.Left && right == sr.Right)
                            return sr;
                        else
                        {
                            return this.MkOr(left, right);
                        }
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
    }
}
