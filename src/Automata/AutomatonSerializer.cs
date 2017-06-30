using System;

namespace Microsoft.Automata
{
    /// <summary>
    /// Is used as a base class of automata classes that implement the IAutomaton interface
    /// and the INameProvider interface.
    /// </summary>
    public class AutomatonSerializer<TERM>
    {
        IAutomaton<TERM> __aut { get { return (IAutomaton<TERM>)this; } }
        string __name { get { return ((INameProvider)this).Name; } }
        public AutomatonSerializer()
        {
        }

        /// <summary>
        /// Saves the automaton in <paramref name="Name"/>.dgml file in the working directory
        /// and opens the file in a new process.
        /// </summary>
        public void ShowGraph()
        {
            Microsoft.Automata.DirectedGraphs.DgmlWriter.ShowGraph<TERM>(-1, __aut, __name);
        }

        /// <summary>
        /// Saves the automaton in dgml format in <paramref name="Name"/>.dgml file in the working directory.
        /// </summary>
        public void SaveAsDgml()
        {
            Microsoft.Automata.DirectedGraphs.DgmlWriter.AutomatonToDgml<TERM>(-1, __aut, __name);
        }

        /// <summary>
        /// Saves the automaton in dot format in <paramref name="Name"/>.dot file in the working directory.
        /// </summary>
        public void SaveAsDot()
        {
            Microsoft.Automata.DirectedGraphs.DotWriter.AutomatonToDot<TERM>(__aut.DescribeLabel, __aut, __name, __name, DirectedGraphs.DotWriter.RANKDIR.LR, 12, true);
        }

        /// <summary>
        /// Saves the automaton in dot format in the given file in the working directory.
        /// </summary>
        public void SaveAsDot(string file)
        {
            Microsoft.Automata.DirectedGraphs.DotWriter.AutomatonToDot<TERM>(__aut.DescribeLabel, __aut, __name, file, DirectedGraphs.DotWriter.RANKDIR.LR, 12, true);
        }

        /// <summary>
        /// Saves the automaton in dgml format in tw.
        /// </summary>
        public void SaveAsDgml(System.IO.TextWriter tw)
        {
            Microsoft.Automata.DirectedGraphs.DgmlWriter.AutomatonToDgml<TERM>(-1,__aut,__name, tw);
        }

        /// <summary>
        /// Saves the automaton in dot format in tw.
        /// </summary>
        public void SaveAsDot(System.IO.TextWriter tw)
        {
            Microsoft.Automata.DirectedGraphs.DotWriter.AutomatonToDot<TERM>(__aut.DescribeLabel, __aut, __name, tw, DirectedGraphs.DotWriter.RANKDIR.TB, 12, true);
        }

        public virtual void ShowGraph(int k)
        {
            Microsoft.Automata.DirectedGraphs.DgmlWriter.ShowGraph<TERM>(k, __aut, __name);
        }

        public void SaveAsDgml(int k)
        {
            Microsoft.Automata.DirectedGraphs.DgmlWriter.AutomatonToDgml<TERM>(k, __aut, __name);
        }
    }
}
