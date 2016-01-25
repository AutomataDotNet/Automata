using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Diagnostics;

using Microsoft.Automata.MSO;
using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Z3;

namespace MSOZ3Test
{
    public class MyTestContext : TestContext
    {
        public MyTestContext()
        {
        }

        //to be able to prit to Immediate window in Debug mode
        //outside this wrapper Debug.WriteLine does not work
        public override void WriteLine(string format, params object[] args)
        {
            //System.Diagnostics.Trace.WriteLine(string.Format(format, args));
            Debug.WriteLine(format, args);
        }


        public override void AddResultFile(string fileName)
        {
            throw new NotImplementedException();
        }

        public override void BeginTimer(string timerName)
        {
            throw new NotImplementedException();
        }

        public override System.Data.Common.DbConnection DataConnection
        {
            get { throw new NotImplementedException(); }
        }

        public override System.Data.DataRow DataRow
        {
            get { throw new NotImplementedException(); }
        }

        public override void EndTimer(string timerName)
        {
            throw new NotImplementedException();
        }

        public override System.Collections.IDictionary Properties
        {
            get { throw new NotImplementedException(); }
        }
    }

    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class MSOTTest
    {

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        //static TestContext testContextInstance;

        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //    testContextInstance = new MyTestContext();
        //}
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            TestContext = new MyTestContext();
        }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        private TestContext testContextInstance;
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void TestWS1S_NotLt()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var fo = new WS1SAnd<BDD>(new WS1SSingleton<BDD>("x"), new WS1SSingleton<BDD>("y"));
            var aut_fo = fo.GetAutomaton(ca, "x", "y");
            WS1SFormula<BDD> xLTy = new WS1SLt<BDD>("x", "y");
            WS1SFormula<BDD> not_xLTy = new WS1SNot<BDD>(xLTy);
            not_xLTy = new WS1SAnd<BDD>(not_xLTy, fo); //*
            WS1SFormula<BDD> xEQy = new WS1SEq<BDD>("x", "y");
            xEQy = new WS1SAnd<BDD>(xEQy, fo); //*
            var yGTx = new WS1SLt<BDD>("y", "x");
            var xEQy_or_yGTx = new WS1SAnd<BDD>(new WS1SOr<BDD>(xEQy, yGTx), fo);
            var aut_not_xLTy = not_xLTy.GetAutomaton(ca, "x", "y");
            var B = xEQy_or_yGTx.GetAutomaton(ca, "x", "y");
            var c_aut_xLTy = xLTy.GetAutomaton(ca, "x", "y").Complement(ca).Determinize(ca).Minimize(ca);
            //c_aut_xLTy = c_aut_xLTy.Intersect(aut_fo, ca).Determinize(ca).Minimize(ca); //*
            //aut_not_xLTy.ShowGraph("aut_not_xLTy");
            //B.ShowGraph("x_geq_y");
            //c_aut_xLTy.ShowGraph("c_aut_xLTy");
            var equiv1 = aut_not_xLTy.IsEquivalentWith(B, ca);
            //var equiv2 = aut_not_xLTy.IsEquivalentWith(c_aut_xLTy, ca);
            Assert.IsTrue(equiv1);
            //Assert.IsTrue(equiv2);
        }


        [TestMethod]
        public void TestWS1S_Equal()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var fo_x = new WS1SSingleton<BDD>("x");
            var fo_y = new WS1SSingleton<BDD>("y");
            WS1SFormula<BDD> fo = new WS1SAnd<BDD>(fo_x, fo_y);
            WS1SFormula<BDD> xSy = new WS1SSubset<BDD>("x", "y");
            WS1SFormula<BDD> ySx = new WS1SSubset<BDD>("y", "x");
            WS1SFormula<BDD> yEQx = new WS1SAnd<BDD>(xSy, ySx);
            yEQx = new WS1SAnd<BDD>(yEQx, fo);
            var aut_yEQx = yEQx.GetAutomaton(ca, "x", "y");
            //aut_yEQx.ShowGraph("aut_yEQx");
        }

        [TestMethod]
        public void TestWS1S_Member()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var fo_x = new WS1SSingleton<BDD>("x");
            WS1SFormula<BDD> xSy = new WS1SSubset<BDD>("x", "y");
            var mem = new WS1SAnd<BDD>(xSy, fo_x);
            var aut_mem = mem.GetAutomaton(ca, "x", "y");
            //aut_mem.ShowGraph("aut_mem");
        }

        [TestMethod]
        public void TestWS1S_Label()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var pred = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'c'), "x");
            var fo_x = new WS1SSingleton<BDD>("x");
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var lab = new WS1SAnd<BDD>(pred, fo_x);
            var lab_aut = lab.GetAutomaton(ca, "x");
            //lab_aut.ShowGraph("lab_aut");
        }


        [TestMethod]
        public void TestWS1S_NotLabel()  
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var pred = new WS1SPred<BDD>(solver.MkCharConstraint(false, 'c'), "x");
            var fo_x = new WS1SSingleton<BDD>("x");
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var lab = new WS1SAnd<BDD>(pred, fo_x);
            WS1SFormula<BDD> not_lab = new WS1SNot<BDD>(lab);
            var not_lab_actual = new WS1SAnd<BDD>(not_lab, fo_x);
            var aut_not_lab = not_lab_actual.GetAutomaton(ca, "x");
            var aut_not_lab_prelim = not_lab.GetAutomaton(ca, "x");
            var c_aut_lab = lab.GetAutomaton(ca, "x").Complement(ca).Minimize(ca);
            //c_aut_lab.ShowGraph("c_aut_lab");
            //aut_not_lab.ShowGraph("aut_not_lab");
            //aut_not_lab_prelim.ShowGraph("aut_not_lab_prelim");
            //TBD: equivalence
        }

        [TestMethod]
        public void TestWS1S_SuccDef_GetAutomaton()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var xLTy = new WS1SLt<BDD>("x", "y");
            var xLTzLTy = new WS1SAnd<BDD>(new WS1SLt<BDD>("x", "z"), new WS1SLt<BDD>("z", "y"));
            var Ez = new WS1SExists<BDD>("z", xLTzLTy);
            var notEz = new WS1SNot<BDD>(Ez);
            var xSyDef = new WS1SAnd<BDD>(xLTy, notEz);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var aut_xSyDef = xSyDef.GetAutomaton(ca,"x","y");
            var aut_xLTzLTy = xLTzLTy.GetAutomaton(ca, "x", "y", "z");
            var aut_Ez = Ez.GetAutomaton(ca, "x", "y");
            var aut_notEz = notEz.GetAutomaton(ca, "x", "y");
            var aut_xLTy = xLTy.GetAutomaton(ca, "x", "y");

            //aut_xSyDef.ShowGraph("aut_xSyDEf");
            //aut_xLTzLTy.ShowGraph("aut_xLTzLTy");
            //aut_Ez.ShowGraph("aut_Ez");
            //aut_notEz.ShowGraph("aut_notEz");

            var xSyPrim = new WS1SSuccN<BDD>("x", "y", 1);
            var aut_xSyPrim = xSyPrim.GetAutomaton(ca, "x", "y");
            var equiv = aut_xSyPrim.IsEquivalentWith(aut_xSyDef, ca);
            Assert.IsTrue(equiv);
        }

        [TestMethod]
        public void TestWS1S_SuccDef_GetAutomatonBDD()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var xLTy = new WS1SLt<BDD>("x", "y");
            var xLTzLTy = new WS1SAnd<BDD>(new WS1SLt<BDD>("x", "z"), new WS1SLt<BDD>("z", "y"));
            var Ez = new WS1SExists<BDD>("z", xLTzLTy);
            var notEz = new WS1SNot<BDD>(Ez);
            var xSyDef = new WS1SAnd<BDD>(xLTy, notEz);

            var aut_xSyDef = xSyDef.GetAutomatonBDD(solver, "x", "y");
            var aut_xLTzLTy = xLTzLTy.GetAutomatonBDD(solver, "x", "y", "z");
            var aut_Ez = Ez.GetAutomatonBDD(solver, "x", "y").Determinize(solver).Minimize(solver);
            var aut_notEz = notEz.GetAutomatonBDD(solver, "x", "y");
            var aut_xLTy = xLTy.GetAutomatonBDD(solver, "x", "y");

            //aut_xSyDef.ShowGraph("aut_xSyDEf");
            //aut_xLTzLTy.ShowGraph("aut_xLTzLTy");
            //aut_Ez.ShowGraph("aut_Ez");
            //aut_notEz.ShowGraph("aut_notEz");

            var xSyPrim = new WS1SSuccN<BDD>("x", "y", 1);
            var aut_xSyPrim = xSyPrim.GetAutomatonBDD(solver, "x", "y");
            var equiv = aut_xSyPrim.IsEquivalentWith(aut_xSyDef, solver);
            Assert.IsTrue(equiv);
        }

        [TestMethod]
        public void TestWS1S_UseOfCharRangePreds_BDD() 
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var isDigit = solver.MkCharSetFromRegexCharClass(@"\d");
            var isWordLetter = solver.MkCharSetFromRegexCharClass(@"\w");
            TestWS1S_UseOfCharRangePreds<BDD>(solver, isDigit, isWordLetter, solver.RegexConverter);
        }

        [TestMethod]
        public void TestWS1S_UseOfCharRangePreds_Z3()
        {
            var solver = new Z3Provider(BitWidth.BV7);
            var isDigit = solver.ConvertFromCharSet(solver.CharSetProvider.MkCharSetFromRegexCharClass(@"\d"));
            var isWordLetter = solver.ConvertFromCharSet(solver.CharSetProvider.MkCharSetFromRegexCharClass(@"\w"));
            TestWS1S_UseOfCharRangePreds<Expr>(solver, isDigit, isWordLetter, solver.RegexConverter);
        }

        public void TestWS1S_UseOfCharRangePreds<T>(IBoolAlgMinterm<T> solver, T isDigit, T isWordLetter, IRegexConverter<T> regexConverter)
        {
            var ca = new CartesianAlgebraBDD<T>(solver);
            //there are at least two distinct positions x and y
            var xy = new WS1SAnd<T>(new WS1SAnd<T>(new WS1SNot<T>(new WS1SEq<T>("x", "y")),
                new WS1SSingleton<T>("x")), new WS1SSingleton<T>("y"));
            //there is a set X containing x and y and all positions z in X have characters that satisfy isWordLetter
            var X = new WS1SExists<T>("X", new WS1SAnd<T>(
                new WS1SAnd<T>(new WS1SSubset<T>("x", "X"), new WS1SSubset<T>("y", "X")),
                new WS1SNot<T>(new WS1SExists<T>("z", new WS1SNot<T>(
                    new WS1SOr<T>(new WS1SNot<T>(new WS1SAnd<T>(new WS1SSingleton<T>("z"), new WS1SSubset<T>("z", "X"))),
                                    new WS1SPred<T>(isWordLetter, "z")))))));

            var psi2 = new WS1SAnd<T>(xy, X);
            var atLeast2wEE = new WS1SExists<T>("x", new WS1SExists<T>("y", psi2));
            var psi1 = new WS1SAnd<T>(new WS1SSingleton<T>("x"), new WS1SPred<T>(isDigit, "x"));
            var aut_psi1 = psi1.GetAutomaton(ca, "x");
            //aut_psi1.ShowGraph("SFA(psi1)");
            var atLeast1d = new WS1SExists<T>("x", psi1);
            var psi = new WS1SAnd<T>(atLeast2wEE, atLeast1d);
            var aut1 = psi.GetAutomaton(solver);
            //var aut_atLeast1d = atLeast1d.GetAutomaton(solver); 

            var aut2 = regexConverter.Convert(@"\w.*\w", System.Text.RegularExpressions.RegexOptions.Singleline).Intersect(regexConverter.Convert(@"\d"), solver);

            //aut1.ShowGraph("aut1");
            //aut2.ShowGraph("aut2");

            bool equiv = aut2.IsEquivalentWith(aut1, solver);
            Assert.IsTrue(equiv);


            //solver.ShowGraph(aut_atLeast1d, "aut_atLeast1d");

            var aut_psi2 = psi2.GetAutomaton(ca, "x", "y");
            // var aut_atLeast2wEE = atLeast2wEE.GetAutomaton(ca, "x", "y");
            // var aut_atLeast2wEE2 = atLeast2wEE.GetAutomaton(solver);
            //aut_psi2.ShowGraph("SFA(psi2)");
            //aut_atLeast2wEE.ShowGraph("aut_atLeast2wEE");
            //aut_atLeast2wEE2.ShowGraph("aut_atLeast2wEE2");
            //solver.ShowGraph(aut_atLeast2wEE2, "aut_atLeast2wEE2");
        }

        [TestMethod]
        public void TestWS1S_GetAutomatonBDD_eq_GetAutomaton()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var isDigit = solver.MkCharSetFromRegexCharClass(@"\d");
            var isLetter = solver.MkCharSetFromRegexCharClass(@"(c|C)");
            //there are at least two distinct positions x and y
            var xy = new WS1SAnd<BDD>(new WS1SAnd<BDD>(new WS1SNot<BDD>(new WS1SEq<BDD>("x", "y")),
                new WS1SSingleton<BDD>("x")), new WS1SSingleton<BDD>("y"));
            //there is a set X containing x and y and all positions z in X have characters that satisfy isWordLetter
            var X = new WS1SExists<BDD>("X", new WS1SAnd<BDD>(
                new WS1SAnd<BDD>(new WS1SSubset<BDD>("x", "X"), new WS1SSubset<BDD>("y", "X")),
                new WS1SNot<BDD>(new WS1SExists<BDD>("z", new WS1SNot<BDD>(
                    new WS1SOr<BDD>(new WS1SNot<BDD>(new WS1SAnd<BDD>(new WS1SSingleton<BDD>("z"), new WS1SSubset<BDD>("z", "X"))),
                                    new WS1SPred<BDD>(isLetter, "z")))))));

            var atLeast2w = new WS1SAnd<BDD>(xy, X);
            var atLeast2wEE = new WS1SExists<BDD>("x", new WS1SExists<BDD>("y", atLeast2w));
            var autBDD = atLeast2w.GetAutomatonBDD(solver, "x", "y");
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var autPROD = atLeast2w.GetAutomaton(ca, "x", "y");
            //autBDD.ShowGraph("autBDD");
            //autPROD.ShowGraph("autPROD");
            var aut_atLeast2wEE1 = atLeast2wEE.GetAutomaton(solver);
            var aut_atLeast2wEE2 = atLeast2wEE.GetAutomatonBDD(solver);
            //aut_atLeast2wEE1.ShowGraph("aut_atLeast2wEE1");
            //aut_atLeast2wEE2.ShowGraph("aut_atLeast2wEE2");
            Assert.IsTrue(aut_atLeast2wEE1.IsEquivalentWith(aut_atLeast2wEE2, solver));
        }

        [TestMethod]
        public void TestMSO_FirstLastZ3() 
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            MSOFormula<BDD> phi = new MSOExistsFo<BDD>("x", new MSOAnd<BDD>(new MSOFirst<BDD>("x"), new MSOLast<BDD>("x")));

            var aut = phi.GetAutomaton(solver);

            var res = solver.Convert("^[\0-\uFFFF]$");
            //solver.ShowGraph(res,"res");
            //solver.SaveAsDgml(res, "res");
            var eq = aut.IsEquivalentWith(res, solver);
            Assert.IsTrue(eq);
        }

        [TestMethod]
        public void TestMSO_Le()
        {
            var solver = new CharSetSolver(BitWidth.BV32);
            //var phi = new MSOTrue();
            MSOFormula<BDD> phi = new MSOExistsFo<BDD>("z", new MSOExistsFo<BDD>("x", new MSOExistsFo<BDD>("y",
                new MSOAnd<BDD>(
                    new MSOLt<BDD>("x", "y"),
                    new MSOLt<BDD>("y", "z") 
                    )
            ))); 


            var aut = phi.GetAutomaton(solver);

            var aut2 = solver.RegexConverter.Convert(".{3,}", System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.IsTrue(aut.IsEquivalentWith(aut2, solver));

            //solver.ShowGraph(aut, "SFA");
        }

        [TestMethod]
        public void TestMSO_Le1()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            //var phi = new MSOTrue();
            MSOFormula<BDD> phi = new MSOExistsFo<BDD>("x", new MSOExistsFo<BDD>("y",
                    new MSOLt<BDD>("x", "y")));


            var aut = phi.GetAutomaton(solver);

            var aut2 = solver.RegexConverter.Convert(".{2,}", System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.IsTrue(aut.IsEquivalentWith(aut2, solver));

            //new MSOLt<BDD>("x", "y").GetAutomaton(solver,"x","y").ShowGraph("");
        }

        [TestMethod]
        public void TestMSO_Eq()
        {
            var solver = new CharSetSolver(BitWidth.BV32);
            //var phi = new MSOTrue();
            MSOFormula<BDD> phi = new MSOExistsFo<BDD>("x", new MSOExistsFo<BDD>("y", new MSONot<BDD>(new MSOEq<BDD>("x", "y"))));

            var aut = phi.GetAutomaton(solver);

            var aut2 = solver.RegexConverter.Convert(".{2,}", System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.IsTrue(aut.IsEquivalentWith(aut2, solver));
        }

        [TestMethod]
        public void TestMSO_Forall()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            MSOFormula<BDD> phi = new MSOForallFo<BDD>("x", new MSOPredicate<BDD>(solver.MkCharConstraint(true, 'c'), "x"));

            var aut = phi.GetAutomaton(solver);
            //aut.ShowGraph("aut");
            for (int i = 0; i < 10; i++)
            {
                TestContext.WriteLine(solver.GenerateMember(aut));
            }
            var aut2 = solver.RegexConverter.Convert("^(c|C)*$");
            //aut2.ShowGraph("aut2");
            Assert.IsTrue(aut2.IsEquivalentWith(aut, solver));
        }

        [TestMethod]
        public void TestMSO_Pred()
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            var pred = new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'c'), "x");
            MSOFormula<BDD> phi = new MSOExistsFo<BDD>("x", pred);

            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var pred_aut = pred.GetAutomaton(ca, "x");
            //pred_aut.ShowGraph("pred_aut");

            var aut = phi.GetAutomaton(solver);
            for (int i = 0; i < 10; i++)
            {
                var s = solver.GenerateMember(aut);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, "c"), "regex mismatch");
            }
            var aut2 = solver.RegexConverter.Convert("c", System.Text.RegularExpressions.RegexOptions.Singleline);
            //aut2.ShowGraph("aut2");
            //aut.ShowGraph("aut");
            Assert.IsTrue(aut2.IsEquivalentWith(aut, solver), "automata not equialent");
        }

        [TestMethod]
        public void TestMSO_FirstC()
        {
            var solver = new CharSetSolver(BitWidth.BV32);
            //var phi = new MSOTrue();
            MSOFormula<BDD> phi = new MSOExistsFo<BDD>("x", new MSOAnd<BDD>(new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'C'), "x"), new MSOFirst<BDD>("x")));

            var aut = phi.GetAutomaton(solver);
            var aut2 = solver.RegexConverter.Convert("^C");
            Assert.IsTrue(aut2.IsEquivalentWith(aut, solver));
        }

        [TestMethod]
        public void TestMSO_Neg()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            //var phi = new MSOTrue();
            MSOFormula<BDD> phi = new MSONot<BDD>(new MSOExistsFo<BDD>("x", new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'c'), "x")));

            var aut = phi.GetAutomaton(solver);
            for (int i = 0; i < 10; i++)
            {
                var s = solver.GenerateMember(aut);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, "^[^c]*$"));
            }
            var aut2 = solver.RegexConverter.Convert("^[^c]*$");
            Assert.IsTrue(aut2.IsEquivalentWith(aut, solver));
        }

        [TestMethod]
        public void TestMSO_Or()
        {
            var solver = new CharSetSolver(BitWidth.BV32);
            MSOFormula<BDD> phi = new MSOForallFo<BDD>("x",
                    new MSOOr<BDD>(
                        new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'c'), "x"),
                        new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'a'), "x")
                    )
                );

            var aut = phi.GetAutomaton(solver);
            for (int i = 0; i < 10; i++)
            {
                var s = solver.GenerateMember(aut);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, "^[ac]*$"));
            }
            var aut2 = solver.RegexConverter.Convert("^[ac]*$");
            Assert.IsTrue(aut2.IsEquivalentWith(aut, solver));
        }

        [TestMethod]
        public void TestMSO_Succ()
        {
            var solver = new CharSetSolver(BitWidth.BV32);
            MSOFormula<BDD> phi = new MSOForallFo<BDD>("x",
                    new MSOIf<BDD>(
                        new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'c'), "x"),
                        new MSOExistsFo<BDD>("y",
                            new MSOAnd<BDD>(
                                new MSOSucc<BDD>("x", "y"),
                                new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'a'), "y")
                            )
                        )
                    )
                );

            var aut = phi.GetAutomaton(solver);
            for (int i = 0; i < 10; i++)
            {
                var s = solver.GenerateMember(aut);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, "^(ca|[^c])*$"));
            }
            var aut2 = solver.RegexConverter.Convert("^(ca|[^c])*$");
            Assert.IsTrue(aut2.IsEquivalentWith(aut, solver));
        }

        [TestMethod]
        public void TestMSO_Succ_Z3()
        {
            var solver = new Z3Provider(BitWidth.BV7);
            //var phi = new MSOTrue();
            MSOFormula<Expr> phi =
                new MSOForallFo<Expr>("x", new MSOForallFo<Expr>("x", new MSOForallFo<Expr>("x", new MSOForallFo<Expr>("x", new MSOForallFo<Expr>("x",
                    new MSOForallFo<Expr>("x", new MSOForallFo<Expr>("x",
                new MSOForallFo<Expr>("x",
                    new MSOIf<Expr>(
                        new MSOPredicate<Expr>(solver.MkCharConstraint(false, 'c'), "x"),
                        new MSOExistsFo<Expr>("y",
                            new MSOAnd<Expr>(
                                new MSOSucc<Expr>("x", "y"),
                                new MSOPredicate<Expr>(solver.MkCharConstraint(false, 'a'), "y")
                            )
                        )
                    )
                )
                )))))));

            var aut = phi.GetAutomaton(solver);
            var moves = new List<Move<BDD>>();
            foreach (var m in aut.GetMoves())
            {
                BDD bdd;
                solver.TryConvertToCharSet(m.Label, out bdd);
                moves.Add(new Move<BDD>(m.SourceState, m.TargetState, bdd));
            }
            var aut1 = Automaton<BDD>.Create(aut.InitialState, aut.GetFinalStates(), moves);
            //aut1.ShowGraph();
            for (int i = 0; i < 10; i++)
            {
                var s = solver.CharSetProvider.GenerateMember(aut1);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, "^(ca|[^c])*$"));
            }
            var aut2 = solver.RegexConverter.Convert("^(ca|[^c])*$");
            Assert.IsTrue(aut2.IsEquivalentWith(aut, solver));
        }

        [TestMethod]
        public void TestCount()
        {
            int maxvars = 10;
            var solver = new CharSetSolver(BitWidth.BV16);
            Stopwatch sw = new Stopwatch();
            var tries = 1;
            var times = new long[maxvars - 1];
            for (int k = 0; k < tries; k++)
            {
                for (int vars = 8; vars <= maxvars; vars++)
                {
                    MSOFormula<BDD> phi = new MSOTrue<BDD>();
                    for (int i = 1; i < vars; i++)
                    {
                        phi = new MSOAnd<BDD>(phi, new MSOLt<BDD>("x" + i, "x" + (i + 1)));
                        phi = new MSOAnd<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'a'), "x" + i));
                    }
                    phi = new MSOAnd<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'a'), "x" + vars));
                    phi = new MSOOr<BDD>(phi,
                        new MSOExistsFo<BDD>("y",
                        new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'c'), "y")));
                    for (int i = vars; i >= 1; i--)
                    {
                        phi = new MSOExistsFo<BDD>("x" + i, phi);
                    }


                    sw.Restart();
                    //var aut1 = phi.GetAutomaton(new CartesianAlgebraBDD<BDD>(solver));
                    var aut1 = phi.GetAutomaton(solver);
                    //aut1.ShowGraph();
                    sw.Stop();
                    times[vars - 2] += sw.ElapsedMilliseconds;
                    if (k == tries - 1)
                    {
                        TestContext.WriteLine(string.Format("{0} variables; {1} ms", vars, times[vars - 2] / tries));
                    }
                }
            }
        }

        [TestMethod]
        public void TestCountNoProduct()
        {
            int maxvars = 13;
            var solver = new CharSetSolver(BitWidth.BV7);
            Stopwatch sw = new Stopwatch();
            var tries = 1;
            var times = new long[maxvars - 1];
            for (int k = 0; k < tries; k++)
            {
                for (int vars = 8; vars <= maxvars; vars++)
                {
                    MSOFormula phi = new MSOTrue();
                    for (int i = 1; i < vars; i++)
                    {
                        phi = new MSOAnd(phi, new MSOLt("x" + i, "x" + (i + 1)));
                        phi = new MSOAnd(phi, new MSOPredicate(solver.MkCharConstraint(false, 'a'), "x" + i));
                        phi = new MSOOr(phi, new MSOPredicate(solver.MkCharConstraint(false, 'k'), "x" + i));
                    }
                    phi = new MSOAnd(phi, new MSOPredicate(solver.MkCharConstraint(false, 'a'), "x" + vars));
                    phi = new MSOOr(phi,
                        new MSOExistsFo("y",
                        new MSOPredicate(solver.MkCharConstraint(false, 'c'), "y")));
                    for (int i = vars; i >= 1; i--)
                    {
                        phi = new MSOExistsFo("x" + i, phi);
                    }

                    sw.Restart();
                    //var aut1 = phi.GetAutomaton(new CartesianAlgebraBDD<BDD>(solver));
                    var aut1 = phi.GetAutomaton(solver);
                    sw.Stop();
                    times[vars - 2] += sw.ElapsedMilliseconds;
                    //Console.WriteLine("States {0} Trans {1}",aut1.StateCount,aut1.MoveCount);
                    if (k == tries - 1)
                    {
                        TestContext.WriteLine(string.Format("{0} variables; {1} ms", vars, times[vars - 2] / tries));
                    }
                    //solver.ShowGraph(aut1, "a"+vars);
                }
            }
        }

        [TestMethod]
        public void TestCountNoProduct2()
        {
            int maxvars = 13;
            var solver = new CharSetSolver(BitWidth.BV7);
            Stopwatch sw = new Stopwatch();
            var tries = 1;
            var times = new long[maxvars - 1];
            for (int k = 0; k < tries; k++)
            {
                for (int vars = 8; vars <= maxvars; vars++)
                {
                    MSOFormula<BDD> phi = new MSOTrue<BDD>();
                    for (int i = 1; i < vars; i++)
                    {
                        phi = new MSOAnd<BDD>(phi, new MSOLt<BDD>("x" + i, "x" + (i + 1)));
                        phi = new MSOAnd<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'a'), "x" + i));
                        phi = new MSOOr<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'k'), "x" + i));
                    }
                    phi = new MSOAnd<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'a'), "x" + vars));
                    phi = new MSOOr<BDD>(phi,
                        new MSOExistsFo<BDD>("y",
                        new MSOPredicate<BDD>(solver.MkCharConstraint(false, 'c'), "y")));
                    for (int i = vars; i >= 1; i--)
                    {
                        phi = new MSOExistsFo<BDD>("x" + i, phi);
                    }

                    sw.Restart();
                    //var aut1 = phi.GetAutomaton(new CartesianAlgebraBDD<BDD>(solver));
                    var aut1 = phi.GetAutomaton(solver);
                    sw.Stop();
                    times[vars - 2] += sw.ElapsedMilliseconds;
                    //Console.WriteLine("States {0} Trans {1}",aut1.StateCount,aut1.MoveCount);
                    if (k == tries - 1)
                    {
                        TestContext.WriteLine(string.Format("{0} variables; {1} ms", vars, times[vars - 2] / tries));
                    }
                    //solver.ShowGraph(aut1, "a"+vars);
                }
            }
        }

        [TestMethod]
        public void TestIntZ3()
        {
            var solver = new Z3Provider();
            var isNeg = solver.MkLt(solver.MkVar(0, solver.IntSort), solver.MkInt(0));
            var isPos = solver.MkLt(solver.MkInt(0), solver.MkVar(0, solver.IntSort));

            var sort = solver.CharacterSort;
            //if x has a negative label then x has successor y with a nonnegative label
            var psi = new MSOIf<Expr>(
                        new MSOPredicate<Expr>(isNeg, "x"),
                        new MSOExistsFo<Expr>("y",
                            new MSOAnd<Expr>(
                                new MSOSucc<Expr>("x", "y"),
                                new MSOPredicate<Expr>(isPos, "y")
                            )
                        )
                    );
            //all negative labels are immediately followed by a positive label
            MSOFormula<Expr> phi = new MSOForallFo<Expr>("x", psi);
            var ca = new CartesianAlgebraBDD<Expr>(solver);
            var aut_psi = psi.GetAutomaton(ca,"x").Determinize(ca).Minimize(ca);
            var aut_phi = phi.GetAutomaton(solver).Determinize(solver).Minimize(solver);
            Assert.IsFalse(aut_phi.IsEmpty);
            //aut_phi.ShowGraph("aut_phi");
            //aut_psi.ShowGraph("aut_psi");
        }

        [TestMethod]
        public void TestIntExZ3()
        {
            var solver = new Z3Provider();

            var sort = solver.CharacterSort;
            //var phi = new MSOTrue();
            MSOFormula<Expr> phi = new MSOExistsFo<Expr>("x",
                   new MSOPredicate<Expr>(solver.MkLe(solver.MkVar(0, solver.IntSort), solver.MkInt(0)), "x")
                );

            var aut = phi.GetAutomaton(solver);
        }

        #region TestLarge helpers

        void TestMintermExplosion(int bitWidth, bool useBDD = false)
        {
            TestContext.WriteLine("----------------");
            TestContext.WriteLine(bitWidth.ToString());

            if (useBDD)
            {
                var S = new CharSetSolver(BitWidth.BV7);

                TestContext.WriteLine("BDD");
                int t = System.Environment.TickCount;
                var aut1 = CreateAutomaton1<BDD>(S.MkSetWithBitTrue, bitWidth, S);
                var aut2 = CreateAutomaton2<BDD>(S.MkSetWithBitTrue, bitWidth, S);
                var aut3 = CreateAutomaton3<BDD>(S.MkSetWithBitTrue, bitWidth, S);
                t = System.Environment.TickCount - t;
                TestContext.WriteLine(t + "ms");
                //aut.ShowGraph("aut" + bitWidth);
            }
            else
            {
                TestContext.WriteLine("Z3");
                Z3Provider Z = new Z3Provider(BitWidth.BV7);
                var x = Z.MkConst("x", Z.IntSort);
                Func<int, Expr> f = (i => Z.MkEq((Z.MkInt(1)), Z.MkMod(Z.MkDiv(x, Z.MkInt(1 << (i % 32))), Z.MkInt(2))));
                int t = System.Environment.TickCount;
                var aut1 = CreateAutomaton1<Expr>(f, bitWidth, Z);
                var aut2 = CreateAutomaton2<Expr>(f, bitWidth, Z);
                var aut3 = CreateAutomaton3<Expr>(f, bitWidth, Z);
                t = System.Environment.TickCount - t;
                TestContext.WriteLine(t + "ms");
                //aut.ShowGraph("aut" + bitWidth);
            }
        }

        Automaton<T> CreateAutomaton1<T>(Func<int,T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {
            Func<int, string, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), s);

            MSOFormula<T> phi = new MSOFalse<T>();

            for (int index = 0; index < bitWidth; index++)
            {
                var phi1 = pred(index, "var");
                phi = new MSOOr<T>(phi, phi1);
            }

            phi = new MSOExistsFo<T>("var", phi);

            var aut = phi.GetAutomaton(Z);

            return aut;
        }

        Automaton<T> CreateAutomaton2<T>(Func<int, T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {

            Func<int, string, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), s);

            MSOFormula<T> phi = new MSOFalse<T>();

            for (int index = 0; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = pred(index, "var");
                phi1 = new MSOExistsFo<T>("var", phi1);
                phi = new MSOOr<T>(phi, phi1);
            }

            phi = new MSOExistsFo<T>("var", phi);

            var aut = phi.GetAutomaton(Z);

            return aut;
        }

        Automaton<T> CreateAutomaton3<T>(Func<int, T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {

            Func<int, string, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), s);

            MSOFormula<T> phi = new MSOTrue<T>();

            // x1<x2<x3<x4...
            for (int index = 1; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = new MSOLt<T>("x" + (index - 1), "x" + index);
                phi = new MSOAnd<T>(phi, phi1);
            }

            // bi(xi)
            for (int index = 0; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = pred(index, "x" + index);
                phi = new MSOAnd<T>(phi, phi1);
            }

            // exists forall...
            for (int index = 0; index < bitWidth; index++)
            {
                if (index % 2 == 0)
                    phi = new MSOExistsFo<T>("x" + index, phi);
                else
                    phi = new MSOForallFo<T>("x" + index, phi);
            }

            Assert.IsTrue(phi.IsWellFormedFormula());
            var aut = phi.GetAutomaton(Z);
            return aut;
        }

        #endregion

        [TestMethod]
        public void TestLarge()
        {
            var max = 10;
            for (int i = 1; i < max; i++)
                TestMintermExplosion(i, true);
        }
    }

    [TestClass]
    public class WS1S_BooleanAlgebraZ3_Tests
    {
        [TestMethod]
        public void BooleanAlgebraZ3_test1()
        {
            var ctx = new Context();
            var sort = ctx.IntSort;
            var solver = new BooleanAlgebraZ3(ctx, sort);
            var alg = new BDDAlgebra<BoolExpr>(solver);
            var pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), "x");
            MSOFormula<BoolExpr> phi = new MSOExistsFo<BoolExpr>("x", pred);
            var pred_aut = pred.GetAutomaton(alg, "x");
            pred_aut.ShowGraph("pred_aut");
        }

        [TestMethod]
        public void BooleanAlgebraZ3_test2()
        {
            var ctx = new Context();
            var sort = ctx.IntSort;
            var solver = new BooleanAlgebraZ3(ctx, sort);
            var alg = new BDDAlgebra<BoolExpr>(solver);
            var xLTy = new WS1SLt<BoolExpr>("x", "y");
            var xLTzLTy = new WS1SAnd<BoolExpr>(new WS1SLt<BoolExpr>("x", "z"), new WS1SLt<BoolExpr>("z", "y"));
            var Ez = new WS1SExists<BoolExpr>("z", xLTzLTy);
            var notEz = new WS1SNot<BoolExpr>(Ez);
            var xSyDef = new WS1SAnd<BoolExpr>(xLTy, notEz);
            var aut_xSyDef = xSyDef.GetAutomaton(alg, "x", "y");
            var aut_xLTzLTy = xLTzLTy.GetAutomaton(alg, "x", "y", "z");
            var aut_Ez = Ez.GetAutomaton(alg, "x", "y");
            var aut_notEz = notEz.GetAutomaton(alg, "x", "y");
            var aut_xLTy = xLTy.GetAutomaton(alg, "x", "y");

            aut_xSyDef.ShowGraph("aut_xSyDEf");
            aut_xLTzLTy.ShowGraph("aut_xLTzLTy");
            aut_Ez.ShowGraph("aut_Ez");
            aut_notEz.ShowGraph("aut_notEz");

            var xSyPrim = new WS1SSuccN<BoolExpr>("x", "y", 1);
            var aut_xSyPrim = xSyPrim.GetAutomaton(alg, "x", "y");
            var equiv = aut_xSyPrim.IsEquivalentWith(aut_xSyDef, alg);
            Assert.IsTrue(equiv);
        }
    }
}

