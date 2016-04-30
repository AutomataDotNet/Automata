using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Microsoft.Automata;
using Microsoft.Automata.Internal;
using Microsoft.Automata.Internal.Generated;
using Microsoft.Automata.Rex;
using Microsoft.Automata.Internal.Utilities;

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
            string a = "aaaaaaaaaaaaaaaaa";
            //takes time exponential in the length of a
            for (int i = 0; i < 15; i++)
            {
                int t = System.Environment.TickCount;
                EvilRegex.IsMatch(a);
                TestContext.WriteLine("{0}ms", System.Environment.TickCount - t);
                a += "a";
            }
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
    }
}
