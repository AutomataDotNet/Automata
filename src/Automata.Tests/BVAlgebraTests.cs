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
    public class BVAlgebraTests
    {
        [TestMethod]
        public void TestBVAlgebraCharOperations()
        {
            var css = new CharSetSolver();
            var regex = new Regex("(?i:abc[^a-z])");
            var sr = css.RegexConverter.ConvertToSymbolicRegex(regex, true);
            var mintermsList = new List<BDD>(sr.ComputeMinterms());
            var minterms = mintermsList.ToArray();
            var all = css.MkOr(minterms);
            Assert.IsTrue(all.IsFull);
            Assert.AreEqual(5, minterms.Length);
            var bva = BVAlgebra.Create(css, minterms);
            var bv1 = bva.MkBV(1, 2);
            var bv2 = bva.MkBV(2, 3, 4);
            var a = bva.MkCharConstraint('A');
            var b = bva.MkCharConstraint('b');
            var c = bva.MkCharConstraint('C');
            var d = bva.MkCharConstraint('6');
            var x = bva.MkCharConstraint('x');
            Assert.AreEqual<ulong>(bva.atoms[bva.dtree.GetId('a')].first, a.first);
            Assert.AreEqual<ulong>(bva.atoms[bva.dtree.GetId('B')].first, b.first);
            Assert.AreEqual<ulong>(bva.atoms[bva.dtree.GetId('C')].first, c.first);
            Assert.AreEqual<ulong>(bva.atoms[bva.dtree.GetId('7')].first, d.first);
            Assert.AreEqual<ulong>(bva.atoms[bva.dtree.GetId('z')].first, x.first);
        }

        [TestMethod]
        public void TestBVAlgebraBasicOperations()
        {
            var css = new CharSetSolver();
            var regexa = new Regex("(?i:a)");
            var aut = css.Convert("(?i:a)");
            var aut_minterms = css.RegexConverter.ConvertToSymbolicRegex(regexa, true).ComputeMinterms();
            var aut_bva = BVAlgebra.Create(css, aut_minterms);
            var aut_BV = aut.ReplaceAlgebra<BV>(bdd => aut_bva.MapPredToBV(bdd, aut_minterms), aut_bva);
            //aut_BV.ShowGraph("aut_BV");
            var aut_BV_det = aut_BV.Determinize().Minimize();
            //aut_BV_det.ShowGraph("aut_BV_det");
            Assert.AreEqual<int>(2, aut_BV_det.StateCount);
            Assert.AreEqual<int>(3, aut_BV_det.MoveCount);
            var a_bv = aut_bva.MkCharConstraint('a');
            var a_id = aut_bva.GetIdOfChar('a');
            var A_id = aut_bva.GetIdOfChar('A');
            Assert.AreEqual<int>(a_id, A_id);
            Assert.AreEqual<BV>(a_bv, aut_bva.atoms[a_id]);
            Assert.AreEqual<int>(3, aut_BV_det.MoveCount);
            Assert.AreEqual<int>(2, aut_bva.atoms.Length);
        }

        [TestMethod]
        public void TestBVAlgebraDeterminization()
        {
            var css = new CharSetSolver();
            var regex1 = new Regex("afaXbccde$");
            var autom1 = css.Convert(regex1.ToString());
            //dfa1.ShowGraph("dfa1");
            var minterms1 = css.RegexConverter.ConvertToSymbolicRegex(regex1).ComputeMinterms();
            BVAlgebra bva = BVAlgebra.Create(css, minterms1);
            var autom1_bv = autom1.ReplaceAlgebra(bdd => bva.MapPredToBV(bdd, minterms1), bva);
            //autom1_bv.ShowGraph("autom1_bv");
            var dfa1_bv = autom1_bv.Determinize();
            var dfa1 = autom1.Determinize();
            Assert.AreEqual<int>(dfa1_bv.StateCount, dfa1.StateCount);
        }

        [TestMethod]
        public void TestBVAlgebraMintermization()
        {
            var css = new CharSetSolver();
            var regex = new Regex(@"^\w\d$");
            var minterms = css.RegexConverter.ConvertToSymbolicRegex(regex).ComputeMinterms();
            Assert.IsTrue(minterms.Length == 3);
            BVAlgebra bva = BVAlgebra.Create(css, minterms);
            var relativeminterms = new List<Tuple<bool[], BV>>(bva.GenerateMinterms(bva.atoms));
            Assert.AreEqual<int>(3, relativeminterms.Count);
        }

        [TestMethod]
        public void TestBVAlgebraBooleanOperations()
        {
            var css = new CharSetSolver();
            var regex = new Regex(@"^\w\d$");
            var minterms = css.RegexConverter.ConvertToSymbolicRegex(regex).ComputeMinterms();
            Assert.IsTrue(minterms.Length == 3);
            BVAlgebra bva = BVAlgebra.Create(css, minterms);
            var phi = bva.atoms[0] & bva.atoms[1];
            Assert.IsFalse(bva.IsSatisfiable(phi));
            var psi = bva.atoms[0] & bva.MkNot(bva.atoms[1]);
            Assert.IsTrue(bva.IsSatisfiable(psi));
            Assert.IsTrue(bva.AreEquivalent(bva.atoms[0], psi));
            var psi2 = bva.atoms[0] | bva.atoms[1];
            Assert.IsTrue(bva.AreEquivalent(bva.atoms[0] | bva.atoms[1],bva.MkNot(bva.atoms[2])));
        }
    }
}
