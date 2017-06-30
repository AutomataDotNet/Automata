using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Diagnostics;

using Microsoft.Automata.MSO;
using Microsoft.Automata;
using Microsoft.Automata.BooleanAlgebras;
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
        public void TestMSO_NotLt()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var x = new Variable("x", true);
            var y = new Variable("y", true);
            MSOFormula<BDD> not_xLTy = new MSONot<BDD>(new MSOLt<BDD>(x,y));
            MSOFormula<BDD> xEQy = new MSOEq<BDD>(x, y);
            var xGTy = new MSOLt<BDD>(y, x);
            var xGEy = new MSOOr<BDD>(xEQy, xGTy);
            var aut_not_xLTy = not_xLTy.GetAutomaton(ca);
            var aut_xGEy = xGEy.GetAutomaton(ca);
            var c_aut_xLTy = (new MSOLt<BDD>(x,y)).GetAutomaton(ca).Complement().Determinize().Minimize();
            //c_aut_xLTy = c_aut_xLTy.Intersect(aut_fo, ca).Determinize(ca).Minimize(ca); //*
            //aut_not_xLTy.ShowGraph("aut_not_xLTy");
            //aut_xGEy.ShowGraph("aut_xGEy");
            //c_aut_xLTy.ShowGraph("c_aut_xLTy");
            var equiv1 = aut_not_xLTy.IsEquivalentWith(aut_xGEy);
            //var equiv2 = aut_not_xLTy.IsEquivalentWith(c_aut_xLTy, ca);
            Assert.IsTrue(equiv1);
            //Assert.IsTrue(equiv2);
        }


        [TestMethod]
        public void TestMSO_Equal()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var x = new Variable("x", false);
            var y = new Variable("y", false);
            var fo_x = new MSOIsSingleton<BDD>(x) ;
            var fo_y = new MSOIsSingleton<BDD>(y);
            MSOFormula<BDD> fo = new MSOAnd<BDD>(fo_x, fo_y);
            MSOFormula<BDD> xSy = new MSOSubset<BDD>(x, y);
            MSOFormula<BDD> ySx = new MSOSubset<BDD>(y, x);
            MSOFormula<BDD> yEQx = new MSOAnd<BDD>(xSy, ySx);
            yEQx = new MSOAnd<BDD>(yEQx, fo);
            var aut_yEQx = yEQx.GetAutomaton(ca);
            //aut_yEQx.ShowGraph("aut_yEQx");
        }

        [TestMethod]
        public void TestMSO_Member()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var x = new Variable("x", false);
            var y = new Variable("y", false);
            var fo_x = new MSOIsSingleton<BDD>(x);
            MSOFormula<BDD> xSy = new MSOSubset<BDD>(x, y);
            var mem = new MSOAnd<BDD>(xSy, fo_x);
            var aut_mem = mem.GetAutomaton(ca);
            //aut_mem.ShowGraph("aut_mem");
        }

        [TestMethod]
        public void TestWS1S_Label()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var x = new Variable("X", false);
            var pred = new MSOPredicate<BDD>(solver.MkCharConstraint('c'), x);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var lab = pred & new MSOIsSingleton<BDD>(x);
            var lab_aut = lab.GetAutomaton(ca); 
            //lab_aut.ShowGraph("lab_aut");
        }

        [TestMethod]
        public void TestWS1S_Forall_x_Exists_y_x_lt_y() 
        {
            var triv = new TrivialBooleanAlgebra();
            var ca = new BDDAlgebra<bool>(triv); 
            var x = new Variable("x", true);
            var y = new Variable("y", true);
            var x_lt_y = new MSOLt<bool>(x, y);
            var aut_x_lt_y = x_lt_y.GetAutomaton(ca);
            //aut_x_lt_y.ShowGraph("aut_x_lt_y");
            var psi4 = new MSOForall<bool>(x, new MSOExists<bool>(y, (x_lt_y)));
            var aut = psi4.GetAutomaton(ca);
            //accepts only the empty word
            Assert.IsTrue(aut.StateCount == 1 && aut.IsFinalState(aut.InitialState) && aut.MoveCount == 0);
        }


        [TestMethod]
        public void TestWS1S_NotLabel()  
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            //var x1 = new Variable("x1", false);
            var x = new Variable("x",false);
            var pred = new MSOPredicate<BDD>(solver.MkCharConstraint( 'c'), x);
            var fo_x = new MSOIsSingleton<BDD>(x);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var lab = new MSOAnd<BDD>(pred, fo_x);
            MSOFormula<BDD> not_lab = new MSONot<BDD>(lab);
            var not_lab_actual = new MSOAnd<BDD>(not_lab, fo_x);
            var aut_not_lab = not_lab_actual.GetAutomaton(ca);
            var aut_not_lab_prelim = not_lab.GetAutomaton(ca);
            var c_aut_lab = lab.GetAutomaton(ca).Complement().Minimize();
            //c_aut_lab.ShowGraph("c_aut_lab");
            //aut_not_lab.ShowGraph("aut_not_lab");
            //aut_not_lab_prelim.ShowGraph("aut_not_lab_prelim");
            //TBD: equivalence
        }

        [TestMethod]
        public void TestWS1S_SuccDef_GetAutomaton()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var x = new Variable("x", true);
            var y = new Variable("y", true);
            var z = new Variable("z", true);
            var xLTy = new MSOLt<BDD>(x, y);
            var xLTzLTy = new MSOAnd<BDD>(new MSOLt<BDD>(x, z),new MSOLt<BDD>(z, y));
            var Ez = new MSOExists<BDD>(z, xLTzLTy);
            var notEz = new MSONot<BDD>(Ez);
            var xSyDef = new MSOAnd<BDD>(xLTy, notEz);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var aut_xSyDef = xSyDef.GetAutomaton(ca);
            var aut_xLTzLTy = xLTzLTy.GetAutomaton(ca);
            var aut_Ez = Ez.GetAutomaton(ca);
            var aut_notEz = notEz.GetAutomaton(ca);
            var aut_xLTy = xLTy.GetAutomaton(ca);

            //aut_xSyDef.ShowGraph("aut_xSyDEf");
            //aut_xLTzLTy.ShowGraph("aut_xLTzLTy");
            //aut_Ez.ShowGraph("aut_Ez");
            //aut_notEz.ShowGraph("aut_notEz");

            var xSyPrim = new MSOSuccN<BDD>(x,y, 1);
            var aut_xSyPrim = xSyPrim.GetAutomaton(ca);
            var equiv = aut_xSyPrim.IsEquivalentWith(aut_xSyDef);
            Assert.IsTrue(equiv);
        }

        [TestMethod]
        public void TestWS1S_SuccDef_GetAutomatonBDD()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            var nrOfLabelBits = (int)BitWidth.BV7;
            var x = new Variable("x",true);
            var y = new Variable("y",true);
            var z = new Variable("z",true);
            var xLTy = new MSOLt<BDD>(x, y);
            var xLTzLTy = new MSOAnd<BDD>(new MSOLt<BDD>(x, z), new MSOLt<BDD>(z, y));
            var Ez = new MSOExists<BDD>(z, xLTzLTy);
            var notEz = new MSONot<BDD>(Ez);
            var xSyDef = new MSOAnd<BDD>(xLTy, notEz);

            var aut_xSyDef = xSyDef.GetAutomaton(solver);
            var aut_xLTzLTy = xLTzLTy.GetAutomaton(solver);
            var aut_Ez = Ez.GetAutomaton(solver).Determinize().Minimize();
            var aut_notEz = notEz.GetAutomaton(solver);
            var aut_xLTy = xLTy.GetAutomaton(solver);

            //aut_xSyDef.ShowGraph("aut_xSyDEf");
            //aut_xLTzLTy.ShowGraph("aut_xLTzLTy");
            //aut_Ez.ShowGraph("aut_Ez");
            //aut_notEz.ShowGraph("aut_notEz");

            var xSyPrim = new MSOSuccN<BDD>(x,y, 1);
            var aut_xSyPrim = xSyPrim.GetAutomaton(solver);
            var equiv = aut_xSyPrim.IsEquivalentWith(aut_xSyDef);
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


        //Automaton<T> Restrict<T>(Automaton<IMonadicPredicate<BDD, T>> autom)
        //{
        //    List<Move<T>> moves = new List<Move<T>>();
        //    foreach (var move in autom.GetMoves())
        //        moves.Add(new Move<T>(move.SourceState, move.TargetState, move.Label.ProjectSecond()));
        //    var res = Automaton<T>.Create(autom.InitialState, autom.GetFinalStates(), moves);
        //    return res;
        //}

        public void TestWS1S_UseOfCharRangePreds<T>(IBooleanAlgebra<T> solver, T isDigit, T isWordLetter, IRegexConverter<T> regexConverter)
        {
            var ca = new CartesianAlgebraBDD<T>(solver);
            var x = new Variable("x",false);
            var y = new Variable("y",false);
            var z = new Variable("z",false);
            var X = new Variable("X",false);
            //there are at least two distinct positions x and y
            var xy = new MSOAnd<T>(new MSOAnd<T>(new MSONot<T>(new MSOEq<T>(x, y)), new MSOIsSingleton<T>(x)), new MSOIsSingleton<T>(y));
            //there is a set X containing x and y and all positions z in X have characters that satisfy isWordLetter
            var phi = new MSOExists<T>(X, new MSOAnd<T>(
                new MSOAnd<T>(new MSOSubset<T>(x, X), new MSOSubset<T>(y, X)),
                new MSONot<T>(new MSOExists<T>(z, new MSONot<T>(
                    new MSOOr<T>(new MSONot<T>(new MSOAnd<T>(new MSOIsSingleton<T>(z), new MSOSubset<T>(z, X))),
                                    new MSOPredicate<T>(isWordLetter, z)))))));

            var psi2 = new MSOAnd<T>(xy, phi);
            var atLeast2wEE = new MSOExists<T>(x, new MSOExists<T>(y, psi2));
            var psi1 = new MSOAnd<T>(new MSOIsSingleton<T>(x), new MSOPredicate<T>(isDigit, x));
            var aut_psi1 = psi1.GetAutomaton(ca);
            //aut_psi1.ShowGraph("SFA(psi1)");
            var atLeast1d = new MSOExists<T>(x, psi1);
            var psi = new MSOAnd<T>(atLeast2wEE, atLeast1d);
            var aut1 = psi.GetAutomaton(ca);
            //var aut_atLeast1d = atLeast1d.GetAutomaton(solver); 

            var aut2 = regexConverter.Convert(@"\w.*\w", System.Text.RegularExpressions.RegexOptions.Singleline).Intersect(regexConverter.Convert(@"\d"));

            //aut1.ShowGraph("aut1");
            //aut2.ShowGraph("aut2");

            bool equiv = aut2.IsEquivalentWith(BasicAutomata.Restrict(aut1));
            Assert.IsTrue(equiv);

            //solver.ShowGraph(aut_atLeast1d, "aut_atLeast1d");

            var aut_psi2 = psi2.GetAutomaton(ca);
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
            //var nrOfLabelBits = (int)BitWidth.BV7;
            var isDigit = solver.MkCharSetFromRegexCharClass(@"\d");
            var isLetter = solver.MkCharSetFromRegexCharClass(@"(c|C)");
            var x = new Variable("x",false);
            var y = new Variable("y",false);
            var z = new Variable("z",false);
            var X = new Variable("X",false);
            //there are at least two distinct positions x and y
            var xy = new MSOAnd<BDD>(new MSONot<BDD>(new MSOEq<BDD>(x,y)), new MSOAnd<BDD>(new MSOIsSingleton<BDD>(x),new MSOIsSingleton<BDD>(y)));
            //there is a set X containing x and y and all positions z in X have characters that satisfy isWordLetter
            var x_sub_X = new MSOSubset<BDD>(x, X);
            var y_sub_X = new MSOSubset<BDD>(y, X);
            var z_sub_X = new MSOSubset<BDD>(z, X);
            var isletter_z = new MSOPredicate<BDD>(isLetter, z);
            var psi = new MSOExists<BDD>(X, (x_sub_X & y_sub_X & ~(new MSOExists<BDD>(z, ~((~((new MSOIsSingleton<BDD>(z)) & z_sub_X)) | isletter_z)))));

            var atLeast2w = xy&psi;
            var atLeast2wEE = new MSOExists<BDD>(x, (new MSOExists<BDD>(y, atLeast2w)));
            var autBDD = atLeast2w.GetAutomaton(solver);
            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var autPROD = atLeast2w.GetAutomaton(ca);
            //autBDD.ShowGraph("autBDD");
            //autPROD.ShowGraph("autPROD");
            var aut_atLeast2wEE1 = BasicAutomata.Restrict(atLeast2wEE.GetAutomaton(ca));
            var aut_atLeast2wEE2 = atLeast2wEE.GetAutomaton(solver);
            //aut_atLeast2wEE1.ShowGraph("aut_atLeast2wEE1");
            //aut_atLeast2wEE2.ShowGraph("aut_atLeast2wEE2");
            Assert.IsTrue(aut_atLeast2wEE1.IsEquivalentWith(aut_atLeast2wEE2));
        }

        [TestMethod]
        public void TestMSO_FirstLastZ3() 
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            var x = new Variable("x", true);
            MSOFormula<BDD> phi = new MSOExists<BDD>(x, new MSOAnd<BDD>(new MSOeqN<BDD>(x,0), new MSOLast<BDD>(x)));

            var aut = phi.GetAutomaton(solver);

            var res = solver.Convert("^[\0-\uFFFF]$");
            //solver.ShowGraph(res,"res");
            //solver.SaveAsDgml(res, "res");
            var eq = aut.IsEquivalentWith(res);
            Assert.IsTrue(eq);
        }

        [TestMethod]
        public void TestMSO_Le()
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            //var phi = new MSOTrue();
            var x = new Variable("x", true);
            var y = new Variable("y", true);
            var z = new Variable("z", true);
            MSOFormula<BDD> phi = new MSOExists<BDD>(z, new MSOExists<BDD>(x, new MSOExists<BDD>(y,
                new MSOAnd<BDD>(
                    new MSOLt<BDD>(x, y),
                    new MSOLt<BDD>(y, z) 
                    )
            ))); 


            var aut = phi.GetAutomaton(solver);

            var aut2 = solver.RegexConverter.Convert(".{3,}", System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.IsTrue(aut.IsEquivalentWith(aut2));

            //solver.ShowGraph(aut, "SFA");
        }

        static Variable V1(string name)
        {
            return new Variable(name, true);
        }

        static Variable V2(string name)
        {
            return new Variable(name, false);
        }

        [TestMethod]
        public void TestMSO_Le1()
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            MSOFormula<BDD> phi = new MSOExists<BDD>(V1("x"), new MSOExists<BDD>(V1("y"),
                    new MSOLt<BDD>(V1("x"), V1("y"))));


            var aut = phi.GetAutomaton(solver);

            var aut2 = solver.RegexConverter.Convert(".{2,}", System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.IsTrue(aut.IsEquivalentWith(aut2));

            //new MSOLt<BDD>("x", "y").GetAutomaton(solver,"x","y").ShowGraph("");
        }

        [TestMethod]
        public void TestMSO_Eq()
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            //var phi = new MSOTrue();
            MSOFormula<BDD> phi = new MSOExists<BDD>(V1("x"), new MSOExists<BDD>(V1("y"), new MSONot<BDD>(new MSOEq<BDD>(V1("x"), V1("y")))));

            var aut = phi.GetAutomaton(solver);

            var aut2 = solver.RegexConverter.Convert(".{2,}", System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.IsTrue(aut.IsEquivalentWith(aut2));
        }

        [TestMethod]
        public void TestMSO_Forall()
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            var x = new Variable("x", true);
            MSOFormula<BDD> phi = new MSOForall<BDD>(x, new MSOPredicate<BDD>(solver.MkCharConstraint('c',true), x));

            var aut = phi.GetAutomaton(solver);
            //aut.ShowGraph("aut");
            for (int i = 0; i < 10; i++)
            {
                TestContext.WriteLine(solver.GenerateMember(aut));
            }
            var aut2 = solver.RegexConverter.Convert("^(c|C)*$");
            //aut2.ShowGraph("aut2");
            Assert.IsTrue(aut2.IsEquivalentWith(aut));
        }

        [TestMethod]
        public void TestMSO_Pred()
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            var x = new Variable("x", true);
            var pred = new MSOPredicate<BDD>(solver.MkCharConstraint( 'c'), x);
            MSOFormula<BDD> phi = new MSOExists<BDD>(x, pred);

            var ca = new CartesianAlgebraBDD<BDD>(solver);
            var pred_aut = pred.GetAutomaton(ca);
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
            Assert.IsTrue(aut2.IsEquivalentWith(aut), "automata not equialent");
        }

        [TestMethod]
        public void TestMSO_FirstC()
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            //var phi = new MSOTrue();
            var x = new Variable("x", true);
            MSOFormula<BDD> phi = new MSOExists<BDD>(x, 
                new MSOAnd<BDD>(new MSOPredicate<BDD>(solver.MkCharConstraint('C'), x), new MSOeqN<BDD>(x,0)));

            var aut = phi.GetAutomaton(solver);
            var aut2 = solver.RegexConverter.Convert("^C");
            Assert.IsTrue(aut2.IsEquivalentWith(aut));
        }

        [TestMethod]
        public void TestMSO_Neg()
        {
            var solver = new CharSetSolver(BitWidth.BV7);
            //var phi = new MSOTrue();
            MSOFormula<BDD> phi = new MSONot<BDD>(new MSOExists<BDD>(V1("x"), new MSOPredicate<BDD>(solver.MkCharConstraint( 'c'), V1("x"))));

            var aut = phi.GetAutomaton(solver);
            for (int i = 0; i < 10; i++)
            {
                var s = solver.GenerateMember(aut);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, "^[^c]*$"));
            }
            var aut2 = solver.RegexConverter.Convert("^[^c]*$");
            Assert.IsTrue(aut2.IsEquivalentWith(aut));
        }

        [TestMethod]
        public void TestMSO_Or()
        {
            var solver = new CharSetSolver(BitWidth.BV32);
            MSOFormula<BDD> phi = new MSOForall<BDD>(V1("x"),
                    new MSOOr<BDD>(
                        new MSOPredicate<BDD>(solver.MkCharConstraint( 'c'), V1("x")),
                        new MSOPredicate<BDD>(solver.MkCharConstraint( 'a'), V1("x"))
                    )
                );

            var aut = phi.GetAutomaton(solver);
            for (int i = 0; i < 10; i++)
            {
                var s = solver.GenerateMember(aut);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, "^[ac]*$"));
            }
            var aut2 = solver.RegexConverter.Convert("^[ac]*$");
            Assert.IsTrue(aut2.IsEquivalentWith(aut));
        }

        [TestMethod]
        public void TestMSO_Succ()
        {
            var solver = new CharSetSolver(BitWidth.BV32);
            var x = new Variable("x", true);
            var y = new Variable("y", true);
            MSOFormula<BDD> phi = new MSOForall<BDD>(x,
                    new MSOImplies<BDD>(
                        new MSOPredicate<BDD>(solver.MkCharConstraint( 'c'), x),
                        new MSOExists<BDD>(y,
                            new MSOAnd<BDD>(
                                new MSOSuccN<BDD>(x, y, 1),
                                new MSOPredicate<BDD>(solver.MkCharConstraint('a'), y)
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
            Assert.IsTrue(aut2.IsEquivalentWith(aut));
        }


        static MSOForall<BoolExpr> Forall(Variable x, MSOFormula<BoolExpr> psi)
        {
            return new MSOForall<BoolExpr>(x, psi);
        }

        static MSOExists<BoolExpr> Exists(Variable x, MSOFormula<BoolExpr> psi)
        {
            return new MSOExists<BoolExpr>(x, psi);
        }

        static MSOImplies<BoolExpr> Implies(MSOFormula<BoolExpr> lhs, MSOFormula<BoolExpr> rhs)
        {
            return new MSOImplies<BoolExpr>(lhs, rhs);
        }

        static MSOAnd<BoolExpr> And(MSOFormula<BoolExpr> psi, MSOFormula<BoolExpr> phi)
        {
            return new MSOAnd<BoolExpr>(psi, phi);
        }

        static MSOOr<BoolExpr> Or(MSOFormula<BoolExpr> psi, MSOFormula<BoolExpr> phi)
        {
            return new MSOOr<BoolExpr>(psi, phi);
        }

        static MSOSuccN<BoolExpr> Succ(Variable x, Variable y)
        {
            return new MSOSuccN<BoolExpr>(x, y, 1);
        }

        [TestMethod]
        public void MSO_Succ_Z3A()
        {
            var z3Context = new Context();
            var S = z3Context.IntSort;
            var solver = new Z3BoolAlg(z3Context, S);
            Func<int, BoolExpr> IsPos = (i => z3Context.MkEq(solver.x, z3Context.MkInt(i)));
            var x = new Variable("x", true);
            var y = new Variable("y", true);
            var x_is_0 = new MSOPredicate<BoolExpr>(IsPos(0), x);
            var y_is_1 = new MSOPredicate<BoolExpr>(IsPos(1), y);
            //every 0 is immediately followed by a 1
            MSOFormula<BoolExpr> phi = Forall(x, Implies(x_is_0, Exists(y, And(Succ(x, y), y_is_1))));

            //var ca = new CartesianAlgebraBDD<BoolExpr>(solver);
            var aut = phi.GetAutomaton(solver);
            //aut.ShowGraph();
            //Automaton<BoolExpr> aut1 = Automaton<BoolExpr>.ProjectSecond<BDD>(aut);

            var expected_automaton = Automaton<BoolExpr>.Create(solver, 0, new int[] { 0 },
                new Move<BoolExpr>[]{
                Move<BoolExpr>.Create(0,0, z3Context.MkNot(IsPos(0))),
                Move<BoolExpr>.Create(0,1, IsPos(0)),
                 Move<BoolExpr>.Create(1,0, IsPos(1))});

            bool equiv = aut.IsEquivalentWith(expected_automaton);
            //Assert.IsTrue(equiv, "the automata must be equivalent");
        }

        [TestMethod]
        public void Minimize_NFA_Z3A()
        {
            var z3Context = new Context();
            var S = z3Context.IntSort;
            var solver = new Z3BoolAlg(z3Context, S);
            var a = z3Context.MkEq(solver.x, z3Context.MkNumeral(0, S));
            var b = z3Context.MkNot(a);

            var sfa = Automaton<BoolExpr>.Create(solver, 0, new int[] { 3, 4, 5 },
                new Move<BoolExpr>[]{
                Move<BoolExpr>.Create(0, 1, a),
                Move<BoolExpr>.Create(0, 2, b),
                Move<BoolExpr>.Create(1, 4, a),
                Move<BoolExpr>.Create(1, 5, a), //<-- nondeterminism
                Move<BoolExpr>.Create(1, 3, b),
                Move<BoolExpr>.Create(2, 4, b),
                Move<BoolExpr>.Create(2, 5, a),
                //Move<BoolExpr>.Create(3, 6, a),
                Move<BoolExpr>.Create(3, 4, b),
                //Move<BoolExpr>.Create(4, 6, a),
                Move<BoolExpr>.Create(4, 3, b),
                Move<BoolExpr>.Create(5, 5, a),
                //Move<BoolExpr>.Create(5, 6, b),
                //Move<BoolExpr>.Create(6, 6, solver.True)
                });

            var sfa_min = sfa.Minimize();

            var sfa_det = sfa_min.Determinize();

            var sfa_det_min = sfa_det.Minimize();

            bool equiv1 = sfa.IsEquivalentWith(sfa_min);
            bool equiv2 = sfa.IsEquivalentWith(sfa_det);


            Assert.IsTrue(equiv1);
            Assert.IsTrue(equiv2);

            //pretend that sfa is deterministic
            sfa.isDeterministic = true;

            //only the smaller half of a split set is pushed 
            var sfa_min2 = sfa.Minimize();

            //the result is incorrect
            bool equiv3 = sfa_min.IsEquivalentWith(sfa_min2);

            Assert.IsFalse(equiv3);
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
                        phi = new MSOAnd<BDD>(phi, new MSOLt<BDD>(V1("x" + i), V1("x" + (i + 1))));
                        phi = new MSOAnd<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint( 'a'), V1("x" + i)));
                    }
                    phi = new MSOAnd<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint( 'a'), V1("x" + vars)));
                    phi = new MSOOr<BDD>(phi,
                        new MSOExists<BDD>(V1("y"),
                        new MSOPredicate<BDD>(solver.MkCharConstraint( 'c'), V1("y"))));
                    for (int i = vars; i >= 1; i--)
                    {
                        phi = new MSOExists<BDD>(V1("x" + i), phi);
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

        //[TestMethod]
        //public void TestCountNoProduct()
        //{
        //    int maxvars = 13;
        //    var solver = new CharSetSolver(BitWidth.BV7);
        //    Stopwatch sw = new Stopwatch();
        //    var tries = 1;
        //    var times = new long[maxvars - 1];
        //    for (int k = 0; k < tries; k++)
        //    {
        //        for (int vars = 8; vars <= maxvars; vars++)
        //        {
        //            MSOFormula phi = new MSOTrue();
        //            for (int i = 1; i < vars; i++)
        //            {
        //                phi = new MSOAnd(phi, new MSOLt("x" + i, "x" + (i + 1)));
        //                phi = new MSOAnd(phi, new MSOPredicate(solver.MkCharConstraint( 'a'), "x" + i));
        //                phi = new MSOOr(phi, new MSOPredicate(solver.MkCharConstraint( 'k'), "x" + i));
        //            }
        //            phi = new MSOAnd(phi, new MSOPredicate(solver.MkCharConstraint( 'a'), "x" + vars));
        //            phi = new MSOOr(phi,
        //                new MSOExistsFo("y",
        //                new MSOPredicate(solver.MkCharConstraint( 'c'), "y")));
        //            for (int i = vars; i >= 1; i--)
        //            {
        //                phi = new MSOExistsFo("x" + i, phi);
        //            }

        //            sw.Restart();
        //            //var aut1 = phi.GetAutomaton(new CartesianAlgebraBDD<BDD>(solver));
        //            var aut1 = phi.GetAutomaton(solver);
        //            sw.Stop();
        //            times[vars - 2] += sw.ElapsedMilliseconds;
        //            //Console.WriteLine("States {0} Trans {1}",aut1.StateCount,aut1.MoveCount);
        //            if (k == tries - 1)
        //            {
        //                TestContext.WriteLine(string.Format("{0} variables; {1} ms", vars, times[vars - 2] / tries));
        //            }
        //            //solver.ShowGraph(aut1, "a"+vars);
        //        }
        //    }
        //}

        [TestMethod]
        public void TestCountNoProduct2()
        {
            int maxvars = 13;
            var solver = new CharSetSolver(BitWidth.BV7);
            Stopwatch sw = new Stopwatch();
            var tries = 1;
            var times = new long[maxvars - 1];
            var x = new Variable("x", true);
            var y = new Variable("y", true);
            for (int k = 0; k < tries; k++)
            {
                for (int vars = 8; vars <= maxvars; vars++)
                {
                    MSOFormula<BDD> phi = new MSOTrue<BDD>();
                    for (int i = 1; i < vars; i++)
                    {
                        phi = new MSOAnd<BDD>(phi, new MSOLt<BDD>(new Variable("x" + i,true), new Variable("x" + (i + 1), true)));
                        phi = new MSOAnd<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint( 'a'), new Variable("x" + i,true)));
                        phi = new MSOOr<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint( 'k'), new Variable("x" + i,true)));
                    }
                    phi = new MSOAnd<BDD>(phi, new MSOPredicate<BDD>(solver.MkCharConstraint( 'a'), new Variable("x" + vars, true)));
                    phi = new MSOOr<BDD>(phi,
                        new MSOExists<BDD>(y,
                        new MSOPredicate<BDD>(solver.MkCharConstraint( 'c'), y)));
                    for (int i = vars; i >= 1; i--)
                    {
                        phi = new MSOExists<BDD>(new Variable("x" + i,true), phi);
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
            var x = new Variable("x", true);
            var y = new Variable("y", true);
            var psi = new MSOImplies<Expr>(
                        new MSOPredicate<Expr>(isNeg, x),
                        new MSOExists<Expr>(y,
                            new MSOAnd<Expr>(
                                new MSOSuccN<Expr>(x, y, 1),
                                new MSOPredicate<Expr>(isPos, y)
                            )
                        )
                    );
            //all negative labels are immediately followed by a positive label
            MSOFormula<Expr> phi = new MSOForall<Expr>(x, psi);
            var ca = new CartesianAlgebraBDD<Expr>(solver);
            var aut_psi = psi.GetAutomaton(ca).Determinize().Minimize();
            var aut_phi = phi.GetAutomaton(solver).Determinize().Minimize();
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
            MSOFormula<Expr> phi = new MSOExists<Expr>(V1("x"),
                   new MSOPredicate<Expr>(solver.MkLe(solver.MkVar(0, solver.IntSort), solver.MkInt(0)), V1("x"))
                );

            var aut = phi.GetAutomaton(solver);
        }

        [TestMethod]
        public void TestLarge()
        {
            var max = 5;
            for (int i = 4; i < max; i++)
                TestMintermExplosion(i, true);
        }

        #region TestLarge helpers

        void TestMintermExplosion(int bitWidth, bool useBDD = false)
        {
            TestContext.WriteLine("----------------");
            TestContext.WriteLine(bitWidth.ToString());

            if (useBDD)
            {
                //var S = new CharSetSolver(BitWidth.BV32);
                var S = new BDDAlgebra();

                TestContext.WriteLine("BDD");
                int t = System.Environment.TickCount;
                var aut1 = CreateAutomaton1<BDD>(S.MkBitTrue, bitWidth, S);
                var aut2 = CreateAutomaton2<BDD>(S.MkBitTrue, bitWidth, S);
                var aut3 = CreateAutomaton3<BDD>(S.MkBitTrue, bitWidth, S);
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
            Func<int, Variable, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), s);

            MSOFormula<T> phi = new MSOFalse<T>();

            for (int index = 0; index < bitWidth; index++)
            {
                var phi1 = pred(index, V1("var"));
                phi = new MSOOr<T>(phi, phi1);
            }

            phi = new MSOExists<T>(V1("var"), phi);

            var aut = phi.GetAutomaton(Z);

            //aut.ShowGraph("aut");

            return aut;
        }

        Automaton<T> CreateAutomaton2<T>(Func<int, T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {

            Func<int, Variable, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), s);

            MSOFormula<T> phi = new MSOFalse<T>();

            for (int index = 0; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = pred(index, V1("var"));
                phi1 = new MSOExists<T>(V1("var"), phi1);
                phi = new MSOOr<T>(phi, phi1);
            }

            phi = new MSOExists<T>(V1("var"), phi);

            var aut = phi.GetAutomaton(Z);

            return aut;
        }

        Automaton<T> CreateAutomaton3<T>(Func<int, T> f, int bitWidth, IBooleanAlgebra<T> Z)
        {

            Func<int, Variable, MSOPredicate<T>> pred = (i, s) => new MSOPredicate<T>(f(i), s);

            MSOFormula<T> phi = new MSOTrue<T>();

            // x1<x2<x3<x4...
            for (int index = 1; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = new MSOLt<T>(V1("x" + (index - 1)), V1("x" + index));
                phi = new MSOAnd<T>(phi, phi1);
            }

            // bi(xi)
            for (int index = 0; index < bitWidth; index++)
            {
                MSOFormula<T> phi1 = pred(index, V1("x" + index));
                phi = new MSOAnd<T>(phi, phi1);
            }

            // exists forall...
            for (int index = 0; index < bitWidth; index++)
            {
                if (index % 2 == 0)
                    phi = new MSOExists<T>(V1("x" + index), phi);
                else
                    phi = new MSOForall<T>(V1("x" + index), phi);
            }
            var aut = phi.GetAutomaton(Z);
            return aut;
        }

        #endregion
    }

    [TestClass]
    public class WS1S_BooleanAlgebraZ3_Tests
    {
        [TestMethod]
        public void BooleanAlgebraZ3_test1()
        {
            var ctx = new Context();
            var sort = ctx.IntSort;
            var solver = new Z3BoolAlg(ctx, sort);
            var alg = new BDDAlgebra<BoolExpr>(solver);
            var x = new Variable("x", true);
            var pred = new MSOPredicate<BoolExpr>(ctx.MkEq(solver.x, ctx.MkNumeral(42, sort)), x);
            MSOFormula<BoolExpr> phi = new MSOExists<BoolExpr>(x, pred);
            var pred_aut = pred.GetAutomaton(alg);
            //pred_aut.ShowGraph("pred_aut");
        }

        [TestMethod]
        public void BooleanAlgebraZ3_test2()
        {
            var ctx = new Context();
            var sort = ctx.IntSort;
            var solver = new Z3BoolAlg(ctx, sort);
            var alg = new BDDAlgebra<BoolExpr>(solver);
            var x = new Variable("x",true);
            var y = new Variable("y",true);
            var z = new Variable("z",true);
            var xLTy = new MSOLt<BoolExpr>(x, y);
            var xLTzLTy = new MSOAnd<BoolExpr>((new MSOLt<BoolExpr>(x, z)),(new MSOLt<BoolExpr>(z, y)));
            var Ez = new MSOExists<BoolExpr>(z, xLTzLTy);
            var notEz = new MSONot<BoolExpr>(Ez);
            var xSyDef = new MSOAnd<BoolExpr>(xLTy, notEz);
            var aut_xSyDef = xSyDef.GetAutomaton(alg);
            var aut_xLTzLTy = xLTzLTy.GetAutomaton(alg);
            var aut_Ez = Ez.GetAutomaton(alg);
            var aut_notEz = notEz.GetAutomaton(alg);
            var aut_xLTy = xLTy.GetAutomaton(alg);

            //aut_xSyDef.ShowGraph("aut_xSyDEf");
            //aut_xLTzLTy.ShowGraph("aut_xLTzLTy");
            //aut_Ez.ShowGraph("aut_Ez");
            //aut_notEz.ShowGraph("aut_notEz");

            var xSyPrim = new MSOSuccN<BoolExpr>(x,y, 1);
            var aut_xSyPrim = xSyPrim.GetAutomaton(alg);
            var equiv = aut_xSyPrim.IsEquivalentWith(aut_xSyDef);
            Assert.IsTrue(equiv);
        }
    }
}

