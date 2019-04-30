using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Trans = System.Tuple<int, int>;
using Pair = System.Tuple<int, int>;

namespace Microsoft.Automata
{
    /// <summary>
    /// Symbolic Push Down Automaton
    /// </summary>
    internal class PushdownAutomaton<S,T> 
    {
        Automaton<PushdownLabel<S, T>> automaton;
        public List<S> stackSymbols;
        public S initialStackSymbol;

        /// <summary>
        /// All deadends and unreachable states are eliminated from the states and moves if cleanup is set to true
        /// </summary>
        public PushdownAutomaton(int initialState, List<int> states, List<int> finalstates,
            S initialStackSymbol, List<S> stackSymbols, 
            List<Move<PushdownLabel<S, T>>> moves, bool cleanup = false) : base()
        {
            this.automaton = Automaton<PushdownLabel<S, T>>.Create(null, initialState, finalstates, moves, cleanup, cleanup);
            this.stackSymbols = new List<S>(stackSymbols);
            this.initialStackSymbol = initialStackSymbol;
        }

        /// <summary>
        /// Assumes that the automaton is clean: has no unsatisfiable moves, no dead states, and no unreachable states
        /// </summary>
        public PushdownAutomaton(Automaton<PushdownLabel<S, T>> automaton, List<S> stackSymbols, S initialStackSymbol)
        {
            this.automaton = automaton;
            this.stackSymbols = stackSymbols;
            this.initialStackSymbol = initialStackSymbol;
        }

        internal class ReachabilityAutomaton
        {
            PushdownAutomaton<S, T> pda;
            internal Dictionary<Trans, Sequence<T>> witnessMap = new Dictionary<Trans, Sequence<T>>();
            internal Dictionary<S, HashSet<Trans>> pushMap = new Dictionary<S, HashSet<Trans>>();
            internal Dictionary<S, HashSet<Move<Sequence<T>>>> popMap = new Dictionary<S, HashSet<Move<Sequence<T>>>>();

            //set of epsilon transitions
            internal HashSet<Trans> epsilons = new HashSet<Trans>();
            //forward image of epsilons
            internal Dictionary<int, HashSet<int>> forward = new Dictionary<int, HashSet<int>>();
            //inverse image of epsilons
            internal Dictionary<int, HashSet<int>> inverse = new Dictionary<int, HashSet<int>>();

            //preinitial state that pushes the empty stack symbol from empty stack to pda.initialState
            internal int q0;
            internal HashSet<int> F = new HashSet<int>();
            internal HashSet<int> Q = new HashSet<int>();
            internal Tuple<int, int> start;

            int __newStateId;
            int GetNewStateId()
            {
                int q = __newStateId;
                __newStateId += 1;
                return q;
            }

            static Trans MkTrans(int x, int y)
            {
                return new Tuple<int, int>(x, y);
            }

            public ReachabilityAutomaton(PushdownAutomaton<S, T> pda, bool allstatesarepositive = false)
            {
                this.pda = pda;
                this.q0 = (allstatesarepositive ? 0 : pda.automaton.MaxState + 1);
                this.__newStateId = (allstatesarepositive ? pda.automaton.MaxState + 1 : pda.automaton.MaxState + 2);
                this.start = new Tuple<int, int>(this.q0, pda.automaton.InitialState);
                this.Q.Add(this.q0);
                this.Q.UnionWith(pda.automaton.States);
                this.F.UnionWith(pda.automaton.GetFinalStates());
                foreach (var s in pda.stackSymbols)
                {
                    pushMap[s] = new HashSet<Trans>();
                    popMap[s] = new HashSet<Move<Sequence<T>>>();
                }
                AddPush(this.start, pda.initialStackSymbol);

                #region  initialize transitions 
                foreach (int q in pda.automaton.States)
                    foreach (var move in pda.automaton.GetMovesFrom(q))
                    {
                        if (move.Label.PushSymbols.IsEmpty)
                        {
                            AddPop(q, move.TargetState, move.Label.PopSymbol, move.Label.Input);
                        }
                        else
                        {
                            int s = q;
                            int t = GetNewStateId();
                            this.Q.Add(t);
                            var symbs = move.Label.PushSymbols.ToArray();
                            S a = move.Label.PopSymbol;
                            var st = MkTrans(s, t);
                            //first add the pop move s --(a-)--> t to intermediate new state t
                            AddPop(s, t, a, move.Label.Input);
                            //next add the intermediate push symbols, starting from the last
                            for (int i = symbs.Length - 1; i > 0 ; i--)
                            {
                                s = t;
                                t = GetNewStateId();
                                this.Q.Add(t);
                                st = MkTrans(s, t);
                                AddPush(st, symbs[i]);
                            }
                            //add the top push symbol to the original target state of the move
                            AddPush(MkTrans(t, move.TargetState), symbs[0]);
                        }
                    }
                #endregion

                #region initialize epsilon relation as the initial reflexive relation over states
                foreach (int q in Q)
                {
                    var qq = new Trans(q, q);
                    var set1 = new HashSet<int>();
                    set1.Add(q);
                    forward[q] = set1;
                    var set2 = new HashSet<int>();
                    set2.Add(q);
                    inverse[q] = set2;
                    witnessMap[qq] = Sequence<T>.Empty;
                    epsilons.Add(qq);
                }
                #endregion
            }

            void AddPush(Trans transition, S label)
            {
                this.pushMap[label].Add(transition);
            }

            void AddPop(int source, int target, S label, T input)
            {
                var input_seq = (object.Equals(input, default(T)) ? Sequence<T>.Empty : new Sequence<T>(input));
                this.popMap[label].Add(Move<Sequence<T>>.Create(source, target, input_seq));
            }

            /// <summary>
            /// Algorithm 'saturate' in Figure 1 from Finkel-Willems-Wolper paper 
            /// 'A Direct Symbolic Approach to Model Checking PushDown Systems'
            /// </summary>
            /// <returns></returns>
            internal HashSet<Trans> Saturate()
            {
                var hash = new HashSet<Trans>();
                var stack = new SimpleStack<Trans>();
                foreach (var q in this.Q)
                    stack.Push(MkTrans(q,q));

                var direct = new Dictionary<Trans, SimpleStack<Trans>>();
                var trans = new Dictionary<Trans, SimpleStack<Tuple<Trans, Trans>>>();

                Func<Trans, SimpleStack<Trans>> Direct = (t) =>
                    {
                        SimpleStack<Trans> t_stack;
                        if (!direct.TryGetValue(t, out t_stack))
                        {
                            t_stack = new SimpleStack<Trans>();
                            direct[t] = t_stack;
                        }
                        return t_stack;
                    };

                Func<Trans, SimpleStack<Tuple<Trans, Trans>>> Transfer = (t) =>
                {
                    SimpleStack<Tuple<Trans, Trans>> t_trans;
                    if (!trans.TryGetValue(t, out t_trans))
                    {
                        t_trans = new SimpleStack<Tuple<Trans, Trans>>();
                        trans[t] = t_trans;
                    }
                    return t_trans;
                };

                foreach (var a in pda.stackSymbols)
                    foreach (var push in pushMap[a])
                        foreach (var pop in popMap[a])
                        {
                            //push_pop goes from target of push to source of pop
                            var push_pop = MkTrans(push.Item2, pop.SourceState);
                            //edge goes from source of push to target of pop
                            var edge = MkTrans(push.Item1, pop.TargetState);
                            Direct(push_pop).Push(edge);
                        }

                foreach (var q in Q)
                    foreach (var p in Q)
                        if (p != q)
                        {
                            var qp = MkTrans(q, p);
                            var qp_trans = Transfer(qp);
                            foreach (var t in Q)
                            {
                                var pt = MkTrans(p, t);
                                var qt = MkTrans(q, t);
                                var pt_qt = new Tuple<Trans, Trans>(pt, qt);
                                var tq = MkTrans(t, q);
                                var tp = MkTrans(t, p);
                                var tq_tp = new Tuple<Trans, Trans>(tq, tp);
                                qp_trans.Push(pt_qt);
                                qp_trans.Push(tq_tp);
                            }
                        }

                while (stack.IsNonempty)
                {
                    var alpha = stack.Pop();
                    hash.Add(alpha);
                    stack.PushAll(direct[alpha]);
                    foreach (var pair in Transfer(alpha))
                    {
                        if (hash.Contains(pair.Item1))
                            stack.Push(pair.Item2);
                        else
                            Direct(pair.Item1).Push(pair.Item2);
                    }
                }

                return hash;
            }

            /// <summary>
            /// Returns witness of words accepted or null if the accepted language is empty.
            /// </summary>
            public Sequence<T> GetWitness()
            {
                #region compute the fixpoint of epsilon transitions together with input witnesses
                var iterate = true;
                while (iterate)
                {
                    iterate = false;
                    foreach (S a in pda.stackSymbols)
                    {
                        foreach (var push in pushMap[a])
                        {
                            foreach (var pop in popMap[a])
                            {
                                //push = (s --(a+)--> s_)
                                int s = push.Item1;
                                int s_ = push.Item2;
                                //pop = (t_ --(a-)--> t)
                                int t_ = pop.SourceState;
                                int t = pop.TargetState;

                                var s2t = MkTrans(s, t);
                                var s_2t_ = MkTrans(s_, t_);

                                if (!IsEpsilon(s2t) && IsEpsilon(s_2t_))
                                {
                                    //NOT(s --eps*--> t) but (s_ --eps*--> t_)
                                    //so: s --eps--> t because s --(a+)--> s_ --eps*--> t_ -(a-)--> t and (a+)(a-) = eps

                                    var s2t_witness = witnessMap[s_2t_].Append(pop.Label);

                                    AddNewEps(s2t, s2t_witness);
                                    //fix point was not reached yet
                                    iterate = true;
                                }
                            }
                        }
                    }
                }
                #endregion

                var reach_aut = Automaton<Sequence<T>>.Create(null, this.q0, this.F, EnumerateRelevantMoves(), true, true);
                if (reach_aut.IsEmpty)
                    return null;
                else
                {
                    var witness = Sequence<T>.AppendAll(reach_aut.FindShortestFinalPath(this.q0).Item1);
                    return witness;
                }
            }

            public IEnumerable<Move<Sequence<T>>> EnumerateRelevantMoves()
            {
                foreach (var entry in witnessMap)
                    if (entry.Key.Item1 != entry.Key.Item2) //ignore loops
                        if (!entry.Value.IsEmpty)
                        {
                            yield return Move<Sequence<T>>.Create(entry.Key.Item1, entry.Key.Item2, entry.Value);
                        }
                        else //represent empty sequences as epsilons
                        {
                            yield return Move<Sequence<T>>.Epsilon(entry.Key.Item1, entry.Key.Item2);
                        }
                foreach (var pushes in pushMap)
                    foreach (var trans in pushes.Value)
                        yield return Move<Sequence<T>>.Epsilon(trans.Item1, trans.Item2);
            }

            /// <summary>
            /// Add new epsilon and maintain its transitive closure.
            /// </summary>
            void AddNewEps(Trans st, Sequence<T> witness)
            {
                var source = st.Item1;
                var target = st.Item2;
                var Y = forward[target];
                var X = inverse[source];
                //add all transitions from X to Y
                var newEps = new List<Trans>();
                foreach (var x in X)
                    foreach (var y in Y)
                    {
                        var xy = MkTrans(x, y);
                        if (epsilons.Add(xy))
                        {
                            var xy_witness = witnessMap[MkTrans(x, source)].Append(witness).Append(witnessMap[MkTrans(target, y)]);
                            witnessMap[xy] = xy_witness;
                            newEps.Add(xy);
                        }
                    }
                foreach (var eps in newEps)
                {
                    forward[eps.Item1].Add(eps.Item2);
                    inverse[eps.Item2].Add(eps.Item1);
                }
            }

            bool IsEpsilon(Trans trans)
            {
                return (epsilons.Contains(trans));
            }

            public void ShowPushPopGraph(string name = "PushPopGraph", bool includeNontrivialEpsilons = false, bool showEpsilonsOnly = false)
            {
                var aut = Automaton<string>.Create(null, q0, F, ShowPushPopGraph_EnumerateMoves(includeNontrivialEpsilons, showEpsilonsOnly));
                aut.ShowGraph(name);
            }

            IEnumerable<Move<string>> ShowPushPopGraph_EnumerateMoves(bool includeNontrivialEpsilons, bool showEpsilonsOnly)
            {
                if (!showEpsilonsOnly)
                {
                    foreach (var entry in popMap)
                        foreach (var trans in entry.Value)
                            yield return Move<string>.Create(trans.SourceState, trans.TargetState, "-" + entry.Key.ToString() + "/" + trans.Label.ToString());
                    foreach (var entry in pushMap)
                        foreach (var trans in entry.Value)
                            yield return Move<string>.Create(trans.Item1, trans.Item2, "+" + entry.Key.ToString());
                }
                if (includeNontrivialEpsilons)
                    foreach (var trans in epsilons)
                        if (trans.Item1 != trans.Item2)
                            yield return Move<string>.Create(trans.Item1, trans.Item2, "/"+ witnessMap[trans].ToString());

            }
        }

        internal bool IsNonempty_(out T[] witness)
        {
            //we know that the initial state is 1
            //and all states are enumerated from 1 upwards
            return IsNonempty_helper(out witness, true);
        }

        public bool IsNonempty(out T[] witness)
        {
            return IsNonempty_helper(out witness, false);
        }

        bool IsNonempty_helper(out T[] witness, bool canonical)
        {
            if (this.automaton.IsEmpty)
            {
                witness = null;
                return false;
            }
            else
            {
                var ra = new ReachabilityAutomaton(this, canonical);
                var witness1 = ra.GetWitness();
                if (witness1 == null)
                {
                    witness = null;
                    return false;
                }
                else
                {
                    witness = witness1.ToArray();
                    return true;
                }
            }
        }

        public void ShowGraph(string name = "PDA")
        {
            automaton.ShowGraph(name);
        }

        /// <summary>
        /// Intersects this PDA with the given NFA. The NFA is assumed to be epsilon-free.
        /// The PDA may be arbitrary. The product PDA will have initial state 1 and all states 
        /// are enumerated consequtively from 1 upwards.
        /// </summary>
        public PushdownAutomaton<S, T> Intersect(Automaton<T> nfa)
        {
            //depth first product construction, PDA may have epsilon moves
            var stateIdMap = new Dictionary<Pair, int>();
            int nextStateId = 1;
            var stack = new SimpleStack<Pair>();
            var moves = new List<Move<PushdownLabel<S, T>>>();
            var states = new List<int>();
            var finalstates = new List<int>();

            #region  GetState: push the pair to stack if the state id was new and update final states as needed
            Func<Pair, int> GetState = (pair) =>
             {
                 int id;
                 if (!stateIdMap.TryGetValue(pair, out id))
                 {
                     id = nextStateId;
                     nextStateId += 1;
                     stateIdMap[pair] = id;
                     stack.Push(pair);
                     states.Add(id);
                     if (this.automaton.IsFinalState(pair.Item1) && nfa.IsFinalState(pair.Item2))
                     {
                         finalstates.Add(id);
                     }
                 }
                 return id;
             };
            #endregion

            var initPair = new Pair(this.automaton.InitialState, nfa.InitialState);
            int initState = GetState(initPair);

            #region compute the product transitions with depth-first search
            while (stack.IsNonempty)
            {
                var sourcePair = stack.Pop();
                var source = stateIdMap[sourcePair];
                var pda_state = sourcePair.Item1;
                var aut_state = sourcePair.Item2;
                foreach (var pda_move in this.automaton.GetMovesFrom(pda_state))
                {
                    if (pda_move.IsEpsilon || pda_move.Label.InputIsEpsilon)
                    {
                        var targetPair = new Pair(pda_move.TargetState, aut_state);
                        int target = GetState(targetPair);
                        moves.Add(Move<PushdownLabel<S, T>>.Create(source, target, pda_move.Label));
                    }
                    else 
                    {
                        //assuming here that the automaton does not have epsilons
                        foreach (var aut_move in nfa.GetMovesFrom(aut_state))
                        {
                            if (aut_move.IsEpsilon)
                                throw new AutomataException(AutomataExceptionKind.AutomatonIsNotEpsilonfree);

                            var cond = nfa.Algebra.MkAnd(aut_move.Label, pda_move.Label.Input);
                            //if the joint condition is not satisfiable then the 
                            //joint move does effectively not exist 
                            if (nfa.Algebra.IsSatisfiable(cond))
                            {
                                var targetPair = new Pair(pda_move.TargetState, aut_move.TargetState);
                                int target = GetState(targetPair);
                                var label = new PushdownLabel<S, T>(cond, pda_move.Label.PushAndPop);
                                var jointmove = Move<PushdownLabel<S, T>>.Create(source, target, label);
                                moves.Add(jointmove);
                            }
                        }
                    }
                }
            }
            #endregion

            //note: automaton creation eliminates unreachable states and deadend states from the product
            var productAutom = Automaton<PushdownLabel<S, T>>.Create(null, initState, finalstates, moves, true, true);
            var product = new PushdownAutomaton<S, T>(productAutom, this.stackSymbols, this.initialStackSymbol);
            return product;
        } 
    }
}
