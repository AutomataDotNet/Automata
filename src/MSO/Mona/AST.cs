using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using QUT.Gppg;

namespace Microsoft.Automata.MSO.Mona
{
    public class Token
    {
        public readonly string text;

        /// <summary>
        /// The line at which the text span starts.
        /// </summary>
        public int StartLine { get { return location.StartLine; } }

        /// <summary>
        /// The column at which the text span starts.
        /// </summary>
        public int StartColumn { get { return location.StartColumn; } }

        /// <summary>
        /// The line on which the text span ends.
        /// </summary>
        public int EndLine { get { return location.EndLine; } }

        /// <summary>
        /// The column of the first character
        /// beyond the end of the text span.
        /// </summary>
        public int EndColumn { get { return location.EndColumn; } }

        /// <summary>
        /// Source file name of the location or null.
        /// </summary>
        public string File { get { return location.File; } }

        LexLocationInFile location;
        internal LexLocationInFile Location
        {
            get
            {
                return location;
            }
        }

        Tokens kind;
        public Tokens Kind
        {
            get { return kind; }
        }

        internal Token(string text, LexLocationInFile loc, Tokens kind)
        {
            this.text = text;
            this.location = loc;
            this.kind = kind;
        }

        public override string ToString()
        {
            return text;
        }

        public bool TryGetInt(out int k)
        {
            if (text.StartsWith("0x"))
                return int.TryParse(text.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out k);
            else
                return int.TryParse(text, out k);
        }

        public int ToInt()
        {
            int k = 0;
            if (TryGetInt(out k))
                return k;
            else
                throw new MonaParseException(MonaParseExceptionKind.InvalidIntegerFormat, location, string.Format("cannot convert token '{0}' to integer", text));
        }
    }

    public abstract class MonaAst 
    {
        public abstract void Print(StringBuilder sb);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(sb);
            return sb.ToString();
        }
    }

    public enum MonaHeader { WS1S, WS2S, M2LSTR, M2LTREE, NOTHING } 

    public partial class MonaProgram : MonaAst
    {
        public Token token;
        public Cons<MonaDecl> declarations;
        public MonaAllposDecl allpos = null;
        public MonaDefaultWhereDecl defaultwhere1 = null;
        public MonaDefaultWhereDecl defaultwhere2 = null;

        public List<MonaVarDecl> vars1 = new List<MonaVarDecl>();
        public List<MonaVarDecl> vars2 = new List<MonaVarDecl>();
        public List<MonaVarDecl> varsT = new List<MonaVarDecl>();
        public List<MonaVar0Decl> vars0 = new List<MonaVar0Decl>();

        Dictionary<string, MonaDecl> globals;


        public string Description
        {
            get
            {
                return ToString();
            }
        }

        public MonaHeader header
        {
            get
            {
                if (token == null)
                    return MonaHeader.NOTHING;
                else 
                {
                    switch (token.Kind)
                    {
                        case Tokens.WS1S: return MonaHeader.WS1S;
                        case Tokens.WS2S: return MonaHeader.WS2S;
                        case Tokens.M2LSTR: return MonaHeader.M2LSTR;
                        default: return MonaHeader.M2LTREE;
                    }
                }
            }
        }

        internal MonaProgram(Token token, Cons<MonaDecl> declarations, Dictionary<string, MonaDecl> globals,         
             List<MonaVar0Decl> vars0, List<MonaVarDecl> vars1, List<MonaVarDecl> vars2, List<MonaVarDecl> varsT)
        {
            this.token = token;
            this.declarations = declarations;
            this.globals = globals;
            this.vars0 = vars0;
            this.vars1 = vars1;
            this.vars2 = vars2;
            this.varsT = varsT;
        }

        public void Typecheck()
        {
            var glob = new Dictionary<string, MonaDecl>();
            var loc = MapStack<string, MonaParam>.Empty;
            var decls = declarations;
            while (!decls.IsEmpty)
            {
                var decl = decls.First;
                decls = decls.Rest;
                decl.TypeCheck(glob, loc);
                switch (decl.kind)
                {
                    case MonaDeclKind.macro:
                    case MonaDeclKind.pred:
                        glob[((MonaPredDecl)decl).name.text] = decl;
                        break;
                    case MonaDeclKind.constant:
                        glob[((MonaConstDecl)decl).name.text] = decl;
                        break;
                    case MonaDeclKind.var0:
                        foreach (var v in ((MonaVar0Decls)decl).vars)
                            glob[v.text] = new MonaVar0Decl(v);
                        break;
                    case MonaDeclKind.var1:
                    case MonaDeclKind.var2:
                        foreach (var v in ((MonaVarDecls)decl).vars)
                            glob[v.name] = new MonaVarDecl(((MonaVarDecls)decl).kind, ((MonaVarDecls)decl).univs, v);
                        break;
                    case MonaDeclKind.universe:
                        foreach (var v in ((MonaUnivDecls)decl).args)
                            glob[v.name] = new MonaUnivDecl(v);
                        break;
                    case MonaDeclKind.allpos:
                        allpos = (MonaAllposDecl)decl;
                        break;
                    case MonaDeclKind.defaultwhere1:
                        defaultwhere1 = (MonaDefaultWhereDecl)decl;
                        break;
                    case MonaDeclKind.defaultwhere2:
                        defaultwhere2 = (MonaDefaultWhereDecl)decl;
                        break;
                    default:
                        break;
                }
            }
        }

        public override void Print(StringBuilder sb)
        {
            if (token != null)
                sb.AppendLine(MonaParser.DescribeTokens(token.Kind) + ";");
            foreach (var decl in declarations)
            {
                decl.Print(sb);
                sb.AppendLine(";");
            }
        }
    }

    public enum MonaExprType
    {
        BOOL, SET, INT, UNKNOWN,
        RANGE
    }

    public abstract class MonaExpr : MonaAst
    {
        public MonaExprType type;
        Token token;
        /// <summary>
        /// gets the function symbol, relation symbol, variable, or quantifier
        /// </summary>
        public Token symbol
        {
            get
            {
                return token;
            }
        }
        protected MonaExpr[] subexprs;

        internal MonaExpr(Token token, MonaExprType type, MonaExpr[] subexprs)
        {
            this.token = token;
            this.type = type;
            this.subexprs = subexprs;
        }

        public MonaExpr this[int i]
        {
            get { return subexprs[i]; }
        }

        public int NrOfSubexprs
        {
            get
            {
                if (subexprs == null)
                    return 0;
                else
                    return subexprs.Length;
            }
        }

        internal abstract void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals);

        public int ToInt(Dictionary<string,MonaDecl> globals)
        {
            switch (token.Kind)
            {
                case Tokens.NUMBER:
                    return ((MonaInt)this).Value;
                case Tokens.CONST:
                    {
                        MonaConstDecl cdecl = (MonaConstDecl)globals[token.text];
                        return cdecl.def.ToInt(globals);
                    }
                case Tokens.PLUS:
                    return subexprs[0].ToInt(globals) + subexprs[1].ToInt(globals);
                case Tokens.MINUS:
                    return Math.Max(0, subexprs[0].ToInt(globals) - subexprs[1].ToInt(globals));
                case Tokens.TIMES:
                    return subexprs[0].ToInt(globals) * subexprs[1].ToInt(globals);
                case Tokens.DIV:
                    return subexprs[0].ToInt(globals) / subexprs[1].ToInt(globals);
                case Tokens.MOD:
                    return subexprs[0].ToInt(globals) % subexprs[1].ToInt(globals);
                default:
                    throw new MonaParseException(MonaParseExceptionKind.UnexpectedToken, token.Location, string.Format("conversion to constant not possible or not supported for: {0}", this));
            }
        }
    }

    #region formulas

    public enum MonaBinaryBooleanOp { AND, OR, IMPLIES, EQUIV }

    public class MonaBinaryBooleanFormula : MonaExpr
    {
        public MonaBinaryBooleanOp op
        {
            get
            {
                switch (symbol.Kind)
                {
                    case Tokens.AND: return MonaBinaryBooleanOp.AND;
                    case Tokens.OR: return MonaBinaryBooleanOp.OR;
                    case Tokens.IMPLIES: return MonaBinaryBooleanOp.IMPLIES;
                    default: return MonaBinaryBooleanOp.EQUIV;                }
            }
        }
        public MonaExpr arg1
        {
            get { return subexprs[0]; }
        }
        public MonaExpr arg2
        {
            get { return subexprs[1]; }
        }
        internal MonaBinaryBooleanFormula(Token token, MonaExpr formula1, MonaExpr formula2)
            : base(token, MonaExprType.BOOL, new MonaExpr[]{formula1, formula2})
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            arg1.Print(sb);
            sb.Append(" ");
            sb.Append(symbol.text);
            sb.Append(" ");
            arg2.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            arg1.TypeCheck(globals, locals);
            arg2.TypeCheck(globals, locals);
            if (arg1.type != MonaExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg1.symbol.Location, string.Format("expecting BOOL not {0}", arg1.type.ToString()));
            if (arg2.type != MonaExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg2.symbol.Location, string.Format("expecting BOOL not {0}", arg2.type.ToString()));
            return;
        }
    }

    public class MonaRestrict : MonaExpr
    {
        public MonaExpr formula
        {
            get
            {
                return subexprs[0];
            }
        }
        internal MonaRestrict(Token token, MonaExpr formula)
            : base(token, MonaExprType.BOOL, new MonaExpr[]{formula})
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("restrict(");
            formula.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            formula.TypeCheck(globals, locals);
            if (formula.type != MonaExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, formula.symbol.Location, string.Format("expecting BOOL not {0}", formula.type.ToString()));

        }
    }

    public class MonaNegatedFormula : MonaExpr
    {
        public MonaExpr formula
        {
            get { return subexprs[0]; }
        }
        internal MonaNegatedFormula(Token token, MonaExpr formula)
            : base(token, MonaExprType.BOOL, new MonaExpr[]{formula})
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("~");
            formula.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            formula.TypeCheck(globals, locals);
            if (formula.type != MonaExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, formula.symbol.Location, string.Format("expecting BOOL not {0}", formula.type.ToString()));
        }
    }

    public class MonaBooleanConstant : MonaExpr
    {
        public bool isTrue
        {
            get { return symbol.Kind == Tokens.TRUE; }
        }

        internal MonaBooleanConstant(Token token)
            : base(token, MonaExprType.BOOL, null)
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(symbol.text);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            return;
        }
    }

    public class MonaBinaryAtom : MonaExpr
    {
        public MonaExpr term1
        {
            get { return subexprs[0]; }
        }
        public MonaExpr term2
        {
            get { return subexprs[1]; }
        }
        internal MonaBinaryAtom(Token token, MonaExpr term1, MonaExpr term2)
            : base(token, MonaExprType.BOOL, new MonaExpr[]{term1, term2})
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            term1.Print(sb);
            sb.Append(" ");
            sb.Append(symbol.text);
            sb.Append(" ");
            term2.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            term1.TypeCheck(globals, locals);
            term2.TypeCheck(globals, locals);
            switch (symbol.Kind)
            {
                case Tokens.GE:
                case Tokens.LE:
                case Tokens.LT:
                case Tokens.GT:
                    {
                        if (term1.type != MonaExprType.INT)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term1.symbol.Location, string.Format("expecting INT not {0}", term1.type.ToString()));
                        if (term2.type != MonaExprType.INT)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term2.symbol.Location, string.Format("expecting INT not {0}", term2.type.ToString()));
                        return;
                    }
                case Tokens.EQ:
                case Tokens.NE:
                    {
                        if (term1.type != term2.type)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term2.symbol.Location, string.Format("expecting {0} not {1}", term1.type.ToString(), term2.type.ToString()));
                        return;
                    }
                case Tokens.IN:
                case Tokens.NOTIN:
                    {
                        if (term1.type != MonaExprType.INT)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term1.symbol.Location, string.Format("expecting INT not {0}", term1.type.ToString()));
                        if (term2.type != MonaExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term2.symbol.Location, string.Format("expecting SET not {0}", term2.type.ToString()));
                        return;
                    }
                case Tokens.SUBSET:
                    {
                        if (term1.type != MonaExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term1.symbol.Location, string.Format("expecting SET not {0}", term1.type.ToString()));
                        if (term2.type != MonaExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term2.symbol.Location, string.Format("expecting SET not {0}", term2.type.ToString()));
                        return;
                    }
                default:
                    throw new MonaParseException(MonaParseExceptionKind.InternalError, symbol.Location, string.Format("unexpected token '{0}'", symbol.text));

        }
        }
    }

    public class MonaPredApp : MonaExpr
    {
        public Cons<MonaExpr> expressions;
        internal MonaPredApp(Token token, Cons<MonaExpr> expressions)
            : base(token, MonaExprType.BOOL, expressions.ToArray())
        {
            this.expressions = expressions;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(symbol.text);
            sb.Append("(");
            sb.Append(expressions.ToString(","));
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            if (!globals.ContainsKey(symbol.text))
                throw new MonaParseException(MonaParseExceptionKind.UndeclaredPredicate, symbol.Location, symbol.text);

            MonaDecl decl = globals[symbol.text];
            MonaPredDecl pdecl = decl as MonaPredDecl;
            if (pdecl == null)
            {
                throw new MonaParseException(MonaParseExceptionKind.InvalidUseOfName, symbol.Location, symbol.text);
            }
            if (pdecl.parameters.Count != expressions.Count)
            {
                throw new MonaParseException(MonaParseExceptionKind.InvalidNrOfParameters, symbol.Location, symbol.text);
            }
            for (int i = 0; i < expressions.Count; i++)
            {
                expressions[i].TypeCheck(globals, locals);
                if (pdecl.parameters[i].type != expressions[i].type)
                    throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, expressions[i].symbol.Location,
                        string.Format("parameter {0} of {1} must be of type {2} not {3}", pdecl.parameters[i].Token.text, pdecl.name.text, pdecl.parameters[i].type, expressions[i].type));
            }
        }
    }

    public class MonaQBFormula : MonaExpr
    {
        Dictionary<string, MonaParam> varmap;
        public Cons<Token> vars;
        public MonaExpr formula
        {
            get { return subexprs[0]; }
        }
        internal MonaQBFormula(Token quantifier, Cons<Token> vars, MonaExpr formula, Dictionary<string, MonaParam> varmap) :
            base(quantifier, MonaExprType.BOOL, new MonaExpr[]{formula})
        {
            this.varmap = varmap;
            this.vars = vars;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(symbol.text);
            sb.Append(" ");
            sb.Append(vars.ToString(","));
            sb.Append(":");
            formula.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            formula.TypeCheck(globals, locals.Push(varmap));
            if (formula.type != MonaExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, formula.symbol.Location, string.Format("expecting BOOL not {0}", formula.type.ToString()));
        }
    }

    public class MonaQFormula : MonaExpr
    {
        public Dictionary<string, MonaParam> varmap;
        public Cons<Token> universes;
        public Cons<MonaVarWhere> vars;
        public MonaExpr formula
        {
            get { return subexprs[0]; }
        }
        internal MonaQFormula(Token quantifier, Cons<MonaVarWhere> vars, MonaExpr formula, Cons<Token> universes, Dictionary<string, MonaParam> varmap) :
            base(quantifier, MonaExprType.BOOL, new MonaExpr[] { formula })
        {
            this.varmap = varmap;
            this.universes = universes;
            this.vars = vars;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(symbol.text);
            if (universes != null)
            {
                sb.Append(" [");
                sb.Append(universes.ToString(","));
                sb.Append("]");
            }
            sb.Append(" ");
            sb.Append(vars.ToString(","));
            sb.Append(":");
            formula.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            //TBD: check where-expressions
            var locals1 = locals.Push(varmap);
            formula.TypeCheck(globals, locals1);
            if (formula.type != MonaExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, formula.symbol.Location, string.Format("expecting BOOL not {0}", formula.type.ToString()));
        }
    }

    public class MonaIsEmpty : MonaExpr
    {
        MonaExpr set
        {
            get { return subexprs[0]; }
        }
        public MonaIsEmpty(Token token, MonaExpr term)
            : base(token, MonaExprType.BOOL, new MonaExpr[]{term})
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("empty(");
            set.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            set.TypeCheck(globals, locals);
            if (set.type != MonaExprType.SET)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, set.symbol.Location, string.Format("expecting SET not {0}", set.type.ToString()));

        }
    }

    public enum MonaLetKind { let0, let1, let2 }

    public class MonaLet : MonaExpr
    {
        public Cons<Tuple<Token, MonaExpr>> lets;
        public Dictionary<string, MonaParam> let_vars;
        public MonaExpr formula
        {
            get { return subexprs[0]; }
        }

        public MonaExprType letType
        {
            get
            {
                MonaExprType lettype = (symbol.Kind == Tokens.LET0 ? MonaExprType.BOOL :
                                      (symbol.Kind == Tokens.LET1 ? MonaExprType.INT : MonaExprType.SET));
                return lettype;
            }
        }

        internal MonaLet(Token letkind, Cons<Tuple<Token, MonaExpr>> lets, MonaExpr formula, Dictionary<string, MonaParam> let_vars)
            : base(letkind, MonaExprType.BOOL, new MonaExpr[]{formula})
        {
            this.lets = lets;
            this.let_vars = let_vars;
        }

        public MonaLetKind Kind
        {
            get
            {
                switch (symbol.Kind)
                {
                    case Tokens.LET1: return MonaLetKind.let1;
                    case Tokens.LET2: return MonaLetKind.let2;
                    default: return MonaLetKind.let0;
                }
            }
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            foreach (var let in lets)
            {
                let.Item2.TypeCheck(globals, locals);
                if (let.Item2.type != letType)
                    throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, let.Item2.symbol.Location, let.Item2.symbol.text);
            }
            formula.TypeCheck(globals, locals.Push(let_vars));
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(symbol.text);
            sb.Append(" ");
            sb.Append(lets.ToString(",", PrintLet));
            sb.Append(" in (");
            formula.Print(sb);
            sb.Append(")");
        }

        static string PrintLet(Tuple<Token, MonaExpr> t)
        {
            return string.Format("{0} = ({1})", t.Item1.text, t.Item2.ToString());
        }
    }

    #endregion

    public class MonaName : MonaExpr
    {
        public string name
        {
            get
            {
                return symbol.text;
            }
        }
        internal MonaName(Token token, MonaExprType type)
            : base(token, type, null)
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            var t = GetExprType(globals, locals);
            if (type == MonaExprType.UNKNOWN)
                type = t;
            else if (type != t)
                throw new MonaParseException(MonaParseExceptionKind.InternalError, symbol.Location,
                    string.Format("inconsistent types {0} and {1} for '{2}'", type.ToString(), t.ToString(), name));
        }

        MonaExprType GetExprType(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            var x = symbol;
            MonaParam xp = null;
            MonaDecl xd = null;
            if (locals.TryGetValue(x.text, out xp))
            {
                if (xp.kind == MonaParamKind.universe || xp.kind == MonaParamKind.var2)
                    return MonaExprType.SET;
                else if (xp.kind == MonaParamKind.var1)
                    return MonaExprType.INT;
                else
                    return MonaExprType.BOOL;
            }
            else if (globals.TryGetValue(x.text, out xd))
            {
                if (xd.kind == MonaDeclKind.constant || xd.kind == MonaDeclKind.var1)
                    return MonaExprType.INT;
                else if (xd.kind == MonaDeclKind.universe || xd.kind == MonaDeclKind.var2)
                    return MonaExprType.SET;
                else if (xd.kind == MonaDeclKind.var0 || ((xd.kind == MonaDeclKind.macro || xd.kind == MonaDeclKind.pred)
                                                      && ((MonaPredDecl)xd).IsNullary))
                    return MonaExprType.BOOL;
                else if (xd.kind == MonaDeclKind.macro || xd.kind == MonaDeclKind.pred)
                    throw new MonaParseException(MonaParseExceptionKind.InvalidUseOfPredicateOrMacroName, x.Location);
            }
            throw new MonaParseException(MonaParseExceptionKind.UndeclaredIdentifier, x.Location, x.text);
        }
    }

    #region terms

    public class MonaInt : MonaExpr
    {
        int i;
        internal MonaInt(Token val)
            : base(val, MonaExprType.INT, null)
        {
            if (!val.TryGetInt(out i))
                throw new MonaParseException(MonaParseExceptionKind.InvalidIntegerFormat, val.Location);
        }

        public int Value
        {
            get
            {
                return i;
            }
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(symbol.text);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            return;
        }
    }

    public enum MonaArithmOp { PLUS, MINUS, TIMES, DIV, MOD }

    public class MonaArithmFuncApp : MonaExpr
    {
        public MonaArithmOp op
        {
            get
            {
                switch (symbol.Kind)
                {
                    case Tokens.PLUS: return MonaArithmOp.PLUS;
                    case Tokens.MINUS: return MonaArithmOp.MINUS;
                    case Tokens.TIMES: return MonaArithmOp.TIMES;
                    case Tokens.DIV: return MonaArithmOp.DIV;
                    case Tokens.MOD: return MonaArithmOp.MOD;
                    default:
                        throw new MonaParseException(MonaParseExceptionKind.UnknownArithmeticOperator, symbol.Location, string.Format("unknown operator '{0}'", symbol.text));
                }
            }
        }
        public MonaExpr arg1
        {
            get { return subexprs[0]; }
        }
        public MonaExpr arg2
        {
            get { return subexprs[1]; }
        }
        internal MonaArithmFuncApp(Token func, MonaExpr arg1, MonaExpr arg2)
            : base(func, MonaExprType.INT, new MonaExpr[]{arg1, arg2})
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            arg1.Print(sb);
            sb.Append(" ");
            sb.Append(symbol.text);
            sb.Append(" ");
            arg2.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            arg1.TypeCheck(globals, locals);
            arg2.TypeCheck(globals, locals);
            if (arg1.type != MonaExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg1.symbol.Location, string.Format("expecting INT not {0}", arg1.type.ToString()));
            if (arg2.type != MonaExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg2.symbol.Location, string.Format("expecting INT not {0}", arg2.type.ToString()));
        }

    }

    public class MonaSet : MonaExpr
    {
        public Cons<MonaExpr> elems;
        internal MonaSet(Token set, Cons<MonaExpr> elems)
            : base(set, MonaExprType.SET, elems.ToArray())
        {
            this.elems = elems;
        }

        public override void Print(StringBuilder sb)
        {
            if (elems == null)
                sb.Append(symbol.text);
            else
            {
                sb.Append("{");
                sb.Append(elems.ToString(","));
                sb.Append("}");
            }
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            foreach (var e in elems)
            {
                e.TypeCheck(globals, locals);
            }

        }
    }

    public class MonaMinOrMax : MonaExpr
    {
        public bool isMin
        {
            get { return symbol.Kind == Tokens.MIN; }
        }
        public MonaExpr set
        {
            get { return subexprs[0]; }
        }
        public MonaMinOrMax(Token minormax, MonaExpr set)
            : base(minormax, MonaExprType.INT, new MonaExpr[]{set})
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(symbol.text);
            sb.Append(" ");
            set.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            if (set.type != MonaExprType.SET)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, set.symbol.Location, string.Format("expecting SET not {0}", set.type.ToString()));

        }
    }

    public class MonaPconst : MonaExpr
    {
        public MonaExpr elem
        {
            get { return subexprs[0]; }
        }
        internal MonaPconst(Token pconst, MonaExpr elem)
            : base(pconst, MonaExprType.SET, new MonaExpr[]{elem})
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(symbol.text);
            sb.Append("(");
            elem.Print(sb);
            sb.Append(")");
        }
        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            elem.TypeCheck(globals, locals);
            if (elem.type != MonaExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, elem.symbol.Location, string.Format("expecting INT not {0}", elem.type.ToString()));
        }
    }

    public class MonaRange : MonaExpr
    {
        public MonaExpr from
        {
            get { return subexprs[0]; }
        }
        public MonaExpr to
        {
            get { return subexprs[1]; }
        }
        internal MonaRange(Token dots, MonaExpr from, MonaExpr to)
            : base(dots, MonaExprType.SET, new MonaExpr[]{from, to})
        {
        }

        public override void Print(StringBuilder sb)
        {
            from.Print(sb);
            sb.Append(",...,");
            to.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            from.TypeCheck(globals, locals);
            if (from.type != MonaExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, from.symbol.Location, string.Format("expecting INT not {0}", from.type.ToString()));
            if (to.type != MonaExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, to.symbol.Location, string.Format("expecting INT not {0}", to.type.ToString()));
        }
    }

    public enum MonaSetOpKind { UNION, INTER, SETMINUS, PLUS, MINUS }

    public class MonaSetOp : MonaExpr
    {
        public MonaExpr arg1
        {
            get { return subexprs[0]; }
        }
        public MonaExpr arg2
        {
            get { return subexprs[1]; }
        }

        public MonaSetOpKind kind
        {
            get
            {
                switch (symbol.Kind)
                {
                    case Tokens.UNION: return MonaSetOpKind.UNION;
                    case Tokens.INTER: return MonaSetOpKind.INTER;
                    case Tokens.PLUS: return MonaSetOpKind.PLUS;
                    case Tokens.MINUS: return MonaSetOpKind.MINUS;
                    default: return MonaSetOpKind.SETMINUS;
                }
            }
        }

        internal MonaSetOp(Token op, MonaExpr arg1, MonaExpr arg2)
            : base(op, MonaExprType.SET, new MonaExpr[]{arg1, arg2})
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            arg1.Print(sb);
            sb.Append(" ");
            sb.Append(MonaParser.DescribeTokens(symbol.Kind));
            sb.Append(" ");
            arg2.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> globals, MapStack<string, MonaParam> locals)
        {
            arg1.TypeCheck(globals, locals);
            arg2.TypeCheck(globals, locals);
            switch (kind)
            {
                case MonaSetOpKind.INTER:
                case MonaSetOpKind.SETMINUS:
                case MonaSetOpKind.UNION:
                    {
                        if (arg1.type != MonaExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg1.symbol.Location, string.Format("expecting SET not {0}", arg1.type.ToString()));
                        if (arg2.type != MonaExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg2.symbol.Location, string.Format("expecting SET not {0}", arg2.type.ToString()));
                        return;
                    }
                default:
                    {
                        if (arg1.type != MonaExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg1.symbol.Location, string.Format("expecting SET not {0}", arg1.type.ToString()));
                        if (arg2.type != MonaExprType.INT)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg2.symbol.Location, string.Format("expecting INT not {0}", arg2.type.ToString()));
                        return;
                    }

            }
        }
    }

    #endregion

    #region declarations

    public enum MonaDeclKind
    {
        formula, guide, universe, include, assert, execute, constant, 
        defaultwhere1, defaultwhere2,
        var0, var1, var2, tree, macro, pred, allpos, type
    };

    public abstract class MonaDecl : MonaAst
    {
        public MonaDeclKind kind;
        internal MonaDecl(MonaDeclKind kind)
        {
            this.kind = kind;
        }

        internal abstract void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc);
    }

    public class MonaVarDecls : MonaDecl
    {
        public Cons<Token> univs;
        public Cons<MonaVarWhere> vars;
        internal MonaVarDecls(MonaDeclKind kind, Cons<Token> univs, Cons<MonaVarWhere> vars)
            : base(kind)
        {
            this.univs = univs;
            this.vars = vars;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + (univs == null ? "" : "[" + univs.ToString(",") + "]") + vars.ToString(","));
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            if (univs != null)
                foreach (var u in univs)
                {
                    MonaDecl d;
                    if (!glob.TryGetValue(u.text, out d))
                        throw new MonaParseException(MonaParseExceptionKind.UndeclaredIdentifier, u.Location, u.text);
                    if (d.kind != MonaDeclKind.universe)
                        throw new MonaParseException(MonaParseExceptionKind.InvalidUniverseReference, u.Location, u.text);
                }
            MonaParamKind paramkind = (this.kind == MonaDeclKind.var1 ? MonaParamKind.var1 : MonaParamKind.var2);
            foreach (var vw in vars)
                if (vw.where != null)
                    vw.where.TypeCheck(glob, loc.Push(vw.name, new MonaVarParam(vw, paramkind)));
        }
    }

    public class MonaVarDecl : MonaDecl
    {
        public Cons<Token> univs;
        public MonaVarWhere varwhere;
        internal MonaVarDecl(MonaDeclKind kind, Cons<Token> univs, MonaVarWhere vw)
            : base(kind)
        {
            this.univs = univs;
            this.varwhere = vw;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + (univs == null ? "" : "[" + univs.ToString(",") + "]") + varwhere.ToString());
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            if (univs != null)
                foreach (var u in univs)
                {
                    MonaDecl d;
                    if (!glob.TryGetValue(u.text, out d))
                        throw new MonaParseException(MonaParseExceptionKind.UndeclaredIdentifier, u.Location, u.text);
                    if (d.kind != MonaDeclKind.universe)
                        throw new MonaParseException(MonaParseExceptionKind.InvalidUniverseReference, u.Location, u.text);
                }
            MonaParamKind paramkind = (this.kind == MonaDeclKind.var1 ? MonaParamKind.var1 : MonaParamKind.var2);
            if (varwhere.where != null)
                varwhere.where.TypeCheck(glob, loc.Push(varwhere.name, new MonaVarParam(new MonaVarWhere(varwhere.token, null), paramkind)));
        }
    }

    public class MonaVar0Decls : MonaDecl
    {
        public Cons<Token> vars;
        internal MonaVar0Decls(Cons<Token> vars)
            : base(MonaDeclKind.var0)
        {
            this.vars = vars;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + vars.ToString(","));
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            return;
        }
    }

    public class MonaVar0Decl : MonaDecl
    {
        public Token var;
        internal MonaVar0Decl(Token var)
            : base(MonaDeclKind.var0)
        {
            this.var = var;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + var.text);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            return;
        }
    }

    public class MonaConstDecl : MonaDecl
    {
        public Token name;
        public MonaExpr def;

        internal MonaConstDecl(Token name, MonaExpr def)
            : base(MonaDeclKind.constant)
        {
            this.name = name;
            this.def = def;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("const ");
            sb.Append(name.text);
            sb.Append(" = ");
            def.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            def.TypeCheck(glob, loc);
        }
    }

    public class MonaDefaultWhereDecl : MonaDecl
    {
        public bool isSecondOrder;
        public Token param;
        public MonaExpr formula;

        internal MonaDefaultWhereDecl(bool isSecondOrder, Token param, MonaExpr formula) : 
            base(isSecondOrder ? MonaDeclKind.defaultwhere2 : MonaDeclKind.defaultwhere1)
        {
            this.isSecondOrder = isSecondOrder;
            this.param = param;
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString());
            sb.Append("(");
            sb.Append(param.text);
            sb.Append(") = ");
            formula.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            MonaParamKind paramkind = (isSecondOrder ? MonaParamKind.var2 : MonaParamKind.var1);
            formula.TypeCheck(glob, loc.Push(param.text, new MonaVarParam(new MonaVarWhere(param,null), paramkind)));
        }
    }

    public class MonaFormulaDecl : MonaDecl
    {
        public MonaExpr formula;
        internal MonaFormulaDecl(MonaExpr formula) : base(MonaDeclKind.formula)
        {
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            formula.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            formula.TypeCheck(glob, loc);
        }
    }

    public class MonaAssertDecl : MonaDecl
    {
        public MonaExpr psi;
        internal MonaAssertDecl(MonaExpr psi)
            : base(MonaDeclKind.assert)
        {
            this.psi = psi;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("assert ");
            psi.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            psi.TypeCheck(glob, loc);
        }
    }

    public class MonaExecuteDecl : MonaDecl
    {
        public MonaExpr psi;
        internal MonaExecuteDecl(MonaExpr psi)
            : base(MonaDeclKind.execute)
        {
            this.psi = psi;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("execute ");
            psi.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            psi.TypeCheck(glob, loc);
        }
    }

    public class MonaUnivDecls : MonaDecl
    {
        public Cons<MonaUnivArg> args;
        internal MonaUnivDecls(Cons<MonaUnivArg> args)
            : base(MonaDeclKind.universe)
        {
            this.args = args;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("universe " + args.ToString(","));
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
        }
    }

    public class MonaUnivDecl : MonaDecl
    {
        public MonaUnivArg universe;
        internal MonaUnivDecl(MonaUnivArg universe)
            : base(MonaDeclKind.universe)
        {
            this.universe = universe;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("universe " + universe.ToString());
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
        }
    }

    public class MonaPredDecl : MonaDecl
    {
        public bool isMacro;
        public Token name;
        public Cons<MonaParam> parameters;
        public MonaExpr formula;
        public Dictionary<string, MonaParam> pmap;
        public bool IsNullary
        {
            get
            {
                return parameters == null || parameters.Count == 0;
            }
        }

        internal MonaPredDecl(Token name, Cons<MonaParam> parameters, Dictionary<string,MonaParam> pmap, MonaExpr formula, bool isMacro = false)
            : base(isMacro ? MonaDeclKind.macro : MonaDeclKind.pred)
        {
            this.pmap = (pmap == null ? new Dictionary<string,MonaParam>() : pmap);
            this.isMacro = isMacro;
            this.name = name;
            this.parameters = (parameters == null ? Cons<MonaParam>.Empty : parameters);
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            if (isMacro)
                sb.Append("macro ");
            else
                sb.Append("pred ");
            sb.Append(name.text);
            if (!(parameters == null))
            {
                sb.Append("(");
                sb.Append(parameters.ToString(","));
                sb.Append(")");
            }
            sb.Append(" = ");
            formula.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            formula.TypeCheck(glob, loc.Push(pmap));
        }
    }

    public class MonaAllposDecl : MonaDecl
    {
        Token v;
        internal MonaAllposDecl(Token v)
            : base(MonaDeclKind.allpos)
        {
            this.v = v;
        }

        internal override void TypeCheck(Dictionary<string, MonaDecl> glob, MapStack<string, MonaParam> loc)
        {
            return;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("allpos ");
            sb.Append(v.text);
        }
    }

    public enum MonaParamKind { var0, var1, var2, universe }

    public abstract class MonaParam : MonaAst
    {
        public MonaParamKind kind;
        internal MonaParam(MonaParamKind kind)
        {
            this.kind = kind;
        }
        public MonaExprType type
        {
            get
            {
                if (kind == MonaParamKind.universe || kind ==  MonaParamKind.var2)
                    return MonaExprType.SET;
                else if (kind == MonaParamKind.var1)
                    return MonaExprType.INT;
                else
                    return MonaExprType.BOOL;
            }
        }
        public abstract Token Token { get; }
    }

    public class MonaVar0Param : MonaParam
    {
        public Token name;

        public override void Print(StringBuilder sb)
        {
            sb.Append("var0 ");
            sb.Append(name.text);
        }

        internal MonaVar0Param(Token name) : base(MonaParamKind.var0)
        {
            this.name = name;
        }

        public override Token Token
        {
            get { return name; }
        }
    }

    public class MonaVarParam : MonaParam
    {
        public MonaVarWhere varwhere;

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString());
            sb.Append(" ");
            varwhere.Print(sb);
        }

        internal MonaVarParam(MonaVarWhere name, MonaParamKind kind)
            : base(kind)
        {
            this.varwhere = name;
        }

        public override Token Token
        {
            get { return varwhere.token; }
        }
    }

    public class MonaUniverseParam : MonaParam
    {
        public Token name;

        public override void Print(StringBuilder sb)
        {
            sb.Append("universe ");
            sb.Append(name.text);
        }

        internal MonaUniverseParam(Token name)
            : base(MonaParamKind.universe)
        {
            this.name = name;
        }

        public override Token Token
        {
            get { return name; }
        }
    }

    #endregion

    public class MonaVarWhere : MonaAst
    {
        public Token token;
        public string name
        {
            get
            {
                return token.text;
            }
        }
        public MonaExpr where;
        internal MonaVarWhere(Token token, MonaExpr where)
        {
            this.token = token;
            this.where = where;
        }

        public override string ToString()
        {
            if (where == null)
                return name;
            else
                return name + " where " + where.ToString();
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name);
            if (where != null)
            {
                sb.Append(" where ");
                where.Print(sb);
            }
        }
    }

    public class MonaUnivArg : MonaAst
    {
        public Token token;
        public string name 
        {
            get 
            {
                return token.text;
            }
        }
        internal MonaUnivArg(Token token)
        {
            this.token = token;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name);
        }
    }

    public class MonaUnivArgWithType : MonaUnivArg
    {
        public Token E;

        public string type
        {
            get {
                return E.text;
            }
        }

        internal MonaUnivArgWithType(Token token, Token E)
            : base(token)
        {
            this.E = E;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name + ":" + type);
        }
    }

    public class MonaUnivArgWithSucc : MonaUnivArg
    {
        /// <summary>
        /// matches (0|1)+
        /// </summary>
        public string succ;

        internal MonaUnivArgWithSucc(Token token, string succ)
            : base(token)
        {
            this.succ = succ;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name + ":" + succ);
        }
    }
}
