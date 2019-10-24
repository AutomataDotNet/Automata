using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Automata.DirectedGraphs
{
    public static class Options
    {
        /// <summary>
        /// Maximum length of transiton labels shown in dgml view
        /// </summary>
        public static int MaxDgmlTransitionLabelLength = 50;
    }

    internal static class DgmlWriter
    {
        #region storing SFAs as graphs in dgml format

        /// <summary>
        /// Write the automaton in dgml format.
        /// </summary>
        /// <param name="k">restiction on label length</param>
        /// <param name="fa">the automaton to write</param>
        /// <param name="name">the name of the output file, if filename does not end with .dgml then .dgml is added as suffix</param>
        public static void AutomatonToDgml<S>(int k, IAutomaton<S> fa, string name)
        {
            System.IO.StreamWriter sw = new System.IO.StreamWriter(name + (name.EndsWith(".dgml") ? "" : ".dgml"));
            AutomatonToDgml(k, fa, name, sw);
            sw.Close();
        }

        static bool __tried_to_load_VS = false;
        /// <summary>
        /// Top-level Visual Studio automation object model, if available.
        /// Used to close and open dgml graph files.
        /// </summary>
        static object VS = null;

        /// <summary>
        /// Write the automaton in dgml format in the current directory and open the file in a new process.
        /// </summary>
        /// <param name="k">restiction on label length</param>
        /// <param name="fa">the automaton to write</param>
        /// <param name="name">the name of the output file, if filename does not end with .dgml then .dgml is added as suffix</param>
        /// <param name="describeS">custom viewer for S, default is null, if null then ToString is used</param>
        public static void ShowGraph<S>(int k, IAutomaton<S> fa, string name, Func<S, string> describeS = null)
        {
            //if (!__tried_to_load_VS)
            //{
            //    //only try to load VS automation object model one time
            //    TryLoadVS();
            //    __tried_to_load_VS = true;
            //}
            //if (VS == null)
            //{
                SaveGraph(k, fa, name, describeS);
                OpenFileInNewProcess(name + (name.EndsWith(".dgml") ? "" : ".dgml"));
            //}
            //else
            //    ShowGraphInVS(k, fa, name, describeS);
        }

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        //static void ShowGraphInVS<S>(int k, IAutomaton<S> fa, string name, Func<S, string> describeS = null)
        //{
        //    string filename = name + (name.EndsWith(".dgml") ? "" : ".dgml");
        //    //Access the top-level VS automation object model
        //    EnvDTE.DTE dte = (EnvDTE.DTE)VS;
        //    #region Close the dgml file if it is open
        //    try
        //    {
        //        System.Reflection.
        //        System.Collections.IEnumerator wins = dte.Windows.GetEnumerator();
        //        while (wins.MoveNext() == true)
        //        {
        //            EnvDTE.Window w = wins.Current as EnvDTE.Window;
        //            if (filename.Equals(w.Caption))
        //            {
        //                w.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
        //                break;
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        //the operation dte.Windows.GetEnumerator()
        //        //may sometimes cause COMException
        //        //Ignore this exception, 
        //        //then the window with given filename may still be open
        //        //and VS may ask to save changes, instead of ignoring
        //        //when the file is subsequently changed on disk
        //    }
        //    #endregion
        //    SaveGraph(k, fa, name, describeS);
        //    #region Open the dgml file in VS
        //    try
        //    {
        //        var dir = System.Environment.CurrentDirectory;
        //        var fullfilename = dir + "/" + filename;
        //        dte.ExecuteCommand("File.OpenFile", dir + "/" + filename);
        //    }
        //    catch
        //    {
        //        OpenFileInNewProcess(filename);
        //    }
        //    #endregion
        //}

        static void TryLoadVS()
        {
            try
            {
                //first try to access VS 2015
                VS = System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.14.0");
            }
            catch (Exception)
            {
                try
                {
                    //second try tom access VS 2017
                    VS = System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.15.0");
                }
                catch (Exception)
                {

                }
            }
        }

        /// <summary>
        /// Write the automaton in dgml format in the current directory.
        /// </summary>
        /// <param name="fa">the automaton to write</param>
        /// <param name="name">the name of the output file, if filename does not end with .dgml then .dgml is added as suffix</param>
        public static void SaveGraph<S>(int k, IAutomaton<S> fa, string name, Func<S, string> describeS = null)
        {
            string filename = name + (name.EndsWith(".dgml") ? "" : ".dgml");
            System.IO.StreamWriter sw = new System.IO.StreamWriter(filename);
            AutomatonToDgml(k, fa, name, sw, describeS);
            sw.Close();
        }

        private static void OpenFileInNewProcess(string file)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo(file);
            p.StartInfo.WorkingDirectory = System.Environment.CurrentDirectory;
            p.Start();
        }

        /// <summary>
        /// Write the automaton in dgml format.
        /// </summary>
        /// <param name="fa">the automaton to write</param>
        /// <param name="tw">text writer for the output</param>
        public static void AutomatonToDgml<S>(int k, IAutomaton<S> fa, string name, System.IO.TextWriter tw, Func<S,string> describeS = null)
        {
            ITransducer<S> faf = fa as ITransducer<S>;
            bool isTransducer = (faf != null);
            Func<S, bool> isfinal = lab => { return (isTransducer ? faf.IsFinalRule(lab) : false); };

            var finalMoves = new Dictionary<int, List<S>>();
            var nonFinalMoves = new Dictionary<Tuple<int, int>, List<S>>();
            var epsilonmoves = new List<Move<S>>();

            var nonEpsilonStates = new HashSet<int>();
            Func<int, bool> IsEpsilonState = (s => !nonEpsilonStates.Contains(s));

            foreach (var move in fa.GetMoves())
            {
                if (move.IsEpsilon)
                    epsilonmoves.Add(move);

                else if (isfinal(move.Label) && 
                         !(faf.IsGuardTrue(move.Label) && faf.GetYieldsLength(move.Label)==0))
                {
                    List<S> rules;
                    if (!finalMoves.TryGetValue(move.SourceState, out rules))
                    {
                        rules = new List<S>();
                        finalMoves[move.SourceState] = rules;
                    }
                    rules.Add(move.Label);
                }
                else if (!isfinal(move.Label))
                {
                    nonEpsilonStates.Add(move.SourceState);
                    List<S> rules;
                    var p = new Tuple<int, int>(move.SourceState, move.TargetState);
                    if (!nonFinalMoves.TryGetValue(p, out rules))
                    {
                        rules = new List<S>();
                        nonFinalMoves[p] = rules;
                    }
                    rules.Add(move.Label);
                }
            }

            tw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            tw.WriteLine("<DirectedGraph xmlns=\"http://schemas.microsoft.com/vs/2009/dgml\" ZoomLevel=\"1.5\" GraphDirection=\"TopToBottom\" >");
            tw.WriteLine("<Nodes>");
            tw.WriteLine("<Node Id=\"init\" Label=\"{0}\" Stroke=\"white\" Background=\"white\"/>", name);
            foreach (int state in fa.GetStates())
                if (state == fa.InitialState && fa.IsFinalState(state))
                {
                    tw.WriteLine("<Node Id=\"{0}\" Label=\"{1}\" Category=\"State\" >", state, EncodeChars(fa.DescribeState(state)));
                    if (!finalMoves.ContainsKey(state))
                    {
                        //if (IsEpsilonState(state))
                        //    tw.WriteLine("<Category Ref=\"EpsilonState\" />");
                        //else
                            tw.WriteLine("<Category Ref=\"FinalState\" />");
                    }
                    //tw.WriteLine("<Category Ref=\"InitialState\" />");
                    tw.WriteLine("</Node>");
                    if (finalMoves.ContainsKey(state))
                    {
                        tw.WriteLine("<Node Id=\"f{0}\" Label=\" \" Category=\"State\" >", state);
                        tw.WriteLine("<Category Ref=\"FinalState\" />");
                        tw.WriteLine("<Category Ref=\"SinkState\" />");
                        tw.WriteLine("</Node>");
                    }
                }
                else if (state == fa.InitialState)
                {
                    tw.WriteLine("<Node Id=\"{0}\" Label=\"{1}\" Category=\"State\" >", state, EncodeChars(fa.DescribeState(state)));
                    tw.WriteLine("<Category Ref=\"InitialState\" />");
                    tw.WriteLine("</Node>");
                }
                else if (fa.IsFinalState(state))
                {
                    tw.WriteLine("<Node Id=\"{0}\" Label=\"{1}\" Category=\"State\" >", state, EncodeChars(fa.DescribeState(state)));
                    if (!finalMoves.ContainsKey(state))
                    {
                        //if (IsEpsilonState(state))
                        //    tw.WriteLine("<Category Ref=\"EpsilonState\" />");
                        //else
                            tw.WriteLine("<Category Ref=\"FinalState\" />");
                    }
                    tw.WriteLine("</Node>");
                    if (finalMoves.ContainsKey(state))
                    {
                        tw.WriteLine("<Node Id=\"f{0}\" Label=\" \" Category=\"State\" >", state);
                        tw.WriteLine("<Category Ref=\"FinalState\" />");
                        tw.WriteLine("<Category Ref=\"SinkState\" />");
                        tw.WriteLine("</Node>");
                    }
                }
                else
                    tw.WriteLine("<Node Id=\"{0}\" Label=\"{1}\" Category=\"State\" />", state, EncodeChars(fa.DescribeState(state)));
            tw.WriteLine("</Nodes>");
            tw.WriteLine("<Links>");
            tw.WriteLine("<Link Source=\"init\" Target=\"{0}\" Label=\"{1}\" Category=\"StartTransition\" />", fa.InitialState, EncodeChars(fa.DescribeStartLabel()));
            foreach (var move in epsilonmoves)
                tw.WriteLine("<Link Source=\"{0}\" Target=\"{1}\" Category=\"EpsilonTransition\" />", move.SourceState, move.TargetState);

            foreach (var move in nonFinalMoves)
                tw.WriteLine(GetNonFinalRuleInfo(k, fa, faf, move.Key.Item1, move.Key.Item2, move.Value, describeS));

            foreach (var move in finalMoves)
                tw.WriteLine(GetFinalRuleInfo(k, faf, move.Key, move.Value));

            tw.WriteLine("</Links>");
            WriteCategoriesAndStyles(tw);
            tw.WriteLine("</DirectedGraph>");
        }

        private static string GetFinalRuleInfo<S>(int k, ITransducer<S> trans, int state, List<S> rules)
        {
            if (trans == null)
                throw new AutomataException(AutomataExceptionKind.InternalError);

            string lab = "";
            string info= "";
            for (int i = 0; i < rules.Count; i++)
            {
                lab += (lab == "" ? "" : ", ") + trans.DescribeLabel(rules[i]);
                info += string.Format("Rule{0}.Guard = \"{1}\" Rule{0}.Yields = \"{2}\" ", i + 1,
                    EncodeChars(trans.DescribeGuard(rules[i])), EncodeChars(trans.DescribeYields(rules[i])));
            }
            if (k >= 0 && lab.Length > k)
                lab = lab.Substring(0, k) + "...";
            lab = EncodeChars(lab);
            return string.Format("<Link Source=\"{0}\" Target=\"f{0}\" Label=\"{1}\" Category=\"FinalLabel\" {2}/>", state, lab, info);
        }

        private static string GetNonFinalRuleInfo<S>(int k, IAutomaton<S> aut, ITransducer<S> trans, int source, int target, List<S> rules, Func<S, string> describeS = null)
        {
            if (describeS == null)
                describeS = aut.DescribeLabel;

            string lab = "";
            string info = "";
            for (int i = 0; i < rules.Count; i++)
            {
                lab += (lab == "" ? "" : ",\n ") + describeS(rules[i]);
                if (trans != null)
                    info += string.Format("Rule{0}.Guard = \"{1}\" Rule{0}.Update = \"{2}\" Rule{0}.Yields = \"{3}\" ", i + 1,
                        EncodeChars(trans.DescribeGuard(rules[i])), 
                        EncodeChars(trans.DescribeUpdate(rules[i])), 
                        EncodeChars(trans.DescribeYields(rules[i])));
            }
            if (k >= 0 && lab.Length > k)
                lab = lab.Substring(0, k) + "...";
            lab = EncodeChars(lab);
            if (lab.Length > Options.MaxDgmlTransitionLabelLength)
            {
                info += string.Format(" HiddenLabel = \"{0}\"", lab);
                lab = "...";
            }
            return string.Format("<Link Source=\"{0}\" Target=\"{1}\" Label=\"{2}\" Category=\"NonepsilonTransition\" {3}/>", source, target, lab, info);
        }

        private static string EncodeChars(string s)
        {
            var v = System.Net.WebUtility.HtmlEncode(s).Replace("\n", "&#13;");
            return v;
        }

        private static void WriteCategoriesAndStyles(System.IO.TextWriter tw)
        {
            tw.WriteLine("<Categories>");
            tw.WriteLine("<Category Id=\"EpsilonTransition\" Label=\"Epsilon transition\" IsTag=\"True\" />");
            tw.WriteLine("<Category Id=\"StartTransition\" Label=\"Initial transition\" IsTag=\"True\" />");
            tw.WriteLine("<Category Id=\"FinalLabel\" Label=\"Final transition\" IsTag=\"True\" />");
            tw.WriteLine("<Category Id=\"FinalState\" Label=\"Final state\" IsTag=\"True\" />");
            tw.WriteLine("<Category Id=\"SinkState\" Label=\"Sink state\" IsTag=\"True\" />");
            tw.WriteLine("<Category Id=\"EpsilonState\" Label=\"Epsilon state\" IsTag=\"True\" />");
            tw.WriteLine("<Category Id=\"InitialState\" Label=\"Initial state\" IsTag=\"True\" />");
            tw.WriteLine("<Category Id=\"NonepsilonTransition\" Label=\"Nonepsilon transition\" IsTag=\"True\" />");
            tw.WriteLine("<Category Id=\"State\" Label=\"State\" IsTag=\"True\" />");
            tw.WriteLine("</Categories>");
            tw.WriteLine("<Styles>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"InitialState\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('InitialState')\" />");
            tw.WriteLine("<Setter Property=\"Background\" Value=\"white\" />");
            tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"FinalState\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('FinalState')\" />");
            //tw.WriteLine("<Setter Property=\"Background\" Value=\"lightgreen\" />");
            tw.WriteLine("<Setter Property=\"StrokeThickness\" Value=\"4\" />");
            //tw.WriteLine("<Setter Property=\"Background\" Value=\"white\" />");
            //tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"SinkState\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('SinkState')\" />");
            tw.WriteLine("<Setter Property=\"NodeRadius\" Value=\"0\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"EpsilonState\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('EpsilonState')\" />");
            tw.WriteLine("<Setter Property=\"Background\" Value=\"tomato\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"State\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('State')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("<Setter Property=\"Background\" Value=\"white\" />");
            tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Link\" GroupLabel=\"NonepsilonTransition\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('NonepsilonTransition')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Link\" GroupLabel=\"StartTransition\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('StartTransition')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Link\" GroupLabel=\"EpsilonTransition\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('EpsilonTransition')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("<Setter Property=\"StrokeDashArray\" Value=\"8 8\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Link\" GroupLabel=\"FinalLabel\" ValueLabel=\"False\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('FinalLabel')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("<Setter Property=\"StrokeDashArray\" Value=\"8 8\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("</Styles>");
        }

        #endregion

        #region STb specific viweing 

        /// <summary>
        /// Write the automaton in dgml format in the current directory and open the file in a new process.
        /// </summary>
        /// <param name="fa">the automaton to write</param>
        /// <param name="name">the name of the output file, if filename does not end with .dgml then .dgml is added as suffix</param>
        internal static void ShowSTb<T>(int k, ISTb<T> stb)
        {
            string filename = stb.Name + (stb.Name.EndsWith(".dgml") ? "" : ".dgml");
            System.IO.StreamWriter sw = new System.IO.StreamWriter(filename);
            STbToDgml(k, stb, sw);
            sw.Close();
            OpenFileInNewProcess(filename);
        }

        static int maxLabelLength = -1;
        static string DisplayLabel(string lab)
        {
            if (maxLabelLength < 0 || lab.Length <= maxLabelLength + 3)
                return string.Format("Label=\"{0}\"", EncodeChars(lab));
            else
            {
                string s = lab.Substring(0, maxLabelLength / 2) + "..." + lab.Substring(lab.Length - (maxLabelLength / 2));
                return string.Format("Label=\"{0}\" Description=\"{1}\"", EncodeChars(s), EncodeChars(lab));
            }
        }

        internal static void STbToDgml<T>(int maxLabel, ISTb<T> stb, System.IO.TextWriter tw)
        {
            maxLabelLength = maxLabel;
            tw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            tw.WriteLine("<DirectedGraph xmlns=\"http://schemas.microsoft.com/vs/2009/dgml\" ZoomLevel=\"1.5\" GraphDirection=\"TopToBottom\" >");
            //-------------------- states ---------------------
            tw.WriteLine("<Nodes>");
            //--- initial state ---
            tw.WriteLine("<Node Id=\"init\" Label=\"{0}\" Stroke=\"white\" Background=\"white\"/>", stb.Name);

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

            foreach (int state in stb.States)
            {
                if (state == stb.InitialState)
                    tw.WriteLine("<Node Id=\"{0}\" Label=\"{0}\" Category=\"InitialState\" />", state);
                else
                    tw.WriteLine("<Node Id=\"{0}\" Label=\"{0}\" Category=\"State\" />", state);
            }

            foreach (int state in stb.States)
            {
                var rule = stb.GetRuleFrom(state);
                if (rule.IsNotUndef)
                {
                    if (rule is IteRule<T>)
                        tw.WriteLine("<Node Id=\"Rule{0}\" Category=\"{1}\" Group=\"Collapsed\" Label=\"Rule {0}\" />", state, rule.IsPartial ? "PartialRule" : "Rule");
                    WriteRuleNodes(state + ".r", rule, tw, stb, false);
                }
                var frule = stb.GetFinalRuleFrom(state);
                if (frule.IsNotUndef)
                {
                    if (frule is IteRule<T>)
                        tw.WriteLine("<Node Id=\"Final{0}\" Category=\"{1}\" Group=\"Collapsed\" Label=\"Final {0}\" />", state, frule.IsPartial ? "FinalPartialRule" : "FinalRule");
                    WriteRuleNodes(state + ".f", frule, tw, stb, true);
                }
            }
            tw.WriteLine("</Nodes>");
            tw.WriteLine("<Links>");
            string rhs = stb.PrettyPrint(stb.InitialRegister);
            if (!(rhs.Equals(STbInfo.__empty_tuple) || rhs.Equals(STbInfo.__register_variable)))  //either registers are not used, or the assignment is trivial
                rhs = string.Format("{0}:={1}", STbInfo.__register_variable, rhs);
            else
                rhs = "";
            tw.WriteLine("<Link Source=\"init\" Target=\"{0}\" {1} Category=\"InitialTransition\" />", stb.InitialState, DisplayLabel(rhs));

            foreach (int state in stb.States)
            {
                var rule = stb.GetRuleFrom(state);
                if (rule.IsNotUndef)
                {
                    tw.WriteLine("<Link Source=\"{0}\" Target=\"{1}\" Category=\"RuleEntry\" />", state, state + ".r");
                    WriteRuleLinks("Rule" + state, state.ToString() + "." + "r", rule, tw, stb, false, rule is IteRule<T>);
                }
                var frule = stb.GetFinalRuleFrom(state);
                if (frule.IsNotUndef)
                {
                    tw.WriteLine("<Link Source=\"{0}\" Target=\"{1}\" Category=\"FinalTransition\" />", state, state + ".f");
                    WriteRuleLinks("Final" + state, state.ToString() + "." + "f", frule, tw, stb, true, frule is IteRule<T>);
                }
            }

            tw.WriteLine("</Links>");
            WriteSTbCategoriesAndStyles(tw);
            tw.WriteLine("</DirectedGraph>");
        }

        private static void WriteRuleNodes<T>(string prefix, BranchingRule<T> rule, System.IO.TextWriter tw, ISTb<T> stb, bool endcase)
        {
            BranchingRule<T> rule1 = (rule is SwitchRule<T> ? (rule as SwitchRule<T>).ToIteForVisualization() : rule);
            UndefRule<T> raise = rule as UndefRule<T>;
            if (raise != null)
            {
                tw.WriteLine("<Node Id=\"{0}\" {1} Category=\"Reject\" />", prefix, DisplayLabel(raise.Exc));
                return;
            }
            BaseRule<T> block = rule as BaseRule<T>;
            if (block != null)
            {
                string lab = Describe(stb, block, endcase);
                string cat = (endcase ? "Accept" : "BasicBlock");
                tw.WriteLine("<Node Id=\"{0}\" {1} Category=\"{2}\" />", prefix, DisplayLabel(lab), cat);
                return;
            }
            else
            {
                IteRule<T> ite = (IteRule<T>)rule1;
                string lab = stb.PrettyPrint(ite.Condition);
                tw.WriteLine("<Node Id=\"{0}\" {1} Category=\"BranchCondition\" />", prefix, DisplayLabel(lab));
                WriteRuleNodes(prefix + "." + "T", ite.TrueCase, tw, stb, endcase);
                WriteRuleNodes(prefix + "." + "F", ite.FalseCase, tw, stb, endcase);
            }
        }
         
        private static void WriteRuleLinks<T>(string cat, string prefix, BranchingRule<T> rule, System.IO.TextWriter tw, ISTb<T> stb, bool endcase, bool writegrouping)
        {
            //TBD: better visualization for switch rules
            rule = (rule is SwitchRule<T> ? (rule as SwitchRule<T>).ToIteForVisualization() : rule); 
            if (writegrouping)
                tw.WriteLine("<Link Source=\"{0}\" Target=\"{1}\" Category=\"Contains\" />", cat, prefix);

            if ((rule is UndefRule<T>) || (endcase && (rule is BaseRule<T>)))
                return;  //there are no further links

            if (rule is BaseRule<T>)
                tw.WriteLine("<Link Source=\"{0}\" Target=\"{1}\" Category=\"RuleExit\" />", prefix, ((BaseRule<T>)rule).State);
            else //if (rule is IteRule<T>)
            {
                IteRule<T> ite = (IteRule<T>)rule;
                tw.WriteLine("<Link Source=\"{0}\" Target=\"{1}\" Label=\"T\" Category=\"BranchCase\" />", prefix, prefix + "." + "T");
                tw.WriteLine("<Link Source=\"{0}\" Target=\"{1}\" Label=\"F\" Category=\"BranchCase\" />", prefix, prefix + "." + "F");
                WriteRuleLinks(cat, prefix + "." + "T", ite.TrueCase, tw, stb, endcase, writegrouping);
                WriteRuleLinks(cat, prefix + "." + "F", ite.FalseCase, tw, stb, endcase, writegrouping);
            }
            //else //switch rule
            //{
            //}
        }

        private static string Describe<T>(ISTb<T> stb, BaseRule<T> basic, bool endcase)
        {
            StringBuilder lab = new StringBuilder("yield (");
            for (int i = 0; i < basic.Yields.Length; i++)
            {
                if (i > 0)
                    lab.Append(",");
                lab.Append(stb.PrettyPrint(basic.Yields[i]));
            }
            lab.Append(")");
            string rhs = stb.PrettyPrint(basic.Register);
            if (!endcase && !(rhs.Equals(STbInfo.__empty_tuple) || rhs.Equals(STbInfo.__register_variable)))  //either registers are not used, or the assignment is trivial
                lab.AppendFormat("; {0}:={1}", STbInfo.__register_variable, rhs);
            var labStr = lab.ToString();
            return labStr;
        }


        private static void WriteSTbCategoriesAndStyles(System.IO.TextWriter tw)
        {
            tw.WriteLine("<Categories>");
            tw.WriteLine("<Category Id=\"Accept\" Label=\"Accept\" />");
            tw.WriteLine("<Category Id=\"BasicBlock\" Label=\"Basic block\" />");
            tw.WriteLine("<Category Id=\"BranchCase\" Label=\"Branch case\" />");
            tw.WriteLine("<Category Id=\"BranchCondition\" Label=\"Branch condition\" />");
            tw.WriteLine("<Category Id=\"Contains\" Label=\"Contains\" CanBeDataDriven=\"False\" CanLinkedNodesBeDataDriven=\"True\" IncomingActionLabel=\"Contained By\" IsContainment=\"True\" OutgoingActionLabel=\"Contains\" />");
            tw.WriteLine("<Category Id=\"FinalTransition\" Label=\"Final transition\" />");
            tw.WriteLine("<Category Id=\"InitialState\" Label=\"Initial state\" BasedOn=\"State\" />");
            tw.WriteLine("<Category Id=\"InitialTransition\" Label=\"Initial transition\" />");
            tw.WriteLine("<Category Id=\"Reject\" Label=\"Reject\" />");
            tw.WriteLine("<Category Id=\"Rule\" Label=\"Rule\" IsTag=\"True\" />");
            tw.WriteLine("<Category Id=\"RuleEntry\" Label=\"Rule entry\" />");
            tw.WriteLine("<Category Id=\"RuleExit\" Label=\"Rule exit\" />");
            tw.WriteLine("<Category Id=\"PartialRule\" Label=\"Partial Rule\" BasedOn=\"Rule\" Stroke=\"tomato\" />");
            tw.WriteLine("<Category Id=\"FinalRule\" Label=\"Final Rule\" BasedOn=\"Rule\" StrokeDashArray=\"8 8\" />");
            tw.WriteLine("<Category Id=\"FinalPartialRule\" Label=\"Final Partial Rule\" BasedOn=\"PartialRule\" StrokeDashArray=\"8 8\" />");
            tw.WriteLine("</Categories>");
            tw.WriteLine("<Styles>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"State\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('State')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("<Setter Property=\"Background\" Value=\"white\" />");
            tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"InitialState\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('InitialState')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("<Setter Property=\"Background\" Value=\"white\" />");
            tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"Reject\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('Reject')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("<Setter Property=\"NodeRadius\" Value=\"0\" />");
            tw.WriteLine("<Setter Property=\"Background\" Value=\"tomato\" />");
            tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("<Setter Property=\"MaxWidth\" Value=\"100\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"Accept\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('Accept')\" />");
            tw.WriteLine("<Setter Property=\"Background\" Value=\"lightgreen\" />");
            tw.WriteLine("<Setter Property=\"NodeRadius\" Value=\"0\" />");
            tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"BranchCondition\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('BranchCondition')\" />");
            tw.WriteLine("<Setter Property=\"NodeRadius\" Value=\"0\" />");
            tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("<Setter Property=\"MaxWidth\" Value=\"100\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("<Setter Property=\"Background\" Value=\"white\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"BasicBlock\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('BasicBlock')\" />");
            tw.WriteLine("<Setter Property=\"NodeRadius\" Value=\"0\" />");
            tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("<Setter Property=\"MaxWidth\" Value=\"100\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("<Setter Property=\"Background\" Value=\"white\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Node\" GroupLabel=\"Rule\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('Rule')\" />");
            tw.WriteLine("<Setter Property=\"MinWidth\" Value=\"0\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Link\" GroupLabel=\"RuleEntry\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('RuleEntry')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Link\" GroupLabel=\"RuleExit\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('RuleExit')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Link\" GroupLabel=\"InitialTransition\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('InitialTransition')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Link\" GroupLabel=\"FinalTransition\" ValueLabel=\"True\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('FinalTransition')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("<Setter Property=\"StrokeDashArray\" Value=\"8 8\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("<Style TargetType=\"Link\" GroupLabel=\"BranchCase\" ValueLabel=\"False\">");
            tw.WriteLine("<Condition Expression=\"HasCategory('BranchCase')\" />");
            tw.WriteLine("<Setter Property=\"Stroke\" Value=\"black\" />");
            tw.WriteLine("</Style>");
            tw.WriteLine("</Styles>");
        }


        #endregion
    }
}
