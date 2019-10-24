using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{

    public partial class CountingAutomaton<S> : Automaton<Tuple<Maybe<S>, Sequence<CounterOperation>>>
    {
        /// <summary>
        /// Maps a counting state to its associated counter
        /// </summary>
        internal Dictionary<int, ICounter> countingStates;
        /// <summary>
        /// Maps a state to the underlying regex AST node
        /// </summary>
        Dictionary<int, SymbolicRegexNode<S>> stateMap;
        /// <summary>
        /// Array of all counters, counter with identifier i is the element counters[i]
        /// </summary>
        internal ICounter[] counters;

        internal ICharAlgebra<S> solver;

        internal CountingAutomaton(Automaton<Tuple<Maybe<S>, Sequence<CounterOperation>>> aut,
            Dictionary<int, SymbolicRegexNode<S>> stateMap, Dictionary<int, ICounter> countingStates) : base(aut)
        {
            this.countingStates = countingStates;
            this.stateMap = stateMap;
            this.solver = ((CABA<S>)Algebra).builder.solver;
            //countingStates only defined in monadic case
            if (countingStates != null)
            {
                this.counters = new ICounter[countingStates.Count];
                foreach (var pair in countingStates)
                    counters[pair.Value.CounterId] = pair.Value;
            }
            else
            {
                var cntrsSet = new HashSet<ICounter>();
                foreach (var m in aut.GetMoves())
                    foreach (var c in m.Label.Item2)
                        cntrsSet.Add(c.Counter);
                this.counters = new ICounter[cntrsSet.Count];
                foreach (var c in cntrsSet)
                    this.counters[c.CounterId] = c;
            }
        }

        /// <summary>
        /// Gets the number of counters.
        /// All counters are numbered from 0 to NrOfCounters-1.
        /// </summary>
        public int NrOfCounters
        {
            get
            {
                return counters.Length;
            }
        }

        /// <summary>
        /// Returns the final state condition of a final state. 
        /// </summary>
        public Sequence<CounterOperation> GetFinalStateCondition(int state)
        {
            if (!IsFinalState(state))
                throw new AutomataException(AutomataExceptionKind.ArgumentMustBeFinalState);

            if (countingStates.ContainsKey(state) && countingStates[state].LowerBound > 0)
            {
                return new Sequence<CounterOperation>(new CounterOperation(countingStates[state], CounterOp.EXIT));
            }
            else
                return Sequence<CounterOperation>.Empty;
        }

        public override string DescribeState(int state)
        {
            string s;
            if (__hideDerivativesInViewer)
                s = SpecialCharacters.q(state);
            else
                s = stateMap[state].ToString();
            if (countingStates != null)
            {
                if (IsFinalState(state) && IsCountingState(state))
                {
                    s += "\n(";
                    var f = GetFinalStateCondition(state);
                    for (int i = 0; i < f.Length; i++)
                    {
                        if (i > 0)
                            s += SpecialCharacters.AND;
                        var op = f[i];
                        s += op.ToString();
                    }
                    s += ")" + SpecialCharacters.CHECKMARK;
                }
                else if (IsCountingState(state))
                    s += "\n(" + SpecialCharacters.c(countingStates[state].CounterId) + ")";
            }
            return s;
        }

        public override string DescribeStartLabel()
        {
            if (IsCountingState(InitialState))
            {
                var c = countingStates[InitialState];
                return string.Format("{0}{1}0", SpecialCharacters.c(c.CounterId),SpecialCharacters.ASSIGN);
            }
            else
                return "";
        }
        
        public override string DescribeLabel(Tuple<Maybe<S>, Sequence<CounterOperation>> lab)
        {
            string s = "";
            if (lab.Item1 != Maybe<S>.Nothing)
            {
                s += lab.Item1.Element.ToString();
                s += SpecialCharacters.MIDDOT;
            }
            if (lab.Item2.Length > 1)
                s += lab.Item2.ToString();
            else if (lab.Item2.Length == 1)
                s += lab.Item2[0].ToString();
            return s;
        }

        /// <summary>
        /// Returns true if q is a counting-state (q is associated with a counter)
        /// </summary>
        /// <param name="q">given state</param>
        public bool IsCountingState(int q)
        {
            if (countingStates == null)
                return false;
            else
                return countingStates.ContainsKey(q);
        }

        Dictionary<int, bool> IsSingletonCountingState_result = new Dictionary<int, bool>();
        /// <summary>
        /// Returns true if q is a counting-state that only needs one copy of the counter.
        /// A sufficient condition is when no incoming transition overlaps with any loop.
        /// </summary>
        /// <param name="q">given state</param>
        public bool IsSingletonCountingState(int q)
        {
            bool res;
            if (!IsSingletonCountingState_result.TryGetValue(q, out res))
            {
                S loopCond = solver.False;
                foreach (var loop in GetMovesFrom(q))
                {
                    if (loop.IsSelfLoop && loop.Label.Item1.IsSomething)
                    {
                        loopCond = solver.MkOr(loopCond, loop.Label.Item1.Element);
                    }
                }
                res = true;
                foreach (var trans in GetMovesTo(q))
                {
                    if (!trans.IsSelfLoop)
                    {
                        if (solver.IsSatisfiable(solver.MkAnd(trans.Label.Item1.Element, loopCond)))
                        {
                            res = false;
                            break;
                        }
                    }
                }
                IsSingletonCountingState_result[q] = res;
                return res;
            }
            return res;
        }

        /// <summary>
        /// Returns the counter associated with the state q.
        /// The state q must be a couting state.
        /// </summary>
        /// <param name="q">given counting state</param>
        public ICounter GetCounter(int q)
        {
            return countingStates[q];
        }

        /// <summary>
        /// Returns the counter with the given id
        /// </summary>
        /// <param name="q">given counter id</param>
        public ICounter GetCounterWithId(int counterId)
        {
            return counters[counterId];
        }

        /// <summary>
        /// Returns true if q is a counting state and outputs the counter of q.
        /// Returns false otherwise and sets counter to null.
        /// </summary>
        public bool TryGetCounter(int q, out ICounter counter)
        {
            return countingStates.TryGetValue(q, out counter);
        }

        public override IEnumerable<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>> GetMoves()
        {
            //provide consolidated view of moves
            var guardMap = new Dictionary<Tuple<int, int, Sequence<CounterOperation>>, S>();
            foreach (var move in base.GetMoves())
            {
                if (move.Label.Item1.IsSomething)
                {
                    var key = new Tuple<int, int, Sequence<CounterOperation>>(move.SourceState, move.TargetState, move.Label.Item2);
                    S guard;
                    if (guardMap.TryGetValue(key, out guard))
                        guardMap[key] = ((CABA<S>)(base.Algebra)).builder.solver.MkOr(move.Label.Item1.Element, guard);
                    else
                        guardMap[key] = move.Label.Item1.Element;
                }
            }
            foreach (var entry in guardMap)
                yield return Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>.Create(
                    entry.Key.Item1, entry.Key.Item2,
                    new Tuple<Maybe<S>, Sequence<CounterOperation>>(Maybe<S>.Something(entry.Value), entry.Key.Item3)
                    );
        }

        public override IEnumerable<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>> GetMovesFrom(int sourceState)
        {
            foreach (var move in base.GetMovesFrom(sourceState))
                if (move.Label.Item1.IsSomething)
                    yield return move;
        }

        public IEnumerable<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>> GetMovesFrom(int sourceState, S predicate)
        {
            var moves = base.delta[sourceState];
            for (int i=0; i < moves.Count; i++)
            {
                var move = moves[i];
                if (move.Label.Item1.IsSomething)
                {
                    if (solver.IsSatisfiable(solver.MkAnd(predicate, move.Label.Item1.Element)))
                        yield return move;
                }
            }
        }

        public bool HasMovesTo(int targetState, S predicate)
        {
            var moves = base.deltaInv[targetState];
            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                if (move.Label.Item1.IsSomething)
                {
                    if (solver.IsSatisfiable(solver.MkAnd(predicate, move.Label.Item1.Element)))
                        return true;
                }
            }
            return false;
        }

        public IEnumerable<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>> GetMovesFrom(IEnumerable<int> sourceStates, S minterm)
        {
            foreach (var sourceState in sourceStates)
                foreach (var move in base.GetMovesFrom(sourceState))
                    if (move.Label.Item1.IsSomething)
                    {
                        if (solver.IsSatisfiable(solver.MkAnd(minterm, move.Label.Item1.Element)))
                            yield return move;
                    }
        }


        bool __hideDerivativesInViewer = false;
        public void ShowGraph(string name = "CountingAutomaton", bool hideDerivatives = false)
        {
            __hideDerivativesInViewer = hideDerivatives;
            base.ShowGraph(name);
        }

        public void SaveGraph(string name = "CountingAutomaton", bool hideDerivatives = false)
        {
            __hideDerivativesInViewer = hideDerivatives;
            base.SaveGraph(name);
        }

        /// <summary>
        /// Returns true if the input string is accepted by this counting automaton
        /// </summary>
        /// <param name="input">given input string</param>
        public bool IsMatch(string input)
        {
            var charsolver = ((CABA<S>)Algebra).builder.solver;
            var cs = input.ToCharArray();
            S[] inputPreds = Array.ConvertAll(cs, c => charsolver.MkCharConstraint(c));

            //current state is a pair (Q, C)
            // Q = set of normal (noncounting) states
            // C = a dictionary of counting states to counting sets  

            var Q = new HashSet<int>();
            var C = new HashSet<int>();
            var counters = new BasicCountingSet[NrOfCounters];
            //create the counting sets
            foreach (var entry in countingStates)
                counters[entry.Value.CounterId] = new BasicCountingSet(entry.Value.UpperBound);

            //intialize the start state of the matcher
            if (IsCountingState(InitialState))
            {
                C.Add(InitialState);
                counters[countingStates[InitialState].CounterId].Set0();
            }
            else
                Q.Add(InitialState);

            //iterate over all elements in the input list
            //if at any point both Q and C are empty then the input is rejected
            int i = 0;
            while (i < inputPreds.Length && (C.Count > 0 || Q.Count > 0))
            {
                var a = inputPreds[i];
                i += 1;

                //construct the set of target states from Q
                var Q1 = new HashSet<int>();
                var C1 = new Dictionary<int, CounterOp>();

                foreach (var q in C)
                {
                    #region moves from counting-states q
                    var c = counters[countingStates[q].CounterId];
                    foreach (var move in GetMovesFrom(q, a))
                    {
                        if (IsCountingState(move.TargetState))
                        {
                            if (move.Label.Item2[0].OperationKind == CounterOp.INCR)
                            {
                                if (move.Label.Item2.Length > 1)
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                CounterOp op;
                                if (C1.TryGetValue(move.TargetState, out op))
                                    C1[move.TargetState] = op | CounterOp.INCR;
                                else
                                    C1[move.TargetState] = CounterOp.INCR;
                            }
                            else if (move.Label.Item2[0].OperationKind == CounterOp.EXIT)
                            {
                                if (move.Label.Item2.Length != 2)
                                    throw new AutomataException(AutomataExceptionKind.InternalError);
                                if (c.Max >= countingStates[q].LowerBound)
                                {
                                    if (move.Label.Item2[1].OperationKind == CounterOp.SET0)
                                    {
                                        CounterOp op;
                                        if (C1.TryGetValue(move.TargetState, out op))
                                            C1[move.TargetState] = op | CounterOp.SET0;
                                        else
                                            C1[move.TargetState] = CounterOp.SET0;
                                    }
                                    else
                                    {
                                        if (move.Label.Item2[1].OperationKind != CounterOp.SET1)
                                            throw new AutomataException(AutomataExceptionKind.InternalError);
                                        CounterOp op;
                                        if (C1.TryGetValue(move.TargetState, out op))
                                            C1[move.TargetState] = op | CounterOp.SET1;
                                        else
                                            C1[move.TargetState] = CounterOp.SET1;
                                    }
                                }
                            }
                            else if (move.Label.Item2[0].OperationKind == CounterOp.EXIT_SET0)
                            {
                                if (move.Label.Item2.Length != 1)
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                if (c.Max >= countingStates[q].LowerBound)
                                {
                                    CounterOp op;
                                    if (C1.TryGetValue(move.TargetState, out op))
                                        C1[move.TargetState] = op | CounterOp.SET0;
                                    else
                                        C1[move.TargetState] = CounterOp.SET0;
                                }
                            }
                            else if (move.Label.Item2[0].OperationKind == CounterOp.EXIT_SET1)
                            {
                                if (move.Label.Item2.Length != 1)
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                if (c.Max >= countingStates[q].LowerBound)
                                {
                                    CounterOp op;
                                    if (C1.TryGetValue(move.TargetState, out op))
                                        C1[move.TargetState] = op | CounterOp.SET1;
                                    else
                                        C1[move.TargetState] = CounterOp.SET1;
                                }
                            }
                            else
                            {
                                throw new AutomataException(AutomataExceptionKind.InternalError);
                            }
                        }

                        else
                        {
                            //if exiting the counting state q is possible then 
                            //add the target state as a reachable state
                            if (c.Max >= countingStates[q].LowerBound)
                                Q1.Add(move.TargetState);
                        }
                    }
                    #endregion
                }

                foreach (var q in Q)
                {
                    #region moves from non-counting-states q
                    foreach (var move in GetMovesFrom(q, a))
                    {
                        if (IsCountingState(move.TargetState))
                        {
                            if (move.Label.Item2.Length != 1)
                                throw new AutomataException(AutomataExceptionKind.InternalError);

                            if (!C1.ContainsKey(move.TargetState))
                                C1[move.TargetState] = move.Label.Item2.First.OperationKind;
                            else
                                C1[move.TargetState] = C1[move.TargetState] | move.Label.Item2.First.OperationKind;
                        }
                        else
                        {
                            if (move.Label.Item2.Length > 0)
                                throw new AutomataException(AutomataExceptionKind.InternalError);

                            Q1.Add(move.TargetState);
                        }
                    }
                    #endregion
                }

                Q = Q1;
                C.Clear();
                foreach (var entry in C1)
                {
                    #region update target registers and construct set of valid counting sets
                    var c = counters[GetCounter(entry.Key).CounterId];
                    if (entry.Value == CounterOp.INCR)
                    {
                        c.Incr();
                    }
                    else if (entry.Value == (CounterOp.INCR | CounterOp.SET0))
                    {
                        c.IncrPush0();
                    }
                    else if (entry.Value == (CounterOp.INCR | CounterOp.SET1))
                    {
                        c.IncrPush1();
                    }
                    else if (entry.Value == (CounterOp.INCR | CounterOp.SET0 | CounterOp.SET1))
                    {
                        c.IncrPush01();
                    }
                    else if (entry.Value == CounterOp.SET0)
                    {
                        c.Set0();
                    }
                    else if (entry.Value == CounterOp.SET1)
                    {
                        c.Set1();
                    }
                    else if (entry.Value == (CounterOp.SET0 | CounterOp.SET1))
                    {
                        c.Set1();
                        c.Push0();
                    }
                    else
                    {
                        throw new AutomataException(AutomataExceptionKind.InternalError);
                    }
                    if (!c.IsEmpty)
                    {
                        C.Add(entry.Key);
                    }
                    #endregion
                }
            }
            if (Q.Overlaps(GetFinalStates()))
                return true;
            else
            {
                foreach (var q in C)
                {
                    if (IsFinalState(q))
                    {
                        //check that the maximum value of the counter is at least the lower bound
                        if (counters[GetCounter(q).CounterId].Max >= GetCounter(q).LowerBound)
                            return true;
                    }
                }
                return false;
            }
        }

        private IEnumerable<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>> GetMovesFrom(List<int> states, S a, CsConditionSeq psi)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates counter minterms for the given set of counting states and input predicate.
        /// </summary>
        /// <param name="list">list of counting states, possibly empty i.e. null</param>
        /// <param name="a">input predicate</param>
        /// <returns></returns>
        private IEnumerable<CsConditionSeq> GenerateCounterMinterms(ConsList<int> list, S a)
        {
            if (list == null)
                yield return CsConditionSeq.MkTrue(countingStates.Count);
            else
            {
                var a_moves = new List<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>>(GetMovesFrom(list.First, a)).ToArray();


                var incr_exists = Array.Exists(a_moves, m => m.Label.Item2.First.OperationKind == CounterOp.INCR);
                var exit_exists = Array.Exists(a_moves, m => m.Label.Item2.First.OperationKind != CounterOp.INCR);
                var i = GetCounter(list.First).CounterId;

                foreach (var seq in GenerateCounterMinterms(list.Rest, a))
                {
                    if (a_moves.Length > 0)
                    {
                        if (incr_exists && exit_exists)
                        {
                            yield return seq.Update(i, CsCondition.LOW);
                            if (!(IsSingletonCountingState(list.First) && GetCounter(list.First).LowerBound == GetCounter(list.First).UpperBound))
                                yield return seq.Update(i, CsCondition.MIDDLE);
                            yield return seq.Update(i, CsCondition.HIGH);
                        }
                        else if (incr_exists)
                        {
                            yield return seq.Update(i, CsCondition.CANLOOP);
                        }
                        else if (exit_exists)
                        {
                            yield return seq.Update(i, CsCondition.CANEXIT);
                        }
                        else
                        {
                            throw new AutomataException(AutomataExceptionKind.InternalError_GenerateCounterMinterms);
                        }
                    }
                    else
                        yield return seq;
                }
            }
        }

        internal CsUpdateSeq  GetCounterUpdate(int q, S input, CsConditionSeq guard)
        {
            var res = CsUpdateSeq.MkNOOP(countingStates.Count);
            foreach (var mv in delta[q])
            {
                if (mv.Label.Item1.IsSomething && solver.IsSatisfiable(solver.MkAnd(mv.Label.Item1.Element, input)))
                {
                    var p = mv.TargetState;
                    if (IsCountingState(p))
                    {
                        var p_counter = GetCounter(p);
                        if (p == q)
                        {
                            #region loop
                            if (mv.Label.Item2.Length != 1)
                                throw new AutomataException(AutomataExceptionKind.InternalError);
                            else
                            {
                                var op = mv.Label.Item2[0];
                                if (guard[p_counter.CounterId].HasFlag(CsCondition.LOW) ||
                                    guard[p_counter.CounterId].HasFlag(CsCondition.MIDDLE) ||
                                    guard[p_counter.CounterId].HasFlag(CsCondition.HIGH))
                                {
                                    if (op.OperationKind == CounterOp.INCR)
                                    {
                                        res = res.Or(op.Counter.CounterId, CsUpdate.INCR);
                                    }
                                    else if (op.OperationKind == CounterOp.EXIT_SET0)
                                    {
                                        res = res.Or(op.Counter.CounterId, CsUpdate.SET0);
                                    }
                                    else if (op.OperationKind == CounterOp.EXIT_SET1)
                                    {
                                        res = res.Or(op.Counter.CounterId, CsUpdate.SET1);
                                    }
                                    else
                                    {
                                        throw new AutomataException(AutomataExceptionKind.InternalError);
                                    }
                                }
                            }
                            #endregion
                        }
                        else if (IsCountingState(q))
                        {
                            #region q is counting state too
                            var q_counter = GetCounter(q);
                            if (mv.Label.Item2.Length != 2)
                                throw new AutomataException(AutomataExceptionKind.InternalError);
                            else
                            {
                                var q_exit = mv.Label.Item2[0];
                                var p_set = mv.Label.Item2[1];
                                if (q_exit.Counter.CounterId != q_counter.CounterId || p_set.Counter.CounterId != p_counter.CounterId)
                                    throw new AutomataException(AutomataExceptionKind.InternalError);

                                if (guard[q_counter.CounterId].HasFlag(CsCondition.HIGH) ||
                                    guard[q_counter.CounterId].HasFlag(CsCondition.MIDDLE))
                                {
                                    if (p_set.OperationKind == CounterOp.SET0)
                                    {
                                        res = res.Or(p_set.Counter.CounterId, CsUpdate.SET0);
                                    }
                                    else if (p_set.OperationKind == CounterOp.SET1)
                                    {
                                        res = res.Or(p_set.Counter.CounterId, CsUpdate.SET1);
                                    }
                                    else 
                                    {
                                        throw new AutomataException(AutomataExceptionKind.InternalError);
                                    }
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            #region q is non-counting state
                            if (mv.Label.Item2.Length != 1)
                                throw new AutomataException(AutomataExceptionKind.InternalError);
                            else
                            {
                                var p_set = mv.Label.Item2[0];
                                if (p_set.Counter.CounterId != p_counter.CounterId)
                                    throw new AutomataException(AutomataExceptionKind.InternalError);


                                if (p_set.OperationKind == CounterOp.SET0)
                                {
                                    res = res.Or(p_set.Counter.CounterId, CsUpdate.SET0);
                                }
                                else if (p_set.OperationKind == CounterOp.SET1)
                                {
                                    res = res.Or(p_set.Counter.CounterId, CsUpdate.SET1);
                                }
                                else
                                {
                                    throw new AutomataException(AutomataExceptionKind.InternalError);
                                }
                            }
                            #endregion
                        }
                    }
                }
            }
            return res;
        }
    }

    /// <summary>
    /// Dummy Boolean algebra use only for customized pretty printing of CountingAutomaton transition labels
    /// </summary>
    internal class CABA<S> : IBooleanAlgebra<Tuple<Maybe<S>, Sequence<CounterOperation>>>, IPrettyPrinter<Tuple<Maybe<S>, Sequence<CounterOperation>>>
    {
        internal SymbolicRegexBuilder<S> builder;
        public CABA(SymbolicRegexBuilder<S> builder)
        {
            this.builder = builder;
        }

        public string PrettyPrint(Tuple<Maybe<S>, Sequence<CounterOperation>> t)
        {
            if (t.Item1.IsSomething)
            {
                if (t.Item2.Length > 0)
                {
                    char imp = SpecialCharacters.IMPLIES;
                    char prod = SpecialCharacters.MIDDOT;
                    if (t.Item2.Length == 1)
                    {
                        if (t.Item2[0].OperationKind == CounterOp.SET0 || t.Item2[0].OperationKind == CounterOp.SET1)
                            return builder.solver.PrettyPrint(t.Item1.Element) + imp + t.Item2[0].ToString();
                        else
                            return builder.solver.PrettyPrint(t.Item1.Element) + prod + t.Item2[0].ToString();
                    }
                    else
                        return builder.solver.PrettyPrint(t.Item1.Element) + prod + t.Item2.ToString();
                }
                else
                    return builder.solver.PrettyPrint(t.Item1.Element);
            }
            else
            {
                string s = "";
                for (int i=0; i < t.Item2.Length; i++)
                {
                    if (t.Item2[i].OperationKind != CounterOp.EXIT)
                        throw new AutomataException(AutomataExceptionKind.InternalError);

                    if (t.Item2[i].Counter.LowerBound > 0)
                    {
                        if (s != "")
                            s += SpecialCharacters.AND;
                        s += string.Format("{0}{1}{2}", SpecialCharacters.c(t.Item2[i].Counter.CounterId), SpecialCharacters.GEQ, t.Item2[i].Counter.LowerBound);
                    }
                }
                return (s == "" ? SpecialCharacters.TOP.ToString() : s) + SpecialCharacters.CHECKMARK;
            }
        }

        #region not implemented
        public Tuple<Maybe<S>, Sequence<CounterOperation>> False
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

        public Tuple<Maybe<S>, Sequence<CounterOperation>> True
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool AreEquivalent(Tuple<Maybe<S>, Sequence<CounterOperation>> predicate1, Tuple<Maybe<S>, Sequence<CounterOperation>> predicate2)
        {
            throw new NotImplementedException();
        }

        public bool CheckImplication(Tuple<Maybe<S>, Sequence<CounterOperation>> lhs, Tuple<Maybe<S>, Sequence<CounterOperation>> rhs)
        {
            throw new NotImplementedException();
        }

        public bool EvaluateAtom(Tuple<Maybe<S>, Sequence<CounterOperation>> atom, Tuple<Maybe<S>, Sequence<CounterOperation>> psi)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tuple<bool[], Tuple<Maybe<S>, Sequence<CounterOperation>>>> GenerateMinterms(params Tuple<Maybe<S>, Sequence<CounterOperation>>[] constraints)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> GetAtom(Tuple<Maybe<S>, Sequence<CounterOperation>> psi)
        {
            throw new NotImplementedException();
        }

        public bool IsSatisfiable(Tuple<Maybe<S>, Sequence<CounterOperation>> predicate)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> MkAnd(params Tuple<Maybe<S>, Sequence<CounterOperation>>[] predicates)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> MkAnd(IEnumerable<Tuple<Maybe<S>, Sequence<CounterOperation>>> predicates)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> MkAnd(Tuple<Maybe<S>, Sequence<CounterOperation>> predicate1, Tuple<Maybe<S>, Sequence<CounterOperation>> predicate2)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> MkDiff(Tuple<Maybe<S>, Sequence<CounterOperation>> predicate1, Tuple<Maybe<S>, Sequence<CounterOperation>> predicate2)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> MkNot(Tuple<Maybe<S>, Sequence<CounterOperation>> predicate)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> MkOr(IEnumerable<Tuple<Maybe<S>, Sequence<CounterOperation>>> predicates)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> MkOr(Tuple<Maybe<S>, Sequence<CounterOperation>> predicate1, Tuple<Maybe<S>, Sequence<CounterOperation>> predicate2)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> MkSymmetricDifference(Tuple<Maybe<S>, Sequence<CounterOperation>> p1, Tuple<Maybe<S>, Sequence<CounterOperation>> p2)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrint(Tuple<Maybe<S>, Sequence<CounterOperation>> t, Func<Tuple<Maybe<S>, Sequence<CounterOperation>>, string> varLookup)
        {
            throw new NotImplementedException();
        }

        public string PrettyPrintCS(Tuple<Maybe<S>, Sequence<CounterOperation>> t, Func<Tuple<Maybe<S>, Sequence<CounterOperation>>, string> varLookup)
        {
            throw new NotImplementedException();
        }

        public Tuple<Maybe<S>, Sequence<CounterOperation>> Simplify(Tuple<Maybe<S>, Sequence<CounterOperation>> predicate)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
