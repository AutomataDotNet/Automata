using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Automata;
using Microsoft.Z3;
using System.Diagnostics;

using Microsoft.Fast;

namespace RunExperiments
{
    class RecognizerGenerator
    {
        const bool app = false;

        static string pathDet = @"..\..\benchmark\fastProgramsDet\";
        static string pathComp = @"..\..\benchmark\fastProgramsComp\";

        static int predNumb = 10;
        static int langRulesNumb = 6;            //Max numb of rules per language        
        static int maxStateNum = 15;            // Max numb of states
        static int stateNum;                 //Numb of states (computed randomly)
        static int labelsNumb = 50;              //How many recognizers
        static int avgProcessedTreesSize = 3;    //Average subset size of recognizers used

        static Random random;
        static StringBuilder sbForGen;
        static int seed;
        static int totgenerations;


        //Examples generated with params (6,15,1000), (10,12,1000),
        public static void GenerateTAUsingFast(int langRules, int maxSt, int totGen)
        {
            DirectoryInfo detDir = new DirectoryInfo(pathDet);
            int fileName = detDir.GetFiles().Length + 1;

            langRulesNumb = langRules;
            maxStateNum = maxSt;
            totgenerations = totGen;

            for (seed = 0; seed < totgenerations; seed++)
            {
                Console.WriteLine(seed);
                random = new Random(seed);
                stateNum = random.Next(4, maxStateNum);
                //Generate one lang
                sbForGen = new StringBuilder();
                GenerateRecognizer();

                var pgm = Microsoft.Fast.Parser.ParseFromString(sbForGen.ToString());
                FastTransducerInstance fti=null;
                try
                {
                    fti = FastTransducerInstance.MkFastTransducerInstance(pgm);
                }
                catch (AutomataException e)
                {
                    fti = null;
                }

                if (fti != null)
                    foreach (var td in fti.treeDefinitions)
                    {
                        var tcd = td.Value;
                        foreach (var acceptor in tcd.acceptors)
                        {
                            var aut = acceptor.Value.Clean();
                            if (aut.IsEmpty)
                                break;

                            var tokenSource = new CancellationTokenSource();
                            CancellationToken token = tokenSource.Token;

                            TreeTransducer detAut = null;

                            var task = Task.Factory.StartNew(() =>
                            {
                                aut.IsDeterminstic();
                                detAut = aut.DeterminizeWithoutCompletion().RemoveUselessStates();
                                Console.WriteLine("done with det no comp");
                            }, token);

                            if (!task.Wait(Program.timeOut, token))
                                Console.WriteLine("The det Task timed out!");
                            else
                            {
                                Console.WriteLine("DET:" + detAut.RuleCount);

                                if (detAut.RuleCount < 100000 && detAut.RuleCount>1)
                                {
                                    var outstring = GetFastProg(detAut, acceptor.Key);
                                    Microsoft.Fast.Parser.ParseFromString(outstring);
                                    using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(pathDet + fileName))
                                    {                                        
                                        outfile.Write(outstring);
                                    }

                                    fileName++;

                                    if (detAut.RuleCount < 5000)
                                    {
                                        TreeTransducer completeAut = null;
                                        CancellationToken token2 = tokenSource.Token;
                                        var task2 = Task.Factory.StartNew(() =>
                                        {
                                            completeAut = detAut.Complete();
                                            Console.WriteLine("done with det with comp");
                                        }, token2);
                                        if (!task2.Wait(Program.timeOut, token2))
                                            Console.WriteLine("The compl Task timed out!");
                                        else
                                        {
                                            Console.WriteLine("COM:" + completeAut.RuleCount);
                                            if (completeAut.RuleCount < 100000)
                                            {
                                                outstring = GetFastProg(completeAut, acceptor.Key);
                                                Microsoft.Fast.Parser.ParseFromString(outstring);
                                                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(pathComp + (fileName - 1)))
                                                {
                                                    outfile.Write(outstring);                                                    
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
            }
        }

        public static string GetFastProg(TreeTransducer aut, string accName)
        {
            StringBuilder sb = new StringBuilder();
            FastGen fg = new FastGen(aut.TT.Z);
            fg.ToFast(aut.InputAlphabet, sb);
            fg.ToFast("fastacc", aut, sb, true);
            return sb.ToString();
            //foreach (var td in fti.treeDefinitions)
            //{
            //    var tcd = td.Value;
            //    var aut = tcd.acceptors[accName];
            //    var tokenSource = new CancellationTokenSource();
            //    CancellationToken token = tokenSource.Token;

            //    TreeTransducer detAut = null;

            //    var task = Task.Factory.StartNew(() =>
            //    {
            //        aut.IsDeterminstic();
            //        if(complete)
            //            detAut = aut.Determinize();
            //        else
            //            detAut = aut.DeterminizeWithoutCompletion().RemoveUselessStates();
            //    }, token);


            //    if (!task.Wait(Program.timeOut, token))
            //    {
            //        Console.WriteLine("The Task timed out!");
            //        return null;
            //    }
            //    else
            //    {
            //        StringBuilder sb = new StringBuilder();
            //        FastGen fg = new FastGen(detAut.TT.Z);
            //        fg.ToFast(td.Value.alphabet.alph, sb);
            //        fg.ToFast("fastacc", detAut, sb, true);
            //        return sb.ToString();
            //    }
            //}
            //return null;
        }


        public static void GenerateRecognizer()
        {            
            GenerateAlphabet();
            //GeneratePredicates();
            GenerateLanguages();
        }

        private static void GenerateAlphabet()
        {
            sbForGen.AppendLine("Alphabet Node[x:int,y:int,s:string]{Three(3),One(1),Zero(0),Cons(2),Nil(0)}");
        }

        /// <summary>
        /// Generates random fast languages
        /// </summary>
        public static void GenerateLanguages()
        {
            for (int i = 0; i < stateNum; i++)
            {
                if(i==0)
                    sbForGen.AppendLine("Public Lang l" + i + " : Node {");
                else
                    sbForGen.AppendLine("Lang l" + i + " : Node {");
                if(random.Next(0,5)==0)
                    GenerateZeroLangRule();
                else
                    GenerateZeroLangRuleFalse();
                for (int j = 0; j < random.Next(0, langRulesNumb); j++)
                    GenerateLangRule();
                sbForGen.AppendLine("}");
            }
        }
        

        #region expressions and predicates generator
        private static void GeneratePredicates()
        {
            for (int i = 0; i < predNumb; i++)
            {
                sbForGen.Append("fun ip" + i + "(i:int) : bool := ");
                GenerateBoolExprOfNumb("i", false);
                sbForGen.AppendLine("");
            }
            for (int i = 0; i < predNumb; i++)
            {
                sbForGen.Append("fun sp" + i + "(s:string) : bool := ");
                GenerateBoolExprOfString("s");
                sbForGen.AppendLine("");
            }
        }

        private static void GenerateBoolExprOfNumb(string var, bool isReal)
        {
            switch (random.Next(0, 4))
            {
                case 0:
                    {
                        sbForGen.Append("(");
                        GenerateExprOfNumb(var, isReal);
                        switch (random.Next(0, 5))
                        {
                            case 0:
                                {
                                    sbForGen.Append(" == ");
                                    break;
                                }
                            case 1:
                                {
                                    sbForGen.Append("< ");
                                    break;
                                }
                            case 2:
                                {
                                    sbForGen.Append("> ");
                                    break;
                                }
                            case 3:
                                {
                                    sbForGen.Append(" <= ");
                                    break;
                                }
                            case 4:
                                {
                                    sbForGen.Append(" >= ");
                                    break;
                                }
                        }                        
                        GenerateExprOfNumb(var, isReal);
                        sbForGen.Append(")");
                        break;
                    }
                case 1:
                    {
                        var v = random.Next(0, 4);

                        if (v == 2)
                        {
                            sbForGen.Append("( not ");
                            GenerateBoolExprOfNumb(var, isReal);
                            sbForGen.Append(")");
                        }
                        else
                        {

                            switch (v)
                            {
                                case 0:
                                case 3:
                                    {
                                        sbForGen.Append("(");
                                        GenerateBoolExprOfNumb(var, isReal);
                                        sbForGen.Append(" and ");
                                        GenerateBoolExprOfNumb(var, isReal);
                                        sbForGen.Append(")");
                                        break;
                                    }
                                case 1:
                                    {
                                        sbForGen.Append("(");
                                        GenerateBoolExprOfNumb(var, isReal);
                                        sbForGen.Append(" or ");
                                        GenerateBoolExprOfNumb(var, isReal);
                                        sbForGen.Append(")");
                                        break;
                                    }
                                case 2:
                                    {                                       
                                        break;
                                    }
                            }
                        }
                        break;
                    }
                case 2:
                case 3:
                    {
                        sbForGen.Append("true");
                        break;
                    }
            }
        }

        private static void GenerateExprOfNumb(string var, bool isReal)
        {
            int randomNumber = random.Next(0, 5);
            switch (randomNumber)
            {
                case 0:
                case 1:
                    {
                        sbForGen.Append(var);
                        break;
                    }
                case 2:
                    {
                        sbForGen.Append("(");
                        GenerateExprOfNumb(var, isReal);
                        switch (random.Next(0, 3))
                        {
                            case 0:
                                {
                                    sbForGen.Append(" + ");
                                    break;
                                }
                            case 1:
                            case 2:
                                {
                                    sbForGen.Append(" - ");
                                    break;
                                }
                        }                      
                        GenerateExprOfNumb(var, isReal);
                        sbForGen.Append(")");
                        break;
                    }
                case 3:
                case 4:
                    {
                        if(isReal)
                            sbForGen.Append(random.NextDouble().ToString().Substring(0, 5));
                        else
                            sbForGen.Append(random.Next(0,9999).ToString());
                        break;
                    }
            }
        }

        private static string GenerateBoolExprOfString(string var)
        {
            if (random.Next(0, avgProcessedTreesSize) == 0)
                return "(" + var + " == \"" + random.Next(0, labelsNumb) + "\")";                
            else
                return "(or (" + var + " == \"" + random.Next(0, labelsNumb) + "\")  " + GenerateBoolExprOfString(var) + ")";
        }

        private static void GenerateString()
        {
            sbForGen.Append("\"" +random.Next(0, labelsNumb) + "\"");
        }
        #endregion

        #region rules generator
        private static void GenerateLangRule()
        {
            sbForGen.Append("\t| ");
            switch(random.Next(0, 2)){                
                case 0:{
                    sbForGen.Append("One(x1) where (");
                    GenerateBoolExprOfNumb("x", false);
                    sbForGen.Append(" and ");
                    GenerateBoolExprOfNumb("y", false);                    
                    sbForGen.Append(")");
                    sbForGen.Append(" given (l" + random.Next(0, stateNum) + " x1) ");
                    sbForGen.AppendLine();
                    break;    
                }
                case 1:{
                    sbForGen.Append("Three(x1,x2,x3) where (");
                    GenerateBoolExprOfNumb("x", false);
                    sbForGen.Append(" and ");
                    GenerateBoolExprOfNumb("y", false);
                    sbForGen.Append(") given (l" + random.Next(0, stateNum) + " x1) (l" + random.Next(0, stateNum) + " x2) (l" + random.Next(0, stateNum) + " x3)");
                    sbForGen.AppendLine();
                    break;
                }
            }
            
        }

        private static void GenerateTranRule(int id)
        {
            sbForGen.Append("\t| Three(x1,x2,x3) where (");
            GenerateBoolExprOfNumb("x", false);
            sbForGen.Append(" and ");
            GenerateBoolExprOfNumb("y", false);
            sbForGen.Append(") ");
            //if (random.Next(0, 3) == 0)
            //    file.Append("given (l" + random.Next(0, langNumb) + " x2) (l" + random.Next(0, langNumb) + " x3) ");
            sbForGen.Append("to (Three [i,x,y,s] (One [i,x,y,s] x1) (t" + random.Next(0, id+1) + " x2) (t" + random.Next(0, id+1) + " x3))");
            sbForGen.AppendLine();
        }

        private static void GenerateZeroLangRule()
        {
            sbForGen.Append("\t  Zero()");
            sbForGen.AppendLine();
        }

        private static void GenerateZeroLangRuleFalse()
        {
            sbForGen.Append("\t  Zero() where false");
            sbForGen.AppendLine();
        }

        private static void GenerateIdentityTranRule()
        {
            sbForGen.Append("\t  Zero() to (Zero [i,x,y,s])"); 
            sbForGen.AppendLine();
            sbForGen.Append("\t| Three(x1,x2,x3) to (Three [i,x,y,s] x1 x2 x3)");
            sbForGen.AppendLine();            
        }

        private static void GenerateConsNilTranRule(int i)
        {
            sbForGen.Append("\t| Cons(x1,x2) where ");
            string s = GenerateBoolExprOfString("s");
            sbForGen.AppendLine(s+" to (Cons [i,x,y,s] (t" + i + " x1) (t" + i + " x2))");
            sbForGen.Append("\t| Cons(x1,x2) where ");
            sbForGen.AppendLine("(not " +s+") to (Cons [i,x,y,s] x1 (t" + i + " x2))");
            sbForGen.AppendLine("\t| Nil() to (Nil [i,x,y,s])");
        }
        #endregion

    }
}
