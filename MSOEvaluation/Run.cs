using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using System.Diagnostics;

using Microsoft.Automata.MSO;
using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Z3;

namespace MSOEvaluation
{
    static class Run
    {
        static int numTests = 1;
        static int kpopl = 40;
        static int kminterm = 40;

        static int maxmint = 19;
        static int maxmphipop = 11;
        static int maxmphipopold = 10;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //POPLTestsNew();
            //POPLTestsOld();
            //POPLTestsNewSolver2();
            //POPLTestsInt();

            MintermTest();
            

            //Console.Read();
        }

        public static void POPLTestsNew()
        {
            
            var sw = new Stopwatch();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"..\msobdd.txt"))
            {
                for (int to = 2; to < kpopl; to++)
                {
                    // T1
                    var solver = new CharSetSolver(BitWidth.BV64);
                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t1 = sw.ElapsedMilliseconds;

                    //T2
                    solver = new CharSetSolver(BitWidth.BV64);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'a'), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t2 = sw.ElapsedMilliseconds;

                    // T3
                    solver = new CharSetSolver(BitWidth.BV64);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'a'), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    var exycy = new WS1SExists<BDD>("y", new WS1SPred<BDD>(solver.MkCharConstraint(false, 'c'), "y"));
                    phi = new WS1SAnd<BDD>(phi, exycy);

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t3 = sw.ElapsedMilliseconds;

                    //T4
                    solver = new CharSetSolver(BitWidth.BV64);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        var axk = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'a'), "x" + (k - 1));
                        var cxk = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'c'), "x" + (k - 1));
                        var inter = new WS1SOr<BDD>(new WS1SAnd<BDD>(leq, axk), cxk);
                        phi = new WS1SAnd<BDD>(phi, inter);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    exycy = new WS1SExists<BDD>("y", new WS1SPred<BDD>(solver.MkCharConstraint(false, 'c'), "y"));
                    phi = new WS1SAnd<BDD>(phi, exycy);

                    var t4 = 60000L*numTests;
                    if (to <= maxmphipop)
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            phi.GetAutomaton(solver);
                        }
                        sw.Stop();
                        t4 = sw.ElapsedMilliseconds;
                    }

                    file.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);
                    Console.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);

                }
            }           
        }

        public static void POPLTestsNewSolver2()
        {

            var sw = new Stopwatch();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"..\msobddsol2.txt"))
            {
                for (int to = 2; to < kpopl; to++)
                {
                    // T1
                    var solver = new CharSetSolver(BitWidth.BV64);
                    var alg = new BDDAlgebra<BDD>(solver);

                    
                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(alg,new string[0]);
                    }
                    sw.Stop();

                    var t1 = sw.ElapsedMilliseconds;

                    //T2
                    solver = new CharSetSolver(BitWidth.BV64);
                    alg = new BDDAlgebra<BDD>(solver);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'a'), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(alg, new string[0]);
                    }
                    sw.Stop();

                    var t2 = sw.ElapsedMilliseconds;

                    // T3
                    solver = new CharSetSolver(BitWidth.BV64);
                    alg = new BDDAlgebra<BDD>(solver);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'a'), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    var exycy = new WS1SExists<BDD>("y", new WS1SPred<BDD>(solver.MkCharConstraint(false, 'c'), "y"));
                    phi = new WS1SAnd<BDD>(phi, exycy);

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(alg, new string[0]);
                    }
                    sw.Stop();

                    var t3 = sw.ElapsedMilliseconds;

                    //T4
                    solver = new CharSetSolver(BitWidth.BV64);
                    alg = new BDDAlgebra<BDD>(solver);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        var axk = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'a'), "x" + (k - 1));
                        var cxk = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'c'), "x" + (k - 1));
                        var inter = new WS1SOr<BDD>(new WS1SAnd<BDD>(leq, axk), cxk);
                        phi = new WS1SAnd<BDD>(phi, inter);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    exycy = new WS1SExists<BDD>("y", new WS1SPred<BDD>(solver.MkCharConstraint(false, 'c'), "y"));
                    phi = new WS1SAnd<BDD>(phi, exycy);

                    var t4 = 60000L * numTests;
                    if (to <= maxmphipop)
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            phi.GetAutomaton(alg, new string[0]);
                        }
                        sw.Stop();
                        t4 = sw.ElapsedMilliseconds;
                    }

                    file.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);
                    Console.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);

                }
            }
        }

        public static void POPLTestsInt()
        {

            var sw = new Stopwatch();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"..\mso-int.txt"))
            {
                for (int to = 2; to < kpopl; to++)
                {
                    // T1
                    var ctx = new Context();
                    var sort = ctx.IntSort;
                    var solver = new BooleanAlgebraZ3(ctx, sort);
                    var alg = new BDDAlgebra<BoolExpr>(solver);
                    var pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), "x");
                  

                    WS1SFormula<BoolExpr> phi = new WS1STrue<BoolExpr>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BoolExpr>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BoolExpr>(phi, leq);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BoolExpr>("x" + k, phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t1 = sw.ElapsedMilliseconds;

                    //T2
                    ctx = new Context();
                    sort = ctx.IntSort;
                    solver = new BooleanAlgebraZ3(ctx, sort);
                    alg = new BDDAlgebra<BoolExpr>(solver);
                    pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), "x");

                    phi = new WS1STrue<BoolExpr>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BoolExpr>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BoolExpr>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BoolExpr>(
                             ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(42+k, sort)), 
                             "x" + k);
                        phi = new WS1SAnd<BoolExpr>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BoolExpr>("x" + k, phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t2 = sw.ElapsedMilliseconds;

                    // T3
                    ctx = new Context();
                    sort = ctx.IntSort;
                    solver = new BooleanAlgebraZ3(ctx, sort);
                    alg = new BDDAlgebra<BoolExpr>(solver);
                    pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), "x");

                    phi = new WS1STrue<BoolExpr>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BoolExpr>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BoolExpr>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BoolExpr>(ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(42+k, sort)), "x" + k);
                        phi = new WS1SAnd<BoolExpr>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BoolExpr>("x" + k, phi);
                    }

                    var exycy = new WS1SExists<BoolExpr>("y", new WS1SPred<BoolExpr>(
                        ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(1000, sort)), "y"));
                    phi = new WS1SAnd<BoolExpr>(phi, exycy);

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t3 = sw.ElapsedMilliseconds;

                    //T4
                    ctx = new Context();
                    sort = ctx.IntSort;
                    solver = new BooleanAlgebraZ3(ctx, sort);
                    alg = new BDDAlgebra<BoolExpr>(solver);
                    pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), "x");

                    phi = new WS1STrue<BoolExpr>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BoolExpr>("x" + (k - 1), "x" + k);
                        var axk = new WS1SPred<BoolExpr>(ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(42+k, sort)), "x" + (k - 1));
                        var cxk = new WS1SPred<BoolExpr>(ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(1000, sort)), "x" + (k - 1));
                        var inter = new WS1SOr<BoolExpr>(new WS1SAnd<BoolExpr>(leq, axk), cxk);
                        phi = new WS1SAnd<BoolExpr>(phi, inter);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BoolExpr>("x" + k, phi);
                    }

                    exycy = new WS1SExists<BoolExpr>("y", new WS1SPred<BoolExpr>(
                        ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(1000, sort)),
                        "y"));
                    phi = new WS1SAnd<BoolExpr>(phi, exycy);

                    var t4 = 60000L * numTests;
                    if (to <= maxmphipop)
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            phi.GetAutomaton(solver);
                        }
                        sw.Stop();
                        t4 = sw.ElapsedMilliseconds;
                    }

                    file.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);
                    Console.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);

                }
            }
        }

        public static void POPLTestsOld()
        {

            var sw = new Stopwatch();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"..\msobddold.txt"))
            {
                for (int to = 2; to < kpopl; to++)
                {
                    // T1
                    var solver = new CharSetSolver(BitWidth.BV64);
                    WS1SFormula phi = new WS1STrue();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd(phi, leq);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists("x" + k, phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.getAutomaton(solver);
                    }
                    sw.Stop();

                    var t1 = sw.ElapsedMilliseconds;

                    //T2
                    solver = new CharSetSolver(BitWidth.BV64);
                    phi = new WS1STrue();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred(solver.MkCharConstraint(false, 'a'), "x" + k);
                        phi = new WS1SAnd(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists("x" + k, phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.getAutomaton(solver);
                    }
                    sw.Stop();

                    var t2 = sw.ElapsedMilliseconds;

                    // T3
                    solver = new CharSetSolver(BitWidth.BV64);
                    phi = new WS1STrue();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred(solver.MkCharConstraint(false, 'a'), "x" + k);
                        phi = new WS1SAnd(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists("x" + k, phi);
                    }

                    var exycy = new WS1SExists("y", new WS1SPred(solver.MkCharConstraint(false, 'c'), "y"));
                    phi = new WS1SAnd(phi, exycy);

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.getAutomaton(solver);
                    }
                    sw.Stop();

                    var t3 = sw.ElapsedMilliseconds;

                    //T4
                    solver = new CharSetSolver(BitWidth.BV64);
                    phi = new WS1STrue();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt("x" + (k - 1), "x" + k);
                        var axk = new WS1SPred(solver.MkCharConstraint(false, 'a'), "x" + (k - 1));
                        var cxk = new WS1SPred(solver.MkCharConstraint(false, 'c'), "x" + (k - 1));
                        var inter = new WS1SOr(new WS1SAnd(leq, axk), cxk);
                        phi = new WS1SAnd(phi, inter);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists("x" + k, phi);
                    }

                    exycy = new WS1SExists("y", new WS1SPred(solver.MkCharConstraint(false, 'c'), "y"));
                    phi = new WS1SAnd(phi, exycy);

                    var t4 = 60000L * numTests;
                    if (to <= maxmphipopold)
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            phi.getAutomaton(solver);
                        }
                        sw.Stop();
                        t4 = sw.ElapsedMilliseconds;
                    }

                    file.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);
                    Console.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);

                }
            }
        }



        public static void MintermTest()
        {

            var sw = new Stopwatch();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"..\msomintermp1s1.txt"))
            {
                for (int size = 2; size < kminterm; size++)
                {
                    // Tsolve no force
                    var solver = new CharSetSolver(BitWidth.BV64);

                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 1; k < size; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < size; k++)
                    {
                        var axk = new WS1SPred<BDD>(solver.MkSetWithBitTrue(k), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t1 = sw.ElapsedMilliseconds;

                    file.WriteLine( (double)t1 / numTests );
                    Console.WriteLine((double)t1 / numTests);
                }
            }
            using (System.IO.StreamWriter file =
           new System.IO.StreamWriter(@"..\msomintermp2s1.txt"))
            {
                for (int size = 2; size < kminterm; size++)
                {

                    // Tsolve force
                    var solver = new CharSetSolver(BitWidth.BV64);

                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 0; k < size; k++)
                    {
                        var axk = new WS1SPred<BDD>(solver.MkSetWithBitTrue(k), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    var t1 = 60000L;
                    if (size <= maxmint)
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            phi.GetAutomaton(solver);
                        }
                        sw.Stop();

                        t1 = sw.ElapsedMilliseconds;
                    }
                    file.WriteLine((double)t1 / numTests);
                    Console.WriteLine((double)t1 / numTests);
                }
            }
            using (System.IO.StreamWriter file =
           new System.IO.StreamWriter(@"..\msomintermp1s2.txt"))
            {
                for (int size = 2; size < kminterm; size++)
                {

                    // Tsolve solver 2
                    var solver = new CharSetSolver(BitWidth.BV64);
                    var alg = new BDDAlgebra<BDD>(solver);

                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 1; k < size; k++)
                    {
                        var leq = new WS1SLt<BDD>("x" + (k - 1), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < size; k++)
                    {
                        var axk = new WS1SPred<BDD>(solver.MkSetWithBitTrue(k), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    var t1 = 60000L;


                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(alg,new string[0]);
                    }
                    sw.Stop();

                    t1 = sw.ElapsedMilliseconds;

                    file.WriteLine((double)t1 / numTests);
                    Console.WriteLine((double)t1 / numTests);
                }
            }
            using (System.IO.StreamWriter file =
           new System.IO.StreamWriter(@"..\msomintermp2s2.txt"))
            {
                for (int size = 2; size < kminterm; size++)
                {

                    var solver = new CharSetSolver(BitWidth.BV64);
                    var alg = new BDDAlgebra<BDD>(solver);
                    //Tforce sol 2


                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 0; k < size; k++)
                    {
                        var axk = new WS1SPred<BDD>(solver.MkSetWithBitTrue(k), "x" + k);
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>("x" + k, phi);
                    }

                    var t1 = 60000L;
                    if (size <= maxmint)
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            phi.GetAutomaton(alg, new string[0]);
                        }
                        sw.Stop();
                        t1 = sw.ElapsedMilliseconds;
                    }
                    file.WriteLine((double)t1 / numTests);
                    Console.WriteLine((double)t1 / numTests);
                }
            }
            using (System.IO.StreamWriter file =
           new System.IO.StreamWriter(@"..\msominterm.txt"))
            {
                for (int size = 2; size < kminterm; size++)
                {
                    //Tminterm
                    var solver = new CharSetSolver(BitWidth.BV64);
                    BDD[] predicates = new BDD[size];
                    solver.GenerateMinterms();
                    for (int k = 0; k < size; k++)
                        predicates[k] = solver.MkSetWithBitTrue(k);

                    var t1 = 60000L * numTests;
                    if (size <= maxmint)
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            var mint = solver.GenerateMinterms(predicates).ToList();
                        }
                        sw.Stop();
                        t1 = sw.ElapsedMilliseconds;
                    }

                    file.WriteLine((double)t1 / numTests);
                    Console.WriteLine((double)t1 / numTests);
                }
            }
        }


        public static void TestLargeLoris()
        {
            var max = 10;
            for (int i = 1; i < max; i++)
                TestMintermExplosion(i, true);
        }

        static void  TestMintermExplosion(int bitWidth, bool useBDD = false)
        {

            Console.WriteLine("----------------");
            Console.WriteLine(bitWidth.ToString());

            if (useBDD)
            {
                var S = new CharSetSolver(BitWidth.BV7);

                Console.WriteLine("BDD");
                int t = System.Environment.TickCount;
                var aut1 = CreateAutomaton1<BDD>(S.MkSetWithBitTrue, bitWidth, S);
                var aut2 = CreateAutomaton2<BDD>(S.MkSetWithBitTrue, bitWidth, S);
                var aut3 = CreateAutomaton3<BDD>(S.MkSetWithBitTrue, bitWidth, S);
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
            Func<int, string, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), s);

            MSOFormula<T> phi = new MSOFalse<T>();

            for (int index = 0; index < bitWidth; index++)
            {
                var phi1 = pred(index, "var");
                phi = new MSOOr<T>(phi, phi1);
            }

            phi = new MSOExistsFo<T>("var", phi);

            var aut = phi.GetAutomaton(Z);

            return aut;
        }

        static Automaton<T> CreateAutomaton2<T>(Func<int, T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {

            Func<int, string, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), s);

            MSOFormula<T> phi = new MSOFalse<T>();

            for (int index = 0; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = pred(index, "var");
                phi1 = new MSOExistsFo<T>("var", phi1);
                phi = new MSOOr<T>(phi, phi1);
            }

            phi = new MSOExistsFo<T>("var", phi);

            var aut = phi.GetAutomaton(Z);

            return aut;
        }

        static Automaton<T> CreateAutomaton3<T>(Func<int, T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {

            Func<int, string, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), s);

            MSOFormula<T> phi = new MSOTrue<T>();

            // x1<x2<x3<x4...
            for (int index = 1; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = new MSOLt<T>("x" + (index - 1), "x" + index);
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
                    phi = new MSOExistsFo<T>("x" + index, phi);
                else
                    phi = new MSOForallFo<T>("x" + index, phi);
            }

            var aut = phi.GetAutomaton(Z);
            return aut;
        }
    }
}
