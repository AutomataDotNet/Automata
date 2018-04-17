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
        public void TestSampleSymbolicRegexesBasicConstructs()
        {
            CharSetSolver css = new CharSetSolver(BitWidth.BV16);
            var r0 = css.RegexConverter.ConvertToSymbolicRegex("");
            Assert.IsTrue(r0.ToString().Equals(".*"));
            //--------
            var r0a = css.RegexConverter.ConvertToSymbolicRegex("^");
            Assert.IsTrue(r0a.ToString().Equals(".*"));
            //--------
            var r0b = css.RegexConverter.ConvertToSymbolicRegex("$");
            Assert.IsTrue(r0b.ToString().Equals(".*"));
            //--------
            var r0e = css.RegexConverter.ConvertToSymbolicRegex("^$");
            Assert.IsTrue(r0e.ToString().Equals("()"));
            //--------
            var r1 = css.RegexConverter.ConvertToSymbolicRegex("^abc$");
            Assert.IsTrue(r1.ToString().Equals("abc"));
            //--------
            var r1b = css.RegexConverter.ConvertToSymbolicRegex("^abc");
            Assert.IsTrue(r1b.ToString().Equals("abc.*"));
            //--------
            var r1c = css.RegexConverter.ConvertToSymbolicRegex("abc$");
            Assert.IsTrue(r1c.ToString().Equals(".*abc"));
            //--------
            var r2 = css.RegexConverter.ConvertToSymbolicRegex(@"^\w\d*$");
            Assert.IsTrue(r2.ToString().Equals(@"\w\d*"));
            Assert.IsTrue(r2.Kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(r2.Left.Kind == SymbolicRegexKind.Singleton);
            Assert.IsTrue(r2.Right.Kind == SymbolicRegexKind.Loop);
            //--------
            var r3 = css.RegexConverter.ConvertToSymbolicRegex(@"^(a|((b)|(c)|(d))|[e-x])$");
            Assert.IsTrue(r3.Kind == SymbolicRegexKind.Or);
            Assert.IsTrue(r3.OrCount == 5);
            Assert.IsTrue(r3.ToString().Equals(@"a|b|c|d|[e-x]"));
            //--------
            var r4 = css.RegexConverter.ConvertToSymbolicRegex(@"^[a-x]{0,5}");
            Assert.IsTrue(r4.Kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(r4.OrCount == 1);
            Assert.IsTrue(r4.ToString().Equals(@"[a-x]{0,5}.*"));
            Assert.IsTrue(r4.Left.LowerBound == 0);
            Assert.IsTrue(r4.Left.UpperBound == 5);
            //--------
            var a = css.RegexConverter.ConvertToSymbolicRegex("^a$");
            var bstar = css.RegexConverter.ConvertToSymbolicRegex("^b*$");
            var cplus = css.RegexConverter.ConvertToSymbolicRegex("^c+$");
            var abc = css.RegexConverter.MkOr(a, bstar, cplus);
            Assert.IsTrue(abc.OrCount == 3);
            Assert.IsTrue(abc.Kind == SymbolicRegexKind.Or);
            Assert.IsTrue(abc.ToString() == "a|b*|c+");
            //--------
            var r5 = css.RegexConverter.ConvertToSymbolicRegex("^[a-z]?$");
            Assert.IsTrue(r5.IsMaybe);
            Assert.IsTrue(!r5.IsStar);
            Assert.IsTrue(!r5.IsPlus);
            Assert.IsTrue(r5.LowerBound == 0 && r5.UpperBound == 1 && r5.Kind == SymbolicRegexKind.Loop);
            //--------
            var r6 = css.RegexConverter.ConvertToSymbolicRegex("^[a-z]+$");
            Assert.IsTrue(!r6.IsMaybe);
            Assert.IsTrue(!r6.IsStar);
            Assert.IsTrue(r6.IsPlus);
            Assert.IsTrue(r6.LowerBound == 1 && r6.UpperBound == int.MaxValue && r6.Kind == SymbolicRegexKind.Loop);
            //--------
            var r7 = css.RegexConverter.ConvertToSymbolicRegex("^[a-[a]]*$");
            Assert.IsTrue(!r7.IsMaybe);
            Assert.IsTrue(r7.IsStar);
            Assert.IsTrue(!r7.IsPlus);
            Assert.IsTrue(r7.LowerBound == 0 && r7.UpperBound == int.MaxValue && r7.Kind == SymbolicRegexKind.Loop);
            //---------
            var r8 = css.RegexConverter.ConvertToSymbolicRegex(@"(?(.*A.*).*B.*|.*C.*)", RegexOptions.Singleline);
            Assert.IsTrue(r8.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(.*A.*).*B.*|.*C.*)", r8.ToString());
            //---------
            var r8b = css.RegexConverter.ConvertToSymbolicRegex(@"(?(A)B|C)", RegexOptions.Singleline);
            Assert.IsTrue(r8b.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(.*A.*).*B.*|.*C.*)", r8b.ToString());
            //---------
            var r8c = css.RegexConverter.ConvertToSymbolicRegex(@"^(?(A)B|C)", RegexOptions.Singleline);
            Assert.IsTrue(r8c.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(A.*)B.*|C.*)", r8c.ToString());
            //---------
            var r8d = css.RegexConverter.ConvertToSymbolicRegex(@"(?(A)B|C)$", RegexOptions.Singleline);
            Assert.IsTrue(r8d.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(.*A).*B|.*C)", r8d.ToString());
            //--------
            var r9 = css.RegexConverter.ConvertToSymbolicRegex(@"()()", RegexOptions.Singleline);
            Assert.IsTrue(r9.Kind == SymbolicRegexKind.Loop);
            Assert.AreEqual<string>(@".*", r9.ToString());
            //-----
            var a_complement = css.RegexConverter.ConvertToSymbolicRegex(@"(?(a)|[0-[0]])", RegexOptions.Singleline);
            Assert.IsTrue(a_complement.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(.*a.*).*|[0-[0]])", a_complement.ToString());
            //-----
        }

        [TestMethod]
        public void TestSampleSymbolicRegexesBasicConstructsKeepAnchors()
        {
            CharSetSolver css = new CharSetSolver(BitWidth.BV16);
            var r0 = css.RegexConverter.ConvertToSymbolicRegex("",RegexOptions.None,true);
            Assert.IsTrue(r0.ToString().Equals("()"));
            //--------
            var r0a = css.RegexConverter.ConvertToSymbolicRegex("^", RegexOptions.None, true);
            Assert.IsTrue(r0a.ToString().Equals("^"));
            //--------
            var r0b = css.RegexConverter.ConvertToSymbolicRegex("$", RegexOptions.None, true);
            Assert.IsTrue(r0b.ToString().Equals("$"));
            //--------
            var r0e = css.RegexConverter.ConvertToSymbolicRegex("^$", RegexOptions.None, true);
            Assert.IsTrue(r0e.ToString().Equals("^$"));
            //--------
            var r1 = css.RegexConverter.ConvertToSymbolicRegex("^abc$", RegexOptions.None, true);
            Assert.IsTrue(r1.ToString().Equals("^abc$"));
            //--------
            var r1b = css.RegexConverter.ConvertToSymbolicRegex("^abc", RegexOptions.None, true);
            Assert.IsTrue(r1b.ToString().Equals("^abc"));
            //--------
            var r1c = css.RegexConverter.ConvertToSymbolicRegex("abc$", RegexOptions.None, true);
            Assert.IsTrue(r1c.ToString().Equals("abc$"));
            //--------
            var r2 = css.RegexConverter.ConvertToSymbolicRegex(@"^\w\d*$", RegexOptions.None, true);
            Assert.IsTrue(r2.ToString().Equals(@"^\w\d*$"));
            Assert.IsTrue(r2.Kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(r2.Left.Kind == SymbolicRegexKind.StartAnchor);
            Assert.IsTrue(r2.Right.Kind == SymbolicRegexKind.Concat);
            //--------
            var r3 = css.RegexConverter.ConvertToSymbolicRegex(@"^(a|((b)|(c)|(d))|[e-x])$", RegexOptions.None, true);
            Assert.IsTrue(r3.Right.Left.Kind == SymbolicRegexKind.Or);
            Assert.IsTrue(r3.Right.Left.OrCount == 5);
            Assert.IsTrue(r3.ToString().Equals(@"^(a|b|c|d|[e-x])$"));
            //--------
            var r4 = css.RegexConverter.ConvertToSymbolicRegex(@"^[a-x]{0,5}", RegexOptions.None, true);
            Assert.IsTrue(r4.Kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(r4.OrCount == 1);
            Assert.IsTrue(r4.ToString().Equals(@"^[a-x]{0,5}"));
            Assert.IsTrue(r4.Left.IsSartAnchor);
            //--------
            var a = css.RegexConverter.ConvertToSymbolicRegex("a", RegexOptions.None, true);
            var bstar = css.RegexConverter.ConvertToSymbolicRegex("b*", RegexOptions.None, true);
            var cplus = css.RegexConverter.ConvertToSymbolicRegex("c+", RegexOptions.None, true);
            var abc = css.RegexConverter.MkOr(a, bstar, cplus);
            Assert.IsTrue(abc.OrCount == 3);
            Assert.IsTrue(abc.Kind == SymbolicRegexKind.Or);
            Assert.IsTrue(abc.ToString() == "a|b*|c+");
            //--------
            var r5 = css.RegexConverter.ConvertToSymbolicRegex("[a-z]?", RegexOptions.None, true);
            Assert.IsTrue(r5.IsMaybe);
            Assert.IsTrue(!r5.IsStar);
            Assert.IsTrue(!r5.IsPlus);
            Assert.IsTrue(r5.LowerBound == 0 && r5.UpperBound == 1 && r5.Kind == SymbolicRegexKind.Loop);
            //--------
            var r6 = css.RegexConverter.ConvertToSymbolicRegex("[a-z]+", RegexOptions.None, true);
            Assert.IsTrue(!r6.IsMaybe);
            Assert.IsTrue(!r6.IsStar);
            Assert.IsTrue(r6.IsPlus);
            Assert.IsTrue(r6.LowerBound == 1 && r6.UpperBound == int.MaxValue && r6.Kind == SymbolicRegexKind.Loop);
            //--------
            var r7 = css.RegexConverter.ConvertToSymbolicRegex("[a-[a]]*", RegexOptions.None, true);
            Assert.IsTrue(!r7.IsMaybe);
            Assert.IsTrue(r7.IsStar);
            Assert.IsTrue(!r7.IsPlus);
            Assert.IsTrue(r7.LowerBound == 0 && r7.UpperBound == int.MaxValue && r7.Kind == SymbolicRegexKind.Loop);
            //---------
            var r8 = css.RegexConverter.ConvertToSymbolicRegex(@"(?(.*A.*).*B.*|.*C.*)", RegexOptions.Singleline, true);
            Assert.IsTrue(r8.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(.*A.*).*B.*|.*C.*)", r8.ToString());
            //---------
            var r8b = css.RegexConverter.ConvertToSymbolicRegex(@"(?(A)B|C)", RegexOptions.Singleline, true);
            Assert.IsTrue(r8b.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(A)B|C)", r8b.ToString());
            //---------
            var r8c = css.RegexConverter.ConvertToSymbolicRegex(@"^(?(A)B|C)", RegexOptions.Singleline, true);
            Assert.IsTrue(r8c.Right.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"^(?(A)B|C)", r8c.ToString());
            //---------
            var r8d = css.RegexConverter.ConvertToSymbolicRegex(@"(?(A)B|C)$", RegexOptions.Singleline, true);
            Assert.IsTrue(r8d.Left.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(A)B|C)$", r8d.ToString());
            //--------
            var r9 = css.RegexConverter.ConvertToSymbolicRegex(@"()()", RegexOptions.Singleline, true);
            Assert.IsTrue(r9.Kind == SymbolicRegexKind.Epsilon);
            Assert.AreEqual<string>(@"()", r9.ToString());
            //-----
            var a_complement = css.RegexConverter.ConvertToSymbolicRegex(@"(?(a)[1-[1]]|.*)", RegexOptions.Singleline,true);
            Assert.IsTrue(a_complement.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(a)[0-[0]]|.*)", a_complement.ToString());
            //-----
        }

        [TestMethod]
        public void TestSampleSymbolicRegexesWithMisplacedAnchors()
        {
            CharSetSolver css = new CharSetSolver(BitWidth.BV7);
            var regex = new Regex("$.+", RegexOptions.Singleline);
            Assert.IsFalse(regex.IsMatch(""));
            Assert.IsFalse(regex.IsMatch("ab"));
            var sregex = css.RegexConverter.ConvertToSymbolicRegex(regex, true);
            Assert.AreEqual(sregex.ToString(), "$.+");
        }

        [TestMethod]
        public void TestSampleSymbolicRegexesMiscOptions()
        {
            CharSetSolver css = new CharSetSolver(BitWidth.BV16);
            //--- ignore case
            var r0 = css.RegexConverter.ConvertToSymbolicRegex("^foo$", RegexOptions.IgnoreCase);
            Assert.IsTrue(r0.ToString().Equals("[Ff][Oo][Oo]"));
            //--- ignore case locally
            var r0b = css.RegexConverter.ConvertToSymbolicRegex("^f(?i:o)o$");
            Assert.IsTrue(r0b.ToString().Equals("f[Oo]o"));
            //--- multiline option
            var r1 = css.RegexConverter.ConvertToSymbolicRegex(@"^foo\Z",RegexOptions.Multiline);
            //var a1 = css.RegexConverter.Convert(@"^foo\Z", RegexOptions.Multiline);
            //a1.ShowGraph("a1");
            Assert.IsTrue(r1.ToString().Equals(@"(.*\n)?foo"));
            //--------
            var r2 = css.RegexConverter.ConvertToSymbolicRegex(@"^", RegexOptions.Multiline);
            //var a2 = css.RegexConverter.Convert(@"^", RegexOptions.Multiline);
            Assert.IsTrue(r2.ToString().Equals(@".*"));
            //a2.ShowGraph("a2");
            //--------
            var r3 = css.RegexConverter.ConvertToSymbolicRegex(@"$", RegexOptions.Multiline);
            //var a3 = css.RegexConverter.Convert(@"$", RegexOptions.Multiline);
            //a3.ShowGraph("a3");
            Assert.IsTrue(r3.ToString().Equals(@".*"));
            //--------
            var r4 = css.RegexConverter.ConvertToSymbolicRegex(@"^$", RegexOptions.Multiline);
            var a4 = css.RegexConverter.Convert(@"^$", RegexOptions.Multiline);
            var regex4 = new Regex(@"^$", RegexOptions.Multiline);
            var regex4b = new Regex(@"^(.*\n)?(\n.*)?$", RegexOptions.Singleline);
            var r4b = css.RegexConverter.ConvertToSymbolicRegex(@"^(.*\n)?(\n.*)?$", RegexOptions.Singleline);
            //a4.ShowGraph("a4");
            var a4min = a4.Determinize().Minimize();
            //a4min.ShowGraph("a4min");
            Assert.IsTrue(css.Accepts(a4min, ""));
            Assert.IsTrue(regex4.IsMatch(""));
            Assert.IsTrue(regex4b.IsMatch(""));
            Assert.IsTrue(css.Accepts(a4min, "\n"));
            Assert.IsTrue(regex4.IsMatch("\n"));
            Assert.IsTrue(regex4b.IsMatch("\n"));
            Assert.IsTrue(css.Accepts(a4min, "a\n"));
            Assert.IsTrue(regex4.IsMatch("a\n"));
            Assert.IsTrue(regex4b.IsMatch("a\n"));
            Assert.IsTrue(css.Accepts(a4min, "a\n\nbcd"));
            Assert.IsTrue(regex4.IsMatch("a\n\nbcd"));
            Assert.IsTrue(regex4b.IsMatch("a\n\nbcd"));
            //some negative cases
            Assert.IsFalse(css.Accepts(a4min, "a\nbcd"));
            Assert.IsFalse(regex4.IsMatch("a\nbcd"));
            Assert.IsFalse(regex4.IsMatch("a\nbcd"));

            Assert.IsTrue(r4b.ToString().Equals(@"(.*\n)?(\n.*)?"));
            Assert.IsTrue(r4.ToString().Equals(@"(.*\n)?(\n.*)?"));
        }

        [TestMethod]
        public void TestSymbolicRegexSampler_Completeness()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);
            string regex = @"^(a|b)cccc(d|e)kkkk(f|l)$";
            SymbolicRegexSampler sampler = new SymbolicRegexSampler(regex, solver, 10);
            Assert.IsTrue(sampler.GetPositiveDataset(1000).Count == 8, "Incomplete Dataset");
        }

        [TestMethod]
        public void TestSymbolicRegexSampler_KleeneStar()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);
            string regex = @"^(a(b*)c)*$";
            SymbolicRegexSampler sampler = new SymbolicRegexSampler(regex, solver, 15);
            Assert.IsTrue(sampler.GetPositiveDataset(1000).Count > 100, "Incomplete Dataset");
        }

        [TestMethod]
        public void TestSymbolicRegexSampler_Anchors() 
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);
            string regex = @"^bcd$";   
            SymbolicRegexSampler sampler = new SymbolicRegexSampler(regex, solver, 15);
        }

        [TestMethod]
        public void TestSymbolicRegex_Restrict()
        {
            CharSetSolver solver = new CharSetSolver();
            var regex = new Regex("^([5-8]|[d-g]+)+([a-k]|()|[1-9][1-9])(?(d)[de]|f)(?([a-k])[de]|f)def[a-g]*(e|8)+$");
            var sr = solver.RegexConverter.ConvertToSymbolicRegex(regex, true);
            var sr1 = sr.Restrict(solver.MkCharSetFromRegexCharClass("[d-x0-8]"));
            Assert.IsTrue(sr1.ToString() == "^([5-8]|[d-g]+)+([d-k]|()|[1-8][1-8])(?(d)[de]|f)(?([d-k])[de]|f)def[d-g]*[8e]+$");
        }

    }
}
