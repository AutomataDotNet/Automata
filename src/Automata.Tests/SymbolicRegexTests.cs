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
            Assert.IsTrue(r2.Left.Kind == SymbolicRegexKind.Singleton);
            Assert.IsTrue(r2.Right.Kind == SymbolicRegexKind.Loop);
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
            Assert.IsTrue(r4.LowerBound == 0);
            Assert.IsTrue(r4.UpperBound == 5);
            //--------
            var a = css.RegexConverter.ConvertToSymbolicRegex("a");
            var bstar = css.RegexConverter.ConvertToSymbolicRegex("b*");
            var abstar = css.RegexConverter.MkConcat(a, bstar);
            Assert.IsTrue(abstar.ChoiceCount == 1);
            Assert.IsTrue(abstar.Kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(abstar.ToString() == "ab*");
            //--------
            var r5 = css.RegexConverter.ConvertToSymbolicRegex("[a-z]?");
            Assert.IsTrue(r5.IsOptional);
            Assert.IsTrue(!r5.IsStar);
            Assert.IsTrue(!r5.IsPlus);
            Assert.IsTrue(r5.LowerBound == 0 && r5.UpperBound == 1 && r5.Kind == SymbolicRegexKind.Loop);
            //--------
            var r6 = css.RegexConverter.ConvertToSymbolicRegex("[a-z]+");
            Assert.IsTrue(!r6.IsOptional);
            Assert.IsTrue(!r6.IsStar);
            Assert.IsTrue(r6.IsPlus);
            Assert.IsTrue(r6.LowerBound == 1 && r6.UpperBound == int.MaxValue && r6.Kind == SymbolicRegexKind.Loop);
            //--------
            var r7 = css.RegexConverter.ConvertToSymbolicRegex("[a-z]*");
            Assert.IsTrue(!r7.IsOptional);
            Assert.IsTrue(r7.IsStar);
            Assert.IsTrue(!r7.IsPlus);
            Assert.IsTrue(r7.LowerBound == 0 && r7.UpperBound == int.MaxValue && r7.Kind == SymbolicRegexKind.Loop);
        }
    }
}
