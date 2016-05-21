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
        internal static string DescribeTokens(Tokens t)
        {
            switch (t)
            {
                case Tokens.AND: return "&";
                case Tokens.ARROW: return "->";
                case Tokens.COLON: return ":";
                case Tokens.COMMA: return ",";
                case Tokens.DIV: return "/";
                case Tokens.DOT: return ".";
                case Tokens.EQ: return "=";
                case Tokens.EQUIV: return "<=>";
                case Tokens.GE: return ">=";
                case Tokens.GT: return ">";
                case Tokens.IMPLIES: return "=>";
                case Tokens.LBRACE: return "{";
                case Tokens.LBRACKET: return "[";
                case Tokens.LE: return "<=";
                case Tokens.LPAR: return "(";
                case Tokens.LT: return "<";
                case Tokens.M2LSTR: return "m2l-str";
                case Tokens.M2LTREE: return "m2l-tree";
                case Tokens.MINUS: return "-";
                case Tokens.MOD: return "%";
                case Tokens.NE: return "~=";
                case Tokens.NOT: return "~";
                case Tokens.OR: return "|";
                case Tokens.PLUS: return "+";
                case Tokens.RBRACE: return "}";
                case Tokens.RBRACKET: return "]";
                case Tokens.RPAR: return ")";
                case Tokens.SEMICOLON: return ";";
                case Tokens.SETMINUS: return @"\";
                case Tokens.SUBSET: return "sub";
                case Tokens.TIMES: return "*";
                case Tokens.UP: return "^";
                default: 
                    return t.ToString().ToLower();
            }
        }

        internal MonaParser(Stream str, string file = null)
            : base(new Scanner(str))
        {
            InitializeAliasses();
            if (file != null)
                ((Scanner)(this.Scanner)).sourcefile = file;
        }

        HashSet<string> FV = new HashSet<string>();

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
                for (int i = 0; i < (int)Tokens.maxParseToken; i++)
                    aliasses[i] = "'" + DescribeTokens((Tokens)i) + "'";
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
            var pgm = new Program(null, (Cons<Decl>)decls);
            program = pgm;
            return pgm;
        }
        Program MkProgram(object header, object decls)
        {
            var pgm = new Program((Token)header, (Cons<Decl>)decls);
            program = pgm;
            return pgm;
        }
        #endregion

        #region Cons<T> construction
        //Cons<Expression> MkExpressions(object first, object rest)
        //{
        //    return new Cons<Expression>((Expression)first, (Cons<Expression>)rest);
        //}
        //Cons<Expression> MkExpressions()
        //{
        //    return Cons<Expression>.Empty;
        //}

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
        Formula MkBooleanFormula(object token, object arg1, object arg2 = null)
        {
            if (arg2 == null)
                return new NegatedFormula((Token)token, arg1 as Formula);
            else
                return new BinaryBooleanFormula((Token)token, arg1 as Formula, arg2 as Formula);
        }

        BooleanConstant MkBooleanConstant(object token)
        {
            return new BooleanConstant((Token)token);
        }

        BinaryAtom MkAtom2(object token, object term1, object term2)
        {
            return new BinaryAtom((Token)token, (Term)term1, (Term)term2);
        }

        PredApp MkPredApp(object token, object exprs)
        {
            return new PredApp((Token)token, (Cons<Expression>)exprs);
        }

        QBFormula MkQ0Formula(object quantifier, object vars, object formula)
        {
            return new QBFormula((Token)quantifier, (Cons<Token>)vars, (Formula)formula);
        }

        QFormula MkQFormula(object quantifier, object vars, object formula)
        {
            return new QFormula((Token)quantifier, (Cons<VarWhere>)vars, (Formula)formula);
        }

        QFormula MkQFormula(object quantifier, object vars, object formula, object univs)
        {
            return new QFormula((Token)quantifier, (Cons<VarWhere>)vars, (Formula)formula, (Cons<Token>)univs);
        }

        BooleanVariable MkBooleanVariable(object token)
        {
            return new BooleanVariable((Token)token);
        }

        #endregion

        VarWhere MkVarWhere(object name, object where)
        {
            return new VarWhere((Token)name, (Formula)where);
        }

        VarWhere MkVarWhere(object name)
        {
            return new VarWhere((Token)name, null);
        }

        ArithmFuncApp MkArithmFuncApp(object func, object arg1, object arg2)
        {
            return new ArithmFuncApp((Token)func, (Term)arg1, (Term)arg2);
        }

        Int MkInt(object integer)
        {
            return new Int((Token)integer);
        }

        Name MkName(object name)
        {
            return new Name((Token)name);
        }

        VarDecl MkVar1Decl(object vars, object univs = null)
        {
            var vars_ = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var v in vars_)
                if (!FV.Add(v.name.text))
                    throw new MonaParseException(v.name.Location, string.Format("name '{0}' is already in use", v.name.text));
            return new VarDecl(DeclKind.var1, univs_, vars_);
        }

        VarDecl MkVar2Decl(object vars, object univs = null)
        {
            var vars_ = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var v in vars_)
                if (!FV.Add(v.name.text))
                    throw new MonaParseException(v.name.Location, string.Format("name '{0}' is already in use", v.name.text));
            return new VarDecl(DeclKind.var2, univs_, vars_);
        }

        VarDecl MkTreeDecl(object vars, object univs = null)
        {
            var vars_ = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var v in vars_)
                if (!FV.Add(v.name.text))
                    throw new MonaParseException(v.name.Location, string.Format("name '{0}' is already in use", v.name.text));
            return new VarDecl(DeclKind.tree, univs_, vars_);
        }

        Var0Decl MkVar0Decl(object names)
        {
            var names_ = (Cons<Token>)names;
            foreach (var v in names_)
                if (!FV.Add(v.text))
                    throw new MonaParseException(v.Location, string.Format("name '{0}' is already in use", v.text));
            return new Var0Decl(names_);
        }

        UnivDecl MkUnivDecl(object univargs)
        {
            var univargs_ = (Cons<UnivArg>)univargs;
            return new UnivDecl(univargs_);
        }

        UnivArg MkUnivArg(object name)
        {
            var v = (Token)name;
            if (!FV.Add(v.text))
                throw new MonaParseException(v.Location, string.Format("name '{0}' is already in use", v.text));

            return new UnivArg((Token)name);
        }

        UnivArgWithType MkUnivArgWithType(object name, object t)
        {
            var v = (Token)name;
            if (!FV.Add(v.text))
                throw new MonaParseException(v.Location, string.Format("name '{0}' is already in use", v.text));

            return new UnivArgWithType((Token)name, (Token)t);
        }

        UnivArgWithSucc MkUnivArgWithSucc(object name, object succ)
        {
            var v = (Token)name;
            if (!FV.Add(v.text))
                throw new MonaParseException(v.Location, string.Format("name '{0}' is already in use", v.text));

            Token i = (Token)succ;

            if (!System.Text.RegularExpressions.Regex.IsMatch(i.text, "^(0|1)+$"))
                throw new MonaParseException(i.Location, string.Format("'{0}' is out of range, must match ^(0|1)+$", i.text));

            return new UnivArgWithSucc((Token)name, i.text);
        }

        AssertDecl MkAssertDecl(object formula)
        {
            return new AssertDecl((Formula)formula);
        }

        ExecuteDecl MkExecuteDecl(object formula)
        {
            return new ExecuteDecl((Formula)formula);
        }


        Dictionary<string, ConstDecl> constDecls = new Dictionary<string, ConstDecl>();

        ConstDecl MkConstDecl(object name, object def)
        {
            var v = (Token)name;
            var t = (Term)def; 
            if (!FV.Add(v.text))
                throw new MonaParseException(v.Location, string.Format("name '{0}' is already in use", v.text));
            var d = new ConstDecl(v, t);
            constDecls[v.text] = d;
            return d;
        }

        Name MkConstRef(object name)
        {
            var v = (Token)name;
            if (!constDecls.ContainsKey(v.text))
                throw new MonaParseException(v.Location, string.Format("constant '{0}' is undeclared", v.text));

            return new Name(v);
        }

        DefaultWhereDecl MkDefaultWhere1Decl(object param, object formula)
        {
            return new DefaultWhereDecl(false, (Token)param, (Formula)formula);
        }

        DefaultWhereDecl MkDefaultWhere2Decl(object param, object formula)
        {
            return new DefaultWhereDecl(true, (Token)param, (Formula)formula);
        }

        FormulaDecl MkFormulaDecl(object formula)
        {
            return new FormulaDecl((Formula)formula);
        }

        Formula MkIsEmpty(object token, object term)
        {
            return new IsEmpty((Token)token, (Term)term);
        }

        MinOrMax MkMinOrMax(object token, object term)
        {
            return new MinOrMax((Token)token, (Term)term);
        }

        Restrict MkRestrict(object token, object formula)
        {
            return new Restrict((Token)token, (Formula)formula);
        }
    }
}

