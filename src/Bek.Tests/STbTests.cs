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

namespace Microsoft.Bek.Query.Tests
{
    [TestClass]
    public class STbTests
    {

        static string sampleDir = "../../../Samples/";

        [TestMethod]
        public void TestTrivial()
        {
            string pgm = "program trivial(t) { return iter(c in t) {case (true): yield (c);}; }";
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekToSTb(solver, pgm);
            var st1 = stb.ToST();
            //stb.ShowGraph();
            //st1.ShowGraph();
            var st = st1.Explore();
            //st.ShowGraph();
            Assert.AreEqual<int>(2, st.MoveCount); //note: one of the moves is a final output
            Assert.AreEqual<int>(1, st.StateCount);
            Assert.AreEqual<int>(0, st.InitialState);
            var moves = new List<Move<Rule<Expr>>>(st.GetNonFinalMovesFrom(0));
            Assert.AreEqual<int>(1, moves.Count);
            //Assert.AreEqual<Expr>(solver.True, moves[0].Condition.Guard);
        }

        [TestMethod]
        public void TestIteCases()
        {
            string pgm = @"
function hex(x)=(x + 4);
program dummy(t) { 
  return iter(c in t) {
    case (c > 'z'): 
      yield (hex(c));
    case (c < '9'):
      yield (c,c);
    case (c < 'z'):
      yield ((c+c));
    }end{
    case(true):
      yield('x','x');
    }; 
}";
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekToSTb(solver, pgm);
            //stb.ShowGraph();
            //stb.ToST().ShowGraph();
            //stb.ToST().Explore().ShowGraph();
        }

        [TestMethod]
        public void TestRaiseStmt()
        {
            string pgm = @"
program dummy(t) { 
  return iter(c in t) [r := 0;]{
    case (c > 'z'): 
      r := 0;
      yield (c + 1);
    case (c < '9'):
      raise E1;
    case (c < 'z'):
      r := 1;
      yield (c + c);
    case (true):
      raise E2;
    }end{
    case(r == 0):
      yield('h','e','l','l','o');
    case(true):
      raise E3;
    }; 
}";
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekToSTb(solver, pgm);
            //stb.ShowGraph();
            //stb.ToST().ShowGraph();
            //stb.ToST().Explore().ShowGraph();
        }

        //[TestMethod]
        public void TestCssEncode6()
        {
            var solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode.bek");
            //stb.ShowGraph();
            // stb.ShowGraph(10);
            //stb.Explore().ShowGraph();
            var st = stb.ToST();
            //st.ShowGraph(10);
            var sft = st.Explore();
            //sft.ShowGraph(10);

            //just to get longer input strings
            var restr = sft.RestrictDomain("^[^\0-\x32]*$").RestrictDomain("^(.....).*$");

            //restr.ShowGraph(10);

            restr.AssertTheory();

            Expr inputConst = solver.MkFreshConst("input", sft.InputListSort);
            Expr outputConst = solver.MkFreshConst("output", sft.OutputListSort);

            solver.MainSolver.Assert(restr.MkAccept(inputConst, outputConst));

            int okCnt = 0;
            int error0Cnt = 0;
            int error1Cnt = 0;

            //validate correctness for some values against the actual CssEncode
            //validate also exceptional behavior
            int K = 3;
            for (int i = 0; i < K; i++)
            {
                var model = solver.MainSolver.GetModel(solver.True, inputConst, outputConst);
                string input = model[inputConst].StringValue;
                string output = model[outputConst].StringValue;
                Assert.IsFalse(string.IsNullOrEmpty(output));
                char lastChar = output[output.Length - 1];

                try
                {
                    var output_expected = System.Web.Security.AntiXss.AntiXssEncoder.CssEncode(input);
                    Assert.AreEqual<string>(output_expected, output);
                    okCnt += 1;
                }
                catch (Exception)
                {
                    Assert.AreEqual<char>('\0', lastChar);
                    error0Cnt += 1;
                }

                //exclude this solution, before picking the next one
                solver.MainSolver.Assert(solver.MkNeq(inputConst, model[inputConst].Value));
            }
            Assert.AreEqual(K, okCnt);
        }

        //[TestMethod]
        public void TestCssEncode5()
        {
            var solver = new Z3Provider();
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode5.bek");
            //stb.Explore().ShowGraph();
            var st = stb.ToST();
            //st.ShowGraph(10);
            var sft = st.Explore();
            //sft.ShowGraph(10);

            //just to get longer input strings
            var restr = sft.RestrictDomain("(.){3,}$");

            restr.Simplify();
            //restr.ShowGraph(10);

            restr.AssertTheory();

            Expr inputConst = solver.MkFreshConst("input", sft.InputListSort);
            Expr outputConst = solver.MkFreshConst("output", sft.OutputListSort);

            solver.MainSolver.Assert(restr.MkAccept(inputConst, outputConst));

            int okCnt = 0;
            int error0Cnt = 0;
            int error1Cnt = 0;

            //validate correctness for some values against the actual CssEncode
            //TBD: validate also exceptional behavior
            int K = 10;
            for (int i = 0; i < K; i++)
            {
                var model = solver.MainSolver.GetModel(solver.True, inputConst, outputConst);
                string input = model[inputConst].StringValue;
                string output = model[outputConst].StringValue;

                Assert.IsFalse(string.IsNullOrEmpty(input));
                Assert.IsFalse(string.IsNullOrEmpty(output));
                if ((input != ""))
                {
                    char lastChar = '\0'; //output[output.Length - 1];

                    try
                    {
                        var output_expected = System.Web.Security.AntiXss.AntiXssEncoder.CssEncode(input);
                        Assert.AreEqual<string>(output_expected, output);
                        okCnt += 1;
                    }
                    catch (Exception)
                    {
                        Assert.AreEqual<char>('\0', lastChar);
                        error0Cnt += 1;
                    }
                }
                //exclude this solution, before picking the next one
                solver.MainSolver.Assert(solver.MkNeq(inputConst, model[inputConst].Value));
            }
            Assert.AreEqual(K, okCnt);
        }

        [TestMethod]
        public void TestRaise()
        {
            string pgm = @"
program dummy(t) { 
  return iter(c in t) {
    case (c == 'a'): 
      yield ('A');
   case (c == 'b'): 
      raise B;
   case (c == 'x'):
      yield ('X');
    } end {
   case (true):
      yield ('x');
};
}";
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekToSTb(solver, pgm);
            //stb.ShowGraph();
            var st = stb.ToST();
            //st.ShowGraph();
            //st.Explore().ShowGraph();

            st.AssertTheory();

            Expr inputConst1 = solver.MkFreshConst("input1", st.InputSort);
            Expr inputConst2 = solver.MkFreshConst("input2", st.InputSort);
            Expr inputConst = solver.MkList(inputConst1, inputConst2);
            Expr outputConst = solver.MkFreshConst("output", st.OutputListSort);

            solver.MainSolver.Assert(st.MkAccept(inputConst, outputConst));
            var model = solver.MainSolver.GetModel(solver.True, inputConst, outputConst);
            string input = model[inputConst].StringValue;
            string output = model[outputConst].StringValue;
            Console.WriteLine(input + "," + output);
        }

        [TestMethod]
        public void TestCssEncode4eq5()
        {
            var solver = new Z3Provider();
            var stb5 = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode5.bek");
            var stb4 = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode4.bek");
            //stb5.ShowGraph(10);
            var fst4 = stb4.ToST().Explore();
            var fst5 = stb5.ToST().Explore();
            var dom4 = fst4.ToSFA();
            var dom5 = fst5.ToSFA();
            bool dom4_subsetof_dom5 = dom4.IsSubsetOf(dom5);
            //var tmp = dom4.Minus(dom5);
            //tmp.ShowGraph();
            //var member = new List<Expr>(tmp.ChoosePathToSomeFinalState()).ToArray();
            //var str = new String(Array.ConvertAll(member, m => solver.GetCharValue(solver.FindOneMember(m).Value)));

            bool dom5_subsetof_dom4 = dom5.IsSubsetOf(dom4);
            bool partial_equiv = fst4.Eq1(fst5);
            Assert.IsTrue(dom4_subsetof_dom5);
            Assert.IsTrue(partial_equiv);
            Assert.IsTrue(dom5_subsetof_dom4);
        }

        //[TestMethod]
        //public void TestCssEncode5eq6()
        //{
        //    var solver = new Z3Provider();
        //    var stb4 = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode5.bek");
        //    var stb6 = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode6.bek");
        //    //stb6.ShowGraph(10);
        //    var fst4 = stb4.Explore();
        //    var fst6 = stb6.Explore();
        //    //fst6.ShowGraph();
        //    //fst6.ToST().ToSFA().ShowGraph(5);
        //    CheckFullEquivalence(fst4, stb6);
        //}

        private static void CheckFullEquivalence(STbModel A, STbModel B)
        {
            CheckFullEquivalence(A.ToST(), B.ToST());
        }

        private static void CheckFullEquivalence(STModel A, STModel B)
        {
            var dom4 = A.ToSFA();
            //dom4.ShowGraph(10);
            var dom6 = B.ToSFA();
            //dom6.ShowGraph(10);
            bool dom4_subsetof_dom6 = dom4.IsSubsetOf(dom6);
            bool dom6_subsetof_dom4 = dom6.IsSubsetOf(dom4);
            bool partial_equiv = A.Eq1(B);
            Assert.IsTrue(dom4_subsetof_dom6);
            Assert.IsTrue(partial_equiv);
            Assert.IsTrue(dom6_subsetof_dom4);
        }

        [TestMethod]
        public void TestIfThenElseStmt()
        {
            string pgm = @"
program IfThenElseSampleTmp(t) { 
  return iter(c in t) [r := 0;]{
    case (c > 10): 
      if (c < 20) {
        yield (c+1);
      } else {
        yield (c-1);
        r := 1;
      }
    case(true):
      raise X;
    } end {
    case (r == 0):
      raise Y;
    case (true):
      yield ();
    };
}";
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekToSTb(solver, pgm);
            //stb.Explore().ShowGraph();
            //stb.ShowGraph();
            //stb.ToST().ShowGraph();
            //stb.Explore().ShowGraph();
        }



        [TestMethod]
        public void TestExplore1()
        {
            string pgm = @"
program IfThenElseSample(t) { 
  return iter(c in t) [r := 0; s := 0;]{
    case ((c >= 'A') && (c <= 'B')): 
      if (c < 'B') {
        yield (c+1);
      } else {
        yield (c,c);
        r := 1;
      }
    case(true):
      raise E1;
    } end {
    case (r == 0):
      raise E2;
    case (true):
      yield ();
    };
}";
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekToSTb(solver, pgm);
            //stb.ToST().ShowGraph(1);
            var sft = stb.Explore();
            //sft.ToST().ShowGraph(1);
            Assert.AreEqual<int>(2, sft.StateCount);
            CheckFullEquivalence(sft.ToST(), stb.ToST());
        }

        [TestMethod]
        public void TestExplore2()
        {
            var solver = new Z3Provider();
            var stb5 = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode5.bek");
            var stb6 = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode4.bek");
            //stb6.ShowGraph(10);
            var fst6 = stb6.Explore();
            //fst6.Name = "CssEncode6";
            //fst6.ShowGraph(20);
            var fst5 = stb5.Explore();
            //fst5.Name = "CssEncode5";
            //fst5.ShowGraph(20);
            CheckFullEquivalence(fst5.ToST(), fst6.ToST());
        }

        [TestMethod]
        public void TestSFTwithPC()
        {
            var solver = new Z3Provider();
            var d = System.Environment.CurrentDirectory;
            var stb = BekConverter.BekFileToSTb(solver, sampleDir + "bek/decode.bek").ExploreBools();
            //stb.ShowGraph(10);
        }

        [TestMethod]
        public void TestExploreBools()
        {
            var solver = new Z3Provider();
            var stb5 = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode5.bek");
            var stb6 = BekConverter.BekFileToSTb(solver, sampleDir + "bek/CssEncode4.bek");
            //stb6.ShowGraph(10);
            var fst6 = stb6.ExploreBools();
            //fst6.ShowGraph();
            var fst5 = stb5.ExploreBools();
            //fst5.ShowGraph();
            //fst5.Name = "CssEncode5";
            //fst5.ShowGraph(20);
            CheckFullEquivalence(fst5.ToST(), fst6.ToST());
        }


        [TestMethod]
        public void TestRoundtrip1()
        {
            string pgm = @"
program IfThenElseSample(t) { 
  return iter(c in t) [r := 0;]{
    case (c > 10): 
      if (c < 20) {
        yield (c+1);
      } else {
        yield (c-1);
        r := 1;
      }
    case(true):
      raise X;
    } end {
    case (r == 0):
      raise Y;
    case (true):
      yield ();
    };
}";
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekToSTb(solver, pgm);
            StringBuilder sb = new StringBuilder();
            stb.ToBek(sb);
            var stb2 = BekConverter.BekToSTb(solver, sb.ToString());
            //stb2.ShowGraph();
            CheckFullEquivalence(stb.ToST(), stb2.ToST());
            //var sft = stb.Explore();
            //StringBuilder sb2 = new StringBuilder();
            //sft.ToBek(sb2);
            //var sft2 = BekConverter.BekToSTb(solver, sb2.ToString());
            ////StreamWriter sw = new StreamWriter("tmp.bek");
            ////sft2.ToBek(sw);
            ////sw.Close();
            ////sft2.ShowGraph();
            //CheckFullEquivalence(stb.ToST(), sft2.ToST());
        }

        [TestMethod]
        public void Utf8sample()
        {
            // Create a UTF-8 encoding.
            UTF8Encoding utf8 = new UTF8Encoding();

            // A Unicode string with two characters outside an 8-bit code range.
            String unicodeString =
                "Pi\u03a0";//and Sigma (\u03a3).";
            Console.WriteLine("Original string:");
            Console.WriteLine(unicodeString);

            // Encode the string.
            Byte[] encodedBytes = utf8.GetBytes(unicodeString);
            Console.WriteLine();
            Console.WriteLine("Encoded bytes:");
            foreach (Byte b in encodedBytes)
            {
                Console.Write("[{0}]", b);
            }
            Console.WriteLine();

            // Decode bytes back to string.
            // Notice Pi and Sigma characters are still present.
            String decodedString = utf8.GetString(encodedBytes);
            Console.WriteLine();
            Console.WriteLine("Decoded bytes:");
            Console.WriteLine(decodedString);
        }

        [TestMethod]
        public void TestParallelUpdateSemantics()
        {
            string pgm = @"
program swap01(t) { 
  return iter(c in t) [f := 0; s := 1;]{
    case ((50 <= c) && (c <= 100)): //restrict to these values
      if (f == 0) {
        f := s;
        s := f;
        yield (f,s,c);
      } else {
        f := s;
        s := f;
        yield (f,s,c);
      }
    };
}";

            string pgm2 = @"
program swap01b(t) { 
  return iter(c in t) [f := false;]{
    case ((50 <= c) && (c <= 100)): //restrict to these values
      if (!f) {
        f := !f;
        yield (0,1,c);
      } else {
        f := !f;
        yield (1,0,c);
      }
    };
}";
            Z3Provider solver = new Z3Provider();
            var stb = BekConverter.BekToSTb(solver, pgm);
            var stb2 = BekConverter.BekToSTb(solver, pgm2);
            CheckFullEquivalence(stb, stb2);
        }

        [TestMethod]
        public void TestSTbComposition0()
        {
            string pgm = @"
program A(t) { 
  return iter(c in t)[q := 0;] {
    case ((q == 0) && (c == 'a')): 
        yield (c); 
        q:= 1;
   case ((q == 1) && (c == 'a')): 
      yield (c+1);
      q := 0;
  case ((q == 1) && (c == 'b')): 
      yield (c); 
      q:= 1;
  case (true):
      raise error;
    } end {
   case (q == 1):
      raise error;
};
}";
            Z3Provider solver = new Z3Provider();
            var A = BekConverter.BekToSTb(solver, pgm).ExploreBools();
            var AA = A.Compose(A);
            var AAs = AA.Simplify();

            var A_exe = A.Compile();
            var s = A_exe.Apply("aaaa");
            Assert.AreEqual<string>("abab", s);
        }

        [TestMethod]
        public void TestSTbComposition1()
        {
            string pgm = @"
program A(t) { 
  return iter(c in t)[r := 0;] {
    case (c in ""[0-9]""): 
        yield (c,c); 
        r:= (r+1)%10;
   case (true): 
      yield();
    } end {
   case (true):
      yield ('-', r + '0');
};
}";
            Z3Provider solver = new Z3Provider();
            var A = BekConverter.BekToSTb(solver, pgm);
            var AA = A.Compose(A);

            var AA_exe = AA.Compile();
            var s = AA_exe.Apply("0123");
            Assert.AreEqual<string>("000011112222333344-9",s);
        }

        [TestMethod]
        public void TestSTbComposition2()
        {
            string pgm = @"
program A(t) { 
  return iter(c in t)[b := false; r := 0;] {
    case (b): 
      if (r == c) {yield();}
      else {yield(c); r:= c;}
    case (true):
      r := c; b := true; 
      yield(c);
    } end {
   case (true):
      yield ();
};
}";
            Z3Provider solver = new Z3Provider();
            var A = BekConverter.BekToSTb(solver, pgm).ExploreBools();
            //A.ShowGraph();
            var AA = A.Compose(A);
            //AA.ShowGraph();

            var A_exe = A.Compile();
            var s = A_exe.Apply("00001111122333");
            Assert.AreEqual<string>("0123", s);

            var AA_exe = AA.Compile();
            var s2 = AA_exe.Apply("00001111122333");
            Assert.AreEqual<string>("0123", s2);
        }

        [TestMethod]
        public void TestSTbComposition3()
        {
            string pgm = @"
function HS(x) = ((55296 <= x) && (x <= 56319));
function LS(x) = ((56320 <= x) && (x <= 57343));
program Fix(t) { 
  return iter(c in t)[q0 := true; r := 0;] {
    case (q0): 
      if (HS(c)) {r := c;q0:=false;}
      else {
        if (LS(c)) { yield(0xFFFD); r:=0;}
        else {yield(c); r:=0;}
      }
    case (true):
      if (HS(c)) {yield(0xFFFD); r := c;}
      else { 
        if (LS(c)) {yield(r,c); r:=0; q0:=true;}
        else {yield(0xFFFD,c); r:=0; q0:=true;}
      }
    } end {
   case (!q0):
      yield (0xFFFD);
};
}";
            Z3Provider solver = new Z3Provider();
            var A = BekConverter.BekToSTb(solver, pgm).ExploreBools();
            //A.ShowGraph();
            var AA = A.Compose(A);
            var AAAA = AA.Compose(AA);
            //AA.ShowGraph();
            var C = AAAA.Simplify(3);
           // AAs.ShowGraph();
        }


        [TestMethod]
        public void TestSTbSimplify0()
        {
            string pgm = @"
program test(input){
  return iter(c in input)[q := true; r := 0;]
  {
    case (q): 
      r := r + c;
      q := false;
    case (!q):
      r := 0;
      q := true;
    end case (true):
      yield(r);
  };
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var dec = BekConverter.BekToSTb(solver, pgm);
            var st = dec.ExploreBools();
            var sts = st.Simplify();
            Assert.AreEqual<int>(2, st.StateCount);
            var st_st = st.Compose(st).ExploreBools();
            Assert.AreEqual<int>(2, st_st.StateCount);
        }

        [TestMethod]
        public void TestSTbSimplify()
        {

               string utf8decode_bek = @"
function fuse(r,c) = ((r << 6) | (c & 0x3F));
function one(c) = ((0 <= c) && (c <= 0x7F));
function C2_DF(c) = ((0xC2 <= c) && (c <= 0xDF));
function E1_EF(c) = ((0xE1 <= c) && (c <= 0xEF));
function A0_BF(c) = ((0xA0 <= c) && (c <= 0xBF));
function x80_BF(c) = ((0x80 <= c) && (c <= 0xBF));
function x80_9F(c) = ((0x80 <= c) && (c <= 0x9F));
program utf8decode(input){
  return iter(c in input)[q := 0; r := 0;] 
  {
    case (q == 0):
      if (one(c))                  {yield (c);}
      else if (C2_DF(c))           {q := 2; r := (c & 0x1F);}    // ------ 2 bytes --------
      else if (c == 0xE0)          {q := 4; r := (c & 0x0F);}    // ------ 3 bytes --------
      else if (c == 0xED)          {q := 5; r := (c & 0x0F);}    // ------ 3 bytes --------
      else if (E1_EF(c))           {q := 3; r := (c & 0x0F);}    // ------ 3 bytes --------
      else {raise InvalidInput;}

    case (q == 2): 
      if (x80_BF(c))                 {q := 0; yield(fuse(r,c)); r := 0;}
      else {raise InvalidInput;}

    case (q == 3): 
      if (x80_BF(c))                 {q := 2; r := fuse(r,c);}
      else {raise InvalidInput;}

    case (q == 4): 
      if (A0_BF(c))                  {q := 2; r := fuse(r,c);}
      else {raise InvalidInput;}

    case (q == 5): 
      if (x80_9F(c))                 {q := 2; r := fuse(r,c);}
      else {raise InvalidInput;}

    end case (!(q == 0)):
      raise InvalidInput;
  }; 
}
";

            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var dec = BekConverter.BekToSTb(solver, utf8decode_bek);
            var utf8decode = dec.ExploreBools();

            Sort bv32 = solver.MkBitVecSort(32);
            Sort outSort = solver.MkTupleSort(solver.StringSort, bv32);
            var initReg = solver.MkTuple(solver.GetNil(solver.StringSort), solver.MkNumeral(0,bv32));
            var regVar = solver.MkVar(1,outSort);
            var reg1 = solver.MkProj(1, regVar);
            var reg0 = solver.MkProj(0, regVar);
            STb<FuncDecl, Expr, Sort> parse = new STbModel(solver, "Parse", solver.CharacterSort, outSort, outSort, initReg, 0);

            var letter = solver.MkOr(//solver.MkAnd(solver.MkCharLe(solver.MkCharExpr('\xC0'), solver.CharVar),
                                     //                    solver.MkCharLe(solver.CharVar, solver.MkCharExpr('\xFF'))),
                                     solver.MkAnd(solver.MkCharLe(solver.MkCharExpr('a'), solver.CharVar),
                                                         solver.MkCharLe(solver.CharVar, solver.MkCharExpr('z'))),
                                     solver.MkAnd(solver.MkCharLe(solver.MkCharExpr('A'), solver.CharVar),
                                                         solver.MkCharLe(solver.CharVar, solver.MkCharExpr('Z'))));

            //var not_letter = solver.MkNot(letter);

            var digit = solver.MkAnd(solver.MkCharLe(solver.MkCharExpr('0'), solver.CharVar),
                                                         solver.MkCharLe(solver.CharVar, solver.MkCharExpr('9')));

            var nl = solver.MkEq(solver.CharVar, solver.MkCharExpr('\n'));

            var space = solver.MkEq(solver.CharVar, solver.MkCharExpr(' '));

            //var not_nl = solver.MkNot(nl);

            var _0 = solver.MkNumeral((int)'0', bv32);

            //var z = solver.Z3.MkFreshConst("z", solver.CharacterSort);

            //var constr = solver.MkNot(solver.Z3.MkExists(new Expr[] { z }, nl.Substitute(solver.CharVar, z)));
            ////var constr = nl.Substitute(solver.CharVar, z);

            //solver.Z3S.Push();
            //solver.Z3S.Assert((BoolExpr)solver.MkNot(constr));
            //var status = solver.Check();
            //var m = solver.Z3S.Model;
            //var zval = m.Evaluate(z, true);
            //solver.Z3S.Pop();


            var loop_0 = new BaseRule<Expr>(Sequence<Expr>.Empty, regVar, 0);
            var brule0_1 = new BaseRule<Expr>(Sequence<Expr>.Empty, solver.MkTuple(solver.MkListCons(solver.CharVar, reg0), reg1), 1);
            var rule0 = new IteRule<Expr>(letter, brule0_1, new IteRule<Expr>(space, loop_0, UndefRule<Expr>.Default));
            parse.AssignRule(0, rule0);

            var brule1_2 = new BaseRule<Expr>(Sequence<Expr>.Empty, solver.MkTuple(solver.MkListCons(solver.CharVar, reg0), reg1), 2);
            var brule_4 = new BaseRule<Expr>(Sequence<Expr>.Empty, regVar, 4);
            var rule1 = new IteRule<Expr>(letter, brule1_2, new IteRule<Expr>(space, brule_4, UndefRule<Expr>.Default));
            parse.AssignRule(1, rule1);

            var brule2_3 = new BaseRule<Expr>(Sequence<Expr>.Empty, solver.MkTuple(solver.MkListCons(solver.CharVar, reg0), reg1), 4);
            var rule2 = new IteRule<Expr>(letter, brule2_3, new IteRule<Expr>(space, brule_4, UndefRule<Expr>.Default));
            parse.AssignRule(2, rule2);

            var bv32var = solver.Z3.MkZeroExt(16, (BitVecExpr)solver.CharVar);

            var brule4_5 = new BaseRule<Expr>(Sequence<Expr>.Empty, solver.MkTuple(reg0, solver.MkBvAdd(
                solver.MkBvMul(solver.MkNumeral(10,bv32),reg1),
                solver.MkBvSub(bv32var,_0))),5);
            var rule4 = new IteRule<Expr>(digit, brule4_5, new IteRule<Expr>(space, brule_4, UndefRule<Expr>.Default));
            parse.AssignRule(4, rule4);

            var brule_0 = new BaseRule<Expr>(Sequence<Expr>.Empty.Append(regVar), initReg, 0);

            var brule_7 = new BaseRule<Expr>(Sequence<Expr>.Empty, regVar, 7);

            var brule5_6 = new BaseRule<Expr>(Sequence<Expr>.Empty, solver.MkTuple(reg0, solver.MkBvAdd(
                solver.MkBvMul(solver.MkNumeral(10, bv32), reg1),
                solver.MkBvSub(bv32var, _0))), 6);
            var rule5 = new IteRule<Expr>(digit, brule5_6, new IteRule<Expr>(nl, brule_0, new IteRule<Expr>(space, brule_7, UndefRule<Expr>.Default)));
            parse.AssignRule(5, rule5);

            var brule6_7 = new BaseRule<Expr>(Sequence<Expr>.Empty, solver.MkTuple(reg0, solver.MkBvAdd(
                solver.MkBvMul(solver.MkNumeral(10, bv32), reg1),
                solver.MkBvSub(bv32var, _0))), 7);
            var rule6 = new IteRule<Expr>(digit, brule6_7, new IteRule<Expr>(nl, brule_0, new IteRule<Expr>(space, brule_7, UndefRule<Expr>.Default)));
            parse.AssignRule(6, rule6);

            var rule7 = new IteRule<Expr>(nl, brule_0, new IteRule<Expr>(space, brule_7, UndefRule<Expr>.Default));
            parse.AssignRule(7, rule7);

            parse.AssignFinalRule(0, new BaseRule<Expr>(Sequence<Expr>.Empty, initReg, 0));

            var comp = utf8decode.Compose(parse);

            //utf8decode.ToST().ShowGraph();

            //parse.ToST().ShowGraph();

            //comp.ToST().ShowGraph();
             
            //var comp1 = new STbSimulator<FuncDecl,Expr,Sort>(comp);
            //comp1.Explore();
            //Console.WriteLine(comp1.exploredSteps);

            //var rules = Array.ConvertAll(comp1.UncoveredMoves.ToArray(), r => new Tuple<int, int>(r.Item3.SourceState, r.Item3.TargetState));

            var simpl = comp.Simplify();

            Assert.AreEqual<int>(35, comp.StateCount);
            Assert.AreEqual<int>(7, simpl.StateCount);

            //simpl.ShowGraph();
        }


        [TestMethod]
        public void TestSTbFromRegex()
        {
            Z3Provider solver = new Z3Provider(BitWidth.BV8);
            STbFromRegexBuilder<FuncDecl, Expr, Sort> builder = new STbFromRegexBuilder<FuncDecl, Expr, Sort>(solver);
            var res = builder.Mk(@"\s*(?<i>\d{2,3})\s(?<b>\w{4,5}),(?<le>\d+)(?<la>[a-z]{2,})\s*", "i", "int", "b", "bool", "le", "length", "la", "last");
            //res.ToST().ShowGraph();
        }

        [TestMethod]
        public void TestSTbFromRegex2()
        {
            Z3Provider solver = new Z3Provider(BitWidth.BV8);
            STbFromRegexBuilder<FuncDecl, Expr, Sort> builder = new STbFromRegexBuilder<FuncDecl, Expr, Sort>(solver);
            var res = builder.Mk(@"([^,]*,){2}(?<int>\d+),(?<bool>(true|false))", "int", "int", "bool", "bool");
            //res.ToST().ShowGraph();
        }


        [TestMethod]
        public void TestSTbFromRegexLoop()
        {
            Z3Provider solver = new Z3Provider(BitWidth.BV8);
            STbFromRegexBuilder<FuncDecl, Expr, Sort> builder = new STbFromRegexBuilder<FuncDecl, Expr, Sort>(solver);
            var res = builder.Mk(@"(\s*(?<i>\d{2,3})\s(?<b>\w{4,5}),(?<le>\d+)(?<la>[a-z]{2,})[\s-[\n]]*\n)*", "i", "int", "b", "bool", "le", "length", "la", "last");
            //res.ToST().ShowGraph();
        }

        [TestMethod]
        public void TestSTb_ParseInteger() 
        {
            Z3Provider solver = new Z3Provider(BitWidth.BV8);
            STbFromRegexBuilder<FuncDecl, Expr, Sort> builder = new STbFromRegexBuilder<FuncDecl, Expr, Sort>(solver);
            var res = builder.Mk(@"(?<i>\d+)", "i", "int");
            res.Name = "ParseInteger";
            //res.ToST().ShowGraph();
        }

        [TestMethod]
        public void TestSTb_ParseIntegers()
        {
            Z3Provider solver = new Z3Provider(BitWidth.BV8);
            STbFromRegexBuilder<FuncDecl, Expr, Sort> builder = new STbFromRegexBuilder<FuncDecl, Expr, Sort>(solver);
            var res = builder.Mk(@"((?<i>\d+),)*", "i", "int");
            res.Name = "ParseIntegers";
            //res.ToST().ShowGraph();
        }

        [TestMethod]
        public void TestSTb_ParseCommaSeparatedIntegers()
        {
            Z3Provider solver = new Z3Provider(BitWidth.BV8);
            STbFromRegexBuilder<FuncDecl, Expr, Sort> builder = new STbFromRegexBuilder<FuncDecl, Expr, Sort>(solver);
            var res = builder.Mk(@"((?<i>\d+),)*", "i", "int");
            res.Name = "ParseCommaSeparatedIntegers";
            //res.ToST().ShowGraph();
        }


        [TestMethod]
        public void TestSTb_UTf8DecodeAndParse()
        {

            string utf8decode_bek = @"
function fuse(r,c) = ((r << 6) | (c & 0x3F));
function one(c) = ((0 <= c) && (c <= 0x7F));
function C2_DF(c) = ((0xC2 <= c) && (c <= 0xDF));
function E1_EF(c) = ((0xE1 <= c) && (c <= 0xEF));
function A0_BF(c) = ((0xA0 <= c) && (c <= 0xBF));
function x80_BF(c) = ((0x80 <= c) && (c <= 0xBF));
function x80_9F(c) = ((0x80 <= c) && (c <= 0x9F));
program utf8decode(input){
  return iter(c in input)[q := 0; r := 0;] 
  {
    case (q == 0):
      if (one(c))                  {yield (c);}
      else if (C2_DF(c))           {q := 2; r := (c & 0x1F);}    // ------ 2 bytes --------
      else if (c == 0xE0)          {q := 4; r := (c & 0x0F);}    // ------ 3 bytes --------
      else if (c == 0xED)          {q := 5; r := (c & 0x0F);}    // ------ 3 bytes --------
      else if (E1_EF(c))           {q := 3; r := (c & 0x0F);}    // ------ 3 bytes --------
      else {raise InvalidInput;}

    case (q == 2): 
      if (x80_BF(c))                 {q := 0; yield(fuse(r,c)); r := 0;}
      else {raise InvalidInput;}

    case (q == 3): 
      if (x80_BF(c))                 {q := 2; r := fuse(r,c);}
      else {raise InvalidInput;}

    case (q == 4): 
      if (A0_BF(c))                  {q := 2; r := fuse(r,c);}
      else {raise InvalidInput;}

    case (q == 5): 
      if (x80_9F(c))                 {q := 2; r := fuse(r,c);}
      else {raise InvalidInput;}

    end case (!(q == 0)):
      raise InvalidInput;
  }; 
}
";

            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var dec = BekConverter.BekToSTb(solver, utf8decode_bek);
            var utf8decode = dec.ExploreBools();

            STbFromRegexBuilder<FuncDecl, Expr, Sort> builder = new STbFromRegexBuilder<FuncDecl, Expr, Sort>(solver);
            var parse = builder.Mk(@"((?<i>[1-9][0-9]*),)*", "i", "int");

            var comp = utf8decode.Compose(parse);

            //utf8decode.ToST().ShowGraph();

            //parse.ToST().ShowGraph();

            //comp.ToST().ShowGraph();

            var simpl = comp.Simplify();

            //simpl.ToST().ShowGraph();
        }
    }
}
