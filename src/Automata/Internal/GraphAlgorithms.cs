using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Automata
{
    internal class GraphAlgorithms
    {
        /// <summary>
        /// Computes the strongly connected components of the automaton
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="automaton"></param>
        /// <param name="solver"></param>
        /// <returns></returns>
        public static IEnumerable<HashSet<int>> GetStronglyConnectedComponents<T>(Automaton<T> automaton)
        {
            var output = new List<HashSet<int>>();
            Tuple<List<int>, List<int>> dfs = Dfs(automaton.InitialState,automaton);
            List<int> startTimes = dfs.Item1;
            List<int> endTimes = dfs.Item2;
            endTimes.Reverse();

            HashSet<int> visited = new HashSet<int>();
            foreach(var v in endTimes){
                if(!visited.Contains(v)){
                    var start = new List<int>();
                    var end = new List<int>();
                    Dfs(v,automaton,visited,start,end, true);
                    var scc = new HashSet<int>(start);
                    foreach (var s in start)
                        visited.Add(s);
                    output.Add(scc);
                }
            }
            return output;
        }

        //Updates start times and end times
        static Tuple<List<int>, List<int>> Dfs<T>(int startState, Automaton<T> automaton, bool reverse=false)
        {
            List<int> startTimes = new List<int>();
            List<int> endTimes = new List<int>();
            Dfs(startState, automaton, new HashSet<int>(), startTimes, endTimes,reverse);
            return new Tuple<List<int>, List<int>>(startTimes, endTimes);
        }

        //Updates start times and end times
        static void Dfs<T>(int curr, Automaton<T> automaton, HashSet<int> visited, List<int> startTimes, List<int> endTimes, bool reverse = false)
        {
            if (!visited.Contains(curr))
            {
                visited.Add(curr);
                startTimes.Add(curr);
                if (reverse)
                    foreach (var move in automaton.GetMovesTo(curr))
                        Dfs(move.SourceState, automaton, visited, startTimes, endTimes, reverse);
                else
                    foreach (var move in automaton.GetMovesFrom(curr))
                        Dfs(move.TargetState, automaton, visited, startTimes, endTimes, reverse);
                endTimes.Add(curr);
            }
        }

    }
}
