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
                throw new MonaParseException(location, string.Format("cannot convert token '{0}' to integer", text));
        }
    }

    public abstract class Ast 
    {
        //public Token token;
        //internal Ast(Token token)
        //{
        //    this.token = token;
        //}

        public abstract void Print(StringBuilder sb);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Print(sb);
            return sb.ToString();
        }
    }

    public enum Header { WS1S, WS2S, M2LSTR, M2LTREE, NOTHING } 

    public class Program : Ast
    {
        public Token token;
        public Cons<Decl> declarations;

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

        internal Program(Token token, Cons<Decl> declarations)
        {
            this.token = token;
            this.declarations = declarations;
        }

        internal void Typecheck()
        {
            throw new NotImplementedException();
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
        BOOL, SET, INT, UNKNOWN
    }

    public abstract class Expr : Ast
    {
        public ExprType type;
        public Token token;
        internal Expr(Token token, ExprType type = ExprType.UNKNOWN)
        {
            this.token = token;
            this.type = type;
        }
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
        public Expr formula1;
        public Expr formula2;
        internal BinaryBooleanFormula(Token token, Expr formula1, Expr formula2)
            : base(token, ExprType.BOOL)
        {
            this.formula1 = formula1;
            this.formula2 = formula2;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            formula1.Print(sb);
            sb.Append(" ");
            sb.Append(token.text);
            sb.Append(" ");
            formula2.Print(sb);
            sb.Append(")");
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
    }

    public class BinaryAtom : Expr
    {
        public Expr term1;
        public Expr term2;
        internal BinaryAtom(Token token, Expr term1, Expr term2)
            : base(token)
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
    }

    public class PredApp : Expr
    {
        public Cons<Expr> expressions;
        internal PredApp(Token token, Cons<Expr> expressions)
            : base(token)
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
    }

    public class QBFormula : Expr
    {
        public Cons<Token> vars;
        public Expr formula;
        internal QBFormula(Token quantifier, Cons<Token> vars, Expr formula) :
            base(quantifier)
        {
            this.vars = vars;
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            throw new NotImplementedException();
        }
    }

    public class QFormula : Expr
    {
        public Cons<Token> universes;
        public Cons<VarWhere> vars;
        public Expr formula;
        internal QFormula(Token quantifier, Cons<VarWhere> vars, Expr formula, Cons<Token> universes = null) :
            base(quantifier)
        {
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
        }
    }

    public class IsEmpty : Expr
    {
        Expr term;
        public IsEmpty(Token token, Expr term)
            : base(token)
        {
            this.term = term;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("empty(");
            term.Print(sb);
            sb.Append(")");
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
    }

    #region terms

    public class Int : Expr
    {
        int i;
        internal Int(Token val)
            : base(val, ExprType.INT)
        {
            if (!val.TryGetInt(out i))
                throw new MonaParseException(val.Location, "invalid integer format");
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
                        throw new MonaParseException(token.Location, string.Format("unknown operator '{0}'", token.text));
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
    }

    public class VarDecl : Decl
    {
        public Cons<Token> univs;
        public Cons<VarWhere> vars;
        internal VarDecl(DeclKind kind, Cons<Token> univs, Cons<VarWhere> vars)
            : base(kind)
        {
            this.univs = univs;
            this.vars = vars;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + (univs == null ? "" : "[" + univs.ToString(",") + "]") + vars.ToString(","));
        }
    }

    public class Var0Decl : Decl
    {
        public Cons<Token> vars;
        internal Var0Decl(Cons<Token> vars)
            : base(DeclKind.var0)
        {
            this.vars = vars;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(kind.ToString() + " " + vars.ToString(","));
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
    }

    public class UnivDecl : Decl
    {
        public Cons<UnivArg> args;
        internal UnivDecl(Cons<UnivArg> args)
            : base(DeclKind.universe)
        {
            this.args = args;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("universe " + args.ToString(","));
        }
    }

    public class PredDecl : Decl
    {
        public bool isMacro;
        public Token name;
        public Cons<Param> parameters;
        public Expr formula;
        Dictionary<string, Param> pmap;

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
    }

    public enum ParamKind { var0, var1, var2, universe }

    public abstract class Param : Ast
    {
        public ParamKind kind;
        internal Param(ParamKind kind)
        {
            this.kind = kind;
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

    public class Var1Param : Param
    {
        public VarWhere varwhere;

        public override void Print(StringBuilder sb)
        {
            sb.Append("var1 ");
            varwhere.Print(sb);
        }

        internal Var1Param(VarWhere name)
            : base(ParamKind.var1)
        {
            this.varwhere = name;
        }

        public override Token Token
        {
            get { return varwhere.token; }
        }
    }

    public class Var2Param : Param
    {
        public VarWhere varwhere;

        public override void Print(StringBuilder sb)
        {
            sb.Append("var2 ");
            varwhere.Print(sb);
        }

        internal Var2Param(VarWhere name)
            : base(ParamKind.var2)
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
