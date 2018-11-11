using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.Automata;
using Microsoft.Automata.Utilities;
using Microsoft.Automata.Rex;

namespace Automata.Tests
{
    [TestClass]
    public class CodeGenTests
    {
        static string regexesFile = "../../../samples/regexes.txt";
        internal static int NrOfStrings = 100;
        internal static int MaxStringLength = 100;
        internal static int Repetitions = 10;

        [TestMethod]
        public void gen_cpp_TestRegex2cpp() 
        {
            Regex Regex9 = new Regex(@"^(?i:www\.bing\.com)$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex Regex10 = new Regex(@"^(?i:SERP|Web|Auth|FacebookConnect)$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex Regex11 = new Regex(@"^(?i:\[])$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex Regex12 = new Regex(@"^(?i:\s*)$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex Regex13 = new Regex(@"^(?i:0AB26296E0B262B222FB6A89E1106324)$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex Regex14 = new Regex(@"^(?i:Bing\.com)$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex Regex15 = new Regex(@"^(?i:beta.*)$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex Regex16 = new Regex(@"^(?i:osjsonrankedct|osjsonranked)$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex EvilRegex = new Regex(@"^(([a-z])+.)+[A-Z]([a-z])+$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex abplus = new Regex(@"^(ab)+$", RegexOptions.Compiled | (RegexOptions.Singleline));
            Regex margus = new Regex(@"^(?i:margus)Veanes$");
            Regex wdplusASCII = new Regex(@"^([\w-[\x80-\uFFFF]][\d-[\x80-\uFFFF]])+$");
            Regex wdplus = new Regex(@"^(\w\d\s)+$");

            var regexes = new Regex[] {Regex9, Regex10, Regex11, Regex12, Regex13, Regex14, Regex15, Regex16, EvilRegex, abplus, margus, wdplusASCII, wdplus};

            //var regexes = new Regex[] {  wdplus };

            TestCppCodeGen(regexes);  

            //TestRegex(Regex16);

            //TestIgnoreCase();

            ////Test3();
            //TestCodeGen();
            //return;
            //var z3 = new Z3Provider(BitWidth.BV7);
            //string regex = "^(([a-z])+.)+[A-Z]([a-z])+$";
            //var sfa = z3.CharSetProvider.Convert(regex, RegexOptions.Singleline).RemoveEpsilons(z3.CharSetProvider.MkOr);
            //var sfaDet = sfa.Determinize(z3.CharSetProvider);
            //var sfaMin = sfaDet.Minimize(z3.CharSetProvider);
            //z3.CharSetProvider.ShowGraph(sfa, "sfa");
            ////z3.CharSetProvider.ShowGraph(sfaDet, "sfaDet");
            //z3.CharSetProvider.ShowGraph(sfaMin, "sfaMin");

            //var regexMin = z3.CharSetProvider.ConvertToRegex(sfaMin);
            //var res3 = Regex.IsMatch("aaaaAaaaa", regex);
            //var res3b = Regex.IsMatch("aaaaAaaaa", regexMin);

            //string s = "a";
            //int t1;
            //int t2;
            //for (int i = 0; i < 35; i++)
            //{
            //    t1 = System.Environment.TickCount;
            //    var res4 = Regex.IsMatch(s, regex);
            //    t1 = System.Environment.TickCount - t1;
            //    t2 = System.Environment.TickCount;
            //    var res4b = Regex.IsMatch(s, regexMin);
            //    t2 = System.Environment.TickCount - t2;
            //    Console.WriteLine(s.Length + ", " + t1 + "," + t2);
            //    s += "a";
            //}

            //var sfaMinExpr = z3.ConvertAutomatonGuardsToExpr(sfaMin);
            //var sfaExpr = new SFA<FuncDecl, Expr, Sort>(z3, z3.CharacterSort, sfaMinExpr);
            //var st = ST<FuncDecl, Expr, Sort>.SFAtoST(sfaExpr);
            //var stb = st.ToSTb();
            //var csAcceptor = stb.Compile("RegexTransfomer", "SampleAcceptor", false, true);
            //var res = csAcceptor.Apply("aaa");
            //var res2 = csAcceptor.Apply("aaaAaaa");
            ////Console.WriteLine("res=" + (res == null ? false : true));
            ////Console.WriteLine("res2=" + (res2 == null ? false : true));
            ////Console.WriteLine("res3=" + (res3 == null ? false : true));
            ////Console.ReadKey();
        }

        //private static void TestIgnoreCase()
        //{
        //    Microsoft.Automata.Utilities.IgnoreCaseRelationGenerator.Generate(
        //        "Microsoft.Automata.Generated",
        //        "IgnoreCaseRelation",
        //    @"C:\GitHub\AutomataDotNet\Automata\src\Automata\Internal\Generated");
        //}

        static void TestCppCodeGen(Regex[] regexes)
        {
            Automaton<BDD>[] automata = new Automaton<BDD>[regexes.Length];
            Automaton<BDD>[] Cautomata = new Automaton<BDD>[regexes.Length];
            var solver = new CharSetSolver();

            #region convert the regexes to automata
            Console.Write("Converting {0} regexes to automata and minimizing the automata ...", regexes.Length);
            int t = System.Environment.TickCount;
            Func<Automaton<BDD>, bool> IsFull = (a => a.StateCount == 1 && a.IsFinalState(a.InitialState) && a.IsLoopState(a.InitialState) && a.GetMovesCountFrom(a.InitialState) == 1 && a.GetMoveFrom(a.InitialState).Label.Equals(solver.True));
            for (int i = 0; i < regexes.Length; i++)
            {
                try
                {
                    var aut = CppCodeGenerator.Regex2Automaton(solver, regexes[i]);
                    automata[i] = aut;
                    if (IsFull(automata[i]) || automata[i].IsEmpty)
                    {
                        Console.WriteLine("\nReplacing trivial regex \"{0}\" with \"^dummy$\"", i, regexes[i]);
                        regexes[i] = new Regex("^dummy$");
                        automata[i] = CppCodeGenerator.Regex2Automaton(solver, regexes[i]);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nCoverting regex {0}: '{1}' failed, reason: {2}, replacing with \"^dummy$\"", i, regexes[i], e.Message);
                    regexes[i] = new Regex("^dummy$");
                    automata[i] = CppCodeGenerator.Regex2Automaton(solver, regexes[i]);
                }
            }
            t = System.Environment.TickCount - t;
            Console.WriteLine(string.Format(" done ({0}ms)", t));
            #endregion

            #region complement the automata
            t = System.Environment.TickCount;
            Console.Write("Creating complements of autmata ...");
            for (int i = 0; i < regexes.Length; i++)
            {
                Cautomata[i] = automata[i].Complement().Minimize();
            }
            t = System.Environment.TickCount - t;
            Console.WriteLine(string.Format(" done ({0}ms)", t));
            #endregion

            #region generate positive test strings
            Console.Write(string.Format("Generating a positive test set for all automata ", NrOfStrings));
            t = System.Environment.TickCount;
            List<string[]> members = new List<string[]>();
            List<string[]> Cmembers = new List<string[]>();
            for (int id = 0; id < automata.Length; id++)
            {
                Console.Write(".");
                var M = automata[id].Intersect(solver.Convert("^[\0-\x7F]{0," + CodeGenTests.MaxStringLength + "}$", RegexOptions.Singleline)).Determinize();
                var tmp = new string[NrOfStrings];
                int time = System.Environment.TickCount;
                for (int i = 0; i < NrOfStrings; i++)
                {
                    tmp[i] = solver.GenerateMemberUniformly(M);
                    //if (i % 10 == 0)
                    //    Console.Write(".");
                }
                time = System.Environment.TickCount - time;
                members.Add(tmp);
            }
            t = System.Environment.TickCount - t;
            Console.WriteLine(string.Format(" done ({0}ms)", t));
            #endregion

            #region generate negative test strings
            t = System.Environment.TickCount;
            Console.Write(string.Format("Generating a negative test set for all automata ", NrOfStrings));
            for (int id = 0; id < Cautomata.Length; id++)
            {
                Console.Write(".");
                //var M = Cautomata[id].Intersect(solver.Convert("^[^\uD800-\uDFFF]{0,100}$", RegexOptions.Singleline), solver).Determinize(solver);
                var M = Cautomata[id].Intersect(solver.Convert("^[\0-\uFFFF]{0,100}$", RegexOptions.Singleline)).Determinize();
                var tmp = new string[NrOfStrings];
                for (int i = 0; i < NrOfStrings; i++)
                {
                    tmp[i] = solver.GenerateMemberUniformly(M);
                    //if (i % 10 == 0)
                    //    Console.Write(".");
                }
                Cmembers.Add(tmp);
            }
            t = System.Environment.TickCount - t;
            Console.WriteLine(string.Format(" done ({0}ms)", t));
            #endregion

            #region generate c++

            int t2 = System.Environment.TickCount;
            CppTest.Compile(automata, solver, true);
            t2 = System.Environment.TickCount - t2;
            Console.WriteLine(string.Format(" done ({0}ms)", t2));
            #endregion

            #region convert the test strings to UTF8
            List<byte[][]> membersUTF8 = new List<byte[][]>();
            List<byte[][]> CmembersUTF8 = new List<byte[][]>();
            for (int id = 0; id < automata.Length; id++)
            {
                var tmp = new byte[NrOfStrings][];
                for (int i = 0; i < NrOfStrings; i++)
                    tmp[i] = Encoding.UTF8.GetBytes(members[id][i]);
                membersUTF8.Add(tmp);
            }
            for (int id = 0; id < Cautomata.Length; id++)
            {
                var tmp = new byte[NrOfStrings][];
                for (int i = 0; i < NrOfStrings; i++)
                    tmp[i] = Encoding.UTF8.GetBytes(Cmembers[id][i]);
                CmembersUTF8.Add(tmp);
            }
            #endregion

            #region compute tot nr of bits
            double bits = 0;
            for (int id = 0; id < automata.Length; id++)
            {
                int nrBytes = 0;
                for (int i = 0; i < NrOfStrings; i++)
                    nrBytes += membersUTF8[id][i].Length + CmembersUTF8[id][i].Length;
                bits += (nrBytes * 8.0);
            }
            bits = bits * CodeGenTests.Repetitions; //repeated Reps times
            #endregion

            #region run c++ tests
            Console.Write("Running c++  tests ... ");
            double totsec_cpp = 0;
            for (int id = 0; id < automata.Length; id++)
            {
                double sec_cpp = 0;
                int accepted = CppTest.Test(true, id, membersUTF8[id], members[id], out sec_cpp);
                totsec_cpp += sec_cpp;
                int Caccepted = CppTest.Test(false, id, CmembersUTF8[id], Cmembers[id], out sec_cpp);
                totsec_cpp += sec_cpp;
            }
            double bps_cpp = bits / totsec_cpp;
            double mbps_cpp = (bps_cpp / 1000000.0);
            int Mbps_cpp = (int)Math.Round(mbps_cpp);
            Console.WriteLine(string.Format("{0}sec, throughput = {1}Mbps", totsec_cpp, Mbps_cpp));
            #endregion

            #region run .NET tests
            Console.Write("Running .NET tests ... ");
            double totsec_net = 0;
            for (int id = 0; id < automata.Length; id++)
            {
                DotNetTest.Compile(regexes[id]); //make sure each regex is precompiled
                double sec_net;
                int accepted2 = DotNetTest.Test(true, members[id], out sec_net);
                totsec_net += sec_net;
                int Caccepted2 = DotNetTest.Test(false, Cmembers[id], out sec_net);
                totsec_net += sec_net;
            }
            double bps_net = bits / totsec_net;
            double mbps_net = (bps_net / 1000000.0);
            int Mbps_net = (int)Math.Round(mbps_net);
            Console.WriteLine(string.Format("{0}sec, throughput = {1}Mbps", totsec_net, Mbps_net));
            #endregion

            Console.WriteLine(string.Format("speedup (.NET-time/c++-time) = {0}X", ((int)Math.Round(totsec_net / totsec_cpp))));
        }


        static void TestRegex(Regex regex)
        {
            var solver = new CharSetSolver();
            string myregex = regex.ToString();

            //Regex.CompileToAssembly(new RegexCompilationInfo[] { new RegexCompilationInfo(myregex, RegexOptions.None, "EvilRegex", "RegexTransfomer", true) },
            //    new System.Reflection.AssemblyName("EvilRegex"));

            var sfa = solver.Convert(myregex, regex.Options).RemoveEpsilons();
            var sfaDet = sfa.Determinize();
            var sfaMin = sfaDet.Minimize();

            //solver.ShowGraph(sfa, "sfa");
            //solver.ShowGraph(sfaDet, "sfaDet");
            //solver.ShowGraph(sfaMin, "sfaMin");

            var cs = solver.ToCS(sfaMin, true, "MyRegex", "RegexTransfomer");

            var regexMin = solver.ConvertToRegex(sfaMin);

            Console.WriteLine("------- given regex --------");
            Console.WriteLine(myregex);
            Console.WriteLine("----------------------------");

            Console.WriteLine("-------- regexMin ----------");
            Console.WriteLine(regexMin);
            Console.WriteLine("----------------------------");

            Console.WriteLine("-------- cs ----------------");
            // Console.WriteLine(cs.SourceCode);
            Console.WriteLine("----------------------------");

            string sIn = solver.GenerateMember(sfaMin);
            string sOut = solver.GenerateMember(sfaMin.Complement());
            string s = sIn;
            int t1;
            //int t2;
            int t3;
            for (int i = 0; i < 2; i++)
            {
                //original regex
                t1 = System.Environment.TickCount;
                bool res1 = false;
                for (int j = 0; j < 100000; j++)
                    res1 = Regex.IsMatch(s, regex.ToString(), regex.Options);
                //res1 = evilregex.IsMatch(s);
                t1 = System.Environment.TickCount - t1;

                ////minimized regex
                //t2 = System.Environment.TickCount;
                //bool res2 = false;
                //for (int j = 0; j < 100000; j++)
                //    res2 = Regex.IsMatch(s, regexMin, regex.Options);
                //t2 = System.Environment.TickCount - t2;

                //code from minimized regex
                t3 = System.Environment.TickCount;
                bool res3 = false;
                for (int j = 0; j < 100000; j++)
                    res3 = cs.IsMatch(s);
                t3 = System.Environment.TickCount - t3;
                Console.WriteLine(String.Format("{0}ms({1}), {2}ms({3})", t1, res1, t3, res3));
                s = sOut;
            }
            Console.WriteLine("done...(press any key)");
            Console.ReadKey();
        }

        [TestMethod]
        public void gen_csharp_TestRegex2csharp()
        {
            var solver = new CharSetSolver();
            string regex = @"^(\w\d)+$";
            var sfa = solver.Convert(regex, RegexOptions.Singleline).RemoveEpsilons();
            var sfaDet = sfa.Determinize();
            var sfaMin = sfaDet.Minimize();
            //solver.ShowGraph(sfa, "sfa");
            //solver.ShowGraph(sfaDet, "sfaDet");
            //solver.ShowGraph(sfaMin, "sfaMin");

            var cs = solver.ToCS(sfaMin, true, "Regex1", "RegexTransfomer");

            var yes = cs.IsMatch("a1b2b4");
            var no = cs.IsMatch("r5t6uu");

            //Console.WriteLine(cs.SourceCode);
            //Console.ReadLine();
            Assert.IsTrue(yes);
            Assert.IsFalse(no);
        }

        [TestMethod]
        public void gen_chsarp_TestSampleRegexes2csharp()
        {
            var solver = new CharSetSolver(BitWidth.BV16);
            List<string> regexesAll = new List<string>(File.ReadAllLines(regexesFile));
            List<int> timedout = new List<int>();
            List<int> excluded = new List<int>(new int[] { 36, 50, 64, 65, 162, 166, 210, 238, 334, 355, 392, 455, 
                471, 490, 594, 611, 612, 671, 725, 731, 741, 760, 775, 800, 852, 
                870, 873, 880, 882, 893, 923, 991, 997, 1053, 1062, 1164, 1220, 
                1228, 1273, 1318, 1339, 1352, 1386, 1404, 1413, 1414, 1423, 1424, 
                1429, 1431, 1434, 1482, 1487, 1516, 1517, 1518, 1519, 1520, 1537, 
                1565, 1566, 1635, 1744, 1749, 1829, 1868 });
            List<string> regexes = new List<string>();

            for (int i = 1; i < regexesAll.Count; i++)
                if (!excluded.Contains(i))
                    regexes.Add(regexesAll[i]);

            int K = 10; //number of pos/neg strings to be generated for each regex
            for (int i = 1; i < 100; i++)
            {
                try
                {
                    var regex = regexes[i];
                    var aut = solver.Convert(regex, RegexOptions.Singleline);
                    var autDet = aut.Determinize(2000);
                    var autMin = autDet.Minimize();
                    var autMinC = aut.Complement();
                    if (autMin.IsEmpty || autMinC.IsEmpty || autMinC.IsEpsilon)
                        continue;

                    CheckIsClean(autMin);

                    //var autMinExpr = z3.ConvertAutomatonGuardsToExpr(autMin);
                    //var sfa = new SFA<FuncDecl, Expr, Sort>(z3, z3.CharacterSort, autMinExpr);
                    //var stbb = new STBuilder<FuncDecl, Expr, Sort>(z3);
                    //var st = ST<FuncDecl, Expr, Sort>.SFAtoST(sfa);
                    //var stb = st.ToSTb();
                    ////var csAcceptor = stb.Compile("RegexTransfomer", "SampleAcceptor", false, true);

                    var csAcceptor = solver.ToCS(autMin);

                    HashSet<string> posSamples = new HashSet<string>();
                    HashSet<string> negSamples = new HashSet<string>();
                    int k = autMin.FindShortestFinalPath(autMin.InitialState).Item1.Length;
                    var maxLengthAut = solver.Convert("^.{0," + (3 * k) + "}$").Determinize().Minimize();
                    int tries = 0;
                    var aut1 = autMin.Intersect(maxLengthAut);
                    while (posSamples.Count < K && tries < 10 * K)
                    {
                        var s = solver.GenerateMemberUniformly(aut1);
                        if (!s.EndsWith("\n"))
                            if (!posSamples.Add(s))
                                tries++;
                    }
                    tries = 0;
                    int k2 = autMinC.FindShortestFinalPath(autMinC.InitialState).Item1.Length;
                    var maxLengthAut2 = solver.Convert("^.{0," + (3 * k2) + "}$").Determinize().Minimize();
                    var autMinCprefix = autMinC.Intersect(maxLengthAut2);
                    while (negSamples.Count < K && tries < 10 * K)
                    {
                        var s = solver.GenerateMemberUniformly(autMinCprefix);
                        if (!s.EndsWith("\n"))
                            if (!negSamples.Add(s))
                                tries++;
                    }

                    foreach (string s in posSamples)
                    {
                        if (!RexEngine.IsMatch(s, regex, RegexOptions.Singleline))
                        {
                            Console.WriteLine("match expected regex:" + i);
                            break;
                        }
                        if (!csAcceptor.IsMatch(s))
                        {
                            Console.WriteLine("match expected regex:" + i);
                            break;
                        }
                    }
                    foreach (string s in negSamples)
                    {
                        if (RexEngine.IsMatch(s, regex, RegexOptions.Singleline))
                        {
                            Console.WriteLine("mismatch expected regex:" + i);
                            break;
                        }
                        if (csAcceptor.IsMatch(s))
                        {
                            Console.WriteLine("mismatch expected regex:" + i);
                            break;
                        }
                    }
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("timeout regex:" + i);
                    timedout.Add(i);
                    continue;
                }
            }
        }

        private void CheckIsClean(Automaton<BDD> aut)
        {
            foreach (var m in aut.GetMoves())
                if (m.Label.IsEmpty)
                    Assert.IsFalse(true);
        }
    }

    internal class CppTest
    {
        internal const string DllFilePath = "test.dll";
        internal static string SourceFile = "test.cpp";
        internal static string BatFile = "test.bat";
        internal static string Directory
        {
            get { return System.Environment.CurrentDirectory + @"\"; }
        }
        internal static string SourceFilePath
        {
            get { return Directory + SourceFile; }
        }
        internal static string BatFilePath
        {
            get { return Directory + BatFile; }
        }

        static void Initialize()
        {
            //create the bat file for compiling the C++ source file
            DirectoryInfo di = new DirectoryInfo(Directory);
            if (!di.Exists)
                di.Create();
            FileInfo fi = new FileInfo(BatFilePath);
            if (fi.Exists)
                fi.IsReadOnly = false;
            StreamWriter sw = new StreamWriter(CppTest.BatFilePath);
            string platform = "";
#if X64
            platform = "amd64";
#endif
            sw.WriteLine(@"
call ""C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\vcvarsall.bat"" " + platform + @"
cl /LD " + SourceFile); //option /O2 takes too much time
            sw.Close();
        }

        internal static void Compile(Automaton<BDD>[] automata, CharSetSolver solver, bool optimize4ascii)
        {
            Initialize();
            Console.Write("Generating c++ ... ");
            solver.ToCppFile(automata, SourceFilePath, true, optimize4ascii);
            Console.Write("compiling c++ ... ");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo();
            p.StartInfo.WorkingDirectory = Directory;
            p.StartInfo.FileName = BatFilePath;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = true;
            var res = p.Start();
            p.WaitForExit(5000);
            p.Close();
        }

        [System.Runtime.InteropServices.DllImport(DllFilePath, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private extern static int IsMatch(int index, int size, byte[] str);

        /// <summary>
        /// Returns the number of accepted strings from the given list of strings.
        /// Outputs the total time taken in seconds.
        /// </summary>
        public static int Test(bool accept, int index, byte[][] strs, string[] strings, out double sec)
        {
            //avoid measuring the unmanaged dll loading time
            //fixed (byte* pArr = strs[0])
            //    IsMatch(index, strs[0].Length, pArr);
            IsMatch(index, strs[0].Length, strs[0]);

            int totAccepted = 0;
            int totTime = System.Environment.TickCount;
            for (int i = 0; i < strs.Length; i++)
            {
                int res = 0;

                //fixed (byte* pArr = strs[i]) //avoid marshalling
                {
                    var pArr = strs[i];
                    for (int j = 0; j < CodeGenTests.Repetitions; j++)
                        res = IsMatch(index, strs[i].Length, pArr);
                    if (res == 1)
                        totAccepted += 1;
                    if (accept && (res == 0))
                        Assert.Fail(string.Format("CppTest Positive mismatch: regex {0} should accept {1}", index, StringUtility.Escape(strings[i])));
                    if (!accept && (res == 1))
                        Assert.Fail(string.Format("CppTest Negative mismatch: regex {0} should reject {1}", index, StringUtility.Escape(strings[i])));
                }
            }
            totTime = System.Environment.TickCount - totTime;
            sec = ((double)totTime) / (1000.0);
            return totAccepted;
        }
    }

    internal class DotNetTest
    {
        static Regex compiledregex;
        internal static void Compile(Regex regex)
        {
            //Regex.CompileToAssembly(new RegexCompilationInfo[] { new RegexCompilationInfo(regex.ToString(), regex.Options, "MyRegex", "RegexTransfomer", true) },
            //    new System.Reflection.AssemblyName("MyRegex"));
            //Assembly assembly = Assembly.LoadFrom("MyRegex.dll");
            //Type type = assembly.GetTypes()[2];
            //compiledregex = Activator.CreateInstance(type) as Regex; //creates an instance of that class
            compiledregex = new Regex(regex.ToString(), RegexOptions.Compiled | regex.Options);
        }

        /// <summary>
        /// Returns the number of accepted strings from the given list of strings.
        /// Outputs the total time taken in seconds.
        /// </summary>
        public static int Test(bool accept, string[] strs, out double sec)
        {
            int totAccepted = 0;
            bool res = false;
            int totTime = System.Environment.TickCount;
            for (int i = 0; i < strs.Length; i++)
            {
                for (int j = 0; j < CodeGenTests.Repetitions; j++)
                    res = compiledregex.IsMatch(strs[i]);
                if (res)
                    totAccepted += 1;
                if (accept && !res)
                    Assert.Fail(string.Format("DotNetTest Positive mismatch: regex {0} should accept {1}", compiledregex.ToString(), StringUtility.Escape(strs[i])));
                if (!accept && res)
                    Assert.Fail(string.Format("DotNetTest Negative mismatch: regex {0} should reject {1}", compiledregex.ToString(), StringUtility.Escape(strs[i])));
            }
            totTime = System.Environment.TickCount - totTime;
            sec = ((double)totTime) / (1000.0);
            return totAccepted;
        }
    }
}
