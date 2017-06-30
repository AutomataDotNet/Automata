using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Microsoft.Automata;

using System.Text.RegularExpressions;


namespace Automata.Tests
{
    [TestClass]
    public class MinimizationTests
    {
        [TestMethod]
        public void TestNFA1()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);
            var nfa = Automaton<BDD>.Create(solver, 0, new int[] { 1 }, new Move<BDD>[] { new Move<BDD>(0, 1, solver.True), new Move<BDD>(0, 2, solver.True), new Move<BDD>(2, 1, solver.True) });
            var min_nfa = nfa.Minimize();
            nfa.isDeterministic = true; //pretend that nfa is equivalent, causes the deterministic version to be executed that provides the wrong result
            var min_nfa_wrong = nfa.Minimize();
            nfa.isDeterministic = false;
            min_nfa_wrong.isDeterministic = false;
            Assert.IsFalse(min_nfa.IsEquivalentWith(min_nfa_wrong));
            Assert.IsTrue(min_nfa.IsEquivalentWith(nfa));
        }

        [TestMethod]
        public void TestNFA2()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);
            var a = solver.MkCharConstraint('a');
            var na = solver.MkNot(a);
            var nfa = Automaton<BDD>.Create(solver, 0, new int[] { 1 }, new Move<BDD>[] { new Move<BDD>(0, 1, solver.True), new Move<BDD>(0, 2, solver.True), new Move<BDD>(2, 1, solver.True), new Move<BDD>(1, 1, a), new Move<BDD>(1, 2, na) });
            var min_nfa = nfa.Minimize();
            nfa.isDeterministic = true; //pretend that nfa is equivalent, causes the deterministic version to be executed that provides the wrong result
            var min_nfa_wrong = nfa.Minimize();
            nfa.isDeterministic = false;
            min_nfa_wrong.isDeterministic = false;
            //min_nfa.ShowGraph("min_nfa");
            //min_nfa_wrong.ShowGraph("min_nfa_wrong");
            //min_nfa.Determinize().Minimize().ShowGraph("min_nfa1");
            //nfa.Determinize().Minimize().ShowGraph("dfa");
            //nfa.ShowGraph("nfa");
            //min_nfa_wrong.Determinize().Minimize().ShowGraph("min_nfa2");
            Assert.IsFalse(min_nfa.IsEquivalentWith(min_nfa_wrong));
            Assert.IsTrue(min_nfa.IsEquivalentWith(nfa));
            //concrete witness "abab" distinguishes nfa from min_nfa_wrong
            Assert.IsTrue(solver.Convert("^abab$").Intersect(nfa).IsEmpty);
            Assert.IsFalse(solver.Convert("^abab$").Intersect(min_nfa_wrong).IsEmpty);

        }
    }
}
