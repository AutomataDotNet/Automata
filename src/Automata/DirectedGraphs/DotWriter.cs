using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Automata;
using BDD = Microsoft.Automata.BDD;

namespace Microsoft.Automata.DirectedGraphs
{

    /// <summary>
    /// Provides functionality for producing dot output.
    /// </summary>
    internal static class DotWriter
    {
        /// <summary>
        /// Used for saving automata as graphs in dot format.
        /// Provides the direction of the graph.
        /// </summary>
        public enum RANKDIR
        {
            LR, TB, BT, RL
        }

        #region storing BDDs as graphs using dot

        ///// <summary>
        ///// View the given BDD as a graph. Requires that dot.exe is installed.
        ///// Uses dot.exe to create a file name.dot and produces a layout in name.format.
        ///// If showgraph is true, starts a process to view the graph.
        ///// For example if name = "foo" and format = "gif", creates a file 
        ///// foo.dot with the dot output and a file foo.gif as a picture.
        ///// Uses the current working directory.
        ///// </summary>
        ///// <param name="fa">the BDD to be viewed</param>
        ///// <param name="name">name of the file where the graph is stored</param>
        ///// <param name="dir">direction of the arrows</param>
        ///// <param name="fontsize">size of the font in node and edge labels</param>
        ///// <param name="showgraph">id true, the graph is viewed</param>
        ///// <param name="format">format of the figure</param>
        //static public void DisplayBdd(BDD bdd, string name, RANKDIR dir, int fontsize, bool showgraph, string format)
        //{
        //    string currentDirectory = System.Environment.CurrentDirectory;
        //    string dotFile = string.Format("{1}\\{0}.dot", name, currentDirectory);
        //    string outFile = string.Format("{2}\\{0}.{1}", name, format, currentDirectory);
        //    FileInfo fi = new FileInfo(dotFile);
        //    if (fi.Exists)
        //        fi.IsReadOnly = false;
        //    fi = new FileInfo(outFile);
        //    if (fi.Exists)
        //        fi.IsReadOnly = false;
        //    BddToDot(bdd, name, dotFile, dir, fontsize);
        //    System.Diagnostics.Process p = new System.Diagnostics.Process();
        //    p.StartInfo = new System.Diagnostics.ProcessStartInfo("dot.exe", string.Format("-T{2} {0} -o {1}", dotFile, outFile, format));
        //    try
        //    {
        //        p.Start();
        //        p.WaitForExit();
        //        if (showgraph)
        //        {
        //            p.StartInfo = new System.Diagnostics.ProcessStartInfo(outFile);
        //            p.Start();
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw new AutomataException(AutomataException.MissingDotViewer);
        //    }
        //}

        /// <summary>
        /// Store the set as a BDD in dot format
        /// </summary>
        /// <param name="bdd"></param>
        /// <param name="bddName"></param>
        /// <param name="filename"></param>
        /// <param name="rankdir"></param>
        /// <param name="fontsize"></param>
        static public void CharSetToDot(BDD bdd, string bddName, string filename, RANKDIR rankdir, int fontsize)
        {
            StreamWriter sw = new StreamWriter(filename);
            CharSetToDot(bdd, bddName, sw, rankdir, fontsize);
            sw.Close();
        }

        static public void CharSetToDot(BDD bdd, string setName, StreamWriter tw, RANKDIR rankdir, int fontsize)
        {
            if (bdd.IsLeaf)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNontrivial);

            Dictionary<BDD, int> nodeIds = new Dictionary<BDD, int>();
            Dictionary<int, int> nodeLevel = new Dictionary<int, int>();
            Dictionary<int, List<int>> sameRanks = new Dictionary<int, List<int>>();
            List<Move<string>> links = new List<Move<string>>();
            Stack<BDD> stack = new Stack<BDD>();
            stack.Push(bdd);
            //nodeIds.Add(bdd.Solver.False, 0);
            //nodeIds.Add(bdd.Solver.True, 1);
            nodeIds.Add(bdd, 2);
            nodeLevel[2] = bdd.Ordinal;
            int id = 3;
            int maxLevel = 0;
            while (stack.Count > 0)
            {
                BDD node = stack.Pop();
                int nodeId = nodeIds[node];
                List<int> rankGroup;
                if (!sameRanks.TryGetValue(node.Ordinal, out rankGroup))
                {
                    rankGroup = new List<int>();
                    sameRanks[node.Ordinal] = rankGroup;
                }
                rankGroup.Add(nodeId);

                maxLevel = Math.Max(node.Ordinal, maxLevel);
                if (!nodeIds.ContainsKey(node.Zero))
                {
                    if (node.Zero.IsLeaf)
                    {
                        if (node.Zero.IsEmpty)
                            nodeIds[node.Zero] = 0;
                        else
                            nodeIds[node.Zero] = 1;
                    }
                    else
                    {
                        nodeIds[node.Zero] = id++;
                        stack.Push(node.Zero);
                    }
                }
                if (!nodeIds.ContainsKey(node.One))
                {
                    if (node.One.IsLeaf)
                    {
                        if (node.One.IsEmpty)
                            nodeIds[node.One] = 0;
                        else
                            nodeIds[node.One] = 1;
                    }
                    else
                    {
                        nodeIds[node.One] = id++;
                        stack.Push(node.One);
                    }
                }
                links.Add(Move<string>.Create(nodeId, nodeIds[node.Zero], "0"));
                links.Add(Move<string>.Create(nodeId, nodeIds[node.One], "1"));
            }

            nodeLevel[0] = maxLevel + 1;
            nodeLevel[1] = maxLevel + 1;

            tw.WriteLine("digraph \"" + setName + "\" {");
            tw.WriteLine(string.Format("rankdir={0};", rankdir.ToString()));
            tw.WriteLine();
            tw.WriteLine("//Nodes");
            tw.WriteLine(string.Format("node [style = filled, shape = circle, peripheries = 1, fillcolor = white, fontsize = {0}]", fontsize));
            foreach (var kv in sameRanks)
            {
                string ranks = "{ rank = same; ";
                foreach (int n in kv.Value)
                    ranks += n.ToString() + "; ";
                ranks += "}";
                tw.WriteLine(ranks);
            }

            foreach (var n in nodeIds.Keys)
                if (!n.IsLeaf)
                    tw.WriteLine("{0} [label = {1}]", nodeIds[n], n.Ordinal);

            tw.WriteLine("//True and False");
            tw.WriteLine(string.Format("node [style = filled, shape = plaintext, fillcolor = white, fontsize = {0}]", fontsize));
            tw.WriteLine("{ rank = same; 0; 1; }");
            tw.WriteLine("0 [label = False, group = {0}]", maxLevel);
            tw.WriteLine("1 [label = True, group = {0}]", maxLevel);

            tw.WriteLine();
            tw.WriteLine("//Links");
            foreach (Move<string> t in links)
                tw.WriteLine(string.Format("{0} -> {1} [label = \"{2}\", fontsize = {3} ];", t.SourceState, t.TargetState, t.Label, fontsize));
            tw.WriteLine("}");
        }
        #endregion

        #region storing SFAs as graphs using dot

        ///// <summary>
        ///// Write the automaton in dot format.
        ///// </summary>
        ///// <param name="fa">the automaton to write</param>
        ///// <param name="faName">the name of the automaton</param>
        ///// <param name="filename">the name of the output file</param>
        ///// <param name="rankdir">the main direction of the arrows</param>
        ///// <param name="fontsize">the size of the font in labels</param>
        ///// <param name="descr">function that describes the labels as strings</param>
        //public static void AutomatonToDot<S>(Func<S, string> descr, IAutomaton<S> fa, string faName, string filename, RANKDIR rankdir, int fontsize)
        //{
        //    string filenamedot = (filename.EndsWith(".dot") ? filename : filename + ".dot");

        //    System.IO.StreamWriter sw = new System.IO.StreamWriter(filenamedot);
        //    AutomatonToDot(descr, fa, faName, sw, rankdir, fontsize, false);
        //    sw.Close();
        //}

        /// <summary>
        /// Write the automaton in dot format.
        /// </summary>
        /// <param name="fa">the automaton to write</param>
        /// <param name="faName">the name of the automaton</param>
        /// <param name="filename">the name of the output file</param>
        /// <param name="rankdir">the main direction of the arrows</param>
        /// <param name="fontsize">the size of the font in labels</param>
        /// <param name="descr">function that describes the labels as strings</param>
        public static void AutomatonToDot<S>(Func<S, string> descr, IAutomaton<S> fa, string faName, string filename, RANKDIR rankdir, int fontsize, bool showName = false)
        {
            string fname = (filename.EndsWith(".dot") ? filename : filename + ".dot");
            System.IO.StreamWriter sw = new System.IO.StreamWriter(fname);
            AutomatonToDot(descr, fa, faName, sw, rankdir, fontsize, showName);
            sw.Close();
        }

        /// <summary>
        /// Write the FSA in dot format.
        /// </summary>
        /// <param name="fa">the FSA to write</param>
        /// <param name="faName">the name of the FSA</param>
        /// <param name="tw">text writer for the output</param>
        /// <param name="rankdir">the main direction of the arrows</param>
        /// <param name="fontsize">the size of the font in labels</param>
        /// <param name="descr">function that describes the labels as strings</param>
        public static void AutomatonToDot<S>(Func<S, string> descr, IAutomaton<S> fa, string faName, System.IO.TextWriter tw, RANKDIR rankdir, int fontsize, bool showName)
        {
            ITransducer<S> faf = fa as ITransducer<S>;
            Func<S, bool> isfinal = lab => { return (faf == null ? false : faf.IsFinalRule(lab)); };

            List<Move<S>> epsilonmoves = new List<Move<S>>();
            Dictionary<Tuple<int, int>, string> nonepsilonMoves = new Dictionary<Tuple<int, int>, string>();
            Dictionary<int, string> finalLabels = new Dictionary<int, string>();
            foreach (var move in fa.GetMoves())
                if (move.IsEpsilon)
                    epsilonmoves.Add(move);
                else if (isfinal(move.Label))
                {
                    string conLab = descr(move.Label);
                    if (!string.IsNullOrEmpty(conLab))
                    {
                        string lab;
                        if (finalLabels.TryGetValue(move.SourceState, out lab))
                            lab = lab + ", " + conLab;
                        else
                            lab = conLab;
                        finalLabels[move.SourceState] = lab;
                    }
                }
                else
                {
                    var key = new Tuple<int, int>(move.SourceState, move.TargetState);
                    string lab;
                    if (nonepsilonMoves.TryGetValue(key, out lab))
                        lab = lab + ",  " + descr(move.Label);
                    else
                        lab = descr(move.Label);
                    nonepsilonMoves[key] = lab;
                }




            tw.WriteLine("digraph \"" + faName + "\" {");
            tw.WriteLine(string.Format("rankdir={0}; fontsize={1};", rankdir.ToString(), fontsize));
            tw.WriteLine();
            tw.WriteLine("//Initial state");
            tw.WriteLine(string.Format("preInit [style = filled, shape = plaintext, color = {1}, fillcolor = white, label = \"{0}\"]", (showName ? faName + ": " : " "), (showName ? "black" : "white")));
            tw.WriteLine("//Final states");
            foreach (int state in fa.GetStates())
            {
                if (fa.IsFinalState(state) && !finalLabels.ContainsKey(state))
                    tw.WriteLine(string.Format("{1} [style = filled, shape = circle, fillcolor = white, fontsize = {0}, peripheries=2]", fontsize, state));
                if (fa.IsFinalState(state) && finalLabels.ContainsKey(state))
                {
                    tw.WriteLine(string.Format("{1} [style = filled, shape = circle, fillcolor = white, fontsize = {0}]", fontsize, state));
                    tw.WriteLine(string.Format("f{0} [style = filled, shape = box, fillcolor = white, label=\"\", peripheries=2]", state));
                }
            }
            tw.WriteLine();
            tw.WriteLine("//Other states");
            foreach (int state in fa.GetStates())
                if (!fa.IsFinalState(state))
                    tw.WriteLine(string.Format("{1} [style = filled, shape = circle, fillcolor = white, fontsize = {0}]", fontsize, state));
            tw.WriteLine();
            tw.WriteLine("//Transitions");
            tw.WriteLine(string.Format("preInit -> {0}", fa.InitialState));
            foreach (Move<S> t in fa.GetMoves())
            {
                if (!isfinal(t.Label))
                {
                    tw.WriteLine(string.Format("{0} -> {1} [label = \"{2}\"{3}, fontsize = {4} ];", t.SourceState, t.TargetState,
                        t.IsEpsilon ? "()" : descr(t.Label).Replace(@"\n", @"\x0A"),
                        t.IsEpsilon ? "" : "", fontsize));
                }
                else if (finalLabels.ContainsKey(t.SourceState))
                {
                    tw.WriteLine(string.Format("{0} -> {1} [label = \"{2}\", fontsize = {3} ];", t.SourceState, "f"+t.TargetState,
                 finalLabels[t.SourceState].Replace(@"\n", @"\x0A"), fontsize));
                }
            }
            tw.WriteLine("}");
        }

        ///// <summary>
        ///// View the given SFA as a graph. Requires that dot.exe is installed.
        ///// Uses dot.exe to create a file name.dot and produces a layout in name.format.
        ///// If showgraph is true, starts a process to view the graph.
        ///// For example if name = "foo" and format = "gif", creates a file 
        ///// foo.dot with the dot output and a file foo.gif as a picture.
        ///// Uses the current working directory.
        ///// </summary>
        ///// <param name="fa">the SFA to be viewed</param>
        ///// <param name="name">name of the file where the graph is stored</param>
        ///// <param name="dir">direction of the arrows</param>
        ///// <param name="fontsize">size of the font in node and edge labels</param>
        ///// <param name="showgraph">if true, a process is started to view the graph</param>
        ///// <param name="format">format of the figure</param>
        ///// <param name="descr">function that describes the labels as strings</param>
        //public static void DisplayAutomaton<S>(Func<S, string> descr, Automaton<S> fa, string name, RANKDIR dir, int fontsize, bool showgraph, string format)
        //{
        //    string currentDirectory = System.Environment.CurrentDirectory;
        //    string dotFile = string.Format("{0}.dot", name);
        //    string outFile = string.Format("{0}.{1}", name, format);
        //    System.IO.FileInfo fi = new System.IO.FileInfo(dotFile);
        //    if (fi.Exists)
        //        fi.IsReadOnly = false;
        //    fi = new System.IO.FileInfo(outFile);
        //    if (fi.Exists)
        //        fi.IsReadOnly = false;
        //    AutomatonToDot(fa, name, dotFile, dir, fontsize);
        //    System.Diagnostics.Process p = new System.Diagnostics.Process();
        //    p.StartInfo = new System.Diagnostics.ProcessStartInfo("dot.exe", string.Format("-T{2} {0} -o {1}", dotFile, outFile, format));
        //    p.StartInfo.WorkingDirectory = currentDirectory;
        //    try
        //    {
        //        p.Start();
        //        p.WaitForExit();
        //        if (showgraph)
        //        {
        //            p.StartInfo = new System.Diagnostics.ProcessStartInfo(outFile);
        //            p.Start();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new AutomataException(AutomataException.MissingDotViewer, e);
        //    }
        //}

        #endregion

        #region STb specific dot generation

        static int maxLabelLength = -1;
        static string DisplayLabel(string lab)
        {
            return lab;
        }

        static string stbFont {
            get
            {
                return string.Format("fontname = \"{0}\", fontsize = {1}", stbFontName, stbFontSize);
            }
        }
        static string stbFontName = "Lucida Console";
        public static string STbFontName
        {
            get { return stbFontName; }
            set { stbFontName = value; }
        }
        static int stbFontSize = 12;
        public static int STbFontSize
        {
            get { return stbFontSize; }
            set { stbFontSize = value; }
        }
        static string excColor = "orangered";
        public static string ExceptionColor
        {
            get { return excColor; }
            set { excColor = value; }
        }
        static string accColor = "lawngreen";
        public static string AcceptColor
        {
            get { return accColor; }
            set { accColor = value; }
        }
        static string direction = "LR";
        public static string Direction
        {
            get { return direction; }
            set { direction = value; }
        }
        static bool inclName = true;
        public static bool IncludeName
        {
            get { return inclName; }
            set { inclName = value; }
        }

        internal static void STbToDot<T>(int maxLabel, ISTb<T> stb, System.IO.TextWriter tw)
        {
            maxLabelLength = maxLabel;
            tw.WriteLine("digraph \"" + stb.Name + "\" {");
            tw.WriteLine(string.Format("rankdir={0};", direction));
            tw.WriteLine();
            tw.WriteLine("//name");
            tw.WriteLine(string.Format("node [style = filled, shape = plaintext, color = white, width = .3 cm, height = .3 cm,  fillcolor = white, {0}];", stbFont));
            tw.WriteLine("preInit [label = \"{0}\", color = white];", (inclName ? stb.Name + ":" : ""));
            var edges = new StringBuilder();
            var edgeMap = new Dictionary<Tuple<string, string>, string>();
            var edgeCat = new Dictionary<Tuple<string, string>, string>();
            Func<Tuple<string, string>, string> getEdge =
                (p => (edgeMap.ContainsKey(p) ? edgeMap[p] : ""));
            Action<string, string, string, string> AddEdge =
                ((s, t, l, cat) =>
                {
                    var p = new Tuple<string, string>(s, t);
                    string v = getEdge(p);
                    edgeMap[p] = (v == "" ? l : v + "," + l);
                    edgeCat[p] = cat;
                });
            tw.WriteLine();
            tw.WriteLine("//control states");
            tw.WriteLine(string.Format("node [style = filled, shape = circle, color = black, fillcolor = white, width = .3 cm, height = .3 cm,  {0}];", stbFont));
            foreach (int state in stb.States)
            {
                tw.WriteLine(string.Format("s{0} [label = {0}, shape = circle];", state));
            }
            tw.WriteLine();
            tw.WriteLine("//branch conditions");
            tw.WriteLine(string.Format("node [style = filled, shape = box, width = .3 cm, height = .3 cm, fillcolor = white, {0}];", stbFont ));
            foreach (int state in stb.States)
            {
                var rule = stb.GetRuleFrom(state);
                if (rule.IsNotUndef)
                {
                    WriteRuleNodes("s" + state + "r", rule, tw, stb, false);
                }
                var frule = stb.GetFinalRuleFrom(state);
                if (frule.IsNotUndef)
                {
                    WriteRuleNodes("s" + state + "f", frule, tw, stb, true);
                }
            }
            tw.WriteLine();
            tw.WriteLine("//edges");
            tw.WriteLine("edge [{0}];", stbFont);
            string rhs = stb.PrettyPrint(stb.InitialRegister);
            if (!(rhs.Equals(STbInfo.__empty_tuple) || rhs.Equals(STbInfo.__register_variable)))  //either registers are not used, or the assignment is trivial
                rhs = string.Format("{0}:={1}", STbInfo.__register_variable, rhs);
            else
                rhs = "";
            tw.WriteLine("preInit -> s{0} [label = \"{1}\"];", stb.InitialState, ShortenLabel(DisplayLabel(rhs)));

            foreach (int state in stb.States)
            {
                var rule = stb.GetRuleFrom(state);
                if (rule.IsNotUndef)
                {
                    tw.WriteLine("s{0} -> s{0}r;", state);
                    WriteRuleLinks("Rule" + state, "s" + state + "r", rule, tw, stb, false, rule is IteRule<T>);
                }
                var frule = stb.GetFinalRuleFrom(state);
                if (frule.IsNotUndef)
                {
                    tw.WriteLine("s{0} -> s{0}f [style = dashed, color = gray];", state);
                    WriteRuleLinks("Final" + state, "s" + state + "f", frule, tw, stb, true, frule is IteRule<T>);
                }
            }

            tw.WriteLine("}");
        }

        private static void WriteRuleNodes<T>(string source, BranchingRule<T> rule, System.IO.TextWriter tw, ISTb<T> stb, bool endcase)
        {
            UndefRule<T> raise = rule as UndefRule<T>;
            if (raise != null)
            {
                tw.WriteLine("{0} [fillcolor = {2}, label =\"{1}\"];", source, ShortenLabel(DisplayLabel(raise.Exc)), excColor);
                return;
            }
            BaseRule<T> block = rule as BaseRule<T>;
            if (block != null)
            {
                string lab = Describe(stb, block, endcase);
                if (endcase)
                    tw.WriteLine("{0} [fillcolor={1}, peripheries = 2, label =\"{2}\"];", source, accColor, DisplayLabel(lab));
                else
                    tw.WriteLine("{0} [label =\"{1}\"];", source, DisplayLabel(lab));
                return;
            }
            else
            {
                IteRule<T> ite = (IteRule<T>)rule;
                string lab = stb.PrettyPrint(ite.Condition);
                tw.WriteLine("{0} [label =\"{1}\", style=rounded];", source, ShortenLabel(DisplayLabel(lab)));
                WriteRuleNodes(source + "T", ite.TrueCase, tw, stb, endcase);
                WriteRuleNodes(source + "F", ite.FalseCase, tw, stb, endcase);
            }
        }

        private static void WriteRuleLinks<T>(string cat, string source, BranchingRule<T> rule, System.IO.TextWriter tw, ISTb<T> stb, bool endcase, bool writegrouping)
        {
            if ((rule is UndefRule<T>) || (endcase && (rule is BaseRule<T>)))
                return;  //there are no further links

            if (rule is BaseRule<T>)
                tw.WriteLine("{0} -> s{1};", source, ((BaseRule<T>)rule).State);
            else
            {
                IteRule<T> ite = (IteRule<T>)rule;
                tw.WriteLine("{0} -> {1} [label = \"T\"];", source, source + "T");
                tw.WriteLine("{0} -> {1} [label = \"F\"];", source, source + "F");
                WriteRuleLinks(cat, source + "T", ite.TrueCase, tw, stb, endcase, writegrouping);
                WriteRuleLinks(cat, source + "F", ite.FalseCase, tw, stb, endcase, writegrouping);
            }
        }

        //private static string Describe<T>(ISTb<T> stb, BaseRule<T> basic, bool endcase)
        //{
        //    List<string> lab = new List<string>();
        //    for (int i = 0; i < basic.Yields.Length; i++)
        //    {
        //        lab.Add("yield " + stb.PrettyPrint(basic.Yields[i]));
        //    }
        //    string rhs = stb.PrettyPrint(basic.Register);
        //    if (!endcase && !(rhs.Equals(STbInfo.__empty_tuple) || rhs.Equals(STbInfo.__register_variable)))  //either registers are not used, or the assignment is trivial
        //        lab.Add(string.Format("{0}:={1}", STbInfo.__register_variable, rhs));
        //    lab = ShortenWidth(lab);
        //    int k = 0; 
        //    foreach (string s in lab)
        //        k = Math.Max(s.Length, k);
        //    //add spaces to simulate left alignment
        //    for (int i = 0; i < lab.Count; i++)
        //        lab[i] = lab[i] + MkSpaces(k - lab[i].Length);
        //    var labStr = "";
        //    for (int i = 0; i < lab.Count; i++)
        //    {
        //        if (i > 0)
        //            labStr += "\\n";
        //        labStr += lab[i];
        //    }
        //    return labStr;
        //}

        private static string Describe<T>(ISTb<T> stb, BaseRule<T> basic, bool endcase)
        {
            List<string> lab = new List<string>();
            string labYield = "[";
            for (int i = 0; i < basic.Yields.Length; i++)
            {
                if (i > 0)
                    labYield += ", ";
                labYield += stb.PrettyPrint(basic.Yields[i]);
            }
            labYield += "]";
            lab.Add(labYield);
            string rhs = stb.PrettyPrint(basic.Register);
            if (!endcase && !(rhs.Equals(STbInfo.__empty_tuple) || rhs.Equals(STbInfo.__register_variable)))  //either registers are not used, or the assignment is trivial
                lab.Add(string.Format("{0}:={1}", STbInfo.__register_variable, rhs));
            lab = ShortenWidth(lab);
            int k = 0;
            foreach (string s in lab)
                k = Math.Max(s.Length, k);
            //add spaces to simulate left alignment
            for (int i = 0; i < lab.Count; i++)
                lab[i] = lab[i] + MkSpaces(k - lab[i].Length);
            var labStr = "";
            for (int i = 0; i < lab.Count; i++)
            {
                if (i > 0)
                    labStr += "\\n";
                labStr += lab[i];
            }
            return labStr;
        }

        private static List<string> ShortenWidth(List<string> lab)
        {
            if (maxLabelLength <= 0)
                return lab;
            List<string> res = new List<string>();
            foreach (string s in lab)
            {
                if (s.Length <= maxLabelLength)
                    res.Add(s);
                else
                {
                    string s1 = s;
                    while (s1.Length > maxLabelLength)
                    {
                        res.Add(s1.Substring(0, maxLabelLength));
                        s1 = "  " + s1.Substring(maxLabelLength);
                    }
                    res.Add(s1);
                }
            }
            return res;
        }

        private static string ShortenLabel(string lab)
        {
            if (maxLabelLength <= 0)
                return lab;
            var res = new StringBuilder();
            string s = lab;
            {
                if (s.Length <= maxLabelLength)
                    res.Append(s);
                else
                {
                    string s1 = s;
                    while (s1.Length > maxLabelLength)
                    {
                        res.Append(s1.Substring(0, maxLabelLength));
                        s1 = s1.Substring(maxLabelLength);
                        res.Append("\\n");
                    }
                    res.Append(s1);
                }
            }
            return res.ToString();
        }

        private static string MkSpaces(int p)
        {
            string s = "";
            for (int i = 0; i < p; i++)
                s += " ";
            return s;
        }

        #endregion
    }
}
