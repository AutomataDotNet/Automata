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

namespace Microsoft.Automata.Tests
{
    [TestClass]
    public class CsAlgebraTests
    {
        [TestMethod]
        public void TestCsAlgebra_Mk()
        {
            var css = new CharSetSolver();
            var aA = css.MkCharConstraint('a', true);
            var a = css.MkCharConstraint('a', false);
            var b = css.MkCharConstraint('b');
            var ab = css.MkOr(a, b);
            var csa = new CsAlgebra<BDD>(css, new ICounter[] {new BoundedCounter(0,4,5), new BoundedCounter(1, 4, 5), new BoundedCounter(2, 4, 5), });
            var alpha = csa.MkPredicate(ab, true, CsCondition.TRUE, CsCondition.CANEXIT, CsCondition.TRUE);
            var alpha_cases = alpha.ToArray();
            Assert.IsTrue(alpha_cases.Length == 1);
            Assert.IsTrue(alpha_cases[0].Item2.Equals(ab));
            Assert.IsTrue(alpha_cases[0].Item1.Equals(CsConditionSeq.MkAND(CsCondition.TRUE, CsCondition.CANEXIT, CsCondition.TRUE)));
            var beta = csa.MkPredicate(aA, true, CsCondition.TRUE, CsCondition.CANLOOP, CsCondition.TRUE);
            var gamma = csa.MkAnd(alpha, beta);
            var res = new List<Tuple<CsConditionSeq, BDD>>(gamma.GetSumOfProducts());
            Assert.IsTrue(res.Count == 1);
            var counter_cond = res[0].Item1;
            var input_pred = res[0].Item2;
            Assert.IsTrue(input_pred.Equals(a));
            Assert.IsTrue(counter_cond[1] == CsCondition.MIDDLE);
        }

        [TestMethod]
        public void Test_CsConditionSeq_And()
        {
            CsConditionSeq seq = CsConditionSeq.MkTrue(64);
            var seq1 = seq.And(45, CsCondition.CANLOOP).And(45, CsCondition.CANEXIT);
            Assert.IsTrue(seq1.IsSatisfiable);
            for (int i = 0; i < 64; i++)
            {
                if (i == 45)
                    Assert.IsTrue(seq1[45] == CsCondition.MIDDLE);
                else
                    Assert.IsTrue(seq1[i] == CsCondition.TRUE);
            }
        }

        [TestMethod]
        public void Test_CsConditionSeq_Or()
        {
            CsConditionSeq seq = CsConditionSeq.MkFalse(64);
            var seq1 = seq.Or(23, CsCondition.LOW).Or(23, CsCondition.MIDDLE);
            seq1 = seq1.Or(63, CsCondition.HIGH).Or(63, CsCondition.MIDDLE);
            Assert.IsTrue(seq1.IsSatisfiable);
            for (int i = 0; i < 64; i++)
            {
                if (i == 23)
                    Assert.IsTrue(seq1[i] == CsCondition.CANLOOP);
                else if (i == 63)
                    Assert.IsTrue(seq1[i] == CsCondition.CANEXIT);
                else
                    Assert.IsTrue(seq1[i] == CsCondition.FALSE);
            }
        }

        [TestMethod]
        public void Test_CsConditionSeq_Not()
        {
            CsConditionSeq seq = CsConditionSeq.MkFalse(64);
            Assert.IsFalse(seq.IsSatisfiable);
            Assert.IsTrue((~seq).IsValid);
            var seq1 = seq.Or(23, CsCondition.LOW).Or(23, CsCondition.MIDDLE);
            Assert.IsTrue(seq1.IsSatisfiable);
            Assert.IsTrue((~seq1).IsSatisfiable);
            var seq2 = seq1.Or(54, CsCondition.TRUE);
            Assert.IsTrue(seq2.IsValid);
            Assert.IsTrue(seq2.IsSatisfiable);
            Assert.IsFalse((~seq2).IsSatisfiable);
            Assert.AreEqual(seq1, ~~seq1);
            var rnd = CsConditionSeq.MkTrue(10);
            var random = new Random(0);
            for (int i = 0; i < 10; i++)
                rnd = rnd.And(i, (CsCondition)random.Next(1, 15));
            Assert.IsTrue(rnd.IsSatisfiable);
            Assert.IsFalse(rnd.IsValid);
            Assert.IsTrue((~rnd).IsSatisfiable);
            Assert.IsFalse((~rnd).IsValid);
        }
    }
}
