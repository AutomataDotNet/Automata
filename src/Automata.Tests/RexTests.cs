using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

using Microsoft.Automata;

using System.Text.RegularExpressions;
using System.IO;

namespace Automata.Tests 
{
    /// <summary>
    /// Tests core functionality used by Rex 
    /// </summary>
    [TestClass]
    public class RexTests
    {

        static string regexesFile = "../../../samples/regexes.txt";
        public RexTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        //[TestMethod]
        public void TestSampleRegexesManyTimes()
        {
            for (int i = 0; i < 10; i++) TestSampleRegexes();
        }

        // "((www|http)(\\W+\\S+[^).,:;?\\]\\} \\r\\n$]+))"

        [TestMethod]
        public void TestWordLetters()
        {
            for (int i = 0; i <= 0xFFFF; i++)
            {
                char c = (char)i;
                UnicodeCategory cat = char.GetUnicodeCategory(c);
                int catCode = (int)cat;
                //is .net 3.5 catCode == 5 was NOT a wordletter
                bool isWordLetter = (catCode == 0 || catCode == 1 || catCode == 2 || catCode == 3 || catCode == 4 || catCode == 5 || catCode == 8 || catCode == 18);
                Assert.IsTrue(isWordLetter != Regex.IsMatch(c.ToString(), @"^\W$"), c.ToString());
            }
        }

        [TestMethod]
        public void TestLazyVsEagerLoop()
        {
            //eager case
            var r = new Regex(@"[a-z]*a+[^y]");
            var s = "000xxxaaayyaaazzzaaawww";
            var matches = r.Matches(s);
            Assert.IsTrue(matches.Count == 1);
            var m0 = matches[0];
            Assert.IsTrue(matches[0].Value == "xxxaaayyaaazzzaaaw");
            var sr_ = r.Compile();
            Automata.Tests.SerializationTests.SerializeObjectToFile_soap("test.soap", sr_);
            var sr = (IMatcher)Automata.Tests.SerializationTests.DeserializeObjectFromFile_soap("test.soap");
            var sr_matches = sr.Matches(s);
            Assert.IsTrue(sr_matches.Length == 1);
            Assert.IsTrue(s.Substring(sr_matches[0].Item1, sr_matches[0].Item2) == "xxxaaayyaaazzzaaaw");
            //lazy case
            var r1 = new Regex(@"[a-z]*?a+[^y]");
            var matches1 = r1.Matches(s);
            Assert.IsTrue(matches1.Count == 3);
        }


        [TestMethod]
        public void TestSampleRegexes()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            string[] regexes = File.ReadAllLines(regexesFile);
            List<int> exclude = new List<int>();
            exclude.Add(299);

            int k = (regexes.Length < 400 ? regexes.Length : 400);

            int rxCount = 10; //number of strings to be generated for each regex
            for (int i = 1; i < k; i++)
                foreach (string s in rex.GenerateMembers(RegexOptions.None, rxCount, regexes[i]))
                    if (!exclude.Contains(i))
                        if (!Microsoft.Automata.Rex.RexEngine.IsMatch(s, regexes[i], RegexOptions.None))
                            Assert.IsTrue(false, "regex on line " + i + " in Samples/regexes.txt");
        }

        [TestMethod]
        public void TestSampleRegexesInMultilineMode()
        {
            Microsoft.Automata.Rex.RexEngine rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            string[] regexes = File.ReadAllLines(regexesFile);
            List<int> exclude = new List<int>();
            exclude.Add(299);

            int rxCount = 10; //number of strings to be generated for each regex
            for (int i = 0; i < regexes.Length; i++)
                foreach (string s in rex.GenerateMembers(RegexOptions.Multiline, rxCount, regexes[i]))
                    if (!exclude.Contains(i))
                        Assert.IsTrue(Microsoft.Automata.Rex.RexEngine.IsMatch(s, regexes[i], RegexOptions.Multiline), "regex on line " + i + " in Samples/regexes.txt");
        }

        [TestMethod]
        public void TestPasswordRegex()
        {
            var r1 = @"^[\x21-\x7E]{8}$";
            var r2 = @"[A-Z].*[A-Z]";
            var r3 = "^.+[a-z].*[a-z]";
            var r4 = "^(...[@#$%&]|....[@#$%&]|.....[@#$%&])";
            var r5 = @"^[\D]";
            var r6 = "^[^AbkG]*$";

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);
            var sfa = rex.CreateFromRegexes(r1, r2, r3, r4, r5, r6);
            var sfa_min = rex.Minimize(sfa);

            var time = System.Environment.TickCount;

            List<string> samples = new List<string>();
            for (int i = 0; i < 1000; i++)
                samples.Add(rex.GenerateMemberUniformly(sfa_min));

            time = System.Environment.TickCount - time;
            Console.WriteLine("nr of SFA states: {1}, time to generate 1000 samples: {0}ms", time, sfa_min.StateCount);
        }

        [TestMethod]
        public void TestPasswAutGenPerf()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //rex for ascii range

            for (int passwlength = 10; passwlength <= 500; passwlength += 10)
            {

                var r1 = @"^[\x21-\x7E]{" + passwlength + "}$";
                var r2 = "[A-Z]";
                var r3 = "[a-z]";
                var r4 = "[0-9]";
                var r5 = @"[\x21-\x2F\x3A-\x40\x5B-\x60\x7B-\x7E]"; 

                var time2 = System.Environment.TickCount;
                var a1 = rex.Minimize(rex.CreateFromRegexes(r1));
                var a2 = rex.Minimize(rex.CreateFromRegexes(r2,r3,r4,r5));
                var sfa2 = rex.Intersect(a1, a2); //no need to minimize
                time2 = System.Environment.TickCount - time2;

                Console.WriteLine("passwlength:{0} sfasize:{1} time:{2}ms", passwlength, sfa2.StateCount, time2);
                var s = rex.GenerateMemberUniformly(sfa2);
                Assert.IsTrue(Regex.IsMatch(s, r1));
                Assert.IsTrue(Regex.IsMatch(s, r2));
                Assert.IsTrue(Regex.IsMatch(s, r3));
                Assert.IsTrue(Regex.IsMatch(s, r4));
                Assert.IsTrue(Regex.IsMatch(s, r5));
            }
        }

        [TestMethod]
        public void TestOrigPasswRegex()
        {

            for (int passwl = 5; passwl <= 200; passwl += 10)
            {
                int time = System.Environment.TickCount;

                var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);

                var r1 = @"^[\x21-\x7E]{" + passwl + "}$";
                var r2 = @"[A-Z].*[A-Z]";
                var r3 = "^.+[a-z].*[a-z]";
                var r4 = "^(...[@#$%&]|....[@#$%&]|.....[@#$%&])";
                var r5 = @"^[\D]";
                var r6 = "^[^AbkG]*$";

                var sfa1 = rex.Minimize(rex.CreateFromRegexes(r1));
                var sfa2 = rex.Minimize(rex.CreateFromRegexes(r2));
                var sfa3 = rex.Minimize(rex.CreateFromRegexes(r3));
                var sfa4 = rex.Minimize(rex.CreateFromRegexes(r4));
                var sfa5 = rex.Minimize(rex.CreateFromRegexes(r5));
                var sfa6 = rex.Minimize(rex.CreateFromRegexes(r6));
                var sfa = rex.Intersect(sfa1, sfa2, sfa3, sfa4, sfa5, sfa6);

                var s = rex.GenerateMemberUniformly(sfa);
                Assert.IsTrue(Regex.IsMatch(s, r1));
                Assert.IsTrue(Regex.IsMatch(s, r2));
                Assert.IsTrue(Regex.IsMatch(s, r3));
                Assert.IsTrue(Regex.IsMatch(s, r4));
                Assert.IsTrue(Regex.IsMatch(s, r5));
                Assert.IsTrue(Regex.IsMatch(s, r6));

                time = System.Environment.TickCount - time;

                Console.WriteLine("length:{0}  time:{1}  size:{2}", passwl, time, sfa.StateCount);
            }
        }

        [TestMethod]
        public void TestLongPasswordAutomatonCreationTime()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //rex for ascii range

            int passwlength = 200;

            var r1 = @"^[\x21-\x7E]{" + passwlength + "}$";
            var r2 = "[A-Z]";
            var r3 = "[a-z]";
            var r4 = "[0-9]";
            var r5 = @"[\x21-\x2F\x3A-\x40\x5B-\x60\x7B-\x7E]";

            var time = System.Environment.TickCount;
            //var a1 = rex.Minimize(rex.CreateFromRegexes(r1));
            var sfa2 = rex.Minimize(rex.CreateFromRegexes(r1, r2, r3, r4, r5));
            //var sfa2 = rex.Intersect(a1, a2); 
            time = System.Environment.TickCount - time;

            var timeToMin = System.Environment.TickCount;
            var sfa_min = rex.Minimize(sfa2);
            timeToMin = System.Environment.TickCount - timeToMin;

            string dag = rex.SerializeDAG(sfa2);
            var sfa3 = rex.DeserializeDAG(dag);
            System.IO.File.WriteAllText("dag.txt", dag);

            Assert.IsTrue(rex.AreEquivalent(sfa3, sfa2));

            Assert.IsTrue(rex.AreEquivalent(sfa3, sfa_min));

            Console.WriteLine("time:{0} size:{1} | timeToMin:{2} sizeMin:{3}", time, sfa2.StateCount, timeToMin, sfa_min.StateCount);
        }

        [TestMethod]
        public void TestDAGserialization()
        {
            var r1 = @"^[\x21-\x7E]{8}$";
            var r2 = @"[A-Z].*[A-Z]";
            var r3 = "^.+[a-z].*[a-z]";
            var r4 = "^(...[@#$%&]|....[@#$%&]|.....[@#$%&])";
            var r5 = @"^[\D]";
            var r6 = "^[^AbkG]*$";

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            var sfa = rex.CreateFromRegexes(r1, r2, r3, r4, r5, r6);
            var sfa_min = rex.Minimize(sfa);

            string dag = rex.SerializeDAG(sfa_min);
            var sfa_min1 = rex.DeserializeDAG(dag);

            bool equiv = rex.AreEquivalent(sfa_min, sfa_min1);

            Assert.IsTrue(equiv);

            string dag2 = rex.SerializeDAG(sfa_min1);

            Assert.AreEqual<string>(dag, dag2);

            Console.WriteLine(rex.GenerateMemberUniformly(sfa_min1));
            Console.WriteLine(rex.GenerateMemberUniformly(sfa_min1));
            Console.WriteLine(rex.GenerateMemberUniformly(sfa_min1));

            //rex.Solver.ShowGraph(sfa_min1, "test");
        }

        [TestMethod]
        public void TestUniformMemberGeneration()
        {
            var r1 = @"^(a|ab|abc|abcd|abcde)$";

            //var randomseed = 123;

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);
            var sfa = rex.CreateFromRegexes(RegexOptions.None, r1);
            var sfa_min = sfa.Determinize().MinimizeHopcroft();

            //rex.Solver.ShowGraph(sfa_min, "test");

            var time = System.Environment.TickCount;

            var frequency = new Dictionary<string, int>();
            frequency["a"]=0;
            frequency["ab"]=0;
            frequency["abc"]=0;
            frequency["abcd"]=0;
            frequency["abcde"]=0;
            for (int i = 0; i < 100000; i++)
            {
                var sample = rex.GenerateMemberUniformly(sfa_min);
                frequency[sample] += 1;
            }

            foreach (var kv in frequency)
            {
                Assert.IsTrue(kv.Value > 19500); //roughly, each one should be equally likely
            }

            var m1 = sfa_min.GetMoveFrom(sfa_min.InitialState);
            Assert.AreEqual<double>(1.0, sfa_min.GetProbability(m1));
            Assert.AreEqual<double>(0.2, sfa_min.GetProbability(m1.TargetState));
            var m2 = sfa_min.GetMoveFrom(m1.TargetState);
            Assert.AreEqual<double>(0.8, sfa_min.GetProbability(m2));
            Assert.AreEqual<double>(0.25, sfa_min.GetProbability(m2.TargetState));
            var m3 = sfa_min.GetMoveFrom(m2.TargetState);
            Assert.AreEqual<double>(0.75, sfa_min.GetProbability(m3));
            Assert.AreEqual<double>(Math.Round(((double)1.0)/3.0, 10), sfa_min.GetProbability(m3.TargetState));
            var m4 = sfa_min.GetMoveFrom(m3.TargetState); 
            Assert.AreEqual<double>(Math.Round(((double)2.0) / 3.0, 10), sfa_min.GetProbability(m4));
            Assert.AreEqual<double>(0.5, sfa_min.GetProbability(m4.TargetState));
            var m5 = sfa_min.GetMoveFrom(m4.TargetState);
            Assert.AreEqual<double>(0.5, sfa_min.GetProbability(m5));
            Assert.AreEqual<double>(1.0, sfa_min.GetProbability(m5.TargetState));

            //Assert.AreEqual<System.Numerics.BigInteger>(new System.Numerics.BigInteger(5), sfa_min.DomainSize);
        }

        [TestMethod]
        public void TestDag()
        {
            var r1 = @"^\w{2,3}$"; //between 2 and 3 word letters
            var r2 = @"\d"; //contains a digit

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //using ascii
            var sfa = rex.Minimize(rex.CreateFromRegexes(r1, r2));

            string dag = rex.SerializeDAG(sfa);
            var sfa1 = rex.DeserializeDAG(dag);

            //rex.Solver.ShowDAG(sfa1, "test");
        }

        [TestMethod]
        public void TestBDD_digit()
        {
            var d = @"^[0-9]$"; //between 2 and 3 word letters

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //using ascii
            var sfa = rex.Minimize(rex.CreateFromRegexes(d));

            var bdd = sfa.GetMoveFrom(sfa.InitialState).Label;

            //bdd.ToDot("C:/tmp/dot/digit.dot");
        }

        [TestMethod]
        public void TestBDD_evendigit()
        {
            var d = @"^[02468]$"; //between 2 and 3 word letters

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //using ascii
            var sfa = rex.Minimize(rex.CreateFromRegexes(d));

            var bdd = sfa.GetMoveFrom(sfa.InitialState).Label;

           // bdd.ToDot("C:/tmp/dot/evendigit.dot");
        }

        [TestMethod]
        public void TestBDD_odddigit()
        {
            var d = @"^[13579]$"; //between 2 and 3 word letters

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //using ascii
            var sfa = rex.Minimize(rex.CreateFromRegexes(d));

            var bdd = sfa.GetMoveFrom(sfa.InitialState).Label;

            //bdd.ToDot("C:/tmp/dot/odddigit.dot");
        }

        [TestMethod]
        public void TestBDD_uppercase()
        {
            var d = @"^[A-Z]$";

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //using ascii
            var sfa = rex.Minimize(rex.CreateFromRegexes(d));

            var bdd = sfa.GetMoveFrom(sfa.InitialState).Label;

            //bdd.ToDot("C:/tmp/dot/uppercase.dot");
        }

        [TestMethod]
        public void TestBDD_lowercase()
        {
            var d = @"^[a-z]$";

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //using ascii
            var sfa = rex.Minimize(rex.CreateFromRegexes(d));

            var bdd = sfa.GetMoveFrom(sfa.InitialState).Label;

            //bdd.ToDot("C:/tmp/dot/lowercase.dot");
        }

        [TestMethod]
        public void TestBDD_letter()
        {
            var d = @"^[A-Za-z]$";

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7); //using ascii
            var sfa = rex.Minimize(rex.CreateFromRegexes(d));

            var bdd = sfa.GetMoveFrom(sfa.InitialState).Label;

            //bdd.ToDot("C:/tmp/dot/letter.dot");
        }

        [TestMethod]
        public void TestDAGserializationSimple()
        {
            var r1 = @"^(a|ab|abc|abcd|abcde)$";

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);
            var sfa = rex.CreateFromRegexes(r1);
            var sfa_min_orig = rex.Minimize(sfa);

            string dag = rex.SerializeDAG(sfa_min_orig);
            var sfa_min = rex.DeserializeDAG(dag);

            //rex.Solver.ShowDAG(sfa_min, "test");

            var frequency = new Dictionary<string, int>();
            frequency["a"] = 0;
            frequency["ab"] = 0;
            frequency["abc"] = 0;
            frequency["abcd"] = 0;
            frequency["abcde"] = 0;
            for (int i = 0; i < 100000; i++)
            {
                var sample = rex.GenerateMemberUniformly(sfa_min);
                frequency[sample] += 1;
            }

            foreach (var kv in frequency)
            {
                Assert.IsTrue(kv.Value > 19500); //roughly, each one should be equally likely
            }

            var m1 = sfa_min.GetMoveFrom(sfa_min.InitialState);
            Assert.AreEqual<double>(1.0, sfa_min.GetProbability(m1));
            Assert.AreEqual<double>(0.2, sfa_min.GetProbability(m1.TargetState));
            var m2 = sfa_min.GetMoveFrom(m1.TargetState);
            Assert.AreEqual<double>(0.8, sfa_min.GetProbability(m2));
            Assert.AreEqual<double>(0.25, sfa_min.GetProbability(m2.TargetState));
            var m3 = sfa_min.GetMoveFrom(m2.TargetState);
            Assert.AreEqual<double>(0.75, sfa_min.GetProbability(m3));
            Assert.AreEqual<double>(Math.Round(((double)1.0) / 3.0, 10), sfa_min.GetProbability(m3.TargetState));
            var m4 = sfa_min.GetMoveFrom(m3.TargetState);
            Assert.AreEqual<double>(Math.Round(((double)2.0) / 3.0, 10), sfa_min.GetProbability(m4));
            Assert.AreEqual<double>(0.5, sfa_min.GetProbability(m4.TargetState));
            var m5 = sfa_min.GetMoveFrom(m4.TargetState);
            Assert.AreEqual<double>(0.5, sfa_min.GetProbability(m5));
            Assert.AreEqual<double>(1.0, sfa_min.GetProbability(m5.TargetState));
        }

        [TestMethod]
        public void TestDAGserializationSimple2()
        {
            var r1 = @"^([\d]{1,3})$"; //sequences of 1 up to 3 digits

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);
            var sfa = rex.CreateFromRegexes(r1);
            var sfa_min_orig = rex.Minimize(sfa);

            var sfa_min = rex.DeserializeDAG(rex.SerializeDAG(sfa_min_orig));

           // rex.Solver.ShowDAG(sfa_min, "test");

            var frequency = new Dictionary<string, int>();

            for (int i = 0; i < 10; i++)
                    frequency[i.ToString()] = 0;

            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                        frequency[i.ToString() + j.ToString()] = 0;

            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                    for (int k = 0; k < 10; k++)
                        frequency[i.ToString() + j.ToString() + k.ToString()] = 0;

            int time = System.Environment.TickCount;
            for (int i = 0; i < 1000000; i++)
            {
                var sample = rex.GenerateMemberUniformly(sfa_min);
                frequency[sample] += 1;
            }
            time = System.Environment.TickCount - time;

            int total = 10 + 100 + 1000;
            int avg = 1000000 / total;
            int avg_low = (avg * 80) / 100;
            int avg_high = (avg * 120) / 100;

            foreach (var kv in frequency)
            {
                Assert.IsTrue(kv.Value > avg_low);
                Assert.IsTrue(kv.Value < avg_high);
            }
        }

        [TestMethod]
        public void TestUniformMemberGeneration2()
        {
            var r1 = @"^(\d{3}|12b)$"; //three digits or 12bc
            var r2 = "^[0-2a-z]*$"; //only digits 0 1 2 or lower case letters are allowed

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);
            var sfa = rex.CreateFromRegexes(RegexOptions.None, r1, r2);
            var sfa_min = sfa.Determinize().MinimizeHopcroft();

            //rex.Solver.ShowGraph(sfa_min, "test");

            var frequency = new Dictionary<string, int>();
            for (int i = 0; i < 100000; i++)
            {
                var sample = rex.GenerateMemberUniformly(sfa_min);
                if (frequency.ContainsKey(sample))
                    frequency[sample] += 1;
                else
                    frequency[sample] = 1;
            }

            foreach (var kv in frequency)
            {
                Assert.IsTrue(kv.Value > 3000);
            }
        }

        [TestMethod]
        public void TestTopSort()
        {
            var r1 = @"^[\x21-\x7E]{5}$";
            var r2 = @"[A-Z].*[A-Z]";
            var r3 = "^.+[a-z].*[a-z]";
            var r4 = "^(.[@#$%&]|..[@#$%&]|...[@#$%&])";
            var r5 = @"^[\D]";
            var r6 = "^[^AbkG]*$";

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);
            var sfa = rex.CreateFromRegexes(RegexOptions.None, r1, r2, r3, r4, r5, r6);
            var sfa_min = rex.Minimize(sfa);

            //rex.Solver.ShowGraph(sfa_min, "test");

            var toposort = sfa_min.TopSort();
            Assert.AreEqual<int>(12, toposort.Count);
            Assert.IsTrue(sfa_min.IsLoopFree);
            Assert.IsTrue(sfa.IsLoopFree);
        }

        [TestMethod]
        public void TestIsLoopFree()
        {
            var r1 = "^abc+d$";

            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);
            var sfa = rex.CreateFromRegexes(RegexOptions.None, r1);
            var sfa_min = sfa.Determinize().MinimizeHopcroft();

            //rex.Solver.ShowGraph(sfa_min, "test");

            Assert.IsFalse(sfa.IsLoopFree);
            Assert.IsFalse(sfa_min.IsLoopFree);
        }

        [TestMethod]
        public void TestMinimizationAlgosOnSampleRegexes()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            string[] regexes = File.ReadAllLines(regexesFile);
            List<int> exclude = new List<int>();
            for (int i = 0; i < 25; i++)
            {
                string regex = regexes[i];
                var autom = rex.CreateFromRegexes(regex).Determinize();
                //rex.Solver.ShowGraph(autom, "autom");
                var automM = autom.MinimizeHopcroft();
                //rex.Solver.ShowGraph(automM, "automM");
                var automMC = autom.MinimizeMoore();
                //rex.Solver.ShowGraph(automMC, "automMC");
                //var automM2 = autom.Minimize2(rex.Solver);
                //rex.Solver.ShowGraph(automM2, "automM2");
                Assert.AreEqual<int>(automMC.StateCount, automM.StateCount);
                Assert.AreEqual<int>(automMC.MoveCount, automM.MoveCount);
            }
        }


        [TestMethod]
        public void TestSampleRegex()
        {
            //string regex = @"^A([\0-\x20]B)+[\0-\x08]C$";
            //string input = "A\u0014B\u0019B\u0020B\u0004C";
            var ok = System.Text.RegularExpressions.Regex.IsMatch("A\u0014B\u0019B\u0020B\u0004C", @"^A([\0-\x20]B)+[\0-\x08]C$");
            Assert.IsTrue(ok);
            //var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            //var aut = rex.CreateFromRegexes(regex).Determinize(rex.Solver).Minimize(rex.Solver);
            //rex.Solver.ShowGraph(aut, "aut");
        }

        [TestMethod]
        public void TestPasswSample()
        {

            for (int passwl = 4; passwl <= 10; passwl += 5)
            {
                int time = System.Environment.TickCount;

                var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);

                var r1 = @"^[\x21-\x7E]{" + passwl + "}$";
                var r2 = @"[a-zA-Z].*[a-zA-Z]";
                var r4 = @"\d";
                var r5 = @"\W";

                var sfa1 = rex.CreateFromRegexes(r1);
                var sfa2 = rex.CreateFromRegexes(r2);
                //var sfa3 = rex.CreateFromRegexes(r3);
                var sfa4 = rex.CreateFromRegexes(r4);
                var sfa5 = rex.CreateFromRegexes(r5);
                var sfa = rex.Intersect(sfa1, sfa2, sfa4, sfa5).Determinize();

                //rex.Solver.ShowGraph(sfa, "sfa");

                time = System.Environment.TickCount - time;

                int timeH = System.Environment.TickCount; 
                var sfaH = sfa.MinimizeHopcroft();
                timeH = System.Environment.TickCount - timeH;

                //rex.Solver.ShowGraph(sfaH, "sfaH");

                int timeM = System.Environment.TickCount; 
                var sfaM = sfa.MinimizeMoore();
                timeM = System.Environment.TickCount - timeM;

                Console.WriteLine("length:{0}  creation time:{1} size:{2}  min(H):{3} min(M):{4} minsize:{5}", passwl, time, sfa.StateCount, timeH, timeM, sfaH.StateCount );

            }
        }

        [TestMethod]
        public void TestMinimizationAlgosOnSampleRegex()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);
            var r = "^((0[01])3|(1[012]3))$";
            var autom = rex.CreateFromRegexes(r).Determinize();//.MakeTotal(rex.Solver);
            //rex.Solver.ShowGraph(autom, "autom");
            var automM = autom.MinimizeHopcroft();
            //rex.Solver.ShowGraph(automM, "automM");
            var automM3 = autom.Minimize();
            //rex.Solver.ShowGraph(automM3, "automM3");
            Assert.AreEqual<int>(automM.StateCount, automM3.StateCount);
            Assert.IsTrue(automM3.IsEquivalentWith(automM));
        }

        [TestMethod]
        public void SampleRegex()
        {
            string regex = "[NS]\\d{1,}(\\:[0-5]\\d){2}.{0,1}\\d{0,},[EW]\\d{1,}(\\:[0-5]\\d){2}.{0,1}\\d{0,}";
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            var autom = rex.CreateFromRegexes(RegexOptions.Singleline, regex).Determinize();//.MakeTotal(rex.Solver);
            autom.CheckDeterminism(true);
            Assert.IsTrue(autom.IsDeterministic, "autom must be deterministic");
            //rex.Solver.ShowGraph(autom, "autom");
            var automM3 = autom.Minimize();
            //rex.Solver.ShowGraph(automM3, "automM3");
            var automM = autom.MinimizeHopcroft();
            var same = automM3.IsEquivalentWith(automM) && automM3.StateCount == automM.StateCount;
            Assert.IsTrue(same);
        }

        [TestMethod]
        public void TestMinimizationForRegexCausingBug()
        {
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV7);
            var regex = "^.*(([01]|[1]0)0)$";
            var autom0 = rex.CreateFromRegexes(RegexOptions.Singleline, regex).RemoveEpsilons();
            var autom = autom0.Determinize();
            //rex.Solver.ShowGraph(autom0, "NFA");
            //rex.Solver.ShowGraph(autom, "DFA");
            autom.CheckDeterminism(true);
            Assert.IsTrue(autom.IsDeterministic, "autom must be deterministic here");

            var automM = RunMinimize1(rex, autom);
            var automM2 = RunMinimize2(rex, autom);
            var automM3 = RunMinimize3(rex, autom);
            //rex.Solver.ShowGraph(automM, "automM");
            //rex.Solver.ShowGraph(automM2, "automM2");
            //rex.Solver.ShowGraph(automM3, "automM3");
            var s = automM.StateCount;
            var m = automM.MoveCount;
            var s2 = automM2.StateCount;
            var m2 = automM2.MoveCount;
            var s3 = automM3.StateCount;
            var m3 = automM3.MoveCount;
            Assert.IsTrue(rex.AreEquivalent(autom, automM2));
            Assert.IsTrue(rex.AreEquivalent(autom, automM3));
            Assert.IsTrue(rex.AreEquivalent(autom, automM));
            Assert.AreEqual<int>(s, s2);
            Assert.AreEqual<int>(m, m2);
            Assert.AreEqual<int>(s, s3);
            Assert.AreEqual<int>(m, m3);
        }


        [TestMethod]
        public void TestMinAlgos()
        {
            string[] regexes = File.ReadAllLines(regexesFile);
            //first create deterministic SFAs for all the regexes
            //set a timeout of 2sec fo determinization
            var SFAs = new List<Automaton<BDD>>();
            var SFAs1 = new List<Automaton<BDD>>();
            var SFAs2 = new List<Automaton<BDD>>();
            var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
            var exclude = new HashSet<int>(new int[]{36,64,65,162,166,210,355,455,490,594,671,725,741,760,800,852,870,873,880,893,991,997});
            int i = 0;
            int minstates = int.MaxValue;
            int maxstates = 0;
            int totStates = 0;
            int K = 50; //only for fifty first regexes
            while (SFAs.Count < K)
            {
                if (!exclude.Contains(i))
                {
                    var regex = regexes[i];
                    var autom = rex.CreateFromRegexes(RegexOptions.Singleline, regex).Determinize();
                    SFAs.Add(autom);
                    minstates = Math.Min(minstates, autom.StateCount);
                    maxstates = Math.Max(maxstates, autom.StateCount);
                    totStates += autom.StateCount;
                }
                i += 1;
            }
            var time2 = System.Environment.TickCount;
            foreach (var sfa in SFAs)
            {
                SFAs2.Add(sfa.MinimizeMoore());
            }
            time2 = System.Environment.TickCount - time2;
            var time1 = System.Environment.TickCount;
            foreach (var sfa in SFAs)
            {
                SFAs1.Add(sfa.MinimizeHopcroft());
            }
            time1 = System.Environment.TickCount - time1 ;
            for (int j = 0; j < K; j++)
            {
                Assert.AreEqual<int>(SFAs2[j].StateCount, SFAs1[j].StateCount);
                Assert.AreEqual<int>(SFAs2[j].MoveCount, SFAs1[j].MoveCount);
            }
        }

        int RunMinimizeTest()
        {
            stateCount =-1;
            time1 = 0;
            time2 = 0;
            System.Threading.Timer timer = new System.Threading.Timer(KillTest, null, 1000, 1000);
            System.Threading.ThreadStart test = new System.Threading.ThreadStart(RunMinimize);
            currTest = new System.Threading.Thread(test);
            currTest.Start();
            while (stateCount == -1)
            {
                System.Threading.Thread.Sleep(50);
            }
            timer.Dispose();
            return stateCount;
        }

        System.Threading.Thread currTest;
        void KillTest(object state)
        {
            currTest.Abort();
        }

        int stateCount =-1;
        int origStateCount = -1;
        int time1 = 0;
        int time2 = 0;
        int time3 = 0;
        //int regexId;
        string regex = "dummy";
        void RunMinimize()
        {
            try
            {
                var rex = new Microsoft.Automata.Rex.RexEngine(BitWidth.BV16);
                var autom = rex.CreateFromRegexes(RegexOptions.Singleline, regex).Determinize();
                
                //---------------------
                var automM = RunMinimize1(rex, autom);
                //---------------------
                //---------------------
                var automM2 = RunMinimize2(rex, autom);
                //---------------------
                //---------------------
                var automM3 = RunMinimize3(rex, autom);
                //---------------------
                var s = automM.StateCount;
                var m = automM.MoveCount;
                var s2 = automM2.StateCount;
                var m2 = automM2.MoveCount;
                var s3 = automM3.StateCount;
                var m3 = automM3.MoveCount;
                if (s != s2)
                    stateCount = -3;
                else if (m != m2)
                    stateCount = -3;
                else if (s != s3)
                    stateCount = -3;
                else if (m != m3)
                    stateCount = -3;
                else if (!rex.AreEquivalent(autom, automM))
                    stateCount = -3;
                else if (!rex.AreEquivalent(autom, automM2))
                    stateCount = -3;
                else if (!rex.AreEquivalent(autom, automM3))
                    stateCount = -3;
                else
                {
                    stateCount = automM2.StateCount;
                    origStateCount = autom.StateCount;
                }
            }
            catch 
            {
                stateCount = -2;
            }
        }

        private Automaton<BDD> RunMinimize1(Microsoft.Automata.Rex.RexEngine rex, Automaton<BDD> autom)
        {
            var t = System.Environment.TickCount;
            Automaton<BDD> automM = null;
            for (int i = 0; i < 1; i++)
                automM = autom.MinimizeMoore();
            time1 = System.Environment.TickCount - t;
            return automM;
        }

        private Automaton<BDD> RunMinimize2(Microsoft.Automata.Rex.RexEngine rex, Automaton<BDD> autom)
        {
            var t = System.Environment.TickCount;
            Automaton<BDD> automM = null;
            for (int i = 0; i < 1; i++)
                automM = autom.MinimizeHopcroft();
            time2 = System.Environment.TickCount - t;
            return automM;
        }

        private Automaton<BDD> RunMinimize3(Microsoft.Automata.Rex.RexEngine rex, Automaton<BDD> autom)
        {
            var t2 = System.Environment.TickCount;
            Automaton<BDD> automM = null;
            for (int i = 0; i < 1; i++)
                automM = autom.Minimize();
            time3 = System.Environment.TickCount - t2;
            return automM;
        }

        [TestMethod]
        public void TestRegexFromTodd()
        {
            var regex = @"^.*[a-zA-Z][a-zA-Z0-9.\-_]{5,10}$";
            var solver = new CharSetSolver(BitWidth.BV16);
            var fat = solver.Convert(regex, System.Text.RegularExpressions.RegexOptions.Singleline);
            //solver.ShowGraph(fat, "fat");
            var fat1 = fat.Determinize();
            //solver.ShowGraph(fat1, "fat1");
            var fat2 = fat1.Minimize();
            //solver.ShowGraph(fat2, "fat2");
            Assert.IsTrue(fat1.IsEquivalentWith(fat2));
        }

        [TestMethod]
        public void TestCharClassRepetitionToSFA()
        {
            var regex = @"^.*\d\w{5,7}$";
            var solver = new CharSetSolver(BitWidth.BV7);
            var fat = solver.Convert(regex, System.Text.RegularExpressions.RegexOptions.Singleline);
            //solver.ShowGraph(fat, "fat");
            var fat1 = fat.Determinize();
            //solver.ShowGraph(fat1, "fat1");
            var fat2 = fat1.Minimize();
            //solver.ShowGraph(fat2, "fat2");
            Assert.IsTrue(fat1.IsEquivalentWith(fat2));
        }
    }
}
