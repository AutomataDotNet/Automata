using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Microsoft.Automata;

using System.Text.RegularExpressions;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class FiniteAutomataTests
    {

        [TestMethod]
        public void CheckDeterminismWithBDDTest()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var fa = solver.Convert(@"^(a*\w)$", System.Text.RegularExpressions.RegexOptions.None);
            fa = fa.RemoveEpsilons();
            //solver.ShowGraph(fa, "fa.dgml");
            fa.CheckDeterminism();
            Assert.IsFalse(fa.IsDeterministic, "fa expected to be nondeterministic");
            var fad = fa.Determinize();
            //solver.ShowGraph(fad, "fad.dgml");
            fad.CheckDeterminism(true);
            Assert.IsTrue(fad.IsDeterministic, "fad expected to be deterministic");

            var fam = fad.Minimize();
            Assert.AreEqual<int>(3, fam.StateCount);
            //solver.ShowGraph(fam, "fam.dgml");

            fa = solver.Convert(@"^((a|B|c)*[\w-[a-d]])$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //ShowAsGraph(converter, fa, "fa3.dgml");
            fa = fa.RemoveEpsilons();
            //ShowAsGraph(converter.Describe, fa, "fa4.dgml");
            fa.CheckDeterminism();
            Assert.IsTrue(fa.IsDeterministic, "fa expected to be deterministic");
        }

        [TestMethod]
        public void CheckPrefix()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var fa = solver.Convert(@"^(ab)*$");
            var pref = fa.PrefixLanguage().Determinize().Minimize();
            Assert.IsTrue(fa.Minus(pref).IsEmpty);
            Assert.IsFalse(pref.Intersect(solver.Convert(@"^a$")).IsEmpty);
            Assert.IsTrue(pref.Intersect(solver.Convert(@"^b$")).IsEmpty);
        }

        [TestMethod]
        public void CheckSuffix()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var fa = solver.Convert(@"^(ab)*$");
            var suffix = fa.SuffixLanguage().Determinize().Minimize();
            Assert.IsTrue(fa.Minus(suffix).IsEmpty);
            Assert.IsFalse(suffix.Intersect(solver.Convert(@"^b$")).IsEmpty);
            Assert.IsTrue(suffix.Intersect(solver.Convert(@"^a$")).IsEmpty);
        }

        [TestMethod]
        public void CheckAmbiguity()
        {
            var solver = new CharSetSolver(BitWidth.BV7);

            var a = solver.MkCharConstraint('a');
            var b = solver.MkCharConstraint('b');

            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 1, a));
            moves.Add(new Move<BDD>(0, 2, a));
            moves.Add(new Move<BDD>(1, 3, a));
            moves.Add(new Move<BDD>(2, 3, b));

            var aut1 = Automaton<BDD>.Create(solver, 0, new int[] { 2, 3 }, moves);
            Automaton<BDD> ambl;

            Assert.IsFalse(aut1.IsAmbiguous(out ambl));
            Assert.IsTrue(ambl.IsEmpty);
            

            var aut2 = Automaton<BDD>.Create(solver, 0, new int[] { 2, 3, 1 }, moves);
            Assert.IsTrue(aut2.IsAmbiguous(out ambl));

            //solver.ShowGraph(ambl.Determinize(solver).Minimize(solver),"amb");
            Assert.IsFalse(ambl.IsEmpty);
        }

        [TestMethod]
        public void MkCheckDeterminismTest()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var fa = solver.Convert(@"^((a|B|c)*[\w-[a-e]])$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //solver.ShowGraph(fa, "fa3.dgml");
            fa = fa.RemoveEpsilons();
            //solver.ShowGraph(fa, "fa.dgml");
            fa.CheckDeterminism(true);
            Assert.IsTrue(fa.IsDeterministic, "fa expected to be deterministic");
        }

        [TestMethod]
        public void MkCheckDeterminismTest2()
        {
            var converter = new CharSetSolver(BitWidth.BV7);
            var regex = @"^[\0-\x7E]*(([01]|01)0)$";
            var fa = converter.Convert(regex, System.Text.RegularExpressions.RegexOptions.None);
            fa = fa.RemoveEpsilons();
            fa.CheckDeterminism();
            Assert.IsFalse(fa.IsDeterministic, "fa expected to be nondeterministic");
            var fad = fa.Determinize();
            //converter.ShowGraph(fa, "fa.dgml");
            //converter.ShowGraph(fad, "fad.dgml");
            fad.CheckDeterminism(true);
            Assert.IsTrue(fad.IsDeterministic, "fa expected to be deterministic");
        }


        [TestMethod]
        public void MkRegexToAutomTest1()
        {
            var solver = new CharSetSolver(BitWidth.BV7);

            string regexA = "^(ab){2,4}$";
            var A = solver.Convert(regexA);
            //solver.ShowGraph(A, "A.dgml");
        }

        [TestMethod]
        public void MkRegexFromAutomatonTest1()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            string regexA = "^abc(d|[a-ce-g])*$";
            var A = solver.Convert(regexA).Determinize().MinimizeHopcroft();
            string a = solver.ConvertToRegex(A);
            Assert.AreEqual<string>("^(abc([a-g])*)$", a);
        }

        [TestMethod]
        public void MkLengthAutomatonTest()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            string regexA = "^(((((00|11)|10((00|11))*01)|(01|10((00|11))*10)(((00|11)|01((00|11))*10))*(10|01((00|11))*01)))*)$";
            BDD _1 = solver.MkCharConstraint( '1');
            var A = solver.Convert(regexA).RelpaceAllGuards(x => (x == null ? null : _1));
            var B = A.Determinize().MinimizeHopcroft();
            var lengthregex = solver.ConvertToRegex(B);
            Assert.AreEqual<string>("^((11)*)$", lengthregex);
        }

        [TestMethod]
        public void MkRegexFromAutomatonOf_3_Divisibility()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var _0 = solver.MkCharConstraint( 'a');
            var _3 = solver.MkCharConstraint( 'd');
            var _03 = solver.MkOr(_0, _3);
            var _1 = solver.MkCharConstraint( 'b');
            var _2 = solver.MkCharConstraint( 'c');
            var moves = new Move<BDD>[]{
                Move<BDD>.Create(0, 0, _03),
                Move<BDD>.Create(0, 1, _2),
                Move<BDD>.Create(0, 2, _1),
                Move<BDD>.Create(1, 1, _03),
                Move<BDD>.Create(1, 0, _1),
                Move<BDD>.Create(1, 2, _2),
                Move<BDD>.Create(2, 2, _03),
                Move<BDD>.Create(2, 0, _2),
                Move<BDD>.Create(2, 1, _1)
            };
            var aut = Automaton<BDD>.Create(solver, 0, new int[] { 0}, moves);
           //solver.ShowGraph(aut, "div3a");
            string regex = solver.ConvertToRegex(aut).Replace("[ad]", "(a|d)").Replace("[b]", "b").Replace("[c]", "c");
            var aut2 = solver.Convert(regex).Determinize().MinimizeHopcroft();
          // solver.ShowGraph(aut2, "div3b");
            bool equiv = aut.IsEquivalentWith(aut2);
            Assert.IsTrue(equiv);
            //binary version of the regex
            string regex01 = regex.Replace("a","00").Replace("b","01").Replace("c","10").Replace("d","11");
            var bits30 = solver.Convert("^[01]{10,30}\\z");
            var aut01_ = solver.Convert(regex01).Determinize();
            //solver.ShowGraph(aut01_, "aut01_");
            var aut01 = aut01_.MinimizeHopcroft();
            //solver.ShowGraph(aut01, "aut01");
            string regex01small = solver.ConvertToRegex(aut01);
            aut01 = aut01.Intersect(bits30);
            //genarate some random paths in this automaton and check that the binary representation is a numer that is divisible by 3.
            for (int i = 0; i < 1000; i++)
            {
                string sample = solver.ChooseString(aut01.ChoosePathToSomeFinalState(solver.Chooser));
                int m = 0;
                for (int j = sample.Length-1; j >=0; j--)
                {
                    if (sample[j] == '0')
                        m = m << 1;
                    else
                        m = (m << 1) | 1;
                }
                bool div3 = ((m % 3) == 0);
                Assert.IsTrue(div3);
            }
        }

        [TestMethod]
        public void MkRegexFromAutomatonTest2()
        {
            string regex1 = @"^([\w-[\d]]|3)$";
            var solver = new CharSetSolver(BitWidth.BV16);
            var aut1 = solver.Convert(regex1);
            //this will be a pretty large regex where the classes have been expanded
            var regex2 = solver.ConvertToRegex(aut1);
            var aut2 = solver.Convert(regex2);
            bool equiv = aut1.IsEquivalentWith(aut2);
            Assert.IsTrue(equiv);
        }

        [TestMethod]
        public void BolTest()
        {
            var solver = new CharSetSolver(BitWidth.BV16);

            string regexA = "\n$|^$"; //meaning of start anchor in multiline mode
            var A = solver.Convert(regexA);

            string regexB = "^\\z"; //start anchor followed by end of string
            var B = solver.Convert(regexB, System.Text.RegularExpressions.RegexOptions.Multiline);

            //solver.ShowGraph(A, "A");
            //solver.ShowGraph(B, "B");

            bool equiv = A.IsEquivalentWith(B);
            Assert.IsTrue(equiv);
        }

        [TestMethod]
        public void EolTest()
        {
            var solver = new CharSetSolver(BitWidth.BV16);

            string regexA = "^$|^\n"; //meaning of end anchor in multiline mode
            var A = solver.Convert(regexA);

            string regexB = "\\A$"; //beginning of string followed by end anchor
            var B = solver.Convert(regexB, System.Text.RegularExpressions.RegexOptions.Multiline);

            //solver.ShowGraph(A, "A");
            //solver.ShowGraph(B, "B");

            bool equiv = A.IsEquivalentWith(B);
            Assert.IsTrue(equiv);
        }

        [TestMethod]
        public void MultilineRegexTest() 
        {

            Rex.RexEngine rex = new Rex.RexEngine(BitWidth.BV7);
            string r = "^abc$";
            var A = rex.CreateFromRegexes(System.Text.RegularExpressions.RegexOptions.Multiline, r);
            //rex.Solver.ShowGraph(A, "A");

           // rex.Solver.Chooser.RandomSeed = 123;
            foreach (string s in rex.GenerateMembers(A, 100))
                Assert.IsTrue(Rex.RexEngine.IsMatch(s, r, RegexOptions.Multiline), "unexpected mismatch");

            var notA = A.Complement().MinimizeHopcroft();
            //rex.Solver.ShowGraph(notA, "notA");
            foreach (string s in rex.GenerateMembers(notA, 10000))
                Assert.IsFalse(Rex.RexEngine.IsMatch(s, r, RegexOptions.Multiline), "unexpected match");
        }

        [TestMethod]
        public void MkDifferenceTest()
        {
            var solver = new CharSetSolver(BitWidth.BV7);

            string regexA = "^[abc]c{3}$";
            string regexB = "^(a|b)+[abc]{3}$";

            var A = solver.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
            //solver.ShowGraph(A, "A.dgml");

            var B = solver.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);
            //solver.ShowGraph(B, "B.dgml");

            var C = Automaton<BDD>.MkDifference(A, B, 0);
            //solver.ShowGraph(C, "C.dgml");

            string s = solver.GenerateMember(C);

            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, regexA), "must be a member of " + regexA);
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(s, regexB), "must not be a member of " + regexB);

            Assert.AreEqual<string>("cccc", s);

        }

        [TestMethod]
        public void AutomSample1() 
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);  //new solver using ASCII encoding
            string r1 = @"^[A-Za-z0-9]+@(([A-Za-z0-9\-])+\.)+([A-Za-z\-])+$";   // regex for "almost" valid emails
            Automaton<BDD> A = solver.Convert(r1); //accepts strings that match the regex r1
            A = A.RemoveEpsilons();         //remove epsilons, uses disjunction of character sets to combine transitions
            //solver.ShowGraph(A, "A.dgml");             //save and visualize the automaton using dgml
            string s = solver.GenerateMember(A);       //grenerate some member
        }

        [TestMethod]
        public void AutomSample2()
        {

            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);//charset solver
            string a = @"^[A-Za-z0-9]+@(([A-Za-z0-9\-])+\.)+([A-Za-z\-])+$";    //.Net regex
            string b = @"^\d.*$";                                               //.Net regex
            Automaton<BDD> A = solver.Convert(a);    //create the equivalent automata
            Automaton<BDD> B = solver.Convert(b);
            Automaton<BDD> C = A.Minus(B);   //construct the difference 
            //solver.ShowGraph(C, "C.dgml");
            var M = C.Determinize().MinimizeHopcroft();  //minimize the automaton
            //solver.ShowGraph(M, "M.dgml");               //save and visualize
            //var M2 = C.Determinize(solver).Minimize2(solver);  //minimize the automaton
            //solver.ShowGraph(M2, "M2.dgml");               //save and visualize
            string s = solver.GenerateMember(M);         //generate some member, e.g. "HV7@9.2.8.-d2bVu0YH.z1f.R"
        }


        // PIETER
        [TestMethod]
        public void CheckDifferenceTest()
        {
            var bddBuilder = new CharSetSolver(BitWidth.BV7);
            var converter = bddBuilder;

            string[] regexes = SampleRegexes.regexes; // File.ReadAllLines(sampleDir + "icst.txt");

            int i = 1;
            int j = 2;

            string regexA = regexes[i];
            string regexB = regexes[j];

            var A = converter.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
            //ShowAsGraph(converter, A, "A.dgml");

            var B = converter.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);
            //ShowAsGraph(converter, B, "B.dgml");

            List<BDD> witness;
            var res = Automaton<BDD>.CheckDifference(A, B, 0, out witness);
            //ShowAsGraph(converter, C, "C.dgml");

            Console.Write(":");
            foreach (BDD cur in witness)
            {
                Console.Write(".");
            }
            Console.WriteLine(":");


            Assert.IsTrue(res == true, @"A-B must be nonempty");
        }

        #region product experiments
        [TestMethod]
        public void MakeProductBDD_ASCII_Test()
        {
            ProdExperimentBDD(BitWidth.BV7);
        }
        [TestMethod]
        public void MakeProductBDD_Unicode_Test()
        {
            ProdExperimentBDD(BitWidth.BV16);
        }
        [TestMethod]
        public void CheckProductBDD_Unicode_Test()
        {
            LProdExperimentBDD(BitWidth.BV16);
        }
        [TestMethod]
        public void CheckProductBDD_ASCII_Test()
        {
            LProdExperimentBDD(BitWidth.BV7);
        }

        private void ProdExperimentBDD(BitWidth encoding)
        {
            var solver = new CharSetSolver(encoding);
            var converter = solver;

            string[] regexes = SampleRegexes.regexes;
            int nonemptyCount = 0;

            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                {
                    string regexA = regexes[i];
                    string regexB = regexes[j];

                    var A = converter.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
                    var B = converter.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);
                    var C = Automaton<BDD>.MkProduct(A, B);

                    if (i == j)
                        Assert.IsFalse(C.IsEmpty);
                    if (!C.IsEmpty)
                    {
                        nonemptyCount += 1;
                        string s = converter.GenerateMember(C);
                        Assert.IsTrue(Regex.IsMatch(s, regexA, RegexOptions.None), "regex mismatch");
                        Assert.IsTrue(Regex.IsMatch(s, regexB, RegexOptions.None), "regex mismatch");
                    }
                }
            Assert.AreEqual<int>(10, nonemptyCount, "wrong number of empty intersections");
        }

        private void LProdExperimentBDD(BitWidth encoding)
        {
            var solver = new CharSetSolver(encoding);
            var converter = solver;

            string[] regexes = SampleRegexes.regexes;
            int nonemptyCount = 0;

            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                {
                    string regexA = regexes[i];
                    string regexB = regexes[j];

                    long start = Microsoft.Automata.Utilities.HighTimer.Now;

                    var A = converter.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
                    var B = converter.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);

                    List<BDD> witness;
                    bool C = Automaton<BDD>.CheckProduct(A, B, 0, out witness);

                    if (i == j)
                        Assert.IsTrue(C, "product must me nonempty");
                    if (C)
                    {
                        nonemptyCount += 1;
                        string s = new String(Array.ConvertAll(witness.ToArray(), cs => {return (char)solver.Choose(cs);}));
                        Assert.IsTrue(Regex.IsMatch(s, regexA, RegexOptions.None), "regex mismatch");
                        Assert.IsTrue(Regex.IsMatch(s, regexB, RegexOptions.None), "regex mismatch");
                    }
                }
            Assert.AreEqual<int>(10, nonemptyCount, "wrong number of empty intersections");
        }

        #endregion


        #region diff experiments
        [TestMethod]
        public void CheckDiffICSTsamples_Unicode()
        {
            LDiffExperimentBDD(BitWidth.BV16);
        }
        [TestMethod]
        public void CheckDiffICSTsamples_ASCII()
        {
            LDiffExperimentBDD(BitWidth.BV7);
        }

        private static void LDiffExperimentBDD(BitWidth encoding)
        {
            var solver = new CharSetSolver(encoding);
            var converter = solver;

            string[] regexes = SampleRegexes.regexes;

            long timeout = 60 * Microsoft.Automata.Utilities.HighTimer.Frequency; //1 minute

            int nonemptyCnt = 0;

            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                {
                    string regexA = regexes[i];
                    string regexB = regexes[j];

                    long start = Microsoft.Automata.Utilities.HighTimer.Now;

                    var A = converter.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
                    var B = converter.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);
                    try
                    {
                        List<BDD> witness;
                        bool isNonempty;
                        isNonempty = Automaton<BDD>.CheckDifference(A, B, (int)timeout, out witness);
                        if (isNonempty)
                        {
                            nonemptyCnt += 1;
                            string s = solver.ChooseString(witness);
                            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, regexA), s + " must be a member of " + regexA);
                            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(s, regexB), s + " must not be a member of " + regexB);
                        }
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Timeout {0},{1}", i, j);
                    }
                }
            Assert.AreEqual<int>(30, nonemptyCnt, "unexpected number of nonempty differences");
        }

        #endregion


        [TestMethod]
        public void Utf8regex()
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            var one = @"[\0-\x7F]";
            var two = @"[\xC2-\xDF][\x80-\xBF]";
            var thr = @"(\xE0[\xA0-\xBF]|\xED[\x80-\x9F]|[\xE1-\xEC\xEE\xEF][\x80-\xBF])[\x80-\xBF]";
            var fou = @"(\xF0[\x90-\x9F]|\xF4[\x80-\x8F]|[\xF1-\xF3][\x80-\xBF])[\x80-\xBF]{2}";
            var regex = string.Format("^({0}|{1}|{2}|{3})*$", one, two, thr, fou);
            var aut = solver.Convert(regex);
            var aut2 = aut.Determinize().MinimizeHopcroft();
            //var aut22 = aut.Determinize(solver).Minimize2(solver);
            //var aut3 = aut2.Complement(solver);
            //solver.ShowGraph(aut, "Utf8");
            //solver.ShowGraph(aut2, "aut2");
            //solver.ShowGraph(aut22, "aut22");
            //solver.ShowGraph(aut3, "Utf8compl");
        }

        [TestMethod]
        public void TestAB()
        {
            var regex = "^(a*b)|(b*a)$";
            var k = 100;
            var regex2 = "^[a-z]{" + k + "}$";

            var rex = new Rex.RexEngine(BitWidth.BV7);
            var sfa2 = rex.Minimize(rex.CreateFromRegexes(regex2));
            var sfa = rex.Minimize(rex.CreateFromRegexes(regex));

            //rex.Solver.ShowGraph(sfa, "TestAB");
            Assert.IsTrue(Regex.IsMatch("foobaba", "^(a*b)|(b*a)$"));

            var sfa3 = rex.Intersect(sfa, sfa2);
            for (int i = 0; i < 1000; i++)
            {
                var s = rex.GenerateMemberUniformly(sfa3);
                Assert.AreEqual<int>(k, s.Length);
                Assert.IsTrue(Regex.IsMatch(s, regex));
                Assert.IsTrue(Regex.IsMatch(s, regex2));
            }
        }

        [TestMethod]
        public void TestUnion()
        {
            var rex = new Rex.RexEngine(BitWidth.BV7);
            var solver = rex.Solver;
            var dfa1 = solver.Convert("^(a*b)$").Determinize().MinimizeHopcroft();
            var dfa2 = solver.Convert("^(b*a)$").Determinize().MinimizeHopcroft();
            var dfa3 = dfa1.Union(dfa2).Determinize().MinimizeHopcroft();
            var dfa4 = solver.Convert("^((a*b)|(b*a))$").Determinize().MinimizeHopcroft();
            Assert.IsTrue(dfa3.IsEquivalentWith(dfa4));

            //solver.ShowGraph(dfa, "TestUnion");
        }

        [TestMethod]
        public void TestSomeRegex()
        {
            var rex = new Rex.RexEngine(BitWidth.BV7);
            var r1 = ",(([^\\n,\"]*)|(\"[^\\n\"]*\")),";
            var nfa = rex.Solver.Convert("^(" + r1 +")$").RemoveEpsilons();
            var dfa = nfa.Determinize().Minimize();
            //rex.Solver.ShowGraph(nfa, "nfa");
            //rex.Solver.ShowGraph(dfa, "dfa");
        }

        [TestMethod]
        public void TestSymbolicPartitionRefinement()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //rex for ascii range

            var pr = new SymbolicPartitionRefinement<BDD>(rex.Solver,rex.Solver.MkCharSetFromRange('0', '9'));
            pr.Refine(rex.Solver.MkCharSetFromRange('0', '9'));
            pr.Refine(rex.Solver.MkCharSetFromRange('a', 'z'));
            pr.Refine(rex.Solver.MkCharSetFromRange('A', 'z'));
            pr.Refine(rex.Solver.MkCharSetFromRanges('a', 'z', 'A', 'Z'));
            pr.Refine(rex.Solver.MkCharSetFromRanges('a', 'z', '0', '9'));
            pr.Refine(rex.Solver.MkCharSetFromRange('A', 'Z'));
            var regions = new List<BDD>(pr.GetRegions());

            Assert.AreEqual<int>(4, regions.Count);
        }

        [TestMethod]
        public void TestMinimization()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //rex for ascii range

            int passwlength = 10;

            var r1 = @"^[\x21-\x7E]{" + passwlength + "}$";
            var r2 = "[A-Z].*[A-Z]";
            var r3 = "[A-Z][A-Z][A-Z]";
            var r4 = "[0-9].*[0-9]";
            var r5 = @"[\x21-\x2F\x3A-\x40\x5B-\x60\x7B-\x7E]";

            var time = System.Environment.TickCount;
            var a1 = rex.Minimize(rex.CreateFromRegexes(r1));
            var a2 = rex.Minimize(rex.CreateFromRegexes(r2));
            var a3 = rex.Minimize(rex.CreateFromRegexes(r3));
            var a4 = rex.Minimize(rex.CreateFromRegexes(r4));
            var a5 = rex.Minimize(rex.CreateFromRegexes(r5));
            var sfa2 = rex.Intersect(a1, a2, a3, a4, a5);
            time = System.Environment.TickCount - time;

            string dag = rex.SerializeDAG(sfa2);
            var sfa3 = rex.DeserializeDAG(dag);
            //rex.Solver.ShowGraph(sfa3, "sfa");

            var timeToMin = System.Environment.TickCount;
            var sfa_min = sfa2.MinimizeMoore();
            timeToMin = System.Environment.TickCount - timeToMin;


            var timeToMinB = System.Environment.TickCount;
            var sfa_minB = sfa3.MinimizeHopcroft();
            timeToMinB = System.Environment.TickCount - timeToMinB;


            System.IO.File.WriteAllText("dag.txt", dag);

            //Assert.IsTrue(rex.AreEquivalent(sfa3, sfa_min));

            //rex.Solver.ShowGraph(sfa_min, "sfa_min");
            //rex.Solver.ShowGraph(sfa_minB, "sfa_minB");

            Assert.IsTrue(rex.AreEquivalent(sfa_minB, sfa_min));

            Assert.AreEqual(sfa_min.StateCount, sfa_minB.StateCount);

            Assert.IsTrue(rex.AreEquivalent(sfa3, sfa_minB));

            //rex.Solver.ShowGraph(sfa_min,"TestMinimization");

            Console.WriteLine("time:{0} size:{1} | timeToMin:{2} sizeMin:{3} | timeToMin:{4} sizeMin:{5}", time, sfa2.StateCount, timeToMin, sfa_min.StateCount, timeToMinB, sfa_minB.StateCount);
        }

        [TestMethod]
        public void TestMinimization_ClassicalHardCase()
        {
            var css = new CharSetSolver();
            var regex = "a.{13}$";
            var aut = css.Convert(regex, RegexOptions.Singleline).RemoveEpsilons().Determinize().Minimize();
            Assert.IsTrue(aut.StateCount == (1 << 14)); //i.e. 2^{14}
        }

        [TestMethod]
        public void TestMinimizationBugFix()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);

            var regex = "^0*(0|1)01$";
            var sfa = rex.CreateFromRegexes(regex).RemoveEpsilons();
            var sfa_det = sfa.Determinize();
            //rex.Solver.ShowGraph(sfa_det, "sfa_det");
            var sfa_min_cl = sfa_det.MinimizeMoore();
            //rex.Solver.ShowGraph(sfa_min_cl, "sfa_min_cl");

            var sfa_min = sfa_det.MinimizeHopcroft();
            //rex.Solver.ShowGraph(sfa_min, "sfa_min");

            Assert.IsTrue(rex.AreEquivalent(sfa, sfa_min_cl));
            Assert.IsTrue(rex.AreEquivalent(sfa, sfa_min));
            Assert.AreEqual(sfa_min_cl.StateCount, sfa_min.StateCount);

        }

        [TestMethod]
        public void TestMinimizationOfSampleICSTregexes()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            string[] regexes = SampleRegexes.regexes;
            for (int i = 0; i < regexes.Length; i++)
            {
                string regex = regexes[i];
                var sfa = rex.CreateFromRegexes(regex);
                var msfa = sfa.Minimize();
                //var msfa2 = sfa.Determinize(rex.Solver).MinimizeClassical(rex.Solver);
                //Assert.IsTrue(rex.AreEquivalent(sfa, msfa2));
                bool eq = rex.AreEquivalent(sfa, msfa);
                Assert.IsTrue(eq);
            }
        }

        [TestMethod]
        public void TestHardRegex1()
        {
            //string regex = @".*MRU.*|.*FilledAppTile.*|.*System.*Tiles.*|.*SnappedAppTile.*|.*Installed.*|.*Installing.*|.*ReadyToInstall.*";
            string regex = @".*viewingoption.*|.*gameclips.*|.*seasons.*|.*rottentomatocriticreview.*|.*rottentomatorating.*|.*showtimes.*|.*purchaseoptions.*|.*broadcasts.*|.*buttons.*|.*episodes.*";
            //string regex = @"MRU|FilledAppTile|System.*Tiles|SnappedAppTile|Installed|Installing|ReadyToInstall";
            CharSetSolver solver = new CharSetSolver();
            var aut = solver.Convert(regex, RegexOptions.Singleline);
            //var autEpsFree = aut.RemoveEpsilons(solver.MkOr);
            //autEpsFree.ShowGraph("autEpsFree");
            var autDet = aut.Determinize();
            var autMin = autDet.Minimize();
            //autMin.ShowGraph("autMin");
        }

        #region misc helper functions

        void MyClearOutput()
        {
            StreamWriter myOutputFile = new StreamWriter("C:/tmp/testOutputFromVS2010UnitTest.txt", false);
            myOutputFile.Write("");
            myOutputFile.Close();
        }

        void MyWriteLine(string pat, params object[] args)
        {
            StreamWriter sw = new StreamWriter("C:/tmp/testOutputFromVS2010UnitTest.txt", true);
            sw.WriteLine(pat, args);
            sw.Flush();
            sw.Close();
        }

        private static void ShowAsGraph<S>(Func<S,string> descr, Automaton<S> fa, string file)
        {
            //DirectedGraphs.DgmlWriter.AutomatonToDgml<S>(descr, fa, file);
            //OpenFileInVS(file);
        }

        private static void OpenFileInVS(string file)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo(file);
            p.StartInfo.WorkingDirectory = System.Environment.CurrentDirectory;
            p.Start();
        }

        #endregion
    } 
}
