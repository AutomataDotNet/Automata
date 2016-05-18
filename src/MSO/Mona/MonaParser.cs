using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using QUT.Gppg;

namespace Microsoft.Automata.MSO.Mona
{
    public partial class MonaParser : ShiftReduceParser<object, LexLocation>
    {
        internal MonaParser(Stream str, string file = null)
            : base(new Scanner(str))
        {
            InitializeAliasses();
            if (file != null)
                ((Scanner)(this.Scanner)).sourcefile = file;
        }

        Dictionary<string, Tuple<VarWhere, Tokens>> FV =
            new Dictionary<string, Tuple<VarWhere, Tokens>>();

        /// <summary>
        /// Parses a Mona program from a given text string.
        /// </summary>
        public static Program Parse(string text, string filename = null)
        {
            MemoryStream mstr = null;
            Program pgm = null;
            try
            {
                mstr = new MemoryStream(Encoding.UTF8.GetBytes(text));
                mstr.Position = 0;
                pgm = Parse(mstr, filename);
            }
            finally
            {
                if (mstr != null)
                    mstr.Dispose();
            }
            return pgm;
        }

        /// <summary>
        /// Parses a Fast program from given stream.
        /// </summary>
        public static Program Parse(Stream stream, string filename = null)
        {
            var parser = new MonaParser(stream, filename);
            bool ok = parser.Parse();
            if (ok)
                return parser.program;
            else
                throw new MonaParseException("Error: mona parser failed", filename);
        }

        private static void InitializeAliasses()
        {
            if (aliasses == null)
            {
                aliasses = new Dictionary<int, string>();
                aliasses[(int)Tokens.ALL0] = "'all0'";
                aliasses[(int)Tokens.ALL1] = "'all1'";
                aliasses[(int)Tokens.ALL2] = "'all2'";
                aliasses[(int)Tokens.AND] = "'&'";
                aliasses[(int)Tokens.ARROW] = "'->'";
                aliasses[(int)Tokens.COLON] = "':'";
                aliasses[(int)Tokens.COMMA] = "','";
                aliasses[(int)Tokens.DIV] = "'/'";
                aliasses[(int)Tokens.DOT] = "'.'";
                aliasses[(int)Tokens.EMPTY] = "'empty'";
                aliasses[(int)Tokens.EQ] = "'='";
                aliasses[(int)Tokens.EQUIV] = "'<=>'";
                aliasses[(int)Tokens.EX0] = "'ex0'";
                aliasses[(int)Tokens.EX1] = "'ex1'";
                aliasses[(int)Tokens.EX2] = "'ex2'";
                aliasses[(int)Tokens.FALSE] = "'false'";
                aliasses[(int)Tokens.GE] = "'>='";
                aliasses[(int)Tokens.GT] = "'>'";
                aliasses[(int)Tokens.IMPLIES] = "'=>'";
                aliasses[(int)Tokens.IN] = "'in'";
                aliasses[(int)Tokens.INTER] = "'inter'";
                aliasses[(int)Tokens.LBRACE] = "'{'";
                aliasses[(int)Tokens.LBRACKET] = "'['";
                aliasses[(int)Tokens.LE] = "'<='";
                aliasses[(int)Tokens.LET0] = "'let0'";
                aliasses[(int)Tokens.LET1] = "'let1'";
                aliasses[(int)Tokens.LET2] = "'let2'";
                aliasses[(int)Tokens.LPAR] = "'('";
                aliasses[(int)Tokens.LT] = "'<'";
                aliasses[(int)Tokens.M2LSTR] = "'m2l-str'";
                aliasses[(int)Tokens.M2LTREE] = "'m2l-tree'";
                aliasses[(int)Tokens.MAX] = "'max'";
                aliasses[(int)Tokens.MIN] = "'min'";
                aliasses[(int)Tokens.MINUS] = "'-'";
                aliasses[(int)Tokens.MOD] = "'%'";
                aliasses[(int)Tokens.NAME] = "name";
                aliasses[(int)Tokens.NE] = "'~='";
                aliasses[(int)Tokens.NOT] = "'~'";
                aliasses[(int)Tokens.NOTIN] = "'notin'";
                aliasses[(int)Tokens.NUMBER] = "integer";
                aliasses[(int)Tokens.OR] = "'|'";
                aliasses[(int)Tokens.PCONST] = "'pconst'";
                aliasses[(int)Tokens.PLUS] = "'+'";
                aliasses[(int)Tokens.PREFIX] = "'prefix'";
                aliasses[(int)Tokens.RBRACE] = "'}'";
                aliasses[(int)Tokens.RBRACKET] = "']'";
                aliasses[(int)Tokens.RESTRICT] = "'restrict'";
                aliasses[(int)Tokens.RPAR] = "')'";
                aliasses[(int)Tokens.SEMICOLON] = "';'";
                aliasses[(int)Tokens.SETMINUS] = @"'\'";
                aliasses[(int)Tokens.SUBSET] = "'subset'";
                aliasses[(int)Tokens.TIMES] = "'*'";
                aliasses[(int)Tokens.TRUE] = "'true'";
                aliasses[(int)Tokens.UNION] = "'union'";
                aliasses[(int)Tokens.UNIVERSE] = "'universe'";
                aliasses[(int)Tokens.UP] = "'^'";
                aliasses[(int)Tokens.VAR0] = "'var0'";
                aliasses[(int)Tokens.VAR1] = "'var1'";
                aliasses[(int)Tokens.VAR2] = "'var2'";
                aliasses[(int)Tokens.WHERE] = "'where'";
                aliasses[(int)Tokens.WS1S] = "'ws1s'";
                aliasses[(int)Tokens.WS2S] = "'ws2s'";
            }
        }

        static int Test(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Error: wrong arguments");
                return -1;
            }

            var path = args[0];
            FileInfo[] files;
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Exists)
                    files = di.GetFiles("*.fast");
                else
                {
                    FileInfo fi = new FileInfo(path);
                    if (!fi.Exists)
                    {
                        Console.Error.WriteLine("Error: file '{0}' does not exist", path);
                        return -1;
                    }
                    files = new FileInfo[] { fi };
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: {0}", e.Message);
                return -1;
            }
            foreach (var fi in files)
            {
                Stream str = null;
                try
                {
                    str = fi.OpenRead();
                    var p = Parse(str);
                    str.Dispose();
                    p.Typecheck();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(fi.FullName + " " + e.ToString());
                    if (str != null)
                        str.Dispose();
                    return -1;
                }
            }
            return 0;
        }

        #region Program construction
        Program program = null;
        Program MkProgram(object decls)
        {
            var pgm = new Program(null, (Cons<Ast>)decls);
            program = pgm;
            return pgm;
        }
        Program MkProgram(object header, object decls)
        {
            var pgm = new Program((Token)header, (Cons<Ast>)decls);
            program = pgm;
            return pgm;
        }
        #endregion

        #region Cons<T> construction
        Cons<Ast> MkDeclarations(object first, object rest)
        {
            return new Cons<Ast>((Ast)first, (Cons<Ast>)rest);
        }
        Cons<Ast> MkDeclarations()
        {
            return Cons<Ast>.Empty;
        }

        Cons<Expression> MkExpressions(object first, object rest)
        {
            return new Cons<Expression>((Expression)first, (Cons<Expression>)rest);
        }
        Cons<Expression> MkExpressions()
        {
            return Cons<Expression>.Empty;
        }

        Cons<T> MkList<T>(object first, object rest)
        {
            return new Cons<T>((T)first, (Cons<T>)rest);
        }
        Cons<T> MkList<T>()
        {
            return Cons<T>.Empty;
        }
        #endregion

        #region Formula construction 
        BooleanFormula MkBooleanFormula(object token, params object[] args)
        {
            var subformulas = Array.ConvertAll(args, a => (Formula)a);
            return new BooleanFormula((Token)token, subformulas);
        }

        Atom MkAtom(object token, params object[] terms)
        {
            var terms_ = Array.ConvertAll(terms, t => (Term)t);
            return new Atom((Token)token, terms_);
        }

        PredApp MkPredApp(object token, object exprs)
        {
            return new PredApp((Token)token, (Cons<Expression>)exprs);
        }

        Q0Formula MkQ0Formula(object quantifier, object vars, object formula)
        {
            return new Q0Formula((Token)quantifier, (Cons<Token>)vars, (Formula)formula);
        }

        QFormula MkQFormula(object quantifier, object vars, object formula)
        {
            return new QFormula((Token)quantifier, (Cons<VarWhere>)vars, (Formula)formula);
        }

        QFormula MkQFormula(object quantifier, object vars, object formula, object univs)
        {
            return new QFormula((Token)quantifier, (Cons<VarWhere>)vars, (Formula)formula, (Cons<Token>)univs);
        }

        #endregion

        VarWhere MkVar(object name, object where)
        {
            return new VarWhere((Token)name, (Formula)where);
        }

        VarWhere MkVar(object name)
        {
            return new VarWhere((Token)name, null);
        }

        FuncApp MkFuncApp(object func, params object[] args)
        {
            var terms = Array.ConvertAll(args, t => (Term)t);
            return new FuncApp((Token)func, terms);
        }

        Int MkInt(object integer)
        {
            return new Int((Token)integer);
        }

        Name MkName(object name)
        {
            return new Name((Token)name);
        }

        VarDecl MkVarDecl(object token, object vars, object univs = null)
        {
            var vars_ = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            var token_ = (Token)token;
            foreach (var v in vars_)
            {
                if (FV.ContainsKey(v.name.text))
                    throw new MonaParseException(v.name.Location, string.Format("variable '{0}' declared twice", v.name.text));
                else
                    FV[v.name.text] = new Tuple<VarWhere, Tokens>(v, token_.Kind);
            }
            return new VarDecl(token_, univs_, vars_);
        }
    }
}

