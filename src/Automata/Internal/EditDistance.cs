using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    internal class EditDistance
    {

        /// <summary>
        /// Based on paper
        /// Order-n correction for regular langauges, http://dl.acm.org/citation.cfm?id=360995
        /// </summary>
        /// <param name="str">input string</param>
        /// <param name="automaton">dfa for which you want to compute the distance</param>
        /// <param name="solver">character solver</param>
        /// <param name="distance">outputs the distance</param>
        /// <returns>the closest string to str in automaton</returns>
        public static string GetClosestElement(string str, Automaton<BDD> automaton, CharSetSolver solver, out int distance, bool checkDeterminism = true)
        {
            return GetClosestElement(str, automaton, solver, automaton.StateCount, out distance, checkDeterminism);
        }

        /// <summary>
        /// Based on paper
        /// Order-n correction for regular langauges, http://dl.acm.org/citation.cfm?id=360995
        /// </summary>
        /// <param name="str">input string</param>
        /// <param name="automaton">dfa for which you want to compute the distance</param>
        /// <param name="solver">character solver</param>
        /// <param name="bound">depth of search for max string insertion</param>
        /// <param name="distance">outputs the distance</param>
        /// <returns>the closest string to str in automaton</returns>
        public static string GetClosestElement(string str, Automaton<BDD> automaton, CharSetSolver solver, int bound, out int distance, bool checkDeterminism = true)
        {
            //bound = Math.Min(bound, str.Length);

            var input = str.ToCharArray();
            var chars = new HashSet<char>(input);
            var maxl = input.Length+1;

            if(automaton.IsEmpty)
                throw new AutomataException("automaton must be nonempty");
            if (checkDeterminism && !automaton.IsDeterministic)
                throw new AutomataException("automaton must be deterministic");

            //Compute P(T,S) L(T,S,c)
            var lstates= automaton.States.ToList();
            lstates.Sort();
            var states = lstates.ToArray();
            var stToInd = new Dictionary<int, int>(states.Length+1);
            for (int i = 0; i < states.Length; i++)
                stToInd[states[i]] = i;

            var Pold = new int[states.Length, states.Length];
            var P1 = new bool[states.Length, states.Length]; //Records the transition relation
            var Pnew = new int[states.Length, states.Length];
            var Lold=new Dictionary<char,bool[,]>();
            var Lnew = new Dictionary<char, bool[ ,]>();   
            
            #region Initialize P L
            foreach (var c in chars)
            {
                Lold[c] = new bool[states.Length, states.Length];
                Lnew[c] = new bool[states.Length, states.Length];
            }
            foreach (var stT in automaton.States)
            {
                var T = stToInd[stT];
                foreach (var stS in automaton.States)
                {
                    var S = stToInd[stS];
                    if (T == S)
                    {
                        Pold[S, T] = 0;
                        char wit;
                        P1[S, T] = MoveFromStoT(stS, stT, automaton, solver, out wit);
                        foreach (var c in chars)
                            if (P1[S, T] && MoveFromStoTContainsC(c, stS, stT, automaton, solver))
                                Lold[c][S, T] = true;
                            else
                                Lold[c][S, T] = false;
                    }
                    else
                    {
                        char wit;
                        if (MoveFromStoT(stS, stT, automaton, solver, out wit))
                        {
                            Pold[S, T] = 1;
                            P1[S, T] = true;
                            foreach (var c in chars)
                                if (MoveFromStoTContainsC(c, stS, stT, automaton, solver))
                                    Lold[c][S, T] = true;
                                else
                                    Lold[c][S, T] = false;
                        }
                        else
                        {
                            Pold[S, T] = int.MaxValue;
                            P1[S, T] = false;
                            foreach (var c in chars)
                                Lold[c][S, T] = false;
                        }
                    }
                }
            }
	        #endregion
            //solver.ShowGraph(automaton,"as");

            //Inductive step
            for(int k=1;k<=bound;k++){
                foreach (var stT in automaton.States)
                {                    
                    var T = stToInd[stT];
                    foreach (var stS in automaton.States)
                    {
                        var S = stToInd[stS];

                        if (Pold[S, T] == int.MaxValue)
                        {
                            bool found=false;
                            foreach (var move in automaton.GetMovesFrom(stS))
                            {
                                var stk = move.TargetState;
                                var K = stToInd[stk];
                                if (Pold[K, T] != int.MaxValue)
                                    if (P1[S, K])
                                    {
                                        found = true;
                                        Pnew[S, T] = Pold[K, T] + 1;
                                        foreach (var c in chars)
                                            Lnew[c][S, T] = Lold[c][K, T] || solver.IsSatisfiable(solver.MkAnd(move.Label,solver.MkCharConstraint(c)));
                                    }
                            }
                            if (!found)
                            {
                                Pnew[S, T] = Pold[S, T];
                                foreach (var c in chars)
                                    Lnew[c][S, T] = Lold[c][S, T];
                            }
                        }
                        else
                        {
                            Pnew[S, T] = Pold[S, T];
                            foreach (var c in chars)
                                Lnew[c][S, T] = Lold[c][S, T];
                        }
                    }

                }
                Pold = Pnew;
                Pnew=new int[states.Length, states.Length];
                foreach (var c in chars)
                    Lold[c] = Lnew[c];
                
                Lnew = new Dictionary<char, bool[,]>();
                foreach (var c in chars)
                    Lnew[c] = new bool[states.Length, states.Length];
                
            }

            //Initialize table for value 0
            Tuple<int, int>[,] F = new Tuple<int, int>[maxl, automaton.StateCount];
            foreach (var st in automaton.States)
            {
                var T = stToInd[st];
                if (st == automaton.InitialState)
                    F[0, T] = new Tuple<int, int>(0, -1);
                else
                    F[0, T] = new Tuple<int, int>(int.MaxValue, -1);
            }

            //solver.ShowGraph(automaton,"aa");
            //Dynamic programming loop
            List<int> stateList = new List<int>();
            for (int j = 1; j < maxl; j++)
            {
                var aj = input[j - 1];
                foreach (var stT in automaton.States)
                {
                    var T = stToInd[stT];
                    int min = int.MaxValue;
                    int minSt = -1;                    
                    foreach (var stS in automaton.States)
                    {
                        var S = stToInd[stS]; 

                        var pts = Pold[S, T];
                        if (pts != int.MaxValue)
                        {
                            var ltsc = Lold[aj][S, T] ? 1 : 0;
                            int vts = pts == 0 ? 1 - ltsc : pts - ltsc;
                            var fjm1t = F[j - 1, S];
                            int expr = fjm1t.Item1 + vts;

                            if (fjm1t.Item1 == int.MaxValue || vts == int.MaxValue)
                                expr = int.MaxValue;
                            else
                                if (expr <= min)
                                {
                                    min = expr;
                                    minSt = S;
                                    if (min == 0)
                                        break;
                                }                            
                        }
                    }
                    F[j, T] = new Tuple<int, int>(min, minSt);                    
                }
            }
        
            //Iteration over final states
            int minAcc = int.MaxValue;
            int minState = -1;
            foreach (var st in automaton.GetFinalStates())
            {
                var S = stToInd[st];
                if (F[input.Length, S].Item1 < minAcc)
                {
                    minAcc = F[input.Length, S].Item1;
                    minState = F[input.Length, S].Item2;
                    minState = S;
                }
            }
            var minString ="";
            int curr = minState;
            int strindex = input.Length;
            while (strindex > 0)
            {                
                var f = F[strindex, curr];
                var aj = input[strindex-1];

                var pts = Pold[f.Item2,curr];
                var ltsc = Lold[aj][f.Item2,curr] ? 1 : 0;
                string vts = pts == 0 ? ((ltsc == 1)? aj.ToString():"") : ((ltsc == 1) ? ShortStringStoTwithC(aj, states[f.Item2], states[curr], automaton, bound, solver) : ShortStringStoT(states[f.Item2], states[curr], automaton, bound, solver));

                minString = vts + minString;

                curr = f.Item2;
                strindex--;
            }

            distance=minAcc;
            return minString;
        }

        //check if delta(S,T,c) exists
        static bool MoveFromStoTContainsC(char c, int S, int T, Automaton<BDD> aut, CharSetSolver solver){
            var ccond = solver.MkCharConstraint(c);
            foreach(var move in aut.GetMovesFrom(S))
                if (move.TargetState == T)
                    if (solver.IsSatisfiable(solver.MkAnd(move.Label, ccond)))
                        return true;
            return false;
        }

        //check if delta(S,T,c) exists
        static bool MoveFromStoTContainsC(char c, int S, int T, Automaton<BDD> aut, CharSetSolver solver, out char witness)
        {
            var ccond = solver.MkCharConstraint(c);
            foreach (var move in aut.GetMovesFrom(S))
                if (move.TargetState == T)
                {
                    if (solver.IsSatisfiable(solver.MkAnd(move.Label, ccond)))
                    {
                        witness = c;
                        return true;
                    }
                    else
                        foreach (var w in solver.GenerateAllCharacters(move.Label, false))
                        {
                            witness = w;
                            return true;
                        }
                }
            witness = c;
            return false;
        }

        //check if delta(S,T,c) exists
        static bool MoveFromStoT(int S, int T, Automaton<BDD> aut, CharSetSolver solver, out char witness)
        {
            foreach (var move in aut.GetMovesFrom(S))
                if (move.TargetState == T)
                {
                    foreach(var w in solver.GenerateAllCharacters(move.Label,false)){
                        witness=w;
                        return true;
                    }
                }
            witness = 'a';
            return false;
        }
        
        //check if delta(S,T,c) exists
        static string ShortStringStoTwithC(char c, int S, int T, Automaton<BDD> aut, int limit, CharSetSolver solver)
        {
            var pair = new Tuple<int, int>(S, T);
            if (S == T)
                return "";

            var aut1 = Automaton<BDD>.Create(solver, S, new int[] { T }, aut.GetMoves());
            var autR = solver.Convert(System.Text.RegularExpressions.Regex.Escape(c.ToString()));

            var contst = aut1.Intersect(autR).Determinize().Minimize();
            var finst= contst.GetFinalStates();
            var strings = new Dictionary<int, string>();
            strings[contst.InitialState] = "";
            Dictionary<int,int> dist = new Dictionary<int,int>();
            HashSet<int> visited = new HashSet<int>();
            List<int> toVisit = new List<int>();
            visited.Add(contst.InitialState);
            toVisit.Add(contst.InitialState);
            dist[contst.InitialState] = 0;
            while (toVisit.Count > 0)
            {
                var curr = toVisit[0];
                toVisit.RemoveAt(0);
                if(dist[curr]<=limit)                    
                    foreach (var move in contst.GetMovesFrom(curr))
                        if (!visited.Contains(move.TargetState))
                        {
                            dist[move.TargetState] = dist[move.SourceState] + 1;
                            visited.Add(move.TargetState);
                            toVisit.Add(move.TargetState);
                            char wit='a';
                            foreach(var w in solver.GenerateAllCharacters(move.Label,false)){
                                wit=w;
                                break;
                            }
                            strings[move.TargetState] = strings[move.SourceState] + wit;                        
                            if (finst.Contains(move.TargetState))
                            {
                                return strings[move.TargetState];
                            }
                        }
            }

            throw new AutomataException("this code shouldn't be reachable");
        }

        //check if delta(S,T,c) exists
        static string ShortStringStoT(int S, int T, Automaton<BDD> aut, int limit, CharSetSolver solver)
        {
            if (S == T)
                return "";

            var aut1 = Automaton<BDD>.Create(aut.Algebra, S, new int[] { T }, aut.GetMoves());
            var contst = aut1.Determinize().Minimize();
            var finst = contst.GetFinalStates();
            var strings = new Dictionary<int, string>();
            strings[contst.InitialState] = "";
            Dictionary<int, int> dist = new Dictionary<int, int>();
            HashSet<int> visited = new HashSet<int>();
            List<int> toVisit = new List<int>();
            visited.Add(contst.InitialState);
            toVisit.Add(contst.InitialState);
            dist[contst.InitialState] = 0;
            while (toVisit.Count > 0)
            {
                var curr = toVisit[0];
                toVisit.RemoveAt(0);
                if (dist[curr] <= limit)
                    foreach (var move in contst.GetMovesFrom(curr))
                        if (!visited.Contains(move.TargetState))
                        {
                            dist[move.TargetState] = dist[move.SourceState] + 1;
                            visited.Add(move.TargetState);
                            toVisit.Add(move.TargetState);
                            char wit = 'a';
                            foreach (var w in solver.GenerateAllCharacters(move.Label, false))
                            {
                                wit = w;
                                break;
                            }
                            strings[move.TargetState] = strings[move.SourceState] + wit;
                            if (finst.Contains(move.TargetState))
                                return strings[move.TargetState];                            
                        }
            }

            throw new AutomataException("this code shouldn't be reachable");
        }

        /// <summary>
        /// String edit-distance between str1 and str2
        /// </summary>
        public static int GetEditDistance(string str1, string str2)
        {
            var lev = new int[str1.Length + 1, str2.Length + 1];
            for (int i = 0; i <= str1.Length;  i++)
                for (int j = 0; j <= str2.Length; j++)
                    if (i == 0 || j == 0)
                        lev[i, j] = (int)Math.Max(i, j);
                    else
                        lev[i, j] = (int)Math.Min(Math.Min(lev[i - 1, j] + 1, lev[i, j - 1] + 1), lev[i - 1, j-1] + (str1[i-1]==str2[j-1]?0:1));
            return lev[str1.Length, str2.Length];
        }
    }
}
