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

        static string[] hardcases = new string[]
        {
@"^([A-Z|a-z|&amp;]{3})(([0-9]{2})([0][13456789]|[1][012])([0][1-9]|[12][\d]|[3][0])|([0-9]{2})([0][13578]|[1][02])([0][1-9]|[12][\d]|[3][01])|([02468][048]|[13579][26])([0][2])([0][1-9]|[12][\d])|([1-9]{2})([0][2])([0][1-9]|[12][0-8]))(\w{2}[A|a|0-9]{1})$|^([A-Z|a-z]{4})(([0-9]{2})([0][13456789]|[1][012])([0][1-9]|[12][\d]|[3][0])|([0-9]{2})([0][13578]|[1][02])([0][1-9]|[12][\d]|[3][01])|([02468][048]|[13579][26])([0][2])([0][1-9]|[12][\d])|([1-9]{2})([0][2])([0][1-9]|[12][0-8]))((\w{2})([A|a|0-9]{1})){0,3}$",
@"^[0-3]{1}[0-9]{1}[ ]{1}(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec){1}[ ]{1}[0-9]{2}$",
@"^((((((0[1-9])|(1\d)|(2[0-8]))\.((0[123456789])|(1[0-2])))|(((29)|(30))\.((0[13456789])|(1[0-2])))|((31)\.((0[13578])|(1[02]))))\.\d{4})|((29)\.(02)\.\d{2}(([02468][048])|([13579][26]))))(\s((0\d)|(1\d)|(2[0-3]))\:([0-5]\d)\:([0-5]\d)\.\d{7})$",
@"^(3[0-1]|2[0-9]|1[0-9]|0[1-9])[\/](Jan|JAN|Feb|FEB|Mar|MAR|Apr|APR|May|MAY|Jun|JUN|Jul|JUL|Aug|AUG|Sep|SEP|Oct|OCT|Nov|NOV|Dec|DEC)[\/]\d{4}$",
@"^(((0?[1-9]|1[012])/(0?[1-9]|1\d|2[0-8])|(0?[13456789]|1[012])/(29|30)|(0?[13578]|1[02])/31)/(19|[2-9]\d)\d{2}|0?2/29/((19|[2-9]\d)(0[48]|[2468][048]|[13579][26])|(([2468][048]|[3579][26])00)))$",
@"^(((0[1-9]|[12]\d|3[01])[\s\.\-\/](0[13578]|1[02])[\s\.\-\/]((19|[2-9]\d)\d{2}))|((0[1-9]|[12]\d|30)[\s\.\-\/](0[13456789]|1[012])[\s\.\-\/]((19|[2-9]\d)\d{2}))|((0[1-9]|1\d|2[0-8])[\s\.\-\/]02[\s\.\-\/]((19|[2-9]\d)\d{2}))|(29[\s\.\-\/]02[\s\.\-\/]((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))))$",
@"^([0-1][0-9]|2[0-3]):([0-5][0-9]):([0-5][0-9])([Z]|\.[0-9]{4}|[-|\+]([0-1][0-9]|2[0-3]):([0-5][0-9]))?$",
@"^(((0[1-9]|[12][0-9]|3[01])([\.])(0[13578]|10|12)([\.])((19[0-9][0-9])|(2[0-9][0-9][0-9])))|(([0][1-9]|[12][0-9]|30)([\.])(0[469]|11)([\.])((19[0-9][0-9])|(2[0-9][0-9][0-9])))|((0[1-9]|1[0-9]|2[0-8])([\.])(02)([\.])((19[0-9][0-9])|(2[0-9][0-9][0-9])))|((29)([\.])(02)([\.])([02468][048]00))|((29)([\.])(02)([\.])([13579][26]00))|((29)([\.])(02)([\.])([0-9][0-9][0][48]))|((29)([\.])(02)([\.])([0-9][0-9][2468][048]))|((29)([\.])(02)([\.])([0-9][0-9][13579][26])))$",
@"^((((19[0-9][0-9])|(2[0-9][0-9][0-9]))([-])(0[13578]|10|12)([-])(0[1-9]|[12][0-9]|3[01]))|(((19[0-9][0-9])|(2[0-9][0-9][0-9]))([-])(0[469]|11)([-])([0][1-9]|[12][0-9]|30))|(((19[0-9][0-9])|(2[0-9][0-9][0-9]))([-])(02)([-])(0[1-9]|1[0-9]|2[0-8]))|(([02468][048]00)([-])(02)([-])(29))|(([13579][26]00)([-])(02)([-])(29))|(([0-9][0-9][0][48])([-])(02)([-])(29))|(([0-9][0-9][2468][048])([-])(02)([-])(29))|(([0-9][0-9][13579][26])([-])(02)([-])(29)))$",
@"^(((19|20)(([0][48])|([2468][048])|([13579][26]))|2000)[\-](([0][13578]|[1][02])[\-]([012][0-9]|[3][01])|([0][469]|11)[\-]([012][0-9]|30)|02[\-]([012][0-9]))|((19|20)(([02468][1235679])|([13579][01345789]))|1900)[\-](([0][13578]|[1][02])[\-]([012][0-9]|[3][01])|([0][469]|11)[\-]([012][0-9]|30)|02[\-]([012][0-8])))$",
@"^((((0[13578]|10|12)([-./])(0[1-9]|[12][0-9]|3[01])([-./])(\d{4}))|((0[469]|1­1)([-./])([0][1-9]|[12][0-9]|30)([-./])(\d{4}))|((2)([-./])(0[1-9]|1[0-9]|2­[0-8])([-./])(\d{4}))|((2)(\.|-|\/)(29)([-./])([02468][048]00))|((2)([-./])­(29)([-./])([13579][26]00))|((2)([-./])(29)([-./])([0-9][0-9][0][48]))|((2)­([-./])(29)([-./])([0-9][0-9][2468][048]))|((2)([-./])(29)([-./])([0-9][0-9­][13579][26]))))$",
@"^(((0?[1-9]|1[012])/(0?[1-9]|1\d|2[0-8])|(0?[13456789]|1[012])/(29|30)|(0?[13578]|1[02])/31)/(19|[2-9]\d)\d{2}|0?2/29/((19|[2-9]\d)(0[48]|[2468][048]|[13579][26])|(([2468][048]|[3579][26])00)))$",
@"^([A-Z|a-z|&amp;]{3}\d{2}((0[1-9]|1[012])(0[1-9]|1\d|2[0-8])|(0[13456789]|1[012])(29|30)|(0[13578]|1[02])31)|([02468][048]|[13579][26])0229)(\w{2})([A|a|0-9]{1})$|^([A-Z|a-z]{4}\d{2}((0[1-9]|1[012])(0[1-9]|1\d|2[0-8])|(0[13456789]|1[012])(29|30)|(0[13578]|1[02])31)|([02468][048]|[13579][26])0229)((\w{2})([A|a|0-9]{1})){0,3}$",
@"^(((0[1-9]|1[012])/(0[1-9]|1\d|2[0-8])|(0[13456789]|1[012])/(29|30)|(0[13578]|1[02])/31)/[2-9]\d{3}|02/29/(([2-9]\d)(0[48]|[2468][048]|[13579][26])|(([2468][048]|[3579][26])00)))$",
@"^([2-9]\d{3}((0[1-9]|1[012])(0[1-9]|1\d|2[0-8])|(0[13456789]|1[012])(29|30)|(0[13578]|1[02])31)|(([2-9]\d)(0[48]|[2468][048]|[13579][26])|(([2468][048]|[3579][26])00))0229)$",
@"^(((0[1-9]|[12]\d|3[01])\/(0[13578]|1[02])\/((19|[2-9]\d)\d{2}))|((0[1-9]|[12]\d|30)\/(0[13456789]|1[012])\/((19|[2-9]\d)\d{2}))|((0[1-9]|1\d|2[0-8])\/02\/((19|[2-9]\d)\d{2}))|(29\/02\/((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))))$",
@"^(((((0?[1-9])|(1\d)|(2[0-8]))\.((0?[1-9])|(1[0-2])))|((31\.((0[13578])|(1[02])))|((29|30)\.((0?[1,3-9])|(1[0-2])))))\.((20[0-9][0-9]))|(29\.0?2\.20(([02468][048])|([13579][26]))))$",
@"^(((((0[1-9])|(1\d)|(2[0-8]))/((0[1-9])|(1[0-2])))|((31/((0[13578])|(1[02])))|((29|30)/((0[1,3-9])|(1[0-2])))))/((20[0-9][0-9]))|((((0[1-9])|(1\d)|(2[0-8]))/((0[1-9])|(1[0-2])))|((31/((0[13578])|(1[02])))|((29|30)/((0[1,3-9])|(1[0-2])))))/((19[0-9][0-9]))|(29/02/20(([02468][048])|([13579][26])))|(29/02/19(([02468][048])|([13579][26]))))$",
@"^(((((0[1-9])|(1\d)|(2[0-8]))/((0[1-9])|(1[0-2])))|((31/((0[13578])|(1[02])))|((29|30)/((0[1,3-9])|(1[0-2])))))/((20[0-9][0-9]))|((((0[1-9])|(1\d)|(2[0-8]))/((0[1-9])|(1[0-2])))|((31/((0[13578])|(1[02])))|((29|30)/((0[1,3-9])|(1[0-2])))))/((19[0-9][0-9]))|(29/02/20(([02468][048])|([13579][26])))|(29/02/19(([02468][048])|([13579][26]))))$",
@"^(((0[1-9]|[12]\d|3[01])\/(0[13578]|1[02])\/(\d{2}))|((0[1-9]|[12]\d|30)\/(0[13456789]|1[012])\/(\d{2}))|((0[1-9]|1\d|2[0-8])\/02\/(\d{2}))|(29\/02\/((0[48]|[2468][048]|[13579][26])|(00))))$",
@"^(((0[13578]|10|12)([-./])(0[1-9]|[12][0-9]|3[01])([-./])(\d{4}))|((0[469]|11)([-./])([0][1-9]|[12][0-9]|30)([-./])(\d{4}))|((02)([-./])(0[1-9]|1[0-9]|2[0-8])([-./])(\d{4}))|((02)(\.|-|\/)(29)([-./])([02468][048]00))|((02)([-./])(29)([-./])([13579][26]00))|((02)([-./])(29)([-./])([0-9][0-9][0][48]))|((02)([-./])(29)([-./])([0-9][0-9][2468][048]))|((02)([-./])(29)([-./])([0-9][0-9][13579][26])))$",
@"^(3[0-1]|2[0-9]|1[0-9]|0[1-9])[\s{1}|\/|-](Jan|JAN|Feb|FEB|Mar|MAR|Apr|APR|May|MAY|Jun|JUN|Jul|JUL|Aug|AUG|Sep|SEP|Oct|OCT|Nov|NOV|Dec|DEC)[\s{1}|\/|-]\d{4}$",
@"^((4\d{3})|(5[1-5]\d{2}))(-?|\040?)(\d{4}(-?|\040?)){3}|^(3[4,7]\d{2})(-?|\040?)\d{6}(-?|\040?)\d{5}",
@"^\d{2}(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\d{4}$",
@"((^(10|12|0?[13578])([/])(3[01]|[12][0-9]|0?[1-9])([/])((1[8-9]\d{2})|([2-9]\d{3}))$)|(^(11|0?[469])([/])(30|[12][0-9]|0?[1-9])([/])((1[8-9]\d{2})|([2-9]\d{3}))$)|(^(0?2)([/])(2[0-8]|1[0-9]|0?[1-9])([/])((1[8-9]\d{2})|([2-9]\d{3}))$)|(^(0?2)([/])(29)([/])([2468][048]00)$)|(^(0?2)([/])(29)([/])([3579][26]00)$)|(^(0?2)([/])(29)([/])([1][89][0][48])$)|(^(0?2)([/])(29)([/])([2-9][0-9][0][48])$)|(^(0?2)([/])(29)([/])([1][89][2468][048])$)|(^(0?2)([/])(29)([/])([2-9][0-9][2468][048])$)|(^(0?2)([/])(29)([/])([1][89][13579][26])$)|(^(0?2)([/])(29)([/])([2-9][0-9][13579][26])$))",
@"^(((((0[1-9])|(1\d)|(2[0-8]))-((0[1-9])|(1[0-2])))|((31-((0[13578])|(1[02])))|((29|30)-((0[1,3-9])|(1[0-2])))))-((20[0-9][0-9]))|(29-02-20(([02468][048])|([13579][26]))))$",
@"^(an|may|oi)$",
        };

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
        public void TestStateDistinguisher_BugRepo()
        {
            var solver = new CharSetSolver();
            var regex = @"^v.g+\\.w{2,3}/v\\?d[w-]{3}";
            var A = solver.Convert(regex).Determinize();
            var states = new List<int>(A.GetStates());
            var dist = new StateDistinguisher<BDD>(A);
            var AMin = A.Minimize();
            ValidateDistinguishers(A, AMin, dist, 0, regex);
        }

        [TestMethod]
        public void TestStateDistinguisher_BugRepo2()
        {
            var solver = new CharSetSolver();
            var regex = @"(^(?:\w\:)?(?:/|\\\\){1}[^/|\\]*(?:/|\\){1})";
            var A = solver.Convert(regex).Determinize();
            var states = new List<int>(A.GetStates());
            var dist = new StateDistinguisher<BDD>(A);
            var distmap = new List<Tuple<Tuple<int, int>, List<ConsList<BDD>>>>(dist.EnumerateAllDistinguishingSequences());
            var AMin = A.Minimize();
            ValidateDistinguishers(A, AMin, dist, 0, regex);
        }

        private static void ValidateDistinguishers(Automaton<BDD> aut, Automaton<BDD> autMin, StateDistinguisher<BDD> dist, int i, string regex)
        {
            var distseqs = dist.GetAllDistinguishingSequences();
            if (distseqs.Length > autMin.StateCount)
            {
                Console.WriteLine(string.Format("{0}, {1}, {2}", i, distseqs.Length - autMin.StateCount, regex));
            }

            foreach (var p in autMin.States)
                foreach (var q in autMin.States)
                    if (p != q)
                    {
                        var d = dist.GetDistinguishingSequence(p, q);
                        Assert.IsTrue(d != null);
                        //check that d distinguishes p from q
                        int p1;
                        int q1;
                        //aut may be partial, if false is returned it means that the implicit sink state was reached
                        bool pok = aut.TryGetTargetState(p, out p1, d.ToArray());
                        bool qok = aut.TryGetTargetState(q, out q1, d.ToArray());
                        Assert.IsTrue(pok || qok);
                        if (!pok)
                            Assert.IsTrue(aut.IsFinalState(q1));
                        else if (!qok)
                            Assert.IsTrue(aut.IsFinalState(p1));
                        else
                            Assert.IsTrue(aut.IsFinalState(q1) != aut.IsFinalState(p1));
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
            var dfas = new List<Automaton<BDD>>();
            int timeoutcount = 0;
            int k = 1600; //where to start
            int K = 200; //how many regexes to consider
            List<string> regexes_used = new List<string>();
            while (dfas.Count < K)
            {
                var solver = new CharSetSolver();
                var ascii = solver.Convert("^[\0-\x7F]*$");
                var regex = regexes[k++];
                var nfa = solver.Convert(regex).RemoveEpsilons().Intersect(ascii);
                try
                {
                    var dfa = nfa.Determinize(1000);
                    dfas.Add(dfa);
                    regexes_used.Add(regex);
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
            for (int i=0; i < K; i++)
            {
                mfas[i] = dfas[i].Minimize();
                dist[i] = new StateDistinguisher<BDD>(dfas[i], x => (char)x.GetMin(), true, true);
                ValidateDistinguishers(dfas[i], mfas[i], dist[i], i, regexes_used[i]);
                totalMininmalStateCount += mfas[i].StateCount;
                totalDistinguisherCount += dist[i].GetAllDistinguishingSequences().Length;
            }
            Console.WriteLine(((double)totalDistinguisherCount)/ ((double)totalMininmalStateCount));
        }

        [TestMethod]
        public void TestStateDistinguisher_HardCases()
        {
            string[] regexes = hardcases;
            var dfas = new List<Automaton<BDD>>();
            int timeoutcount = 0;
            int k = hardcases.Length-1; //where to start
            int K = 1;// hardcases.Length; //how many regexes to consider
            List<string> regexes_used = new List<string>();
            while (dfas.Count < K)
            {
                var solver = new CharSetSolver();
                var ascii = solver.Convert("^[\0-\x7F]*$");
                var regex = regexes[k++];
                var nfa = solver.Convert(regex).RemoveEpsilons().Intersect(ascii);
                try
                {
                    var dfa = nfa.Determinize(1000);
                    dfas.Add(dfa);
                    regexes_used.Add(regex);
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
            for (int i = 0; i < K; i++)
            {
                mfas[i] = dfas[i].Minimize();
                dist[i] = new StateDistinguisher<BDD>(dfas[i], x => (char)x.GetMin());
                ValidateDistinguishers(dfas[i], mfas[i], dist[i], i, regexes_used[i]);
                totalMininmalStateCount += mfas[i].StateCount;
                totalDistinguisherCount += dist[i].GetAllDistinguishingSequences().Length;
            }
            Console.WriteLine(((double)totalDistinguisherCount) / ((double)totalMininmalStateCount));
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
