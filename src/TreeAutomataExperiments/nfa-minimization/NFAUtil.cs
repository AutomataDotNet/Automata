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
using Microsoft.Automata.Rex;

namespace RunExperiments
{
    class NFAUtil
    {

        static double GetAvgTime(long t, long max)
        {
            return (double)(t-max) / (double)(Program.numTests-1);
        }

        //Runs the three algorithms. It requires the deterministic automton, the total automaton, and the output file
        public static void RunAllAlgorithms(Automaton<BDD> automaton, 
            string exampleName, string outputFileName, CharSetSolver rex)
        {
            Automaton<BDD> algo1Min = null;
            Automaton<BDD> algo2Min = null;

            var noEps = automaton.RemoveEpsilons().MakeTotal();
            noEps.isDeterministic = false;

            if (noEps == null)
                return;

            // Quardatic algorithm
            int time = System.Environment.TickCount;
            int maxTime = 0;
            for (int i = 0; i < Program.numTests; i++)
            {
                int tLoc = System.Environment.TickCount;
                algo1Min = noEps.NonDetGetMinAut(true);
                maxTime = Math.Max(System.Environment.TickCount - tLoc, maxTime);

                //Console.WriteLine(System.Environment.TickCount - tLoc);
            }

            var timeAlgo1 = GetAvgTime(System.Environment.TickCount - time,maxTime);

            // Logarithmic algorithm
            time = System.Environment.TickCount;
            maxTime = 0;
            for (int i = 0; i < Program.numTests; i++)
            {
                int tLoc = System.Environment.TickCount;
                algo2Min = noEps.NonDetGetMinAut(false);
                maxTime = Math.Max(System.Environment.TickCount - tLoc, maxTime);

                //Console.WriteLine(System.Environment.TickCount - tLoc);
            }

            var timeAlgo2 = GetAvgTime(System.Environment.TickCount - time, maxTime);

            
            //Check that results are correct
            if (algo1Min != null && algo2Min != null)
            {
                //bool eq = rex.AreEquivalent(algo1Min, algo2Min);
                //eq = eq && rex.AreEquivalent(automaton, algo2Min);
                //if(!eq)
                //    Console.WriteLine("wrong mini");

                if (algo2Min.StateCount!= algo1Min.StateCount)
                    Console.WriteLine("size differ by " + (algo2Min.StateCount - algo1Min.StateCount));
                
            }

            // Append results on file 
            // ID, StateCount, RuleCount, CompleteRuleCount, MinStateCount, MinRuleCount, PTime, CTime
            using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(Program.path + outputFileName, true))
            {
                outfile.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}",
                    exampleName,
                    noEps.StateCount,
                    noEps.MoveCount,
                    algo1Min.StateCount,
                    algo1Min.MoveCount,
                    timeAlgo1+0.1,
                    timeAlgo2+0.1);
            }

        }

    }
}
