using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Automata;

namespace Microsoft.Automata
{
    internal class ExtendedAutomaton<S> 
    {
        private Dictionary<int, List<ExtendedNormalMove<S>>> delta;
        private Dictionary<int, List<ExtendedFinalMove<S>>> deltaFinal;
        private Dictionary<int, List<ExtendedNormalMove<S>>> deltaInv;
        private List<ExtendedFinalMove<S>> finalMoves;
        private int initialState;
        private int maxState;

        internal bool isDeterministic = false;

        internal bool didTopSort = false;
        internal List<int> topsort = null;

        ///// <summary>
        ///// Retunrs true iff the automaton has no states p such that there is a nonempty path from p to p.
        ///// </summary>
        //public bool IsLoopFree
        //{
        //    get
        //    {
        //        if (!didTopSort)
        //            topsort = TopSort();
        //        return topsort != null;
        //    }
        //}

        ///// <summary>
        ///// Returns a topological sorting of all the states. 
        ///// Returns null iff there is a loop: a state p and a nonempty sequence of moves from p to p.
        ///// </summary>
        ///// <returns></returns>
        //public List<int> TopSort()
        //{
        //    if (didTopSort)
        //        return this.topsort;

        //    didTopSort = true;
        //    var remainingMoves = new HashSet<Move<S>>(GetMoves());

        //    var stack = new Stack<int>();
        //    var tops = new List<int>();
        //    var set = new HashSet<int>();

        //    foreach (var st in States)
        //    {
        //        if (InDegree(st) == 0)
        //        {
        //            stack.Push(st);
        //            set.Add(st);
        //        }
        //    }

        //    while (stack.Count > 0)
        //    {
        //        var q = stack.Pop();
        //        tops.Add(q);
        //        foreach (var move in GetMovesFrom(q))
        //        {
        //            if (move.TargetState == move.SourceState)
        //                return null;

        //            remainingMoves.Remove(move);
        //        }
        //        foreach (var move in GetMovesFrom(q))
        //        {
        //            var p = move.TargetState;
        //            bool p_hasNoIncomingLinks = true;
        //            foreach (var move1 in GetMovesTo(p))
        //            {
        //                if (remainingMoves.Contains(move1))
        //                {
        //                    p_hasNoIncomingLinks = false;
        //                    break;
        //                }
        //            }
        //            if (p_hasNoIncomingLinks)
        //            {
        //                if (set.Add(p))
        //                    stack.Push(p);
        //            }
        //        }
        //    }
        //    if (remainingMoves.Count == 0)
        //    {
        //        this.topsort = tops;
        //        return tops;
        //    }
        //    else
        //        return null;
        //}

        /// <summary>
        /// The empty automaton
        /// </summary>
        public readonly static ExtendedAutomaton<S> Empty = MkEmpty();

        static ExtendedAutomaton<S> MkEmpty()
        {
            var fsa = new ExtendedAutomaton<S>();
            fsa.initialState = 0;
            fsa.maxState = 0;
            fsa.delta = new Dictionary<int, List<ExtendedNormalMove<S>>>();
            fsa.deltaFinal = new Dictionary<int, List<ExtendedFinalMove<S>>>();
            fsa.deltaInv = new Dictionary<int, List<ExtendedNormalMove<S>>>();
            fsa.finalMoves = new List<ExtendedFinalMove<S>>();


            fsa.delta[0] = new List<ExtendedNormalMove<S>>();
            fsa.deltaInv[0] = new List<ExtendedNormalMove<S>>();
            fsa.deltaFinal[0] = new List<ExtendedFinalMove<S>>();

            fsa.isDeterministic = true;
            return fsa;
        }

        public int MoveCount
        {
            get
            {
                int res = 0;
                foreach (int state in delta.Keys)
                    res += delta[state].Count;
                foreach (int state in deltaFinal.Keys)
                    res += deltaFinal[state].Count;
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

        /// <summary>
        /// True iff there are no final states
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return finalMoves.Count == 0;
            }
        }

        /// <summary>
        /// Create a symbolic automaton.
        /// </summary>
        /// <param name="initialState">initial state</param>
        /// <param name="finalStates">final states</param>
        /// <param name="moves">moves</param>
        /// <returns></returns>
        public static ExtendedAutomaton<S> Create(int initialState, IEnumerable<ExtendedMove<S>> moves, bool eliminateUnrreachableStates = false, bool eliminateDeadStates = false)
        {
            var delta = new Dictionary<int, List<ExtendedNormalMove<S>>>();
            var deltaInv = new Dictionary<int, List<ExtendedNormalMove<S>>>();
            var deltaFinal = new Dictionary<int, List<ExtendedFinalMove<S>>>();
            var finalMoves = new List<ExtendedFinalMove<S>>();

            delta[initialState] = new List<ExtendedNormalMove<S>>();
            deltaInv[initialState] = new List<ExtendedNormalMove<S>>();
            deltaFinal[initialState] = new List<ExtendedFinalMove<S>>();

            int maxState = initialState;
            bool isDeterministic = true;
            foreach (ExtendedMove<S> move in moves)
            {
                if (!delta.ContainsKey(move.SourceState))
                    delta[move.SourceState] = new List<ExtendedNormalMove<S>>();
                if (!deltaInv.ContainsKey(move.SourceState))
                    deltaInv[move.SourceState] = new List<ExtendedNormalMove<S>>();
                if (!deltaFinal.ContainsKey(move.SourceState))
                    deltaFinal[move.SourceState] = new List<ExtendedFinalMove<S>>();

                maxState = Math.Max(maxState, move.SourceState);

                if (move.isFinal)
                {
                    var mv = move as ExtendedFinalMove<S>;
                    deltaFinal[move.SourceState].Add(mv);
                    finalMoves.Add(mv);
                }
                else
                {
                    var mv = move as ExtendedNormalMove<S>;
                    delta[mv.SourceState].Add(mv);
                    deltaInv[mv.TargetState].Add(mv);
                    maxState = Math.Max(maxState, mv.TargetState);
                }
            }

            ExtendedAutomaton<S> fsa = new ExtendedAutomaton<S>();
            fsa.initialState = initialState;
            fsa.maxState = maxState;
            fsa.delta = delta;
            fsa.deltaFinal = deltaFinal;
            fsa.deltaInv = deltaInv;
            fsa.finalMoves = finalMoves;
            fsa.isDeterministic = isDeterministic;
            if (eliminateUnrreachableStates)
                fsa.EliminateUnrreachableStates();
            if (eliminateDeadStates)
                fsa.EliminateDeadStates();
            return fsa;
        }

        private ExtendedAutomaton() { }


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

        public IEnumerable<ExtendedMove<S>> GetMovesFrom(int sourceState)
        {
            var output = new List<ExtendedMove<S>>();
            foreach (var v in delta[sourceState])
                output.Add(v);
            foreach (var v in deltaFinal[sourceState])
                output.Add(v);
            return output;
        }

        public IEnumerable<ExtendedNormalMove<S>> GetNormalMovesFrom(int sourceState)
        {
            return delta[sourceState];
        }

        public IEnumerable<ExtendedNormalMove<S>> GetNormalMovesTo(int sourceState)
        {
            return deltaInv[sourceState];
        }

        public IEnumerable<ExtendedFinalMove<S>> GetFinalMovesFrom(int sourceState)
        {
            return deltaFinal[sourceState];
        }

        public IEnumerable<ExtendedFinalMove<S>> GetFinalMoves()
        {
            return finalMoves;
        }

        public IEnumerable<ExtendedMove<S>> GetMovesFromStates(IEnumerable<int> sourceStates)
        {
            foreach (int s in sourceStates)
                foreach (var t in GetMovesFrom(s))
                    yield return t;
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
            foreach (var move in deltaFinal[state])
                finalMoves.Remove(move);

            deltaFinal.Remove(state);
            delta.Remove(state);
            deltaInv.Remove(state);
        }

        internal ExtendedAutomaton<S> MakeCopy(Func<int> NewStateId)
        {
            int newInitialState = NewStateId();
            Dictionary<int, int> stateRemap = new Dictionary<int, int>();
            stateRemap[initialState] = newInitialState;
            List<ExtendedMove<S>> moves = new List<ExtendedMove<S>>();
            foreach (var move in GetMoves())
            {
                int s;
                int t;
                if (!stateRemap.TryGetValue(move.SourceState, out s))
                {
                    s = NewStateId();
                    stateRemap[move.SourceState] = s;
                }
                if (move is ExtendedFinalMove<S>)
                {
                    moves.Add(new ExtendedFinalMove<S>(s, move.action));
                }
                else
                {
                    var normove = move as ExtendedNormalMove<S>;
                    if (!stateRemap.TryGetValue(normove.TargetState, out t))
                    {
                        t = NewStateId();
                        stateRemap[normove.TargetState] = t;
                    }
                    moves.Add(new ExtendedNormalMove<S>(s,t, move.action));
                }
            }   
            return Create(newInitialState, moves);    
        }

        /// <summary>
        /// Get the condition from the move from source to target
        /// </summary>
        internal ExtendedAction<S> GetAction(int source, int target)
        {
            foreach (var move in delta[source])
                if (move.TargetState == target)
                    return move.action;
            throw new AutomataException(AutomataExceptionKind.InternalError);
        }


        /// <summary>
        /// Eliminate all non-initial states from the automaton from which no final state is recahable 
        /// </summary>
        public void EliminateDeadStates()
        {
            //backwards reachability, collect states that reach some final state
            var finalStates = this.deltaFinal.Keys;
            Stack<int> stack = new Stack<int>(finalStates);
            HashSet<int> backReachableFromSomeFinal = new HashSet<int>(finalStates);
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
        /// Checks that for all states q, if q has two or more outgoing 
        /// moves then the conditions of the moves are pairwise disjoint, i.e., 
        /// their conjunction is unsatisfiable. 
        /// If the check succeeds, sets IsDeterministic to true.
        /// Throws AutomataException if the FSA is not epsilon-free.
        /// </summary>
        /// <param name="solver">used to make conjunctions and to check satisfiability of resulting conditions</param>
        public void CheckDeterminism(IBooleanAlgebraPositive<S> solver, bool resetIsDeterministicToFalse = false)
        {
            if (resetIsDeterministicToFalse)
                isDeterministic = false;

            if (isDeterministic) //already known to be deterministic
                return;

            foreach (int state in States)
            {
                //Normal Moves
                var moves = delta[state];
                int k = moves.Count;
                if (k > 1)
                {
                    S sofar = moves[0].action.guard;
                    for (int i = 1; i < k; i++)
                    {
                        var sofar_and_i = solver.MkAnd(sofar, moves[i].action.guard);
                        if (solver.IsSatisfiable(sofar_and_i))
                        {
                            isDeterministic = false;
                            return; //nondeterministic
                        }

                        if (i < k - 1)
                            sofar = solver.MkOr(sofar, moves[i].action.guard);
                    }
                }

                //Final moves
                var finmoves = deltaFinal[state];
                k = finmoves.Count;
                if (k > 1)
                {
                    S sofar = finmoves[0].action.guard;
                    for (int i = 1; i < k; i++)
                    {
                        var sofar_and_i = solver.MkAnd(sofar, finmoves[i].action.guard);
                        if (solver.IsSatisfiable(sofar_and_i))
                        {
                            isDeterministic = false;
                            return; //nondeterministic
                        }

                        if (i < k - 1)
                            sofar = solver.MkOr(sofar, finmoves[i].action.guard);
                    }
                }

                //Fin vs nonfin
                foreach (var move in delta[state])
                    foreach (var finalMove in deltaFinal[state])
                        if (move.action.lookahead <= finalMove.action.lookahead) //Move does not have longer lookahead than fin move
                            if (solver.IsSatisfiable(solver.MkAnd(move.action.guard, finalMove.action.guard)))
                            {
                                isDeterministic = false;
                                return; //nondeterministic
                            }                        
            }
            isDeterministic = true; //deterministic
        }
        

        /// <summary>
        /// Returns a non-circular path from source to some final state.
        /// The path is empty if source is a final state.
        /// Only the last state of the returned path is a final state.
        /// May throw AutomataException if the automaton contains dead-states.
        /// </summary>
        /// <param name="source">a given source state</param>
        public List<ExtendedMove<S>> GetPathToSomeFinalState(int source)
        {
            List<ExtendedMove<S>> res = new List<ExtendedMove<S>>();
            HashSet<int> visited = new HashSet<int>();
            int state = source;
            visited.Add(state);
            while (!IsFinalState(state))
            {
                bool ok = false;
                foreach (var move in GetNormalMovesFrom(state))
                    if (!visited.Contains(move.TargetState))
                    {
                        res.Add(move);
                        ok = true;
                        state = move.TargetState;
                        visited.Add(state);
                        break;
                    }
                if (!ok)
                    throw new AutomataException(AutomataExceptionKind.AutomatonMustNotContainDeadStates);
            }
            //Add final move
            foreach (var move in GetFinalMovesFrom(state))
            {
                res.Add(move);
                break;
            }
            return res;
        }

        public bool AllStatesAreFinal()
        {
            foreach (int state in States)
                if (!IsFinalState(state))
                    return false;
            return true;
        }


        #region Accessory methods

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
            return deltaFinal[state].Count > 0;
        }

        /// <summary>
        /// Returns true iff there exists a move from state to state.
        /// </summary>
        public bool IsLoopState(int state)
        {
            foreach (var move in GetNormalMovesFrom(state))
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

        /// <summary>
        /// Enumerates all moves of the automaton.
        /// </summary>
        public IEnumerable<ExtendedMove<S>> GetMoves()
        {
            foreach (int state in States)
            {
                foreach (var move in delta[state])
                    yield return move;
                foreach (var move in deltaFinal[state])
                    yield return move;
            }
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
        public string DescribeLabel(S lab)
        {
            if (typeof(S).IsValueType)
                return lab.ToString();
            else if (object.Equals(lab, null))
                return "";
            else
                return lab.ToString();
        }
        public string DescribeStartLabel()
        {
            return "";
        }

        #endregion

    }
}

