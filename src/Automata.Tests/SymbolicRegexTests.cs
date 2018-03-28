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
    [TestClass]
    public class SymbolicRegexTests
    {
        [TestMethod]
        public void TestSampleSymbolicRegexes()
        {
            CharSetSolver css = new CharSetSolver();
            var r1 = css.RegexConverter.ConvertToSymbolicRegex("abc");
            Assert.IsTrue(r1.ToString().Equals("abc"));
            //--------
            var r2 = css.RegexConverter.ConvertToSymbolicRegex(@"[\w][\p{Nd}]*");
            Assert.IsTrue(r2.ToString().Equals(@"[\w][\p{Nd}]*"));
            Assert.IsTrue(r2.Kind == SymbolicRegexKind.Concat);
            var wd1 = r2 as SymbolicRegexConcat;
            Assert.IsTrue(wd1.First.Kind == SymbolicRegexKind.Singleton);
            Assert.IsTrue(wd1.Second.Kind == SymbolicRegexKind.Loop);
            //--------
            var r3 = css.RegexConverter.ConvertToSymbolicRegex(@"a|((b)|(c)|(d))|[e-x]");
            Assert.IsTrue(r3.Kind == SymbolicRegexKind.Choice);
            Assert.IsTrue(r3.ChoiceCount == 5);
            Assert.IsTrue(r3.ToString().Equals(@"a|b|c|d|[e-x]"));
            //--------
            var r4 = css.RegexConverter.ConvertToSymbolicRegex(@"[a-z]{0,5}");
            Assert.IsTrue(r4.Kind == SymbolicRegexKind.Loop);
            Assert.IsTrue(r4.ChoiceCount == 1);
            Assert.IsTrue(r4.ToString().Equals(@"[a-z]{0,5}"));
            var r4l = r4 as SymbolicRegexLoop;
            Assert.IsTrue(r4l.LowerBound == 0);
            Assert.IsTrue(r4l.UpperBound == 5);
            //--------
            var a = css.RegexConverter.ConvertToSymbolicRegex("a");
            var bstar = css.RegexConverter.ConvertToSymbolicRegex("b*");
            var abstar = css.RegexConverter.MkConcat(a, bstar);
            Assert.IsTrue(abstar.ChoiceCount == 1);
            Assert.IsTrue(abstar.Kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(abstar.ToString() == "ab*");
        }
    }
}
