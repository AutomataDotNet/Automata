using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Automata.BooleanAlgebras;

namespace Microsoft.Automata
{
    /*
    /// <summary>
    /// Represents in-memory generated automaton from a .NET regex that is optimized for matching.
    /// </summary>
    public class RegexAutomaton : IAutomaton<BV>
    {
        /// <summary>
        /// Original regex from which this automaton was constructed.
        /// </summary>
        internal Regex regex;

        /// <summary>
        /// Symbols are in the range 0..K-1.
        /// </summary>
        public readonly int SymbolCount;

        /// <summary>
        /// States are in the range 0..StateCount-1.
        /// </summary>
        public readonly int StateCount;

        /// <summary>
        /// delta is an array of size &gt;= StateCount*SymbolCount.
        /// Transition from state p to state q on symbol x is q = delta[(p*SymbolCount)+x].
        /// </summary>
        internal int[] delta;

        /// <summary>
        /// state q is mapped to true iff q is a final state.
        /// </summary>
        internal HashSet<int> finalstates;

        /// <summary>
        /// Decision tree that maps characters to symbols in the range 0...maxSymbol-1.
        /// </summary>
        internal DecisionTree dt;

        /// <summary>
        /// Nonfinal sink state or -1 if such a state does not exist.
        /// </summary>
        public readonly int NonfinalSinkState;

        /// <summary>
        /// Final sink state or -1 if such a state does not exist.
        /// </summary>
        public readonly int FinalSinkState;

        /// <summary>
        /// The initial state is 0.
        /// </summary>
        public int InitialState
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Underlying BitVector algebra with fixed minterm partition
        /// </summary>
        BVAlgebra algebra;

        /// <summary>
        /// Underlying BitVector algebra with fixed minterm partition
        /// </summary>
        public IBooleanAlgebra<BV> Algebra
        {
            get
            {
                return algebra;
            }
        }

        internal RegexAutomaton(BVAlgebra algebra, Regex regex, int alphabetSize, int stateCount, int[] delta, HashSet<int> isfinalstate, DecisionTree dt, int nonfinalSinkState = -1, int finalSinkState = -1)
        {
            this.algebra = algebra;
            this.regex = regex;
            this.SymbolCount = alphabetSize;
            this.StateCount = stateCount; 
            this.delta = delta;
            this.finalstates = isfinalstate;
            this.NonfinalSinkState = nonfinalSinkState;
            this.FinalSinkState = finalSinkState;
            this.dt = dt;
        }

        static internal RegexAutomaton Create(CharSetSolver solver, Regex regex)
        {
            int t0 = System.Environment.TickCount;
            var nfa = solver.Convert(regex.ToString(), regex.Options);
            var minterms = GetMinterms(nfa);
            //create specialized BV algebra for this particular minterm partition
            var bvsolver = new BVAlgebra(solver, minterms);
            //convert the nfa to the specialized algebra
            var nfa_BV = nfa.ReplaceAlgebra<BV>(bvsolver.MapPredToBV, bvsolver);
            t0 = System.Environment.TickCount - t0;
            int t = System.Environment.TickCount;
            var nfa1 = nfa.Minimize();//.RemoveEpsilons().Minimize();
            var dfa = nfa1.Determinize().Minimize().Normalize();
            t = System.Environment.TickCount - t;
            int t_BV = System.Environment.TickCount;
            var nfa1_BV = nfa_BV;//.RemoveEpsilons().Minimize();
            var dfa_BV = nfa1_BV.Minimize().Determinize();
            var dfa_BV_min = dfa_BV.Minimize().Normalize();
            t_BV = System.Environment.TickCount - t_BV;
            //number of states
            var N = dfa.StateCount;
            //number of symbols
            int t2 = System.Environment.TickCount;
            var K = minterms.Length;
            var isfinalstate = new HashSet<int>();
            var nonfinalSinkstate = -1;
            var finalSinkstate = -1;
            for (int q = 0; q < N; q++)
            {
                if (dfa.IsFinalState(q))
                    isfinalstate.Add(q);
                if (dfa.IsLoopState(q) && dfa.GetMovesCountFrom(q) == 1)
                {
                    //there can only be at most one of each because dfa is minimal
                    if (dfa.IsFinalState(q))
                    {
                        if (finalSinkstate != -1)
                            throw new AutomataException(AutomataExceptionKind.InternalError_RegexAutomaton);
                        finalSinkstate = q;
                    }
                    else
                    {
                        if (nonfinalSinkstate != -1)
                            throw new AutomataException(AutomataExceptionKind.InternalError_RegexAutomaton);
                        nonfinalSinkstate = q;
                    }
                }
            }
            var delta = new int[K * N];
            for (int q = 0; q < dfa.StateCount; q++)
            {
                int symbols_mapped = 0;
                foreach (var move in dfa.GetMovesFrom(q))
                {
                    for (int a = 0; a < K; a++)
                    {
                        var phi = solver.MkAnd(move.Label, minterms[a]);
                        if (!phi.IsEmpty)
                        {
                            delta[(move.SourceState * K) + a] = move.TargetState;
                            symbols_mapped += 1;
                        }
                    }
                }
                if (symbols_mapped != K)
                    throw new AutomataException(AutomataExceptionKind.InternalError_RegexAutomaton);
            }
            var dt = DecisionTree.Create(solver, minterms);
            var ra = new RegexAutomaton(bvsolver, regex, K, N, delta, isfinalstate, dt, nonfinalSinkstate, finalSinkstate);
            t2 = System.Environment.TickCount - t2;
            return ra;
        }

        static BDD[] GetMinterms(Automaton<BDD> aut)
        {
            HashSet<BDD> predicates = new HashSet<BDD>();
            foreach (var m in aut.GetMoves())
                if (!m.IsEpsilon)
                    predicates.Add(m.Label);
            var mintermsList = new List<Tuple<bool[], BDD>>(aut.Algebra.GenerateMinterms(predicates.ToArray()));
            var minterms = Array.ConvertAll(mintermsList.ToArray(), elem => elem.Item2);
            return minterms;
        }

        /// <summary>
        /// Returns true iff the automaton accepts the input string.
        /// </summary>
        public bool IsMatch(string input)
        {
            var chars = input.ToArray();
            int i = 0;
            int q = 0;
            while (i < chars.Length)
            {
                var c = chars[i];
                int s = dt.GetId(c);
                q = delta[(q * SymbolCount) + s];
                if (q == FinalSinkState)
                    return true;
                if (q == NonfinalSinkState)
                    return false;
                i += 1;
            }
            return finalstates.Contains(q);
        }

        bool hideSinkState = false;
        public void ShowGraph(string name = "DFA", bool hideSinkState = true)
        {
            this.hideSinkState = hideSinkState;
            DirectedGraphs.DgmlWriter.ShowGraph(-1, this, name);
            this.hideSinkState = false;
        }

        public bool IsFinalState(int q)
        {
            return finalstates.Contains(q);
        }

        public IEnumerable<Move<BV>> GetMoves()
        {
            for (int q = 0; q < StateCount; q++)
            {
                var q_moves = new Dictionary<Tuple<int, int>, BV>();
                for (int s = 0; s < SymbolCount; s++)
                {
                    int p = delta[(q * SymbolCount) + s];
                    var qp = new Tuple<int, int>(q, p);
                    BV labels;
                    if (!q_moves.TryGetValue(qp, out labels))
                        q_moves[qp] = this.algebra.atoms[s];
                    else
                        q_moves[qp] = this.algebra.atoms[s] | labels;
                }
                foreach (var entry in q_moves)
                {
                    if ((!hideSinkState) || (hideSinkState && (entry.Key.Item1 != NonfinalSinkState) && (entry.Key.Item2 != NonfinalSinkState)))
                        yield return Move<BV>.Create(entry.Key.Item1, entry.Key.Item2, entry.Value);
                }
            }
        }

        public IEnumerable<int> GetStates()
        {
            for (int q = 0; q < StateCount; q++)
              if ((!hideSinkState) | (hideSinkState & (q != NonfinalSinkState)))
                    yield return q;
        }

        public string DescribeState(int state)
        {
            return state.ToString();
        }

        public string DescribeLabel(BV lab)
        {
            BDD set = algebra.ConvertToCharSet(lab);
            var str = algebra.CharSetProvider.PrettyPrint(set);
            return str;
        }

        public string DescribeStartLabel()
        {
            return "";
        }

        public Automaton<BDD> ConvertToAutomatonOverBDD(bool hideNonfinalSinkState = true)
        {
            List<Move<BDD>> moves = new List<Move<BDD>>();
            for (int p = 0; p < StateCount; p++)
                if (!(hideNonfinalSinkState && p == NonfinalSinkState))
                    for (int s = 0; s < SymbolCount; s++)
                    {
                        int q = delta[(p * SymbolCount) + s];
                        if (!(hideNonfinalSinkState && q == NonfinalSinkState))
                            moves.Add(Move<BDD>.Create(p, q, dt.partition[s]));
                    }
            var aut = Automaton<BDD>.Create(algebra.CharSetProvider, 0, finalstates, moves, false, false, true);
            return aut;
        }
    }
    */
}
