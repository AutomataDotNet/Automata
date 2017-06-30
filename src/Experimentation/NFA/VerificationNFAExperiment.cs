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
    class VerificationNFAExperiment
    {
        static int startAt = 0;
        static int endAt = 1000;

        public static void RunTest()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);  //new solver using ASCII encoding
            var nfaPath = Program.path + @"NFA\";
            DirectoryInfo detDir = new DirectoryInfo(nfaPath);

            if (startAt == 0)
                NFAUtil.PrintHeader(Program.verifNFAOutputFile);                

            int count = 0;
            foreach (var file in detDir.GetFiles("*", SearchOption.AllDirectories))
            {
                if (startAt <= count && count <= endAt)
                {
                    Automaton<BDD> detAut = TimbukNFAParser.ParseVataFile(file.FullName, solver);
                    NFAUtil.RunAllAlgorithms(detAut, file.Name, Program.verifNFAOutputFile, solver);

                }
                count++;
            }

        }

        public static void RunFinAlphTest()
        {
            var nfaPath = Program.path + @"NFA\";
            DirectoryInfo detDir = new DirectoryInfo(nfaPath);

            if (startAt == 0)
                NFAUtil.PrintHeader(Program.verifNFAOutputFile);

            int count = 0;
            foreach (var file in detDir.GetFiles("*", SearchOption.AllDirectories))
            {
                if (startAt <= count && count <= endAt)
                {
                    var res = TimbukNFAParser.ParseVataFileFinSet(file.FullName);
                    NFAUtil.RunAllAlgorithms(res.Item1, file.Name, Program.verifNFAOutputFile, res.Item2);

                }
                count++;
            }

        }        
    }
}
