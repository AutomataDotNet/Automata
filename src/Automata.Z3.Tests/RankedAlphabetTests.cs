using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Automata;
using Microsoft.Z3;

namespace Automata.Z3.Tests
{
    [TestClass]
    public class Z3_RankedAlphabetTests
    {
        [TestMethod]
        public void ZeroOneTwoAlphabetTest1()
        {
            Z3Provider z3p = new Z3Provider();
            var A = z3p.TT.MkRankedAlphabet("A", z3p.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 });
            var Asort = A.AlphabetSort;
            Assert.IsTrue(A.AlphabetSort.Equals(Asort));
            Assert.IsTrue(A.ContainsConstructor("one"));
            Assert.IsFalse(A.ContainsConstructor("one1"));
            Assert.IsTrue(A.GetRank("one") == 1);
            Assert.IsTrue(z3p.GetDomain(A.GetTester("one")).Length == 1);
            Assert.IsTrue(z3p.GetDomain(A.GetTester("one"))[0].Equals(Asort));
            Assert.IsTrue(z3p.GetDomain(A.GetConstructor("one")).Length == 2);
            Assert.IsTrue(z3p.GetDomain(A.GetConstructor("one"))[0].Equals(z3p.IntSort));
            Assert.IsTrue(z3p.GetDomain(A.GetConstructor("one"))[1].Equals(Asort));
            Assert.IsTrue(z3p.GetRange(A.GetConstructor("one")).Equals(Asort));
            Assert.IsTrue(z3p.GetDomain(A.GetChildAccessor("one", 1)).Length.Equals(1));
            Assert.IsTrue(z3p.GetDomain(A.GetChildAccessor("one", 1))[0].Equals(Asort));
            Assert.IsTrue(z3p.GetRange(A.GetChildAccessor("one", 1)).Equals(Asort));
            Assert.IsTrue(z3p.GetDomain(A.GetAttributeAccessor("one")).Length.Equals(1));
            Assert.IsTrue(z3p.GetDomain(A.GetAttributeAccessor("one"))[0].Equals(Asort));
            Assert.IsTrue(z3p.GetRange(A.GetAttributeAccessor("one")).Equals(z3p.IntSort));
        }

        [TestMethod]
        public void ZeroOneTwoAlphabetTest2()
        {
            Z3Provider z3p = new Z3Provider();
            try
            {   //number of ranks is wrong
                var A = z3p.TT.MkRankedAlphabet("A", z3p.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1 });
                Assert.IsTrue(false, "expecting exception RankedAlphabet_IsInvalid");
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(AutomataExceptionKind.RankedAlphabet_IsInvalid == e.kind);
            }
        }

        [TestMethod]
        public void ZeroOneTwoAlphabetTest3()
        {
            Z3Provider z3p = new Z3Provider();
            try
            {
                //names are repeated
                var A = z3p.TT.MkRankedAlphabet("A", z3p.IntSort, new string[] { "zero", "one", "one" }, new int[] { 0, 1, 2 });
                Assert.IsTrue(false, "expecting exception RankedAlphabet_IsInvalid");
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(AutomataExceptionKind.RankedAlphabet_IsInvalid == e.kind);
            }
        }

        [TestMethod]
        public void ZeroOneTwoAlphabetTest4()
        {
            Z3Provider z3p = new Z3Provider();
            try
            {
                //no leaf
                var A = z3p.TT.MkRankedAlphabet("A", z3p.IntSort, new string[] { "one", "two" }, new int[] { 1, 2 });
                Assert.IsTrue(false, "expecting exception RankedAlphabet_ContainsNoLeaf");
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(AutomataExceptionKind.RankedAlphabet_ContainsNoLeaf == e.kind);
            }
        }

        [TestMethod]
        public void ZeroOneTwoAlphabetTest5()
        {
            Z3Provider z3p = new Z3Provider();
            try
            {
                //empty name or only whitespace
                var A = z3p.TT.MkRankedAlphabet("A", z3p.IntSort, new string[] { "  ", "one", "two" }, new int[] { 0, 1, 2 });
                Assert.IsTrue(false, "expecting exception RankedAlphabet_IsInvalid");
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(AutomataExceptionKind.RankedAlphabet_IsInvalid == e.kind);
            }
        }

        [TestMethod]
        public void ZeroOneTwoAlphabetTest6()
        {
            try
            {
                Z3Provider z3p = new Z3Provider();
                var A = z3p.TT.MkRankedAlphabet("A", z3p.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 });
                var f = A.GetAttributeAccessor("foo");
                Assert.IsTrue(false, "expecting exception RankedAlphabet_UnknownSymbol");
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(AutomataExceptionKind.RankedAlphabet_UnknownSymbol == e.kind);
            }
        }

        [TestMethod]
        public void ZeroOneTwoAlphabetTest7()
        {
            try
            {
                Z3Provider z3p = new Z3Provider();
                var A = z3p.TT.MkRankedAlphabet("A", z3p.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 });
                var f = A.GetChildAccessor("one", 4);
                Assert.IsTrue(false, "expecting exception RankedAlphabet_ChildAccessorIsOutOufBounds");
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(AutomataExceptionKind.RankedAlphabet_ChildAccessorIsOutOufBounds == e.kind);
            }
        }

        [TestMethod]
        public void ZeroOneTwoAlphabetTest8()
        {
            try
            {
                Z3Provider z3p = new Z3Provider();
                var A = z3p.TT.MkRankedAlphabet("A", z3p.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 });
                z3p.TT.GetRankedAlphabet(z3p.IntSort);
                Assert.IsTrue(false, "expecting exception RankedAlphabet_UnrecognizedAlphabetSort");
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(AutomataExceptionKind.RankedAlphabet_UnrecognizedAlphabetSort == e.kind);
            }
        }

        [TestMethod]
        public void ZeroOneTwoAlphabetTest9()
        {
            try
            {
                Z3Provider z3p = new Z3Provider();
                var A = z3p.TT.MkRankedAlphabet("A", z3p.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 });
                z3p.TT.GetRankedAlphabetSort("B", z3p.IntSort);
                Assert.IsTrue(false, "expecting exception RankedAlphabet_UnrecognizedAlphabetSort");
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(AutomataExceptionKind.RankedAlphabet_UnrecognizedAlphabetSort == e.kind);
            }
        }

        [TestMethod]
        public void TestCreationOfTwoAlphabets1()
        {
            Z3Provider Z = new Z3Provider();
            var A = Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 });
            var B = Z.TT.MkRankedAlphabet("B", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 });
            Assert.IsTrue(A.ContainsConstructor("two"));
            Assert.IsTrue(B.ContainsConstructor("two"));
            var twoA = A.GetConstructor("two");
            var twoB = B.GetConstructor("two");
            Assert.AreNotEqual<FuncDecl>(twoA, twoB);
            Assert.AreEqual<string>("two", twoA.Name.ToString());
            Assert.AreEqual<string>("two", twoB.Name.ToString());
        }

        [TestMethod]
        public void TestCreationOfTwoAlphabets2()
        {
            try
            { 
                Z3Provider Z = new Z3Provider();
                var A = Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 });
                var B = Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "two" }, new int[] { 0, 1 });
                Assert.IsTrue(false, "expecting exception RankedAlphabet_IsAlreadyDefined");
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(e.kind == AutomataExceptionKind.RankedAlphabet_IsAlreadyDefined);
            }
        }
    }
}