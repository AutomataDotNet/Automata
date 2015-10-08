using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Microsoft.Automata;
using Microsoft.Automata.Internal;

using System.Text.RegularExpressions;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class MiscRegexConversionTests
    {
        [TestMethod]
        public void TestGroups()
        {
            CharSetSolver css = new CharSetSolver(BitWidth.BV7);
            var regex = @"\W*(?<key>\w{1,3})\s*(?<value>\d{2,3})\D*";
            bool b;
            var captures = css.ConvertCaptures(regex, out b);
            Assert.IsTrue(captures.Length == 5);
            for (int i = 0; i < captures.Length; i++ )
            {
                var aut = captures[i].Item2;
                var name = captures[i].Item1;
                if (name == "")
                    name = "skip" + i;
                //css.ShowGraph(aut, name);
            }
            for (int i = 0; i < captures.Length; i++)
            {

            }
        }

        [TestMethod]
        public void TestWordBoundary()
        {
            CharSetSolver css = new CharSetSolver(BitWidth.BV7);
            var regex = @"\b(@|A)B\b";

            Assert.IsTrue(Regex.IsMatch("AB", regex));
            Assert.IsTrue(Regex.IsMatch("A@B", regex));
            Assert.IsTrue(Regex.IsMatch("A@B&", regex));
            Assert.IsTrue(Regex.IsMatch("+AB+", regex));
            Assert.IsFalse(Regex.IsMatch("@B", regex));
            Assert.IsFalse(Regex.IsMatch("A@B_", regex));

            var aut = css.Convert(regex);//.RemoveEpsilons(css.MkOr).Determinize(css).Minimize(css);
            //css.ShowGraph(aut, "aut");

            Assert.IsTrue(css.Accepts(aut, "AB"));
            Assert.IsTrue(css.Accepts(aut, "A@B"));
            Assert.IsTrue(css.Accepts(aut, "A@B&"));
            Assert.IsTrue(css.Accepts(aut, "+AB+"));
            Assert.IsFalse(css.Accepts(aut, "@B"));
            Assert.IsFalse(css.Accepts(aut, "A@B_"));

            CheckValidity(css, aut, regex);
        }

        void CheckValidity(CharSetSolver css, Automaton<BDD> aut, string regex)
        {
            if (!aut.IsEmpty)
                for (int i = 0; i < 1000; i++)
                {
                    var str = css.GenerateMember(aut);
                    if (!str.Contains("\u200C") && !str.Contains("\u200D") && !str.Contains("\n"))
                        Assert.IsTrue(Regex.IsMatch(str, regex, RegexOptions.Singleline), str);
                }

            var aut_compl = aut.Complement(css).Minimize(css);
            if (!aut_compl.IsEmpty)
                for (int i = 0; i < 1000; i++)
                {
                    var str = css.GenerateMember(aut_compl);
                    if (!str.Contains("\u200C") && !str.Contains("\u200D") && !str.Contains("\n"))
                        if (Regex.IsMatch(str, regex, RegexOptions.Singleline))
                            Assert.IsFalse(true, regex + ":" + StringUtility.Escape(str));
                }
        }

        //[TestMethod]
        public void TestC4Cregexes()
        {
            string[] regexes = File.ReadAllLines(@"..\..\..\Samples\C4C\mathiasbynens_url_regex.txt");
            CharSetSolver css = new CharSetSolver(BitWidth.BV7);
            var notsupportedcases = new HashSet<int>(new int[] {
              1, //uses require (?=)
              4, //uses prevent (?!)
              9, //uses lazy a*?
              11, //uses prevent (?!)
            });
            for (int i = 0; i < regexes.Length; i++)
            {
                if (!notsupportedcases.Contains(i))
                {
                    string regex = regexes[i];
                    var aut = css.Convert(regex);
                    CheckValidity(css, aut, regex);
                }
            }
        }

        [TestMethod]
        public void TestLoopThatStartsWith0()
        {
            string regex = @"(w(a|bc){0,2})";
            CharSetSolver css = new CharSetSolver(BitWidth.BV7);
            var aut = css.Convert(regex,RegexOptions.Singleline,true);
            //css.ShowGraph(aut, "CornerCase");
            var str = "w.-J_";
            var actual = css.Accepts(aut, str);
            var expected = Regex.IsMatch(str, regex);
            Assert.AreEqual(expected, actual);
        }	

        [TestMethod]
        public void TestWordBoundaryCase()
        {
            string s = "abc";
            string r = @"\b";
            CharSetSolver css = new CharSetSolver(BitWidth.BV7);
            var aut = css.Convert(r,RegexOptions.Singleline,true);
            //css.ShowGraph(aut, "test1");
            css.RegexConverter.EliminateBoundaryStates(aut);
            //css.ShowGraph(aut,"test2");
            Assert.IsTrue(css.Accepts(aut, s));
            Assert.IsTrue(Regex.IsMatch(s, r, RegexOptions.Singleline));
        }

        [TestMethod]
        public void TestTrivialWordBoundary1()
        {
            string r = @"\b";
            CharSetSolver css = new CharSetSolver(BitWidth.BV7);
            var aut = css.Convert(r).RemoveEpsilons(css.MkOr).Determinize(css).Minimize(css);
            //css.ShowGraph(aut, "TrivialWordBoundary1");
            CheckValidity(css, aut, r);
        }

        [TestMethod]
        public void TestTrivialWordBoundary2()
        {
            string r = @"^\b$";
            CharSetSolver css = new CharSetSolver(BitWidth.BV7);
            var aut = css.Convert(r).RemoveEpsilons(css.MkOr).Determinize(css).Minimize(css);
            //css.ShowGraph(aut, "TrivialWordBoundary2");
            CheckValidity(css, aut, r);
        }

        [TestMethod]
        public void TestTrivialWordBoundary3()
        {
            string r = @"^\b";
            CharSetSolver css = new CharSetSolver(BitWidth.BV7);
            var aut = css.Convert(r).RemoveEpsilons(css.MkOr).Determinize(css).Minimize(css);
            //css.ShowGraph(aut, "TrivialWordBoundary3");
            CheckValidity(css, aut, r);
        }

        [TestMethod]
        public void TestTrivialWordBoundary4()
        {
            string r = @"\b[A@]\b$"; 
            CharSetSolver css = new CharSetSolver(BitWidth.BV16);
            var aut = css.Convert(r, RegexOptions.Singleline, true);
            //css.ShowGraph(aut, "TrivialWordBoundary4_with_b");
            css.RegexConverter.EliminateBoundaryStates(aut);
            var aut1 = aut.RemoveEpsilons(css.MkOr).Determinize(css).Minimize(css);
            //css.ShowGraph(aut1, "TrivialWordBoundary4");
            CheckValidity(css, aut, r);
        }

        [TestMethod]
        public void TestTrivialWordBoundary()
        {
            for (int i = 0; i <= 0xFFFF; i++)
            {
                if (i != 0x200C && i != 0x200D)
                {
                    char c = (char)i;
                    string s = c.ToString();
                    var s_ends_with_wordboundary = Regex.IsMatch(s, @"\b$");
                    var s_is_wordletter = Regex.IsMatch(s, @"\w");
                    Assert.AreEqual<bool>(s_ends_with_wordboundary, s_is_wordletter);
                }
            }
        }
    }
}