using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata;

namespace Microsoft.Automata.MSO
{
    public abstract class WS1SFormula
    {
        public virtual Automaton<BDD> getAutomaton(CharSetSolver solver)
        {
            return getAutomaton(SimpleList<string>.Empty, solver);
        }

        internal abstract Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver);

        protected static bool MINIMIZE = true; 
    }

    /// <summary>
    /// Conjunction of two formulas
    /// </summary>
    public class WS1SAnd : WS1SFormula
    {
        public WS1SFormula phi1;
        public WS1SFormula phi2;

        public WS1SAnd(WS1SFormula phi1, WS1SFormula phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var aut1 = phi1.getAutomaton(variables, solver);
            var aut2 = phi2.getAutomaton(variables, solver);
            var res = aut1.Intersect(aut2, solver);
            if (MINIMIZE)
                res = res.Determinize(solver).Minimize(solver);
            return res;
        }
    }

    /// <summary>
    /// Disjunction of two formulas
    /// </summary>
    public class WS1SOr : WS1SFormula
    {
        public WS1SFormula phi1;
        public WS1SFormula phi2;

        public WS1SOr(WS1SFormula phi1, WS1SFormula phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var aut1 = phi1.getAutomaton(variables, solver);
            var aut2 = phi2.getAutomaton(variables, solver);
            //var res = aut1.Union(aut2, solver).RemoveEpsilons(solver.MkOr);
            var res = aut1.Complement(solver).Intersect(aut2.Complement(solver), solver).Complement(solver);
            if (MINIMIZE)
                res = res.Determinize(solver).Minimize(solver);
            return res;
        } 
    }

    /// <summary>
    /// Negation
    /// </summary>
    public class WS1SNot : WS1SFormula
    {
        public WS1SFormula phi;

        public WS1SNot(WS1SFormula phi)
        {
            this.phi = phi;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, solver.True) };

            //True automaton and then difference
            var trueAut = Automaton<BDD>.Create(solver, 0, new int[] { 0 }, moves);
            var aut = phi.getAutomaton(variables, solver);
            var neg_aut = trueAut.Minus(aut, solver).Determinize(solver).Minimize(solver);
            return neg_aut;
        }
    }

    /// <summary>
    /// X1 is a subset of X2
    /// </summary>
    public class WS1SSubset : WS1SFormula
    {
        /// <summary>
        /// SO variable
        /// </summary>
        public readonly string X1;
        /// <summary>
        /// SO variable
        /// </summary>
        public readonly string X2;

        public WS1SSubset(string X1, string X2)
        {
            this.X1 = X1;
            this.X2 = X2;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var pos1 = variables.IndexOf(X1) + ((int)solver.Encoding);
            var pos2 = variables.IndexOf(X2) + ((int)solver.Encoding);

            //pos1is1 implies pos2is1
            //  is equivalent to
            //not(pos1is1) or pos2is1
            //  is equivalent to
            //pos1is0 or pos2is1
            //var trueBv = MkTrue(variables, solver);
            var pos2is1 = solver.MkBitTrue(pos2);
            var pos1is0 = solver.MkBitFalse(pos1);
            var subsetCond = solver.MkOr(pos1is0, pos2is1);


            //Create automaton for condition
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, subsetCond) };
            return Automaton<BDD>.Create(solver, 0, new int[] { 0 }, moves);//.Determinize(solver).Minimize(solver);
        }
    }

    /// <summary>
    /// The SO variable var contains exactly one element
    /// </summary>
    public class WS1SSingleton : WS1SFormula
    {
        public string var;

        public WS1SSingleton(string var)
        {
            this.var = var;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var pos = variables.IndexOf(var) + ((int)solver.Encoding);

            //var trueBv = MkTrue(variables, solver);
            var posIs1 = solver.MkBitTrue(pos);
            var posIs0 = solver.MkBitFalse(pos);

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, posIs0),
                new Move<BDD>(0, 1, posIs1), 
                new Move<BDD>(1, 1, posIs0)
            };
            return Automaton<BDD>.Create(solver, 0, new int[] { 1 }, moves);
        }
    }

    /// <summary>
    /// the position in variable var has label pred (for example a(x))
    /// </summary>
    public class WS1SPred : WS1SFormula
    {
        public BDD pred;
        public string var;

        public WS1SPred(BDD pred, string var)
        {
            this.pred = pred;
            this.var = var;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var k = variables.IndexOf(var) + ((int)solver.Encoding);
            //var trueBv = MkTrue(variables, solver);

            //Compute predicates for k-th bit is 0 or 1
            //var posIs1 = solver.MkAnd(new BDD[] { trueBv, solver.MkSetWithBitTrue(k), solver.ShiftLeft(pred, variables.Count) });
            //var posIs0 = solver.MkAnd(trueBv, solver.MkSetWithBitFalse(k));
            var posIs1 = solver.MkAnd(solver.MkBitTrue(k), pred);
            var posIs0 = solver.MkBitFalse(k);
            var psi = solver.MkOr(posIs0, posIs1);

            //Create automaton for condition
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, psi) };
            return Automaton<BDD>.Create(solver, 0, new int[] { 0 }, moves);
        }
    }

    /// <summary>
    /// Var1 and var2 are singleton and var2=var1+1
    /// </summary>
    public class WS1SSucc : WS1SFormula
    {
        public string var1;
        public string var2;

        public WS1SSucc(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var pos1 = variables.IndexOf(var1) + ((int)solver.Encoding);
            var pos2 = variables.IndexOf(var2) + ((int)solver.Encoding);

            //var trueBv = MkTrue(variables, solver);
            var pos1is0 = solver.MkBitFalse(pos1);
            var pos1is1 = solver.MkBitTrue(pos1);
            var pos2is0 = solver.MkBitFalse(pos2);
            var pos2is1 = solver.MkBitTrue(pos2);

            var both0 = solver.MkAnd(new BDD[] { pos1is0, pos2is0 });
            var pos11pos20 = solver.MkAnd(new BDD[] { pos1is1, pos2is0 });
            var pos10pos21 = solver.MkAnd(new BDD[] { pos1is0, pos2is1 });

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, both0),
                new Move<BDD>(0, 1, pos11pos20), 
                new Move<BDD>(1, 2, pos10pos21), 
                new Move<BDD>(2, 2, both0), 
            };
            return Automaton<BDD>.Create(solver, 0, new int[] { 2 }, moves);//.Determinize(solver).Minimize(solver);
        }
    }

    /// <summary>
    /// Var1 and var2 are singletons and var1 is less than var2
    /// </summary>
    public class WS1SLt : WS1SFormula
    {
        public string var1;
        public string var2;

        public WS1SLt(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var pos1 = variables.IndexOf(var1) + ((int)solver.Encoding);
            var pos2 = variables.IndexOf(var2) + ((int)solver.Encoding);

            //var trueBv = MkTrue(variables, solver);
            var pos1is0 = solver.MkBitFalse(pos1);
            var pos1is1 = solver.MkBitTrue(pos1);
            var pos2is0 = solver.MkBitFalse(pos2);
            var pos2is1 = solver.MkBitTrue(pos2);

            var both0 = solver.MkAnd(pos1is0, pos2is0);
            var pos11pos20 = solver.MkAnd(pos1is1, pos2is0);
            var pos10pos21 = solver.MkAnd(pos1is0, pos2is1);

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, both0),
                new Move<BDD>(0, 1, pos11pos20), 
                new Move<BDD>(1, 1, both0),
                new Move<BDD>(1, 2, pos10pos21), 
                new Move<BDD>(2, 2, both0), 
            };
            return Automaton<BDD>.Create(solver, 0, new int[] { 2 }, moves);//.Determinize(solver).Minimize(solver);
        }
    }

    /// <summary>
    /// var1 = var2
    /// </summary>
    public class WS1SEq : WS1SFormula
    {
        public string var1;
        public string var2;

        public WS1SEq(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var pos1 = variables.IndexOf(var1) + ((int)solver.Encoding);
            var pos2 = variables.IndexOf(var2) + ((int)solver.Encoding);

            var both1 = solver.MkAnd(solver.MkBitTrue(pos1), solver.MkBitTrue(pos2));
            var both0 = solver.MkAnd(solver.MkBitFalse(pos1), solver.MkBitFalse(pos2));
            var eqCond = solver.MkOr(both0, both1);

            //Create automaton for condition
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, eqCond) };
            return Automaton<BDD>.Create(solver, 0, new int[] { 0 }, moves);//.Determinize(solver).Minimize(solver);
        }
    }

    /// <summary>
    /// True formula
    /// </summary>
    public class WS1STrue : WS1SFormula
    {
        public WS1STrue() { }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, solver.True) };
            return Automaton<BDD>.Create(solver, 0, new int[] { 0 }, moves);
        }
    }

    /// <summary>
    /// Exists X: phi
    /// </summary>
    public class WS1SExists : WS1SFormula
    {
        public readonly string X;
        public readonly WS1SFormula phi;

        public WS1SExists(string var, WS1SFormula phi)
        {
            this.X = var;
            this.phi = phi;
        }

        internal override Automaton<BDD> getAutomaton(SimpleList<string> variables, CharSetSolver solver)
        {
            //Automaton<BvSet> for formula
            var varCopy = variables.Append(X);
            var autPhi = phi.getAutomaton(varCopy, solver);

            var newMoves = new List<Move<BDD>>();
            var k = variables.Count +((int)solver.Encoding);
            foreach (var move in autPhi.GetMoves())
            {
                var newCond = solver.OmitBit(move.Label, k);
                newMoves.Add(new Move<BDD>(move.SourceState, move.TargetState, newCond));
            }

            var res = Automaton<BDD>.Create(solver, autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
            if (MINIMIZE)
                res = res.Determinize(solver).Minimize(solver);
            return res;
        }
    }
}