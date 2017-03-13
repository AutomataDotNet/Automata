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
            var aut = cs.Automaton;
            Assert.IsFalse(aut.IsFinalState(aut.Transition(0, "ab01".ToCharArray())));
            Assert.IsTrue(aut.IsFinalState(aut.Transition(0, "01ab".ToCharArray())));
            Assert.IsFalse(aut.IsFinalState(aut.Transition(0, "0881abc".ToCharArray())));
            Assert.IsTrue(aut.IsFinalState(aut.Transition(0, "0881ac".ToCharArray())));
            Assert.IsFalse(aut.IsFinalState(aut.Transition(0, "013333a.".ToCharArray())));
        }

        [TestMethod]
        public void TestRegexMatcher()
        {
            Assert.IsFalse(RegexMatcher.IsMatch("ab01"));
            Assert.IsTrue(RegexMatcher.IsMatch("01ab"));
            Assert.IsFalse(RegexMatcher.IsMatch("0881abc"));
            Assert.IsTrue(RegexMatcher.IsMatch("0881ac"));
            Assert.IsFalse(RegexMatcher.IsMatch("013333a."));
        }

        /// <summary>
        /// generated from the above unit test 
        /// </summary>
        public class RegexMatcher : Microsoft.Automata.IDeterministicFiniteAutomaton
        {

            System.Func<char, int>[] delta = new System.Func<char, int>[5];

            public System.Func<char, int>[] Delta { get { return delta; } }

            public bool IsFinalState(int x) { return (0x2 <= x && x <= 0x3); }

            public bool IsSinkState(int x) { return (x == 0x4); }

            //return the state from the given state after reading the input
            public int Transition(int state, params char[] input)
            {
                int x = state;
                for (int i = 0; i < input.Length; i++)
                {
                    x = delta[x](input[i]);
                    if ((x == 0x4))
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
                    return 4;
                };
                delta[1] = x =>
                {
                    if ((0x30 <= x && x <= 0x39))
                        return 1;
                    if ((x < 0x61 ? (0x41 <= x && x <= 0x44) : (x <= 0x64)))
                        return 2;
                    return 4;
                };
                delta[2] = x =>
                {
                    if ((x < 0x61 ? (0x41 <= x && x <= 0x44) : (x <= 0x64)))
                        return 3;
                    return 4;
                };
                delta[3] = x =>
                {
                    return 4;
                };
                delta[4] = x =>
                {
                    return 4;
                };
            }

            public static bool IsMatch(string input)
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
                goto State4;
            State1:
                i += 1;
                if (i == k)
                    return false;
                x = (int)cs[i];
                if ((0x30 <= x && x <= 0x39))
                    goto State1;
                if ((x < 0x61 ? (0x41 <= x && x <= 0x44) : (x <= 0x64)))
                    goto State2;
                goto State4;
            State2:
                i += 1;
                if (i == k)
                    return true;
                x = (int)cs[i];
                if ((x < 0x61 ? (0x41 <= x && x <= 0x44) : (x <= 0x64)))
                    goto State3;
                goto State4;
            State3:
                i += 1;
                if (i == k)
                    return true;
                x = (int)cs[i];
                goto State4;
            State4:
                return false;
            }
        }
    }
}