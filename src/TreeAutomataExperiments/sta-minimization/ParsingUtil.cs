using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using Microsoft.Automata.Z3;
using Microsoft.Automata.Z3.Internal;
using Microsoft.Automata;
using Microsoft.Z3;
using System.Diagnostics;


namespace RunExperiments
{
    class VataParsing
    {
        public static TreeTransducer ParseVataFile(string pathFN)
        {
            string text = System.IO.File.ReadAllText(pathFN);
            return ParseVataFormat(text);
        }

        public static TreeTransducer ParseVataFormat(string vataString)
        {
            var lines = vataString.Split('\r', '\n');
            Z3Provider Z = new Z3Provider();
            RankedAlphabet A = null;
            string name = null;
            List<int> finStates = new List<int>();
            var rules = new List<TreeRule>();
            Dictionary<string, int> names = new Dictionary<string, int>();

            List<string> constructorNames = new List<string>();
            List<int> constructorArities = new List<int>();

            bool transitionsStarted = false;

            foreach (var line in lines)
            {
                if (!transitionsStarted)
                {
                    if (line.StartsWith("Ops"))
                    {
                        var constructors = line.Split(' ');

                        foreach (var constructor in constructors)
                        {
                            var sp = constructor.Split(':');
                            if (sp.Length > 1)
                            {
                                if (!constructorNames.Contains(sp[0]))
                                {
                                    constructorNames.Add(sp[0]);
                                    int ar = int.Parse(sp[1]);
                                    constructorArities.Add(ar);
                                }
                            }
                        }
                        if (constructorNames.Count == 0)
                            return null;
                        A = Z.TT.MkRankedAlphabet("A", Z.UnitSort, constructorNames.ToArray(), constructorArities.ToArray());
                    }

                    if (line.StartsWith("Automaton"))
                    {
                        var sp = line.Split(' ');
                        name = sp[1];
                    }

                    if (line.StartsWith("Final"))
                    {
                        var sp = line.Split(' ');
                        for (int i = 2; i < sp.Length; i++)
                        {
                            if (sp[i].Length > 0)
                                finStates.Add(GetState(sp[i], names));
                        }
                    }
                    if (line.StartsWith("Transit"))
                    {
                        transitionsStarted = true;
                    }
                }
                else
                {
                    var sp = line.Split('-', '>');
                    if (sp.Length > 1)
                    {
                        var pieces = sp[0].Split('(', ',', ')', ' ');
                        var constructor = pieces[0];
                        List<int> from = new List<int>();
                        for (int i = 1; i < pieces.Length - 1; i++)
                            if (pieces[i].Length > 0)
                                from.Add(GetState(pieces[i], names));


                        var to = GetState(sp[sp.Length - 1], names);
                        rules.Add(Z.TT.MkTreeAcceptorRule(A, to, constructor, Z.True, from.ToArray()));
                    }

                }
            }
            return Z.TT.MkTreeAutomaton(finStates, A, A, rules);
        }

        public static int GetState(string st, Dictionary<string, int> names)
        {
            var n = st.Trim();
            if (names.ContainsKey(n))
                return names[n];

            names[n] = names.Count;
            return names[n];
        }

        public static string GetVataFormatString(TreeTransducer aut)
        {
            Z3Provider Z = new Z3Provider();
            StringBuilder sb = new StringBuilder();

            sb.Append("Ops");
            var alph = aut.InputAlphabet;
            foreach (var constructor in alph.constructors)
            {
                sb.AppendFormat(" {0}:{1}", constructor.Name, constructor.Arity - 1);
            }
            sb.AppendLine();

            sb.AppendLine();
            sb.AppendLine("Automaton a");

            sb.AppendLine();

            sb.Append("States");
            var states = aut.GetStates();
            foreach (var state in states)
            {
                sb.AppendFormat(" q{0}", int.Parse(state.ToString()));
            }
            sb.AppendLine();

            sb.Append("Final States");
            var finalStates = aut.roots;
            foreach (var state in finalStates)
            {
                sb.AppendFormat(" q{0}", int.Parse(state.ToString()));
            }
            sb.AppendLine();

            sb.AppendLine("Transitions");
            var transition = aut.GetRules();
            foreach (var rule in transition)
            {
                sb.Append(rule.Symbol.Name.ToString());
                sb.Append("(");

                for (int j = 1; j <= rule.Rank; j++)
                {
                    var args_j = rule.Lookahead(j - 1);
                    if (args_j.ToList().Count > 1)
                        throw new Exception("Some i has more than one state");
                    foreach (var rlastate in args_j)
                    {
                        if (j > 1)
                            sb.Append(",");
                        sb.Append("q" + Z.GetNumeralInt(rlastate));
                    }
                }
                sb.Append(") -> ");

                sb.AppendLine("q" + Z.GetNumeralInt(rule.State));
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    class DTAMINParsing
    {
        static string pathDet = @"..\..\benchmark\timbukdeterminized\";
        static string pathtoDTAMINtimb = @"..\..\benchmark\dtamin_timbuk\";
        static string pathtoDTAMINlarge = @"..\..\benchmark\dtamin_large\";

        static int startFile = 1;

        //Generates the format for the DTAMin library
        public static void RunGeneration()
        {
            DirectoryInfo detDir = new DirectoryInfo(pathDet);
            FileInfo[] detFiles = detDir.GetFiles("*");
            int numFiles = detFiles.Length;
                

            for (int fileNum = startFile; fileNum <= numFiles; fileNum++)
            {
                string s = FromTimbukToDTAMIN(pathDet + fileNum);
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(pathtoDTAMINtimb + fileNum))
                {
                    outfile.WriteLine(s);
                }
            }
        }


        public static void RunLargeGeneration()
        {
            for (int fileNum = 1; fileNum <= 21; fileNum++)
            {
                RunLargeGenerationForGivenSize(fileNum, pathtoDTAMINlarge + fileNum);
                
            }
        }

        private static void RunLargeGenerationForGivenSize(int K, string path)
        {
            using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(path))
            {

                outfile.WriteLine("1 zero1");
                outfile.WriteLine("2 zero2");
                outfile.WriteLine("3 zero3");

                for (int i = 0; i < K; i++)
                {
                    for (int bv = 1; bv < (long)Math.Pow(2, K); bv++)
                    {
                        if (!(((1 << i) & bv) == 0))
                        {
                            outfile.WriteLine((3 * i + 4) + " " + "two" + bv + " " + (3 * i+1)+ " " + (3 * i + 3));
                            outfile.WriteLine((3 * i + 5) + " " + "two" + bv + " " + (3 * i +2) + " " + (3 * i + 3));
                            outfile.WriteLine((3 * i + 6) + " " + "two" + bv + " " + (3 * i +3) + " " + (3 * i + 3));
                        }
                    }
                }

                outfile.WriteLine("0 "+(3 * K+1));
                outfile.WriteLine("0 " + (3 * K + 2));   
            }
            
        }

        public static string FromTimbukToDTAMIN(string pathFN)
        {
            var sta = VataParsing.ParseVataFile(pathFN);
            return GetDTAMINFormatString(sta);
        }

        public static string GetDTAMINFormatString(TreeTransducer aut)
        {
            Z3Provider Z = new Z3Provider();
            StringBuilder sb = new StringBuilder();

            var transition = aut.GetRules();
            foreach (var rule in transition)
            {
                sb.Append(Z.GetNumeralInt(rule.State) + 1);
                sb.Append(" ");
                sb.Append(rule.Symbol.Name.ToString());

                for (int j = 1; j <= rule.Rank; j++)
                {
                    var args_j = rule.Lookahead(j - 1);
                    if (args_j.ToList().Count > 1)
                        throw new Exception("Some i has more than one state");
                    foreach (var rlastate in args_j)
                    {
                        sb.Append(" ");
                        sb.Append(Z.GetNumeralInt(rlastate)+1);
                    }
                }
                sb.AppendLine();
            }

            int c = 0;
            foreach (var st in aut.roots)
            {
                if(c==aut.roots.Count-1)
                    sb.Append("0 " + (Z.GetNumeralInt(st) + 1));
                else
                    sb.AppendLine("0 " + (Z.GetNumeralInt(st) + 1));
                c++;
            }

            return sb.ToString();
        }
    }
}
