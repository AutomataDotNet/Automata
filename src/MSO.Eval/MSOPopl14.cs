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
    public class MSOPopl14
    {
        
        static int numTests = 1;
        static int kpopl = 40;
   

        static long timeout = 60000;


        public static void RunPOPLTests()
        {
            //// all x1...xn. xi<xi+1
            List<Tuple<MSOFormula<BDD>, CharSetSolver>> phis = new List<Tuple<MSOFormula<BDD>, CharSetSolver>>();

            for (int to = 2; to < kpopl; to++)
            {
                var solver = new CharSetSolver();
                MSOFormula<BDD> phi = new MSOTrue<BDD>();

                for (int k = 1; k < to; k++)
                {
                    var leq = new MSOLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                    phi = new MSOAnd<BDD>(phi, leq);

                }
                for (int k = to - 1; k >= 0; k--)
                {
                    phi = new MSOExists<BDD>(new Variable("x" + k, true), phi);
                }

                phis.Add(new Tuple<MSOFormula<BDD>, CharSetSolver>(phi, solver));
            }

            RunTest(new StreamWriter(@"popl14-1.csv"), phis);


            // all x1...xn. xi<xi+1 and a(xi)
            phis = new List<Tuple<MSOFormula<BDD>, CharSetSolver>>();
            for (int to = 2; to < kpopl; to++)
            {
                var solver = new CharSetSolver(BitWidth.BV64);
                MSOFormula<BDD> phi = new MSOTrue<BDD>();

                for (int k = 1; k < to; k++)
                {
                    var leq = new MSOLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                    phi = new MSOAnd<BDD>(phi, leq);

                }
                for (int k = 0; k < to; k++)
                {
                    var axk = new MSOPredicate<BDD>(
                        solver.MkCharConstraint('a', false), new Variable("x" + k, true));
                    phi = new MSOAnd<BDD>(phi, axk);

                }
                for (int k = to - 1; k >= 0; k--)
                {
                    phi = new MSOExists<BDD>(new Variable("x" + k, true), phi);
                }
                phis.Add(new Tuple<MSOFormula<BDD>, CharSetSolver>(phi, solver));
            }
            RunTest(new StreamWriter(@"popl14-2.csv"), phis);

            // all x1...xn. (xi<xi+1 and a(xi)) and ex y. c(y)
            phis = new List<Tuple<MSOFormula<BDD>, CharSetSolver>>();
            for (int to = 2; to < kpopl; to++)
            {
                var solver = new CharSetSolver(BitWidth.BV64);
                MSOFormula<BDD> phi = new MSOTrue<BDD>();

                for (int k = 1; k < to; k++)
                {
                    var leq = new MSOLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                    phi = new MSOAnd<BDD>(phi, leq);

                }
                for (int k = 0; k < to; k++)
                {
                    var axk = new MSOPredicate<BDD>(solver.MkCharConstraint('a', false), new Variable("x" + k, true));
                    phi = new MSOAnd<BDD>(phi, axk);

                }
                for (int k = to - 1; k >= 0; k--)
                {
                    phi = new MSOExists<BDD>(new Variable("x" + k, true), phi);
                }

                var exycy = new MSOExists<BDD>(new Variable("y", true), new MSOPredicate<BDD>(solver.MkCharConstraint('c', false), new Variable("y", true)));
                phi = new MSOAnd<BDD>(phi, exycy);

                phis.Add(new Tuple<MSOFormula<BDD>, CharSetSolver>(phi, solver));
            }

            RunTest(new StreamWriter(@"popl14-3.csv"), phis);

            // all x1...xn. (xi<xi+1 and a(xi) \/ c(xi))

            phis = new List<Tuple<MSOFormula<BDD>, CharSetSolver>>();
            for (int to = 2; to < kpopl; to++)
            {
                var solver = new CharSetSolver(BitWidth.BV64);
                MSOFormula<BDD> phi = new MSOTrue<BDD>();

                for (int k = 1; k < to; k++)
                {
                    var leq = new MSOLt<BDD>(new Variable("x" + (k - 1), true), new Variable("x" + k, true));
                    var axk = new MSOPredicate<BDD>(solver.MkCharConstraint('a', false), new Variable("x" + (k - 1), true));
                    var cxk = new MSOPredicate<BDD>(solver.MkCharConstraint('c', false), new Variable("x" + (k - 1), true));
                    var inter = new MSOOr<BDD>(new MSOAnd<BDD>(leq, axk), cxk);
                    phi = new MSOAnd<BDD>(phi, inter);

                }
                for (int k = to - 1; k >= 0; k--)
                {
                    phi = new MSOExists<BDD>(new Variable("x" + k, true), phi);
                }

                MSOFormula<BDD> exycy = new MSOExists<BDD>(new Variable("y", true), new MSOPredicate<BDD>(solver.MkCharConstraint('c', false), new Variable("y", true)));
                phi = new MSOAnd<BDD>(phi, exycy);

                phis.Add(new Tuple<MSOFormula<BDD>, CharSetSolver>(phi, solver));
            }

            RunTest(new StreamWriter(@"popl14-4.csv"), phis, 11, 11, 11);
        }


        //Run the test on each phi in phis and store result in infFile
        static void RunTest(System.IO.StreamWriter outFile, List<Tuple<MSOFormula<BDD>, CharSetSolver>> phis, int stop1At = 100, int stop2At = 100, int stop3At = 100)
        {

            var sw = new Stopwatch();

            using (System.IO.StreamWriter file = outFile)
            {
                Console.WriteLine("k, old, cartesian, generic-bdd");
                file.WriteLine("k, old, cartesian, generic-bdd");
                int to = 2;
                foreach (var p in phis)
                {
                    // T1
                    var t1 = timeout;
                    if (to < stop1At)
                    {
                        try
                        {
                            sw.Restart();
                            for (int t = 0; t < numTests; t++)
                            {
                                p.Item1.GetAutomaton(p.Item2);
                            }
                            sw.Stop();
                            t1 = sw.ElapsedMilliseconds;
                        }
                        catch (OutOfMemoryException)
                        {
                            t1 = timeout * numTests;
                        }
                    }

                    //T2
                    var t2 = timeout;
                    if (to < stop1At)
                    {
                        var cartesianBDD = new CartesianAlgebraBDD<BDD>(p.Item2);

                        try
                        {
                            sw.Restart();
                            for (int t = 0; t < numTests; t++)
                            {
                                p.Item1.GetAutomaton(cartesianBDD);
                            }
                            sw.Stop();
                            t2 = sw.ElapsedMilliseconds;

                        }
                        catch (OutOfMemoryException)
                        {
                            t2 = timeout * numTests;
                        }
                    }

                    // T3
                    var t3 = timeout;
                    if (to < stop3At)
                    {
                        BDDAlgebra<BDD> cartesianProduct = new BDDAlgebra<BDD>(p.Item2);

                        try
                        {
                            sw.Restart();
                            for (int t = 0; t < numTests; t++)
                            {
                                p.Item1.GetAutomaton(cartesianProduct, false);
                            }
                            sw.Stop();

                            t3 = sw.ElapsedMilliseconds;
                        }
                        catch (OutOfMemoryException)
                        {
                            t3 = timeout * numTests;
                        }
                    }

                    file.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests);
                    Console.WriteLine(to + "," + (double)t1 / numTests + "," + (double)t2 / numTests + "," + (double)t3 / numTests);
                    to++;
                }
            }
        }

    }
}
