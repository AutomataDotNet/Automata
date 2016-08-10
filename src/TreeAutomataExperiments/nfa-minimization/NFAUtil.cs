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

        //Runs the three algorithms. It requires the deterministic automton, the total automaton, and the output file
        public static void RunAllAlgorithms(Automaton<BDD> automaton, 
            string exampleName, string outputFileName, RexEngine rex)
        {
            Automaton<BDD> algo1Min = null;
            Automaton<BDD> algo2Min = null;

            var noEps = automaton.RemoveEpsilons().MakeTotal();

            if (noEps == null)
                return;

            // Quardatic algorithm
            int time = System.Environment.TickCount;
            for (int i = 0; i < Program.numTests; i++)
                algo1Min = noEps.NonDetGetMinAut(true);
              
            var timeAlgo1 = (System.Environment.TickCount - time) / Program.numTests;

            // Logarithmic algorithm
            time = System.Environment.TickCount;
            for (int i = 0; i < Program.numTests; i++)
                algo2Min = noEps.NonDetGetMinAut(false); 
            
            var timeAlgo2 = (System.Environment.TickCount - time) / Program.numTests;

            
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
                    timeAlgo1.ToString(),
                    timeAlgo2.ToString());
            }

        }

    }
}
