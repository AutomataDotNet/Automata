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
using System.Runtime.Serialization.Formatters.Soap;

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
            Assert.IsTrue(r3.ToString().Equals(@"[e-x]|a|b|c|d"));
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
            Assert.AreEqual<string>(@"(?(.*A.*)(.*B.*)|(.*C.*))", r8.ToString());
            //---------
            var r8b = css.RegexConverter.ConvertToSymbolicRegex(@"(?(A)B|C)", RegexOptions.Singleline);
            Assert.IsTrue(r8b.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(.*A.*)(.*B.*)|(.*C.*))", r8b.ToString());
            //---------
            var r8c = css.RegexConverter.ConvertToSymbolicRegex(@"^(?(A)B|C)", RegexOptions.Singleline);
            Assert.IsTrue(r8c.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(A.*)(B.*)|(C.*))", r8c.ToString());
            //---------
            var r8d = css.RegexConverter.ConvertToSymbolicRegex(@"(?(A)B|C)$", RegexOptions.Singleline);
            Assert.IsTrue(r8d.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(.*A)(.*B)|(.*C))", r8d.ToString());
            //--------
            var r9 = css.RegexConverter.ConvertToSymbolicRegex(@"()()", RegexOptions.Singleline);
            Assert.IsTrue(r9.Kind == SymbolicRegexKind.Loop);
            Assert.AreEqual<string>(@".*", r9.ToString());
            //-----
            var a_complement = css.RegexConverter.ConvertToSymbolicRegex(@"(?(a)[0-[0]]|.*)", RegexOptions.Singleline);
            Assert.IsTrue(a_complement.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(.*a.*)([0-[0]])|(.*))", a_complement.ToString());
            //-----
            var conj = css.RegexConverter.ConvertToSymbolicRegex(@"(?(.*b.*)(.*a.*)|[a-z-[a-z]])", RegexOptions.Singleline);
            Assert.IsTrue(conj.Kind == SymbolicRegexKind.And);
            Assert.AreEqual<string>(@"(?(.*a.*)(.*b.*)|[0-[0]])", conj.ToString());
            //-----
        }

        [TestMethod]
        public void TestSampleSymbolicRegexesBasicConstructsKeepAnchors()
        {
            CharSetSolver css = new CharSetSolver(BitWidth.BV16);
            var r0 = css.RegexConverter.ConvertToSymbolicRegex("", RegexOptions.None, true);
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
            Assert.IsTrue(r3.ToString().Equals(@"^([e-x]|a|b|c|d)$"));
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
            Assert.AreEqual<string>(@"(?(.*A.*)(.*B.*)|(.*C.*))", r8.ToString());
            //---------
            var r8b = css.RegexConverter.ConvertToSymbolicRegex(@"(?(A)B|C)", RegexOptions.Singleline, true);
            Assert.IsTrue(r8b.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(A)(B)|(C))", r8b.ToString());
            //---------
            var r8c = css.RegexConverter.ConvertToSymbolicRegex(@"^(?(A)B|C)", RegexOptions.Singleline, true);
            Assert.IsTrue(r8c.Right.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"^(?(A)(B)|(C))", r8c.ToString());
            //---------
            var r8d = css.RegexConverter.ConvertToSymbolicRegex(@"(?(A)B|C)$", RegexOptions.Singleline, true);
            Assert.IsTrue(r8d.Left.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(A)(B)|(C))$", r8d.ToString());
            //--------
            var r9 = css.RegexConverter.ConvertToSymbolicRegex(@"()()", RegexOptions.Singleline, true);
            Assert.IsTrue(r9.Kind == SymbolicRegexKind.Epsilon);
            Assert.AreEqual<string>(@"()", r9.ToString());
            //-----
            var a_complement = css.RegexConverter.ConvertToSymbolicRegex(@"(?(a)[1-[1]]|.*)", RegexOptions.Singleline, true);
            Assert.IsTrue(a_complement.Kind == SymbolicRegexKind.IfThenElse);
            Assert.AreEqual<string>(@"(?(a)([0-[0]])|(.*))", a_complement.ToString());
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
            var r1 = css.RegexConverter.ConvertToSymbolicRegex(@"^foo\Z", RegexOptions.Multiline);
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
            var sr = solver.RegexConverter.ConvertToSymbolicRegex(regex);
            var sampler = new SymbolicRegexSampler<BDD>(solver.RegexConverter.srBuilder, sr, 10);
            Assert.IsTrue(sampler.GetPositiveDataset(1000).Count == 8, "Incomplete Dataset");
        }

        [TestMethod]
        public void TestSymbolicRegexSampler_KleeneStar()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);
            string regex = @"^(a(b*)c)*$";
            var sr = solver.RegexConverter.ConvertToSymbolicRegex(regex);
            var sampler = new SymbolicRegexSampler<BDD>(solver.RegexConverter.srBuilder, sr, 15);
            Assert.IsTrue(sampler.GetPositiveDataset(1000).Count > 100, "Incomplete Dataset");
        }

        [TestMethod]
        public void TestSymbolicRegexSampler_Anchors()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);
            string regex = @"^bcd$";
            var sr = solver.RegexConverter.ConvertToSymbolicRegex(regex);
            var sampler = new SymbolicRegexSampler<BDD>(solver.RegexConverter.srBuilder, sr, 15);
            Assert.IsTrue(sampler.GetPositiveDataset(100).Count == 1, "Too large Dataset");
        }

        [TestMethod]
        public void TestSymbolicRegex_Restrict()
        {
            CharSetSolver solver = new CharSetSolver();
            var regex = new Regex("^(([5-8]|[d-g]+)+)([a-k]|()|[1-9][1-9])(?(d)[de]|f)(?([a-k])[de]|f)def[a-g]*(e|8)+$");
            var sr = solver.RegexConverter.ConvertToSymbolicRegex(regex, true);
            var sr1 = sr.Restrict(solver.MkCharSetFromRegexCharClass("[d-x0-8]"));
            string expected = "^([5-8]|[d-g]+)+(()|[1-8][1-8]|[d-k])(?(d)([de])|(f))(?([d-k])([de])|(f))def[d-g]*[8e]+$";
            Assert.IsTrue(sr1.ToString() == expected);
        }

        [TestMethod]
        public void TestDerivative_GenerateMinterms()
        {
            CharSetSolver css = new CharSetSolver();
            ValidateRegexNrOfPredicatesAndMinterms(css, @"^\w\d\w$", 2, 3);
            ValidateRegexNrOfPredicatesAndMinterms(css, @"^\w$", 1, 2);
            ValidateRegexNrOfPredicatesAndMinterms(css, @"^abc$", 3, 4);
            ValidateRegexNrOfPredicatesAndMinterms(css, @"^[a-k0-9.:]\w[0-5z;.]$", 3, 8); //maximum blowup
            ValidateRegexNrOfPredicatesAndMinterms(css, @"^[a-k0-9.:]\w[0-5z;.]\s$", 4, 9); //medium blowup
        }

        void ValidateRegexNrOfPredicatesAndMinterms(CharSetSolver css, string regex, int expected_pred_count, int expected_minterm_count)
        {
            var sr = css.RegexConverter.ConvertToSymbolicRegex(regex);
            var preds = sr.GetPredicates();
            Assert.AreEqual(expected_pred_count, preds.Count);
            var ms = sr.ComputeMinterms();
            Assert.AreEqual(expected_minterm_count, ms.Length);
        }

        [TestMethod]
        public void TestDerivative_IsMatch1()
        {
            var regex = @"^\w\d\w{1,8}$";
            CharSetSolver css = new CharSetSolver();
            var sr = css.RegexConverter.ConvertToSymbolicRegex(regex, RegexOptions.None, true);
            Func<string, BDD[]> F = s => Array.ConvertAll<char, BDD>(s.ToCharArray(), c => css.MkCharConstraint(c));
            var matcher = new SymbolicRegex<BDD>(sr, css, sr.ComputeMinterms());
            Assert.IsTrue(matcher.IsMatch("a0d"));
            Assert.IsFalse(matcher.IsMatch("a0"));
            Assert.IsTrue(matcher.IsMatch("a5def"));
            Assert.IsFalse(matcher.IsMatch("aa"));
            Assert.IsTrue(matcher.IsMatch("a3abcdefg"));
            Assert.IsTrue(matcher.IsMatch("a3abcdefgh"));
            Assert.IsFalse(matcher.IsMatch("a3abcdefghi"));
        }

        [TestMethod]
        public void TestDerivative_IsMatch2()
        {
            var regex = @"^(abc|bbd|add|dde|ddd){1,2000}$";
            CharSetSolver css = new CharSetSolver();
            var sr = css.RegexConverter.ConvertToSymbolicRegex(regex, RegexOptions.None, true);
            Func<string, BDD[]> F = s => Array.ConvertAll<char, BDD>(s.ToCharArray(), c => css.MkCharConstraint(c));
            var matcher = new SymbolicRegex<BDD>(sr, css, sr.ComputeMinterms());
            Assert.IsTrue(matcher.IsMatch("addddd"));
            Assert.IsFalse(matcher.IsMatch("adddddd"));
        }

        [TestMethod]
        public void TestDerivative_IsMatch3()
        {
            var R = new Regex(@".*(ab|ba)+$", RegexOptions.Singleline);
            var R1 = new Regex(@"(ab|ba)+", RegexOptions.Singleline);
            CharSetSolver css = new CharSetSolver();
            var sr = css.RegexConverter.ConvertToSymbolicRegex(R, true);
            var matcher = new SymbolicRegex<BDD>(sr, css, sr.ComputeMinterms());
            Assert.IsTrue(matcher.IsMatch("xxabbabbaba"));
            Assert.IsTrue(matcher.IsMatch("abba"));
            Assert.IsTrue(R1.IsMatch("baba"));
            Assert.IsFalse(R1.IsMatch("bb"));
            var matches = R1.Matches("xxabbabbaba");
            Assert.IsTrue(matches.Count == 2);
            Assert.IsTrue(matches[0].Index == 2);
            Assert.IsTrue(matches[0].Value == "abba");
            Assert.IsTrue(matches[1].Value == "baba");
            Assert.IsTrue(matches[1].Index == 7);
        }
        [TestMethod]
        public void TestDerivative_IsMatch4()
        {
            var R = new Regex(@"(ab|ba)+|ababbba", RegexOptions.Singleline);
            CharSetSolver css = new CharSetSolver();
            var sr = css.RegexConverter.ConvertToSymbolicRegex(R, true);
            var matcher = new SymbolicRegex<BDD>(sr, css, sr.ComputeMinterms());
            Assert.IsTrue(matcher.IsMatch("ababba"));
            var matches = R.Matches("xaababbba");
            Assert.IsTrue(matches.Count == 2);
            Assert.IsTrue(matches[0].Value == "abab");
            Assert.IsTrue(matches[1].Value == "ba");
            var R2 = new Regex(@"ababbba|(ab|ba)+", RegexOptions.Singleline);
            Assert.IsTrue(R2.Matches("ababba").Count == 1);
        }

        [TestMethod]
        public void TestDerivative_IsMatch5()
        {
            var R = new Regex(@"^(ab*a|bbba*)$", RegexOptions.Singleline);
            CharSetSolver css = new CharSetSolver();
            var A = css.Convert(R.ToString(), R.Options).Determinize().Minimize().Normalize();
            //A.ShowGraph("A");
            var R1 = new Regex(@"^.*(ab*a|bbba*)$", RegexOptions.Singleline);
            var A1 = css.Convert(R1.ToString(), R1.Options).Determinize().Minimize().Normalize();
            //A1.ShowGraph("A1");
            var sr = css.RegexConverter.ConvertToSymbolicRegex(R, true);
            var sr1 = css.RegexConverter.ConvertToSymbolicRegex(R1, true);
            var matcher = new SymbolicRegex<BDD>(sr, css, sr.ComputeMinterms());
            var matcher1 = new SymbolicRegex<BDD>(sr1, css, sr1.ComputeMinterms());
            Assert.IsTrue(matcher.IsMatch("aa"));
            Assert.IsTrue(matcher.IsMatch("abbbbbbbbbba"));
            Assert.IsTrue(matcher.IsMatch("bbb"));
            Assert.IsTrue(matcher.IsMatch("bbbaaaaaaaaa"));
            Assert.IsFalse(matcher.IsMatch("baba"));
            Assert.IsFalse(matcher.IsMatch("abab"));
            //--------------
            Assert.IsTrue(matcher1.IsMatch("xxxxaa"));
            Assert.IsTrue(matcher1.IsMatch("xxabbbbbbbbbba"));
            Assert.IsTrue(matcher1.IsMatch("xxbbb"));
            Assert.IsTrue(matcher1.IsMatch("xxxbbbaaaaaaaaa"));
            Assert.IsFalse(matcher1.IsMatch("babab"));
            Assert.IsFalse(matcher1.IsMatch("ababx"));
            //---
            var R2 = new Regex(@"bbba*|ab*a", RegexOptions.Singleline);
            var matches = R2.Matches("xxabbba");
            Assert.AreEqual<int>(1, matches.Count);
            Assert.AreEqual<int>(2, matches[0].Index);
            Assert.AreEqual<string>("abbba", matches[0].Value);
            var matches2 = R2.Matches("xxabbbbaa");
            Assert.AreEqual<int>(1, matches2.Count);
            Assert.AreEqual<int>(2, matches2[0].Index);
            Assert.AreEqual<string>("abbbba", matches2[0].Value);
            var matches3 = R2.Matches("xxabbbbbbbbbaa");
            Assert.AreEqual<int>(1, matches3.Count);
            var matches4 = R2.Matches("xxxbbbbbbbbbaa");
            Assert.AreEqual<int>(3, matches4.Count);
        }

        [TestMethod]
        public void TestDerivative_IsMatch_LargeLoop()
        {
            var R = new Regex(@"(ab|x|ba){1,20000}");
            var sr = R.Compile();
            Assert.IsTrue(sr.IsMatch("abba"));
            Assert.IsTrue(sr.IsMatch("abxxx"));
            Assert.IsTrue(sr.IsMatch("ab"));
            Assert.IsTrue(sr.IsMatch("abxxxba"));
            Assert.IsTrue(sr.IsMatch("baba"));
            Assert.IsTrue(sr.IsMatch("abab"));
            Assert.IsFalse(sr.IsMatch("aayybb"));
        }

        [TestMethod]
        public void TestSymbolicRegex_Reverse()
        {
            CharSetSolver css = new CharSetSolver();
            //-----
            var R1 = new Regex(@"abc");
            var sr1 = css.RegexConverter.ConvertToSymbolicRegex(R1, true);
            var rev1 = sr1.Reverse();
            var matcher1 = new SymbolicRegex<BDD>(rev1, css, rev1.ComputeMinterms());
            Assert.IsTrue(matcher1.IsMatch("cba"));
            //-----
            var R2 = new Regex(@"^(foo|ab+d)+$");
            var sr2 = css.RegexConverter.ConvertToSymbolicRegex(R2, true);
            var rev2 = sr2.Reverse();
            var matcher2 = new SymbolicRegex<BDD>(rev2, css, rev2.ComputeMinterms());
            Assert.IsTrue(sr2.Equals(rev2.Reverse()));
            Assert.IsTrue(matcher2.IsMatch("oof"));
            Assert.IsTrue(matcher2.IsMatch("oofdbbaoofoofdbbadba"));
            var sampler = new SymbolicRegexSampler<BDD>(css.RegexConverter.srBuilder, rev2, 10);
            var samples = sampler.GetPositiveDataset(100);
            foreach (var sample in samples)
                Assert.IsTrue(matcher2.IsMatch(sample));
        }

        //[TestMethod]
        //public void TestSymbolicRegex_Explore_Simple()
        //{
        //    CharSetSolver css = new CharSetSolver();
        //    //-----
        //    var R1 = new Regex(@"abc");
        //    var srBDD = R1.ConvertToSymbolicRegexBDD(css);
        //    var autBDD = srBDD.Explore();
        //    //-----
        //    //autBDD.ShowGraph("autBDD");
        //    var srBV = R1.Compile(css);
        //    var autBV = srBV.Explore();
        //    //autBV.ShowGraph("autBV");
        //    Assert.AreEqual<int>(autBV.StateCount, autBDD.StateCount);
        //}

        //[TestMethod]
        //public void TestSymbolicRegex_Explore_Loop_TrickyCase()
        //{
        //    int k = 10;
        //    var r = "pwd=[^a][^b]{0,10}b";
        //    //var r = "\n.*(thisid|that).*(foobar|[^a-z]bar)=[^@][^;]{5," + k + "};";
        //    //var r = "[^a][^b]{0," + k + "}b";
        //    //SymbolicRegexSet<BV>.optimizeLoops = false;
        //    TestSymbolicRegex_Explore_Helper(r);
        //}

        //[TestMethod]
        //public void TestSymbolicRegex_Explore_Choice_Small2()
        //{
        //    var r = ".*(bc|[^bcd])";
        //    var R = new Regex(r, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        //    //var r3 = "(?i:(bc|[^bcd]))";
        //    //var R3 = new Regex(r3, RegexOptions.Singleline);
        //    CharSetSolver css = new CharSetSolver();
        //    //var bcd = css.MkCharSetFromRanges('b', 'd', 'B', 'D');
        //    //var range1 = css.MkCharSetFromRegexCharClass("[\0-AE-ae-\uFFFF]");
        //    //var range2 = css.MkCharSetFromRegexCharClass("[^bcdBCD]");
        //    //Assert.AreEqual<BDD>(range1, range2);
        //    //Assert.AreEqual<BDD>(!bcd, range2);
        //    //var neg_abc = css.MkNot(bcd);
        //    //int neg_abc_cnt = (int)css.ComputeDomainSize(neg_abc);
        //    //int abc_cnt = (int)css.ComputeDomainSize(bcd);
        //    //Assert.AreEqual<int>(abc_cnt, 6);
        //    //Assert.AreEqual<int>(neg_abc_cnt, 0x10000 - 6);
        //    var sr = R.Compile(css);
        //    //--- construnct specialized RegexAutomaton
        //    int t = System.Environment.TickCount;
        //    var ar = sr.Explore(10000);
        //    t = System.Environment.TickCount - t;
        //    //--- covert to sfa over BDDs
        //    var sfa = css.Convert("(" + R.ToString() + ")$", R.Options);
        //    //--- convert to specialized sfa over BVs
        //    var sfa_bv = sfa.ReplaceAlgebra<BV>(sr.builder.solver.ConvertFromCharSet, sr.builder.solver);
        //    //--- minimize that sfa
        //    int t2 = System.Environment.TickCount;
        //    var sfa_m = sfa.Determinize().Minimize().Normalize();
        //    t2 = System.Environment.TickCount - t2;
        //    //--- minimize the sfa over BVs
        //    int t2bv = System.Environment.TickCount;
        //    var sfa_bv_m = sfa_bv.Determinize().Minimize().Normalize();
        //    t2bv = System.Environment.TickCount - t2bv;
        //    //--- covret back from BVs to BDDs
        //    var sfa_bv_m_bdd = sfa_bv_m.ReplaceAlgebra<BDD>(sr.builder.solver.ConvertToCharSet, sr.builder.solver.CharSetProvider);
        //    //---
        //    Console.WriteLine("Explore:|Q|={0},t={1}ms, MinAut:|Q|={2},t={3}ms, MinAut_bv:|Q|={4},t={5}ms", ar.StateCount, t, sfa_m.StateCount, t2, sfa_bv_m.StateCount, t2bv);
        //    Assert.IsTrue(sfa_m.StateCount == sfa_bv_m.StateCount, "automata must have same nr of states");
        //    Assert.IsTrue(sfa_m.StateCount == sfa_bv_m_bdd.StateCount, "automata must have same nr of states");
        //    Assert.IsTrue(sfa_m.IsEquivalentWith(sfa_bv_m_bdd), "automata must be equivalent");
        //}

        //[TestMethod]
        //public void TestSymbolicRegex_Explore_Loop_Simple()
        //{
        //    for (int k = 10; k < 12; k++)
        //    {
        //        var r = "ba{0,3}b";
        //        TestSymbolicRegex_Explore_Helper(r);
        //    }
        //}



        [TestMethod]
        public void TestSymbolicRegex_Simplify()
        {
            var css = new CharSetSolver();
            string[] regexes = new string[] {
                "ba{3,7}b",
                "ba{3,7}b|baa{2,6}b",
                "ba{3,}z|baa{2,}z",
                "(foo){4,5}|(bar){2,7}",
                "(bar){2,7}|(foo){4,5}"
            };
            string[] simpl = new string[] {
                "baaaa{0,4}b",
                "baaaa{0,4}b",
                "baaaa*z",
                "barbar(bar){0,5}|foofoofoofoo(foo)?",
                "barbar(bar){0,5}|foofoofoofoo(foo)?"
            };
            for (int i = 0; i < regexes.Length; i++)
            {
                var R = new Regex(regexes[i]);
                var SR = R.ConvertToSymbolicRegexBDD(css);
                var SRS = SR.Simplify();
                Assert.AreEqual<string>(simpl[i], SRS.ToString());
            }
        }

        [TestMethod]
        public void TestSymbolicRegex_Matches_abc()
        {
            var regex = new Regex("abc", RegexOptions.IgnoreCase);
            //var solver = new CharSetSolver();
            var sr = regex.Compile();
            var input = "xbxabcabxxxxaBCabcxx";
            Func<int, int, Tuple<int, int>> f = (x, y) => new Tuple<int, int>(x, y);
            var expectedMatches = new Sequence<Tuple<int, int>>(f(3, 3), f(12, 3), f(15, 3));
            var matches = new Sequence<Tuple<int, int>>(sr.Matches(input));
            Assert.AreEqual<Sequence<Tuple<int, int>>>(expectedMatches, matches);
        }

        [TestMethod]
        public void TestSymbolicRegex_Matches_simple_loops()
        {
            var regex = new Regex("bcd|(cc)+|e+");
            //var solver = new CharSetSolver();
            var sr = regex.Compile();
            var input = "cccccbcdeeeee";
            Func<int, int, Tuple<int, int>> f = (x, y) => new Tuple<int, int>(x, y);
            var expectedMatches = new Sequence<Tuple<int, int>>(f(0, 4), f(5, 3), f(8, 5));
            var matches = new Sequence<Tuple<int, int>>(sr.Matches(input));
            Assert.AreEqual<Sequence<Tuple<int, int>>>(expectedMatches, matches);
        }

        [TestMethod]
        public void TestSymbolicRegex_Matches_bounded_loops()
        {
            var regex = new Regex("a{2,4}");
            //var solver = new CharSetSolver();
            var sr = regex.Compile();
            var input = "..aaaaaaaaaaa..";
            Func<int, int, Tuple<int, int>> f = (x, y) => new Tuple<int, int>(x, y);
            var expectedMatches = new Sequence<Tuple<int, int>>(f(2,4),f(6,4),f(10,3));
            var matches = new Sequence<Tuple<int, int>>(sr.Matches(input));
            Assert.AreEqual<int>(3, matches.Length);
            Assert.AreEqual<Sequence<Tuple<int, int>>>(expectedMatches, matches);
        }

        //[TestMethod]
        //public void TestSymbolicRegex_Explore_Choice_Large()
        //{
        //    var r = "(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty|" +
        //        "twenty-one|twenty-two|twenty-three|twenty-four|twenty-five|twenty-six|twenty-seven|twenty-eight|twenty-nine|thirty|" +
        //        "thirty-one|thirty-two|thirty-three|thirty-four|thirty-five|thirty-six|thirty-seven|thirty-eight|thirty-nine|forty|" +
        //        "forty-one|forty-two|forty-three|forty-four|forty-five|forty-six|forty-seven|forty-eight|forty-nine|fifty|" +
        //        "fifty-one|fifty-two|fifty-three|fifty-four|fifty-five|fifty-six|fifty-seven|fifty-eight|fifty-nine|sixty|" +
        //        "sixty-one|sixty-two|sixty-three|sixty-four|sixty-five|sixty-six|sixty-seven|sixty-eight|sixty-nine|seventy|" +
        //        "seventy-one|seventy-two|seventy-three|seventy-four|seventy-five|seventy-six|seventy-seven|seventy-eight|seventy-nine|" +
        //        "eighty|eighty-one|eighty-two|eighty-three|eighty-four|eighty-five|eighty-six|eighty-seven|eighty-eight|eighty-nine|ninety|" +
        //        "ninety-one|ninety-two|ninety-three|ninety-four|ninety-five|ninety-six|ninety-seven|ninety-eight|ninety-nine|onehundred)";
        //    TestSymbolicRegex_Explore_Helper(r);
        //}

        //[TestMethod]
        //public void TestSymbolicRegex_Explore_Choice_Medium()
        //{
        //    var r = "one|two|three|four|five|six|seven|eight|nine|ten|twenty|twenty-one";
        //    TestSymbolicRegex_Explore_Helper(r);
        //}

        //[TestMethod]
        //public void TestSymbolicRegex_Explore_Choice_Small()
        //{
        //    var r = "one|two|three";
        //    TestSymbolicRegex_Explore_Helper(r);
        //}

        //[TestMethod]
        //public void TestSymbolicRegex_Explore_Trivial()
        //{
        //    var r = "a";
        //    CharSetSolver css = new CharSetSolver();
        //    var R3 = new Regex(".*(" + r + ")", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        //    var sr3 = R3.Compile(css);
        //    var bva = ((BVAlgebra)sr3.builder.solver);
        //    Assert.IsTrue(2 == bva.atoms.Length);
        //    Assert.IsTrue(2 == bva.atoms.Length);
        //    Assert.IsTrue(bva.GetIdOfChar('a') == bva.GetIdOfChar('A'));
        //    Assert.IsTrue(bva.GetIdOfChar('d') == bva.GetIdOfChar('x'));
        //    var aut = sr3.Explore(100000);
        //    Assert.IsFalse(aut.IsMatch(""));
        //    Assert.IsTrue(aut.IsMatch("xxxa"));
        //    Assert.IsTrue(aut.IsMatch("xxsdsdxA"));
        //    Assert.IsFalse(aut.IsMatch("xxsdsdxAx"));
        //}

        //private static void TestSymbolicRegex_Explore_Helper(string r3)
        //{
        //    CharSetSolver css = new CharSetSolver();
        //    var R3 = new Regex(".*(" + r3 + ")", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        //    var sr3 = R3.Compile(css, false);
        //    int t = System.Environment.TickCount;
        //    var ar3 = sr3.Explore(10000);
        //    t = System.Environment.TickCount - t;
        //    var sfa3 = css.Convert("(" + r3 + ")$", R3.Options);
        //    var sfa3_bv = sfa3.ReplaceAlgebra<BV>(sr3.builder.solver.ConvertFromCharSet, sr3.builder.solver);
        //    int t2 = System.Environment.TickCount;
        //    var sfa3m = sfa3.Determinize().Minimize().Normalize();
        //    t2 = System.Environment.TickCount - t2;
        //    string[] partition = Array.ConvertAll(GetMinterms(sfa3m), ((CharSetSolver)(sfa3m.Algebra)).PrettyPrint);
        //    int t2bv = System.Environment.TickCount;
        //    var sfa3mbv = sfa3_bv.Determinize().Minimize().Normalize();
        //    t2bv = System.Environment.TickCount - t2bv;
        //    Func<BV, BDD> F = bv =>
        //    {
        //        BDD bdd;
        //        sr3.builder.solver.TryConvertToCharSet(bv, out bdd);
        //        return bdd;
        //    };
        //    var sfa3mbv_bdd = sfa3mbv.ReplaceAlgebra<BDD>(F, sr3.builder.solver.CharSetProvider);
        //    Console.WriteLine("Explore:|Q|={0},t={1}ms, MinAut:|Q|={2},t={3}ms, MinAut_bv:|Q|={4},t={5}ms", ar3.StateCount, t, sfa3m.StateCount, t2, sfa3mbv.StateCount, t2bv);
        //    //--- run agains a set of samples
        //    Assert.AreEqual<int>(sfa3m.StateCount, sfa3mbv.StateCount);
        //    Assert.AreEqual<int>(sfa3m.StateCount, sfa3mbv_bdd.StateCount);
        //    Assert.IsTrue(sfa3m.IsEquivalentWith(sfa3mbv_bdd));
        //    var ar3_bdd = ar3.ConvertToAutomatonOverBDD();
        //    Assert.IsTrue(sfa3m.IsEquivalentWith(ar3_bdd));
        //    var dataset = sr3.GetPositiveDataset(50);
        //    foreach (var s in dataset)
        //    {
        //        Assert.IsTrue(ar3.IsMatch(s));
        //        Assert.IsTrue(sr3.IsMatch(s));
        //        css.Accepts(sfa3m, s);
        //        Assert.IsTrue(R3.IsMatch(s));
        //    }
        //}

        private static IEnumerable<BDD> EnumeratePredicates(Automaton<BDD> sfa3m)
        {
            foreach (var m in sfa3m.GetMoves())
                yield return m.Label;
        }

        private static BDD[] GetMinterms(Automaton<BDD> sfa3m)
        {
            var predsArray = new List<BDD>(new HashSet<BDD>(EnumeratePredicates(sfa3m))).ToArray();
            var mts = new List<Tuple<bool[], BDD>>(sfa3m.Algebra.GenerateMinterms(predsArray)).ToArray();
            BDD[] partition = Array.ConvertAll(mts, x => x.Item2);
            return partition;
        }

        [TestMethod]
        public void TestSymbolicRegexBDD_IsMatch()
        {
            var css = new CharSetSolver();
            var R = new Regex(@"^abc[\0-\xFF]+$");
            var sr = R.ConvertToSymbolicRegexBDD(css);
            var matcher = new SymbolicRegex<BDD>(sr, css, sr.ComputeMinterms());
            var str = "abc" + CreateRandomString(1000);
            Assert.IsTrue(matcher.IsMatch(str));
            Assert.IsFalse(matcher.IsMatch(str + "\uFFFD\uFFFD\uFFFD"));
        }

        [TestMethod]
        public void TestSymbolicRegexBV_IsMatch()
        {
            //var css = new CharSetSolver();
            var R = new Regex(@"^abc[\0-\xFF]+$");
            var sr = R.Compile();
            var str = "abc" + CreateRandomString(1000);
            Assert.IsTrue(sr.IsMatch(str));
            Assert.IsFalse(sr.IsMatch(str + "\uFFFD\uFFFD"));
        }

        public static string CreateRandomString(int length)
        {
            byte[] bytes = new byte[length];
            RNGCryptoServiceProvider rnd = new RNGCryptoServiceProvider();
            rnd.GetBytes(bytes);
            char[] cs = Array.ConvertAll(bytes, b => (char)b);
            string s = new string(cs);
            return s;
        }

        [TestMethod]
        public void TestDerivative_BasicCreation()
        {
            var regex = new Regex("[ab]*a[ab]{0,5}", RegexOptions.Singleline);
            //var regex = new Regex(@".*ab{0,5}ea{0,5}d", RegexOptions.Singleline);
            //var regex = new Regex("<[^>]*>.*", RegexOptions.Singleline);
            var r_c = regex; //.Complement();
            var sr = (SymbolicRegex<ulong>)r_c.Compile();
            //var deriv = sr.MkDerivative(sr.builder.solver.MkCharConstraint('<'), true);
            var aut = ((SymbolicRegex<ulong>)sr).A.Unwind();
            //regex.Display("minDFA",true);
            Assert.IsTrue(aut.DescribeState(aut.InitialState) == sr.A.ToString());
            //sr.Pattern.ShowGraph();
        }

        [TestMethod]
        public void TestDerivative_BasicCreation2()
        {
            var regex = new Regex(".*a(c|.*){0,10}", RegexOptions.Singleline);
            //var regex = new Regex(@".*ab{0,5}ea{0,5}d", RegexOptions.Singleline);
            //var regex = new Regex("<[^>]*>.*", RegexOptions.Singleline);
            var r_c = regex; //.Complement();
            var sr = (SymbolicRegex<ulong>)r_c.Compile();
            //var deriv = sr.MkDerivative(sr.builder.solver.MkCharConstraint('<'), true);
            var aut = ((SymbolicRegex<ulong>)sr).A.Unwind();
            //regex.Display("minDFA",true);
            Assert.IsTrue(aut.DescribeState(aut.InitialState) == sr.A.ToString());
            //sr.Pattern.ShowGraph();
        }

        [TestMethod]
        public void TestSymbolicRegex_Serialization()
        {
            var regex = new Regex("abc*", RegexOptions.Singleline);
            var sr = (SymbolicRegex<ulong>)regex.Compile();
            var B = sr.builder;
            //
            var node = B.Deserialize("[6]");
            Assert.IsTrue(node.kind == SymbolicRegexKind.Singleton);
            Assert.AreEqual<string>(node.Serialize(), "[6]");
            //---
            var star = B.Deserialize("L(0,*,[c])");
            Assert.IsTrue(star.kind == SymbolicRegexKind.Loop);
            Assert.IsTrue(star.IsStar);
            Assert.AreEqual<string>(star.Serialize(), "L(0,*,[c])");
            //---
            var plus = B.Deserialize("L(1,*,[e])");
            Assert.IsTrue(plus.kind == SymbolicRegexKind.Loop);
            Assert.IsTrue(plus.IsPlus);
            Assert.AreEqual<string>(plus.Serialize(), "L(1,*,[e])"); //e is 1110 i.e. [a-c]
            //---
            var dotstar = B.Deserialize("L(0,*,.)");
            Assert.IsTrue(dotstar.kind == SymbolicRegexKind.Loop);
            var isdotstar = dotstar.IsDotStar;
            Assert.IsTrue(isdotstar);
            Assert.AreEqual<string>(dotstar.Serialize(), "L(0,*,.)");
            //---
            var seq = B.Deserialize("S(.,L(0,*,[1]))");
            Assert.IsTrue(seq.kind == SymbolicRegexKind.Concat);
            Assert.IsTrue(seq.left.kind == SymbolicRegexKind.Singleton);
            Assert.IsTrue(seq.right.kind == SymbolicRegexKind.Loop);
            Assert.AreEqual<string>(seq.Serialize(), "S(.,L(0,*,[1]))");
            //---
            var disj = B.Deserialize("D([3],[2],S(.,L(4,7,[1])))");
            Assert.IsTrue(disj.kind == SymbolicRegexKind.Or);
            Assert.IsTrue(disj.alts.Count == 3);
            var disj_ser = disj.Serialize();
            Assert.AreEqual<string>(disj_ser, "D([2],[3],S(.,L(4,7,[1])))");
            //---
            var conj = B.Deserialize("C(.,[2],S(.,L(0,*,[1])))");
            Assert.IsTrue(conj.kind == SymbolicRegexKind.And);
            Assert.IsTrue(conj.alts.Count == 3);
            Assert.AreEqual<string>(conj.Serialize(), "C(.,[2],S(.,L(0,*,[1])))");
            //---
            var empty = B.Deserialize("E");
            Assert.IsTrue(empty.IsEpsilon);
            Assert.AreEqual<string>(empty.Serialize(), "E");
            //---
            var a = B.Deserialize("[a]");
            Assert.AreEqual<string>(a.Serialize(), "[a]");
        }

        [TestMethod]
        public void TestDerivative_Tags()
        {
            //var regex = new Regex("[ab]*a[ab]{0,5}", RegexOptions.Singleline);
            var regex = new Regex(@"a[^ab]+b");
            var sr = (SymbolicRegexUInt64)regex.Compile();
            var aut = ((SymbolicRegex<ulong>)sr).A.Unwind();
            //regex.Display("Tags",true);
            Assert.IsTrue(aut.DescribeState(aut.InitialState) == sr.A.ToString());
            //sr.Pattern.ShowGraph(0, "ab", true);
        }
    }
}

namespace Automata.Tests
{
    [TestClass]
    public class RegexMatcherTests
    {
        SoapFormatter sf = new System.Runtime.Serialization.Formatters.Soap.SoapFormatter();
        [TestMethod]
        public void TestSRM()
        {
            var regex = new Regex(@"a[^ab]+b");
            var srm = (SymbolicRegexUInt64)regex.Compile();
            var matches = srm.Matches("xaTAG1bxaTAG2bc");
            Assert.IsTrue(matches.Length == 2);
            Assert.IsTrue(matches[0].Item1 == 1);
            Assert.IsTrue(matches[0].Item2 == 6);
            Assert.IsTrue(matches[1].Item1 == 8);
            Assert.IsTrue(matches[1].Item2 == 6);
            var s = srm.GenerateRandomMatch();
            //srm.Pattern.ShowGraph(0,"tag",true);
            srm.Serialize("tag.bin");
            var srm2 = RegexMatcher.Deserialize("tag.bin");
            var matches2 = srm2.Matches("a<tag1>b<tag2>c");
        }

        [TestMethod]
        public void TestSRM_singlePass()
        {
            var regex = new Regex(@"abcbc1|cbc2");
            var srm = (SymbolicRegexUInt64)regex.Compile();
            var matches = srm.Matches("xxxabcbc1yyyccbc2xxx");
            Assert.IsTrue(matches.Length == 2);
            Assert.IsTrue(matches[0].Item1 == 3);
            Assert.IsTrue(matches[0].Item2 == 6);
            Assert.IsTrue(matches[1].Item1 == 13);
            Assert.IsTrue(matches[1].Item2 == 4);
            var s = srm.GenerateRandomMatch();
            //srm.Pattern.ShowGraph(0,"abcbc",true);
            srm.Serialize("tag.bin");
            var srm2 = RegexMatcher.Deserialize("tag.bin");
            var matches2 = srm2.Matches("xxxabcbc1yyyccbc2xxx");
            Assert.AreEqual(new Sequence<Tuple<int, int>>(matches), new Sequence<Tuple<int, int>>(matches2));
        }

        [TestMethod]
        public void TestSRM_singletonSeq()
        {
            var regex = new Regex(@"a[bB]c");
            var srm_ = regex.Compile();
            var srm = (SymbolicRegexUInt64)srm_;
            var matches = srm.Matches("xxxabcyyyaBcxxx");
            Assert.IsTrue(matches.Length == 2);
            Assert.IsTrue(matches[0].Item1 == 3);
            Assert.IsTrue(matches[0].Item2 == 3);
            Assert.IsTrue(matches[1].Item1 == 9);
            Assert.IsTrue(matches[1].Item2 == 3);
            var s = srm.GenerateRandomMatch();
            //srm.Pattern.ShowGraph(0, "abc", true);
            srm.Serialize("tag.bin");
            var srm2 = RegexMatcher.Deserialize("tag.bin");
            var matches2 = srm2.Matches("xxxabcyyyaBcxxx");
            Assert.AreEqual(new Sequence<Tuple<int, int>>(matches), new Sequence<Tuple<int, int>>(matches2));
        }
    }
}
