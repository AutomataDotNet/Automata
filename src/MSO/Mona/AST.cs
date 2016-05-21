using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using QUT.Gppg;

namespace Microsoft.Automata.MSO.Mona
{
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

    public class Token : Ast
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

        internal Token(string name, Tokens kind)
        {
            this.text = name;
            this.location = new LexLocation();
            this.kind = kind;
            this.file = null;
        }

        internal Token(string name)
        {
            this.text = name;
            this.location = new LexLocation();
            this.kind = (Tokens)(-1);
            this.file = null;
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
                throw new MonaParseException(location, string.Format("cannot convert to integer: {0}", text));
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(text);
        }
    }

    public class Program : Ast
    {
        public Token header;
        public Cons<Decl> declarations;

        internal Program(Token header, Cons<Decl> declarations)
        {
            this.header = header;
            this.declarations = declarations;
        }

        internal void Typecheck()
        {
            throw new NotImplementedException();
        }

        public override void Print(StringBuilder sb)
        {
            if (header != null)
                sb.AppendLine(MonaParser.DescribeTokens(header.Kind) + ";");
            foreach (var decl in declarations)
            {
                decl.Print(sb);
                sb.AppendLine(";");
            }
        }
    }

    public abstract class Expression : Ast
    {
    }

    public abstract class Formula : Expression
    {
        public Token token;
        internal Formula(Token token)
        {
            this.token = token;
        }
    }

    public abstract class Term : Expression
    {
        public Token token;
        internal Term(Token token)
        {
            this.token = token;
        }
    }

    public enum BinaryBooleanOp { AND, OR, IMPLIES, EQUIV }
    
    public class BinaryBooleanFormula : Formula
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
                    case Tokens.EQUIV: return BinaryBooleanOp.EQUIV;
                    default:
                        throw new AutomataException(AutomataExceptionKind.InternalError);
                }
            }
        }
        public Formula formula1;
        public Formula formula2;
        internal BinaryBooleanFormula(Token token, Formula formula1, Formula formula2)
            : base(token)
        {
            this.formula1 = formula1;
            this.formula2 = formula2;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            formula1.Print(sb);
            sb.Append(" ");
            token.Print(sb);
            sb.Append(" ");
            formula2.Print(sb);
            sb.Append(")");
        }
    }

    public class Restrict : Formula
    {
        Formula formula;
        internal Restrict(Token token, Formula formula)
            : base(token)
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

    public class NegatedFormula : Formula
    {
        public Formula formula;
        internal NegatedFormula(Token token, Formula formula)
            : base(token)
        {
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("~");
            formula.Print(sb);
        }
    }

    public class BooleanConstant : Formula
    {
        public bool isTrue
        {
            get { return token.Kind == Tokens.TRUE; }
        }

        internal BooleanConstant(Token token)
            : base(token)
        {
        }

        public override void Print(StringBuilder sb)
        {
            token.Print(sb);
        }
    }

    public class BooleanVariable : Formula
    {
        internal BooleanVariable(Token token)
            : base(token)
        {
        }

        public override void Print(StringBuilder sb)
        {
            token.Print(sb);
        }
    }

    public class BinaryAtom : Formula
    {
        public Term term1;
        public Term term2;
        internal BinaryAtom(Token token, Term term1, Term term2)
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
            token.Print(sb);
            sb.Append(" ");
            term2.Print(sb);
            sb.Append(")");
        }
    }

    public class PredApp : Formula
    {
        public Cons<Expression> expressions;
        internal PredApp(Token token, Cons<Expression> expressions)
            : base(token)
        {
            this.expressions = expressions;
        }

        public override void Print(StringBuilder sb)
        {
            token.Print(sb);
            sb.Append("(");
            sb.Append(expressions.ToString(","));
            sb.Append(")");
        }
    }

    public class QBFormula : Formula
    {
        public Cons<Token> vars;
        public Formula formula;
        internal QBFormula(Token quantifier, Cons<Token> vars, Formula formula) :
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

    public class QFormula : Formula
    {
        public Cons<Token> universes;
        public Cons<VarWhere> vars;
        public Formula formula;
        internal QFormula(Token quantifier, Cons<VarWhere> vars, Formula formula, Cons<Token> universes = null) :
            base(quantifier)
        {
            this.universes = universes;
            this.vars = vars;
            this.formula = formula;
        }

        public override void Print(StringBuilder sb)
        {
            token.Print(sb);
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

    public class VarWhere : Ast
    {
        public Token name;
        public Formula where;
        internal VarWhere(Token name, Formula where)
        {
            this.name = name;
            this.where = where;
        }

        public override string ToString()
        {
            if (where == null)
                return name.text;
            else
                return name.text + " where " + where.ToString();
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name.text);
            if (where != null)
            {
                sb.Append(" where ");
                where.Print(sb);
            }
        }
    }

    public class Name : Term
    {
        internal Name(Token name)
            : base(name)
        {
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(token.text);
        }
    }

    public class Int : Term
    {
        int i;
        internal Int(Token val)
            : base(val)
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

    //public class FunctionSymbol : Token
    //{
    //}

    public enum ArithmOp { PLUS, MINUS, TIMES, DIV, MOD }

    public class ArithmFuncApp : Term
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
                        throw new AutomataException(AutomataExceptionKind.InternalError);
                }
            }
        }
        public Term arg1;
        public Term arg2;
        internal ArithmFuncApp(Token func, Term arg1, Term arg2)
            : base(func)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("(");
            arg1.Print(sb);
            sb.Append(" ");
            token.Print(sb);
            sb.Append(" ");
            arg2.Print(sb);
            sb.Append(")");
        }
    }

    public class Set : Term
    {
        Cons<Term> elems;
        internal Set(Token lbrace, Cons<Term> elems)
            : base(lbrace)
        {
            this.elems = elems;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append("{");
            sb.Append(elems.ToString(","));
            sb.Append("}");
        }
    }

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
        public Term def;

        internal ConstDecl(Token name, Term def)
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
        public Formula formula;

        internal DefaultWhereDecl(bool isSecondOrder, Token param, Formula formula) : 
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
        public Formula formula;
        internal FormulaDecl(Formula formula) : base(DeclKind.formula)
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
        public Formula psi;
        internal AssertDecl(Formula psi)
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
        public Formula psi;
        internal ExecuteDecl(Formula psi)
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
        public Token name;
        internal PredDecl(Token name)
            : base(DeclKind.pred)
        {
            this.name = name;
        }

        public override void Print(StringBuilder sb)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    public class UnivArg : Ast
    {
        public Token name;

        internal UnivArg(Token name)
        {
            this.name = name;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name.text);
        }
    }

    public class UnivArgWithType : UnivArg
    {
        public Token E;

        internal UnivArgWithType(Token name, Token E)
            : base(name)
        {
            this.E = E;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name.text + ":" + E.text);
        }
    }

    public class UnivArgWithSucc : UnivArg
    {
        /// <summary>
        /// matches (0|1)+
        /// </summary>
        public string succ;

        internal UnivArgWithSucc(Token name, string succ)
            : base(name)
        {
            this.succ = succ;
        }

        public override void Print(StringBuilder sb)
        {
            sb.Append(name.text + ":" + succ);
        }
    }

    public abstract class Param : Ast
    {
    }

    public class Var0Param : Param
    {
        public Cons<Token> names;

        public override void Print(StringBuilder sb)
        {
            sb.Append(names.ToString(","));
        }
    }

    public class Var1Param : Param
    {
        public Cons<VarWhere> names;

        public override void Print(StringBuilder sb)
        {
            sb.Append(names.ToString(","));
        }
    }

    public class Var2Param : Param 
    {
        public Cons<VarWhere> names;

        public override void Print(StringBuilder sb)
        {
            sb.Append(names.ToString(","));
        }
    }

    public class UniverseParam : Param 
    {
        public Token name;

        public override void Print(StringBuilder sb)
        {
            sb.Append(name.text);
        }
    }

    public class IsEmpty : Formula
    {
        Term term;
        public IsEmpty(Token token, Term term)
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

    public class MinOrMax : Term
    {
        Term set;
        public MinOrMax(Token minormax, Term set)
            : base(minormax)
        {
            this.set = set;
        }

        public override void Print(StringBuilder sb)
        {
            token.Print(sb);
            sb.Append(" ");
            set.Print(sb);
        }
    }
}
