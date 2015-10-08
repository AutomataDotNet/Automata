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
    public class Z3_Z3ContextTests
    {
        [TestMethod]
        public void MkFuncDeclTest()
        {
            try
            {
                Z3Provider Z = new Z3Provider();
                var f = Z.MkFuncDecl("foo", Z.IntSort, Z.IntSort);
                var g = Z.MkFuncDecl("foo", Z.IntSort, Z.BoolSort);
                Assert.IsTrue(true);
            }
            catch (AutomataException e)
            {
                Assert.IsTrue(e.kind == AutomataExceptionKind.FunctionIsAlreadyDeclared);
            }

        }

        [TestMethod]
        public void FuncDeclTest3()
        {
            try
            {
                Z3Provider Z = new Z3Provider();
                Z.MainSolver.Push();
                FuncDecl f = Z.MkFuncDecl("temp", Z.IntSort, Z.IntSort);
                Z.MainSolver.Push();
                Z.MainSolver.Push();
                Z.MainSolver.Pop();
                Z.MainSolver.Pop();
                FuncDecl g = Z.MkFuncDecl("temp", Z.IntSort, Z.IntSort);
                Z.MainSolver.Push();
                Z.MainSolver.Pop();
            }
            catch (AutomataException e)
            {
                Assert.AreEqual(e.kind, AutomataExceptionKind.FunctionIsAlreadyDeclared);
            }
        }

        [TestMethod]
        public void SolverTest()
        {
            var z3c = new Z3Provider();
            var z3s = z3c.MkSolver();
            var phi = z3c.MkLe(z3c.MkVar(0, z3c.IntSort), z3c.MkVar(1, z3c.IntSort));
            var psi = z3c.MkGt(z3c.MkVar(0, z3c.IntSort), z3c.MkVar(2, z3c.IntSort));
            var eq = z3c.MkEq(z3c.MkVar(1, z3c.IntSort), z3c.MkVar(2, z3c.IntSort));
            z3s.Push();
            z3s.Assert(phi);
            z3s.Assert(psi);
            z3s.Push();
            z3s.Assert(eq);
            var isSat1 = z3s.Check();
            z3s.Pop();
            var isSat2 = z3s.Check();
            z3s.Pop();
            Assert.IsFalse(isSat1);
            Assert.IsTrue(isSat2);
        }

        [TestMethod]
        public void EmptyTupleTest()
        {
            Z3Provider Z = new Z3Provider();
            Sort sort = Z.MkTupleSort();
            Expr unit = Z.MkTuple();
            Expr t = Z.MkVar(0, sort);
            Expr t_eq_unit = Z.MkEq(t, unit);
            var v = new List<IValue<Expr>>(Z.MainSolver.FindAllMembers(t_eq_unit));
            Assert.IsTrue(v.Count == 1);
            Assert.AreEqual(unit, v[0].Value);
        }

        [TestMethod]
        public void HexProjTest()
        {
            Z3Provider z3p = new Z3Provider();
            //Sort bv64 = z3p.MkBitVecSort(64);
            Sort sort = z3p.MkBitVecSort(64);
            Expr _0x654321 = z3p.MkNumeral((uint)0x654321, sort);
            Expr _5 = z3p.MkHexProj(4, _0x654321, sort).Simplify();
            Expr _4 = z3p.MkHexProj(3, _0x654321, sort).Simplify();
            Expr _3 = z3p.MkHexProj(2, _0x654321, sort).Simplify();
            Expr _2 = z3p.MkHexProj(1, _0x654321, sort).Simplify();
            Expr _1 = z3p.MkHexProj(0, _0x654321, sort).Simplify();
            int _5v = (int)z3p.GetNumeralInt(_5);
            int _4v = (int)z3p.GetNumeralInt(_4);
            int _3v = (int)z3p.GetNumeralInt(_3);
            int _2v = (int)z3p.GetNumeralInt(_2);
            int _1v = (int)z3p.GetNumeralInt(_1);
            Assert.AreEqual<int>(5, _5v);
            Assert.AreEqual<int>(4, _4v);
            Assert.AreEqual<int>(3, _3v);
            Assert.AreEqual<int>(2, _2v);
            Assert.AreEqual<int>(1, _1v);
        }

        [TestMethod]
        public void HexProjTest2()
        {
            Z3Provider z3p = new Z3Provider();
            Sort bv64 = z3p.MkBitVecSort(64);
            Sort bv16 = z3p.MkBitVecSort(16);
            Expr _0x654321 = z3p.MkNumeral((uint)0xF54321, bv64);
            Expr _6 = z3p.MkHexProj(5, _0x654321, bv16).Simplify();
            Expr _5 = z3p.MkHexProj(4, _0x654321, bv16).Simplify();
            Expr _4 = z3p.MkHexProj(3, _0x654321, bv16).Simplify();
            Expr _3 = z3p.MkHexProj(2, _0x654321, bv16).Simplify();
            Expr _2 = z3p.MkHexProj(1, _0x654321, bv16).Simplify();
            Expr _1 = z3p.MkHexProj(0, _0x654321, bv16).Simplify();
            int _6v = (int)z3p.GetNumeralInt(_6);
            int _5v = (int)z3p.GetNumeralInt(_5);
            int _4v = (int)z3p.GetNumeralInt(_4);
            int _3v = (int)z3p.GetNumeralInt(_3);
            int _2v = (int)z3p.GetNumeralInt(_2);
            int _1v = (int)z3p.GetNumeralInt(_1);
            Assert.AreEqual<int>(0xF, _6v);
            Assert.AreEqual<int>(5, _5v);
            Assert.AreEqual<int>(4, _4v);
            Assert.AreEqual<int>(3, _3v);
            Assert.AreEqual<int>(2, _2v);
            Assert.AreEqual<int>(1, _1v);
        }

        [TestMethod]
        public void TestCssCombinedCodepoint()
        {
            var kkkk = 0x7FFF - 65532;
            Z3Provider solver = new Z3Provider();
            Sort bv64 = solver.MkBitVecSort(64);
            Sort bv16 = solver.CharacterSort;

            Expr hs = solver.MkConst("hs", bv16);
            Expr ls = solver.MkConst("ls", bv16);

            Expr res = solver.MkConst("res", bv64);

            Expr maxVal = solver.MkNumeral(0x10FFFF, bv64); //max 16 bit nr

            Expr x = solver.ConvertBitVector(hs, bv64);
            Expr y = solver.ConvertBitVector(ls, bv64);
            Expr _0x10000 = solver.MkNumeral(0x10000, bv64);
            Expr _0xD800 = solver.MkNumeral(0xD800, bv64);
            Expr _0x400 = solver.MkNumeral(0x400, bv64);
            Expr _0xDC00 = solver.MkNumeral(0xDC00, bv64);

            ushort tmpLS = ((ushort)0xdfff) - ((ushort)0xdc00);

            for (int i = 0xdc00; i <= 0xdfff; i++ )
            {
                int j = (i - 0xdc00) >> 8;
                int k = (i >> 8) & 3;
                Assert.AreEqual<int>(j, k);
            }

            int tmpHS = (((int)0xdbff) - ((int)0xd800)) * ((int)0x400);
            //int tmpHS = (((int)0xdbff) - ((int)0xd800)) << 10;

            int tmpHSLS = tmpLS + tmpHS;

            int maxcodepoint = tmpHSLS + 0x10000;

            Expr cp = solver.MkCharAdd(_0x10000,
                       solver.MkCharAdd(solver.MkCharMul(solver.MkCharSub(x, _0xD800), _0x400),
                                        solver.MkCharSub(y, _0xDC00)));

            Expr ls_is_lowSurrogate =
                solver.MkAnd(solver.MkCharGe(ls, solver.MkNumeral(0xdc00, bv16)),
                                solver.MkCharLe(ls, solver.MkNumeral(0xdfff, bv16)));

            Expr hs_is_highSurrogate = 
                solver.MkAnd(solver.MkCharGe(hs, solver.MkNumeral(0xd800, bv16)),
                                solver.MkCharLe(hs, solver.MkNumeral(0xdbff, bv16)));

            Expr assert = solver.Simplify(solver.MkAnd(
                ls_is_lowSurrogate,
                hs_is_highSurrogate,
                solver.MkEq(res, cp)));

            //string s = solver.PrettyPrint(assert);

            solver.MainSolver.Assert(assert);

            var model = solver.MainSolver.GetModel(solver.MkCharLt(maxVal, res), hs, ls, res);

            Assert.IsNull(model);

            //if (model != null)
            //{
            //    int hsVal = solver.GetNumeralInt(model[hs].Value);
            //    int lsVal = solver.GetNumeralInt(model[ls].Value);
            //    long resval = solver.GetNumeralUInt(model[res].Value);
            //    Assert.AreEqual<long>(CssCombinedCodepoint(hsVal, lsVal), resval);
            //}
        }

        static long CssCombinedCodepoint(int hs, int ls)
        {
            long combinedCodePoint =
                            0x10000 + ((hs - 0xD800) * 0x400) + (ls - 0xDC00);
            return combinedCodePoint;
        }

        [TestMethod]
        public void TestFuncDecl()
        {
            Z3Provider z3p = new Z3Provider();
            FuncDecl f = z3p.MkFuncDecl("foo[56:205]", z3p.IntSort, z3p.IntSort);
            //FuncDecl f = z3p.Z3.GetAppDecl(z3p.MkBvExtract(0, 0, z3p.MkNumeral(34, z3p.CharacterSort)));
            uint[] p = z3p.GetDeclParameters(f);
            Assert.AreEqual<int>(2, p.Length);
            Assert.AreEqual<uint>(56, p[0]);
            Assert.AreEqual<uint>(205, p[1]);
        }

        [TestMethod]
        public void TestBitVectorOps()
        {
            Context z3 = new Context();
            var bv16 = z3.MkBitVecSort(16);
            var c = (BitVecExpr)z3.MkConst("c",bv16);
            var _3 = (BitVecExpr)z3.MkNumeral(3, bv16);
            var _7 = (BitVecExpr)z3.MkNumeral(7, bv16);
            var _1 = (BitVecExpr)z3.MkNumeral(1, bv16);
            var c_and_7 = z3.MkBVAND(c, _7);
            //((1 + (c & 7)) & 3)
            var t = z3.MkBVAND(z3.MkBVAdd(_1, c_and_7), _3);
            var s = t.Simplify(); //comes out as: (1 + (c & 3))
            var t_neq_s = z3.MkNot(z3.MkEq(t, s));
            var solv =z3.MkSolver();
            solv.Assert(t_neq_s);
            Assert.AreEqual(Status.UNSATISFIABLE, solv.Check());
        }

        [TestMethod]
        public void TupleTest()
        {
            Z3Provider z3p = new Z3Provider();
            //create the tuple sort for mouth
            FuncDecl mouth;
            FuncDecl[] mouth_accessors;
            var MOUTH = z3p.MkTupleSort("MOUTH", new string[] { "open", "teeth" }, new Sort[] { z3p.BoolSort, z3p.IntSort }, out mouth, out mouth_accessors);
            Func<Expr,Expr,Expr> mk_mouth = ((o,t) => z3p.MkApp(mouth, o, t));
            Func<Expr,Expr> get_open = (m => z3p.MkApp(mouth_accessors[0], m));
            Func<Expr,Expr> get_teeth = (m => z3p.MkApp(mouth_accessors[1], m));
            //create the tuple sort for nose
            FuncDecl nose;
            FuncDecl[] nose_accessors;
            var NOSE = z3p.MkTupleSort("NOSE", new string[] { "size" }, new Sort[] { z3p.IntSort }, out nose, out nose_accessors);
            Func<Expr,Expr> mk_nose = (s => z3p.MkApp(nose, s));
            Func<Expr,Expr> get_size = (n => z3p.MkApp(nose_accessors[0], n));
            //create the tuple sort for head
            FuncDecl head;
            FuncDecl[] head_accessors;
            var HEAD = z3p.MkTupleSort("HEAD", new string[] { "bald", "nose", "mouth" }, new Sort[] { z3p.BoolSort, NOSE, MOUTH }, out head, out head_accessors);
            Func<Expr,Expr,Expr,Expr> mk_head = ((b,n,m) => z3p.MkApp(head, b,n,m));
            Func<Expr,Expr> get_bald = (h => z3p.MkApp(head_accessors[0], h));
            Func<Expr,Expr> get_nose = (h => z3p.MkApp(head_accessors[1], h));
            Func<Expr,Expr> get_mouth = (h => z3p.MkApp(head_accessors[2], h));
            //------------------------ 
            // create a transformation "punch" from HEAD tp HEAD that removes k teeth, k is  the second parameter of the transformation
            var punch = z3p.MkFuncDecl("punch", new Sort[]{HEAD, z3p.IntSort}, HEAD);
            var x = z3p.MkVar(0, HEAD);        // <-- this is the input HEAD
            var y = z3p.MkVar(1, z3p.IntSort); // <-- this is the n parameter
            //this is the actual transformation of x that removes one tooth
            var new_mouth = mk_mouth(get_open(get_mouth(x)), z3p.MkSub(get_teeth(get_mouth(x)), y));
            var old_nose = get_nose(x);
            var old_bald = get_bald(x);
            var punch_def = mk_head(old_bald, old_nose,new_mouth);
            var punch_axiom = z3p.MkEqForall(z3p.MkApp(punch, x , y), punch_def, x, y);
            Func<Expr,Expr,Expr> punch_app = ((h,k) => z3p.MkApp(punch, h,k));
            z3p.MainSolver.Assert(punch_axiom);
            //------------------------ 
            // create a transformation "hit" from HEAD tp HEAD that doubles the size of the nose
            var hit = z3p.MkFuncDecl("hit", HEAD, HEAD);  
            var hit_def = mk_head(get_bald(x), mk_nose(z3p.MkMul(z3p.MkInt(2),get_size(get_nose(x)))), get_mouth(x));
            var hit_axiom = z3p.MkEqForall(z3p.MkApp(hit, x), hit_def, x);
            Func<Expr,Expr> hit_app = (h => z3p.MkApp(hit, h));
            z3p.MainSolver.Assert(hit_axiom);
            //-------------------------------
            // Analysis
            var H = z3p.MkConst("H", HEAD);
            var N = z3p.MkConst("N", z3p.IntSort);
            // check that hit and punch commute
            z3p.MainSolver.Push();
            z3p.MainSolver.Assert(z3p.MkNeq(punch_app(hit_app(H), N), hit_app(punch_app(H, N))));
            Status status = z3p.Check(); //here status must be UNSATISFIABLE
            z3p.MainSolver.Pop(); //remove the temporary context
            //check that hit is not idempotent
            z3p.MainSolver.Push();
            z3p.MainSolver.Assert(z3p.MkNeq(hit_app(hit_app(H)), hit_app(H)));
            status = z3p.Check(); //here status must not be UNSATISFIABLE (it is UNKNOWN due to use of axioms)
            var model1 = z3p.Z3S.Model;
            var witness1 = model1.Evaluate(H, true);   //a concrete instance of HEAD that shows when hitting twice is not the same as hitting once
            z3p.MainSolver.Pop();
            //but it is possible that hitting twice does no harm (when nose has size 0)
            z3p.MainSolver.Push();
            z3p.MainSolver.Assert(z3p.MkEq(hit_app(hit_app(H)), hit_app(H)));
            status = z3p.Check(); 
            var model2 = z3p.Z3S.Model;
            var witness2 = model2.Evaluate(H, true);
            z3p.MainSolver.Pop();
        }
    }
}