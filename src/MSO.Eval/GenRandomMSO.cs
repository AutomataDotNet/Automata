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
    public class GenRandomMSO
    {
        static CartesianAlgebraBDD<BoolExpr> solver;
        static MSOFormula<BoolExpr> formula;
        static List<BoolExpr> predicates;
        static void Minterm()
        {
            try
            {
                z3.GenerateMinterms(predicates.ToArray()).ToList();
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
                formula.GetAutomaton(solver);
            }
            catch (ThreadAbortException)
            {
                // cleanup code, if needed...
            }
        }

        static int maxConst = 200;
        static long timeout = 5000;

        public static void Run(){

            random = new Random(0);

            c = new Context();
            z3 = new Z3BoolAlg(c, c.BoolSort);

            for (int numVars = 1; numVars < 4; numVars ++)
                for (int phisize = 10; phisize < 200; phisize += 10)
                {
                    foreach (var pair in GenerateMSOZ3Formulas(phisize, 1, 5))
                    {
                        formula = pair.First;
                        predicates = pair.Second;

                        var bdd = new BDDAlgebra();
                        //foreach (var p in predicates)
                        //    Console.WriteLine(p);
                        solver = new CartesianAlgebraBDD<BoolExpr>(bdd, z3);
                        var sw = new Stopwatch();
                        sw.Restart();
                        Thread t = new Thread(CartesianSolver);
                        t.Start();
                        long t1 = timeout;
                        if (!t.Join(TimeSpan.FromMilliseconds(timeout)))
                        {
                            t.Abort();
                            t1 = timeout;
                        }
                        else {
                            sw.Stop();
                            t1 = sw.ElapsedMilliseconds;
                        }

                        sw.Restart();
                        //t = new Thread(Minterm);
                        //t.Start();
                        long t2 = timeout;
                        //if (!t.Join(TimeSpan.FromMilliseconds(timeout)))
                        //{
                        //    t.Abort();
                        //    t2 = timeout;
                        //}
                        //else {
                        var mint = z3.GenerateMinterms(predicates.ToArray()).ToList();
                        sw.Stop();
                        t2 = sw.ElapsedMilliseconds;
                        //}

                        Console.WriteLine("#phi: " + predicates.Count + ", #mint: " + mint.Count +", time mint: " + (double)t2 + ", time ws1s: " + (double)t1);

                    }
                }
            Console.Read();

        }


        const bool app = false;        
        
        static int size;                 //Numb of states (computed randomly)
        static int alphVars;
        static Random random;
        static int seed;
        static int totgenerations;

        static Z3BoolAlg z3;
        static Context c;


        //Examples generated with params (6,15,1000), (10,12,1000),
        public static IEnumerable<Pair<MSOFormula<BoolExpr>, List<BoolExpr>>> GenerateMSOZ3Formulas(int maxS, int numVarsinAlphabet, int totGen)
        {                 
            totgenerations = totGen;
            alphVars = numVarsinAlphabet;

            

            for (seed = 0; seed < totgenerations; seed++)
            {
                size = maxS;

                var pair =  GenerateMSOFormula(1);       
                yield return new Pair<MSOFormula<BoolExpr>, List<BoolExpr>>(
                    new MSOExists<BoolExpr>(new Variable("x" + 0, true), pair.First), pair.Second);
            }
        }

        private static Pair<MSOFormula<BoolExpr>, List<BoolExpr>> GenerateMSOFormula(int maxVarIndex)
        {
            int randomNumber = random.Next(0, 8);
            size--;
            if (size <= 0)
            {
                int variable = random.Next(0, maxVarIndex-1);
                BoolExpr b = GeneratePredicate();
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
        private static BoolExpr GeneratePredicateOut()
        {
            var v = GeneratePredicate();
            Solver s = c.MkSolver();
            s.Assert(v);
            var res = s.Check();
            if (res == Status.SATISFIABLE)
                return v;
            else
                return GeneratePredicateOut();    

        }

        private static BoolExpr GeneratePredicate()
        {
            //ax+by+d%i=j


            var d= (IntExpr)(c.MkInt(random.Next(-maxConst, maxConst)));
            var i = (IntExpr)(c.MkInt(random.Next(-maxConst, maxConst)));
            var j = (IntExpr)(c.MkInt(random.Next(-maxConst, maxConst)));
            IntExpr ex = d;
            for(int v = 0;v< alphVars; v++)
            {
                ex = (IntExpr)(c.MkAdd(ex, c.MkMul(c.MkInt(random.Next(-maxConst, maxConst)), (IntExpr)(c.MkConst("y" + v, c.IntSort)))));
            }
            ex = c.MkMod(ex, i);
            return c.MkEq(ex, j);


            //switch (random.Next(0, 2))
            //{
            //    case 0:
            //        {
            //            IntExpr e1 = GenerateExprOfNumb();
            //            IntExpr e2 = c.MkInt(random.Next(0, maxConst));
            //            switch (random.Next(0, 5))
            //            {
            //                case 0:
            //                    {
            //                        return c.MkEq(e1, e2);
            //                    }
            //                case 1:
            //                    {
            //                        return c.MkGe(e1, e2);
            //                    }
            //                case 2:
            //                    {
            //                        return c.MkGt(e1, e2);
            //                    }
            //                case 3:
            //                    {
            //                        return c.MkLe(e1, e2);
            //                    }
            //                case 4:
            //                    {
            //                        return c.MkLt(e1, e2);
            //                    }
            //            }                        
            //            break;
            //        }
            //    case 1:
            //        {
            //            var v = random.Next(0, 4);

            //            BoolExpr e1 = GeneratePredicate();
            //            switch (v)
            //            {
            //                case 0:
            //                    {
            //                        BoolExpr e2 = GeneratePredicate();
            //                        return c.MkAnd(e1, e2);
            //                    }
            //                case 1:
            //                    {
            //                        BoolExpr e2 = GeneratePredicate();
            //                        return c.MkOr(e1, e2);
            //                    }
            //                case 2:
            //                    {
            //                        return c.MkNot(e1);
            //                    }

            //            }
            //            return e1;
            //        }
            //    case 2:
            //        {
            //            break;
            //        }
            //}
            //return c.MkTrue();
        }

        private static IntExpr GenerateExprOfNumb()
        {
            int randomNumber = random.Next(0,4);
            switch (randomNumber)
            {
                case 0:
                case 4:
                    {
                        return (IntExpr)(c.MkConst("y"+random.Next(0,1),c.IntSort));
                    }
                case 1:
                    {
                        var e1 = GenerateExprOfNumb();
                        var e2 = c.MkInt(random.Next(0, maxConst));
                        return (IntExpr)(c.MkAdd(e1, e2));
                    }
                case 2:
                    {
                        switch (random.Next(0, 2))
                        {
                            case 0:
                                {
                                    var e1 = GenerateExprOfNumb();
                                    var e2 = c.MkInt(random.Next(0, maxConst));
                                    return (IntExpr)(c.MkAdd(e1, e2));
                                }
                            case 1:
                            case 2:
                                {
                                    var e1 = GenerateExprOfNumb();
                                    var e2 = c.MkInt(random.Next(0, maxConst));
                                    return (IntExpr)(c.MkMod(e1, e2));
                                }
                        }
                        break;
                    }
                case 3:
                    {
                        var e1 = (IntExpr)(c.MkConst("y" + random.Next(0, 1), c.IntSort));
                        var e2 = c.MkInt(random.Next(0, maxConst));
                        return (IntExpr)(c.MkMul(e1, e2));
                    }
            }
            throw new Exception("this shouldn't happen");
        }
        #endregion
        
    }
}
