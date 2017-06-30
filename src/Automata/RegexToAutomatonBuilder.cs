using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata
{
    /// <summary>
    /// Builder from elements of type T into automata over S. 
    /// S is the type of elements of an effective Boolean algebra.
    /// T is the type of an AST of regexes.
    /// Used as the main building block for converting regexes to automata over S.
    /// </summary>
    public class RegexToAutomatonBuilder<T, S>
    {
        internal Automaton<S> epsilon;
        internal Automaton<S> empty;
        /// <summary>
        /// Create a new incremental automata builder.
        /// </summary>
        /// <param name="solver">Effective Boolean algebra over S.</param>
        /// <param name="callback">The callback function is assumed to map regex nodes to corresponding methods of this builder.</param>
        public RegexToAutomatonBuilder(IBooleanAlgebra<S> solver, Func<T, Automaton<S>> callback)
        {
            this.nodeId = 0;
            this.solver = solver;
            this.Callback = callback;
            this.epsilon = Automaton<S>.MkEpsilon(solver);
            this.empty = Automaton<S>.MkEmpty(solver);
        }

        private int nodeId;
        private int MkStateId() { return nodeId++; }
        private IBooleanAlgebra<S> solver;

        /// <summary>
        /// Resets the automata builder.
        /// </summary>
        public void Reset()
        {
            nodeId = 0;
            isBeg = true;
            isEnd = true;
        }

        internal bool isBeg = true;
        internal bool isEnd = true;

        Func<T, Automaton<S>> Callback;

        #region MkUnion
        /// <summary>
        /// Builds an automaton equivalent to the regex r[0] | r[1] | ... | r[r.Length-1], 
        /// returns the automaton of the empty language when r is empty.
        /// </summary>
        /// <param name="r">regular expression nodes</param>
        public Automaton<S> MkUnion(params T[] r)
        {
            if (r.Length == 0)
                return MkEmptyLang();

            var sfas = new List<Automaton<S>>();
            var saveState = this.MkStateId();
            bool includesEmptyWord = false;
            bool noBoundaries = true;
            foreach (var child in r)
            {
                var sfa = Callback(child);
                if (sfa.IsEmpty)                  //may happen if a move condition is unsat
                    continue;                     //just ignore the empty SFA
                if (sfa.IsEpsilon)                //may happen if some branch is ()
                {
                    includesEmptyWord = true;
                    continue;
                }
                if (sfa.IsFinalState(sfa.InitialState))
                    includesEmptyWord = true;

                noBoundaries = noBoundaries && sfa.DoesNotContainWordBoundaries;

                sfas.Add(sfa);
            }
            return AlternateSFAs(saveState, sfas, includesEmptyWord, noBoundaries);
        }

        IEnumerable<int> EnumFinStates(IEnumerable<Automaton<S>> sfas)
        {
            foreach (var sfa in sfas)
                foreach (var f in sfa.GetFinalStates())
                    yield return f;
        }

        IEnumerable<Move<S>> EnumMoves(int start, IEnumerable<Automaton<S>> sfas)
        {
            foreach (var sfa in sfas)
            {
                yield return Move<S>.Epsilon(start, sfa.InitialState);
                foreach (var m in sfa.GetMoves())
                    yield return m;
            }
        }

        IEnumerable<int> EnumWordBounbdaries(IEnumerable<Automaton<S>> sfas)
        {
            foreach (var sfa in sfas)
                foreach (var b in sfa.EnumerateWordBoundaries())
                    yield return b;
        }


        private Automaton<S> AlternateSFAs(int start, List<Automaton<S>> sfas, bool addEmptyWord, bool noWordBoundaries)
        {
            if (!noWordBoundaries)
            {
                var res0 = Automaton<S>.Create(solver, start, EnumFinStates(sfas), EnumMoves(start, sfas));
                res0.AddWordBoundaries(EnumWordBounbdaries(sfas));
                return res0;
            }

            #region special cases for sfas.Count == 0 or sfas.Count == 1
            if (sfas.Count == 0)
            {
                if (addEmptyWord)
                    return this.epsilon;
                else
                    return this.empty;
            }
            if (sfas.Count == 1)
            {
                if (addEmptyWord && !sfas[0].IsFinalState(sfas[0].InitialState))
                {
                    if (sfas[0].InitialStateIsSource)
                        sfas[0].MakeInitialStateFinal();
                    else
                        sfas[0].AddNewInitialStateThatIsFinal(start);
                }
                return sfas[0];
            }
            #endregion //special cases for sfas.Count == 0 or sfas.Count == 1

            bool allSingleSource = true;
            #region check if all sfas have a single source
            foreach (var sfa in sfas)
                if (!sfa.InitialStateIsSource)
                {
                    allSingleSource = false;
                    break;
                }
            #endregion //check if all sfas have a single source

            bool isDeterministic = !sfas.Exists(IsNonDeterministic);
            bool isEpsilonFree = !sfas.Exists(HasEpsilons);

            bool allFinalSink = true;
            int sinkId;

            #region check if all sfas have a single final sink and calulate a representative sinkId as the maximum of the ids
            sinkId = int.MinValue;
            foreach (var sfa in sfas)
                if (!sfa.HasSingleFinalSink)
                {
                    allFinalSink = false;
                    break;
                }
                else
                    sinkId = Math.Max(sfa.FinalState, sinkId);
            #endregion

            var finalStates = new List<int>();
            if (addEmptyWord)
                finalStates.Add(start); //epsilon is accepted so initial state is also final
            var conditionMap = new Dictionary<Tuple<int, int>, S>();//for normalization of move conditions
            var eMoves = new HashSet<Tuple<int, int>>();

            if (!allSingleSource)
            {
                isDeterministic = false;
                isEpsilonFree = false;
                foreach (var sfa in sfas) //add initial epsilon transitions
                    eMoves.Add(new Tuple<int, int>(start, sfa.InitialState));
            }
            else if (isDeterministic)
            {
                //check if determinism is preserved
                for (int i = 0; i < sfas.Count - 1; i++)
                {
                    for (int j = i + 1; j < sfas.Count; j++)
                    {
                        S cond1 = solver.False;
                        foreach (var move in sfas[i].GetMovesFrom(sfas[i].InitialState))
                            cond1 = solver.MkOr(cond1, move.Label);
                        S cond2 = solver.False;
                        foreach (var move in sfas[j].GetMovesFrom(sfas[j].InitialState))
                            cond2 = solver.MkOr(cond2, move.Label);
                        S checkCond = solver.MkAnd(cond1, cond2);
                        isDeterministic = (checkCond.Equals(solver.False));
                        if (!isDeterministic)
                            break;
                    }
                    if (!isDeterministic)
                        break;
                }
            }

            if (allFinalSink)
                finalStates.Add(sinkId); //this will be the final state 

            Dictionary<int, int> stateRenamingMap = new Dictionary<int, int>();

            foreach (var sfa in sfas)
            {
                foreach (var move in sfa.GetMoves())
                {
                    int source = (allSingleSource && sfa.InitialState == move.SourceState ? start : move.SourceState);
                    int target = (allFinalSink && sfa.FinalState == move.TargetState ? sinkId : move.TargetState);
                    var p = new Tuple<int, int>(source, target);
                    stateRenamingMap[move.SourceState] = source;
                    stateRenamingMap[move.TargetState] = target;
                    if (move.IsEpsilon)
                    {
                        if (source != target)
                            eMoves.Add(new Tuple<int, int>(source, target));
                        continue;
                    }

                    S cond;
                    if (conditionMap.TryGetValue(p, out cond))
                        conditionMap[p] = solver.MkOr(cond, move.Label);  //join the conditions into a disjunction
                    else
                        conditionMap[p] = move.Label;
                }
                if (!allFinalSink)
                    foreach (int s in sfa.GetFinalStates())
                    {
                        int s1 = stateRenamingMap[s];
                        if (!finalStates.Contains(s1))
                            finalStates.Add(s1);
                    }
            }

            Automaton<S> res = Automaton<S>.Create(solver, start, finalStates, GenerateMoves(conditionMap, eMoves));
            res.isDeterministic = isDeterministic;
            return res;
        }
        #endregion

        #region Concatenation
        /// <summary>
        /// Builds an automaton equivalent to the regex r[0]r[1] ... r[r.Length-1], 
        /// returns the automaton accepting only the empty word when r is empty.
        /// </summary>
        public Automaton<S> MkConcatenate(T[] r, bool implicitAnchors = false)
        {
            if (r.Length == 0)
                return MkEmptyWord();

            bool start = isBeg;
            bool end = isEnd;

            if (implicitAnchors)
            {
                isBeg = false;
                isEnd = false;
            }

            var sfas = new List<Automaton<S>>();
            for (int i = 0; i < r.Length; i++)
            {
                var child = r[i];
                isBeg = (start && i == 0);
                isEnd = (end && i == r.Length - 1);
                var sfa = Callback(child);
                if (sfa.IsEmpty)
                {
                    isBeg = start;
                    isEnd = end;
                    return this.empty; //the whole concatenation becomes empty
                }
                if (sfa.IsEpsilon)
                    continue;  //the epsilon is a noop in concatenation, just ignore it
                sfas.Add(sfa);
            }
            isBeg = start;
            isEnd = end;
            return ConcatenateSFAs(sfas);
        }

        private Automaton<S> ConcatenateSFAs(List<Automaton<S>> sfas)
        {
            if (sfas.Count == 0)
                return this.epsilon; //all of SFAS were epsilons, so the result is Epsilon

            if (sfas.Count == 1)   //concatenation is trivial
                return sfas[0];

            //desctructively update the first sfa
            Automaton<S> result = sfas[0];
            for (int i = 1; i < sfas.Count; i++)
                result.Concat(sfas[i]);
            return result;
        }

        #endregion //Concatenation
        /// <summary>
        /// Builds an automaton equivalent to the regex (), 
        /// Same as MkSeq().
        /// </summary>
        public Automaton<S> MkEmptyWord()
        {
            if (isBeg || isEnd)
            {  //may start or end with any characters since no anchor is used 
                var st = this.MkStateId();
                return Automaton<S>.Create(this.solver, st, new int[] { st }, new Move<S>[] { Move<S>.Create(st, st, solver.True) });
            }
            else //an intermediate empty string is just an epsilon transition
                return this.epsilon;
        }

        public Automaton<S> MkEmptyAutomaton()
        {
            var st = this.MkStateId();
            return Automaton<S>.Create(this.solver, st, new int[] { }, new Move<S>[] { });
        }

        /// <summary>
        /// Builds an automaton that accepts all words.
        /// </summary>
        public Automaton<S> MkFull()
        {
            var st = this.MkStateId();
            return Automaton<S>.Create(this.solver, st, new int[] { st }, new Move<S>[] { Move<S>.Create(st, st, solver.True) });
        }

        /// <summary>
        /// Builds an automaton that accepts nothing.
        /// </summary>
        public Automaton<S> MkEmptyLang()
        {
            return this.empty;
        }

        /// <summary>
        /// Builds an automaton equivalent to the regex s[0]s[1] ... s[r.Length-1], 
        /// returns the automaton accepting only the empty word when s is empty.
        /// </summary>
        public Automaton<S> MkSeq(params S[] s)
        {
            if (s.Length == 0)
                return MkEmptyWord();

            bool start = isBeg;
            bool end = isEnd;

            //sequence of characters
            //string sequence = node._str;
            int count = s.Length;

            //bool ignoreCase = ((node._options & RegexOptions.IgnoreCase) != 0);

            int initialstate = nodeId;
            nodeId = nodeId + count + 1;
            int finalstate = initialstate + count;
            int[] finalstates = new int[] { finalstate };

            var moves = new List<Move<S>>();
            for (int i = 0; i < count; i++)
                moves.Add(Move<S>.Create(initialstate + i, initialstate + i + 1, s[i]));

            Automaton<S> res = Automaton<S>.Create(this.solver, initialstate, finalstates, moves);
            res.isDeterministic = true;
            if (start) //may start with any characters
            {
                res.AddMove(Move<S>.Create(initialstate, initialstate, solver.True));
                res.isDeterministic = false;
            }
            if (end) //may end with any characters
                res.AddMove(Move<S>.Create(finalstate, finalstate, solver.True));
            res.isEpsilonFree = true;
            return res;
        }

        #region Loops
        /// <summary>
        /// Builds an automaton equivalent to the regex r{m,n}, or r{m,} when n is int.MaxValue;
        /// r{0,int.MaxValue} is the same as r*; 
        /// r{1,int.MaxValue} is the same as r+.
        /// </summary>
        /// <param name="r">regular expression node</param>
        /// <param name="m">lower loop bound</param>
        /// <param name="n">upper loop bound</param>
        /// <returns></returns>
        public Automaton<S> MkLoop(T r, int m, int n)
        {
            bool start = isBeg;
            bool end = isEnd;

            isBeg = false;
            isEnd = false;
            Automaton<S> sfa = Callback(r);
            isBeg = start;
            isEnd = end;


            Automaton<S> loop;

            if (m == 0 && sfa.IsEmpty)
                loop = this.epsilon;
            else if (m == 0 && n == int.MaxValue) //case: *
            {
                loop = MakeKleeneClosure(sfa);
            }
            else if (m == 0 && n == 1) //case: ?
            {
                ;
                if (sfa.IsFinalState(sfa.InitialState))
                    return sfa;
                else if (sfa.InitialStateIsSource)
                    sfa.MakeInitialStateFinal();
                else
                    sfa.AddNewInitialStateThatIsFinal(this.MkStateId());
                loop = sfa;
            }
            else if (m == 1 && n == 1) //trivial case: r{1,1} = r
            {
                if (sfa.IsEmpty)
                    return this.empty;
                loop = sfa;
            }
            else if (n == int.MaxValue) //case: + or generally {m,} for m >= 1
            {
                if (sfa.IsEmpty)
                    return this.empty;

                if (sfa.IsFinalState(sfa.InitialState))
                    loop = MakeKleeneClosure(sfa); //the repetition is a loop
                else
                {
                    List<Automaton<S>> sfas = new List<Automaton<S>>();
                    for (int i = 0; i < m; i++)
                    {
                        //make m fresh copies
                        sfas.Add(sfa);
                        sfa = sfa.MakeCopy(this.MkStateId);
                    }
                    //the last one is made into a Kleene closure
                    sfas.Add(MakeKleeneClosure(sfa));
                    //concatenate them all
                    loop = ConcatenateSFAs(sfas);
                }
            }
            else //general case {m,n} 
            {
                List<Automaton<S>> sfas = new List<Automaton<S>>();
                //List<int> newFinals = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    sfas.Add(sfa);
                    if (i < n - 1)
                    {
                        sfa = sfa.MakeCopy(this.MkStateId);
                        if (i >= m - 1)
                        {
                            if (sfa.DoesNotContainWordBoundaries && sfa.InitialStateIsSource && !sfa.IsFinalState(sfa.InitialState))
                                sfa.MakeInitialStateFinal();
                            else
                                sfa.AddNewInitialStateThatIsFinal(this.MkStateId());
                        }
                    }
                }
                loop = ConcatenateSFAs(sfas);
                if (m == 0)
                    loop.MakeInitialStateFinal();
                //loop.SetFinalStates(newFinals);
            }
            loop = ExtendLoop(start, end, loop);
            return loop;
        }

        /// <summary>
        /// Builds an automaton equivalent to the regex s{m,n}, or s{m,} when n is int.MaxValue;
        /// s{0,int.MaxValue} is the same as s*; 
        /// s{1,int.MaxValue} is the same as s+.
        /// </summary>
        /// <param name="s">condition corresponding to a set of characters</param>
        /// <param name="m">lower loop bound</param>
        /// <param name="n">upper loop bound</param>
        /// <returns></returns>
        public Automaton<S> MkOneLoop(S s, int m, int n)
        {
            Automaton<S> loop = CreateLoopFromCondition(s, m, n);
            loop = ExtendLoop(isBeg, isEnd, loop);
            return loop;
        }

        private Automaton<S> ExtendLoop(bool start, bool end, Automaton<S> loop)
        {
            if (start)
            {
                var st = this.MkStateId();
                if (!loop.IsEpsilon)
                {
                    Automaton<S> prefix = Automaton<S>.Create(this.solver, st, new int[] { st },
                        new Move<S>[] { Move<S>.Create(st, st, solver.True) });
                    prefix.Concat(loop);
                    loop = prefix;
                }
                else
                {
                    loop = Automaton<S>.Create(this.solver, st, new int[] { st },
                        new Move<S>[] { Move<S>.Create(st, st, solver.True) });
                }

            }
            if (end)
            {
                var st = this.MkStateId();
                if (!loop.IsEpsilon)
                    loop.Concat(Automaton<S>.Create(this.solver, st, new int[] { st },
                        new Move<S>[] { Move<S>.Create(st, st, solver.True) }));
                else
                {
                    loop = Automaton<S>.Create(this.solver, st, new int[] { st },
                        new Move<S>[] { Move<S>.Create(st, st, solver.True) });
                }
            }
            return loop;
        }

        private Automaton<S> MakeKleeneClosure(Automaton<S> sfa)
        {
            if (sfa.IsEmpty || sfa.IsEpsilon)
                return this.epsilon;

            if (sfa.IsKleeneClosure())
                return sfa;


            if (sfa.DoesNotContainWordBoundaries && sfa.InitialStateIsSource && sfa.HasSingleFinalSink)
            {
                //common case, avoid epsilons in this case
                //just destructively make the final state to be the initial state
                sfa.RenameInitialState(sfa.FinalState);
                return sfa;
            }

            int origInitState = sfa.InitialState;

            if (!sfa.IsFinalState(sfa.InitialState))//the initial state is not final
            {
                if (sfa.InitialStateIsSource)
                    //make the current initial state final
                    sfa.MakeInitialStateFinal();
                else
                    //add a new initial state that is also final
                    sfa.AddNewInitialStateThatIsFinal(this.MkStateId());
            }

            //add epsilon transitions from final states to the original initial state
            foreach (int state in sfa.GetFinalStates())
                if (state != sfa.InitialState && state != origInitState)
                    sfa.AddMove(Move<S>.Epsilon(state, origInitState));

            //epsilon loops might have been created, remove them
            var sfa1 = sfa.RemoveEpsilonLoops();
            if (!sfa.DoesNotContainWordBoundaries)
                sfa1.AddWordBoundaries(sfa.EnumerateWordBoundaries());
            return sfa1;
        }

        private Automaton<S> CreateLoopFromCondition(S cond, int m, int n)
        {
            Automaton<S> res;
            int st = this.MkStateId();
            if (m == 0 && n == int.MaxValue) //case : *
            {
                res = Automaton<S>.Create(this.solver, st,
                                 new int[] { st }, new Move<S>[] { Move<S>.Create(st, st, cond) });
            }
            else if (m == 0 && n == 1) //case : ?
            {
                int st2 = this.MkStateId();
                res = Automaton<S>.Create(this.solver, st,
                                 new int[] { st, st2 }, new Move<S>[] { Move<S>.Create(st, st2, cond) });
            }
            else if (n == int.MaxValue) //case : + or {m,}
            {
                int st1 = st;
                var moves = new List<Move<S>>();
                for (int i = 0; i < m; i++)
                {
                    int st2 = this.MkStateId();
                    moves.Add(Move<S>.Create(st1, st2, cond));
                    st1 = st2;
                }
                moves.Add(Move<S>.Create(st1, st1, cond));
                res = Automaton<S>.Create(this.solver, st, new int[] { st1 }, moves);
            }
            else //general case {m,n}
            {
                Move<S>[] moves = new Move<S>[n];
                List<int> finalstates = new List<int>();
                int st1 = st;
                if (m == 0)
                    finalstates.Add(st);
                for (int i = 0; i < n; i++)
                {
                    int st2 = this.MkStateId();
                    moves[i] = Move<S>.Create(st1, st2, cond);
                    if (i >= (m - 1))
                        finalstates.Add(st2);
                    st1 = st2;
                }
                res = Automaton<S>.Create(this.solver, st, finalstates, moves);
            }
            res.isEpsilonFree = true;
            res.isDeterministic = true;
            return res;
        }

        #endregion

        #region Anchors
        /// <summary>
        /// Builds a start of line anchor automaton (^ in multiline mode)
        /// </summary>
        /// <param name="newLineCond">condition that is true only for a newline character</param>
        public Automaton<S> MkBol(S newLineCond)
        {
            if (!isBeg)
                throw new AutomataException(AutomataExceptionKind.MisplacedStartAnchor);

            if (isEnd)
                return MkFull();

            var st = this.MkStateId();
            var st1 = this.MkStateId();
            var notNewLineCond = solver.MkNot(newLineCond);

            Automaton<S> fa = Automaton<S>.Create(this.solver, st, new int[] { st },
                new Move<S>[]{
                    Move<S>.Create(st,st, newLineCond), 
                    Move<S>.Create(st,st1, notNewLineCond),
                    Move<S>.Create(st1,st1, notNewLineCond),
                    Move<S>.Create(st1,st, newLineCond)});
            return fa;
        }

        /// <summary>
        /// Builds an end of line anchor automaton ($ in multiline mode)
        /// </summary>
        /// <param name="newLineCond">condition that is true only for a newline character</param>
        public Automaton<S> MkEol(S newLineCond)
        {
            if (!isEnd)
                throw new AutomataException(AutomataExceptionKind.MisplacedEndAnchor);

            if (isBeg)
                return MkFull();

            var st = this.MkStateId();
            var st1 = this.MkStateId();
            var minStateId2 = this.MkStateId();

            Automaton<S> fa = Automaton<S>.Create(this.solver, st, new int[] { st, st1 },
                    new Move<S>[]{
                           Move<S>.Create(st,st1, newLineCond),
                           Move<S>.Create(st1,st1, solver.True)});

            return fa;
        }

        /// <summary>
        /// Builds an end anchor automaton.
        /// </summary>
        public Automaton<S> MkEnd()
        {
            if (!isEnd)
                throw new AutomataException(AutomataExceptionKind.MisplacedEndAnchor);

            if (isBeg)
                return MkFull();
            else
                return this.epsilon; //must end without additional characters
        }

        /// <summary>
        /// Builds a start anchor automaton.
        /// </summary>
        public Automaton<S> MkBeginning()
        {
            if (!isBeg)
                throw new AutomataException(AutomataExceptionKind.MisplacedStartAnchor);

            if (isEnd)
                //otherwise, allow any trailing characters
                return MkFull();
            else
                return this.epsilon;
        }


        /// <summary>
        /// Builds a word boundary automaton.
        /// </summary>
        internal Automaton<S> MkWordBoundary()
        {
            if (isBeg && isEnd)
            {
                var f1 = MkFull();
                int stateid = MkStateId();
                var aut = Automaton<S>.Create(this.solver, stateid, new int[] { stateid }, new Move<S>[] { });
                aut.AddWordBoundary(stateid);
                var f2 = MkFull();
                f1.Concat(aut);
                f1.Concat(f2);
                return f1;
            }
            else if (isBeg)
            {
                var f = MkFull();
                int stateid = MkStateId();
                var aut = Automaton<S>.Create(this.solver, stateid, new int[] { stateid }, new Move<S>[] { });
                aut.AddWordBoundary(stateid);
                f.Concat(aut);
                return f;
            }
            else if (isEnd)
            {
                int stateid = MkStateId();
                var aut = Automaton<S>.Create(this.solver, stateid, new int[] { stateid }, new Move<S>[] { });
                aut.AddWordBoundary(stateid);
                var f = MkFull();
                aut.Concat(f);
                return aut;
            }
            else
            {
                int stateid = MkStateId();
                var aut = Automaton<S>.Create(this.solver, stateid, new int[] { stateid }, new Move<S>[] { });
                aut.AddWordBoundary(stateid);
                return aut;
            }
        }

        #endregion

        #region Misc helper functions related to SFAs
        static bool IsNonDeterministic(Automaton<S> sfa)
        {
            return !sfa.isDeterministic;
        }

        static bool HasEpsilons(Automaton<S> sfa)
        {
            return !sfa.IsEpsilonFree;
        }

        IEnumerable<Move<S>> GenerateMoves(Dictionary<Tuple<int, int>, S> condMap, IEnumerable<Tuple<int, int>> eMoves)
        {
            foreach (var kv in condMap)
                yield return Move<S>.Create(kv.Key.Item1, kv.Key.Item2, kv.Value);
            foreach (var emove in eMoves)
                yield return Move<S>.Epsilon(emove.Item1, emove.Item2);
        }

        #endregion

    }
}
