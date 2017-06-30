using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Automata.BooleanAlgebras;

namespace Microsoft.Automata.MSO
{
    public static class BasicAutomata
    {
        /// <summary>
        /// x_i in X_j with singleton-set-semantics for x_i
        /// </summary>
        internal static Automaton<BDD> MkIn1(int i, int j, IBDDAlgebra alg)
        {
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_i_is0 = alg.MkBitFalse(i);
            var both1 = alg.MkAnd(bit_i_is1, bit_j_is1);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, bit_i_is0), 
                new Move<BDD>(0, 1, both1), new Move<BDD>(1, 1, bit_i_is0) };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }
        /// <summary>
        /// x_i in X_j with singleton-set-semantics for x_i
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkIn1<T>(int i, int j, ICartesianAlgebraBDD<T> ca)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> lift = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            var alg = ca.BDDAlgebra;
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_i_is0 = alg.MkBitFalse(i);
            var both1 = alg.MkAnd(bit_i_is1, bit_j_is1);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, lift(bit_i_is0)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, lift(both1)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, lift(bit_i_is0)) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(ca, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i in X_j with min(nonempty-set)-semantics for x_i
        /// </summary>
        public static Automaton<BDD> MkIn2(int i, int j, IBDDAlgebra alg)
        {
            var ieq0 = alg.MkBitFalse(i); 
            var ieq1 = alg.MkBitTrue(i);
            var jeq1 = alg.MkBitTrue(j);
            var ieq1_jeq1 = alg.MkAnd(jeq1, ieq1);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, ieq0), 
                new Move<BDD>(0, 1, ieq1_jeq1),
                new Move<BDD>(1, 1, alg.True)
            };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i in X_j with min(nonempty-set)-semantics for x_i
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkIn2<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var ieq0 = alg.BDDAlgebra.MkBitFalse(i);
            var ieq1 = alg.BDDAlgebra.MkBitTrue(i);
            var jeq1 = alg.BDDAlgebra.MkBitTrue(j);
            var ieq1_jeq1 = alg.BDDAlgebra.MkAnd(jeq1, ieq1);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(ieq0, alg.Second.True)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, alg.MkCartesianProduct(ieq1_jeq1, alg.Second.True)),
                new Move<IMonadicPredicate<BDD, T>>(1, 1, alg.True)
            };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// X_i sub X_j
        /// </summary>
        public static Automaton<BDD> MkSubset(int i, int j, IBDDAlgebra alg)
        {
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is0 = alg.MkBitFalse(i);
            var subsetCond = alg.MkOr(bit_j_is1, bit_i_is0);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, subsetCond) };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// X_i sub X_j
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkSubset<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var bit_j_is1 = alg.BDDAlgebra.MkBitTrue(j);
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var subsetCond = alg.BDDAlgebra.MkOr(bit_j_is1, bit_i_is0);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(subsetCond, alg.Second.True)) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// X_i = X_j
        /// </summary>
        public static Automaton<BDD> MkEqualSets(int i, int j, IBDDAlgebra alg)
        {
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var cond = alg.MkOr(alg.MkAnd(bit_i_is1, bit_j_is1), alg.MkAnd(bit_i_is0, bit_j_is0));
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, cond) };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// X_i = X_j
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkEqualSets<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var bit_j_is1 = alg.BDDAlgebra.MkBitTrue(j);
            var bit_i_is1 = alg.BDDAlgebra.MkBitTrue(i);
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var bit_j_is0 = alg.BDDAlgebra.MkBitFalse(j);
            var cond = alg.MkCartesianProduct(alg.BDDAlgebra.MkOr(alg.BDDAlgebra.MkAnd(bit_i_is1, bit_j_is1), alg.BDDAlgebra.MkAnd(bit_i_is0, bit_j_is0)), alg.Second.True);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { new Move<IMonadicPredicate<BDD, T>>(0, 0, cond) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i = x_j, with singleton-set-semantics for f-o vars
        /// </summary>
        public static Automaton<BDD> MkEqualPositions1(int i, int j, IBDDAlgebra alg)
        {
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var both0 = alg.MkAnd(bit_i_is0, bit_j_is0);
            var both1 = alg.MkAnd(bit_i_is1, bit_j_is1);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, both0), 
                new Move<BDD>(0, 1, both1), new Move<BDD>(1, 1, both0) };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i = x_j, with min(nonempty-set)-semantics for f-o vars
        /// </summary>
        public static Automaton<BDD> MkEqualPositions2(int i, int j, IBDDAlgebra alg)
        {
            var bit_j_is1 = alg.MkBitTrue(j);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var both0 = alg.MkAnd(bit_i_is0, bit_j_is0);
            var both1 = alg.MkAnd(bit_i_is1, bit_j_is1);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, both0), 
                new Move<BDD>(0, 1, both1), new Move<BDD>(1, 1, alg.True) };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i = x_j, with singleton-set-semantics for f-o vars
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkEqualPositions1<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var bit_j_is1 = alg.BDDAlgebra.MkBitTrue(j);
            var bit_i_is1 = alg.BDDAlgebra.MkBitTrue(i);
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var bit_j_is0 = alg.BDDAlgebra.MkBitFalse(j);
            var both0 = alg.BDDAlgebra.MkAnd(bit_i_is0, bit_j_is0);
            var both1 = alg.BDDAlgebra.MkAnd(bit_i_is1, bit_j_is1);
            Func<BDD, IMonadicPredicate<BDD, T>> lift =
                bdd => alg.MkCartesianProduct(bdd, alg.Second.True);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, lift(both0)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, lift(both1)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, lift(both0)) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i = x_j, with min(nonempty-set)-semantics for f-o vars
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkEqualPositions2<T>(int i, int j, ICartesianAlgebraBDD<T> alg) 
        {
            var bit_j_is1 = alg.BDDAlgebra.MkBitTrue(j);
            var bit_i_is1 = alg.BDDAlgebra.MkBitTrue(i);
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var bit_j_is0 = alg.BDDAlgebra.MkBitFalse(j);
            var both0 = alg.BDDAlgebra.MkAnd(bit_i_is0, bit_j_is0);
            var both1 = alg.BDDAlgebra.MkAnd(bit_i_is1, bit_j_is1);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(both0, alg.Second.True)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, alg.MkCartesianProduct(both1, alg.Second.True)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, alg.True) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// all1 x: x in X_i => [psi](x)
        /// </summary>
        public static Automaton<BDD> MkLabelOfSet(int i, BDD psi, IBDDAlgebra alg)
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var cond = alg.MkOr(psi, bit_i_is0);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, cond) };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// all1 x: x in X_i => [psi](x)
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkLabelOfSet<T>(int i, T psi, ICartesianAlgebraBDD<T> ca)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> lift1 = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            Func<T, IMonadicPredicate<BDD, T>> lift2 = p => ca.MkCartesianProduct(ca.BDDAlgebra.True, p);
            var bit_i_is0 = ca.BDDAlgebra.MkBitFalse(i);
            var cond = ca.MkOr(lift2(psi), lift1(bit_i_is0));
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { new Move<IMonadicPredicate<BDD, T>>(0, 0, cond) };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(ca, 0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// [psi](x_i) with singleton-set-semantics
        /// </summary>
        /// <returns></returns>
        public static Automaton<BDD> MkLabelOfPosition1(int i, BDD psi, IBDDAlgebra alg)
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var cond = alg.MkAnd(bit_i_is1, psi);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, bit_i_is0),
                new Move<BDD>(0, 1, cond), 
                new Move<BDD>(1, 1, bit_i_is0)};
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// [psi](x_i) with min(nonempty-set)-semantics
        /// </summary>
        /// <returns></returns>
        public static Automaton<BDD> MkLabelOfPosition2(int i, BDD psi, IBDDAlgebra alg) 
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var cond = alg.MkAnd(bit_i_is1, psi);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, bit_i_is0),
                new Move<BDD>(0, 1, cond), 
                new Move<BDD>(1, 1, alg.True)};
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// [psi](x_i) with singleton-set-semantics
        /// </summary>
        /// <returns></returns>
        public static Automaton<IMonadicPredicate<BDD, T>> MkLabelOfPosition1<T>(int i, T lab, ICartesianAlgebraBDD<T> ca)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> lift1 = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            Func<T, IMonadicPredicate<BDD, T>> lift2 = psi => ca.MkCartesianProduct(ca.BDDAlgebra.True, psi);
            var alg = ca.BDDAlgebra;
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var cond = ca.MkCartesianProduct(bit_i_is1, lab);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, lift1(bit_i_is0)),
                new Move<IMonadicPredicate<BDD, T>>(0, 1, cond), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, lift1(bit_i_is0))};
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(ca, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// [psi](x_i) with min(nonempty-set)-semantics
        /// </summary>
        /// <returns></returns>
        public static Automaton<IMonadicPredicate<BDD, T>> MkLabelOfPosition2<T>(int i, T pred, ICartesianAlgebraBDD<T> alg)  
        {
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var bit_i_is1 = alg.BDDAlgebra.MkBitTrue(i);
            var cond = alg.MkCartesianProduct(bit_i_is1, pred);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(bit_i_is0,alg.Second.True)),
                new Move<IMonadicPredicate<BDD, T>>(0, 1, cond), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, alg.True)};
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i &lt; x_j, with singleton-set semantics for f-o vars
        /// </summary>
        public static Automaton<BDD> MkLt1(int i, int j, IBDDAlgebra alg)
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var bit_j_is1 = alg.MkBitTrue(j);
            var both0 = alg.MkAnd(bit_i_is0, bit_j_is0);
            var cond1 = alg.MkAnd(bit_i_is1, bit_j_is0);
            var cond2 = alg.MkAnd(bit_j_is1, bit_i_is0);
            var moves = new Move<BDD>[] { new Move<BDD>(0, 0, both0),
                new Move<BDD>(0, 1, cond1), new Move<BDD>(1, 1, both0),
                new Move<BDD>(1, 2, cond2), new Move<BDD>(2, 2, both0)};
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 2 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i &lt; x_j, with singleton-set-semantics for f-o vars
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkLt1<T>(int i, int j, ICartesianAlgebraBDD<T> ca)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> lift1 = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            var alg = ca.BDDAlgebra;
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var bit_j_is1 = alg.MkBitTrue(j);
            var both0 = alg.MkAnd(bit_i_is0, bit_j_is0);
            var cond1 = alg.MkAnd(bit_i_is1, bit_j_is0);
            var cond2 = alg.MkAnd(bit_j_is1, bit_i_is0);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, lift1(both0)),
                new Move<IMonadicPredicate<BDD, T>>(0, 1, lift1(cond1)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, lift1(both0)),
                new Move<IMonadicPredicate<BDD, T>>(1, 2, lift1(cond2)), 
                new Move<IMonadicPredicate<BDD, T>>(2, 2, lift1(both0))};
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(ca, 0, new int[] { 2 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i &lt; x_j, with min(nonempty-set)-semantics for f-o vars
        /// </summary>
        public static Automaton<BDD> MkLt2(int i, int j, IBDDAlgebra alg)
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var bit_j_is1 = alg.MkBitTrue(j);
            var both0 = alg.MkAnd(bit_i_is0, bit_j_is0);
            var bit_i_is1_bit_j_is0 = alg.MkAnd(bit_i_is1, bit_j_is0);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, both0),
                new Move<BDD>(0, 1, bit_i_is1_bit_j_is0), 
                new Move<BDD>(1, 1, bit_j_is0),
                new Move<BDD>(1, 2, bit_j_is1), 
                new Move<BDD>(2, 2, alg.True)};
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 2 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i &lt; x_j, with min(nonempty-set)-semantics for f-o vars
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkLt2<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var bit_i_is1 = alg.BDDAlgebra.MkBitTrue(i);
            var bit_j_is0 = alg.BDDAlgebra.MkBitFalse(j);
            var bit_j_is1 = alg.BDDAlgebra.MkBitTrue(j);
            var both0 = alg.BDDAlgebra.MkAnd(bit_i_is0, bit_j_is0);
            var bit_i_is1_bit_j_is0 = alg.BDDAlgebra.MkAnd(bit_i_is1, bit_j_is0);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(both0,alg.Second.True)),
                new Move<IMonadicPredicate<BDD, T>>(0, 1, alg.MkCartesianProduct(bit_i_is1_bit_j_is0,alg.Second.True)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, alg.MkCartesianProduct(bit_j_is0,alg.Second.True)),
                new Move<IMonadicPredicate<BDD, T>>(1, 2, alg.MkCartesianProduct(bit_j_is1,alg.Second.True)), 
                new Move<IMonadicPredicate<BDD, T>>(2, 2, alg.True)};
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 2 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i &lt;= x_j, with singleton-set-semantics for f-o vars
        /// </summary>
        internal static Automaton<BDD> MkLe1(int i, int j, IBDDAlgebra alg)
        {
            throw new NotImplementedException("MkLe1");
        }

        /// <summary>
        /// x_i &lt;= x_j, with singleton-set-semantics for f-o vars
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkLe1<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            throw new NotImplementedException("MkLe1<T>");
        }

        /// <summary>
        /// x_i &lt;= x_j, with min(nonempty-set)-semantics for f-o vars
        /// </summary>
        public static Automaton<BDD> MkLe2(int i, int j, IBDDAlgebra alg) 
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var bit_j_is0 = alg.MkBitFalse(j);
            var bit_j_is1 = alg.MkBitTrue(j);
            var both0 = alg.MkAnd(bit_i_is0, bit_j_is0);
            var both1 = alg.MkAnd(bit_i_is1, bit_j_is1);
            var bit_i_is1_bit_j_is0 = alg.MkAnd(bit_i_is1, bit_j_is0);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, both0),
                new Move<BDD>(0, 1, bit_i_is1_bit_j_is0), 
                new Move<BDD>(1, 1, bit_j_is0),
                new Move<BDD>(1, 2, bit_j_is1), 
                new Move<BDD>(2, 2, alg.True),
                new Move<BDD>(0, 2, both1)
            };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 2 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i &lt;= x_j, with min(nonempty-set)-semantics for f-o vars
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkLe2<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> f = bdd => alg.MkCartesianProduct(bdd, alg.Second.True);
            var bit_i_is0 = alg.BDDAlgebra.MkBitFalse(i);
            var bit_i_is1 = alg.BDDAlgebra.MkBitTrue(i);
            var bit_j_is0 = alg.BDDAlgebra.MkBitFalse(j);
            var bit_j_is1 = alg.BDDAlgebra.MkBitTrue(j);
            var both0 = alg.BDDAlgebra.MkAnd(bit_i_is0, bit_j_is0);
            var both1 = alg.BDDAlgebra.MkAnd(bit_i_is1, bit_j_is1);
            var bit_i_is1_bit_j_is0 = alg.BDDAlgebra.MkAnd(bit_i_is1, bit_j_is0);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, f(both0)),
                new Move<IMonadicPredicate<BDD, T>>(0, 1, f(bit_i_is1_bit_j_is0)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, f(bit_j_is0)),
                new Move<IMonadicPredicate<BDD, T>>(1, 2, f(bit_j_is1)), 
                new Move<IMonadicPredicate<BDD, T>>(2, 2, alg.True),
                new Move<IMonadicPredicate<BDD, T>>(0, 2, f(both1))
            };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 2 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// ex1 x: X_i = {x}
        /// </summary>
        public static Automaton<BDD> MkSingleton(int i, IBDDAlgebra alg)
        {
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, bit_i_is0),
                new Move<BDD>(0, 1, bit_i_is1), 
                new Move<BDD>(1, 1, bit_i_is0)};
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// ex1 x: X_i = {x}
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkSingleton<T>(int i, ICartesianAlgebraBDD<T> ca)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> f = bdd => ca.MkCartesianProduct(bdd, ca.Second.True);
            var alg = ca.BDDAlgebra;
            var bit_i_is0 = alg.MkBitFalse(i);
            var bit_i_is1 = alg.MkBitTrue(i);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, f(bit_i_is0)),
                new Move<IMonadicPredicate<BDD, T>>(0, 1, f(bit_i_is1)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, f(bit_i_is0))};
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(ca, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<BDD> MkTrue(IBDDAlgebra alg)
        {
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, alg.True) 
            };
            return Automaton<BDD>.Create(alg, 0, new int[] { 0 }, moves, false, false, true);
        }

        public static Automaton<IMonadicPredicate<BDD, T>> MkTrue<T>(ICartesianAlgebraBDD<T> ca)
        {
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, ca.True) 
            };
            return Automaton<IMonadicPredicate<BDD, T>>.Create(ca, 0, new int[] { 0 }, moves, false, false, true);
        }

        public static Automaton<BDD> MkFalse(IBDDAlgebra alg)
        {
            var moves = new Move<BDD>[] { };
            return Automaton<BDD>.Create(alg, 0, new int[] {}, moves, false, false, true);
        }

        public static Automaton<IMonadicPredicate<BDD, T>> MkFalse<T>(ICartesianAlgebraBDD<T> ca)
        {
            var moves = new Move<IMonadicPredicate<BDD, T>>[] {  };
            return Automaton<IMonadicPredicate<BDD, T>>.Create(ca, 0, new int[] {}, moves, false, false, true);
        }

        /// <summary>
        /// x_j = x_i + n, with singleton-set-semantics
        /// </summary>
        public static Automaton<BDD> MkSuccN1(int i, int j, int n, IBDDAlgebra alg)
        {
            var ieq0 = alg.MkBitFalse(i);
            var ieq1 = alg.MkBitTrue(i);
            var jeq0 = alg.MkBitFalse(j);
            var jeq1 = alg.MkBitTrue(j);
            var both0 = alg.MkAnd(ieq0, jeq0);
            var ieq1_jeq0 = alg.MkAnd(ieq1, jeq0);
            var ieq0_jeq1 = alg.MkAnd(ieq0, jeq1);

            //Create moves
            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, both0));
            moves.Add(new Move<BDD>(0, 1, ieq1_jeq0));

            for (int k = 1; k < n; k++)
                moves.Add(new Move<BDD>(k, k + 1, both0));

            moves.Add(new Move<BDD>(n, n + 1, ieq0_jeq1));
            moves.Add(new Move<BDD>(n + 1, n + 1, both0));

            var aut = Automaton<BDD>.Create(alg, 0, new int[] { n + 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_j = x_i + n, with min(nonempty-set)-semantics
        /// </summary>
        public static Automaton<BDD> MkSuccN2(int i, int j, int n, IBDDAlgebra alg)
        {
            var ieq0 = alg.MkBitFalse(i);
            var ieq1 = alg.MkBitTrue(i);
            var jeq0 = alg.MkBitFalse(j);
            var jeq1 = alg.MkBitTrue(j);
            var both0 = alg.MkAnd(ieq0, jeq0);
            var ieq1_jeq0 = alg.MkAnd(ieq1, jeq0);

            //Create moves
            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 0, both0));
            moves.Add(new Move<BDD>(0, 1, ieq1_jeq0));

            for (int k = 1; k < n; k++)
                moves.Add(new Move<BDD>(k, k + 1, jeq0));

            moves.Add(new Move<BDD>(n, n + 1, jeq1));
            moves.Add(new Move<BDD>(n + 1, n + 1, alg.True));

            var aut = Automaton<BDD>.Create(alg, 0, new int[] { n + 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_j = x_i + n, with min(nonempty-set)-semantics
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkSuccN2<T>(int i, int j, int n, ICartesianAlgebraBDD<T> alg)
        {
            var ieq0 = alg.BDDAlgebra.MkBitFalse(i);
            var ieq1 = alg.BDDAlgebra.MkBitTrue(i);
            var jeq0 = alg.BDDAlgebra.MkBitFalse(j);
            var jeq1 = alg.BDDAlgebra.MkBitTrue(j);
            var both0 = alg.BDDAlgebra.MkAnd(ieq0, jeq0);
            var ieq1_jeq0 = alg.BDDAlgebra.MkAnd(ieq1, jeq0);

            //Create moves
            var moves = new List<Move<IMonadicPredicate<BDD, T>>>();
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(both0, alg.Second.True)));
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(0, 1, alg.MkCartesianProduct(ieq1_jeq0, alg.Second.True)));

            for (int k = 1; k < n; k++)
                moves.Add(new Move<IMonadicPredicate<BDD, T>>(k, k + 1, alg.MkCartesianProduct(jeq0, alg.Second.True)));

            moves.Add(new Move<IMonadicPredicate<BDD, T>>(n, n + 1, alg.MkCartesianProduct(jeq1, alg.Second.True)));
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(n + 1, n + 1, alg.True));

            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { n + 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_j = x_i + n, with singleton-set-semantics
        /// </summary>
        public static Automaton<IMonadicPredicate<BDD, T>> MkSuccN1<T>(int i1, int i2, int n, ICartesianAlgebraBDD<T> ca)
        {

            var bddAlg = ca.BDDAlgebra;
            var ind10 = bddAlg.MkBitFalse(i1);
            var ind11 = bddAlg.MkBitTrue(i1);
            var ind20 = bddAlg.MkBitFalse(i2);
            var ind21 = bddAlg.MkBitTrue(i2);
            var both0 = bddAlg.MkAnd(ind10, ind20);
            var ind11ind20 = bddAlg.MkAnd(ind11, ind20);
            var ind10ind21 = bddAlg.MkAnd(ind10, ind21);

            var both0t = ca.MkCartesianProduct(both0, ca.Second.True);
            var ind11ind20t = ca.MkCartesianProduct(ind11ind20, ca.Second.True);
            var ind10ind21t = ca.MkCartesianProduct(ind10ind21, ca.Second.True);

            //Create moves
            var moves = new List<Move<IMonadicPredicate<BDD, T>>>();
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(0, 0, both0t));
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(0, 1, ind11ind20t));
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(n, n + 1, ind10ind21t));
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(n + 1, n + 1, both0t));

            for (int i = 1; i < n; i++)
                moves.Add(new Move<IMonadicPredicate<BDD, T>>(i, i + 1, both0t));

            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(ca, 0, new int[] { n + 1 }, moves, false, false, true);
            return aut;
        }

        public static Automaton<T> Restrict<T>(Automaton<IMonadicPredicate<BDD, T>> autom)
        {
            List<Move<T>> moves = new List<Move<T>>();
            foreach (var move in autom.GetMoves())
                moves.Add(new Move<T>(move.SourceState, move.TargetState, move.Label.ProjectSecond()));
            var res = Automaton<T>.Create(((ICartesianAlgebra<BDD, T>)autom.Algebra).Second, autom.InitialState, autom.GetFinalStates(), moves);
            return res;
        }
        /// <summary>
        /// x_i = n, with singleton-set-semantics
        /// </summary>
        internal static Automaton<BDD> MkEqN1(int k, int n, IBDDAlgebra alg)
        {
            throw new NotImplementedException("MkEqN1");
        }
        /// <summary>
        /// x_i = n, with singleton-set-semantics
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkEqN1<T>(int i, int n, ICartesianAlgebraBDD<T> alg)
        {
            throw new NotImplementedException("MkEqN1<T>");
        }

        /// <summary>
        /// x_i = n, with min(nonempty-set)-semantics
        /// </summary>
        internal static Automaton<BDD> MkEqN2(int i, int n, IBDDAlgebra alg)
        {
            var ieq0 = alg.MkBitFalse(i);
            var ieq1 = alg.MkBitTrue(i);
            var moves = new List<Move<BDD>>();

            for (int k = 0; k < n; k++)
                moves.Add(new Move<BDD>(k, k + 1, ieq0));

            moves.Add(new Move<BDD>(n, n + 1, ieq1));
            moves.Add(new Move<BDD>(n + 1, n + 1, alg.True));

            var aut = Automaton<BDD>.Create(alg, 0, new int[] { n + 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i = n, with min(nonempty-set)-semantics
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkEqN2<T>(int i, int n, ICartesianAlgebraBDD<T> alg) 
        {
            var ieq0 = alg.BDDAlgebra.MkBitFalse(i);
            var ieq1 = alg.BDDAlgebra.MkBitTrue(i);
            var moves = new List<Move<IMonadicPredicate<BDD, T>>>();

            for (int k = 0; k < n; k++)
                moves.Add(new Move<IMonadicPredicate<BDD, T>>(k, k + 1, alg.MkCartesianProduct(ieq0, alg.Second.True)));

            moves.Add(new Move<IMonadicPredicate<BDD, T>>(n, n + 1, alg.MkCartesianProduct(ieq1, alg.Second.True)));
            moves.Add(new Move<IMonadicPredicate<BDD, T>>(n + 1, n + 1, alg.True));

            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { n + 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i = min X_j with singleton-set-semantics for x_i
        /// </summary>
        internal static Automaton<BDD> MkMin1(int p1, int p2, IBDDAlgebra alg)
        {
            throw new NotImplementedException("MkMin1");
        }

        /// <summary>
        /// x_i = min X_j with singleton-set-semantics for x_i
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkMin1<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            throw new NotImplementedException("MkMin1<T>"); 
        }

        /// <summary>
        /// x_i = min X_j with min(nonempty-set)-semantics for x_i
        /// </summary>
        internal static Automaton<BDD> MkMin2(int i, int j, IBDDAlgebra alg)
        {
            var ieq0 = alg.MkBitFalse(i);
            var ieq1 = alg.MkBitTrue(i);
            var jeq0 = alg.MkBitFalse(j);
            var jeq1 = alg.MkBitTrue(j);
            var both1 = alg.MkAnd(jeq1, ieq1);
            var both0 = alg.MkAnd(jeq0, ieq0);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, both0), 
                new Move<BDD>(0, 1, both1),
                new Move<BDD>(1, 1, alg.True)
            };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i = min X_j with min(nonempty-set)-semantics for x_i
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkMin2<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var ieq0 = alg.BDDAlgebra.MkBitFalse(i);
            var ieq1 = alg.BDDAlgebra.MkBitTrue(i);
            var jeq0 = alg.BDDAlgebra.MkBitFalse(j);
            var jeq1 = alg.BDDAlgebra.MkBitTrue(j);
            var both1 = alg.BDDAlgebra.MkAnd(jeq1, ieq1);
            var both0 = alg.BDDAlgebra.MkAnd(jeq0, ieq0);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(both0, alg.Second.True)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, alg.MkCartesianProduct(both1, alg.Second.True)),
                new Move<IMonadicPredicate<BDD, T>>(1, 1, alg.True)
            };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i = max X_j with singleton-set-semantics for x_i
        /// </summary>
        internal static Automaton<BDD> MkMax1(int p1, int p2, IBDDAlgebra alg)
        {
            throw new NotImplementedException("MkMax1");
        }
        /// <summary>
        /// x_i = max X_j with singleton-set-semantics for x_i
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkMax1<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            throw new NotImplementedException("MkMax1<T>");
        }

        /// <summary>
        /// x_i = max X_j with min(nonempty-set)-semantics for x_i
        /// </summary>
        internal static Automaton<BDD> MkMax2(int i, int j, IBDDAlgebra alg) 
        {
            var ieq0 = alg.MkBitFalse(i);
            var ieq1 = alg.MkBitTrue(i);
            var jeq0 = alg.MkBitFalse(j);
            var jeq1 = alg.MkBitTrue(j);
            var both1 = alg.MkAnd(jeq1, ieq1);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, ieq0), 
                new Move<BDD>(0, 1, both1),
                new Move<BDD>(1, 1, jeq0)
            };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i = max X_j with min(nonempty-set)-semantics for x_i
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkMax2<T>(int i, int j, ICartesianAlgebraBDD<T> alg)
        {
            var ieq0 = alg.BDDAlgebra.MkBitFalse(i);
            var ieq1 = alg.BDDAlgebra.MkBitTrue(i);
            var jeq0 = alg.BDDAlgebra.MkBitFalse(j);
            var jeq1 = alg.BDDAlgebra.MkBitTrue(j);
            var both1 = alg.BDDAlgebra.MkAnd(jeq1, ieq1);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(ieq0, alg.Second.True)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, alg.MkCartesianProduct(both1, alg.Second.True)),
                new Move<IMonadicPredicate<BDD, T>>(1, 1, alg.MkCartesianProduct(jeq0, alg.Second.True))
            };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i is the last position
        /// </summary>
        internal static Automaton<BDD> MkLast(int i, IBDDAlgebra alg)
        {
            var ieq0 = alg.MkBitFalse(i);
            var ieq1 = alg.MkBitTrue(i);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, ieq0), 
                new Move<BDD>(0, 1, ieq1),
            };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// x_i is the last position
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkLast<T>(int i, ICartesianAlgebraBDD<T> alg)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> f = bdd => alg.MkCartesianProduct(bdd, alg.Second.True);
            var ieq0 = alg.BDDAlgebra.MkBitFalse(i);
            var ieq1 = alg.BDDAlgebra.MkBitTrue(i);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, f(ieq0)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, f(ieq1)),
            };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// X_i = {}
        /// </summary>
        internal static Automaton<BDD> MkIsEmpty(int i, IBDDAlgebra alg)
        {
            var ieq0 = alg.MkBitFalse(i);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, ieq0), 
            };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// X_i = {}
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkIsEmpty<T>(int i, ICartesianAlgebraBDD<T> alg)
        {
            var ieq0 = alg.BDDAlgebra.MkBitFalse(i);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, alg.MkCartesianProduct(ieq0, alg.Second.True)), 
            };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 0 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// X_i ~= {}
        /// </summary>
        internal static Automaton<BDD> MkIsNonempty(int i, IBDDAlgebra alg)
        {
            var ieq1 = alg.MkBitTrue(i);
            var ieq0 = alg.MkBitFalse(i);
            var moves = new Move<BDD>[] { 
                new Move<BDD>(0, 0, ieq0), 
                new Move<BDD>(0, 1, ieq1), 
                new Move<BDD>(1, 1, alg.True)
            };
            var aut = Automaton<BDD>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }

        /// <summary>
        /// X_i ~= {}
        /// </summary>
        internal static Automaton<IMonadicPredicate<BDD, T>> MkIsNonempty<T>(int i, ICartesianAlgebraBDD<T> alg)
        {
            Func<BDD, IMonadicPredicate<BDD, T>> f = bdd => alg.MkCartesianProduct(bdd, alg.Second.True);
            var ieq1 = alg.BDDAlgebra.MkBitTrue(i);
            var ieq0 = alg.BDDAlgebra.MkBitFalse(i);
            var moves = new Move<IMonadicPredicate<BDD, T>>[] { 
                new Move<IMonadicPredicate<BDD, T>>(0, 0, f(ieq0)), 
                new Move<IMonadicPredicate<BDD, T>>(0, 1, f(ieq1)), 
                new Move<IMonadicPredicate<BDD, T>>(1, 1, alg.True)
            };
            var aut = Automaton<IMonadicPredicate<BDD, T>>.Create(alg, 0, new int[] { 1 }, moves, false, false, true);
            return aut;
        }
    }
}
