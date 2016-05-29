using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Automata.Internal.Utilities;
using QUT.Gppg;

namespace Microsoft.Automata.MSO.Mona
{
    public partial class MonaParser : ShiftReduceParser<object, LexLocationInFile>
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

        Dictionary<string,Decl> GlobalDecls = new Dictionary<string,Decl>();


        /// <summary>
        /// Parses a Mona program from a given file.
        /// </summary>
        public static Program ParseFromFile(string filename)
        {
            Stream stream = null; 
            Program pgm = null;
            try
            {
                stream = File.OpenRead(filename);
                pgm = Parse(stream, filename);
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
            return pgm;
        }

        /// <summary>
        /// Parses a Mona program from a given text string.
        /// </summary>
        public static Program Parse(string text)
        {
            MemoryStream mstr = null;
            Program pgm = null;
            try
            {
                mstr = new MemoryStream(Encoding.UTF8.GetBytes(text));
                mstr.Position = 0;
                pgm = Parse(mstr, null);
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
            try
            {
                var parser = new MonaParser(stream, filename);
                bool ok = parser.Parse();
                if (ok)
                {
                    parser.program.Typecheck();
                    return parser.program;
                }
                else
                    throw new MonaParseException();
            }
            catch (Exception e)
            {
                if (e is MonaParseException)
                    throw e;
                else
                    throw new MonaParseException("unexpected error", e); 
            }
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
        Program MkProgram(object decls, object header = null)
        {
            var pgm = new Program(header as Token, (Cons<Decl>)decls, GlobalDecls);
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

        Cons<T> MkList<T>(object first, object rest = null)
        {
            return new Cons<T>((T)first, (rest == null ? Cons<T>.Empty : (Cons<T>)rest));
        }
        Cons<T> MkList<T>()
        {
            return Cons<T>.Empty;
        }
        #endregion

        #region Formula construction 
        Expr MkBooleanFormula(object token, object arg1, object arg2)
        {
            return new BinaryBooleanFormula((Token)token, arg1 as Expr, arg2 as Expr);
        }

        Expr MkNegatedFormula(object token, object arg1)
        {
            return new NegatedFormula((Token)token, arg1 as Expr);
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
            var vars_ = (Cons<Token>)vars;
            Dictionary<string, Param> varmap = new Dictionary<string, Param>();
            foreach (var v in vars_)
            {
                if (varmap.ContainsKey(v.text))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location);

                varmap[v.text] = new Var0Param(v);
            }
            return new QBFormula((Token)quantifier, vars_, (Expr)formula, varmap);
        }

        QFormula MkQFormula(object quantifier, object vars, object formula, object univs)
        {
            var vws = (Cons<VarWhere>)vars;
            var Q = (Token)quantifier;
            Dictionary<string, Param> varmap = new Dictionary<string, Param>();
            if (Q.Kind == Tokens.EX1 || Q.Kind == Tokens.ALL1)
            {
                foreach (var vw in vws)
                {
                    if (varmap.ContainsKey(vw.name))
                        throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location);

                    varmap[vw.name] = new VarParam(vw, ParamKind.var1);
                }
            }
            else
            {
                foreach (var vw in vws)
                {
                    if (varmap.ContainsKey(vw.name))
                        throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location);

                    varmap[vw.name] = new VarParam(vw, ParamKind.var2);
                }
            }
            return new QFormula(Q, (Cons<VarWhere>)vars, (Expr)formula, null, varmap);
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

        VarDecls MkVar1Decl(object vars, object univs)
        {
            var vws = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var vw in vws)
                if (GlobalDecls.ContainsKey(vw.name))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location, string.Format("name '{0}' is already declared", vw.name));
                else
                    GlobalDecls[vw.name] = new VarDecl(DeclKind.var1, univs_, vw);
            return  new VarDecls(DeclKind.var1, univs_, vws);
        }

        VarDecls MkVar2Decl(object vars, object univs)
        {
            var vars_ = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var vw in vars_)
                if (GlobalDecls.ContainsKey(vw.name))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location, string.Format("name '{0}' is already declared", vw.name));
                else
                    GlobalDecls[vw.name] = new VarDecl(DeclKind.var2, univs_, vw);
            return new VarDecls(DeclKind.var2, univs_, vars_);
        }

        VarDecls MkTreeDecl(object vars, object univs)
        {
            var vars_ = (Cons<VarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var vw in vars_)
                if (GlobalDecls.ContainsKey(vw.name))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location, string.Format("name '{0}' is already declared", vw.name));
                else
                    GlobalDecls[vw.name] = new VarDecl(DeclKind.tree, univs_, vw);
            return new VarDecls(DeclKind.tree, univs_, vars_);
        }

        Var0Decls MkVar0Decl(object names)
        {
            var names_ = (Cons<Token>)names;
            foreach (var v in names_)
                if (GlobalDecls.ContainsKey(v.text))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));
                else
                    GlobalDecls[v.text] = new Var0Decl(v);
            return new Var0Decls(names_);
        }

        UnivDecls MkUnivDecl(object univargs)
        {
            var univargs_ = (Cons<UnivArg>)univargs;
            return new UnivDecls(univargs_);
        }

        UnivArg MkUnivArg(object name)
        {
            var v = (Token)name;
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));
            var u = new UnivArg((Token)name);
            GlobalDecls[v.text] = new UnivDecl(u);
            return u;
        }

        UnivArgWithType MkUnivArgWithType(object name, object t)
        {
            var v = (Token)name;
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));
            var u = new UnivArgWithType((Token)name, (Token)t);
            GlobalDecls[v.text] = new UnivDecl(u);
            return u;
        }

        UnivArgWithSucc MkUnivArgWithSucc(object name, object succ)
        {
            var v = (Token)name;
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));

            Token i = (Token)succ;

            if (!System.Text.RegularExpressions.Regex.IsMatch(i.text, "^(0|1)+$"))
                throw new MonaParseException(MonaParseExceptionKind.InvalidUniverseDeclaration, i.Location, string.Format("'{0}' is out of range, must match ^(0|1)+$", i.text));

            var u = new UnivArgWithSucc((Token)name, i.text);
            GlobalDecls[v.text] = new UnivDecl(u);
            return u;
        }

        AssertDecl MkAssertDecl(object formula)
        {
            return new AssertDecl((Expr)formula);
        }

        ExecuteDecl MkExecuteDecl(object formula)
        {
            return new ExecuteDecl((Expr)formula);
        }

        ConstDecl MkConstDecl(object name, object def)
        {
            var v = (Token)name;
            var t = (Expr)def;
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));

            var d = new ConstDecl(v, t);
            GlobalDecls[v.text] = d;
            return d;
        }

        Name MkConstRef(object name)
        {
            var v = (Token)name;
            if (!GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.UndeclaredConstant, v.Location, string.Format("constant '{0}' is undeclared", v.text));

            if (GlobalDecls.ContainsKey(v.text) && GlobalDecls[v.text].kind != DeclKind.constant)
                throw new MonaParseException(MonaParseExceptionKind.IdentifierIsNotDeclaredConstant, v.Location, string.Format("'{0}' is not a constant", v.text));

            return new Name(v, ExprType.INT);
        }

        bool macro_or_pred_decl_occurred = false;

        bool MkDefaultWhere1DeclDone = false;
        DefaultWhereDecl MkDefaultWhere1Decl(object param, object formula)
        {
            Token t = (Token)param;
            if (MkDefaultWhere1DeclDone)
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, t.Location, "defaultwhere1 can occur at most once");
            if (macro_or_pred_decl_occurred)
                throw new MonaParseException(MonaParseExceptionKind.UnexpectedDeclaration, t.Location, "defaultwhere1 must occur before all predicate and macro definitions");
            MkDefaultWhere1DeclDone = true;
            return new DefaultWhereDecl(false, t, (Expr)formula);
        }

        bool MkDefaultWhere2DeclDone = false;
        DefaultWhereDecl MkDefaultWhere2Decl(object param, object formula)
        {
            Token t = (Token)param;
            if (MkDefaultWhere2DeclDone)
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, t.Location, "defaultwhere2 can occur at most once");
            if (macro_or_pred_decl_occurred)
                throw new MonaParseException(MonaParseExceptionKind.UnexpectedDeclaration, t.Location, "defaultwhere2 must occur before all predicate and macro definitions");
            MkDefaultWhere2DeclDone = true;
            return new DefaultWhereDecl(true, t, (Expr)formula);
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
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));
            var params_ = parameters as Cons<Param>;
            var pmap = new Dictionary<string,Param>();
            if (params_ != null && !params_.IsEmpty)
            {
                foreach (var p in params_)
                {
                    if (pmap.ContainsKey(p.Token.text))
                        throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, p.Token.Location, string.Format("duplicate parameter name '{0}'", p.Token.text));
                    pmap[p.Token.text] = p;
                }
            }
            var P = new PredDecl(v, params_, pmap, (Expr)formula, isMacro);
            GlobalDecls[v.text] = P;
            macro_or_pred_decl_occurred = true;
            return P; 
        }

        Var0Param MkVar0Param(object name)
        {
            return new Var0Param((Token)name);
        }

        VarParam MkVar1Param(object varwhere)
        {
            return new VarParam((VarWhere)varwhere, ParamKind.var1);
        }

        VarParam MkVar2Param(object varwhere)
        {
            return new VarParam((VarWhere)varwhere, ParamKind.var2);
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

        bool MkAllposDeclDone = false;
        AllposDecl MkAllposDecl(object name)
        {
            Token v = (Token)name;
            if (MkAllposDeclDone)
                throw new MonaParseException(MonaParseExceptionKind.DuplicateAllpos, v.Location, v.text);
            Decl d;
            if (!GlobalDecls.TryGetValue(v.text, out d))
                throw new MonaParseException(MonaParseExceptionKind.UndeclaredIdentifier, v.Location, v.text);
            if (d.kind != DeclKind.var2)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, v.Location, 
                    string.Format("allpos parameter must be a secondorder variable"));
            MkAllposDeclDone = true;
            return new AllposDecl(v);
        }

        Let MkLet(object letkind, object lets, object formula)
        {
            Token letkind_ = (Token)letkind;
            Cons<Tuple<Token, Expr>> lets_ = (Cons<Tuple<Token, Expr>>)lets;
            Expr formula_ = (Expr)formula;
            Dictionary<string, Param> letmap = new Dictionary<string, Param>();
            Func<Token, Param> MkParam = x => 
                {
                    if (letkind_.Kind == Tokens.LET0)
                        return new Var0Param(x);
                    else if (letkind_.Kind == Tokens.LET1)
                        return new VarParam(new VarWhere(x,null), ParamKind.var1);
                    else
                        return new VarParam(new VarWhere(x,null), ParamKind.var2);
                };
            foreach (var let in lets_)
            {
                if (letmap.ContainsKey(let.Item1.text))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, let.Item1.Location, let.Item1.text);
                letmap[let.Item1.text] = MkParam(let.Item1);
            }
            return new Let(letkind_, lets_, formula_, letmap);
        }
    }
}

