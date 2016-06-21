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
        public static void Main()
        {
            MSOPopl14.RunPOPLTests();

            //GenRandomMSO.Run();

            //string outF = @"..\ws1stest.txt";
            //string inpD = @"C:\github\automatark\ws1s\";

            ////LTLTest(inpD,outF);

            //outF = @"..\ltltest.txt";
            //inpD = @"C:\github\automatark\m2l-str\LTL-finite\random\";
            //LTLTest(inpD, outF);


            //POPLTestsNew();
            //POPLTestsOld();
            //POPLTestsNewSolver2();
            //POPLTestsInt();

            //MintermTest();

            //Console.Read();
        }
        
    }
}

