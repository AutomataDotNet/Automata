using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Pair = System.Tuple<int, int>;

namespace Microsoft.Automata
{
    /// <summary>
    /// Utility for distinguishing states of a deterministic automaton
    /// </summary>
    public class StateDistinguisher<T>
    {

        Automaton<T> aut;
        IBooleanAlgebra<T> solver;
        Dictionary<Tuple<int, int>, ConsList<T>> distinguisher = new Dictionary<Tuple<int, int>, ConsList<T>>();
        T[] empty = new T[] { };

        Func<int, int, Pair> MkPair = (x, y) => (x <= y ? new Pair(x, y) : new Pair(y, x));

        Func<T, char> selectCharacter;

        internal StateDistinguisher(Automaton<T> aut, Func<T, char> selectCharacter = null)
        {
            this.aut = aut;
            this.solver = aut.Algebra;
            this.selectCharacter = selectCharacter;
            ComputeDistinguisher();
        }

        /// <summary>
        /// Based on MinimizeMoore method.
        /// </summary>
        void ComputeDistinguisher()
        {
            var fa = aut;

            //fa = fa.MakeTotal();

            
           
            var thisLayer = new SimpleStack<Tuple<int, int>>();
            var nextLayer = new SimpleStack<Tuple<int, int>>();

            //any pair of states (p,q) where one state is final and the other is not are distinguished by 
            //the empty list represented by null, this set of distinguishing sequences is suffix-closed
            foreach (var p in fa.GetStates())
                if (!fa.IsFinalState(p))
                    foreach (var q in fa.GetFinalStates())
                    {
                        var pair = MkPair(p, q);
                        if (!distinguisher.ContainsKey(pair))
                        {
                            distinguisher[pair] = null;
                            nextLayer.Push(pair);
                        }
                    }

            //if a target pair of states is distinguished by some sequence
            //then any pair entering those states is also distinguished by extending that sequence
            //work breadth-first to maintain shortest witnesses
            while (nextLayer.IsNonempty)
            {
                thisLayer = nextLayer;
                nextLayer = new SimpleStack<Tuple<int, int>>();
                while (thisLayer.IsNonempty)
                {
                    var targetpair = thisLayer.Pop();
                    foreach (var m1 in fa.GetMovesTo(targetpair.Item1))
                        foreach (var m2 in fa.GetMovesTo(targetpair.Item2))
                            if (m1.SourceState != m2.SourceState)
                            {
                                var overlap = solver.MkAnd(m1.Label, m2.Label);
                                if (solver.IsSatisfiable(overlap))
                                {
                                    var sourcepair = MkPair(m1.SourceState, m2.SourceState);
                                    if (!distinguisher.ContainsKey(sourcepair))
                                    {
                                        //add a new distinguishing sequence for the source pair
                                        //it extends a sequence of the target pair
                                        //thus the sequences remain suffix-closed
                                        distinguisher[sourcepair] = new ConsList<T>(overlap, distinguisher[targetpair]);
                                        nextLayer.Push(sourcepair);
                                    }
                                }
                            }
                }
            }
        }

        /// <summary>
        /// Output distinguishability witness for p and q if p and q are distinguishable.
        /// </summary>
        public bool AreDistinguishable(int p, int q, out T[] witness)
        {
            var pq = MkPair(p, q);
            ConsList<T> w;
            if (distinguisher.TryGetValue(pq, out w))
            {
                if (w == null)
                    witness = empty;
                else
                    witness = w.ToArray();
                return true;
            }
            else
            {
                witness = null;
                return false;
            }
        }

        /// <summary>
        /// Output concrete distinguishability witness for p and q if p and q are distinguishable.
        /// Not supported if selectCharacter function has not been provided.
        /// </summary>
        public bool AreDistinguishable(int p, int q, out string witness)
        {
            if (selectCharacter == null)
                throw new AutomataException(AutomataExceptionKind.NotSupported);

            var pq = MkPair(p, q);
            ConsList<T> w;
            if (distinguisher.TryGetValue(pq, out w))
            {
                if (w == null)
                    witness = "";
                else
                    witness = new string(Array.ConvertAll(w.ToArray(), x => selectCharacter(x)));
                return true;
            }
            else
            {
                witness = null;
                return false;
            }
        }
    }
}
