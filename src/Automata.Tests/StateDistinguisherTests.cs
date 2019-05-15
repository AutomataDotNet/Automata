using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata.Grammars;
using Microsoft.Automata;
using System.Collections.Generic;
using System.IO;

namespace Automata.Tests
{
    [TestClass]
    public class StateDistinguisherTests
    {
        static string regexesFile = "../../../samples/regexes.txt";

        [TestMethod]
        public void TestStateDistinguisher()
        {
            var solver = ContextFreeGrammar.GetContext();
            var A = solver.Convert("A.*A").Determinize().RemoveEpsilons();
            var B = solver.Convert("B.*B.*B").Determinize().RemoveEpsilons();
            var AB = A.Intersect(B);
            var states = new List<int>(AB.GetStates());
            var dist = new StateDistinguisher_Moore<BDD>(AB, bdd => (char)bdd.GetMin());
            //check suffix-closedness
            foreach (var s in dist.GetAllDistinguishingStrings())
                for (int i = 0; i < s.Length; i++)
                    Assert.IsTrue(dist.IsDistinguishingString(s.Substring(i)));
            //check that the states in the minimized automaton are all distiguishable 
            var ABMin = AB.Minimize();
            //here we know that the minimization algorithm keeps the same state ids
            Func<int, int, Tuple<int,int>> MkPair = (x, y) => (x <= y ? new Tuple<int, int>(x, y) : new Tuple<int, int>(y, x));
            foreach (var p in ABMin.States)
                foreach (var q in ABMin.States)
                    if (p != q)
                    {
                        string s;
                        Assert.IsTrue(dist.AreDistinguishable(p, q, out s));
                    }
        }

        [TestMethod]
        public void TestStateDistinguisherH()
        {
            var solver = new CharSetSolver();
            var A = solver.Convert("A").Determinize().RemoveEpsilons();
            var B = solver.Convert("B.*B").Determinize().RemoveEpsilons();
            var AB = A.Intersect(B);
            var states = new List<int>(AB.GetStates());
            var dist = new StateDistinguisher<BDD>(AB);
            //check suffix-closedness
            var ABMin = AB.Minimize();
            var diststr = dist.GetAllDistinguishingSequences();
            //here we know that the minimization algorithm keeps the same state ids
            Func<int, int, Tuple<int, int>> MkPair = (x, y) => (x <= y ? new Tuple<int, int>(x, y) : new Tuple<int, int>(y, x));
            foreach (var p in ABMin.States)
                foreach (var q in ABMin.States)
                    if (p != q)
                    {
                        var d = dist.GetDistinguishingSequence(p, q);
                        Assert.IsTrue(d != null);
                        //check that d distinguishes p from q
                        var p1 = p;
                        var q1 = q;
                        for (int i = 0; i < d.Length; i++)
                        {
                            p1 = AB.GetTargetState(p1, d[i]);
                            q1 = AB.GetTargetState(q1, d[i]);
                        }
                        Assert.IsTrue(AB.IsFinalState(p1) != AB.IsFinalState(q1));
                    }
        }

        [TestMethod]
        public void TestStateDistinguisher_RandomCharSelector()
        {
            var solver = new CharSetSolver();
            var A = solver.Convert("[A-Z].*[A-Z]").Determinize().RemoveEpsilons();
            var B = solver.Convert("[0-9].*[0-9].*[0-9]").Determinize().RemoveEpsilons();
            var AB = A.Intersect(B);
            var states = new List<int>(AB.GetStates());
            var dist = new StateDistinguisher_Moore<BDD>(AB, solver.ChooseUniformly);
            //check that the states in the minimized automaton are all distiguishable 
            var ABMin = AB.Minimize();
            //here we know that the minimization algorithm keeps the same state ids
            foreach (var p in ABMin.States)
                foreach (var q in ABMin.States)
                    if (p != q)
                    {
                        string w;
                        Assert.IsTrue(dist.AreDistinguishable(p, q, out w));
                        var w_preds = Array.ConvertAll(w.ToCharArray(), c => solver.MkCharConstraint(c));
                        var p1 = ABMin.GetTargetState(p, w_preds);
                        var q1 = ABMin.GetTargetState(q, w_preds);
                        Assert.IsTrue(ABMin.IsFinalState(p1) != ABMin.IsFinalState(q1));
                    }
        }

        [TestMethod]
        public void TestStateDistinguisher_OnSampleRegexes()
        {
            string[] regexes = File.ReadAllLines(regexesFile);
            int K = 50; //how many regexes
            var dfas = new List<Automaton<BDD>>();
            int timeoutcount = 0;
            int k = 0;
            while (dfas.Count < K)
            {
                var solver = new CharSetSolver();
                var ascii = solver.Convert("^[\0-\x7F]*$");
                var nfa = solver.Convert(regexes[k++]).RemoveEpsilons().Intersect(ascii);
                try
                {
                    var dfa = nfa.Determinize(1000);
                    dfas.Add(dfa);
                }
                catch (TimeoutException)
                {
                    timeoutcount += 1;
                }
            }

            int totalMininmalStateCount = 0;
            int totalDistinguisherCount = 0;
            var dist = new StateDistinguisher<BDD>[K];
            var mfas = new Automaton<BDD>[K];
            for (int i=5; i < K; i++)
            {
                mfas[i] = dfas[i].Minimize();
                dist[i] = new StateDistinguisher<BDD>(dfas[i], x => (char)x.GetMin());
                int minimalStateCount = mfas[i].StateCount;
                var witnesses = dist[i].GetAllDistinguishingSequences();
                AssertIsSuffixClosed(witnesses);
                int distinguisherCount = witnesses.Length;
                //    Assert.IsTrue(distinguisherCount <= minimalStateCount);
                CheckAllStatePairDistinguishers(dfas[i], mfas[i], dist[i]);
                totalMininmalStateCount += minimalStateCount;
                totalDistinguisherCount += distinguisherCount;
            }
            Console.WriteLine(((double)totalDistinguisherCount)/ ((double)totalMininmalStateCount));
        }

        [TestMethod]
        public void TestStateDistinguisherH2()
        {
            var solver = ContextFreeGrammar.GetContext();
            var regex = "^([0-9]{4,}|[3-9][0-9]{2}|2[5-9][0-9])$";
            string[] regexes = File.ReadAllLines(regexesFile);
            var nfa = solver.Convert(regex).RemoveEpsilons();
            var dfa = nfa.Determinize();//.MakeTotal();
            var mfa = dfa.Minimize();
            var dist = new StateDistinguisher<BDD>(dfa);
            int minimalStateCount = mfa.StateCount;
            var witnesses = dist.GetAllDistinguishingSequences();
            AssertIsSuffixClosed(witnesses);
            //var w = dist.GetDistinguishingSequence(1, 10);
            int distinguisherCount = witnesses.Length;
            Assert.IsTrue(distinguisherCount < minimalStateCount + 1);
            CheckAllStatePairDistinguishers(dfa, mfa, dist);
        }

        [TestMethod]
        public void TestStateDistinguisher_EnumerateAllDistinguishingSequences()
        {
            var solver = ContextFreeGrammar.GetContext();
            var regex = "^([0-9]{4,}|[3-9][0-9]{2}|2[5-9][0-9])$";
            string[] regexes = File.ReadAllLines(regexesFile);
            var nfa = solver.Convert(regex).RemoveEpsilons();
            var dfa = nfa.Determinize();//.MakeTotal();
            var mfa = dfa.Minimize();
            var dist = new StateDistinguisher<BDD>(dfa, x => (char)x.GetMin(), false);
            int minimalStateCount = mfa.StateCount;
            var distinguishers = new Dictionary<Tuple<int, int>, List<ConsList<BDD>>>();
            foreach (int p in mfa.GetStates())
                foreach (int q in mfa.GetStates())
                    if (p != q)
                    {
                        var pq = (p < q ? new Tuple<int, int>(p, q) : new Tuple<int, int>(q, p));
                        if (!distinguishers.ContainsKey(pq))
                            distinguishers[pq] = new List<ConsList<BDD>>(dist.EnumerateAllDistinguishingSequences(p, q));
                    }
            Assert.AreEqual<int>((mfa.StateCount * (mfa.StateCount-1))/2, distinguishers.Count);
        }

        [TestMethod]
        public void TestStateDistinguisherH2_Concrete()
        {
            var solver = ContextFreeGrammar.GetContext();
            var regex = "^([0-9]{4,}|[3-9][0-9]{2}|2[5-9][0-9])$";
            var nfa = solver.Convert(regex).RemoveEpsilons();
            var dfa = nfa.Determinize();//.MakeTotal();
            var mfa = dfa.Minimize();
            var dist = new StateDistinguisher<BDD>(dfa, x => (char)x.GetMin());
            int minimalStateCount = mfa.StateCount;
            var witnesses = dist.GetAllDistinguishingStrings();
            AssertIsSuffixClosed(witnesses);
            //var w = dist.GetDistinguishingSequence(1, 10);
            int distinguisherCount = witnesses.Length;
            Assert.IsTrue(distinguisherCount < minimalStateCount + 1);
        }

        private static void AssertIsSuffixClosed(Sequence<BDD>[] witnesses)
        {
            //check that witnesses is a suffix closed set
            foreach (var w in witnesses)
                if (!w.IsEmpty)
                    Assert.IsTrue(Array.Exists(witnesses, x => x.Equals(w.Rest())));
        }

        private static void AssertIsSuffixClosed(string[] witnesses)
        {
            //check that witnesses is a suffix closed set
            foreach (var w in witnesses)
                if (w != "")
                    Assert.IsTrue(Array.Exists(witnesses, x => x.Equals(w.Substring(1))));
        }

        private static void CheckAllStatePairDistinguishers(Automaton<BDD> dfa, Automaton<BDD> mfa, StateDistinguisher<BDD> dist)
        {
            foreach (var p in mfa.GetStates())
                foreach (var q in mfa.GetStates())
                    if (p != q)
                    {
                        var d = dist.GetDistinguishingSequence(p, q);
                        int q1;
                        int p1;
                        bool ok_q = dfa.TryGetTargetState(q, out q1, d.ToArray());
                        bool ok_p = dfa.TryGetTargetState(p, out p1, d.ToArray());
                        Assert.IsTrue(ok_q || ok_p);
                        if (ok_q && ok_p)
                        {
                            Assert.IsTrue(dfa.IsFinalState(q1) != dfa.IsFinalState(p1));
                        }
                    }
        }
    }
}
