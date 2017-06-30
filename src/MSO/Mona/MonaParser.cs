using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Automata.Utilities;
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

        Dictionary<string,MonaDecl> GlobalDecls = new Dictionary<string,MonaDecl>();


        /// <summary>
        /// Parses a Mona program from a given file.
        /// </summary>
        public static MonaProgram ParseFromFile(string filename)
        {
            Stream stream = null; 
            MonaProgram pgm = null;
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
        public static MonaProgram Parse(string text)
        {
            MemoryStream mstr = null;
            MonaProgram pgm = null;
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
        public static MonaProgram Parse(Stream stream, string filename = null)
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
        MonaProgram program = null;
        MonaProgram MkProgram(object decls, object header = null)
        {
            var pgm = new MonaProgram(header as Token, (Cons<MonaDecl>)decls, GlobalDecls, vars0, vars1, vars2, varsT);
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
        MonaExpr MkBooleanFormula(object token, object arg1, object arg2)
        {
            return new MonaBinaryBooleanFormula((Token)token, arg1 as MonaExpr, arg2 as MonaExpr);
        }

        MonaExpr MkNegatedFormula(object token, object arg1)
        {
            return new MonaNegatedFormula((Token)token, arg1 as MonaExpr);
        }

        MonaBooleanConstant MkBooleanConstant(object token)
        {
            return new MonaBooleanConstant((Token)token);
        }

        MonaBinaryAtom MkAtom2(object token, object term1, object term2)
        {
            return new MonaBinaryAtom((Token)token, (MonaExpr)term1, (MonaExpr)term2);
        }

        MonaPredApp MkPredApp(object token, object exprs)
        {
            return new MonaPredApp((Token)token, (Cons<MonaExpr>)exprs);
        }

        MonaQBFormula MkQ0Formula(object quantifier, object vars, object formula)
        {
            var vars_ = (Cons<Token>)vars;
            Dictionary<string, MonaParam> varmap = new Dictionary<string, MonaParam>();
            foreach (var v in vars_)
            {
                if (varmap.ContainsKey(v.text))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location);

                varmap[v.text] = new MonaVar0Param(v);
            }
            return new MonaQBFormula((Token)quantifier, vars_, (MonaExpr)formula, varmap);
        }

        MonaQFormula MkQFormula(object quantifier, object vars, object formula, object univs)
        {
            var vws = (Cons<MonaVarWhere>)vars;
            var Q = (Token)quantifier;
            Dictionary<string, MonaParam> varmap = new Dictionary<string, MonaParam>();
            if (Q.Kind == Tokens.EX1 || Q.Kind == Tokens.ALL1)
            {
                foreach (var vw in vws)
                {
                    if (varmap.ContainsKey(vw.name))
                        throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location);

                    varmap[vw.name] = new MonaVarParam(vw, MonaParamKind.var1);
                }
            }
            else
            {
                foreach (var vw in vws)
                {
                    if (varmap.ContainsKey(vw.name))
                        throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location);

                    varmap[vw.name] = new MonaVarParam(vw, MonaParamKind.var2);
                }
            }
            return new MonaQFormula(Q, (Cons<MonaVarWhere>)vars, (MonaExpr)formula, null, varmap);
        }

        #endregion

        MonaVarWhere MkVarWhere(object name, object where)
        {
            return new MonaVarWhere((Token)name, (MonaExpr)where);
        }

        MonaVarWhere MkVarWhere(object name)
        {
            return new MonaVarWhere((Token)name, null);
        }

        MonaArithmFuncApp MkArithmFuncApp(object func, object arg1, object arg2)
        {
            return new MonaArithmFuncApp((Token)func, (MonaExpr)arg1, (MonaExpr)arg2);
        }

        MonaInt MkInt(object integer)
        {
            return new MonaInt((Token)integer);
        }

        MonaName MkName(object name)
        {
            return new MonaName((Token)name, MonaExprType.UNKNOWN);
        }

        List<MonaVarDecl> vars1 = new List<MonaVarDecl>();
        List<MonaVarDecl> vars2 = new List<MonaVarDecl>();
        List<MonaVarDecl> varsT = new List<MonaVarDecl>();
        List<MonaVar0Decl> vars0 = new List<MonaVar0Decl>();


        MonaVarDecls MkVar1Decl(object vars, object univs)
        {
            var vws = (Cons<MonaVarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var vw in vws)
                if (GlobalDecls.ContainsKey(vw.name))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location, string.Format("name '{0}' is already declared", vw.name));
                else
                {
                    var vd = new MonaVarDecl(MonaDeclKind.var1, univs_, vw);
                    GlobalDecls[vw.name] = vd;
                    vars1.Add(vd);
                }
            return  new MonaVarDecls(MonaDeclKind.var1, univs_, vws);
        }

        MonaVarDecls MkVar2Decl(object vars, object univs)
        {
            var vars_ = (Cons<MonaVarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var vw in vars_)
                if (GlobalDecls.ContainsKey(vw.name))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location, string.Format("name '{0}' is already declared", vw.name));
                else
                {
                    var vd = new MonaVarDecl(MonaDeclKind.var2, univs_, vw);
                    GlobalDecls[vw.name] = vd;
                    vars2.Add(vd);
                }
            return new MonaVarDecls(MonaDeclKind.var2, univs_, vars_);
        }

        MonaVarDecls MkTreeDecl(object vars, object univs)
        {
            var vars_ = (Cons<MonaVarWhere>)vars;
            var univs_ = (univs == null ? null : (Cons<Token>)univs);
            foreach (var vw in vars_)
                if (GlobalDecls.ContainsKey(vw.name))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, vw.token.Location, string.Format("name '{0}' is already declared", vw.name));
                else
                {
                    var vd = new MonaVarDecl(MonaDeclKind.tree, univs_, vw);
                    GlobalDecls[vw.name] = vd;
                    varsT.Add(vd);
                }
            return new MonaVarDecls(MonaDeclKind.tree, univs_, vars_);
        }

        MonaVar0Decls MkVar0Decl(object names)
        {
            var names_ = (Cons<Token>)names;
            foreach (var v in names_)
                if (GlobalDecls.ContainsKey(v.text))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));
                else
                {
                    var vd = new MonaVar0Decl(v);
                    GlobalDecls[v.text] = new MonaVar0Decl(v);
                    vars0.Add(vd);
                }
            return new MonaVar0Decls(names_);
        }

        MonaUnivDecls MkUnivDecl(object univargs)
        {
            var univargs_ = (Cons<MonaUnivArg>)univargs;
            return new MonaUnivDecls(univargs_);
        }

        MonaUnivArg MkUnivArg(object name)
        {
            var v = (Token)name;
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));
            var u = new MonaUnivArg((Token)name);
            GlobalDecls[v.text] = new MonaUnivDecl(u);
            return u;
        }

        MonaUnivArgWithType MkUnivArgWithType(object name, object t)
        {
            var v = (Token)name;
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));
            var u = new MonaUnivArgWithType((Token)name, (Token)t);
            GlobalDecls[v.text] = new MonaUnivDecl(u);
            return u;
        }

        MonaUnivArgWithSucc MkUnivArgWithSucc(object name, object succ)
        {
            var v = (Token)name;
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));

            Token i = (Token)succ;

            if (!System.Text.RegularExpressions.Regex.IsMatch(i.text, "^(0|1)+$"))
                throw new MonaParseException(MonaParseExceptionKind.InvalidUniverseDeclaration, i.Location, string.Format("'{0}' is out of range, must match ^(0|1)+$", i.text));

            var u = new MonaUnivArgWithSucc((Token)name, i.text);
            GlobalDecls[v.text] = new MonaUnivDecl(u);
            return u;
        }

        MonaAssertDecl MkAssertDecl(object formula)
        {
            return new MonaAssertDecl((MonaExpr)formula);
        }

        MonaExecuteDecl MkExecuteDecl(object formula)
        {
            return new MonaExecuteDecl((MonaExpr)formula);
        }

        MonaConstDecl MkConstDecl(object name, object def)
        {
            var v = (Token)name;
            var t = (MonaExpr)def;
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));

            var d = new MonaConstDecl(v, t);
            GlobalDecls[v.text] = d;
            return d;
        }

        MonaName MkConstRef(object name)
        {
            var v = (Token)name;
            if (!GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.UndeclaredConstant, v.Location, string.Format("constant '{0}' is undeclared", v.text));

            if (GlobalDecls.ContainsKey(v.text) && GlobalDecls[v.text].kind != MonaDeclKind.constant)
                throw new MonaParseException(MonaParseExceptionKind.IdentifierIsNotDeclaredConstant, v.Location, string.Format("'{0}' is not a constant", v.text));

            return new MonaName(v, MonaExprType.INT);
        }

        bool macro_or_pred_decl_occurred = false;

        bool MkDefaultWhere1DeclDone = false;
        MonaDefaultWhereDecl MkDefaultWhere1Decl(object param, object formula)
        {
            Token t = (Token)param;
            if (MkDefaultWhere1DeclDone)
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, t.Location, "defaultwhere1 can occur at most once");
            if (macro_or_pred_decl_occurred)
                throw new MonaParseException(MonaParseExceptionKind.UnexpectedDeclaration, t.Location, "defaultwhere1 must occur before all predicate and macro definitions");
            MkDefaultWhere1DeclDone = true;
            return new MonaDefaultWhereDecl(false, t, (MonaExpr)formula);
        }

        bool MkDefaultWhere2DeclDone = false;
        MonaDefaultWhereDecl MkDefaultWhere2Decl(object param, object formula)
        {
            Token t = (Token)param;
            if (MkDefaultWhere2DeclDone)
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, t.Location, "defaultwhere2 can occur at most once");
            if (macro_or_pred_decl_occurred)
                throw new MonaParseException(MonaParseExceptionKind.UnexpectedDeclaration, t.Location, "defaultwhere2 must occur before all predicate and macro definitions");
            MkDefaultWhere2DeclDone = true;
            return new MonaDefaultWhereDecl(true, t, (MonaExpr)formula);
        }

        MonaFormulaDecl MkFormulaDecl(object formula)
        {
            return new MonaFormulaDecl((MonaExpr)formula);
        }

        MonaExpr MkIsEmpty(object token, object term)
        {
            return new MonaIsEmpty((Token)token, (MonaExpr)term);
        }

        MonaMinOrMax MkMinOrMax(object token, object term)
        {
            return new MonaMinOrMax((Token)token, (MonaExpr)term);
        }

        MonaRestrict MkRestrict(object token, object formula)
        {
            return new MonaRestrict((Token)token, (MonaExpr)formula);
        }

        MonaPredDecl MkPredDecl(object name, object parameters, object formula, bool isMacro = false)
        {
            var v = (Token)name;
            if (GlobalDecls.ContainsKey(v.text))
                throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, v.Location, string.Format("name '{0}' is already declared", v.text));
            var params_ = parameters as Cons<MonaParam>;
            var pmap = new Dictionary<string,MonaParam>();
            if (params_ != null && !params_.IsEmpty)
            {
                foreach (var p in params_)
                {
                    if (pmap.ContainsKey(p.Token.text))
                        throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, p.Token.Location, string.Format("duplicate parameter name '{0}'", p.Token.text));
                    pmap[p.Token.text] = p;
                }
            }
            var P = new MonaPredDecl(v, params_, pmap, (MonaExpr)formula, isMacro);
            GlobalDecls[v.text] = P;
            macro_or_pred_decl_occurred = true;
            return P; 
        }

        MonaVar0Param MkVar0Param(object name)
        {
            return new MonaVar0Param((Token)name);
        }

        MonaVarParam MkVar1Param(object varwhere)
        {
            return new MonaVarParam((MonaVarWhere)varwhere, MonaParamKind.var1);
        }

        MonaVarParam MkVar2Param(object varwhere)
        {
            return new MonaVarParam((MonaVarWhere)varwhere, MonaParamKind.var2);
        }

        MonaUniverseParam MkUniverseParam(object universename)
        {
            return new MonaUniverseParam((Token)universename);
        }

        MonaSet MkSet(object empty, object elems = null)
        {
            return new MonaSet((Token)empty, (elems == null ? Cons<MonaExpr>.Empty : (Cons<MonaExpr>)elems));
        }

        MonaSetOp MkSetOp(object op, object arg1, object arg2)
        {
            return new MonaSetOp((Token)op, (MonaExpr)arg1, (MonaExpr)arg2);
        }

        MonaPconst MkPconst(object pconstToken, object i)
        {
            return new MonaPconst((Token)pconstToken, (MonaExpr)i);
        }

        MonaRange MkRange(object rangeToken, object from, object to)
        {
            return new MonaRange((Token)rangeToken, (MonaExpr)from, (MonaExpr)to);
        }

        bool MkAllposDeclDone = false;
        MonaAllposDecl MkAllposDecl(object name)
        {
            Token v = (Token)name;
            if (MkAllposDeclDone)
                throw new MonaParseException(MonaParseExceptionKind.DuplicateAllpos, v.Location, v.text);
            MonaDecl d;
            if (!GlobalDecls.TryGetValue(v.text, out d))
                throw new MonaParseException(MonaParseExceptionKind.UndeclaredIdentifier, v.Location, v.text);
            if (d.kind != MonaDeclKind.var2)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, v.Location, 
                    string.Format("allpos parameter must be a secondorder variable"));
            MkAllposDeclDone = true;
            return new MonaAllposDecl(v);
        }

        MonaLet MkLet(object letkind, object lets, object formula)
        {
            Token letkind_ = (Token)letkind;
            Cons<Tuple<Token, MonaExpr>> lets_ = (Cons<Tuple<Token, MonaExpr>>)lets;
            MonaExpr formula_ = (MonaExpr)formula;
            Dictionary<string, MonaParam> letmap = new Dictionary<string, MonaParam>();
            Func<Token, MonaParam> MkParam = x => 
                {
                    if (letkind_.Kind == Tokens.LET0)
                        return new MonaVar0Param(x);
                    else if (letkind_.Kind == Tokens.LET1)
                        return new MonaVarParam(new MonaVarWhere(x,null), MonaParamKind.var1);
                    else
                        return new MonaVarParam(new MonaVarWhere(x,null), MonaParamKind.var2);
                };
            foreach (var let in lets_)
            {
                if (letmap.ContainsKey(let.Item1.text))
                    throw new MonaParseException(MonaParseExceptionKind.DuplicateDeclaration, let.Item1.Location, let.Item1.text);
                letmap[let.Item1.text] = MkParam(let.Item1);
            }
            return new MonaLet(letkind_, lets_, formula_, letmap);
        }
    }
}

