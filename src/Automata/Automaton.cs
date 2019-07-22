using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Automata;
using Microsoft.Automata.Utilities;
using Microsoft.Automata.BooleanAlgebras;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.Automata
{
    /// <summary>
    /// Symbolic Finite Automaton, provides basic generic algorithms for manipulating SFAs
    /// </summary>
    /// <typeparam name="T">type of the labels</typeparam>
    public class Automaton<T> : IAutomaton<T>
    {
        protected Dictionary<int, List<Move<T>>> delta;
        protected Dictionary<int, List<Move<T>>> deltaInv;
        private int initialState;
        private HashSet<int> finalStateSet;
        private int maxState;

        public bool isEpsilonFree = false;
        public bool isDeterministic = false;
        internal bool isUnambiguous = false;

        private IBooleanAlgebra<T> algebra;

        /// <summary>
        /// Solver for labels
        /// </summary>
        public IBooleanAlgebra<T> Algebra
        {
            get
            {
                return algebra;
            }
        }

        #region special word boundary states
        internal bool DoesNotContainWordBoundaries
        {
            get { return wordBoundaries == null || wordBoundaries.Count == 0; }
        }
        HashSet<int> wordBoundaries = null;
        internal IEnumerable<int> EnumerateWordBoundaries()
        {
            if (DoesNotContainWordBoundaries)
                yield break;
            else
            {
                foreach (var s in wordBoundaries)
                    yield return s;
            }
        }
        internal void AddWordBoundary(int wordBoundaryState)
        {
            if (wordBoundaries == null)
                wordBoundaries = new HashSet<int>();
            wordBoundaries.Add(wordBoundaryState);
        }

        internal void AddWordBoundaries(IEnumerable<int> wordBoundaryStates)
        {
            foreach (var s in wordBoundaryStates)
                AddWordBoundary(s);
        }

        internal bool IsWordBoundary(int s)
        {
            return wordBoundaries != null && wordBoundaries.Contains(s);
        }

        #endregion

        /// <summary>
        /// Gets some final state of the automaton
        /// </summary>
        public int FinalState
        {
            get
            {
                foreach (int f in finalStateSet)
                    return f; //return some final state
                throw new AutomataException(AutomataExceptionKind.NoFinalState);
            }
        }


        internal bool didTopSort = false;
        internal List<int> topsort = null;

        /// <summary>
        /// Returns true iff the automaton has no states p such that there is a nonempty path from p to p.
        /// </summary>
        public bool IsLoopFree
        {
            get
            {
                if (!didTopSort)
                    topsort = TopSort();
                return topsort != null;
            }
        }

        /// <summary>
        /// Returns true iff the automaton isnonempty has a single initial state and a single final state, and
        /// either the initial state is also final and has no transitions or a loop, 
        /// the initial state has no incoming transtions, the final state has either no outgoing transitions or has a single selfloop, 
        /// and all other states have one incoming and one outgoing transition.
        /// </summary>
        public bool CheckIfSequence(out int length)
        {
            if (IsFinalState(initialState) && (OutDegree(initialState) == 0 || (OutDegree(initialState) == 1 && IsLoopState(initialState))))
            {
                length = 0;
                return true;
            }
            if (IsEmpty || InDegree(initialState) != 0 || OutDegree(initialState) != 1)
            {
                length = -1;
                return false;
            }

            var s = GetMoveFrom(initialState).TargetState;

            int k = 1;

            while (!IsFinalState(s))
            {
                if (OutDegree(s) != 1 || InDegree(s) != 1)
                {
                    length = -1;
                    return false;
                }
                s = GetMoveFrom(s).TargetState;
                k += 1;
            }

            if (OutDegree(s) == 0 || (OutDegree(s) == 1 && IsLoopState(s)))
            {
                length = k;
                return true;
            }

            length = -1;
            return false;
        }

        ///// <summary>
        ///// Gets the depth pf the automaton, assume that the automaton is a dag.
        ///// </summary>
        ///// <returns></returns>
        //public int GetDepth()
        //{
        //    var S = new Stack<Tuple<int,int>>();
        //    var V = new HashSet<int>();
        //    S.Push(new Tuple<int,int>(initialState,0));
        //    V.Add(initialState);
        //    var dist = new Dictionary<int, int>();
        //    foreach (int st in States)
        //        dist[st] = 0;
        //    dist[initialState] = 0;
        //    while (S.Count > 0)
        //    {
        //        var pair = S.Pop();
        //        foreach (var move in GetMovesFrom(pair.First))
        //        {
        //            var q = move.TargetState;
        //            var d = Math.Max(dist[q], pair.Second + 1);
        //            if (V.Add(q))
        //                S.Push(new Tuple<int, int>(q, pair.Second + 1));
        //            dist[q] = ;
        //        }
        //    }
        //}

        /// <summary>
        /// Returns a topological sorting of all the states. 
        /// Returns null iff the graph is cyclic.
        /// </summary>
        /// <returns></returns>
        public List<int> TopSort()
        {
            if (didTopSort)
                return this.topsort;

            didTopSort = true;

            var remainingMoves = new HashSet<Move<T>>(GetMoves());

            var stack = new Stack<int>();
            var tops = new List<int>();
            var set = new HashSet<int>();

            foreach (var st in States)
            {
                if (InDegree(st) == 0)
                {
                    stack.Push(st);
                    set.Add(st);
                }
            }

            while (stack.Count > 0)
            {
                var q = stack.Pop();
                tops.Add(q);
                foreach (var move in GetMovesFrom(q))
                {
                    if (move.TargetState == move.SourceState)
                        return null;

                    remainingMoves.Remove(move);
                }
                foreach (var move in GetMovesFrom(q))
                {
                    var p = move.TargetState;
                    bool p_hasNoIncomingLinks = true;
                    foreach (var move1 in GetMovesTo(p))
                    {
                        if (remainingMoves.Contains(move1))
                        {
                            p_hasNoIncomingLinks = false;
                            break;
                        }
                    }
                    if (p_hasNoIncomingLinks)
                    {
                        if (set.Add(p))
                            stack.Push(p);
                    }
                }
            }
            if (remainingMoves.Count == 0)
            {
                this.topsort = tops;
                return tops;
            }
            else
                return null;
        }


        public static Automaton<T> MkEmpty(IBooleanAlgebra<T> algebra)
        {
            var fsa = new Automaton<T>();
            fsa.algebra = algebra;
            fsa.initialState = 0;
            fsa.finalStateSet = new HashSet<int>();
            fsa.isEpsilonFree = true;
            fsa.maxState = 0;
            fsa.delta = new Dictionary<int, List<Move<T>>>();
            fsa.delta[0] = new List<Move<T>>();
            fsa.deltaInv = new Dictionary<int, List<Move<T>>>();
            fsa.deltaInv[0] = new List<Move<T>>();
            fsa.isDeterministic = true;
            return fsa;
        }

        ///// <summary>
        ///// The automaton with one state that is also final and one move state --psi--&gt; state
        ///// </summary>
        ///// <param name="psi">condition on the loop</param>
        ///// <param name="state">state id of the loop</param>
        ///// <returns></returns>
        //public static Automaton<T> Loop(T psi, int state = 0)
        //{
        //    return Automaton<T>.Create(state, new int[] { state }, new Move<T>[] { Move<T>.Create(state, state, psi) });
        //}

        /// <summary>
        /// The automaton that accepts only the empty word.
        /// </summary>
        public static Automaton<T> MkEpsilon(IBooleanAlgebra<T> algebra)
        {
            return Automaton<T>.Create(algebra, 0, new int[] { 0 }, new Move<T>[] { });
        }

        /// <summary>
        /// The automaton that accepts nothing.
        /// </summary>
        public static Automaton<T> MkFull(IBooleanAlgebra<T> algebra)
        {
            return Automaton<T>.Create(algebra, 0, new int[] { 0 }, new Move<T>[] { Move<T>.Create(0, 0, algebra.True) });
        }

        ///// <summary>
        ///// The automaton with a single state 0 and transition from 0 to 0 with the given condition.
        ///// </summary>
        //public static Automaton<T> Loop(T cond)
        //{
        //    return Automaton<T>.Create(0, new int[] { 0 }, new Move<T>[] {Move<T>.Create(0, 0, cond)});
        //}

        public bool HasMoreThanOneFinalState
        {
            get { return finalStateSet.Count > 1; }
        }

        public int OutDegree(int state)
        {
            return delta[state].Count;
        }

        public int InDegree(int state)
        {
            return this.deltaInv[state].Count;
        }

        public Move<T> GetMoveFrom(int state)
        {
            return delta[state][0];
        }

        public Move<T> GetMoveTo(int state)
        {
            return deltaInv[state][0];
        }

        public bool InitialStateIsSource
        {
            get
            {
                return deltaInv[initialState].Count == 0;
            }
        }

        public bool IsEpsilonFree
        {
            get { return isEpsilonFree; }
        }

        public int MoveCount
        {
            get
            {
                int res = 0;
                foreach (int state in delta.Keys)
                    res += delta[state].Count;
                return res;
            }
        }

        public bool IsDeterministic
        {
            get
            {
                return isDeterministic;
            }
        }

        public void ShowGraph(string name = "Automaton")
        {
            CharSetSolver css = this.algebra as CharSetSolver;
            if (css != null)
                css.ShowGraph(this as Automaton<BDD>, name);
            else
            {
                var pp = algebra as IPrettyPrinter<T>;
                if (pp != null)
                    DirectedGraphs.DgmlWriter.ShowGraph<T>(-1, this, name, pp.PrettyPrint);
                else
                    DirectedGraphs.DgmlWriter.ShowGraph<T>(-1, this, name);
            }
        }

        public void SaveGraph(string name = "Automaton")
        {
            var pp = algebra as IPrettyPrinter<T>;
            if (pp != null)
                DirectedGraphs.DgmlWriter.SaveGraph<T>(-1, this, name, pp.PrettyPrint);
            else
                DirectedGraphs.DgmlWriter.SaveGraph<T>(-1, this, name);
        }

        /// <summary>
        /// Returns all states that are reachable via epsilon moves, including the state itself.
        /// </summary>
        public IEnumerable<int> GetEpsilonClosure(int state)
        {
            var stack = new Stack<int>();
            var done = new HashSet<int>();
            done.Add(state);
            stack.Push(state);
            while (stack.Count > 0)
            {
                int s = stack.Pop();
                yield return s;
                foreach (var move in delta[s])
                    if (move.IsEpsilon && !done.Contains(move.TargetState))
                    {
                        done.Add(move.TargetState);
                        stack.Push(move.TargetState);
                    }
            }
            yield break;
        }

        /// <summary>
        /// Returns all states that are reachable via backwards epsilon moves, including the state itself.
        /// </summary>
        public IEnumerable<int> GetInvEpsilonClosure(int state)
        {
            var stack = new Stack<int>();
            var done = new HashSet<int>();
            done.Add(state);
            stack.Push(state);
            while (stack.Count > 0)
            {
                int s = stack.Pop();
                yield return s;
                foreach (var move in deltaInv[s])
                    if (move.IsEpsilon && !done.Contains(move.SourceState))
                    {
                        done.Add(move.SourceState);
                        stack.Push(move.SourceState);
                    }
            }
            yield break;
        }

        /// <summary>
        /// True iff there are no final states
        /// </summary>
        public bool IsEmpty
        {
            get { return finalStateSet.Count == 0; }
        }

        /// <summary>
        /// True iff there are no moves and the initial state is also final.
        /// </summary>
        public bool IsEpsilon
        {
            get { return finalStateSet.Count == 1 && StateCount == 1 && delta[initialState].Count == 0 && DoesNotContainWordBoundaries; }
        }

        /// <summary>
        /// Create a symbolic automaton.
        /// </summary>
        /// <param name="initialState">initial state</param>
        /// <param name="finalStates">final states</param>
        /// <param name="moves">moves</param>
        /// <returns></returns>
        public static Automaton<T> Create(IBooleanAlgebra<T> algebra, int initialState, IEnumerable<int> finalStates, IEnumerable<Move<T>> moves, bool eliminateUnrreachableStates = false, bool eliminateDeadStates = false, bool deterministic = false)
        {
            var delta = new Dictionary<int, List<Move<T>>>();
            var deltaInv = new Dictionary<int, List<Move<T>>>();
            delta[initialState] = new List<Move<T>>();
            deltaInv[initialState] = new List<Move<T>>();
            bool noEpsilons = true;
            int maxState = initialState;
            bool isDeterministic = true;
            foreach (Move<T> move in moves)
            {
                if (move.IsEpsilon)
                    noEpsilons = false;
                if (!delta.ContainsKey(move.SourceState))
                    delta[move.SourceState] = new List<Move<T>>();
                if (!delta.ContainsKey(move.TargetState))
                    delta[move.TargetState] = new List<Move<T>>();
                if (!deltaInv.ContainsKey(move.SourceState))
                    deltaInv[move.SourceState] = new List<Move<T>>();
                if (!deltaInv.ContainsKey(move.TargetState))
                    deltaInv[move.TargetState] = new List<Move<T>>();
                delta[move.SourceState].Add(move);
                deltaInv[move.TargetState].Add(move);
                isDeterministic = (isDeterministic && delta[move.SourceState].Count < 2);
                maxState = Math.Max(maxState, Math.Max(move.SourceState, move.TargetState));
            }
            isDeterministic = (isDeterministic || deterministic);
            HashSet<int> finalStateSet = new HashSet<int>(finalStates);
            finalStateSet.RemoveWhere(x => !delta.ContainsKey(x)); //remove irrelevant states          

            Automaton<T> fsa = new Automaton<T>();
            fsa.algebra = algebra;
            fsa.initialState = initialState;
            fsa.finalStateSet = finalStateSet;
            fsa.isEpsilonFree = noEpsilons;
            fsa.maxState = maxState;
            fsa.delta = delta;
            fsa.deltaInv = deltaInv;
            fsa.isDeterministic = isDeterministic;
            if (eliminateUnrreachableStates)
                fsa.EliminateUnrreachableStates();
            if (eliminateDeadStates)
                fsa.EliminateDeadStates();
            return fsa;
        }

        private Automaton() { }

        /// <summary>
        /// Creates a shallow copy of aut with exactly the same fields
        /// </summary>
        protected Automaton(Automaton<T> aut)
        {
            this.algebra = aut.algebra;
            this.initialState = aut.initialState;
            this.finalStateSet = aut.finalStateSet;
            this.isEpsilonFree = aut.isEpsilonFree;
            this.maxState = aut.maxState;
            this.delta = aut.delta;
            this.deltaInv = aut.deltaInv;
            this.isDeterministic = aut.isDeterministic;

            #region fields that are mostly unused and may be uninitialized
            this.cardinality = aut.cardinality;
            this.didTopSort =  aut.didTopSort;
            this.isUnambiguous = aut.isUnambiguous;
            this.probabilities = aut.probabilities;
            this.topsort = aut.topsort;
            this.wordBoundaries = aut.wordBoundaries;
            #endregion
        }

        /// <summary>
        /// Convert the automaton to an automaton where each guard p has been replaced with transform(p).
        /// </summary>
        public Automaton<T> RelpaceAllGuards(Func<T, T> transform)
        {
            List<Move<T>> moves1 = new List<Move<T>>();
            foreach (var move in GetMoves())
                moves1.Add(Move<T>.Create(move.SourceState, move.TargetState, transform(move.Label)));
            return Automaton<T>.Create(this.algebra, initialState, this.GetFinalStates(), moves1);
        }

        /// <summary>
        /// Convert the T-automaton to an S-automaton where each guard p has been replaced with f(p) and the S-algebra is alg.
        /// </summary>
        public Automaton<S> ReplaceAlgebra<S>(Func<T, S> f, IBooleanAlgebra<S> alg)
        {
            List<Move<S>> moves1 = new List<Move<S>>();
            foreach (var move in GetMoves())
                moves1.Add(Move<S>.Create(move.SourceState, move.TargetState, f(move.Label)));
            return Automaton<S>.Create(alg, initialState, this.GetFinalStates(), moves1);
        }

        /// <summary>
        /// Project the second component an automaton with monadic predicates.
        /// </summary>
        static public Automaton<T> ProjectSecond<S>(Automaton<IMonadicPredicate<S, T>> automaton)
        {
            ICartesianAlgebra<S, T> alg = automaton.algebra as ICartesianAlgebra<S, T>;
            if (alg == null)
                throw new AutomataException(AutomataExceptionKind.NotCartesianAlgebra);
            List<Move<T>> moves1 = new List<Move<T>>();
            foreach (var move in automaton.GetMoves())
                moves1.Add(Move<T>.Create(move.SourceState, move.TargetState, move.Label.ProjectSecond()));
            return Automaton<T>.Create(alg.Second, automaton.initialState, automaton.GetFinalStates(), moves1);
        }

        /// <summary>
        /// A number that is either equal to or greater than the largest state id used in the SFA
        /// </summary>
        public int MaxState
        {
            get { return maxState; }
        }

        public IEnumerable<int> States
        {
            get { return delta.Keys; }
        }

        public int StateCount
        {
            get { return delta.Count; }
        }

        public IEnumerable<Move<T>> GetEpsilonMoves()
        {
            foreach (int state in States)
                foreach (Move<T> move in delta[state])
                    if (move.IsEpsilon)
                        yield return move;
        }

        public IEnumerable<int> GetEpsilonTargetsFrom(int state)
        {
            foreach (Move<T> move in delta[state])
                if (move.IsEpsilon)
                    yield return move.TargetState;
        }

        public IEnumerable<int> GetFinalStates()
        {
            return finalStateSet;
        }

        public IEnumerable<int> GetNonFinalStates()
        {
            foreach (var s in GetStates())
                if (!IsFinalState(s))
                    yield return s;
        }

        public virtual IEnumerable<Move<T>> GetMovesFrom(int sourceState)
        {
            return delta[sourceState];
        }

        public IEnumerable<Move<T>> GetEpsilonMovesFrom(int sourceState)
        {
            foreach (var m in delta[sourceState])
                if (m.IsEpsilon)
                    yield return m;
        }

        public IEnumerable<Move<T>> GetNonepsilonMovesFrom(int sourceState)
        {
            foreach (var m in delta[sourceState])
                if (!m.IsEpsilon)
                    yield return m;
        }

        public IEnumerable<Move<T>> GetNonepsilonMovesTo(int targetState)
        {
            foreach (var m in deltaInv[targetState])
                if (!m.IsEpsilon)
                    yield return m;
        }

        public IEnumerable<Move<T>> GetNonepsilonMovesTo(IEnumerable<int> targetStates)
        {
            foreach (var targetState in targetStates)
                foreach (var m in deltaInv[targetState])
                    if (!m.IsEpsilon)
                        yield return m;
        }

        public IEnumerable<Move<T>> GetMovesTo(int targetState)
        {
            return deltaInv[targetState];
        }

        public int GetMovesCountFrom(int sourceState)
        {
            return delta[sourceState].Count;
        }

        //assumes 0 <= n < GetMovesCountFrom(sourceState)
        public Move<T> GetNthMoveFrom(int sourceState, int n)
        {
            return delta[sourceState][n];
        }

        public IEnumerable<Move<T>> GetMovesFromStates(IEnumerable<int> sourceStates)
        {
            foreach (int s in sourceStates)
                foreach (Move<T> t in delta[s])
                    yield return t;
        }

        public bool HasSingleFinalSink
        {
            get
            {
                return finalStateSet.Count == 1 && delta[FinalState].Count == 0;
            }
        }

        /// <summary>
        /// Assumes that the states of the SFAs are disjoint and adds epsilon transitions if needed.
        /// Assumes also that fa is not Empty and not Epsilon.
        /// </summary>
        public void Concat(Automaton<T> fa)
        {
            foreach (var s in fa.States)
            {
                delta[s] = new List<Move<T>>(fa.delta[s]);
                deltaInv[s] = new List<Move<T>>(fa.deltaInv[s]);
            }

            //the optimization of the extended case seems to slow things down, thus commented it out
            if (this.HasSingleFinalSink && this.DoesNotContainWordBoundaries && fa.DoesNotContainWordBoundaries)// || (fa.InitialStateIsSource && !this.HasMoreThanOneFinalState))//???
            {
                #region rename all the final states to be the initial state of fa
                foreach (var f in finalStateSet)
                {
                    foreach (var move in deltaInv[f])
                    {
                        delta[move.SourceState].Remove(move);
                        var move1 = Move<T>.Create(move.SourceState == f ? fa.InitialState : move.SourceState, fa.InitialState, move.Label);
                        delta[move1.SourceState].Add(move1);
                        deltaInv[move1.TargetState].Add(move1);
                    }
                    delta.Remove(f);
                    deltaInv.Remove(f);
                }
                if (finalStateSet.Contains(initialState))
                    initialState = fa.initialState; //for example if this SFA is just a loop
                isEpsilonFree = isEpsilonFree && fa.isEpsilonFree;
                isDeterministic = isDeterministic && fa.isDeterministic;
                #endregion
            }
            else
            {
                #region add epsilon moves from final states to fa.InitialState
                foreach (var state in finalStateSet)
                {
                    var emove = Move<T>.Epsilon(state, fa.initialState);
                    this.delta[state].Add(emove);
                    this.deltaInv[fa.initialState].Add(emove);
                }
                this.isEpsilonFree = false;
                this.isDeterministic = false;
                #endregion
            }
            finalStateSet = fa.finalStateSet;
            maxState = Math.Max(maxState, fa.maxState);
            if (!fa.DoesNotContainWordBoundaries)
                this.AddWordBoundaries(fa.EnumerateWordBoundaries());
        }

        private bool AllFinalStatesAreSinks
        {
            get
            {
                foreach (int f in finalStateSet)
                    if (delta[f].Count > 0)
                        return false;
                return true;
            }
        }

        //assumes that the new final states are indeed states of the current sfa
        internal void SetFinalStates(IEnumerable<int> newFinalStates)
        {
            this.finalStateSet = new HashSet<int>(newFinalStates);
        }

        /// <summary>
        /// Mark the initial state as a final state.
        /// </summary>
        public void MakeInitialStateFinal()
        {
            finalStateSet.Add(initialState);
        }

        /// <summary>
        /// Add a new move.
        /// </summary>
        /// <param name="move">move to be added</param>
        public void AddMove(Move<T> move)
        {
            if (!delta.ContainsKey(move.SourceState))
            {
                delta[move.SourceState] = new List<Move<T>>();
                deltaInv[move.SourceState] = new List<Move<T>>();
            }
            if (!deltaInv.ContainsKey(move.TargetState))
            {
                delta[move.TargetState] = new List<Move<T>>();
                deltaInv[move.TargetState] = new List<Move<T>>();
            }
            if (delta[move.SourceState].Count > 0 || move.IsEpsilon)
                isDeterministic = false; //potentially the new move may spoil determinism
            delta[move.SourceState].Add(move);
            deltaInv[move.TargetState].Add(move);
            maxState = Math.Max(maxState, Math.Max(move.SourceState, move.TargetState));
            isEpsilonFree = isEpsilonFree && (!move.IsEpsilon);
        }


        public int MkNewFinalState(bool removeOldFinalStates = false)
        {
            if (removeOldFinalStates)
                finalStateSet.Clear();
            maxState += 1;
            finalStateSet.Add(maxState);
            this.delta[maxState] = new List<Move<T>>();
            this.deltaInv[maxState] = new List<Move<T>>();
            didTopSort = false;
            return maxState;
        }

        /// <summary>
        /// Returns true if there is an epsilon move from each final state to the initial state 
        /// and the initial state is also final
        /// </summary>
        public bool IsKleeneClosure()
        {
            if (!IsFinalState(initialState))
                return false;

            foreach (int state in finalStateSet)
                if (state != initialState)
                    if (!delta[state].Exists(IsEpsilonMoveToInitialState))
                        return false;

            return true;
        }

        internal bool IsEpsilonMoveToInitialState(Move<T> move)
        {
            return move.IsEpsilon && move.TargetState == initialState;
        }

        //assumes initial state has no incoming moves and p has no outgoing moves
        internal void RenameInitialState(int p)
        {
            var movesFromInitialState = delta[initialState];
            if (!delta.ContainsKey(p))
                delta[p] = new List<Move<T>>();
            if (!deltaInv.ContainsKey(p))
                deltaInv[p] = new List<Move<T>>();
            foreach (var move in movesFromInitialState)
            {
                var move1 = Move<T>.Create(p, move.TargetState, move.Label);
                deltaInv[move.TargetState].Remove(move);
                deltaInv[move.TargetState].Add(move1);
                delta[p].Add(move1);
            }
            if (finalStateSet.Contains(initialState))
            {
                finalStateSet.Remove(initialState);
                finalStateSet.Add(p);
            }
            delta.Remove(initialState);
            deltaInv.Remove(initialState);
            initialState = p;
        }

        /// <summary>
        /// Adds a new initial state that is also marked as a final state.
        /// Adds an epsilon transition from the new initial state to the original initial state.
        /// Assumes that newInitialState does not occur in the set of states.
        /// </summary>
        public void AddNewInitialStateThatIsFinal(int newInitialState)
        {
            finalStateSet.Add(newInitialState);
            var initialMoves = new List<Move<T>>();
            initialMoves.Add(Move<T>.Epsilon(newInitialState, initialState));
            delta[newInitialState] = initialMoves;
            deltaInv[newInitialState] = new List<Move<T>>();
            deltaInv[initialState].Add(initialMoves[0]);
            isDeterministic = false;
            isEpsilonFree = false;
            maxState = Math.Max(maxState, newInitialState);
            initialState = newInitialState;
        }

        //internal Automaton<S> MakeCopy(int newInitialState)
        //{
        //    int stateId = Math.Max(maxState, newInitialState) + 1;
        //    Dictionary<int, int> stateRemap = new Dictionary<int, int>();
        //    stateRemap[initialState] = newInitialState;
        //    List<Move<S>> moves = new List<Move<S>>();
        //    HashSet<int> newFinalStates = new HashSet<int>();
        //    foreach (Move<S> move in GetMoves())
        //    {
        //        int s;
        //        int t;
        //        if (!stateRemap.TryGetValue(move.SourceState, out s))
        //        {
        //            s = stateId++;
        //            stateRemap[move.SourceState] = s;
        //            if (finalStateSet.Contains(move.SourceState))
        //                newFinalStates.Add(s);
        //        }
        //        if (!stateRemap.TryGetValue(move.TargetState, out t))
        //        {
        //            t = stateId++;
        //            stateRemap[move.TargetState] = t;
        //            if (finalStateSet.Contains(move.TargetState))
        //                newFinalStates.Add(t);
        //        }
        //        moves.Add(Move<S>.M(s, t, move.Condition));
        //    }
        //    if (finalStateSet.Contains(initialState))
        //        newFinalStates.Add(newInitialState);
        //    return Create(newInitialState, newFinalStates, moves);
        //}

        internal Automaton<T> MakeCopy(Func<int> NewStateId)
        {
            int newInitialState = NewStateId();
            Dictionary<int, int> stateRemap = new Dictionary<int, int>();
            stateRemap[initialState] = newInitialState;
            List<Move<T>> moves = new List<Move<T>>();
            HashSet<int> newFinalStates = new HashSet<int>();
            foreach (Move<T> move in GetMoves())
            {
                int s;
                int t;
                if (!stateRemap.TryGetValue(move.SourceState, out s))
                {
                    s = NewStateId();
                    stateRemap[move.SourceState] = s;
                    if (finalStateSet.Contains(move.SourceState))
                        newFinalStates.Add(s);
                }
                if (!stateRemap.TryGetValue(move.TargetState, out t))
                {
                    t = NewStateId();
                    stateRemap[move.TargetState] = t;
                    if (finalStateSet.Contains(move.TargetState))
                        newFinalStates.Add(t);
                }
                moves.Add(Move<T>.Create(s, t, move.Label));
            }
            if (finalStateSet.Contains(initialState))
                newFinalStates.Add(newInitialState);
            var aut = Create(this.algebra, newInitialState, newFinalStates, moves);
            if (!this.DoesNotContainWordBoundaries)
                foreach (var q in EnumerateWordBoundaries())
                    aut.AddWordBoundary(stateRemap[q]);
            return aut;
        }

        /// <summary>
        /// Remove the state.
        /// </summary>
        /// <param name="state">state to be removed, must not be the initial state</param>
        public void RemoveTheState(int state)
        {
            foreach (var move in delta[state])
                deltaInv[move.TargetState].Remove(move);
            foreach (var move in deltaInv[state])
                delta[move.SourceState].Remove(move);
            delta.Remove(state);
            deltaInv.Remove(state);
            finalStateSet.Remove(state);
        }

        public void MakeStateNotFinal(int state)
        {
            if (finalStateSet.Contains(state))
            {
                finalStateSet.Remove(state);
            }
        }

        public void RemoveTheMove(Move<T> move)
        {
            delta[move.SourceState].Remove(move);
            deltaInv[move.TargetState].Remove(move);
            if (delta[move.SourceState].Count == 0 && deltaInv[move.SourceState].Count == 0)
            {
                //the source state has no incoming or outgoing moves
                if (move.SourceState != initialState)
                {
                    delta.Remove(move.SourceState);
                    deltaInv.Remove(move.SourceState);
                }
            }
            if (move.SourceState != move.TargetState)
            {
                if (delta[move.TargetState].Count == 0 && deltaInv[move.TargetState].Count == 0)
                {
                    //the target state has no incoming or outgoing moves
                    if (move.TargetState != initialState)
                    {
                        delta.Remove(move.TargetState);
                        deltaInv.Remove(move.TargetState);
                    }
                }
            }
        }

        /// <summary>
        /// Get the condition from the move from source to target
        /// </summary>
        internal T GetCondition(int source, int target)
        {
            foreach (var move in delta[source])
                if (move.TargetState == target)
                    return move.Label;
            throw new AutomataException(AutomataExceptionKind.InternalError);
        }

        #region Product construction
        /// <summary>
        /// Make a product of a and b. First removes epsilons from a and b.
        /// </summary>
        /// <param name="a">first automaton</param>
        /// <param name="b">second automaton</param>
        /// <param name="timeout">timeout in milliseconds</param>
        public static Automaton<T> MkProduct(Automaton<T> a, Automaton<T> b)
        {
            CheckIdentityOfAlgebras(a.algebra, b.algebra);
            return MkProduct(a, b, a.algebra, 0);
        }

        /// <summary>
        /// Internal usage when a and b have algebra = null, algebra is passed in explicitly
        /// and is only required to be IBooleanAlgebraPositive.
        /// </summary>
        internal static Automaton<T> MkProduct_(Automaton<T> a, Automaton<T> b, IBooleanAlgebraPositive<T> algebra)
        {
            return MkProduct(a, b, algebra, 0);
        }

        public static Automaton<T> MkProductOfDeterministicAutomata(params Automaton<T>[] automata)
        {
            if (automata.Length == 0)
                throw new AutomataException(AutomataExceptionKind.InvalidArgument_MustBeNonempty);


            IBooleanAlgebra<T> solver = automata[0].algebra;
            Automaton<T> res = automata[0];

            if (!res.isDeterministic)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNondeterministic);

            for (int i = 1; i < automata.Length; i++)
            {
                var a = automata[i];
                if (!a.isDeterministic)
                    throw new AutomataException(AutomataExceptionKind.AutomatonIsNondeterministic);
                CheckIdentityOfAlgebras(solver, a.algebra);
                res = MkProduct(res, a).Minimize();
            }

            return res;
        }

        static Automaton<T> MkProduct(Automaton<T> a, Automaton<T> b, IBooleanAlgebraPositive<T> solver, int timeout)
        {
            long timeout1 = Microsoft.Automata.Utilities.HighTimer.Frequency * ((long)timeout / (long)1000);
            long timeoutLimit;
            if (timeout > 0)
                timeoutLimit = Utilities.HighTimer.Now + timeout1;
            else
                timeoutLimit = 0;

            a = a.RemoveEpsilons(solver.MkOr);
            b = b.RemoveEpsilons(solver.MkOr);

            var stateIdMap = new Dictionary<Tuple<int, int>, int>();
            var initPair = new Tuple<int, int>(a.InitialState, b.InitialState);
            var frontier = new Stack<Tuple<int, int>>();
            frontier.Push(initPair);
            stateIdMap[initPair] = 0;

            var delta = new Dictionary<int, List<Move<T>>>();
            delta[0] = new List<Move<T>>();
            var states = new List<int>();
            states.Add(0);
            var finalStates = new List<int>();
            if (a.IsFinalState(a.InitialState) && b.IsFinalState(b.InitialState))
                finalStates.Add(0);

            int n = 1;
            while (frontier.Count > 0)
            {
                CheckTimeout(timeoutLimit);
                var currPair = frontier.Pop();
                int source = stateIdMap[currPair];
                var outTransitions = delta[source];

                foreach (var t1 in a.GetMovesFrom(currPair.Item1))
                    foreach (var t2 in b.GetMovesFrom(currPair.Item2))
                    {
                        var cond = solver.MkAnd(t1.Label, t2.Label);
                        if (!solver.IsSatisfiable(cond))
                            continue; //ignore the unsatisfiable move

                        Tuple<int, int> targetPair = new Tuple<int, int>(t1.TargetState, t2.TargetState);
                        int target;
                        if (!stateIdMap.TryGetValue(targetPair, out target))
                        {
                            //state has not yet been found
                            target = n;
                            n += 1;
                            stateIdMap[targetPair] = target;
                            states.Add(target);
                            delta[target] = new List<Move<T>>();
                            frontier.Push(targetPair);
                            if (a.IsFinalState(t1.TargetState) && b.IsFinalState(t2.TargetState))
                                finalStates.Add(target);
                        }
                        outTransitions.Add(Move<T>.Create(source, target, cond));
                    }
            }

            var incomingTransitions = new Dictionary<int, List<Move<T>>>();
            foreach (int state in states)
                incomingTransitions[state] = new List<Move<T>>();
            foreach (int state in states)
                foreach (Move<T> t in delta[state])
                    incomingTransitions[t.TargetState].Add(t);

            //backwards reachability, collect states that reach some final state
            Stack<int> stack = new Stack<int>(finalStates);
            HashSet<int> backReachableFromSomeFinal = new HashSet<int>(finalStates);
            while (stack.Count > 0)
                foreach (var t in incomingTransitions[stack.Pop()])
                    if (!backReachableFromSomeFinal.Contains(t.SourceState)) //otherwise already handled
                    {
                        stack.Push(t.SourceState);
                        backReachableFromSomeFinal.Add(t.SourceState);
                    }

            if (backReachableFromSomeFinal.Count == 0)
                return Automaton<T>.MkEmpty(a.algebra); //nothing is accepted

            //eliminate all dead states, i.e. states not in backReachableFromSomeFinal
            List<int> states1 = new List<int>();
            foreach (int state in states)
                if (!backReachableFromSomeFinal.Contains(state))
                    delta.Remove(state);
                else
                    states1.Add(state);
            states = states1;

            foreach (int state in states)
            {
                List<Move<T>> trans = new List<Move<T>>();
                foreach (Move<T> t in delta[state])
                {
                    if (backReachableFromSomeFinal.Contains(t.TargetState))
                        trans.Add(t);
                }
                delta[state] = trans;
            }
            if (finalStates.Count == 0)
                return Automaton<T>.MkEmpty(a.algebra); //nothing is accepted

            Automaton<T> product = Automaton<T>.Create(a.algebra, 0, finalStates, EnumerateMoves(delta));
            product.isEpsilonFree = true;
            product.isDeterministic = (a.IsDeterministic == true && b.IsDeterministic == true ? true : false);
            return product;
        }

        public static Automaton<T> MkAmbiguitySelfProduct(Automaton<T> a, out Automaton<T> ambiguousLanguage)
        {
            IBooleanAlgebra<T> solver = a.algebra;
            if (a.IsEmpty)
            {
                ambiguousLanguage = a;
                return a;
            }

            var stateIdMap = new Dictionary<Tuple<int, int>, int>();
            var initPair = new Tuple<int, int>(a.InitialState, a.InitialState);
            var frontier = new Stack<Tuple<int, int>>();
            frontier.Push(initPair);
            stateIdMap[initPair] = 0;

            var delta = new Dictionary<int, List<Move<T>>>();
            delta[0] = new List<Move<T>>();
            var states = new List<int>();
            states.Add(0);
            var finalStates = new List<int>();
            if (a.IsFinalState(a.InitialState))
                finalStates.Add(0);

            var ambiguousStates = new HashSet<int>();
            int n = 1;
            while (frontier.Count > 0)
            {
                var currPair = frontier.Pop();
                int source = stateIdMap[currPair];
                if (currPair.Item1 != currPair.Item2)
                    ambiguousStates.Add(source);

                var outTransitions = delta[source];

                foreach (var t1 in a.GetMovesFrom(currPair.Item1))
                    foreach (var t2 in a.GetMovesFrom(currPair.Item2))
                    {
                        var cond = solver.MkAnd(t1.Label, t2.Label);
                        if (!solver.IsSatisfiable(cond))
                            continue; //ignore the unsatisfiable move

                        Tuple<int, int> targetPair = new Tuple<int, int>(t1.TargetState, t2.TargetState);
                        int target;
                        if (!stateIdMap.TryGetValue(targetPair, out target))
                        {
                            //state has not yet been found
                            target = n;
                            n += 1;
                            stateIdMap[targetPair] = target;
                            states.Add(target);
                            delta[target] = new List<Move<T>>();
                            frontier.Push(targetPair);
                            if (a.IsFinalState(t1.TargetState) && a.IsFinalState(t2.TargetState))
                                finalStates.Add(target);
                        }
                        outTransitions.Add(Move<T>.Create(source, target, cond));
                    }
            }

            #region Dead states and unreachable state elimination
            var incomingTransitions = new Dictionary<int, List<Move<T>>>();
            foreach (int state in states)
                incomingTransitions[state] = new List<Move<T>>();
            foreach (int state in states)
                foreach (Move<T> t in delta[state])
                    incomingTransitions[t.TargetState].Add(t);

            //backwards reachability, collect states that reach some final state
            Stack<int> stack = new Stack<int>(finalStates);
            HashSet<int> backReachableFromSomeFinal = new HashSet<int>(finalStates);
            while (stack.Count > 0)
                foreach (var t in incomingTransitions[stack.Pop()])
                    if (!backReachableFromSomeFinal.Contains(t.SourceState)) //otherwise already handled
                    {
                        stack.Push(t.SourceState);
                        backReachableFromSomeFinal.Add(t.SourceState);
                    }

            //eliminate all dead states, i.e. states not in backReachableFromSomeFinal
            List<int> states1 = new List<int>();
            foreach (int state in states)
                if (!backReachableFromSomeFinal.Contains(state))
                    delta.Remove(state);
                else
                    states1.Add(state);
            states = states1;

            foreach (int state in states)
            {
                List<Move<T>> trans = new List<Move<T>>();
                foreach (Move<T> t in delta[state])
                {
                    if (backReachableFromSomeFinal.Contains(t.TargetState))
                        trans.Add(t);
                }
                delta[state] = trans;
            }
            #endregion


            var finalMoves = new List<Move<T>>(EnumerateMoves(delta));
            Automaton<T> product = Automaton<T>.Create(a.Algebra, 0, finalStates, finalMoves, true, true);
            product.isEpsilonFree = true;

            ambiguousLanguage = Automaton<T>.MkEmpty(a.algebra);
            foreach (var ambSt in ambiguousStates)
            {
                var auta = Automaton<T>.Create(a.Algebra, 0, new int[] { ambSt }, finalMoves, true, true);
                if (!auta.IsEmpty)
                {
                    var autb = Automaton<T>.Create(a.Algebra, ambSt, finalStates, finalMoves, true, true);
                    if (!autb.IsEmpty)
                    {
                        auta.Concat(autb);
                        ambiguousLanguage = ambiguousLanguage.Union(auta);
                    }
                }
            }

            return product;
        }

        private static IEnumerable<Move<T>> EnumerateMoves(Dictionary<int, List<Move<T>>> delta)
        {
            foreach (var kv in delta)
                foreach (var move in kv.Value)
                    yield return move;
        }

        #endregion

        #region Sum construction

        /// <summary>
        /// Make a sum (union) of a and b. Produces an automaton a+b such that L(a+b) = L(a) union L(b)
        /// </summary>
        public static Automaton<T> MkSum(Automaton<T> a, Automaton<T> b)
        {
            CheckIdentityOfAlgebras(a.algebra, b.algebra);

            int stateId = a.MaxState + 1;
            var b1 = b.MakeCopy(() => { return stateId++; });
            int initialState = stateId;

            List<Move<T>> moves = new List<Move<T>>();
            moves.Add(Move<T>.Epsilon(initialState, a.InitialState));
            moves.Add(Move<T>.Epsilon(initialState, b1.InitialState));
            moves.AddRange(a.GetMoves());
            moves.AddRange(b1.GetMoves());
            List<int> finalStates = new List<int>(a.GetFinalStates());
            finalStates.AddRange(b1.GetFinalStates());
            return Create(a.algebra, initialState, finalStates, moves);
        }

        /// <summary>
        /// Make a sum (union) of a and b. Produces an automaton a+b such that L(a+b) = L(a) union L(b)
        /// </summary>
        public static Automaton<T> MkSum(Automaton<T> a, IEnumerable<Automaton<T>> automata)
        {
            int stateId = a.MaxState + 1;

            List<Move<T>> moves = new List<Move<T>>();
            int initialState = stateId;

            moves.Add(Move<T>.Epsilon(initialState, a.InitialState));
            moves.AddRange(a.GetMoves());
            List<Automaton<T>> b1s = new List<Automaton<T>>();
            List<int> finalStates = new List<int>(a.GetFinalStates());
            int diff = 1;
            foreach (var b in automata)
            {
                var b1 = b.MakeCopy(() => { return stateId++; });
                b1s.Insert(0, b1);
                moves.Add(Move<T>.Epsilon(initialState, b1.InitialState));
                moves.AddRange(b1.GetMoves());
                finalStates.AddRange(b1.GetFinalStates());
                diff = b1.maxState + 1;
            }

            return Create(a.algebra, initialState, finalStates, moves);
        }

        #endregion

        #region Epsilon and Epsilon-loop removal

        /// <summary>
        /// Remove epsilon loops from this fsa and normalize the result, use disj to 
        /// to make disjunctions of conditions during normalization
        /// </summary>
        public Automaton<T> RemoveEpsilonLoops()
        {
            Func<T, T, T> disj = algebra.MkOr;
            var eEquiv = new Dictionary<int, int>();
            foreach (int state in States)
            {
                var eClosure = new IntSet(GetEpsilonClosure(state));
                eEquiv[state] = eClosure.Intersect(GetInvEpsilonClosure(state)).Choice;
            }
            Dictionary<Tuple<int, int>, T> conditionMap = new Dictionary<Tuple<int, int>, T>();
            HashSet<Move<T>> eMoves = new HashSet<Move<T>>();

            #region combine the moves using eEquiv representatives
            foreach (var move in GetMoves())
            {
                int s = eEquiv[move.SourceState];
                int t = eEquiv[move.TargetState];
                if (move.IsEpsilon)
                {
                    if (s == t)
                        continue; //ignore self-epsilon-loops
                    else
                        eMoves.Add(Move<T>.Epsilon(s, t));
                }
                else
                {
                    var p = new Tuple<int, int>(s, t);
                    T cond;
                    if (conditionMap.TryGetValue(p, out cond))
                        conditionMap[p] = disj(cond, move.Label);
                    else
                        conditionMap[p] = move.Label;
                }
            }
            #endregion

            int initialState = eEquiv[InitialState];
            HashSet<int> finalStates = new HashSet<int>();
            foreach (int state in GetFinalStates())
                finalStates.Add(eEquiv[state]);

            return Automaton<T>.Create(this.algebra, initialState, finalStates, EnumerateMoves(conditionMap, eMoves));
        }

        private IEnumerable<Move<T>> EnumerateMoves(Dictionary<Tuple<int, int>, T> conditionMap, HashSet<Move<T>> eMoves)
        {
            foreach (var kv in conditionMap)
                yield return Move<T>.Create(kv.Key.Item1, kv.Key.Item2, kv.Value);
            foreach (var move in eMoves)
                yield return move;
        }

        /// <summary> 
        /// Creates an automaton without epsilon transitions.
        /// States unreachable from the initial state are eliminated.
        /// The reachable states remain the same.
        /// </summary>
        public Automaton<T> RemoveEpsilons()
        {
            return RemoveEpsilons(algebra.MkOr);
        }

        /// <summary> 
        /// Used internally or when algebra=null
        /// </summary>
        internal Automaton<T> RemoveEpsilons(Func<T, T, T> disj)
        {
            var fa = this;
            if (fa.IsEpsilonFree)
                return fa;

            #region collect all conditions on non-epsilon transitions
            Dictionary<Tuple<int, int>, T> conditions = new Dictionary<Tuple<int, int>, T>();
            foreach (Move<T> t in fa.GetMoves())
                if (!t.IsEpsilon)
                {
                    T cond;
                    var pair = new Tuple<int, int>(t.SourceState, t.TargetState);
                    if (conditions.TryGetValue(pair, out cond))
                        conditions[pair] = disj(t.Label, cond);
                    else
                        conditions[pair] = t.Label;
                }
            #endregion

            #region accumulate transition conditions via epsilon transitions
            foreach (int q in fa.States)
                foreach (int p in fa.GetEpsilonClosure(q))
                    if (p != q)
                        foreach (Move<T> t in fa.GetMovesFrom(p))
                            if (!t.IsEpsilon)
                            {
                                T cond;
                                var pair = new Tuple<int, int>(q, t.TargetState);
                                if (conditions.TryGetValue(pair, out cond) && !cond.Equals(t.Label))
                                    conditions[pair] = disj(t.Label, cond);
                                else
                                    conditions[pair] = t.Label;
                            }
            #endregion

            #region map all states to their outgoing transitions
            Dictionary<int, List<Move<T>>> outgoingTransitions = new Dictionary<int, List<Move<T>>>();
            foreach (int s in fa.States)
                outgoingTransitions[s] = new List<Move<T>>();
            foreach (var kv in conditions)
                outgoingTransitions[kv.Key.Item1].Add(Move<T>.Create(kv.Key.Item1, kv.Key.Item2, kv.Value));
            #endregion

            #region collect all states reachable from the initial state
            //standard depth first traversal of the graph
            Stack<int> frontier = new Stack<int>();
            frontier.Push(fa.InitialState);
            HashSet<int> reachableFromInitial = new HashSet<int>();
            reachableFromInitial.Add(fa.InitialState);
            while (frontier.Count > 0)
                foreach (var t in outgoingTransitions[frontier.Pop()])
                    if (!reachableFromInitial.Contains(t.TargetState)) //otherwise already handled
                    {
                        frontier.Push(t.TargetState);
                        reachableFromInitial.Add(t.TargetState);
                    }
            #endregion

            #region eliminate unreachable states
            List<int> states = new List<int>();
            //eliminate states that are not reachable from the initial state
            //also remove all the outgoing transitions from such states
            foreach (int s in fa.States)
                if (reachableFromInitial.Contains(s))
                    states.Add(s);
                else
                    outgoingTransitions.Remove(s);
            #endregion

            //a state is final if its epsilon closure contains a final state
            List<int> finalStates = new List<int>();
            foreach (int state in states)
            {
                foreach (int s in fa.GetEpsilonClosure(state))
                {
                    if (fa.IsFinalState(s))
                    {
                        finalStates.Add(state);
                        break;
                    }
                }
            }

            Automaton<T> nfa = Automaton<T>.Create(this.algebra, fa.InitialState, finalStates, EnumerateMoves(outgoingTransitions));
            nfa.isEpsilonFree = true;
            return nfa;
        }

        #endregion

        /// <summary>
        /// Eliminate all non-initial states from the automaton from which no final state is recahable 
        /// </summary>
        public void EliminateDeadStates()
        {
            //backwards reachability, collect states that reach some final state
            Stack<int> stack = new Stack<int>(this.finalStateSet);
            HashSet<int> backReachableFromSomeFinal = new HashSet<int>(this.finalStateSet);
            while (stack.Count > 0)
                foreach (var t in deltaInv[stack.Pop()])
                    if (backReachableFromSomeFinal.Add(t.SourceState)) //if returns false then already added
                        stack.Push(t.SourceState);

            //eliminate all dead states, i.e. states not in backReachableFromSomeFinal
            List<int> deadstates = new List<int>();
            foreach (int state in States)
                if (!backReachableFromSomeFinal.Contains(state))
                    deadstates.Add(state);

            foreach (int state in deadstates)
                if (state != initialState)
                    RemoveTheState(state);
        }

        /// <summary>
        /// Eliminate all non-initial states from the automaton from which no final state is recahable 
        /// </summary>
        public void EliminateUnrreachableStates()
        {
            //forward reachability, keep only states that are reacable fron the initial state
            Stack<int> stack = new Stack<int>();
            stack.Push(InitialState);
            HashSet<int> reachableStates = new HashSet<int>();
            reachableStates.Add(InitialState);
            while (stack.Count > 0)
            {
                var q = stack.Pop();
                foreach (var t in delta[q])
                    if (reachableStates.Add(t.TargetState)) //if returns false then already added
                        stack.Push(t.TargetState);
            }

            //eliminate all nonreachable states, i.e. states not in reachableStates
            List<int> unreachableStates = new List<int>();
            foreach (int state in States)
                if (!reachableStates.Contains(state))
                    unreachableStates.Add(state);

            foreach (int state in unreachableStates)
                RemoveTheState(state);
        }

        /// <summary>
        /// Creates the automaton that accepts L(this)-L(B).
        /// </summary>
        /// <param name="B">another automaton</param>
        /// <param name="solver">boolean algebra solver over S</param>
        public Automaton<T> Minus(Automaton<T> B)
        {
            return MkDifference(this, B, -1);
        }

        /// <summary>
        /// Creates the automaton that accepts the complement of L(this).
        /// </summary>
        /// <param name="solver">boolean algebra solver over S</param>
        public Automaton<T> Complement()
        {
            return MkDifference(MkFull(algebra), this, -1);
        }

        /// <summary>
        /// Creates the automaton that accepts the intersection of L(this) and L(B).
        /// </summary>
        /// <param name="B">another automaton</param>
        /// <param name="solver">boolean algebra solver over S</param>
        /// <param name="timeout">timeout in ms</param>
        public Automaton<T> Intersect(Automaton<T> B, int timeout = 0)
        {
            return MkProduct(this, B, algebra, timeout);
        }

        /// <summary>
        /// Creates the automaton that accepts the union of L(this) and L(B).
        /// Uses additional epsilon transitions and does not need the solver for S.
        /// </summary>
        /// <param name="B">another automaton</param>
        public Automaton<T> Union(Automaton<T> B)
        {
            return MkSum(this, B);
        }

        /// <summary>
        /// Creates the automaton that accepts the union of L(this) and L(B).
        /// Uses additional epsilon transitions and does not need the solver for S.
        /// </summary>
        /// <param name="B">another automaton</param>
        public Automaton<T> Union(IEnumerable<Automaton<T>> automata)
        {
            return MkSum(this, automata);
        }

        /// <summary>
        /// Returns the language containing all the suffixes of L
        /// </summary>
        public Automaton<T> SuffixLanguage()
        {
            List<Move<T>> newMoves = new List<Move<T>>(this.GetMoves());
            int newInitState = maxState + 1;
            foreach (var state in States)
                newMoves.Add(Move<T>.Epsilon(newInitState, state));

            List<int> finalStates = new List<int>(GetFinalStates());
            return Create(this.algebra, newInitState, finalStates, newMoves);
        }

        /// <summary>
        /// Returns the language containing all the prefixes of L
        /// </summary>
        public Automaton<T> PrefixLanguage()
        {
            List<Move<T>> newMoves = new List<Move<T>>(this.GetMoves());
            int newFinalState = maxState + 1;
            foreach (var state in States)
                newMoves.Add(Move<T>.Epsilon(state, newFinalState));

            return Create(this.algebra, initialState, new int[] { newFinalState }, newMoves);
        }

        static void CheckIdentityOfAlgebras(IBooleanAlgebra<T> solver1, IBooleanAlgebra<T> solver2)
        {
            if (solver1 != solver2)
                throw new AutomataException(AutomataExceptionKind.IncompatibleAlgebras);
        }

        /// <summary>
        /// Returns true iff this automaton and another automaton B are equivalent
        /// </summary>
        /// <param name="B">another automaton</param>
        public bool IsEquivalentWith(Automaton<T> B, out List<T> witness)
        {
            CheckIdentityOfAlgebras(algebra, B.algebra);

            if (this.isDeterministic && B.isDeterministic)
            {
                var res1 = areHKEquivalentDeterministic(this.MakeTotal(), B.MakeTotal());
                witness = res1.Item2;
                return res1.Item1;
            }

            var res = areHKEquivalent(this.RemoveEpsilons().MakeTotal(), B.RemoveEpsilons().MakeTotal());
            witness = res.Item2;
            return res.Item1;
        }

        /// <summary>
        /// Returns true iff this automaton and another automaton B are equivalent
        /// </summary>
        /// <param name="B">another automaton</param>
        public bool IsEquivalentWith(Automaton<T> B)
        {
            CheckIdentityOfAlgebras(algebra, B.algebra);      
      
            if(this.isDeterministic && B.isDeterministic)
                return areHKEquivalentDeterministic(this.MakeTotal(), B.MakeTotal()).Item1;

            return areHKEquivalent(this.RemoveEpsilons().MakeTotal(), B.RemoveEpsilons().MakeTotal()).Item1;
        }

        /**
	     * Checks whether laut and raut are equivalent using HopcroftKarp on the SFA
	     * accepting the reverse language
	    */
        public static Tuple<Boolean, List<T>> areHKEquivalent(Automaton<T> aut1, Automaton<T> aut2)
        {
            var ds = new UnionFindHopKarp<T>();

            var reached1 = new Dictionary<int, int>();
            var reached2 = new Dictionary<int, int>();

            var toVisit = new List<Tuple<int, int>>();

            List<int> aut1States = new List<int>(aut1.States);
            PowerSetStateBuilder dfaStateBuilderForAut1 = PowerSetStateBuilder.Create(aut1States.ToArray());
            List<int> aut2States = new List<int>(aut2.States);
            PowerSetStateBuilder dfaStateBuilderForAut2 = PowerSetStateBuilder.Create(aut2States.ToArray());
            Func<int, bool> IsDFAFinalStateForAut1 = (state) =>
            {
                foreach (int st in dfaStateBuilderForAut1.GetMembers(state))
                    if (aut1.IsFinalState(st))
                        return true;
                return false;
            };
            Func<int, bool> IsDFAFinalStateForAut2 = (state) =>
            {
                foreach (int st in dfaStateBuilderForAut2.GetMembers(state))
                    if (aut2.IsFinalState(st))
                        return true;
                return false;
            };


            int st1 = dfaStateBuilderForAut1.MakePowerSetState(new int[] { aut1.InitialState });
            int st2 = dfaStateBuilderForAut2.MakePowerSetState(new int[] { aut2.InitialState });

            reached1[st1] = 0;
            reached2[st2] = 1;

            toVisit.Add(new Tuple<int, int>(st1, st2));

            bool isIn1Final = IsDFAFinalStateForAut1(st1);
            bool isIn2Final = IsDFAFinalStateForAut2(st2);
            if (isIn1Final != isIn2Final)
                return new Tuple<bool, List<T>>(false, new List<T>());

            ds.add(0, isIn1Final, new List<T>());
            ds.add(1, isIn2Final, new List<T>());
            ds.mergeSets(0, 1);

            while (toVisit.Count > 0)
            {

                var curr = toVisit[0];
                toVisit.RemoveAt(0);

                var curr1 = dfaStateBuilderForAut1.GetMembers(curr.Item1);
                var curr2 = dfaStateBuilderForAut2.GetMembers(curr.Item2);

                var movesFromCurr1 = new List<Move<T>>(aut1.GetMovesFromStates(curr1));
                var movesFromCurr2 = new List<Move<T>>(aut2.GetMovesFromStates(curr2));
                var predicates1 = Array.ConvertAll(movesFromCurr1.ToArray(), move => { return move.Label; });
                var predicates2 = Array.ConvertAll(movesFromCurr2.ToArray(), move => { return move.Label; });

                var minterms1 = aut1.algebra.GenerateMinterms(predicates1);
                var minterms2 = aut2.algebra.GenerateMinterms(predicates2);


                foreach (var minterm1 in minterms1)
                {                    
                    foreach (var minterm2 in minterms2)
                    {
                        var conj = aut1.algebra.MkAnd(minterm1.Item2, minterm2.Item2);
                        if (aut1.algebra.IsSatisfiable(conj))
                        {
                            var to1 = new HashSet<int>();
                            for (int i = 0; i < minterm1.Item1.Length; i++)
                                if (minterm1.Item1[i])
                                    to1.Add(movesFromCurr1[i].TargetState);
                            var to1st = dfaStateBuilderForAut1.MakePowerSetState(to1);

                            var to2 = new HashSet<int>();
                            for (int i = 0; i < minterm2.Item1.Length; i++)
                                if (minterm2.Item1[i])
                                    to2.Add(movesFromCurr2[i].TargetState);
                                
                            var to2st = dfaStateBuilderForAut2.MakePowerSetState(to2);


                            var wit = ds.getWitness(reached1[curr.Item1]);
                            var pref = new List<T>(wit);
                            pref.Add(conj);


                            // If not in union find add them
                            int r1 = 0, r2 = 0;
                            if (!reached1.ContainsKey(to1st))
                            {
                                r1 = ds.getNumberOfElements();
                                reached1[to1st] = r1;
                                ds.add(r1, IsDFAFinalStateForAut1(to1st), pref);
                            }
                            else
                                r1 = reached1[to1st];

                            if (!reached2.ContainsKey(to2st))
                            {
                                r2 = ds.getNumberOfElements();
                                reached2[to2st] = r2;
                                ds.add(r2, IsDFAFinalStateForAut2(to2st), pref);
                            }
                            else
                                r2 = reached2[to2st];

                            // Check whether are in simulation relation
                            if (!ds.areInSameSet(r1, r2))
                            {
                                if (!ds.mergeSets(r1, r2))
                                    return new Tuple<Boolean, List<T>>(false, pref);

                                toVisit.Add(new Tuple<int, int>(to1st, to2st));
                            }
                        }
                    }
                }
            }
            return new Tuple<bool, List<T>>(true, null);
        }

        /**
	     * Checks whether laut and raut are equivalent using HopcroftKarp on the SFA
	     * accepting the reverse language
	    */
        public static Tuple<Boolean, List<T>> areHKEquivalentDeterministic(Automaton<T> aut1, Automaton<T> aut2)
        {
            var ds = new UnionFindHopKarp<T>();

            var reached1 = new Dictionary<int, int>();
            var reached2 = new Dictionary<int, int>();

            var toVisit = new List<Tuple<int, int>>();

            reached1[aut1.initialState] = 0;
            reached2[aut2.initialState] = 1;

            toVisit.Add(new Tuple<int, int>(aut1.initialState, aut2.initialState));

            bool isIn1Final = aut1.IsFinalState(aut1.initialState);
            bool isIn2Final = aut2.IsFinalState(aut2.initialState);
            if (isIn1Final != isIn2Final)
                return new Tuple<bool, List<T>>(false, new List<T>());

            ds.add(0, isIn1Final, new List<T>());
            ds.add(1, isIn2Final, new List<T>());
            ds.mergeSets(0, 1);

            while (toVisit.Count > 0)
            {

                var curr = toVisit[0];
                toVisit.RemoveAt(0);

                var curr1 = curr.Item1;
                var curr2 = curr.Item2;

                var movesFromCurr1 = new List<Move<T>>(aut1.GetMovesFrom(curr1));
                var movesFromCurr2 = new List<Move<T>>(aut2.GetMovesFrom(curr2));


                foreach (var move1 in movesFromCurr1)
                    foreach (var move2 in movesFromCurr2)
                    {
                        var conj = aut1.algebra.MkAnd(move1.Label, move2.Label);
                        if (aut1.algebra.IsSatisfiable(conj))
                        {
                            var to1 = move1.TargetState;
                            var to2 = move2.TargetState;


                            var wit = ds.getWitness(reached1[curr.Item1]);
                            var pref = new List<T>(wit);
                            pref.Add(conj);


                            // If not in union find add them
                            int r1 = 0, r2 = 0;
                            if (!reached1.ContainsKey(to1))
                            {
                                r1 = ds.getNumberOfElements();
                                reached1[to1] = r1;
                                ds.add(r1, aut1.IsFinalState(to1), pref);
                            }
                            else
                                r1 = reached1[to1];

                            if (!reached2.ContainsKey(to2))
                            {
                                r2 = ds.getNumberOfElements();
                                reached2[to2] = r2;
                                ds.add(r2, aut2.IsFinalState(to2), pref);
                            }
                            else
                                r2 = reached2[to2];

                            // Check whether are in simulation relation
                            if (!ds.areInSameSet(r1, r2))
                            {
                                if (!ds.mergeSets(r1, r2))
                                    return new Tuple<Boolean, List<T>>(false, pref);

                                toVisit.Add(new Tuple<int, int>(to1, to2));
                            }
                        }
                    }
            }
            return new Tuple<bool, List<T>>(true, null);
        }

        /// <summary>
        /// Make the automaton A x Complement(B). The automaton is empty iff L(A) is a subset of L(B).
        /// Throws TimeoutException if the construction does not finish within the given mumber of milliseconds.
        /// </summary>
        /// <param name="A">subset automaton</param>
        /// <param name="B">superset automaton</param>
        /// <param name="timeout">timeout in milliseconds (0 or a negative number means no timeout)</param>
        /// <returns>automaton accepting L(A x Complement(B))</returns>
        static public Automaton<T> MkDifference(Automaton<T> A, Automaton<T> B, int timeout)
        {
            CheckIdentityOfAlgebras(A.algebra, B.algebra);
            var solver = A.algebra;
            long timeout1 = Microsoft.Automata.Utilities.HighTimer.Frequency * ((long)timeout / (long)1000);
            long timeoutLimit;
            if (timeout > 0)
                timeoutLimit = Utilities.HighTimer.Now + timeout1;
            else
                timeoutLimit = 0;

            A = A.RemoveEpsilons();
            B = B.RemoveEpsilons();

            #region determinisitic case for B
            B.CheckDeterminism();
            if (B.isDeterministic)
                return MkProduct(A, B.MkComplement(solver));
            #endregion

            B = B.MakeTotal(); //???

            //the sink state is represented implicitly, there is no need to make B total
            List<int> bStates = new List<int>(B.States);
            PowerSetStateBuilder dfaStateBuilderForB = PowerSetStateBuilder.Create(bStates.ToArray());

            //#region compute potential states

            //HashSet<int> potentialStatesInB = new HashSet<int>();
            //foreach (int state in B.States)
            //    if (!B.IsFinalState(state))
            //        potentialStatesInB.Add(state);

            //Stack<int> stackOfPotentials = new Stack<int>(potentialStatesInB);
            //while (stackOfPotentials.Count > 0)
            //{
            //    var potState = stackOfPotentials.Pop();
            //    foreach (var move in B.GetMovesTo(potState))
            //    {
            //        if (!potentialStatesInB.Contains(move.SourceState))
            //        {
            //            potentialStatesInB.Add(move.SourceState);
            //            stackOfPotentials.Push(move.SourceState);
            //        }
            //    }
            //}

            //Func<Tuple<int, int>, bool> IsPotentialState = pair =>
            //{
            //    foreach (int BState in dfaStateBuilderForB.GetNfaStates(pair.Second))
            //        if (!potentialStatesInB.Contains(BState))
            //            return false;
            //    return true;
            //};

            //#endregion

            //returns true if the first element is a final state in A and none of the B states represented by the second dfa state is final in B
            Func<Tuple<int, int>, bool> IsFinalState = pair =>
            {
                if (!A.IsFinalState(pair.Item1))
                    return false;
                if (pair.Item2 == -1)
                    return true; //the implicit sink state in determinization of B is a final state in the complement
                foreach (int BState in dfaStateBuilderForB.GetMembers(pair.Item2))
                    if (B.IsFinalState(BState))
                        return false;
                return true;
            };

            var stack = new Stack<Tuple<int, int>>();

            var prodStartState = new Tuple<int, int>(A.InitialState, dfaStateBuilderForB.MakePowerSetState(new int[] { B.InitialState }));
            stack.Push(prodStartState);

            int prodInitialStateId = 0;
            var prodStateIdMap = new Dictionary<Tuple<int, int>, int>();
            prodStateIdMap[prodStartState] = prodInitialStateId; //initial state
            int prodStateId = prodInitialStateId + 1;
            var prodDelta = new Dictionary<int, List<Move<T>>>();
            prodDelta[prodInitialStateId] = new List<Move<T>>();
            var prodFinalStateIds = new HashSet<int>();
            if (IsFinalState(prodStartState))
                prodFinalStateIds.Add(prodInitialStateId);

            Func<Tuple<int, int>, int> GetProdStateID = pair =>
            {
                int stateId;
                if (!prodStateIdMap.TryGetValue(pair, out stateId))
                {
                    stateId = prodStateId++;
                    prodStateIdMap[pair] = stateId;
                }
                return stateId;
            };

            while (stack.Count > 0)
            {

                var prodSourceState = stack.Pop();
                if (prodSourceState.Item2 == -1) //sink state, that is a final state in the complement of B
                {
                    foreach (var move in A.GetMovesFrom(prodSourceState.Item1))
                    {
                        CheckTimeout(timeoutLimit);
                        var prodSrcStateId = GetProdStateID(prodSourceState);
                        var prodTgtWithSink = new Tuple<int, int>(move.TargetState, -1);
                        int prodTgtWithSinkId = GetProdStateID(prodTgtWithSink);
                        prodDelta[prodSrcStateId].Add(Move<T>.Create(prodSrcStateId, prodTgtWithSinkId, move.Label));
                        if (!prodDelta.ContainsKey(prodTgtWithSinkId))
                        {
                            prodDelta[prodTgtWithSinkId] = new List<Move<T>>();
                            if (A.IsFinalState(move.TargetState))
                                prodFinalStateIds.Add(prodTgtWithSinkId);
                            stack.Push(prodTgtWithSink);
                        }
                    }
                }
                else
                {
                    var Amoves = A.delta[prodSourceState.Item1];
                    var Bmoves = new List<Move<T>>(B.GetMovesFromStates(dfaStateBuilderForB.GetMembers(prodSourceState.Item2)));
                    var Aconds = Array.ConvertAll(Amoves.ToArray(), move => { return move.Label; });
                    var Bconds = Array.ConvertAll(Bmoves.ToArray(), move => { return move.Label; });

                    int m = Amoves.Count;
                    int n = Bmoves.Count;

                    var ABcombinations = new List<Tuple<int, Tuple<bool[], T>>>();
                    //var Bcombinations = ConsList<Tuple<bool[],S>>.Create(solver.GenerateMinterms(Bconds));
                    foreach (var Bcomb in solver.GenerateMinterms(Bconds))
                        for (int i = 0; i < m; i++)
                        {
                            CheckTimeout(timeoutLimit);
                            var ABcond = solver.MkAnd(Aconds[i], Bcomb.Item2);
                            if (solver.IsSatisfiable(ABcond))
                                ABcombinations.Add(new Tuple<int, Tuple<bool[], T>>(i, new Tuple<bool[], T>(Bcomb.Item1, ABcond)));
                        }

                    //construct and push the new product states
                    //consider all A moves one at a time

                    int prodSourceStateId = GetProdStateID(prodSourceState);

                    foreach (var solution in ABcombinations)
                    {
                        CheckTimeout(timeoutLimit);
                        var Amove = Amoves[solution.Item1];
                        var nfaTargetStates = new List<int>();
                        for (int j = 0; j < n; j++)
                            if (solution.Item2.Item1[j])
                                nfaTargetStates.Add(Bmoves[j].TargetState);
                        //if all B-conditions are false then this leads to the sink state -1
                        int dfaTargetState;
                        if (nfaTargetStates.Count > 0)
                            dfaTargetState = dfaStateBuilderForB.MakePowerSetState(nfaTargetStates);
                        else
                            dfaTargetState = -1; //sink state
                        var prodTargetState = new Tuple<int, int>(Amove.TargetState, dfaTargetState);
                        int prodTargetStateId = GetProdStateID(prodTargetState);
                        T prodCondition = solution.Item2.Item2;
                        var prodMove = Move<T>.Create(prodSourceStateId, prodTargetStateId, prodCondition);
                        prodDelta[prodSourceStateId].Add(prodMove);
                        if (!prodDelta.ContainsKey(prodTargetStateId))
                        {
                            prodDelta[prodTargetStateId] = new List<Move<T>>();
                            if (IsFinalState(prodTargetState))
                                prodFinalStateIds.Add(prodTargetStateId);
                            // if (IsPotentialState(prodTargetState))//???
                            stack.Push(prodTargetState);
                            //else
                            //{
                            //    int foo = 0;
                            //    foo = foo + 1;
                            //}

                        }
                    }
                }

            }
            if (prodFinalStateIds.Count == 0)
                return Automaton<T>.MkEmpty(A.algebra);

            var prodFinalStateIdsList = new List<int>(prodFinalStateIds);
            Automaton<T> prod = Automaton<T>.Create(A.algebra, prodInitialStateId, prodFinalStateIdsList, EnumerateMoves(prodDelta));
            prod.isEpsilonFree = true;
            if (A.IsDeterministic)
                prod.isDeterministic = true;

            prod.EliminateDeadStates();
            return prod;
        }


        #region Lazy difference

        /// <summary>
        /// Returns true iff A-B is nonempty.
        /// If true, outputs a witness that is a symbolic trace in A but not in B.
        /// </summary>
        static public bool CheckDifference(Automaton<T> A, Automaton<T> B, int timeout,
                                           out List<T> witness)
        {
            CheckIdentityOfAlgebras(A.algebra, B.algebra);
            IBooleanAlgebra<T> solver = A.algebra;
            long timeout1 = Microsoft.Automata.Utilities.HighTimer.Frequency * ((long)timeout / (long)1000);
            long timeoutLimit;
            if (timeout > 0)
                timeoutLimit = Utilities.HighTimer.Now + timeout1;
            else
                timeoutLimit = 0;

            A = A.RemoveEpsilons();
            B = B.RemoveEpsilons();

            B = B.MakeTotal(); //???

            // #region determinisitic case for B

            // // PIETER: Does this really help? Seems like we're going to implicitly 
            // // check for pairwise disjointness anyway; we'll simply end up with
            // // many sets of size 1. If set creation cost is a concern, we could
            // // use a custom datastructure<T> optimized for size 1, with a set type
            // // T for sets of size > 1
            // B.CheckDeterminism(solver);
            // if (B.isDeterministic)
            //    return MkProduct(A, B.MkComplement(solver), solver);
            // #endregion


            //the sink state is represented implicitly, there is no need to make B total
            List<int> bStates = new List<int>(B.States);
            PowerSetStateBuilder dfaStateBuilderForB = PowerSetStateBuilder.Create(bStates.ToArray());

            //returns true if the first element is a final state in A and none of the B states represented by the second dfa state is final in B
            Func<Tuple<int, int>, bool> IsFinalState = pair =>
            {
                if (!A.IsFinalState(pair.Item1))
                    return false;
                if (pair.Item2 == -1)
                    return true; //the implicit sink state in determinization of B is a final state in the complement
                foreach (int BState in dfaStateBuilderForB.GetMembers(pair.Item2))
                    if (B.IsFinalState(BState))
                        return false;
                return true;
            };

            var stack = new Stack<Tuple<int, int>>();
            var prodStartState = new Tuple<int, int>(A.InitialState, dfaStateBuilderForB.MakePowerSetState(new int[] { B.InitialState }));
            stack.Push(prodStartState);
            var witnessTree = new Dictionary<Tuple<int, int>, Tuple<T, Tuple<int, int>>>();
            witnessTree[prodStartState] = null;

            if (IsFinalState(prodStartState))
            {
                witness = new List<T>();
                return true;
            }

            while (stack.Count > 0)
            {
                var prodSourceState = stack.Pop();

                if (prodSourceState.Item2 == -1) //sink state, that is a final state in the complement of B
                {
                    #region implicit sink
                    foreach (var move in A.GetMovesFrom(prodSourceState.Item1))
                    {
                        var prodTgtWithSink = new Tuple<int, int>(move.TargetState, -1);

                        if (!witnessTree.ContainsKey(prodTgtWithSink))
                        {
                            if (A.IsFinalState(move.TargetState))
                            {
                                #region return true and set the witness
                                //final state was found, extract the witness from the witness tree
                                List<T> w = new List<T>();
                                w.Add(move.Label);
                                var parent = witnessTree[prodSourceState];
                                while (parent != null)
                                {
                                    w.Add(parent.Item1);
                                    parent = witnessTree[parent.Item2];
                                }
                                w.Reverse();
                                witness = w;
                                return true;
                                #endregion
                            }
                            witnessTree[prodTgtWithSink] = new Tuple<T, Tuple<int, int>>(move.Label, prodSourceState);
                            stack.Push(prodTgtWithSink);
                        }
                    }
                    #endregion
                }
                else
                {
                    #region explicit moves
                    var Amoves = A.delta[prodSourceState.Item1];
                    var Bmoves = new List<Move<T>>(B.GetMovesFromStates(dfaStateBuilderForB.GetMembers(prodSourceState.Item2)));
                    var Aconds = Array.ConvertAll(Amoves.ToArray(), move => { return move.Label; });
                    var Bconds = Array.ConvertAll(Bmoves.ToArray(), move => { return move.Label; });

                    int m = Amoves.Count;
                    int n = Bmoves.Count;

                    var ABcombinations = new List<Tuple<int, Tuple<bool[], T>>>();
                    //var Bcombinations = ConsList<Tuple<bool[],S>>.Create(solver.GenerateMinterms(Bconds));
                    foreach (var Bcomb in solver.GenerateMinterms(Bconds))
                        for (int i = 0; i < m; i++)
                        {
                            CheckTimeout(timeoutLimit);
                            var ABcond = solver.MkAnd(Aconds[i], Bcomb.Item2);
                            if (solver.IsSatisfiable(ABcond))
                                ABcombinations.Add(new Tuple<int, Tuple<bool[], T>>(i, new Tuple<bool[], T>(Bcomb.Item1, ABcond)));
                        }

                    //construct and push the new product states
                    //consider all A moves one at a time
                    foreach (var solution in ABcombinations)
                    {
                        CheckTimeout(timeoutLimit);
                        var Amove = Amoves[solution.Item1];
                        var nfaTargetStates = new List<int>();
                        for (int j = 0; j < n; j++)
                            if (solution.Item2.Item1[j])
                                nfaTargetStates.Add(Bmoves[j].TargetState);
                        //if all B-conditions are false then this leads to the sink state -1
                        int dfaTargetState;
                        if (nfaTargetStates.Count > 0)
                            dfaTargetState = dfaStateBuilderForB.MakePowerSetState(nfaTargetStates);
                        else
                            dfaTargetState = -1; //sink state
                        var prodTargetState = new Tuple<int, int>(Amove.TargetState, dfaTargetState);

                        T prodCondition = solution.Item2.Item2;
                        if (!witnessTree.ContainsKey(prodTargetState))
                        {
                            if (IsFinalState(prodTargetState))
                            {
                                #region return true and set the witness
                                //final state was found, extract the witness from the witness tree
                                List<T> w = new List<T>();
                                w.Add(prodCondition);
                                var parent = witnessTree[prodSourceState];
                                while (parent != null)
                                {
                                    w.Add(parent.Item1);
                                    parent = witnessTree[parent.Item2];
                                }
                                w.Reverse();
                                witness = w;
                                return true;
                                #endregion
                            }
                            witnessTree[prodTargetState] = new Tuple<T, Tuple<int, int>>(prodCondition, prodSourceState);
                            stack.Push(prodTargetState);
                        }
                    }
                    #endregion
                }
            }


            //if (prodFinalStateIds.Count == 0)
            //   return false;

            //var prodFinalStateIdsList = new List<int>(prodFinalStateIds);
            // Automaton<S> prod = Automaton<S>.Create(prodInitialStateId, prodFinalStateIdsList, EnumerateMoves(prodDelta));
            // prod.isEpsilonFree = true;
            // prod.EliminateDeadStates();
            witness = null;
            return false;
        }


        #endregion

        /// <summary>
        /// The sink state will be the state with the largest id.
        /// </summary>
        public Automaton<T> MakeTotal()
        {
            IBooleanAlgebra<T> solver = algebra;
            var aut = this;
            if (!this.isEpsilonFree)
                aut = this.RemoveEpsilons();

            int deadState = aut.maxState + 1;
            var newMoves = new List<Move<T>>();
            foreach (int state in aut.States)
            {
                var conds = new List<T>(aut.EnumerateConditions(state));
                var or_conds = solver.MkOr(conds);
                //var str_or_conds = or_conds.ToString();
                var cond = solver.MkNot(or_conds);
                if (solver.IsSatisfiable(cond))
                    newMoves.Add(Move<T>.Create(state, deadState, cond));
            }
            if (newMoves.Count == 0)
                return this;

            newMoves.Add(Move<T>.Create(deadState, deadState, solver.True));
            newMoves.AddRange(GetMoves());
            var tot = Automaton<T>.Create(aut.Algebra, aut.initialState, aut.finalStateSet, newMoves, false, false, aut.isDeterministic);
            return tot;
        }

        /// <summary>
        /// Make a complement of the automaton.
        /// The automaton must be deterministic, otherwise throws AutomataException.
        /// </summary>
        /// <param name="solver">solver for character constraints</param>
        /// <returns>Complement of this automaton</returns>
        public Automaton<T> MkComplement(IBooleanAlgebra<T> solver)
        {
            if (!isDeterministic)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNondeterministic);

            int deadState = maxState + 1;
            bool deadStateIsUsed = false;
            List<Move<T>> complMoves = new List<Move<T>>(GetMoves());
            foreach (int state in States)
            {
                var cond = solver.MkNot(solver.MkOr(EnumerateConditions(state)));
                if (solver.IsSatisfiable(cond))
                {
                    deadStateIsUsed = true;
                    complMoves.Add(Move<T>.Create(state, deadState, cond));
                }
            }
            if (deadStateIsUsed)
                complMoves.Add(Move<T>.Create(deadState, deadState, solver.True));

            List<int> complFinalStates = new List<int>();
            if (deadStateIsUsed)
                complFinalStates.Add(deadState);
            foreach (int state in States)
                if (!IsFinalState(state))
                    complFinalStates.Add(state);

            var complAut = Automaton<T>.Create(this.algebra, initialState, complFinalStates, complMoves);
            complAut.isDeterministic = true;
            complAut.EliminateDeadStates();
            return complAut;
        }

        private IEnumerable<T> EnumerateConditions(int sourceState)
        {
            foreach (var move in delta[sourceState])
                yield return move.Label;
        }

        /// <summary>
        /// Checks that for all states q, if q has two or more outgoing 
        /// moves then the conditions of the moves are pairwise disjoint, i.e., 
        /// their conjunction is unsatisfiable. 
        /// If the check succeeds, sets IsDeterministic to true.
        /// Throws AutomataException if the FSA is not epsilon-free.
        /// </summary>
        /// <param name="solver">used to make conjunctions and to check satisfiability of resulting conditions</param>
        public void CheckDeterminism(bool resetIsDeterministicToFalse = false)
        {
            IBooleanAlgebraPositive<T> solver = algebra;

            if (!isEpsilonFree)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNotEpsilonfree);

            if (resetIsDeterministicToFalse)
                isDeterministic = false;

            if (isDeterministic) //already known to be deterministic
                return;

            foreach (int state in States)
            {
                List<Move<T>> moves = delta[state];
                int k = moves.Count;
                if (k > 1)
                {
                    T sofar = moves[0].Label;
                    for (int i = 1; i < k; i++)
                    {
                        var sofar_and_i = solver.MkAnd(sofar, moves[i].Label);
                        if (solver.IsSatisfiable(sofar_and_i))
                        {
                            isDeterministic = false;
                            return; //nondeterministic
                        }

                        if (i < k - 1)
                            sofar = solver.MkOr(sofar, moves[i].Label);
                    }
                }
            }
            isDeterministic = true; //deterministic
        }

        /// <summary>
        /// Checks whether the autoamton is ambiguous, and returns the ambiguous set of string in the output
        /// paramter <i>ambiguousLanguage</i>
        /// </summary>
        /// <param name="solver">used to make conjunctions and to check satisfiability of resulting conditions</param>
        public bool IsAmbiguous(out Automaton<T> ambiguousLanguage)
        {
            IBooleanAlgebra<T> solver = algebra;

            ambiguousLanguage = Automaton<T>.MkEmpty(algebra);
            if (!isEpsilonFree)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNotEpsilonfree);

            if (isDeterministic || isUnambiguous)
            { //already known to be deterministic
                isUnambiguous = true;
                return false;
            }

            var product = MkAmbiguitySelfProduct(this, out ambiguousLanguage);
            if (product.StateCount == this.StateCount)
            {
                //ambiguousLanguage = Automaton<S>.Empty;
                isUnambiguous = true;
                return false;
            }
            return true;

        }

        #region timeout check
        static void CheckTimeout(System.Diagnostics.Stopwatch sw, long timeoutLimit)
        {
            if (sw.ElapsedMilliseconds > timeoutLimit)
                throw new TimeoutException();
        }
        static void CheckTimeout(long timeoutLimit)
        {
            if (timeoutLimit > 0)
            {
                if (Utilities.HighTimer.Now > timeoutLimit)
                    throw new TimeoutException();
            }
        }
        #endregion


        /// <summary>
        /// Returns true if the intersection of L(A) and L(B) is nonempty i.e. if L(A*B) is nonempty. 
        /// Produces a symbolic list of elements that represents a path in A*B if the intersection is nonempty.
        /// </summary>
        /// <param name="A">FSA to be intersected</param>
        /// <param name="B">FSA to be intersected</param>
        /// <param name="witness">symbolic list of elements that represents a path from the initial state to a final state in A*B</param>
        /// <param name="timeout">timeout in milliseconds for termination, 0 or negative number means no timeout</param>
        /// <returns>true iff the intersection of L(A) and L(B) is nonempty</returns>
        public static bool CheckProduct(Automaton<T> A, Automaton<T> B, int timeout, out List<T> witness)
        {
            CheckIdentityOfAlgebras(A.algebra, B.algebra);
            IBooleanAlgebraPositive<T> solver = A.algebra;

            A = A.RemoveEpsilons();
            B = B.RemoveEpsilons();

            long timeoutLimit;
            if (timeout > 0)
                timeoutLimit = Utilities.HighTimer.Now + timeout;
            else
                timeoutLimit = 0;

            var witnessTree = new Dictionary<Tuple<int, int>, Tuple<T, Tuple<int, int>>>();
            var initPair = new Tuple<int, int>(A.InitialState, B.InitialState);
            var frontier = new Stack<Tuple<int, int>>();
            frontier.Push(initPair);
            witnessTree[initPair] = null;

            //depth first search for the first final state in the product
            while (frontier.Count > 0)
            {

                var sourcePair = frontier.Pop();

                if (A.IsFinalState(sourcePair.Item1) && B.IsFinalState(sourcePair.Item2))
                {
                    //final state was found, extract the witness from the witness tree
                    List<T> w = new List<T>();
                    var parent = witnessTree[sourcePair];
                    while (parent != null)
                    {
                        w.Add(parent.Item1);
                        parent = witnessTree[parent.Item2];
                    }
                    w.Reverse();
                    witness = w;
                    return true;
                }

                foreach (var t1 in A.GetMovesFrom(sourcePair.Item1))
                    foreach (var t2 in B.GetMovesFrom(sourcePair.Item2))
                    {
                        T cond = solver.MkAnd(t1.Label, t2.Label);
                        if (!solver.IsSatisfiable(cond))
                            continue; //ignore the transition

                        Tuple<int, int> targetPair = new Tuple<int, int>(t1.TargetState, t2.TargetState);
                        if (!witnessTree.ContainsKey(targetPair))
                        {
                            witnessTree.Add(targetPair, new Tuple<T, Tuple<int, int>>(cond, sourcePair));
                            frontier.Push(targetPair);
                        }
                    }

                CheckTimeout(timeoutLimit);
            }
            witness = null;
            return false;
        }



        public IEnumerable<Move<T>> GetWitness()
        {
            if (IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeNonempty);

            var witnessTree = new Dictionary<int, Move<T>>();
            witnessTree[initialState] = null;
            var stack = new Stack<int>();
            stack.Push(initialState);

            int state = initialState;

            while (!finalStateSet.Contains(state))
            {
                foreach (var move in GetMovesFrom(state))
                {
                    if (!witnessTree.ContainsKey(move.TargetState))
                    {
                        stack.Push(move.TargetState);
                        witnessTree[move.TargetState] = move;
                    }
                }
                state = stack.Pop();      
            }

            ConsList<Move<T>> path = null;
            while (witnessTree[state] != null)
            {
                path = new ConsList<Move<T>>(witnessTree[state], path);
                state = witnessTree[state].SourceState;
            }
            if (path == null)
                yield break;
            else
                foreach (var move in path)
                    yield return move;
        }


        public int GetMovesCountTo(int s)
        {
            return deltaInv[s].Count;
        }


        #region Determinization

        public Automaton<T> DeterminizeOld(int timeout = 0)
        {
            IBooleanAlgebra<T> solver = algebra;       

            if (IsDeterministic)
                return this;

            //Automaton<T>[] disjuncts;
            //if (TryDecompose(out disjuncts))
            //{
            //    var disjuncts_det = Array.ConvertAll(disjuncts, d => d.Determinize().Minimize());
            //    var disjuncts_comp = Array.ConvertAll(disjuncts_det, d => d.Complement().Minimize());
            //    var prod = Automaton<T>.MkProductOfDeterministicAutomata(disjuncts_comp); 
            //    var union = prod.Complement();
            //    return union;
            //}

            var full = Automaton<T>.MkFull(algebra);
            var compl = Automaton<T>.MkDifference(full, this, timeout); //make complement
            var totCompl = compl.MakeTotal(); //make total
            //the above algo guarantees that totCompl is deterministic
            //so just switch final states with nonfinal states
            var fstates = new HashSet<int>(totCompl.GetStates());
            fstates.ExceptWith(totCompl.GetFinalStates());
            var det = Automaton<T>.Create(algebra, totCompl.InitialState, fstates, totCompl.GetMoves());
            det.EliminateDeadStates();
            det.isDeterministic = true;
            return det;
        }

        public Automaton<T> Determinize(int timeout = 0)
        {
            IBooleanAlgebra<T> solver = algebra;

            if (IsDeterministic)
                return this;

            Automaton<T>[] disjuncts;
            if (TryDecompose(out disjuncts))
            {
                var disjuncts_det = Array.ConvertAll(disjuncts, d => d.Determinize().Minimize());
                var disjuncts_comp = Array.ConvertAll(disjuncts_det, d => d.Complement().Minimize());
                var prod = Automaton<T>.MkProductOfDeterministicAutomata(disjuncts_comp);
                var union = prod.Complement();
                return union;
            }


            long timeout1 = Microsoft.Automata.Utilities.HighTimer.Frequency * ((long)timeout / (long)1000);
            long timeoutLimit;
            if (timeout > 0)
                timeoutLimit = Utilities.HighTimer.Now + timeout1;
            else
                timeoutLimit = 0;

            var A = this.RemoveEpsilons();

            //the sink state is represented implicitly, there is no need to make B total
            List<int> states = new List<int>(A.States);
            PowerSetStateBuilder dfaStateBuilder = PowerSetStateBuilder.Create(states.ToArray());


            var stack = new Stack<int>();

            var startState = dfaStateBuilder.MakePowerSetState(new int[] { A.InitialState });
            stack.Push(startState);

            var delta = new Dictionary<int, List<Move<T>>>();
            delta[startState] = new List<Move<T>>();
            var finalStateList = new HashSet<int>();

            Func<int, bool> IsDFAFinalState = id =>
            {
                foreach (int state in dfaStateBuilder.GetMembers(id))
                    if (A.IsFinalState(state))
                        return true;
                return false;
            };

            if (IsDFAFinalState(startState))
                finalStateList.Add(startState);

            while (stack.Count > 0)
            {

                var sourceState = stack.Pop();

                var moves = new List<Move<T>>(A.GetMovesFromStates(dfaStateBuilder.GetMembers(sourceState)));
                var conds = Array.ConvertAll(moves.ToArray(), move => { return move.Label; });

                int n = moves.Count;

                foreach (var solution in solver.GenerateMinterms(conds))
                {
                    CheckTimeout(timeoutLimit);
                    var nfaTargetStates = new List<int>();
                    for (int j = 0; j < n; j++)
                        if (solution.Item1[j])
                            nfaTargetStates.Add(moves[j].TargetState);

                    //if all conditions are false then this leads to the sink state -1
                    int targetState;
                    if (nfaTargetStates.Count > 0)
                    {
                        targetState = dfaStateBuilder.MakePowerSetState(nfaTargetStates);

                        T prodCondition = solution.Item2;
                        var prodMove = Move<T>.Create(sourceState, targetState, prodCondition);
                        delta[sourceState].Add(prodMove);
                        if (!delta.ContainsKey(targetState))
                        {
                            delta[targetState] = new List<Move<T>>();
                            if (IsDFAFinalState(targetState))
                                finalStateList.Add(targetState);
                            stack.Push(targetState);
                        }
                    }
                }
            }
            if (finalStateList.Count == 0)
                return Automaton<T>.MkEmpty(solver);

            Automaton<T> det = Automaton<T>.Create(solver, startState, finalStateList, EnumerateMoves(delta));
            det.isEpsilonFree = true;
            det.isDeterministic = true;

            det.EliminateDeadStates();
            return det;
        }

        /// <summary>
        /// Determinize and return the state builder that maps generated states to sets of original states
        /// </summary>
        /// <param name="timeout">if 0 then no timeout is enforced else it is the nr of ms</param>
        /// <param name="statebuilder">maps generated state ids to sets of original state ids</param>
        /// <returns></returns>
        public Automaton<T> Determinize(out PowerSetStateBuilder statebuilder, int timeout = 0)
        {
            IBooleanAlgebra<T> solver = algebra;

            if (IsDeterministic)
            {
                statebuilder = null;
                return this;
            }

            long timeout1 = Microsoft.Automata.Utilities.HighTimer.Frequency * ((long)timeout / (long)1000);
            long timeoutLimit;
            if (timeout > 0)
                timeoutLimit = Utilities.HighTimer.Now + timeout1;
            else
                timeoutLimit = 0;

            var A = this.RemoveEpsilons();

            //the sink state is represented implicitly, there is no need to make B total
            List<int> states = new List<int>(A.States);
            PowerSetStateBuilder dfaStateBuilder = PowerSetStateBuilder.Create(states.ToArray());


            var stack = new Stack<int>();

            var startState = dfaStateBuilder.MakePowerSetState(new int[] { A.InitialState });
            stack.Push(startState);

            var delta = new Dictionary<int, List<Move<T>>>();
            delta[startState] = new List<Move<T>>();
            var finalStateList = new HashSet<int>();

            Func<int, bool> IsDFAFinalState = id =>
            {
                foreach (int state in dfaStateBuilder.GetMembers(id))
                    if (A.IsFinalState(state))
                        return true;
                return false;
            };

            if (IsDFAFinalState(startState))
                finalStateList.Add(startState);

            while (stack.Count > 0)
            {

                var sourceState = stack.Pop();

                var moves = new List<Move<T>>(A.GetMovesFromStates(dfaStateBuilder.GetMembers(sourceState)));
                var conds = Array.ConvertAll(moves.ToArray(), move => { return move.Label; });

                int n = moves.Count;

                foreach (var solution in solver.GenerateMinterms(conds))
                {
                    CheckTimeout(timeoutLimit);
                    var nfaTargetStates = new List<int>();
                    for (int j = 0; j < n; j++)
                        if (solution.Item1[j])
                            nfaTargetStates.Add(moves[j].TargetState);

                    //if all conditions are false then this leads to the sink state -1
                    int targetState;
                    if (nfaTargetStates.Count > 0)
                    {
                        targetState = dfaStateBuilder.MakePowerSetState(nfaTargetStates);

                        T prodCondition = solution.Item2;
                        var prodMove = Move<T>.Create(sourceState, targetState, prodCondition);
                        delta[sourceState].Add(prodMove);
                        if (!delta.ContainsKey(targetState))
                        {
                            delta[targetState] = new List<Move<T>>();
                            if (IsDFAFinalState(targetState))
                                finalStateList.Add(targetState);
                            stack.Push(targetState);
                        }
                    }
                }
            }
            if (finalStateList.Count == 0)
            {
                statebuilder = null;
                return Automaton<T>.MkEmpty(solver);
            }

            Automaton<T> det = Automaton<T>.Create(solver, startState, finalStateList, EnumerateMoves(delta));
            det.isEpsilonFree = true;
            det.isDeterministic = true;

            det.EliminateDeadStates();
            statebuilder = dfaStateBuilder;
            return det;
        }

        private bool TryDecompose(out Automaton<T>[] disjuncts)
        {
            if (!InitialStateIsSource || delta[InitialState].Count < 2 || delta[InitialState].Exists(m => !m.IsEpsilon))
            {
                disjuncts = null;
                return false;
            }

            HashSet<int> previous = new HashSet<int>();
            List<Automaton<T>> automata = new List<Automaton<T>>();

            foreach (var move in delta[InitialState])
            {
                List<Move<T>> current_moves = new List<Move<T>>();
                List<int> finals = new List<int>();
                HashSet<int> current = new HashSet<int>();
                SimpleStack<int> stack = new SimpleStack<int>();
                int q0 = move.TargetState;
                stack.Push(q0);
                current.Add(q0);
                if (IsFinalState(q0))
                    finals.Add(q0);
                while (stack.IsNonempty)
                {
                    int q = stack.Pop();
                    foreach (var q_move in delta[q])
                    {
                        if (previous.Contains(q_move.TargetState))
                        {
                            disjuncts = null;
                            return false;
                        }
                        if (current.Add(q_move.TargetState))
                        {
                            stack.Push(q_move.TargetState);
                            if (IsFinalState(q_move.TargetState))
                                finals.Add(q_move.TargetState);
                        }
                        current_moves.Add(q_move);
                    }
                }
                automata.Add(Automaton<T>.Create(this.algebra, q0, finals, current_moves));
                previous.UnionWith(current);
            }

            disjuncts = automata.ToArray();
            return true;
        }

        #endregion


        #region Minimization
        /// <summary>
        /// Minimization of SFAs using a symbolic generalization of Moore's algorithm.
        /// This algorithm is quadratic in the number of states.
        /// If the SFA is nondeterministic, the minimized SFA will be equivalent.
        /// </summary>
        public Automaton<T> MinimizeMoore(int timeout = 0)
        {
            IBooleanAlgebra<T> solver = algebra;
            //return MinimizeClassical(solver, 0, false);
            if (IsEmpty)
                return Minimize();

            if (this.IsEpsilon)
                return this;

            var fa = this;
            var sw = new System.Diagnostics.Stopwatch();
            long timeoutLimit = timeout;
            if (timeout > 0)
                sw.Start();

            //if (fa.IsDeterministic != true)
            //    throw new AutomataException(AutomataExceptionKind.AutomatonIsNotDeterministic);

            fa = fa.MakeTotal();

            Func<int, int, Tuple<int, int>> MkPair = (x, y) => (x < y ? new Tuple<int, int>(x, y) : new Tuple<int, int>(y, x));

            var distinguishable = new HashSet<Tuple<int, int>>();
            var stack = new Stack<Tuple<int, int>>();

            foreach (var p in fa.GetStates())
                if (!fa.IsFinalState(p))
                    foreach (var q in fa.GetFinalStates())
                    {
                        var pair = MkPair(p, q);
                        distinguishable.Add(pair);
                        stack.Push(pair);
                    }

            while (stack.Count > 0)
            {
                var pair = stack.Pop();
                foreach (var m1 in fa.GetMovesTo(pair.Item1))
                    foreach (var m2 in fa.GetMovesTo(pair.Item2))
                        if (m1.SourceState != m2.SourceState)
                            if (solver.IsSatisfiable(solver.MkAnd(m1.Label, m2.Label)))
                            {
                                var sources = MkPair(m1.SourceState, m2.SourceState);
                                if (distinguishable.Add(sources))
                                    stack.Push(sources);
                            }

                CheckTimeout(sw, timeout);
            }

            var states = new List<int>(fa.GetStates()).ToArray();

            var repr = new Dictionary<int, int>();

            for (int i = 0; i < states.Length; i++)
            {
                var p = states[i];
                if (!repr.ContainsKey(p))
                    repr[p] = p;
                for (int j = i + 1; j < states.Length; j++)
                {
                    var q = states[j];
                    if (!distinguishable.Contains(MkPair(p, q)))
                    {
                        repr[q] = repr[p];
                    }
                }
            }

            var guards = new Dictionary<Tuple<int, int>, T>();

            foreach (var move in fa.GetMoves())
            {
                var p = repr[move.SourceState];
                var q = repr[move.TargetState];
                var pq = new Tuple<int, int>(p, q);
                T guard;
                if (guards.TryGetValue(pq, out guard))
                    guard = solver.MkOr(guard, move.Label);
                else
                    guard = move.Label;
                guards[pq] = guard;
            }

            var moves = new List<Move<T>>();
            foreach (var entry in guards)
                moves.Add(Move<T>.Create(entry.Key.Item1, entry.Key.Item2, entry.Value));

            var finals = new HashSet<int>();
            foreach (var final in fa.GetFinalStates())
                finals.Add(repr[final]);

            var minimized = Automaton<T>.Create(this.Algebra, repr[fa.InitialState], finals, moves, true, true, isDeterministic);
            minimized.isEpsilonFree = true;
            //minimized.isDeterministic = true;
            minimized.isUnambiguous = true;
            return minimized;
        }

        internal class EquivClass
        {
            IBooleanAlgebra<T> solver;
            T elem;

            internal EquivClass(IBooleanAlgebra<T> solver, T elem)
            {
                this.solver = solver;
                this.elem = elem;
            }

            public override bool Equals(object obj)
            {
                EquivClass e = obj as EquivClass;
                if (e == null)
                    return false;
                return solver.AreEquivalent(elem, e.elem);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        /// <summary>
        /// Minimization of FAs using a symbolic generalization of Hopcroft's algorithm.
        /// </summary>
        public Automaton<T> MinimizeHopcroft(int timeout = 0)
        {
            IBooleanAlgebra<T> solver = algebra;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            if (timeout > 0)
                sw.Start();

            if (IsEmpty)
                return Minimize();

            //If it's singleton state accepting only the empty string
            if (MoveCount == 0)
                return this;

            if (this.IsEpsilon)
                return this;

            if (IsDeterministic != true)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNondeterministic);

            //if (typeof(S) != typeof(BvSet))
            //    return MinimizeClassical(solver,0);

            var condsSet = new HashSet<EquivClass>();
            var condsSetOfBvSet = new HashSet<T>();
            var allPreds = new List<T>();

            var fa = this;

            //collect non-equivalent conditions

            if (typeof(T) == typeof(BDD))
                foreach (var move in fa.GetMoves())
                {
                    if (condsSetOfBvSet.Add(move.Label))
                        allPreds.Add(move.Label);
                }
            else
                foreach (var move in fa.GetMoves())
                {
                    if (condsSet.Add(new EquivClass(solver, move.Label)))
                        allPreds.Add(move.Label);
                }

            var refinement = new SymbolicPartitionRefinement<T>(solver, allPreds[0]);
            foreach (var c in allPreds)
                refinement.Refine(c);

            var minterms = new List<T>(refinement.GetRegions());
            CheckTimeout(sw, timeout);
            //var minterms2str = Array.ConvertAll(minterms.ToArray(), mt => ((CharSetSolver)solver).PrettyPrint(mt as BvSet));

            var PR = new PartitionRefinement(GetStates(), maxState);
            var initparts = new List<Part[]>(PR.Refine(GetFinalStates()));

            var W = new Stack<Part>();

            if (initparts.Count > 0)
            {
                W.Push(initparts[0][0]); //nonfinal states
                W.Push(initparts[0][1]); //final states
            }
            else
                W.Push(PR.InitialPart);

            while (W.Count > 0)
            {
                var P = W.Pop();
                var Pelems = new List<int>(P.GetElements());
                foreach (var minterm in minterms)
                {
                    CheckTimeout(sw, timeout);
                    var sources = new HashSet<int>(GetSources(minterm, Pelems));
                    if (sources.Count > 0)
                    {
                        var splits = PR.Refine(sources);
                        foreach (var R in splits)
                        {
                            CheckTimeout(sw, timeout);
                            if (W.Contains(R[0])) //note that R[0] has already been modified and is in W
                                W.Push(R[1]);
                            else if (R[0].Size <= R[1].Size)
                                W.Push(R[0]);
                            else
                                W.Push(R[1]);
                        }
                    }
                }
            }

            Dictionary<Tuple<int, int>, HashSet<T>> condMap = new Dictionary<Tuple<int, int>, HashSet<T>>();
            foreach (var move in GetMoves())
            {
                int s = PR.GetPart(move.SourceState).Representative;
                int t = PR.GetPart(move.TargetState).Representative;
                var st = new Tuple<int, int>(s, t);
                HashSet<T> condSet;
                if (!condMap.TryGetValue(st, out condSet))
                {
                    condSet = new HashSet<T>();
                    condSet.Add(move.Label);
                    condMap[st] = condSet;
                }
                else
                {
                    condSet.Add(move.Label);
                }
            }
            int newInitState = PR.GetPart(fa.InitialState).Representative;
            var newMoves = new List<Move<T>>();
            var newFinals = new HashSet<int>();
            foreach (var entry in condMap)
            {
                newMoves.Add(Move<T>.Create(entry.Key.Item1, entry.Key.Item2, solver.MkOr(entry.Value)));
            }
            foreach (var f in GetFinalStates())
                newFinals.Add(PR.GetPart(f).Representative);

            var res = Create(this.algebra, newInitState, newFinals, newMoves);
            res.isDeterministic = true;
            res.isEpsilonFree = true;
            return res;
        }

        /// <summary>
        /// Minimization of SFAs.
        /// Can also be applied to nondeterministic SFAs.
        /// </summary>
        public Automaton<T> NonDetGetMinAut(int which)
        {
            if (which==1)
                return MinSFA(this);
            if (which == 2)
                return MinSFANew(this);
            if (which==3)
                return MinSFACount(this);
            throw new AutomataException("which is different than 1,2,3 ");
        }

        /// <summary>
        /// Minimization of SFAs.
        /// Can also be applied to nondeterministic SFAs.
        /// </summary>
        public Automaton<T> Minimize()
        {
            if (IsEmpty)
            {
                if (StateCount > 1 || MoveCount > 0)
                    return MkEmpty(algebra);
                else
                    return this;
            }

            if (this.IsEpsilon)
                return this;

            Automaton<T> fa = this.RemoveEpsilons();

            if (fa.isDeterministic)
            {
                return MinSFA(fa);
            }
            else
            {
                return MinSFANew(fa);
                //var fa_m = MinSFA(fa);
                //if (fa_m.StateCount < fa.StateCount)
                //{
                //    var fa_m_r = fa_m.Reverse().RemoveEpsilons();
                //    var fa_m_r_m = MinSFANew(fa_m_r);
                //    var fa_m_r_m_r = fa_m_r_m.Reverse().RemoveEpsilons();
                //    if (fa_m.StateCount <= fa_m_r_m_r.StateCount)
                //        return fa_m;
                //    else
                //        return fa_m_r_m_r;
                //}
                //else
                //{
                //    return fa;
                //}
            }
        }

        public static int totalExploredBlocks = 0;

        /// <summary>
        /// Algorithm MinSFA from POPL14.
        /// </summary>
        static Automaton<T> MinSFANew(Automaton<T> autom)
        {
            totalExploredBlocks = 0;
            var solver = autom.algebra;
            var fa = autom.MakeTotal();

            var finalBlock = new Block(fa.GetFinalStates());
            var nonfinalBlock = new Block(fa.GetNonFinalStates());
            var Blocks = new Dictionary<int, Block>();
            foreach (var q in fa.GetFinalStates()) Blocks[q] = finalBlock;
            foreach (var q in fa.GetNonFinalStates()) Blocks[q] = nonfinalBlock;

            //Stores what block this was the split of
            var ComplementBlock = new Dictionary<Block, Block>();
            var BlockPre = new Dictionary<Block, Dictionary<int, T>>();     //BlockPre[B][q]= all symbols that go from q to B                       

            int totalPre = 0;

            //Computes and memoizes BlockPre
            Func<Block, Dictionary<int, T>> GetBlockPre = (B) =>
            {                
                if (BlockPre.ContainsKey(B))
                    return BlockPre[B];
                else
                {                    
                    var dicB = new Dictionary<int, T>();
                    foreach (var q in B)
                    {
                        totalPre++;
                        foreach (var move in fa.deltaInv[q]) //moves leading to q
                            if (Blocks[move.SourceState].Count > 1) //singleton blocks cannot be further split
                                if (dicB.ContainsKey(move.SourceState))
                                    dicB[move.SourceState] = solver.MkOr(dicB[move.SourceState], move.Label);
                                else
                                    dicB[move.SourceState] = move.Label;
                    }
                    BlockPre[B] = dicB;
                    return dicB;
                }
            };

            var W = new BlockStack();
            if (nonfinalBlock.Count < finalBlock.Count)
            {
                W.Push(nonfinalBlock);
                ComplementBlock[nonfinalBlock] = finalBlock;
            }
            else
            {
                W.Push(finalBlock);
                ComplementBlock[finalBlock] = nonfinalBlock;
            }

            Func<T, T, T> MkDiff = (x, y) => solver.MkAnd(x, solver.MkNot(y));
            #region UpdateBlocks
            Func<Block, Block, Block, bool> UpdateBlocks = (P, P1, P2) =>
            {
                // Something was split
                foreach (var st in P1)
                    Blocks[st] = P1;
                foreach (var st in P2)
                    Blocks[st] = P2;

                if (W.Contains(P))
                {
                    W.Remove(P);
                    if (P1.Count > 0)
                    {
                        W.Push(P1);
                        ComplementBlock[P1] = ComplementBlock[P];
                    }

                    if (P2.Count > 0)
                    {
                        W.Push(P2);
                        ComplementBlock[P2] = ComplementBlock[P];
                    }
                }
                else
                {
                    // If both non-empty keep the smallest
                    if (P2.Count <= P1.Count)
                    {
                        W.Push(P2);
                        ComplementBlock[P2] = P1;
                    }
                    else
                    {
                        W.Push(P1);
                        ComplementBlock[P1] = P2;
                    }
                }
                return true;
            };
            #endregion

                     
            while (!W.IsEmpty)
            {
                totalExploredBlocks++;
             
                var B = W.Pop();

                var Gamma = GetBlockPre(B);                

                #region apply initial splitting without using guards
                var relevant = new HashSet<Block>();
                foreach (var q in Gamma.Keys)
                    if (Blocks[q].Count > 1)
                        relevant.Add(Blocks[q]);

                var KeySet = new HashSet<int>(Gamma.Keys);
                foreach (var P in relevant)
                {
                    var P1 = new Block();
                    var P2 = new Block();

                    foreach (var p in P)
                        if (KeySet.Contains(p))
                            P1.Add(p);
                        else                        
                            P2.Add(p);                     

                    //If it was there put both halves otherwise only one half
                    if (P1.Count > 0 && P2.Count > 0)
                        UpdateBlocks(P, P1, P2);
                                        
                }
                #endregion


                //in each relevant block all states lead to B due to the initial splitting
                var relevant2 = new HashSet<Block>();
                foreach (var q in Gamma.Keys)
                    if (Blocks[q].Count > 1)
                        relevant2.Add(Blocks[q]); //collect the relevant blocks

                var relevantList = new List<Block>(relevant2);
                var gammaHatList = new List<Block>();
                                               
                Dictionary<int, T> GammaHat = null;                

                //only relevant blocks are potentially split               
                while (relevantList.Count > 0)
                {                    
                    //this loop splits using gamma, if a block that is processed is not split, we feed it to gamma hat
                    while (relevantList.Count > 0)
                    {
                        var P = relevantList[0];
                        relevantList.RemoveAt(0);

                        var PE = P.GetEnumerator();
                        PE.MoveNext();

                        var P1 = new Block();
                        var P2 = new Block();

                        int p = PE.Current;
                        P1.Add(p);

                        bool splitFound = false;                        
                        var psi = Gamma[p];

                        var witness = solver.False;                        

                        #region compute P1 and P2 as subblocks
                        while (PE.MoveNext())
                        {
                            var q = PE.Current;
                            var phi = Gamma[q];

                            //Have a witness for splitting
                            if (splitFound)
                            {
                                var inters = solver.MkAnd(witness, phi);
                                if (solver.IsSatisfiable(inters))
                                    P1.Add(q);
                                else
                                    P2.Add(q);
                            }
                            else
                            {
                                // Look for a splitter
                                witness = MkDiff(psi, phi);
                                if (solver.IsSatisfiable(witness))
                                {
                                    //there is some a: p --a--> B and q --a--> compl(B) 
                                    splitFound = true;

                                    P2.Add(q);
                                }
                                else // [[psi]] is subset of [[phi]]
                                {
                                    witness = MkDiff(phi, psi);
                                    if (solver.IsSatisfiable(witness))
                                    {
                                        //there is some a: q --a--> B and p --a--> compl(B) for all p in C1
                                        var tmp = P1;
                                        P1 = P2;
                                        P2 = tmp;

                                        P1.Add(q);

                                        splitFound = true;
                                    }
                                    else
                                        P1.Add(q); //psi and phi are equivalent
                                }
                            }
                        }
                        #endregion

                        #region split P
                        //If nothing changed, copy the pre-function
                        if (!splitFound)
                        {
                            gammaHatList.Add(P);
                        }
                        else
                        {
                            //New created blocks have to be investigated
                            // Investigate new blocks
                            if (P1.Count > 1)
                                relevantList.Add(P1);                                
                            if (P2.Count > 1)
                                relevantList.Add(P2);
                            
                            UpdateBlocks(P, P1, P2);
                        }
                        
                        #endregion
                    }

                    
                    #region GammaHatSplit
                    //do splitting with respect to GammaHat                               
                    while (gammaHatList.Count > 0)
                    {
                        totalExploredBlocks++;
                        if (GammaHat == null)
                            GammaHat = GetBlockPre(ComplementBlock[B]);

                        var P = gammaHatList[0];
                        gammaHatList.RemoveAt(0);

                        var PE = P.GetEnumerator();
                        PE.MoveNext();

                        var P1 = new Block();
                        var P2 = new Block();

                        int p = PE.Current;
                        P1.Add(p);

                        bool splitFound = false;

                        var psi = Gamma[p];
                        var psihat = solver.False;
                        if (GammaHat.ContainsKey(p))
                            psihat = GammaHat[p];

                        var psi_and_psihat = solver.MkAnd(psi, psihat);
                        var witness = solver.False;

                        #region compute P1 and P2 as subblocks
                        while (PE.MoveNext())
                        {
                            var q = PE.Current;
                            var phi = Gamma[q];
                            var phihat = solver.False;
                            if (GammaHat.ContainsKey(q))
                                phihat = GammaHat[q];

                            var phi_and_phihat = solver.MkAnd(phi, phihat);

                            //Have a witness for splitting
                            if (splitFound)
                            {
                                var inters = solver.MkAnd(witness, phi_and_phihat);
                                if (solver.IsSatisfiable(inters))
                                    P1.Add(q);                               
                                else
                                    P2.Add(q);
                            }
                            else
                            {
                                witness = MkDiff(psi_and_psihat, phi_and_phihat);
                                if (solver.IsSatisfiable(witness))
                                {
                                    //there is some a: p --a--> B p--a--> compl(b) and q--a--> B  but not q--a--> compl(B)
                                    splitFound = true;

                                    P2.Add(q);
                                }
                                else
                                {
                                    witness = MkDiff(phi_and_phihat, psi_and_psihat);
                                    if (solver.IsSatisfiable(witness))
                                    {
                                        //there is some a: q --a--> B q--a--> compl(b) and p--a--> B  but not p--a--> compl(B)
                                        var tmp = P1;
                                        P1 = P2;
                                        P2 = tmp;

                                        P1.Add(q);

                                        splitFound = true;
                                    }
                                    else
                                        P1.Add(q); //p and q go to gammahat with same symbols
                                }

                            }
                        }
                        #endregion

                        #region split P
                        if (splitFound)
                        {
                            // Investigate new blocks
                            if (P1.Count > 1)
                                relevantList.Add(P1);
                            if (P2.Count > 1)
                                relevantList.Add(P2);

                            UpdateBlocks(P, P1, P2);
                        }
                        #endregion
                    } 
                    #endregion                    
                }
            }

            //Console.WriteLine("NEW Explored blocks: " + totalExploredBlocks);
            //Console.WriteLine("NEW PRE: " + totalPre);

            Func<int, int> GetRepresentative = (q => Blocks[q].Elem());
            return autom.JoinStates(GetRepresentative, solver.MkOr);
        }

        
        /// <summary>
        /// NFA minimization algorithm based on counting
        /// </summary>
        static Automaton<T> MinSFACount(Automaton<T> autom)
        {
            totalExploredBlocks = 0;
            var solver = autom.algebra;
            var fa = autom.MakeTotal();

            // Remembers what block was split to create the new one. 
            // If P=P1 U P2, ParentBlock(Pi)=P
            var ComplementBlock = new Dictionary<Block, Block>();
            var BlockPre = new Dictionary<Block, Dictionary<int, IteBag<T>>>();     //BlockPre[B][q]= all symbols that go from q to B                       
            var iteBuilder = new IteBagBuilder<T>(solver);

            //This block is only used as the parent of the initial block
            var statesBlock = new Block(fa.States);
            var finalBlock = new Block(fa.GetFinalStates());
            var nonfinalBlock = new Block(fa.GetNonFinalStates());
            var Blocks = new Dictionary<int, Block>();
            foreach (var q in fa.GetFinalStates())
                Blocks[q] = finalBlock;
            foreach (var q in fa.GetNonFinalStates())
                Blocks[q] = nonfinalBlock;

            ComplementBlock[finalBlock] = nonfinalBlock;
            ComplementBlock[nonfinalBlock] = finalBlock;


            Dictionary<Block, Tuple<Block, Block>> MinusStructure = new Dictionary<Block, Tuple<Block, Block>>();

            //Computes and memoizes BlockPre
            Func<Block, Dictionary<int, IteBag<T>>> GetBlockPre = (B) =>
                AuxGetBlockPre(B, BlockPre, MinusStructure, fa.deltaInv, iteBuilder, solver);


            //Computes the state-wise subtraction of blocks
            Func<Block, Block, Block, bool> BlockSplit = (b, b1, b2) =>
            {
                var ratio = ((double)b1.Count) / ((double)b2.Count);
                //var bDic = BlockPre[b];
                if (b1.Count < b2.Count)
                {
                    MinusStructure[b2] = new Tuple<Block, Block>(b, b1);
                }
                else
                {
                    MinusStructure[b1] = new Tuple<Block, Block>(b, b2);
                }

                return true;
            };

            var W = new BlockStack();
            if (nonfinalBlock.Count < finalBlock.Count)
                W.Push(nonfinalBlock);
            else
                W.Push(finalBlock);


            #region UpdateBlocks
            Func<Block, Block, Block, bool> UpdateBlocks = (P, P1, P2) =>
            {
                // Something was split
                foreach (var st in P1)
                    Blocks[st] = P1;
                foreach (var st in P2)
                    Blocks[st] = P2;

                if (W.Contains(P))
                {
                    W.Remove(P);
                    if (P1.Count > 0)
                    {
                        W.Push(P1);
                        ComplementBlock[P1] = ComplementBlock[P];
                    }

                    if (P2.Count > 0)
                    {
                        W.Push(P2);
                        ComplementBlock[P2] = ComplementBlock[P];
                    }
                }
                else
                {
                    // If both non-empty keep the smallest
                    if (P2.Count <= P1.Count)
                    {
                        W.Push(P2);
                        ComplementBlock[P2] = P1;
                    }
                    else
                    {
                        W.Push(P1);
                        ComplementBlock[P1] = P2;
                    }
                }
                return true;
            };
            #endregion


            Func<T, T, T> MkDiff = (x, y) => solver.MkAnd(x, solver.MkNot(y));
            Func<IteBag<T>, bool> IsSat = (b) => b.IntersectsWith(iteBuilder.MkSingleton(solver.True));
            

            while (!W.IsEmpty)
            {
                totalExploredBlocks++;

                var B = W.Pop();

                //Gamma is a bag
                var Gamma = GetBlockPre(B);


                var relevant = new HashSet<Block>();
                foreach (var q in Gamma.Keys)
                    if (Blocks[q].Count > 1)
                        relevant.Add(Blocks[q]);

                if (relevant.Count > 0)
                {
                    #region apply initial splitting without using guards
                    foreach (var P in relevant)
                    {
                        var P1 = new Block();
                        var P2 = new Block();

                        foreach (var p in P)
                            if (Gamma.ContainsKey(p))
                                P1.Add(p);
                            else
                                P2.Add(p);

                        if (P1.Count > 0 && P2.Count > 0)
                        {
                            // Something was split
                            //Compute the updated pre efficiently doing the difference and update W
                            BlockSplit(P, P1, P2);
                            UpdateBlocks(P, P1, P2);
                        }
                    }
                    #endregion

                    //in each relevant block all states lead to B due to the initial splitting
                    var relevant2 = new HashSet<Block>();
                    foreach (var q in Gamma.Keys)
                        if (Blocks[q].Count > 1)
                            relevant2.Add(Blocks[q]); //collect the relevant blocks

                    var relevantList = new List<Block>(relevant2);
                    var gammaHatList = new List<Block>();

                    Dictionary<int, IteBag<T>> GammaHat = null;


                    //only relevant blocks are potentially split               
                    while (relevantList.Count > 0)
                    {
                        //only relevant blocks are potentially split               
                        while (relevantList.Count > 0)
                        {
                            var P = relevantList[0];
                            relevantList.RemoveAt(0);

                            var PE = P.GetEnumerator();
                            PE.MoveNext();

                            var P1 = new Block();
                            var P2 = new Block();

                            int p = PE.Current;
                            P1.Add(p);

                            bool splitFound = false;
                            var psi = Gamma[p];

                            IteBag<T> witness = null;

                            #region compute P1 and P2 as subblocks
                            while (PE.MoveNext())
                            {
                                var q = PE.Current;
                                var phi = Gamma[q];

                                //Have a witness for splitting
                                if (splitFound)
                                {

                                    if (witness.IntersectsWith(phi))
                                        P1.Add(q);
                                    else
                                        P2.Add(q);

                                }
                                else
                                {
                                    // Look for a splitter   
                                    witness = psi.SetMinus(phi);
                                    if (IsSat(witness))
                                    {
                                        //there is some a: p --a--> B and q --a--> compl(B) 
                                        splitFound = true;
                                        P2.Add(q);
                                    }
                                    else // [[psi]] is subset of [[phi]]
                                    {
                                        witness = phi.SetMinus(psi);
                                        if (IsSat(witness))
                                        {
                                            //there is some a: q --a--> B and p --a--> compl(B) for all p in C1
                                            var tmp = P1;
                                            P1 = P2;
                                            P2 = tmp;

                                            P1.Add(q);

                                            splitFound = true;
                                        }
                                        else
                                            P1.Add(q); //psi and phi are equivalent
                                    }
                                }
                            }
                            #endregion

                            #region split P
                            //If nothing changed, copy the pre-function
                            if (!splitFound)
                            {
                                gammaHatList.Add(P);
                            }
                            else
                            {
                                if (P1.Count > 1)
                                    relevantList.Add(P1);
                                if (P2.Count > 1)
                                    relevantList.Add(P2);

                                // Something was split
                                //Compute the updated pre efficiently doing the difference and update W
                                BlockSplit(P, P1, P2);
                                UpdateBlocks(P, P1, P2);
                            }

                            #endregion
                        }
                        #region GammaHatSplit
                        //do splitting with respect to GammaHat                               
                        while (gammaHatList.Count > 0)
                        {
                            if (GammaHat == null)
                                GammaHat = GetBlockPre(ComplementBlock[B]);

                            var P = gammaHatList[0];
                            gammaHatList.RemoveAt(0);

                            var PE = P.GetEnumerator();
                            PE.MoveNext();

                            var P1 = new Block();
                            var P2 = new Block();

                            int p = PE.Current;
                            P1.Add(p);

                            bool splitFound = false;

                            var psi = Gamma[p];
                            IteBag<T> psihat = null;
                            IteBag<T> psi_and_psihat = null;
                            if (GammaHat.ContainsKey(p))
                            {
                                psihat = GammaHat[p];
                                psi_and_psihat = psi.Min(psihat);
                            }

                            IteBag<T> witness = null;

                            #region compute P1 and P2 as subblocks
                            while (PE.MoveNext())
                            {
                                var q = PE.Current;
                                var phi = Gamma[q];
                                IteBag<T> phihat = null;
                                IteBag<T> phi_and_phihat = null;
                                if (GammaHat.ContainsKey(q))
                                {
                                    phihat = GammaHat[q];
                                    phi_and_phihat = phi.Min(phihat);
                                }

                                //Have a witness for splitting
                                if (splitFound)
                                {
                                    if (phi_and_phihat != null && witness.IntersectsWith(phi_and_phihat))
                                        P1.Add(q);
                                    else
                                        P2.Add(q);
                                }
                                else
                                {
                                    if (psi_and_psihat != null)
                                    {
                                        if (phi_and_phihat == null)
                                            witness = psi_and_psihat;
                                        else
                                            witness = psi_and_psihat.SetMinus(phi_and_phihat);
                                    }

                                    if (witness != null && IsSat(witness))
                                    {
                                        //there is some a: p --a--> B p--a--> compl(b) and q--a--> B  but not q--a--> compl(B)
                                        splitFound = true;
                                        P2.Add(q);
                                    }
                                    else
                                    {
                                        if (phi_and_phihat != null)
                                        {
                                            if (psi_and_psihat == null)
                                                witness = phi_and_phihat;
                                            else
                                                witness = phi_and_phihat.SetMinus(psi_and_psihat);
                                        }
                                        if (witness != null && IsSat(witness))
                                        {
                                            //there is some a: q --a--> B q--a--> compl(b) and p--a--> B  but not p--a--> compl(B)
                                            var tmp = P1;
                                            P1 = P2;
                                            P2 = tmp;

                                            P1.Add(q);

                                            splitFound = true;
                                        }
                                        else
                                        {
                                            //Both null means they don't go anywhere else on gammahat
                                            P1.Add(q); //p and q go to gammahat with same symbols
                                        }
                                    }

                                }
                            }
                            #endregion

                            #region split P
                            if (splitFound)
                            {
                                if (P1.Count > 1)
                                    relevantList.Add(P1);
                                if (P2.Count > 1)
                                    relevantList.Add(P2);

                                // Something was split
                                //Compute the updated pre efficiently doing the difference and update W
                                BlockSplit(P, P1, P2);
                                UpdateBlocks(P, P1, P2);
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
            }
            //Console.WriteLine();
            //Console.WriteLine("Calls IntersEmp: " + iteBuilder.GetCallCount(BagOpertion.ISNONEMPTYINTERSECTION));
            //Console.WriteLine("Time IntersEmp: " + iteBuilder.GetElapsedMilliseconds(BagOpertion.ISNONEMPTYINTERSECTION));

            //Console.WriteLine("Calls Minus: " + iteBuilder.GetCallCount(BagOpertion.MINUS));
            //Console.WriteLine("Time Minus: " + iteBuilder.GetElapsedMilliseconds(BagOpertion.MINUS));

            //Console.WriteLine("Calls SETMINUS: " + iteBuilder.GetCallCount(BagOpertion.SETMINUS));
            //Console.WriteLine("Time SETMINUS: " + iteBuilder.GetElapsedMilliseconds(BagOpertion.SETMINUS));

            //Console.WriteLine("Calls Min: " + iteBuilder.GetCallCount(BagOpertion.MIN));
            //Console.WriteLine("Time Min: " + iteBuilder.GetElapsedMilliseconds(BagOpertion.MIN));

            //Console.WriteLine("Calls plus: " + iteBuilder.GetCallCount(BagOpertion.PLUS));
            //Console.WriteLine("Time plus: " + iteBuilder.GetElapsedMilliseconds(BagOpertion.PLUS));

            //Console.WriteLine("N log N Explored blocks: "+totalExploredBlocks);
            //Console.WriteLine("N log N PRES: " + totalPreCount);
            Func<int, int> GetRepresentative = (q => Blocks[q].Elem());
            return autom.JoinStates(GetRepresentative, solver.MkOr);
        }

        //Computes the state-wise subtraction of blocks
        private static Dictionary<int, IteBag<T>> DicMinus(Dictionary<int, IteBag<T>> dic1, Dictionary<int,
            IteBag<T>> dic2, IteBagBuilder<T> itebuilder,
            IBooleanAlgebra<T> solver)
        {
            var dicNew = new Dictionary<int, IteBag<T>>();
            foreach (var q in dic1.Keys)
            {
                IteBag<T> qBag = dic1[q];
                if (dic2.ContainsKey(q))
                    qBag = qBag.Minus(dic2[q]);
                if (qBag.IntersectsWith(itebuilder.MkSingleton(solver.True)))
                    dicNew[q] = qBag;
            }
            return dicNew;
        }


        //Computes and memoizes BlockPre
        private static Dictionary<int, IteBag<T>> AuxGetBlockPre(
            Block B,
            Dictionary<Block, Dictionary<int, IteBag<T>>> BlockPre,
            Dictionary<Block, Tuple<Block, Block>> MinusStructure,
            Dictionary<int, List<Move<T>>> deltaInv,
            IteBagBuilder<T> iteBuilder,
            IBooleanAlgebra<T> solver)
        {
            if (BlockPre.ContainsKey(B))
                return BlockPre[B];
            else
            {
                if (B.Count > 1 && MinusStructure.ContainsKey(B) && BlockPre.ContainsKey(MinusStructure[B].Item1))
                {
                    var tup = MinusStructure[B];
                    var big = tup.Item1;
                    var small = tup.Item2;
                    return DicMinus(
                        AuxGetBlockPre(big, BlockPre, MinusStructure, deltaInv, iteBuilder, solver),
                        AuxGetBlockPre(small, BlockPre, MinusStructure, deltaInv, iteBuilder, solver),
                        iteBuilder, solver);
                }
                else
                {
                    var dicB = new Dictionary<int, IteBag<T>>();

                    foreach (var q in B)
                    {
                        foreach (var move in deltaInv[q]) //moves leading to q
                        {
                            var moveBag = iteBuilder.MkSingleton(move.Label);
                            if (dicB.ContainsKey(move.SourceState))
                            {
                                var oldBag = dicB[move.SourceState];
                                dicB[move.SourceState] = oldBag.Plus(moveBag);
                            }
                            else
                                dicB[move.SourceState] = moveBag;
                        }
                    }
                    BlockPre[B] = dicB;
                    return dicB;
                }
            }
        }


        /// <summary>
        /// Algorithm MinSFA from POPL14.
        /// </summary>
        static Automaton<T> MinSFA(Automaton<T> autom)
        {
            totalExploredBlocks = 0;
            var solver = autom.algebra;
            var fa = autom.MakeTotal();

            var finalBlock = new Block(fa.GetFinalStates());
            var nonfinalBlock = new Block(fa.GetNonFinalStates());
            var Blocks = new Dictionary<int, Block>();
            foreach (var q in fa.GetFinalStates()) Blocks[q] = finalBlock;
            foreach (var q in fa.GetNonFinalStates()) Blocks[q] = nonfinalBlock;

            var W = new BlockStack();
            if (!fa.isDeterministic)
            {
                W.Push(nonfinalBlock);
                W.Push(finalBlock);
            }
            else if (nonfinalBlock.Count < finalBlock.Count)
                W.Push(nonfinalBlock);
            else
                W.Push(finalBlock);

            Func<T, T, T> MkDiff = (x, y) => solver.MkAnd(x, solver.MkNot(y));            
            int totalPre = 0;
            while (!W.IsEmpty)
            {
                totalExploredBlocks++;

                var B = W.Pop();

                var Bcopy = new Block(B);                //make a copy of B for iterating over its elemenents
                var Gamma = new Dictionary<int, T>();     //joined conditions leading to B from states leading to B
                foreach (var q in Bcopy)
                {
                    totalPre++;
                    foreach (var move in fa.deltaInv[q]) //moves leading to q
                        if (Blocks[move.SourceState].Count > 1) //singleton blocks cannot be further split
                            if (Gamma.ContainsKey(move.SourceState))
                                Gamma[move.SourceState] = solver.MkOr(Gamma[move.SourceState], move.Label);
                            else
                                Gamma[move.SourceState] = move.Label;
                }

                #region apply initial splitting without using guards
                var relevant = new HashSet<Block>();
                foreach (var q in Gamma.Keys)
                    if (Blocks[q].Count > 1)
                        relevant.Add(Blocks[q]);

                foreach (var P in relevant)
                {
                    var P1 = new Block(Gamma.Keys, P.Contains); //all q in Gamma.Keys such that P.Contains(q)
                    if (P1.Count < P.Count)
                    {
                        foreach (var p in P1)
                        {
                            P.Remove(p);
                            Blocks[p] = P1;
                        }
                        if (W.Contains(P))
                            W.Push(P1);
                        else if (!fa.isDeterministic)
                        {
                            //both blocks are needed, bisumlation based minimization
                            W.Push(P);
                            W.Push(P1);
                        }
                        else if (P.Count <= P1.Count)
                            W.Push(P);
                        else
                            W.Push(P1);
                    }
                }
                #endregion


                //in each relevant block all states lead to B due to the initial splitting
                var relevant2 = new HashSet<Block>();
                foreach (var q in Gamma.Keys)
                    if (Blocks[q].Count > 1)
                        relevant2.Add(Blocks[q]); //collect the relevant blocks
                var relevantList = new List<Block>(relevant2);

                //only relevant blocks are potentially split               
                while (relevantList.Count>0)
                {
                    var P = relevantList[0];
                    relevantList.RemoveAt(0);

                    var PE = P.GetEnumerator();
                    PE.MoveNext();

                    var P1 = new Block();
                    bool splitFound = false;

                    var psi = Gamma[PE.Current];
                    P1.Add(PE.Current); //note that PE has at least 2 elements

                    #region compute P1 as the new sub-block of P
                    while (PE.MoveNext())
                    {
                        var q = PE.Current;
                        var phi = Gamma[q];
                        if (splitFound)
                        {
                            var psi_and_phi = solver.MkAnd(psi, phi);
                            if (solver.IsSatisfiable(psi_and_phi))
                                P1.Add(q);
                        }
                        else
                        {
                            var psi_min_phi = MkDiff(psi, phi);
                            if (solver.IsSatisfiable(psi_min_phi))
                            {
                                psi = psi_min_phi;
                                splitFound = true;
                            }
                            else // [[psi]] is subset of [[phi]]
                            {
                                var phi_min_psi = MkDiff(phi, psi);
                                if (!solver.IsSatisfiable(phi_min_psi))
                                    P1.Add(q); //psi and phi are equivalent
                                else
                                {
                                    //there is some a: q --a--> B and p --a--> compl(B) for all p in C1
                                    P1.Clear();
                                    P1.Add(q);
                                    psi = phi_min_psi;
                                    splitFound = true;
                                }
                            }
                        }
                    }
                    #endregion

                    #region split P
                    if (P1.Count < P.Count)
                    {
                        foreach (var p in P1)
                        {
                            P.Remove(p);
                            Blocks[p] = P1;
                        }

                        if (W.Contains(P))
                            W.Push(P1);
                        else if (!fa.isDeterministic)
                        {
                            //both blocks are needed, bisumlation based minimization
                            W.Push(P);
                            W.Push(P1);
                        }
                        else if (P.Count <= P1.Count)
                            W.Push(P);
                        else
                            W.Push(P1);

                        // Something was split
                        if (P.Count > 1)
                            relevantList.Add(P);
                        if (P1.Count > 1)
                            relevantList.Add(P1);
                    }
                    #endregion
                }

            }

            //Console.WriteLine("OLD Explored blocks: " + totalExploredBlocks);
            //Console.WriteLine("OLD PRES: " + totalPre);

            Func<int, int> GetRepresentative = (q => Blocks[q].Elem());
            return autom.JoinStates(GetRepresentative, solver.MkOr);
        }

        Automaton<T> JoinStates(Func<int, int> GetRepresentative, Func<IEnumerable<T>, T> MkDisjunction)
        {
            var autom = this;
            var condMap = new Dictionary<Tuple<int, int>, HashSet<T>>();
            foreach (var move in autom.GetMoves())
            {
                int s = GetRepresentative(move.SourceState);
                int t = GetRepresentative(move.TargetState);
                var st = new Tuple<int, int>(s, t);
                HashSet<T> condSet;
                if (!condMap.TryGetValue(st, out condSet))
                {
                    condSet = new HashSet<T>();
                    condSet.Add(move.Label);
                    condMap[st] = condSet;
                }
                else
                    condSet.Add(move.Label);
            }
            int newInitState = GetRepresentative(autom.InitialState);
            var newMoves = new List<Move<T>>();
            var newFinals = new HashSet<int>();
            foreach (var entry in condMap)
                newMoves.Add(Move<T>.Create(entry.Key.Item1, entry.Key.Item2, MkDisjunction(entry.Value)));
            foreach (var f in autom.GetFinalStates())
                newFinals.Add(GetRepresentative(f));

            var res = Create(this.Algebra, newInitState, newFinals, newMoves, false, false, autom.isDeterministic);
            return res;
        }


        /// <summary>
        /// Algorithm for minimizing nondeterministic SFAs based on bisimilarity of states.
        /// </summary>
        static Automaton<T> MinBiSim(Automaton<T> automIn)
        {
            var solver = automIn.algebra;

            var autom = automIn.MakeTotal();
            var msb = new MultiSetBuilder(solver);

            var root = new SimBlockContainer();
            root.InitializeIncomingForRoot(autom, solver, msb);

            var F_Block = new SimBlock(msb, autom.GetIncoming, root, autom.GetFinalStates());
            var NF_Block = new SimBlock(msb, autom.GetIncoming, root, autom.GetNonFinalStates());
            root.Add(F_Block, NF_Block);

            List<SimBlock> Partition = new List<SimBlock>();
            Partition.Add(F_Block);
            Partition.Add(NF_Block);

            var W = new SimpleStack<SimBlockContainer>();
            var NextW = new SimpleStack<SimBlockContainer>();
            var NextWelems = new HashSet<SimBlockContainer>();
            NextW.Push(root);
            var newContainers = new HashSet<SimBlockContainer>();

            while (NextW.IsNonempty)
            {
                var tmp1 = W;
                W = NextW;
                NextW = tmp1;
                NextWelems.Clear();

                newContainers.Clear();

                //W has compound containers whose blocks have not yet been considered as splitters
                //each container has a multiset map corresponding to its original block
                //that original block has already been considered as a splitter 
                //all blocks in the containers in W are also members of Partition
                while (!W.IsEmpty)
                {
                    SimBlockContainer S = W.Pop();
                    SimBlock B = S.RemoveBlock();
                    if (S.IsCompound)
                        W.Push(S);

                    var NextPartition = new List<SimBlock>();

                    foreach (var D in Partition)
                    {
                        SimBlockContainer DC;
                        if (newContainers.Contains(D.container))
                            DC = D.container; //D.container was created during current iteration over W
                        else
                        {
                            //D.container was created during previous iteration over W
                            DC = new SimBlockContainer();
                            newContainers.Add(DC);
                            DC.Incoming = D.Incoming;
                        }

                        #region partition D wrt B with DC as the container
                        var Ds = new Dictionary<T, SimBlock>();
                        SimBlock Drest = null;
                        foreach (int q in D)
                        {
                            MultiSet q2B;
                            if (B.Incoming.TryGetValue(q, out q2B))
                            {
                                var q2B_set = q2B.ToSet();
                                SimBlock block;
                                if (!Ds.TryGetValue(q2B_set, out block))
                                {
                                    block = new SimBlock(msb, autom.GetIncoming, DC);
                                    Ds[q2B_set] = block;
                                    DC.Add(block);
                                }
                                block.Add(q);
                            }
                            else //states having no transitions to B
                            {
                                if (Drest == null)
                                    Drest = new SimBlock(msb, autom.GetIncoming, DC);
                                Drest.Add(q);
                            }
                        }
                        #endregion

                        #region continue to partition D wrt S\B
                        LinkedListNode<SimBlock> D1node = DC.blocks.First;
                        while (D1node != null)
                        {
                            SimBlock D1 = D1node.Value;
                            if (!D1.IsSingleton)
                            {
                                //see if D1 can be split further wrt S\B
                                var D1s = new Dictionary<T, SimBlock>();
                                foreach (int q in D1)
                                {
                                    MultiSet q2B = B.Incoming[q];
                                    MultiSet q2S = B.container.Incoming[q];
                                    MultiSet diff = q2S.Minus(q2B);
                                    T diff_set = diff.ToSet();
                                    SimBlock block;
                                    if (!D1s.TryGetValue(diff_set, out block))
                                    {
                                        block = new SimBlock(msb, autom.GetIncoming, DC);
                                        D1s[diff_set] = block;
                                    }
                                    block.Add(q);
                                }
                                if (D1s.Count > 1) //some further split happened
                                {
                                    foreach (var block in D1s.Values)
                                        DC.blocks.AddBefore(D1node, block);
                                    DC.blocks.Remove(D1node);
                                }
                            }
                            D1node = D1node.Next;
                        }
                        //Drest cannot be further split wrt S\B because no states in Drest lead to B
                        //and because of prior refinement steps, either all states (or no states) in D lead to B.container
                        if (Drest != null)
                            DC.blocks.AddLast(Drest);
                        #endregion

                        if (DC.IsCompound)
                        {
                            //D was split
                            NextPartition.AddRange(DC.blocks);
                            if (NextWelems.Add(DC)) //DC might already be in NextW
                                NextW.Push(DC);
                        }
                        else
                            //D was not split, the container is not pushed again
                            NextPartition.Add(D);
                    }
                    Partition = NextPartition;
                }
            }
            Dictionary<int, SimBlock> finalPartition = new Dictionary<int, SimBlock>();
            foreach (SimBlock sb in Partition)
                foreach (int q in sb)
                    finalPartition[q] = sb;
            Func<int, int> GetRepresentative = (q => finalPartition[q].Elem());
            return automIn.JoinStates(GetRepresentative, solver.MkOr);
        }


        public IEnumerable<Tuple<int, T>> GetIncoming(int target)
        {
            foreach (Move<T> move in deltaInv[target])
                yield return new Tuple<int, T>(move.SourceState, move.Label);
        }

        public IEnumerable<Tuple<int, T>> GetOutgoing(int source)
        {
            foreach (Move<T> move in delta[source])
                yield return new Tuple<int, T>(move.TargetState, move.Label);
        }

        internal class MultiSetBuilder
        {
            internal IBooleanAlgebra<T> solver;
            internal Func<T, T> GetCanonicalPredicate;

            internal MultiSet zero;
            internal MultiSet one;

            Dictionary<Tuple<T, MultiSet, MultiSet>, MultiSet> nodes =
                new Dictionary<Tuple<T, MultiSet, MultiSet>, MultiSet>();
            Dictionary<int, MultiSet> leaves = new Dictionary<int, MultiSet>();

            internal MultiSetBuilder(IBooleanAlgebra<T> solver)
            {
                this.solver = solver;
                if (solver.IsExtensional)
                    GetCanonicalPredicate = (psi => psi);
                else if (solver.IsAtomic)
                    GetCanonicalPredicate = new PredicateTrie<T>(solver).Search;
                else
                    GetCanonicalPredicate = new PredicateIdMapper<T>(solver).GetId;
                zero = new MultiSet(this, 0);
                one = new MultiSet(this, 1);
                leaves[0] = zero;
                leaves[1] = one;
            }

            internal MultiSet CreateLeaf(int count)
            {
                if (count < 1)
                    throw new ArgumentOutOfRangeException("count", "value must be positive");
                MultiSet leaf;
                if (!leaves.TryGetValue(count, out leaf))
                {
                    leaf = new MultiSet(this, count);
                    leaves[count] = leaf;
                }
                return leaf;
            }

            internal MultiSet CreateOne(T pred)
            {
                return CreateNode(pred, one, zero);
            }

            internal MultiSet CreateNode(T pred, MultiSet trueCase, MultiSet falseCase)
            {
                if (pred.Equals(solver.True))
                    return trueCase;
                else if (pred.Equals(solver.False))
                    return falseCase;
                else
                {
                    if (trueCase == falseCase)
                        return trueCase;
                    else
                    {
                        var cases = new Tuple<T, MultiSet, MultiSet>(pred, trueCase, trueCase);
                        MultiSet ms;
                        if (!nodes.TryGetValue(cases, out ms))
                        {
                            ms = new MultiSet(this, cases);
                            nodes[cases] = ms;
                        }
                        return ms;
                    }
                }
            }
        }

        internal class MultiSet
        {
            internal Tuple<T, MultiSet, MultiSet> cases = null;
            MultiSetBuilder msb = null;
            int k;

            internal MultiSet(MultiSetBuilder msb, Tuple<T, MultiSet, MultiSet> cases)
            {
                this.msb = msb;
                this.cases = cases;
            }

            internal MultiSet(MultiSetBuilder msb, int k)
            {
                this.msb = msb;
                this.k = k;
                cases = null;
            }

            internal bool IsEmpty
            {
                get
                {
                    return k == 0;
                }
            }

            internal MultiSet AddOne(T predIn)
            {
                var pred = msb.GetCanonicalPredicate(predIn);
                if (cases == null)
                {
                    var leaf_plus_1 = msb.CreateLeaf(k + 1);
                    var node = msb.CreateNode(pred, leaf_plus_1, this);
                    return node;
                }
                else
                {
                    if (cases.Item1.Equals(pred))
                    {
                        var node = msb.CreateNode(cases.Item1, cases.Item2.IncrAll(new Dictionary<MultiSet, MultiSet>()), cases.Item3);
                        return node;
                    }
                    else
                    {
                        var node = msb.CreateNode(pred, this.IncrAll(new Dictionary<MultiSet, MultiSet>()), this);
                        return node;
                    }
                }
            }

            private MultiSet IncrAll(Dictionary<MultiSet, MultiSet> done)
            {
                MultiSet res;
                if (!done.TryGetValue(this, out res))
                {
                    if (cases == null)
                        res = msb.CreateLeaf(k + 1);
                    else
                        res = msb.CreateNode(cases.Item1, cases.Item2.IncrAll(done), cases.Item3.IncrAll(done));
                }
                return res;
            }

            internal MultiSet Minus(MultiSet multiSet)
            {
                throw new NotImplementedException();
            }

            internal T ToSet()
            {
                return ToSet_(new Dictionary<MultiSet, T>());
            }

            private T ToSet_(Dictionary<MultiSet, T> done)
            {
                T res;
                if (done.TryGetValue(this, out res))
                    return res;
                else
                {
                    if (cases == null)
                        if (k == 0)
                            res = msb.solver.False;
                        else
                            res = msb.solver.True;
                    else
                    {
                        var t = cases.Item2.ToSet_(done);
                        var f = cases.Item3.ToSet_(done);
                        res = msb.solver.MkOr(msb.solver.MkAnd(cases.Item1, t),
                                                  msb.solver.MkAnd(msb.solver.MkNot(cases.Item1), f));
                    }
                    done[this] = res;
                    return res;
                }
            }
        }

        internal class SimBlock : Block
        {
            Dictionary<int, MultiSet> incoming = new Dictionary<int, MultiSet>();
            Func<int, IEnumerable<Tuple<int, T>>> GetIncomingMoves;
            MultiSetBuilder msb;

            internal Dictionary<int, MultiSet> Incoming
            {
                get
                {
                    if (incoming == null)
                    {
                        incoming = new Dictionary<int, MultiSet>();
                        foreach (int q in this.set)
                            foreach (var pair in GetIncomingMoves(q))
                            {
                                MultiSet ms;
                                if (incoming.TryGetValue(pair.Item1, out ms))
                                    ms = ms.AddOne(pair.Item2);
                                else
                                    ms = msb.CreateOne(pair.Item2);
                                incoming[q] = ms;
                            }
                    }
                    return incoming;
                }
            }

            internal SimBlockContainer container;

            internal SimBlock(MultiSetBuilder msb, Func<int, IEnumerable<Tuple<int, T>>> GetIncomingMoves, SimBlockContainer container, IEnumerable<int> states)
                : base(states)
            {
                this.msb = msb;
                this.GetIncomingMoves = GetIncomingMoves;
                this.container = container;
            }

            internal SimBlock(MultiSetBuilder msb, Func<int, IEnumerable<Tuple<int, T>>> GetIncomingMoves, SimBlockContainer container)
                : base()
            {
                this.msb = msb;
                this.GetIncomingMoves = GetIncomingMoves;
                this.container = container;
            }
        }

        internal class SimBlockContainer
        {
            internal LinkedList<SimBlock> blocks = new LinkedList<SimBlock>();
            Dictionary<int, MultiSet> incoming = new Dictionary<int, MultiSet>();
            internal Dictionary<int, MultiSet> Incoming
            {
                get { return incoming; }
                set { incoming = value; }
            }
            internal bool IsCompound
            {
                get { return blocks.Count > 1; }
            }

            internal SimBlockContainer()
            {
            }

            internal void SetIncoming(IDictionary<int, MultiSet> multisets)
            {
                incoming = new Dictionary<int, MultiSet>(multisets);
            }

            internal void InitializeIncomingForRoot(Automaton<T> autom, IBooleanAlgebra<T> solver, MultiSetBuilder msb)
            {
                foreach (var q in autom.GetStates())
                    //assuming the automaton is normalized, total, and clean
                    //the total number of distinct target states from q is automaton.delta[q].Count
                    incoming[q] = msb.CreateLeaf(autom.delta[q].Count);
            }

            /// <summary>
            /// Container is assumed two contain at least two blocks.
            /// </summary>
            public SimBlock RemoveBlock()
            {
                SimBlock block;
                if (blocks.First.Value.Count <= blocks.Last.Value.Count)
                {
                    block = blocks.First.Value;
                    blocks.RemoveFirst();
                }
                else
                {
                    block = blocks.Last.Value;
                    blocks.RemoveLast();

                }
                return block;
            }

            public SimBlock First
            {
                get
                {
                    return blocks.First.Value;
                }
            }

            public SimBlock Last
            {
                get
                {
                    return blocks.Last.Value;
                }
            }

            internal void Add(params SimBlock[] newBlocks)
            {
                for (int i = 0; i < newBlocks.Length; i++)
                {
                    //if (i == 0)
                    //    incoming = new Dictionary<int, MultiSet>(newBlocks[i].incoming);
                    //else
                    //{
                    //    foreach (var entry in newBlocks[i].incoming)
                    //    {
                    //        MultiSet ms;
                    //        if (incoming.TryGetValue(entry.Key, out ms))
                    //            ms = ms.Plus(entry.Value);
                    //        else
                    //            ms = entry.Value;
                    //        incoming[entry.Key] = ms;
                    //    }
                    //}
                    blocks.AddLast(newBlocks[i]);
                }
            }
        }

        //internal class Block : IEnumerable<int>
        //{
        //    int representative = -1;
        //    bool reprChosen = false;
        //    protected HashSet<int> set;

        //    //internal void Update(Block other)
        //    //{
        //    //    this.set = other.set;
        //    //    reprChosen = false;
        //    //}

        //    internal Block() : base()
        //    {
        //        set = new HashSet<int>();
        //    }

        //    internal bool IsSingleton
        //    {
        //        get
        //        {
        //            return set.Count == 1;
        //        }
        //    }

        //    //internal Block(HashSet<int> set) 
        //    //{
        //    //    this.set = set;
        //    //}

        //    //internal bool IsEmpty


        internal Dictionary<int, Block> GetStateEquivalenceClasses(bool makeTotal = true)
        {
            IBooleanAlgebra<T> solver = algebra;

            if (IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonInvalidInput);

            if (IsEpsilon)
                throw new AutomataException(AutomataExceptionKind.AutomatonInvalidInput);

            if (IsDeterministic != true)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNondeterministic);

            var fa = makeTotal ? this.MakeTotal() : this;

            var finalBlock = new Block(fa.GetFinalStates());
            var nonfinalBlock = new Block(fa.GetNonFinalStates());
            var Blocks = new Dictionary<int, Block>();
            foreach (var q in fa.GetFinalStates()) Blocks[q] = finalBlock;
            foreach (var q in fa.GetNonFinalStates()) Blocks[q] = nonfinalBlock;

            var W = new BlockStack();

            if (makeTotal)
            {
                if (nonfinalBlock.Count < finalBlock.Count)
                    W.Push(nonfinalBlock);
                else
                    W.Push(finalBlock);
            }
            else
            {
                // This version should work when you don't want to make the automaton total
                W.Push(nonfinalBlock);
                W.Push(finalBlock);
            }

            Func<T, T, T> MkDiff = (x, y) => solver.MkAnd(x, solver.MkNot(y));

            while (!W.IsEmpty)
            {
                var R = W.Pop();
                var Rcopy = new Block(R);                //make a copy of B for iterating over its elemenents
                var Gamma = new Dictionary<int, T>();     //joined conditions leading to B from states leading to B
                foreach (var q in Rcopy)
                    foreach (var move in fa.deltaInv[q]) //moves leading to q
                        if (Blocks[move.SourceState].Count > 1) //singleton blocks cannot be further split
                            if (Gamma.ContainsKey(move.SourceState))
                                Gamma[move.SourceState] = solver.MkOr(Gamma[move.SourceState], move.Label);
                            else
                                Gamma[move.SourceState] = move.Label;

                #region apply initial splitting without using guards
                var relevant = new HashSet<Block>();
                foreach (var q in Gamma.Keys)
                    if (Blocks[q].Count > 1)
                        relevant.Add(Blocks[q]);

                foreach (var P in relevant)
                {
                    var P1 = new Block(Gamma.Keys, P.Contains); //all q in Gamma.Keys such that P.Contains(q)
                    if (P1.Count < P.Count)
                    {
                        foreach (var p in P1)
                        {
                            P.Remove(p);
                            Blocks[p] = P1;
                        }
                        if (W.Contains(P))
                            W.Push(P1);
                        else if (P.Count <= P1.Count)
                            W.Push(P);
                        else
                            W.Push(P1);
                    }
                }
                #endregion

                //keep using Bcopy until no more changes occur
                //effectively, this replaces the loop over characters
                bool iterate = true;
                while (iterate)
                {
                    iterate = false;
                    //in each relevant block all states lead to B due to the initial splitting
                    var relevant2 = new HashSet<Block>();
                    foreach (var q in Gamma.Keys)
                        if (Blocks[q].Count > 1)
                            relevant2.Add(Blocks[q]); //collect the relevant blocks

                    //only relevant blocks are potentially split
                    foreach (var P in relevant2)
                    {
                        var PE = P.GetEnumerator();
                        PE.MoveNext();

                        var P1 = new Block();
                        bool splitFound = false;

                        var psi = Gamma[PE.Current];
                        P1.Add(PE.Current); //C has at least 2 elements

                        #region compute C1 as the new sub-block of C
                        while (PE.MoveNext())
                        {
                            var q = PE.Current;
                            var phi = Gamma[q];
                            if (splitFound)
                            {
                                var psi_and_phi = solver.MkAnd(psi, phi);
                                if (solver.IsSatisfiable(psi_and_phi))
                                    P1.Add(q);
                            }
                            else
                            {
                                var psi_min_phi = MkDiff(psi, phi);
                                if (solver.IsSatisfiable(psi_min_phi))
                                {
                                    psi = psi_min_phi;
                                    splitFound = true;
                                }
                                else // [[psi]] is subset of [[phi]]
                                {
                                    var phi_min_psi = MkDiff(phi, psi);
                                    if (!solver.IsSatisfiable(phi_min_psi))
                                        P1.Add(q); //psi and phi are equivalent
                                    else
                                    {
                                        //there is some a: q --a--> B and p --a--> compl(B) for all p in C1
                                        P1.Clear();
                                        P1.Add(q);
                                        psi = phi_min_psi;
                                        splitFound = true;
                                    }
                                }
                            }
                        }
                        #endregion

                        #region split P
                        if (P1.Count < P.Count)
                        {
                            iterate = (iterate || (P.Count > 2)); //otherwise C was split into singletons
                            foreach (var p in P1)
                            {
                                P.Remove(p);
                                Blocks[p] = P1;
                            }

                            if (W.Contains(P))
                                W.Push(P1);
                            else if (P.Count <= P1.Count)
                                W.Push(P);
                            else
                                W.Push(P1);
                        }
                        #endregion
                    }
                }
            }

            return Blocks;

            //Dictionary<Tuple<int, int>, HashSet<T>> condMap = new Dictionary<Tuple<int, int>, HashSet<T>>();
            //foreach (var move in GetMoves())
            //{
            //    int s = Blocks[move.SourceState].GetRepresentative();
            //    int t = Blocks[move.TargetState].GetRepresentative();
            //    var st = new Tuple<int, int>(s, t);
            //    HashSet<T> condSet;
            //    if (!condMap.TryGetValue(st, out condSet))
            //    {
            //        condSet = new HashSet<T>();
            //        condSet.Add(move.Label);
            //        condMap[st] = condSet;
            //    }
            //    else
            //        condSet.Add(move.Label);
            //}
            //int newInitState = Blocks[fa.InitialState].GetRepresentative();
            //var newMoves = new List<Move<T>>();
            //var newFinals = new HashSet<int>();
            //foreach (var entry in condMap)
            //    newMoves.Add(Move<T>.Create(entry.Key.First, entry.Key.Second, solver.MkOr(entry.Value)));
            //foreach (var f in GetFinalStates())
            //    newFinals.Add(Blocks[f].GetRepresentative());

            //var res = Create(newInitState, newFinals, newMoves);
            //res.isDeterministic = true;
            //res.isEpsilonFree = true;
            //return res;
        }

        private string PrintStack(Stack<Part> W)
        {
            string w = "(";
            foreach (var e in W)
            {
                if (w != "(")
                    w += ",";
                w += "[" + e.ToString() + "]";
            }
            w += ")";
            return w;
        }

        IEnumerable<int> GetSources(T cond, IEnumerable<int> targets)
        {
            foreach (int p in targets)
                foreach (var move in GetMovesTo(p))
                    if (algebra.IsSatisfiable(algebra.MkAnd(move.Label, cond)))
                        yield return move.SourceState;
        }

        /// <summary>
        /// Extension of standard minimization of FAs, use timeout.
        /// This is a naive cubic algorithm.
        /// </summary>
        public Automaton<T> MinimizeClassical(int timeout, bool isTotal)
        {
            IBooleanAlgebra<T> solver = algebra;
            if (IsEmpty || IsEpsilon)
                return Minimize();

            var fa = this;
            var sw = new System.Diagnostics.Stopwatch();
            long timeoutLimit = timeout;
            if (timeout > 0)
                sw.Start();

            if (fa.IsDeterministic != true)
                throw new ArgumentException("FA must be deterministic");

            if (!isTotal)
                fa = fa.MakeTotal();

            Equivalence E = new Equivalence();

            //initialize E, all nonfinal states are quivalent 
            //and all final states are equivalent
            List<int> stateList = new List<int>(fa.States);
            for (int i = 0; i < stateList.Count; i++)
            {
                //E.Add(stateList[i], stateList[i]);
                for (int j = 0; j < stateList.Count; j++)
                {
                    int p = stateList[i];
                    int q = stateList[j];
                    bool sIsFinal = fa.IsFinalState(p);
                    bool tIsFinal = fa.IsFinalState(q);
                    if ((sIsFinal && tIsFinal) || (!sIsFinal && !tIsFinal))
                        E.Add(p, q);
                }
                CheckTimeout(sw, timeoutLimit);
            }

            //refine E
            bool continueRefinement = true;
            List<int> statesList = new List<int>(fa.States);
            while (continueRefinement)
            {
                continueRefinement = false;
                for (int i = 0; i < statesList.Count; i++)
                    for (int j = 0; j < statesList.Count; j++)
                    {
                        Tuple<int, int> pq = new Tuple<int, int>(statesList[i], statesList[j]);
                        if (E.Contains(pq))
                            if (pq.Item1 != pq.Item2 && AreDistinguishable(fa, E, pq))
                            {
                                E.Remove(pq);
                                continueRefinement = true;
                            }
                        CheckTimeout(sw, timeoutLimit);
                    }
            }

            //create id's for equivalence classes
            Dictionary<int, int> equivIdMap = new Dictionary<int, int>();
            List<int> mfaStates = new List<int>();
            foreach (Tuple<int, int> pq in E)
            {
                int equivId;
                if (equivIdMap.TryGetValue(pq.Item1, out equivId))
                    equivIdMap[pq.Item2] = equivId;
                else if (equivIdMap.TryGetValue(pq.Item2, out equivId))
                    equivIdMap[pq.Item1] = equivId;
                else
                {
                    equivIdMap[pq.Item1] = pq.Item1;
                    equivIdMap[pq.Item2] = pq.Item1;
                    mfaStates.Add(pq.Item1);
                }
                CheckTimeout(sw, timeoutLimit);
            }
            //remaining states map to themselves
            foreach (int state in fa.States)
                if (!equivIdMap.ContainsKey(state))
                {
                    equivIdMap[state] = state;
                    mfaStates.Add(state);
                }

            int mfaInitialState = equivIdMap[fa.InitialState];

            //group together transition conditions for transitions on equivalent states
            Dictionary<Tuple<int, int>, T> combinedConditionMap = new Dictionary<Tuple<int, int>, T>();
            foreach (int state in fa.States)
            {
                int fromStateId = equivIdMap[state];
                foreach (Move<T> trans in fa.GetMovesFrom(state))
                {
                    int toStateId = equivIdMap[trans.TargetState];
                    T cond;
                    var p = new Tuple<int, int>(fromStateId, toStateId);
                    if (combinedConditionMap.TryGetValue(p, out cond))
                        combinedConditionMap[p] = solver.MkOr(cond, trans.Label);
                    else
                        combinedConditionMap[p] = trans.Label;
                }
                CheckTimeout(sw, timeoutLimit);
            }

            //form the transitions of the mfa
            List<Move<T>> mfaTransitions = new List<Move<T>>();
            foreach (var kv in combinedConditionMap)
            {
                mfaTransitions.Add(Move<T>.Create(kv.Key.Item1, kv.Key.Item2, kv.Value));
            }

            //final states
            HashSet<int> mfaFinalStates = new HashSet<int>();
            foreach (int state in fa.GetFinalStates())
                mfaFinalStates.Add(equivIdMap[state]);

            Automaton<T> mfa = Automaton<T>.Create(this.algebra, mfaInitialState, mfaFinalStates, mfaTransitions);
            mfa.isDeterministic = true;
            mfa.isEpsilonFree = true;
            mfa.EliminateDeadStates();

            return mfa;
        }

        /// <summary>
        /// Creates an automaton that accepts the reverse of the language.
        /// The resulting automaton will contain epsilon moves if this automaton has more than one final state.
        /// </summary>
        /// <returns></returns>
        public Automaton<T> Reverse()
        {
            if (IsEmpty || IsEpsilon)
            {
                return this;
            }

            List<Move<T>> reversed_moves = new List<Move<T>>();
            foreach (var move in this.GetMoves())
                reversed_moves.Add(Move<T>.Create(move.TargetState, move.SourceState, move.Label));

            Automaton<T> res = null;
            if (finalStateSet.Count == 1)
                res = Create(this.algebra, FinalState, new int[] { initialState }, reversed_moves);
            else
            {
                int new_initial_state = maxState + 1;
                foreach (var f in this.finalStateSet)
                    reversed_moves.Add(Move<T>.Epsilon(new_initial_state, f));
                res = Create(this.algebra, new_initial_state, new int[] { initialState }, reversed_moves);
            }
            return res;
        }

        //public Automaton<S> MinimizeBrzozowski(IBooleanAlgebra<S> solver)
        //{
        //    var minReversed = this.Determinize(solver).Reverse().RemoveEpsilons(solver.MkOr).Determinize(solver);
        //    var res = minReversed.Reverse().RemoveEpsilons(solver.MkOr).Determinize(solver);
        //    return res;
        //}

        private T MkComplementCondition(IEnumerable<Move<T>> list, IBooleanAlgebra<T> solver)
        {
            List<T> conds = new List<T>();
            foreach (var t in list)
                conds.Add(solver.MkNot(t.Label));
            return solver.MkAnd(conds.ToArray());
        }

        private bool AreDistinguishable(Automaton<T> fa, Equivalence E, Tuple<int, int> pq)
        {
            IBooleanAlgebraPositive<T> solver = fa.algebra;
            foreach (Move<T> from_p in fa.GetMovesFrom(pq.Item1))
                foreach (Move<T> from_q in fa.GetMovesFrom(pq.Item2))
                    if (from_p.TargetState != from_q.TargetState &&
                        !E.AreEquiv(from_p.TargetState, from_q.TargetState))
                        if (solver.IsSatisfiable(solver.MkAnd(from_p.Label, from_q.Label)))
                            return true;
            return false;
        }

        #endregion

        public bool AllStatesAreFinal()
        {
            foreach (int state in States)
                if (!IsFinalState(state))
                    return false;
            return true;
        }

        #region Choose an accepting path

        /// <summary>
        /// Produces a random path of labels from the initial state to some final state.
        /// Assumes that the automaton is nonempty and does not contain deadends.
        /// </summary>
        /// <param name="chooser">uses the chooser for randomizing the choices</param>
        public IEnumerable<T> ChoosePathToSomeFinalState(Chooser chooser)
        {
            if (IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeNonempty);

            int state = InitialState;
            while (!IsFinalState(state) || (OutDegree(state) > 0 && chooser.ChooseTrueOrFalse()))
            {
                if (!IsFinalState(state) && OutDegree(state) == 0)
                    throw new AutomataException(AutomataExceptionKind.AutomatonMustNotContainDeadStates);

                var move = GetNthMoveFrom(state, chooser.Choose(GetMovesCountFrom(state)));
                if (!move.IsEpsilon)
                    yield return move.Label;

                state = move.TargetState;
            }
            yield break;
        }

        #endregion

        #region IAutomaton<S> Members

        /// <summary>
        /// The initial state of the automaton.
        /// </summary>
        public int InitialState
        {
            get { return initialState; }
        }

        /// <summary>
        /// Returns true iff the state is a final state of the automaton.
        /// </summary>
        public bool IsFinalState(int state)
        {
            return finalStateSet.Contains(state);
        }

        /// <summary>
        /// Returns true iff there exists a move from state to state.
        /// </summary>
        public bool IsLoopState(int state)
        {
            foreach (var move in GetMovesFrom(state))
                if (move.TargetState == state)
                    return true;

            return false;
        }

        /// <summary>
        /// Enumerates all states of the automaton.
        /// </summary>
        public IEnumerable<int> GetStates()
        {
            return States;
        }

        public IEnumerable<int> GetTargetStates(int source, Predicate<T> pred)
        {
            foreach (var m in GetMovesFrom(source))
                if (pred(m.Label))
                    yield return m.TargetState;
        }

        /// <summary>
        /// Assumes that the automaton has some target state for 
        /// some element in the given predicate and returns such a target state.
        /// </summary>
        int GetTargetState_(int source, T pred)
        {
            var moves = delta[source];
            int target = 0;
            int i = 0;
            while (i < moves.Count)
            {
                var move = moves[i];
                if (!move.IsEpsilon && algebra.IsSatisfiable(algebra.MkAnd(pred, move.Label)))
                {
                    target = move.TargetState;
                    break;
                }
                i += 1;
            }
            if (i == moves.Count)
                throw new AutomataException(AutomataExceptionKind.AutomatonMissingTransition);
            return target;
        }

        /// <summary>
        /// Returns true if the automaton has some target state for 
        /// some element in the given predicate then such a target state is output. 
        /// Else returns false and sets target to -1.
        /// </summary>
        bool TryGetTargetState_(int source, out int target, T pred)
        {
            var moves = delta[source];
            int i = 0;
            while (i < moves.Count)
            {
                var move = moves[i];
                if (!move.IsEpsilon && algebra.IsSatisfiable(algebra.MkAnd(pred, move.Label)))
                {
                    target = move.TargetState;
                    return true;
                }
                i += 1;
            }
            target = -1;
            return false;
        }

        /// <summary>
        /// Assumes that the automaton is total and deterministic and all input predicates are satisfiable. 
        /// Returns the target state after the given sequence of inputs.
        /// </summary>
        public int GetTargetState(int source, params T[] pred)
        {
            int p = source;
            for (int i = 0; i < pred.Length; i++)
                p = GetTargetState_(p, pred[i]);
            return p;
        }


        /// <summary>
        /// Returns true if the automaton has some target state for 
        /// some elements in the given predicate sequence, then such a target is output. 
        /// Else returns false and sets target to -1.
        /// </summary>
        public bool TryGetTargetState(int source, out int target, params T[] pred)
        {
            int p = source;
            for (int i = 0; i < pred.Length; i++)
            {
                int t;
                if (TryGetTargetState_(p, out t, pred[i]))
                {
                    p = t;
                }
                else
                {
                    target = -1;
                    return false;
                }
            }
            target = p;
            return true;
        }


        /// <summary>
        /// Enumerates all moves of the automaton.
        /// </summary>
        public virtual IEnumerable<Move<T>> GetMoves()
        {
            foreach (int state in States)
                foreach (Move<T> move in delta[state])
                    yield return move;
        }

        /// <summary>
        /// Returns state.ToString().
        /// </summary>
        public virtual string DescribeState(int state)
        {
            return state.ToString();
        }

        /// <summary>
        /// Returns lab.ToString(), or the empty string when S is not a value type and lab is null. 
        /// </summary>
        public virtual string DescribeLabel(T lab)
        {
            if (typeof(T).IsValueType)
                return lab.ToString();
            else if (object.Equals(lab, null))
                return "";
            else
                return lab.ToString();
        }

        #endregion

        #region IAutomaton<S> Members


        public virtual string DescribeStartLabel()
        {
            return "";
        }

        #endregion

        internal Dictionary<int, double[]> probabilities = null;

        /// <summary>
        /// Gets the probability of moving with the given move.
        /// </summary>
        /// <param name="move">given move of the automaton</param>
        public double GetProbability(Move<T> move)
        {
            if (probabilities == null)
                throw new AutomataException(AutomataExceptionKind.ProbabilitiesHaveNotBeenComputed);

            List<Move<T>> moves;
            if (!delta.TryGetValue(move.SourceState, out moves))
                throw new AutomataException(AutomataExceptionKind.AutomatonInvalidMove);

            for (int i = 0; i < moves.Count; i++)
                if (move == moves[i])
                    return probabilities[move.SourceState][i];

            throw new AutomataException(AutomataExceptionKind.AutomatonInvalidMove);
        }

        /// <summary>
        /// Gets the probability of finalizing in state q. 
        /// Returns 0.0 if q is not a final state.
        /// </summary>
        /// <param name="q">given state of the automaton</param>
        public double GetProbability(int q)
        {
            if (probabilities == null)
                throw new AutomataException(AutomataExceptionKind.ProbabilitiesHaveNotBeenComputed);

            double[] probs;
            if (!probabilities.TryGetValue(q, out probs))
                throw new AutomataException(AutomataExceptionKind.AutomatonInvalidState);

            if (IsFinalState(q))
                return probs[probs.Length - 1];
            else
                return 0.0;
        }

        BigInt cardinality = null;
        public BigInt Cardinality
        {
            get
            {
                return cardinality;
            }
        }

        /// <summary>
        /// Associates all moves and final states with a probability that is a double between 0.0 and 1.0.
        /// The sum of probabilities of all outgoing moves from a state plus the probability of finishing in the state is 1.0.
        /// The probability of a move (s,l,t) is (f(l) * DomainSize(t)) / DomainSize(s). 
        /// where DomainSize(s) is the sum of all f(l) * DomainSize(t) such that (s,l,t) 
        /// is a move from s, plus 1 if s is a final state.
        /// The method requires that the automaton has no loops, no deadends and is deterministic.
        /// </summary>
        /// <param name="f">function that calculates the cardinality of a label of a move</param>
        public void ComputeProbabilities(Func<T, ulong> f)
        {
            if (probabilities != null)
                return;

            if (!IsDeterministic || !IsLoopFree || IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeLoopFreeAndDeterministic);

            var stateCardinalities = new Dictionary<int, BigInt>();
            probabilities = new Dictionary<int, double[]>();

            for (int i = topsort.Count - 1; i >= 0; i--)
            {
                var st = topsort[i];
                var card = (IsFinalState(st) ? BigInt.One : BigInt.Zero);
                int k = (IsFinalState(st) ? delta[st].Count + 1 : delta[st].Count);
                probabilities[st] = new double[k];
                var transitionCardinalities = new BigInt[k];

                for (int j = 0; j < delta[st].Count; j++)
                {
                    var move = delta[st][j];
                    var m = f(move.Label);
                    var lcard = new BigInt(m);
                    transitionCardinalities[j] = lcard.Times(stateCardinalities[move.TargetState]);
                    card = card.Plus(transitionCardinalities[j]);
                }
                if (IsFinalState(st))
                {
                    transitionCardinalities[k - 1] = BigInt.One;
                }
                double acc = 0.0;
                for (int j = 0; j < transitionCardinalities.Length; j++)
                {
                    if (j < transitionCardinalities.Length - 1)
                    {
                        double d = transitionCardinalities[j].DivideAsDouble(card);
                        probabilities[st][j] = d;
                        acc += d;
                    }
                    else
                    {
                        probabilities[st][j] = Math.Round(((double)1.0) - acc, BigInt.NrOfDecimals);
                    }
                }
                stateCardinalities[st] = card;
            }

            cardinality = stateCardinalities[initialState];
        }

        /// <summary>
        /// Returns null if st is a final state and the final implicit 
        /// epsilon transition was chosen to the imaginary final sink state
        /// </summary>
        internal Move<T> ChooseTransitionUniformly(Chooser cs, int st)
        {
            var w = probabilities[st];
            double d = cs.ChooseDouble();
            int i = 0;
            while (d > w[i] && i < w.Length - 1)
            {
                d = d - w[i];
                i = i + 1;
            }
            if (i < delta[st].Count)
                return delta[st][i];
            else
                return null; //st is a final state and end of string was chosen here
        }

        internal void EliminateWordBoundaries(Func<bool, T> getWordCharPred)
        {
            IBooleanAlgebra<T> solver = algebra;

            if (DoesNotContainWordBoundaries)
                return;

            //new initial state
            var tmp = initialState;
            initialState = maxState + 1;
            AddMove(Move<T>.Epsilon(initialState, tmp));
            maxState = initialState;

            //new single final state called sink
            var sink = maxState + 1;
            foreach (var f in finalStateSet)
                AddMove(Move<T>.Epsilon(f, sink));
            finalStateSet.Clear();
            finalStateSet.Add(sink);
            maxState = sink;

            var wordCharPred = getWordCharPred(true);
            var not_wordCharPred = solver.MkNot(wordCharPred);

            //eliminate all boundaries one at a time
            var stack = new Stack<int>(wordBoundaries);
            while (stack.Count > 0)
            {
                var b = stack.Pop();
                var p1 = maxState + 1;
                var p2 = maxState + 2;
                maxState = p2;


                bool isAtStart;
                var enteringMoves = GetProperEnteringMoves(b, out isAtStart);
                bool isAtEnd;
                var exitingMoves = GetProperExitingMoves(b, out isAtEnd);

                //List<Move<S>> newMoves = new List<Move<S>>();

                foreach (var move in enteringMoves)
                {
                    var cond1 = solver.MkAnd(move.Label, wordCharPred);
                    var cond2 = solver.MkAnd(move.Label, not_wordCharPred);
                    if (solver.IsSatisfiable(cond1))
                        AddMove(Move<T>.Create(move.SourceState, p1, cond1));
                    //newMoves.Add(Move<S>.M(move.SourceState, p1, cond1));
                    if (solver.IsSatisfiable(cond2))
                        AddMove(Move<T>.Create(move.SourceState, p2, cond2));
                    // newMoves.Add(Move<S>.M(move.SourceState, p2, cond2));
                }

                foreach (var move in exitingMoves)
                {
                    var cond1 = solver.MkAnd(move.Label, wordCharPred);
                    var cond2 = solver.MkAnd(move.Label, not_wordCharPred);
                    if (solver.IsSatisfiable(cond2))
                        AddMove(Move<T>.Create(p1, move.TargetState, cond2));
                    //newMoves.Add(Move<S>.M(p1, move.TargetState, cond2));
                    if (solver.IsSatisfiable(cond1))
                        AddMove(Move<T>.Create(p2, move.TargetState, cond1));
                    //newMoves.Add(Move<S>.M(p2, move.TargetState, cond1));
                }

                if (isAtStart)
                    AddMove(Move<T>.Epsilon(initialState, p2));
                //newMoves.Add(Move<S>.Epsilon(initialState, p2));

                if (isAtEnd)
                    AddMove(Move<T>.Epsilon(p1, sink));
                //newMoves.Add(Move<S>.Epsilon(p1, sink));

                //foreach (var move in newMoves)
                //    AddMove(move);

                RemoveTheState(b);
            }

            //foreach (var st in wordBoundaries)
            //    RemoveTheState(st);

            wordBoundaries = null;
            //((CharSetSolver)solver).ShowGraph(this as Automaton<BvSet>, "foo");
            EliminateUnrreachableStates();
            EliminateDeadStates();
        }

        List<Move<T>> GetProperEnteringMoves(int q, out bool isAtStart)
        {
            var res = new List<Move<T>>();
            bool b = false;
            foreach (var q1 in GetInvEpsilonClosure(q))
            {
                if (initialState == q1)
                    b = true;
                foreach (var move in GetNonepsilonMovesTo(q1))
                    res.Add(move);
            }
            isAtStart = b;
            return res;
        }

        List<Move<T>> GetProperExitingMoves(int q, out bool isAtEnd)
        {
            var res = new List<Move<T>>();
            bool b = false;
            foreach (var q1 in GetEpsilonClosure(q))
            {
                if (IsFinalState(q1))
                    b = true;
                foreach (var move in GetNonepsilonMovesFrom(q1))
                    res.Add(move);
            }
            isAtEnd = b;
            return res;
        }

        /// <summary>
        /// Returns a path from pStart to a final state.
        /// If there are no epsilon moves then the path is shortest.
        /// Returns null if pStart does not lead to a final state.
        /// </summary>
        /// <param name="pStart">given start state</param>
        /// <returns>(label path, final state) or null</returns>
        public Tuple<T[], int> FindShortestFinalPath(int pStart)
        {
            if (!this.delta.ContainsKey(pStart))
                throw new AutomataException(AutomataExceptionKind.AutomatonInvalidState);

            if (IsFinalState(pStart))
                return Tuple.Create(new T[] { }, pStart);

            SimpleStack<int> currentStack = null;
            var nextStack = new SimpleStack<int>();
            var path = new Dictionary<int, SimpleList<T>>();
            nextStack.Push(pStart);
            path[pStart] = SimpleList<T>.Empty;

            while (nextStack.IsNonempty)
            {
                currentStack = nextStack;
                nextStack = new SimpleStack<int>();
                while (currentStack.IsNonempty)
                {
                    var p = currentStack.Pop();
                    var p_path = path[p];
                    foreach (var move in GetMovesFrom(p))
                    {
                        var q = move.TargetState;
                        if (IsFinalState(q))
                        {
                            if (move.IsEpsilon)
                                return Tuple.Create(p_path.ToArray(), q);
                            else
                                return Tuple.Create(p_path.Append(move.Label).ToArray(), q);
                        }
                        else if (!path.ContainsKey(q))
                        {
                            nextStack.Push(q);
                            if (move.IsEpsilon)
                                path[q] = p_path;
                            else
                                path[q] = p_path.Append(move.Label);
                        }
                    }
                }
            }
            return null;
        }

        public bool IsState(int q)
        {
            return this.delta.ContainsKey(q);
        }

        /// <summary>
        ///States are counted consequtively from q0 to q0+n is there are n states (the automaton is made total).
        ///If the original automaton was partial then q0+n is a nonaccepting sink state.
        ///Automaton is assumed to be epsilon free.
        /// </summary>
        /// <param name="q0">the id of the initial state default is 0</param>
        /// <returns></returns>
        public Automaton<T> Normalize(int q0 = 0) 
        {
            IBooleanAlgebra<T> solver = this.algebra;
            if (!this.isEpsilonFree)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNotEpsilonfree);

            Dictionary<int, int> state2norm = new Dictionary<int, int>();
            state2norm[this.initialState] = q0;
            int nextState = q0 + 1;
            Func<int, int> Norm = q =>
            {
                int n;
                if (state2norm.TryGetValue(q, out n))
                    return n;
                n = nextState;
                nextState += 1;
                state2norm[q] = n;
                return n;
            };

            bool deadStateIsUsed = false;
            int deadState = q0 + this.StateCount;

            var normMoves = new List<Move<T>>();

            foreach (int state in this.States)
            {
                foreach (var move in this.GetMovesFrom(state))
                    normMoves.Add(Move<T>.Create(Norm(move.SourceState), Norm(move.TargetState), move.Label));

                var conds = new List<T>(this.EnumerateConditions(state));
                var or_conds = solver.MkOr(conds);
                var cond = solver.MkNot(or_conds);
                if (solver.IsSatisfiable(cond))
                {
                    normMoves.Add(Move<T>.Create(Norm(state), deadState, cond));
                    deadStateIsUsed = true;
                }
            }
            if (deadStateIsUsed)
                normMoves.Add(Move<T>.Create(deadState, deadState, solver.True));

            List<int> normFinalStates = new List<int>();
            foreach (var q in this.finalStateSet)
                normFinalStates.Add(Norm(q));

            var norm = Automaton<T>.Create(this.Algebra, q0, normFinalStates, normMoves, false, false, this.isDeterministic);
            return norm;
        }

        /// <summary>
        /// Normalizes and compiles the automaton to 
        /// C# code that is exposed through the ICompiledStringMatcher interface.
        /// The automaton must be deterministic and the algebra must be CharSetSolver.
        /// </summary>
        public IMatcher Compile(string classname = null, string namespacename = null)
        {
            if (!(algebra is CharSetSolver))
                throw new AutomataException(AutomataExceptionKind.AlgebraMustBeCharSetSolver);
            if (!this.IsDeterministic)
                throw new AutomataException(AutomataExceptionKind.AutomatonIsNondeterministic);

            var compiler = new AutomataCSharpCompiler(this as Automaton<BDD>, classname, namespacename, true);
            var res = compiler.Compile();
            return res;
        }
    }

    #region Helper classes used in various algorithms
    internal class CompiledFiniteAutomaton : IMatcher
    {
        public IDeterministicFiniteAutomaton Automaton { get; }

        public string SourceCode { get; }

        internal CompiledFiniteAutomaton(string source, IDeterministicFiniteAutomaton automaton)
        {
            this.SourceCode = source;
            this.Automaton = automaton;
        }

        public bool IsMatch(string input, int start = 0, int end = -1)
        {
            return Automaton.IsMatch(input.Substring(start));
        }

        public Tuple<int, int>[] Matches(string input, int start=0, int limit = 0, int end = -1)
        {
            throw new NotImplementedException();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public string GenerateRandomMatch(int maxLoopUnrol = 10, int cornerCaseProb = 5, int maxSamplingIter = 3)
        {
            throw new NotImplementedException();
        }

        public void Serialize(Stream stream, IFormatter formatter = null)
        {
            throw new NotImplementedException();
        }

        public void Serialize(string file, IFormatter formatter = null)
        {
            throw new NotImplementedException();
        }
    }

    internal class Block : IEnumerable<int>
    {
        int representative = -1;
        bool reprChosen = false;
        internal HashSet<int> set;

        public int Elem()
        {
            if (reprChosen)
                return representative;
            else
            {
                var e = set.GetEnumerator();
                e.MoveNext();
                representative = e.Current;
                reprChosen = true;
                return representative;
            }
        }

        internal bool Add(int item)
        {
            return set.Add(item);
        }

        internal Block(IEnumerable<int> items, Predicate<int> match = null)
        {
            if (match == null)
                set = new HashSet<int>(items);
            else
            {
                set = new HashSet<int>();
                foreach (var item in items)
                    if (match(item))
                        set.Add(item);
            }
        }

        internal void Update(Block other)
        {
            this.set = other.set;
            reprChosen = false;
        }

        internal Block()
            : base()
        {
            set = new HashSet<int>();
        }

        internal bool Contains(int item)
        {
            return set.Contains(item);
        }

        internal Block(HashSet<int> set)
        {
            this.set = set;
        }

        internal bool IsEmpty
        {
            get { return set.Count == 0; }
        }

        internal bool IsSingleton
        {
            get { return set.Count == 1; }
        }

        internal int Count
        {
            get { return set.Count; }
        }

        internal void Remove(int item)
        {
            set.Remove(item);
            reprChosen = false;
        }

        internal void Clear()
        {
            set.Clear();
            reprChosen = false;
        }

        public override string ToString()
        {
            string res = "[";
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                if (res != "[")
                    res += ",";
                res += e.Current;
            }
            res += "]";
            return res;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return set.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    internal class BlockStack
    {
        ConsList<Block> list = null;
        HashSet<Block> set = new HashSet<Block>();

        internal BlockStack()
        {
            Stack<int> foo = new Stack<int>();
            foo.Contains(0);
        }

        internal bool IsEmpty
        {
            get { return set.Count == 0; }
        }

        internal void Push(Block b)
        {
            list = new ConsList<Block>(b, list);
            set.Add(b);
        }

        internal Block Pop()
        {
            while (true)
            {
                var b = list.First;
                list = list.Rest;
                if (set.Contains(b))
                {
                    set.Remove(b);
                    return b;
                }                
            }
        }

        internal bool Contains(Block b)
        {
            return set.Contains(b);
        }

        internal void Remove(Block b)
        {
            if (Contains(b))
                set.Remove(b);
        }

        public override string ToString()
        {
            string res = "[";
            var l = list;
            while (l != null)
            {
                if (res != "[")
                    res += ",";
                res += l.First.ToString();
                l = l.Rest;
            }
            res += "]";
            return res;
        }
    }

    internal class Equivalence : IEnumerable<Tuple<int, int>>
    {
        HashSet<Tuple<int, int>> E = new HashSet<Tuple<int, int>>();
        public Equivalence() { }
        public void Add(int p, int q)
        {
            if (p < q)
                E.Add(new Tuple<int, int>(p, q));
            else if (q < p)
                E.Add(new Tuple<int, int>(q, p));
        }
        public bool AreEquiv(int p, int q)
        {
            if (p == q)
                return true;
            else if (p < q)
                return E.Contains(new Tuple<int, int>(p, q));
            else
                return E.Contains(new Tuple<int, int>(q, p));
        }

        internal void Remove(int p, int q)
        {
            if (p < q)
                E.Remove(new Tuple<int, int>(p, q));
            else
                E.Remove(new Tuple<int, int>(q, p));
        }

        #region IEnumerable<Tuple<int,int>> Members

        public IEnumerator<Tuple<int, int>> GetEnumerator()
        {
            return E.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return E.GetEnumerator();
        }

        #endregion

        internal void Remove(Tuple<int, int> equivToBeRemoved)
        {
            E.Remove(equivToBeRemoved);
        }

        internal bool Contains(Tuple<int, int> equiv)
        {
            return E.Contains(equiv);
        }
    }

    public abstract class PowerSetStateBuilder
    {
        public abstract int MakePowerSetState(IEnumerable<int> members);

        public abstract IEnumerable<int> GetMembers(int powersetstate);

        public abstract bool IsMember(int elem, int powersetstate);

        public static PowerSetStateBuilder Create(int[] states)
        {
            if (states.Length <= 31)
                return new DfaStateBuilder32(states);
            if (states.Length <= 64)
                return new DfaStateBuilder64(states);
            return new DfaStateBuilderGeneral();
        }
    }

    internal class DfaStateBuilder32 : PowerSetStateBuilder
    {
        int[] nfaStates;
        Dictionary<int, int> nfaStateToBitMap = new Dictionary<int, int>();

        internal DfaStateBuilder32(int[] nfaStates)
        {
            this.nfaStates = nfaStates;
            nfaStateToBitMap = new Dictionary<int, int>(nfaStates.Length);
            int b = 1;
            for (int i = 0; i < nfaStates.Length; i++)
                nfaStateToBitMap[nfaStates[i]] = (b << i);
        }

        public override int MakePowerSetState(IEnumerable<int> someNfaStates)
        {
            int dfaState = 0;
            foreach (int nfaState in someNfaStates)
                dfaState = dfaState | nfaStateToBitMap[nfaState];
            return dfaState;
        }

        public override IEnumerable<int> GetMembers(int dfaState)
        {
            if (dfaState == 0)
                yield break;

            int b = 1;
            for (int i = 0; i < nfaStates.Length; i++)
            {
                if ((dfaState & b) != 0) //checks that the i'th bit is 1
                    yield return nfaStates[i];
                b <<= 1;
            }
        }

        public override bool IsMember(int state, int dfaState)
        {
            int bitmap = nfaStateToBitMap[state];
            if ((dfaState & bitmap) == 0)
                return false;
            else
                return true;
        }
    }

    internal class DfaStateBuilder64 : PowerSetStateBuilder
    {
        int[] nfaStates;
        Dictionary<int, long> nfaStateToBitMap = new Dictionary<int, long>();

        Dictionary<long, int> actualDFAstateMap = new Dictionary<long, int>();
        Dictionary<int, long> reverseActualDFAstateMap = new Dictionary<int, long>();

        internal DfaStateBuilder64(int[] nfaStates)
        {
            this.nfaStates = nfaStates;
            nfaStateToBitMap = new Dictionary<int, long>(nfaStates.Length);
            long b = 1;
            for (int i = 0; i < nfaStates.Length; i++)
                nfaStateToBitMap[nfaStates[i]] = (b << i);
        }

        public override int MakePowerSetState(IEnumerable<int> someNfaStates)
        {
            long dfaState = 0;
            foreach (int nfaState in someNfaStates)
                dfaState = dfaState | nfaStateToBitMap[nfaState];
            if (dfaState == 0)
                return 0;

            int actualDFAstate;
            if (actualDFAstateMap.TryGetValue(dfaState, out actualDFAstate))
                return actualDFAstate;

            actualDFAstate = actualDFAstateMap.Count + 1;
            actualDFAstateMap[dfaState] = actualDFAstate;
            reverseActualDFAstateMap[actualDFAstate] = dfaState;
            return actualDFAstate;
        }

        public override IEnumerable<int> GetMembers(int dfaState)
        {
            if (dfaState == 0)
                yield break;

            long dfaState64 = reverseActualDFAstateMap[dfaState];
            long b = 1;
            for (int i = 0; i < nfaStates.Length; i++)
            {
                if ((dfaState64 & b) != 0) //checks that the i'th bit is 1
                    yield return nfaStates[i];
                b <<= 1;
            }
        }

        public override bool IsMember(int state, int dfaState)
        {
            if (dfaState == 0)
                return false;

            long dfaState64 = reverseActualDFAstateMap[dfaState];
            long bitmap = nfaStateToBitMap[state];
            if ((dfaState64 & bitmap) == 0)
                return false;
            else
                return true;
        }
    }

    internal class DfaStateBuilderGeneral : PowerSetStateBuilder
    {
        Dictionary<IntSet, int> dfaStateIdMap = new Dictionary<IntSet, int>();
        Dictionary<int, IntSet> nfaLookup = new Dictionary<int, IntSet>();

        internal DfaStateBuilderGeneral() { }

        public override int MakePowerSetState(IEnumerable<int> someNfaStates)
        {
            IntSet nfaStates = new IntSet(someNfaStates);
            if (nfaStates.IsEmpty)
                return 0;

            int dfaStateId;
            if (dfaStateIdMap.TryGetValue(nfaStates, out dfaStateId))
                return dfaStateId;
            dfaStateId = dfaStateIdMap.Count + 1;
            dfaStateIdMap[nfaStates] = dfaStateId;
            nfaLookup[dfaStateId] = nfaStates;
            return dfaStateId;
        }

        int[] nothing = new int[] { };
        public override IEnumerable<int> GetMembers(int dfaState)
        {
            if (dfaState == 0)
                return nothing;

            return nfaLookup[dfaState].EnumerateMembers();
        }

        public override bool IsMember(int state, int dfaState)
        {
            if (dfaState == 0)
                return false;

            return nfaLookup[dfaState].Contains(state);
        }
    }

    #endregion
}