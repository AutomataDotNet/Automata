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


namespace Microsoft.Bek.Tests
{
    [TestClass]
    public class BexTests
    {
        [DllImport(@"C:\Automata\Release\CppEncoders.dll", SetLastError = true)]
        static extern int HtmlDecode_T(string input, int length);

        [TestMethod]
        public void TestLooping()
        {
            string program = @"
program RegexEscape(_){ replace {
  ""\t"" ==> ""\\t"";
  ""\n"" ==> ""\\n"";
  ""\f"" ==> ""\\f"";
  ""\r"" ==> ""\\r"";
  @""(\ |\#|\$|\(|\)|\*|\+|\.|\?|\[|\\|\^|\{|\|)"" ==> ['\\',#0];
  else ==> [#0];
}}
==
a =join(RegexEscape,RegexEscape);
eqB(1,RegexEscape,a);
";

            StringWriter sw = new StringWriter();
            Pgm.Run(program, sw, "");
            Assert.IsTrue(sw.ToString().Contains("Not Partial-Equivalent"));

        }


        [TestMethod]
        public void Test_aaa2b_Keep()
        {
            string bek = @"
program a2b(_){ 
  replace {
     ""aaa"" ==> [#1 + 1];
     else  ==> [#0];
  }
}";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToSTb(solver, bek).ToST();
            //st.ShowGraph();
            var st1 = st.ExploreBools();
            //st1.ShowGraph();
            var stb = st1.ToSTb();
            //stb.ShowGraph();
            var meth = stb.Compile();
            var res = meth.Apply("ac_aaaaGGhh");
            Assert.AreEqual<string>("ac_baGGhh", res);
        }

        [TestMethod]
        public void Test_a2b_ElseX()
        {
            string bek = @"
program a2b(_){ 
  replace {
     ""a"" ==> ""b"";
     else  ==> ""X"";
  }
}";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToSTb(solver, bek).ExploreBools();
            var meth = st.Compile();
            var res = meth.Apply("a_a_aX");
            Assert.AreEqual<string>("bXbXbX", res);
        }

        [TestMethod]
        public void Test_aa2b_Delete()
        {
            string bek = @"
program a2b(_){ 
  replace {
     ""aa"" ==> ""b""; 
     else   ==> [];
  };
}";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToSTb(solver, bek).ExploreBools();
            var meth = st.Compile();
            var res = meth.Apply("aa_a_aa__aaa_");
            Assert.AreEqual<string>("bbb", res);
        }

        [TestMethod]
        public void Test_a2b_Error()
        {
            string bek = @"
program a2b(_){ 
  replace {
     ""a"" ==> ""b""; 
  };
}";
            Z3Provider solver = new Z3Provider();
            var st = BekConverter.BekToSTb(solver, bek).ExploreBools();
            var meth = st.Compile();
            try
            {
                meth.Apply("a_a_a___");
                Assert.IsTrue(false);
            }
            catch (Exception e)
            {
                Assert.AreEqual<string>("a2b", e.Message);
            }
            var res = meth.Apply("aaa");
            Assert.AreEqual<string>("bbb", res);
        }

        [TestMethod]
        public void TestFinalCompletion()
        {
            string bek = @"
program test(_){ 
  replace {
     ""abc"" ==> ""ABC""; 
     ""bcd"" ==> ""BCD""; 
     else    ==> [#0 + 1];
  };
}";
            Z3Provider solver = new Z3Provider();
            var test = BekConverter.BekToSTb(solver, bek);
            var test1 = test.ExploreBools().ToST();
            //test1.ShowGraph();
            var a2b = test.Compile();
            Assert.AreEqual<string>("ABCef", a2b.Apply("abcde"));
            Assert.AreEqual<string>("bcBCD", a2b.Apply("abbcd"));
        }


        static string[] fixedPatterns = new string[] { 
              "nbsp", "iexcl", "cent", "pound", "curren",
              "yen", "brvbar", "sect", "uml", "copy", "ordf", "laquo", "not", "shy", "reg",
              "macr", "deg", "plusmn", "sup2", "sup3", "acute", "micro", "para", "middot", 
              "cedil", "sup1", "ordm", "raquo", "frac14", "frac12", "frac34", "iquest", 
              "times", "divide", "Agrave", "Aacute", "Acirc", "Atilde", "Auml", "Aring",
              "AElig", "Ccedil", "Egrave", "Eacute", "Ecirc", "Euml", "Igrave", "Iacute", 
              "Icirc", "Iuml", "ETH", "Ntilde", "Ograve", "Oacute", "Ocirc", "Otilde", "Ouml",
              "Oslash", "Ugrave", "Uacute", "Ucirc", "Uuml", "Yacute", "THORN", "szlig", 
              "agrave", "aacute", "acirc", "atilde", "auml", "aring", "aelig", "ccedil", 
              "egrave", "eacute", "ecirc", "euml", "igrave", "iacute", "icirc", "iuml", "eth",
              "ntilde", "ograve", "oacute", "ocirc", "otilde", "ouml", "oslash", "ugrave", 
              "uacute", "ucirc", "uuml", "yacute", "thorn", "yuml",
              "OElig", "oelig", "Scaron", "scaron", "Yuml", "fnof", "circ", "tilde", "Alpha", 
              "Beta","Gamma","Delta","Epsilon","Zeta","Eta","Theta","Iota","Kappa",
              "Lambda","Mu","Nu","Xi","Omicron","Pi","Rho","Sigma","Tau","Upsilon",
              "Phi","Chi","Psi","Omega","alpha","beta","gamma","delta","epsilon","zeta",
              "theta","iota","kappa","lambda","mu","nu","xi","omicron","pi",
              "rho","sigmaf","sigma","tau","upsilon","phi","chi","psi","omega","thetasym",
              "upsih","piv","ensp","emsp","thinsp","zwnj","zwj","lrm","rlm","ndash","mdash",
              "lsquo","rsquo","sbquo","ldquo","rdquo","bdquo","dagger","Dagger","bull",
              "hellip","permil","prime","Prime", "lsaquo","rsaquo","oline","frasl",
              "euro","image","weierp","real","trade","alefsym","larr","uarr",
              "rarr","darr","harr","crarr","lArr","uArr","rArr","dArr",
              "hArr","forall","part","exist","empty","nabla","isin","notin",
              "ni","prod","sum","minus","lowast","radic","prop","infin","ang",
              "and","or","cap","cup","int","there4",
              "sim","cong","asymp","ne","equiv","le","ge","sub","sup","nsub","sube","supe",
              "oplus","otimes","perp","sdot","lceil","rceil","lfloor","rfloor","lang","rang",
              "loz","spades","clubs","hearts","diams"
            };

        [TestMethod]
        public void TestHtmlDecode()
        {
            string bekprefix = @"
//helpers
function D1(c) = ite(('0' <= c) && (c <= '9'), c -'0', 10 + ite(('A' <= c) && (c <= 'F'), c -'A', c - 'a'));
function D2(b,x0,x1) = (b*D1(x0) + D1(x1));
function D3(b,x0,x1,x2) = (b*D2(b,x0,x1) + D1(x2));
function D4(b,x0,x1,x2,x3) = (b*D3(b,x0,x1,x2) + D1(x3));
function D5(b,x0,x1,x2,x3,x4) = (b*D4(b,x0,x1,x2,x3) + D1(x4));

program HtmlDecode(_){ 
  replace {
    //most common cases should come first for efficiency
    ""[^&]"" ==> [#0];
    ""&amp;""  ==> ""&"";
    ""&lt;""   ==> ""<"";
    ""&gt;""   ==> "">"";
    ""&quot;"" ==> ""\"""";
    ""&apos;"" ==> ""\'"";
    //exceptions, do not rewrite 0
    //""&#0;"" ==> ""&#0;"";
    //""&#00;"" ==> ""&#00;"";
    //""&#000;"" ==> ""&#000;"";
    //""&#0000;"" ==> ""&#0000;"";
    //""&#00000;"" ==> ""&#00000;"";
    //""&#x0;"" ==> ""&#x0;"";
    //""&#x00;"" ==> ""&#x00;"";
    //""&#x000;"" ==> ""&#x000;"";
    //""&#x0000;"" ==> ""&#x0000;"";
    //""&#X0;"" ==> ""&#X0;"";
    //""&#X00;"" ==> ""&#X00;"";
    //""&#X000;"" ==> ""&#X000;"";
    //""&#X0000;"" ==> ""&#X0000;"";
    //decimal encodings
    ""&#[0-9];""             ==> [D1(#2)];
    ""&#[0-9][0-9];""        ==> [D2(10,#2,#3)];
    ""&#[0-9][0-9][0-9];""   ==> [D3(10,#2,#3,#4)];
    ""&#[0-9]{4};""          ==> [D4(10,#2,#3,#4,#5)];
    ""&#[0-5][0-9]{4};""     ==> [D5(10,#2,#3,#4,#5,#6)];
    ""&#6[0-4][0-9]{3};""    ==> [D5(10,#2,#3,#4,#5,#6)];
    ""&#65[0-4][0-9]{2};""   ==> [D5(10,#2,#3,#4,#5,#6)];
    ""&#655[0-2][0-9];""     ==> [D5(10,#2,#3,#4,#5,#6)];
    ""&#6553[0-5];""         ==> [D5(10,#2,#3,#4,#5,#6)];
    //hexadecimal encodings
    ""&#[xX][0-9A-Fa-f]{1};""==> [D1(#3)];
    ""&#[xX][0-9A-Fa-f]{2};""==> [D2(16,#3,#4)];
    ""&#[xX][0-9A-Fa-f]{3};""==> [D3(16,#3,#4,#5)];
    ""&#[xX][0-9A-Fa-f]{4};""==> [D4(16,#3,#4,#5,#6)];
    //other, less common fixed patterns
";
            //fixed HtmlDecode patterns 

            var time = System.Environment.TickCount;

            StringBuilder sb = new StringBuilder(bekprefix);

            HashSet<string> newones = new HashSet<string>();

            foreach (var p in fixedPatterns)
            {
                if (newones.Add(p))
                {
                    var c = System.Net.WebUtility.HtmlDecode("&" + p + ";");
                    Assert.AreEqual<int>(1, c.Length);
                    sb.AppendLine(string.Format("    \"&{0};\" ==> ['{1}'];", p, StringUtility.Escape(c[0])));
                }
            }
            sb.AppendLine("    else ==> [#0];");
            sb.AppendLine("  };");
            sb.AppendLine("}");

            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, sb.ToString());
            var test1 = test.ExploreBools();
            //test1.ShowGraph();

            var methS = test1.Compile("Microsoft.Bek.Query.Tests", "HtmlDecodeS");

            time = System.Environment.TickCount - time;
            Console.WriteLine("states : " + test1.StateCount);
            Console.WriteLine("time : " + time);

            var methA = test1.Compile("Microsoft.Bek.Query.Tests", "HtmlDecodeA", true);

            //just test on couple of samples first
            string[] samples = new string[] { "&lt;&amp;gt;", "&#00065;&#x41;&#x&apos&apos;aaa", "&#00;", "&#x0000;", "&#010;", "&#65535;" };
            for (int i = 0; i < samples.Length; i++)
            {
                var expected = System.Web.HttpUtility.HtmlDecode(samples[i]);
                var actualS = methS.Apply(samples[i]);
                Assert.AreEqual<string>(expected, actualS);
                var actualA = methA.Apply(samples[i]);
                Assert.AreEqual<string>(expected, actualA);
            }
        }



        [TestMethod]
        public void TestHtmlDecodeSimple()
        {
            string bek = @"
//helpers
function D1(c) = ite(('0' <= c) && (c <= '9'), c -'0', 10 + ite(('A' <= c) && (c <= 'F'), c -'A', c - 'a'));
function D2(b,x0,x1) = (b*D1(x0) + D1(x1));

program HtmlDecode(_){ 
  replace {
    //""&amp;""  ==> ""&"";
    //""&lt;""   ==> ""<"";
    //""&gt;""   ==> "">"";
    //""&quot;"" ==> ""\"""";
    ""&#[0-9];""             ==> [D1(#2)];
    ""&#[0-9][0-9];""        ==> [D2(10,#2,#3)];
    else ==> [#0];
  }
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, bek);
            var test1 = test.ExploreBools();
            //test1.ToST().ShowGraph();


            //test1.ToJS("../../../src/GeneratedCodeSamples/HtmlDecodeSimple.html", true);

            var meth = test1.Compile();

            string[] samples = new string[] { "&#70;&#70", "&#23;xxx&#70;" };
            for (int i = 0; i < samples.Length; i++)
            {
                var expected = System.Net.WebUtility.HtmlDecode(samples[i]);
                var actual = meth.Apply(samples[i]);
                Assert.AreEqual<string>(expected, actual);
            }
        }


        [TestMethod]
        public void TestHtmlEncodeStrict()
        {
            string bek = @"
function H(x) = ite((0 <= x) && (x <= 9), x + 48, x + 55);
program HtmlEncode(_){ 
  replace {
    ""<""   ==> ""&lt;"";
    "">""   ==> ""&gt;"";
    ""\x22""==> ""&quot;"";
    ""&""   ==> ""&amp;"";
    ""\'""  ==> ""&apos"";
    @""[\x01-\x0F]""          ==> ['&','#','X', H(#0), ';']; //do not encode \0
    @""[\x10-\x1F\x7F-\xFF]"" ==> ['&','#','X', H(#0 >> 4), H(#0 & 0xF), ';'];
    @""[\u0100-\u0FFF]""      ==> ['&','#','X', H(#0 >> 8), H((#0 >> 4) & 0xF), H(#0 & 0xF), ';'];
    @""[\u1000-\uFFFF]""      ==> ['&','#','X', H(#0 >> 12), H((#0 >> 8) & 0xF), H((#0 >> 4) & 0xF), H(#0 & 0xF), ';'];
    else ==> [#0];
}}
";
            //fixed HtmlDecode patterns 

            StringBuilder sb = new StringBuilder(bek);
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, sb.ToString());
            var test1 = test.ExploreBools();
            //test1.ShowGraph();

            var meth = test1.Compile("Microsoft.Bek.Query.Tests", "HtmlEncodeStrict");

            // test1.ToCS("../../../src/GeneratedCodeSamples/HtmlEncodeStrict.cs", "GeneratedCodeSamples", "HtmlEncodeStrict", false, false);



            string foo = System.Net.WebUtility.HtmlEncode(@"&&&\0\0&#000;&&&");
            string bazz = System.Net.WebUtility.HtmlDecode(foo + "\\&#01;");

            //just test on couple of samples first
            string[] samples = new string[] { "<.<", ">\xFF   \x7F", };
            for (int i = 0; i < samples.Length; i++)
            {
                var s = meth.Apply(samples[i]);
                var expected = System.Net.WebUtility.HtmlDecode(s);
                Assert.AreEqual<string>(expected, samples[i]);
            }
        }


        [TestMethod]
        public void TestHtmlEncodeStrictJSgen()
        {
            string bek = @"
function H(x) = ite((0 <= x) && (x <= 9), x + 48, x + 55);
program HtmlEncode(_){ 
  replace {
    ""<""   ==> ""&lt;"";
    "">""   ==> ""&gt;"";
    ""\x22""==> ""&quot;"";
    ""&""   ==> ""&amp;"";
    ""\'""  ==> ""&apos;"";
    //encode all control and non ascii chars
    @""[\x01-\x0F]""          ==> ['&','#','X', H(#0), ';']; //do not encode \0
    @""[\x10-\x1F\x7F-\xFF]"" ==> ['&','#','X', H(#0 >> 4), H(#0 & 0xF), ';'];
    @""[\u0100-\u0FFF]""      ==> ['&','#','X', H(#0 >> 8), H((#0 >> 4) & 0xF), H(#0 & 0xF), ';'];
    @""[\u1000-\uFFFF]""      ==> ['&','#','X', H(#0 >> 12), H((#0 >> 8) & 0xF), H((#0 >> 4) & 0xF), H(#0 & 0xF), ';'];
    else ==> [#0];
}}
";
            //fixed HtmlDecode patterns 

            StringBuilder sb = new StringBuilder(bek);
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, sb.ToString());
            var test1 = test.ExploreBools();
            //test1.ToST().ShowGraph();

            //test1.ToJS("../../../GeneratedCodeSamples/HtmlEncodeStrict.html", true);
        }


        //[TestMethod]
        public void RunPerformanceComparison()
        {
            //string testfilesDirectory = @"C:\Users\margus\Desktop\IJCAR12\proceedings";
            string testfilesDirectory = "../../../GeneratedCodeSamples/SampleTexts";
            var someTestFiles = System.IO.Directory.GetFiles(testfilesDirectory);

            int decodeTimeSystem_tot = 0;
            int decodeTimeSystemWeb_tot = 0;
            int decodeTimeA_tot = 0;
            int decodeTimeS_tot = 0;

            long totsize = 0;

            int someTestFileNr = 0;

            while (totsize < 100000000)
            {
                string file = someTestFiles[someTestFileNr];
                someTestFileNr = (someTestFileNr + 1) % someTestFiles.Length;

                int LIMIT = 1000000;
                var fi = new System.IO.FileInfo(file);
                var size = fi.Length;
                string content = "<" + System.IO.File.ReadAllText(file, Encoding.UTF8) + ">";
                string encoded = HtmlEncodeStrict.Apply(content);

                string decoded_System = "";
                string decoded_SystemWeb = "";
                string decoded_A = "";
                string decoded_S = "";

                //--- decode with system
                int decodeTimeSystemNet = System.Environment.TickCount;
                for (long K = 0; K < LIMIT; K += size)
                {
                    decoded_System = System.Net.WebUtility.HtmlDecode(encoded);
                    totsize += size;
                }
                decodeTimeSystemNet = System.Environment.TickCount - decodeTimeSystemNet;

                int decodeTimeSystemWeb = System.Environment.TickCount;
                for (long K = 0; K < LIMIT; K += size)
                    decoded_SystemWeb = System.Web.HttpUtility.HtmlDecode(encoded);
                decodeTimeSystemWeb = System.Environment.TickCount - decodeTimeSystemWeb;

                //--- decode with methB (using StringBuilder)
                int decodeTimeS = System.Environment.TickCount;
                for (long K = 0; K < LIMIT; K += size)
                    decoded_S = HtmlDecodeS.Apply(encoded);
                decodeTimeS = System.Environment.TickCount - decodeTimeS;

                //--- decode with methA (using Array)
                int decodeTimeA = System.Environment.TickCount;
                for (long K = 0; K < LIMIT; K += size)
                    decoded_A = HtmlDecodeA.Apply(encoded);
                decodeTimeA = System.Environment.TickCount - decodeTimeA;

                //--- summary of times
                //string summary = string.Format("{0}: System:{1}ms, methA:{2}ms, methS:{3}ms", fi.Name, decodeTimeSystem, decodeTimeA, decodeTimeS);
                //Console.WriteLine(summary);

                //--- validate outputs
                Assert.AreEqual<string>(decoded_System, decoded_SystemWeb);
                if (!decoded_System.Equals(decoded_A))
                {
                    for (int i = 0; i < decoded_System.Length; i++)
                        if (decoded_System[i] != decoded_A[i])
                            Assert.Fail();
                }
                Assert.AreEqual<string>(decoded_System, decoded_S);


                decodeTimeSystem_tot += decodeTimeSystemNet;
                decodeTimeSystemWeb_tot += decodeTimeSystemWeb;
                decodeTimeA_tot += decodeTimeA;
                decodeTimeS_tot += decodeTimeS;
            }

            string summary_tot = string.Format("{0}MB: System.Net.WebUtility.HtmlDecode:{1}ms, System.Web.HttpUtility.HtmlDecode:{4}, methA:{2}ms, methS:{3}ms",
                totsize / 1000000, decodeTimeSystem_tot, decodeTimeA_tot, decodeTimeS_tot, decodeTimeSystemWeb_tot);
            Console.WriteLine(summary_tot);
        }

        [TestMethod]
        public void TestBase64Encode()
        {
            string bek = @"

function E(x)=(ite(x<=25,x+65,ite(x<=51,x+71,ite(x<=61,x-4,ite(x==62,'+','/')))));

program base64encode(_){ 
  replace {
    @""[\0-\xFF]{3}""  ==> [E(Bits(7,2,#0)), E((Bits(1,0,#0)<<4)|Bits(7,4,#1)), E((Bits(3,0,#1)<<2)|Bits(7,6,#2)), E(Bits(5,0,#2))];
    @""[\0-\xFF]{2}$"" ==> [E(Bits(7,2,#0)), E((Bits(1,0,#0)<<4)|Bits(7,4,#1)), E(Bits(3,0,#1)<<2), '='];
    @""[\0-\xFF]$""    ==> [E(Bits(7,2,#0)), E(Bits(1,0,#0)<<4), '=', '='];
  }
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, bek).ExploreBools();
            //test.ToST().ShowGraph();

            StringBuilder sb = new StringBuilder();
            test.ToJS(x => { sb.AppendLine(x); return; });

            var meth = test.Compile();
            Assert.AreEqual<string>("TWFu", meth.Apply("Man"));
        }

        [TestMethod]
        public void TestBase64Decode()
        {
            string bek = @"
function D(x)=(ite(x=='/',63,ite(x=='+',62,ite(x<='9',x+4,ite(x<='Z',x-65,x-71)))));
program base64decode(_){ 
  replace {
    ""[a-zA-Z0-9+/]{4}""   ==> [(D(#0)<<2)|bits(5,4,D(#1)), (bits(3,0,D(#1))<<4)|bits(5,2,D(#2)), (bits(1,0,D(#2))<<6)|D(#3)];
    ""[a-zA-Z0-9+/]{3}=$"" ==> [(D(#0)<<2)|bits(5,4,D(#1)), (bits(3,0,D(#1))<<4)|bits(5,2,D(#2))];
    ""[a-zA-Z0-9+/]{2}==$""==> [(D(#0)<<2)|bits(5,4,D(#1))];
  }
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);

            var x = solver.MkVar(1, solver.IntSort);
            Console.WriteLine(((IntSymbol)x.FuncDecl.Name).Int);

            var test = BekConverter.BekToSTb(solver, bek).ExploreBools();
            //test.ToST().ShowGraph();

            var meth = test.Compile();
            Assert.AreEqual<string>("Man", meth.Apply("TWFu"));
        }

        [TestMethod]
        public void TestBase64Equiv()
        {
            string bek = @"

function E(x)=(ite(x<=25,x+65,ite(x<=51,x+71,ite(x<=61,x-4,ite(x==62,'+','/')))));
function D(x)=(ite(x=='/',63,ite(x=='+',62,ite(x<='9',x+4,ite(x<='Z',x-65,x-71)))));
program base64encode(_){ 
  replace {
    @""[\0-\xFF]{3}""  ==> [E(Bits(7,2,#0)), E((Bits(1,0,#0)<<4)|Bits(7,4,#1)), E((Bits(3,0,#1)<<2)|Bits(7,6,#2)), E(Bits(5,0,#2))];
    @""[\0-\xFF]{2}$"" ==> [E(Bits(7,2,#0)), E((Bits(1,0,#0)<<4)|Bits(7,4,#1)), E(Bits(3,0,#1)<<2), '='];
    @""[\0-\xFF]$""    ==> [E(Bits(7,2,#0)), E(Bits(1,0,#0)<<4), '=', '='];
  }
}
program base64decode(_){ 
  replace {
    ""[a-zA-Z0-9+/]{4}""   ==> [(D(#0)<<2)|bits(5,4,D(#1)), (bits(3,0,D(#1))<<4)|bits(5,2,D(#2)), (bits(1,2,D(#2))<<6)|D(#3)];
    ""[a-zA-Z0-9+/]{3}=$"" ==> [(D(#0)<<2)|bits(5,4,D(#1)), (bits(3,0,D(#1))<<4)|bits(5,2,D(#2))];
    ""[a-zA-Z0-9+/]{2}==$""==> [(D(#0)<<2)|bits(5,4,D(#1))];
  }
}
program ID(w){return iter(x in w){case (true):yield(x);};}
==
BYTES = regex(@""^[\0-\xFF]*$"");            //domain of sequences of bytes
ID_BYTES = restrict(ID,BYTES);             //identity over sequences of bytes
ed = join(base64encode, base64decode);     //composition, first encode then decode
eq(ID_BYTES, ed); 
";
            StringWriter sw = new StringWriter();
            Pgm.Run(bek, sw, "");
            string s = sw.ToString();
            Console.WriteLine(s);
            Assert.IsTrue(s.Contains("Equivalent"));
        }

        [TestMethod]
        public void TestBase16()
        {
            string bek = @"
function E(x)=ite(x < 10, x + 48, x + 55);
function D(x)=ite(x < 58, x - 48, x - 55);

program base16encode(input){ 
  replace {
    @""[\0-\xFF]""  ==> [E(#0 >> 4),E(#0 & 0xF)];
  }
}

program base16decode(input){ 
  replace {
    ""[A-Z0-9]{2}"" ==> [(D(#0) << 4) + D(#1)];
  }
}
// Identity Encoder
program ID(_){replace { "".""   ==> [#0];}}
==
js(base16encode);
BYTES = regex(@""^[\0-\xFF]*$"");            //domain of sequences of bytes
ID_BYTES = restrict(ID,BYTES);             //identity over sequences of bytes
ed = join(base16encode, base16decode);     //composition, first encode then decode
eq(ID_BYTES, ed);   
";
            StringWriter sw = new StringWriter();
            Pgm.Run(bek, sw, "");
            string s = sw.ToString();
            Console.WriteLine(s);
            Assert.IsTrue(s.Contains("Result: Equivalent"));
        }

        [TestMethod]
        public void TestBase16Encode()
        {
            string bek = @"
function E(x)=ite(x < 10, x + 48, x + 55);
program base16encode(input){ 
  replace {
    @""[\0-\xFF]""  ==> [E(#0 >> 4),E(#0 & 0xF)];
  }
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, bek).ExploreBools();
            //test.ToST().ShowGraph();

            StringBuilder sb = new StringBuilder();
            test.ToJS(x => { sb.AppendLine(x); return; });

            var meth = test.Compile();

            var res = meth.Apply("012");
            Assert.AreEqual<string>("303132", res);

            res = meth.Apply("\0\x01\x02\x03");
            Assert.AreEqual<string>("00010203", res);
        }

        [TestMethod]
        public void TestHtmlDecode1()
        {
            string bek = @"
function f(x) = ite(x<='9', x-48, ite(x<='F', x-55, x-87));
program HtmlDecode1(_){replace{
    ""&#(x|X)00;""             ==> [#0,#1,#2,#3,#4,#5]; //dont decode
    ""&#(x|X)[0-9A-Fa-f]{2};"" ==> [(f(#3)<<4)+f(#4)];  //decode
    else                     ==> [#0]; 
  };
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, bek).ExploreBools();

            var css = solver.CharSetProvider;

            StringBuilder sb = new StringBuilder();
            test.ToJS(x => { sb.AppendLine(x); return; });

            var meth = test.Compile();

            var P0 = "^&#[0-9]{3};";
            var P1 = "^&#[0-9]{3}";

            var M0 = css.Convert(P0).Determinize().Minimize();
            var M1 = css.Convert(P1).Determinize().Minimize();
            var M2 = M1.Minus(M0).Minimize();

            //css.ShowGraph(M2, "M2");

            var res = meth.Apply("&&#X00;&#x26;#x00;");
            Assert.AreEqual<string>("&&#X00;&#x00;", res);

            var res2 = meth.Apply(res);
            Assert.AreEqual<string>(res, res2);
        }

        [TestMethod]
        public void TestHtmlDecode1b()
        {
            string bek = @"
program B(input){replace{
    ""&#00;""       ==> ""&#00;""; //dont decode
    ""&#[0-9]{2};"" ==> [(10*(#2-48))+(#3-48)];  //decode
    ""&#$"" ==> ""&#"";  //keep
    else            ==> [#0]; //default
  };
}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, bek).ExploreBools();

            var css = solver.CharSetProvider;

            StringBuilder sb = new StringBuilder();
            test.ToJS(x => { sb.AppendLine(x); return; });

            var meth = test.Compile();

            var res = meth.Apply("&&#00;&#38;&#");
            Assert.AreEqual<string>("&&#00;&&#", res);

            var res2 = meth.Apply(res);
            Assert.AreEqual<string>(res, res2);
        }


        [TestMethod]
        public void Test_a2b()
        {
            string bek = @"
program a2b(_){replace{""a"" ==> ""bb""; else ==> [#0];};}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, bek).ExploreBools();

            var css = solver.CharSetProvider;

            StringBuilder sb = new StringBuilder();
            test.ToJS(x => { sb.AppendLine(x); return; });

            var meth = test.Compile();
            //test.ShowGraph();

            var res = meth.Apply("aaac");
            Assert.AreEqual<string>("bbbbbbc", res);

            var res2 = meth.Apply(res);
            Assert.AreEqual<string>(res, res2);
        }

        [TestMethod]
        public void TestEndAnchor()
        {
            string bek = @"
program sample(_){ 
  replace {
    ""ab$"" ==> ""123="";
    ""a$""  ==> ""12=="";
}}
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var test = BekConverter.BekToSTb(solver, bek);
            var test1 = test.ExploreBools();
            //test1.ShowGraph();
            var meth = test1.Compile();

            //just test on couple of samples
            Assert.AreEqual<string>("123=", meth.Apply("ab"));
            Assert.AreEqual<string>("12==", meth.Apply("a"));
            //must throw in other cases
            try
            {
                meth.Apply("abab");
                Assert.Fail("meth did not throw an excetion");
            }
            catch (Exception e)
            {
                Assert.AreEqual<string>("sample_B", e.Message);
            }
        }

        [TestMethod]
        public void TestRegexEscaping()
        {
            string bek = @"
program RegexEscape(_){ replace {
  ""\t"" ==> ""\\t"";
  ""\n"" ==> ""\\n"";
  ""\f"" ==> ""\\f"";
  ""\r"" ==> ""\\r"";
  @""(\ |\#|\$|\(|\)|\*|\+|\.|\?|\[|\\|\^|\{|\|)"" ==> ['\\',#0];
  else ==> [#0];
}}
//==
//js(RegexEscape);
";
            Z3Provider solver = new Z3Provider(BitWidth.BV16);
            var stb = BekConverter.BekToSTb(solver, bek);
            var st = stb.ExploreBools().ToST();
            //st.ShowGraph();
            var meth = st.STb.Compile();

            //st.STb.ToJS("../../../GeneratedCodeSamples/RegexEscape.html", true);

            //just test on couple of samples
            Assert.AreEqual<string>(@"\\\.\$xabc\^", meth.Apply(@"\.$xabc^"));
            Assert.AreEqual<string>(@"\(\)\[]\{}\?\.\|", meth.Apply(@"()[]{}?.|"));
        }

        [TestMethod]
        public void TestHtmlEncodeDecode()
        {
            //geberate a random string 
            StringBuilder sb = new StringBuilder();
            Random rnd = new Random();
            int k = 0;
            int MAX = 1000;
            while (k < MAX)
            {
                char c = (char)rnd.Next(0xFF);
                if (!char.IsSurrogate(c)) //ignore surrogates
                {
                    k += 1;
                    sb.Append(c);
                }
            }

            string str = sb.ToString();
            string e = HtmlEncodeStrict.Apply(str);
            string d = System.Net.WebUtility.HtmlDecode(e);
            if (!str.Equals(d))
                Assert.Fail();
        }

        [TestMethod]
        public void TestAntiXSSHtmlEncodeSemantics()
        {
            //characters in this range are safe and are thus mapped to themselves
            Predicate<int> safe = x => ((32 <= x && x <= 33) |
                                        (35 <= x && x <= 37) |
                                        (40 <= x && x <= 59) |
                                        (x == 61) |
                                        (63 <= x && x <= 126) |
                                        (161 <= x && x <= 172) |
                                        (174 <= x && x <= 879));
            //these special characters are encoded as follows
            var specials = new Dictionary<int, string>();
            specials[(int)'<'] = "&lt;";
            specials[(int)'>'] = "&gt;";
            specials[(int)'&'] = "&amp;";
            specials[(int)'\"'] = "&quot;";
            //all other nonsurrogate characters are encoded with decimals using format &#dd...;
            for (int i = 0; i <= 0xFFFF; i++)
            {
                char c = (char)i;
                string s = c.ToString();
                if (!char.IsSurrogate(c))
                {
                    var e = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(s, false);
                    var e2 = Bek4funSample.Rep_o_HtmlEncode.Apply(s);
                    Assert.IsTrue(e == e2);

                    if (safe(i))
                        Assert.IsTrue(s == e);
                    else if (specials.ContainsKey(i))
                        Assert.IsTrue(specials[i] == e);
                    else
                        Assert.IsTrue(e == "&#" + i.ToString() + ";");
                }
            }
            //all misplaced surrogates are encoded as the replacement character FFFD
            for (int i = 0xD800; i <= 0xDFFF; i++)
            {
                string s = ((char)i).ToString();
                var e = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(s, false);
                var e2 = Bek4funSample.Rep_o_HtmlEncode.Apply(s);
                Assert.IsTrue(e == e2);

                Assert.IsTrue(e == "&#" + 0xFFFD.ToString() + ";");
            }
            //all surrogate pairs are encoded using the corresponding codepoint number
            //for all high surrogates hs
            for (int hs = 0xD800; hs <= 0xDBFF; hs++)
            {
                //for all low surrogates ls
                for (int ls = 0xDC00; ls <= 0xDFFF; ls++)
                {
                    //construct surrogate pair s
                    string s = new string(new char[] {(char)hs,(char)ls});
                    string e = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(s, false);
                    var e2 = Bek4funSample.Rep_o_HtmlEncode.Apply(s);
                    Assert.IsTrue(e == e2);

                    //Unicode codepoint corresponding to the (hs,ls) pair
                    int cp = char.ConvertToUtf32((char)hs, (char)ls);
                    //cp is the same as cp_
                    int hs_10_bits = hs & 0x3FF;
                    int ls_10_bits = ls & 0x3FF;
                    int cp_ = (((hs_10_bits + 0x40) << 10) | ls_10_bits);
                    Assert.IsTrue(cp == cp_);
                    Assert.IsTrue(e ==  "&#" + cp.ToString() + ";");
                }
            }
        }
    }
}

/// <summary>
/// Autiomatically generated from bex
/// </summary>
namespace Bek4funSample
{
    public class Rep_o_HtmlEncode
    {
        static int CP(int _0, int _1) { return ((((_0 & 0x3FF) + 0x40) << 0xA) | (_1 & 0x3FF)); }
        static int D5(int _0) { return ((((_0 / 0x64) / 0x3E8) % 0xA) + 0x30); }
        static int D4(int _0) { return (((_0 / 0x2710) % 0xA) + 0x30); }
        static int D3(int _0) { return (((_0 / 0x3E8) % 0xA) + 0x30); }
        static int D2(int _0) { return (((_0 / 0x64) % 0xA) + 0x30); }
        static int D1(int _0) { return (((_0 / 0xA) % 0xA) + 0x30); }
        static int D0(int _0) { return ((_0 % 0xA) + 0x30); }

        public static string Apply(string input)
        {
            var output = new System.Text.StringBuilder();
            int r0 = 0; int r1 = 0; int state = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                switch (state)
                {
                    case (0):
                        {
                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x2710 <= c) && (c <= 0xD7FF)) || (0xE000 <= c))))
                            {
                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                r0 = 0; r1 = 0;
                                state = 0;
                            }
                            else
                            {
                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (0x3E8 <= c) && (((c >> 14) & 0x3) == 0) && ((c & 0x3FFF) <= 0x270F)))
                                {
                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                    r0 = 0; r1 = 0;
                                    state = 0;
                                }
                                else
                                {
                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x7F <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xA0)) || (c == 0xAD) || ((0x370 <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x3E7)))))
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                        r0 = 0; r1 = 0;
                                        state = 0;
                                    }
                                    else
                                    {
                                        if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0xA <= c) && (((c >> 5) & 0x7FF) == 0)) || (c == 0x27))))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                            r0 = 0; r1 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((c >> 4) & 0xFFF) == 0) && ((c & 0xF) <= 9)))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                                r0 = 0; r1 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x22)))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                                                    r0 = 0; r1 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x26)))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                                                        r0 = 0; r1 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x3E)))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                                                            r0 = 0; r1 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x3C)))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                                                                r0 = 0; r1 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x64 <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x7E)) || ((0xA1 <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xAC)) || ((0xAE <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x36F)))))
                                                                {
                                                                    output.Append((char)c);
                                                                    r0 = 0; r1 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x20 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x21)) || ((0x23 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x25)) || ((0x28 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x3B)) || (c == 0x3D) || ((0x3F <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x63)))))
                                                                    {
                                                                        output.Append((char)c);
                                                                        r0 = 0; r1 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (((0xDC00 <= c) && (c <= 0xDFFF)))
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B });
                                                                            r0 = 0; r1 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            if (((0xD800 <= c) && (c <= 0xDBFF)))
                                                                            {
                                                                                r0 = c;
                                                                                state = 1;
                                                                            }
                                                                            else
                                                                            {
                                                                                throw new System.Exception("Rep_o_HtmlEncode");
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    default:
                        {
                            if (((0xDC00 <= c) && (c <= 0xDFFF) && (0xDB91 <= r0) && (r0 <= 0xDBFF)))
                            {
                                output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), (char)0x3B });
                                r0 = 0; r1 = 0;
                                state = 0;
                            }
                            else
                            {
                                if (((0xDC00 <= c) && (c <= 0xDFFF) && (r0 == 0xDB90) && (0xDE40 <= c)))
                                {
                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)0x30, (char)0x30, (char)0x30, (char)D2(CP(0xDB90, c)), (char)D1(CP(0xDB90, c)), (char)D0(CP(0xDB90, c)), (char)0x3B });
                                    r0 = 0; r1 = 0;
                                    state = 0;
                                }
                                else
                                {
                                    if (((0xDC00 <= c) && (c <= 0xDFFF) && (r0 == 0xDB90) && (c <= 0xDE3F)))
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D5(CP(0xDB90, c)), (char)D4(CP(0xDB90, c)), (char)D3(CP(0xDB90, c)), (char)D2(CP(0xDB90, c)), (char)D1(CP(0xDB90, c)), (char)D0(CP(0xDB90, c)), (char)0x3B });
                                        r0 = 0; r1 = 0;
                                        state = 0;
                                    }
                                    else
                                    {
                                        if (((0xDC00 <= c) && (c <= 0xDFFF) && (0xD822 <= r0) && (r0 <= 0xDB8F)))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), (char)0x3B });
                                            r0 = 0; r1 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if (((0xDC00 <= c) && (c <= 0xDFFF) && (r0 == 0xD821) && (0xDEA0 <= c)))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)0x30, (char)0x30, (char)D2(CP(0xD821, c)), (char)D1(CP(0xD821, c)), (char)D0(CP(0xD821, c)), (char)0x3B });
                                                r0 = 0; r1 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if (((0xDC00 <= c) && (c <= 0xDFFF) && (r0 == 0xD821) && (c <= 0xDE9F)))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(CP(0xD821, c)), (char)D3(CP(0xD821, c)), (char)D2(CP(0xD821, c)), (char)D1(CP(0xD821, c)), (char)D0(CP(0xD821, c)), (char)0x3B });
                                                    r0 = 0; r1 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if (((0xDC00 <= c) && (c <= 0xDFFF) && (0xD800 <= r0) && (r0 <= 0xD820)))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), (char)0x3B });
                                                        r0 = 0; r1 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x2710 <= c) && (c <= 0xD7FF)) || (0xE000 <= c))))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                            r0 = 0; r1 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (0x3E8 <= c) && (((c >> 14) & 0x3) == 0) && ((c & 0x3FFF) <= 0x270F)))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                r0 = 0; r1 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x7F <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xA0)) || (c == 0xAD) || ((0x370 <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x3E7)))))
                                                                {
                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                    r0 = 0; r1 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0xA <= c) && (((c >> 5) & 0x7FF) == 0)) || (c == 0x27))))
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        r0 = 0; r1 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((c >> 4) & 0xFFF) == 0) && ((c & 0xF) <= 9)))
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                                                            r0 = 0; r1 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x22)))
                                                                            {
                                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                                                                                r0 = 0; r1 = 0;
                                                                                state = 0;
                                                                            }
                                                                            else
                                                                            {
                                                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x26)))
                                                                                {
                                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                                                                                    r0 = 0; r1 = 0;
                                                                                    state = 0;
                                                                                }
                                                                                else
                                                                                {
                                                                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x3E)))
                                                                                    {
                                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                                                                                        r0 = 0; r1 = 0;
                                                                                        state = 0;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x3C)))
                                                                                        {
                                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                                                                                            r0 = 0; r1 = 0;
                                                                                            state = 0;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x64 <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x7E)) || ((0xA1 <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xAC)) || ((0xAE <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x36F)))))
                                                                                            {
                                                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)c });
                                                                                                r0 = 0; r1 = 0;
                                                                                                state = 0;
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x20 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x21)) || ((0x23 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x25)) || ((0x28 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x3B)) || (c == 0x3D) || ((0x3F <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x63)))))
                                                                                                {
                                                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)c });
                                                                                                    r0 = 0; r1 = 0;
                                                                                                    state = 0;
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    if (((0xD800 <= c) && (c <= 0xDBFF)))
                                                                                                    {
                                                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B });
                                                                                                        r0 = c; r1 = 0;
                                                                                                        state = 1;
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        throw new System.Exception("Rep_o_HtmlEncode");
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                }
            }
            switch (state)
            {
                case (0):
                    {
                        if (true)
                        {
                        }
                        else
                        {
                            throw new System.Exception("Rep_o_HtmlEncode");
                        }
                        break;
                    }
                default:
                    {
                        if (true)
                        {
                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B });
                        }
                        else
                        {
                            throw new System.Exception("Rep_o_HtmlEncode");
                        }
                        break;
                    }
            }
            return output.ToString();
        }
    }
}
