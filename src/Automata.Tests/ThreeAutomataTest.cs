using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class ThreeAutomataTest
    {
        [TestMethod]
        public void CheckBigSmall()
        {
            CharSetSolver solver = new CharSetSolver();
            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 1, solver.True));
            moves.Add(new Move<BDD>(1, 2, solver.True));
            var sfa = ThreeAutomaton<BDD>.Create(0, new int[] { 0 }, new int[] { 2 }, moves);

            var big = sfa.GetBiggestLanguageSFA();
            var small = sfa.GetSmallestLanguageSFA();

            Assert.IsTrue(small.Minus(big, solver).IsEmpty);
            Assert.IsFalse(big.Minus(small, solver).IsEmpty);
        }

        [TestMethod]
        public void CheckComplement()
        {
            CharSetSolver solver = new CharSetSolver();
            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 1, solver.True));
            moves.Add(new Move<BDD>(1, 2, solver.True));
            var sfa = ThreeAutomaton<BDD>.Create(0, new int[] { 0 }, new int[] { 2 }, moves);
            var csfa = sfa.MkComplement(solver);

            Assert.IsFalse(sfa.GetBiggestLanguageSFA().Intersect(csfa.GetBiggestLanguageSFA(),solver).IsEmpty);
            Assert.IsTrue(sfa.GetSmallestLanguageSFA().Intersect(csfa.GetSmallestLanguageSFA(), solver).IsEmpty);
        }

        [TestMethod]
        public void CheckEquivalence()
        {
            CharSetSolver solver = new CharSetSolver();
            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 1, solver.True));
            moves.Add(new Move<BDD>(1, 2, solver.True));
            var sfa1 = ThreeAutomaton<BDD>.Create(0, new int[] { 0 }, new int[] { 2 }, moves);

            var c = solver.MkCharConstraint(false,'c');
            moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 1, c));
            moves.Add(new Move<BDD>(1, 2, solver.True));
            moves.Add(new Move<BDD>(0, 3, solver.MkNot(c)));
            moves.Add(new Move<BDD>(3, 2, solver.True));
            var sfa2 = ThreeAutomaton<BDD>.Create(0, new int[] { 0 }, new int[] { 2 }, moves);

            Assert.IsTrue(sfa1.IsEquivalentWith(sfa2,solver));
        }

        [TestMethod]
        public void CheckMinimization()
        {
            CharSetSolver solver = new CharSetSolver();
            var moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 1, solver.True));
            moves.Add(new Move<BDD>(1, 2, solver.True));
            var sfa1 = ThreeAutomaton<BDD>.Create(0, new int[] { 0 }, new int[] { 2 }, moves);

            var min = sfa1.Minimize(solver);

            Assert.IsTrue(min.StateCount==4);
        }

        [TestMethod]
        public void CheckDeMorgan()
        {
            CharSetSolver solver = new CharSetSolver();
            var moves = new List<Move<BDD>>();
            var c = solver.MkCharConstraint(false, 'a');

            moves.Add(new Move<BDD>(0, 1, c));
            moves.Add(new Move<BDD>(1, 2, solver.True));
            var sfa1 = ThreeAutomaton<BDD>.Create(0, new int[] { 0 }, new int[] { 2 }, moves);

            
            moves = new List<Move<BDD>>();
            moves.Add(new Move<BDD>(0, 1, c));
            moves.Add(new Move<BDD>(1, 2, solver.True));
            moves.Add(new Move<BDD>(0, 3, solver.MkNot(c)));
            moves.Add(new Move<BDD>(3, 2, solver.True));
            var sfa2 = ThreeAutomaton<BDD>.Create(0, new int[] { 0 }, new int[] { 2 }, moves);

            var inters = sfa1.Intersect(sfa2,solver);
            var union = sfa1.Union(sfa2, solver);

            var u2 = sfa1.MkComplement(solver).Intersect(sfa1.MkComplement(solver), solver).MkComplement(solver);

        }
    }
}

