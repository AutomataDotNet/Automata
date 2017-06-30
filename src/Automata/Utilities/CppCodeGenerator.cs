using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Automata.Utilities
{

    /// <summary>
    /// Provides functionality for generating c++ acceptors for given automata or regexes
    /// </summary>
    public class CppCodeGenerator
    {
        string namespacename;
        string classname;
        Automaton<BDD>[] automata;
        CharSetSolver solver;
        HelperPredicates helper_predicates;
        bool exportIsMatch;

        public CppCodeGenerator(CharSetSolver solver, string classname, string namespacename, bool exportIsMatch, bool optimzeForAsciiInput, params Automaton<BDD>[] automata)
        {
            this.automata = automata;
            this.solver = solver;
            this.classname = classname;
            this.namespacename = namespacename;
            this.helper_predicates = new HelperPredicates(solver, optimzeForAsciiInput);
            this.exportIsMatch = exportIsMatch;
        }

        public CppCodeGenerator(CharSetSolver solver, string classname, string namespacename, bool exportIsMatch, bool optimzeForAsciiInput, params Regex[] regexes)
        {
            this.automata = Array.ConvertAll(regexes, r => Regex2Automaton(solver, r));
            this.solver = solver;
            this.classname = classname;
            this.namespacename = namespacename;
            this.helper_predicates = new HelperPredicates(solver, optimzeForAsciiInput);
            this.exportIsMatch = exportIsMatch;
        }

        public static Automaton<BDD> Regex2Automaton(CharSetSolver solver, Regex regex)
        {
            var aut = solver.Convert(regex.ToString(), regex.Options);
            //var autEpsFree = aut.RemoveEpsilons(solver.MkOr);
            var autDet = aut.Determinize();
            var autMin = autDet.Minimize();
            return autMin;
        }

        static string UTF8toUTF16_Method = @"
        //*i is the current idex of str, size is the length of str, *i must be in [0..size-1], initially 0
        //*r is the the leftover lowsurrogate portion from first three bytes in 4byte encoding, intially 0
        //*c is the next UTF16 character code if the return value is true and *i is the index to the next character encoding
        //if the return value is false then the UTF8 encoding was incorrect
        static bool UTF8toUTF16(unsigned short* r, int* i, unsigned short* c, int size, unsigned char* str)
        {
            if (*r == 0)
            {   //*r==0 means that we are not in the middle of 4byte encoding
                unsigned short b1 = str[*i];
                if (b1 <= 0x7F)
                {
                    *c = b1; 
                    *i += 1;
                    return true;
                }
                else if (0xC2 <= b1 && b1 <= 0xDF) //two byte encoding
                {
                    *i += 1;
                    if (*i == size)
                        return false; 
                    else {
                        unsigned short b2 = str[*i];
                        if (0x80 <= b2 && b2 <= 0xBF)
                        {
                            *c = ((b1 & 0x3F) << 6) | (b2 & 0x3F);
                            *i += 1;
                            return true;
                        }
                        return false;
                    }
                }
                else if (0xE0 <= b1 && b1 <= 0xEF)  //three byte encoding
                {
                    *i += 1;
                    if (*i + 1 >= size)
                        return false; 
                    else
                    {
                        unsigned short b2 = str[*i];
                        if ((b1 == 0xE0 && 0xA0 <= b2 && b2 <= 0xBF) ||
                            (b1 == 0xED && 0x80 <= b2 && b2 <= 0x9F) ||
                            (0x80 <= b2 && b2 <= 0xBF))
                        {
                            *i += 1;
                            unsigned short b3 = str[*i];
                            if (0x80 <= b3 && b3 <= 0xBF)
                            {
                                *c = ((b1 & 0xF) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F); //utf8decode the bytes
                                *i += 1;
                                return true;
                            }
                            return false; //invalid third byte
                        }
                        return false; //invalid second byte
                    }
                }
                else if (0xF0 <= b1 && b1 <= 0xF4) //4 byte encoding decoded and reencoded into UTF16 surrogate pair (high, low)
                {
                    *i += 1;
                    if (*i + 2 >= size) //(4 byte check)
                        return false;  //second byte, third byte or fourth byte is missing
                    else
                    {
                        unsigned short b2 = str[*i];
                        if ((b1 == 0xF0 && (0x90 <= b2 && b2 <= 0xBF)) ||
                            (b1 == 0xF4 && (0x80 <= b2 && b2 <= 0x8F)) ||
                            (0x80 <= b2 && b2 <= 0xBF))
                        {
                            *i += 1;
                            unsigned short b3 = str[*i];
                            if (0x80 <= b3 && b3 <= 0xBF)
                            {
                                //set *c to high surrogate
                                *c = 0xD800 | (((((b1 & 7) << 2) | ((b2 & 0x30) >> 4)) - 1) << 6) | ((b2 & 0x0F) << 2) | ((b3 >> 4) & 3);
                                *r = 0xDC00 | ((b3 & 0xF) << 6); //set the low surrogate register
                                *i += 1;
                                return true;
                            }
                            else
                                return false; //incorrect third byte
                        }
                        else
                            return false; //incorrect second byte
                    }
                }
                else
                    return false; //incorrect first byte
            }
            else //compute the low surrogate
            {
                unsigned short b4 = str[*i]; //we know *i < size due to the above check (4 byte check)
                if (0x80 <= b4 && b4 <= 0xBF)
                {
                    *i += 1;
                    *c = (*r | (b4 & 0x3F)); //set *c to low surrogate
                    *r = 0;                  //reset the low surrogate register
                    return true;
                }
                return false; //incorrect fourth byte
            }
        }";

        string isMatchExport
        {
            get
            {
                return @"

extern ""C"" __declspec(dllexport) int IsMatch(int index, int size, unsigned char* str)
{
	bool status = " + namespacename + "::" + classname + @"::IsMatchByIndex(index, size, str);
    if (status)
        return 1;
    else 
        return 0;
}
";
            }
        }

        string Generate_IsMatchByIndex()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"
        static bool IsMatchByIndex(int index, int size, unsigned char* str)
        {
            switch (index)
            {");
            for (int i = 0; i < automata.Length; i++)
                sb.Append(string.Format(@"
            case {0}: return IsMatch{1}(size, str);", i, (i == 0 ? "" : i.ToString())));
            sb.Append(@"
            default: return false;
            }
        }");
            return sb.ToString();
        }

        string Generate_IsMatch(int index)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format(@"
        static bool IsMatch{0}(int k, unsigned char* str)
        {{
            unsigned short r = 0;
            unsigned short x = 0;
            int i = 0;", (index == 0 ? "" : index.ToString())));
            sb.Append(GenerateTransitionsFor(index));
            sb.Append(@"
        }");
            return sb.ToString();
        }

        string GenerateTransitionsFor(int i)
        {
            StringBuilder transitions = new StringBuilder();
            var stack = new SimpleStack<int>();
            var set = new HashSet<int>();
            var automaton = automata[i];
            stack.Push(automaton.InitialState);
            set.Add(automaton.InitialState);

            Predicate<int> IsFinalSink = (q =>
                (automaton.GetMovesCountFrom(q) == 1 && automaton.IsLoopState(q)
                && automaton.IsFinalState(q) && automaton.GetMoveFrom(q).Label.IsFull));

            Predicate<int> IsNonfinalSink = (q =>
                (!automaton.IsFinalState(q) &&
                  (automaton.GetMovesCountFrom(q) == 0 ||
                    (automaton.GetMovesCountFrom(q) == 1 && automaton.IsLoopState(q)))));

            while (stack.IsNonempty)
            {
                int q = stack.Pop();
                bool q_is_complete = false;
                if (IsFinalSink(q))
                    transitions.Append(String.Format(@"
        State{0}:
            return true;", q));
                else if (IsNonfinalSink(q))
                {
                    transitions.Append(String.Format(@"
        State{0}:
            return false;", q));
                }
                else
                {
                    transitions.Append(String.Format(@"
        State{0}:
            if (i == k)
                return {1};", q, (automaton.IsFinalState(q) ? "true" : "false")));
                    if (automaton.GetMovesCountFrom(q) > 0) //q is a sink
                    {
                        transitions.Append(String.Format(@"
            if (!UTF8toUTF16(&r, &i, &x, k, str))
                return false;"));
                        //---------------------------------------------------------------------
                        //many potential optimizations can be made in generating the conditions
                        //---------------------------------------------------------------------
                        var path = solver.True;
                        foreach (var move in automaton.GetMovesFrom(q))
                        {
                            path = solver.MkDiff(path, move.Label);
                            if (path == solver.False) //this is the last else case
                            {
                                transitions.Append(String.Format(@"
            goto State{0};", move.TargetState));
                                q_is_complete = true;
                            }
                            else
                                transitions.Append(String.Format(@" 
            if ({0})
                goto State{1};", helper_predicates.GeneratePredicate(move.Label), move.TargetState));
                            if (set.Add(move.TargetState))
                                stack.Push(move.TargetState);
                        }
                    }
                    if (!q_is_complete)
                        //reject the input, this corresponds to q being a partial state 
                        //the implicit transition is to a deadend sink state
                        transitions.Append(@"
            return false;");
                }
            }
            return transitions.ToString();
        }


        public string GenerateMatcher()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format(@"
#include <stdio.h>

namespace {0}
{{
    class {1}
    {{
    private:", namespacename, classname));
            sb.Append(UTF8toUTF16_Method);
            StringBuilder matchers = new StringBuilder();
            for (int i = 0; i < automata.Length; i++)
                matchers.AppendLine(Generate_IsMatch(i));
            sb.Append(helper_predicates.ToString());
            sb.Append(@"
    public:");
            sb.Append(matchers.ToString());
            sb.Append(Generate_IsMatchByIndex());
            sb.Append(@"
    };
}");
            if (exportIsMatch)
                sb.Append(isMatchExport);

            return sb.ToString();
        }

        public void GenerateMatcherToFile(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            DirectoryInfo di = fi.Directory;
            if (!di.Exists)
                di.Create();
            if (fi.Exists)
                fi.IsReadOnly = false;
            StreamWriter sw = new StreamWriter(filename);
            string cpp = GenerateMatcher();
            sw.WriteLine(cpp);
            sw.Close();
        }
    }
}
