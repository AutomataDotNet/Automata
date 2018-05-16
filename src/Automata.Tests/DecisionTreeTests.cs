using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;
using Microsoft.Automata.BooleanAlgebras;
using System.Text.RegularExpressions;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class DecisionTreeTests
    {
        [TestMethod]
        public void TestDecisionTreePrecomputeASCII()
        {
            var css = new CharSetSolver();
            var regex = new Regex("abc[^a-i]");
            var sr = css.RegexConverter.ConvertToSymbolicRegex(regex, true);
            var partition = sr.ComputeMinterms();
            Assert.AreEqual<int>(5, partition.Length);
            var dt = DecisionTree.Create(css, partition, 127);
            for (int i = 0; i < partition.Length; i++)
            {
                foreach (var c in css.GenerateAllCharacters(partition[i]))
                    Assert.AreEqual(i, dt.GetId(c));
            }
            Assert.IsTrue(dt.Tree.IsLeaf);
        }

        [TestMethod]
        public void TestDecisionTreePrecomputeExtendedASCII()
        {
            var css = new CharSetSolver();
            var regex = new Regex("(?i:abc[^a-i])");
            var sr = css.RegexConverter.ConvertToSymbolicRegex(regex, true);
            var partition = sr.ComputeMinterms();
            Assert.AreEqual<int>(5, partition.Length);
            var dt = DecisionTree.Create(css, partition, 0xFF);
            for (int i = 0; i < partition.Length; i++)
            {
                foreach (var c in css.GenerateAllCharacters(partition[i]))
                    Assert.AreEqual(i, dt.GetId(c));
            }
            //there is a special unicode 'i' character that is equivalent to i with ignore-case option
            //that forms a separate character class here
            Assert.IsFalse(dt.Tree.IsLeaf);
        }

        [TestMethod]
        public void TestDecisionTreePrecomputeNothing()
        {
            var css = new CharSetSolver();
            var regex = new Regex("abc[^a-i]");
            var sr = css.RegexConverter.ConvertToSymbolicRegex(regex, true);
            var partition = sr.ComputeMinterms();
            Assert.AreEqual<int>(5, partition.Length);
            var dt = DecisionTree.Create(css, partition, 0);
            for (int i = 0; i < partition.Length; i++)
            {
                foreach (var c in css.GenerateAllCharacters(partition[i]))
                    Assert.AreEqual(i, dt.GetId(c));
            }
            Assert.IsFalse(dt.Tree.IsLeaf);
        }

        [TestMethod]
        public void TestDecisionTreePrecomputeEverything()
        {
            var css = new CharSetSolver();
            var regex = new Regex("abc[^a-i]");
            var sr = css.RegexConverter.ConvertToSymbolicRegex(regex, true);
            var partition = sr.ComputeMinterms();
            Assert.AreEqual<int>(5, partition.Length);
            var dt = DecisionTree.Create(css, partition, 0xFFFF);
            for (int i = 0; i < partition.Length; i++)
            {
                foreach (var c in css.GenerateAllCharacters(partition[i]))
                    Assert.AreEqual(i, dt.GetId(c));
            }
            Assert.IsTrue(dt.Tree == null);
        }
    }
}
