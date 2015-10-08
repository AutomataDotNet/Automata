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
    public class Z3_TreeTransducerTests
    {
        [TestMethod]
        public void TestComposition1()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zeroA", "oneA", "twoA" }, new int[] { 0, 1, 2 }));
            var B = (Z.TT.MkRankedAlphabet("B", Z.IntSort, new string[] { "zeroB", "oneB", "twoB" }, new int[] { 0, 1, 2 }));
            var C = (Z.TT.MkRankedAlphabet("C", Z.IntSort, new string[] { "zeroC", "oneC", "twoC" }, new int[] { 0, 1, 2 }));

            //(two (plus 1 x0) (one (plus 1 x0) (q x1)) (one (plus 2 x0) (q x2)))
            var b = Z.MkApp(B["twoB"],
                        Z.MkAdd(Z.MkInt(1), A.AttrVar),
                        B.MkTree("oneB", Z.MkAdd(Z.MkInt(1), A.AttrVar), A.MkTrans(B, 0, 1)),
                        B.MkTree("oneB", Z.MkAdd(Z.MkInt(2), A.AttrVar), A.MkTrans(B, 0, 2)));

            //(two (plus 1 x0) (zero x0) (one (plus 100 x0) (q x2)))
            var b2 = Z.MkApp(B["twoB"],
                        Z.MkAdd(Z.MkInt(1), A.AttrVar),
                        B.MkTree("zeroB", A.AttrVar),
                        B.MkTree("oneB", Z.MkAdd(Z.MkInt(9), A.AttrVar), A.MkTrans(B, 0, 2)));

            var rule0 = Z.TT.MkTreeRule(A, B, 0, "zeroA", Z.True, B.MkTree("zeroB", A.AttrVar));
            var rule1 = Z.TT.MkTreeRule(A, B, 0, "twoA", Z.MkGt(A.AttrVar, Z.MkInt(0)), b);
            var rule2 = Z.TT.MkTreeRule(A, B, 0, "twoA", Z.MkGt(A.AttrVar, Z.MkInt(0)), b2);
            var rule3 = Z.TT.MkTreeRule(A, B, 0, "oneA", Z.MkGt(A.AttrVar, Z.MkInt(0)), B.MkTree("oneB", A.AttrVar, A.MkTrans(B, 0, 1)));

            var trans1 = Z.TT.MkTreeAutomaton(0, A, B, new TreeRule[] { rule0, rule1, rule2, rule3 });

            //(two x0 (one (plus 1 x0) (p x1)) (one (plus 2 x0) (p x2)))
            var a = A.MkTree("twoA", C.AttrVar,
             A.MkTree("oneA", Z.MkAdd(Z.MkInt(1), C.AttrVar), C.MkTrans(A, 1, 1)),
             A.MkTree("oneA", Z.MkAdd(Z.MkInt(2), C.AttrVar), C.MkTrans(A, 1, 2)));

            var a2 = A.MkTree("zeroA", C.AttrVar);

            var rule4 = Z.TT.MkTreeRule(C, A, 1, "twoC", Z.MkGt(C.AttrVar, Z.MkInt(-2)), a);
            var rule5 = Z.TT.MkTreeRule(C, A, 1, "zeroC", Z.MkGt(C.AttrVar, Z.MkInt(-3)), a2);

            var trans2 = Z.TT.MkTreeAutomaton(1, C, A, new TreeRule[] { rule4, rule5 });

            var trans12 = trans2.Compose(trans1);

            var rulesOut = trans12.GetRules(trans12.Root, C["twoC"]);
            Assert.AreEqual<int>(2, rulesOut.Count);

            var rulesOut2 = trans12.GetRules(trans12.Root, C["zeroC"]);
            Assert.AreEqual<int>(1, rulesOut2.Count);

            var tin = C.MkTree("twoC", Z.MkInt(55), C.MkTree("zeroC", Z.MkInt(66)), C.MkTree("zeroC", Z.MkInt(77)));
            var res = trans12[tin];
            Assert.AreEqual<int>(2, res.Length);
            Assert.AreEqual<int>(3, trans12.RuleCount);
        }

        [TestMethod]
        public void TestCompositionCornerCase1()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));

            var q0 = Z.MkInt(0);
            Func<int, int, Expr> q = (state,var) => 
                {
                    return A.MkTrans(A, state, var);
                };

            //two(1+x0,one(x1),one(x2))
            var b = A.MkTree("two",
                        Z.MkAdd(Z.MkInt(1), A.AttrVar),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(1), A.AttrVar), q(0,1)),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(2), A.AttrVar), q(0,2)));

            var rule0 = Z.TT.MkTreeRule(A, A, 0, "zero", Z.True, A.MkTree("zero", A.AttrVar));
            var rule1 = Z.TT.MkTreeRule(A, A, 0, "one", Z.True, A.MkTree("one", A.AttrVar, q(0,1)));
            var rule2 = Z.TT.MkTreeRule(A, A, 0, "two", Z.True, A.MkTree("two", A.AttrVar, q(0,2), q(0,1)));

            var F = Z.TT.MkTreeAutomaton(0, A, A, new TreeRule[] { rule0, rule1, rule2 });
            var FF = F.Compose(F);

            var t1 = A.MkTree("two", Z.MkInt(22), A.MkTree("zero", Z.MkInt(5)), A.MkTree("zero", Z.MkInt(6)));
            var t2 = A.MkTree("two", Z.MkInt(22), A.MkTree("zero", Z.MkInt(6)), A.MkTree("zero", Z.MkInt(5)));

            var FFF = FF.Compose(F);
            Assert.AreEqual(3, FFF.RuleCount);

            var s1 = F[t1][0];
            var s2 = FF[t1][0];
            var s3 = FFF[t1][0];

            Assert.AreEqual<Expr>(t2, s1);
            Assert.AreEqual<Expr>(t1, s2);
            Assert.AreEqual<Expr>(t2, s3);
        }

        [TestMethod]
        public void TestCompositionCornerCase2()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));

            var q0 = Z.MkInt(0);
            Func<int, int, Expr> q = (state, var) =>
            {
                return A.MkTrans(A, state, var);
            };

            //two(1+x0,one(x1),one(x2))
            var b = A.MkTree("two",
                        Z.MkAdd(Z.MkInt(1), A.AttrVar),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(1), A.AttrVar), q(0,1)),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(2), A.AttrVar), q(0,1)));

            //add 100 to the attribute of a zero-node
            var rule00 = Z.TT.MkTreeRule(A, A, 0, "zero", Z.True, A.MkTree("zero", Z.MkAdd(Z.MkInt(100),A.AttrVar)));
            //keep one-nodes unchanged
            var rule01 = Z.TT.MkTreeRule(A, A, 0, "one", Z.True, A.MkTree("one", A.AttrVar, q(1,1)));
            //apply transformation to the second child and swap it with the first child
            var rule02 = Z.TT.MkTreeRule(A, A, 0, "two", Z.True, A.MkTree("two", A.AttrVar, A.MkTrans(A, 0, 2), q(1,1)));

            //identity mapping
            var rule10 = Z.TT.MkTreeRule(A, A, 1, "zero", Z.True, A.MkTree("zero", A.AttrVar));
            var rule11 = Z.TT.MkTreeRule(A, A, 0, "one", Z.True, A.MkTree("one", A.AttrVar, q(1, 1)));
            var rule12 = Z.TT.MkTreeRule(A, A, 0, "two", Z.True, A.MkTree("two", A.AttrVar, q(1, 1), q(1, 2)));


            var F = Z.TT.MkTreeAutomaton(0, A, A, new TreeRule[] { rule00, rule01, rule02, rule10, rule11, rule12 });
            var FF = F.Compose(F);

            var t1 = A.MkTree("two", Z.MkInt(22), A.MkTree("zero", Z.MkInt(5)), A.MkTree("zero", Z.MkInt(6)));
            var t2 = A.MkTree("two", Z.MkInt(22), A.MkTree("zero", Z.MkInt(106)), A.MkTree("zero", Z.MkInt(5)));
            var t3 = A.MkTree("two", Z.MkInt(22), A.MkTree("zero", Z.MkInt(105)), A.MkTree("zero", Z.MkInt(106)));
            var t4 = A.MkTree("two", Z.MkInt(22), A.MkTree("zero", Z.MkInt(206)), A.MkTree("zero", Z.MkInt(105)));

            var FFF = FF.Compose(F);
            //Assert.AreEqual(3, FFF.Rules.Count);

            var s2 = F[t1][0];
            var s3 = FF[t1][0];
            var s4 = FFF[t1][0];

            Assert.AreEqual<Expr>(t2, s2);
            Assert.AreEqual<Expr>(t3, s3);
            Assert.AreEqual<Expr>(t4, s4);
        }

        [TestMethod]
        public void TestErrorCases()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));

            //two(1+x0,one(x1),one(x2))
            var b = A.MkTree("two",
                        Z.MkAdd(Z.MkInt(1), A.AttrVar),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(1), A.AttrVar), A.ChildVar(1)),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(2), A.AttrVar), A.ChildVar(2)));

            //add 100 to the attribute of a zero-node
            var rule0 = Z.TT.MkTreeRule(A, A, 0, "zero", Z.True, A.MkTree("zero", Z.MkAdd(Z.MkInt(100), A.AttrVar)));
            //keep one-nodes unchanged
            var rule1 = Z.TT.MkTreeRule(A, A, 0, "one", Z.True, A.MkTree("one", A.AttrVar, A.ChildVar(1)));
            //apply transformation to the second child and swap it with the first child
            var rule2 = Z.TT.MkTreeRule(A, A, 0, "two", Z.True, A.MkTree("two", A.AttrVar, A.MkTrans(A, 1, 2), A.ChildVar(1)));

            try
            {
                var F = Z.TT.MkTreeAutomaton(0, A, A, new TreeRule[] { rule0, rule1, rule2 });
                Assert.IsTrue(false, "must not reach this line");
            }
            catch (AutomataException e)
            {
                Assert.AreEqual(AutomataExceptionKind.TreeTransducer_InvalidStateId, e.kind);
            }

            try
            {
                var rule2b = Z.TT.MkTreeRule(A, A, 0, "two", Z.True, A.MkTree("two", A.AttrVar, A.MkTrans(A, 0, 3), A.ChildVar(1)));
                var F = Z.TT.MkTreeAutomaton(0, A, A, new TreeRule[] { rule0, rule1, rule2b });
                Assert.IsTrue(false, "must not reach this line");
            }
            catch (AutomataException e)
            {
                Assert.AreEqual(AutomataExceptionKind.RankedAlphabet_ChildAccessorIsOutOufBounds, e.kind);
            }
        }

        [TestMethod]
        public void TestLanguageIntersection()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));
            var B = A;
            var C = A;

            var a_rule2 = A.MkAcceptorRule(0, "two", Z.MkGe(A.AttrVar, Z.MkInt(2)), 0, 0);
            var a_rule1 = A.MkAcceptorRule(0, "one", Z.MkGe(A.AttrVar, Z.MkInt(1)), 0);
            var a_rule0 = A.MkAcceptorRule(0, "zero", Z.MkGe(A.AttrVar, Z.MkInt(0)));

            var b_rule2 = A.MkAcceptorRule(0, "two", Z.MkLe(A.AttrVar, Z.MkInt(2)), 0, 0);
            var b_rule1 = A.MkAcceptorRule(0, "one", Z.MkLt(A.AttrVar, Z.MkInt(1)), 0);
            var b_rule0 = A.MkAcceptorRule(0, "zero", Z.MkLe(A.AttrVar, Z.MkInt(0)));

            var a = A.MkTreeAcceptor(a_rule2, a_rule1, a_rule0);
            var b = A.MkTreeAcceptor(b_rule2, b_rule1, b_rule0);

            //all two-nodes have attribute 2, all zero-nodes have attribute 0 and no one-node is possible because the guards conflict
            var ab = a.Intersect(b);

            var t = A.MkTree("two", Z.MkInt(2), A.MkTree("two", Z.MkInt(2), A.MkTree("zero", Z.MkInt(0)), A.MkTree("zero", Z.MkInt(0))), A.MkTree("zero", Z.MkInt(0)));

            var t_out = ab[t];
            Assert.AreEqual<int>(1, t_out.Length);
            Assert.AreEqual<Expr>(null, t_out[0]);

            var t2 = A.MkTree("two", Z.MkInt(2), A.MkTree("two", Z.MkInt(3), A.MkTree("zero", Z.MkInt(0)), A.MkTree("zero", Z.MkInt(0))), A.MkTree("zero", Z.MkInt(0)));

            var t_out2 = ab[t2];
            Assert.AreEqual<int>(0, t_out2.Length);

            var t_a = a[t2];
            Assert.AreEqual<int>(1, t_a.Length);
            Assert.AreEqual<Expr>(null, t_a[0]);
        }

        [TestMethod]
        public void TestIdempotenceOfIdentityTransducer()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.UnitSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));
            var zero = A.MkTree("zero", Z.UnitConst);
            Func<Expr, Expr> one = x =>
            {
                return A.MkTree("one", Z.UnitConst, x);
            };
            Func<Expr, Expr, Expr> two = (x, y) =>
            {
                return A.MkTree("two", Z.UnitConst, x, y);
            };

            var id = A.IdTransducer;

            var id_o_id = id.Compose(id);
            var t1 = id_o_id[two(two(zero, zero), zero)];
            Assert.AreEqual<int>(1, t1.Length);
        }

        [TestMethod]
        public void TestLanguageIntersectionClassicalCase()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.UnitSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));

            var zero = A.MkTree("zero", Z.UnitConst);
            Func<Expr, Expr> one = x =>
            {
                return A.MkTree("one", Z.UnitConst, x);
            };
            Func<Expr, Expr, Expr> two = (x, y) =>
            {
                return A.MkTree("two", Z.UnitConst, x, y);
            };
 
            //the left child has no ones
            var a_rule2 = A.MkAcceptorRule(0, "two", 1, 0);
            var a_rule1 = A.MkAcceptorRule(0, "one", 0);
            var a_rule0 = A.MkAcceptorRule(0, "zero");
            var a_rule3 = A.MkAcceptorRule(1, "two", 1, 1);
            var a_rule4 = A.MkAcceptorRule(1, "zero");

            //the right child has no ones
            var b_rule2 = A.MkAcceptorRule(0, "two", 0, 1);
            var b_rule1 = A.MkAcceptorRule(0, "one", 0);
            var b_rule0 = A.MkAcceptorRule(0, "zero");
            var b_rule3 = A.MkAcceptorRule(1, "two", 1, 1);
            var b_rule4 = A.MkAcceptorRule(1, "zero");


            var a = A.MkTreeAcceptor(a_rule2, a_rule1, a_rule0, a_rule3, a_rule4);
            var b = A.MkTreeAcceptor(b_rule2, b_rule1, b_rule0, b_rule3, b_rule4);

            //left and right subtrees have no ones
            var ab = a.Intersect(b);

            Assert.IsTrue(ab.Accepts(two(two(zero, zero), zero)));
            Assert.IsTrue(ab.Accepts(one(one(two(zero, zero)))));
            Assert.IsFalse(ab.Accepts(two(two(zero, one(zero)), zero)));
        }

        [TestMethod]
        public void TestDomainAutomatonCreation()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));

            var r1 = Z.TT.MkTreeRule(A, A, 0, "two", Z.MkGe(A.AttrVar, Z.MkInt(2)),
                A.MkTree("two", A.AttrVar, A.MkTree("one", A.AttrVar, A.MkTrans(A, 0, 1)),
                                           A.MkTree("two", A.AttrVar, A.MkTrans(A, 0, 2), A.MkTrans(A, 1, 2))));

            var r2 = Z.TT.MkTreeRule(A, A, 1, "two", Z.MkLe(A.AttrVar, Z.MkInt(5)),
               A.MkTree("two", A.AttrVar, A.MkTree("one", A.AttrVar, A.MkTrans(A, 0, 1)),
                                          A.MkTree("two", A.AttrVar, A.MkTrans(A, 0, 1), A.MkTrans(A, 1, 2))));

            var r3 = Z.TT.MkTreeRule(A, A, 1, "one", Z.True, A.MkTree("zero", A.AttrVar));

            var r4 = Z.TT.MkTreeRule(A, A, 0, "one", Z.True, A.MkTree("zero", A.AttrVar));
            var r5 = Z.TT.MkTreeRule(A, A, 0, "zero", Z.True, A.MkTree("zero", A.AttrVar));

            var T = Z.TT.MkTreeAutomaton(0, A, A, new TreeRule[] { r1, r2, r3, r4, r5 });

            var D = T.ComputeDomainAcceptor();

            Assert.AreEqual<int>(2, T.StateCount);
            Assert.AreEqual<int>(2, D.StateCount);
            Assert.AreEqual<int>(5, D.RuleCount);
        }

        [TestMethod]
        public void TestRegularLookaheadComposition1()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zeroA", "oneA", "twoA" }, new int[] { 0, 1, 2 }));
            var B = (Z.TT.MkRankedAlphabet("B", Z.IntSort, new string[] { "zeroB", "oneB", "twoB" }, new int[] { 0, 1, 2 }));
            var C = (Z.TT.MkRankedAlphabet("C", Z.IntSort, new string[] { "zeroC", "oneC", "twoC" }, new int[] { 0, 1, 2 }));

            //(two (plus 1 x0) (one (plus 1 x0) (q x1)) (one (plus 2 x0) (q x2)))
            var b = Z.MkApp(B["twoB"],
                        Z.MkAdd(Z.MkInt(1), A.AttrVar),
                        B.MkTree("oneB", Z.MkAdd(Z.MkInt(1), A.AttrVar), A.MkTrans(B, 0, 1)),
                        B.MkTree("oneB", Z.MkAdd(Z.MkInt(2), A.AttrVar), A.MkTrans(B, 0, 2)));

            //(two (plus 1 x0) (zero x0) (one (plus 100 x0) (q x2)))
            var b2 = Z.MkApp(B["twoB"],
                        Z.MkAdd(Z.MkInt(1), A.AttrVar),
                        B.MkTree("zeroB", A.AttrVar),
                        B.MkTree("oneB", Z.MkAdd(Z.MkInt(9), A.AttrVar), A.MkTrans(B, 0, 2)));

            var rule0 = Z.TT.MkTreeRule(A, B, 0, "zeroA", Z.True, B.MkTree("zeroB", A.AttrVar));
            var rule1 = Z.TT.MkTreeRule(A, B, 0, "twoA", Z.MkGt(A.AttrVar, Z.MkInt(0)), b);
            var rule2 = Z.TT.MkTreeRule(A, B, 0, "twoA", Z.MkGt(A.AttrVar, Z.MkInt(0)), b2);
            var rule3 = Z.TT.MkTreeRule(A, B, 0, "oneA", Z.MkGt(A.AttrVar, Z.MkInt(0)), B.MkTree("oneB", A.AttrVar, A.MkTrans(B, 0, 1)));

            var trans1 = Z.TT.MkTreeAutomaton(0, A, B, new TreeRule[] { rule0, rule1, rule2, rule3 });

            //(two x0 (one (plus 1 x0) (p x1)) (one (plus 2 x0) (p x2)))
            var a = A.MkTree("twoA", C.AttrVar,
             A.MkTree("oneA", Z.MkAdd(Z.MkInt(1), C.AttrVar), C.MkTrans(A, 1, 1)),
             A.MkTree("oneA", Z.MkAdd(Z.MkInt(2), C.AttrVar), C.MkTrans(A, 1, 2)));

            var a2 = A.MkTree("zeroA", C.AttrVar);

            var rule4 = Z.TT.MkTreeRule(C, A, 1, "twoC", Z.MkGt(C.AttrVar, Z.MkInt(-2)), a);
            var rule5 = Z.TT.MkTreeRule(C, A, 1, "zeroC", Z.MkGt(C.AttrVar, Z.MkInt(-3)), a2);

            var trans2 = Z.TT.MkTreeAutomaton(1, C, A, new TreeRule[] { rule4, rule5 });

            var trans12 = TreeTransducer.ComposeR(trans2, trans1);

            var rulesOut = trans12.GetRules(trans12.Root, C["twoC"]);
            Assert.AreEqual<int>(2, rulesOut.Count);

            var rulesOut2 = trans12.GetRules(trans12.Root, C["zeroC"]);
            Assert.AreEqual<int>(1, rulesOut2.Count);

            var tin = C.MkTree("twoC", Z.MkInt(55), C.MkTree("zeroC", Z.MkInt(66)), C.MkTree("zeroC", Z.MkInt(77)));
            var res = trans12[tin];
            Assert.AreEqual<int>(2, res.Length);
            Assert.AreEqual<int>(3, trans12.RuleCount);
        }

        [TestMethod]
        public void TestRegularLookaheadComposition2()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zeroA", "oneA", "twoA" }, new int[] { 0, 1, 2 }));
            var B = (Z.TT.MkRankedAlphabet("B", Z.IntSort, new string[] { "zeroB", "oneB", "twoB" }, new int[] { 0, 1, 2 }));
            var C = (Z.TT.MkRankedAlphabet("C", Z.IntSort, new string[] { "zeroC", "oneC", "twoC" }, new int[] { 0, 1, 2 }));

            var _0 = Z.MkInt(0);
            var _1 = Z.MkInt(1);
            var _2 = Z.MkInt(2);

            var AB_r0 = Z.TT.MkTreeRule(A, B, 0, "oneA", Z.MkEq(_0, A.AttrVar), B.MkTree("oneB", Z.MkAdd(_1, A.AttrVar), A.MkTrans(B, 1, 1)), new int[][] { new int[] { 1 } });
            var AB_r1 = Z.TT.MkTreeRule(A, B, 1, "oneA", Z.MkEq(_1, A.AttrVar), B.MkTree("oneB", Z.MkAdd(_1, A.AttrVar), A.MkTrans(B, 0, 1)), new int[][] { new int[] { 0 } });
            var AB_r2 = Z.TT.MkTreeRule(A, B, 1, "oneA", Z.MkEq(_2, A.AttrVar), B.MkTree("zeroB", Z.MkAdd(_1, A.AttrVar)));

            //just accept the input if the attribute is 1 and delete the child subtree
            var BC_r0 = Z.TT.MkTreeRule(B, C, 0, "oneB", Z.MkEq(_1, B.AttrVar), C.MkTree("zeroC", B.AttrVar));

            var AB = Z.TT.MkTreeAutomaton(0, A, B, new TreeRule[] { AB_r0, AB_r1, AB_r2});
            var BC = Z.TT.MkTreeAutomaton(0, B, C, new TreeRule[] { BC_r0 });

            var AC = TreeTransducer.ComposeR(AB, BC);

            Assert.AreEqual<int>(4, AC.RuleCount);
        }

        [TestMethod]
        public void TestRegularLookaheadComposition3()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "z", "u", "b" }, new int[] { 0, 1, 2 }));
            var B = (Z.TT.MkRankedAlphabet("B", Z.IntSort, new string[] { "z", "u", "b" }, new int[] { 0, 1, 2 }));
            var C = (Z.TT.MkRankedAlphabet("C", Z.IntSort, new string[] { "z", "u", "b" }, new int[] { 0, 1, 2 }));

            var _0 = Z.MkInt(0);
            var _1 = Z.MkInt(1);
            var _2 = Z.MkInt(2);
            var _7 = Z.MkInt(7);

            var AB_r0 = Z.TT.MkTreeRule(A, B, 0, "u", Z.MkLe(_0, A.AttrVar), B.MkTree("u", Z.MkAdd(_1, A.AttrVar), A.MkTrans(B, 1, 1)), new int[][] { new int[] { 3, 1 } });
            var AB_r1 = Z.TT.MkTreeRule(A, B, 1, "u", Z.MkLe(_1, A.AttrVar), B.MkTree("u", Z.MkAdd(_1, A.AttrVar), A.MkTrans(B, 0, 1)), new int[][] { new int[] { 2, 0 } });
            var AB_r2 = Z.TT.MkTreeRule(A, B, 1, "u", Z.MkEq(_2, A.AttrVar), B.MkTree("z", Z.MkAdd(_1, A.AttrVar)));

            var AB_q2 = Z.TT.MkTreeRule(A, B, 2, "u", Z.MkGe(_0, A.AttrVar), null, new int[][] { new int[] { 3 } });
            var AB_q3a = Z.TT.MkTreeRule(A, B, 3, "u", Z.MkGe(_1, A.AttrVar), null, new int[][] { new int[] { 2 } });
            var AB_q3b = Z.TT.MkTreeRule(A, B, 3, "u", Z.MkEq(_2, A.AttrVar), null);

            //just accept the input if the attribute is 1, delete the child subtree and return zeroC(1)
            var BC_r0 = Z.TT.MkTreeRule(B, C, 0, "u", Z.MkEq(_1, B.AttrVar), C.MkTree("z", Z.MkAdd(_7, B.AttrVar)));

            var AB = Z.TT.MkTreeAutomaton(0, A, B, new TreeRule[] { AB_r0, AB_r1, AB_r2, AB_q2, AB_q3a, AB_q3b });
            var BC = Z.TT.MkTreeAutomaton(0, B, C, new TreeRule[] { BC_r0 });

            var AC = TreeTransducer.ComposeR(AB, BC);

            Assert.AreEqual<int>(5, AC.RuleCount);
        }

        [TestMethod]
        public void TestInvalidAcceptorStateInOutput() 
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));

            Func<int, int, Expr> q = (state, var) => { return A.MkTrans(A, state, var); };

            //two(1+x0,one(x1),one(x2))
            var b = A.MkTree("two",
                        Z.MkAdd(Z.MkInt(1), A.AttrVar),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(1), A.AttrVar), q(0, 1)),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(2), A.AttrVar), q(0, 2)));

            var rule0 = Z.TT.MkTreeRule(A, A, 0, "zero", Z.True, A.MkTree("zero", A.AttrVar));
            var rule1 = Z.TT.MkTreeRule(A, A, 0, "one", Z.True, A.MkTree("one", A.AttrVar, q(0, 1)));
            var rule2 = Z.TT.MkTreeRule(A, A, 0, "two", Z.True, A.MkTree("two", A.AttrVar, q(0, 2), q(1, 1)));
            var rule3 = Z.TT.MkTreeRule(A, A, 1, "two", Z.True, null, new int[]{1}, new int[]{1});
            var rule4 = Z.TT.MkTreeRule(A, A, 1, "zero", Z.True, null);

            try
            {
                var F = Z.TT.MkTreeAutomaton(0, A, A, new TreeRule[] { rule0, rule1, rule2, rule3, rule4 });
            }
            catch (AutomataException e)
            {
                Assert.AreEqual(AutomataExceptionKind.TreeTransducer_InvalidUseOfAcceptorState, e.kind);
            }
        }

        [TestMethod]
        public void TestInvalidAcceptorStateInStart()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));

            Func<int, int, Expr> q = (state, var) => { return A.MkTrans(A, state, var); };

            //two(1+x0,one(x1),one(x2))
            var b = A.MkTree("two",
                        Z.MkAdd(Z.MkInt(1), A.AttrVar),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(1), A.AttrVar), q(0, 1)),
                        A.MkTree("one", Z.MkAdd(Z.MkInt(2), A.AttrVar), q(0, 2)));

            var rule0 = Z.TT.MkTreeRule(A, A, 0, "zero", Z.True, A.MkTree("zero", A.AttrVar));
            var rule1 = Z.TT.MkTreeRule(A, A, 0, "one", Z.True, A.MkTree("one", A.AttrVar, q(0, 1)));
            var rule2 = Z.TT.MkTreeRule(A, A, 0, "two", Z.True, A.MkTree("two", A.AttrVar, q(0, 2), q(0, 1)));
            var rule3 = Z.TT.MkTreeRule(A, A, 1, "two", Z.True, null, new int[] { 1 }, new int[] { 1 });
            var rule4 = Z.TT.MkTreeRule(A, A, 1, "zero", Z.True, A.MkTree("zero", A.AttrVar));

            try
            {
                var F = Z.TT.MkTreeAutomaton(0, A, A, new TreeRule[] { rule0, rule1, rule2, rule3, rule4 });
            }
            catch (AutomataException e)
            {
                Assert.AreEqual(AutomataExceptionKind.TreeTransducer_InvalidUseOfAcceptorState, e.kind);
            }
        }

        [TestMethod]
        public void TestDomainRestriction() 
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));
            var B = (Z.TT.MkRankedAlphabet("B", Z.IntSort, new string[] { "nolla", "yksi", "kaksi" }, new int[] { 0, 1, 2 }));
            var C = (Z.TT.MkRankedAlphabet("C", Z.IntSort, new string[] { "noll", "ett", "tva" }, new int[] { 0, 1, 2 }));

            Expr _0 = A.TT.Z.MkInt(0);
            Expr _1 = A.TT.Z.MkInt(1);
            Expr _11 = A.TT.Z.MkInt(11);
            Expr _22 = A.TT.Z.MkInt(22);
            Expr _33 = A.TT.Z.MkInt(33);
            Expr _111 = A.TT.Z.MkInt(111);
            Expr _222 = A.TT.Z.MkInt(222);
            Expr _333 = A.TT.Z.MkInt(333);

            Func<Expr, Expr> one = t => A.MkTree("one", _11, t);
            Func<Expr, Expr, Expr> two = (t1, t2) => A.MkTree("two", _22, t1, t2);
            Expr zero = A.MkTree("zero", _33);

            Func<int, int, Expr> q = (state, var) => { return A.MkTrans(B, state, var); };

            Func<Expr, Expr> yksi = t => B.MkTree("yksi", _111, t);
            Func<Expr, Expr, Expr> kaksi = (t1, t2) => B.MkTree("kaksi", _222, t1, t2);
            Expr nolla = B.MkTree("nolla", _333);

            var AB_rule0 = Z.TT.MkTreeRule(A, B, 0, "zero", Z.True, nolla);
            var AB_rule1 = Z.TT.MkTreeRule(A, B, 0, "two", Z.True, yksi(q(0, 1)), new int[]{0}, new int[]{1});
            var AB_rule2 = A.MkAcceptorRule(1, "zero");
            var AB_rule3 = A.MkAcceptorRule(1, "one", 1);
            var AB_rule4 = Z.TT.MkTreeRule(A, B, 0, "one", Z.True, yksi(q(0, 1)), new int[]{0});


            var AB = Z.TT.MkTreeAutomaton(0, A, B, new TreeRule[] { AB_rule0, AB_rule1, AB_rule2, AB_rule3, AB_rule4 });

            var D_rule1 = A.MkAcceptorRule(1, "zero");
            var D_rule2 = A.MkAcceptorRule(1, "two", 1, 1);

            var D = Z.TT.MkTreeAutomaton(1, A, A, new TreeRule[] { D_rule1, D_rule2 });
            var AB_D = AB.RestrictDomain(D);

            Assert.AreEqual<int>(3, AB_D.RuleCount);

            var inp1 = two(two(two(zero, zero), zero), zero);
            var inp2 = two(two(two(zero,zero),zero), one(one(zero)));
            var out1_actual = AB[inp1];
            var out2_actual = AB[inp2];
            var out_expected = yksi(yksi(yksi(nolla)));
            Assert.AreEqual<int>(1, out1_actual.Length);
            Assert.AreEqual<int>(1, out2_actual.Length);
            Assert.AreEqual<Expr>(out_expected, out1_actual[0]);
            Assert.AreEqual<Expr>(out_expected, out2_actual[0]);

            var out1_actual2 = AB_D[inp1];
            var out2_actual2 = AB_D[inp2];
            bool acc = AB_D.Accepts(inp2);
            var out_expected2 = yksi(yksi(yksi(nolla)));
            Assert.AreEqual<int>(1, out1_actual2.Length);
            Assert.AreEqual<int>(0, out2_actual2.Length);
            Assert.IsFalse(acc);
            Assert.AreEqual<Expr>(out_expected, out1_actual2[0]);
        }

        [TestMethod]
        public void TestDeterminization()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));

            var r1 = Z.TT.MkTreeRule(A, A, 0, "two", Z.MkGe(A.AttrVar, Z.MkInt(2)),
                A.MkTree("two", A.AttrVar, A.MkTree("one", A.AttrVar, A.MkTrans(A, 0, 1)),
                                           A.MkTree("two", A.AttrVar, A.MkTrans(A, 0, 2), A.MkTrans(A, 1, 2))));

            var r2 = Z.TT.MkTreeRule(A, A, 1, "two", Z.MkLe(A.AttrVar, Z.MkInt(5)),
               A.MkTree("two", A.AttrVar, A.MkTree("one", A.AttrVar, A.MkTrans(A, 0, 1)),
                                          A.MkTree("two", A.AttrVar, A.MkTrans(A, 0, 1), A.MkTrans(A, 1, 2))));

            var r3 = Z.TT.MkTreeRule(A, A, 1, "one", Z.True, A.MkTree("zero", A.AttrVar));

            var r4 = Z.TT.MkTreeRule(A, A, 0, "one", Z.True, A.MkTree("zero", A.AttrVar));
            var r5 = Z.TT.MkTreeRule(A, A, 0, "zero", Z.True, A.MkTree("zero", A.AttrVar));

            var T = Z.TT.MkTreeAutomaton(0, A, A, new TreeRule[] { r1, r2, r3, r4, r5 });

            var D = T.ComputeDomainAcceptor();

            Assert.AreEqual<int>(2, T.StateCount);
            Assert.AreEqual<int>(2, D.StateCount);
            Assert.AreEqual<int>(5, D.RuleCount);

            var D1 = D.Determinize();
            var D2 = D1.Minimize();

            Assert.AreEqual<int>(3, D2.StateCount);

            var D3 = D2.Complement();

            var D4 = D3.Complement();

            Assert.AreEqual<int>(3, D3.StateCount);
            Assert.AreEqual<int>(3, D4.StateCount);

        }

        [TestMethod]
        public void TestComplement()
        {
            #region copied from above: creates the tree acceptor D
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));
            var B = (Z.TT.MkRankedAlphabet("B", Z.IntSort, new string[] { "nolla", "yksi", "kaksi" }, new int[] { 0, 1, 2 }));
            var C = (Z.TT.MkRankedAlphabet("C", Z.IntSort, new string[] { "noll", "ett", "tva" }, new int[] { 0, 1, 2 }));

            Expr _0 = A.TT.Z.MkInt(0);
            Expr _1 = A.TT.Z.MkInt(1);
            Expr _11 = A.TT.Z.MkInt(11);
            Expr _22 = A.TT.Z.MkInt(22);
            Expr _33 = A.TT.Z.MkInt(33);
            Expr _111 = A.TT.Z.MkInt(111);
            Expr _222 = A.TT.Z.MkInt(222);
            Expr _333 = A.TT.Z.MkInt(333);

            Func<Expr, Expr> one = t => A.MkTree("one", _11, t);
            Func<Expr, Expr, Expr> two = (t1, t2) => A.MkTree("two", _22, t1, t2);
            Expr zero = A.MkTree("zero", _33);

            Func<int, int, Expr> q = (state, var) => { return A.MkTrans(B, state, var); };

            Func<Expr, Expr> yksi = t => B.MkTree("yksi", _111, t);
            Func<Expr, Expr, Expr> kaksi = (t1, t2) => B.MkTree("kaksi", _222, t1, t2);
            Expr nolla = B.MkTree("nolla", _333);

            var AB_rule0 = Z.TT.MkTreeRule(A, B, 0, "zero", Z.True, nolla);
            var AB_rule1 = Z.TT.MkTreeRule(A, B, 0, "two", Z.True, yksi(q(0, 1)), new int[] { 0 }, new int[] { 1 });
            var AB_rule2 = A.MkAcceptorRule(1, "zero");
            var AB_rule3 = A.MkAcceptorRule(1, "one", 1);
            var AB_rule4 = Z.TT.MkTreeRule(A, B, 0, "one", Z.True, yksi(q(0, 1)), new int[] { 0 });


            var AB = Z.TT.MkTreeAutomaton(0, A, B, new TreeRule[] { AB_rule0, AB_rule1, AB_rule2, AB_rule3, AB_rule4 });

            var D_rule1 = A.MkAcceptorRule(1, "zero");
            var D_rule2 = A.MkAcceptorRule(1, "two", 1, 1);

            var D = Z.TT.MkTreeAutomaton(1, A, A, new TreeRule[] { D_rule1, D_rule2 });
            var AB_D = AB.RestrictDomain(D);

            Assert.AreEqual<int>(3, AB_D.RuleCount);

            var inp1 = two(two(two(zero, zero), zero), zero);
            var inp2 = two(two(two(zero, zero), zero), one(one(zero)));
            var out1_actual = AB[inp1];
            var out2_actual = AB[inp2];
            var out_expected = yksi(yksi(yksi(nolla)));
            Assert.AreEqual<int>(1, out1_actual.Length);
            Assert.AreEqual<int>(1, out2_actual.Length);
            Assert.AreEqual<Expr>(out_expected, out1_actual[0]);
            Assert.AreEqual<Expr>(out_expected, out2_actual[0]);

            var out1_actual2 = AB_D[inp1];
            var out2_actual2 = AB_D[inp2];
            bool acc = AB_D.Accepts(inp2);
            var out_expected2 = yksi(yksi(yksi(nolla)));
            Assert.AreEqual<int>(1, out1_actual2.Length);
            Assert.AreEqual<int>(0, out2_actual2.Length);
            Assert.IsFalse(acc);
            Assert.AreEqual<Expr>(out_expected, out1_actual2[0]);
            #endregion

            var dta = D.Determinize();
            var cdta = dta.Complement();
            Assert.AreEqual(2, cdta.StateCount);
            Assert.AreEqual(7, cdta.RuleCount);
        }

        [TestMethod]
        public void TestComplementOfEmpty()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "one", "two" }, new int[] { 0, 1, 2 }));
            var dta = A.EmptyAcceptor;
            var cdta = dta.Complement();
            Assert.AreEqual(1, cdta.StateCount);
            Assert.AreEqual(3, cdta.RuleCount);
        }


        [TestMethod]
        public void TestTreeAutomataMinimization()
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "two" }, new int[] { 0, 2 }));

            Func<int,Expr> beta = (i => Z.MkEq(Z.MkInt(1), Z.MkMod(Z.MkDiv(A.AttrVar,Z.MkInt(1<<i)), Z.MkInt(2))));

            var r0 = Z.TT.MkTreeAcceptorRule(A, 0, "zero", beta(0));
            var r1 = Z.TT.MkTreeAcceptorRule(A, 1, "two", beta(1), 0 ,0 );
            var r2 = Z.TT.MkTreeAcceptorRule(A, 2, "two", beta(2), 1, 1);

            var T = Z.TT.MkTreeAutomaton(2, A, A, new TreeRule[] { r0, r1, r2 });

            var Tmin = T.Minimize();

            Assert.AreEqual(T.StateCount, Tmin.StateCount);
        }

        [TestMethod]
        public void TestTreeAutomataMinimization2()
        {
            for (int k = 2; k <= 5; k++)
                TestMinimization(k);
        }

        private static void TestMinimization(int K)
        {
            Z3Provider Z = new Z3Provider();
            var A = (Z.TT.MkRankedAlphabet("A", Z.IntSort, new string[] { "zero", "two" }, new int[] { 0, 2 }));

            Func<int, Expr> beta = (i => Z.MkEq(Z.MkInt(1), Z.MkMod(Z.MkDiv(A.AttrVar, Z.MkInt(1 << i)), Z.MkInt(2))));

            var r0 = Z.TT.MkTreeAcceptorRule(A, 0, "zero", beta(0));
            var r1 = Z.TT.MkTreeAcceptorRule(A, 1, "zero", beta(1));
            var rules = new List<TreeRule>();
            rules.Add(r0);
            rules.Add(r1);
            for (int i = 0; i < K; i++)
                rules.Add(Z.TT.MkTreeAcceptorRule(A, i + 1, "two", beta(i + 1), i, i));

            var T = Z.TT.MkTreeAutomaton(K, A, A, rules);

            var T1 = T.Determinize();
            var T2 = T1.RemoveUselessStates();

            var Tmin = T2.Minimize();

            Assert.AreNotEqual(T1.StateCount, Tmin.StateCount);

            Console.WriteLine("k = {0}, |Q| = {1}, |Delta| = {2}, |Q_d| = {3}, |Delta_d| = {4}, |Q_u| = {5}, |Delta_u| = {6},|Q_m| = {7}, |Delta_m| = {8},",
                K, T.StateCount, T.RuleCount, T1.StateCount, T1.RuleCount, T2.StateCount, T2.RuleCount, Tmin.StateCount, Tmin.RuleCount);
        }

    }
}