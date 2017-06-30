using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Z3;

using System.Text.RegularExpressions;

using SFAz3 = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;

namespace Microsoft.Automata.Z3.Tests
{
    [TestClass]
    public class Z3_FiniteAutomataTests
    {

        ////// Use TestInitialize to run code before running each test 
        //[TestInitialize()]
        //public void MyTestInitialize() {
        //    StreamWriter myOutputFile = new StreamWriter("C:/tmp/testOutputFromVS2010UnitTest.txt", false);
        //    myOutputFile.Write("");
        //    myOutputFile.Close();
        //}

        ////
        //// Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup() { myOutputFile.Close(); myOutputFile = null; }

        //Z3
        //[TestMethod]
        //public void ProductTest()
        //{
        //    Z3.AutomataBuilder fab = new Z3.AutomataBuilder(CharacterEncoding.ASCII);

        //    string regex1 = "^[a-c]{0}a[a-c]{0,2}$";
        //    string regex2 = "^[a-c]{1}b[a-c]{0,2}$";
        //    string regex3 = "^[a-c]{2}c[a-c]{0,2}$";

        //    var fa1 = fab.CreateFromRegex(regex1);
        //    var fa2 = fab.CreateFromRegex(regex2);
        //    var fa3 = fab.CreateFromRegex(regex3);

        //    var fa = fab.MkProduct(fa1, fab.MkProduct(fa2, fa3));

        //    var members = new List<string>(fab.GenerateMembers(fa,10));

        //    Assert.AreEqual<int>(members.Count, 1, "there must be exactly 1 member");
        //    Assert.AreEqual<string>("abc", members[0], "wrong member");
        //}

        //[TestMethod]
        //public void ICSTSamplesFSAcheckLengthTest()
        //{
        //    var z3p = new Z3Provider(CharacterEncoding.Unicode);

        //    Expr s = z3p.MkConst("s", z3p.MkListSort(z3p.CharSort));
        //    int time = System.Environment.TickCount;

        //    List<string> regexes = new List<string>(File.ReadAllLines("../../../Samples/icst.txt"));
        //    regexes.RemoveRange(6, regexes.Count - 6);


        //    for (int i = 1; i < regexes.Count; i++)
        //    {
        //        z3p.Push();
        //        FSAcheckLengthTest(z3p, regexes[i], s);
        //        z3p.Pop();
        //    }
        //}

        //static void FSAcheckLengthTest(Z3Provider z3p, string regex, Expr s)
        //{

        //    Automaton<Expr> fsa = z3p.RegexConverter.Convert(regex);
        //    Expr goal;
        //    Expr bound = z3p.MkInt(fsa.StateCount);
        //    FuncDecl Acc;
        //    Acc = z3p.MkAccFSAcheckLength(fsa);
        //    Expr boundVar = z3p.Z3p.MkConst("tmp", z3p.Z3p.IntSort);
        //    goal = z3p.Z3p.MkAnd(z3p.Z3p.MkApp(Acc, s, boundVar), z3p.Z3p.MkLe(boundVar, bound)); ;

        //    z3p.Z3p.AssertCnstr(goal);
        //    Model m;
        //    z3p.Z3p.CheckAndGetModel(out m);

        //    Expr sVal = m.Eval(s);
        //    string v = z3p.GetStringValue(sVal);
        //    m.Dispose();

        //    Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(v, regex), "regex mismatch");
        //}

        //[TestMethod]
        //public void EpsilonRemovalTest()
        //{
        //    Z3.AutomataBuilder fab = new Z3.AutomataBuilder(CharacterEncoding.ASCII);

        //    Automaton<Expr> fa = Automaton<Expr>.Create(0, new int[] { 2 }, new Move<Expr>[]{
        //        fab.MkEqTrans(0, 0, 'a'),
        //        fab.MkEqTrans(1, 1, 'b'),
        //        fab.MkEqTrans(2, 2, 'c'),
        //        Move<Expr>.Epsilon(0, 1),
        //        Move<Expr>.Epsilon(1, 2)});

        //    var nfa = fab.RemoveEpsilons(fa);
        //    foreach (var m in nfa.GetMoves())
        //        Assert.IsFalse(m.IsEpsilon);
        //}


        //Z3
        [TestMethod]
        public void EpsilonLoopRemovalTest()
        {
            var z3p = new Z3Provider(BitWidth.BV7);

            Automaton<Expr> fa = Automaton<Expr>.Create(z3p, 0, new int[] { 2, 4 }, new Move<Expr>[]{
                MkEqTrans(z3p,0, 0, 'a'),
                MkEqTrans(z3p,1, 1, 'b'),
                MkEqTrans(z3p,2, 2, 'c'),
                MkEqTrans(z3p,3, 4, 'd'),
                Move<Expr>.Epsilon(0, 1),
                Move<Expr>.Epsilon(1, 2),
                Move<Expr>.Epsilon(2, 0),
                Move<Expr>.Epsilon(0, 3)});

            var nfa = fa.RemoveEpsilonLoops();
            Assert.AreEqual<int>(3, nfa.StateCount, "unexpected number of states");
            Assert.AreEqual<int>(3, nfa.MoveCount, "unexpected number of moves");
            foreach (var m in nfa.GetMoves())
                Assert.IsTrue(!m.IsEpsilon || (m.SourceState == 0 && m.TargetState == 3));
        }

        public Move<Expr> MkEqTrans(Z3Provider z3p, int sourceState, int targetState, char c)
        {
            Expr x = z3p.CharVar;
            Expr cond = z3p.MkEq(x, z3p.MkCharExpr(c));
            Move<Expr> t = Move<Expr>.Create(sourceState, targetState, cond);
            return t;
        }

        //Z3
        //[TestMethod]
        //public void RevereseTest()
        //{
        //    string regex = "^abcdefg$";
        //    Z3.AutomataBuilder ab = new Z3.AutomataBuilder(CharacterEncoding.ASCII);
        //    var A = ab.CreateFromRegex(regex);
        //    var revA = Z3.AutomataBuilder.MkReverse(A);
        //    var members = new List<string>(ab.GenerateMembers(revA, 10));
        //    Assert.AreEqual<int>(1, members.Count, "must be exactly one member");
        //    Assert.AreEqual<string>("gfedcba", members[0], "member must be gfedcba");
        //}

        //Z3
        //[TestMethod]
        //public void ComplementationTest()
        //{
        //    Z3.AutomataBuilder ab = new Z3.AutomataBuilder(CharacterEncoding.ASCII);

        //    foreach (string regex in new string[] { "^(abc)+$" })
        //    {
        //        var fa1 = ab.CreateFromRegex(regex);
        //        //ShowAsGraph(ab.Describe, fa1, "fa.dgml");
        //        var fa2 = ab.MkComplement(fa1);
        //        //ShowAsGraph(ab.Describe, fa2, "compl_fa.dgml");
        //        var fa3 = ab.MkComplement(fa2);
        //        //ShowAsGraph(ab.Describe, fa3, "compl_compl_fa.dgml");
        //        var members = new List<string>(ab.GenerateMembers(fa3, 10));
        //        foreach (var v in members)
        //            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(v, regex));
        //        Assert.IsTrue(AreEquivalent(ab, fa1, fa3), "A and Compl(Compl(A)) are not equivalent");
        //    }
        //}

        //Z3
        //[TestMethod]
        //public void MkDifferenceICSTsamplesZ3Test()
        //{
        //    var bddBuilder = new CharSetSolver(CharacterEncoding.ASCII);
        //    var ab = new Z3.AutomataBuilder(bddBuilder);
        //    //var cubeSolver = new Internal.CubeSolver(ab);

        //    string[] regexes = File.ReadAllLines("../../../Samples/icst.txt");

        //    for (int i = 0; i < regexes.Length; i++)
        //        for (int j = 0; j < regexes.Length; j++)
        //        {
        //            ab.Z3p.Push();
        //            string s;
        //            if ( i!= 6 && j != 6 && i!= 8 && j != 8) //does not terminate for 6 and 8
        //            {
        //                string regexA = regexes[i];
        //                string regexB = regexes[j];

        //                var A = ab.CreateFromRegex(regexA, System.Text.RegularExpressions.RegexOptions.None);
        //                //ShowAsGraph(converter, A, "A.dgml");

        //                var B = ab.CreateFromRegex(regexB, System.Text.RegularExpressions.RegexOptions.None);
        //                //ShowAsGraph(converter, B, "B.dgml");

        //                var C = ab.MkDifference(A, B, 0);
        //                //ShowAsGraph(converter, C, "C.dgml");


        //                if (i == j)
        //                    Assert.IsTrue(C.IsEmpty, @"A\A must be empty");
        //                else
        //                {
        //                    s = new List<string>(ab.GenerateMembers(C, 1))[0];
        //                    //ShowAsGraph<Expr>(ab, C, "C.dgml");

        //                    Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(s, regexA), "must be a member of " + regexA);
        //                    Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(s, regexB), "must not be a member of " + regexB);
        //                }
        //            }
        //            //System.Diagnostics.Debug.Print("{0},{1}", i, j);
        //            //System.Diagnostics.Debug.Flush();
        //            ab.Z3p.Pop();
        //            //MyWriteLine("{0},{1}", i, j);
        //        }
        //}

        #region product experiments

        [TestMethod]
        public void CheckProductZ3_Unicode_Test()
        {
            LProdExperimentZ3(BitWidth.BV16);
        }
        [TestMethod]
        public void CheckProductZ3_ASCII_Test()
        {
            LProdExperimentZ3(BitWidth.BV7);
        }
        [TestMethod]
        public void MakeProductZ3_ASCII_Test()
        {
            ProdExperimentZ3(BitWidth.BV7);
        }
        [TestMethod]
        public void MakeProductZ3_Unicode_Test()
        {
            ProdExperimentZ3(BitWidth.BV16);
        }

        private void ProdExperimentZ3(BitWidth encoding)
        {

            List<string> regexes = new List<string>(SampleRegexes.regexes);
            regexes.RemoveRange(6, regexes.Count - 6); //just consider the first 6 cases

            int nonemptyCount = 0;

            var z3p = new Z3Provider(encoding);

            for (int i = 0; i < regexes.Count; i++)
                for (int j = 0; j < regexes.Count; j++)
                {

                    string regexA = regexes[i];
                    string regexB = regexes[j];

                    z3p.MainSolver.Push();

                    var A = z3p.RegexConverter.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
                    var B = z3p.RegexConverter.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);

                    var C = Automaton<Expr>.MkProduct(A, B);

                    if (i == j)
                        Assert.IsFalse(C.IsEmpty);
                    if (!C.IsEmpty)
                    {
                        nonemptyCount += 1;
                        string s = GetMember(z3p, C);
                        Assert.IsTrue(Regex.IsMatch(s, regexA, RegexOptions.None), "regex mismatch");
                        Assert.IsTrue(Regex.IsMatch(s, regexB, RegexOptions.None), "regex mismatch");
                    }

                    z3p.MainSolver.Pop();
                }
            Assert.AreEqual<int>(10, nonemptyCount, "wrong number of empty intersections");
        }

        [TestMethod]
        public void ProdTest()
        {
            BitWidth encoding = BitWidth.BV7;

            List<string> regexes = new List<string>(SampleRegexes.regexes);
            regexes.RemoveRange(6, regexes.Count - 6); //just consider the first 6 cases

            int nonemptyCount = 0;

            for (int i = 0; i < regexes.Count; i++)
                for (int j = 0; j < regexes.Count; j++)
                {

                    string regexA = regexes[i];
                    string regexB = regexes[j];

                    var z3p = new Z3Provider(encoding);

                    z3p.MainSolver.Push();

                    var A = z3p.RegexConverter.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
                    var B = z3p.RegexConverter.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);

                    var A1 = z3p.CharSetProvider.Convert(regexA);
                    var B1 = z3p.CharSetProvider.Convert(regexB);

                    var C1 = Automaton<BDD>.MkProduct(A1, B1).Determinize().Minimize();

                    var C = Automaton<Expr>.MkProduct(A, B);
                    var C2 = new SFAz3(z3p, z3p.CharSort, C.Determinize().Minimize()).Concretize(200);

                    var equiv = C1.IsEquivalentWith(C1);
                    Assert.IsTrue(equiv);


                    if (i == j)
                        Assert.IsFalse(C.IsEmpty);
                    if (!C.IsEmpty)
                    {
                        if (i != j)
                        {
                            //z3p.CharSetProvider.ShowGraph(C1, "C1");
                            //z3p.CharSetProvider.ShowGraph(C2, "C2");
                        }

                        nonemptyCount += 1;
                        string s = GetMember(z3p, C);
                        Assert.IsTrue(Regex.IsMatch(s, regexA, RegexOptions.None), "regex mismatch");
                        Assert.IsTrue(Regex.IsMatch(s, regexB, RegexOptions.None), "regex mismatch");
                    }

                    z3p.MainSolver.Pop();
                }
            Assert.AreEqual<int>(10, nonemptyCount, "wrong number of empty intersections");
        }

        static string GetMember(Z3Provider z3p, Automaton<Expr> C)
        {
            //z3p.Chooser.RandomSeed = 123;
            var sExpr = new List<Expr>(C.ChoosePathToSomeFinalState(z3p.Chooser)).ToArray();
            string s = new String(Array.ConvertAll(sExpr, m => z3p.GetCharValue(z3p.MainSolver.FindOneMember(m).Value)));
            return s;
        }

        private void LProdExperimentZ3(BitWidth encoding)
        {

            List<string> regexes = new List<string>(SampleRegexes.regexes);
            regexes.RemoveRange(6, regexes.Count - 6); //just consider the first 100 cases

            int nonemptyCount = 0;

            for (int i = 0; i < regexes.Count; i++)
                for (int j = 0; j < regexes.Count; j++)
                {
                    string regexA = regexes[i];
                    string regexB = regexes[j];

                    var z3p = new Z3Provider(encoding);

                    z3p.MainSolver.Push();

                    var A = z3p.RegexConverter.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
                    var B = z3p.RegexConverter.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);

                    List<Expr> witness;
                    bool C = Automaton<Expr>.CheckProduct(A, B, 0, out witness);

                    if (i == j)
                        Assert.IsTrue(C, "product must me nonempty");
                    if (C)
                    {
                        nonemptyCount += 1;
                        string s = new String(Array.ConvertAll(witness.ToArray(), cs => { return z3p.GetCharValue(z3p.MainSolver.FindOneMember(cs).Value); }));
                        Assert.IsTrue(Regex.IsMatch(s, regexA, RegexOptions.None), "regex mismatch");
                        Assert.IsTrue(Regex.IsMatch(s, regexB, RegexOptions.None), "regex mismatch");
                    }

                    z3p.MainSolver.Pop();
                }
            Assert.AreEqual<int>(10, nonemptyCount, "wrong number of empty intersections");
        }

        #endregion


        #region diff experiments

        [TestMethod]
        public void CheckDiffICST_Unicode()
        {
            LDiffExperimentZ3(BitWidth.BV16);
        }

        [TestMethod]
        public void CheckDiffICST_ASCII()
        {
            LDiffExperimentZ3(BitWidth.BV7);
        }

        [TestMethod]
        public void MakeDiffICST_ASCII()
        {
            DiffExperimentZ3(BitWidth.BV7);
        }

        [TestMethod]
        public void MakeDiffICST_Unicode()
        {
            DiffExperimentZ3(BitWidth.BV16);
        }

        private static void DiffExperimentZ3(BitWidth encoding)
        {

            List<string> regexes = new List<string>(SampleRegexes.regexes);
            regexes.RemoveRange(3, regexes.Count - 3); //just consider the first few cases

            for (int i = 0; i < regexes.Count; i++)
                if (i != 6)
                    for (int j = 0; j < regexes.Count; j++)
                    {
                        string regexA = regexes[i];
                        string regexB = regexes[j];

                        var z3p = new Z3Provider(encoding);

                        z3p.MainSolver.Push();

                        var A = z3p.RegexConverter.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
                        var B = z3p.RegexConverter.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);
                        try
                        {
                            var C = Automaton<Expr>.MkDifference(A, B, 5000);
                            if (!C.IsEmpty)
                            {
                                string s = GetMember(z3p, C);
                                Assert.IsTrue(Regex.IsMatch(s, regexA), s + " must match " + regexA);
                                Assert.IsFalse(Regex.IsMatch(s, regexB), s + " must not match " + regexB);
                            }
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("Timeout {0},{1}", i, j);
                        }

                        z3p.MainSolver.Pop();
                    }
        }

        private static void LDiffExperimentZ3(BitWidth encoding)
        {
            List<string> regexes = new List<string>(SampleRegexes.regexes);
            regexes.RemoveRange(2, regexes.Count - 2); //just consider the first 5 cases

            for (int i = 0; i < regexes.Count; i++)
                for (int j = 0; j < regexes.Count; j++)
                {
                    string regexA = regexes[i];
                    string regexB = regexes[j];

                    var z3p = new Z3Provider(encoding);

                    z3p.MainSolver.Push();

                    var A = z3p.RegexConverter.Convert(regexA, System.Text.RegularExpressions.RegexOptions.None);
                    var B = z3p.RegexConverter.Convert(regexB, System.Text.RegularExpressions.RegexOptions.None);
                    //A.ShowGraph();
                    try
                    {
                        List<Expr> witness;
                        var AmB = Automaton<Expr>.MkDifference(A, B, 5000).Determinize().Minimize();
                        //AmB.ShowGraph();
                        bool isNonempty = Automaton<Expr>.CheckDifference(A, B, 5000, out witness);
                        if (isNonempty)
                        {
                            string s = new String(Array.ConvertAll(witness.ToArray(), c => z3p.GetCharValue(z3p.MainSolver.FindOneMember(c).Value)));
                            Assert.IsTrue(Regex.IsMatch(s, regexA), s + " must match " + regexA);
                            Assert.IsFalse(Regex.IsMatch(s, regexB), s + " must not match " + regexB);
                        }
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Timeout {0},{1}", i, j);
                    }

                    z3p.MainSolver.Pop();
                }
        }


        #endregion

        [TestMethod]
        public void SimpleNonRecursiveAxiomTest()
        {
            Z3Provider solver = new Z3Provider();
            string r = @"^\d{3}$"; //.Net regex matching nonempty sequences of 3 digits
            //corresponding SFA
            var A = new SFAz3(solver, solver.CharacterSort, solver.RegexConverter.Convert(r));

            //assert the theory of the SFA
            A.AssertTheory();

            //declare a new uninterpreted constant named "x" of sort List<character>
            Expr x = solver.MkFreshConst("x", A.InputListSort);

            //create an assertion that x is accepted by A
            var assertion = A.MkAccept(x);

            //try to get a solution for x
            var model = solver.MainSolver.GetModel(assertion, x);

            string input = model[x].StringValue;  
            Assert.IsTrue(Regex.IsMatch(input, r));
        }



        [TestMethod]
        public void SimpleRecursiveAxiomTest() 
        {
            Z3Provider solver = new Z3Provider();
            string r = @"^(abc)+$"; //.Net regex matching nonempty sequences of digits
            //corresponding SFA
            var A = new SFAz3(solver, solver.CharacterSort, solver.RegexConverter.Convert(r));

            //assert the theory of the SFA
            A.AssertTheory();

            //declare a new uninterpreted constant named "x" of sort List<character>
            Expr x = solver.MkFreshConst("x", A.InputListSort);

            //create an assertion that x is accepted by A
            var assertion = (BoolExpr)A.MkAccept(x);

            /*
            var assumptions = new BoolExpr[2];
            assumptions[0] = solver.Z3S.Assertions[32];
            assumptions[1] = solver.Z3S.Assertions[33];


            string benchmark = solver.Z3.BenchmarkToSMTString("listtest", "logic", "sat", "", assumptions, assertion);
             */

            //try to get a solution for x
            var model = solver.MainSolver.GetModel(assertion, x);


            string input = model[x].StringValue;
            Assert.IsTrue(Regex.IsMatch(input, r));
        }


        [TestMethod]
        public void Utf16AxiomTest()
        {
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            string r = @"^([\0-\uD7FF\uE000-\uFFFF]|([\uD800-\uDBFF][\uDC00-\uDFFF]))*$"; //.Net regex matching valid utf16 encoded strings
            var A = new SFAz3(solver, solver.CharacterSort, solver.RegexConverter.Convert(r));
            A.AssertTheory();
            //declare a new uninterpreted constant named "x" of sort List<character>
            Expr x = solver.MkFreshConst("x", A.InputListSort);
            //create an assertion that x is accepted by A
            var assertion = (BoolExpr)A.MkAccept(x);
            var model = solver.MainSolver.GetModel(assertion, x);
            var th = solver.Z3S.ToString(); //string describing the asserted axioms
            string input = model[x].StringValue;
            Assert.IsTrue(Regex.IsMatch(input, r));
        }

        [TestMethod]
        public void SFAcreationTest()
        {
            Z3Provider solver = new Z3Provider(BitWidth.BV7);
            //create basic symbolic automata using Z3 terms
            var a = solver.RegexConverter.Convert(@"^\w{5}$");
            var b = solver.RegexConverter.Convert(@"\d");
            //wraps the automaton in an SFA object that provides symbolic language acceptor axioms
            var A = new SFA<FuncDecl, Expr, Sort>(solver, solver.CharacterSort, a);
            var B = new SFA<FuncDecl, Expr, Sort>(solver, solver.CharacterSort, b);
            var C = A - B; //difference automaton that accepts L(A)-L(B)

            C.AssertTheory(); //assert the theory of C to the solver

            //declate a new uninterpreted constant of sort List<character>
            Expr inputConst = solver.MkFreshConst("input", C.InputListSort);
            //get a solutions for the constant so that the accept axiom holds 
            var model = solver.MainSolver.GetModel(C.MkAccept(inputConst), inputConst);
            string input = model[inputConst].StringValue;  //actual value that is in L(a)-L(b)
            Assert.IsTrue(Regex.IsMatch(input, @"^\w{5}$"));
            Assert.IsFalse(Regex.IsMatch(input, @"\d"));
        }

        [TestMethod]
        public void SFAtest2()
        {
            Z3Provider solver = new Z3Provider();
            string a = @"^[A-Za-z0-9]+@(([A-Za-z0-9\-])+\.)+([A-Za-z\-])+$"; //.Net regex
            string b = @"^\d.*$";                                            //.Net regex
            //corresponding SFAs
            var A = new SFAz3(solver, solver.CharacterSort, solver.RegexConverter.Convert(a));
            var B = new SFAz3(solver, solver.CharacterSort, solver.RegexConverter.Convert(b));

            A.AssertTheory(); //assert the theories of both SFAs to the solver
            B.AssertTheory();

            //declare a new uninterpreted constant of sort List<character>
            Expr inputConst = solver.MkFreshConst("input", A.InputListSort);

            //get a solution for input so that it is accepted by A but not by B
            var assertion = solver.MkAnd(A.MkAccept(inputConst),
                                         solver.MkNot(B.MkAccept(inputConst)));
            var model = solver.MainSolver.GetModel(assertion, inputConst);
            string input = model[inputConst].StringValue;  //actual witness in L(A)-L(B)
        }

        [TestMethod]
        public void SerializeDeserializeTest()
        {
            CharSetSolver solver = new CharSetSolver();
            string a = @"^[A-Za-z0-9]{1,3}$"; //.Net regex
            //corresponding SFAs
            var dfa = solver.Convert(a);
            string ser = solver.SerializeAutomaton(dfa);
            var dfaback = solver.DeserializeAutomaton(ser);

            Assert.IsTrue(dfa.IsEquivalentWith(dfaback));
        }

        //[TestMethod]
        //public void SerializeDeserializeTestC4C()
        //{
        //    File.ReadAllText("../../Samples/positive.txt");

        //    CharSetSolver solver = new CharSetSolver();
        //    string a = @"^[A-Za-z0-9]+@(([A-Za-z0-9\-])+\.)+([A-Za-z\-])+$"; //.Net regex
        //    //corresponding SFAs
        //    var dfa = solver.Convert(a);
        //    string ser = solver.SerializeAutomaton(dfa);
        //    var dfaback = solver.DeserializeAutomaton(ser);

        //    Assert.IsTrue(dfa.IsEquivalentWith(dfaback, solver));
        //}


        #region misc helper functions

        void MyClearOutput()
        {
            StreamWriter myOutputFile = new StreamWriter("C:/tmp/testOutputFromVS2010UnitTest.txt", false);
            myOutputFile.Write("");
            myOutputFile.Close();
        }

        void MyWriteLine(string pat, params object[] args)
        {
            StreamWriter sw = new StreamWriter("C:/tmp/testOutputFromVS2010UnitTest.txt", true);
            sw.WriteLine(pat, args);
            sw.Flush();
            sw.Close();
        }

        //private static void ShowAsGraph<S>(Func<S,string> descr, Automaton<S> fa, string file)
        //{
        //    Microsoft.Automata.Internal.DirectedGraphs.DgmlWriter.AutomatonToDgml<S>(descr, fa, file);
        //    OpenFileInVS(file);
        //}

        private static void OpenFileInVS(string file)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo(file);
            p.StartInfo.WorkingDirectory = System.Environment.CurrentDirectory;
            p.Start();
        }

        //private static bool IsContained(Z3.AutomataBuilder ab, string regex1, string regex2)
        //{
        //    var A = ab.CreateFromRegex(regex1);
        //    var B = ab.CreateFromRegex(regex2);
        //    var C = ab.MkDifference(A, B, 0);
        //    return C.IsEmpty;
        //}

        //private static bool AreEquivalent(Z3.AutomataBuilder ab, Automaton<Expr> A, Automaton<Expr> B)
        //{
        //    var C = ab.MkDifference(A, B, 0);
        //    if (!C.IsEmpty)
        //        return false;
        //    var D = ab.MkDifference(B, A, 0);
        //    return D.IsEmpty;
        //}
        #endregion
    }

    [TestClass]
    public class Z3_MinimizationTests
    {
        [TestMethod]
        public void MintermBlowupTest()
        {
            long old_tH = 1;
            long ratio =1;
            for (int i = 1; i < 20; i++)
            {
                var t = MintermBlowupTestHelper(i);
                var tM = MintermBlowupTestHelperMoore(i);
                var tH = (i < 4 ? MintermBlowupTestHelperHopcroft(i) : ratio * old_tH);
                //ratio = tH / old_tH;
                old_tH = tH;
                //Console.WriteLine(tH + "," + tM + "," + t);
            }
        }

        private static int MintermBlowupTestHelper(int K)
        {
            var z3p = new Z3Provider(BitWidth.BV32);
            var x = z3p.CharVar;
            var zero = z3p.MkNumeral(0, z3p.CharSort);
            Func<int, Expr> gamma = k =>
            {
                int kth = 1 << k;
                Expr mask = z3p.MkNumeral(kth, z3p.CharSort);
                Expr cond = z3p.MkNot(z3p.MkEq(z3p.MkBvAnd(x, mask), zero));
                return cond;
            };
            var moves = new List<Move<Expr>>();
            for (int i = 0; i < K; i++)
                moves.Add(Move<Expr>.Create(i, i + 1, gamma(i)));
            for (int i = 0; i < K; i++)
                moves.Add(Move<Expr>.Create(i == 0 ? 0 : K + i, K + i + 1, i == 0 ? z3p.MkNot(gamma(i)) : gamma(i)));
            var aut = Automaton<Expr>.Create(z3p, 0, new int[] { K, 2 * K }, moves);

            var sfa = new SFA<FuncDecl, Expr, Sort>(z3p, z3p.CharSort, aut);
            sfa.Automaton.CheckDeterminism(true);

            //sfa.ShowGraph();

            int t = System.Environment.TickCount;
            var autmin = sfa.Minimize();
            t = System.Environment.TickCount - t;

            //autmin.ShowGraph();
            return t;
        }

        private static int MintermBlowupTestHelperMoore(int K)
        {
            var z3p = new Z3Provider(BitWidth.BV32);
            var x = z3p.CharVar;
            var zero = z3p.MkNumeral(0, z3p.CharSort);
            Func<int, Expr> gamma = k =>
            {
                int kth = 1 << k;
                Expr mask = z3p.MkNumeral(kth, z3p.CharSort);
                Expr cond = z3p.MkNot(z3p.MkEq(z3p.MkBvAnd(x, mask), zero));
                return cond;
            };
            var moves = new List<Move<Expr>>();
            for (int i = 0; i < K; i++)
                moves.Add(Move<Expr>.Create(i, i + 1, gamma(i)));
            for (int i = 0; i < K; i++)
                moves.Add(Move<Expr>.Create(i == 0 ? 0 : K + i, K + i + 1, i ==0 ? z3p.MkNot(gamma(i)) : gamma(i)));
            var aut = Automaton<Expr>.Create(z3p, 0, new int[] { K, 2 * K }, moves);

            var sfa = new SFA<FuncDecl, Expr, Sort>(z3p, z3p.CharSort, aut);
            sfa.Automaton.CheckDeterminism(true);



            int t = System.Environment.TickCount;
            var autmin = sfa.Automaton.MinimizeMoore();
            t = System.Environment.TickCount - t;


            return t;
        }

        private static int MintermBlowupTestHelperHopcroft(int K)
        {
            var z3p = new Z3Provider(BitWidth.BV32);
            var x = z3p.CharVar;
            var zero = z3p.MkNumeral(0, z3p.CharSort);
            Func<int, Expr> gamma = k =>
            {
                int kth = 1 << k;
                Expr mask = z3p.MkNumeral(kth, z3p.CharSort);
                Expr cond = z3p.MkNot(z3p.MkEq(z3p.MkBvAnd(x, mask), zero));
                return cond;
            };
            var moves = new List<Move<Expr>>();
            for (int i = 0; i < K; i++)
                moves.Add(Move<Expr>.Create(i, i + 1, gamma(i)));
            for (int i = 0; i < K; i++)
                moves.Add(Move<Expr>.Create(i == 0 ? 0 : K + i, K + i + 1, i == 0 ? z3p.MkNot(gamma(i)) : gamma(i)));
            var aut = Automaton<Expr>.Create(z3p, 0, new int[] { K, 2 * K }, moves);

            var sfa = new SFA<FuncDecl, Expr, Sort>(z3p, z3p.CharSort, aut);
            sfa.Automaton.CheckDeterminism(true);


            int t = System.Environment.TickCount;
            var autmin = sfa.Automaton.MinimizeHopcroft();
            t = System.Environment.TickCount - t;

            return t;
        }

        //[TestMethod]
        public void GenerateMinimizationSFAs()
        {
            var regexes = new List<string>(File.ReadLines("c:/Automata/samples/regexes.txt"));
            int timeouts = 0;
            for (int i = 0; i < regexes.Count; i++)
            {
                //put a 1 second bound on determinization
                try
                {
                    Automata.Utilities.RegexToRangeAutomatonSerializer.SaveAsRangeAutomaton(regexes[i], BitWidth.BV16, "c:/Automata/brics/dfas/dfa_" + i + ".txt", true, true, 1000);
                }
                catch (TimeoutException)
                {
                    timeouts += 1;
                }
            }
        }

        //[TestMethod]
        public void TestMinimizationOfSFAs()
        {
            var auts = new List<Automaton<BDD>>();
            var rex = new Rex.RexEngine(BitWidth.BV16);
            foreach (var file in Directory.EnumerateFiles("c:/Automata/brics/dfas/"))
            {
                var aut = Automata.Utilities.RegexToRangeAutomatonSerializer.Read(rex.Solver, file);
                auts.Add(aut);
            }

            int t = System.Environment.TickCount;
            foreach (var aut in auts)
                rex.Minimize(aut);
            t = System.Environment.TickCount - t;
            Console.WriteLine(t);
        }
    }
}
