using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Automata;

namespace Microsoft.Automata
{
    internal class ThreeAutomaton<S> : IAutomaton<S> 
    {
        private Dictionary<int, List<Move<S>>> delta;
        private Dictionary<int, List<Move<S>>> deltaInv;
        private int initialState;
        private HashSet<int> dontCareStateSet;
        private HashSet<int> rejectingStateSet;
        private HashSet<int> acceptingStateSet;
        private int maxState;
        IBooleanAlgebra<S> algebra;


        #region accessors
        /// <summary>
        /// Gets some final state of the automaton
        /// </summary>
        public int FinalState
        {
            get
            {
                foreach (int f in acceptingStateSet)
                    return f; //return some final state
                throw new AutomataException(AutomataExceptionKind.NoFinalState);
            }
        }


        public int OutDegree(int state)
        {
            return delta[state].Count;
        }

        public int InDegree(int state)
        {
            return this.deltaInv[state].Count;
        }

        public Move<S> GetMoveFrom(int state)
        {
            return delta[state][0];
        }

        public Move<S> GetMoveTo(int state)
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

        /// <summary>
        /// True iff there are no final states
        /// </summary>
        public bool IsEmpty
        {
            get { return acceptingStateSet.Count == 0; }
        } 
        #endregion

        /// <summary>
        /// Return the approx of the min SFA accepting only the strings accepted by the 3SFA
        /// </summary>
        /// <returns></returns>
        public Automaton<S> GetApproxMinimalConsistentSFA(IBooleanAlgebra<S> solver, int numAttempts)
        {
            var min = this.Minimize(solver);
            Automaton<S> minDfa = min.GetBiggestLanguageSFA().Minimize();
            var minSt = minDfa.StateCount;

            Automaton<S> tmp = min.GetSmallestLanguageSFA().Minimize();
            if (tmp.StateCount < minSt)
                minDfa = tmp;

            Random r = new Random();
            for (int i = 0; i < numAttempts; i++)
            {
                var fin = new HashSet<int>(min.acceptingStateSet);
                foreach (var st in min.dontCareStateSet)
                    if (r.Next() % 2 == 0)
                        fin.Add(st);
                tmp = Automaton<S>.Create(solver, min.initialState, fin, min.GetMoves());
                if (tmp.StateCount < minSt)
                    minDfa = tmp;
            }
            return minDfa;
        }

        /// <summary>
        /// Return the SFA accepting only the strings accepted by the 3SFA
        /// </summary>
        /// <returns></returns>
        public Automaton<S> GetSmallestLanguageSFA()
        {
            return Automaton<S>.Create(this.algebra, initialState, acceptingStateSet, GetMoves());
        }

        /// <summary>
        /// Return the SFA rejecting only the strings rejected by the 3SFA
        /// </summary>
        /// <returns></returns>
        public Automaton<S> GetBiggestLanguageSFA()
        {
            List<int> finalStates = new List<int>(acceptingStateSet);
            finalStates.AddRange(dontCareStateSet);
            return Automaton<S>.Create(algebra, initialState, finalStates, GetMoves());
        }

        /// <summary>
        /// Create a three symbolic automaton.
        /// </summary>
        /// <param name="initialState">initial state</param>
        /// <param name="acceptingStates">final states</param>
        /// <param name="moves">moves</param>
        /// <returns></returns>
        public static ThreeAutomaton<S> Create(IBooleanAlgebra<S> algebra, int initialState, IEnumerable<int> rejectingStates, IEnumerable<int> acceptingStates, IEnumerable<Move<S>> moves)
        {
            var delta = new Dictionary<int, List<Move<S>>>();
            var deltaInv = new Dictionary<int, List<Move<S>>>();
            delta[initialState] = new List<Move<S>>();
            deltaInv[initialState] = new List<Move<S>>();
            int maxState = initialState;
            foreach (Move<S> move in moves)
            {
                if (move.IsEpsilon)
                    throw new AutomataException("Epsilon transitions not supported.");
                if (!delta.ContainsKey(move.SourceState))
                    delta[move.SourceState] = new List<Move<S>>();
                if (!delta.ContainsKey(move.TargetState))
                    delta[move.TargetState] = new List<Move<S>>();
                if (!deltaInv.ContainsKey(move.SourceState))
                    deltaInv[move.SourceState] = new List<Move<S>>();
                if (!deltaInv.ContainsKey(move.TargetState))
                    deltaInv[move.TargetState] = new List<Move<S>>();
                delta[move.SourceState].Add(move);
                deltaInv[move.TargetState].Add(move);
                maxState = Math.Max(maxState, Math.Max(move.SourceState, move.TargetState));
            }
            HashSet<int> acceptingStateSet = new HashSet<int>(acceptingStates);
            acceptingStateSet.RemoveWhere(x => !delta.ContainsKey(x)); //remove irrelevant states
            HashSet<int> rejectingStateSet = new HashSet<int>(rejectingStates);
            rejectingStateSet.RemoveWhere(x => !delta.ContainsKey(x));
            HashSet<int> dontCareStateSet = new HashSet<int>();
            foreach(var state in delta.Keys)
                if(!acceptingStateSet.Contains(state) && !rejectingStateSet.Contains(state))
                    dontCareStateSet.Add(state);


            ThreeAutomaton<S> fsa = new ThreeAutomaton<S>();
            fsa.algebra = algebra;
            fsa.initialState = initialState;
            fsa.acceptingStateSet = acceptingStateSet;
            fsa.rejectingStateSet = rejectingStateSet;
            fsa.dontCareStateSet = dontCareStateSet;

            fsa.maxState = maxState;
            fsa.delta = delta;
            fsa.deltaInv = deltaInv;
            //TODO add determinism check
            return fsa;
        }

        private ThreeAutomaton() { }

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

        public IEnumerable<int> GetFinalStates()
        {
            return acceptingStateSet;
        }

        public IEnumerable<int> GetNonFinalStates()
        {
            foreach (var s in GetStates())
                if (!IsFinalState(s))
                    yield return s;
        }

        public IEnumerable<Move<S>> GetMovesFrom(int sourceState)
        {
            return delta[sourceState];
        }

        public IEnumerable<Move<S>> GetEpsilonMovesFrom(int sourceState)
        {
            foreach (var m in delta[sourceState])
                if (m.IsEpsilon)
                    yield return m;
        }

        public IEnumerable<Move<S>> GetNonepsilonMovesFrom(int sourceState)
        {
            foreach (var m in delta[sourceState])
                if (!m.IsEpsilon)
                    yield return m;
        }

        public IEnumerable<Move<S>> GetMovesTo(int targetState)
        {
            return deltaInv[targetState];
        }

        public int GetMovesCountFrom(int sourceState)
        {
            return delta[sourceState].Count;
        }

        //assumes 0 <= n < GetMovesCountFrom(sourceState)
        public Move<S> GetNthMoveFrom(int sourceState, int n)
        {
            return delta[sourceState][n];
        }

        public IEnumerable<Move<S>> GetMovesFromStates(IEnumerable<int> sourceStates)
        {
            foreach (int s in sourceStates)
                foreach (Move<S> t in delta[s])
                    yield return t;
        }

        public bool HasSingleFinalSink
        {
            get
            {
                return acceptingStateSet.Count == 1 && delta[FinalState].Count == 0;
            }
        }

        //assumes that the new final states are indeed states of the current sfa
        internal void SetFinalStates(IEnumerable<int> newFinalStates)
        {
            this.acceptingStateSet = new HashSet<int>(newFinalStates);
        }

        /// <summary>
        /// Mark the initial state as a final state.
        /// </summary>
        public void MakeInitialStateFinal()
        {
            acceptingStateSet.Add(initialState);
        }

        /// <summary>
        /// Add a new move.
        /// </summary>
        /// <param name="move">move to be added</param>
        public void AddMove(Move<S> move)
        {
            if (!delta.ContainsKey(move.SourceState))
                delta[move.SourceState] = new List<Move<S>>();
            if (!deltaInv.ContainsKey(move.TargetState))
                deltaInv[move.TargetState] = new List<Move<S>>();
            delta[move.SourceState].Add(move);
            deltaInv[move.TargetState].Add(move);
            maxState = Math.Max(maxState, Math.Max(move.SourceState, move.TargetState));
        }


        public int MkNewFinalState(bool removeOldFinalStates = false)
        {
            if (removeOldFinalStates)
                acceptingStateSet.Clear();
            maxState += 1;
            acceptingStateSet.Add(maxState);
            this.delta[maxState] = new List<Move<S>>();
            this.deltaInv[maxState] = new List<Move<S>>();
            return maxState;
        }

        internal ThreeAutomaton<S> MakeCopy(Func<int> NewStateId)
        {
            int newInitialState = NewStateId();
            Dictionary<int, int> stateRemap = new Dictionary<int, int>();
            stateRemap[initialState] = newInitialState;
            List<Move<S>> moves = new List<Move<S>>();

            HashSet<int> newRejectingStates = new HashSet<int>();
            HashSet<int> newAcceptingStates = new HashSet<int>();
            HashSet<int> newDontCareStates = new HashSet<int>();

            foreach (Move<S> move in GetMoves())
            {
                int s;
                int t;
                if (!stateRemap.TryGetValue(move.SourceState, out s))
                {
                    s = NewStateId();
                    stateRemap[move.SourceState] = s;
                    if (acceptingStateSet.Contains(move.SourceState))
                        newAcceptingStates.Add(s);
                    else
                        if (rejectingStateSet.Contains(move.SourceState))
                            newRejectingStates.Add(s);
                }
                if (!stateRemap.TryGetValue(move.TargetState, out t))
                {
                    t = NewStateId();
                    stateRemap[move.TargetState] = t;
                    if (acceptingStateSet.Contains(move.TargetState))
                        newAcceptingStates.Add(t);
                    else
                        if (rejectingStateSet.Contains(move.TargetState))
                            newRejectingStates.Add(s);
                }
                moves.Add(Move<S>.Create(s, t, move.Label));
            }
            if (acceptingStateSet.Contains(initialState))
                newAcceptingStates.Add(newInitialState);
            return Create(algebra, newInitialState, newRejectingStates, newAcceptingStates, moves);
        }

        #region Product construction

        /// <summary>
        /// Make a product of a and b. First removes epsilons from a and b.
        /// </summary>
        /// <param name="a">first SFA of the product</param>
        /// <param name="b">second SFA of the product</param>
        /// <param name="conj">make the conjunction of two conditions</param>
        /// <param name="disj">make the disjunction of two conditions, used during initial epsilon elimination</param>
        /// <param name="isSat">check if a condition is satisfiable, used to keep the result clean</param>
        /// <returns>the product SFA of a and b</returns>
        public static ThreeAutomaton<S> MkProduct(ThreeAutomaton<S> a, ThreeAutomaton<S> b, IBooleanAlgebra<S> solver)
        {
            return MkProduct(a, b, solver, true);
        }

        public static ThreeAutomaton<S> MkProduct(ThreeAutomaton<S> aut1, ThreeAutomaton<S> aut2, IBooleanAlgebra<S> solver, bool inters)
        {
            var a = aut1.MakeTotal();
            var b = aut2.MakeTotal();

            var stateIdMap = new Dictionary<Tuple<int, int>, int>();
            var initPair = new Tuple<int, int>(a.InitialState, b.InitialState);
            var frontier = new Stack<Tuple<int, int>>();
            frontier.Push(initPair);
            stateIdMap[initPair] = 0;

            var delta = new Dictionary<int, List<Move<S>>>();
            delta[0] = new List<Move<S>>();
            var states = new List<int>();
            states.Add(0);

            var accStates = new List<int>();
            var rejStates = new List<int>();

            if (inters)
            {
                if (a.IsFinalState(a.InitialState) && b.IsFinalState(b.InitialState))
                    accStates.Add(0);
                else
                    if (a.IsRejectingState(a.InitialState) || b.IsRejectingState(b.InitialState))
                        rejStates.Add(0);
            }
            else
            {
                if (a.IsRejectingState(a.InitialState) && b.IsRejectingState(b.InitialState))
                    rejStates.Add(0);
                else
                    if (a.IsFinalState(a.InitialState) || b.IsFinalState(b.InitialState))
                        accStates.Add(0);
            }

            int n = 1;
            while (frontier.Count > 0)
            {
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
                            delta[target] = new List<Move<S>>();
                            frontier.Push(targetPair);

                            if (inters)
                            {
                                if (a.IsFinalState(t1.TargetState) && b.IsFinalState(t2.TargetState))
                                    accStates.Add(target);
                                else
                                    if (a.IsRejectingState(t1.TargetState) || b.IsRejectingState(t2.TargetState))
                                        rejStates.Add(target);
                            }
                            else
                            {
                                if (a.IsRejectingState(t1.TargetState) && b.IsRejectingState(t2.TargetState))
                                    rejStates.Add(target);
                                else
                                    if (a.IsFinalState(t1.TargetState) || b.IsFinalState(t2.TargetState))
                                        accStates.Add(target);
                            }

                            
                        }
                        outTransitions.Add(Move<S>.Create(source, target, cond));
                    }
            }

            var incomingTransitions = new Dictionary<int, List<Move<S>>>();
            foreach (int state in states)
                incomingTransitions[state] = new List<Move<S>>();
            foreach (int state in states)
                foreach (Move<S> t in delta[state])
                    incomingTransitions[t.TargetState].Add(t);


            return ThreeAutomaton<S>.Create(aut1.algebra, 0, rejStates, accStates, EnumerateMoves(delta));            
        }

        private static IEnumerable<Move<S>> EnumerateMoves(Dictionary<int, List<Move<S>>> delta)
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
        public static ThreeAutomaton<S> MkSum(ThreeAutomaton<S> a, ThreeAutomaton<S> b, IBooleanAlgebra<S> solver)
        {
            return MkProduct(a, b, solver, false);
        }
        #endregion


        public bool IsSound(Automaton<S> l1, Automaton<S> l2, IBooleanAlgebra<S> solver)
        {
            if (!l1.Minus(GetSmallestLanguageSFA()).IsEmpty)
                return false;
            return GetBiggestLanguageSFA().Minus(l2.Complement()).IsEmpty;
        }

        public bool IsComplete(Automaton<S> l1, Automaton<S> l2, IBooleanAlgebra<S> solver)
        {
            if (!GetSmallestLanguageSFA().Minus(l1).IsEmpty)
                return false;
            return l2.Complement().Minus(GetBiggestLanguageSFA()).IsEmpty;
        }

        /// <summary>
        /// Creates the automaton that accepts the complement of L(this).
        /// </summary>
        /// <param name="solver">boolean algebra solver over S</param>
        public ThreeAutomaton<S> Complement()
        {
            return MkComplement();
        }

        /// <summary>
        /// Creates the automaton that accepts the intersection of L(this) and L(B).
        /// </summary>
        /// <param name="B">another automaton</param>
        /// <param name="solver">boolean algebra solver over S</param>
        public ThreeAutomaton<S> Intersect(ThreeAutomaton<S> B, IBooleanAlgebra<S> solver)
        {
            return MkProduct(this, B, solver);
        }

        /// <summary>
        /// Creates the automaton that accepts the union of L(this) and L(B).
        /// Uses additional epsilon transitions and does not need the solver for S.
        /// </summary>
        /// <param name="B">another automaton</param>
        public ThreeAutomaton<S> Union(ThreeAutomaton<S> B, IBooleanAlgebra<S> solver)
        {
            return MkSum(this, B, solver);
        }        

        /// <summary>
        /// Returns true iff this automaton and another automaton B are equivalent
        /// </summary>
        /// <param name="B">another autonmaton</param>
        public bool IsEquivalentWith(ThreeAutomaton<S> B, IBooleanAlgebra<S> solver)
        {
            Automaton<S> accA = Automaton<S>.Create(algebra, this.initialState, this.acceptingStateSet, this.GetMoves());
            Automaton<S> accB = Automaton<S>.Create(algebra, B.initialState, B.acceptingStateSet, B.GetMoves());
            if (!accA.IsEquivalentWith(accB))
                return false;

            Automaton<S> rejA = Automaton<S>.Create(algebra, this.initialState, this.rejectingStateSet, this.GetMoves());
            Automaton<S> rejB = Automaton<S>.Create(algebra,  B.initialState, B.rejectingStateSet, B.GetMoves());
            return rejA.IsEquivalentWith(rejB);
        }       

        /// <summary>
        /// The sink state will be the state with the largest id.
        /// </summary>
        public ThreeAutomaton<S> MakeTotal()
        {
            var aut = this;

            int deadState = aut.maxState + 1;

            var newMoves = new List<Move<S>>();
            foreach (int state in aut.States)
            {
                var cond = algebra.MkNot(algebra.MkOr(aut.EnumerateConditions(state)));
                if (algebra.IsSatisfiable(cond))
                    newMoves.Add(Move<S>.Create(state, deadState, cond));
                    
            }
            if (newMoves.Count == 0)
                return this;

            newMoves.Add(Move<S>.Create(deadState, deadState, algebra.True));
            newMoves.AddRange(GetMoves());

            return ThreeAutomaton<S>.Create(algebra, aut.initialState, aut.rejectingStateSet, aut.acceptingStateSet, newMoves);
        }

        /// <summary>
        /// Make a complement of the automaton.
        /// Assumes that the automaton is deterministic, otherwise throws AutomataException.
        /// </summary>
        /// <param name="solver">solver for character constraints</param>
        /// <returns>Complement of this automaton</returns>
        public ThreeAutomaton<S> MkComplement()
        {
            return ThreeAutomaton<S>.Create(algebra, initialState, acceptingStateSet, rejectingStateSet, this.GetMoves());
        }

        private IEnumerable<S> EnumerateConditions(int sourceState)
        {
            foreach (var move in delta[sourceState])
                yield return move.Label;
        }

        #region timeout check
        static void CheckTimeout(long timeoutLimit)
        {
            if (timeoutLimit > 0)
            {
                if (Utilities.HighTimer.Now > timeoutLimit)
                    throw new TimeoutException();
            }
        }
        #endregion

        public IEnumerable<Move<S>> GetWitness()
        {
            if (IsEmpty)
                throw new AutomataException(AutomataExceptionKind.AutomatonMustBeNonempty);

            var witnessTree = new Dictionary<int, Move<S>>();
            witnessTree[initialState] = null;
            var stack = new Stack<int>();
            stack.Push(initialState);

            int state = initialState;

            while (!acceptingStateSet.Contains(state))
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

            ConsList<Move<S>> path = null;
            while (witnessTree[state] != null)
            {
                path = new ConsList<Move<S>>(witnessTree[state], path);
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

        #region Minimization
        /// <summary>
        /// Minimization of FAs using a symbolic generalization of Moore's algorithm.
        /// This is a quadratic algorithm.
        /// </summary>
        public ThreeAutomaton<S> Minimize(IBooleanAlgebra<S> solver)
        {
            return MinimizeClassical(solver, 0);
        }
         
        /// <summary>
        /// Extension of standard minimization of FAs, use timeout.
        /// </summary>
        ThreeAutomaton<S> MinimizeClassical(IBooleanAlgebra<S> solver, int timeout)
        {
            var fa = this.MakeTotal();

            Equivalence E = new Equivalence();

            //initialize E, all nonfinal states are equivalent 
            //and all final states are equivalent and all dontcare are equivalent
            List<int> stateList = new List<int>(fa.States);
            for (int i = 0; i < stateList.Count; i++)
            {
                //E.Add(stateList[i], stateList[i]);
                for (int j = 0; j < stateList.Count; j++)
                {
                    int p = stateList[i];
                    int q = stateList[j];
                    bool pIsFinal = fa.IsFinalState(p);
                    bool qIsFinal = fa.IsFinalState(q);                    
                    if(pIsFinal == qIsFinal)
                        if(pIsFinal)
                            E.Add(p, q);
                        else{
                            bool pIsRej = fa.IsRejectingState(p);
                            bool qIsRej = fa.IsRejectingState(q);
                            if(pIsRej == qIsRej)
                                E.Add(p, q);
                        }
                }
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
                            if (pq.Item1 != pq.Item2 && AreDistinguishable(fa, E, pq, solver))
                            {
                                E.Remove(pq);
                                continueRefinement = true;
                            }
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
            Dictionary<Tuple<int, int>, S> combinedConditionMap = new Dictionary<Tuple<int, int>, S>();
            foreach (int state in fa.States)
            {
                int fromStateId = equivIdMap[state];
                foreach (Move<S> trans in fa.GetMovesFrom(state))
                {
                    int toStateId = equivIdMap[trans.TargetState];
                    S cond;
                    var p = new Tuple<int, int>(fromStateId, toStateId);
                    if (combinedConditionMap.TryGetValue(p, out cond))
                        combinedConditionMap[p] = solver.MkOr(cond, trans.Label);
                    else
                        combinedConditionMap[p] = trans.Label;
                }
            }

            //form the transitions of the mfa
            List<Move<S>> mfaTransitions = new List<Move<S>>();
            foreach (var kv in combinedConditionMap)
                mfaTransitions.Add(Move<S>.Create(kv.Key.Item1, kv.Key.Item2, kv.Value));


            //accepting states and rejecting
            HashSet<int> mfaAccStates = new HashSet<int>();
            HashSet<int> mfaRejStates = new HashSet<int>();
            foreach (int state in acceptingStateSet)
                mfaAccStates.Add(equivIdMap[state]);
            foreach (int state in rejectingStateSet)
                mfaRejStates.Add(equivIdMap[state]);

            return ThreeAutomaton<S>.Create(algebra, mfaInitialState, mfaRejStates, mfaAccStates, mfaTransitions);
        }

        private S MkComplementCondition(IEnumerable<Move<S>> list, IBooleanAlgebra<S> solver)
        {
            List<S> conds = new List<S>();
            foreach (var t in list)
                conds.Add(solver.MkNot(t.Label));
            return solver.MkAnd(conds.ToArray());
        }

        private bool AreDistinguishable(ThreeAutomaton<S> fa, Equivalence E, Tuple<int, int> pq, IBooleanAlgebraPositive<S> solver)
        {
            foreach (Move<S> from_p in fa.GetMovesFrom(pq.Item1))
                foreach (Move<S> from_q in fa.GetMovesFrom(pq.Item2))
                    if (from_p.TargetState != from_q.TargetState &&
                        !E.AreEquiv(from_p.TargetState, from_q.TargetState))
                        if (solver.IsSatisfiable(solver.MkAnd(from_p.Label, from_q.Label)))
                            return true;
            return false;
        }

        #endregion

        /// <summary>
        /// Returns a non-circular path from source to some final state.
        /// The path is empty if source is a final state.
        /// Only the last state of the returned path is a final state.
        /// May throw AutomataException if the automaton contains dead-states.
        /// </summary>
        /// <param name="source">a given source state</param>
        public List<Move<S>> GetPathToSomeFinalState(int source)
        {
            List<Move<S>> res = new List<Move<S>>();
            HashSet<int> visited = new HashSet<int>();
            int state = source;
            visited.Add(state);
            while (!IsFinalState(state))
            {
                bool ok = false;
                foreach (var move in GetMovesFrom(state))
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
            return res;
        }

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
        public IEnumerable<S> ChoosePathToSomeFinalState(Chooser chooser)
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

        #region IThreeAutomaton<S> Members

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
            return acceptingStateSet.Contains(state);
        }

        /// <summary>
        /// Returns true iff the state is a final state of the automaton.
        /// </summary>
        public bool IsRejectingState(int state)
        {
            return rejectingStateSet.Contains(state);
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

        /// <summary>
        /// Enumerates all moves of the automaton.
        /// </summary>
        public IEnumerable<Move<S>> GetMoves()
        {
            foreach (int state in States)
                foreach (Move<S> move in delta[state])
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
        public string DescribeLabel(S lab)
        {
            if (typeof(S).IsValueType)
                return lab.ToString();
            else if (object.Equals(lab, null))
                return "";
            else
                return lab.ToString();
        }

        #endregion

        #region IThreeAutomaton<S> Members


        public string DescribeStartLabel()
        {
            return "";
        }

        #endregion



        public IBooleanAlgebra<S> Algebra
        {
            get { return algebra; }
        }
    }
}

