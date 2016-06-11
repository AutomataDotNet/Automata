using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class EditDistanceTest
    {
        [TestMethod]
        public void EDTest1()
        {
            CharSetSolver solver = new CharSetSolver();
            string a = "aaaaaa";

            var aut = solver.Convert("^(ab)*$").Determinize().Minimize();

            int dist;
            var output = EditDistance.GetClosestElement(a, aut, solver, out dist);

            Console.WriteLine("string: {0}, distance: {1}", output, dist);
            Assert.IsTrue(dist == 3);

            output = EditDistance.GetClosestElement("aba", aut, solver, out dist);
            Console.WriteLine("string: {0}, distance: {1}", output, dist);
            Assert.IsTrue(dist == 1);
        }

        [TestMethod]
        public void EDTest2()
        {
            CharSetSolver solver = new CharSetSolver();
            string a = "aa";

            var aut = solver.Convert("^(a|b){3}$").Determinize().Minimize();

            int dist;
            var output = EditDistance.GetClosestElement(a, aut, solver, out dist);

            Console.WriteLine("string: {0}, distance: {1}", output, dist);
            Assert.IsTrue(dist == 1);

            output = EditDistance.GetClosestElement("bc", aut, solver, out dist);
            Console.WriteLine("string: {0}, distance: {1}", output, dist);
            Assert.IsTrue(dist == 2);
        }

        [TestMethod]
        public void EDTestNew()
        {
            CharSetSolver solver = new CharSetSolver();
            string a = "absabaasd";

            var aut = solver.Convert("^((ab|b){1,2}cc)*$").Determinize().Minimize();

            int dist;
            var output = EditDistance.GetClosestElement(a, aut, solver, out dist);

            Console.WriteLine("string: {0}, distance: {1}", output, dist);
            Assert.IsTrue(dist == 5);

            output = EditDistance.GetClosestElement("aba", aut, solver, out dist);
            Console.WriteLine("string: {0}, distance: {1}", output, dist);
            Assert.IsTrue(dist == 2);
        }
    }
}
