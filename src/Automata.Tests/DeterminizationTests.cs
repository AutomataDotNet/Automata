using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata;

namespace Automata.Tests
{
    [TestClass]
    public class DeterminizationTests
    {
        [TestMethod]
        public void CheckDeterminize()
        {
            var solver = new CharSetSolver();
            string regex = @"abc|abcd|abdd|ddabbsd|aaabbc";
            var fa = solver.Convert(regex);
            int t = System.Environment.TickCount;
            var fadet = fa.Determinize();
            t = System.Environment.TickCount - t;
            //fa.ShowGraph();
            //fadet.ShowGraph();
        }
    }
}
