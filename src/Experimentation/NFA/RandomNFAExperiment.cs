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
    class RandomNFAExperiment
    {
        static int startAt=0;
        static int endAt = 10;

        public static void RunTest()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);  //new solver using ASCII encoding
            

            
        }

        public static Automaton<BDD> fadoToSFA (string description, CharSetSolver solver){

            var finalStates = new HashSet<int>();
            var moves = new List<Move<BDD>>();

            moves.Add(new Move<BDD>(0,1,solver.MkCharConstraint('a')));



            return Automaton<BDD>.Create(solver, 0, finalStates, moves).RemoveEpsilonLoops(); //This causes normalization
        }

    }
}
