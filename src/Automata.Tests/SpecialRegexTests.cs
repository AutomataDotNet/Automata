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

namespace Automata.Tests
{
    public class MyTestContext : TestContext
    {
        public MyTestContext()
        {
        }

        //to be able to prit to Immediate window in Debug mode
        //outside this wrapper Debug.WriteLine does not work
        public override void WriteLine(string format, params object[] args)
        {
            //Produces output in Immediate Window with "Debug Selected Tests" in Release mode as well as Debug mode
            System.Diagnostics.Trace.WriteLine(string.Format(format, args));

            //Produces output in Immediate Window with "Debug Selected Tests" only in Debug mode
            //System.Diagnostics.Debug.WriteLine(format, args);
        }


        public override void AddResultFile(string fileName)
        {
            throw new NotImplementedException();
        }

        public override void BeginTimer(string timerName)
        {
            throw new NotImplementedException();
        }

        public override System.Data.Common.DbConnection DataConnection
        {
            get { throw new NotImplementedException(); }
        }

        public override System.Data.DataRow DataRow
        {
            get { throw new NotImplementedException(); }
        }

        public override void EndTimer(string timerName)
        {
            throw new NotImplementedException();
        }

        public override System.Collections.IDictionary Properties
        {
            get { throw new NotImplementedException(); }
        }
    }

    [TestClass]
    public class SpecialRegexTests
    {
        [TestInitialize()]
        public void MyTestInitialize()
        {
            TestContext = new MyTestContext();
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

        [TestMethod]
        public void TestEvilRegex()
        {
            Regex EvilRegex = new Regex(@"^(([a-z])+.)+[A-Z]([a-z])+$", RegexOptions.Compiled | (RegexOptions.Singleline));
            string a = "aaaaaaaaaaaaaaaaaaaa";
            //takes time exponential in the length of a
            int t = 0;
            for (int i = 0; i < 10; i++)
            {
                t = System.Environment.TickCount;
                EvilRegex.IsMatch(a);
                t = System.Environment.TickCount - t;
                TestContext.WriteLine("{0}, {1}", a.Length, t);
                a += "a";
            }
            Assert.IsTrue(t > 100);
        }

        [TestMethod]
        public void TestEvilRegex2()
        {
            Regex EvilRegex = new Regex(@"^(([^\0])+.)+[\0]([^\0])+$", RegexOptions.Compiled | (RegexOptions.Singleline));
            string a = "text....with .....xxx...chars";
            //takes time exponential in the length of a
            int t = 0;
            for (int i = 0; i < 2; i++)
            {
                t = System.Environment.TickCount;
                bool res = EvilRegex.IsMatch(a);
                t = System.Environment.TickCount - t;
                TestContext.WriteLine("{0}, {1}", a.Length, t);
                a += "a";
            }
            Assert.IsTrue(t > 100);
        }

        [TestMethod]
        public void TestIgnoreCaseBug()
        {
            //ignore case option has implementation errors
            //r1 and r2 should be equivalent
            string r1 = @"^(?i:[\xD7-\xD8])$";
            string r2 = @"^(?i:[\xD7\xD8])$";
            var solver = new CharSetSolver();
            Assert.IsTrue(Regex.IsMatch("\xF7", r1));  //<--- ERROR
            Assert.IsFalse(Regex.IsMatch("\xF7", r2)); //<--- CORRECT
            //var a1 = solver.Convert(r1);
            //var a2 = solver.Convert(r2);
            //solver.ShowGraph(a1, "a1");
            //solver.ShowGraph(a2, "a2");
            //Assert.IsFalse(solver.Accepts(a1, "\xD7"));
            //Assert.IsTrue(solver.Accepts(a2, "\xD7"));
        }

        [TestMethod]
        public void TestMatchTermination()
        {
            //exponetial time
            string[] s = new string[] {
                "_a_aa_aa_aa@",
                "_a_aa_aa__aa@",
                "_a_aa_aa___aa@",
                "_a_aa_aa____aa@",
                //"_a_aa_aa_____aa@",
                //"_a_aa_aa______aa@", //takes 1 min!!!
            };
            var regex = new Regex("^(_?a?_?a?_?)+$", RegexOptions.Compiled);
            int t = 0;
            for (int i = 0; i < s.Length; i++)
            {
                t = System.Environment.TickCount;
                bool res = regex.IsMatch(s[i]);
                t = System.Environment.TickCount - t;
                TestContext.WriteLine("{0}, {1}", s[i].Length, t);
                Assert.IsFalse(res);
            }
            Assert.IsTrue(t > 1000);
        }

        [TestMethod]
        public void TestPerformanceRegressionOfBigChoice()
        {
            CharSetSolver css = new CharSetSolver();
            Regex testregex = new Regex(@"(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eightteen|nineteen|twenty)", RegexOptions.IgnoreCase);
            var aut = css.Convert(testregex.ToString(), testregex.Options);

            var matcher = aut.Determinize().Minimize().Compile();
            var regexc = testregex.Compile();
            //aut.ShowGraph();
        }

        [TestMethod]
        public void TestRegexComplement()
        {
            CharSetSolver css = new CharSetSolver();
            Regex L1 = new Regex("^(([^>]|><)*(>|>[^<].*))$", RegexOptions.Singleline);
            var A1 = css.Convert(L1.ToString(), L1.Options);
            Regex L1n = new Regex("^([^>]([^>]|><)*(>|>[^<].*))$", RegexOptions.Singleline);
            var A1n = css.Convert(L1n.ToString(), L1n.Options);
            var A3 = A1.Minus(A1n).Minimize();
            var L3 = css.ConvertToRegex(A3);
            Console.WriteLine(L3);
        }

        [TestMethod]
        public void TestAnchors()
        {
            Regex r = new Regex("^AA$|^BB$|^$", RegexOptions.Multiline);
            string input = "xxx\nAA\nBB\n";
            var matches = r.Matches(input);
            var match0 = matches[0];
            var match1 = matches[1];
            var match2 = matches[2];
            var match3 = matches[3];
            Console.WriteLine(match0.Value);
        }

        [TestMethod]
        public void TestOptions()
        {
            Regex r = new Regex("(?i:a)b", RegexOptions.Multiline);
            string input = "xxx\nAb\naB\n";
            var matches = r.Matches(input);
            var match0 = matches[0];
            Console.WriteLine(match0.Value);
        }
    }
}
