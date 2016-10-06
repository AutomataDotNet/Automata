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
    static class Run
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            if (args.Length >= 1 && args[0] == "-h")
            {
                Console.WriteLine(
                    "MSO.Eval.exe -> runs all experiments\n" +
                    "MSO.Eval.exe 7.1 \n" +
                    "runs all experiments in sec 7.1(similarly for 7.2, 7.3)\n" +
                    "\n" +
                     "Further options\n" +
                    "MSO.Eval.exe 7.1 t(runs 1st part of Table 2)\n" +
                    "MSO.Eval.exe 7.1 ws1s(runs 2nd part of Table 2)\n" +
                    "MSO.Eval.exe 7.1 ltl(runs 3rd part of Table 2)\n" +
                    "                    \n" +
                    "Each experiments produces a print on screen as well as a file with the results.\n");
                return;
            }

            if (args.Length == 0 || args[0] == "7.1")
            {
                if (args.Length <= 1 || args[1] == "t")
                {
                    Console.WriteLine("t1,t2,t3,t4 in Figure 6");
                    MSOPopl14.RunPOPLTests();
                }
                if (args.Length <= 1 || args[1] == "ws1s")
                {
                    Console.WriteLine("horn-sub to set-closed in Figure 6");
                    LTLMSO.RunWS1S();
                }
                if (args.Length <= 1 || args[1] == "ltl")
                {
                    Console.WriteLine("LTL in Figure 6");
                    LTLMSO.RunM2LSTR();
                }
            }

            if (args.Length == 0 || args[0] == "7.2")
            {
                Console.WriteLine("f1, f2 in Figure 7");
                LargeMinterm.Run();
            }

            if (args.Length == 0 || args[0] == "7.3")
            {
                Console.WriteLine("Figure 8");
                GenRandomMSO.Run();
            }
        }

    }
}

