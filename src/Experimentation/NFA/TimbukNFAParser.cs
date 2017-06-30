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
using Microsoft.Automata.BooleanAlgebras;


namespace RunExperiments
{
    class TimbukNFAParser
    {
        public static Automaton<BDD> ParseVataFile(string pathFN, CharSetSolver solver)
        {
            string text = System.IO.File.ReadAllText(pathFN);
            return ParseVataFormat(text, solver);
        }

        public static Tuple<Automaton<UIntW>, FiniteSetAlgebra<string>> 
            ParseVataFileFinSet(string pathFN)
        {
            string text = System.IO.File.ReadAllText(pathFN);
            var algebra = FindSetAlgebra(text);
            var aut = ParseVataFormatFinSet(text,algebra);

            return new Tuple<Automaton<UIntW>, FiniteSetAlgebra<string>>(
                aut,algebra
                );
        }

        public static Automaton<BDD> ParseVataFormat(string vataString, CharSetSolver solver)
        {
            var lines = vataString.Split('\r', '\n');

            HashSet<int> finStates = new HashSet<int>();
            var rules = new List<Move<BDD>>();

            Dictionary<string, int> stateNames = new Dictionary<string, int>();
            Dictionary<string, char> constructorNames = new Dictionary<string, char>();            

            bool transitionsStarted = false;
            var initialStates = new HashSet<int>();

            foreach (var line in lines)
            {
                if (!transitionsStarted)
                {
                    if (line.StartsWith("Ops"))
                    {
                        var constructors = line.Split(' ');

                        foreach (var constructor in constructors)
                        {
                            var sp = constructor.Split(':');
                            if (sp.Length > 1)
                                if (!constructorNames.ContainsKey(sp[0]))
                                    constructorNames[sp[0]]= Convert.ToChar(constructorNames.Count);
                        }
                        if (constructorNames.Count == 0)
                            return null;
                    }

                    if (line.StartsWith("Final"))
                    {
                        var sp = line.Split(' ');
                        for (int i = 2; i < sp.Length; i++)
                        {
                            if (sp[i].Length > 0)
                                finStates.Add(GetState(sp[i], stateNames));
                        }
                    }
                    if (line.StartsWith("Transit"))
                    {
                        transitionsStarted = true;
                    }
                }
                else
                {
                    var sp = line.Split('-', '>');
                    if (sp.Length > 1)
                    {
                        var pieces = sp[0].Split('(', ',', ')', ' ');
                        var constructor = pieces[0];
                        List<int> from = new List<int>();
                        for (int i = 1; i < pieces.Length - 1; i++)
                            if (pieces[i].Length > 0)
                                from.Add(GetState(pieces[i], stateNames));

                        

                        var to = GetState(sp[sp.Length - 1], stateNames);

                        if(from.Count==0){
                            initialStates.Add(to);
                        }
                        else{
                            if (from.Count == 1)
                            {
                                var pred = solver.MkCharConstraint(constructorNames[constructor]);
                                var move = new Move<BDD>(from[0], to, pred);
                                rules.Add(move);
                            }
                            else
                            {
                                throw new Exception("tree automaton not NFA");
                            }
                        }
                    }

                }
            }            
            if (initialStates.Count > 1)
            {
                int specialState = 100000;
                foreach (var st in initialStates)
                {
                    rules.Add(new Move<BDD>(specialState, st, null));
                }
                return Automaton<BDD>.Create(solver, specialState, finStates, rules).RemoveEpsilonLoops();
            }

            return Automaton<BDD>.Create(solver, new List<int>(initialStates)[0], finStates, rules).RemoveEpsilonLoops();
        }

        public static Automaton<UIntW> 
            ParseVataFormatFinSet(string vataString, FiniteSetAlgebra<string> solver)
        {
            var lines = vataString.Split('\r', '\n');

            HashSet<int> finStates = new HashSet<int>();
            var rules = new List<Move<UIntW>>();

            Dictionary<string, int> stateNames = new Dictionary<string, int>();

            bool transitionsStarted = false;
            var initialStates = new HashSet<int>();

            foreach (var line in lines)
            {
                if (!transitionsStarted)
                {                   
                    if (line.StartsWith("Final"))
                    {
                        var sp = line.Split(' ');
                        for (int i = 2; i < sp.Length; i++)
                        {
                            if (sp[i].Length > 0)
                                finStates.Add(GetState(sp[i], stateNames));
                        }
                    }
                    if (line.StartsWith("Transit"))
                    {
                        transitionsStarted = true;
                    }
                }
                else
                {
                    var sp = line.Split('-', '>');
                    if (sp.Length > 1)
                    {
                        var pieces = sp[0].Split('(', ',', ')', ' ');
                        var constructor = pieces[0];
                        List<int> from = new List<int>();
                        for (int i = 1; i < pieces.Length - 1; i++)
                            if (pieces[i].Length > 0)
                                from.Add(GetState(pieces[i], stateNames));



                        var to = GetState(sp[sp.Length - 1], stateNames);

                        if (from.Count == 0)
                        {
                            initialStates.Add(to);
                        }
                        else
                        {
                            if (from.Count == 1)
                            {
                                var pred = solver.MkAtom(constructor);
                                var move = new Move<UIntW>(from[0], to, pred);
                                rules.Add(move);
                            }
                            else
                            {
                                throw new Exception("tree automaton not NFA");
                            }
                        }
                    }

                }
            }
            if (initialStates.Count > 1)
            {
                int specialState = 100000;
                foreach (var st in initialStates)
                {
                    rules.Add(new Move<UIntW>(specialState, st, null));
                }
                return Automaton<UIntW>.Create(solver, specialState, finStates, rules).RemoveEpsilonLoops();
            }

            return Automaton<UIntW>.Create(solver, new List<int>(initialStates)[0], finStates, rules).RemoveEpsilonLoops();
        }

        public static FiniteSetAlgebra<string> 
            FindSetAlgebra(string vataString)
        {
            var lines = vataString.Split('\r', '\n');

            HashSet<string> constructorNames = new HashSet<string>();

            bool transitionsStarted = false;
            var initialStates = new HashSet<int>();

            foreach (var line in lines)
            {
                if (!transitionsStarted)
                {
                    if (line.StartsWith("Ops"))
                    {
                        var constructors = line.Split(' ');

                        foreach (var constructor in constructors)
                        {
                            var sp = constructor.Split(':');
                            if (sp.Length > 1 && sp[1]!="0")
                            {
                                constructorNames.Add(sp[0]);
                            }
                        }
                        if (constructorNames.Count == 0)
                            return null;
                    }

                    if (line.StartsWith("Transit"))
                    {
                        break;
                    }
                }
            }

            return new FiniteSetAlgebra<string>(constructorNames);
        }

        public static int GetState(string st, Dictionary<string, int> names)
        {
            var n = st.Trim();
            if (names.ContainsKey(n))
                return names[n];

            names[n] = names.Count;
            return names[n];
        }

    }
}
