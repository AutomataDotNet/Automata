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
        public string file;

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
        public string File { get { return file; } }

        LexLocation location;
        internal LexLocation Location
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

        internal Token(string text, LexLocation loc, Tokens kind, string file)
        {
            this.text = text;
            this.location = loc;
            this.kind = kind;
            this.file = file;
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

    public abstract class Ast 
    {
        public abstract void Print(StringBuilder sb);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(sb);
            return sb.ToString();
        }
    }

    public enum Header { WS1S, WS2S, M2LSTR, M2LTREE, NOTHING } 

    public partial class Program : Ast
    {
        public Token token;
        public Cons<Decl> declarations;
        public AllposDecl allpos = null;
        public DefaultWhereDecl defaultwhere1 = null;
        public DefaultWhereDecl defaultwhere2 = null;

        Dictionary<string, Decl> globals;

        public string Description
        {
            get
            {
                return ToString();
            }
        }

        public Header header
        {
            get
            {
                if (token == null)
                    return Header.NOTHING;
                else 
                {
                    switch (token.Kind)
                    {
                        case Tokens.WS1S: return Header.WS1S;
                        case Tokens.WS2S: return Header.WS2S;
                        case Tokens.M2LSTR: return Header.M2LSTR;
                        default: return Header.M2LTREE;
                    }
                }
            }
        }

        internal Program(Token token, Cons<Decl> declarations, Dictionary<string, Decl> globals)
        {
            this.token = token;
            this.declarations = declarations;
            this.globals = globals;
        }

        public void Typecheck()
        {
            var glob = new Dictionary<string, Decl>();
            var loc = MapStack<string, Param>.Empty;
            var decls = declarations;
            while (!decls.IsEmpty)
            {
                var decl = decls.First;
                decls = decls.Rest;
                decl.TypeCheck(glob, loc);
                switch (decl.kind)
                {
                    case DeclKind.macro:
                    case DeclKind.pred:
                        glob[((PredDecl)decl).name.text] = decl;
                        break;
                    case DeclKind.constant:
                        glob[((ConstDecl)decl).name.text] = decl;
                        break;
                    case DeclKind.var0:
                        foreach (var v in ((Var0Decls)decl).vars)
                            glob[v.text] = new Var0Decl(v);
                        break;
                    case DeclKind.var1:
                    case DeclKind.var2:
                        foreach (var v in ((VarDecls)decl).vars)
                            glob[v.name] = new VarDecl(((VarDecls)decl).kind, ((VarDecls)decl).univs, v);
                        break;
                    case DeclKind.universe:
                        foreach (var v in ((UnivDecls)decl).args)
                            glob[v.name] = new UnivDecl(v);
                        break;
                    case DeclKind.allpos:
                        allpos = (AllposDecl)decl;
                        break;
                    case DeclKind.defaultwhere1:
                        defaultwhere1 = (DefaultWhereDecl)decl;
                        break;
                    case DeclKind.defaultwhere2:
                        defaultwhere2 = (DefaultWhereDecl)decl;
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

    public enum ExprType
    {
        BOOL, SET, INT, UNKNOWN,
        RANGE
    }

    public abstract class Expr : Ast
    {
        public ExprType type;
        public Token token;
        internal Expr(Token token, ExprType type)
        {
            this.token = token;
            this.type = type;
        }

        internal abstract void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals);
    }

    #region formulas

    public enum BinaryBooleanOp { AND, OR, IMPLIES, EQUIV }

    public class BinaryBooleanFormula : Expr
    {
        public BinaryBooleanOp op
        {
            get
            {
                switch (token.Kind)
                {
                    case Tokens.AND: return BinaryBooleanOp.AND;
                    case Tokens.OR: return BinaryBooleanOp.OR;
                    case Tokens.IMPLIES: return BinaryBooleanOp.IMPLIES;
                    default: return BinaryBooleanOp.EQUIV;                }
            }
        }
        public Expr arg1;
        public Expr arg2;
        internal BinaryBooleanFormula(Token token, Expr formula1, Expr formula2)
            : base(token, ExprType.BOOL)
        {
            this.arg1 = formula1;
            this.arg2 = formula2;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            arg1.Print(sb);
            sb.Append(" ");
            sb.Append(token.text);
            sb.Append(" ");
            arg2.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            arg1.TypeCheck(globals, locals);
            arg2.TypeCheck(globals, locals);
            if (arg1.type != ExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg1.token.Location, string.Format("expecting BOOL not {0}", arg1.type.ToString()));
            if (arg2.type != ExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg2.token.Location, string.Format("expecting BOOL not {0}", arg2.type.ToString()));
            return;
        }
    }

    public class Restrict : Expr
    {
        Expr formula;
        internal Restrict(Token token, Expr formula)
            : base(token, ExprType.BOOL)
        {
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("restrict(");
            formula.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            formula.TypeCheck(globals, locals);
            if (formula.type != ExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, formula.token.Location, string.Format("expecting BOOL not {0}", formula.type.ToString()));

        }
    }

    public class NegatedFormula : Expr
    {
        public Expr formula;
        internal NegatedFormula(Token token, Expr formula)
            : base(token, ExprType.BOOL)
        {
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("~");
            formula.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            formula.TypeCheck(globals, locals);
            if (formula.type != ExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, formula.token.Location, string.Format("expecting BOOL not {0}", formula.type.ToString()));
        }
    }

    public class BooleanConstant : Expr
    {
        public bool isTrue
        {
            get { return token.Kind == Tokens.TRUE; }
        }

        internal BooleanConstant(Token token)
            : base(token, ExprType.BOOL)
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(token.text);
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            return;
        }
    }

    public class BinaryAtom : Expr
    {
        public Expr term1;
        public Expr term2;
        internal BinaryAtom(Token token, Expr term1, Expr term2)
            : base(token, ExprType.BOOL)
        {
            this.term1 = term1;
            this.term2 = term2;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            term1.Print(sb);
            sb.Append(" ");
            sb.Append(token.text);
            sb.Append(" ");
            term2.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            term1.TypeCheck(globals, locals);
            term2.TypeCheck(globals, locals);
            switch (token.Kind)
            {
                case Tokens.GE:
                case Tokens.LE:
                case Tokens.LT:
                case Tokens.GT:
                    {
                        if (term1.type != ExprType.INT)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term1.token.Location, string.Format("expecting INT not {0}", term1.type.ToString()));
                        if (term2.type != ExprType.INT)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term2.token.Location, string.Format("expecting INT not {0}", term2.type.ToString()));
                        return;
                    }
                case Tokens.EQ:
                case Tokens.NE:
                    {
                        if (term1.type != term2.type)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term2.token.Location, string.Format("expecting {0} not {1}", term1.type.ToString(), term2.type.ToString()));
                        return;
                    }
                case Tokens.IN:
                case Tokens.NOTIN:
                    {
                        if (term1.type != ExprType.INT)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term1.token.Location, string.Format("expecting INT not {0}", term1.type.ToString()));
                        if (term2.type != ExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term2.token.Location, string.Format("expecting SET not {0}", term2.type.ToString()));
                        return;
                    }
                case Tokens.SUBSET:
                    {
                        if (term1.type != ExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term1.token.Location, string.Format("expecting SET not {0}", term1.type.ToString()));
                        if (term2.type != ExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, term2.token.Location, string.Format("expecting SET not {0}", term2.type.ToString()));
                        return;
                    }
                default:
                    throw new MonaParseException(MonaParseExceptionKind.InternalError, token.Location, string.Format("unexpected token '{0}'", token.text));

        }
        }
    }

    public class PredApp : Expr
    {
        public Cons<Expr> expressions;
        internal PredApp(Token token, Cons<Expr> expressions)
            : base(token, ExprType.BOOL)
        {
            this.expressions = expressions;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(token.text);
            sb.Append("(");
            sb.Append(expressions.ToString(","));
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            if (!globals.ContainsKey(token.text))
                throw new MonaParseException(MonaParseExceptionKind.UndeclaredPredicate, token.Location, token.text);

            Decl decl = globals[token.text];
            PredDecl pdecl = decl as PredDecl;
            if (pdecl == null)
            {
                throw new MonaParseException(MonaParseExceptionKind.InvalidUseOfName, token.Location, token.text);
            }
            if (pdecl.parameters.Count != expressions.Count)
            {
                throw new MonaParseException(MonaParseExceptionKind.InvalidNrOfParameters, token.Location, token.text);
            }
            for (int i = 0; i < expressions.Count; i++)
            {
                expressions[i].TypeCheck(globals, locals);
                if (pdecl.parameters[i].type != expressions[i].type)
                    throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, expressions[i].token.Location,
                        string.Format("parameter {0} of {1} must be of type {2} not {3}", pdecl.parameters[i].Token.text, pdecl.name.text, pdecl.parameters[i].type, expressions[i].type));
            }
        }
    }

    public class QBFormula : Expr
    {
        Dictionary<string, Param> varmap;
        public Cons<Token> vars;
        public Expr formula;
        internal QBFormula(Token quantifier, Cons<Token> vars, Expr formula, Dictionary<string, Param> varmap) :
            base(quantifier, ExprType.BOOL)
        {
            this.varmap = varmap;
            this.vars = vars;
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(token.text);
            sb.Append(" ");
            sb.Append(vars.ToString(","));
            sb.Append(":");
            formula.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            formula.TypeCheck(globals, locals.Push(varmap));
            if (formula.type != ExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, formula.token.Location, string.Format("expecting BOOL not {0}", formula.type.ToString()));
        }
    }

    public class QFormula : Expr
    {
        public Dictionary<string, Param> varmap;
        public Cons<Token> universes;
        public Cons<VarWhere> vars;
        public Expr formula;
        internal QFormula(Token quantifier, Cons<VarWhere> vars, Expr formula, Cons<Token> universes, Dictionary<string, Param> varmap) :
            base(quantifier, ExprType.BOOL)
        {
            this.varmap = varmap;
            this.universes = universes;
            this.vars = vars;
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(token.text);
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

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            //TBD: check where-expressions
            var locals1 = locals.Push(varmap);
            formula.TypeCheck(globals, locals1);
            if (formula.type != ExprType.BOOL)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, formula.token.Location, string.Format("expecting BOOL not {0}", formula.type.ToString()));
        }
    }

    public class IsEmpty : Expr
    {
        Expr set;
        public IsEmpty(Token token, Expr term)
            : base(token, ExprType.BOOL)
        {
            this.set = term;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("empty(");
            set.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            set.TypeCheck(globals, locals);
            if (set.type != ExprType.SET)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, set.token.Location, string.Format("expecting SET not {0}", set.type.ToString()));

        }
    }

    public enum LetKind { let0, let1, let2 }

    public class Let : Expr
    {
        public Cons<Tuple<Token, Expr>> lets;
        public Dictionary<string, Param> let_vars;
        public Expr formula;

        public ExprType letType
        {
            get
            {
                ExprType lettype = (token.Kind == Tokens.LET0 ? ExprType.BOOL :
                                      (token.Kind == Tokens.LET1 ? ExprType.INT : ExprType.SET));
                return lettype;
            }
        }

        internal Let(Token letkind, Cons<Tuple<Token, Expr>> lets, Expr formula, Dictionary<string, Param> let_vars)
            : base(letkind, ExprType.BOOL)
        {
            this.lets = lets;
            this.let_vars = let_vars;
            this.formula = formula;
        }

        public LetKind Kind
        {
            get
            {
                switch (token.Kind)
                {
                    case Tokens.LET1: return LetKind.let1;
                    case Tokens.LET2: return LetKind.let2;
                    default: return LetKind.let0;
                }
            }
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            foreach (var let in lets)
            {
                let.Item2.TypeCheck(globals, locals);
                if (let.Item2.type != letType)
                    throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, let.Item2.token.Location, let.Item2.token.text);
            }
            formula.TypeCheck(globals, locals.Push(let_vars));
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(token.text);
            sb.Append(" ");
            sb.Append(lets.ToString(",", PrintLet));
            sb.Append(" in (");
            formula.Print(sb);
            sb.Append(")");
        }

        static string PrintLet(Tuple<Token, Expr> t)
        {
            return string.Format("{0} = ({1})", t.Item1.text, t.Item2.ToString());
        }
    }

    #endregion

    public class Name : Expr
    {
        public string name
        {
            get
            {
                return token.text;
            }
        }
        internal Name(Token token, ExprType type = ExprType.UNKNOWN)
            : base(token, type)
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name);
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            var t = GetExprType(globals, locals);
            if (type == ExprType.UNKNOWN)
                type = t;
            else if (type != t)
                throw new MonaParseException(MonaParseExceptionKind.InternalError, token.Location,
                    string.Format("inconsistent types {0} and {1} for '{2}'", type.ToString(), t.ToString(), name));
        }

        ExprType GetExprType(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            var x = token;
            Param xp = null;
            Decl xd = null;
            if (locals.TryGetValue(x.text, out xp))
            {
                if (xp.kind == ParamKind.universe || xp.kind == ParamKind.var2)
                    return ExprType.SET;
                else if (xp.kind == ParamKind.var1)
                    return ExprType.INT;
                else
                    return ExprType.BOOL;
            }
            else if (globals.TryGetValue(x.text, out xd))
            {
                if (xd.kind == DeclKind.constant || xd.kind == DeclKind.var1)
                    return ExprType.INT;
                else if (xd.kind == DeclKind.universe || xd.kind == DeclKind.var2)
                    return ExprType.SET;
                else if (xd.kind == DeclKind.var0 || ((xd.kind == DeclKind.macro || xd.kind == DeclKind.pred)
                                                      && ((PredDecl)xd).IsNullary))
                    return ExprType.BOOL;
                else if (xd.kind == DeclKind.macro || xd.kind == DeclKind.pred)
                    throw new MonaParseException(MonaParseExceptionKind.InvalidUseOfPredicateOrMacroName, x.Location);
            }
            throw new MonaParseException(MonaParseExceptionKind.UndeclaredIdentifier, x.Location, x.text);
        }
    }

    #region terms

    public class Int : Expr
    {
        int i;
        internal Int(Token val)
            : base(val, ExprType.INT)
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
            sb.Append(token.text);
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            return;
        }
    }

    public enum ArithmOp { PLUS, MINUS, TIMES, DIV, MOD }

    public class ArithmFuncApp : Expr
    {
        public ArithmOp op
        {
            get
            {
                switch (token.Kind)
                {
                    case Tokens.PLUS: return ArithmOp.PLUS;
                    case Tokens.MINUS: return ArithmOp.MINUS;
                    case Tokens.TIMES: return ArithmOp.TIMES;
                    case Tokens.DIV: return ArithmOp.DIV;
                    case Tokens.MOD: return ArithmOp.MOD;
                    default:
                        throw new MonaParseException(MonaParseExceptionKind.UnknownArithmeticOperator, token.Location, string.Format("unknown operator '{0}'", token.text));
                }
            }
        }
        public Expr arg1;
        public Expr arg2;
        internal ArithmFuncApp(Token func, Expr arg1, Expr arg2)
            : base(func, ExprType.INT)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            arg1.Print(sb);
            sb.Append(" ");
            sb.Append(token.text);
            sb.Append(" ");
            arg2.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            arg1.TypeCheck(globals, locals);
            arg2.TypeCheck(globals, locals);
            if (arg1.type != ExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg1.token.Location, string.Format("expecting INT not {0}", arg1.type.ToString()));
            if (arg2.type != ExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg2.token.Location, string.Format("expecting INT not {0}", arg2.type.ToString()));
        }
    }

    public class Set : Expr
    {
        public Cons<Expr> elems;
        internal Set(Token set, Cons<Expr> elems = null)
            : base(set, ExprType.SET)
        {
            this.elems = elems;
        }

        public override void Print(StringBuilder sb)
        {
            if (elems == null)
                sb.Append(token.text);
            else
            {
                sb.Append("{");
                sb.Append(elems.ToString(","));
                sb.Append("}");
            }
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            foreach (var e in elems)
            {
                e.TypeCheck(globals, locals);
            }

        }
    }

    public class MinOrMax : Expr
    {
        public bool isMin
        {
            get { return token.Kind == Tokens.MIN; }
        }
        public Expr set;
        public MinOrMax(Token minormax, Expr set)
            : base(minormax, ExprType.INT)
        {
            this.set = set;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(token.text);
            sb.Append(" ");
            set.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            if (set.type != ExprType.SET)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, set.token.Location, string.Format("expecting SET not {0}", set.type.ToString()));

        }
    }

    public class Pconst : Expr
    {
        public Expr elem;
        internal Pconst(Token pconst, Expr elem)
            : base(pconst, ExprType.SET)
        {
            this.elem = elem;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(token.text);
            sb.Append("(");
            elem.Print(sb);
            sb.Append(")");
        }
        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            elem.TypeCheck(globals, locals);
            if (elem.type != ExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, elem.token.Location, string.Format("expecting INT not {0}", elem.type.ToString()));
        }
    }

    public class Range : Expr
    {
        public Expr from;
        public Expr to;
        internal Range(Token dots, Expr from, Expr to)
            : base(dots, ExprType.SET)
        {
            this.from = from;
            this.to = to;
        }

        public override void Print(StringBuilder sb)
        {
            from.Print(sb);
            sb.Append(",...,");
            to.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            from.TypeCheck(globals, locals);
            if (from.type != ExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, from.token.Location, string.Format("expecting INT not {0}", from.type.ToString()));
            if (to.type != ExprType.INT)
                throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, to.token.Location, string.Format("expecting INT not {0}", to.type.ToString()));
        }
    }

    public enum SetOpKind { UNION, INTER, SETMINUS, PLUS, MINUS }

    public class SetOp : Expr
    {
        public Expr arg1;
        public Expr arg2;

        public SetOpKind kind
        {
            get
            {
                switch (token.Kind)
                {
                    case Tokens.UNION: return SetOpKind.UNION;
                    case Tokens.INTER: return SetOpKind.INTER;
                    case Tokens.PLUS: return SetOpKind.PLUS;
                    case Tokens.MINUS: return SetOpKind.MINUS;
                    default: return SetOpKind.SETMINUS;
                }
            }
        }

        internal SetOp(Token op, Expr arg1, Expr arg2)
            : base(op, ExprType.SET)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            arg1.Print(sb);
            sb.Append(" ");
            sb.Append(MonaParser.DescribeTokens(token.Kind));
            sb.Append(" ");
            arg2.Print(sb);
            sb.Append(")");
        }

        internal override void TypeCheck(Dictionary<string, Decl> globals, MapStack<string, Param> locals)
        {
            arg1.TypeCheck(globals, locals);
            arg2.TypeCheck(globals, locals);
            switch (kind)
            {
                case SetOpKind.INTER:
                case SetOpKind.SETMINUS:
                case SetOpKind.UNION:
                    {
                        if (arg1.type != ExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg1.token.Location, string.Format("expecting SET not {0}", arg1.type.ToString()));
                        if (arg2.type != ExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg2.token.Location, string.Format("expecting SET not {0}", arg2.type.ToString()));
                        return;
                    }
                default:
                    {
                        if (arg1.type != ExprType.SET)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg1.token.Location, string.Format("expecting SET not {0}", arg1.type.ToString()));
                        if (arg2.type != ExprType.INT)
                            throw new MonaParseException(MonaParseExceptionKind.TypeMismatch, arg2.token.Location, string.Format("expecting INT not {0}", arg2.type.ToString()));
                        return;
                    }

            }
        }
    }

    #endregion

    #region declarations

    public enum DeclKind
    {
        formula, guide, universe, include, assert, execute, constant, 
        defaultwhere1, defaultwhere2,
        var0, var1, var2, tree, macro, pred, allpos, type
    };

    public abstract class Decl : Ast
    {
        public DeclKind kind;
        internal Decl(DeclKind kind)
        {
            this.kind = kind;
        }

        internal abstract void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc);
    }

    public class VarDecls : Decl
    {
        public Cons<Token> univs;
        public Cons<VarWhere> vars;
        internal VarDecls(DeclKind kind, Cons<Token> univs, Cons<VarWhere> vars)
            : base(kind)
        {
            this.univs = univs;
            this.vars = vars;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + (univs == null ? "" : "[" + univs.ToString(",") + "]") + vars.ToString(","));
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            if (univs != null)
                foreach (var u in univs)
                {
                    Decl d;
                    if (!glob.TryGetValue(u.text, out d))
                        throw new MonaParseException(MonaParseExceptionKind.UndeclaredIdentifier, u.Location, u.text);
                    if (d.kind != DeclKind.universe)
                        throw new MonaParseException(MonaParseExceptionKind.InvalidUniverseReference, u.Location, u.text);
                }
            ParamKind paramkind = (this.kind == DeclKind.var1 ? ParamKind.var1 : ParamKind.var2);
            foreach (var vw in vars)
                if (vw.where != null)
                    vw.where.TypeCheck(glob, loc.Push(vw.name, new VarParam(vw, paramkind)));
        }
    }

    public class VarDecl : Decl
    {
        public Cons<Token> univs;
        public VarWhere vw;
        internal VarDecl(DeclKind kind, Cons<Token> univs, VarWhere vw)
            : base(kind)
        {
            this.univs = univs;
            this.vw = vw;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + (univs == null ? "" : "[" + univs.ToString(",") + "]") + vw.ToString());
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            if (univs != null)
                foreach (var u in univs)
                {
                    Decl d;
                    if (!glob.TryGetValue(u.text, out d))
                        throw new MonaParseException(MonaParseExceptionKind.UndeclaredIdentifier, u.Location, u.text);
                    if (d.kind != DeclKind.universe)
                        throw new MonaParseException(MonaParseExceptionKind.InvalidUniverseReference, u.Location, u.text);
                }
            ParamKind paramkind = (this.kind == DeclKind.var1 ? ParamKind.var1 : ParamKind.var2);
            if (vw.where != null)
                vw.where.TypeCheck(glob, loc.Push(vw.name, new VarParam(new VarWhere(vw.token, null), paramkind)));
        }
    }

    public class Var0Decls : Decl
    {
        public Cons<Token> vars;
        internal Var0Decls(Cons<Token> vars)
            : base(DeclKind.var0)
        {
            this.vars = vars;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + vars.ToString(","));
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            return;
        }
    }

    public class Var0Decl : Decl
    {
        public Token var;
        internal Var0Decl(Token var)
            : base(DeclKind.var0)
        {
            this.var = var;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + var.text);
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            return;
        }
    }

    public class ConstDecl : Decl
    {
        public Token name;
        public Expr def;

        internal ConstDecl(Token name, Expr def)
            : base(DeclKind.constant)
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

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            def.TypeCheck(glob, loc);
        }
    }

    public class DefaultWhereDecl : Decl
    {
        public bool isSecondOrder;
        public Token param;
        public Expr formula;

        internal DefaultWhereDecl(bool isSecondOrder, Token param, Expr formula) : 
            base(isSecondOrder ? DeclKind.defaultwhere2 : DeclKind.defaultwhere1)
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

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            ParamKind paramkind = (isSecondOrder ? ParamKind.var2 : ParamKind.var1);
            formula.TypeCheck(glob, loc.Push(param.text, new VarParam(new VarWhere(param,null), paramkind)));
        }
    }

    public class FormulaDecl : Decl
    {
        public Expr formula;
        internal FormulaDecl(Expr formula) : base(DeclKind.formula)
        {
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            formula.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            formula.TypeCheck(glob, loc);
        }
    }

    public class AssertDecl : Decl
    {
        public Expr psi;
        internal AssertDecl(Expr psi)
            : base(DeclKind.assert)
        {
            this.psi = psi;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("assert ");
            psi.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            psi.TypeCheck(glob, loc);
        }
    }

    public class ExecuteDecl : Decl
    {
        public Expr psi;
        internal ExecuteDecl(Expr psi)
            : base(DeclKind.execute)
        {
            this.psi = psi;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("execute ");
            psi.Print(sb);
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            psi.TypeCheck(glob, loc);
        }
    }

    public class UnivDecls : Decl
    {
        public Cons<UnivArg> args;
        internal UnivDecls(Cons<UnivArg> args)
            : base(DeclKind.universe)
        {
            this.args = args;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("universe " + args.ToString(","));
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
        }
    }

    public class UnivDecl : Decl
    {
        public UnivArg universe;
        internal UnivDecl(UnivArg universe)
            : base(DeclKind.universe)
        {
            this.universe = universe;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("universe " + universe.ToString());
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
        }
    }

    public class PredDecl : Decl
    {
        public bool isMacro;
        public Token name;
        public Cons<Param> parameters;
        public Expr formula;
        public Dictionary<string, Param> pmap;
        public bool IsNullary
        {
            get
            {
                return parameters == null || parameters.Count == 0;
            }
        }

        internal PredDecl(Token name, Cons<Param> parameters, Dictionary<string,Param> pmap, Expr formula, bool isMacro = false)
            : base(isMacro ? DeclKind.macro : DeclKind.pred)
        {
            this.pmap = pmap;
            this.isMacro = isMacro;
            this.name = name;
            this.parameters = parameters;
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

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            formula.TypeCheck(glob, loc.Push(pmap));
        }
    }

    public class AllposDecl : Decl
    {
        Token v;
        internal AllposDecl(Token v)
            : base(DeclKind.allpos)
        {
            this.v = v;
        }

        internal override void TypeCheck(Dictionary<string, Decl> glob, MapStack<string, Param> loc)
        {
            return;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("allpos ");
            sb.Append(v.text);
        }
    }

    public enum ParamKind { var0, var1, var2, universe }

    public abstract class Param : Ast
    {
        public ParamKind kind;
        internal Param(ParamKind kind)
        {
            this.kind = kind;
        }
        public ExprType type
        {
            get
            {
                if (kind == ParamKind.universe || kind ==  ParamKind.var2)
                    return ExprType.SET;
                else if (kind == ParamKind.var1)
                    return ExprType.INT;
                else
                    return ExprType.BOOL;
            }
        }
        public abstract Token Token { get; }
    }

    public class Var0Param : Param
    {
        public Token name;

        public override void Print(StringBuilder sb)
        {
            sb.Append("var0 ");
            sb.Append(name.text);
        }

        internal Var0Param(Token name) : base(ParamKind.var0)
        {
            this.name = name;
        }

        public override Token Token
        {
            get { return name; }
        }
    }

    public class VarParam : Param
    {
        public VarWhere varwhere;

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString());
            sb.Append(" ");
            varwhere.Print(sb);
        }

        internal VarParam(VarWhere name, ParamKind kind)
            : base(kind)
        {
            this.varwhere = name;
        }

        public override Token Token
        {
            get { return varwhere.token; }
        }
    }

    public class UniverseParam : Param
    {
        public Token name;

        public override void Print(StringBuilder sb)
        {
            sb.Append("universe ");
            sb.Append(name.text);
        }

        internal UniverseParam(Token name)
            : base(ParamKind.universe)
        {
            this.name = name;
        }

        public override Token Token
        {
            get { return name; }
        }
    }

    #endregion

    public class VarWhere : Ast
    {
        public Token token;
        public string name
        {
            get
            {
                return token.text;
            }
        }
        public Expr where;
        internal VarWhere(Token token, Expr where)
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

    public class UnivArg : Ast
    {
        public Token token;
        public string name 
        {
            get 
            {
                return token.text;
            }
        }
        internal UnivArg(Token token)
        {
            this.token = token;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name);
        }
    }

    public class UnivArgWithType : UnivArg
    {
        public Token E;

        public string type
        {
            get {
                return E.text;
            }
        }

        internal UnivArgWithType(Token token, Token E)
            : base(token)
        {
            this.E = E;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name + ":" + type);
        }
    }

    public class UnivArgWithSucc : UnivArg
    {
        /// <summary>
        /// matches (0|1)+
        /// </summary>
        public string succ;

        internal UnivArgWithSucc(Token token, string succ)
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
