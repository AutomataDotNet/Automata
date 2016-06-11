using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

using Microsoft.Bek;
using Microsoft.Bek.Frontend;

using Microsoft.Z3;

using Microsoft.Bek.Model;

using Microsoft.Automata.Z3;
using Microsoft.Automata;

using SFAModel = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STBuilderZ3 = Microsoft.Automata.STBuilder<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STModel = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;



namespace Microsoft.Bek.Tests
{
    [TestClass]
    public class MiscQueries
    {
        [TestMethod]
        public void TestLocalFunc()
        {
            string pgm = @"

function bar(x, y) = (y + x);
function foo(x) = bar(x,1);

program incr(w) {
   return iter(c in w) {
      case(((foo(c)-1)==c)&&true||false):
         yield(c + 0 + 1);
      };
}
";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToST(solver, pgm);
            Assert.AreEqual(1, st.StateCount);
            var moves = new List<Move<Rule<Expr>>>(st.GetNonFinalMovesFrom(st.InitialState));
            Assert.AreEqual(1, moves.Count);
            var move = moves[0];
            Assert.AreEqual(solver.True, move.Label.Guard);
        }

        [TestMethod]
        public void TestLocalFuncQuery()
        {
            string pgm = @"

function bar(x, y) = (y + x);
function foo(x) = bar(x,1);

program incr(w) {
   return iter(c in w) {
      case(((foo(c)-1)==c)&&true||false):
         yield(c + 0 + 1);
      };
}
==
js(incr);

";

            StringWriter sw = new StringWriter();
            Pgm.Run(pgm, sw, "bek_master.html");
            string s = sw.ToString();
            Console.WriteLine(s);
            Assert.IsTrue(s.Contains("foo"));
        }

        [TestMethod]
        public void BasicDisplay()
        {
            string program = @"
function dec(x)=(x-48);
program alpha(w) {
   return iter(c in w) [ b := false; ] {
      case(true):
	     yield(dec(c), ' ');
      };
}

==

display(alpha);";

            StringWriter sw = new StringWriter();
            Pgm.Run(program, sw, "bek_master.html");
            string s = sw.ToString();
            Console.WriteLine(s);
            Assert.IsTrue(s.Contains("Fig_1"));
        }

        [TestMethod]
        public void TestImage()
        {
            string program = @"

program alpha(w) {
   return iter(c in w) {
      case((c == 'h') || (c == 'l')):
	     yield('X');
      case(c == 'o'):
         yield(c);
      case(true): 
         yield('i');
      };
}

==
image(alpha, " + "\"hello\"" + @");
";

            StringWriter sw = new StringWriter();
            Pgm.Run(program, sw,"");
            Assert.IsTrue(sw.ToString().Contains("XiXXo"));

        }

        [TestMethod]
        public void TestCharRange()
        {
            string alpha = @"

program alpha(w) {
   return iter(c in w) {
      case(c in " + "\"[a-z]\"" + @"):
	     yield('x');
      case(true):
         yield(c);
      };
}
";

            string beta = @"

program beta(w) {
   return iter(c in w) {
      case((c >= 'a') && (c <= 'z')):
	     yield('x');
      case (true):
         yield(c);
      };
}
";
            Z3Provider solver = new Z3Provider();
            var aST = BekConverter.BekToST(solver, alpha);
            var bST = BekConverter.BekToST(solver, beta);
            bool equiv = STModel.Eq1(aST, bST);
            Assert.IsTrue(equiv);

            //aST.ShowGraph();
            //bST.ShowGraph();
        }

        [TestMethod]
        public void TestCharIn2()
        {
            string alpha = @"

program alpha(w) {
   return iter(c in w) {
      case(c in " + "\"[a-d]\"" + @"):
	     yield('x');
      case(true):
         yield(c);
      };
}
";

            string beta = @"

program beta(w) {
   return iter(c in w) {
      case((c >= 'a') && (c <= 'z')):
	     yield('y');
      };
}
";
            Z3Provider solver = new Z3Provider();
            var aST = BekConverter.BekToST(solver, alpha);
            var bST = BekConverter.BekToST(solver, beta);
            bool equiv = STModel.Eq1(aST, bST);
            Assert.IsFalse(equiv);

            //aST.ShowGraph();
            //bST.ShowGraph();
        }

        [TestMethod]
        public void TestSanitize2()
        {
            string pgm = @"
program sanitize2(t) {
return iter(c in t)[ b := false; r := '$';]
     {
       case (!b && ((c == '\'') || (c == '\" + "\"" + @"'))) :
	     b := false;
	     yield ('\\', c); 

       case (c == '\\') :
	     b := !b;
	     yield (c);
       
       case (true)  :
	     b := false;
	     yield (c);
     }end{
       case (true) :
         yield (r);
     };
}";
            Z3Provider solver = new Z3Provider();
            var st1 = BekConverter.BekToST(solver, pgm);

            //st1.ShowGraph();

            var st = st1 + st1 + st1 + st1;

            //st.ShowGraph(20);

            var sft = st.ExploreBools();

            //sft.ShowGraph(50);

            //st.Explore().ShowGraph();

            Assert.IsFalse(sft.Eq1(st1));
            var w = sft.Diff(st1);
            Assert.IsNotNull(w);
            string inputStr = w.Input.GetStringValue(false);
            string out1 = w.Output1.GetStringValue(false);
            string out2 = w.Output2.GetStringValue(false);
            Assert.IsTrue(out1.StartsWith(out2));
            Assert.IsTrue(out1.EndsWith("$$$$"));
        }

        [TestMethod]
        public void TestSanitize()
        {
            string pgm = @"
program sanitize(t) {
return iter(c in t)[ b := false; d := '$'; ]
     {
       case (!b && ((c == '\'') || (c == '\" + "\"" + @"'))) :
	     b := false; yield ('\\', c); 
       case (c == '\\') : b := !b; yield (c);     
       case (true)  : b := false; yield (c);
     };
}";
            Z3Provider solver = new Z3Provider();
            var st1 = BekConverter.BekToST(solver, pgm).ExploreBools();

            //st1.ShowGraph();

            var st = st1 + st1;

            st.Simplify();

            //st.ShowGraph();

            var sft = st.ExploreBools(); 

            //sft.ShowGraph();

            Assert.IsTrue(sft.Eq1(st1));

            Assert.AreEqual<int>(2, sft.StateCount);

        }

        [TestMethod]
        public void TestTrivial2()
        {
            string pgm = "program trivial2(t) { return iter(c in t)[a := 'a'; b := 'b';] {case (true): yield (c); }; }";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToST(solver, pgm).Explore();  
            //st.ShowGraph();
            Assert.AreEqual<int>(2, st.MoveCount); //note: one of the moves is a final output
            Assert.AreEqual<int>(1, st.StateCount);
            Assert.AreEqual<int>(0, st.InitialState);
            var moves = new List<Move<Rule<Expr>>>(st.GetNonFinalMovesFrom(0));
            Assert.AreEqual<int>(1, moves.Count);
            Assert.AreEqual<Expr>(solver.True, moves[0].Label.Guard); // (not #1) at this point
        }

        [TestMethod]
        public void TestPreimage()
        {
            string program = @"
program alpha(w) {
   return iter(c in w) {
      case(c == 'Y') :
         yield('H');
      case((c in " + "\"[a-zA-Z ]\"" + @") && (c != 'H')):
	     yield(c);
      end case(true):
        yield(';');
      };
}
==
display(invimage(alpha," + "\"Hello World\"" + @"));
";
            StringWriter sw = new StringWriter();
            Pgm.Run(program, sw, "");
            string s = sw.ToString();
            Assert.IsTrue(s.Contains("Fig_1"));
        }

        [TestMethod]
        public void TestOutput()
        {
            string program = @"

program alpha(w) {
   return iter(c in w) [ b := false; ] {
      case(c in " + "\"[a-zA-Z ]\"" + @"):
	     yield(c);
      end case(true):
        yield(';');
      };
}

==
display(inv(alpha, " + "\"Hello World;\"" + @"));";

            StringWriter sw = new StringWriter();
            Pgm.Run(program, sw,"");
            string s = sw.ToString();
            Assert.IsTrue(s.Contains("Fig_1"));
        }

        [TestMethod]
        public void TestEndCase()
        {
            string program = @"

program alpha(w) {
   return iter(c in w) [ b := false; ] {
       case(c in " + "\"[a-zA-Z ]\"" + @"):
	     yield(c);
      } end { 
       case(true):
         yield(';');
      };
}

program beta(w) {
   return iter(c in w) [ b := false; ] {
      case(c in " + "\"[a-zA-Z ]\"" + @"):
	     yield(c);
      } end {
      case(!b):
        yield(';');
      };
}


==

image(alpha, " + "\"Hello World\"" + @");
eq(alpha,beta);

";

            StringWriter sw = new StringWriter();
            Pgm.Run(program, sw,"");
            string s = sw.ToString();
            Assert.IsTrue(s.Contains("Result: Equivalent"));
        }

        [TestMethod]
        public void TestHtmlDecode2()
        {
            string locals = @"
function dec2(x,y)=((10*(x-48))+(y-48));
function dec1(y)=(y-48);
";
            string pgm = @"
program decode2(w) {
   return iter(c in w) [ A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;] 
      {
          case (D2 && (c == ';')) :     //e.g. &#38;                                              
            yield(dec2(d1,d2));
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

          case (D2 && (c == '&')) :     //e.g. &#38&                                              
            yield('&','#', d1, d2);
            A := true; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

          case (D2) :                  //e.g. &#38a
            yield('&','#',d1, d2, c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

          case (D1 && (c == ';')) :    //e.g. &#3;
            yield(dec1(d1));  
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

          case (D1 && (c == '&')) :    //e.g. &#4&
            yield('&','#',d1);
            A := true; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

          case (D1 && (('0' <= c) && (c <= '9'))) :  //e.g. &#65
            A := true; H := false; D1 := false; D2 := true; d2 := c;

          case (D1) :                 //e.g. &#6e
            yield('&','#',d1, c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;
     
          case (H && (c == '&')) :    // &#&
            yield('&','#');
            A := true; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

          case (H && (('0' <= c) && (c <= '9'))) : //e.g. &#6
            A := false; H := false; D1 := true; D2 := false; d1 := c; d2 := 0;

          case (H) :                 //e.g. &#g
            yield('&','#', c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

          case (A && (c == '#')) :  // &#
            A := false; H := true; D1 := false; D2 := false; d1 := 0; d2 := 0;

          case (A && (c == '&')) : // &&
            yield('&');

          case (A) :              // e.g. &3
            yield('&',c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

          case (c == '&'):        // &
            A := true;

          case (true) :           //any other case
            yield(c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;
       } end  
       {  //characters to yield when input finished in the middle of a pattern
          case (D2) :
            yield('&','#',d1,d2);

          case (D1) :
            yield('&','#',d1);

          case (H) :
            yield('&','#');

          case (A) :
            yield('&');

          case (true):
            yield();
      };
}
";
            Z3Provider solver = new Z3Provider();
            var D = BekConverter.BekToST(solver, locals + pgm);
            D.Simplify();
            var D1 = D.ExploreBools();
            D1.Simplify();
            var stb = BekConverter.BekToSTb(solver, pgm);
            //avoid introduction of '0' into the intermediate result
            var dom = new SFAModel(D1.Solver, D1.Solver.CharSort, D1.Solver.RegexConverter.Convert("&#48;|&#0;").Complement());
            var D1r = D1.RestrictDomain(dom);
            var DD = D1r + D1r;
            var w = D1r.Diff(DD);
            Assert.IsNotNull(w);
            string i_str = w.Input.GetStringValue(true);
            string o1_str = w.Output1.GetStringValue(true);
            string o2_str = w.Output2.GetStringValue(true);
            Assert.IsFalse(o1_str.Equals(o2_str));
            Assert.AreEqual(WebUtility.HtmlDecode(i_str), o1_str);
            Assert.AreEqual(WebUtility.HtmlDecode(WebUtility.HtmlDecode(i_str)), o2_str);
        }

        [TestMethod]
        public void TestArithm1()
        {
            string pgm = @"

program incr(w) {
   return iter(c in w) {
      case(((c+1-1)==c)&&true||false):
         yield(c + 0 + 1);
      };
}
";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToST(solver, pgm);
            Assert.AreEqual(1, st.StateCount);
            var moves = new List<Move<Rule<Expr>>>(st.GetNonFinalMovesFrom(st.InitialState));
            Assert.AreEqual(1, moves.Count);
            var move = moves[0];
            //Assert.AreEqual(solver.True, move.Condition.Guard);
        }

        [TestMethod]
        public void TestArithm2()
        {
            string pgm = @"

program incr(w) {
   return iter(c in w) {
      case(((0 + 0*c + 3*4*c + 0) == 2*6*c) && true || false):
         yield(1*c + 3*0 + 1);
      };
}
";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToST(solver, pgm).Explore();
            //st.ShowGraph();
            Assert.AreEqual(1, st.StateCount);
            var moves = new List<Move<Rule<Expr>>>(st.GetNonFinalMovesFrom(st.InitialState));
            Assert.AreEqual(1, moves.Count);
            //var move = moves[0];
            //Assert.AreEqual(solver.True, move.Condition.Guard);
        }

        [TestMethod]
        public void TestToUpper()
        {
            string bek = @"

program ToUpper(w) {
   return iter(c in w) {
      case (('a' <= c) && (c <= 'z')):
         yield(c - ('a' - 'A'));
      case (true):
         yield(c);
      //case (('0' <= c) && (c <= '5')):
      //   yield(c);
      //case (('5' <= c) && (c <= '8')):
      //   yield(c - 1);
      };
}
";
            var solver = new Z3Provider();
            var st = BekConverter.BekToST(solver, bek);
            var preim = st.ComputePreImage("^([0-9]+A+)$");
            var preimSFA = preim.ToSFA();
            //preimSFA.ShowGraph();
        }

        [TestMethod]
        public void TestDigitToCode()
        {
            string digit = "\"[0-9]\"";
            string bek1 = @"
function dec(x)=(x-48);
program ToUpper1(w) {
   return iter(c in w) {
      case (c in " + digit + @"):
         yield(dec(c));
      case (true):
         yield();
   };
}
";
            string bek2 = @"
program ToUpper2(w) {
   return iter(c in w) {
      case (c in " + digit + @"):
         yield(c - '0');
      case (true):
         yield();
   };
}
";
            var solver = new Z3Provider();
            var st1 = BekConverter.BekToST(solver, bek1);
            var st2 = BekConverter.BekToST(solver, bek2);
            bool eq = st1.Eq1(st2);
            Assert.IsTrue(eq);
        }

        [TestMethod]
        public void TestX6() 
        {
            int value1 = 1;
            string s1 = value1.ToString("X6", System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual("000001", s1);
            int value2 = 100;
            string s2 = value2.ToString("X6", System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual("000064", s2);
        }

        [TestMethod]
        public void TestCssSafeList()
        {
            Z3Provider solver = new Z3Provider();
            var cs = solver.CharSetProvider.MkRangesConstraint(false,
                new char[][]{
                    new char[]{'0','9'}, 
                    new char[]{'A','Z'},
                    new char[]{'a','z'},
                    new char[]{'\x80','\x90'},
                    new char[]{'\x93','\x9A'},
                    new char[]{'\xA0','\xA5'}});
            Expr t = solver.ConvertFromCharSet(cs);
            int k = cs.CountNodes();
            Assert.AreEqual<char>('0', (char)solver.CharSetProvider.GetMin(cs));
            Assert.AreEqual<char>('0', (char)solver.CharSetProvider.GetMin(cs));
            Assert.AreEqual<char>('\xA5', solver.CharSetProvider.GetMax(cs));
        }

        private static IEnumerable<int> CssSafeList()
        {
            for (int i = '0'; i <= '9'; i++)
            {
                yield return i;
            }

            for (int i = 'A'; i <= 'Z'; i++)
            {
                yield return i;
            }

            for (int i = 'a'; i <= 'z'; i++)
            {
                yield return i;
            }

            // Extended higher ASCII, Ç to É
            for (int i = 0x80; i <= 0x90; i++)
            {
                yield return i;
            }

            // Extended higher ASCII, ô to Ü
            for (int i = 0x93; i <= 0x9A; i++)
            {
                yield return i;
            }

            // Extended higher ASCII, á to Ñ
            for (int i = 0xA0; i <= 0xA5; i++)
            {
                yield return i;
            }
        }

        [TestMethod]
        public void caesarcipher()
        {

            string sample = @"
// Caesar used a simple cipher 
// where all the letters were shifted by 3
function foo(x)= (x + 1 + 1);
program caesarcipher(w) { 
  return iter(c in w) {
    case(true): yield(c + 3);
  };
}
==
display(caesarcipher); js(caesarcipher);";

            StringWriter sw = new StringWriter();
            Pgm.Run(sample, sw, "../../bek.master.html");
            string s = sw.ToString();
            Console.WriteLine(s);
            Assert.IsTrue(s.Contains("foo"));

        }

        [TestMethod]
        public void TestGeneratedBase64()
        {
            Random rand = new Random();
            byte[] somebytes = new byte[rand.Next(10, 100)];
            rand.NextBytes(somebytes);
            var somechars = Array.ConvertAll(somebytes, b => (char)b);
            var encoded = base64encode_F.Apply(somechars);
            var decoded = base64decode_F.Apply(encoded);
            int i = 0;
            foreach (var c in decoded)
                Assert.IsTrue(((byte)c) == somebytes[i++]);

            Assert.IsTrue(i == somebytes.Length);
        }

        //[TestMethod]
        public void TestCssEncode()
        {
            var solver = new Z3Provider();
            var st = BekConverter.BekFileToST(solver, "../../Samples/bek/CssEncode.bek");
            var st1 = st;// st.ExploreBools();
            //st1.Simplify();
            //st1.ShowGraph(10);

            var comp = st1 + st1;
            //comp.Simplify();
            //comp.ShowGraph(10);

            //5 or more characters in the input
            var restr = st1.RestrictDomain("^[^\0-\x32]{7,}$");
            //restr.ShowGraph(10);

            restr.AssertTheory();
            comp.AssertTheory();

            Expr inp = solver.MkFreshConst("inp", comp.InputListSort);
            Expr out1 = solver.MkFreshConst("out1", comp.OutputListSort);
            Expr out2 = solver.MkFreshConst("out2", comp.OutputListSort);

            solver.MainSolver.Assert(restr.MkAccept(inp, out1));
            solver.MainSolver.Assert(comp.MkAccept(inp, out2));
            solver.MainSolver.Assert(solver.MkNeq(out1, out2));

            //validate correctness for some values
            for (int i = 0; i < 5; i++)
            {
                var model = solver.MainSolver.GetModel(solver.True, inp, out1, out2);
                string input = model[inp].StringValue;
                string output1 = model[out1].StringValue;
                string output2 = model[out2].StringValue;

                var output1_expected = System.Web.Security.AntiXss.AntiXssEncoder.CssEncode(input);
                var output2_expected = System.Web.Security.AntiXss.AntiXssEncoder.CssEncode(output1_expected);

                Assert.AreNotEqual(output1, output2);
                Assert.AreEqual(output1_expected, output1);
                Assert.AreEqual(output2_expected, output2);

                solver.MainSolver.Assert(solver.MkNeq(inp, model[inp].Value));
            }
        }
    }

    [TestClass]
    public class BekSamples
    {
        [TestMethod]
        public void TestTrivial()
        {
            string pgm = "program trivial(t) { return iter(c in t) {case (true): yield (c);}; }";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToST(solver, pgm).Explore(); 
            //st.ShowGraph();
            Assert.AreEqual<int>(2, st.MoveCount); //note: one of the moves is a final output
            Assert.AreEqual<int>(1, st.StateCount);
            Assert.AreEqual<int>(0, st.InitialState);
            var moves = new List<Move<Rule<Expr>>>(st.GetNonFinalMovesFrom(0));
            Assert.AreEqual<int>(1, moves.Count);
            //Assert.AreEqual<Expr>(solver.True, moves[0].Condition.Guard);
        }

        [TestMethod]
        public void TestBitvectorOps()
        {
            string noop = @"

program noop(w) {
   return iter(c in w) {
      case (c <= 0xFF):
         yield((((c >> 4) & 0xF) << 4)|(c & 0xF));
      case (true):
         yield(c);
      };
}
";

            string triv = @"

program triv(w) {
   return iter(c in w) {
      case (true):
         yield(c);
      };
}
";
            var solver = new Z3Provider();
            var A = BekConverter.BekToST(solver, noop);
            var B = BekConverter.BekToST(solver, triv);
            bool equiv = A.Eq1(B);
            Assert.IsTrue(equiv);
            //A.ShowGraph();
        }

        [TestMethod]
        public void TestSanitize()
        {
            string pgm = @"program sanitize(t) {
    return iter(c in t)[ b := false; ]
     {
       case (!b && ((c == '\'') || (c == '\" + "\"" + @"'))) :
	     b := false;
	     yield ('\\', c); 

       case (c == '\\') :
	     b := !b;
	     yield (c);
       
       case (true)  :
	     b := false;
	     yield (c);
     };
}";
            Z3Provider solver = new Z3Provider();
            var st1 = BekConverter.BekToST(solver, pgm).ExploreBools();

            //st1.ShowGraph();

            var st = st1 + st1 + st1 + st1;

            st.Simplify();

            //st.ShowGraph(20);

            var sft = st.ExploreBools();

            //sft.ShowGraph();

            Assert.IsTrue(STModel.Eq1(sft, st1));

            Assert.AreEqual<int>(2, sft.StateCount);

        }

        [TestMethod]
        public void TestDecodePairs()
        {
            string regex = "\"[0-9]\"";
            string pgm = @"
function dec(x,y)=((10*(x-48))+y-48);
program decodePairs(w) {
   return iter(c in w) [ saved := false; r := 0; bad := false;] 
      {
      case (!bad  && !(c in " + regex + @")):
         bad := true;

      case(!bad && !saved):
         r := c;
         saved := true;

      case(!bad && saved):
         yield(dec(r,c));
         r := 0;
         saved := false;
     } end {
      case (!bad && saved):
         yield(dec(r,0));

      case (true):
         yield();
     };
}
";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToST(solver, pgm);
            st.Simplify();
            var st1 = st.ExploreBools();
            st1.Simplify();
            var sft = st.Explore();
            sft.Simplify();

            //st.ShowGraph();
            //st1.ShowGraph();
            //sft.ShowGraph();

            //var st1st1 = st1 + st1;
            //st1st1.Simplify();
            //st1st1.ShowGraph(20);

            //var st1st1f = st1st1.Explore();

            //st1st1f.ShowGraph(5);
        }

        [TestMethod]
        public void TestHtmlDecode()
        {
            string pgm = @"
function dec2(x,y) = ((10*(x-48)) + y - 48);
function dec1(x) = (x-48);
program decode(w) {
   return iter(c in w) [ A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;] 
      {
//e.g. &#38;
          case (D2 && (c == ';')) :                                                   
            yield(dec2(d1,d2));
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

//e.g. &#38&
          case (D2 && (c == '&')) :                                                   
            yield('&','#', d1, d2);
            A := true; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

//e.g. &#38a
          case (D2) : 
            yield('&','#',d1, d2, c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

//e.g. &#3;
          case (D1 && (c == ';')) :
            yield(dec1(d1));  
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

//e.g. &#4&
          case (D1 && (c == '&')) :  
            yield('&','#',d1);
            A := true; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

//e.g. &#65
          case (D1 && (('0' <= c) && (c <= '9'))) : 
            D2 := true;
            d2 := c;

//e.g. &#6e
          case (D1) :
            yield('&','#',d1, c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

// &#&     
          case (H && (c == '&')) :
            yield('&','#');
            A := true; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

//e.g. &#6
          case (H && (('0' <= c) && (c <= '9'))) :
            D1 := true;
            d1 := c;

//e.g. &#g
          case (H) :
            yield('&','#', c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

// &#
          case (A && (c == '#')) :
            H := true;

// &&
          case (A && (c == '&')) :
            yield('&');

// e.g. &3
          case (A) :
            yield('&',c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

// &
          case (c == '&'):
            A := true;

//any other case
          case (true) :
            yield(c);
            A := false; H := false; D1 := false; D2 := false; d1 := 0; d2 := 0;

//characters to yield when input finished in the middle of a pattern
          end case (D2) :
            yield('&','#',d1,d2);

          end case (D1) :
            yield('&','#',d1);

          end case (H) :
            yield('&','#');

          end case (A) :
            yield('&');

          end case (true):
            yield();
      };
}
";
            Z3Provider solver = new Z3Provider();
            var D = BekConverter.BekToST(solver, pgm);
            D.Simplify();
            var D1 = D.ExploreBools();
            D1.Simplify();
            //var Dfst = D.Explore();
            //Dfst.Simplify();

            //D.ShowGraph(20);
            //D1.ShowGraph(50);
            //Dfst.ShowGraph(5);

            //var D1r = D1.RestrictDomain(@"^.{5,}$");
            var D1r = D1;

            var DD = D1r + D1r;

            //D1r.ShowGraph(10);

            var w = D1r.Diff(DD);

            Assert.IsNotNull(w);
            string i_str = w.Input.GetStringValue(true);
            string o1_str = w.Output1.GetStringValue(true);
            string o2_str = w.Output2.GetStringValue(true);
            Assert.IsFalse(o1_str.Equals(o2_str));
            Assert.AreEqual(WebUtility.HtmlDecode(i_str), o1_str);
            Assert.AreEqual(WebUtility.HtmlDecode(WebUtility.HtmlDecode(i_str)), o2_str);


            //DD.Simplify();
            //DD.ShowGraph(0);

            //var st1st1f = st1st1.Explore();

            //st1st1f.ShowGraph(5);
        }
    }
}
