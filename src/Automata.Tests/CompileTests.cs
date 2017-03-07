using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Automata.Tests
{
    [TestClass]
    public class CompileTests
    {
        [TestMethod]
        public void CompileTest()
        {
            var solver = new CharSetSolver();
            string regex = @"ab|ac";
            var fa = solver.Convert(regex);
            var fadet = fa.Determinize().Minimize();
            fadet.ShowGraph("fadet");
            var cs = fadet.Compile();
            var aut = cs.Automaton;
            Assert.AreEqual<int>(aut.Transition(0, 'd'), 0);
            Assert.AreEqual<int>(aut.Transition(0, 'c', 'a'), 1);
            Assert.AreEqual<int>(aut.Transition(1, 'b', 'f', 'o', 'o'), 2);
            Assert.IsTrue(aut.IsFinalState(2));
            Assert.IsTrue(aut.IsSinkState(2));
            Assert.IsFalse(aut.IsSinkState(1));
            Assert.IsFalse(aut.IsFinalState(0));
        }

        [TestMethod]
        public void CompileEvilRegex()
        {
            var regex = @"^(([a-z])+.)+[A-Z]([a-z])+$";
            Regex EvilRegex = new Regex(regex, RegexOptions.Compiled | (RegexOptions.Singleline));
            string a = "aaaaaaaaaaaaaaaaaaa";
            //takes time exponential in the length of a
            int t = 0;
            for (int i = 0; i < 15; i++)
            {
                t = System.Environment.TickCount;
                EvilRegex.IsMatch(a);
                t = System.Environment.TickCount - t;
                a += "a";
            }
            Assert.IsTrue(t > 1000);
            var solver = new CharSetSolver();
            var fa = solver.Convert(regex);
            var fadet = fa.Determinize().Minimize();
            var cs = fadet.Compile();
            //fadet.ShowGraph("EvilRegex");
        }



    }
}
