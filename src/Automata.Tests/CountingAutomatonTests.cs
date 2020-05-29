using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Microsoft.Automata;
using Microsoft.Automata.Generated;
using Microsoft.Automata.Rex;
using Microsoft.Automata.Utilities;

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Security.Cryptography;

namespace Automata.Tests
{
    [TestClass]
    public class CountingAutomatonTests
    {
        static object dte = null;

        [ClassInitialize]
        static public void Initialize(TestContext context)
        {
            //increase label length for viewing
            Microsoft.Automata.DirectedGraphs.Options.MaxDgmlTransitionLabelLength = 500;
            //dte = System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.14.0");
        }

        [TestMethod]
        public void TestCA_a3()
        {
            var regex = new Regex("(a{3})*", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.StateCount == 2);
            Assert.IsTrue(aut.NrOfCounters == 1);
            //aut.ShowGraph("aaa");
            var daut = CsAutomaton<ulong>.CreateFrom(aut);
            //daut.ShowGraph("aaa_det", true);
        }

        [TestMethod]
        public void TestCA_a4b()
        {
            var regex = new Regex("a.{4}b", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 1);
            Assert.IsTrue(aut.MoveCount == 6);
            //aut.ShowGraph("a4b");
        }

        [TestMethod]
        public void TestCA_LoopTwice()
        {
            var regex = new Regex("[ab]{5,9}[bc]{7,19}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.CreateCountingAutomaton();
            var daut = CsAutomaton<ulong>.CreateFrom(aut);
            //Assert.IsTrue(aut.NrOfCounters == 2);
            //Assert.IsTrue(aut.MoveCount == 4);
            //Assert.IsTrue(aut.NrOfCounters == 2);
            //aut.ShowGraph("LoopTwice",true);
            //daut.ShowGraph("LoopTwice_det", true);
        }

        [TestMethod]
        public void TestCA_TwoLoops()
        {
            var regex = new Regex("a{9}a{0,9}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            Assert.IsTrue(aut.MoveCount == 5);
            Assert.IsTrue(aut.NrOfCounters == 2);
            //aut.ShowGraph("TwoLoops");
        }

        [TestMethod]
        public void TestCA_LoopWithNoUpperBound()
        {
            var regex = new Regex("ab{10,}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 1);
            Assert.IsTrue(aut.MoveCount == 6);
            //aut.ShowGraph("ab10bstar");
        }

        [TestMethod]
        public void TestCA_NestedLoop()
        {
            var regex = new Regex("(a{3}){1,2}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern;
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            Assert.IsTrue(aut.StateCount == 2);
            //aut.ShowGraph("nestedloop");
        }

        [TestMethod]
        public void TestCA__CompositionBugFix()
        {
            var regex = new Regex(".*(a{3}a{3}(()|a{3}a{3}))", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern;
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 4);
            Assert.IsTrue(aut.StateCount == 5);
            //aut.ShowGraph("CompositionBugFix");
        }

        [TestMethod]
        public void TestCA_Normalize()
        {
            var regex = new Regex(".*(a{3}a{3}){1,2}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern.MkMonadic();
            Assert.IsTrue(q1.kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(q1.ConcatCount == 3);
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 4);
            Assert.IsTrue(aut.StateCount == 5);
            //aut.ShowGraph("Normalize");
        }

        [TestMethod]
        public void TestCA_Normalize1()
        {
            var regex = new Regex(".*a{3}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern.MkMonadic();
            Assert.IsTrue(q1.kind == SymbolicRegexKind.Concat);
            //Assert.IsTrue(q1.ConcatCount == 3);
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 1);
            //aut.ShowGraph("Normalize1");
        }

        [TestMethod]
        public void TestCA_Normalize2()
        {
            var regex = new Regex("(([abc]{3,30})b{1,}c){2}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern.MkMonadic();
            Assert.IsTrue(q1.kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(q1.ConcatCount == 7);
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            //aut.ShowGraph("Normalize2");
        }

        [TestMethod]
        public void TestCA_Normalize3()
        {
            var regex = new Regex("(?i:a{3,30}[abc]{4,40})", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern;
            Assert.IsTrue(q1.kind == SymbolicRegexKind.Concat);
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            //aut.ShowGraph("Normalize3");
        }

        [TestMethod]
        public void TestCA_ATVARunningExample()
        {
            var regex = new Regex(".*a.{300}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern;
            Assert.IsTrue(q1.kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(q1.ConcatCount == 2);
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 1);
            //aut.ShowGraph("ATVARunningExample");
            var CsA = CsAutomaton<ulong>.CreateFrom(aut);
            //CsA.ShowGraph("ATVARunningExample_det", false);
        }

        [TestMethod]
        public void TestCA_ATVARunningExample_v2()
        {
            var regex = new Regex("[ab]*a[ab]{10,100}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern;
            Assert.IsTrue(q1.kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(q1.ConcatCount == 2);
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 1);
            //aut.ShowGraph("ATVARunningExample_v2");
        }

        [TestMethod]
        public void TestCA_ATVARunningExample_v3()
        {
            var regex = new Regex("b*a[ab]{10,100}[ab]{5,50}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern;
            Assert.IsTrue(q1.kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(q1.ConcatCount == 3);
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            //aut.ShowGraph("ATVARunningExample_v3");
        }

        [TestMethod]
        public void TestCA_MonadicLoopInStar0()
        {
            var regex = new Regex("(..{8})*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 1);
            //aut.ShowGraph("");
        }

        [TestMethod]
        public void TestCA_MonadicLoopInStar1()
        {
            var regex = new Regex("(.{9})*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 1);
            //aut.ShowGraph("");
        }

        [TestMethod]
        public void TestCA_DotStarMonadicLoopInStar()
        {
            var regex = new Regex(".*(.{9})*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            var det = CsAutomaton<ulong>.CreateFrom(aut);
            Assert.IsTrue(aut.NrOfCounters == 1);
            //aut.ShowGraph("");
            //det.ShowGraph("DotStarMonadicLoopInStar_det");
        }

        [TestMethod]
        public void TestCA_DetSample()
        {
            var regex = new Regex(".*a.{4,8}a", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            var det = CsAutomaton<ulong>.CreateFrom(aut);
            Assert.IsTrue(aut.NrOfCounters == 1);
            //aut.ShowGraph("", true);
            //det.ShowGraph("det", false, true);
        }

        [TestMethod]
        public void TestCA_TwoOverlappingMonadicLoops()
        {
            var regex = new Regex("a{10,100}a{5,50}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            //aut.ShowGraph("TwoOverlappingMonadicLoops");
        }

        [TestMethod]
        public void TestCA_TwoOverlappingMonadicLoops2()
        {
            var regex = new Regex("a{10,100}aa{4,49}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            //aut.ShowGraph("TwoOverlappingMonadicLoops2");
        }

        [TestMethod]
        public void TestCA_SameMonadicLoopX3()
        {
            var regex = new Regex("(a{5,50}){3}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 3);
            //aut.ShowGraph("SameMonadicLoopX3");
        }

        [TestMethod]
        public void TestCA_SameMonadicRegexX3()
        {
            var regex = new Regex("(ab{5,50}){3}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 3);
            //aut.ShowGraph("SameMonadicRegexX3");
        }

        [TestMethod]
        public void TestCA_TwoLoopsInStar()
        {
            var regex = new Regex("(.{10}.{20})*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            //aut.ShowGraph("TwoLoopsInStar", true);
        }

        [TestMethod]
        public void TestCA_ThreeLoopsInStar()
        {
            var regex = new Regex("((.{10}){3})*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 3);
            //aut.ShowGraph("ThreeLoopsInStar", true);
        }

        [TestMethod]
        public void TestCA_SingleMintermLoops()
        {
            var regex = new Regex(".{10,100}.{10,100}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            //aut.ShowGraph("SingleMintermLoops");
        }

        [TestMethod]
        public void TestCA_IsMatch1()
        {
            var regex = new Regex(".*a.{5}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.IsMatch("xaxxxxx"));
            Assert.IsTrue(!aut.IsMatch("xaxxxxxxx"));
            Assert.IsTrue(aut.IsMatch("xaaaaxxxx"));
        }

        [TestMethod]
        public void TestCA_IsMatch2()
        {
            var regex = new Regex(".*a.{5,7}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.IsMatch("xaxxxxx"));
            Assert.IsTrue(aut.IsMatch("xaxxxaaa"));
            Assert.IsTrue(aut.IsMatch("xaxxxxxxx"));
            Assert.IsTrue(!aut.IsMatch("xaxxxxxxxx"));
            Assert.IsTrue(aut.IsMatch("xaaaaxxxx"));
        }

        [TestMethod]
        public void TestCA_IsMatch_SeqOf5orMoreDigits()
        {
            var regex = new Regex(".*[0-9]{5,};.*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            //aut.ShowGraph("SeqOf5orMoreDigits");
            Assert.IsTrue(!aut.IsMatch("___1234;__123__"));
            Assert.IsTrue(aut.IsMatch("___12345;__123__"));
            Assert.IsTrue(!aut.IsMatch("___123456__123__"));
            Assert.IsTrue(aut.IsMatch("___123456__12334567;__"));
        }

        [TestMethod]
        public void TestCA_IncrPush01()
        {
            var regex = new Regex(".*(a{5}|[aA]a{5})*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            //aut.ShowGraph("IncrPush01");
            Assert.IsTrue(aut.IsMatch("xAaaaaa"));
            Assert.IsTrue(aut.IsMatch("xaaaaaa"));
            Assert.IsTrue(aut.IsMatch("xaaaaabaa"));
        }

        [TestMethod]
        public void TestCA_CsAutomatonConstruction1()
        {
            var regex = new Regex(".*(a{5}|[aA]a{5})*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            //aut.ShowGraph("CsAutomatonConstruction1");
            Assert.IsTrue(aut.IsMatch("xAaaaaa"));
            Assert.IsTrue(aut.IsMatch("xaaaaaa"));
            Assert.IsTrue(aut.IsMatch("xaaaaabaa"));
            var det = CsAutomaton<ulong>.CreateFrom(aut);
        }

        [TestMethod]
        public void TestCA_CsAutomatonConstruction2()
        {
            Microsoft.Automata.DirectedGraphs.Options.MaxDgmlTransitionLabelLength = 500;
            var regex = new Regex("((.{5,10}){2})*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            //aut.ShowGraph("CsAutomatonConstruction2_nondet");
            var det = CsAutomaton<ulong>.CreateFrom(aut);
            //det.ShowGraph("CsAutomatonConstruction2_det_dbg",true);
            //det.ShowGraph("CsAutomatonConstruction2_det", true);
            Assert.IsTrue(det.StateCount == 4);
        }

        [TestMethod]
        public void TestCA_CsAutomatonConstruction3()
        {
            Microsoft.Automata.DirectedGraphs.Options.MaxDgmlTransitionLabelLength = 500;
            var regex = new Regex(".*ab{9}d", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            //aut.ShowGraph("CsAutomatonConstruction3_nondet");
            var det = CsAutomaton<ulong>.CreateFrom(aut);
            //det.ShowGraph("CsAutomatonConstruction3_det");
            Assert.IsTrue(det.StateCount == 3);
        }

        [TestMethod]
        public void TestCA_IsMatch_PosAndNeg()
        {
            var regex = new System.Text.RegularExpressions.Regex("^ab{4,10}c$");
            var sr = (SymbolicRegex<ulong>)regex.Compile(false, false);
            var pos_samples = regex.GenerateRandomDataSet(10);
            var neg_samples = regex.Complement().GenerateRandomDataSet(10);
            var ca = sr.Pattern.CreateCountingAutomaton();
            foreach (var str in pos_samples)
            {
                Assert.IsTrue(ca.IsMatch(str));
                Assert.IsTrue(regex.IsMatch(str));
            }
            foreach (var str in neg_samples)
            {
                Assert.IsFalse(ca.IsMatch(str));
                Assert.IsFalse(regex.IsMatch(str));
            }
        }

        [TestMethod]
        public void TestCA_TrickyCase()
        {
            var regex = new Regex("([ab]{3,6}[bc]{4,8})*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern;
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            var CsA = CsAutomaton<ulong>.CreateFrom(aut);
            //aut.ShowGraph("TrickyCase_NCA", true);
            //CsA.ShowGraph("TrickyCase_DCA_dbg", true, false);
            //CsA.ShowGraph("TrickyCase_DCA", false, true);
        }

        [TestMethod]
        public void TestCA_Discussion()
        {
            var regex = new Regex("b*[ab][ab]{9}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var q1 = sr.Pattern;
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 1);
            var CsA = CsAutomaton<ulong>.CreateFrom(aut);
            //aut.ShowGraph("Discussion_NCA", true);
            //CsA.ShowGraph("Discussion_DCA_dbg", true, false);
            //CsA.ShowGraph("Discussion_DCA", false, true);
        }

        [TestMethod]
        public void TestCA_TrickyCase2()
        {
            var regex = new Regex("d{1,20}aUa$|d{1,20}aOa$", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(false, false);
            var q1 = sr.Pattern;
            var aut = q1.CreateCountingAutomaton();
            Assert.IsTrue(aut.NrOfCounters == 2);
            var CsA = CsAutomaton<ulong>.CreateFrom(aut);
            //aut.ShowGraph("TrickyCase2_NCA", true);
            //CsA.ShowGraph("TrickyCase2_DCA_dbg", true, false);
            //CsA.ShowGraph("TrickyCase2_DCA", false, true);
        }

        [TestMethod]
        public void TestCA_StartOne()
        {
            var regex = new Regex(".*a{5}", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            Assert.IsTrue(aut.IsMatch("aaaaa"));
            Assert.IsTrue(!aut.IsMatch("aaaa"));
            //aut.ShowGraph("One");
        }

        [TestMethod]
        public void TestCA_BugRepo()
        {
            var regex = new Regex("(([0-1]|())[0-9])|2", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile(true, false);
            var aut = sr.Pattern.CreateCountingAutomaton();
            //Assert.IsTrue(aut.IsMatch("3"));
            //Assert.IsTrue(!aut.IsMatch("a"));
            //aut.ShowGraph("TestCA_BugRepo");
        }
    }
}
