using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Automata
{

    public class CountingAutomaton<S> : Automaton<Tuple<Maybe<S>, Sequence<CounterOperation>>>
    {
        Dictionary<int, ICounter> countingStates;
        Dictionary<int, SymbolicRegexNode<S>> stateMap;

        internal CountingAutomaton(Automaton<Tuple<Maybe<S>, Sequence<CounterOperation>>> aut,
            Dictionary<int, SymbolicRegexNode<S>> stateMap, Dictionary<int, ICounter> countingStates) : base(aut)
        {
            this.countingStates = countingStates;
            this.stateMap = stateMap;
        }

        /// <summary>
        /// Gets the number of counters.
        /// </summary>
        public int NrOfCounters
        {
            get
            {
                return countingStates.Count;
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
                s = state.ToString();
            else
                s = stateMap[state].ToString();
            if (IsCountingState(state))
                s += "&#13;(c" + countingStates[state].CounterId.ToString() + ")";
            if (IsFinalState(state) && IsCountingState(state))
            {
                var f = GetFinalStateCondition(state);
                if (f.Length > 0)
                    s += f.ToString();
            }
            return s;
        }

        public override string DescribeStartLabel()
        {
            if (IsCountingState(InitialState))
            {
                var c = countingStates[InitialState];
                return string.Format("c{0}:=0", c.CounterId);
            }
            else
                return "";
        }

        public bool IsCountingState(int state)
        {
            return countingStates.ContainsKey(state);
        }

        public ICounter GetCounter(int state)
        {
            return countingStates[state];
        }

        public override IEnumerable<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>> GetMoves()
        {
            foreach (var move in base.GetMoves())
                if (move.Label.Item1.IsSomething)
                    yield return move;
        }

        public override IEnumerable<Move<Tuple<Maybe<S>, Sequence<CounterOperation>>>> GetMovesFrom(int sourceState)
        {
            foreach (var move in base.GetMovesFrom(sourceState))
                if (move.Label.Item1.IsSomething)
                    yield return move;
        }

        bool __hideDerivativesInViewer = false;
        public void ShowGraph(string name = "CountingAutomaton", bool hideDerivatives = false)
        {
            __hideDerivativesInViewer = hideDerivatives;
            base.ShowGraph(name);
        }
    }

    /// <summary>
    /// Dummy Boolean algebra use only for customized pretty printing of CountingAutomaton transition labels
    /// </summary>
    internal class CABA<S> : IBooleanAlgebra<Tuple<Maybe<S>, Sequence<CounterOperation>>>, IPrettyPrinter<Tuple<Maybe<S>, Sequence<CounterOperation>>>
    {
        SymbolicRegexBuilder<S> builder;
        public CABA(SymbolicRegexBuilder<S> builder)
        {
            this.builder = builder;
        }

        public string PrettyPrint(Tuple<Maybe<S>, Sequence<CounterOperation>> t)
        {
            if (t.Item1.IsSomething)
            {
                if (t.Item2.Length > 0)
                    return builder.solver.PrettyPrint(t.Item1.Element) + "/" + t.Item2.ToString();
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

                    if (t.Item2[i].Counter.LowerBound == t.Item2[i].Counter.UpperBound)
                    {
                        if (s != "")
                            s += " & ";
                        s += string.Format("c{0}=={1}", t.Item2[i].Counter.CounterId, t.Item2[i].Counter.LowerBound);
                    }
                    else if (t.Item2[i].Counter.LowerBound > 0)
                    {
                        if (s != "")
                            s += " & ";
                        s += string.Format("c{0}>={1}", t.Item2[i].Counter.CounterId, t.Item2[i].Counter.LowerBound);
                    }
                }
                return "F:" + (s == "" ? "true" : s);
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
