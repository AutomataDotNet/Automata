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
using System.IO;
using Microsoft.Automata.MSO.Mona;
using System.Threading;

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
        public static void Main()
        {
            GenRandomMSO.Run();

            string outF = @"..\ws1stest.txt";
            string inpD = @"C:\github\automatark\ws1s\";

            //LTLTest(inpD,outF);

            //outF = @"..\ltltest.txt";
            //inpD = @"C:\github\automatark\m2l-str\";
            //LTLTest(inpD, outF);


            //POPLTestsNew();
            //POPLTestsOld();
            //POPLTestsNewSolver2();
            //POPLTestsInt();

            //MintermTest();

            //Console.Read();
        }


        static MSOFormula<BDD> phi;
        static BDDAlgebra solver;
        static CharSetSolver cartSolver;
        static void BDDSolver()
        {
            try
            {
                var aut = phi.GetAutomaton(solver);
            }
            catch (ThreadAbortException)
            {
                // cleanup code, if needed...
            }
        }

        static void CartesianSolver()
        {
            try
            {
                var aut = phi.GetAutomaton(cartSolver);
            }
            catch (ThreadAbortException)
            {
                // cleanup code, if needed...
            }
        }
        public static int cmp(string s1, string s2)
        {
            var s1splits = s1.Split('\\');
            var s2splits = s2.Split('\\');

            var dir1 = s1splits[s1splits.Length - 2];
            var dir2 = s2splits[s2splits.Length - 2];
            if (dir1.CompareTo(dir2) < 0)
                return -1;
            if (dir1.CompareTo(dir2) > 0)
                return 1;
            


            var s1c = s1splits[s1splits.Length - 1].ToCharArray();
            var s2c = s2splits[s2splits.Length - 1].ToCharArray();


            int ll = 15;
            if (s1c[0] == 'P' && s2c[0] == 'P')
            {
                var pref1 = s1splits[s1splits.Length - 1].Substring(0, s1splits[s1splits.Length - 1].IndexOf("form"));
                var pref2 = s2splits[s2splits.Length - 1].Substring(0, s1splits[s1splits.Length - 1].IndexOf("form"));
                if (pref1.CompareTo(pref2) != 0)
                    return pref1.CompareTo(pref2);


                var ind1 = s1splits[s1splits.Length - 1].IndexOf("form") + 4;
                var ind2 = s2splits[s2splits.Length - 1].IndexOf("form") + 4;
                var i1 = int.Parse(s1splits[s1splits.Length - 1].Substring(s1splits[s1splits.Length - 1].IndexOf("form")+4, s1splits[s1splits.Length - 1].Length-5-ind1));
                var i2 = int.Parse(s2splits[s2splits.Length - 1].Substring(s2splits[s2splits.Length - 1].IndexOf("form") + 4, s2splits[s2splits.Length - 1].Length - 5-ind2));
                return i1 - i2;
            }

            return s1.CompareTo(s2);

            
        }


        //LTL over finite traces
        public static void LTLTest(string inputDir, string outFile)
        {
            using (System.IO.StreamWriter file =
               new System.IO.StreamWriter(@"..\ws1stest.txt"))
            {
                var files = new List<string>(Directory.EnumerateFiles(@"C:\github\automatark\ws1s\", "*.mona", SearchOption.AllDirectories));
                files.Sort( (s1, s2) => cmp(s1, s2));
                foreach (string fileName in files)
                {
                    string contents = File.ReadAllText(fileName);
                    MonaProgram pgm1 = MonaParser.Parse(contents);
                    phi = pgm1.ToMSO();

                    solver = new BDDAlgebra(); //CharSetSolver(BitWidth.BV64);
                    var sw = new Stopwatch();
                    sw.Restart();
                    Thread t = new Thread(BDDSolver);
                    t.Start();
                    long t1=5000;
                    if (!t.Join(TimeSpan.FromSeconds(5)))
                    {
                        t.Abort();
                        t1 = 5000;
                    }
                    else {
                        sw.Stop();
                        t1 = sw.ElapsedMilliseconds;
                    }

                    //var newSolver = new CharSetSolver(BitWidth.BV64);

                    sw.Restart();
                    //t = new Thread(CartesianSolver);
                    //t.Start();
                    //if (!t.Join(TimeSpan.FromSeconds(5)))
                    //{
                    //    phi.GetAutomaton(newSolver);
                    //}                  
                    //sw.Stop();

                    var t2 = sw.ElapsedMilliseconds;
                    //if (t2 > 5000)
                    //    t2 = 5000;

                    file.WriteLine(fileName + "," + (double)t1 / numTests +"," + (double)t2 / numTests);
                    Console.WriteLine(fileName + "," + (double)t1 / numTests + "," + (double)t2 / numTests);
                }
            }
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
                    var s1 = new CharSetSolver(BitWidth.BV64);
                    var solver = new CartesianAlgebraBDD<BDD>(s1);

                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t1 = sw.ElapsedMilliseconds;

                    //T2
                    s1 = new CharSetSolver(BitWidth.BV64);
                    solver = new CartesianAlgebraBDD<BDD>(s1);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BDD>(
                            s1.MkCharConstraint('a', false), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t2 = sw.ElapsedMilliseconds;

                    // T3
                    s1 = new CharSetSolver(BitWidth.BV64);
                    solver = new CartesianAlgebraBDD<BDD>(s1);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BDD>(s1.MkCharConstraint('a', false), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    var exycy = new WS1SExists<BDD>(new Variable("y", true), new WS1SPred<BDD>(s1.MkCharConstraint('c', false), new Variable("y", true)));
                    phi = new WS1SAnd<BDD>(phi, exycy);

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t3 = sw.ElapsedMilliseconds;

                    //T4
                    s1 = new CharSetSolver(BitWidth.BV64);
                    solver = new CartesianAlgebraBDD<BDD>(s1);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        var axk = new WS1SPred<BDD>(s1.MkCharConstraint('a', false), new Variable("x" + (k - 1), true));
                        var cxk = new WS1SPred<BDD>(s1.MkCharConstraint('c', false), new Variable("x" + (k - 1), true));
                        var inter = new WS1SOr<BDD>(new WS1SAnd<BDD>(leq, axk), cxk);
                        phi = new WS1SAnd<BDD>(phi, inter);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    exycy = new WS1SExists<BDD>(new Variable("y", true), new WS1SPred<BDD>(s1.MkCharConstraint('c', false), new Variable("y", true)));
                    phi = new WS1SAnd<BDD>(phi, exycy);

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

        public static void POPLTestsNewSolver2()
        {

            var sw = new Stopwatch();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"..\msobddsol2.txt"))
            {
                for (int to = 2; to < kpopl; to++)
                {
                    // T1
                    var s1 = new CharSetSolver(BitWidth.BV64);
                    var solver = new BDDAlgebra<BDD>(s1);


                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t1 = sw.ElapsedMilliseconds;

                    //T2
                    s1 = new CharSetSolver(BitWidth.BV64);
                    solver = new BDDAlgebra<BDD>(s1);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BDD>(s1.MkCharConstraint('a', false), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t2 = sw.ElapsedMilliseconds;

                    // T3
                    s1 = new CharSetSolver(BitWidth.BV64);
                    solver = new BDDAlgebra<BDD>(s1);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < to; k++)
                    {
                        var axk = new WS1SPred<BDD>(s1.MkCharConstraint('a', false), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    var exycy = new WS1SExists<BDD>(new Variable("y", true), new WS1SPred<BDD>(s1.MkCharConstraint('c', false), new Variable("y", true)));
                    phi = new WS1SAnd<BDD>(phi, exycy);

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t3 = sw.ElapsedMilliseconds;

                    //T4
                    s1 = new CharSetSolver(BitWidth.BV64);
                    solver = new BDDAlgebra<BDD>(s1);
                    phi = new WS1STrue<BDD>();

                    for (int k = 1; k < to; k++)
                    {
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        var axk = new WS1SPred<BDD>(s1.MkCharConstraint('a', false), new Variable("x" + (k - 1), true));
                        var cxk = new WS1SPred<BDD>(s1.MkCharConstraint('c', false), new Variable("x" + (k - 1), true));
                        var inter = new WS1SOr<BDD>(new WS1SAnd<BDD>(leq, axk), cxk);
                        phi = new WS1SAnd<BDD>(phi, inter);

                    }
                    for (int k = to - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    exycy = new WS1SExists<BDD>(new Variable("y", true), new WS1SPred<BDD>(s1.MkCharConstraint('c', false), new Variable("y", true)));
                    phi = new WS1SAnd<BDD>(phi, exycy);

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

        //public static void POPLTestsInt()
        //{

        //    var sw = new Stopwatch();

        //    using (System.IO.StreamWriter file =
        //    new System.IO.StreamWriter(@"..\mso-int.txt"))
        //    {
        //        for (int to = 2; to < kpopl; to++)
        //        {
        //            // T1
        //            var ctx = new Context();
        //            var sort = ctx.IntSort;
        //            var solver = new BooleanAlgebraZ3(ctx, sort);
        //            var alg = new BDDAlgebra<BoolExpr>(solver);
        //            var pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), "x");


        //            WS1SFormula<BoolExpr> phi = new WS1STrue<BoolExpr>();

        //            for (int k = 1; k < to; k++)
        //            {
        //                var leq = new WS1SLt<BoolExpr>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
        //                phi = new WS1SAnd<BoolExpr>(phi, leq);

        //            }
        //            for (int k = to - 1; k >= 0; k--)
        //            {
        //                phi = new WS1SExists<BoolExpr>(new Variable("x" + k, true), phi);
        //            }

        //            sw.Restart();
        //            for (int t = 0; t < numTests; t++)
        //            {
        //                phi.GetAutomaton(solver);
        //            }
        //            sw.Stop();

        //            var t1 = sw.ElapsedMilliseconds;

        //            //T2
        //            ctx = new Context();
        //            sort = ctx.IntSort;
        //            solver = new BooleanAlgebraZ3(ctx, sort);
        //            alg = new BDDAlgebra<BoolExpr>(solver);
        //            pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), "x");

        //            phi = new WS1STrue<BoolExpr>();

        //            for (int k = 1; k < to; k++)
        //            {
        //                var leq = new WS1SLt<BoolExpr>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
        //                phi = new WS1SAnd<BoolExpr>(phi, leq);

        //            }
        //            for (int k = 0; k < to; k++)
        //            {
        //                var axk = new WS1SPred<BoolExpr>(
        //                     ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(42+k, sort)), 
        //                     "x" + k);
        //                phi = new WS1SAnd<BoolExpr>(phi, axk);

        //            }
        //            for (int k = to - 1; k >= 0; k--)
        //            {
        //                phi = new WS1SExists<BoolExpr>(new Variable("x" + k, true), phi);
        //            }

        //            sw.Restart();
        //            for (int t = 0; t < numTests; t++)
        //            {
        //                phi.GetAutomaton(solver);
        //            }
        //            sw.Stop();

        //            var t2 = sw.ElapsedMilliseconds;

        //            // T3
        //            ctx = new Context();
        //            sort = ctx.IntSort;
        //            solver = new BooleanAlgebraZ3(ctx, sort);
        //            alg = new BDDAlgebra<BoolExpr>(solver);
        //            pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), "x");

        //            phi = new WS1STrue<BoolExpr>();

        //            for (int k = 1; k < to; k++)
        //            {
        //                var leq = new WS1SLt<BoolExpr>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
        //                phi = new WS1SAnd<BoolExpr>(phi, leq);

        //            }
        //            for (int k = 0; k < to; k++)
        //            {
        //                var axk = new WS1SPred<BoolExpr>(ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(42+k, sort)), new Variable("x" + k, true));
        //                phi = new WS1SAnd<BoolExpr>(phi, axk);

        //            }
        //            for (int k = to - 1; k >= 0; k--)
        //            {
        //                phi = new WS1SExists<BoolExpr>(new Variable("x" + k, true), phi);
        //            }

        //            var exycy = new WS1SExists<BoolExpr>(new Variable("y", true), new WS1SPred<BoolExpr>(
        //                ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(1000, sort)), new Variable("y", true)));
        //            phi = new WS1SAnd<BoolExpr>(phi, exycy);

        //            sw.Restart();
        //            for (int t = 0; t < numTests; t++)
        //            {
        //                phi.GetAutomaton(solver);
        //            }
        //            sw.Stop();

        //            var t3 = sw.ElapsedMilliseconds;

        //            //T4
        //            ctx = new Context();
        //            sort = ctx.IntSort;
        //            solver = new BooleanAlgebraZ3(ctx, sort);
        //            alg = new BDDAlgebra<BoolExpr>(solver);
        //            pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), "x");

        //            phi = new WS1STrue<BoolExpr>();

        //            for (int k = 1; k < to; k++)
        //            {
        //                var leq = new WS1SLt<BoolExpr>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
        //                var axk = new WS1SPred<BoolExpr>(ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(42+k, sort)), new Variable("x" + (k - 1), true));
        //                var cxk = new WS1SPred<BoolExpr>(ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(1000, sort)), new Variable("x" + (k - 1), true));
        //                var inter = new WS1SOr<BoolExpr>(new WS1SAnd<BoolExpr>(leq, axk), cxk);
        //                phi = new WS1SAnd<BoolExpr>(phi, inter);

        //            }
        //            for (int k = to - 1; k >= 0; k--)
        //            {
        //                phi = new WS1SExists<BoolExpr>(new Variable("x" + k, true), phi);
        //            }

        //            exycy = new WS1SExists<BoolExpr>(new Variable("y", true), new WS1SPred<BoolExpr>(
        //                ctx.MkLe((IntExpr)solver.x, (IntExpr)ctx.MkNumeral(1000, sort)),
        //                "y"));
        //            phi = new WS1SAnd<BoolExpr>(phi, exycy);

        //            var t4 = 60000L * numTests;
        //            if (to <= maxmphipop)
        //            {
        //                sw.Restart();
        //                for (int t = 0; t < numTests; t++)
        //                {
        //                    phi.GetAutomaton(solver);
        //                }
        //                sw.Stop();
        //                t4 = sw.ElapsedMilliseconds;
        //            }

        //            file.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);
        //            Console.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);

        //        }
        //    }
        //}

        //public static void POPLTestsOld()
        //{

        //    var sw = new Stopwatch();

        //    using (System.IO.StreamWriter file =
        //    new System.IO.StreamWriter(@"..\msobddold.txt"))
        //    {
        //        for (int to = 2; to < kpopl; to++)
        //        {
        //            // T1
        //            var s1 = new CharSetSolver(BitWidth.BV64);
        //            WS1SFormula phi = new WS1STrue();

        //            for (int k = 1; k < to; k++)
        //            {
        //                var leq = new WS1SLt("x" + (k - 1), "x" + k);
        //                phi = new WS1SAnd(phi, leq);

        //            }
        //            for (int k = to - 1; k >= 0; k--)
        //            {
        //                phi = new WS1SExists("x" + k, phi);
        //            }

        //            sw.Restart();
        //            for (int t = 0; t < numTests; t++)
        //            {
        //                phi.getAutomaton(s1);
        //            }
        //            sw.Stop();

        //            var t1 = sw.ElapsedMilliseconds;

        //            //T2
        //            s1 = new CharSetSolver(BitWidth.BV64);
        //            phi = new WS1STrue();

        //            for (int k = 1; k < to; k++)
        //            {
        //                var leq = new WS1SLt("x" + (k - 1), ("x" + k));
        //                phi = new WS1SAnd(phi, leq);

        //            }
        //            for (int k = 0; k < to; k++)
        //            {
        //                var axk = new WS1SPred(s1.MkCharConstraint('a', false), "x" + k);
        //                phi = new WS1SAnd(phi, axk);

        //            }
        //            for (int k = to - 1; k >= 0; k--)
        //            {
        //                phi = new WS1SExists("x" + k, phi);
        //            }

        //            sw.Restart();
        //            for (int t = 0; t < numTests; t++)
        //            {
        //                phi.getAutomaton(s1);
        //            }
        //            sw.Stop();

        //            var t2 = sw.ElapsedMilliseconds;

        //            // T3
        //            s1 = new CharSetSolver(BitWidth.BV64);
        //            phi = new WS1STrue();

        //            for (int k = 1; k < to; k++)
        //            {
        //                var leq = new WS1SLt("x" + (k - 1), ("x" + k));
        //                phi = new WS1SAnd(phi, leq);

        //            }
        //            for (int k = 0; k < to; k++)
        //            {
        //                var axk = new WS1SPred(s1.MkCharConstraint('a', false), ("x" + k));
        //                phi = new WS1SAnd(phi, axk);

        //            }
        //            for (int k = to - 1; k >= 0; k--)
        //            {
        //                phi = new WS1SExists(("x" + k), phi);
        //            }

        //            var exycy = new WS1SExists(("y"), new WS1SPred(s1.MkCharConstraint('c', false), ("y")));
        //            phi = new WS1SAnd(phi, exycy);

        //            sw.Restart();
        //            for (int t = 0; t < numTests; t++)
        //            {
        //                phi.getAutomaton(s1);
        //            }
        //            sw.Stop();

        //            var t3 = sw.ElapsedMilliseconds;

        //            //T4
        //            s1 = new CharSetSolver(BitWidth.BV64);
        //            phi = new WS1STrue();

        //            for (int k = 1; k < to; k++)
        //            {
        //                var leq = new WS1SLt(("x" + (k - 1)), ("x" + k));
        //                var axk = new WS1SPred(s1.MkCharConstraint('a', false), ("x" + (k - 1)));
        //                var cxk = new WS1SPred(s1.MkCharConstraint('c', false), ("x" + (k - 1)));
        //                var inter = new WS1SOr(new WS1SAnd(leq, axk), cxk);
        //                phi = new WS1SAnd(phi, inter);

        //            }
        //            for (int k = to - 1; k >= 0; k--)
        //            {
        //                phi = new WS1SExists(("x" + k), phi);
        //            }

        //            exycy = new WS1SExists(("y"), new WS1SPred(s1.MkCharConstraint('c', false), ("y")));
        //            phi = new WS1SAnd(phi, exycy);

        //            var t4 = 60000L * numTests;
        //            if (to <= maxmphipopold)
        //            {
        //                sw.Restart();
        //                for (int t = 0; t < numTests; t++)
        //                {
        //                    phi.getAutomaton(s1);
        //                }
        //                sw.Stop();
        //                t4 = sw.ElapsedMilliseconds;
        //            }

        //            file.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);
        //            Console.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests + "," + (double)t4 / numTests);

        //        }
        //    }
        //}



        public static void MintermTest()
        {

            var sw = new Stopwatch();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"..\msomintermp1s1.txt"))
            {
                for (int size = 2; size < kminterm; size++)
                {
                    // Tsolve no force
                    var s1 = new CharSetSolver(BitWidth.BV64);
                    var solver = new CartesianAlgebraBDD<BDD>(s1);

                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 1; k < size; k++)
                    {
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < size; k++)
                    {
                        var axk = new WS1SPred<BDD>(s1.MkBitTrue(k), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(solver);
                    }
                    sw.Stop();

                    var t1 = sw.ElapsedMilliseconds;

                    file.WriteLine((double)t1 / numTests);
                    Console.WriteLine((double)t1 / numTests);
                }
            }
            using (System.IO.StreamWriter file =
           new System.IO.StreamWriter(@"..\msomintermp2s1.txt"))
            {
                for (int size = 2; size < kminterm; size++)
                {

                    // Tsolve force
                    var s1 = new CharSetSolver(BitWidth.BV64);
                    var solver = new CartesianAlgebraBDD<BDD>(s1);

                    WS1SFormula<BDD> phi = new WS1STrue<BDD>();

                    for (int k = 0; k < size; k++)
                    {
                        var axk = new WS1SPred<BDD>(s1.MkBitTrue(k), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
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
                        var leq = new WS1SLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, leq);

                    }
                    for (int k = 0; k < size; k++)
                    {
                        var axk = new WS1SPred<BDD>(solver.MkBitTrue(k), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    var t1 = 60000L;


                    sw.Restart();
                    for (int t = 0; t < numTests; t++)
                    {
                        phi.GetAutomaton(alg);
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
                        var axk = new WS1SPred<BDD>(solver.MkBitTrue(k), new Variable("x" + k, true));
                        phi = new WS1SAnd<BDD>(phi, axk);

                    }
                    for (int k = size - 1; k >= 0; k--)
                    {
                        phi = new WS1SExists<BDD>(new Variable("x" + k, true), phi);
                    }

                    var t1 = 60000L;
                    if (size <= maxmint)
                    {
                        sw.Restart();
                        for (int t = 0; t < numTests; t++)
                        {
                            phi.GetAutomaton(alg);
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
                        predicates[k] = solver.MkBitTrue(k);

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

            var aut = phi.GetAutomaton(Z, phi.FreeVariables);

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

            var aut = phi.GetAutomaton(Z, phi.FreeVariables);

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

            var aut = phi.GetAutomaton(Z, phi.FreeVariables);
            return aut;
        }



    }
}

