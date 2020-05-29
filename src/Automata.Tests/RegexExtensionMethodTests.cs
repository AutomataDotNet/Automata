using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Automata;
using Microsoft.Automata.Rex;
using System.Text;

namespace Automata.Tests
{
    [TestClass]
    public class RegexExtensionMethodTests
    {
        static string myconsole = "c://tmp/vsoutput.txt";
        static string regexesFile = "../../../../Automata.Tests/Samples/regexes.txt";
        static string inputFile = "../../../../Automata.Tests/Samples/input.txt";
        static string regexesWithoutAnchorsFile = "../../../../Automata.Tests/Samples/regexesWithoutAnchors.txt";
        //static string regexesWithoutAnchorsFile = "../../../../Automata.Tests/Samples/regex_tiny.txt";
        static string regexesWithLazyLoops = "../../../../Automata.Tests/Samples/regexesWithLazyLoops.txt";
        static string regexesPerfFile_IgnoreCaseTrue = "regexesPerf_IgnoreCaseTrue.txt";
        static string regexesPerfFile_IgnoreCaseFalse = "regexesPerf_IgnoreCaseFalse.txt";
        static string regexesPerfFile_IsMatch_IgnoreCaseTrue = "regexesPerf_IsMatch_IgnoreCaseTrue.txt";
        static string regexesPerfFile_IsMatch_IgnoreCaseFalse = "regexesPerf_IsMatch_IgnoreCaseFalse.txt";
        static string regexinputsPath = "../../../../Automata.Tests/Samples/regexinputs/";

        //[TestMethod]
        //public void TestRegex_Compile()
        //{
        //    Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x));
        //    for (int i = 0; i < regexes.Length && i < 50; i++)
        //    {
        //        var regex = regexes[i];
        //        var rex = new RexEngine(BitWidth.BV16);
        //        var aut = rex.CreateFromRegexes(regex.ToString());
        //        var comp = regex.Compile();
        //        if (comp == null)
        //            continue;
        //        foreach (var s in rex.GenerateMembers(aut, 10))
        //        {
        //            Assert.IsTrue(regex.IsMatch(s));
        //            Assert.IsTrue(comp.IsMatch(s));
        //        }
        //    }
        //}

        [TestMethod]
        public void TestRegex_GenerateCpp()
        {
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesFile), x => new Regex(x));
            var brexman = new BREXManager("Matcher", "FString", 0, 0);
            for (int i = 0; i < regexes.Length && i < 50; i++)
            {
                var regex = regexes[i];
                var cpp = regex.GenerateCpp();
                Assert.IsNotNull(cpp);
            }
        }

        [TestMethod]
        public void TestRegex_GenerateRandomDataset()
        {
            var regex1 = new Regex(@"^bcd$");
            var set1 = regex1.GenerateRandomDataSet(100);
            Assert.IsTrue(set1.Count == 1, "Incorrect Dataset");
            var regex1a = new Regex(@"^b(c|C)d$");
            var set1a = regex1a.GenerateRandomDataSet(100);
            Assert.IsTrue(set1a.Count == 2, "Incorrect Dataset");
            var regex1b = new Regex(@"(?i:^bcd$)");
            var set1b = regex1b.GenerateRandomDataSet(100);
            Assert.IsTrue(set1b.Count == 8, "Incorrect Dataset");
            var regex1c = new Regex(@"(?i:^bcd$)");
            var set1c = regex1c.GenerateRandomDataSet(100, "[bcCdD]");
            Assert.IsTrue(set1c.Count == 4, "Incorrect Dataset");
            //----
            var regex2 = new Regex(@"^(\d[\w-[\d]])+$");
            var set2 = regex2.GenerateRandomDataSet(100, "[012a-d]", 20);
            Assert.IsTrue(set2.Count == 100);
            foreach (string s in set2)
                Assert.IsTrue(Regex.IsMatch(s, "^([0-2][a-d])+$"), "Incorrect Dataset");
        }

        [TestMethod]
        public void TestRegex_GenerateRandomDatasetFromComplement()
        {
            var regex1 = new Regex(@"^bcd$").Complement();
            var set1 = regex1.GenerateRandomDataSet(100);
            Assert.IsTrue(set1.Count == 100, "Incorrect Dataset");
            //----
            var regex2 = new Regex(@"^(\d[\w-[\d]])+$").Complement();
            var set2 = regex2.GenerateRandomDataSet(100);
            Assert.IsTrue(set2.Count == 100);
            foreach (string s in set2)
                Assert.IsTrue(!Regex.IsMatch(s, @"^(\d[\w-[\d]])+$"), "Incorrect Dataset");
        }

        [TestMethod]
        public void TestRegex_GenerateRandomMember()
        {
            var regex = new Regex(@"^(\d[\w-[\d]])+$");
            for (int i = 0; i < 100; i++)
            {
                var s = regex.GenerateRandomMember("[01ab]");
                Assert.IsTrue(Regex.IsMatch(s, "^([01][ab])+$"));
                s = regex.GenerateRandomMember("[9a-z]");
                Assert.IsTrue(Regex.IsMatch(s, "^(9[a-z])+$"));
            }
        }

        Random rnd = new Random(123);
        private string CreateRandomString(int k)
        {
            var elems = new char[k];
            for (int i = 0; i < k; i++)
                elems[i] = (char)rnd.Next(0, 0xFF);
            return new string(elems);
        }

        internal void TestRegex_GenerateInputFile(SymbolicRegexBuilder<BV> builder, int nrOfMatches, int randomTextSizeLimit, SymbolicRegexNode<BV> sr, int id)
        {
            string str = TestRegex_GenerateInput(builder, nrOfMatches, randomTextSizeLimit, sr);
            File.WriteAllText(regexinputsPath + "input." + id + ".txt", str, System.Text.Encoding.Unicode);
        }

        internal string TestRegex_GenerateInput(SymbolicRegexBuilder<BV> builder, int nrOfMatches, int randomTextSizeLimit, SymbolicRegexNode<BV> sr)
        {
            if (nrOfMatches < 1)
                throw new ArgumentOutOfRangeException();

            string str = sr.GenerateRandomMember();

            for (int i = 1; i < nrOfMatches; i++)
            {
                if (randomTextSizeLimit > 0)
                {
                    int k = rnd.Next(0, randomTextSizeLimit);
                    string tmp = sr.GenerateRandomMember();
                    int j = rnd.Next(1, tmp.Length);
                    str += tmp.Substring(0, j) + CreateRandomString(k) + tmp.Substring(j);
                }
                str += sr.GenerateRandomMember();
            }
            return str;
        }

        //[TestMethod]
        public void TestRegex_CompileToSymbolicRegex_Matches_IgnoreCaseTrue()
        {
            ClearLog();
            //GenerateInputFile();
            RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase;
            TestRegex_CompileToSymbolicRegex_Matches_Helper(options);
        }

        //[TestMethod]
        public void TestRegex_CompileToSymbolicRegex_Matches_IgnoreCaseFalse()
        {
            ClearLog();
            //GenerateInputFile();
            RegexOptions options = RegexOptions.Compiled;
            TestRegex_CompileToSymbolicRegex_Matches_Helper(options);
        }

        void ClearLog()
        {
            File.WriteAllText(myconsole, "");
        }

        void Log(string text)
        {
            File.AppendAllText(myconsole, text + "\r\n");
            Console.WriteLine(text);
        }

        [TestMethod]
        unsafe public void TestRegex_CompileToSymbolicRegex_Matches_Comparison()
        {
            RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
            //CharSetSolver css = new CharSetSolver();

            //1 sec timeout for matching
            Regex[] regexesall = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x, options, new TimeSpan(0, 0, 1)));
            string whynot;
            Regex[] regexes = Array.FindAll(regexesall, r => r.IsCompileSupported(out whynot));

            ClearLog();

            //make sure k is at most regexes.Length
            //int k = regexes.Length;
            int k_from = 1;
            int k_to = 50; // regexes.Length - 1; 
            int k = k_to - k_from + 1;

            int sr_comp_ms = System.Environment.TickCount;
            var srs = new IMatcher[k];
            var srs_U = new IMatcher[k];
            var srs_B = new IMatcher[k];
            for (int i = 0; i < k; i++)
            {
                srs[i] = regexes[k_from + i].Compile();
            }
            sr_comp_ms = System.Environment.TickCount - sr_comp_ms;

            for (int i = 0; i < k; i++)
            {
                srs_U[i] = regexes[k_from + i].Compile();
                srs_B[i] = regexes[k_from + i].Compile();
            }

            Log("Compile time(ms): " + sr_comp_ms);

            var str = File.ReadAllText(inputFile);
            var str1 = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (!(char.IsHighSurrogate(c) || char.IsLowSurrogate(c)))
                    str1.Append(c);
            }
            //eliminate surrogates
            str = str1.ToString();
            var bytes = System.Text.UnicodeEncoding.UTF8.GetBytes(str);
            Assert.IsFalse(Array.Exists(bytes, b => (b & 0xF0) == 0xF0));

            fixed (char* strp = str)
            {
                //------
                int sr_tot_ms = System.Environment.TickCount;
                int sr_tot_matches = 0;
                Tuple<int, int>[] sr_matches = null;
                for (int i = 0; i < k; i++)
                {
                    sr_matches = srs[i].Matches(str);
                    sr_tot_matches += sr_matches.Length;
                }
                sr_tot_ms = System.Environment.TickCount - sr_tot_ms;
                //--------------

                Log("Matches(string): " + sr_tot_ms);

                //------
                int sr_tot_ms_U = System.Environment.TickCount;
                int sr_tot_matches_U = 0;
                Tuple<int, int>[] sr_matches_U = null;
                for (int i = 0; i < k; i++)
                {
                    sr_matches_U = srs_U[i].Matches(str);
                    sr_tot_matches_U += sr_matches_U.Length;
                }
                sr_tot_ms_U = System.Environment.TickCount - sr_tot_ms_U;
                //--------------

                Log("Matches_(string): " + sr_tot_ms_U);

                ////------
                //int sr_tot_ms_B = System.Environment.TickCount;
                //int sr_tot_matches_B = 0;
                //Tuple<int, int>[] sr_matches_B = null;
                //for (int i = 0; i < k; i++)
                //{
                //    sr_matches_B = srs_B[i].Matches(bytes);
                //    sr_tot_matches_B += sr_matches_B.Length;
                //}
                //sr_tot_ms_B = System.Environment.TickCount - sr_tot_ms_B;
                ////--------------

                //Log("Matches(byte[]): " + sr_tot_ms_B);

                //var diff = new HashSet<Tuple<int, int>>(sr_matches);
                //diff.ExceptWith(sr_matches_B);

                Assert.IsTrue(sr_tot_matches == sr_tot_matches_U);
                //Assert.IsTrue(sr_tot_matches == sr_tot_matches_B);

                //check also that the the last match is the same
                Assert.AreEqual<Sequence<Tuple<int, int>>>(
                    new Sequence<Tuple<int, int>>(sr_matches),
                    new Sequence<Tuple<int, int>>(sr_matches_U));

                Console.WriteLine(string.Format("total: Matches(string):{0}ms, Matches_(char):{1}ms, matchcount={2}", sr_tot_ms, sr_tot_ms_U, sr_tot_matches));
            }
        }

        [TestMethod]
        public void TestRegex_CompileToSymbolicRegex_Matches()
        {
            try
            {
                RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
                //CharSetSolver css = new CharSetSolver();

                //1 sec timeout for matching
                Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x, options, new TimeSpan(0, 0, 1)));


                ClearLog();

                //make sure k is at most regexes.Length, regexes.Length is around 1600
                int k = 20;

                int sr_comp_ms = System.Environment.TickCount;
                var srs = new IMatcher[k];
                for (int i = 0; i < k; i++)
                    srs[i] = regexes[i].Compile();
                sr_comp_ms = System.Environment.TickCount - sr_comp_ms;

                Log("Compile time(ms): " + sr_comp_ms);

                var str = File.ReadAllText(inputFile);

                //first filter out those regexes that cause tiomeout in .net

                HashSet<int> timeouts = new HashSet<int>();
                if (k > 20)
                {
                    //some regexes above 20 cause timeouts, exclude those
                    //--- .net ---
                    for (int i = 0; i < k; i++)
                    {
                        try
                        {
                            var re_matches = regexes[i].Matches(str);
                            int tmp = re_matches.Count;
                            Log("ok: " + i);
                        }
                        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
                        {
                            timeouts.Add(i);
                            Log("timeout: " + i);
                        }
                    }
                }
                //-------------

                //--- aut ---
                int sr_tot_ms = System.Environment.TickCount;
                int sr_tot_matches = 0;
                for (int i = 0; i < k; i++)
                {
                    //here we could also allow the regexes that timed out in .net
                    //but the Assert below would fail
                    if (!timeouts.Contains(i))
                    {
                        var sr_matches = srs[i].Matches(str);
                        sr_tot_matches += sr_matches.Length;
                    }
                }
                sr_tot_ms = System.Environment.TickCount - sr_tot_ms;
                //--------------

                Log("AUT: " + sr_tot_ms);

                //--- .net ---
                int re_tot_ms = System.Environment.TickCount;
                int re_tot_matches = 0;
                for (int i = 0; i < k; i++)
                {
                    if (!timeouts.Contains(i))
                    {
                        var re_matches = regexes[i].Matches(str);
                        re_tot_matches += re_matches.Count;
                    }
                }
                re_tot_ms = System.Environment.TickCount - re_tot_ms;
                //--------------


                Log(".NET: " + re_tot_ms);

                //allow some variation (+- 5 in either direction)
                Assert.IsTrue(sr_tot_matches <= re_tot_matches + 5);
                Assert.IsTrue(re_tot_matches <= sr_tot_matches + 5);


                Console.WriteLine(string.Format("total: AUT:{0}ms, .NET:{1}ms, matchcount={2}", sr_tot_ms, re_tot_ms, re_tot_matches));
            }
            catch (Exception e)
            {
                //some regex options like lazy loop may not be implemented 
                Assert.IsTrue(e is AutomataException);
            }
        }


        public void GenerateInputFile()
        {
            if (!File.Exists(inputFile))
            {
                //CharSetSolver css = new CharSetSolver();
                Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x));
                IMatcher[] matchers = Array.ConvertAll(regexes, r => r.Compile());

                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < 1; j++)
                    for (int i = 0; i < regexes.Length; i++)
                    {
                        sb.Append(((SymbolicRegex<BV>)matchers[i]).A.GenerateRandomMember());
                        sb.Append(CreateRandomString(10000));
                    }
                File.WriteAllText(inputFile, sb.ToString(), Encoding.Unicode);
            }
        }

        [TestMethod]
        public void TestRegex_CompileToSymbolicRegex_IsMatch_IgnoreCaseTrue()
        {
            File.Delete(regexesPerfFile_IsMatch_IgnoreCaseTrue);
            RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase;
            TestRegex_CompileToSymbolicRegex_IsMatch_Helper(options, regexesPerfFile_IsMatch_IgnoreCaseTrue);
        }

        [TestMethod]
        public void TestRegex_CompileToSymbolicRegex_IsMatch_IgnoreCaseFalse()
        {
            File.Delete(regexesPerfFile_IsMatch_IgnoreCaseFalse);
            RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
            TestRegex_CompileToSymbolicRegex_IsMatch_Helper(options, regexesPerfFile_IsMatch_IgnoreCaseFalse);
        }

        //[TestMethod]
        public void FilterRegexes()
        {
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines("../../../regexes.txt"), x => new Regex(x, RegexOptions.None, new TimeSpan(0, 0, 1)));
            List<string> filtered = new List<string>();
            foreach (var regex in regexes)
            {
                var sr = (SymbolicRegex<BV>)regex.Compile();
                if (!sr.A.containsAnchors && !sr.A.isNullable)
                    filtered.Add(regex.ToString());
            }
            File.WriteAllLines(regexesFile, filtered.ToArray());
        }

        List<int> FindRegexesWithLazyLoops()
        {
            //set match timeout to 1 second for .net regexes
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesWithLazyLoops), x => new Regex(x, RegexOptions.None, new TimeSpan(0, 0, 1)));
            var res = new List<int>();
            for (int i = 0; i < regexes.Length; i++)
            {
                var re = regexes[i];
                string reasonWhyNotSupported;
                var ok = re.IsCompileSupported(out reasonWhyNotSupported);
                if (!ok)
                    if (reasonWhyNotSupported.Contains("lazy loop"))
                        res.Add(i);
            }
            return res;
        }
        //[TestMethod]
        public void Test_FindRegexesWithLazyLoops()
        {
            var res = FindRegexesWithLazyLoops();
            var regexes = File.ReadAllLines(regexesWithLazyLoops);
            Assert.IsTrue(res.Count == regexes.Length);
        }

        public void TestRegex_CompileToSymbolicRegex_Matches_Helper(RegexOptions options)
        {
            //set match timeout to 1 second for .net regexes
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x, options, new TimeSpan(0, 0, 1)));
            int sr_tot = 0;
            int re_tot = 0;
            //repeat each test that many times
            int matchRepeatCount = 1;
            int timeouts = 0;
            var str = File.ReadAllText(inputFile);
            //str = str.Substring(330850,50);
            var strArray = str.ToCharArray();
            int k = str.Length;
            //CharSetSolver css = new CharSetSolver();
            for (int i = 2; i < 10; i++)
            {
                var re = regexes[i];
                var sr = re.Compile();

                //-------------------------------
                //--------------------------------
                //--- measure time for sr.Matches
                Tuple<int, int>[] sr_res = null;
                int sr_t = System.Environment.TickCount;
                for (int j = 0; j < matchRepeatCount; j++)
                {
                    //the main work is done here
                    sr_res = sr.Matches(str);
                }
                sr_t = System.Environment.TickCount - sr_t;
                //-------------------------------

                //Assert.AreEqual<int>(sr_resU.Length, sr_res.Length);
                //Assert.AreEqual<int>(sr_resU2.Length, sr_res.Length);

                //--- measure time for .NET re.Matches
                int re_t = 0;
                Tuple<int, int>[] re_res = null;
                Match[] re_arr = new Match[] { };
                try
                {
                    re_t = System.Environment.TickCount;
                    for (int j = 0; j < matchRepeatCount; j++)
                    {
                        //the work is actually done when matches are accessed/counted
                        var re_matches = re.Matches(str);
                        re_arr = new Match[re_matches.Count];
                        re_matches.CopyTo(re_arr, 0);
                    }
                    re_t = System.Environment.TickCount - re_t;
                }
                catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
                {
                    //skip this entry because it causes timeout
                    File.AppendAllText(myconsole, string.Format("--- {1}, sr:{0}ms, re:timeout ---\r\n", sr_t, i + 1));
                    timeouts += 1;
                    continue;
                }
                re_res = Array.ConvertAll(re_arr, x => new Tuple<int, int>(x.Index, x.Length));
                //----------------------------

                // regex 1366 causes a .NET regex bug to give wrong result
                if (i != 1366)
                {
                    ValidateMatches(re, sr, str, sr_res, re_res);
                }
                // ---- due to small semantic differences this assert may not always be true ---
                //Assert.IsTrue(sr_res.Length == re_res.Length);
                //--- accumulate total matching times ---
                sr_tot += sr_t;
                re_tot += re_t;
                File.AppendAllText(myconsole, string.Format("{0}, sr:{1}ms({2}ms), re:{3}ms({4}ms)\r\n", i + 1, sr_t, sr_tot, re_t, re_tot));
            }
            Console.WriteLine(string.Format("total: sr:{0}ms, re:{1}ms", sr_tot, re_tot));
            Console.WriteLine(string.Format("re timeouts: {0}\r\n", timeouts));
        }

        public void TestRegex_CompileToSymbolicRegex_IsMatch_Helper(RegexOptions options, string regexesPerfFile)
        {
            //set match timeout to 1 second for .net regexes
            Regex[] regexesall = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x, options, new TimeSpan(0, 0, 1)));
            string whynot;
            var regexes = Array.FindAll(regexesall, r => r.IsCompileSupported(out whynot));

            int sr_tot_ms = 0;
            int re_tot_ms = 0;
            //repeat each test that many times
            int matchRepeatCount = 10;
            int randomBlockLimit = 10;
            int timeouts = 0;
            //CharSetSolver css = new CharSetSolver();
            for (int i = 0; i < 34; i++)
            {
                var re = regexes[i];
                var sr = re.Compile();

                var str = sr.GenerateRandomMatch();
                str = CreateRandomString(randomBlockLimit) + str + CreateRandomString(randomBlockLimit);

                //--------------------------------
                //--- measure time for sr.Matches
                int sr_t = System.Environment.TickCount;
                bool sr_res = false;
                for (int j = 0; j < matchRepeatCount; j++)
                {
                    sr_res = sr.IsMatch(str);
                }
                sr_t = System.Environment.TickCount - sr_t;
                //-------------------------------

                //--- measure time for .NET re.Matches
                int re_t = 0;
                bool re_res = false;
                Match[] re_arr = new Match[] { };
                try
                {
                    re_t = System.Environment.TickCount;
                    for (int j = 0; j < matchRepeatCount; j++)
                    {
                        re_res = re.IsMatch(str);
                    }
                    re_t = System.Environment.TickCount - re_t;
                }
                catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
                {
                    //skip this entry because it causes timeout
                    File.AppendAllText(regexesPerfFile, string.Format("--- {1}, sr:{0}ms, re:timeout ---\r\n", sr_t, i + 1));
                    timeouts += 1;
                    continue;
                }
                //---------------------------

                Assert.AreEqual<bool>(sr_res, re_res);
                sr_tot_ms += sr_t;
                re_tot_ms += re_t;
                int ratio = (sr_tot_ms == 0 ? -1 : re_tot_ms / sr_tot_ms);
                File.AppendAllText(regexesPerfFile, string.Format("{0}, sr:{1}ms({2}ms), re:{3}ms({4}ms)\r\n", i + 1, sr_t, sr_tot_ms, re_t, re_tot_ms));
                Console.WriteLine(string.Format("{0}, sr:{1}ms({2}ms), re:{3}ms({4}ms)\r\n", i + 1, sr_t, sr_tot_ms, re_t, re_tot_ms));

            }
            Console.WriteLine(string.Format("total: sr:{0}ms, re:{1}ms, speedup={2}\r\n", sr_tot_ms, re_tot_ms, re_tot_ms / (sr_tot_ms == 0 ? 1 : sr_tot_ms)));
            Console.WriteLine(string.Format("re timeouts: {0}\r\n", timeouts));
        }

        private static void ValidateMatches(Regex re, IMatcher sr, string str, Tuple<int, int>[] sr_res, Tuple<int, int>[] re_res)
        {

            //--- correctness check of different matches ---
            //Assert.IsTrue(re_matches.Count == sr_matches.Count);
            var sr_matches_minus_re_matches = new HashSet<Tuple<int, int>>(sr_res);
            sr_matches_minus_re_matches.ExceptWith(re_res);
            var re_matches_minus_sr_matches = new HashSet<Tuple<int, int>>(re_res);
            re_matches_minus_sr_matches.ExceptWith(sr_res);
            foreach (var pair in sr_matches_minus_re_matches)
            {
                Assert.IsTrue(re.IsMatch(str.Substring(pair.Item1, pair.Item2)));
                Assert.IsTrue(sr.IsMatch(str.Substring(pair.Item1, pair.Item2)));
            }
            foreach (var pair in re_matches_minus_sr_matches)
            {
                Assert.IsTrue(re.IsMatch(str.Substring(pair.Item1, pair.Item2)));
                Assert.IsTrue(sr.IsMatch(str.Substring(pair.Item1, pair.Item2)));
            }
        }

        [TestMethod]
        public void TestRegex_CompileToSymbolicRegex_one()
        {
            var regex = new Regex("[A-Za-z]+");
            var digits = new Regex("[0-9]{2,3}");
            var sr = regex.Compile();
            var sr_digits = digits.Compile();
            int size = 100;
            var dataset = new HashSet<string>();
            for (int i = 0; i < size; i++)
                dataset.Add(sr.GenerateRandomMatch());

            string str = "";
            foreach (var s in dataset)
            {
                str += s + sr_digits.GenerateRandomMatch();
            }
            var sr_matches = new HashSet<Tuple<int, int>>(sr.Matches(str));
            var regex_matches = new HashSet<Tuple<int, int>>();
            foreach (Match match in regex.Matches(str))
                regex_matches.Add(new Tuple<int, int>(match.Index, match.Length));
            Assert.IsTrue(regex_matches.SetEquals(sr_matches));
        }

        [TestMethod]
        public void TestRegex_Kelvin_Sign_Bug()
        {
            // '\u212A' is Kelvin sign
            // '\u212B' is Angstrom sign
            Regex r = new Regex("^[\u212A-\u212B]$", RegexOptions.IgnoreCase);
            Regex r2 = new Regex("^[\u212A\u212B]$", RegexOptions.IgnoreCase);
            Assert.IsTrue(r2.IsMatch("k"), "k must match");
            Assert.IsTrue(r2.IsMatch("K"), "K must match");
            Assert.IsTrue(r2.IsMatch("\u212A"), "Kelvin sign must match"); //Kelvin sign
            Assert.IsTrue(r2.IsMatch("\u212B"), "Angstrom sign must match"); //Angstrom sign
            Assert.IsTrue(r2.IsMatch("å")); //same as '\xE5' 
            Assert.IsTrue(r2.IsMatch("Å")); //same as '\xC5'
            //regexmatcher gives the correct semantics to both regexs
            var m = r.Compile();
            Assert.IsTrue(m.IsMatch("k"));
            Assert.IsTrue(m.IsMatch("K"));
            Assert.IsTrue(m.IsMatch("\u212A"));
            Assert.IsTrue(m.IsMatch("\u212B"));
            Assert.IsTrue(m.IsMatch("å"));
            Assert.IsTrue(m.IsMatch("Å"));
            //---
            var m2 = r2.Compile();
            Assert.IsTrue(m2.IsMatch("k"));
            Assert.IsTrue(m2.IsMatch("K"));
            Assert.IsTrue(m2.IsMatch("\u212A"));
            Assert.IsTrue(m2.IsMatch("\u212B"));
            Assert.IsTrue(m2.IsMatch("å"));
            Assert.IsTrue(m2.IsMatch("Å"));
            //----------------- BUG in .NET regex matcher ------
            //culprit: interval notation together with ignore-case 
            //particular instance here:
            //for the character '\u212A' (Kelvin sign)
            //Kelvin sign is considered equivalent to K and k when case is ignored
            //but this is not true if the interval [\u212A-\u212B] is used
            //rather than the equivalent [\u212A\u212B] is used
            Assert.IsFalse(r.IsMatch("k"), "k"); //<--- BUG ---
            Assert.IsFalse(r.IsMatch("K"), "K"); //<--- BUG ---
            Assert.IsFalse(r.IsMatch("\u212A"), "\u212A"); //<--- BUG ---
            Assert.IsFalse(r.IsMatch("\u212B"), "\u212B"); //<--- BUG ---
            Assert.IsFalse(r.IsMatch("å"), "å"); //<--- BUG ---
            Assert.IsFalse(r.IsMatch("Å"), "Å"); //<--- BUG ---
            //--------------------------------------------
            //works correctly without ignore case
            Regex r3 = new Regex("^[\u212A-\u212B]$");
            Assert.IsTrue(r3.IsMatch("\u212A"));
            Assert.IsTrue(r3.IsMatch("\u212B"));
            //-----------------------------------------
            //also, this problem does not occur always with interval + ignore case:
            Regex r4 = new Regex("^[k-l]$", RegexOptions.IgnoreCase);
            Assert.IsTrue(r4.IsMatch("\u212A"));
            Assert.IsTrue(r4.IsMatch("k"));
            Assert.IsTrue(r4.IsMatch("K"));
            Regex r5 = new Regex("^[kl]$", RegexOptions.IgnoreCase);
            Assert.IsTrue(r5.IsMatch("\u212A"));
            Assert.IsTrue(r5.IsMatch("k"));
            Assert.IsTrue(r5.IsMatch("K"));
            //related error for a different range 
            //clearly \u212B is in the interval [\u00FF-\u21FF] and adding ignore-case cannot remove it from the interval
            Regex r6 = new Regex("^[\u2129-\u212F]$", RegexOptions.IgnoreCase);
            Assert.IsTrue(r6.IsMatch("\u2129")); //<--- correct ---- turned greek small letter iota
            Assert.IsFalse(r6.IsMatch("\u212A"), "Kelvin sign"); //<--- BUG --- Kelvin sign
            Assert.IsFalse(r6.IsMatch("\u212B"), "Angstrom sign"); //<--- BUG --- Angstrom sign
            Assert.IsTrue(r6.IsMatch("\u212C")); //<--- correct --- script capital B
            Assert.IsTrue(r6.IsMatch("\u212D")); //<--- correct --- black-letter capital C
            Assert.IsTrue(r6.IsMatch("\u212E")); //<--- correct --- 'estimated' symbol
            Assert.IsTrue(r6.IsMatch("\u212F")); //<--- correct --- script small e
            Regex r7 = new Regex("^[\u2129-\u212F]$");
            Assert.IsTrue(r7.IsMatch("\u2129"));
            Assert.IsTrue(r7.IsMatch("\u212A"));
            Assert.IsTrue(r7.IsMatch("\u212B"));
            Assert.IsTrue(r7.IsMatch("\u212C"));
            Assert.IsTrue(r7.IsMatch("\u212D"));
            Assert.IsTrue(r7.IsMatch("\u212E"));
            Assert.IsTrue(r7.IsMatch("\u212F"));
            //---- separately it works as expected -----
            Regex r8 = new Regex("^å$", RegexOptions.IgnoreCase);
            Assert.IsTrue(r8.IsMatch("\u212B")); //Angstrom sign
            Assert.IsTrue(r8.IsMatch("å"));
            Assert.IsTrue(r8.IsMatch("Å"));
            Regex r9 = new Regex("^k$", RegexOptions.IgnoreCase);
            Assert.IsTrue(r9.IsMatch("\u212A")); //Kelvin sign
            Assert.IsTrue(r9.IsMatch("k"));
            Assert.IsTrue(r9.IsMatch("K"));
        }

        [TestMethod]
        public void TestRegex_TryCompile()
        {
            TestSuccess(@"foo.*bar");
            TestSuccess(@"(foo).+bar?");
            TestFailure(@"foo\bbar", @"\b");
            TestFailure(@"foo\Bbar", @"\B");
            TestFailure(@"foo\Gbar", @"\G");
            TestFailure(@"\w*?", "lazy loop");
            TestFailure(@"b*?", "lazy loop");
            TestFailure(@"a{3,4}?", "lazy loop");
            TestFailure(@"(\w)\1", "references");
            TestFailure(@"(?!un)\w+", "prevent constructs");
            TestFailure(@"(?<!un)\w+", "prevent constructs");
            TestFailure(@"(?<=un)\w+", "require constructs");
            TestFailure(@"(?=un)\w+", "require constructs");
            TestFailure(@"((?'4'a)(?(4))a|b)\w+", "test construct");
        }

        private static void TestFailure(string r, string reason)
        {
            var regex = new Regex(r);
            string failure;
            string whynot;
            RegexMatcher sr;
            bool ok = regex.TryCompile(out sr, out failure);
            bool ok2 = regex.IsCompileSupported(out whynot);
            Assert.IsFalse(ok);
            Assert.IsFalse(ok2);
            Assert.IsTrue(failure.Contains(reason));
            Assert.AreEqual<string>(failure, whynot);
            Assert.IsTrue(sr == null);
        }

        private static void TestSuccess(string r)
        {
            var regex = new Regex(r);
            string failure;
            RegexMatcher sr;
            bool ok = regex.TryCompile(out sr, out failure);
            Assert.IsTrue(ok);
            Assert.IsTrue(failure == "");
            Assert.IsTrue(sr != null);
        }

        [TestMethod]
        public void TestCompileToSymbolicRegex_StartupTime()
        {
            RegexOptions options = RegexOptions.None;
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x, options, new TimeSpan(0, 0, 1)));
            //var css = new CharSetSolver();
            int t = System.Environment.TickCount;
            var srs = new RegexMatcher[regexes.Length];
            int k = (regexes.Length < 100 ? regexes.Length : 100);
            for (int i = 0; i < regexes.Length; i++)
            {
                string reason;
                regexes[i].TryCompile(out srs[i], out reason);
            }
            t = System.Environment.TickCount - t;
            Console.WriteLine(t);
        }

        [TestMethod]
        public void TestRegex_Compile_Serialize_Deserialize_Match_Regexes()
        {
            TestRegex_Compile_Serialize_Deserialize_Match_Rexexes_helper();
        }

        [TestMethod]
        public void TestRegex_Compile_Serialize_Deserialize_Match_Regexes_1()
        {
            TestRegex_Compile_Serialize_Deserialize_Match_Rexexes_helper(1);
        }


        public void TestRegex_Compile_Serialize_Deserialize_Match_Rexexes_helper(int matchlimit = 0)
        {
            RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;

            //1 sec timeout for matching
            Regex[] regexes_ = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x, options, new TimeSpan(0, 0, 5)));
            string whynot;
            Regex[] regexes = Array.FindAll(regexes_, r => r.IsCompileSupported(out whynot));
            Regex[] regexes10 = Array.ConvertAll(regexes_, x => new Regex(x.ToString(), x.Options, new TimeSpan(0, 0, 10)));

            //var srs = (SymbolicRegexMatcher<BDD>[])Array.ConvertAll(regexes, r => r.Compile());

            ClearLog();

            int MAX = 10;

            //make sure k is at most regexes.Length, regexes.Length is around 1600
            int k = (regexes.Length < MAX ? regexes.Length : MAX);

            int sr_comp_ms = System.Environment.TickCount;
            var srs = new IMatcher[k];
            for (int i = 0; i < k; i++)
                srs[i] = regexes[i].Compile();
            sr_comp_ms = System.Environment.TickCount - sr_comp_ms;

            Log("Compile time(ms): " + sr_comp_ms);

            //var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //var formatter = new System.Web.UI.ObjectStateFormatter();
            //--- soap formatter does not support generic types
            //var formatter = new System.Runtime.Serialization.Formatters.Soap.SoapFormatter();

            int sr_ser_ms = System.Environment.TickCount;
            for (int i = 0; i < k; i++)
                SerializationTests.SerializeObjectToFile_soap(string.Format("sr{0}.soap", i), srs[i]);
            sr_ser_ms = System.Environment.TickCount - sr_ser_ms;

            Log("Serialization time(ms): " + sr_ser_ms);

            int sr_deser_ms = System.Environment.TickCount;
            var srs_ = new IMatcher[k];
            for (int i = 0; i < k; i++)
                srs_[i] = (IMatcher)SerializationTests.DeserializeObjectFromFile_soap(string.Format("sr{0}.soap", i));
            sr_deser_ms = System.Environment.TickCount - sr_deser_ms;

            Log("Deserialization time(ms): " + sr_deser_ms);

            var str = File.ReadAllText(inputFile);

            //first filter out those regexes that cause tiomeout in .net

            HashSet<int> timeouts = new HashSet<int>();
            timeouts.Add(4);
            timeouts.Add(137);


            //some regexes cause timeouts, exclude those
            //--- .net ---
            for (int i = 0; i < k; i++)
            {
                if (!timeouts.Contains(i))
                    try
                    {
                        var re_matches = regexes[i].Matches(str);
                        int tmp = re_matches.Count;
                        Log("ok: " + i);
                    }
                    catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
                    {
                        timeouts.Add(i);
                        Log("timeout: " + i);
                    }
            }

            //-------------


            //--- aut ---
            int sr_tot_ms = System.Environment.TickCount;
            int sr_tot_matches = 0;
            for (int i = 0; i < k; i++)
            {
                //here we could also allow the regexes that timed out in .net
                //but the Assert below would fail
                if (!timeouts.Contains(i))
                {
                    var sr_matches = srs[i].Matches(str, matchlimit);
                    sr_tot_matches += sr_matches.Length;
                }
            }
            sr_tot_ms = System.Environment.TickCount - sr_tot_ms;
            //--------------

            Log("AUT: " + sr_tot_ms);

            //--- deserialized versions ---
            int sr_tot_ms_d = System.Environment.TickCount;
            int sr_tot_matches_d = 0;
            for (int i = 0; i < k; i++)
            {
                //here we could also allow the regexes that timed out in .net
                //but the Assert below would fail
                if (!timeouts.Contains(i))
                {
                    var sr_matches_d = srs_[i].Matches(str, matchlimit);
                    sr_tot_matches_d += sr_matches_d.Length;
                }
            }
            sr_tot_ms_d = System.Environment.TickCount - sr_tot_ms_d;
            //--------------

            Assert.AreEqual<int>(sr_tot_matches, sr_tot_matches_d);

            Log("AUT_d: " + sr_tot_ms_d);

            //--- .net matching on regexes that did not timeout---
            //use here trhe regexes with larger timeout to avoid borderline cases
            int re_tot_ms = System.Environment.TickCount;
            int re_tot_matches = 0;
            for (int i = 0; i < k; i++)
            {
                if (!timeouts.Contains(i))
                {
                    var re_matches = regexes10[i].Matches(str);
                    re_tot_matches += (matchlimit <= 0 ? re_matches.Count : Math.Min(re_matches.Count, matchlimit));
                }
            }
            re_tot_ms = System.Environment.TickCount - re_tot_ms;
            //--------------


            Log(".NET: " + re_tot_ms);

            //allow some variation (+- 5 in either direction)
            Assert.IsTrue(sr_tot_matches <= re_tot_matches + 5);
            Assert.IsTrue(re_tot_matches <= sr_tot_matches + 5);


            Console.WriteLine(string.Format("total: AUT:{0}ms osr deser{3}, .NET:{1}ms, matchcount={2}", sr_tot_ms, re_tot_ms, re_tot_matches, sr_tot_ms_d));
        }

        [TestMethod]
        public void TestMatcher_InternalConsistency()
        {
            TestMatcher_InternalConsistency_(RegexOptions.Singleline);
        }

        void TestMatcher_InternalConsistency_(RegexOptions options)
        {
            var regexesFile = "../../../../Automata.Tests/Samples/simpl.txt";
            var regexes = Array.ConvertAll(File.ReadAllLines(regexesFile), x => new Regex(x, options));
            int k = regexes.Length;
            string badone = null;
            for (int i = 0; i < k; i++)
            {
                var regex = regexes[i];
                RegexMatcher matcher;
                string reasonwhyfailed;
                if (regex.TryCompile(out matcher, out reasonwhyfailed))
                {

                    //TBD:remove the condition once anchors are supported
                    if (matcher is SymbolicRegexUInt64 && ((SymbolicRegexUInt64)matcher).Pattern.containsAnchors)
                        continue;
                    else
                    {
                        for (int m = 0; m < 100; m++)
                        {
                            var input = matcher.GenerateRandomMatch();
                            var matches = matcher.Matches(input);
                            Assert.IsTrue(matches.Length == 1);
                            Assert.AreEqual(0, matches[0].Item1);
                            Assert.AreEqual(input.Length, matches[0].Item2);
                        }
                    }
                }
                else
                {
                    Assert.Fail("Regex compilation failed: " + reasonwhyfailed);
                }
            }
        }

        [TestMethod]
        public void TestMatcher_SemanticDifference_EarliestVsLeftmost()
        {
            var regex = new Regex(@"x+ax+|\(x+[^\)]*\)", RegexOptions.Singleline);
            RegexMatcher matcher;
            string reasonwhyfailed;
            if (regex.TryCompile(out matcher, out reasonwhyfailed))
            {
                //earliest eager is not the same as leftmost eager
                var input = "___(xxaxx this is some more stuff)";
                //leftmost eager finds the match "(xxaxx this is some more stuff)"
                var matches_net = regex.Matches(input);
                Assert.IsTrue(matches_net.Count == 1);
                Assert.AreEqual(3, matches_net[0].Index);
                Assert.AreEqual(input.Length - 3, matches_net[0].Length);
                //earliest eager finds the match "xxaxx"
                var matches = matcher.Matches(input);
                Assert.IsTrue(matches.Length == 1);
                Assert.AreEqual(4, matches[0].Item1);
                Assert.AreEqual(5, matches[0].Item2);
                //
                var input2 = "___(xxxxx this is some more stuff)";
                var matches_net2 = regex.Matches(input2);
                var matches2 = matcher.Matches(input2);
                Assert.IsTrue(matches_net2.Count == 1);
                Assert.AreEqual(3, matches_net2[0].Index);
                Assert.AreEqual(input2.Length - 3, matches_net2[0].Length);
                Assert.IsTrue(matches2.Length == 1);
                Assert.AreEqual(3, matches2[0].Item1);
                Assert.AreEqual(input2.Length - 3, matches2[0].Item2);
            }
            else
            {
                Assert.Fail("Regex compilation failed: " + reasonwhyfailed);
            }
        }

        [TestMethod]
        public void TestMatcher_StartAt_EndAt()
        {
            var A = new Regex("pw+d", RegexOptions.IgnoreCase);
            var B = new Regex(@"<ignore>[^><]*</ignore>");
            var input = "xxxxxxxxxxxxxaaPwwWDxx<ignore>PWWWWWWWD</ignore> <IGNORE>PWWD</IGNORE> xxpwwwwwwdxxx";
            var mA = A.Compile();
            var mB = B.Compile();
            bool done = false;
            int start = 0;
            var matches = new List<Tuple<int, int>>();
            while (!done)
            {
                //find at most one match from start
                var matches1 = mA.Matches(input, 1, start);
                if (matches1.Length == 0)
                {
                    //there are no more matches of A
                    done = true;
                }
                else
                {
                    //extract the match start and end 
                    var match_start = matches1[0].Item1;
                    var match_end = matches1[0].Item1 + matches1[0].Item2 - 1;
                    //decide if this match may be ignored by 
                    //checking if B applies to the immediate surrounding containg the match
                    //immediate surrounding here means 10 chars from start and end
                    var ignore = mB.IsMatch(input, match_start - 10, match_end + 10);
                    if (!ignore)
                    {
                        matches.Add(matches1[0]);
                    }
                    //continue from the next position after the found match
                    start = match_end + 1;
                }
            }
            Assert.IsTrue(matches.Count == 3);
            var PwwWD = input.Substring(matches[0].Item1, matches[0].Item2);
            var PWWD = input.Substring(matches[1].Item1, matches[1].Item2);
            var pwwwwwwd = input.Substring(matches[2].Item1, matches[2].Item2);
            Assert.AreEqual<string>("PwwWD", PwwWD);
            Assert.AreEqual<string>("PWWD", PWWD);
            Assert.AreEqual<string>("pwwwwwwd", pwwwwwwd);
            //observe that the second match PWWWWWWWD must be ignored
        }

        [TestMethod]
        public void TestIntersection()
        {
            var r1 = "^([abc][xyz])*#([mn][ij])*#([rst][uvw])*$";
            var r2 = "^([ax]{1000})*##$";
            CharSetSolver css = new CharSetSolver();
            var a1 = css.Convert(r1).Determinize().Minimize();
            var a2 = css.Convert(r2).Determinize().Minimize();
            var a = a1.Intersect(a2);
            //a.ShowGraph();
        }

        [TestMethod]
        public void TestLazyMatching()
        {
            var r = new Regex(@"sig=.*?[^a-z0-9]");
            var matcher = r.Compile();
            var input = "sig=foo;___sig=bar123;__sig=abc;";
            var matches = matcher.Matches(input);
            var r_matches = r.Matches(input);
            Assert.IsTrue(matches.Length == 3);
            Assert.IsTrue(r_matches.Count == 3);
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(r_matches[i].Index == matches[i].Item1 && r_matches[i].Length == matches[i].Item2);
        }

        [TestMethod]
        public void TestLazyMatching2()
        {
            var r = new Regex(@"[\\/]?[^\\/]*?(foobar|bazz)[^\\/]*?xxx");
            var matcher = r.Compile();
            var input = "foobar123xxxxxxyyy\n";
            var matches = matcher.Matches(input);
            var r_matches = r.Matches(input);
            Assert.IsTrue(matches.Length == r_matches.Count);
            for (int i = 0; i < matches.Length; i++)
                Assert.IsTrue(r_matches[i].Index == matches[i].Item1 && r_matches[i].Length == matches[i].Item2);
        }

        System.Runtime.Serialization.Formatters.Soap.SoapFormatter soap =
            new System.Runtime.Serialization.Formatters.Soap.SoapFormatter();

        [TestMethod]
        public void TestWatchdogs()
        {
            var r = new Regex("(?i:mar{4}gus|mar[gG]us|mar[kK]us|mam+a)");
            var matcher_ = (SymbolicRegexUInt64)r.Compile();
            matcher_.Serialize("test.soap", soap);
            var matcher = (SymbolicRegexUInt64)RegexMatcher.Deserialize("test.soap", soap);
            var input = "xxxxxxxxmarxxxxxxxmarrrrgusxxxmaRrrrgusxyzmarkusxxx";
            var matches = matcher.Matches(input);
            Assert.IsTrue(matches.Length == 3);
            var regex_matches = r.Matches(input);
            Assert.IsTrue(regex_matches.Count == 3);
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(matches[i].Item1 == regex_matches[i].Index && matches[i].Item2 == regex_matches[i].Length);
            //matcher.Pattern.ShowGraph(0,"regex_with_Watchdogs");
            //matcher.DotStarPattern.ShowGraph(0, "dotstar_regex_with_Watchdogs", false, true);
        }

        [TestMethod]
        public void TestAnchors()
        {
            var r = new Regex(@"^foo|bar$", RegexOptions.Multiline);
            var input = "foo0\nbar1foo\n234bar\nfooxxbar";
            var matches = r.Matches(input);
            var k = matches.Count;
            Assert.IsTrue(k == 4);
            var sr = r.Compile();
            var sr_matches = sr.Matches(input);
            Assert.IsTrue(sr_matches.Length == 4);
        }
    }
}
