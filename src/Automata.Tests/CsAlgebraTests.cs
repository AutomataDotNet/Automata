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
            var csa = new CsAlgebra<BDD>(css, 3);
            var alpha = csa.MkPredicate(ab, CsCondition.EMPTY, CsCondition.CANEXIT, CsCondition.EMPTY);
            var beta = csa.MkPredicate(aA, CsCondition.EMPTY, CsCondition.CANLOOP, CsCondition.EMPTY);
            var gamma = csa.MkAnd(alpha, beta);
            var res = new List<Tuple<CsConditionSeq, BDD>>(gamma.GetSumOfProducts());
            Assert.IsTrue(res.Count == 1);
            var counter_cond = res[0].Item1;
            var input_pred = res[0].Item2;
            Assert.IsTrue(input_pred.Equals(a));
            Assert.IsTrue(counter_cond[1] == CsCondition.MIDDLE); 
        }
    }
}
