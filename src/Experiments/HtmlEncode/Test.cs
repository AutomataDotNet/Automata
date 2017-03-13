using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Z3;
using Microsoft.Bek;
using Microsoft.Bek.Model;

namespace Experiments.HtmlEncode
{
    class Test
    {
        static int LengthOfInputStringLower = 100;
        static int LengthOfInputStringUpper = 10000;

        static public void Run()
        {
            Console.WriteLine("Start HtmlEncode.Test");
            Console.WriteLine("extended ASCII:");
            Experiments.HtmlEncode.Test.ComparePerformance(true);
            Console.WriteLine("Unicode:");
            Experiments.HtmlEncode.Test.ComparePerformance();
            Console.WriteLine("Unicode:");
            Experiments.HtmlEncode.Test.ComparePerformance();
            Console.WriteLine("Unicode above 0xFF:");
            Experiments.HtmlEncode.Test.ComparePerformance(false, true);
            Console.WriteLine("End HtmlEncode.Test");
            //Console.ReadKey();
        }

        static public void ComparePerformance(bool asciiOnly = false, bool noASCII = false)
        {
            Console.WriteLine("AntiXss.HtmlEncode vs Rep_o_HtmlEncode");

            int totalChars = 0;
            int NrOfInputStrings = 0;
            var inputs = new List<string>();
            while (totalChars < 100000000)
            {
                var str = GenerateInputString(inputs.Count, asciiOnly, noASCII);
                inputs.Add(str);
                totalChars += str.Length;
                NrOfInputStrings += 1;
            }

            Console.WriteLine("Random inputs have been generated. Running comparison ...");

            var BekRep_o_HtmlEncode = Bek_Rep_o_HtmlEncode();

            //var BexRep_o_HtmlEncode = Bex_Rep_o_HtmlEncode();

            //var Rep = MkRep();

            //var HtmlEncode1 = MkHtmlEncode();

            var systems = new string[inputs.Count];
            var ours = new string[inputs.Count];

            //warm up
            for (int i = 0; i < 5; i++)
            {
                //systems[i] = 
                System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(inputs[i], false);
                BekRep_o_HtmlEncode.Apply(inputs[i]);
                HtmlEncode.Apply(Rep.Apply(inputs[i]));
                Rep_o_Html_HandCoded.Apply(inputs[i]);
            }

            int t2 = System.Environment.TickCount;
            for (int i = 0; i < NrOfInputStrings; i++)
            {
                //ours[i] = 
                BekRep_o_HtmlEncode.Apply(inputs[i]);
            }
            t2 = System.Environment.TickCount - t2;

            int t1 = System.Environment.TickCount;
            for (int i = 0; i < NrOfInputStrings; i++)
            {
                //systems[i] = 
                System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(inputs[i], false);
            }
            t1 = System.Environment.TickCount - t1;

            //#region  check conformance
            //for (int i = 0; i < inputs.Length; i++)
            //{
            //    if (systems[i] != ours[i])
            //    {
            //        Console.WriteLine("BekRep_o_HtmlEncode: ERROR");
            //        return;
            //    }
            //}
            //Console.WriteLine("BekRep_o_HtmlEncode: OK");
            //#endregion

            //int t3 = System.Environment.TickCount;
            //for (int i = 0; i < NrOfInputStrings; i++)
            //{
            //    BexRep_o_HtmlEncode.Apply(inputs[i]);
            //}
            //t3 = System.Environment.TickCount - t3;

            int t4 = System.Environment.TickCount;
            for (int i = 0; i < NrOfInputStrings; i++)
            {
                //ours[i] = 
                    HtmlEncode.Apply(Rep.Apply(inputs[i]));
            }
            t4 = System.Environment.TickCount - t4;

            //#region  check conformance
            //for (int i = 0; i < inputs.Length; i++)
            //{
            //    if (systems[i] != ours[i])
            //    {
            //        Console.WriteLine("HtmlEncode(Rep(input)): ERROR");
            //        return;
            //    }
            //}
            //Console.WriteLine("HtmlEncode(Rep(input)): OK");
            //#endregion

            //int t5 = System.Environment.TickCount;
            //for (int i = 0; i < NrOfInputStrings; i++)
            //{
            //    //ours[i] = 
            //    HtmlEncode.Apply(inputs[i]);
            //}
            //t5 = System.Environment.TickCount - t5;

            //var RepoHtmlEnc = new Bek_Rep_o_HtmlEncode();
            //int t6 = System.Environment.TickCount;
            //for (int i = 0; i < NrOfInputStrings; i++)
            //{
            //    //ours[i] = 
            //    RepoHtmlEnc.Apply(inputs[i]);
            //}
            //t6 = System.Environment.TickCount - t6;

            int t7 = System.Environment.TickCount;
            for (int i = 0; i < NrOfInputStrings; i++)
            {
                //ours[i] = 
                Rep_o_Html_HandCoded.Apply(inputs[i]);
            }
            t7 = System.Environment.TickCount - t7;

            //#region  check conformance
            //for (int i = 0; i < inputs.Length; i++)
            //{
            //    if (systems[i] != ours[i])
            //    {
            //        Console.WriteLine("Rep_o_Html_HandCoded ERROR:iput:{0}, \r\nsys:{1}, \r\nour:{2}", inputs[i], systems[i], ours[i]);
            //        return;
            //    }
            //}
            //Console.WriteLine("Rep_o_HtmlEncode: OK");
            //#endregion

            //obs: t1 and t2 are in ms

            double totalKBytes = totalChars / 1000.0;
            if (!asciiOnly)
                totalKBytes = totalKBytes * 2;

            double throughputSys = totalKBytes / t1;
            double throughputBek = totalKBytes / t2;
            //double throughputBex = totalKBytes / t3;
            double throughputCom = totalKBytes / t4;
            //double throughputHtm = totalKBytes / t5;
            //double throughputBek2 = totalKBytes / t6;
            double throughputHand = totalKBytes / t7;

            double bekProcSlower = ((throughputSys - throughputBek) / throughputSys) * 100.0;
            //double bexProcSlower = ((throughputSys - throughputBex) / throughputSys) * 100.0;
            double comProcSlower = ((throughputSys - throughputCom) / throughputSys) * 100.0;
            //double htmProcSlower = ((throughputSys - throughputHtm) / throughputSys) * 100.0;
            //double bekProcSlower2 = ((throughputSys - throughputBek2) / throughputSys) * 100.0;
            double handProcSlower = ((throughputSys - throughputHand) / throughputSys) * 100.0;

            Console.WriteLine("AntiXss.HtmlEncode  : {0} MByte/sec", throughputSys.ToString("0"));
            Console.WriteLine("Rep_o_HtmlEncode    : {0} MByte/sec, {1}% slower than system", throughputBek.ToString("0"), bekProcSlower.ToString("0"));
            //Console.WriteLine("Bex:Rep_o_HtmlEncode= {0} MByte/sec, {1}% slower than system", throughputBex.ToString("0"), bexProcSlower.ToString("0"));
            Console.WriteLine("HtmlEncode(Rep(inp)): {0} MByte/sec, {1}% slower than system", throughputCom.ToString("0"), comProcSlower.ToString("0"));
            //Console.WriteLine("HtmlEncode(inp)     : {0} MByte/sec, {1}% slower than system", throughputHtm.ToString("0"), htmProcSlower.ToString("0"));
            //Console.WriteLine("2:Rep_o_HtmlEncode  : {0} MByte/sec, {1}% slower than system", throughputBek2.ToString("0"), bekProcSlower2.ToString("0"));
            Console.WriteLine("Rep_o_Html_Hand  : {0} MByte/sec, {1}% slower than system", throughputHand.ToString("0"), handProcSlower.ToString("0"));
        }

        private static string GenerateInputString(int i, bool asciiOnly = false, bool noASCII = false)
        {
            StringBuilder sb = new StringBuilder();
            int k = 0;
            var rnd = new Random(i);
            int L = rnd.Next(LengthOfInputStringLower, LengthOfInputStringUpper);
            while (k < L)
            {
                //each individual caracter in UTF16 is two bytes
                char c = (asciiOnly ? (char)rnd.Next(0xFF) : (noASCII ? (char)rnd.Next(0x100, 0xFFFF) : (char)rnd.Next(0xFFFF)));
                k += 1;
                sb.Append(c);
            }
            string s = sb.ToString();
            return s;
        }

        /// <summary>
        /// HtmlEncode that assumes correct UTF16 input. Its semantics (for correct input)
        /// is the same as System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(_, false)
        /// </summary>
        public static STb<FuncDecl, Expr, Sort> BekHtmlEncode(Z3Provider solver)
        {
            string bek = @"
function CP(h,l) = ((((h & 0x3FF) + 0x40) << 10) | (l & 0x3FF));
function D5(x) = ((((x / 100)/1000) % 10) + '0');
function D4(x) = (((x / 10000) % 10) + '0');
function D3(x) = (((x / 1000) % 10) + '0');
function D2(x) = (((x / 100) % 10) + '0');
function D1(x) = (((x / 10) % 10) + '0');
function D0(x) = ((x % 10) + '0');
function Safe(x) = (((0x3F <= x) && (x <= 0x7E)) || ((0x28 <= x) && (x <= 0x3B)) || (x == 0x20) || (x == 0x21) || ((0x23 <= x) && (x <= 0x25)) || (x == 0x3D) || ((0xA1 <= x) && (x <= 0xAC)) || ((0xAE <= x) && (x <= 0x36F)));

program H(input){
 return iter(x in input)[q:=true;r:=0;]{
   case (q): 
     if (Safe(x))
     { yield(x); }
     else if ((0xD800 <= x) && (x <= 0xDBFF))
            { r:= x; q:= false; }
            else if (x == '<')
            { yield('&', 'l', 't', ';'); }
            else if (x == '>')
            { yield('&', 'g', 't', ';'); }
            else if (x == '&')
            { yield('&', 'a', 'm', 'p', ';'); }
            else if (x == '""')
            { yield('&', 'q', 'u', 'o', 't', ';'); }
            else if (x < 10)
            { yield('&', '#', D0(x), ';'); }
            else if (x < 100)
            { yield('&', '#', D1(x), D0(x), ';'); }
            else if (x < 1000)
            { yield('&', '#', D2(x), D1(x), D0(x), ';'); }
            else if (x < 10000)
            { yield('&', '#', D3(x), D2(x), D1(x), D0(x), ';'); }
            else
            { yield('&', '#', D4(x), D3(x), D2(x), D1(x), D0(x), ';'); }
   case (true): 
            if (  ((0xD800 <= r) && (r <= 0xD820) &&  (0xDC00 <= x) && (x <= 0xDFFF))
                || ((r == 0xD821) && (0xDC00 <= x) && (x <= 0xDE9F)) )
                { yield('&', '#', D4(CP(r, x)), D3(CP(r, x)), D2(CP(r, x)), D1(CP(r, x)), D0(CP(r, x)), ';'); r:= 0; q:= true; }
            else if ( ((r == 0xD821) && (0xDEA0 <= x) && (x <= 0xDFFF)) ||
                      ((0xD822 <= r) && (r <= 0xDB8F) && (0xDC00 <= x) && (x <= 0xDFFF)) ||
                      ((r == 0xDB90) && (0xDC00 <= x) && (x <= 0xDE3F)) )
            { yield('&', '#', D5(CP(r, x)), D4(CP(r, x)), D3(CP(r, x)), D2(CP(r, x)), D1(CP(r, x)), D0(CP(r, x)), ';'); r:= 0; q:= true; }
            else
            { yield('&', '#', '1', D5(CP(r, x)), D4(CP(r, x)), D3(CP(r, x)), D2(CP(r, x)), D1(CP(r, x)), D0(CP(r, x)), ';'); r:= 0; q:= true; }
        };
    }";
            var stb = Microsoft.Bek.Model.BekConverter.BekToSTb(solver, bek);
            return stb;
        }

        /// <summary>
        /// Repairs malformed UTF16 input to correct UTF16 by replacing 
        /// misplaced surrogates with the replacement character \uFFFD
        /// </summary>
        static public STb<FuncDecl, Expr, Sort> BekRep(Z3Provider solver)
        {
            string bek = @"
program R(input){
 return iter(x in input)[q:=true;r:=0;]{
   case (q): 
     if (((x < 0xD800) || (x > 0xDFFF)))
     { yield (x); }
     else if (x <= 0xDBFF)
     { r := x; q := false; }
     else 
     { yield (0xFFFD); }
   case (true): 
     if (((0xDC00 <= x) && (x <= 0xDFFF)))
     { yield (r,x); q := true; r := 0; }
     else if ((0xD800 <= x) && (x <= 0xDBFF))
     { yield (0xFFFD); r:= x; }
     else 
     { yield (0xFFFD,x); q := true; r := 0; }
   end case (q): 
     yield ( );
   end case (true): 
     yield (0xFFFD);
 };
}
";
            var stb = BekConverter.BekToSTb(solver, bek).ExploreBools();
            //stb.ShowGraph();
            return stb;
        }

        static public IExecutableTransducer MkRep()
        {
            var stb = BekRep(new Z3Provider());
            return stb.Compile("Experiments.HtmlEncode", "Rep");
        }

        static public IExecutableTransducer MkHtmlEncode()
        {
            var stb = BekHtmlEncode(new Z3Provider());
            return stb.Compile("Experiments.HtmlEncode", "HtmlEncode");
        }

        /// <summary>
        /// Composes BekRep with BekHtmlEncode
        /// </summary>
        public static IExecutableTransducer Bek_Rep_o_HtmlEncode()
        {
            var solver = new Z3Provider();
            var rep = BekRep(solver);
            var htlm = BekHtmlEncode(solver);
            //var repC = rep.Compile();
            //Console.WriteLine(repC.Source);
            var rep_o_html = rep.Compose(htlm);
            var trans = rep_o_html.Compile("Experiments.HtmlEncode", "Bek_Rep_o_HtmlEncode");
            return trans;
        }

        /// <summary>
        /// HtmlEncode that assumes correct UTF16 input. Its semantics (for correct input)
        /// is the same as System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(_, false)
        /// </summary>
        public static STb<FuncDecl, Expr, Sort> BexHtmlEncode(Z3Provider solver)
        {
            string bex = @"
function CP(h,l) = ((((h & 0x3FF) + 0x40) << 10) | (l & 0x3FF));
function D5(x) = ((((x / 100)/1000) % 10) + '0');
function D4(x) = (((x / 10000) % 10) + '0');
function D3(x) = (((x / 1000) % 10) + '0');
function D2(x) = (((x / 100) % 10) + '0');
function D1(x) = (((x / 10) % 10) + '0');
function D0(x) = ((x % 10) + '0');

    program HtmlEncode(input)
    {
        replace {
            @""[\x20\x21\x23-\x25\x28-\x3B\x3D\x3F-\x7E\xA1-\xAC\xAE-\u036F]"" ==> [#0];
            ""<"" ==> ""&lt;"";
            "">"" ==> ""&gt;"";
            ""&"" ==> ""&amp;"";
            ""\"""" ==> ""&quot;"";
            @""[\0-\x09]"" ==> ['&', '#', D0(#0),';'];
    @""[\x0A-\x63]"" ==> ['&', '#', D1(#0),D0(#0),';'];
    @""[\x64-\u03E7]"" ==> ['&', '#', D2(#0),D1(#0),D0(#0),';'];
    @""[\u03E8-\u270F]"" ==> ['&', '#', D3(#0),D2(#0),D1(#0),D0(#0),';'];
    @""[\u2710-\uD7FF\uE000-\uFFFF]"" ==> ['&', '#', D4(#0),D3(#0),D2(#0),D1(#0),D0(#0),';'];
    @""[\uD800-\uD820][\uDC00-\uDFFF]"" ==> ['&', '#', D4(CP(#0,#1)),D3(CP(#0,#1)),D2(CP(#0,#1)),D1(CP(#0,#1)),D0(CP(#0,#1)),';'];
    @""\uD821[\uDC00-\uDE9F]"" ==> ['&', '#', D4(CP(#0,#1)),D3(CP(#0,#1)),D2(CP(#0,#1)),D1(CP(#0,#1)),D0(CP(#0,#1)),';'];
    @""\uD821[\uDEA0-\uDFFF]"" ==> ['&', '#', '1', '0', '0', D2(CP(#0,#1)),D1(CP(#0,#1)),D0(CP(#0,#1)),';'];
    @""[\uD822-\uDB8F][\uDC00-\uDFFF]"" ==> ['&', '#', D5(CP(#0,#1)),D4(CP(#0,#1)),D3(CP(#0,#1)),D2(CP(#0,#1)),D1(CP(#0,#1)),D0(CP(#0,#1)),';'];
    @""\uDB90[\uDC00-\uDE3F]"" ==> ['&', '#', D5(CP(#0,#1)),D4(CP(#0,#1)),D3(CP(#0,#1)),D2(CP(#0,#1)),D1(CP(#0,#1)),D0(CP(#0,#1)),';'];
    @""\uDB90[\uDE40-\uDFFF]"" ==> ['&', '#', '1', '0', '0', '0', D2(CP(#0,#1)),D1(CP(#0,#1)),D0(CP(#0,#1)),';'];
    @""[\uDB91-\uDBFF][\uDC00-\uDFFF]"" ==> ['&', '#', '1', D5(CP(#0,#1)),D4(CP(#0,#1)),D3(CP(#0,#1)),D2(CP(#0,#1)),D1(CP(#0,#1)),D0(CP(#0,#1)),';'];
  }
    }";
            return Microsoft.Bek.Model.BekConverter.BekToSTb(solver, bex).ExploreBools();
        }

        /// <summary>
        /// Repairs malformed UTF16 input to correct UTF16 by replacing 
        /// misplaced surrogates with the replacement character \uFFFD
        /// </summary>
        static public STb<FuncDecl, Expr, Sort> BexRep(Z3Provider solver)
        {
            string bex = @"
program Rep(input){ 
  replace {
    @""[^\uD800-\uDFFF]""               ==> [#0];
    @""[\uD800-\uDBFF]$""               ==> ['\uFFFD'];
    @""[\uD800-\uDBFF][\uDC00-\uDFFF]"" ==> [#0, #1];
    else                                ==> ['\uFFFD'];
        }
    }";
            return Microsoft.Bek.Model.BekConverter.BekToSTb(solver, bex).ExploreBools();
        }

        public static IExecutableTransducer Bex_Rep_o_HtmlEncode()
        {
            var solver = new Z3Provider();
            var rep = BexRep(solver);
            var htlm = BexHtmlEncode(solver);
            var rep_o_html = rep.Compose(htlm);
            var trans = rep_o_html.Compile("Experiments.HtmlEncode", "Bex_Rep_o_HtmlEncode");
            return trans;
        }

    }

    /// <summary>
    /// Generated code by MkHtmlEncode and slightly hand modified
    /// </summary>
    public class HtmlEncode
    {
        public static int CP(int _0, int _1) { return ((((_0 & 0x3FF) + 0x40) << 0xA) | (_1 & 0x3FF)); }
        static int D5(int _0) { return (((_0 / 100000) % 0xA) + 0x30); }
        static int D4(int _0) { return (((_0 / 10000) % 10) + 0x30); }
        static int D3(int _0) { return (((_0 / 1000) % 10) + 0x30); }
        static int D2(int _0) { return (((_0 / 100) % 10) + 0x30); }
        static int D1(int _0) { return (((_0 / 10) % 10) + 0x30); }
        static int D0(int _0) { return ((_0 % 10) + 0x30); }
        public static bool Safe(int _0) { return (_0 <= 0x7E ? ((0x3F <= _0) || ((0x28 <= _0) && (_0 <= 0x3B)) || (_0 == 0x20) || (_0 == 0x21) || ((0x23 <= _0) && (_0 <= 0x25)) || (_0 == 0x3D)) : (((0xA1 <= _0) && (_0 <= 0xAC)) || ((0xAE <= _0) && (_0 <= 0x36F)))); }
        public static string Apply(string input)
        {
            var output = new System.Text.StringBuilder(input.Length * 5);
            bool r0 = true; int r1 = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                if (r0)
                {
                    if (Safe(c))
                    {
                        output.Append((char)c);
                    }
                    else
                    {
                        if (((0xD800 <= c) && (c <= 0xDBFF)))
                        {
                            r0 = false; r1 = c;
                        }
                        else
                        {
                            if ((c == 0x3C))
                            {
                                output.Append(new char[] { (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                            }
                            else
                            {
                                if ((c == 0x3E))
                                {
                                    output.Append(new char[] { (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                                }
                                else
                                {
                                    if ((c == 0x26))
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                                    }
                                    else
                                    {
                                        if ((c == 0x22))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                                        }
                                        else
                                        {
                                            if (c < 10)
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                            }
                                            else
                                            {
                                                if (c < 100)
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                                }
                                                else
                                                {
                                                    if (c < 1000)
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                    }
                                                    else
                                                    {
                                                        if (c < 10000)
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                        }
                                                        else
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
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
                else
                {
                    int cp = CP(r1, c);
                    if (cp < 100000)
                    {
                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(cp), (char)D3(cp), (char)D2(cp), (char)D1(cp), (char)D0(cp), (char)0x3B });
                        r0 = true; r1 = 0;
                    }
                    else
                    {
                        if (cp < 1000000)
                        {
                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D5(cp), (char)D4(cp), (char)D3(cp), (char)D2(cp), (char)D1(cp), (char)D0(cp), (char)0x3B });
                            r0 = true; r1 = 0;
                        }
                        else
                        {
                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(cp), (char)D4(cp), (char)D3(cp), (char)D2(cp), (char)D1(cp), (char)D0(cp), (char)0x3B });
                            r0 = true; r1 = 0;
                        }
                    }
                }
            }
            return output.ToString();
        }
    }

    /// <summary>
    /// Generated code by MkRep
    /// </summary>
    public class Rep
    {

        public static string Apply(string input)
        {
            var output = new System.Text.StringBuilder();
            int r0 = 0; int state = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                switch (state)
                {
                    case (0):
                        {
                            if ((!(0xD800 <= c) || !(c <= 0xDFFF)))
                            {
                                output.Append((char)c);
                                state = 0;
                            }
                            else
                            {
                                if ((c <= 0xDBFF))
                                {
                                    r0 = c;
                                    state = 1;
                                }
                                else
                                {
                                    output.Append((char)0xFFFD);
                                    state = 0;
                                }
                            }
                            break;
                        }
                    default:
                        {
                            if (((0xDC00 <= c) && (c <= 0xDFFF)))
                            {
                                output.Append(new char[] { (char)r0, (char)c });
                                r0 = 0;
                                state = 0;
                            }
                            else
                            {
                                if (((0xD800 <= c) && (c <= 0xDBFF)))
                                {
                                    output.Append((char)0xFFFD);
                                    r0 = c;
                                    state = 1;
                                }
                                else
                                {
                                    output.Append(new char[] { (char)0xFFFD, (char)c });
                                    r0 = 0;
                                    state = 0;
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
                        break;
                    }
                default:
                    {
                        output.Append((char)0xFFFD);
                        break;
                    }
            }
            return output.ToString();
        }
    }

    public class Bek_Rep_o_HtmlEncode
    {
        static int CP(int _0, int _1) { return ((((_0 & 0x3FF) + 0x40) << 0xA) | (_1 & 0x3FF)); }
        static int D5(int _0) { return ((((_0 / 0x64) / 0x3E8) % 0xA) + 0x30); }
        static int D4(int _0) { return (((_0 / 0x2710) % 0xA) + 0x30); }
        static int D3(int _0) { return (((_0 / 0x3E8) % 0xA) + 0x30); }
        static int D2(int _0) { return (((_0 / 0x64) % 0xA) + 0x30); }
        static int D1(int _0) { return (((_0 / 0xA) % 0xA) + 0x30); }
        static int D0(int _0) { return ((_0 % 0xA) + 0x30); }
        static bool Safe(int _0) {
            return ( _0 <= 0xAC ? ( _0 <= 0x25 ? ((_0 == 0x20) || (_0 == 0x21) || (0x23 <= _0)) 
                                               : (_0 < 0x3F ? (((0x28 <= _0) && (_0 <= 0x3B)) || (_0 == 0x3D)) : ((_0 <= 0x7E) || (0xA1 <= _0)))
                                  )
                                : ((0xAE <= _0) && (_0 <= 0x36F))
                   ); }

        string[] lookup = new string[0x100];

        public string Apply(string input)
        {
            var output = new System.Text.StringBuilder();
            int r0 = 0; bool r1 = true; int r2 = 0; int state = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];

                #region optimization
                if (c < 0x100)
                {
                    if (r2 == 0 && r0 == 0)
                    {
                        output.Append(lookup[c]);
                    }
                    else
                    {
                        output.Append("&#65533;");
                        output.Append(lookup[c]);
                    }
                    r0 = 0;
                    r1 = true;
                    r2 = 0;
                    state = 0;
                    continue;
                }

                #endregion

                #region actual computation
                switch (state)
                {
                    case (0):
                        {
                            if ((!(0xD800 <= c) || !(c <= 0xDFFF)))
                            {
                                if (r1)
                                {
                                    if (Safe(c))
                                    {
                                        output.Append((char)c);
                                        state = 0;
                                    }
                                    else
                                    {
                                        if ((c == 0x3C))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((c == 0x3E))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((c == 0x26))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((c == 0x22))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if (!(0xA <= c))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if (!(0x64 <= c))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (!(0x3E8 <= c))
                                                                {
                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if (!(0x2710 <= c))
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        state = 0;
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
                                else
                                {
                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, c)), (char)D4(CP(r2, c)), (char)D3(CP(r2, c)), (char)D2(CP(r2, c)), (char)D1(CP(r2, c)), (char)D0(CP(r2, c)), (char)0x3B });
                                    r1 = true; r2 = 0;
                                    state = 0;
                                }
                            }
                            else
                            {
                                if ((c <= 0xDBFF))
                                {
                                    r0 = c;
                                    state = 1;
                                }
                                else
                                {
                                    if (r1)
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B });
                                        state = 0;
                                    }
                                    else
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B });
                                        r1 = true; r2 = 0;
                                        state = 0;
                                    }
                                }
                            }
                            break;
                        }
                    default:
                        {
                            if (((0xDC00 <= c) && (c <= 0xDFFF)))
                            {
                                if (r1)
                                {
                                    if (Safe(r0))
                                    {
                                        output.Append(new char[] { (char)r0, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                        r0 = 0;
                                        state = 0;
                                    }
                                    else
                                    {
                                        if (((0xD800 <= r0) && (r0 <= 0xDBFF)))
                                        {
                                            if ((((0xD800 <= r0) && (r0 <= 0xD820) && (0xDC00 <= c) && (c <= 0xDFFF)) || ((r0 == 0xD821) && (0xDC00 <= c) && (c <= 0xDE9F))))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), (char)0x3B });
                                                r0 = 0; r1 = true; r2 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((((r0 == 0xD821) && (0xDEA0 <= c) && (c <= 0xDFFF)) || ((0xD822 <= r0) && (r0 <= 0xDB8F) && (0xDC00 <= c) && (c <= 0xDFFF)) || ((r0 == 0xDB90) && (0xDC00 <= c) && (c <= 0xDE3F))))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), (char)0x3B });
                                                    r0 = 0; r1 = true; r2 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), (char)0x3B });
                                                    r0 = 0; r1 = true; r2 = 0;
                                                    state = 0;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if ((r0 == 0x3C))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x6C, (char)0x74, (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                r0 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((r0 == 0x3E))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x67, (char)0x74, (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                    r0 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((r0 == 0x26))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                        r0 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((r0 == 0x22))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                            r0 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if (!(0xA <= r0))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                r0 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (!(0x64 <= r0))
                                                                {
                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D1(r0), (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                    r0 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if (!(0x3E8 <= r0))
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D2(r0), (char)D1(r0), (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        r0 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (!(0x2710 <= r0))
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D3(r0), (char)D2(r0), (char)D1(r0), (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(r0), (char)D3(r0), (char)D2(r0), (char)D1(r0), (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0;
                                                                            state = 0;
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
                                else
                                {
                                    if ((((0xD800 <= r2) && (r2 <= 0xD820) && (0xDC00 <= r0) && (r0 <= 0xDFFF)) || ((r2 == 0xD821) && (0xDC00 <= r0) && (r0 <= 0xDE9F))))
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(CP(r2, r0)), (char)D3(CP(r2, r0)), (char)D2(CP(r2, r0)), (char)D1(CP(r2, r0)), (char)D0(CP(r2, r0)), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                        r0 = 0; r1 = true; r2 = 0;
                                        state = 0;
                                    }
                                    else
                                    {
                                        if ((((r2 == 0xD821) && (0xDEA0 <= r0) && (r0 <= 0xDFFF)) || ((0xD822 <= r2) && (r2 <= 0xDB8F) && (0xDC00 <= r0) && (r0 <= 0xDFFF)) || ((r2 == 0xDB90) && (0xDC00 <= r0) && (r0 <= 0xDE3F))))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D5(CP(r2, r0)), (char)D4(CP(r2, r0)), (char)D3(CP(r2, r0)), (char)D2(CP(r2, r0)), (char)D1(CP(r2, r0)), (char)D0(CP(r2, r0)), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                            r0 = 0; r1 = true; r2 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, r0)), (char)D4(CP(r2, r0)), (char)D3(CP(r2, r0)), (char)D2(CP(r2, r0)), (char)D1(CP(r2, r0)), (char)D0(CP(r2, r0)), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                            r0 = 0; r1 = true; r2 = 0;
                                            state = 0;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (((0xD800 <= c) && (c <= 0xDBFF)))
                                {
                                    if (r1)
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B });
                                        r0 = c;
                                        state = 1;
                                    }
                                    else
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B });
                                        r0 = c; r1 = true; r2 = 0;
                                        state = 1;
                                    }
                                }
                                else
                                {
                                    if (r1)
                                    {
                                        if (Safe(c))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)c });
                                            r0 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((c == 0x3C))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                                                r0 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((c == 0x3E))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                                                    r0 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((c == 0x26))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                                                        r0 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((c == 0x22))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                                                            r0 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if (!(0xA <= c))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                                                r0 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (!(0x64 <= c))
                                                                {
                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                                                    r0 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if (!(0x3E8 <= c))
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        r0 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (!(0x2710 <= c))
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0;
                                                                            state = 0;
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
                                    else
                                    {
                                        if (Safe(c))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)c });
                                            r0 = 0; r1 = true; r2 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((c == 0x3C))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                                                r0 = 0; r1 = true; r2 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((c == 0x3E))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                                                    r0 = 0; r1 = true; r2 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((c == 0x26))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                                                        r0 = 0; r1 = true; r2 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((c == 0x22))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                                                            r0 = 0; r1 = true; r2 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if (!(0xA <= c))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                                                r0 = 0; r1 = true; r2 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (!(0x64 <= c))
                                                                {
                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                                                    r0 = 0; r1 = true; r2 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if (!(0x3E8 <= c))
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        r0 = 0; r1 = true; r2 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (!(0x2710 <= c))
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0; r1 = true; r2 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0; r1 = true; r2 = 0;
                                                                            state = 0;
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
                #endregion
            }
            switch (state)
            {
                #region end case
                case (0):
                    {
                        break;
                    }
                default:
                    {
                        if (r1)
                        {
                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B });
                        }
                        else
                        {
                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B });
                        }
                        break;
                    }
                    #endregion
            }
            return output.ToString();
        }

        /// <summary>
        /// generated and then slightly modified version optimized to use lookup table for extended ASCII
        /// </summary>
        public Bek_Rep_o_HtmlEncode()
        {
            for (int i = 0; i < 0x100; i++)
            {
                lookup[i] = Apply2(new String(new char[] { (char)i }));
            }
        }

        /// <summary>
        /// Original code without the optimization
        /// </summary>
        public static string Apply2(string input)
        {
            var output = new System.Text.StringBuilder();
            int r0 = 0; bool r1 = true; int r2 = 0; int state = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                switch (state)
                {
                    case (0):
                        {
                            if ((!(0xD800 <= c) || !(c <= 0xDFFF)))
                            {
                                if (r1)
                                {
                                    if (Safe(c))
                                    {
                                        output.Append((char)c);
                                        state = 0;
                                    }
                                    else
                                    {
                                        if ((c == 0x3C))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((c == 0x3E))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((c == 0x26))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((c == 0x22))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if (!(0xA <= c))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if (!(0x64 <= c))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (!(0x3E8 <= c))
                                                                {
                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if (!(0x2710 <= c))
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        state = 0;
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
                                else
                                {
                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, c)), (char)D4(CP(r2, c)), (char)D3(CP(r2, c)), (char)D2(CP(r2, c)), (char)D1(CP(r2, c)), (char)D0(CP(r2, c)), (char)0x3B });
                                    r1 = true; r2 = 0;
                                    state = 0;
                                }
                            }
                            else
                            {
                                if ((c <= 0xDBFF))
                                {
                                    r0 = c;
                                    state = 1;
                                }
                                else
                                {
                                    if (r1)
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B });
                                        state = 0;
                                    }
                                    else
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B });
                                        r1 = true; r2 = 0;
                                        state = 0;
                                    }
                                }
                            }
                            break;
                        }
                    default:
                        {
                            if (((0xDC00 <= c) && (c <= 0xDFFF)))
                            {
                                if (r1)
                                {
                                    if (Safe(r0))
                                    {
                                        output.Append(new char[] { (char)r0, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                        r0 = 0;
                                        state = 0;
                                    }
                                    else
                                    {
                                        if (((0xD800 <= r0) && (r0 <= 0xDBFF)))
                                        {
                                            if ((((0xD800 <= r0) && (r0 <= 0xD820) && (0xDC00 <= c) && (c <= 0xDFFF)) || ((r0 == 0xD821) && (0xDC00 <= c) && (c <= 0xDE9F))))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), (char)0x3B });
                                                r0 = 0; r1 = true; r2 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((((r0 == 0xD821) && (0xDEA0 <= c) && (c <= 0xDFFF)) || ((0xD822 <= r0) && (r0 <= 0xDB8F) && (0xDC00 <= c) && (c <= 0xDFFF)) || ((r0 == 0xDB90) && (0xDC00 <= c) && (c <= 0xDE3F))))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), (char)0x3B });
                                                    r0 = 0; r1 = true; r2 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), (char)0x3B });
                                                    r0 = 0; r1 = true; r2 = 0;
                                                    state = 0;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if ((r0 == 0x3C))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x6C, (char)0x74, (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                r0 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((r0 == 0x3E))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x67, (char)0x74, (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                    r0 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((r0 == 0x26))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                        r0 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((r0 == 0x22))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                            r0 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if (!(0xA <= r0))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                r0 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (!(0x64 <= r0))
                                                                {
                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D1(r0), (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                    r0 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if (!(0x3E8 <= r0))
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D2(r0), (char)D1(r0), (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        r0 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (!(0x2710 <= r0))
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D3(r0), (char)D2(r0), (char)D1(r0), (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(r0), (char)D3(r0), (char)D2(r0), (char)D1(r0), (char)D0(r0), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0;
                                                                            state = 0;
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
                                else
                                {
                                    if ((((0xD800 <= r2) && (r2 <= 0xD820) && (0xDC00 <= r0) && (r0 <= 0xDFFF)) || ((r2 == 0xD821) && (0xDC00 <= r0) && (r0 <= 0xDE9F))))
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(CP(r2, r0)), (char)D3(CP(r2, r0)), (char)D2(CP(r2, r0)), (char)D1(CP(r2, r0)), (char)D0(CP(r2, r0)), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                        r0 = 0; r1 = true; r2 = 0;
                                        state = 0;
                                    }
                                    else
                                    {
                                        if ((((r2 == 0xD821) && (0xDEA0 <= r0) && (r0 <= 0xDFFF)) || ((0xD822 <= r2) && (r2 <= 0xDB8F) && (0xDC00 <= r0) && (r0 <= 0xDFFF)) || ((r2 == 0xDB90) && (0xDC00 <= r0) && (r0 <= 0xDE3F))))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D5(CP(r2, r0)), (char)D4(CP(r2, r0)), (char)D3(CP(r2, r0)), (char)D2(CP(r2, r0)), (char)D1(CP(r2, r0)), (char)D0(CP(r2, r0)), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                            r0 = 0; r1 = true; r2 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, r0)), (char)D4(CP(r2, r0)), (char)D3(CP(r2, r0)), (char)D2(CP(r2, r0)), (char)D1(CP(r2, r0)), (char)D0(CP(r2, r0)), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                            r0 = 0; r1 = true; r2 = 0;
                                            state = 0;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (((0xD800 <= c) && (c <= 0xDBFF)))
                                {
                                    if (r1)
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B });
                                        r0 = c;
                                        state = 1;
                                    }
                                    else
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B });
                                        r0 = c; r1 = true; r2 = 0;
                                        state = 1;
                                    }
                                }
                                else
                                {
                                    if (r1)
                                    {
                                        if (Safe(c))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)c });
                                            r0 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((c == 0x3C))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                                                r0 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((c == 0x3E))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                                                    r0 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((c == 0x26))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                                                        r0 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((c == 0x22))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                                                            r0 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if (!(0xA <= c))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                                                r0 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (!(0x64 <= c))
                                                                {
                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                                                    r0 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if (!(0x3E8 <= c))
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        r0 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (!(0x2710 <= c))
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0;
                                                                            state = 0;
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
                                    else
                                    {
                                        if (Safe(c))
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)c });
                                            r0 = 0; r1 = true; r2 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((c == 0x3C))
                                            {
                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x6C, (char)0x74, (char)0x3B });
                                                r0 = 0; r1 = true; r2 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((c == 0x3E))
                                                {
                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x67, (char)0x74, (char)0x3B });
                                                    r0 = 0; r1 = true; r2 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((c == 0x26))
                                                    {
                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x61, (char)0x6D, (char)0x70, (char)0x3B });
                                                        r0 = 0; r1 = true; r2 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((c == 0x22))
                                                        {
                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x71, (char)0x75, (char)0x6F, (char)0x74, (char)0x3B });
                                                            r0 = 0; r1 = true; r2 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if (!(0xA <= c))
                                                            {
                                                                output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                                                r0 = 0; r1 = true; r2 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (!(0x64 <= c))
                                                                {
                                                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                                                    r0 = 0; r1 = true; r2 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if (!(0x3E8 <= c))
                                                                    {
                                                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                        r0 = 0; r1 = true; r2 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (!(0x2710 <= c))
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0; r1 = true; r2 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B, (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                                                            r0 = 0; r1 = true; r2 = 0;
                                                                            state = 0;
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
                        break;
                    }
                default:
                    {
                        if (r1)
                        {
                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(0xFFFD), (char)D3(0xFFFD), (char)D2(0xFFFD), (char)D1(0xFFFD), (char)D0(0xFFFD), (char)0x3B });
                        }
                        else
                        {
                            output.Append(new char[] { (char)0x26, (char)0x23, (char)0x31, (char)D5(CP(r2, 0xFFFD)), (char)D4(CP(r2, 0xFFFD)), (char)D3(CP(r2, 0xFFFD)), (char)D2(CP(r2, 0xFFFD)), (char)D1(CP(r2, 0xFFFD)), (char)D0(CP(r2, 0xFFFD)), (char)0x3B });
                        }
                        break;
                    }
            }
            return output.ToString();
        }
    }

    /// <summary>
    /// Generated code by MkRep
    /// </summary>
    public class Rep_o_Html_HandCoded
    {
        static int CP(int _0, int _1) { return ((((_0 & 0x3FF) + 0x40) << 0xA) | (_1 & 0x3FF)); }
        static int D5(int _0) { return (((_0 / 100000) % 0xA) + 0x30); }
        static int D4(int _0) { return (((_0 / 10000) % 10) + 0x30); }
        static int D3(int _0) { return (((_0 / 1000) % 10) + 0x30); }
        static int D2(int _0) { return (((_0 / 100) % 10) + 0x30); }
        static int D1(int _0) { return (((_0 / 10) % 10) + 0x30); }
        static int D0(int _0) { return ((_0 % 10) + 0x30); }
        static bool Safe(int _0) { return (_0 <= 0x7E ? ((0x3F <= _0) || ((0x28 <= _0) && (_0 <= 0x3B)) || (_0 == 0x20) || (_0 == 0x21) || ((0x23 <= _0) && (_0 <= 0x25)) || (_0 == 0x3D)) : (((0xA1 <= _0) && (_0 <= 0xAC)) || ((0xAE <= _0) && (_0 <= 0x36F)))); }
        public static string Apply(string input)
        {
            var output = new System.Text.StringBuilder();
            int r0 = 0; int state = 0;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int c = (int)chars[i];
                switch (state)
                {
                    case (0):
                        {
                            if ((!(0xD800 <= c) || !(c <= 0xDFFF)))
                            {
                                #region normal case
                                if (Safe(c))
                                {
                                    output.Append((char)c);
                                }
                                else
                                {
                                    if (chars[i] == '<')
                                        output.Append("&lt;");
                                    else if (chars[i] == '>')
                                        output.Append("&gt;");
                                    else if (chars[i] == '&')
                                        output.Append("&amp;");
                                    else if (chars[i] == '"')
                                        output.Append("&quot;");
                                    else if (c < 10)
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                    }
                                    else if (c < 100)
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                    }
                                    else if (c < 1000)
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                    }
                                    else if (c < 10000)
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                    }
                                    else
                                    {
                                        output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                    }
                                }
                                #endregion
                                state = 0;
                            }
                            else
                            {
                                if ((c <= 0xDBFF))
                                {
                                    r0 = c;
                                    state = 1;
                                }
                                else
                                {
                                    output.Append("&#65533;");
                                    state = 0;
                                }
                            }
                            break;
                        }
                    default:
                        {
                            if (((0xDC00 <= c) && (c <= 0xDFFF)))
                            {
                                #region surrogate code point
                                c = CP(r0, c);
                                if (c < 100000)
                                {
                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                }
                                else if (c < 1000000)
                                {
                                    output.Append(new char[] { (char)0x26, (char)0x23, (char)D5(c), (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                }
                                else
                                {
                                    output.Append(new char[] { (char)0x26, (char)0x23, '1', (char)D5(c), (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                }
                                #endregion
                                r0 = 0;
                                state = 0;
                            }
                            else
                            {
                                if (((0xD800 <= c) && (c <= 0xDBFF)))
                                {
                                    output.Append("&#65533;");
                                    r0 = c;
                                    state = 1;
                                }
                                else
                                {
                                    output.Append("&#65533;");
                                    #region normal case
                                    if (Safe(c))
                                    {
                                        output.Append((char)c);
                                    }
                                    else
                                    {
                                        if (chars[i] == '<')
                                            output.Append("&lt;");
                                        else if (chars[i] == '>')
                                            output.Append("&gt;");
                                        else if (chars[i] == '&')
                                            output.Append("&amp;");
                                        else if (chars[i] == '"')
                                            output.Append("&quot;");
                                        else if (c < 10)
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D0(c), (char)0x3B });
                                        }
                                        else if (c < 100)
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D1(c), (char)D0(c), (char)0x3B });
                                        }
                                        else if (c < 1000)
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                        }
                                        else if (c < 10000)
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                        }
                                        else
                                        {
                                            output.Append(new char[] { (char)0x26, (char)0x23, (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), (char)0x3B });
                                        }
                                    }
                                    #endregion
                                    r0 = 0;
                                    state = 0;
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
                        break;
                    }
                default:
                    {
                        output.Append("&#65533;");
                        break;
                    }
            }
            return output.ToString();
        }
    }
}
