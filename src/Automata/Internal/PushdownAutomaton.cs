using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Automata
{
    /// <summary>
    /// Symbolic Push Down Automaton
    /// </summary>
    internal class PushdownAutomaton<T>
    {
        public List<int> states;
        internal List<int> stackSymbols;
        public int initialState;
        public int initialStackSymbol;
        public Dictionary<int, Dictionary<int, PushdownMoves<T>>> delta;
        internal bool isEpsilonFree;

        public int InitialState
        {
            get
            {
                return initialState;
            }
        }

        public int InitialStackSymbol
        {
            get
            {
                return initialStackSymbol;
            }
        }

        public IList<int> States
        {
            get { return states; }
        }

        public IList<int> StackSymbols
        {
            get { return stackSymbols; }
        }

        public IEnumerable<PushdownMove<T>> GetMoves(int state, int stackSymbol)
        {
            Dictionary<int, PushdownMoves<T>> movesFromState;
            if (delta.TryGetValue(state, out movesFromState))
            {
                PushdownMoves<T> movesFromStateAndSymbol;
                if (movesFromState.TryGetValue(stackSymbol, out movesFromStateAndSymbol))
                {
                    foreach (PushdownMove<T> move in movesFromStateAndSymbol.moves)
                        yield return move;
                    foreach (PushdownMove<T> move in movesFromStateAndSymbol.eMoves)
                        yield return move;
                }
            }
        }

        public PushdownAutomaton(int[] states, int initialState, int[] stackSymbols, int initialStackSymbol,
            params PushdownMove<T>[] moves)
        {
            //TBD: validate that the arguments are correct
            this.states = new List<int>(states);
            this.initialState = initialState;
            this.stackSymbols = new List<int>(stackSymbols);
            this.initialStackSymbol = initialStackSymbol;
            bool noEpsilons = true;
            var delta1 = new Dictionary<int, Dictionary<int, PushdownMoves<T>>>();
            foreach (PushdownMove<T> m in moves)
            {
                #region add the move to delta1
                Dictionary<int, PushdownMoves<T>> fromStackSymbols;
                if (delta1.TryGetValue(m.SourceState, out fromStackSymbols))
                {
                    PushdownMoves<T> moves1;
                    if (!fromStackSymbols.TryGetValue(m.PopSymbol, out moves1))
                    {
                        moves1 = new PushdownMoves<T>();
                        fromStackSymbols[m.PopSymbol] = moves1;
                    }
                    if (m.IsEpsilon)
                    {
                        noEpsilons = false;
                        moves1.eMoves.Add(m);
                    }
                    else
                        moves1.moves.Add(m);
                }
                else
                {
                    fromStackSymbols = new Dictionary<int, PushdownMoves<T>>();
                    PushdownMoves<T> moves2 = new PushdownMoves<T>();
                    if (m.IsEpsilon)
                    {
                        noEpsilons = false;
                        moves2.eMoves.Add(m);
                    }
                    else
                        moves2.moves.Add(m);
                    fromStackSymbols[m.PopSymbol] = moves2;
                    delta1[m.SourceState] = fromStackSymbols;
                }
                #endregion
            }
            this.delta = delta1;
            this.isEpsilonFree = noEpsilons;
        }

        public PushdownAutomaton(int initialState, int initialStackSymbol, params PushdownMove<T>[] moves)
        {
            this.states = new List<int>();
            this.initialState = initialState;
            states.Add(initialState);
            HashSet<int> statesSet = new HashSet<int>();
            statesSet.Add(initialState);
            this.stackSymbols = new List<int>();
            this.initialStackSymbol = initialStackSymbol;
            stackSymbols.Add(initialStackSymbol);
            HashSet<int> stackSymbolsSet = new HashSet<int>();
            stackSymbolsSet.Add(initialStackSymbol);
            bool noEpsilons = true;
            var delta1 = new Dictionary<int, Dictionary<int, PushdownMoves<T>>>();
            foreach (PushdownMove<T> m in moves)
            {
                #region collect states and stack symbols
                if (!statesSet.Contains(m.SourceState))
                {
                    states.Add(m.SourceState);
                    statesSet.Add(m.SourceState);
                }
                if (!statesSet.Contains(m.TargetState))
                {
                    states.Add(m.TargetState);
                    statesSet.Add(m.TargetState);
                }
                if (!stackSymbolsSet.Contains(m.PopSymbol))
                {
                    stackSymbolsSet.Add(m.PopSymbol);
                    stackSymbols.Add(m.PopSymbol);
                }
                if (m.PushSymbols != null & m.PushSymbols.Length > 0)
                {
                    foreach (int symbol in m.PushSymbols)
                    {
                        if (!stackSymbolsSet.Contains(symbol))
                        {
                            stackSymbolsSet.Add(symbol);
                            stackSymbols.Add(symbol);
                        }
                    }
                }
                #endregion

                #region add the move to delta1
                Dictionary<int, PushdownMoves<T>> fromStackSymbols;
                if (delta1.TryGetValue(m.SourceState, out fromStackSymbols))
                {
                    PushdownMoves<T> moves1;
                    if (!fromStackSymbols.TryGetValue(m.PopSymbol, out moves1))
                    {
                        moves1 = new PushdownMoves<T>();
                        fromStackSymbols[m.PopSymbol] = moves1;
                    }
                    if (m.IsEpsilon)
                    {
                        noEpsilons = false;
                        moves1.eMoves.Add(m);
                    }
                    else
                        moves1.moves.Add(m);
                }
                else
                {
                    fromStackSymbols = new Dictionary<int, PushdownMoves<T>>();
                    PushdownMoves<T> moves2 = new PushdownMoves<T>();
                    if (m.IsEpsilon)
                    {
                        noEpsilons = false;
                        moves2.eMoves.Add(m);
                    }
                    else
                        moves2.moves.Add(m);
                    fromStackSymbols[m.PopSymbol] = moves2;
                    delta1[m.SourceState] = fromStackSymbols;
                }
                #endregion
            }
            this.delta = delta1;
            this.isEpsilonFree = noEpsilons;
        }

        public void Display(TextWriter tw)
        {
            foreach (int s in states)
            {
                foreach (var kv in delta[s])
                {
                    foreach (PushdownMove<T> move in kv.Value.eMoves)
                        tw.WriteLine(move.ToString());
                    foreach (PushdownMove<T> move in kv.Value.moves)
                        tw.WriteLine(move.ToString());
                }
            }
        }
    }

    internal class PushdownMoves<S>
    {
        public List<PushdownMove<S>> moves;
        public List<PushdownMove<S>> eMoves;

        internal PushdownMoves()
        {
            this.moves = new List<PushdownMove<S>>();
            this.eMoves = new List<PushdownMove<S>>();
        }
    }
}
