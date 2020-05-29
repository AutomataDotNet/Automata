using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata;

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class CharSetTests
    {
        [TestMethod]
        public void TestRanges()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            BDD cond = solver.MkCharSetFromRange('A', 'Y');
            Tuple<uint, uint>[] ranges = solver.ToRanges(cond);
            Assert.AreEqual<int>(1, ranges.Length);
            Assert.AreEqual<uint>((uint)'A', ranges[0].Item1);
            Assert.AreEqual<uint>((uint)'Y', ranges[0].Item2);
        }

        [TestMethod]
        public void TestDotGen()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);
            BDD cond = solver.MkCharSetFromRange('\0', '\x0F');
            int cnt = (int)solver.ComputeDomainSize(cond);
            cond.ToDot(@"bdd2.dot");
        }

        [TestMethod]
        public void TestDotGenTmp()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV7);
            BDD cond = solver.MkCharSetFromRange('0', '9');
            int cnt = (int)solver.ComputeDomainSize(cond);
            cond.ToDot(@"C:\git\loris\msrpapers\CACM\figures\is_digit_bdd.dot");
        }

        [TestMethod]
        public void TestRanges2()
        {
            BitWidth enc = BitWidth.BV7;
            CharSetSolver solver = new CharSetSolver(enc);
            BDD cond = solver.MkCharSetFromRegexCharClass(@"\w");
            int nodes = cond.CountNodes();
            Tuple<uint, uint>[] ranges = solver.ToRanges(cond);
            BDD cond2 = solver.MkCharSetFromRanges(ranges);
            Assert.AreSame(cond, cond2);
            int nodes2 = cond2.CountNodes();
            Assert.AreEqual<uint>((uint)'0', ranges[0].Item1);
            Assert.AreEqual<uint>((uint)'9', ranges[0].Item2);
            Assert.AreEqual<uint>((uint)'A', ranges[1].Item1);
            Assert.AreEqual<uint>((uint)'Z', ranges[1].Item2);
            Assert.AreEqual<uint>((uint)'_', ranges[2].Item1);
            Assert.AreEqual<uint>((uint)'_', ranges[2].Item2);
            Assert.AreEqual<uint>((uint)'a', ranges[3].Item1);
            Assert.AreEqual<uint>((uint)'z', ranges[3].Item2);
            Assert.AreEqual<int>(4, ranges.Length);
        }

        [TestMethod]
        public void TestRanges2b()
        {
            BitWidth enc = BitWidth.BV16;
            CharSetSolver solver = new CharSetSolver(enc);
            BDD cond = solver.MkCharSetFromRegexCharClass(@"\w");
            var ranges1 = solver.ToRanges(cond);
            var cond1 = solver.MkCharSetFromRanges(ranges1);
            Tuple<uint, uint>[] ranges = solver.ToRanges(cond1);
            var cond2 = solver.MkCharSetFromRanges(ranges);
            Assert.AreSame(cond1, cond2);
            Assert.AreSame(cond, cond1);
            //cond.ToDot("cond.dot");
            Assert.AreEqual<uint>((uint)'0', ranges[0].Item1);
            Assert.AreEqual<uint>((uint)'9', ranges[0].Item2);
            Assert.AreEqual<uint>((uint)'A', ranges[1].Item1);
            Assert.AreEqual<uint>((uint)'Z', ranges[1].Item2);
            Assert.AreEqual<uint>((uint)'_', ranges[2].Item1);
            Assert.AreEqual<uint>((uint)'_', ranges[2].Item2);
            Assert.AreEqual<uint>((uint)'a', ranges[3].Item1);
            Assert.AreEqual<uint>((uint)'z', ranges[3].Item2);
        }

        [TestMethod]
        public void TestRanges3()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            BDD cond = solver.MkCharSetFromRegexCharClass(@"\d");
            int cnt = cond.CountNodes();
            Tuple<uint, uint>[] ranges = solver.ToRanges(cond);
            BDD set = solver.MkCharSetFromRanges(ranges);
            int nodes = set.CountNodes();
            var ranges2 = new List<Tuple<uint, uint>>(ranges);
            ranges2.Reverse();
            BDD set2 = solver.MkCharSetFromRanges(ranges2);
            int nodes2 = set.CountNodes();
            var ranges3 = solver.ToRanges(set2);
            BDD set3 = solver.MkCharSetFromRanges(ranges3);

            int cnt2 = set2.CountNodes();
            int cnt3 = set3.CountNodes();
            Assert.IsTrue(set2 == set3);

            Assert.AreEqual<int>(nodes, nodes2);
            Assert.AreSame(set,set2);

            //set.ToDot("digits.dot"); 

            //check equivalence
            bool equiv = solver.MkOr(solver.MkAnd(cond, solver.MkNot(set)), solver.MkAnd(set, solver.MkNot(cond))) == solver.False;

            Assert.IsTrue(equiv);
        }

        [TestMethod]
        public void TestCardinality()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            BDD cond = solver.MkCharSetFromRegexCharClass(@"\d");
            int cnt = cond.CountNodes();
            Tuple<uint, uint>[] ranges = solver.ToRanges(cond);
            BDD set = solver.MkCharSetFromRanges(ranges);
            int nodes = set.CountNodes();
            int size = (int)solver.ComputeDomainSize(set);
            int expected = 0;
            foreach (var range in ranges)
                expected += ((int)(range.Item2 - range.Item1) + 1);
            Assert.AreEqual<int>(expected, size);
            int digitCnt = 0;
            for (int i = 0; i <= 0xFFFF; i++)
            {
                if (char.IsDigit(((char)i)))
                    digitCnt += 1;
            }
            Assert.AreEqual<int>(digitCnt, size);
        }

        [TestMethod]
        public void TestCardinality2()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            BDD cond = solver.MkCharSetFromRegexCharClass(@"\w");
            int cnt = cond.CountNodes();
            Tuple<uint, uint>[] ranges = solver.ToRanges(cond);
            BDD set = solver.MkCharSetFromRanges(ranges);
            int nodes = set.CountNodes();
            int size = (int)solver.ComputeDomainSize(set);
            int expected = 0;
            foreach (var range in ranges)
                expected += ((int)(range.Item2 - range.Item1) + 1);
            Assert.AreEqual<int>(expected, size);
            int wCnt = 0;
            for (int i = 0; i <= 0xFFFF; i++)
            {
                int cat = (int)char.GetUnicodeCategory((char)i);
                if ( cat == 0 || cat == 1 || cat == 2 || cat == 3 || cat == 4 || cat == 5 ||
                     cat == 8 || cat == 18) //same as \w in regex
                    wCnt += 1;
            }
            Assert.AreEqual<int>(wCnt, size);
        }

        [TestMethod]
        public void TestCardinality3()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            BDD cond = solver.MkCharSetFromRegexCharClass(@"[\w-[\d]]");
            int cnt = cond.CountNodes();
            Tuple<uint, uint>[] ranges = solver.ToRanges(cond);
            BDD set = solver.MkCharSetFromRanges(ranges);
            int nodes = set.CountNodes();
            int size = (int)solver.ComputeDomainSize(set);
            int expected = 0;
            foreach (var range in ranges)
                expected += ((int)(range.Item2 - range.Item1) + 1);
            Assert.AreEqual<int>(expected, size);
            int wCnt = 0;
            for (int i = 0; i <= 0xFFFF; i++)
            {
                int cat = (int)char.GetUnicodeCategory((char)i);
                if (cat == 0 || cat == 1 || cat == 2 || cat == 3 || cat == 4 || cat == 5 ||
                     cat == 8 || cat == 18) //same as \w in regex
                    if (!char.IsDigit((char)i))
                        wCnt += 1;
            }
            Assert.AreEqual<int>(wCnt, size);
        }

        [TestMethod]
        public void TestLargeRange()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            BDD cond = solver.MkCharSetFromRegexCharClass(@"[\u0000-\u7FFF]");
            int elems = (int)solver.ComputeDomainSize(cond);
            int nodes = cond.CountNodes();
            Assert.AreEqual<int>(3, nodes);
            Assert.AreEqual<int>((1 << 15), elems);
        }

        [TestMethod]
        public void TestBvSetSolver()
        {
            var solver = new BDDAlgebra();
            var set = solver.MkOr(solver.MkSetFromRange(10001, uint.MaxValue, 31),solver.MkSetFromRange(5, 1004, 31));
            ulong elems = solver.ComputeDomainSize(set,31);
            int nodes = set.CountNodes();
            uint expected = (uint.MaxValue - 10000) + 1000;
            Assert.AreEqual<ulong>(expected, elems); 
        }

        [TestMethod]
        public void TestBvSetSolver2()
        {
            var solver = new BDDAlgebra();
            var set = solver.MkNot(solver.MkOr(solver.MkSetFromRange(10001, uint.MaxValue, 31), solver.MkSetFromRange(5, 1004, 31)));
            ulong elems = solver.ComputeDomainSize(set,31);
            int nodes = set.CountNodes();
            uint expected = 10000 - 1000 + 1;
            Assert.AreEqual<ulong>(expected, elems);
        }

        [TestMethod]
        public void TestBvSetSolver3()
        {
            var solver = new BDDAlgebra();
            var letters_ASCII = solver.MkOr(solver.MkSetFromRange('a', 'z', 6), solver.MkSetFromRange('A', 'Z', 6));
            var letters_eASCII = solver.MkOr(solver.MkSetFromRange('a', 'z', 7), solver.MkSetFromRange('A', 'Z', 7));

            Assert.AreEqual<ulong>(52, solver.ComputeDomainSize(letters_ASCII, 6));
            Assert.AreEqual<ulong>(52, solver.ComputeDomainSize(letters_eASCII, 7));

            var set7 = solver.MkNot(letters_ASCII);
            ulong elems7 = solver.ComputeDomainSize(set7, 6);
            uint expected7 = (1<<7) - 52;
            Assert.AreEqual<ulong>(expected7, elems7);

            var set8 = solver.MkNot(letters_eASCII);
            ulong elems8 = solver.ComputeDomainSize(set8, 7);
            ulong expected8 = (1<<8) - 52;
            Assert.AreEqual<ulong>(expected8, elems8);
        }

        [TestMethod]
        public void TestBvSetSolver4()
        {
            var solver = new BDDAlgebra();
            var _0_3 = solver.MkSetFromRange(0, 3, 6);
            var _10_13 = solver.MkSetFromRange(0x40, 0x43, 6);

            //_0_3.ToDot("_0_3.dot");
            //_10_13.ToDot("_10_13.dot");

            var _0_3_size = solver.ComputeDomainSize(_0_3, 6);
            var _10_13_size = solver.ComputeDomainSize(_10_13, 6);

            var x = solver.MkAnd(_0_3, _10_13);

            var _0_3_u_10_13 = solver.MkOr(_0_3, _10_13);
            var _0_3_u_10_13_size = solver.ComputeDomainSize(_0_3_u_10_13, 6);

            //_0_3_u_10_13.ToDot("_0_3_u_10_13.dot");

            var _0_3_u_10_13_compl = solver.MkNot(_0_3_u_10_13);
            //_0_3_u_10_13_compl.ToDot("_0_3_u_10_13_compl.dot");


            var _0_3_u_10_13_compl_size = solver.ComputeDomainSize(_0_3_u_10_13_compl, 6);


            Assert.AreEqual<ulong>(4, _0_3_size);
            Assert.AreEqual<ulong>(4, _10_13_size);
            Assert.IsTrue(x.IsEmpty);
            Assert.AreEqual<ulong>(8, _0_3_u_10_13_size);

            var ranges = solver.ToRanges(_0_3_u_10_13, 6);
            Assert.AreEqual<int>(2, ranges.Length);
            Assert.AreEqual<uint>(0, ranges[0].Item1);
            Assert.AreEqual<uint>(3, ranges[0].Item2);
            Assert.AreEqual<uint>(0x40, ranges[1].Item1);
            Assert.AreEqual<uint>(0x43, ranges[1].Item2);
        }

        [TestMethod]
        public void TestBvSetSolver5()
        {
            var solver = new BDDAlgebra();
            var a = solver.MkSetFromRange(0, 3, 6);
            var b = solver.MkSetFromRange(0x40, 0x43, 6);
            var c = solver.MkSetFromRange(0x20, 0x23, 6);
            var d = solver.MkSetFromRange(0x60, 0x63, 6);

            var all = new BDD[] { a, b, c, d };

            //a.ToDot("_0_3.dot");
            //b.ToDot("_10_13.dot");

            var a_size = solver.ComputeDomainSize(a, 6);
            var b_size = solver.ComputeDomainSize(b, 6);

            var x = solver.MkAnd(a, b);

            var u = solver.MkOr(all);
            var u_size = solver.ComputeDomainSize(u, 6);

            //u.ToDot("_0_3_u_10_13.dot");

            var u_compl = solver.MkNot(u);
            //u_compl.ToDot("_0_3_u_10_13_compl.dot");


            var u_compl_size = solver.ComputeDomainSize(u_compl, 6);


            Assert.AreEqual<ulong>(4, a_size);
            Assert.AreEqual<ulong>(4, b_size);
            Assert.IsTrue(x.IsEmpty);
            Assert.AreEqual<ulong>(16, u_size);

            var ranges = solver.ToRanges(u, 6);
            Assert.AreEqual<int>(4, ranges.Length);
            Assert.AreEqual<uint>(0, ranges[0].Item1);
            Assert.AreEqual<uint>(3, ranges[0].Item2);
            Assert.AreEqual<uint>(0x20, ranges[1].Item1);
            Assert.AreEqual<uint>(0x23, ranges[1].Item2);
            Assert.AreEqual<uint>(0x40, ranges[2].Item1);
            Assert.AreEqual<uint>(0x43, ranges[2].Item2);
            Assert.AreEqual<uint>(0x60, ranges[3].Item1);
            Assert.AreEqual<uint>(0x63, ranges[3].Item2);
        }

        [TestMethod]
        public void TestLargeRange2()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            BDD cond = solver.MkCharSetFromRegexCharClass(@"[\u0000-\u7FFF\uA000-\uA00F]");
            uint elems = (uint)solver.ComputeDomainSize(cond);
            int nodes = cond.CountNodes();
            Assert.AreEqual<int>(14, nodes);
            var ranges = solver.ToRanges(cond);
            Assert.AreEqual<int>(2, ranges.Length);
            Assert.AreEqual<uint>(ranges[0].Item1, 0);
            Assert.AreEqual<uint>(ranges[0].Item2, 0x7FFF);
            Assert.AreEqual<uint>(ranges[1].Item1, 0xA000);
            Assert.AreEqual<uint>(ranges[1].Item2, 0xA00F);
            Assert.AreEqual<uint>(((uint)1 << 15) + ((uint)1 << 4), elems);
        }

        [TestMethod]
        public void TestSurrogateRange()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            //high and low surrogate pair elements
            BDD cond = solver.MkCharSetFromRegexCharClass(@"\p{Cs}");
            cond.ToDot("surr.dot");
            int elems = (int)solver.ComputeDomainSize(cond);
            int nodes = cond.CountNodes();
            Assert.AreEqual<int>(7, nodes); //highly compact BDD representation
            var ranges = solver.ToRanges(cond);
            Assert.AreEqual<int>(1, ranges.Length);
            Assert.AreEqual<uint>(ranges[0].Item1, 0xd800);
            Assert.AreEqual<uint>(ranges[0].Item2, 0xdFFF);
            //the total number of surrogates (there are 1024 low surrogates and 1024 high surrogates)
            Assert.AreEqual<int>(2048, elems); 
        }

        [TestMethod]
        public void TestNodeCount()
        {
            CharSetSolver solver = new CharSetSolver(BitWidth.BV16);
            BDD cond = solver.MkCharSetFromRegexCharClass(@"[\x00-\u7FFF]");
            int cnt = cond.CountNodes();
            Assert.AreEqual<int>(3, cnt);
        }
    }

    [TestClass]
    public class BvSetTests
    {
        [TestMethod]
        public void TestShiftRight1()
        {
            BDDAlgebra solver = new BDDAlgebra();
            BDD cond1 = solver.MkSetFromRange(0, 7, 3);
            BDD cond2 = solver.MkSetFromElements(new uint[] {9, 10, 12, 15}, 3);
            BDD cond = solver.MkOr(cond1, cond2);
            Assert.AreEqual<int>(8, cond.CountNodes());
            //Automata.DirectedGraphs.DotWriter.CharSetToDot(cond, "TestShiftRight", "c:/tmp/TestShiftRight1.dot", DirectedGraphs.DotWriter.RANKDIR.TB, 12);
            BDD sr = solver.ShiftRight(cond);
            Assert.AreEqual<BDD>(solver.True, sr);
        }

        [TestMethod]
        public void TestShiftRight2()
        {
            BDDAlgebra solver = new BDDAlgebra();
            BDD cond = solver.MkSetFromElements(new uint[] {0, 15}, 3);
            Assert.AreEqual<int>(9, cond.CountNodes());
            //Automata.DirectedGraphs.DotWriter.CharSetToDot(cond, "TestShiftRight", "c:/tmp/TestShiftRight2.dot", DirectedGraphs.DotWriter.RANKDIR.TB, 12);
            var sr = solver.ShiftRight(cond);
            Assert.AreEqual<int>(7, sr.CountNodes());
            //Automata.DirectedGraphs.DotWriter.CharSetToDot(sr, "TestShiftRight", "c:/tmp/TestShiftRight2res.dot", DirectedGraphs.DotWriter.RANKDIR.TB, 12);
        }

        [TestMethod]
        public void TestShiftLeft1()
        {
            BDDAlgebra solver = new BDDAlgebra();
            BDD cond = solver.MkSetFromElements(new uint[] { 0, 15 }, 3);
            //Automata.DirectedGraphs.DotWriter.CharSetToDot(cond, "TestShiftLeft", "c:/tmp/TestShiftLeftIn.dot", DirectedGraphs.DotWriter.RANKDIR.TB, 12);
            //BvSet bvs = solver.ShiftLeft(cond, 30);
            //Assert.AreEqual<int>(5, bvs.CountNodes());
            BDD bvs2 = solver.ShiftLeft(cond, 28);
            Assert.AreEqual<int>(9, bvs2.CountNodes());
            //Automata.DirectedGraphs.DotWriter.CharSetToDot(bvs, "TestShiftLeft", "c:/tmp/TestShiftLeftOut.dot", DirectedGraphs.DotWriter.RANKDIR.TB, 12);
        }

        [TestMethod]
        public void TestShiftLeftThenRight()
        {
            BDDAlgebra solver = new BDDAlgebra();
            BDD cond = solver.MkSetFromElements(new uint[] { 0, 15 }, 3);
            //Automata.DirectedGraphs.DotWriter.CharSetToDot(cond, "TestShiftLeft2_cond", "c:/tmp/TestShiftLeft2_cond.dot", DirectedGraphs.DotWriter.RANKDIR.TB, 12);
            BDD bvs = solver.ShiftLeft(cond, 4);
            BDD concat = solver.MkAnd(bvs, cond);
            Assert.AreEqual<int>(16, concat.CountNodes());
            BDD cond1 = solver.ShiftRight(solver.ShiftRight(solver.ShiftRight(solver.ShiftRight(concat))));
            Assert.AreEqual<BDD>(cond1, cond);
            //Automata.DirectedGraphs.DotWriter.CharSetToDot(concat, "TestShiftLeft2_concat", "c:/tmp/TestShiftLeft2_concat.dot", DirectedGraphs.DotWriter.RANKDIR.TB, 12);
        }

        [TestMethod]
        public void TestUlongBvsSetCreation()
        {
            BDDAlgebra solver = new BDDAlgebra();
            BDD cond = solver.MkSetFromElements(new ulong[] { 0UL, (ulong)0xFFFFFFFF }, 31);
            BDD bvs = solver.ShiftLeft(cond, 4);
            var ranges = solver.ToRanges64(bvs, bvs.Ordinal);
            Assert.AreEqual<int>(2, ranges.Length);
            Assert.AreEqual<ulong>(0UL, ranges[0].Item1);
            Assert.AreEqual<ulong>(0xFUL, ranges[0].Item2);
            Assert.AreEqual<ulong>((ulong)0xFFFFFFFF0, ranges[1].Item1);
            Assert.AreEqual<ulong>((ulong)0xFFFFFFFFF, ranges[1].Item2);
        }


    }
}
