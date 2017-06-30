using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

using Microsoft.Bek.Frontend;

using Microsoft.Z3;

using Microsoft.Bek.Model;

using Microsoft.Automata.Z3;
using Microsoft.Automata;

using SFAModel = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STModel = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STbModel = Microsoft.Automata.STb<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;


namespace Microsoft.Bek.Tests
{

    [TestClass]
    public class Bek2Tests
    {

        static string sampleDir = "../../../Samples/";

        [TestMethod]
        public void TestBase64Encode()
        {
            Z3Provider solver = new Z3Provider();
            var bek = BekConverter.BekFileToBekProgram(sampleDir + "bek/Base64encode.bek");
            var st = BekConverter.BekToSTb(solver, bek).ExploreBools().ToST();
            //st.ShowGraph();
            var st0 = st.RestrictDomain(@"^([AB]{3})*$");
            //st0.ShowGraph();
            var st1 = st0.Explore();
            //st1.Simplify();
            //st1.ShowGraph();
            //st0.ToDot("c:/tmp/b64eRestr.dot");
            var sft = st.Explore();
            var Q = sft.StateCount;
            int M = 0;
            int F = 0;
            int tot = 0;
            var tmp = new Dictionary<Expr, int>();
            foreach (var m in sft.GetMoves())
            {
                if (m.Label.IsFinal)
                {
                    F += 1;
                    tot += 1;
                }
                else
                {
                    M += 1;
                    int k = 0;
                    if (tmp.TryGetValue(m.Label.Guard, out k))
                        tot += k;
                    else
                    {
                        foreach (var v in solver.MainSolver.FindAllMembers(m.Label.Guard))
                            k += 1;
                        tot += k;
                        tmp[m.Label.Guard] = k;
                    }
                }
            }
            Console.WriteLine(tot);



            StringBuilder sb = new StringBuilder();
            //bek.ast.ToJS(sb);
        }

        [TestMethod]
        public void TestBase64Decode()
        {
            Z3Provider solver = new Z3Provider();
            var bek = BekConverter.BekFileToBekProgram(sampleDir + "bek/Base64decode.bek");
            var st = BekConverter.BekToSTb(solver, bek).ExploreBools().ToST();
            st.Simplify();
            //st.ShowGraph();
            var st0 = st.RestrictDomain(@"^([AB]{3})*$");
            //st0.ShowGraph();
            var st1 = st0.Explore();
            //st1.Simplify();
            //st1.ShowGraph();
            //st.ToDot("c:/tmp/b64d.dot");
            var sft = st.Explore();
            var Q = sft.StateCount;
            int M = 0;
            int F = 0;
            int tot = 0;
            var tmp = new Dictionary<Expr, int>();
            foreach (var m in sft.GetMoves())
            {
                if (m.Label.IsFinal)
                {
                    F += 1;
                    tot += 1;
                }
                else
                {
                    M += 1;
                    int k = 0;
                    if (tmp.TryGetValue(m.Label.Guard, out k))
                        tot += k;
                    else
                    {
                        foreach (var v in solver.MainSolver.FindAllMembers(m.Label.Guard))
                            k += 1;
                        tot += k;
                        tmp[m.Label.Guard] = k;
                    }
                }
            }
            Console.WriteLine(tot);


            Assert.AreEqual<int>(87, Q);
        }

        // [TestMethod]
        public void DecodeDecode()
        {
            Z3Provider solver = new Z3Provider();
            var A = BekConverter.BekToSTb(solver, BekConverter.BekFileToBekProgram(sampleDir + "bek/Base64decode.bek")).ExploreBools().ToST();
            var ID = ST<FuncDecl, Expr, Sort>.MkId(solver, solver.CharSort);
            var st = A.Compose(A);
            //st.ShowGraph();
            var sfa1 = st.ToSFA().Automaton;
            var sfa = ConvertToAutomatonOverBvSet(solver, sfa1).Determinize().MinimizeHopcroft();
            //solver.CharSetProvider.ShowGraph(sfa, "test");
        }

         static Automaton<BDD> ConvertToAutomatonOverBvSet(Z3Provider solver, Automaton<Expr> aut)
         {
             var moves = new List<Move<BDD>>();
             foreach (var move in aut.GetMoves())
                 if (move.IsEpsilon)
                     moves.Add(Move<BDD>.Epsilon(move.SourceState, move.TargetState));
                 else
                     moves.Add(Move<BDD>.Create(move.SourceState, move.TargetState, solver.ConvertToCharSet(move.Label)));

             var res = Automaton<BDD>.Create(solver.CharSetProvider, aut.InitialState, aut.GetFinalStates(), moves);
             return res;
         }

        [TestMethod]
        public void TestUtf8range1()
        {
            //coding sequences
            var one = @"[\x00-\x7F]";
            var two = @"[\xC2-\xDF][\x80-\xBF]";
            var thr = @"[\xE0-\xEF][\x80-\xBF]{2}";
            var fou = @"[\xF0-\xF4][\x80-\xBF]{3}";
            var regex = string.Format("^(({0})|({1})|({2})|({3}))*$", one, two, thr, fou);
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekFileToST(solver, sampleDir + "bek/UTF8Encode.bek");

            var aut2 = solver.CharSetProvider.Convert(regex).Determinize().MinimizeHopcroft();
            //solver.CharSetProvider.ShowGraph(aut2, "Utf8_1");

            var aut = new SFAModel(solver, solver.CharSort, solver.RegexConverter.Convert(regex)).Complement();
            var st1 = st.ComputePreImage(aut);
            //st1.ShowGraph(5);
            var preim = st1.ToSFA();
            //preim.ShowGraph();
            Assert.IsTrue(preim.IsEmpty);
        }

        [TestMethod]
        public void TestUtf8range2()
        {
            //coding sequences with more precision regarding min max cases
            var one = @"[\x00-\x7F]";
            var two = @"[\xC2-\xDF][\x80-\xBF]";
            var thr = @"(\xE0[\xA0-\xBF]|[\xE1-\xEF][\x80-\xBF])[\x80-\xBF]";
            var fou = @"(\xF0[\x90-\xBF]|[\xF1-\xF3][\x80-\xBF]|\xF4[\x80-\x8F])[\x80-\xBF]{2}";
            var regex = string.Format("^(({0})|({1})|({2})|({3}))*$", one, two, thr, fou);
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekFileToST(solver, sampleDir + "bek/UTF8Encode.bek");

            var aut2 = solver.CharSetProvider.Convert(regex);
            aut2 = aut2.Determinize();
            aut2 = aut2.MinimizeHopcroft();
            //solver.CharSetProvider.ShowGraph(aut2, "Utf8_2");

            var aut = new SFAModel(solver, solver.CharSort, solver.RegexConverter.Convert(regex)).Complement();
            var preim = st.ComputePreImage(aut).ToSFA();
            //preim.ShowGraph();
            Assert.IsTrue(preim.IsEmpty);
        }

        [TestMethod]
        public void TestUtf8range3()
        {
            //coding sequences with full precision inlcuding elimination of surrogates as valid codepoints
            var one = @"[\x00-\x7F]";
            var two = @"[\xC2-\xDF][\x80-\xBF]";
            var thr = @"(\xE0[\xA0-\xBF]|\xED[\x80-\x9F]|[\xE1-\xEC\xEE\xEF][\x80-\xBF])[\x80-\xBF]";
            var fou = @"(\xF0[\x90-\xBF]|[\xF1-\xF3][\x80-\xBF]|\xF4[\x80-\x8F])[\x80-\xBF]{2}";
            var regex = string.Format("^(({0})|({1})|({2})|({3}))*$", one, two, thr, fou);
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekFileToST(solver, sampleDir + "bek/UTF8Encode.bek");

            var aut2 = solver.CharSetProvider.Convert(regex).Determinize().Minimize();
            //solver.CharSetProvider.ShowGraph(aut2, "Utf8_3");

            //solver.CharSetProvider.ShowGraph(st.ToSFA().Concretize(), "test");

            var aut = new SFAModel(solver, solver.CharSort, solver.RegexConverter.Convert(regex)).Complement();
            var preim = st.ComputePreImage(aut).ToSFA();
            //preim.ShowGraph(1);
            Assert.IsTrue(preim.IsEmpty);
        }

        [TestMethod]
        public void TestDecodeDigitPairs()
        {
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekFileToSTb(solver, sampleDir + "bek/DecodeDigitPairs.bek");
            //st.ShowGraph();
            var f1 = st.Explore();                        //convert to SFT by exploring the register
            //f1.ToCSt("../../../src/Bek.Query.Tests/DecodeDigitPairs.cs", "", "Experiments", "DecodeDigitPairs");
            //f1.ToDot("../../../src/Bek.Query.Tests/DecodeDigitPairs.dot");
            //f1.ShowGraph();
            var f = f1.ToST();
            var fof = f + f;                             //functional composition
            bool idempotent = f.Eq1(fof);                //check idempotence
            if (!idempotent)
            {
                var w = f.Diff(fof);                     //find a witness where f and fof differ
                string input = w.Input.StringValue;      // e.g. "5555"
                string output1 = w.Output1.StringValue;  // e.g. f("5555") == "77"
                string output2 = w.Output2.StringValue;  // e.g. f(f("5555")) == "M"
                Console.WriteLine("not idempotent, witness: {0}, {1}, {2}", input, output1, output2);
            }
            else
            {
                Console.WriteLine("idempotent");
            }
        }

        [TestMethod]
        public void TestUTF8EncodeCSgen()
        {
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/UTF8Encode.bek");
            stb.ToCS("UTF8Encode.cs", "Test", "TestClass", false, true);
            var st1 = stb.ExploreBools();
            st1.ToCS("UTF8Encode_B.cs", "Test", "TestClass", false, true);
            var sft = stb.Explore();
            sft.ToCS("UTF8Encode_F.cs", "Test", "TestClass", false, true);
        }

        [TestMethod]
        public void TestBitsBug()
        {
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/BitsBug.bek");
            var sft = stb; // stb.Explore();

            var bitsbug = stb.Compile("Microsoft.Bek.Tests", "BitsBug", true);

            int c = 15;
            int k = ((1 + (c & 7)) & 3);
            string res = bitsbug.Apply(((char)c).ToString());
            Assert.AreEqual<int>(1, res.Length);
            Assert.AreEqual<int>(k, (int)res[0]);
        }

        [TestMethod]
        public void TestUTF8()
        {
            string _1;
            string _2;
            string _3;
            TryGeneratedUtf8EncodeFlat("\uDAE1\uDCA5", out _1);
            TryGeneratedUtf8Encode_F("\uDAE1\uDCA5", out _2);
            TryActualUtf8Encode("\uDAE1\uDCA5", out _3);
            Assert.AreEqual<string>(_3, _1);
            Assert.AreEqual<string>(_3, _2);
        }


        [TestMethod]
        public void TestGeneratedUtf8EncodeFlat()
        {
            int K = 100; //number of strings
            int L = 10000; //length of each string

            string _1;
            string _2;
            string _3;
            TryGeneratedUtf8EncodeFlat("\uDAE1\uDCA5", out _1);
            TryGeneratedUtf8Encode_F("\uDAE1\uDCA5", out _2);
            TryActualUtf8Encode("\uDAE1\uDCA5", out _3);

            Assert.AreEqual<string>(_1, _2);
            Assert.AreEqual<string>(_1, _3);

            CharSetSolver css = new CharSetSolver(BitWidth.BV16);
            var A = css.Convert("^.{" + L + "}$");
            //var utf16 = css.Convert(@"^([\0-\uD7FF\uE000-\uFFFD]|([\uD800-\uDBFF][\uDC00-\uDFFF]))*$");
            //var utf16 = css.Convert(@"^([\uD800-\uDBFF][\uDC00-\uDFFF])*$");
            var utf16 = css.Convert(@"^([\0-\uD7FF\uE000-\uFFFD])*$");
            A = Automaton<BDD>.MkProduct(A, utf16);

            //css.Chooser.RandomSeed = 123;

            string[] inputs = new string[K];

            for (int i = 0; i < K; i++)
            {
                inputs[i] = css.GenerateMember(A);
            }

            for (int i = 0; i < K; i++)
            {
                string out_expected;
                string out_bek;
                string out_bek_stream;
                string out_bek_orig;
                int stat_expected = TryActualUtf8Encode(inputs[i], out out_expected);
                int stat_actual = TryGeneratedUtf8EncodeFlat(inputs[i], out out_bek);
                int stat_actual_stream = TryGeneratedUtf8EncodeStream(inputs[i], out out_bek_stream);
                int stat_actual_orig = TryGeneratedUtf8Encode_F(inputs[i], out out_bek_orig);
                Assert.AreEqual<string>(out_expected, out_bek_orig);
                Assert.AreEqual<string>(out_expected, out_bek);
                Assert.AreEqual<string>(out_expected, out_bek_stream);
            }

            int timeOur = System.Environment.TickCount;
            for (int i = 0; i < K; i++)
            {
                string tmp;
                TryGeneratedUtf8EncodeFlat(inputs[i], out tmp);
            }
            timeOur = System.Environment.TickCount - timeOur;

            int timeOurStream = System.Environment.TickCount;
            for (int i = 0; i < K; i++)
            {
                string tmp;
                TryGeneratedUtf8EncodeStream(inputs[i], out tmp);
            }
            timeOurStream = System.Environment.TickCount - timeOurStream;

            int timeOurOrig = System.Environment.TickCount;
            for (int i = 0; i < K; i++)
            {
                string tmp;
                TryGeneratedUtf8Encode(inputs[i], out tmp);
            }
            timeOurOrig = System.Environment.TickCount - timeOurOrig;

            int timeSys = System.Environment.TickCount;
            for (int i = 0; i < K; i++)
            {
                string tmp;
                TryActualUtf8Encode(inputs[i], out tmp);
            }
            timeSys = System.Environment.TickCount - timeSys;

            Console.WriteLine("timeOurStream:{3}ms, timeOur:{0}ms, timeOurOrig:{1}ms, timeSys:{2}ms", timeOur, timeOurOrig, timeSys, timeOurStream);
        }


        [TestMethod]
        public void TestUTF8EncodeDotGen()
        {
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/UTF8Encode.bek");
            stb.ToDot("UTF8Encode.dot");
            var st1 = stb.ExploreBools();
            st1.ToDot("UTF8Encode_B.dot");
            var sft = stb.Explore();
            sft.ToDot("UTF8Encode_F.dot");
        }

        [TestMethod]
        public void TestCssEncodeDotGen()
        {
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode.bek");
            stb.ToDot(15, "CssEncode.dot");
            var st1 = stb.ExploreBools();
            st1.ToDot(15, "CssEncode_B.dot");
            var sft = stb.Explore();
            sft.ToDot(15, "CssEncode_F.dot");
        }

        [TestMethod]
        public void TestCssEncodeDomainDotGen()
        {
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode.bek");
            var sfa = stb.ToST().ToSFA().Determinize().Minimize();
            Assert.AreEqual<int>(2, sfa.StateCount);
            sfa.SaveAsDot("CssEncode_Dom.dot");
            //sfa.ShowGraph();
        }

        [TestMethod]
        public void TestDecodeDigitPairsDotGen()
        {
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/DecodeDigitPairs.bek");
            stb.ToDot("DecodeDigitPairs.dot");
            var sft = stb.Explore();
            sft.ToDot("DecodeDigitPairs_F.dot");
        }

        [TestMethod]
        public void TestCssEncodeCSgen()
        {
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode.bek");
            stb.ToCS("CssEncode.cs", "Test", "CssEncode", false, true);
            var st1 = stb.ExploreBools();
            st1.ToCS("CssEncode_B.cs", "Test", "CssEncode_B", false, true);
            var sft = stb.Explore();
            sft.ToCS("CssEncode_F.cs", "Test", "CssEncode_F", false, true);
        }

        [TestMethod]
        public void TestUTF8DecodeDomain()
        {
            Z3Provider solver = new Z3Provider();
            var dec = BekConverter.BekFileToSTb(solver, sampleDir + "bek/UTF8Decode.bek").ToST();
            var decDom = dec.ToSFA();
            //decDom.ShowGraph();

            var one = @"[\x00-\x7F]";
            var two = @"[\xC2-\xDF][\x80-\xBF]";
            var thr = @"(\xE0[\xA0-\xBF]|\xED[\x80-\x9F]|[\xE1-\xEC\xEE\xEF][\x80-\xBF])[\x80-\xBF]";
            var fou = @"(\xF0[\x90-\xBF]|[\xF1-\xF3][\x80-\xBF]|\xF4[\x80-\x8F])[\x80-\xBF]{2}";
            var regex = string.Format("^(({0})|({1})|({2})|({3}))*$", one, two, thr, fou);

            var aut = solver.RegexConverter.Convert(regex);
            //solver.CharSetProvider.ShowGraph(solver.CharSetProvider.Convert(regex).Minimize(solver.CharSetProvider), "temp");
            var sfa = new SFAModel(solver, solver.CharacterSort, aut);
            //sfa.ShowGraph();

            bool equiv = aut.IsEquivalentWith(decDom.Automaton);
            Assert.IsTrue(equiv);

        }

        //[TestMethod]
        public void TestUTF8Decode()
        {
            Z3Provider solver = new Z3Provider();
            var dec = BekConverter.BekFileToSTb(solver, sampleDir + "bek/UTF8Decode.bek").ToST().ExploreBools();
            var enc = BekConverter.BekFileToSTb(solver, sampleDir + "bek/UTF8Encode.bek").ToST().Explore();
            var id = STModel.MkId(solver, solver.CharacterSort);
            //var id = BekConverter.BekFileToSTb(solver, sampleDir + "bek/identity.bek").ToST();
            var ed = enc + dec; //first encode then decode
            var de = dec + enc; //first decode then encode
            ed.Simplify();
            //ed.ShowGraph(10);
            //ed.ShowGraph(5);
            //return;
            //id.ShowGraph();
            //de.ShowGraph(5);

            int timeE = System.Environment.TickCount;
            var encF = enc.Explore();
            timeE = System.Environment.TickCount - timeE;

            int timeD = System.Environment.TickCount;
            var decF = dec.Explore();
            timeD = System.Environment.TickCount - timeD;
            decF.Simplify();

            Console.WriteLine(timeE);
            Console.WriteLine(timeD);

            var encRuleCnt = encF.MoveCount;
            var decRuleCnt = decF.MoveCount;

            var tmp =  new Dictionary<Expr,int>();
            int m = 0;
            //foreach (var move in decF.GetMoves())
            //{
            //    int k = 0;
            //    if (move.Condition.IsFinal && move.Condition.Guard.Equals(solver.True))
            //        k = 1;
            //    else if (!tmp.TryGetValue(move.Condition.Guard, out k))
            //    {

            //        foreach (var v in solver.FindAllMembers(move.Condition.Guard))
            //            k += 1;
            //        tmp[move.Condition.Guard] = k;
            //    }
            //    m += k;
            //}

            //var dec_filtered = dec.RestrictDomain(@"^[^\xE0\xED\xF0\xF4]*$");
            //dec_filtered.ShowGraph(5);
            //var dec_filtered_e = dec_filtered.Explore();
            //int stateCount = dec_filtered_e.StateCount;
            //int moveCount = dec_filtered_e.MoveCount;

            string utf16 = @"^([\0-\uD7FF\uE000-\uFFFF]|([\uD800-\uDBFF][\uDC00-\uDFFF]))*$";

            var one = @"[\x00-\x7F]";
            var two = @"[\xC2-\xDF][\x80-\xBF]";
            var thr = @"(\xE0[\xA0-\xBF]|\xED[\x80-\x9F]|[\xE1-\xEC\xEE\xEF][\x80-\xBF])[\x80-\xBF]";
            var fou = @"(\xF0[\x90-\xBF]|[\xF1-\xF3][\x80-\xBF]|\xF4[\x80-\x8F])[\x80-\xBF]{2}";
            var utf8 = string.Format("^(({0})|({1})|({2})|({3}))*$", one, two, thr, fou);

            var UTF_8 = new SFAModel(solver, solver.CharacterSort, solver.RegexConverter.Convert(utf8).Determinize().MinimizeHopcroft());

            //UTF_8.ShowGraph();

            //var F0 = new SFAModel(solver, solver.CharacterSort, solver.RegexConverter.Convert("^(\xF0[\0-\uFFFF]{3})*$").Determinize(solver).Minimize(solver));

            //var decF0 = dec.RestrictDomain(F0);

            //var decF0_E = decF0.Explore();

            //var surr = @"^([\uD800-\uDBFF][\uDC00-\uDFFF])*$";

            //var ed_surr = ed.RestrictDomain(surr);
            //ed_surr.Simplify();
            //ed_surr.ShowGraph(10);

            //F0.ShowGraph();

            //ed_F0.ShowGraph(5);




            //var decE = dec.Explore();

            //dec.STb.ShowGraph();
            //enc.STb.ShowGraph();

            var decDom = dec.ToSFA();
            
            var edDom = ed.ToSFA();
            var eDom = enc.ToSFA();


            var UTF_16 = solver.RegexConverter.Convert(utf16);

            int timeDEdom = System.Environment.TickCount;
            var deDom = de.ToSFA();
            bool equivDomDE = UTF_8.Automaton.IsEquivalentWith(deDom.Automaton);
            timeDEdom = System.Environment.TickCount - timeDEdom;
            Console.WriteLine(timeDEdom);
            

            bool equivDom = UTF_16.IsEquivalentWith(edDom.Automaton);
            Assert.IsTrue(equivDom);
            bool equivDom2 = UTF_16.IsEquivalentWith(eDom.Automaton);
            Assert.IsTrue(equivDom2);

            int time0 = System.Environment.TickCount;

            bool equiv0 = id.Eq1(ed);

            time0 = System.Environment.TickCount - time0;

            int time1 = System.Environment.TickCount;

            bool equiv1 = id.Eq1(de);

            time1 = System.Environment.TickCount - time1;

            Assert.IsTrue(equiv1);
            Assert.IsTrue(equiv0);

            int time = System.Environment.TickCount;

            bool equiv = id.Eq1(ed);

            time = System.Environment.TickCount - time;

            Console.WriteLine(time);

            Assert.IsTrue(equiv);
        }

        //[TestMethod]
        public void TestUTF8Encode()
        {
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/UTF8Encode.bek");
            var tmp = stb.ToST();
            var sft = stb.Explore();
            var sft1 = sft.ToST();
            //sft.ShowGraph();
            //sft1.SaveAsDot("C:/tmp/dot/utf8encode.dot");

            #region data for the popl paper
            var st = sft.ToST();
            int n = st.StateCount;
            var moves = new List<Move<Rule<Expr>>>(st.GetMoves());
            moves.RemoveAll(x => x.Label.IsFinal);
            int m = moves.Count;
            int t = System.Environment.TickCount;
            var st_o_st = st + st;
            int n1 = st_o_st.StateCount;
            var moves1 = new List<Move<Rule<Expr>>>(st_o_st.GetMoves());
            moves1.RemoveAll(y => y.Label.IsFinal);
            int m1 = moves1.Count;
            bool diff = st.Eq1(st_o_st);
            t = System.Environment.TickCount - t;
            #endregion

            var restr = sft.ToST().RestrictDomain(".+");
            restr.AssertTheory();

            Expr inputConst = solver.MkFreshConst("input", restr.InputListSort);
            Expr outputConst = solver.MkFreshConst("output", restr.OutputListSort);

            solver.MainSolver.Assert(restr.MkAccept(inputConst, outputConst));

            //validate correctness for some values against the actual UTF8Encode
            //TBD: validate also exceptional behavior, when the generated code throws an exception 
            //the builtin one must contain the character 0xFFFD
            int K = 50;
            for (int i = 0; i < K; i++)
            {
                var model = solver.MainSolver.GetModel(solver.True, inputConst, outputConst);
                string input = model[inputConst].StringValue;
                string output = model[outputConst].StringValue;

                Assert.IsFalse(string.IsNullOrEmpty(input));
                Assert.IsFalse(string.IsNullOrEmpty(output));

                byte[] encoding = Encoding.UTF8.GetBytes(input);
                char[] chars = Array.ConvertAll(encoding, b => (char)b);
                string output_expected = new String(chars);
                string output_generated = UTF8Encode_F.Apply(input);
                string output_generated2 = UTF8Encode.Apply(input);
                string output_generated3 = UTF8Encode_B.Apply(input);

                Assert.AreEqual<string>(output_expected, output_generated);

                Assert.AreEqual<string>(output_expected, output_generated2);

                Assert.AreEqual<string>(output_expected, output_generated3);

                Assert.AreEqual<string>(output_expected, output);

                //exclude this solution, before picking the next one
                solver.MainSolver.Assert(solver.MkNeq(inputConst, model[inputConst].Value));
            }
        }

        [TestMethod]
        public void TestHtmlDecode()
        {
            int k = 3134 % 100;
            int foo = 0xff41;
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/HtmlDecode.bek");
            var st = stb.ExploreBools().ToST();
            //st.ShowGraph();

        }

        //[TestMethod]
        public void TestCssEncode6()
        {
            var solver = new Z3Provider();
            var st = BekConverter.BekFileToST(solver, sampleDir + "bek/CssEncode6.bek");
            //st.STb.ShowGraph();
            //st.STb.ExploreBools().ShowGraph();
            var sft = st.Explore();
            //sft.Simplify();
            //sft.ShowGraph(20);

            //just to get longer input strings
            var restr = sft.RestrictDomain("^[^\0-\x32]{5,}$");

            restr.AssertTheory();

            Expr inputConst = solver.MkFreshConst("input", sft.InputListSort);
            Expr outputConst = solver.MkFreshConst("output", sft.OutputListSort);

            solver.MainSolver.Assert(restr.MkAccept(inputConst, outputConst));

            int okCnt = 0;
            int error0Cnt = 0;
            int error1Cnt = 0;

            //validate correctness for some values against the actual CssEncode
            //validate also exceptional behavior
            for (int i = 0; i < 10; i++)
            {
                var model = solver.MainSolver.GetModel(solver.True, inputConst, outputConst);
                string input = model[inputConst].StringValue;
                string output = model[outputConst].StringValue;
                Assert.IsFalse(string.IsNullOrEmpty(output));
                char lastChar = output[output.Length - 1];

                //try
                //{
                var output_expected = System.Web.Security.AntiXss.AntiXssEncoder.CssEncode(input);
                    Assert.AreEqual<string>(output_expected, output);
                    okCnt += 1;
                //}
                //catch (Microsoft.Security.Application.InvalidSurrogatePairException)
                //{
                //    Assert.AreEqual<char>('\0', lastChar);
                //    error0Cnt += 1;
                //}
                //catch (Microsoft.Security.Application.InvalidUnicodeValueException)
                //{
                //    Assert.AreEqual<char>('\x01', lastChar);
                //    error1Cnt += 1;
                //}

                //exclude this solution, before picking the next one
                    solver.MainSolver.Assert(solver.MkNeq(inputConst, model[inputConst].Value));
            }
            Console.WriteLine(string.Format("okCnt={0}, error0Cnt={1}, error1Cnt={2}", okCnt, error0Cnt, error1Cnt));
        }

        [TestMethod]
        public void TestCssEncodeBekRoundtrip()
        {
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode.bek");
            var sb = new StringBuilder();
            stb.ToBek(sb);
            var stb2 = BekConverter.BekToSTb(solver, sb.ToString());
            CheckFullEquivalence(stb, stb2);
        }


        //[TestMethod]
        //public void TestCssEncode6BekRoundtrip()
        //{
        //    Z3Context solver = new Z3Context();
        //    var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode6.bek");
        //    //stb.ShowGraph();
        //    var sft = stb.Explore();
        //    sft.ToBek("TestCssEncode6BekRoundtrip.txt");
        //    var st2 = BekConverter.BekFileToSTb(solver, "TestCssEncode6BekRoundtrip.txt").Explore();
        //    //st2.ShowGraph();
        //    CheckFullEquivalence(stb, st2);
        //}

        private static void CheckFullEquivalence(STbModel A, STbModel B)
        {
            CheckFullEquivalence(A.ToST(), B.ToST());
        }

        private static void CheckFullEquivalence(STModel A, STModel B)
        {
            var domA = A.ToSFA();
            var domB = B.ToSFA();
            bool domA_subsetof_domB = domA.IsSubsetOf(domB);
            bool domB_subsetof_domA = domB.IsSubsetOf(domA);
            bool partial_equiv = A.Eq1(B);
            Assert.IsTrue(domA_subsetof_domB);
            Assert.IsTrue(domB_subsetof_domA);
            Assert.IsTrue(partial_equiv);
        }

        [TestMethod]
        public void TestGeneratedCssEncode()
        {
            CharSetSolver css = new CharSetSolver(BitWidth.BV16);
            var A = css.Convert(".{50,}"); //at least 100 characters
            var utf16 = css.Convert(@"^([\0-\uD7FF\uE000-\uFFFD]|([\uD800-\uDBFF][\uDC00-\uDFFF]))*$");
            A = A.Intersect(utf16);
            //css.Chooser.RandomSeed = 123;
            int okCnt = 0;
            int error1Cnt = 0;
            int error2Cnt = 0;
            int diffErrors = 0;
            for (int i = 0; i < 1000; i++)
            {
                string input = css.GenerateMember(A);
                string out_expected;
                string out_CssEncode;
                string out_CssEncode_B;
                string out_CssEncode_F;
                int stat_expected = TryActualCssEncode(input, out out_expected);
                int stat_CssEncode = TryGeneratedCssEncode(input, out out_CssEncode);
                int stat_CssEncode_B = TryGeneratedCssEncode_B(input, out out_CssEncode_B);
                int stat_CssEncode_F = TryGeneratedCssEncode_F(input, out out_CssEncode_F);
                Assert.AreEqual<string>(out_expected, out_CssEncode);
                Assert.AreEqual<string>(out_expected, out_CssEncode_B);
                Assert.AreEqual<string>(out_expected, out_CssEncode_F);
                Assert.AreEqual<int>(stat_CssEncode, stat_CssEncode_B);
                Assert.AreEqual<int>(stat_CssEncode, stat_CssEncode_F);
                if (stat_expected != stat_CssEncode)
                    diffErrors += 1;
                if (stat_expected == 0)
                    okCnt += 1;
                else if (stat_expected == 1)
                    error1Cnt += 1;
                else
                    error2Cnt += 1;
            }
            Console.WriteLine("okCnt={0}, error1Cnt={1}, error2Cnt={2}, diffErrors={3}", okCnt, error1Cnt, error2Cnt, diffErrors);
        }

        public static int TryActualCssEncode(string input, out string output)
        {
            try
            {
                var res = System.Web.Security.AntiXss.AntiXssEncoder.CssEncode(input);
                output = res;
                return 0;
            }
            catch (Exception)
            {
                output = null;
                return 1;
            }
        }

        public static int TryActualUtf8Encode(string input, out string output)
        {
            try
            {
                var res = UTF8.Apply(input);
                output = res;
                return 0;
            }
            catch (Exception)
            {
                output = null;
                return 1;
            }
        }

        int TryGeneratedCssEncode(string input, out string output)
        {
            try
            {
                var res = CssEncode.Apply(input);
                output = res;
                return 0;
            }
            catch (Exception e)
            {
                if (e.Message == "InvalidSurrogatePairException")
                {
                    output = null;
                    return 1;
                }
                else if (e.Message == "InvalidUnicodeValueException")
                {
                    output = null;
                    return 2;
                }
                else
                    throw e;
            }
        }

        //int TryGeneratedCssEncodeFlat(string input, out string output)
        //{
        //    try
        //    {
        //        var res = AntiXssExperimental.CssEncoder.Apply(input);
        //        output = res;
        //        return 0;
        //    }
        //    catch (Exception e)
        //    {
        //        output = null;
        //        return 1;
        //    }
        //}

        int TryGeneratedUtf8Encode(string input, out string output)
        {
            try
            {
                var res = UTF8Encode_F.Apply(input);
                output = res;
                return 0;
            }
            catch (Exception e)
            {
                output = null;
                return 1;
            }
        }

        int TryGeneratedUtf8EncodeFlat(string input, out string output)
        {
            try
            {
                var res = UTF8Encoder.Apply(input);
                output = res;
                return 0;
            }
            catch (Exception e)
            {
                output = null;
                return 1;
            }
        }

        int TryGeneratedUtf8EncodeStream(string input, out string output)
        {
            try
            {
                var res = new String(new List<char>(UTF8Encoder.Transduce(input)).ToArray());
                output = res;
                return 0;
            }
            catch (Exception e)
            {
                output = null;
                return 1;
            }
        }

        int TryGeneratedUtf8Encode_F(string input, out string output)
        {
            try
            {
                var res = UTF8Encode_F.Apply(input);
                output = res;
                return 0;
            }
            catch (Exception e)
            {
                output = null;
                return 1;
            }
        }

        int TryGeneratedCssEncode_B(string input, out string output)
        {
            try
            {
                var res = CssEncode_B.Apply(input);
                output = res;
                return 0;
            }
            catch (Exception e)
            {
                if (e.Message == "InvalidSurrogatePairException")
                {
                    output = null;
                    return 1;
                }
                else if (e.Message == "InvalidUnicodeValueException")
                {
                    output = null;
                    return 2;
                }
                else
                    throw e;
            }
        }

        int TryGeneratedCssEncode_F(string input, out string output)
        {
            try
            {
                var res = CssEncode_F.Apply(input);
                output = res;
                return 0;
            }
            catch (Exception e)
            {
                if (e.Message == "InvalidSurrogatePairException")
                {
                    output = null;
                    return 1;
                }
                else if (e.Message == "InvalidUnicodeValueException")
                {
                    output = null;
                    return 2;
                }
                else
                    throw e;
            }
        }

        [TestMethod]
        public void TestGeneratedCssEncodePerformance()
        {
            CharSetSolver css = new CharSetSolver(BitWidth.BV16);
            var A = css.Convert("^.{100,}$"); //at least 50 chars
            var utf16 = css.Convert(@"^([\0-\uD7FF\uE000-\uFFFD]|([\uD800-\uDBFF][\uDC00-\uDFFF]))*$");
            A = A.Intersect(utf16);
            //css.Chooser.RandomSeed = 123;
            List<string> samples = new List<string>();
            //construct a sample set of 100000 strings of length >= 50 that are valid inputs
            while (samples.Count < 100)
            {
                string input = css.GenerateMember(A);//margus
                samples.Add(input);
               // if (TryActualCssEncode(input, out tmp) == 0)
               //     samples.Add(input);
            }
            //now use the sample set for performace comparison

            var antiXssTimes = new List<int>();
            var CssEncodeTimes = new List<int>();
            var CssEncodeTimes_B = new List<int>();
            var CssEncodeTimes_F = new List<int>();

            int NrOfReps = 100;

            for (int j = 0; j < NrOfReps; j++)
            {
                //the AntiXss encoder
                int t_AntiXss = System.Environment.TickCount;
                for (int i = 0; i < samples.Count; i++)
                {
                    string tmp = System.Web.Security.AntiXss.AntiXssEncoder.CssEncode(samples[i]);
                }
                t_AntiXss = System.Environment.TickCount - t_AntiXss;
                antiXssTimes.Add(t_AntiXss);
                //generated encoder without exploration
                int t_CssEncode = System.Environment.TickCount;
                for (int i = 0; i < samples.Count; i++)
                {
                    string tmp = CssEncode.Apply(samples[i]);
                }
                t_CssEncode = System.Environment.TickCount - t_CssEncode;
                CssEncodeTimes.Add(t_CssEncode);
                //generated encoder with Boolean exploration
                int t_CssEncode_B = System.Environment.TickCount;
                for (int i = 0; i < samples.Count; i++)
                {
                    string tmp = CssEncode_B.Apply(samples[i]);
                }
                t_CssEncode_B = System.Environment.TickCount - t_CssEncode_B;
                CssEncodeTimes_B.Add(t_CssEncode_B);
                //generated encoder with Full exploration
                int t_CssEncode_F = System.Environment.TickCount;
                for (int i = 0; i < samples.Count; i++)
                {
                    string tmp = CssEncode_F.Apply(samples[i]);
                }
                t_CssEncode_F = System.Environment.TickCount - t_CssEncode_F;
                CssEncodeTimes_F.Add(t_CssEncode_F);
            }
            //compute the average times
            int antiXssTime = ComputeAverage(antiXssTimes);
            int CssEncodeTime = ComputeAverage(CssEncodeTimes);
            int CssEncodeTime_B = ComputeAverage(CssEncodeTimes_B);
            int CssEncodeTime_F = ComputeAverage(CssEncodeTimes_F);

            double[] stdevs = CombinedStandardDeviation(antiXssTimes, CssEncodeTimes, CssEncodeTimes_B, CssEncodeTimes_F);
            Console.WriteLine("antiXssTime={0}, CssEncodeTime={1}, CssEncodeTime_B={2}, CssEncodeTime_F={3}, stddvAntiXSS={4}, stddvCssEncode={5}, stddvCssEncodeB={6}, stddvCssEncodeF={7}",
                               antiXssTime, CssEncodeTime, CssEncodeTime_B, CssEncodeTime_F, stdevs[0], stdevs[1], stdevs[2], stdevs[3]);
        }

        private int ComputeAverage(List<int> times)
        {
            int tot = 0;
            foreach (int t in times)
                tot += t;
            int res = tot / times.Count;
            return res;
        }

        double ComputeStandardDeviation(List<int> times)
        {
            double mean = (double)ComputeAverage(times);
            double sum = 0;
            foreach (var t in times)
                sum += (((double)t) - mean) * (((double)t) - mean);
            double d = sum / ((double)(times.Count - 1));
            double s = Math.Sqrt(d);
            return s;
        }

        double[] CombinedStandardDeviation(params List<int>[] times)
        {
            
            double[] sdevs = new double[times.Length];
            for (int i = 0; i < times.Length; i++)
                sdevs[i] = ComputeStandardDeviation(times[i]);

            return sdevs; 
        }
    }

    /*
     * 
     *  The following are automatically generated using the above unit test: TestUTF8EncodeCSgen
     * 
     */

    //no exploration
    public static class UTF8Encode
    {
        public static string Apply(string input)
        {
            var output = new char[(input.Length * 3) + 0];
            bool r0 = false; int r1 = 0;
            int pos = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                if (r0)
                {
                    if (((56320 > c) || (c > 57343)))
                    {
                        throw new Exception("InvalidSurrogatePairException");
                    }
                    else
                    {
                        output[pos++] = ((char)(((c >> 6) & 15) | ((r1 << 4) | 128))); output[pos++] = ((char)((c & 63) | 128));
                        r0 = false; r1 = 0;
                    }
                }
                else
                {
                    if ((c <= 127))
                    {
                        output[pos++] = ((char)c);
                    }
                    else
                    {
                        if ((c <= 2047))
                        {
                            output[pos++] = ((char)(((c >> 6) & 31) | 192)); output[pos++] = ((char)((c & 63) | 128));
                        }
                        else
                        {
                            if (((55296 > c) || (c > 56319)))
                            {
                                if (((56320 <= c) && (c <= 57343)))
                                {
                                    throw new Exception("InvalidSurrogatePairException");
                                }
                                else
                                {
                                    output[pos++] = ((char)(((c >> 12) & 15) | 224)); output[pos++] = ((char)(((c >> 6) & 63) | 128)); output[pos++] = ((char)((c & 63) | 128));
                                }
                            }
                            else
                            {
                                output[pos++] = ((char)((((1 + ((c >> 6) & 15)) >> 2) & 7) | 240)); output[pos++] = ((char)(((c >> 2) & 15) | ((((1 + ((c >> 6) & 15)) & 3) << 4) | 128)));
                                r0 = true; r1 = (c & 3);
                            }
                        }
                    }
                }
            }
            if (r0)
            {
                throw new Exception("InvalidSurrogatePairException");
            }
            else
            {

            }
            return new String(output, 0, pos);
        }
    }

    ////boolean exploration
    public static class UTF8Encode_B
    {
        public static string Apply(string input)
        {
            var output = new char[(input.Length * 3) + 0];
            int r0 = 0; int state = 0;
            int pos = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                switch (state)
                {
                    case (0):
                        {
                            if ((c <= 127))
                            {
                                output[pos++] = ((char)c);
                                state = 0;
                            }
                            else
                            {
                                if ((c <= 2047))
                                {
                                    output[pos++] = ((char)(((c >> 6) & 31) | 192)); output[pos++] = ((char)((c & 63) | 128));
                                    state = 0;
                                }
                                else
                                {
                                    if (((55296 > c) || (c > 56319)))
                                    {
                                        if (((56320 <= c) && (c <= 57343)))
                                        {
                                            throw new Exception("InvalidSurrogatePairException");
                                        }
                                        else
                                        {
                                            output[pos++] = ((char)(((c >> 12) & 15) | 224)); output[pos++] = ((char)(((c >> 6) & 63) | 128)); output[pos++] = ((char)((c & 63) | 128));
                                            state = 0;
                                        }
                                    }
                                    else
                                    {
                                        output[pos++] = ((char)((((1 + ((c >> 6) & 15)) >> 2) & 7) | 240)); output[pos++] = ((char)(((c >> 2) & 15) | ((((1 + ((c >> 6) & 15)) & 3) << 4) | 128)));
                                        r0 = (c & 3);
                                        state = 1;
                                    }
                                }
                            }
                            break;
                        }
                    case (1):
                        {
                            if (((56320 > c) || (c > 57343)))
                            {
                                throw new Exception("InvalidSurrogatePairException");
                            }
                            else
                            {
                                output[pos++] = ((char)(((c >> 6) & 15) | ((r0 << 4) | 128))); output[pos++] = ((char)((c & 63) | 128));
                                r0 = 0;
                                state = 0;
                            }
                            break;
                        }
                }
            }
            if (state == 0)
            {

                state = 0;
            }
            else if (state == 1)
            {
                throw new Exception("InvalidSurrogatePairException");
            }
            return new String(output, 0, pos);
        }
    }
}
