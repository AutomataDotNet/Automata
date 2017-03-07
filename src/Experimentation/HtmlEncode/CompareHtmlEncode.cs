using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Automata;
using Microsoft.Automata.Z3;
using Microsoft.Z3;

namespace Experimentation.HtmlEncode
{
    class CompareHtmlEncode
    {
        static public void CompareHtmlEncodePerformance(bool useGeneratedCode = false)
        {
            Console.WriteLine("AntiXss.HtmlEncode vs Rep_o_HtmlEncode");

            Func<string, string> rep_o_htmlencode = Rep_o_HtmlEncode.Apply;
            if (useGeneratedCode)
                rep_o_htmlencode = GenRep_o_HtmlEncode(new Z3Provider()).Apply;

            //generate 100 random 1MByte input strings
            var inputs = new List<string>();
            for (int i = 0; i < 100; i++)
                inputs.Add(Generate1MByteString(i));

            //do a conformance test first
            if (rep_o_htmlencode(inputs[0]) !=
                System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(inputs[0], false))
                Console.WriteLine("!!! DIFFERENT ENCODINGS !!!");

            int t2 = System.Environment.TickCount;
            if (useGeneratedCode)
            {
                for (int i = 0; i < inputs.Count; i++)
                {
                    var res2 = GenRep_o_HtmlEncode(new Z3Provider()).Apply(inputs[i]);
                } 
            }
            else
            {
                for (int i = 0; i < inputs.Count; i++)
                {
                    var res2 = Rep2HtmlEncode.Apply(inputs[i]);
                }
            }
            t2 = System.Environment.TickCount - t2;

            int t1 = System.Environment.TickCount;
            for (int i = 0; i < inputs.Count; i++)
            {
                var res1 = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(inputs[i], false);
            }
            t1 = System.Environment.TickCount - t1;

            Console.WriteLine("AntiXss.HtmlEncode(100MB)={0}ms, Rep_o_HtmlEncode(100MB)={1}ms", t1, t2);
        }

        private static string Generate1MByteString(int i)
        {
            StringBuilder sb = new StringBuilder();
            Random rnd = new Random(i);
            int k = 0;
            int MAX = 500000; 
            while (k < MAX)
            {
                //each individual caracter in UTF16 is two bytes
                char c = (char)rnd.Next(0xFFFF);
                k += 1;
                sb.Append(c);
            }
            //so there are in total 2*MAX bytes in s
            string s = sb.ToString();
            return s;
        }

        /// <summary>
        /// HtmlEncode that assumes correct UTF16 input. Its semantics (for correct input)
        /// is the same as System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(_, false)
        /// </summary>
        public static STb<FuncDecl,Expr,Sort> BexHtmlEncode(Z3Provider solver)
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
            return Microsoft.Bek.Model.BekConverter.BekToSTb(solver, bex);
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
            return Microsoft.Bek.Model.BekConverter.BekToSTb(solver, bex);
        }

        public static IExecutableTransducer GenRep_o_HtmlEncode(Z3Provider solver)
        {
            var rep = BexRep(solver);
            var htlm = BexHtmlEncode(solver);
            var repC = rep.Compile("test","rep"); 
            Console.WriteLine(repC.Source);
            var rep_o_html = rep.Compose(htlm);
            var trans = rep_o_html.Compile();
            return trans;
        }


        /// <summary>
        /// generated up front from the same bex program as above
        /// </summary>
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
                                    output.Append(new char[] { '&', '#', (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                    r0 = 0; r1 = 0;
                                    state = 0;
                                }
                                else
                                {
                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (0x3E8 <= c) && (((c >> 14) & 0x3) == 0) && ((c & 0x3FFF) <= 0x270F)))
                                    {
                                        output.Append(new char[] { '&', '#', (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                        r0 = 0; r1 = 0;
                                        state = 0;
                                    }
                                    else
                                    {
                                        if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x7F <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xA0)) || (c == 0xAD) || ((0x370 <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x3E7)))))
                                        {
                                            output.Append(new char[] { '&', '#', (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                            r0 = 0; r1 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0xA <= c) && (((c >> 5) & 0x7FF) == 0)) || (c == 0x27))))
                                            {
                                                output.Append(new char[] { '&', '#', (char)D1(c), (char)D0(c), ';' });
                                                r0 = 0; r1 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((c >> 4) & 0xFFF) == 0) && ((c & 0xF) <= 9)))
                                                {
                                                    output.Append(new char[] { '&', '#', (char)D0(c), ';' });
                                                    r0 = 0; r1 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x22)))
                                                    {
                                                        output.Append(new char[] { '&', (char)0x71, (char)0x75, (char)0x6F, (char)0x74, ';' });
                                                        r0 = 0; r1 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x26)))
                                                        {
                                                            output.Append(new char[] { '&', (char)0x61, (char)0x6D, (char)0x70, ';' });
                                                            r0 = 0; r1 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x3E)))
                                                            {
                                                                output.Append(new char[] { '&', (char)0x67, (char)0x74, ';' });
                                                                r0 = 0; r1 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x3C)))
                                                                {
                                                                    output.Append(new char[] { '&', (char)0x6C, (char)0x74, ';' });
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
                                                                                output.Append("&#65533;");
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
                                    output.Append(new char[] { '&', '#', (char)0x31, (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), ';' });
                                    r0 = 0; r1 = 0;
                                    state = 0;
                                }
                                else
                                {
                                    if (((0xDC00 <= c) && (c <= 0xDFFF) && (r0 == 0xDB90) && (0xDE40 <= c)))
                                    {
                                        output.Append(new char[] { '&', '#', (char)0x31, (char)0x30, (char)0x30, (char)0x30, (char)D2(CP(0xDB90, c)), (char)D1(CP(0xDB90, c)), (char)D0(CP(0xDB90, c)), ';' });
                                        r0 = 0; r1 = 0;
                                        state = 0;
                                    }
                                    else
                                    {
                                        if (((0xDC00 <= c) && (c <= 0xDFFF) && (r0 == 0xDB90) && (c <= 0xDE3F)))
                                        {
                                            output.Append(new char[] { '&', '#', (char)D5(CP(0xDB90, c)), (char)D4(CP(0xDB90, c)), (char)D3(CP(0xDB90, c)), (char)D2(CP(0xDB90, c)), (char)D1(CP(0xDB90, c)), (char)D0(CP(0xDB90, c)), ';' });
                                            r0 = 0; r1 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if (((0xDC00 <= c) && (c <= 0xDFFF) && (0xD822 <= r0) && (r0 <= 0xDB8F)))
                                            {
                                                output.Append(new char[] { '&', '#', (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), ';' });
                                                r0 = 0; r1 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if (((0xDC00 <= c) && (c <= 0xDFFF) && (r0 == 0xD821) && (0xDEA0 <= c)))
                                                {
                                                    output.Append(new char[] { '&', '#', (char)0x31, (char)0x30, (char)0x30, (char)D2(CP(0xD821, c)), (char)D1(CP(0xD821, c)), (char)D0(CP(0xD821, c)), ';' });
                                                    r0 = 0; r1 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if (((0xDC00 <= c) && (c <= 0xDFFF) && (r0 == 0xD821) && (c <= 0xDE9F)))
                                                    {
                                                        output.Append(new char[] { '&', '#', (char)D4(CP(0xD821, c)), (char)D3(CP(0xD821, c)), (char)D2(CP(0xD821, c)), (char)D1(CP(0xD821, c)), (char)D0(CP(0xD821, c)), ';' });
                                                        r0 = 0; r1 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if (((0xDC00 <= c) && (c <= 0xDFFF) && (0xD800 <= r0) && (r0 <= 0xD820)))
                                                        {
                                                            output.Append(new char[] { '&', '#', (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), ';' });
                                                            r0 = 0; r1 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x2710 <= c) && (c <= 0xD7FF)) || (0xE000 <= c))))
                                                            {
                                                                output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                                                r0 = 0; r1 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (0x3E8 <= c) && (((c >> 14) & 0x3) == 0) && ((c & 0x3FFF) <= 0x270F)))
                                                                {
                                                                    output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                                                    r0 = 0; r1 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x7F <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xA0)) || (c == 0xAD) || ((0x370 <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x3E7)))))
                                                                    {
                                                                        output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                                                        r0 = 0; r1 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0xA <= c) && (((c >> 5) & 0x7FF) == 0)) || (c == 0x27))))
                                                                        {
                                                                            output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D1(c), (char)D0(c), ';' });
                                                                            r0 = 0; r1 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((c >> 4) & 0xFFF) == 0) && ((c & 0xF) <= 9)))
                                                                            {
                                                                                output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D0(c), ';' });
                                                                                r0 = 0; r1 = 0;
                                                                                state = 0;
                                                                            }
                                                                            else
                                                                            {
                                                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x22)))
                                                                                {
                                                                                    output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', (char)0x71, (char)0x75, (char)0x6F, (char)0x74, ';' });
                                                                                    r0 = 0; r1 = 0;
                                                                                    state = 0;
                                                                                }
                                                                                else
                                                                                {
                                                                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x26)))
                                                                                    {
                                                                                        output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', (char)0x61, (char)0x6D, (char)0x70, ';' });
                                                                                        r0 = 0; r1 = 0;
                                                                                        state = 0;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x3E)))
                                                                                        {
                                                                                            output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', (char)0x67, (char)0x74, ';' });
                                                                                            r0 = 0; r1 = 0;
                                                                                            state = 0;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            if ((((c <= 0xD7FF) || (0xE000 <= c)) && (c == 0x3C)))
                                                                                            {
                                                                                                output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', (char)0x6C, (char)0x74, ';' });
                                                                                                r0 = 0; r1 = 0;
                                                                                                state = 0;
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x64 <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x7E)) || ((0xA1 <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xAC)) || ((0xAE <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x36F)))))
                                                                                                {
                                                                                                    output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', (char)c });
                                                                                                    r0 = 0; r1 = 0;
                                                                                                    state = 0;
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    if ((((c <= 0xD7FF) || (0xE000 <= c)) && (((0x20 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x21)) || ((0x23 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x25)) || ((0x28 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x3B)) || (c == 0x3D) || ((0x3F <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x63)))))
                                                                                                    {
                                                                                                        output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', (char)c });
                                                                                                        r0 = 0; r1 = 0;
                                                                                                        state = 0;
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        if (((0xD800 <= c) && (c <= 0xDBFF)))
                                                                                                        {
                                                                                                            output.Append("&#65533;");
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
                                output.Append("&#65533;");
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

        public class Rep2HtmlEncode
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
                                if (((0xD800 <= c) && (c <= 0xDBFF)))
                                {
                                    r0 = c;
                                    state = 1;
                                }
                                else
                                {
                                    if (((0xDC00 <= c) && (c <= 0xDFFF)))
                                    {
                                        output.Append("&#65533;");
                                        r0 = 0; r1 = 0;
                                        state = 0;
                                    }
                                    else
                                    {
                                        if ((c == 0x3C))
                                        {
                                            output.Append(new char[] { '&', (char)0x6C, (char)0x74, ';' });
                                            r0 = 0; r1 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((c == 0x3E))
                                            {
                                                output.Append(new char[] { '&', (char)0x67, (char)0x74, ';' });
                                                r0 = 0; r1 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((c == 0x26))
                                                {
                                                    output.Append(new char[] { '&', (char)0x61, (char)0x6D, (char)0x70, ';' });
                                                    r0 = 0; r1 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((c == 0x22))
                                                    {
                                                        output.Append(new char[] { '&', (char)0x71, (char)0x75, (char)0x6F, (char)0x74, ';' });
                                                        r0 = 0; r1 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((((0x20 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x21)) || ((0x23 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x25)) || ((0x28 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x3B)) || (c == 0x3D) || ((0x3F <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x63))))
                                                        {
                                                            output.Append((char)c);
                                                            r0 = 0; r1 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if ((((0x64 <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x7E)) || ((0xA1 <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xAC)) || ((0xAE <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x36F))))
                                                            {
                                                                output.Append((char)c);
                                                                r0 = 0; r1 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (((((c >> 4) & 0xFFF) == 0) && ((c & 0xF) <= 9)))
                                                                {
                                                                    output.Append(new char[] { '&', '#', (char)D0(c), ';' });
                                                                    r0 = 0; r1 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if ((((0xA <= c) && (((c >> 5) & 0x7FF) == 0)) || (c == 0x27)))
                                                                    {
                                                                        output.Append(new char[] { '&', '#', (char)D1(c), (char)D0(c), ';' });
                                                                        r0 = 0; r1 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if ((((0x7F <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xA0)) || (c == 0xAD) || ((0x370 <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x3E7))))
                                                                        {
                                                                            output.Append(new char[] { '&', '#', (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                                                            r0 = 0; r1 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            if (((0x3E8 <= c) && (((c >> 14) & 0x3) == 0) && ((c & 0x3FFF) <= 0x270F)))
                                                                            {
                                                                                output.Append(new char[] { '&', '#', (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                                                                r0 = 0; r1 = 0;
                                                                                state = 0;
                                                                            }
                                                                            else
                                                                            {
                                                                                output.Append(new char[] { '&', '#', (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                                                                r0 = 0; r1 = 0;
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
                        default:
                            {
                                if (((0xD800 <= c) && (c <= 0xDBFF)))
                                {
                                    output.Append("&#65533;");
                                    r0 = c; r1 = 0;
                                    state = 1;
                                }
                                else
                                {
                                    if (((c <= 0xD7FF) || (0xE000 <= c)))
                                    {
                                        if ((c == 0x3C))
                                        {
                                            output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', (char)0x6C, (char)0x74, ';' });
                                            r0 = 0; r1 = 0;
                                            state = 0;
                                        }
                                        else
                                        {
                                            if ((c == 0x3E))
                                            {
                                                output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', (char)0x67, (char)0x74, ';' });
                                                r0 = 0; r1 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                if ((c == 0x26))
                                                {
                                                    output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', (char)0x61, (char)0x6D, (char)0x70, ';' });
                                                    r0 = 0; r1 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    if ((c == 0x22))
                                                    {
                                                        output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', (char)0x71, (char)0x75, (char)0x6F, (char)0x74, ';' });
                                                        r0 = 0; r1 = 0;
                                                        state = 0;
                                                    }
                                                    else
                                                    {
                                                        if ((((0x20 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x21)) || ((0x23 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x25)) || ((0x28 <= c) && (((c >> 6) & 0x3FF) == 0) && ((c & 0x3F) <= 0x3B)) || (c == 0x3D) || ((0x3F <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x63))))
                                                        {
                                                            output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', (char)c });
                                                            r0 = 0; r1 = 0;
                                                            state = 0;
                                                        }
                                                        else
                                                        {
                                                            if ((((0x64 <= c) && (((c >> 7) & 0x1FF) == 0) && ((c & 0x7F) <= 0x7E)) || ((0xA1 <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xAC)) || ((0xAE <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x36F))))
                                                            {
                                                                output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', (char)c });
                                                                r0 = 0; r1 = 0;
                                                                state = 0;
                                                            }
                                                            else
                                                            {
                                                                if (((((c >> 4) & 0xFFF) == 0) && ((c & 0xF) <= 9)))
                                                                {
                                                                    output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D0(c), ';' });
                                                                    r0 = 0; r1 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if ((((0xA <= c) && (((c >> 5) & 0x7FF) == 0)) || (c == 0x27)))
                                                                    {
                                                                        output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D1(c), (char)D0(c), ';' });
                                                                        r0 = 0; r1 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if ((((0x7F <= c) && (((c >> 8) & 0xFF) == 0) && ((c & 0xFF) <= 0xA0)) || (c == 0xAD) || ((0x370 <= c) && (((c >> 10) & 0x3F) == 0) && ((c & 0x3FF) <= 0x3E7))))
                                                                        {
                                                                            output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                                                            r0 = 0; r1 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            if (((0x3E8 <= c) && (((c >> 14) & 0x3) == 0) && ((c & 0x3FFF) <= 0x270F)))
                                                                            {
                                                                                output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                                                                r0 = 0; r1 = 0;
                                                                                state = 0;
                                                                            }
                                                                            else
                                                                            {
                                                                                output.Append(new char[] { '&', '#', '6', '5', '5', '3', '3', ';', '&', '#', (char)D4(c), (char)D3(c), (char)D2(c), (char)D1(c), (char)D0(c), ';' });
                                                                                r0 = 0; r1 = 0;
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
                                        if ((r0 == 0xD821))
                                        {
                                            if (((0xDC00 <= c) && (c <= 0xDE9F)))
                                            {
                                                output.Append(new char[] { '&', '#', (char)D4(CP(0xD821, c)), (char)D3(CP(0xD821, c)), (char)D2(CP(0xD821, c)), (char)D1(CP(0xD821, c)), (char)D0(CP(0xD821, c)), ';' });
                                                r0 = 0; r1 = 0;
                                                state = 0;
                                            }
                                            else
                                            {
                                                output.Append(new char[] { '&', '#', (char)0x31, (char)0x30, (char)0x30, (char)D2(CP(0xD821, c)), (char)D1(CP(0xD821, c)), (char)D0(CP(0xD821, c)), ';' });
                                                r0 = 0; r1 = 0;
                                                state = 0;
                                            }
                                        }
                                        else
                                        {
                                            if ((r0 == 0xDB90))
                                            {
                                                if (((0xDC00 <= c) && (c <= 0xDE3F)))
                                                {
                                                    output.Append(new char[] { '&', '#', (char)D5(CP(0xDB90, c)), (char)D4(CP(0xDB90, c)), (char)D3(CP(0xDB90, c)), (char)D2(CP(0xDB90, c)), (char)D1(CP(0xDB90, c)), (char)D0(CP(0xDB90, c)), ';' });
                                                    r0 = 0; r1 = 0;
                                                    state = 0;
                                                }
                                                else
                                                {
                                                    output.Append(new char[] { '&', '#', (char)0x31, (char)0x30, (char)0x30, (char)0x30, (char)D2(CP(0xDB90, c)), (char)D1(CP(0xDB90, c)), (char)D0(CP(0xDB90, c)), ';' });
                                                    r0 = 0; r1 = 0;
                                                    state = 0;
                                                }
                                            }
                                            else
                                            {
                                                if ((r0 == 0x3C))
                                                {
                                                    throw new System.Exception("Rep2HtmlEncode");
                                                }
                                                else
                                                {
                                                    if ((r0 == 0x3E))
                                                    {
                                                        throw new System.Exception("Rep2HtmlEncode");
                                                    }
                                                    else
                                                    {
                                                        if ((r0 == 0x26))
                                                        {
                                                            throw new System.Exception("Rep2HtmlEncode");
                                                        }
                                                        else
                                                        {
                                                            if ((r0 == 0x22))
                                                            {
                                                                throw new System.Exception("Rep2HtmlEncode");
                                                            }
                                                            else
                                                            {
                                                                if (((0xD800 <= r0) && (r0 <= 0xD820)))
                                                                {
                                                                    output.Append(new char[] { '&', '#', (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), ';' });
                                                                    r0 = 0; r1 = 0;
                                                                    state = 0;
                                                                }
                                                                else
                                                                {
                                                                    if (((0xD822 <= r0) && (r0 <= 0xDB8F)))
                                                                    {
                                                                        output.Append(new char[] { '&', '#', (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), ';' });
                                                                        r0 = 0; r1 = 0;
                                                                        state = 0;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (((0xDB91 <= r0) && (r0 <= 0xDBFF)))
                                                                        {
                                                                            output.Append(new char[] { '&', '#', (char)0x31, (char)D5(CP(r0, c)), (char)D4(CP(r0, c)), (char)D3(CP(r0, c)), (char)D2(CP(r0, c)), (char)D1(CP(r0, c)), (char)D0(CP(r0, c)), ';' });
                                                                            r0 = 0; r1 = 0;
                                                                            state = 0;
                                                                        }
                                                                        else
                                                                        {
                                                                            throw new System.Exception("Rep2HtmlEncode");
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
                            output.Append("&#65533;");
                            break;
                        }
                }
                return output.ToString();
            }
        }
    }

}
