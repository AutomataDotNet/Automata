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
                case Tokens.RANGE: return ",...,";
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
        Expr MkBooleanFormula(object token, object arg1, object arg2 = null)
        {
            if (arg2 == null)
                return new NegatedFormula((Token)token, arg1 as Expr);
            else
                return new BinaryBooleanFormula((Token)token, arg1 as Expr, arg2 as Expr);
        }

        BooleanConstant MkBooleanConstant(object token)
        {
            return new BooleanConstant((Token)token);
        }

        BinaryAtom MkAtom2(object token, object term1, object term2)
        {
            return new BinaryAtom((Token)token, (Expr)term1, (Expr)term2);
        }

        PredApp MkPredApp(object token, object exprs)
        {
            return new PredApp((Token)token, (Cons<Expr>)exprs);
        }

        QBFormula MkQ0Formula(object quantifier, object vars, object formula)
        {
            return new QBFormula((Token)quantifier, (Cons<Token>)vars, (Expr)formula);
        }

        QFormula MkQFormula(object quantifier, object vars, object formula)
        {
            return new QFormula((Token)quantifier, (Cons<VarWhere>)vars, (Expr)formula);
        }

        QFormula MkQFormula(object quantifier, object vars, object formula, object univs)
        {
            return new QFormula((Token)quantifier, (Cons<VarWhere>)vars, (Expr)formula, (Cons<Token>)univs);
        }

        Name MkBooleanVariable(object token)
        {
            return new Name((Token)token, ExprType.BOOL);
        }

        #endregion

        VarWhere MkVarWhere(object name, object where)
        {
            return new VarWhere((Token)name, (Expr)where);
        }

        VarWhere MkVarWhere(object name)
        {
            return new VarWhere((Token)name, null);
        }

        ArithmFuncApp MkArithmFuncApp(object func, object arg1, object arg2)
        {
            return new ArithmFuncApp((Token)func, (Expr)arg1, (Expr)arg2);
        }

        Int MkInt(object integer)
        {
            return new Int((Token)integer);
        }

        Name MkName(object name)
        {
            return new Name((Token)name);
        }

        Name MkName1(object name)
        {
            return new Name((Token)name, ExprType.INT);
        }

        Name MkName2(object name)
        {
            return new Name((Token)name, ExprType.SET);
        }

        VarDecl MkVar1Decl(object vars, object univs = null)
        {
            var vars_ = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var v in vars_)
                if (!FV.Add(v.name))
                    throw new MonaParseException(v.token.Location, string.Format("name '{0}' is already in use", v.name));
            return new VarDecl(DeclKind.var1, univs_, vars_);
        }

        VarDecl MkVar2Decl(object vars, object univs = null)
        {
            var vars_ = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var v in vars_)
                if (!FV.Add(v.name))
                    throw new MonaParseException(v.token.Location, string.Format("name '{0}' is already in use", v.name));
            return new VarDecl(DeclKind.var2, univs_, vars_);
        }

        VarDecl MkTreeDecl(object vars, object univs = null)
        {
            var vars_ = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var v in vars_)
                if (!FV.Add(v.name))
                    throw new MonaParseException(v.token.Location, string.Format("name '{0}' is already in use", v.name));
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
            return new AssertDecl((Expr)formula);
        }

        ExecuteDecl MkExecuteDecl(object formula)
        {
            return new ExecuteDecl((Expr)formula);
        }


        Dictionary<string, ConstDecl> constDecls = new Dictionary<string, ConstDecl>();

        ConstDecl MkConstDecl(object name, object def)
        {
            var v = (Token)name;
            var t = (Expr)def; 
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

            return new Name(v, ExprType.INT);
        }

        DefaultWhereDecl MkDefaultWhere1Decl(object param, object formula)
        {
            return new DefaultWhereDecl(false, (Token)param, (Expr)formula);
        }

        DefaultWhereDecl MkDefaultWhere2Decl(object param, object formula)
        {
            return new DefaultWhereDecl(true, (Token)param, (Expr)formula);
        }

        FormulaDecl MkFormulaDecl(object formula)
        {
            return new FormulaDecl((Expr)formula);
        }

        Expr MkIsEmpty(object token, object term)
        {
            return new IsEmpty((Token)token, (Expr)term);
        }

        MinOrMax MkMinOrMax(object token, object term)
        {
            return new MinOrMax((Token)token, (Expr)term);
        }

        Restrict MkRestrict(object token, object formula)
        {
            return new Restrict((Token)token, (Expr)formula);
        }

        PredDecl MkPredDecl(object name, object parameters, object formula, bool isMacro = false)
        {
            var v = (Token)name;
            if (!FV.Add(v.text))
                throw new MonaParseException(v.Location, string.Format("name '{0}' is already in use", v.text));
            var params_ = parameters as Cons<Param>;
            var pmap = new Dictionary<string,Param>();
            if (params_ != null && !params_.IsEmpty)
            {
                foreach (var p in params_)
                {
                    if (pmap.ContainsKey(p.Token.text))
                        throw new MonaParseException(p.Token.Location, string.Format("duplicate parameter name '{0}'", p.Token.text));
                    pmap[p.Token.text] = p;
                }
            }
            return new PredDecl((Token)name, params_, pmap, (Expr)formula, isMacro);
        }

        Var0Param MkVar0Param(object name)
        {
            return new Var0Param((Token)name);
        }
        Var1Param MkVar1Param(object varwhere)
        {
            return new Var1Param((VarWhere)varwhere);
        }
        Var2Param MkVar2Param(object varwhere)
        {
            return new Var2Param((VarWhere)varwhere);
        }
        UniverseParam MkUniverseParam(object universename)
        {
            return new UniverseParam((Token)universename);
        }

        Set MkSet(object empty, object elems = null)
        {
            return new Set((Token)empty, elems as Cons<Expr>);
        }

        SetOp MkSetOp(object op, object arg1, object arg2)
        {
            return new SetOp((Token)op, (Expr)arg1, (Expr)arg2);
        }

        Pconst MkPconst(object pconstToken, object i)
        {
            return new Pconst((Token)pconstToken, (Expr)i);
        }

        Range MkRange(object rangeToken, object from, object to)
        {
            return new Range((Token)rangeToken, (Expr)from, (Expr)to);
        }


    }
}

