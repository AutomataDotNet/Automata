using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata.Utilities
{
    /// <summary>
    /// Functionality to serialize a regex as a range automaton and to deserialize as Automaton over BDDs.
    /// </summary>
    public static class RegexToRangeAutomatonSerializer
    {
        public static void SaveAsRangeAutomaton(string regex, BitWidth encoding, string file, bool epsilonfree = false, bool determinize = false, int determinization_timeout_ms = 0)
        {
            var solver = new CharRangeSolver(encoding);
            var converter = new RegexToAutomatonConverterRanges(solver);

            var A = converter.Convert(regex, System.Text.RegularExpressions.RegexOptions.None);
            if (epsilonfree)
                A = A.RemoveEpsilons();

            if (determinize)
            {
                A = A.RemoveEpsilons();
                A.CheckDeterminism();
                A = A.Determinize(determinization_timeout_ms);
            }
            //A.CheckDeterminism(solver,true);
            //if (!A.isDeterministic)
            //    throw new AutomataException(AutomataExceptionKind.InternalError);

            System.IO.StreamWriter sw = new System.IO.StreamWriter(file);
            sw.WriteLine(A.InitialState);
            foreach (var s in A.GetFinalStates())
                sw.Write("{0} ", s);
            sw.WriteLine();
            foreach (var move in A.GetMoves())
                if (move.IsEpsilon)
                    sw.WriteLine("{0} {1} {2} {3}", move.SourceState, -1, -1, move.TargetState);
                else
                    foreach (var range in move.Label)
                        sw.WriteLine("{0} {1} {2} {3}", move.SourceState, (int)range.Item1, (int)range.Item2, move.TargetState);
            sw.Close();
        }

        public static Automaton<BDD> Read(CharSetSolver solver, string file)
        {
            var lines = System.IO.File.ReadAllLines(file);
            int initialState = int.Parse(lines[0]);
            var moves = new Dictionary<Tuple<int, int>,BDD>();
            var allmoves = new List<Move<BDD>>();
            int[] finals = Array.ConvertAll(lines[1].TrimEnd(' ').Split(' '), s => int.Parse(s));
            for (int i = 2; i < lines.Length; i++)
            {
                int[] elems = Array.ConvertAll(lines[i].TrimEnd(' ').Split(' '), s => int.Parse(s));
                var key = new Tuple<int, int>(elems[0], elems[3]);
                if (elems[1] == -1)
                    allmoves.Add(Move<BDD>.Epsilon(elems[0],elems[3]));
                else
                {
                    var pred = solver.MkCharSetFromRange((char)elems[1], (char)elems[2]);
                    if (moves.ContainsKey(key))
                        moves[key] = solver.MkOr(moves[key], pred);
                    else
                        moves[key] = pred;
                }
            }
            foreach (var kv in moves)
                allmoves.Add(Move<BDD>.Create(kv.Key.Item1, kv.Key.Item2, kv.Value));

            var aut = Automaton<BDD>.Create(solver, initialState, finals, allmoves);
            return aut;
        }

        public static Automaton<BDD> ReadFromString(CharSetSolver solver, string automaton)
        {
            var lines = automaton.Split(new char[] { '\n','\r' }, StringSplitOptions.RemoveEmptyEntries);
            int initialState = int.Parse(lines[0]);
            var moves = new Dictionary<Tuple<int, int>, BDD>();
            var allmoves = new List<Move<BDD>>();
            int[] finals = Array.ConvertAll(lines[1].TrimEnd(' ').Split(' '), s => int.Parse(s));
            for (int i = 2; i < lines.Length; i++)
            {
                int[] elems = Array.ConvertAll(lines[i].TrimEnd(' ').Split(' '), s => int.Parse(s));
                var key = new Tuple<int, int>(elems[0], elems[3]);
                if (elems[1] == -1)
                    allmoves.Add(Move<BDD>.Epsilon(elems[0], elems[3]));
                else
                {
                    var pred = solver.MkCharSetFromRange((char)elems[1], (char)elems[2]);
                    if (moves.ContainsKey(key))
                        moves[key] = solver.MkOr(moves[key], pred);
                    else
                        moves[key] = pred;
                }
            }
            foreach (var kv in moves)
                allmoves.Add(Move<BDD>.Create(kv.Key.Item1, kv.Key.Item2, kv.Value));

            var aut = Automaton<BDD>.Create(solver, initialState, finals, allmoves);
            return aut;
        }

        /// <summary>
        /// Each transition has the form int[]{fromState, intervalStart, intervalEnd, toState}.
        /// If intervalStart = intervalEnd = -1 then this is an epsilon move.
        /// </summary>
        public static Automaton<BDD> ReadFromRanges(CharSetSolver solver, int initialState, int[] finalStates, IEnumerable<int[]> transitions)
        {
            var moves = new Dictionary<Tuple<int, int>, BDD>();
            var allmoves = new List<Move<BDD>>();
            int[] finals = finalStates;
            foreach (var elems in transitions) 
            {
                var key = new Tuple<int, int>(elems[0], elems[3]);
                if (elems[1] == -1)
                    allmoves.Add(Move<BDD>.Epsilon(elems[0], elems[3]));
                else
                {
                    var pred = solver.MkCharSetFromRange((char)elems[1], (char)elems[2]);
                    if (moves.ContainsKey(key))
                        moves[key] = solver.MkOr(moves[key], pred);
                    else
                        moves[key] = pred;
                }
            }
            foreach (var kv in moves)
                allmoves.Add(Move<BDD>.Create(kv.Key.Item1, kv.Key.Item2, kv.Value));

            var aut = Automaton<BDD>.Create(solver, initialState, finals, allmoves);
            return aut;
        }
    }
}
