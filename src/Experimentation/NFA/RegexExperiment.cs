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
    class RegexExperiment
    {
        static int startAt=0;
        static int endAt = 5000;

        public static void RunTest()
        {

            NFAUtil.PrintHeader(Program.regexOutputFile);

            var lines = File.ReadAllLines(Program.path + Program.regexInputFile);

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            var solver = rex.Solver;
            for (int i = 0; i < Math.Min(lines.Length, endAt); i++)
            {
                if (i >= startAt)
                {
                    string regex = lines[i].Trim();
                    try
                    {
                        var sfa = rex.CreateFromRegexes(regex).RemoveEpsilons().MakeTotal();
                        NFAUtil.RunAllAlgorithms(sfa, i.ToString(), Program.regexOutputFile, solver);
                    }
                    catch (Exception e)
                    {
                        e = e;
                        //Console.WriteLine("Can't parse " + regex);
                        //Console.WriteLine(e);
                    }
                }
            }
        }

    }
}
