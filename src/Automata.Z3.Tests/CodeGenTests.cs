using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Microsoft.Automata;
using Microsoft.Automata.Internal;
using Microsoft.Automata.Internal.Generated;
using Microsoft.Automata.Rex;
using Microsoft.Automata.Z3;
using Microsoft.Z3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Automata.Z3.Tests
{
    [TestClass]
    public class CodeGenTests
    {
        static string regexesFile = "../../../Automata.Tests/regexes.txt";
        internal static int NrOfStrings = 100;
        internal static int MaxStringLength = 100;
        internal static int Repetitions = 10;

        [TestMethod]
        public void TestSampleRegexes()
        {
            var z3 = new Z3Provider(BitWidth.BV7);
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

            int K = 50; //number of pos/neg strings to be generated for each regex
            for (int i = 1; i < 100; i++)
            {
                try
                {
                    var regex = regexes[i];
                    var aut = z3.CharSetProvider.Convert(regex, RegexOptions.Singleline);
                    var autDet = aut.Determinize(z3.CharSetProvider, 2000);
                    var autMin = autDet.Minimize(z3.CharSetProvider);
                    var autMinC = aut.Complement(z3.CharSetProvider);
                    if (autMin.IsEmpty || autMinC.IsEmpty)
                        continue;

                    var autMinExpr = z3.ConvertAutomatonGuardsToExpr(autMin);
                    var sfa = new SFA<FuncDecl, Expr, Sort>(z3, z3.CharacterSort, autMinExpr);
                    var stbb = new STBuilder<FuncDecl, Expr, Sort>(z3);
                    var st = ST<FuncDecl, Expr, Sort>.SFAtoST(sfa);
                    var stb = st.ToSTb();
                    var csAcceptor = stb.Compile("RegexTransfomer", "SampleAcceptor", false, true);

                    HashSet<string> posSamples = new HashSet<string>();
                    HashSet<string> negSamples = new HashSet<string>();
                    int k = autMin.FindShortestFinalPath(autMin.InitialState).Item1.Length;
                    var maxLengthAut = z3.CharSetProvider.Convert("^.{0," + (3 * k) + "}$").Determinize(z3.CharSetProvider).Minimize(z3.CharSetProvider);
                    int tries = 0;
                    while (posSamples.Count < K && tries < 10 * K)
                    {

                        var s = z3.CharSetProvider.GenerateMemberUniformly(autMin.Intersect(maxLengthAut, z3.CharSetProvider));
                        if (!s.EndsWith("\n"))
                            if (!posSamples.Add(s))
                                tries++;
                    }
                    tries = 0;
                    int k2 = autMinC.FindShortestFinalPath(autMin.InitialState).Item1.Length;
                    var maxLengthAut2 = z3.CharSetProvider.Convert("^.{0," + (3 * k2) + "}$").Determinize(z3.CharSetProvider).Minimize(z3.CharSetProvider);
                    while (negSamples.Count < K && tries < 10 * K)
                    {
                        var autMinCprefix = autMinC.Intersect(maxLengthAut2, z3.CharSetProvider);
                        var s = z3.CharSetProvider.GenerateMemberUniformly(autMinCprefix);
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
                        if (csAcceptor.Apply(s) == null)
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
                        if (csAcceptor.Apply(s) != null)
                        {
                            Console.WriteLine("match expected regex:" + i);
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
    }
}
