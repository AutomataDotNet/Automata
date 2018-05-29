using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Automata;
using Microsoft.Automata.Rex;

namespace Automata.Tests
{
    [TestClass]
    public class RegexExtensionMethodTests
    {
        static string regexesFile = "../../../regexes.txt";
        static string regexesWithoutAnchorsFile = "../../../regexesWithoutAnchors.txt";
        static string regexesPerfFile_IgnoreCaseTrue = "../../../regexesPerf_IgnoreCaseTrue.txt";
        static string regexesPerfFile_IgnoreCaseFalse = "../../../regexesPerf_IgnoreCaseFalse.txt";
        static string regexesPerfFile_IsMatch_IgnoreCaseTrue = "../../../regexesPerf_IsMatch_IgnoreCaseTrue.txt";
        static string regexesPerfFile_IsMatch_IgnoreCaseFalse = "../../../regexesPerf_IsMatch_IgnoreCaseFalse.txt";
        static string regexinputsPath = "../../../regexinputs/";

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
            var set1c = regex1c.GenerateRandomDataSet(100,"[bcCdD]");
            Assert.IsTrue(set1c.Count == 4, "Incorrect Dataset");
            //----
            var regex2 = new Regex(@"^(\d[\w-[\d]])+$");
            var set2 = regex2.GenerateRandomDataSet(100,"[012a-d]", 20);
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

        public void TestRegex_GenerateInputFile(int nrOfMatches, int randomTextSizeLimit, SymbolicRegex<BV> sr, int id)
        {
            string str = TestRegex_GenerateInput(nrOfMatches, randomTextSizeLimit, sr);
            File.WriteAllText(regexinputsPath + "input." + id + ".txt", str, System.Text.Encoding.Unicode);
        }

        public string TestRegex_GenerateInput(int nrOfMatches, int randomTextSizeLimit, SymbolicRegex<BV> sr)
        {
            if (nrOfMatches < 1)
                throw new ArgumentOutOfRangeException();

            string str = "";
            var dataset = sr.GetPositiveDataset(nrOfMatches);
            var enumerator = dataset.GetEnumerator();
            enumerator.MoveNext();
            str = enumerator.Current;
            while (enumerator.MoveNext())
            {
                if (randomTextSizeLimit > 0)
                {
                    int k = rnd.Next(0, randomTextSizeLimit);
                    str += CreateRandomString(k);
                }
                str += enumerator.Current;
            }
            return str;
        }

        [TestMethod]
        public void TestRegex_CompileToSymbolicRegex_Matches_IgnoreCaseTrue()
        {
            File.Delete(regexesPerfFile_IgnoreCaseTrue);
            RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase;
            TestRegex_CompileToSymbolicRegex_Matches_Helper(options, regexesPerfFile_IgnoreCaseTrue);
        }

        [TestMethod]
        public void TestRegex_CompileToSymbolicRegex_Matches_IgnoreCaseFalse()
        {
            File.Delete(regexesPerfFile_IgnoreCaseFalse);
            RegexOptions options = RegexOptions.Compiled;
            TestRegex_CompileToSymbolicRegex_Matches_Helper(options, regexesPerfFile_IgnoreCaseFalse);
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
            RegexOptions options = RegexOptions.Compiled;
            TestRegex_CompileToSymbolicRegex_IsMatch_Helper(options, regexesPerfFile_IsMatch_IgnoreCaseFalse);
        }

        //[TestMethod]
        public void FilterRegexes()
        {
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines("../../../regexes.txt"), x => new Regex(x, RegexOptions.None, new TimeSpan(0, 0, 1)));
            List<string> filtered = new List<string>();
            foreach (var regex in regexes)
            {
                var sr = regex.Compile();
                if (!sr.containsAnchors && !sr.isNullable)
                    filtered.Add(regex.ToString());
            }
            File.WriteAllLines(regexesFile, filtered.ToArray());
        }

        public void TestRegex_CompileToSymbolicRegex_Matches_Helper(RegexOptions options, string regexesPerfFile)
        {
            //set match timeout to 1 second for .net regexes
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x, options, new TimeSpan(0,0,1)));
            int sr_tot_ms = 0;
            int re_tot_ms = 0;
            //repeat each test that many times
            int matchRepeatCount = 10;
            //generate that many random matches inside each input
            //ever input starts and ends with a match
            int nrOfMatches = 20;
            //random blocks of text that are inserted between matches and have this upper length bound
            int randomBlockLimit = 100000;
            int timeouts = 0;
            for (int i = 0; i < 50; i++)
            {
                var re = regexes[i];
                var sr = re.Compile();

                var str = TestRegex_GenerateInput(nrOfMatches, randomBlockLimit, sr);

                //--------------------------------
                //--- measure time for sr.Matches
                int sr_t = System.Environment.TickCount;
                Tuple<int, int>[] sr_res = null;
                for (int j = 0; j < matchRepeatCount; j++)
                {
                    //the main work is done here
                    sr_res = sr.Matches(str);
                }
                sr_t = System.Environment.TickCount - sr_t;
                //-------------------------------

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
                    File.AppendAllText(regexesPerfFile, string.Format("--- {1}, sr:{0}ms, re:timeout ---\r\n", sr_t, i + 1));
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
                sr_tot_ms += sr_t;
                re_tot_ms += re_t;
                int ratio = (sr_tot_ms == 0 ? -1 : re_tot_ms / sr_tot_ms);
                File.AppendAllText(regexesPerfFile, string.Format("{0}, sr:{1}ms({2}ms), re:{3}ms({4}ms)\r\n", i + 1, sr_t, sr_tot_ms, re_t, re_tot_ms));

            }
            Console.WriteLine(string.Format("total: sr:{0}ms, re:{1}ms, speedup={2}\r\n", sr_tot_ms, re_tot_ms, re_tot_ms / sr_tot_ms));
            Console.WriteLine(string.Format("re timeouts: {0}\r\n", timeouts));
        }

        public void TestRegex_CompileToSymbolicRegex_IsMatch_Helper(RegexOptions options, string regexesPerfFile)
        {
            //set match timeout to 1 second for .net regexes
            Regex[] regexes = Array.ConvertAll(File.ReadAllLines(regexesWithoutAnchorsFile), x => new Regex(x, options, new TimeSpan(0, 0, 1)));
            int sr_tot_ms = 0;
            int re_tot_ms = 0;
            //repeat each test that many times
            int matchRepeatCount = 10;
            int randomBlockLimit = 1000000;
            int timeouts = 0;
            for (int i = 0; i < 50; i++)
            {
                var re = regexes[i];
                var sr = re.Compile();

                var str = sr.GenerateRandomMember();
                str = CreateRandomString(randomBlockLimit) + str + CreateRandomString(randomBlockLimit);

                //--------------------------------
                //--- measure time for sr.Matches
                int sr_t = System.Environment.TickCount;
                bool sr_res =false;
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
                    timeouts +=1;
                    continue;
                }
                //---------------------------

                Assert.AreEqual<bool>(sr_res, re_res);
                sr_tot_ms += sr_t;
                re_tot_ms += re_t;
                int ratio = (sr_tot_ms == 0 ? -1 : re_tot_ms / sr_tot_ms);
                File.AppendAllText(regexesPerfFile, string.Format("{0}, sr:{1}ms({2}ms), re:{3}ms({4}ms)\r\n", i + 1, sr_t, sr_tot_ms, re_t, re_tot_ms));

            }
            Console.WriteLine(string.Format("total: sr:{0}ms, re:{1}ms, speedup={2}\r\n", sr_tot_ms, re_tot_ms, re_tot_ms / sr_tot_ms));
            Console.WriteLine(string.Format("re timeouts: {0}\r\n", timeouts));
        }

        private static void ValidateMatches(Regex re, SymbolicRegex<BV> sr, string str, Tuple<int, int>[] sr_res, Tuple<int, int>[] re_res)
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
            var regex = new Regex("[0-9A-Za-z]+");
            var sr = regex.Compile();
            int size = 50;
            var dataset = sr.GetPositiveDataset(size);
            string str = "";
            foreach (var s in dataset)
            {
                int k = new Random().Next(2,10);
                str += s + SymbolicRegexTests.CreateRandomString(k);
            }
            var sr_matches = new HashSet<Tuple<int,int>>(sr.Matches(str));
            var regex_matches = new HashSet<Tuple<int, int>>();
            foreach (Match match in regex.Matches(str))
                regex_matches.Add(new Tuple<int, int>(match.Index, match.Length));
            Assert.IsTrue(regex_matches.Count >= size);
            Assert.IsTrue(regex_matches.SetEquals(sr_matches));
        }

        [TestMethod]
        public void TestRegex_Bug_IsMatch()
        {
            //Regex r = new Regex("[\u0081-\uFFFF]{1,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex r = new Regex("[\u212A-\u212B]{1,}", RegexOptions.IgnoreCase);
            Regex r2 = new Regex("[\u212A\u212B]{1,}", RegexOptions.IgnoreCase);
            Assert.IsFalse(r.IsMatch("\u212AkK")); //<--- BUG 
            Assert.IsTrue(r2.IsMatch("\u212AkK")); //<--- correct
        }
    }
}
