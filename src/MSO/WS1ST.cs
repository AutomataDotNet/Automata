using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Automata;

//namespace Microsoft.Automata.MSO    
//{
//    public abstract class WS1SFormula<T>
//    {
//        public Automaton<T> getAutomaton(IBooleanAlgebra<T> solver)
//        {
//            //var css = (solver is IContextCore<T> ? ((IContextCore<T>)solver).CharSetProvider : solver as CharSetSolver);
//            var css = new CharSetSolver(BitWidth.BV32);
//            var ca = new CartesianAlgebra<BDD, T>(css, solver);
//            var aut = getAutomaton(new List<string>(), ca);
//            var newMoves = new List<Move<T>>();
//            foreach (var move in aut.GetMoves())
//                newMoves.Add(new Move<T>(move.SourceState, move.TargetState, move.Label.ProjectSecond()));
//            return Automaton<T>.Create(aut.InitialState,aut.GetFinalStates(),newMoves);
//        }

//        internal abstract Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD, T> solver);
//    }

//    public class WS1SAnd<T> : WS1SFormula<T>
//    {
//        public WS1SFormula<T> phi1;
//        public WS1SFormula<T> phi2;

//        public WS1SAnd(WS1SFormula<T> phi1, WS1SFormula<T> phi2)
//        {
//            this.phi1 = phi1;
//            this.phi2 = phi2;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD, T> solver)
//        {
//            var aut1 = phi1.getAutomaton(variables, solver);
//            var aut2 = phi2.getAutomaton(variables, solver);
//            return aut1.Intersect(aut2, solver);//.Determinize(solver).Minimize(solver);
//        }
//    }

//    public class WS1SOr<T> : WS1SFormula<T>
//    {
//        public WS1SFormula<T> phi1;
//        public WS1SFormula<T> phi2;

//        public WS1SOr(WS1SFormula<T> phi1, WS1SFormula<T> phi2)
//        {
//            this.phi1 = phi1;
//            this.phi2 = phi2;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD,T> solver)
//        {
//            var aut1 = phi1.getAutomaton(variables, solver);
//            var aut2 = phi2.getAutomaton(variables, solver);
//            return aut1.Union(aut2, solver);//.Determinize(solver).Minimize(solver);
//        }
//    }

//    public class WS1SNot<T> : WS1SFormula<T>
//    {
//        public WS1SFormula<T> phi;

//        public WS1SNot(WS1SFormula<T> phi)
//        {
//            this.phi = phi;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD,T> solver)
//        {
//            //Create condition that only considers bvs of size |variables|
//            var bddAlg = (BDDAlgebra)solver.First;
//            var trueBv = bddAlg.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count) - 1), 31);

//            var pairCond = solver.MkProduct(trueBv,solver.Second.True);
//            var moves = new Move<ISumOfProducts<BDD, T>>[] { new Move<ISumOfProducts<BDD, T>>(0, 0, pairCond) };

//            //True automaton and then difference
//            var trueAut = Automaton<ISumOfProducts<BDD,T>>.Create(0, new int[] { 0 }, moves);
//            var aut = phi.getAutomaton(variables, solver);

//            var a = trueAut.Minus(aut, solver).Determinize(solver).Minimize(solver);
//            return a;
//        }
//    }

//    public class WS1SSubset<T> : WS1SFormula<T>
//    {
//        public string var1;
//        public string var2;

//        public WS1SSubset(string var1, string var2)
//        {
//            this.var1 = var1;
//            this.var2 = var2;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD,T> solver)
//        {
//            var pos1 = variables.IndexOf(var1);
//            var pos2 = variables.IndexOf(var2);

//            //Create conditions that bit in pos1 is smaller than pos2
//            var charSolver = (CharSetSolver)solver.First;
//            var trueBv = charSolver.MkSetFromRange(0,(uint)(Math.Pow(2, variables.Count) - 1), 31);
//            var pos2is1 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitTrue(pos2));
//            var pos1is0 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitFalse(pos1));
//            var subsetCond = charSolver.MkOr(pos2is1, pos1is0);


//            //Create automaton for condition
//            var pairCond = solver.MkProduct(subsetCond, solver.Second.True);
//            var moves = new Move<ISumOfProducts<BDD, T>>[] { new Move<ISumOfProducts<BDD, T>>(0, 0, pairCond) };
//            return Automaton<ISumOfProducts<BDD,T>>.Create(0, new int[] { 0 }, moves);
//        }
//    }

//    public class WS1SSingleton<T> : WS1SFormula<T>
//    {
//        public string var;

//        public WS1SSingleton(string var)
//        {
//            this.var = var;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD, T> solver)
//        {
//            var pos = variables.IndexOf(var);

//            //Create conditions that bit in pos1 is smaller than pos2
//            var charSolver = (CharSetSolver)solver.First;
//            var trueBv = charSolver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count) - 1), 31);
//            var posIs1 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitTrue(pos));
//            var posIs0 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitFalse(pos));

//            //Create automaton for condition
//            var moves = new Move<ISumOfProducts<BDD, T>>[] { 
//                new Move<ISumOfProducts<BDD,T>>(0, 0, solver.MkProduct(posIs0, solver.Second.True)),
//                new Move<ISumOfProducts<BDD,T>>(0, 1, solver.MkProduct(posIs1, solver.Second.True)), 
//                new Move<ISumOfProducts<BDD,T>>(1, 1, solver.MkProduct(posIs0, solver.Second.True))
//            };
//            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { 1 }, moves);
//        }
//    }

//    public class WS1SPred<T> : WS1SFormula<T>
//    {
//        public T pred;
//        public string var;

//        public WS1SPred(T pred, string var)
//        {
//            this.pred = pred;
//            this.var = var;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD,T> solver)
//        {
//            var k = variables.IndexOf(var);

//            //Compute predicates for k'th bit is 0 or 1
//            var charSolver = (CharSetSolver)solver.First;
//            var trueBv = charSolver.MkSetFromRange(0,(uint)(Math.Pow(2, variables.Count) - 1), 31);
//            var posIs1 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitTrue(k));
//            var posIs0 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitFalse(k));

//            var psi1 = solver.MkProduct(posIs0, solver.Second.True);
//            var psi2 = solver.MkProduct(posIs1, pred);
//            var psi = solver.MkOr(psi1, psi2);

//            //Create automaton for condition
//            var moves = new Move<ISumOfProducts<BDD,T>>[] { 
//                new Move<ISumOfProducts<BDD,T>>(0, 0, psi),
//            };
//            return Automaton<ISumOfProducts<BDD,T>>.Create(0, new int[] { 0 }, moves);
//        }
//    }

//    public class WS1SSuccN<T> : WS1SFormula<T>
//    {
//        string var1;
//        string var2;
//        int gap;

//        public WS1SSuccN(string var1, string var2, int gap)
//        {
//            if (gap < 1)
//                throw new Exception("gap should be at least 1");
//            this.var1 = var1;
//            this.var2 = var2;
//            this.gap = gap;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD, T> solver)
//        {
//            var ind1 = variables.IndexOf(var1);
//            var ind2 = variables.IndexOf(var2);

//            var charSolver = (CharSetSolver)solver.First;
//            var trueP = charSolver.MkSetFromRange(0, (uint)(Math.Pow(2, variables.Count - 1) - 1), 31);
//            var ind10 = charSolver.MkSetWithBitFalse(ind1);
//            var ind11 = charSolver.MkSetWithBitTrue(ind1);
//            var ind20 = charSolver.MkSetWithBitFalse(ind2);
//            var ind21 = charSolver.MkSetWithBitTrue(ind2);
//            var both0 = charSolver.MkAnd(ind10, ind20);
//            var ind11ind20 = charSolver.MkAnd(ind11, ind20);
//            var ind10ind21 = charSolver.MkAnd(ind10, ind21);

//            var both0t = solver.MkProduct(both0, solver.Second.True);
//            var ind11ind20t = solver.MkProduct(ind11ind20, solver.Second.True);
//            var ind10ind21t = solver.MkProduct(ind10ind21, solver.Second.True);

//            //Create moves
//            var moves = new List<Move<ISumOfProducts<BDD, T>>>();
//            moves.Add(new Move<ISumOfProducts<BDD, T>>(0, 0, both0t));
//            moves.Add(new Move<ISumOfProducts<BDD, T>>(0, 1, ind11ind20t));
//            moves.Add(new Move<ISumOfProducts<BDD, T>>(gap, gap + 1, ind10ind21t));
//            moves.Add(new Move<ISumOfProducts<BDD, T>>(gap + 1, gap + 1, both0t));

//            for (int i = 1; i < gap; i++)
//                moves.Add(new Move<ISumOfProducts<BDD, T>>(i, i + 1, both0t));

//            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { gap + 1 }, moves);
//        }
//    }

//    public class WS1SLt<T> : WS1SFormula<T>
//    {
//        public string var1;
//        public string var2;

//        public WS1SLt(string var1, string var2)
//        {
//            this.var1 = var1;
//            this.var2 = var2;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD,T> solver)
//        {
//            var pos1 = variables.IndexOf(var1);
//            var pos2 = variables.IndexOf(var2);

//            var charSolver = (CharSetSolver)solver.First;
//            var trueBv = charSolver.MkSetFromRange(0,(uint)(Math.Pow(2, variables.Count) - 1), 31);
//            var pos1is0 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitFalse(pos1));
//            var pos1is1 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitTrue(pos1));
//            var pos2is0 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitFalse(pos2));
//            var pos2is1 = charSolver.MkAnd(trueBv, charSolver.MkSetWithBitTrue(pos2));

//            var both0 = charSolver.MkAnd(pos1is0, pos2is0);
//            var pos11pos20 = charSolver.MkAnd(pos1is1, pos2is0);
//            var pos10pos21 = charSolver.MkAnd(pos1is0, pos2is1);

//            //Create automaton for condition
//            var moves = new Move<ISumOfProducts<BDD,T>>[] { 
//                new Move<ISumOfProducts<BDD,T>>(0, 0, solver.MkProduct(both0,solver.Second.True)),
//                new Move<ISumOfProducts<BDD,T>>(0, 1, solver.MkProduct(pos11pos20,solver.Second.True)), 
//                new Move<ISumOfProducts<BDD,T>>(1, 1, solver.MkProduct(both0,solver.Second.True)),
//                new Move<ISumOfProducts<BDD,T>>(1, 2, solver.MkProduct(pos10pos21,solver.Second.True)), 
//                new Move<ISumOfProducts<BDD,T>>(2, 2, solver.MkProduct(both0,solver.Second.True))
//            };
//            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { 2 }, moves);
//        }
//    }

//    public class WS1SEq<T> : WS1SFormula<T>
//    {
//        public string var1;
//        public string var2;

//        public WS1SEq(string var1, string var2)
//        {
//            this.var1 = var1;
//            this.var2 = var2;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD,T> solver)
//        {
//            var pos1 = variables.IndexOf(var1);
//            var pos2 = variables.IndexOf(var2);

//            //Create conditions that bit in pos1 is smaller than pos2
//            var charSolver = (CharSetSolver)solver.First;
//            var trueBv = charSolver.MkSetFromRange(0,(uint)(Math.Pow(2, variables.Count) - 1), 31);
//            var both1 = charSolver.MkAnd(new BDD[] { trueBv, charSolver.MkSetWithBitTrue(pos1), charSolver.MkSetWithBitTrue(pos2) });
//            var both0 = charSolver.MkAnd(new BDD[] { trueBv, charSolver.MkSetWithBitFalse(pos1), charSolver.MkSetWithBitFalse(pos2) });
//            var eqCond = charSolver.MkOr(new BDD[] { both0, both1 });

//            //Create automaton for condition
//            var moves = new Move<ISumOfProducts<BDD,T>>[] { 
//                new Move<ISumOfProducts<BDD,T>>(0, 0, solver.MkProduct(eqCond,solver.Second.True)) 
//            };
//            return Automaton<ISumOfProducts<BDD,T>>.Create(0, new int[] { 0 }, moves);
//        }
//    }

//    public class WS1STrue<T> : WS1SFormula<T>
//    {
//        public WS1STrue() { }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD,T> solver)
//        {
//            //Create condition that only considerst bv of size |variables|
//            var charSolver = (CharSetSolver)solver.First;
//            var trueBv = charSolver.MkSetFromRange(0,(uint)(Math.Pow(2, variables.Count) - 1), 31);
//            var moves = new Move<ISumOfProducts<BDD, T>>[] { 
//                new Move<ISumOfProducts<BDD, T>>(0, 0, solver.MkProduct(trueBv, solver.Second.True)) 
//            };
//            //True automaton 
//            return Automaton<ISumOfProducts<BDD,T>>.Create(0, new int[] { 0 }, moves);
//        }
//    }

//    public class WS1SExists<T> : WS1SFormula<T>
//    {
//        public string var;
//        public WS1SFormula<T> phi;

//        public WS1SExists(string var, WS1SFormula<T> phi)
//        {
//            this.var = var;
//            this.phi = phi;
//        }

//        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BDD,T> solver)
//        {
//            if (variables.Count > 32)
//                throw new AutomataException("More than 32 nested variables.");

//            //Automaton<T> for formula
//            var varCopy = new List<string>(variables);
//            varCopy.Insert(0, var);
//            var autPhi = phi.getAutomaton(varCopy, solver);

//            //autPhi.ShowGraph();

//            //Remove first bit from each move
//            var newMoves = new List<Move<ISumOfProducts<BDD,T>>>();
//            var charSolver = (CharSetSolver)solver.First;
//            foreach (var move in autPhi.GetMoves())
//            {
//                //var newRows = new List<Pair<BDD, T>>();
//                //foreach(var r in move.Label.GetRows())
//                //    newRows.Add(new Pair<BDD,T>(charSolver.LShiftRight(r.First),r.Second));

//                //var newTable = solver.MkTable(newRows);

//                var newTable = move.Label.TransformFirst(charSolver.LShiftRight);
//                newMoves.Add(new Move<ISumOfProducts<BDD, T>>(move.SourceState, move.TargetState, newTable));
//            }

//            var aut1 = Automaton<ISumOfProducts<BDD,T>>.Create(autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
//            //var aut2 = aut1.Determinize(solver);
//            //var aut3 = aut2.Minimize(solver);

//            return aut1;
//        }
//    }
//}


//namespace Microsoft.Automata.MSO
//{
//    public abstract class WS1SFormula<T>
//    {
//        public Automaton<T> getAutomaton(IBooleanAlgebra<T> solver)
//        {
//            var alg = new BBDDAlgebra();
//            var ca = new CartesianAlgebra<BBDD, T>(alg, solver);
//            var aut = getAutomaton(new List<string>(), ca);
//            var newMoves = new List<Move<T>>();
//            foreach (var move in aut.GetMoves())
//                newMoves.Add(new Move<T>(move.SourceState, move.TargetState, move.Label.ProjectSecond()));
//            return Automaton<T>.Create(aut.InitialState, aut.GetFinalStates(), newMoves);
//        }

//        internal abstract Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver);
//    }

//    public class WS1SAnd<T> : WS1SFormula<T>
//    {
//        public WS1SFormula<T> phi1;
//        public WS1SFormula<T> phi2;

//        public WS1SAnd(WS1SFormula<T> phi1, WS1SFormula<T> phi2)
//        {
//            this.phi1 = phi1;
//            this.phi2 = phi2;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            var aut1 = phi1.getAutomaton(variables, solver);
//            var aut2 = phi2.getAutomaton(variables, solver);
//            return aut1.Intersect(aut2, solver);//.Determinize(solver).Minimize(solver);
//        }
//    }

//    public class WS1SOr<T> : WS1SFormula<T>
//    {
//        public WS1SFormula<T> phi1;
//        public WS1SFormula<T> phi2;

//        public WS1SOr(WS1SFormula<T> phi1, WS1SFormula<T> phi2)
//        {
//            this.phi1 = phi1;
//            this.phi2 = phi2;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            var aut1 = phi1.getAutomaton(variables, solver);
//            var aut2 = phi2.getAutomaton(variables, solver);
//            return aut1.Union(aut2, solver);//.Determinize(solver).Minimize(solver);
//        }
//    }

//    public class WS1SNot<T> : WS1SFormula<T>
//    {
//        public WS1SFormula<T> phi;

//        public WS1SNot(WS1SFormula<T> phi)
//        {
//            this.phi = phi;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            //Create condition that only considers bvs of size |variables|
//            var bddAlg = (BBDDAlgebra)solver.First;
//            var trueBv = bddAlg.MkTrue(variables.Count);

//            var pairCond = solver.MkProduct(trueBv, solver.Second.True);
//            var moves = new Move<ISumOfProducts<BBDD, T>>[] { new Move<ISumOfProducts<BBDD, T>>(0, 0, pairCond) };

//            //True automaton and then difference
//            var trueAut = Automaton<ISumOfProducts<BBDD, T>>.Create(0, new int[] { 0 }, moves);
//            var aut = phi.getAutomaton(variables, solver);

//            var a = trueAut.Minus(aut, solver).Determinize(solver).Minimize(solver);
//            return a;
//        }
//    }

//    public class WS1SSubset<T> : WS1SFormula<T>
//    {
//        public string var1;
//        public string var2;

//        public WS1SSubset(string var1, string var2)
//        {
//            this.var1 = var1;
//            this.var2 = var2;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            var pos1 = variables.IndexOf(var1);
//            var pos2 = variables.IndexOf(var2);

//            //Create conditions that bit in pos1 is smaller than pos2
//            var bbddAlg = (BBDDAlgebra)solver.First;
//            var pos2is1 = bbddAlg.MkSetWithBitTrue(pos2, variables.Count);
//            var pos1is0 = bbddAlg.MkSetWithBitFalse(pos1, variables.Count);
//            var subsetCond = bbddAlg.MkOr(pos2is1, pos1is0);

//            //Create automaton for condition
//            var pairCond = solver.MkProduct(subsetCond, solver.Second.True);
//            var moves = new Move<ISumOfProducts<BBDD, T>>[] { new Move<ISumOfProducts<BBDD, T>>(0, 0, pairCond) };
//            return Automaton<ISumOfProducts<BBDD, T>>.Create(0, new int[] { 0 }, moves);
//        }
//    }

//    public class WS1SSingleton<T> : WS1SFormula<T>
//    {
//        public string var;

//        public WS1SSingleton(string var)
//        {
//            this.var = var;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            var pos = variables.IndexOf(var);

//            //Create conditions that bit in pos1 is smaller than pos2
//            var bbddAlg = (BBDDAlgebra)solver.First;
//            var posIs1 = bbddAlg.MkSetWithBitTrue(pos, variables.Count);
//            var posIs0 = bbddAlg.MkSetWithBitFalse(pos, variables.Count);

//            //Create automaton for condition
//            var moves = new Move<ISumOfProducts<BBDD, T>>[] { 
//                new Move<ISumOfProducts<BBDD,T>>(0, 0, solver.MkProduct(posIs0, solver.Second.True)),
//                new Move<ISumOfProducts<BBDD,T>>(0, 1, solver.MkProduct(posIs1, solver.Second.True)), 
//                new Move<ISumOfProducts<BBDD,T>>(1, 1, solver.MkProduct(posIs0, solver.Second.True))
//            };
//            return Automaton<ISumOfProducts<BBDD, T>>.Create(0, new int[] { 1 }, moves);
//        }
//    }

//    public class WS1SPred<T> : WS1SFormula<T>
//    {
//        public T pred;
//        public string var;

//        public WS1SPred(T pred, string var)
//        {
//            this.pred = pred;
//            this.var = var;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            var k = variables.IndexOf(var);

//            //Compute predicates for k'th bit is 0 or 1
//            var charSolver = (BBDDAlgebra)solver.First;
//            var posIs1 = charSolver.MkSetWithBitTrue(k, variables.Count);
//            var posIs0 = charSolver.MkSetWithBitFalse(k, variables.Count);

//            var psi1 = solver.MkProduct(posIs0, solver.Second.True);
//            var psi2 = solver.MkProduct(posIs1, pred);
//            var psi = solver.MkOr(psi1, psi2);

//            //Create automaton for condition
//            var moves = new Move<ISumOfProducts<BBDD, T>>[] { 
//                new Move<ISumOfProducts<BBDD,T>>(0, 0, psi),
//            };
//            return Automaton<ISumOfProducts<BBDD, T>>.Create(0, new int[] { 0 }, moves);
//        }
//    }

//    public class WS1SSuccN<T> : WS1SFormula<T>
//    {
//        string var1;
//        string var2;
//        int gap;

//        public WS1SSuccN(string var1, string var2, int gap)
//        {
//            if (gap < 1)
//                throw new Exception("gap should be at least 1");
//            this.var1 = var1;
//            this.var2 = var2;
//            this.gap = gap;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            var ind1 = variables.IndexOf(var1);
//            var ind2 = variables.IndexOf(var2);

//            var bbddAlg = (BBDDAlgebra)solver.First;
//            var ind10 = bbddAlg.MkSetWithBitFalse(ind1,variables.Count);
//            var ind11 = bbddAlg.MkSetWithBitTrue(ind1, variables.Count);
//            var ind20 = bbddAlg.MkSetWithBitFalse(ind2, variables.Count);
//            var ind21 = bbddAlg.MkSetWithBitTrue(ind2, variables.Count);
//            var both0 = bbddAlg.MkAnd(ind10, ind20);
//            var ind11ind20 = bbddAlg.MkAnd(ind11, ind20);
//            var ind10ind21 = bbddAlg.MkAnd(ind10, ind21);

//            var both0t = solver.MkProduct(both0, solver.Second.True);
//            var ind11ind20t = solver.MkProduct(ind11ind20, solver.Second.True);
//            var ind10ind21t = solver.MkProduct(ind10ind21, solver.Second.True);

//            //Create moves
//            var moves = new List<Move<ISumOfProducts<BBDD, T>>>();
//            moves.Add(new Move<ISumOfProducts<BBDD, T>>(0, 0, both0t));
//            moves.Add(new Move<ISumOfProducts<BBDD, T>>(0, 1, ind11ind20t));
//            moves.Add(new Move<ISumOfProducts<BBDD, T>>(gap, gap + 1, ind10ind21t));
//            moves.Add(new Move<ISumOfProducts<BBDD, T>>(gap + 1, gap + 1, both0t));

//            for (int i = 1; i < gap; i++)
//                moves.Add(new Move<ISumOfProducts<BBDD, T>>(i, i + 1, both0t));

//            return Automaton<ISumOfProducts<BBDD, T>>.Create(0, new int[] { gap + 1 }, moves);
//        }
//    }

//    public class WS1SLt<T> : WS1SFormula<T>
//    {
//        public string var1;
//        public string var2;

//        public WS1SLt(string var1, string var2)
//        {
//            this.var1 = var1;
//            this.var2 = var2;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            var pos1 = variables.IndexOf(var1);
//            var pos2 = variables.IndexOf(var2);

//            var alg = (BBDDAlgebra)solver.First;
//            var pos1is0 = alg.MkSetWithBitFalse(pos1, variables.Count);
//            var pos1is1 = alg.MkSetWithBitTrue(pos1, variables.Count);
//            var pos2is0 = alg.MkSetWithBitFalse(pos2, variables.Count);
//            var pos2is1 = alg.MkSetWithBitTrue(pos2, variables.Count);

//            var both0 = alg.MkAnd(pos1is0, pos2is0);
//            var pos11pos20 = alg.MkAnd(pos1is1, pos2is0);
//            var pos10pos21 = alg.MkAnd(pos1is0, pos2is1);

//            //Create automaton for condition
//            var moves = new Move<ISumOfProducts<BBDD, T>>[] { 
//                new Move<ISumOfProducts<BBDD,T>>(0, 0, solver.MkProduct(both0,solver.Second.True)),
//                new Move<ISumOfProducts<BBDD,T>>(0, 1, solver.MkProduct(pos11pos20,solver.Second.True)), 
//                new Move<ISumOfProducts<BBDD,T>>(1, 1, solver.MkProduct(both0,solver.Second.True)),
//                new Move<ISumOfProducts<BBDD,T>>(1, 2, solver.MkProduct(pos10pos21,solver.Second.True)), 
//                new Move<ISumOfProducts<BBDD,T>>(2, 2, solver.MkProduct(both0,solver.Second.True))
//            };
//            return Automaton<ISumOfProducts<BBDD, T>>.Create(0, new int[] { 2 }, moves);
//        }
//    }

//    public class WS1SEq<T> : WS1SFormula<T>
//    {
//        public string var1;
//        public string var2;

//        public WS1SEq(string var1, string var2)
//        {
//            this.var1 = var1;
//            this.var2 = var2;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            var pos1 = variables.IndexOf(var1);
//            var pos2 = variables.IndexOf(var2);

//            //Create conditions that bit in pos1 is smaller than pos2
//            var alg = (BBDDAlgebra)solver.First;
//            var both1 = alg.MkAnd(alg.MkSetWithBitTrue(pos1,variables.Count), alg.MkSetWithBitTrue(pos2,variables.Count));
//            var both0 = alg.MkAnd(alg.MkSetWithBitFalse(pos1,variables.Count), alg.MkSetWithBitFalse(pos2,variables.Count));
//            var eqCond = alg.MkOr(both0, both1);

//            //Create automaton for condition
//            var moves = new Move<ISumOfProducts<BBDD, T>>[] { 
//                new Move<ISumOfProducts<BBDD,T>>(0, 0, solver.MkProduct(eqCond,solver.Second.True)) 
//            };
//            return Automaton<ISumOfProducts<BBDD, T>>.Create(0, new int[] { 0 }, moves);
//        }
//    }

//    public class WS1STrue<T> : WS1SFormula<T>
//    {
//        public WS1STrue() { }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            var alg = (BBDDAlgebra)solver.First;
//            var trueBv = alg.MkTrue(variables.Count);
//            var moves = new Move<ISumOfProducts<BBDD, T>>[] { 
//                new Move<ISumOfProducts<BBDD, T>>(0, 0, solver.MkProduct(trueBv, solver.Second.True)) 
//            };
//            //True automaton 
//            return Automaton<ISumOfProducts<BBDD, T>>.Create(0, new int[] { 0 }, moves);
//        }
//    }

//    public class WS1SExists<T> : WS1SFormula<T>
//    {
//        public string var;
//        public WS1SFormula<T> phi;

//        public WS1SExists(string var, WS1SFormula<T> phi)
//        {
//            this.var = var;
//            this.phi = phi;
//        }

//        internal override Automaton<ISumOfProducts<BBDD, T>> getAutomaton(List<string> variables, ICartesianAlgebra<BBDD, T> solver)
//        {
//            //Automaton<T> for formula
//            var varCopy = new List<string>(variables);
//            varCopy.Add(var);
//            var autPhi = phi.getAutomaton(varCopy, solver);

//            //autPhi.ShowGraph();

//            //Remove first bit from each move
//            var newMoves = new List<Move<ISumOfProducts<BBDD, T>>>();
//            var alg = (BBDDAlgebra)solver.First;
//            foreach (var move in autPhi.GetMoves())
//            {
//                var newPred = move.Label.TransformFirst(alg.RemoveMaxBit);
//                newMoves.Add(new Move<ISumOfProducts<BBDD, T>>(move.SourceState, move.TargetState, newPred));
//            }

//            var aut1 = Automaton<ISumOfProducts<BBDD, T>>.Create(autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
//            //var aut2 = aut1.Determinize(solver);
//            //var aut3 = aut2.Minimize(solver);

//            return aut1;
//        }
//    }
//}

namespace Microsoft.Automata.MSO
{
    /// <summary>
    /// Abstract base class for generic WS1S formulas.
    /// </summary>
    public abstract class WS1SFormula<T>
    {
        public Automaton<T> GetAutomaton(IBooleanAlgebra<T> alg)
        {
            var fvs = GetFreeVariables();
            if (fvs.IsNonempty)
                throw new ArgumentException("This formula must not conatain free variables","this");

            var aut = getAutomaton(fvs, new CartesianAlgebraBDD<T>(alg));
            var newMoves = new List<Move<T>>();
            foreach (var move in aut.GetMoves())
                newMoves.Add(new Move<T>(move.SourceState, move.TargetState, move.Label.ProjectSecond()));

            return Automaton<T>.Create(aut.InitialState, aut.GetFinalStates(), newMoves);
        }

        public Automaton<ISumOfProducts<BDD, T>> GetAutomaton(ICartesianAlgebraBDD<T> ca, params string[] fv)
        {
            var fvs = SimpleList<string>.Empty.Append(fv);
            return getAutomaton(fvs, ca);
        }

        internal abstract Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> ca);

        public Automaton<BDD> GetAutomatonBDD(CharSetSolver alg, params string[] fv)
        {
            if (!(typeof(T).Equals(typeof(BDD))))
                throw new ArgumentException("Method is not supported because T is not BDD");

            return getAutomatonBDD(SimpleList<string>.Empty.Append(fv), alg); 
        }

        internal abstract Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg); 

        internal abstract IEnumerable<string> EnumerateFreeVariables();

        public SimpleList<string> GetFreeVariables()
        {
            HashSet<string> set = new HashSet<string>();
            var fvs = SimpleList<string>.Empty;
            foreach (var v in EnumerateFreeVariables())
                if (set.Add(v))
                    fvs = fvs.Append(v);
            return fvs;
        }

        public bool IsClosed
        {
            get { return !GetFreeVariables().IsNonempty; }
        }
    }

    public class WS1SAnd<T> : WS1SFormula<T>
    {
        public readonly WS1SFormula<T> phi1;
        public readonly WS1SFormula<T> phi2;

        public WS1SAnd(WS1SFormula<T> phi1, WS1SFormula<T> phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> ca)
        {
            var aut1 = phi1.getAutomaton(variables, ca);
            var aut2 = phi2.getAutomaton(variables, ca);
            return aut1.Intersect(aut2, ca).Determinize(ca).Minimize(ca);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg)
        {
            var aut1 = phi1.getAutomatonBDD(variables, alg);
            var aut2 = phi2.getAutomatonBDD(variables, alg);
            return aut1.Intersect(aut2, alg).Determinize(alg).Minimize(alg);
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            foreach (var v in phi1.EnumerateFreeVariables())
                yield return v;
            foreach (var v in phi2.EnumerateFreeVariables())
                yield return v;
        }

        public override string ToString()
        {
            return "(" + phi1.ToString() + "&" + phi2.ToString() + ")";
        }
    }

    public class WS1SOr<T> : WS1SFormula<T>
    {
        public readonly WS1SFormula<T> phi1;
        public readonly WS1SFormula<T> phi2;

        public WS1SOr(WS1SFormula<T> phi1, WS1SFormula<T> phi2)
        {
            this.phi1 = phi1;
            this.phi2 = phi2;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> alg)
        {
            var aut1 = phi1.getAutomaton(variables, alg);
            var aut2 = phi2.getAutomaton(variables, alg);
            var res = aut1.Complement(alg).Intersect(aut2.Complement(alg), alg).Complement(alg);
            res = res.Determinize(alg).Minimize(alg);
            return res;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg)
        {
            var aut1 = phi1.getAutomatonBDD(variables, alg);
            var aut2 = phi2.getAutomatonBDD(variables, alg);
            var res = aut1.Complement(alg).Intersect(aut2.Complement(alg), alg).Complement(alg);
            res = res.Determinize(alg).Minimize(alg);
            return res;
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            foreach (var v in phi1.EnumerateFreeVariables())
                yield return v;
            foreach (var v in phi2.EnumerateFreeVariables())
                yield return v;
        }

        public override string ToString()
        {
            return "(" + phi1.ToString() + "|" + phi2.ToString() + ")";
        }
    }

    public class WS1SNot<T> : WS1SFormula<T>
    {
        public readonly WS1SFormula<T> phi;

        public WS1SNot(WS1SFormula<T> phi)
        {
            this.phi = phi;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> alg)
        {
            var aut = phi.getAutomaton(variables, alg);
            var res = aut.Determinize(alg).Complement(alg).Minimize(alg);
            foreach (var x in phi.EnumerateFreeVariables())
            {
                if (char.IsLower(x[0])) //TBD: x is f.o.
                {
                    var sing = new WS1SSingleton<T>("x").getAutomaton(variables, alg);
                    res = res.Intersect(sing, alg);
                }
            }
            return res;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg)
        {
            var aut = phi.getAutomatonBDD(variables, alg);
            var res = aut.Determinize(alg).Complement(alg).Minimize(alg);
            foreach (var x in phi.EnumerateFreeVariables())
            {
                if (char.IsLower(x[0])) //TBD: x is f.o.
                {
                    var sing = new WS1SSingleton<T>(x).getAutomatonBDD(variables, alg);
                    res = res.Intersect(sing, alg);
                }
            }
            return res;
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            return phi.EnumerateFreeVariables();
        }

        public override string ToString()
        {
            return "~" + phi.ToString();
        }
    }

    public class WS1SSubset<T> : WS1SFormula<T>
    {
        public readonly string var1;
        public readonly string var2;

        public WS1SSubset(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentException("variable '" + var1 + "' is missing from the list of free variables", "variables");
            if (pos2 < 0)
                throw new ArgumentException("variable '" + var2 + "' is missing from the list of free variables", "variables"); 


            //Create conditions that bit in pos1 is smaller than pos2
            var pos2is1 = ca.BDDAlgebra.MkSetWithBitTrue(pos2);
            var pos1is0 = ca.BDDAlgebra.MkSetWithBitFalse(pos1);
            var subsetCond = ca.BDDAlgebra.MkOr(pos2is1, pos1is0);

            //Create automaton for condition
            var pairCond = ca.MkProduct(subsetCond, ca.Second.True);
            var moves = new Move<ISumOfProducts<BDD, T>>[] { new Move<ISumOfProducts<BDD, T>>(0, 0, pairCond) };
            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { 0 }, moves);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg) 
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentException("variable '" + var1 + "' is missing from the list of free variables", "variables");
            if (pos2 < 0)
                throw new ArgumentException("variable '" + var2 + "' is missing from the list of free variables", "variables");

            pos1 = pos1 + (int)alg.Encoding;
            pos2 = pos2 + (int)alg.Encoding;

            //Create conditions that bit in pos1 is smaller than pos2
            var pos2is1 = alg.MkSetWithBitTrue(pos2);
            var pos1is0 = alg.MkSetWithBitFalse(pos1);
            var subsetCond = alg.MkOr(pos2is1, pos1is0);

            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, subsetCond) };
            return Automaton<BDD>.Create(0, new int[] { 0 }, moves);
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            yield return var1;
            yield return var2;
        }

        public override string ToString()
        {
            return "Subset(" + var1 + "," + var2 + ")";
        }
    }

    public class WS1SSingleton<T> : WS1SFormula<T>
    {
        public readonly string var;

        public WS1SSingleton(string var)
        {
            this.var = var;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos = variables.IndexOf(var);

            if (pos < 0)
                throw new ArgumentException("variable '" + var + "' is missing from the given list", "variables");

            //Create conditions that bit in pos1 is smaller than pos2
            var bbddAlg = ca.BDDAlgebra;
            var posIs1 = bbddAlg.MkSetWithBitTrue(pos);
            var posIs0 = bbddAlg.MkSetWithBitFalse(pos);

            //Create automaton for condition
            var moves = new Move<ISumOfProducts<BDD, T>>[] { 
                new Move<ISumOfProducts<BDD,T>>(0, 0, ca.MkProduct(posIs0, ca.Second.True)),
                new Move<ISumOfProducts<BDD,T>>(0, 1, ca.MkProduct(posIs1, ca.Second.True)), 
                new Move<ISumOfProducts<BDD,T>>(1, 1, ca.MkProduct(posIs0, ca.Second.True))
            };
            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { 1 }, moves);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg)
        {
            var pos = variables.IndexOf(var);

            if (pos < 0)
                throw new ArgumentException("variable '" + var + "' is missing from the given list", "variables");

            pos = pos + (int)alg.Encoding;

            //Create conditions that bit in pos1 is smaller than pos2
            var posIs1 = alg.MkSetWithBitTrue(pos);
            var posIs0 = alg.MkSetWithBitFalse(pos);

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, posIs0),
                new Move<BDD>(0, 1, posIs1), 
                new Move<BDD>(1, 1, posIs0)
            };
            return Automaton<BDD>.Create(0, new int[] { 1 }, moves);
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            yield return var;
        }

        public override string ToString()
        {
            return "Singleton(" + var + ")";
        }
    }

    public class WS1SPred<T> : WS1SFormula<T>
    {
        public readonly T pred;
        public readonly string var;

        public WS1SPred(T pred, string var)
        {
            this.pred = pred;
            this.var = var;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> ca)
        {
            var k = variables.IndexOf(var);

            if (k < 0)
                throw new ArgumentException("variable '" + var + "' is missing from the given list", "variables");

            //Compute predicates for k'th bit is 0 or 1
            var bddAlg = ca.BDDAlgebra;
            var posIs1 = bddAlg.MkSetWithBitTrue(k);
            var posIs0 = bddAlg.MkSetWithBitFalse(k);

            var psi1 = ca.MkProduct(posIs0, ca.Second.True);
            var psi2 = ca.MkProduct(posIs1, pred);
            var psi = ca.MkOr(psi1, psi2);

            //Create automaton for condition
            var moves = new Move<ISumOfProducts<BDD, T>>[] { 
                new Move<ISumOfProducts<BDD,T>>(0, 0, psi),
            };
            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { 0 }, moves);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg)
        {
            var k = variables.IndexOf(var);

            if (k < 0)
                throw new ArgumentException("variable '" + var + "' is missing from the given list", "variables");

            k = k + (int)alg.Encoding;

            //Compute predicates for k'th bit is 0 or 1
            var posIs1 = alg.MkSetWithBitTrue(k);
            var posIs0 = alg.MkSetWithBitFalse(k);

            BDD predBDD = pred as BDD;              //T is known to be BDD
            var psi2 = alg.MkAnd(posIs1, predBDD);  //builds the Cartesian product
            var psi = alg.MkOr(posIs0, psi2);

            //Create automaton for condition
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, psi) };
            return Automaton<BDD>.Create(0, new int[] { 0 }, moves);
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            yield return var;
        }

        public override string ToString()
        {
            return "[" + pred.ToString() + "](" + var + ")";
        }
    }

    public class WS1SSuccN<T> : WS1SFormula<T>
    {
        public readonly string var1;
        public readonly string var2;
        public readonly int n;

        public WS1SSuccN(string var1, string var2, int n)
        {
            if (n < 1)
                throw new ArgumentException("successor offset must be positive", "n");

            this.var1 = var1;
            this.var2 = var2;
            this.n = n;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> ca)
        {
            var ind1 = variables.IndexOf(var1);
            var ind2 = variables.IndexOf(var2);

            if (ind1 < 0)
                throw new ArgumentException("variable '" + var1 + "' is missing from the list of free variables", "variables");
            if (ind2 < 0)
                throw new ArgumentException("variable '" + var2 + "' is missing from the list of free variables", "variables");

            var bddAlg = ca.BDDAlgebra;
            var ind10 = bddAlg.MkSetWithBitFalse(ind1);
            var ind11 = bddAlg.MkSetWithBitTrue(ind1);
            var ind20 = bddAlg.MkSetWithBitFalse(ind2);
            var ind21 = bddAlg.MkSetWithBitTrue(ind2);
            var both0 = bddAlg.MkAnd(ind10, ind20);
            var ind11ind20 = bddAlg.MkAnd(ind11, ind20);
            var ind10ind21 = bddAlg.MkAnd(ind10, ind21);

            var both0t = ca.MkProduct(both0, ca.Second.True);
            var ind11ind20t = ca.MkProduct(ind11ind20, ca.Second.True);
            var ind10ind21t = ca.MkProduct(ind10ind21, ca.Second.True);

            //Create moves
            var moves = new List<Move<ISumOfProducts<BDD, T>>>();
            moves.Add(new Move<ISumOfProducts<BDD, T>>(0, 0, both0t));
            moves.Add(new Move<ISumOfProducts<BDD, T>>(0, 1, ind11ind20t));
            moves.Add(new Move<ISumOfProducts<BDD, T>>(n, n + 1, ind10ind21t));
            moves.Add(new Move<ISumOfProducts<BDD, T>>(n + 1, n + 1, both0t));

            for (int i = 1; i < n; i++)
                moves.Add(new Move<ISumOfProducts<BDD, T>>(i, i + 1, both0t));

            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { n + 1 }, moves);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg) 
        {
            var ind1 = variables.IndexOf(var1);
            var ind2 = variables.IndexOf(var2);

            if (ind1 < 0)
                throw new ArgumentException("variable '" + var1 + "' is missing from the list of free variables", "variables");
            if (ind2 < 0)
                throw new ArgumentException("variable '" + var2 + "' is missing from the list of free variables", "variables");

            ind1 = ind1 + (int)alg.Encoding;
            ind2 = ind2 + (int)alg.Encoding;


            var bddAlg = alg;
            var ind10 = bddAlg.MkSetWithBitFalse(ind1);
            var ind11 = bddAlg.MkSetWithBitTrue(ind1);
            var ind20 = bddAlg.MkSetWithBitFalse(ind2);
            var ind21 = bddAlg.MkSetWithBitTrue(ind2);
            var both0 = bddAlg.MkAnd(ind10, ind20);
            var ind11ind20 = bddAlg.MkAnd(ind11, ind20);
            var ind10ind21 = bddAlg.MkAnd(ind10, ind21);

            var both0t = both0;
            var ind11ind20t = ind11ind20;
            var ind10ind21t = ind10ind21;

            //Create moves
            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, both0t));
            moves.Add(new Move<BDD>(0, 1, ind11ind20t));
            moves.Add(new Move<BDD>(n, n + 1, ind10ind21t));
            moves.Add(new Move<BDD>(n + 1, n + 1, both0t));

            for (int i = 1; i < n; i++)
                moves.Add(new Move<BDD>(i, i + 1, both0t));

            return Automaton<BDD>.Create(0, new int[] { n + 1 }, moves);
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            yield return var1;
            yield return var2;
        }

        public override string ToString()
        {
            return "(" + var2 + "=" + var1 + "+" + n.ToString() + ")"; 
        }
    }

    public class WS1SLt<T> : WS1SFormula<T>
    {
        public readonly string var1;
        public readonly string var2;

        public WS1SLt(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentException("variable '" + var1 + "' is missing from the list of free variables", "variables");
            if (pos2 < 0)
                throw new ArgumentException("variable '" + var2 + "' is missing from the list of free variables", "variables");

            var bddAlg = ca.BDDAlgebra;
            var pos1is0 = bddAlg.MkSetWithBitFalse(pos1);
            var pos1is1 = bddAlg.MkSetWithBitTrue(pos1);
            var pos2is0 = bddAlg.MkSetWithBitFalse(pos2);
            var pos2is1 = bddAlg.MkSetWithBitTrue(pos2);

            var both0 = bddAlg.MkAnd(pos1is0, pos2is0);
            var pos11pos20 = bddAlg.MkAnd(pos1is1, pos2is0);
            var pos10pos21 = bddAlg.MkAnd(pos1is0, pos2is1);

            //Create automaton for condition
            var moves = new Move<ISumOfProducts<BDD, T>>[] { 
                new Move<ISumOfProducts<BDD,T>>(0, 0, ca.MkProduct(both0,ca.Second.True)),
                new Move<ISumOfProducts<BDD,T>>(0, 1, ca.MkProduct(pos11pos20,ca.Second.True)), 
                new Move<ISumOfProducts<BDD,T>>(1, 1, ca.MkProduct(both0,ca.Second.True)),
                new Move<ISumOfProducts<BDD,T>>(1, 2, ca.MkProduct(pos10pos21,ca.Second.True)), 
                new Move<ISumOfProducts<BDD,T>>(2, 2, ca.MkProduct(both0,ca.Second.True))
            };
            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { 2 }, moves);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg) 
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentException("variable '" + var1 + "' is missing from the list of free variables", "variables");
            if (pos2 < 0)
                throw new ArgumentException("variable '" + var2 + "' is missing from the list of free variables", "variables");

            pos1 = pos1 + (int)alg.Encoding;
            pos2 = pos2 + (int)alg.Encoding;


            var bddAlg = alg;
            var pos1is0 = bddAlg.MkSetWithBitFalse(pos1);
            var pos1is1 = bddAlg.MkSetWithBitTrue(pos1);
            var pos2is0 = bddAlg.MkSetWithBitFalse(pos2);
            var pos2is1 = bddAlg.MkSetWithBitTrue(pos2);

            var both0 = bddAlg.MkAnd(pos1is0, pos2is0);
            var pos11pos20 = bddAlg.MkAnd(pos1is1, pos2is0);
            var pos10pos21 = bddAlg.MkAnd(pos1is0, pos2is1);

            //Create automaton for condition
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, both0),
                new Move<BDD>(0, 1, pos11pos20), 
                new Move<BDD>(1, 1, both0),
                new Move<BDD>(1, 2, pos10pos21), 
                new Move<BDD>(2, 2, both0)
            };
            return Automaton<BDD>.Create(0, new int[] { 2 }, moves);
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            yield return var1;
            yield return var2;
        }

        public override string ToString()
        {
            return "(" + var1 + "<" + var2 + ")";
        }
    }

    public class WS1SEq<T> : WS1SFormula<T>
    {
        public readonly string var1;
        public readonly string var2;

        public WS1SEq(string var1, string var2)
        {
            this.var1 = var1;
            this.var2 = var2;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> ca)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentException("variable '" + var1 + "' is missing from the list of free variables", "variables");
            if (pos2 < 0)
                throw new ArgumentException("variable '" + var2 + "' is missing from the list of free variables", "variables");

            //Create conditions that bit in pos1 is smaller than pos2
            var both1 = ca.BDDAlgebra.MkAnd(ca.BDDAlgebra.MkSetWithBitTrue(pos1), ca.BDDAlgebra.MkSetWithBitTrue(pos2));
            var both0 = ca.BDDAlgebra.MkAnd(ca.BDDAlgebra.MkSetWithBitFalse(pos1), ca.BDDAlgebra.MkSetWithBitFalse(pos2));
            var eqCond = ca.BDDAlgebra.MkOr(both0, both1);

            //Create automaton for condition
            var moves = new Move<ISumOfProducts<BDD, T>>[] { 
                new Move<ISumOfProducts<BDD,T>>(0, 0, ca.MkProduct(eqCond,ca.Second.True)) 
            };
            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { 0 }, moves);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg)
        {
            var pos1 = variables.IndexOf(var1);
            var pos2 = variables.IndexOf(var2);

            if (pos1 < 0)
                throw new ArgumentException("variable '" + var1 + "' is missing from the list of free variables", "variables");
            if (pos2 < 0)
                throw new ArgumentException("variable '" + var2 + "' is missing from the list of free variables", "variables");

            pos1 = pos1 + (int)alg.Encoding;
            pos2 = pos2 + (int)alg.Encoding;

            //Create conditions that bit in pos1 is smaller than pos2
            var both1 = alg.MkAnd(alg.MkSetWithBitTrue(pos1), alg.MkSetWithBitTrue(pos2));
            var both0 = alg.MkAnd(alg.MkSetWithBitFalse(pos1), alg.MkSetWithBitFalse(pos2));
            var eqCond = alg.MkOr(both0, both1);

            //Create automaton for condition
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, eqCond) };
            return Automaton<BDD>.Create(0, new int[] { 0 }, moves);
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            yield return var1;
            yield return var2;
        }

        public override string ToString()
        {
            return "(" + var1 + "=" + var2 + ")";
        }
    }

    public class WS1STrue<T> : WS1SFormula<T>
    {
        public WS1STrue() { }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> ca)
        {
            var moves = new Move<ISumOfProducts<BDD, T>>[] { 
                new Move<ISumOfProducts<BDD, T>>(0, 0, ca.True) 
            };
            return Automaton<ISumOfProducts<BDD, T>>.Create(0, new int[] { 0 }, moves);
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg)
        {
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, alg.True) 
            };
            return Automaton<BDD>.Create(0, new int[] { 0 }, moves);
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            yield break;
        }
        public override string ToString()
        {
            return "true";
        }
    }

    public class WS1SExists<T> : WS1SFormula<T>
    {
        public readonly string var;
        public readonly WS1SFormula<T> phi;

        public WS1SExists(string var, WS1SFormula<T> phi)
        {
            this.var = var;
            this.phi = phi;
        }

        internal override Automaton<ISumOfProducts<BDD, T>> getAutomaton(SimpleList<string> variables, ICartesianAlgebraBDD<T> alg)
        {
            //the existential variable will shadow any previous occurrence with the same name
            //because when looking up the index of var it will be the last occurrence
            var varIndex = variables.Count; 
            var variablesExt = variables.Append(var);
            var autPhi = phi.getAutomaton(variablesExt, alg); 

            //Project away the the existential variable
            var newMoves = new List<Move<ISumOfProducts<BDD, T>>>();
            foreach (var move in autPhi.GetMoves())
            {
                var newPred = move.Label.TransformFirst(bdd => alg.BDDAlgebra.ProjectBit(bdd, varIndex));
                newMoves.Add(new Move<ISumOfProducts<BDD, T>>(move.SourceState, move.TargetState, newPred));
            }

            var res = Automaton<ISumOfProducts<BDD, T>>.Create(autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
            //res = res.Determinize(alg).Minimize(alg);

            return res;
        }

        internal override Automaton<BDD> getAutomatonBDD(SimpleList<string> variables, CharSetSolver alg)
        {
            //the existential variable will shadow any previous occurrence with the same name
            //because when looking up the index of var it will be the last occurrence
            var varIndex = variables.Count + (int)alg.Encoding;
            var variablesExt = variables.Append(var);
            var autPhi = phi.getAutomatonBDD(variablesExt, alg);

            //Project away the the existential variable
            var newMoves = new List<Move<BDD>>();
            foreach (var move in autPhi.GetMoves())
                newMoves.Add(new Move<BDD>(move.SourceState, move.TargetState, alg.ProjectBit(move.Label, varIndex)));

            var aut = Automaton<BDD>.Create(autPhi.InitialState, autPhi.GetFinalStates(), newMoves);
            var res = aut.Determinize(alg);
            res = res.Minimize(alg);

            return res;
        }

        internal override IEnumerable<string> EnumerateFreeVariables()
        {
            foreach (var v in phi.EnumerateFreeVariables())
                if (!var.Equals(v))
                    yield return v;
        }

        public override string ToString()
        {
            return "Exists(" + var + "," + phi.ToString() + ")";
        }
    }
}