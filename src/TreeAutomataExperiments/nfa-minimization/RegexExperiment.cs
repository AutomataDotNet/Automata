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
using Microsoft.Automata.Tests;
using Microsoft.Z3;
using System.Diagnostics;

namespace RunExperiments
{
    class RegexExperiment
    {
        static int startAt=0;

        public static void RunTest()
        {
            using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(Program.path + Program.regexOutputFile, false))
            {
                outfile.WriteLine("ID, StateCount, RuleCount, MinStateCount, MinRuleCount, quadratic, n log n");
            }

            var lines = File.ReadAllLines(Program.path + Program.regexInputFile);

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i >= startAt)
                {
                    string regex = lines[i].Trim();
                    try
                    {
                        var sfa = rex.CreateFromRegexes(regex);
                        NFAUtil.RunAllAlgorithms(sfa, i.ToString(), Program.regexOutputFile, rex);
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine("Can't parse " + regex);
                        //Console.WriteLine(e);
                    }
                }
            }


        }

    }
}
