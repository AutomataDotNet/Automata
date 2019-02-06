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

namespace Automata.Tests
{
    [TestClass]
    public class ConditionalDerivativeTests
    {
        [TestMethod]
        public void TestConditionalDerivativeEnumeration()
        {
            var regex = new Regex("((ab){3,9}){7}");
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true,false)).Pattern;
            var a = q1.builder.solver.MkCharConstraint('a');
            var b = q1.builder.solver.MkCharConstraint('b');
            //---
            var q1_a_derivs = q1.GetConditinalDerivatives(a);
            var q1_b_derivs = q1.GetConditinalDerivatives(b);
            Assert.IsTrue(q1_b_derivs.Count == 0);
            Assert.IsTrue(q1_a_derivs.Count == 1);
            Assert.IsTrue(q1_a_derivs[0].Condition.Length == 2);
            //---
            var q2 = q1_a_derivs[0].PartialDerivative;
            var q2_a_derivs = q2.GetConditinalDerivatives(a);
            var q2_b_derivs = q2.GetConditinalDerivatives(b);
            Assert.IsTrue(q2_a_derivs.Count == 0);
            Assert.IsTrue(q2_b_derivs.Count == 1);
            Assert.IsTrue(q2_b_derivs[0].Condition.Length == 0);
            //---
            var q3 = q2_b_derivs[0].PartialDerivative;
            var q3_a_derivs = q3.GetConditinalDerivatives(a);
            var q3_b_derivs = q3.GetConditinalDerivatives(b);
            Assert.IsTrue(q3_b_derivs.Count == 0);
            Assert.IsTrue(q3_a_derivs.Count == 2);
        }

        [TestMethod]
        public void TestConditionalDerivativeEnumeration2()
        {
            var regex = new Regex("ab{3,10}a");
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var a = q1.builder.solver.MkCharConstraint('a');
            var b = q1.builder.solver.MkCharConstraint('b');
            //---
            var q1_a_derivs = q1.GetConditinalDerivatives(a);
            var q1_b_derivs = q1.GetConditinalDerivatives(b);
            Assert.IsTrue(q1_b_derivs.Count == 0);
            Assert.IsTrue(q1_a_derivs.Count == 1);
            Assert.IsTrue(q1_a_derivs[0].Condition.Length == 0);
            //---
            var q2 = q1_a_derivs[0].PartialDerivative;
            var q2_a_derivs = q2.GetConditinalDerivatives(a);
            var q2_b_derivs = q2.GetConditinalDerivatives(b);
            Assert.IsTrue(q2_a_derivs.Count == 1);
            Assert.IsTrue(q2_b_derivs.Count == 1);
            Assert.IsTrue(q2_b_derivs[0].Condition.Length == 1);
            Assert.IsTrue(q2_a_derivs[0].Condition.Length == 1);
            //---
            var q3 = q2_b_derivs[0].PartialDerivative;
            Assert.IsTrue(q3.Equals(q2));
            var q4 = q2_a_derivs[0].PartialDerivative;
            Assert.IsTrue(q4.Equals(q4.builder.epsilon));


        }

        [TestMethod]
        public void TestConditionalDerivativeEnumeration3()
        {
            var regex = new Regex("((ac){10}|(ab){20}){50}");
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var a = q1.builder.solver.MkCharConstraint('a');
            var b = q1.builder.solver.MkCharConstraint('b');
            //---
            var q1_a_derivs = q1.GetConditinalDerivatives(a);
            var q1_b_derivs = q1.GetConditinalDerivatives(b);
            Assert.IsTrue(q1_b_derivs.Count == 0);
            Assert.IsTrue(q1_a_derivs.Count == 2);
            Assert.IsTrue(q1_a_derivs[0].Condition.Length == 2);
            //---
            var q2 = q1_a_derivs[1].PartialDerivative;
            var q2_a_derivs = q2.GetConditinalDerivatives(a);
            var q2_b_derivs = q2.GetConditinalDerivatives(b);
            Assert.IsTrue(q2_a_derivs.Count == 0);
            Assert.IsTrue(q2_b_derivs.Count == 1);
            Assert.IsTrue(q2_b_derivs[0].Condition.Length == 0);
            //---
        }

        [TestMethod]
        public void TestConditionalDerivativeExploration()
        {
            var regex = new Regex(".*a.{10}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            //aut.ShowGraph("CA");
        }

        [TestMethod]
        public void TestConditionalDerivativeExploration2()
        {
            var regex = new Regex(".*a(.|..){10}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            //aut.ShowGraph("CA2");
        }

        [TestMethod]
        public void TestConditionalDerivativeExploration3()
        {
            var regex = new Regex(".*a(.|..){10,20}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            //aut.ShowGraph("CA3");
        }

        [TestMethod]
        public void TestNestedLoopExploration()
        {
            var regex = new Regex("((ab){5}){7}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            //aut.ShowGraph("nestedloop");
        }

        [TestMethod]
        public void TestNestedNestedLoopExploration()
        {
            var regex = new Regex("((a{3}){4}){5}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            Assert.IsTrue(aut.MoveCount == 6);
            //aut.ShowGraph("nestednestedloop");
        }

        [TestMethod]
        public void TestConditionalDerivativeExploration4()
        {
            var regex = new Regex("(a{3})*", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            //aut.ShowGraph("test");
        }

        [TestMethod]
        public void TestConditionalDerivativeExploration5()
        {
            var regex = new Regex("a.{4}b", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            Assert.IsTrue(aut.MoveCount == 6);
            //aut.ShowGraph("test");
        }

        [TestMethod]
        public void TestConditionalDerivativeExploration_LoopTwice()
        {
            var regex = new Regex("a{9}a{9}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            Assert.IsTrue(aut.MoveCount == 4);
            //aut.ShowGraph("a9a9");
        }

        [TestMethod]
        public void TestConditionalDerivativeExploration_TwoLoops()
        {
            var regex = new Regex("a{9}a{0,9}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            Assert.IsTrue(aut.MoveCount == 5);
            //aut.ShowGraph("a9a09");
        }

        [TestMethod]
        public void TestConditionalDerivativeExploration_LoopWithNoUpperBound()
        {
            var regex = new Regex("ab{10,}", RegexOptions.Singleline);
            var q1 = ((SymbolicRegex<ulong>)regex.Compile(true, false)).Pattern;
            var aut = q1.Explore();
            Assert.IsTrue(aut.MoveCount == 6);
            aut.ShowGraph("ab10bstar");
        }
    }
}
