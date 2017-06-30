using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using System.Diagnostics;

using Microsoft.Automata.MSO;
using Microsoft.Automata;
using Microsoft.Automata.BooleanAlgebras;
using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Z3;
using System.IO;
using Microsoft.Automata.MSO.Mona;
using System.Threading;

namespace MSO.Eval
{
    class LargeMinterm
    {
        static int kminterm = 40;
        static int maxmint = 22;
        static int numTests = 1;

        public static void Run()
        {

            var sw = new Stopwatch();

            //ex x1 x2... a(x1) /\ a(x2).../\ x1<x2...
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"mso-minterm-p1.csv"))
            {
                Console.WriteLine("k, old, cartesian, generic-bdd, minterm");
                file.WriteLine("k, old, cartesian, generic-bdd, minterm");
                for (int size = 2; size < kminterm; size++)
                {
                    var solver = new CharSetSolver(BitWidth.BV64);
                    MSOFormula<BDD> phi = new MSOTrue<BDD>();

                    //x1<x2 /\...
                    for (int k = 1; k < size; k++)
                    {
                        var leq = new MSOSuccN<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true), 1);
                        phi = new MSOAnd<BDD>(phi, leq);

                    }

                    //ai(xi) /\ ...
                    for (int k = 0; k < size; k++)
                    {
                        var axk = new MSOPredicate<BDD>(solver.MkBitTrue(k), new Variable("x" + k, true));
                        phi = new MSOAnd<BDD>(phi, axk);

                    }
                    phi = new MSOAnd<BDD>(new MSOeqN<BDD>(new Variable("x" + 0, true), 0), phi);
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new MSOExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    //Old
                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        var aut = phi.GetAutomaton(solver);
                    }

                    var told = sw.ElapsedMilliseconds;

                    //classic MSO old
                    var t1 = 60000L * numTests;
                    bool oom = false;
                    try
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            var aut = phi.GetAutomaton(new CartesianAlgebraBDD<BDD>(solver));
                        }
                        sw.Stop();
                    }
                    catch (OutOfMemoryException e)
                    {
                        oom = true;
                    }
                    if (!oom)
                        t1 = sw.ElapsedMilliseconds;

                    //Trie
                    var t2 = 60000L * numTests;
                    oom = false;
                    try
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            var aut = phi.GetAutomaton(new BDDAlgebra<BDD>(solver), false);
                        }
                    }
                    catch (OutOfMemoryException e)
                    {
                        oom = true;
                    }
                    if (!oom)
                        t2 = sw.ElapsedMilliseconds;

                    //Tminterm
                    var t3 = 60000L * numTests;
                    oom = false;

                    solver = new CharSetSolver(BitWidth.BV64);
                    BDD[] predicates = new BDD[size];
                    solver.GenerateMinterms();
                    for (int k = 0; k < size; k++)
                        predicates[k] = solver.MkBitTrue(k);


                    if (size <= maxmint)
                    {
                        try
                        {
                            sw.Restart();
                            for (int t = 0; t < numTests; t++)
                            {
                                var mint = solver.GenerateMinterms(predicates).ToList();
                            }
                            sw.Stop();
                        }
                        catch (OutOfMemoryException e)
                        {
                            oom = true;
                        }
                        if (!oom)
                            t3 = sw.ElapsedMilliseconds;
                    }
                    file.WriteLine(size + ", " + (double)told / numTests + ", " + (double)t1 / numTests + ", " + (double)t2 / numTests + ", " + (double)t3 / numTests);
                    Console.WriteLine(size + ", " + (double)told / numTests + ", " + (double)t1 / numTests + ", " + (double)t2 / numTests + ", " + (double)t3 / numTests);
                }
            }

            //ex x1 x2... a(x1) /\ a(x2)...
            using (System.IO.StreamWriter file =
           new System.IO.StreamWriter(@"mso-minterm-p2.csv"))
            {
                Console.WriteLine("k, old, cartesian, trie, minterm");
                file.WriteLine("k, old, cartesian, trie, minterm");
                for (int size = 2; size < 10; size++)
                {

                    // Tsolve force
                    var solver = new CharSetSolver();

                    MSOFormula<BDD> phi = new MSOTrue<BDD>();

                    for (int k = 0; k < size; k++)
                    {
                        var axk = new MSOPredicate<BDD>(solver.MkBitTrue(k), new Variable("x" + k, true));
                        phi = new MSOAnd<BDD>(phi, axk);

                    }
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new MSOExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    //Old
                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        var aut = phi.GetAutomaton(solver);
                    }

                    var told = sw.ElapsedMilliseconds;

                    //Cartesian
                    //classic MSO old
                    var t1 = 60000L * numTests;
                    bool oom = false;
                    try
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            phi.GetAutomaton(new CartesianAlgebraBDD<BDD>(solver));
                        }
                        sw.Stop();
                    }
                    catch (OutOfMemoryException e)
                    {
                        oom = true;
                    }
                    if (!oom)
                        t1 = sw.ElapsedMilliseconds;


                    //Trie
                    var t2= 60000L * numTests;
                    oom = false;
                    try
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            phi.GetAutomaton(new BDDAlgebra<BDD>(solver), false);
                        }
                        sw.Stop();
                    }
                    catch (OutOfMemoryException e)
                    {
                        oom = true;
                    }
                    if (!oom)
                         t2 = sw.ElapsedMilliseconds;

                    //Tminterm
                    solver = new CharSetSolver(BitWidth.BV64);
                    BDD[] predicates = new BDD[size];
                    solver.GenerateMinterms();
                    for (int k = 0; k < size; k++)
                        predicates[k] = solver.MkBitTrue(k);

                    var t3 = 60000L * numTests;
                    oom = false;
                    try
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            var mint = solver.GenerateMinterms(predicates).ToList();
                        }
                        sw.Stop();
                    }
                    catch (OutOfMemoryException e)
                    {
                        oom = true;
                    }
                    if (!oom)
                        t3 = sw.ElapsedMilliseconds;

                    file.WriteLine(size + ", " + (double)told / numTests + ", " + (double)t1 / numTests + ", " + (double)t2 / numTests + ", " + (double)t3 / numTests);
                    Console.WriteLine(size + ", " + (double)told / numTests + ", " + (double)t1 / numTests + ", " + (double)t2 / numTests + ", " + (double)t3 / numTests);
                }
            }
        }


        #region old
        public static void TestLargeLoris()
        {
            var max = 10;
            for (int i = 1; i < max; i++)
                TestMintermExplosion(i, true);
        }

        static void TestMintermExplosion(int bitWidth, bool useBDD = false)
        {

            Console.WriteLine("----------------");
            Console.WriteLine(bitWidth.ToString());

            if (useBDD)
            {
                var S = new CharSetSolver(BitWidth.BV7);

                Console.WriteLine("BDD");
                int t = System.Environment.TickCount;
                var aut1 = CreateAutomaton1<BDD>(S.MkBitTrue, bitWidth, S);
                var aut2 = CreateAutomaton2<BDD>(S.MkBitTrue, bitWidth, S);
                var aut3 = CreateAutomaton3<BDD>(S.MkBitTrue, bitWidth, S);
                t = System.Environment.TickCount - t;
                Console.WriteLine(t + "ms");
                //aut.ShowGraph("aut" + bitWidth);
            }
            else
            {
                Console.WriteLine("Z3");
                Z3Provider Z = new Z3Provider(BitWidth.BV7);
                var x = Z.MkConst("x", Z.IntSort);
                Func<int, Expr> f = (i => Z.MkEq((Z.MkInt(1)), Z.MkMod(Z.MkDiv(x, Z.MkInt(1 << (i % 32))), Z.MkInt(2))));
                int t = System.Environment.TickCount;
                var aut1 = CreateAutomaton1<Expr>(f, bitWidth, Z);
                var aut2 = CreateAutomaton2<Expr>(f, bitWidth, Z);
                var aut3 = CreateAutomaton3<Expr>(f, bitWidth, Z);
                t = System.Environment.TickCount - t;
                Console.WriteLine(t + "ms");
                //aut.ShowGraph("aut" + bitWidth);
            }
        }

        static Automaton<T> CreateAutomaton1<T>(Func<int, T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {
            Func<int, string, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), new Variable(s, true));

            MSOFormula<T> phi = new MSOFalse<T>();

            for (int index = 0; index < bitWidth; index++)
            {
                var phi1 = pred(index, "var");
                phi = new MSOOr<T>(phi, phi1);
            }

            phi = new MSOExists<T>(new Variable("var", true), phi);

            var aut = phi.GetAutomaton(Z);

            return aut;
        }

        static Automaton<T> CreateAutomaton2<T>(Func<int, T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {

            Func<int, string, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), new Variable(s, true));

            MSOFormula<T> phi = new MSOFalse<T>();

            for (int index = 0; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = pred(index, "var");
                phi1 = new MSOExists<T>(new Variable("var", true), phi1);
                phi = new MSOOr<T>(phi, phi1);
            }

            phi = new MSOExists<T>(new Variable("var", true), phi);

            var aut = phi.GetAutomaton(Z);

            return aut;
        }

        static Automaton<T> CreateAutomaton3<T>(Func<int, T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {

            Func<int, string, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), new Variable(s, true));

            MSOFormula<T> phi = new MSOTrue<T>();

            // x1<x2<x3<x4...
            for (int index = 1; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = new MSOLt<T>(new Variable("x" + (index - 1), true), new Variable("x" + index, true));
                phi = new MSOAnd<T>(phi, phi1);
            }

            // bi(xi)
            for (int index = 0; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = pred(index, "x" + index);
                phi = new MSOAnd<T>(phi, phi1);
            }

            // exists forall...
            for (int index = 0; index < bitWidth; index++)
            {
                if (index % 2 == 0)
                    phi = new MSOExists<T>(new Variable("x" + index, true), phi);
                else
                    phi = new MSOForall<T>(new Variable("x" + index, true), phi);
            }

            var aut = phi.GetAutomaton(Z);
            return aut;
        } 
        #endregion
    }
}
