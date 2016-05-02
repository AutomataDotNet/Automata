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

namespace RunExperiments
{
    class Util
    {       
        static int complimit = 350000;
        static int detlimit = 15000;

        //Runs the three algorithms. It requires the deterministic automton, the total automaton, and the output file
        public static void RunAllAlgorithms(TreeTransducer detAut, TreeTransducer totAut,
            string exampleName, string outputFileName)
        {
            TreeTransducer algo1Min = null;
            TreeTransducer algo2Min = null;
            TreeTransducer algoSFAMin = null;

            var algo1TO = false;
            var algo2TO = false;
            var algoSFATO = false;

            if (detAut == null)
                return;

            // Completion time
            //int time = System.Environment.TickCount;
            //if (totAut != null)
            //{
            //    for (int i = 0; i < Program.numTests; i++)
            //        if (ActionTimesOut(() => { algo1Min = detAut.Complete(); }))
            //        {
            //            compTO = true; break;
            //        }
            //}
            //var timeComp = (System.Environment.TickCount - time) / Program.numTests;

            detAut.Clean();

            // Algorithm 1
            int time = System.Environment.TickCount;
            if (totAut != null)
            {
                //TODO Remove this                
                if (totAut.RuleCount < complimit)
                    for (int i = 0; i < Program.numTests; i++)
                    {
                        if (ActionTimesOut(() => { algo1Min = totAut.Minimize(); }))
                        {
                            algo1TO = true; break;
                        }
                    }
                else
                {
                    algo1TO = true;
                }
            }
            else
                algo1TO = true;
            var timeAlgo1 = (System.Environment.TickCount - time) / Program.numTests;

            // Algorithm 2
            time = System.Environment.TickCount;
            if (detAut != null)
            {
                //TODO Remove this
                for (int i = 0; i < Program.numTests; i++)
                    if (ActionTimesOut(() => { algo2Min = detAut.Minimize(); }))
                    {
                        algo2TO = true; break;
                    }
            }

            else
                algo2TO = true;
            var timeAlgo2 = (System.Environment.TickCount - time) / Program.numTests;

            //Console.WriteLine(algo2Min.StateCount);

            // Algorithm SFA            
            time = System.Environment.TickCount;
            if (detAut != null)
            {
                if (detAut.RuleCount < detlimit)
                {
                    for (int i = 0; i < Program.numTests; i++)
                        if (ActionTimesOut(() => { algoSFAMin = detAut.MinimizeViaSFA(); }))
                        {
                            algoSFATO = true; break;
                        }
                }
            }
            else
                algoSFATO = true;

            var timeAlgoSFA = (System.Environment.TickCount - time) / Program.numTests;

            //Check that results are correct
            if (algoSFAMin != null && algo2Min != null)
            {
                if (algo2Min.StateCount - algoSFAMin.StateCount > 1 || algo2Min.StateCount - algoSFAMin.StateCount < -1)
                {
                    detAut.Clean();
                    var v = detAut.IsDeterminstic();
                    Console.WriteLine(v);
                    Console.WriteLine("wrong mini");
                }
            }

            // Append results on file 
            // ID, StateCount, RuleCount, CompleteRuleCount, MinStateCount, MinRuleCount, PTime, CTime
            using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(Program.path + outputFileName, true))
            {
                outfile.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}",
                    exampleName, detAut.StateCount, detAut.RuleCount,
                    totAut == null ? -1 : totAut.RuleCount,
                    algo2Min == null ? "NA" : algo2Min.StateCount.ToString(),
                    algo2Min == null ? "NA" : algo2Min.RuleCount.ToString(),
                    //compTO ? Program.timeOut.ToString() : timeComp.ToString(),
                    algo1TO ? Program.timeOut.ToString() : timeAlgo1.ToString(),
                    algo2TO ? Program.timeOut.ToString() : timeAlgo2.ToString(),
                    algoSFATO ? Program.timeOut.ToString() : timeAlgoSFA.ToString());
            }

        }

        private static bool ActionTimesOut(Action a)
        {
            var tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            var task = Task.Factory.StartNew(a, token);
            if (!task.Wait(Program.timeOut, token))
                return true;
            return false;
        }

        public static void GatherResults(String inputFile, String outputPrefix)
        {
            string[] lines = System.IO.File.ReadAllLines(Program.path + inputFile);

            Dictionary<int, Tuple<int, int, int, int>> statesToRes = new Dictionary<int,Tuple<int,int,int,int>>();
            Dictionary<int, Tuple<int, int, int, int>> transToRes = new Dictionary<int,Tuple<int,int,int,int>>();
            Dictionary<int, Tuple<int, int, int, int>> compTransToRes = new Dictionary<int,Tuple<int,int,int,int>>();
            Dictionary<int, Tuple<int, int, int, int>> minTransToRes = new Dictionary<int,Tuple<int,int,int,int>>();

            foreach (string line in lines)
            {
                if (!line.Contains("State"))
                {

                    var spl = line.Split(',');
                    if (spl.Length > 1)
                    {
                        var stC = Int32.Parse(spl[1]);
                        var trC = Int32.Parse(spl[2]);
                        var trCompC = Int32.Parse(spl[3]);

                        int trMinC = -1;
                        if (!spl[5].Contains("NA"))
                            trMinC = Int32.Parse(spl[5]);

                        //To avoid 0 ms just add 1 ms to everything
                        var timeAlg1 = Int32.Parse(spl[6]) + 1;
                        var timeAlg2 = Int32.Parse(spl[7]) + 1;
                        var timeAlgSFA = Int32.Parse(spl[8]) + 1;

                        statesToRes[stC] = update(!statesToRes.ContainsKey(stC) ? null : statesToRes[stC],
                          timeAlg1, timeAlg2, timeAlgSFA);
                        transToRes[trC] = update(!transToRes.ContainsKey(trC) ? null : transToRes[trC], timeAlg1, timeAlg2, timeAlgSFA);
                        if (trCompC > 0)
                            compTransToRes[trCompC] = update(!compTransToRes.ContainsKey(trCompC) ? null : compTransToRes[trCompC], timeAlg1, timeAlg2, timeAlgSFA);
                        if (trMinC >= 0)
                            minTransToRes[trMinC] = update(!minTransToRes.ContainsKey(trMinC) ? null : minTransToRes[trMinC], timeAlg1, timeAlg2, timeAlgSFA);
                    }
                }
            }
            //States
            WriteToFile(statesToRes, outputPrefix, "States.txt");
            WriteToFile(transToRes, outputPrefix, "Trans.txt");
            WriteToFile(compTransToRes, outputPrefix, "CompTrans.txt");
            WriteToFile(minTransToRes, outputPrefix, "MinTrans.txt");
        }

        public static void GatherResultsTimbuk(String inputFile, String timbukFile, String outputPrefix)
        {
            string[] lines = System.IO.File.ReadAllLines(Program.path + inputFile);
            string[] dmlines = System.IO.File.ReadAllLines(Program.path + timbukFile);

            Dictionary<int, Tuple<int, int, int, int,int>> statesToRes = new Dictionary<int, Tuple<int, int, int,int, int>>();
            Dictionary<int, Tuple<int, int, int, int,int>> transToRes = new Dictionary<int, Tuple<int, int, int,int, int>>();
            Dictionary<int, Tuple<int, int, int, int,int>> compTransToRes = new Dictionary<int, Tuple<int, int,int, int, int>>();
            Dictionary<int, Tuple<int, int, int, int,int>> minTransToRes = new Dictionary<int, Tuple<int, int,int, int, int>>();

            foreach (string line in lines)
            {
                if (!line.Contains("State"))
                {

                    var spl = line.Split(',');
                    var dmTime=0;
                    foreach (string dmline in dmlines)
                    {
                        if(dmline.StartsWith(spl[0]+",")){
                            var spl1 = dmline.Split(',');
                            dmTime=int.Parse(spl1[4])+1;
                        }
                    }

                    if (spl.Length > 1)
                    {
                        var stC = Int32.Parse(spl[1]);
                        var trC = Int32.Parse(spl[2]);
                        var trCompC = Int32.Parse(spl[3]);

                        int trMinC = -1;
                        if (!spl[5].Contains("NA"))
                            trMinC = Int32.Parse(spl[5]);

                        //To avoid 0 ms just add 1 ms to everything
                        var timeAlg1 = Int32.Parse(spl[6]) + 1;
                        var timeAlg2 = Int32.Parse(spl[7]) + 1;
                        var timeAlgSFA = Int32.Parse(spl[8]) + 1;

                        statesToRes[stC] = update(!statesToRes.ContainsKey(stC) ? null : statesToRes[stC],
                          timeAlg1, timeAlg2, timeAlgSFA,dmTime);
                        transToRes[trC] = update(!transToRes.ContainsKey(trC) ? null : transToRes[trC], timeAlg1, timeAlg2, timeAlgSFA,dmTime);
                        if (trCompC > 0)
                            compTransToRes[trCompC] = update(!compTransToRes.ContainsKey(trCompC) ? null : compTransToRes[trCompC], timeAlg1, timeAlg2, timeAlgSFA,dmTime);
                        if (trMinC >= 0)
                            minTransToRes[trMinC] = update(!minTransToRes.ContainsKey(trMinC) ? null : minTransToRes[trMinC], timeAlg1, timeAlg2, timeAlgSFA,dmTime);
                    }
                }
            }
            //States
            WriteToFile(statesToRes, outputPrefix, "States.txt");
            WriteToFile(transToRes, outputPrefix, "Trans.txt");
            WriteToFile(compTransToRes, outputPrefix, "CompTrans.txt");
            WriteToFile(minTransToRes, outputPrefix, "MinTrans.txt");
        }

        public static void WriteToFile(Dictionary<int, Tuple<int, int, int, int>> dic,
            string prefix, string suffix)
        {
            using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(Program.path + prefix + suffix, false))
            {
                var sortedKeys = new List<int>(dic.Keys);
                sortedKeys.Sort();
                foreach (int key in sortedKeys)
                {
                    if (key >= 0)
                    {
                        var tup = dic[key];

                        var howMany = (double)tup.Item1;
                        var timeAlg1 = ((double)tup.Item2) / howMany;
                        var timeAlg2 = ((double)tup.Item3) / howMany;
                        var timeAlgSFA = ((double)tup.Item4) / howMany;

                        outfile.WriteLine("{0}, {1}, {2}, {3}",
                        key, timeAlg1, timeAlg2, timeAlgSFA);
                    }
                }

            }
        }


        public static Tuple<int, int, int, int> update(Tuple<int, int, int, int> t,
            int v1, int v2, int v3)
        {
            if (t == null)
                return new Tuple<int, int, int, int>(1, v1, v2, v3);
            return new Tuple<int, int, int, int>(t.Item1 + 1, t.Item2 + v1, t.Item3 + v2, t.Item4 + v3);
        }

        public static void WriteToFile(Dictionary<int, Tuple<int, int, int, int, int>> dic,
            string prefix, string suffix)
        {
            using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(Program.path + prefix + suffix, false))
            {
                var sortedKeys = new List<int>(dic.Keys);
                sortedKeys.Sort();
                foreach (int key in sortedKeys)
                {
                    if (key >= 0)
                    {
                        var tup = dic[key];

                        var howMany = (double)tup.Item1;
                        var timeAlg1 = ((double)tup.Item2) / howMany;
                        var timeAlg2 = ((double)tup.Item3) / howMany;
                        var timeAlgSFA = ((double)tup.Item4) / howMany;
                        var dmTime = ((double)tup.Item5) / howMany;

                        outfile.WriteLine("{0}, {1}, {2}, {3}, {4}",
                        key, timeAlg1, timeAlg2, timeAlgSFA, dmTime);
                    }
                }

            }
        }


        public static Tuple<int, int, int, int, int> update(Tuple<int, int, int, int, int> t, 
            int v1, int v2, int v3, int v4){
                if (t == null)
                    return new Tuple<int, int, int, int, int>(1, v1, v2, v3,v4);
                return new Tuple<int, int, int, int, int>(t.Item1 + 1, t.Item2 + v1, t.Item3 + v2, t.Item4 + v3, t.Item5 + v4);
        }

    }
}
