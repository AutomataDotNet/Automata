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
            //MSOPopl14.RunPOPLTests();
            
            //LTLMSO.RunM2LSTR();
            //LTLMSO.RunWS1S();

            LargeMinterm.Run();

            //GenRandomMSO.Run();
        }

    }
}

