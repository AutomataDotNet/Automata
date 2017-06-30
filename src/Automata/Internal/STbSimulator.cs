using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Automata
{
    internal class STbSimulator<F, T, S>
    {

        STb<F, T, S> stb;
        public Dictionary<int, Dictionary<Tuple<T, T>, Tuple<int, T>>> exploredSteps;
        Dictionary<int, List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>>> moves;
        Dictionary<int, List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>>> final_moves;
        List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>> uncovered_moves;
        //List<Move<Rule<T>>> covered_moves;
        List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>> uncovered_final_moves;

        IContext<F,T,S> Z 
        {
            get {return stb.Solver;}
        }


        public STbSimulator(STb<F, T, S> stb)
        {
            this.stb = stb;
            this.exploredSteps = new Dictionary<int, Dictionary<Tuple<T, T>, Tuple<int, T>>>();
            this.moves = new Dictionary<int, List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>>>();
            this.final_moves = new Dictionary<int, List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>>>();
            this.uncovered_moves = new List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>>();
            this.uncovered_final_moves = new List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>>();
            foreach (var q in stb.States)
            {
                moves[q] = new List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>>();
                moves[q].AddRange(stb.EnumerateNonFinalMovesWithDirections(q));
                final_moves[q] = new List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>>();
                final_moves[q].AddRange(stb.EnumerateFinalMovesWithDirections(q));
            }
        }

        public List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>> UncoveredMoves
        {
            get { return uncovered_moves; }
        }

        public List<Tuple<ConsList<bool>, BaseRule<T>, Move<Rule<T>>>> UncoveredFinalMoves
        {
            get { return uncovered_final_moves; }
        }

        //public List<Move<Rule<T>>> CoveredMoves
        //{
        //    get { return covered_moves; }
        //}

        public void Explore()
        {
            var seen_states = new HashSet<int>();
            seen_states.Add(stb.InitialState);

            var stack = new SimpleStack<Tuple<int,T>>();
            var stack_next = new SimpleStack<Tuple<int,T>>();
            var seen_next = new HashSet<Tuple<int,T>>();
            stack_next.Push(new Tuple<int,T>(stb.InitialState,stb.InitialRegister));



            while (stack_next.IsNonempty)
            {
                foreach (var p_r in seen_next)
                    seen_states.Add(p_r.Item1);

                var tmp = stack;
                stack = stack_next;
                stack_next = tmp;
                seen_next.Clear();

                while (stack.IsNonempty)
                {
                    var q_r = stack.Pop();
                    var q = q_r.Item1;
                    var r_in = q_r.Item2;
                    foreach (var dir_move in moves[q])
                    {
                        var move = dir_move.Item3;
                        var guard1 = Z.ApplySubstitution(move.Label.Guard, stb.RegVar, r_in);
                        var witness = Z.MainSolver.FindOneMember(Z.MkAnd(guard1, Z.MkEq(stb.InputVar, stb.InputVar)));
                        if (witness != null)
                        {
                            var r_out = Z.Simplify(Z.ApplySubstitution(move.Label.Update, stb.InputVar, witness.Value, stb.RegVar, r_in));
                            var p_r = new Tuple<int, T>(move.TargetState, r_out);
                            if (!exploredSteps.ContainsKey(q))
                                exploredSteps[q] = new Dictionary<Tuple<T, T>, Tuple<int, T>>();
                            exploredSteps[q][new Tuple<T, T>(witness.Value, r_in)] = p_r;
                            if (!seen_states.Contains(move.TargetState))
                                if (seen_next.Add(p_r))
                                    stack_next.Push(p_r);
                        }
                        else //the move is inaccessible from this state (and register value) since guard1 is unsatisfiable
                            uncovered_moves.Add(dir_move);
                    }
                    foreach (var dir_fmove in final_moves[q])
                    {
                        var fmove = dir_fmove.Item3;
                        var guard1 = Z.Simplify(Z.ApplySubstitution(fmove.Label.Guard, stb.RegVar, r_in));
                        if (guard1.Equals(stb.Solver.False))
                            uncovered_final_moves.Add(dir_fmove);
                    }
                }
            }
        }
    }
}
