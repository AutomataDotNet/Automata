using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using System.Runtime.InteropServices;

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
    public class Neq1Tests
    {
        [TestMethod]
        public void TestLengthConflict()
        {
            string bek = @"
program decode(input){ 
replace { 
  ""&amp;"" ==> ""&""; 
  ""&lt;"" ==> ""<""; 
  ""&gt;"" ==> "">""; 
  else ==> [#0]; 
}
} 
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, bek);
            var test1 = test.ExploreBools();
            //test1.ShowGraph();
            //var meth = test1.Compile();

            //just test on couple of samples
            //Assert.AreEqual<string>("<", meth.Apply("&lt;"));
            //Assert.AreEqual<string>(">", meth.Apply("&gt;"));
            //Assert.AreEqual<string>("&g>", meth.Apply("&g&gt;"));
            //Assert.AreEqual<string>("&g>", meth.Apply("&g&gt;"));

            var dec = test.ToST();
            var d = dec.Explore();

            var dd = d.Compose(d);
            //var ddmeth = dd.ToSTb().Compile();
            var witness = d.Neq1(dd);

            Assert.IsFalse(witness == null);
            var inp = new String(Array.ConvertAll(witness.ToArray(), x => (char)solver.GetNumeralInt(x)));
            //var out1 = meth.Apply(inp);
            //var out2 = ddmeth.Apply(inp);
            var out1b = new String(Array.ConvertAll(new List<Expr>(dec.Apply(witness)).ToArray(), x => ((char)solver.GetNumeralInt(x))));
            var out2b = new String(Array.ConvertAll(new List<Expr>(dd.Apply(witness)).ToArray(), x => ((char)solver.GetNumeralInt(x))));
            //Assert.AreEqual<string>(out1, out1b);
            //Assert.AreEqual<string>(out2, out2b);
            Assert.AreNotEqual<string>(out1b, out2b);
        }

        [TestMethod]
        public void TestLengthConflict2()
        {
            string bek1 = @"
program decode(input){ 
replace { 
  ""&amp;"" ==> ""&""; 
  ""&lt;"" ==> ""<""; 
  ""&gt;"" ==> "">""; 
  else ==> [#0]; 
}
} 
";
            string bek2 = @"
program decode(input){ 
replace { 
  ""&amp;"" ==> ""&""; 
  ""&lt;"" ==> ""<""; 
  ""&gt;"" ==> "">>""; 
  else ==> [#0]; 
}
} 
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test1 = BekConverter.BekToSTb(solver, bek1).ExploreBools();
            //test1.ShowGraph();
            var meth1 = test1.Compile();
            var test2 = BekConverter.BekToSTb(solver, bek2).ExploreBools();
            //test1.ShowGraph();
            var meth2 = test2.Compile();

            //just test on couple of samples
            Assert.AreEqual<string>("<", meth1.Apply("&lt;"));
            Assert.AreEqual<string>(">", meth1.Apply("&gt;"));
            Assert.AreEqual<string>("&g>", meth1.Apply("&g&gt;"));
            Assert.AreEqual<string>("&g>", meth1.Apply("&g&gt;"));

            var d1 = test1.ToST().Explore();
            var d2 = test2.ToST().Explore();
            var witness = d1.Neq1(d2);
            Assert.IsFalse(witness == null);
            var inp = new String(Array.ConvertAll(witness.ToArray(), x => (char)solver.GetNumeralInt(x)));
            var out1 = meth1.Apply(inp);
            var out2 = meth2.Apply(inp);
            Assert.AreNotEqual<string>(out1, out2);
        }

        [TestMethod]
        public void TestPositionConflict()
        {
            string bek1 = @"
program decode(input){ 
replace { 
  ""&amp;"" ==> ""&""; 
  ""&lt;"" ==> ""<""; 
  ""&gt;"" ==> "">""; 
  else ==> [#0]; 
}
} 
";
            string bek2 = @"
program decode(input){ 
replace { 
  ""&amp;"" ==> ""&""; 
  ""&lt;"" ==> ""<""; 
  ""&gt;"" ==> "")""; 
  else ==> [#0]; 
}
} 
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test1 = BekConverter.BekToSTb(solver, bek1).ExploreBools();
            //test1.ShowGraph();
            var meth1 = test1.Compile();
            var test2 = BekConverter.BekToSTb(solver, bek2).ExploreBools();
            //test1.ShowGraph();
            var meth2 = test2.Compile();

            //just test on couple of samples
            Assert.AreEqual<string>("<", meth1.Apply("&lt;"));
            Assert.AreEqual<string>(">", meth1.Apply("&gt;"));
            Assert.AreEqual<string>("&g>", meth1.Apply("&g&gt;"));
            Assert.AreEqual<string>("&g>", meth1.Apply("&g&gt;"));

            var d1 = test1.ToST().Explore();
            var d2 = test2.ToST().Explore();
            var witness = d1.Neq1(d2);
            Assert.IsFalse(witness == null);
            var inp = new String(Array.ConvertAll(witness.ToArray(), x => (char)solver.GetNumeralInt(x)));
            var out1 = meth1.Apply(inp);
            var out2 = meth2.Apply(inp);
            Assert.AreNotEqual<string>(out1, out2);
        }

        [TestMethod]
        public void TestPositionConflictFinal()
        {
            string bek1 = @"
program decode(input) {
  return iter(c in input)[pc := 0;]{
    case ((pc == 0) && (c == 'a')) :                   
      pc := 1; 
      yield (c,c+1);
    case ((pc == 1) && (c == 'b')) : 
      pc := 1;          
      yield (c,c); 
    case ((pc == 1) && (c == 'a')) : 
      pc := 2;          
      yield (c);
    case (true):
      raise Error;
} end {
    case (pc == 2):
      yield ('a');
    case (true):
      raise Error;
};
}
";
            string bek2 = @" 
program decode(input) {
  return iter(c in input)[pc := 0;]{
    case ((pc == 0) && (c == 'a')) :                   
      pc := 1; 
      yield (c);
    case ((pc == 1) && (c == 'b')) : 
      pc := 1;          
      yield (c,c); 
    case ((pc == 1) && (c == 'a')) : 
      pc := 2;          
      yield (c+1,c);
    case (true):
      raise Error;
} end { 
    case (pc == 2):
      yield ('b');
    case (true):
      raise Error;
};
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);

            Func<IEnumerable<Expr>, string> GetString = w =>
            {
                return new String(Array.ConvertAll(new List<Expr>(w).ToArray(), x => (char)solver.GetNumeralInt(x)));
            };

            var A = BekConverter.BekToSTb(solver, bek1).ExploreBools().ToST().Explore();
            var B = BekConverter.BekToSTb(solver, bek2).ExploreBools().ToST().Explore();

            var witness = A.Neq1(B);

            Assert.IsFalse(witness == null);
            var inp = GetString(witness);
            var out1 = GetString(A.Apply(witness));
            var out2 = GetString(B.Apply(witness));
            Assert.AreNotEqual<string>(out1, out2);
        }

        [TestMethod]
        public void TestPositionConflictMiddle()
        {
            string bek1 = @"
program decode(input) {
  return iter(c in input)[pc := 0;]{
    case ((pc == 0) && (c == 'a')) :                   
      pc := 1; 
      yield (c,c+1);
    case ((pc == 1) && ((c == 'b') || (c == 'd'))) : 
      pc := 1;          
      yield (c,c); 
    case ((pc == 1) && (c == 'a')) : 
      pc := 2;          
      yield (c);
    case (true):
      raise Error;
} end {
    case (pc == 2):
      yield ('a');
    case (true):
      raise Error;
};
}
";
            string bek2 = @" 
program decode(input) {
  return iter(c in input)[pc := 0;]{
    case ((pc == 0) && (c == 'a')) :                   
      pc := 1; 
      yield (c);
    case ((pc == 1) && ((c == 'b') || (c == 'd'))) : 
      pc := 1;          
      yield (c,c); 
    case ((pc == 1) && (c == 'a')) : 
      pc := 2;          
      yield (c+1,c);
    case (true):
      raise Error;
} end { 
    case (pc == 2):
      yield ('a');
    case (true):
      raise Error;
};
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);

            Func<IEnumerable<Expr>, string> GetString = w =>
            {
                return new String(Array.ConvertAll(new List<Expr>(w).ToArray(), x => (char)solver.GetNumeralInt(x)));
            };

            var A = BekConverter.BekToSTb(solver, bek1).ExploreBools().ToST().Explore();
            var B = BekConverter.BekToSTb(solver, bek2).ExploreBools().ToST().Explore();

            var witness = A.Neq1(B);

            Assert.IsFalse(witness == null);
            var inp = GetString(witness);
            var out1 = GetString(A.Apply(witness));
            var out2 = GetString(B.Apply(witness));
            Assert.AreNotEqual<string>(out1, out2);
        }

        [TestMethod]
        public void TestEq1()
        {
            string bek1 = @"
program decode(input) {
  return iter(c in input)[pc := 0;]{
    case (pc == 0) :                   //initial state
      if (c == '&') { pc := 1; }
      else { yield (c); }
    case (pc == 1) :                   //memorized &
      if (c == '&') { yield ('&'); }  
      else if (c == 'l') { pc := 2; }  
      else if (c == 'g') { pc := 3; }
      else { yield ('&',c); pc := 0; } 
    case (pc == 2) :                   //memorized &l
      if (c == 't') { pc := 4; }
      else if (c == '&') { yield ('&','l'); pc := 1; }  
      else { yield ('&','l',c); pc := 0; }
    case (pc == 3) :                   //memorized &g 
      if (c == 't') { pc := 5; }
      else if (c == '&') { yield ('&','g'); pc := 1; }  
      else { yield ('&','g',c); pc := 0; }
    case (pc == 4) :                   //memorized &lt 
      if (c == ';')
        { yield ('<'); pc := 0; }      //finished &lt;
      else if (c == '&') { yield ('&','l','t'); pc := 1; }  
      else 
        { yield ('&','l','t',c); pc := 0; }
    case (true) :                     //memorized &gt
      if (c == ';')
        { yield ('>'); pc := 0; }      //finished &gt;
      else if (c == '&') { yield ('&','g','t'); pc := 1; }  
      else 
        { yield ('&','g','t',c); pc := 0; }
  } end {//final nonempty yields are unfinished patterns
    case (pc == 0) : yield ();
    case (pc == 1) : yield ('&');      
    case (pc == 2) : yield ('&','l');
    case (pc == 3) : yield ('&','g');
    case (pc == 4) : yield ('&','l','t');
    case (true)    : yield ('&','g','t');
  };
} 
";
            string bek2 = @"
program decode(input){ 
replace { 
  ""&lt;"" ==> ""<""; 
  ""&gt;"" ==> "">""; 
  else ==> [#0]; 
}
} 
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);

            //Func<IEnumerable<Expr>, string> GetString = w =>
            //{
            //    return new String(Array.ConvertAll(new List<Expr>(w).ToArray(), x => (char)solver.GetNumeralInt(x)));
            //};

            var A = BekConverter.BekToSTb(solver, bek1).ExploreBools().ToST().Explore();
            var B = BekConverter.BekToSTb(solver, bek2).ExploreBools().ToST().Explore();

            var witness = A.Neq1(B);

            Assert.IsTrue(witness == null);
        }

        [TestMethod]
        public void TestEq1_a()
        {
            string bek1 = @"
program decode(input) {
  return iter(c in input)[pc := 0;]{
    case ((pc == 0) && (c == 'a')) :                   
      pc := 1; 
      yield (c,c+1);
    case ((pc == 1) && ((c == 'b'))) : 
      pc := 1;          
      yield (c,c); 
    case ((pc == 1) && (c == 'a')) : 
      pc := 2;          
      yield (c);
    case (true):
      raise Error;
} end {
    case (pc == 2):
      yield ('a');
    case (true):
      raise Error;
};
}
";
            string bek2 = @" 
program decode(input) {
  return iter(c in input)[pc := 0;]{
    case ((pc == 0) && (c == 'a')) :                   
      pc := 1; 
      yield (c);
    case ((pc == 1) && ((c == 'b'))) : 
      pc := 1;          
      yield (c,c); 
    case ((pc == 1) && (c == 'a')) : 
      pc := 2;          
      yield (c+1,c);
    case (true):
      raise Error;
} end { 
    case (pc == 2):
      yield ('a');
    case (true):
      raise Error;
};
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);

            Func<IEnumerable<Expr>, string> GetString = w =>
            {
                return new String(Array.ConvertAll(new List<Expr>(w).ToArray(), x => (char)solver.GetNumeralInt(x)));
            };

            var A = BekConverter.BekToSTb(solver, bek1).ExploreBools().ToST().Explore();
            var B = BekConverter.BekToSTb(solver, bek2).ExploreBools().ToST().Explore();

            var witness = A.Neq1(B);

            Assert.IsNull(witness);
        }

        [TestMethod]
        public void TestPositionConflict2()
        {
            string bek1 = @"
program decode(input){ 
replace { 
  ""&amp;"" ==> ""&""; 
  ""&lt;"" ==> ""<""; 
  ""&gt;"" ==> "">""; 
  @""&\d{3}$"" ==> [(#1|#2)|#3];
  else ==> [#0]; 
}
} 
";
            string bek2 = @"
program decode(input){ 
replace { 
  ""&amp;"" ==> ""&""; 
  ""&lt;"" ==> ""<""; 
  ""&gt;"" ==> "">""; 
  @""&[0-9]{3}$"" ==> [(#1|#2)|#3];
  else ==> [#0]; 
}
}  
";
            Tuple<int, string> foo = Tuple.Create(34, "abc");
            Tuple<int, string> bar = Tuple.Create(34, "ab" + "c");
            bool b = foo.Equals(bar);


            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var d1 = BekConverter.BekToSTb(solver, bek1).ToST();
            var d2 = BekConverter.BekToSTb(solver, bek2).ToST();

            var witness = d1.Neq1(d2);
            Assert.IsFalse(witness == null);
            var inp = new String(Array.ConvertAll(witness.ToArray(), x => (char)solver.GetNumeralInt(x)));
            var out1 = new String(Array.ConvertAll(new List<Expr>(d1.Apply(witness)).ToArray(), x => (char)solver.GetNumeralInt(x)));
            var out2 = new String(Array.ConvertAll(new List<Expr>(d2.Apply(witness)).ToArray(), x => (char)solver.GetNumeralInt(x)));
            solver.Dispose();
            Assert.AreNotEqual<string>(out1, out2);
        }
    }
}
