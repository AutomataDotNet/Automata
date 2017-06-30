using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bek.Query;
using Microsoft.Bek.Frontend;
using Microsoft.Bek.Frontend.Meta;
using Microsoft.Bek.Model;
using System.IO;
using STModel = Microsoft.Automata.ST<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using STbModel = Microsoft.Automata.STb<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;
using SFAModel = Microsoft.Automata.SFA<Microsoft.Z3.FuncDecl, Microsoft.Z3.Expr, Microsoft.Z3.Sort>;

using Microsoft.Automata.Z3;
using Microsoft.Automata;



namespace Microsoft.Bek
{
    public class Pgm
    {
        internal static int CountLines(string str)
        {
            int i = 0;
            int pos = str.IndexOf('\n');

            while (pos != -1)
            {
                pos = str.IndexOf('\n', pos + 1);
                ++i;
            }

            return i;
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: tool.exe queryfile.bek [js_master_file]");
                return;
            }

            StreamReader sr = new StreamReader(args[0]);
            string input = sr.ReadToEnd();
            sr.Close();

            string bek_js_master_file = (args.Length > 1 ? args[1] : "");

            Run(input, Console.Out, bek_js_master_file);
        }

        public static void Run(string input, TextWriter tw, string bek_js_master_file)
        {
            //input = input.Replace("\t", "  ");
            input = input.Replace("\r\n", "\n"); 
            string input1a = (input.StartsWith("==") ? "\n" + input : input);
            string input1 = (input1a.EndsWith("\n") ? input1a : input1a + "\n");

            string[] piece = input1.Split(new string[1] { "\n==\n" }, StringSplitOptions.None);

            if (piece.Length != 2)
            {
                tw.WriteLine(@"bek.bek(1,1): error : Expecting bek programs followed by \n==\n followed by bek queries");
                return;
            }
            //remove whitespace from the start
            //piece[1] = piece[1].TrimStart('\n', '\t', '\f', '\r', '\v', ' ');

            int lines = CountLines(piece[0]);

            List<BekProgram> progs;
            if (lines == 0)
            {
                progs = new List<BekProgram>();
            }
            else
            {
                try
                {
                    progs = BekParser.DefsFromString(piece[0]);
                }
                catch (BekParseException e)
                {
                    tw.WriteLine("pgm.bek({0},{1}): error : BekParseError, {2}", min1(e.line), min1(e.pos), e.Message);
                    return;
                }
                catch (BekException e)
                {
                    tw.WriteLine("pgm.bek({0},{1}): error : BekError, {2}", 1, 1, e.Message);
                    return;
                }
                catch (Exception e)
                {
                    tw.WriteLine("pgm.bek({0},{1}): error : InternalError, {2}", 1, 1, e.Message);
                    return;
                }
            }
            List<Expression> queries;
            List<string> queries_text;
            try
            {
                queries = Expression.ParseQueries(piece[1], out queries_text);
            }
            catch (QueryParseException e)
            {
                tw.WriteLine("query.bek({0},{1}): error : QueryParseError, {2}", e.line + lines + 2, e.pos, e.Message);
                return;
            }
            catch (Exception e)
            {
                tw.WriteLine("query.bek({0},{1}): error : QueryError, {2}", 1, 1, e.Message);
                return;
            }

            var ec = new EvaluationContext(progs, tw, bek_js_master_file);
            try
            {
                for (int i = 0; i < queries.Count; i++)
                {
                    if (i > 0)
                        tw.WriteLine();
                    tw.WriteLine(">> {0}", queries_text[i]);
                    queries[i].RunQuery(ec);
                }
               // ec.Dispose();
            }
            catch (BekParseException e) //bek pgms may be parsed as a result of exploration
            {
                tw.WriteLine("pgm.bek({0},{1}): error : ParseError, {2}", min1(e.line), min1(e.pos), e.Message);
               // ec.Dispose();
                return;
            }
            catch (BekException e)
            {
                tw.WriteLine("pgm.bek({0},{1}): error : BekError, {2}", 1, 1, e.Message);
               // ec.Dispose();
                return;
            }
            catch (QueryParseException e)
            {
                tw.WriteLine("pgm.bek({0},{1}): error : QueryParseError, {2}", e.line + lines + 2, e.pos, e.Message);
               // ec.Dispose();
                return;
            }
            catch (Exception e)
            {
                tw.WriteLine("pgm.bek({0},{1}): error : InternalError, {2}", 1, 1, e.Message);
               // ec.Dispose();
                return;
            }
            finally
            {
                //causes in some cases unhandled exception from Z3 for some reason
                //ec.Dispose();
            }
        }
        static int min1(int i)
        {
            if (i < 1)
                return 1;
            else return i;
        }
    }

    internal class EvaluationContext : IDisposable
    {
        internal Z3Provider solver;
        List<BekProgram> progs;
        internal TextWriter tw;
        internal int imagecount = 1;
        internal string bek_js_master_file;

        internal List<Bek.Frontend.AST.BekLocalFunction> GetLocalFunctions()
        {
            if (progs.Count == 0)
                return new List<Frontend.AST.BekLocalFunction>();
            else
                return progs[0].ast.funcs;
        }

        internal Dictionary<string, BekProgram> bekMap;

        internal Dictionary<string, STModel> stMap = new Dictionary<string, STModel>();

        internal IConverter<STbModel> converter = null;

        internal STModel GetST(Identifier name)
        {
            STModel st = null;
            if (stMap.TryGetValue(name.Name, out st))
                return st;
            else
            {
                BekProgram prog = null;
                if (bekMap.TryGetValue(name.Name, out prog))
                {
                    if (converter == null)  //same local functions for all bek programs
                        converter = BekConverter.MkBekToSTbConverter(solver, prog.ast.funcs, prog.ast.name);

                    st = converter.Convert(prog).ExploreBools().ToST();
                    st.Name = name.Name;
                    stMap[name.Name] = st;
                    return st;
                }
                else
                {
                    throw new QueryParseException(name.Line, name.Pos, string.Format("Undefined transducer: {0}", name));
                }
            }
        }

        //internal Tuple<Z3.Expr[], Z3.Expr> GetDefinition(string fname)
        //{
        //    conver
        //}

        internal Dictionary<string, SFAModel> sfaMap = new Dictionary<string, SFAModel>();
        internal SFAModel GetSFA(Identifier name)
        {
            SFAModel sfa = null;
            if (sfaMap.TryGetValue(name.Name, out sfa))
                return sfa;
            else
                throw new QueryParseException(name.Line, name.Pos, string.Format("Undefined automaton '{0}'", name.Name));
        }

        public EvaluationContext(List<BekProgram> progs, TextWriter tw, string bek_js_master_file)
        {
            this.bek_js_master_file = bek_js_master_file;
            this.tw = tw;
            this.progs = progs;
            this.solver = new Z3Provider();
            this.bekMap = new Dictionary<string, BekProgram>();
            foreach (var bek in progs)
            {
                if (bekMap.ContainsKey(bek.ast.name))
                    throw new BekException(string.Format("Bek program {0} has multiple definitions.", bek.ast.name));

                bekMap[bek.ast.name] = bek;
            }
        }

        public void Dispose()
        {
            solver.Dispose();
        }
    }
}
