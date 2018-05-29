using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata;
using System.Collections.Generic;

namespace Automata.Tests
{
    [TestClass]
    public class DeterminizationTests
    {
        [TestMethod]
        public void CheckDeterminize()
        {
            var solver = new CharSetSolver();
            string regex = @"^[0-9]+(?i:[a-d])$|^[0-9]+[a-dA-D]{2}$";
            var fa = solver.Convert(regex, System.Text.RegularExpressions.RegexOptions.Singleline);
            //fa.ShowGraph("fa");
            var fadet = fa.Determinize().Normalize();
            //fadet.ShowGraph("fadet");
            //fadet.Normalize().ShowGraph("fadet_norm");
            var cs = fadet.Compile();
            //var aut = cs.Automaton;
            //Assert.IsFalse(fadet.IsFinalState(fadet.Transition(0, "ab01".ToCharArray())));
            //Assert.IsTrue(fadet.IsFinalState(fadet.Transition(0, "01ab".ToCharArray())));
            //Assert.IsFalse(fadet.IsFinalState(fadet.Transition(0, "0881abc".ToCharArray())));
            //Assert.IsTrue(fadet.IsFinalState(fadet.Transition(0, "0881ac".ToCharArray())));
            //Assert.IsFalse(fadet.IsFinalState(fadet.Transition(0, "013333a.".ToCharArray())));
        }

        [TestMethod]
        public void TestRegexMatcher()
        {
            var aut = new RegexMatcher();
            Assert.IsFalse(aut.IsMatch("ab01"));
            Assert.IsTrue(aut.IsMatch("01ab"));
            Assert.IsFalse(aut.IsMatch("0881abc"));
            Assert.IsTrue(aut.IsMatch("0881ac"));
            Assert.IsFalse(aut.IsMatch("013333a."));
        }

        /// <summary>
        /// generated from the above unit test 
        /// </summary>
        public class RegexMatcher : Microsoft.Automata.IDeterministicFiniteAutomaton
        {

            System.Func<char, int>[] delta = new System.Func<char, int>[5];

            int prevStartIndex = 0;

            int currIndex = 0;

            public System.Func<char, int>[] Delta { get { return delta; } }

            public bool IsFinalState(int x) { return (0x3 <= x && x <= 0x4); }

            public bool IsSinkState(int x) { return (x == 0x2); }

            //return the state from the given state after reading the input
            public int Transition(int state, params char[] input)
            {
                int x = state;
                for (int i = 0; i < input.Length; i++)
                {
                    x = delta[x](input[i]);
                    if ((x == 0x2))
                        return x;
                }
                return x;
            }

            public RegexMatcher()
            {
                delta[0] = x =>
                {
                    if ((0x30 <= x && x <= 0x39))
                        return 1;
                    return 2;
                };
                delta[1] = x =>
                {
                    if ((0x30 <= x && x <= 0x39))
                        return 1;
                    if ((x < 0x61 ? (0x41 <= x && x <= 0x44) : (x <= 0x64)))
                        return 3;
                    return 2;
                };
                delta[2] = x =>
                {
                    return 2;
                };
                delta[3] = x =>
                {
                    if ((x < 0x61 ? (0x41 <= x && x <= 0x44) : (x <= 0x64)))
                        return 4;
                    return 2;
                };
                delta[4] = x =>
                {
                    return 2;
                };
            }

            public bool IsMatch(string input)
            {
                var cs = input.ToCharArray();
                int k = input.Length;
                int x = 0;
                int i = -1;
                i += 1;
                if (i == k)
                    return false;
                x = (int)cs[i];
                if ((0x30 <= x && x <= 0x39))
                    goto State1;
                goto State2;
            State1:
                i += 1;
                if (i == k)
                    return false;
                x = (int)cs[i];
                if ((0x30 <= x && x <= 0x39))
                    goto State1;
                if ((x < 0x61 ? (0x41 <= x && x <= 0x44) : (x <= 0x64)))
                    goto State3;
                goto State2;
            State2:
                return false;
            State3:
                i += 1;
                if (i == k)
                    return true;
                x = (int)cs[i];
                if ((x < 0x61 ? (0x41 <= x && x <= 0x44) : (x <= 0x64)))
                    goto State4;
                goto State2;
            State4:
                i += 1;
                if (i == k)
                    return true;
                x = (int)cs[i];
                goto State2;
            }

            public int GenerateMatches(string input, System.Tuple<int, int>[] matches)
            {
                var cs = input.ToCharArray();
                int i0 = 0;
                int q = 0;
                int j = 0;
                for (int i = 0; i < cs.Length; i++)
                {
                    if (j == matches.Length)
                        return j;
                    if (q == 0)
                        i0 = i;
                    q = this.delta[q](cs[i]);
                    if (this.IsFinalState(q))
                    {
                        matches[j] = new System.Tuple<int, int>(i0, i);
                        j += 1;
                        q = 0;
                    }
                }
                return j;
            }
        }
    }
}