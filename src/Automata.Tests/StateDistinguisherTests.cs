using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata.Grammars;
using Microsoft.Automata;
using System.Collections.Generic;

namespace Automata.Tests
{
    [TestClass]
    public class StateDistinguisherTests
    {
        [TestMethod]
        public void TestStateDistinguisher()
        {
            var solver = ContextFreeGrammar.GetContext();
            var regex = "a";
            var A = solver.Convert("A.*A").Determinize().RemoveEpsilons();
            var B = solver.Convert("B.*B.*B").Determinize().RemoveEpsilons();
            var AB = A.Intersect(B);
            var states = new List<int>(AB.GetStates());
            var dist = new StateDistinguisher<BDD>(AB, bdd => (char)bdd.GetMin());
            var witnesses = new HashSet<string>();
            var distinguishableStates = new HashSet<Tuple<int, int>>();
            foreach (var p in AB.States)
                foreach (var q in AB.States)
                {
                    string w;
                    if (dist.AreDistinguishable(p, q, out w))
                    {
                        witnesses.Add(w);
                    }
                }
            //check that witnesses is a suffix closed set
            foreach (var w in witnesses)
                for (int i = 0; i < w.Length; i++)
                    Assert.IsTrue(witnesses.Contains(w.Substring(i)));
            //check that the states in the minimized automaton are all distiguishable 
            string _;
            var ABMin = AB.Minimize();
            Assert.IsTrue(ABMin.StateCount < AB.StateCount);
            //here we know that the minimization algorithm keeps the same state ids
            foreach (var p in ABMin.States)
                foreach (var q in ABMin.States)
                    if (p != q)
                    {
                        Assert.IsTrue(dist.AreDistinguishable(p, q, out _));
                    }
        }

    }
}
