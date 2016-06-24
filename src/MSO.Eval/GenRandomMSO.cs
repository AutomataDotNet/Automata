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

namespace MSO.Eval
{
    public class GenRandomMSO
    {
        static CartesianAlgebraBDD<BoolExpr> solver;
        static MSOFormula<BoolExpr> formula;
        static List<BoolExpr> predicates;
        
        static int maxConst = 15;
        static long timeout = 5000;
        static int howMany =100;
        static int currSeed;

        public static void Run()
        {


            using (System.IO.StreamWriter file =
               new System.IO.StreamWriter(@"..\randomMSOInt.csv", false))
            {
                file.WriteLine("num-predicates, num-minterms, minterm-time, generic-BDD, product ");
            }
            random = new Random(0);

            currSeed = 0;


            for (int maxConst = 3; maxConst < 4; maxConst++)
                for (int phisize = 5; phisize < 8; phisize += 1)
                {
                    Console.WriteLine(maxConst + "," + phisize);


                    for (int i = 0; i < howMany; i++)
                    {
                        c = new Context();
                        z3 = new Z3BoolAlg(c, c.IntSort, timeout);
                        size = phisize;
                        try
                        {
                            var pair = GenerateMSOZ3Formula();

                        if (maxConst == 4 && phisize > 5)
                            break;

                        

                            formula = pair.First;
                            predicates = pair.Second;
                            if (predicates.Count > 2)
                            {
                                var bddsolver = new BDDAlgebra<BoolExpr>(z3);
                                var sw = new Stopwatch();
                                sw.Restart();

                                long tbdd = timeout;

                                try
                                {
                                    formula.GetAutomaton(bddsolver, false);
                                    sw.Stop();
                                    tbdd = sw.ElapsedMilliseconds;
                                    if (tbdd > timeout)
                                        tbdd = timeout;
                                }
                                catch (Z3Exception e)
                                {
                                    tbdd = timeout;
                                }
                                catch (AutomataException e)
                                {
                                    tbdd = timeout;
                                }




                                if (tbdd != timeout)
                                {
                                    long tcart = timeout;
                                    try
                                    {
                                        var bdd = new BDDAlgebra();
                                        solver = new CartesianAlgebraBDD<BoolExpr>(bdd, z3);
                                        sw.Restart();
                                        formula.GetAutomaton(solver);
                                        sw.Stop();
                                        tcart = sw.ElapsedMilliseconds;
                                        if (tcart > timeout)
                                            tcart = timeout;
                                    }
                                    catch (Z3Exception e)
                                    {
                                        tcart = timeout;
                                    }
                                    catch (AutomataException e)
                                    {
                                        tcart = timeout;
                                    }

                                    sw.Restart();
                                    long tminterm = timeout;
                                    List<Pair<bool[], BoolExpr>> mint = new List<Pair<bool[], BoolExpr>>();
                                    try
                                    {
                                        mint = z3.GenerateMinterms(predicates.ToArray()).ToList();
                                        sw.Stop();
                                        tminterm = sw.ElapsedMilliseconds;
                                        if (tminterm > timeout)
                                            tminterm = timeout;
                                    }
                                    catch (Z3Exception e)
                                    {

                                        tminterm = timeout;

                                    }
                                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"..\randomMSOInt.csv", true))
                                    {
                                        Console.WriteLine("#phi: " + predicates.Count + ", #mint: " + (tminterm == timeout ? (int)Math.Pow(2, predicates.Count) : mint.Count) + ", time mint: " + (double)tminterm + ", generic-bdd: " + (double)tbdd + ", product: " + (double)tcart);
                                        file.WriteLine(predicates.Count + ", " + (tminterm == timeout ? (int)Math.Pow(2, predicates.Count) : mint.Count) + ", " + (double)tminterm + ", " + (double)tbdd + ", " + (double)tcart);
                                    }
                                }
                                else {
                                    Console.WriteLine("moving to next one");

                                }
                            }
                            else
                            {
                                Console.WriteLine("moving to next one");

                            }
                        }
                        catch (Z3Exception e)
                        {
                            Console.WriteLine("Z3 out of memory , time to stop");
                            return;

                        }
                    }
                }
        }


        const bool app = false;        
        
        static int size;                 //Numb of states (computed randomly)
        static Random random;
        static int seed;
        static int totgenerations;

        static Z3BoolAlg z3;
        static Context c;


        //Examples generated with params (6,15,1000), (10,12,1000),
        public static Pair<MSOFormula<BoolExpr>, List<BoolExpr>> GenerateMSOZ3Formula()
        {
            currSeed++;
            

            var pair =  GenerateMSOFormula(1);       
            return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(
                new MSOExists<BoolExpr>(new Variable("x" + 0, true), pair.First), pair.Second);
            
        }

        private static Pair<MSOFormula<BoolExpr>, List<BoolExpr>> GenerateMSOFormula(int maxVarIndex)
        {
            int randomNumber = random.Next(0, 8);
            size--;
            if (size <= 0)
            {
                int variable = random.Next(0, maxVarIndex-1);
                BoolExpr b = GeneratePredicateOut(200);
                List<BoolExpr> l = new List<BoolExpr>();
                l.Add(b);
                return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(new MSOPredicate<BoolExpr>(b, new Variable("x"+variable, true)), l);
            }
            switch (randomNumber)
            {
                case 0:
                    {
                        Pair<MSOFormula<BoolExpr>, List<BoolExpr>> phi1 = GenerateMSOFormula(maxVarIndex + 1);
                        MSOFormula<BoolExpr> phi = new MSOExists<BoolExpr>(new Variable("x"+maxVarIndex, true), phi1.First);
                        return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(phi, phi1.Second);
                    }
                case 1:
                    {
                        Pair<MSOFormula<BoolExpr>, List<BoolExpr>> phi1 = GenerateMSOFormula(maxVarIndex + 1);
                        MSOFormula<BoolExpr> phi = new MSOForall<BoolExpr>(new Variable("x" + maxVarIndex, true), phi1.First);
                        return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(phi, phi1.Second);
                    }
                case 2:
                case 3:
                    {
                        Pair<MSOFormula<BoolExpr>, List<BoolExpr>> phi1 = GenerateMSOFormula(maxVarIndex);
                        Pair<MSOFormula<BoolExpr>, List<BoolExpr>> phi2 = GenerateMSOFormula(maxVarIndex);
                        MSOFormula<BoolExpr> phi = new MSOAnd<BoolExpr>(phi1.First, phi2.First);
                        return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(phi, new List<BoolExpr>(phi1.Second.Union(phi2.Second)));
                    }
                case 4:
                case 5:
                    {
                        Pair<MSOFormula<BoolExpr>, List<BoolExpr>> phi1 = GenerateMSOFormula(maxVarIndex);
                        Pair<MSOFormula<BoolExpr>, List<BoolExpr>> phi2 = GenerateMSOFormula(maxVarIndex);
                        MSOFormula<BoolExpr> phi = new MSOOr<BoolExpr>(phi1.First, phi2.First);
                        return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(phi, new List<BoolExpr>(phi1.Second.Union(phi2.Second)));
                    }
                case 6:
                    {
                        Pair<MSOFormula<BoolExpr>, List<BoolExpr>> phi1 = GenerateMSOFormula(maxVarIndex);
                        MSOFormula<BoolExpr> phi = new MSONot<BoolExpr>(phi1.First);
                        return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(phi, phi1.Second);
                    }
                case 7:
                    {
                        if (maxVarIndex > 1)
                        {
                            int variable1 = random.Next(0, maxVarIndex - 1);
                            int variable2 = random.Next(0, maxVarIndex - 1);
                            if (variable1 == variable2)
                            {
                                if (variable1 == maxVarIndex - 1)
                                    variable1 = variable1 - 1;
                                else
                                    variable2 = variable2 + 1;
                            }

                            //Successor
                            MSOFormula<BoolExpr> phi = new MSOSuccN<BoolExpr>(varOf(variable1),varOf(variable2),random.Next(1,4));
                            return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(phi, new List<BoolExpr>());
                        }
                        else
                        {
                            int variable = random.Next(0, maxVarIndex - 1);
                            BoolExpr b = GeneratePredicate();
                            List<BoolExpr> l = new List<BoolExpr>();
                            l.Add(b);
                            return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(new MSOPredicate<BoolExpr>(b, new Variable("x" + variable, true)), l);
                        }

                    }
                case 8:
                    {
                        int variable1 = random.Next(0, maxVarIndex - 1);
                        int variable2 = random.Next(0, maxVarIndex - 1);

                        //less than
                        MSOFormula<BoolExpr> phi = new MSOLe<BoolExpr>(varOf(variable1), varOf(variable2));
                        return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(phi, new List<BoolExpr>());
                    }
            }
            return null;
        }
        
        private static Variable varOf(int n)
        {
            return new Variable("x" + n, true);
        }

        #region expressions and predicates generator
        private static BoolExpr GeneratePredicateOut(int attemptsLeft)
        {
            if (attemptsLeft == 0)
            {
                var i = (IntExpr)(c.MkInt(2));
                var ex = (IntExpr)(c.MkMul(c.MkInt(5), (IntExpr)(c.MkConst("x", c.IntSort))));

                ex = c.MkMod(ex, i);
                return c.MkEq(ex, (IntExpr)(c.MkInt(1)));
            }
            try
            {
                var v = GeneratePredicate();


                Solver s = c.MkSolver();
                s.Assert(v);
                var res = s.Check();
                if (res == Status.SATISFIABLE)
                    return v;
                else 
                    return GeneratePredicateOut(attemptsLeft-1);
            }
            catch (Z3Exception e)
            {
                return GeneratePredicateOut(attemptsLeft-1);
            }


        }

        private static BoolExpr GeneratePredicate()
        {
            //ax+by+d%i=j


            var d= (IntExpr)(c.MkInt(random.Next(-maxConst, maxConst)));
            var i = (IntExpr)(c.MkInt(random.Next(0, maxConst/2)*2+1));
            var j = (IntExpr)(c.MkInt(random.Next(-maxConst, maxConst)));
            IntExpr ex = d;
            ex = (IntExpr)(c.MkAdd(ex, c.MkMul(c.MkInt(random.Next(-maxConst, maxConst)), (IntExpr)(c.MkConst("x", c.IntSort)))));
            
            ex = c.MkMod(ex, i);
            return c.MkEq(ex, j);

            
        }
        #endregion
        
    }
}
