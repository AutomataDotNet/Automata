using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Automata;
using Microsoft.Z3;

using STz3 = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using Rulez3 = Microsoft.Automata.Rule<Microsoft.Z3.Expr>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;

namespace Automata.Z3.Tests
{
    [TestClass]
    public class Z3_STTests
    {
        [TestMethod]
        public void TestGetTagsAxioms1()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            TestGetTagsAxioms1(stb, stb.Solver.IntSort);
            TestGetTagsAxioms1(stb, stb.Solver.MkBitVecSort(16));
            TestGetTagsAxioms1(stb, stb.Solver.MkBitVecSort(8));
            TestGetTagsAxioms1(stb, stb.Solver.MkBitVecSort(7));
        }

        [TestMethod]
        public void TestGetTagsAxioms2()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            TestGetTagsAxioms2(stb, stb.Solver.IntSort);
            TestGetTagsAxioms2(stb, stb.Solver.MkBitVecSort(16));
            TestGetTagsAxioms2(stb, stb.Solver.MkBitVecSort(8));
            TestGetTagsAxioms2(stb, stb.Solver.MkBitVecSort(7));
        }

        [TestMethod]
        public void TestGetTagsVsGetTags2()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            STz3 GetTags = MkGetTags(stb, stb.Solver.IntSort);
            STz3 GetTags2 = MkGetTags2(stb, stb.Solver.IntSort);

            var w = GetTags.Diff(GetTags2);
            IValue<Expr> i = w.Input;
            IValue<Expr> a = w.Output1;
            IValue<Expr> b = w.Output2;
            int k = w.InputLength;

            //any shortest input witness has length 5
            Assert.AreEqual<int>(5, k);
            string input = i.GetStringValue(false);
            //any shortest input witness must match the following pattern
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(input, "^<.<.>$"));
        }

        static void TestGetTagsAxioms1(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            z3p.MainSolver.Push();
            var st = MkGetTags(stb, charSort);
            st.AssertTheory();

            //st.ShowAsGraph();

            Expr input = z3p.MkListFromString("<<s><<>><f><t", charSort);
            Expr output = z3p.MkFreshConst("output1", z3p.MkListSort(charSort));

            Expr assertion = st.MkAccept(input, output);
            var model = z3p.MainSolver.GetModel(assertion, output);
            Assert.IsNotNull(model);
            string actual = model[output].GetStringValue(false);
            Assert.AreEqual("<s><>><f>", actual);

            Expr input2 = z3p.MkListFromString("<a<a><b>", charSort);
            Expr output2 = z3p.MkFreshConst("output2", z3p.MkListSort(charSort));

            Expr assertion2 = st.MkAccept(input2, output2);
            var model2 = z3p.MainSolver.GetModel(assertion2, output2);
            Assert.IsNotNull(model2);
            string actual2 = model2[output2].GetStringValue(false);
            Assert.AreEqual("<b>", actual2);
            z3p.MainSolver.Pop();
        }

        static void TestGetTagsAxioms2(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            z3p.MainSolver.Push();
            STz3 st = MkGetTags2(stb, charSort);
            st.AssertTheory();

            //st.ShowAsGraph();

            Expr input = z3p.MkListFromString("<<s><<>><f><t", charSort);
            Expr output = z3p.MkFreshConst("output1", z3p.MkListSort(charSort));

            Expr assertion = st.MkAccept(input, output);
            var model = z3p.MainSolver.GetModel(assertion, output);
            Assert.IsNotNull(model);
            string actual = model[output].GetStringValue(false);
            Assert.AreEqual("<s><>><f>", actual);

            Expr input2 = z3p.MkListFromString("<a<a><b>", charSort);
            Expr output2 = z3p.MkFreshConst("output2", z3p.MkListSort(charSort));

            Expr assertion2 = st.MkAccept(input2, output2);
            var model2 = z3p.MainSolver.GetModel(assertion2, output2);
            Assert.IsNotNull(model2);
            string actual2 = model2[output2].GetStringValue(false);
            Assert.AreEqual("<a><b>", actual2);
            z3p.MainSolver.Pop();
        }

        /// <summary>
        /// The buggy version of GetTags
        /// </summary>
        private static STz3 MkGetTags(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            Expr tt = z3p.True;
            Expr[] eps = new Expr[] { };
            Expr lt = z3p.MkNumeral((int)'<', charSort);
            Expr gt = z3p.MkNumeral((int)'>', charSort);
            List<Move<Rulez3>> rules = new List<Move<Rulez3>>();
            Expr _0 = z3p.MkNumeral(0, charSort);
            rules.Add(stb.MkFinalOutput(0, tt, eps));
            rules.Add(stb.MkFinalOutput(1, tt, eps));
            rules.Add(stb.MkFinalOutput(2, tt, eps));
            Expr x = stb.MkInputVariable(charSort);
            Expr c = stb.MkRegister(charSort);
            //rules from q0
            rules.Add(stb.MkRule(0, 0, z3p.MkNeq(x, lt), _0));
            rules.Add(stb.MkRule(0, 1, z3p.MkEq(x, lt), _0));
            //rules from q1
            rules.Add(stb.MkRule(1, 2, z3p.MkNeq(x, lt), x));
            rules.Add(stb.MkRule(1, 1, z3p.MkEq(x, lt), _0));
            //rules from q2
            rules.Add(stb.MkRule(2, 0, z3p.MkNeq(x, gt), _0));
            rules.Add(stb.MkRule(2, 0, z3p.MkEq(x, gt), _0, lt, c, gt));

            STz3 st = stb.MkST("GetTags", _0, charSort, charSort, charSort, 0, rules);
            return st;
        }

        /// <summary>
        /// The corrected version of GetTags
        /// </summary>
        private static STz3 MkGetTags2(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            Expr tt = z3p.True;
            Expr[] eps = new Expr[] { };
            List<Move<Rulez3>> rules = new List<Move<Rulez3>>();
            Expr lt = z3p.MkNumeral((int)'<', charSort);
            Expr gt = z3p.MkNumeral((int)'>', charSort);
            //final outputs are all epmpty
            rules.Add(stb.MkFinalOutput(0, tt, eps));
            rules.Add(stb.MkFinalOutput(1, tt, eps));
            rules.Add(stb.MkFinalOutput(2, tt, eps));
            Expr x = stb.MkInputVariable(charSort);
            Expr c = stb.MkRegister(charSort);
            //rules from q0
            rules.Add(stb.MkRule(0, 0, z3p.MkNeq(x, lt), x));
            rules.Add(stb.MkRule(0, 1, z3p.MkEq(x, lt), x));
            //rules from q1
            rules.Add(stb.MkRule(1, 2, z3p.MkNeq(x, lt), x));
            rules.Add(stb.MkRule(1, 1, z3p.MkEq(x, lt), x));
            //rules from q2
            rules.Add(stb.MkRule(2, 0, z3p.MkAnd(z3p.MkNeq(x, gt), z3p.MkNeq(x, lt)), x));
            rules.Add(stb.MkRule(2, 0, z3p.MkEq(x, gt), x, lt, c, gt));
            rules.Add(stb.MkRule(2, 1, z3p.MkEq(x, lt), x));

            STz3 st = stb.MkST("GetTags2", z3p.MkNumeral(0, charSort), charSort, charSort, charSort, 0, rules);
            return st;
        }

        [TestMethod]
        public void TestDecodeAxioms()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            TestDecodeAxioms(stb, stb.Solver.IntSort);
        }

        [TestMethod]
        public void TestDecodeVsDecodeWithBug()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            STz3 D = MkDecode(stb, stb.Solver.IntSort);
            STz3 D2 = MkDecodeWithBug(stb, stb.Solver.IntSort);
            var w = D.Diff(D2);
            IValue<Expr> i = w.Input;
            IValue<Expr> a = w.Output1;
            IValue<Expr> b = w.Output2;
            int k = w.InputLength;
            //any shortest input witness has length 6
            Assert.AreEqual<int>(6, k);
            string input = i.GetStringValue(false);
            string outA = a.GetStringValue(false);
            string outB = b.GetStringValue(false);

            //any shortest input witness must match the following pattern
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(input, "^&#....$"));

            //the following characters are swapped
            string x = outA.Substring(2, 2);
            string y = outB.Substring(3, 1) + outB.Substring(2, 1);
            Assert.AreEqual<string>(x, y);
        }

        static void TestDecodeAxioms(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            z3p.MainSolver.Push();
            STz3 st = MkDecode(stb, charSort);
            st.AssertTheory();

            //st.ShowAsGraph();

            Expr input = z3p.MkListFromString("&#38;\0", charSort);
            Expr output = z3p.MkFreshConst("output1", z3p.MkListSort(charSort));

            Expr assertion = st.MkAccept(input, output);
            var model = z3p.MainSolver.GetModel(assertion, output);
            Assert.IsNotNull(model);
            string actual = model[output].GetStringValue(false);
            Assert.AreEqual("&\0", actual);

            Expr input2 = z3p.MkListFromString("&#38;&#38\0", charSort);
            Expr output2 = z3p.MkFreshConst("output2", z3p.MkListSort(charSort));

            Expr assertion2 = st.MkAccept(input2, output2);
            var model2 = z3p.MainSolver.GetModel(assertion2, output2);
            Assert.IsNotNull(model2);
            string actual2 = model2[output2].GetStringValue(false);
            Assert.AreEqual("&&#38\0", actual2);
            z3p.MainSolver.Pop();
        }

        [TestMethod]
        public void TestDecodeAxioms2()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            TestDecodeAxioms2(stb, stb.Solver.IntSort);
        }
        static void TestDecodeAxioms2(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            z3p.MainSolver.Push();
            STz3 st = MkDecode(stb, charSort);
            st.AssertTheory();

            //st.ShowAsGraph();

            Expr input = z3p.MkListFromString("&#38;#38;\0", charSort);
            Expr output1 = z3p.MkFreshConst("output1", z3p.MkListSort(charSort));
            Expr output2 = z3p.MkFreshConst("output2", z3p.MkListSort(charSort));

            Expr assertion1 = st.MkAccept(input, output1);
            Expr assertion2 = st.MkAccept(output1, output2);
            var model = z3p.MainSolver.GetModel(z3p.MkAnd(assertion1, assertion2), output1, output2);
            Assert.IsNotNull(model);
            string actual1 = model[output1].GetStringValue(false);
            string actual2 = model[output2].GetStringValue(false);
            Assert.AreEqual("&#38;\0", actual1);
            Assert.AreEqual("&\0", actual2);
            z3p.MainSolver.Pop();
        }


        private static STz3 MkDecode(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            Expr tt = z3p.True;
            Expr[] eps = new Expr[] { };
            List<Move<Rulez3>> rules = new List<Move<Rulez3>>();
            Expr x = stb.MkInputVariable(charSort);
            Sort regSort = z3p.MkTupleSort(charSort, charSort);
            //the compound register
            Expr yz = stb.MkRegister(regSort);
            //the individual registers
            Expr y = z3p.MkProj(0, yz);
            Expr z = z3p.MkProj(1, yz);
            //constant characer values
            Expr amp = z3p.MkNumeral((int)'&', charSort);
            Expr sharp = z3p.MkNumeral((int)'#', charSort);
            Expr semi = z3p.MkNumeral((int)';', charSort);
            Expr zero = z3p.MkNumeral((int)'0', charSort);
            Expr nine = z3p.MkNumeral((int)'9', charSort);
            Expr _1 = z3p.MkNumeral(1, charSort);
            Expr _0 = z3p.MkNumeral(0, charSort);
            Expr _10 = z3p.MkNumeral(10, charSort);
            Expr _48 = z3p.MkNumeral(48, charSort);
            //initial register value
            Expr _11 = z3p.MkTuple(_1, _1);
            //various terms
            Expr xNEQ0 = z3p.MkNeq(x, _0);
            Expr xEQ0 = z3p.MkEq(x, _0);
            Expr xNEQamp = z3p.MkNeq(x, amp);
            Expr xEQamp = z3p.MkEq(x, amp);
            Expr xNEQsharp = z3p.MkNeq(x, sharp);
            Expr xEQsharp = z3p.MkEq(x, sharp);
            Expr xNEQsemi = z3p.MkNeq(x, semi);
            Expr xEQsemi = z3p.MkEq(x, semi);
            Expr xIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, x), z3p.MkCharLe(x, nine));
            Expr yIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, y), z3p.MkCharLe(y, nine));
            Expr zIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, z), z3p.MkCharLe(z, nine));
            Expr yzAreDigits = z3p.MkAnd(yIsDigit, zIsDigit);
            Expr xIsNotDigit = z3p.MkNot(xIsDigit);
            Expr decode = z3p.MkCharAdd(z3p.MkCharMul(_10, z3p.MkCharSub(y, _48)), z3p.MkCharSub(z, _48));
            //final state 
            rules.Add(stb.MkFinalOutput(5, tt));
            //terminating rules
            rules.Add(stb.MkRule(0, 5, xEQ0, _11, _0));
            rules.Add(stb.MkRule(1, 5, xEQ0, _11, amp, _0));
            rules.Add(stb.MkRule(2, 5, xEQ0, _11, amp, sharp, _0));
            rules.Add(stb.MkRule(3, 5, xEQ0, _11, amp, sharp, y, _0));
            rules.Add(stb.MkRule(4, 5, xEQ0, _11, amp, sharp, y, z, _0));
            //main rules 
            //rules from state q0
            rules.Add(stb.MkRule(0, 0, z3p.MkAnd(xNEQ0, xNEQamp), yz, x));
            rules.Add(stb.MkRule(0, 1, z3p.MkAnd(xNEQ0, xEQamp), yz));
            //rules from state q1
            rules.Add(stb.MkRule(1, 0, z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQamp, xNEQsharp)), yz, amp, x));
            rules.Add(stb.MkRule(1, 1, z3p.MkAnd(xNEQ0, xEQamp), yz, amp));
            rules.Add(stb.MkRule(1, 2, z3p.MkAnd(xNEQ0, xEQsharp), yz));
            //rules from state q2
            rules.Add(stb.MkRule(2, 0, z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQamp, xIsNotDigit)), yz, amp, sharp, x));
            rules.Add(stb.MkRule(2, 1, z3p.MkAnd(xNEQ0, xEQamp), yz, amp, sharp));
            rules.Add(stb.MkRule(2, 3, z3p.MkAnd(xNEQ0, xIsDigit), z3p.MkTuple(x, z)));
            //rules from state q3
            rules.Add(stb.MkRule(3, 0, z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQamp, xIsNotDigit)), _11, amp, sharp, y, x));
            rules.Add(stb.MkRule(3, 1, z3p.MkAnd(xNEQ0, xEQamp), _11, amp, sharp, y));
            rules.Add(stb.MkRule(3, 4, z3p.MkAnd(xNEQ0, xIsDigit), z3p.MkTuple(y, x)));
            //rules from state q4
            rules.Add(stb.MkRule(4, 0, z3p.MkAnd(xNEQ0, xEQsemi), _11, decode));
            rules.Add(stb.MkRule(4, 0, z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQsemi, xNEQamp)), _11, amp, sharp, y, z, x));
            rules.Add(stb.MkRule(4, 1, z3p.MkAnd(xNEQ0, xEQamp), _11, amp, sharp, y, z));


            STz3 st = stb.MkST("Decode", _11, charSort, charSort, regSort, 0, rules);
            return st;
        }

        private static STz3 MkDecodeWithFinalOutputs(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            Expr tt = z3p.True;
            Expr[] eps = new Expr[] { };
            List<Move<Rulez3>> rules = new List<Move<Rulez3>>();
            Expr x = stb.MkInputVariable(charSort);
            Sort regSort = z3p.MkTupleSort(charSort, charSort);
            //the compound register
            Expr yz = stb.MkRegister(regSort);
            //the individual registers
            Expr y = z3p.MkProj(0, yz);
            Expr z = z3p.MkProj(1, yz);
            //constant characer values
            Expr amp = z3p.MkNumeral((int)'&', charSort);
            Expr sharp = z3p.MkNumeral((int)'#', charSort);
            Expr semi = z3p.MkNumeral((int)';', charSort);
            Expr zero = z3p.MkNumeral((int)'0', charSort);
            Expr nine = z3p.MkNumeral((int)'9', charSort);
            Expr _1 = z3p.MkNumeral(1, charSort);
            Expr _0 = z3p.MkNumeral(0, charSort);
            Expr _10 = z3p.MkNumeral(10, charSort);
            Expr _48 = z3p.MkNumeral(48, charSort);
            //initial register value
            Expr _11 = z3p.MkTuple(_1, _1);
            //various terms
            Expr xNEQamp = z3p.MkNeq(x, amp);
            Expr xEQamp = z3p.MkEq(x, amp);
            Expr xNEQsharp = z3p.MkNeq(x, sharp);
            Expr xEQsharp = z3p.MkEq(x, sharp);
            Expr xNEQsemi = z3p.MkNeq(x, semi);
            Expr xEQsemi = z3p.MkEq(x, semi);
            Expr xIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, x), z3p.MkCharLe(x, nine));
            Expr yIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, y), z3p.MkCharLe(y, nine));
            Expr zIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, z), z3p.MkCharLe(z, nine));
            Expr yzAreDigits = z3p.MkAnd(yIsDigit, zIsDigit);
            Expr xIsNotDigit = z3p.MkNot(xIsDigit);
            Expr decode = z3p.MkCharAdd(z3p.MkCharMul(_10, z3p.MkCharSub(y, _48)), z3p.MkCharSub(z, _48));
            //final outputs 
            rules.Add(stb.MkFinalOutput(0, tt));
            rules.Add(stb.MkFinalOutput(1, tt, amp));
            rules.Add(stb.MkFinalOutput(2, tt, amp, sharp));
            rules.Add(stb.MkFinalOutput(3, tt, amp, sharp, y));
            rules.Add(stb.MkFinalOutput(4, tt, amp, sharp, y, z));
            //main rules 
            //rules from state q0
            rules.Add(stb.MkRule(0, 0, xNEQamp, yz, x));
            rules.Add(stb.MkRule(0, 1, xEQamp, yz));
            //rules from state q1
            rules.Add(stb.MkRule(1, 0, z3p.MkAnd(xNEQamp, xNEQsharp), yz, amp, x));
            rules.Add(stb.MkRule(1, 1, xEQamp, yz, amp));
            rules.Add(stb.MkRule(1, 2, xEQsharp, yz));
            //rules from state q2
            rules.Add(stb.MkRule(2, 0, z3p.MkAnd(xNEQamp, xIsNotDigit), yz, amp, sharp, x));
            rules.Add(stb.MkRule(2, 1, xEQamp, yz, amp, sharp));
            rules.Add(stb.MkRule(2, 3, xIsDigit, z3p.MkTuple(x, z)));
            //rules from state q3
            rules.Add(stb.MkRule(3, 0, z3p.MkAnd(xNEQamp, xIsNotDigit), _11, amp, sharp, y, x));
            rules.Add(stb.MkRule(3, 1, xEQamp, _11, amp, sharp, y));
            rules.Add(stb.MkRule(3, 4, xIsDigit, z3p.MkTuple(y, x)));
            //rules from state q4
            rules.Add(stb.MkRule(4, 0, xEQsemi, _11, decode));
            rules.Add(stb.MkRule(4, 0, z3p.MkAnd(xNEQsemi, xNEQamp), _11, amp, sharp, y, z, x));
            rules.Add(stb.MkRule(4, 1, xEQamp, _11, amp, sharp, y, z));

            STz3 st = stb.MkST("Decode", _11, charSort, charSort, regSort, 0, rules);
            return st;
        }

        private static STz3 MkDecodeWithBug(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            Expr tt = z3p.True;
            Expr[] eps = new Expr[] { };
            List<Move<Rulez3>> rules = new List<Move<Rulez3>>();
            Expr x = stb.MkInputVariable(charSort);
            Sort regSort = z3p.MkTupleSort(charSort, charSort);
            //the compound register
            Expr yz = stb.MkRegister(regSort);
            //the individual registers
            Expr y = z3p.MkProj(0, yz);
            Expr z = z3p.MkProj(1, yz);
            //constant characer values
            Expr amp = z3p.MkNumeral((int)'&', charSort);
            Expr sharp = z3p.MkNumeral((int)'#', charSort);
            Expr semi = z3p.MkNumeral((int)';', charSort);
            Expr zero = z3p.MkNumeral((int)'0', charSort);
            Expr nine = z3p.MkNumeral((int)'9', charSort);
            Expr _1 = z3p.MkNumeral(1, charSort);
            Expr _0 = z3p.MkNumeral(0, charSort);
            Expr _10 = z3p.MkNumeral(10, charSort);
            Expr _48 = z3p.MkNumeral(48, charSort);
            //initial register value
            Expr _11 = z3p.MkTuple(_1, _1);
            //various terms
            Expr xNEQ0 = z3p.MkNeq(x, _0);
            Expr xEQ0 = z3p.MkEq(x, _0);
            Expr xNEQamp = z3p.MkNeq(x, amp);
            Expr xEQamp = z3p.MkEq(x, amp);
            Expr xNEQsharp = z3p.MkNeq(x, sharp);
            Expr xEQsharp = z3p.MkEq(x, sharp);
            Expr xNEQsemi = z3p.MkNeq(x, semi);
            Expr xEQsemi = z3p.MkEq(x, semi);
            Expr xIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, x), z3p.MkCharLe(x, nine));
            Expr yIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, y), z3p.MkCharLe(y, nine));
            Expr zIsDigit = z3p.MkAnd(z3p.MkCharLe(zero, z), z3p.MkCharLe(z, nine));
            Expr yzAreDigits = z3p.MkAnd(yIsDigit, zIsDigit);
            Expr xIsNotDigit = z3p.MkNot(xIsDigit);
            Expr decode = z3p.MkCharAdd(z3p.MkCharMul(_10, z3p.MkCharSub(y, _48)), z3p.MkCharSub(z, _48));
            //final state 
            rules.Add(stb.MkFinalOutput(5, tt));
            //terminating rules
            rules.Add(stb.MkRule(0, 5, xEQ0, _11, _0));
            rules.Add(stb.MkRule(1, 5, xEQ0, _11, amp, _0));
            rules.Add(stb.MkRule(2, 5, xEQ0, _11, amp, sharp, _0));
            rules.Add(stb.MkRule(3, 5, xEQ0, _11, amp, sharp, y, _0));
            rules.Add(stb.MkRule(4, 5, xEQ0, _11, amp, sharp, y, z, _0));
            //main rules 
            //rules from state q0
            rules.Add(stb.MkRule(0, 0, z3p.MkAnd(xNEQ0, xNEQamp), yz, x));
            rules.Add(stb.MkRule(0, 1, z3p.MkAnd(xNEQ0, xEQamp), yz));
            //rules from state q1
            rules.Add(stb.MkRule(1, 0, z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQamp, xNEQsharp)), yz, amp, x));
            rules.Add(stb.MkRule(1, 1, z3p.MkAnd(xNEQ0, xEQamp), yz, amp));
            rules.Add(stb.MkRule(1, 2, z3p.MkAnd(xNEQ0, xEQsharp), yz));
            //rules from state q2
            rules.Add(stb.MkRule(2, 0, z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQamp, xIsNotDigit)), yz, amp, sharp, x));
            rules.Add(stb.MkRule(2, 1, z3p.MkAnd(xNEQ0, xEQamp), yz, amp, sharp));
            rules.Add(stb.MkRule(2, 3, z3p.MkAnd(xNEQ0, xIsDigit), z3p.MkTuple(x, z)));
            //rules from state q3
            rules.Add(stb.MkRule(3, 0, z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQamp, xIsNotDigit)), _11, amp, sharp, y, x));
            rules.Add(stb.MkRule(3, 1, z3p.MkAnd(xNEQ0, xEQamp), _11, amp, sharp, y));
            rules.Add(stb.MkRule(3, 4, z3p.MkAnd(xNEQ0, xIsDigit), z3p.MkTuple(y, x)));
            //rules from state q4
            rules.Add(stb.MkRule(4, 0, z3p.MkAnd(xNEQ0, xEQsemi), _11, decode));
            //rules.Add(stb.MkRule(4, 0, z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQsemi, xNEQamp)), _11, amp, sharp, y, z, x)); //correct
            rules.Add(stb.MkRule(4, 0, z3p.MkAnd(xNEQ0, z3p.MkAnd(xNEQsemi, xNEQamp)), _11, amp, sharp, z, y, x)); //BUG
            rules.Add(stb.MkRule(4, 1, z3p.MkAnd(xNEQ0, xEQamp), _11, amp, sharp, y, z));


            STz3 st = stb.MkST("Decode", _11, charSort, charSort, regSort, 0, rules);
            return st;
        }

        [TestMethod]
        public void TestCompositionOfGetTags()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            STz3 D = MkGetTags(stb, stb.Solver.IntSort);
            STz3 DD = D + D;
            //DD.ShowGraph();
            //check that D and DD are eqivalent up to depth 6
            var w = D.Diff(DD, 6);
            Assert.IsNull(w);
        }



        //[TestMethod]
        public void TestCompositionOfDecode()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            TestIdempotenceOfDecode(stb, stb.Solver.IntSort);
        }
        //uses ST composition
        private static void TestIdempotenceOfDecode(STBuilderZ3 stb, Sort charSort)
        {
            STz3 D = MkDecode(stb, charSort);
            int time = System.Environment.TickCount;
            STz3 DD = D + D;
            time = System.Environment.TickCount - time;
            //DD.ShowAsGraph();
            int k;
            IValue<Expr> i;
            IValue<Expr> a;
            IValue<Expr> b;
            int time2 = System.Environment.TickCount;
            var w = DD.Diff(D, 20);
            Assert.IsNotNull(w);
            k = w.InputLength;
            i = w.Input;
            a = w.Output1;
            b = w.Output2;
            time2 = System.Environment.TickCount - time2;
            Assert.AreEqual<int>(10, k);
            string istr = i.GetStringValue(false);
            string astr = a.GetStringValue(false);
            string bstr = b.GetStringValue(false);
            string bExpected = System.Net.WebUtility.HtmlDecode(istr);
            string aExpected = System.Net.WebUtility.HtmlDecode(bExpected);
            Assert.AreEqual<string>(bExpected, bstr);
            Assert.AreEqual<string>(aExpected, astr);
        }

        //[TestMethod]
        public void TestCompositionOfDecode2()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            TestIdempotenceOfDecode2(stb, stb.Solver.IntSort);
        }
        //do not use ST composition, use an intermediate output variable
        static void TestIdempotenceOfDecode2(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            z3p.MainSolver.Push();
            STz3 st = MkDecode(stb, charSort);
            st.AssertTheory();

            //st.ShowGraph();

            Expr input = z3p.MkListFromString("", charSort);
            Expr output1 = z3p.MkFreshConst("output1", z3p.MkListSort(charSort));
            Expr output2 = z3p.MkFreshConst("output2", z3p.MkListSort(charSort));

            string i;
            string o1;
            string o2;
            int k = 0;
            while (true)
            {
                var model = z3p.MainSolver.GetModel(z3p.MkAnd(st.MkAccept(input, output1), z3p.MkAnd(st.MkAccept(output1, output2), z3p.MkNeq(output1, output2))), input, output1, output2);
                if (model != null)
                {
                    i = model[input].GetStringValue(false);
                    o1 = model[output1].GetStringValue(false);
                    o2 = model[output2].GetStringValue(false);
                    break;
                }
                input = z3p.MkListCons(z3p.MkFreshConst("tmp", charSort), input);
                k += 1;
            }
            Assert.AreEqual(o1, System.Net.WebUtility.HtmlDecode(i));
            Assert.AreEqual(o2, System.Net.WebUtility.HtmlDecode(System.Net.WebUtility.HtmlDecode(i)));
            Assert.AreNotEqual(o1, o2);
            z3p.MainSolver.Pop();
        }

        //[TestMethod]
        public void TestCompositionOfDecode3()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            TestIdempotenceOfDecode3(stb, stb.Solver.IntSort);
        }
        //completetly unconstrained model search
        static void TestIdempotenceOfDecode3(STBuilderZ3 stb, Sort charSort)
        {
            var z3p = stb.Solver;
            z3p.MainSolver.Push();
            STz3 st = MkDecode(stb, charSort);
            st.AssertTheory();

            //st.ShowAsGraph();

            Expr input = z3p.MkFreshConst("input", z3p.MkListSort(charSort));
            Expr output1 = z3p.MkFreshConst("output1", z3p.MkListSort(charSort));
            Expr output2 = z3p.MkFreshConst("output2", z3p.MkListSort(charSort));

            string i;
            string o1;
            string o2;

            var model = z3p.MainSolver.GetModel(z3p.MkAnd(st.MkAccept(input, output1), z3p.MkAnd(st.MkAccept(output1, output2), z3p.MkNeq(output1, output2))), input, output1, output2);

            i = model[input].GetStringValue(false);
            o1 = model[output1].GetStringValue(false);
            o2 = model[output2].GetStringValue(false);

            Assert.AreEqual(o1, System.Net.WebUtility.HtmlDecode(i));
            Assert.AreEqual(o2, System.Net.WebUtility.HtmlDecode(System.Net.WebUtility.HtmlDecode(i)));
            Assert.AreNotEqual(o1, o2);
            z3p.MainSolver.Pop();
        }

        //[TestMethod]
        public void TestCompositionOfDecodeWithFinals()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            TestCompositionOfDecodeWithFinals(stb, stb.Solver.IntSort);
        }

        //[testmethod]
        //public void fuzzparamdecode()
        //{
        //    stbuilder stb = new stbuilder();
        //    z3provider z3p = stb.z3p;
        //    z3p.push();
        //    var charsort = z3p.intsort;
        //    st st = mkparametricdecode(stb, stb.z3p.intsort, 6);
        //    st.showasgraph();
        //    st.asserttheory();

        //    int count = 0;
        //    for (int i = 0; i < 100; ++i)
        //    {
        //        for (int j = 0; j <= 6 - i.tostring().length; ++j)
        //        {
        //            var zeros = "".padleft(j, '0');
        //            count++;

        //            z3p.push();

        //            var instr = "&#" + zeros + i.tostring() +";";
        //            term input = z3p.mklistfromstring(instr, charsort);
        //            term output1 = z3p.mkfreshconst("output1", z3p.mklistsort(charsort));
        //            var model = z3p.getmodel(st.mkaccept(input, output1), input, output1);

        //            var modelstr = model[output1].getunicodestringvalue();
        //            var actual = system.net.webutility.htmldecode(instr);

        //            if (!modelstr.equals(actual))
        //            {
        //                console.writeline("input: \"" + instr + "\"\t model: \"" + modelstr + "\"(" + modelstr.length.tostring() + ")\t actual: \"" + actual + "\"(" + actual.length.tostring() + ")");
        //            }

        //            z3p.pop();
        //        }
        //    }
        //    console.writeline("tested " + count.tostring() + " values");
        //}

        //uses ST composition
        private static void TestCompositionOfDecodeWithFinals(STBuilderZ3 stb, Sort charSort)
        {
            STz3 D = MkDecodeWithFinalOutputs(stb, charSort);

            // ST D = MkParametricDecode(stb, charSort, 2);

            int time = System.Environment.TickCount;
            STz3 DD = D + D;
            time = System.Environment.TickCount - time;
            //D.ShowAsGraph();
            // DD.ShowAsGraph();
            int k;
            IValue<Expr> i;
            IValue<Expr> a;
            IValue<Expr> b;
            int time2 = System.Environment.TickCount;
            var w = DD.Diff(D);
            Assert.IsNotNull(w);
            k = w.InputLength;
            i = w.Input;
            a = w.Output1;
            b = w.Output2;
            time2 = System.Environment.TickCount - time2;
            // Assert.AreEqual<int>(9, k);
            string istr = i.GetStringValue(false);
            string astr = a.GetStringValue(false);
            string bstr = b.GetStringValue(false);
            string bExpected = System.Net.WebUtility.HtmlDecode(istr);
            string aExpected = System.Net.WebUtility.HtmlDecode(bExpected);
            Assert.AreEqual<string>(bExpected, bstr);
            Assert.AreEqual<string>(aExpected, astr);
        }

        //[TestMethod]
        //public void STHtmlDecSelf()
        //{
        //    int maxcomposecount = 2;
        //    STBuilder stb = new STBuilder();
        //    Z3Provider z3p = stb.Z3p;

        //    var titles = new String[] { "Name", "Number of Compositions", "States", "Edges", "Time Assert/Compose", "Time to Find Witness" };

        //    Console.WriteLine(String.Join("\t", titles));

        //    for (int i = 2; i < 7; ++i)
        //    {
        //        ST cur = MkParametricDecode(stb, z3p.IntSort, i);

        //        for (int j = 0; j <= maxcomposecount; ++j)
        //        {
        //            SelfComposeExp_ConcreteComp(stb, cur, j);
        //        }
        //    }
        //}

        //[TestMethod]
        //public void STHtmlDecSelf_Lazy()
        //{
        //    int maxcomposecount = 1;  // 2 causes wrong models ??? 
        //    STBuilder stb = new STBuilder();
        //    Z3Provider z3p = stb.Z3p;

        //    var titles = new String[] { "Name", "Number of Compositions", "Time Assert/Compose", "Time to Find Witness" };

        //    Console.WriteLine(String.Join("\t", titles));

        //    for (int i = 2; i < 7; ++i)
        //    {
        //        ST cur = MkParametricDecode(stb, z3p.IntSort, i);

        //        for (int j = 1; j <= maxcomposecount; ++j)
        //        {
        //            SelfComposeExp_LazyComp(stb, cur, j);
        //        }
        //    }
        //}


        //[TestMethod]
        //public void SFTHtmlDecSelf()
        //{
        //    int maxcomposecount = 1;
        //    STBuilder stb = new STBuilder();
        //    Z3Provider z3p = stb.Z3p;

        //    var titles = new String[] { "Name", "Number of Compositions", "States", "Edges", "Time Assert/Compose", "Time to find witness" };

        //    Console.WriteLine(String.Join("\t", titles));

        //    for (int i = 2; i < 7; ++i)
        //    {
        //      z3p.Push();
        //        //ST curst = MkParametricDecode(stb, z3p.IntSort, i);
        //        ST curst = MkParametricDecode(stb, z3p.Bv16Sort, i);
        //        FiniteTransducerBuilder ftb = new FiniteTransducerBuilder(stb.Z3p);
        //        SFT cur = SFTfromST.Convert(curst, ftb);
        //        for (int j = 0; j <= maxcomposecount; ++j)
        //        {
        //            SelfComposeExp_ConcreteComp(cur, j);
        //        }
        //        z3p.Pop();
        //    }
        //}



        private static string ApplyHtmlDecode(int composecount, string instr)
        {
            string s = instr;
            for (int i = 0; i <= composecount; i++)
                s = System.Net.WebUtility.HtmlDecode(s);
            return s;
        }

        [TestMethod]
        public void TestSTranRestr() 
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            string regex1 = @"^[^a]+$";
            string regex2 = @"^(&#36;|abc){0,2}$";
            string regex = @"^(&#36;){1,2}$";
            var sfa1 = stb.CreateFromRegex("SFA1", regex1).EliminateEpsilons();
            var sfa2 = stb.CreateFromRegex("SFA2", regex2).EliminateEpsilons();
            var sfa = (sfa1 * sfa2).Minimize().NormalizeLabels();
            sfa.Name = "SFA";
            var SMT = sfa.Solver;
            
            //sfa.ShowGraph();

            sfa.AssertTheory();

            Sort strSort = SMT.MkListSort(sfa.InputSort);
            Expr y = SMT.MkConst("y", strSort);
            Expr acc_y = SMT.MkApp(sfa.Acceptor, y);

            var y_val = SMT.MainSolver.GetModel(acc_y, y)[y].GetStringValue(false);

            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(y_val, regex1));
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(y_val, regex2));
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(y_val, regex));

            Assert.IsTrue(y_val.StartsWith("&#36;"));

            STz3 st = MkDecodeWithFinalOutputs(stb, sfa.InputSort);
            st.AssertTheory();
            Expr x = SMT.MkConst("x", st.InputListSort);
            Expr x2 = SMT.MkConst("x2", st.InputListSort);

            Expr decode_x_to_y = SMT.MkApp(st.Acceptor, x, y);
            Expr decode_x2_to_y = SMT.MkApp(st.Acceptor, x2, y);

            //note that acc_y is a range-restriction on y and x,x2 are input lists
            var model = SMT.MainSolver.GetModel(SMT.MkAnd(new Expr[] { acc_y, decode_x_to_y, decode_x2_to_y, SMT.MkNeq(x, x2) }), x, x2, y);

            var x_val = model[x].GetStringValue(false);
            var x2_val = model[x2].GetStringValue(false);
            y_val = model[y].GetStringValue(false);

            Assert.IsTrue(y_val.StartsWith("&#36;"));
            Assert.IsFalse(x_val.StartsWith("&#36;"));
            Assert.IsFalse(x2_val.StartsWith("&#36;"));
            Assert.AreNotEqual(x_val, x2_val);

            string y_expected1 = System.Net.WebUtility.HtmlDecode(x_val);
            string y_expected2 = System.Net.WebUtility.HtmlDecode(x2_val);
            Assert.AreEqual(y_expected1, y_val);
            Assert.AreEqual(y_expected2, y_val);

        }


        [TestMethod]
        public void TestSTdomRestr()
        {
            STBuilderZ3 stb = new STBuilderZ3(new Z3Provider(BitWidth.BV16));
            string d = @"^(&#36;){2,}$";
            var D = stb.CreateFromRegex("D", d).EliminateEpsilons();
            var SMT = D.Solver;
            STz3 A = MkDecodeWithFinalOutputs(stb, D.InputSort);
            STz3 AD = A.RestrictDomain(D);

            //AD.ShowAsGraph();

            AD.AssertTheory();
            Expr x = SMT.MkConst("x", AD.InputListSort);
            Expr y = SMT.MkConst("y", AD.OutputListSort);
            Expr decode_x_to_y = SMT.MkApp(AD.Acceptor, x, y);
            var model = SMT.MainSolver.GetModel(decode_x_to_y, x, y);

            string x_val = model[x].GetStringValue(false);
            string y_val = model[y].GetStringValue(false);

            Assert.IsTrue(x_val.StartsWith("&#36;&#36;"));
            Assert.IsTrue(y_val.StartsWith("$$"));

        }

    }
}
