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
    public class LTLMSO
    {
        static int numTests = 1;
        static MSOFormula<BDD> phi;
        static BDDAlgebra<BDD> bddSolver;
        static CartesianAlgebraBDD<BDD> cartSolver;

        public static void RunM2LSTR()
        {
            string outF = outF = @"ltltest.csv";
            var inpD = @"C:\github\automatark\m2l-str\LTL-finite\";

            AutomatarkMsoFormulasTest(inpD, outF);           
        }

        public static void RunWS1S()
        {
            string outF = @"ws1s-generated.csv";
            string inpD = @"C:\github\automatark\ws1s\generated-formulae\";

            AutomatarkMsoFormulasTest(inpD, outF);

            //outF = @"..\ws1s-STRAND.csv";
            //inpD = @"C:\github\automatark\ws1s\STRAND\";
            //LTLTest(inpD, outF);
        }


        //LTL over finite traces
        private static void AutomatarkMsoFormulasTest(string inputDir, string outFile)
        {
            Console.WriteLine("fileName , generic-bdd, product");
            using (System.IO.StreamWriter file =
               new System.IO.StreamWriter(outFile))
            {
                var files = new List<string>(Directory.EnumerateFiles(inputDir, "*.mona", SearchOption.AllDirectories));
                files.Sort((s1, s2) => cmp(s1, s2));
                foreach (string fileName in files)
                {
                    string contents = File.ReadAllText(fileName);
                    MonaProgram pgm1 = MonaParser.Parse(contents);
                    phi = pgm1.ToMSO();

                    var bv7 = new CharSetSolver(BitWidth.BV7);
                    bddSolver = new BDDAlgebra<BDD>(bv7);
                    var sw = new Stopwatch();
                    sw.Restart();
                    Thread t = new Thread(BDDSolver);
                    t.Start();
                    long t1 = 5000;
                    if (!t.Join(TimeSpan.FromSeconds(5)))
                    {
                        t.Abort();
                        t1 = 5000;
                    }
                    else {
                        sw.Stop();
                        t1 = sw.ElapsedMilliseconds;
                    }

                    bv7 = new CharSetSolver(BitWidth.BV7);
                    cartSolver = new CartesianAlgebraBDD<BDD>(bv7);

                    sw.Restart();
                    t = new Thread(CartesianSolver);
                    t.Start();
                    long t2 = 5000;
                    if (!t.Join(TimeSpan.FromSeconds(5)))
                    {
                        t.Abort();
                        t2 = 5000;
                    }
                    else {
                        sw.Stop();
                        t2 = sw.ElapsedMilliseconds;
                    }

                    //if (t2 > 5000)
                    //    t2 = 5000;

                    file.WriteLine(fileName + "," + (double)t1 / numTests + "," + (double)t2 / numTests);
                    Console.WriteLine(fileName + "," + (double)t1 / numTests + "," + (double)t2 / numTests);
                }
            }
        }

        static void BDDSolver()
        {
            try
            {
                var aut = phi.GetAutomaton(bddSolver, false);
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
                var i1 = int.Parse(s1splits[s1splits.Length - 1].Substring(s1splits[s1splits.Length - 1].IndexOf("form") + 4, s1splits[s1splits.Length - 1].Length - 5 - ind1));
                var i2 = int.Parse(s2splits[s2splits.Length - 1].Substring(s2splits[s2splits.Length - 1].IndexOf("form") + 4, s2splits[s2splits.Length - 1].Length - 5 - ind2));
                return i1 - i2;
            }

            return s1.CompareTo(s2);
        }
    }
}
