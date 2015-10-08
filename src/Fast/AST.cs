using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using QUT.Gppg;

namespace Microsoft.Fast.AST
{
    public abstract class Ast
    {
        public abstract void PrettyPrint(StringBuilder sb);

        protected string _str_ = null;

        public override string ToString()
        {
            if (_str_ == null)
            {
                StringBuilder sb = new StringBuilder();
                PrettyPrint(sb);
               _str_= sb.ToString();
            }
            return _str_;
        }
    }

    public class FastToken : Ast
    {
        public readonly string text;
        public int line
        {
            get
            {
                return location.StartLine;
            }
        }
        public int position
        {
            get
            {
                return location.StartColumn;
            }
        }

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

        FastLexLocation location;
        internal FastLexLocation Location
        {
            get
            {
                return location;
            }
        }

        Tokens kind;
        internal Tokens Kind
        {
            get { return kind; }
        }


        internal FastToken(string text, FastLexLocation loc, Tokens kind)
        {
            this.text = text;
            this.location = loc;
            this.kind = kind;
        }

        internal FastToken(string name, Tokens kind)
        {
            this.text = name;
            this.location = new FastLexLocation();
            this.kind = kind;
        }

        internal FastToken(string name)
        {
            this.text = name;
            this.location = new FastLexLocation();
            this.kind = (Tokens)(-1);
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append(text);
        }

        public override string ToString()
        {
            return text;
        }

        public bool TryGetInt(out int k)
        {
            if (text.StartsWith("0x"))
                return int.TryParse(text.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out k);
            else if (text.StartsWith("'"))
            {
                var v = NumericValue.ExtractValue(this);
                if (!(v is char))
                {
                    k = -1;
                    return false;
                }
                k = (int)((char)v);
                return true;
            }
            else
                return int.TryParse(text, out k);
        }

        public int ToInt()
        {
            int k = 0;
            if (TryGetInt(out k))
                return k;
            else
                throw new FastParseException(location, string.Format("cannot convert to integer: {0}", text));
        }
    }

    #region basic sorts

    public enum FastSortKind { Bool, Real, String, Int, Record, Function, Char, Tree }

    public class FastSort : Ast
    {
        FastSortKind _kind;
        protected FastToken _name;
        public FastSortKind kind
        {
            get
            {
                return _kind;
            }
        }
        public virtual FastToken name
        {
            get
            {
                return _name;
            }
        }

        public FastSort(FastToken name, FastSortKind kind)
        {
            this._name = name;
            this._kind = kind;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            name.PrettyPrint(sb);
        }

        internal static FastSort GetSort(FastToken sortName)
        {
            if (sortName.text == "real")
                return new FastSort(sortName, FastSortKind.Real);
            if (sortName.text == "bool")
                return new FastSort(sortName, FastSortKind.Bool);
            if (sortName.text == "string")
                return new FastSort(sortName, FastSortKind.String);
            if (sortName.text == "int")
                return new FastSort(sortName, FastSortKind.Int);
            if (sortName.text == "char")
                return new FastSort(sortName, FastSortKind.Char);

            return new FastSort(sortName, FastSortKind.Tree);
        }

        public static readonly FastSort Bool = new FastSort(new FastToken("bool", Tokens.ID), FastSortKind.Bool);
        public static readonly FastSort Real = new FastSort(new FastToken("real", Tokens.ID), FastSortKind.Real);
        public static readonly FastSort String = new FastSort(new FastToken("string", Tokens.ID), FastSortKind.String);
        public static readonly FastSort Int = new FastSort(new FastToken("int", Tokens.ID), FastSortKind.Int);
        public static readonly FastSort Char = new FastSort(new FastToken("char", Tokens.ID), FastSortKind.Char);

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            FastSort s = obj as FastSort;
            if (s == null)
                return false;
            return s.ToString().Equals(this.ToString());
        }
    }

    public class RecordSort : FastSort
    {
        public List<KeyValuePair<FastToken, FastSort>> fields = new List<KeyValuePair<FastToken, FastSort>>();
        public Dictionary<string, FastSort> fieldSorts = new Dictionary<string, FastSort>();

        public static readonly RecordSort Empty = new RecordSort(new KeyValuePair<FastToken, FastSort>[] { });

        public RecordSort(IEnumerable<KeyValuePair<FastToken, FastSort>> attrs) : base(null, FastSortKind.Record)
        {
            foreach (var attr in attrs)
            {
                if (fieldSorts.ContainsKey(attr.Key.text))
                    throw new FastParseException(attr.Key.Location, string.Format("Duplicate attribute ID '{0}'", attr.Key.text));
                fieldSorts[attr.Key.text] = attr.Value;
                fields.Add(attr);
            }
            _name = new FastToken(this.ToString(), Tokens.LBRACKET);
        }

        public FastSort getSort(FastToken id)
        {
            if (!fieldSorts.ContainsKey(id.text))
                throw new FastParseException(id.Location, string.Format("Undefined attribute field '{0}'", id.text));

            return (fieldSorts[id.text]);
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            if (this._str_ != null)
                sb.Append(_str_);
            else
            {
                if (fields.Count > 1)
                    sb.Append("(");
                for (int i = 0; i < fields.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    fields[i].Value.PrettyPrint(sb);
                }
                if (fields.Count > 1)
                    sb.Append(")");
            }
        }
    }

    public class FunctionSort : FastSort
    {
        public List<FastSort> domain;
        public FastSort range;

        public FunctionSort(FastToken name, List<FastSort> domain, FastSort range)
            : base(name, FastSortKind.Function)
        {
            this.domain = domain; 
            this.range = range;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            if (domain.Count > 1)
                sb.Append("(");
            for (int i = 0; i < domain.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");
                domain[i].PrettyPrint(sb);
            }
            if (domain.Count > 1)
                sb.Append(")");
            sb.Append("->");
            range.PrettyPrint(sb);
        }

    }

    #endregion

    #region function symbols

    public class FuncSymbol : Ast
    {
        FastToken n;
        public FastToken name
        {
            get
            {
                return n;
            }
        }
        public int arity
        {
            get
            {
                return k;
            }
        }
        internal bool isConstructor;
        public bool IsConstructor
        {
            get { return isConstructor; }
        }
        int k;

        bool isAcceptor;
        public bool IsAcceptor
        {
            get { return isAcceptor; }
        }

        bool isTransducer;
        public bool IsTransducer
        {
            get { return isTransducer; }
        }


        public FuncSymbol(FastToken n, int k, bool isConstructor = false, bool isAcceptor = false, bool isTransducer = false)
        {
            this.n = n;
            this.k = k;
            this.isConstructor = isConstructor;
            this.isAcceptor = isAcceptor;
            this.isTransducer = isTransducer;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            name.PrettyPrint(sb);
        }

        public bool IsBuiltinBooleanConnective
        {
            get
            {
                return
                    name.Kind == Tokens.AND ||
                    name.Kind == Tokens.OR ||
                    name.Kind == Tokens.IMPLIES ||
                    name.Kind == Tokens.NOT;
            }
        }

        public bool IsBuiltinAssociative
        {
            get
            {
                return
                   name.Kind == Tokens.PLUS ||
                   name.Kind == Tokens.TIMES ||
                   name.Kind == Tokens.AND ||
                   name.Kind == Tokens.OR ||
                   name.Kind == Tokens.INTERSECT ||
                   name.Kind == Tokens.UNION ||
                   name.Kind == Tokens.BVAND ||
                   name.Kind == Tokens.BAR;
            }
        }

        public bool IsBuiltinUnary
        {
            get
            {
                return
                   name.Kind == Tokens.NOT ||
                   name.Kind == Tokens.BVNOT ||
                   name.Kind == Tokens.COMPLEMENT;
            }
        }

        public bool IsBuiltinITE
        {
            get
            {
                return name.Kind == Tokens.ITE;
            }
        }

        public bool IsBuiltinEq
        {
            get { return name.Kind == Tokens.EQ; }
        }

        public bool IsBuiltinArithmeticRelation
        {
            get
            {
                return
                    name.Kind == Tokens.EQ ||
                    name.Kind == Tokens.LE ||
                    name.Kind == Tokens.GE ||
                    name.Kind == Tokens.LT ||
                    name.Kind == Tokens.GT ||
                    name.Kind == Tokens.NE;
            }
        }

        public bool IsBuiltinArithmeticFunction
        {
            get
            {
                return
                    name.Kind == Tokens.PLUS ||
                    name.Kind == Tokens.TIMES ||
                    name.Kind == Tokens.MINUS ||
                    name.Kind == Tokens.DIV;
            }
        }

        public bool IsBuiltinIntegerFunction
        {
            get
            {
                return
                    name.Kind == Tokens.SHL ||
                    name.Kind == Tokens.SHR ||
                    name.Kind == Tokens.BVNOT ||
                    name.Kind == Tokens.BVAND ||
                    name.Kind == Tokens.BVXOR ||
                    name.Kind == Tokens.BAR ||
                    name.Kind == Tokens.MOD;
            }
        }

        public bool IsBuiltinStringFunction
        {
            get
            {
                return
                    name.text == "concat";
            }
        }

        AlphabetDef _alph;
        internal AlphabetDef alph
        {
            get
            {
                return _alph;
            }
            set
            {
                _alph = value;
            }
        }
    }

    #endregion

    #region exressions

    public enum FExpKind { Var, Value, App }

    public abstract class FExp : Ast
    {
        public abstract FastSort sort { get; }
        public abstract FExpKind kind { get; }
        public abstract FastToken token { get; }

        internal static FExp MkId(FastToken token)
        {

            string[] parts = token.text.Split('.');

            //This is the case X.Y
            if (parts.Length == 2)
            {
                var succ = Regex.Match(parts[0], @"^[A-Z]");
                if (succ.Success)
                {
                    //X starts with a capital letter, then this is an enum
                    return new EnumValue(token);
                }
                else
                {
                    //Otherwise we are accessing the attribute of the current node
                    return new AttributeVariable(token);
                }
            }
            else
            {
                var res = new Variable(token);
                return res;
            }
        }

        internal abstract void CheckFunctionExpr(Dictionary<string, FastSort> vars, HashSet<string> attrNames, HashSet<string> patternTrees, FastPgm pgm);

        internal abstract void CheckSubtreeGuard(Dictionary<string, FastSort> subs, AlphabetDef alph, FastPgm pgm);

        internal abstract void CheckTransformation(HashSet<string> subs, AlphabetDef domAlph, AlphabetDef rangeAlph, FastPgm pgm); 

        internal abstract void CheckSort(FastSort s);
         
        internal abstract void CalcSort(Func<FastToken, FastSort> context, FastPgm program);

        internal virtual void SetConstructorSort(AlphabetDef alph) 
        {
        }
    }

    #region values

    public abstract class Value : FExp
    {
        public object val;

        public override FExpKind kind
        {
            get { return FExpKind.Value; }
        }

        FastToken _token;

        protected Value(object val, FastToken token)
        {
            this.val = val;
            this._token = token;
        }

        public override FastToken token
        {
            get { return _token; }
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            token.PrettyPrint(sb);
        }

        internal override void CheckFunctionExpr(Dictionary<string, FastSort> vars, HashSet<string> attrNames, HashSet<string> patternTrees, FastPgm pgm) { }

        internal override void CheckSubtreeGuard(Dictionary<string, FastSort> subs, AlphabetDef alph, FastPgm pgm) { }

        internal override void CheckTransformation(HashSet<string> subs, AlphabetDef domAlph, AlphabetDef rangeAlph, FastPgm pgm) { }

        internal override void CheckSort(FastSort expected_sort)
        {
            if (!expected_sort.Equals(sort))
                throw new FastParseException(token.Location, string.Format("wrong sort '{0}', expecting '{1}'", sort, expected_sort));
        }
    }

    public class StringValue : Value
    {
        public StringValue(FastToken token)
            : base(getLiteral(token.text), token)
        {
        }
        public override FastSort sort
        {
            get { return FastSort.String; }
        }
        static string getLiteral(string s)
        {
            if (s.StartsWith("@"))
                return s.Substring(2, s.Length - 3); //remove '@"' from beginning and '"' from end
            else
                return System.Text.RegularExpressions.Regex.Unescape(s.Substring(1, s.Length - 2)); //remove '"' from beginning and '"' from end
        }

        internal override void CalcSort(Func<FastToken, FastSort> context, FastPgm program)
        {
        }

    }

    public class NumericValue : Value
    {
        public NumericValue(FastToken token)
            : base(ExtractValue(token), token)
        {
        }
        public override FastSort sort
        {
            get
            {
                if (token.text.StartsWith("'"))
                    return FastSort.Char;
                return ((token.text.Contains(".") || token.text.Contains("/")) ? FastSort.Real : FastSort.Int);
            }
        }

        public static object ExtractValue(FastToken token)
        {
            if (token.text.StartsWith("'"))
            {
                string x = token.text.Substring(1, token.text.Length - 2); //remove single quotes
                string xv = Regex.Unescape(x);
                if (xv.Length != 1)
                    throw new FastParseException(token.Location, string.Format("invalid character in '{0}'", token.text)); ;
                return xv[0];
            }
            if (token.text.StartsWith("\""))
            {
                string x = token.text.Substring(1, token.text.Length - 2); //escaped string, remove double quotes
                string v = Regex.Unescape(x);
                return v;
            }
            if (token.text.StartsWith("@"))
            {
                string x = token.text.Substring(2, token.text.Length - 3); //literal string, remove @ and double quotes
                return x;
            }

            int k;
            if (token.TryGetInt(out k))
                return k;
            else
                return token.text;
        }

        internal override void CalcSort(Func<FastToken, FastSort> context, FastPgm program)
        {
        }
    }

    public class BoolValue : Value
    {
        public static readonly BoolValue True = new BoolValue(new FastToken("true",Tokens.TRUE));
        public static readonly BoolValue False = new BoolValue(new FastToken("false", Tokens.FALSE));
        
        public BoolValue(FastToken token)
            : base(token.Kind == Tokens.TRUE ? true : false, token)
        {
        }
        public override FastSort sort
        {
            get { return FastSort.Bool; }
        }

        internal override void CalcSort(Func<FastToken, FastSort> context, FastPgm program)
        {
        }
    }

    public class EnumValue : Value
    {
        public FastSort _sort;
        public EnumValue(FastToken token)
            : base(GetEnumValue(token), token)
        {
            this._sort = new FastSort(new FastToken(GetEnumName(token)), FastSortKind.Tree);
        }

        static string GetEnumName(FastToken token)
        {
            string[] split = token.text.Split('.');
            return split[0];
        }

        static string GetEnumValue(FastToken token)
        {
            string[] split = token.text.Split('.');
            return split[1];
        }

        public override FastSort sort
        {
            get { return _sort; }
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            _sort.PrettyPrint(sb);
            sb.Append(".");
            sb.Append(val);
        }

        public override FExpKind kind
        {
            get
            {
                return FExpKind.Value;
            }
        }

        internal override void CheckFunctionExpr(Dictionary<string, FastSort> vars, HashSet<string> attrNames, HashSet<string> patternTrees, FastPgm pgm)
        {
            EnumDef def = pgm.FindEnumDef(_sort.name);
            if (!def.elems.Contains((string)val))
                throw new FastParseException(_sort.name.Location, string.Format("unspecified enum value '{0}.{1}'", sort.name, val));
        }

        internal override void CalcSort(Func<FastToken, FastSort> context, FastPgm program)
        {
            string[] split = token.text.Split('.');
            Def d;
            if (!program.defsMap.TryGetValue(split[0], out d))
                throw new FastParseException(token.Location, string.Format("Undefined Enum '{0}'", split[0]));
            EnumDef ed = d as EnumDef;
            if (ed == null)
                throw new FastParseException(token.Location, string.Format("ID '{0}' is not an Enum", split[0]));
            if (!ed.elems.Contains(split[1]))
                throw new FastParseException(token.Location, string.Format("Enum '{0}' does not contain member '{1}'", split[0], split[1]));
            _sort = ed.sort;
        }
    }

    #endregion

    public class Variable : FExp
    {
        protected FastSort _sort;

        public override FastSort sort
        {
            get { return _sort; }
        }

        public override FExpKind kind
        {
            get { return FExpKind.Var; }
        }

        internal Variable() { }

        protected FastToken _token;
        public Variable(FastToken token)
        {
            this._token = token;
            _sort = null;
        }

        public override FastToken token
        {
            get { return _token; }
        }

        public override string ToString()
        {
            return token.text;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            token.PrettyPrint(sb);
        }

        internal override void CheckFunctionExpr(Dictionary<string, FastSort> vars, HashSet<string> attrNames, HashSet<string> patternTrees, FastPgm pgm)
        {
            if (!vars.ContainsKey(token.text))
                throw new FastParseException(token.Location, string.Format("unexpected identifier '{0}'", token.text));

            if (_sort == null)
                _sort = vars[token.text];
            else if (!_sort.name.text.Equals(vars[token.text].name))
                throw new FastParseException(token.Location, string.Format("unexpected sort '{1}' of '{0}' expecting '{2}'", token.text, _sort.name, vars[token.text].name));
        }

        internal override void CheckSubtreeGuard(Dictionary<string, FastSort> subs, AlphabetDef alph, FastPgm pgm)
        {
            if (!subs.ContainsKey(token.text))
                throw new FastParseException(token.Location, string.Format("unexpected identifier '{0}', expecting a subtree parameter", token.text));
            else
                _sort = subs[token.text];
        }

        internal override void CheckTransformation(HashSet<string> subs, AlphabetDef domAlph, AlphabetDef rangeAlph, FastPgm pgm)
        {
            if (subs.Contains(this.token.text))
            {
                _sort = domAlph.sort;
            }
            else
            {
                //Check if the current variable is a constant
                FastSort s;
                foreach (var def in pgm.defs)
                {
                    if (def.kind == DefKind.Const && ((ConstDef)def).id.text == this.token.text)
                    {
                        s = ((ConstDef)def).sort;
                        if (s == null || (_sort != null && s.name.text != _sort.name.text))
                            throw new FastParseException(token.Location, string.Format("unexpected identifier '{0}'", token.text));
                        _sort = s;
                        return;
                    }
                }

                //Oterwise look for attr symbol

                if (Array.Exists(rangeAlph.symbols.ToArray(), x => x.name.text == this.token.text))
                    throw new FastParseException(this.token.Location, string.Format("the constructor '{0}' is not applied to a tuple", this.token.text));

                if (domAlph == null)
                    throw new FastParseException(this.token.Location, string.Format("the variable '{0}' is not defined", this.token.text));


                s = domAlph.attrSort.getSort(this.token);

                if (s == null || (_sort != null && s.name.text != _sort.name.text))
                    throw new FastParseException(token.Location, string.Format("unexpected identifier '{0}'", token.text));
                _sort = s;
            }
        }

        internal override void CheckSort(FastSort s)
        {
            if (!sort.Equals(s))
                throw new FastParseException(_token.Location, string.Format("wrong sort '{0}', expecting '{1}'", sort.name.text, s.name.text));
        }

        internal override void CalcSort(Func<FastToken, FastSort> context, FastPgm program)
        {
            if (program.defsMap.ContainsKey(this._token.text))
                _sort = program.GetConstantSort(this._token);
            else
                _sort = context(this._token);
        }
    }

    public class AttributeVariable : Variable
    {
        FastToken treeNodeName;

        public override FExpKind kind
        {
            get { return FExpKind.Var; }
        }

        //FastToken _token;
        public AttributeVariable(FastToken token)
        {
            var strs = token.text.Split('.');
            this.treeNodeName = new FastToken(strs[0], token.Location, token.Kind);
            _token = new FastToken(strs[1], token.Location, token.Kind);
        }

        public override string ToString()
        {
            return treeNodeName.text + "." + token.text;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            token.PrettyPrint(sb);
        }

        internal override void CheckFunctionExpr(Dictionary<string, FastSort> vars, HashSet<string> attrNames, HashSet<string> patternTrees, FastPgm pgm)
        {
            //Check whether the current tree name is bounded
            if (!patternTrees.Contains(this.treeNodeName.text))
                throw new FastParseException(treeNodeName.Location, string.Format("unexpected tree identifier '{0}'", treeNodeName.text));

            //Check whether the current tree name has any of these attributes
            if (!attrNames.Contains(token.text))
                throw new FastParseException(token.Location, string.Format("the tree '{0}' does not have an attribute '{0}'", treeNodeName.text, token.text));

            if (_sort == null)
                _sort = vars[token.text];
            else if (!_sort.name.text.Equals(vars[token.text].name))
                throw new FastParseException(token.Location, string.Format("unexpected sort '{1}' of '{0}', expecting '{2}'", token.text, _sort.name, vars[token.text].name));
        }

        internal override void CheckSubtreeGuard(Dictionary<string, FastSort> subs, AlphabetDef alph, FastPgm pgm)
        {
            //Double check
            if (!subs.ContainsKey(token.text))
                throw new FastParseException(token.Location, string.Format("unexpected identifier '{0}', expecting a subtree parameter", token.text));
            else
                _sort = subs[token.text];
        }

        internal override void CheckTransformation(HashSet<string> subs, AlphabetDef domAlph, AlphabetDef rangeAlph, FastPgm pgm)
        {
            if (subs.Contains(this.token.text))
            {
                _sort = domAlph.sort;
            }
            else
            {
                //Check if the current variable is a constant
                FastSort s;
                foreach (var def in pgm.defs)
                {
                    if (def.kind == DefKind.Const && ((ConstDef)def).id.text == this.token.text)
                    {
                        s = ((ConstDef)def).sort;
                        if (s == null || (_sort != null && s.name.text != _sort.name.text))
                            throw new FastParseException(token.Location, string.Format("unexpected identifier '{0}'", token.text));
                        _sort = s;
                        return;
                    }
                }

                //Oterwise look for attr symbol

                if (Array.Exists(rangeAlph.symbols.ToArray(), x => x.name.text == this.token.text))
                    throw new FastParseException(this.token.Location, string.Format("the constructor '{0}' is not applied to a tuple", this.token.text));

                if (domAlph == null)
                    throw new FastParseException(this.token.Location, string.Format("the variable '{0}' is not defined", this.token.text));


                s = domAlph.attrSort.getSort(this.token);

                if (s == null || (_sort != null && s.name.text != _sort.name.text))
                    throw new FastParseException(token.Location, string.Format("unexpected identifier '{0}'", token.text));
                _sort = s;
            }
        }

        internal override void CalcSort(Func<FastToken, FastSort> context, FastPgm program)
        {
            var alph = context(treeNodeName);

            if (alph.kind != FastSortKind.Tree)
                throw new FastParseException(this.treeNodeName.Location, string.Format("Invalid input tree variable '{0}'", this.treeNodeName.text));

            var alphdef = program.FindAlphabetDef(alph.name);

            var s = alphdef.attrSort.getSort(_token);
            _sort = s;
        }
    }

    public class AppExp : FExp   
    {
        public FuncSymbol func;
        public List<FExp> args;
        internal FastSort _sort;
        bool isTranDef;
        bool isLangDef;
        public bool IsTranDef {get {return isTranDef;}} 
        public bool IsLangDef {get {return IsLangDef;}}

        public AppExp(FastToken func, IEnumerable<FExp> args)
        {
            this.args = new List<FExp>(args);
            this.func = new FuncSymbol(func, this.args.Count);
            this._sort = null;
            this.isTranDef = false;
            this.isLangDef = false;
        }

        public override FastToken token
        {
            get { return func.name; }
        }

        public override FastSort sort
        {
            get
            {
                return _sort;
            }
        }

        public override FExpKind kind
        {
            get { return FExpKind.App; }
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            foreach (var arg in args)
            {
                sb.Append(" ");
                arg.PrettyPrint(sb);
            }
            sb.Append(")");
        }

        internal override void CheckFunctionExpr(Dictionary<string, FastSort> vars, HashSet<string> attrNames, HashSet<string> patternTrees, FastPgm pgm)
        {
            foreach (var expr in args)
                expr.CheckFunctionExpr(vars, attrNames, patternTrees, pgm);

            //check standard operations, allow no other ops
            bool ok = CheckStandardOps(vars);
            if (!ok)
                throw new FastParseException(func.name.Location, string.Format("unknown function '{0}'", func.name));
        }

        private bool CheckStandardOps(Dictionary<string, FastSort> varsConstsSorts)
        {
            Predicate<FExp> IsNotBool = (x => { return x.sort == null || x.sort.kind != FastSortKind.Bool; });
            if (func.IsBuiltinBooleanConnective)
            {
                if (args.Exists(IsNotBool))
                    throw new FastParseException(func.name.Location, string.Format("all arguments of '{0}' must be Boolean", func.name));
                if (func.IsBuiltinUnary && args.Count != 1)
                    throw new FastParseException(func.name.Location, string.Format("'{0}' is unary, not arity {1}", func.name, args.Count));
                if (!func.IsBuiltinAssociative && !func.IsBuiltinUnary && args.Count != 2)
                    throw new FastParseException(func.name.Location, string.Format("'{0}' is binary, not arity {1}", func.name, args.Count));

                _sort = FastSort.Bool;
            }
            else if (func.IsBuiltinITE)
            {
                if (args.Count != 3)
                    throw new FastParseException(func.name.Location, string.Format("'{0}' takes 3 arguments, wrong nr of arguments {1}", func.name, args.Count));
                if (args[0].sort.kind != FastSortKind.Bool)
                    throw new FastParseException(func.name.Location, string.Format("first argument of '{0}' must be Boolean not '{1}'", func.name, args[0].sort.name));
                if (args[1].sort.name.text != args[2].sort.name.text)
                    throw new FastParseException(func.name.Location, string.Format("arguments 2 and 3 of '{0}' must have the same type, not {1},{2}", func.name, args[1].sort.name.text, args[2].sort.name.text));

                _sort = args[1].sort;
            }
            else if (func.IsBuiltinEq || func.IsBuiltinArithmeticRelation)
            {
                if (args.Count != 2)
                    throw new FastParseException(func.name.Location, string.Format("'{0}' is binary, wrong nr of arguments {1}", func.name, args.Count));

                if (!args[0].sort.name.text.Equals(args[1].sort.name.text))
                    throw new FastParseException(func.name.Location, string.Format("both arguments of '{0}' must have the same type, not {1},{2}", func.name, args[0].sort.name.text, args[1].sort.name.text));

                _sort = FastSort.Bool;
            }
            else if (func.IsBuiltinArithmeticFunction)
            {
                if (args.Count != 2)
                    throw new FastParseException(func.name.Location, string.Format("'{0}' is binary, wrong nr of arguments {1}", func.name, args.Count));

                if (!args[0].sort.name.text.Equals(args[1].sort.name.text))
                    throw new FastParseException(func.name.Location, string.Format("both arguments of '{0}' must have the same sort, not {1},{2}", func.name, args[0].sort.name.text, args[1].sort.name.text));

                if (!args[0].sort.name.text.Equals(FastSort.Real.name.text) && !args[0].sort.name.text.Equals(FastSort.Int.name.text) && !args[0].sort.name.text.Equals(FastSort.Char.name.text))
                    throw new FastParseException(func.name.Location, string.Format("arguments of '{0}' must be either 'int' or 'real' or 'char', not '{1}',", func.name, args[0].sort.name.text));

                _sort = args[0].sort;
            }
            else if (func.IsBuiltinIntegerFunction)
            {
                if (args.Count != 2)
                    throw new FastParseException(func.name.Location, string.Format("'{0}' is binary, wrong nr of arguments {1}", func.name, args.Count));

                if (!args[0].sort.name.text.Equals("int"))
                    throw new FastParseException(func.name.Location, string.Format("both arguments of '{0}' must have the same sort, not int,{2}", func.name, args[0].sort.name.text, args[1].sort.name.text));

                _sort = args[0].sort;
            }
            else if (func.IsBuiltinStringFunction)
            {
                if (args.Count != 2)
                    throw new FastParseException(func.name.Location, string.Format("'{0}' is binary, wrong nr of arguments {1}", func.name, args.Count));

                if (!args[0].sort.name.text.Equals("string") || !args[1].sort.name.text.Equals("string"))
                    throw new FastParseException(func.name.Location, string.Format("both arguments of '{0}' must have the same sort, not string,{2}", func.name, args[0].sort.name.text, args[1].sort.name.text));

                _sort = args[0].sort;
            }
            else if (varsConstsSorts.ContainsKey(func.name.text))
            {
                FunctionSort fsort = (FunctionSort)varsConstsSorts[func.name.text];
                if (args.Count != fsort.domain.Count)
                    throw new FastParseException(func.name.Location, string.Format("'{0}' expects '{1}' arguments, but it is applied to {2}", func.name, fsort.domain.Count, args.Count));

                int i = 0;
                foreach (var v in args)
                {
                    if (!v.sort.name.text.Equals(fsort.domain[i].name.text))
                        throw new FastParseException(func.name.Location, string.Format("argument '{0}' of '{1}' must have sort '{2}', not '{3}'", i, func.name, fsort.domain[i], v.sort));
                    i++;
                }

                _sort = fsort.range;
            }
            else
            {
                return false;
            }
            return true;
        }

        internal override void CheckSubtreeGuard(Dictionary<string, FastSort> subs, AlphabetDef alph, FastPgm pgm)
        {
            Predicate<FExp> IsNotBool = (x => { return x.sort == null || x.sort.kind != FastSortKind.Bool; });
            foreach (var expr in args)
                expr.CheckSubtreeGuard(subs, alph, pgm);

            if (func.name.text != "and" && func.name.text != "or" && !pgm.defs.Exists(d => d.id.text == func.name.text && d.kind == DefKind.Lang && ((LangDef)d).domain.name.text == alph.sort.name.text))
                throw new FastParseException(func.name.Location, string.Format("illeagal identifier '{0}'", func.name.text));

            if ((func.name.text == "and" || func.name.text == "or") && args.Exists(IsNotBool))
                throw new FastParseException(func.name.Location, string.Format("arguments of '{0}' must be Boolean", func.name.text));

            if (pgm.defs.Exists(d => d.id.text == func.name.text && d.kind == DefKind.Lang && ((LangDef)d).domain.name.text == alph.sort.name.text))
                isLangDef = true;

            _sort = FastSort.Bool;
        }

        internal override void CheckTransformation(HashSet<string> subs, AlphabetDef domAlph, AlphabetDef rangeAlph, FastPgm pgm)
        {
            foreach (var expr in args)
                expr.CheckTransformation(subs, domAlph, rangeAlph, pgm);

            var constsAndFuns = new Dictionary<string, FastSort>();
            foreach (var def in pgm.defs)
            {
                if (def.kind == DefKind.Function)
                    constsAndFuns.Add(((FunctionDef)def).name.text, ((FunctionDef)def).sort);
                if (def.kind == DefKind.Const)
                    constsAndFuns.Add(((ConstDef)def).name.text, ((ConstDef)def).sort);
            }
            bool ok = CheckStandardOps(constsAndFuns);

            if (!ok)
            {
                string f = func.name.text;
                if (this is RecordExp)                                 // --- record constrctor ---
                {
                    if (rangeAlph.attrSort.fields.Count != args.Count)
                        throw new FastParseException(func.name.Location, string.Format("unxecpected nr of attribute fields {0}, expecting {1}", args.Count, rangeAlph.attrSort.fields.Count));
                    for (int i = 0; i < args.Count; i++)
                    {
                        if (args[i].sort.name.text != rangeAlph.attrSort.fields[i].Value.name.text)
                            throw new FastParseException(args[i].sort.name.Location, string.Format("invalid argument sort '{0}' of field '{1}', expecting sort '{2}'", args[i].sort.name.text, rangeAlph.attrSort.fields[i].Key.text, rangeAlph.attrSort.fields[i].Value.name.text));
                    }
                    _sort = rangeAlph.attrSort;
                }
                else if (rangeAlph.symbols.Exists(_f => func.name.text == _f.name.text)) // --- tree constructor ---
                {
                    if (!rangeAlph.IsValidSymbol(func.name, func.arity))
                    {
                        throw new FastParseException(func.name.Location, string.Format("wrong number of arguments of constructor '{0}'", func.name));
                    }

                    for (int i = 1; i < args.Count; i++)
                    {
                        if (args[i].sort.name.text != rangeAlph.id.text)
                            throw new FastParseException(func.name.Location, string.Format("unexected argument of function '{0}'", func.name));
                    }
                    _sort = rangeAlph.sort;
                }
                else
                {
                    var def = pgm.FindDef(func.name);

                    if (def.kind == DefKind.Trans)
                    {
                        if (args.Count != 1)
                            throw new FastParseException(func.name.Location, string.Format("transduction '{0}' is unary", func.name));

                        var tdef = def as TransDef;
                        if (tdef.domain.name.text != args[0].sort.name.text)
                            throw new FastParseException(args[0].sort.name.Location, string.Format("transduction '{0}' has unexpected argument of sort '{1}', expecting sort '{2}'", func.name, args[0].sort.name.text, tdef.domain.name.text));

                        _sort = tdef.range;
                        isTranDef = true;
                        if (args[0].kind == FExpKind.App)
                            throw new FastParseException(func.name.Location, string.Format("Transduction '{0}' cannot be nested inside another Transduction", func.name));
                    }
                    else
                    {
                        throw new FastParseException(func.name.Location, string.Format("ID '{0}' is not a Transduction", func.name));
                    }
                }
            }
        }

        internal override void CheckSort(FastSort s)
        {
            if (!sort.Equals(s))
                throw new FastParseException(token.Location, string.Format("wrong sort '{0}', expecting '{1}'", sort, s));
        }

        internal override void CalcSort(Func<FastToken, FastSort> context, FastPgm program) 
        {
            foreach (var arg in this.args)
                arg.CalcSort(context, program);

            if (func.alph != null) //func is a costructor
            {
                if (args.Count != func.arity)
                    throw new FastParseException(func.name.Location,
                        string.Format("Invalid use of constructor '{0}' of alphabet '{1}', wrong nr of arguments", func.name.text, func.alph.sort));

                if (!args[0].sort.Equals(func.alph.attrSort))
                    throw new FastParseException(func.name.Location,
                         string.Format("Invalid use of constructor '{0}' of alphabet '{1}', wrong attribute sort", func.name.text, func.alph.sort));

                for (int i = 1; i < args.Count; i++)
                    if (!args[i].sort.Equals(func.alph.sort))
                        throw new FastParseException(func.name.Location,
                    string.Format("Invalid use of constructor '{0}' of alphabet '{1}', subtree nr {2} has unexpected sort '{3}'", func.name.text, func.alph.sort, i, args[i].sort));


                _sort = func.alph.sort;
                return;
            }

            switch (func.name.Kind)
            {
                case (Tokens.EQ):
                case (Tokens.NE):
                    {
                        if (this.args.Count != 2)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 2 arguments");
                        if (!args[0].sort.name.text.Equals(args[1].sort.name.text))
                            throw new FastParseException(func.name.Location, "Both arguments must have the same sort");
                        _sort = FastSort.Bool;
                        break;
                    }
                case (Tokens.LE):
                case (Tokens.GE):
                case (Tokens.LT):
                case (Tokens.GT):
                    {
                        if (this.args.Count != 2)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 2 arguments");
                        if (!args[0].sort.name.text.Equals(args[1].sort.name.text))
                            throw new FastParseException(func.name.Location, "Both arguments must have the same sort");
                        if (!(args[0].sort.kind == FastSortKind.Char || args[0].sort.kind == FastSortKind.Int || args[0].sort.kind == FastSortKind.Real))
                            throw new FastParseException(func.name.Location, "Arguments have wrong sort, expecting numeric sort");
                        _sort = FastSort.Bool;
                        break;
                    }
                case (Tokens.AND):
                case (Tokens.OR):
                case (Tokens.IMPLIES):
                    {
                        if (this.args.Count != 2)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 2 arguments");
                        if (!args[0].sort.name.text.Equals(args[1].sort.name.text))
                            throw new FastParseException(func.name.Location, "Both arguments must have the same sort");
                        if (!(args[0].sort.kind == FastSortKind.Bool))
                            throw new FastParseException(func.name.Location, "Arguments must be Boolean");
                        _sort = FastSort.Bool;
                        break;
                    }
                case (Tokens.PLUS):
                case (Tokens.MINUS):
                case (Tokens.TIMES):
                case (Tokens.DIV):
                    {
                        if (this.args.Count != 2)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 2 arguments");
                        if (!args[0].sort.name.text.Equals(args[1].sort.name.text))
                            throw new FastParseException(func.name.Location, "Both arguments must have the same sort");
                        if (!(args[0].sort.kind == FastSortKind.Char || args[0].sort.kind == FastSortKind.Int || args[0].sort.kind == FastSortKind.Real))
                            throw new FastParseException(func.name.Location, "Wrong argument sorts");
                        _sort = args[0].sort;
                        break;
                    }
                case (Tokens.MOD):
                case (Tokens.SHL):
                case (Tokens.SHR):
                case (Tokens.BVAND):
                case (Tokens.BAR):
                case (Tokens.BVXOR):
                    {
                        if (this.args.Count != 2)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 2 arguments");
                        if (!args[0].sort.name.text.Equals(args[1].sort.name.text))
                            throw new FastParseException(func.name.Location, "Both arguments must have the same sort");
                        if (!(args[0].sort.kind == FastSortKind.Char || args[0].sort.kind == FastSortKind.Int))
                            throw new FastParseException(func.name.Location, "Wrong argument sorts");
                        _sort = args[0].sort;
                        break;
                    }
                case (Tokens.NOT):
                    {
                        if (this.args.Count != 1)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 1 argument");
                        if (!(args[0].sort.kind == FastSortKind.Bool))
                            throw new FastParseException(func.name.Location, string.Format("Wrong argument sort, expecting '{0}'",FastSort.Bool.name.text));
                        _sort = args[0].sort;
                        break;
                    }
                case (Tokens.BVNOT):
                    {
                        if (this.args.Count != 1)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 1 argument");
                        if (!(args[0].sort.kind == FastSortKind.Char || args[0].sort.kind == FastSortKind.Int))
                            throw new FastParseException(func.name.Location, "Wrong argument sort, expecting 'char' or 'int'");
                        _sort = args[0].sort;
                        break;
                    }
                case (Tokens.COMPLEMENT):
                    {
                        if (this.args.Count != 1)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 1 argument");
                        if (!(args[0].sort.kind == FastSortKind.Tree))
                            throw new FastParseException(func.name.Location, "Wrong argument sort, expecting a tree sort");
                        _sort = args[0].sort;
                        break;
                    }
                case (Tokens.INTERSECT):
                case (Tokens.UNION):
                    {
                        if (this.args.Count != 2)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 2 arguments");
                        if (!(args[0].sort.kind == FastSortKind.Tree && args[1].sort.kind == FastSortKind.Tree))
                            throw new FastParseException(func.name.Location, "Wrong argument sort, expecting a tree sort");
                        if (!(args[0].sort.name.Equals(args[1].sort.name)))
                            throw new FastParseException(func.name.Location, "Both arguments must have the same tree sort");
                        _sort = args[0].sort;
                        break;
                    }
                case (Tokens.EQ_LANG):
                    {
                        if (this.args.Count != 2)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments, expecting 2 arguments");
                        if (!(args[0].sort.kind == FastSortKind.Tree && args[1].sort.kind == FastSortKind.Tree))
                            throw new FastParseException(func.name.Location, "Wrong argument sort, expecting a tree sort");
                        if (!(args[0].sort.name.Equals(args[1].sort.name)))
                            throw new FastParseException(func.name.Location, "Both arguments must have the same tree sort");
                        _sort = FastSort.Bool;
                        break;
                    }
                case (Tokens.ITE):
                    {
                        if (this.args.Count != 3)
                            throw new FastParseException(func.name.Location, "Wrong nr of arguments of If-Then-Else expression, expecting 3 arguments");
                        if (!(args[0].sort.kind == FastSortKind.Bool))
                            throw new FastParseException(func.name.Location, string.Format("If-Then-Else condition has wrong sort '{0}' expecting '{1}'", args[0].sort.name.text, FastSort.Bool.name.text));
                        if (!(args[1].sort.name.text.Equals(args[2].sort.name.text)))
                            throw new FastParseException(func.name.Location, string.Format("If-Then-Else true and false cases have different sorts '{0}' and '{1}' but must have the same sort", args[1].sort.name.text, args[2].sort.name.text));
                        _sort = args[1].sort;
                        break;
                    }
                case (Tokens.ID):
                    {
                        var fdef = program.FindDef(func.name);
                        var tdef = fdef as TransDef;
                        if (tdef != null)
                        {
                            if (1 != args.Count)
                                throw new FastParseException(func.name.Location, string.Format("Transduction '{0}' arity is 1 not {2}", func.name.text, 1, args.Count));

                            if (!args[0].sort.Equals(tdef.domain))
                                throw new FastParseException(func.name.Location, string.Format("Transduction '{0}' has domain '{0}' not '{1}'", func.name.text, tdef.domain, args[0].sort));

                            _sort = tdef.range;
                            isTranDef = true;
                        }
                        else
                        {
                            var ldef = fdef as LangDef;
                            if (ldef != null)
                            {
                                if (1 != args.Count)
                                    throw new FastParseException(func.name.Location, string.Format("Acceptor '{0}' arity is 1 not {2}", func.name.text, 1, args.Count));

                                if (!args[0].sort.Equals(ldef.domain))
                                    throw new FastParseException(func.name.Location, string.Format("Acceptor '{0}' has domain '{0}' not '{1}'", func.name.text, ldef.domain, args[0].sort));

                                _sort = FastSort.Bool;
                                isLangDef = true;
                            }
                            else
                            {

                                FunctionDef d = fdef as FunctionDef;
                                if (d == null)
                                    throw new FastParseException(func.name.Location, string.Format("Unexpected ID '{0}' ", func.name.text));

                                if (d.inputVariables.Count != args.Count)
                                    throw new FastParseException(func.name.Location, string.Format("Function '{0}' arity is {1} not {2}", func.name.text, d.inputVariables.Count, args.Count));

                                for (int i = 0; i < args.Count; i++)
                                {
                                    var expectedSort = d.inputVariables[i].Value;
                                    var actualSort = args[i].sort;
                                    if (expectedSort.kind != actualSort.kind || !expectedSort.name.text.Equals(actualSort.name.text))
                                        throw new FastParseException(func.name.Location, string.Format("Function '{0}' parameter (#{4}) '{3}' has sort '{1}' not '{2}'", func.name.text, d.inputVariables[i].Key.text, expectedSort.name.text, actualSort.name.text, i + 1));
                                }
                                _sort = d.outputSort;
                            }
                        }
                        break;
                    }
                case (Tokens.LBRACKET): //Record constructor
                    {
                        List<KeyValuePair<FastToken, FastSort>> sorts = new List<KeyValuePair<FastToken, FastSort>>();
                        for (int i = 0; i < args.Count; i++)
                            sorts.Add(new KeyValuePair<FastToken, FastSort>(new FastToken("_" + (1+i).ToString()), args[i].sort));
                        _sort = new RecordSort(sorts);
                        break;
                    }
                default:
                    throw new FastParseException(func.name.Location, string.Format("Unexpected function '{0}'", func.name.text));
            }
        }

        internal override void SetConstructorSort(AlphabetDef alph)
        {
            for (int i=0; i < args.Count; i++)
                args[i].SetConstructorSort(alph);

            if (func.name.Kind == Tokens.ID && alph.IsValidSymbol(func.name, func.arity))
            {
                func.alph = alph;
                func.isConstructor = true;
            }
        }
    }

    public class RecordExp : AppExp
    {
        public RecordExp(FastToken symbol, IEnumerable<FExp> args)
            : base(symbol, args)
        {
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("[");
            for (int i = 0; i < args.Count; i++)
            {
                args[i].PrettyPrint(sb);
                if (i < args.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("]");
        }
    }
    #endregion

    #region definitions

    public enum DefKind { Enum, Alphabet, Const, Lang, Trans, Def, Function, Query }

    public class EnumDef : Def
    {
        public FastToken name;
        public FastSort sort;
        public List<string> elems = new List<string>();

        public EnumDef(FastToken name)
            : base(DefKind.Enum)
        {
            this.name = name;
            this.sort = new FastSort(name, FastSortKind.Tree);
        }

        public override FastToken id
        {
            get { return name; }
        }

        public void Add(FastToken elem)
        {
            if (elems.Contains(elem.text))
                throw new FastParseException(elem.Location, string.Format("Enum element '{0}' already exists", elem.text));
            elems.Add(elem.text);
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("enum ");
            name.PrettyPrint(sb);
            sb.Append(" {");
            for (int i = 0; i < elems.Count; i++)
            {
                sb.Append(elems[i]);
                if (i < elems.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("}");
        }

        internal override void Typecheck(FastPgm pgm)
        {
            //correct by construction, in particular cannot be empty
        }
    }

    public class AlphabetDef : Def
    {
        public RecordSort attrSort;
        public List<FuncSymbol> symbols;
        public FastSort sort;
        FastToken name;
        Dictionary<string, FastSort> attrSortMap = new Dictionary<string, FastSort>();
        Dictionary<string, int> constructorRankMap = new Dictionary<string, int>();

        public AlphabetDef(FastToken name, IEnumerable<KeyValuePair<FastToken, FastToken>> attrs, IEnumerable<KeyValuePair<FastToken, FastToken>> constructors)
            : base(DefKind.Alphabet)
        {
            this.sort = new FastSort(name, FastSortKind.Tree);
            this.symbols = new List<FuncSymbol>();
            this.name = name;
            AddAttributes(attrs);
            AddConstructors(constructors);
        }

        public override FastToken id
        {
            get { return name; }
        }

        internal override void Typecheck(FastPgm pgm)
        {
            //correct by construction
        }

        void AddAttributes(IEnumerable<KeyValuePair<FastToken, FastToken>> attrs)
        {
            var attributeSorts = new List<KeyValuePair<FastToken, FastSort>>();
            foreach (var kv in attrs)
            {
                var attr = kv.Key;
                var sort = kv.Value;
                FastSort s = FastSort.GetSort(sort);
                if (attrSortMap.ContainsKey(attr.text))
                    throw new FastParseException(attr.Location, string.Format("Attribute '{0}' is already defined", attr.text));
                attrSortMap[attr.text] = s;
                attributeSorts.Add(new KeyValuePair<FastToken, FastSort>(attr, s));
            }
            attrSort = new RecordSort(attributeSorts);
        }

        void AddConstructors(IEnumerable<KeyValuePair<FastToken, FastToken>> contructors)
        {
            bool leafExists = false;
            foreach (var kv in contructors)
            {
                if (constructorRankMap.ContainsKey(kv.Key.text))
                    throw new FastParseException(kv.Key.Location, string.Format("Constructor '{0}' is already defined", kv.Key.text));
                var k = kv.Value.ToInt();
                constructorRankMap[kv.Key.text] = k;
                symbols.Add(new FuncSymbol(kv.Key, k + 1, true)); //arity is +1 because of the attribute record
                if (k == 0)
                    leafExists = true;
            }
            if (!leafExists)
                throw new FastParseException(name.Location, string.Format("Alphabet '{0}' has no leaves (0-rank members), it must have at least one leaf", name.text));
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("Alphabet ");
            sort.name.PrettyPrint(sb);
            attrSort.PrettyPrint(sb);
            sb.Append("{");
            for (int i = 0; i < symbols.Count; i++)
            {
                symbols[i].PrettyPrint(sb);
                sb.Append("(");
                sb.Append(symbols[i].arity - 1);
                sb.Append(")");
                if (i < symbols.Count - 1)
                    sb.Append(", ");
            }
            sb.Append("}");
        }

        internal bool IsValidSymbol(FastToken symb, int arity)
        {
            int rank;
            if (!constructorRankMap.TryGetValue(symb.text, out rank))
                return false;

            if (arity != rank + 1)
                return false;

            return true;
        }
    }

    public abstract class Def : Ast
    {
        public DefKind kind;
        public abstract FastToken id { get; }

        protected Def(DefKind kind)
        {
            this.kind = kind;
        }

        internal abstract void Typecheck(FastPgm pgm);
    }

    public class ConstDef : Def
    {
        public FastToken name;
        public FastSort sort;
        public FExp expr;

        public ConstDef(FastToken name, FastToken sort, FExp expr)
            : base(DefKind.Const)
        {
            this.name = name;
            this.sort = FastSort.GetSort(sort);
            this.expr = expr;
        }

        public override FastToken id
        {
            get { return name; }
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("const ");
            name.PrettyPrint(sb);
            sb.Append(" : ");
            sort.PrettyPrint(sb);
            sb.Append(" = ");
            expr.PrettyPrint(sb);
        }

        internal override void Typecheck(FastPgm pgm)
        {
            expr.CalcSort(x => { throw new FastParseException(x.Location, string.Format("unexpected ID '{0}'",x.text)); }, pgm);
            if (!expr.sort.name.text.Equals(this.sort.name.text))
                throw new FastParseException(expr.token.Location, string.Format("wrong sort '{0}' expecting '{1}'", expr.sort.name.text, sort.name.text));
        }
    }
     
    public abstract class LangOrTransDef : Def
    {
        public FuncSymbol func;
        public FuncSymbol treeMatchName;
        public FastSort domain;
        public List<GuardedExp> cases;
        public bool isPublic;
        public FastSort range;

        public void AddCase(GuardedExp gexpr)
        {
            cases.Add(gexpr);
        }

        protected LangOrTransDef(DefKind kind, FastSort domain)
            : base(kind)
        {
            this.domain = domain;
        }

        internal override void Typecheck(FastPgm pgm)
        {
            AlphabetDef adef = pgm.FindAlphabetDef(domain.name);
            foreach (var c in cases)
            {
                Func<FastToken, FastSort> context = x =>
                {
                    if (treeMatchName != null && x.text.Equals(this.treeMatchName.name.text))
                        return domain;
                    if (c.pat.Contains(x.text))
                        return adef.sort;
                    else return adef.attrSort.getSort(x);
                };

                if (!adef.IsValidSymbol(c.pat.symbol, c.pat.children.Count + 1))
                    throw new FastParseException(c.pat.symbol.Location, string.Format("Symbol '{0}' of rank {1} does not exist in alphabet '{2}'", c.pat.symbol.text, c.pat.children.Count, domain.name.text));

                c.where.CalcSort(context, pgm);
                foreach (var g in c.given)
                {
                    g.CalcSort(context, pgm);
                    if (g.sort.kind != FastSortKind.Bool)
                        throw new FastParseException(g.token.Location, string.Format("Wrong sort '{0}', expecting 'bool'", g.sort.name.text));
                } 

                if (c.to != null)
                {
                    if (this.range == null)
                        throw new FastException(FastExceptionKind.InternalError);

                    AlphabetDef range_adef = pgm.FindAlphabetDef(this.range.name);
                    //sets sorts of constructor symbols of the output alphabet
                    c.to.SetConstructorSort(range_adef);//???

                    c.to.CalcSort(context, pgm);
                    if (!range.name.text.Equals(c.to.sort.name.text))
                        throw new FastParseException(c.to.token.Location, string.Format("Wrong target sort '{0}', expecting '{1}'", c.to.sort.name.text, range.name.text));
                }
            }
        }

        public override FastToken id
        {
            get { return func.name; }
        }
    }

    public class LangDef : LangOrTransDef
    {
        public LangDef(FastToken acc, FastToken alphabet, FastToken patternMatchName)
            : base(DefKind.Lang, new FastSort(alphabet, FastSortKind.Tree))
        {
            this.func = new FuncSymbol(acc, 1, false, true);
            this.cases = new List<GuardedExp>();
            if (patternMatchName != null)
                this.treeMatchName = new FuncSymbol(patternMatchName, 0);
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("Lang ");
            func.name.PrettyPrint(sb);
            sb.Append(" : ");
            domain.name.PrettyPrint(sb);
            sb.AppendLine(" {");
            for (int i = 0; i < cases.Count; i++)
            {
                cases[i].PrettyPrint(sb);
                sb.AppendLine();
                if (i < cases.Count - 1)
                {
                    sb.Append("| ");
                }
            }
            sb.AppendLine("}");
        }
    }

    public class TransDef : LangOrTransDef
    {
        public TransDef(FastToken trans, FastToken domain, FastToken range, FastToken patternMatchName)
            : base(DefKind.Trans, new FastSort(domain, FastSortKind.Tree))
        {
            this.range = new FastSort(range, FastSortKind.Tree);
            this.func = new FuncSymbol(trans, 1, false, false, true);
            cases = new List<GuardedExp>();
            if (patternMatchName != null)
                this.treeMatchName = new FuncSymbol(patternMatchName, 0);
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("Trans ");
            func.PrettyPrint(sb);
            sb.Append(" : ");
            domain.PrettyPrint(sb);
            sb.Append(" -> ");
            range.PrettyPrint(sb);
            sb.AppendLine(" {");
            for (int i = 0; i < cases.Count; i++)
            {
                cases[i].PrettyPrint(sb);
                sb.AppendLine();
                if (i < cases.Count - 1)
                {
                    sb.Append("| ");
                }
            }
            sb.AppendLine("}");
        }
    }

    public class FunctionDef : Def
    {
        public FastToken name;
        public List<KeyValuePair<FastToken, FastSort>> inputVariables;
        public FastSort outputSort;
        public FExp expr;
        public FastSort sort;

        Dictionary<string, FastSort> varSortMap = new Dictionary<string,FastSort>();

        public FunctionDef(FastToken name, List<KeyValuePair<FastToken, FastSort>> vars, FastSort outputSort, FExp expr)
            : base(DefKind.Function)
        {
            this.name = name;
            this.outputSort = outputSort;
            this.expr = expr;

            inputVariables = vars;
            var domain = new List<FastSort>();
            foreach (var kv in vars)
            {
                if (varSortMap.ContainsKey(kv.Key.text))
                    throw new FastParseException(kv.Key.Location, string.Format("Duplicate parameter ID '{0}'", kv.Key.text));
                FastSort s = kv.Value;
                varSortMap[kv.Key.text] = s;
                domain.Add(s);
            }
            this.sort = new FunctionSort(this.name, domain, this.outputSort);
        }

        public override FastToken id
        {
            get { return name; }
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("fun ");
            name.PrettyPrint(sb);
            foreach (var v in inputVariables)
            {
                sb.Append("(" + v.Key);
                sb.Append(" ");
                v.Value.PrettyPrint(sb);
                sb.Append(") ");
            }
            sb.Append(": ");
            outputSort.PrettyPrint(sb);
            if (expr != null)
            {
                sb.Append(" = ");
                expr.PrettyPrint(sb);
            }
        }

        internal override void Typecheck(FastPgm pgm)
        {

            if (this.expr != null) //else constructor
            {
                Func<FastToken, FastSort> context = x =>
                    {
                        if (varSortMap.ContainsKey(x.text))
                            return varSortMap[x.text];
                        else
                            return pgm.GetConstantSort(x);
                    };
                this.expr.CalcSort(context, pgm);
                if (!this.outputSort.name.text.Equals(this.expr.sort.name.text))
                    throw new FastParseException(this.expr.token.Location, string.Format("Wrong sort '{0}', expecting '{1}'", expr.sort.name, outputSort.name.text));
            }
        }
    }

    #endregion

    #region DefDef
    public enum DefDefKind { Tree, Lang, Trans }

    public abstract class DefDef : Def
    {

        public FuncSymbol func;
        public FastSort domain;
        public DefDefKind ddkind;

        protected DefDef(DefDefKind ddkind, FastSort domain)
            : base(DefKind.Def)
        {
            this.ddkind = ddkind;
            this.domain = domain;
        }

        internal override void Typecheck(FastPgm pgm)
        {
            throw new NotImplementedException();
        }
    } 

    public enum TreeDefKind { Apply, Tree, Witness }

    public abstract class TreeDef : DefDef
    {
        internal TreeDefKind tdkind;
        public TreeDef(FastToken acc, FastToken alphabet, TreeDefKind kind)
            : base(DefDefKind.Tree, new FastSort(alphabet, FastSortKind.Tree))
        {
            this.func = new FuncSymbol(acc, 1);
            this.tdkind = kind;
        }

        public override FastToken id
        {
            get { return func.name; }
        }
    }

    public class TreeAppDef : TreeDef
    {
        public BuiltinTransExp transducer;
        public FExp expr;

        public TreeAppDef(FastToken acc, FastToken alphabet, BuiltinTransExp tran, FExp tree)
            : base(acc, alphabet, TreeDefKind.Apply)
        {
            this.transducer = tran;
            this.expr = tree;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("Tree ");
            func.name.PrettyPrint(sb);
            sb.Append(" : ");
            domain.name.PrettyPrint(sb);
            sb.Append(" :=");
            sb.Append("(apply ");
            sb.Append(transducer.ToString());
            sb.Append(" ");
            expr.PrettyPrint(sb);
            sb.AppendLine(")");
        }

        internal override void Typecheck(FastPgm pgm)
        {
            var domAlph = pgm.FindAlphabetDef(domain.name);
            string dom = "", ran = "";


            var defs = new Dictionary<string, Def>();
            bool skipNextDef = false;
            foreach (var d in pgm.defs)
            {
                if (d.id.text == func.name.text)
                    skipNextDef = true;
                else
                    if (!skipNextDef)
                        defs[d.id.text] = d;
                    else
                        if (d.kind != DefKind.Def)
                            defs[d.id.text] = d;
            }
            transducer.BTECheckSorts(defs, pgm);

            dom = transducer.domain.name.text;
            ran = transducer.range.name.text;

            if (ran != domain.name.text)
                throw new FastParseException(domain.name.Location, string.Format("wrong range '{1}' of '{0}'", transducer, domain.name.text));


            var subs = new HashSet<string>();
            if (expr.kind != FExpKind.Var)
                expr.CheckTransformation(subs, null, domAlph, pgm);
            else
            {
                foreach (var d in pgm.defs)
                {
                    if ((expr as Variable).token.text == d.id.text)
                    {
                        if (d.kind == DefKind.Def && (d as DefDef).ddkind == DefDefKind.Tree)
                        {
                            if ((d as TreeDef).domain.name.text != dom)
                                throw new FastParseException(expr.token.Location, string.Format("'{0}' does not have domain '{1}'", expr.token.text, dom));

                            return;
                        }
                        else
                            throw new FastParseException(expr.token.Location, string.Format("'{0}' is not a tree", expr.token.text));
                    }
                }
                throw new FastParseException(expr.token.Location, string.Format("expression '{0}' is undefined", expr.token.text));
            }
        }
    }

    public class TreeWitnessDef : TreeDef
    {
        public BuiltinLangExp language;
        public FExp expr;

        public TreeWitnessDef(FastToken acc, FastToken alphabet, BuiltinLangExp language)
            : base(acc, alphabet, TreeDefKind.Witness)
        {
            this.language = language;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("Tree ");
            func.name.PrettyPrint(sb);
            sb.Append(" : ");
            domain.name.PrettyPrint(sb);
            sb.Append(" :=");
            sb.Append("(witness ");
            sb.Append(language.ToString());
            sb.AppendLine(")");
        }

        internal override void Typecheck(FastPgm pgm)
        {
            var domAlph = pgm.FindAlphabetDef(domain.name);

            string dom = "";


            var defs = new Dictionary<string, Def>();
            bool skipNextDef = false;
            foreach (var d in pgm.defs)
            {
                if (d.id.text == func.name.text)
                    skipNextDef = true;
                else
                    if (!skipNextDef)
                        defs[d.id.text] = d;
                    else
                        if (d.kind != DefKind.Def)
                            defs[d.id.text] = d;
            }
            language.BLECheckSorts(defs, pgm);

            dom = language.domain.name.text;

            if (dom != domain.name.text)
                throw new FastParseException(domain.name.Location, string.Format("wrong domain '{1}' of '{0}'", language, domain.name.text));
        }
    }

    public class TreeExpDef : TreeDef
    {
        public FExp expr;

        public TreeExpDef(FastToken acc, FastToken alphabet, FExp expr)
            : base(acc, alphabet, TreeDefKind.Tree)
        {
            this.expr = expr;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("Tree ");
            func.name.PrettyPrint(sb);
            sb.Append(" : ");
            domain.name.PrettyPrint(sb);
            sb.Append(" :=");
            expr.PrettyPrint(sb);
            sb.AppendLine();
        }

        internal override void Typecheck(FastPgm pgm)
        {
            var domAlph = pgm.FindAlphabetDef(domain.name);

            var subs = new HashSet<string>();
            if (expr.kind != FExpKind.Var)
                expr.CheckTransformation(subs, null, domAlph, pgm);
            else
            {
                foreach (var d in pgm.defs)
                {
                    if ((expr as Variable).token.text == d.id.text)
                    {
                        if (d.kind == DefKind.Def && (d as DefDef).ddkind == DefDefKind.Tree)
                        {
                            if ((d as TreeDef).domain.name.text != domain.name.text)
                                throw new FastParseException((d as TreeDef).domain.name.Location, string.Format("wrong dommain '{1}' of '{0}'", expr.token.text, domain.name.text));

                            return;
                        }
                        else
                            throw new FastParseException(expr.token.Location, string.Format("'{0}' is not a tree", expr.token.text));
                    }
                }
                throw new FastParseException(expr.token.Location, string.Format("'{0}' is undefined", expr.token.text));
            }
        }
    }



    // Def a : A :=...
    public class LangDefDef : DefDef
    {
        public BuiltinLangExp expr;

        public LangDefDef(FastToken acc, FastToken alphabet, BuiltinLangExp expr)
            : base(DefDefKind.Lang, new FastSort(alphabet, FastSortKind.Tree))
        {
            this.func = new FuncSymbol(acc, 1, false, true);
            this.expr = expr;
        }

        public override FastToken id
        {
            get { return func.name; }
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("Def ");
            func.name.PrettyPrint(sb);
            sb.Append(" : ");
            domain.name.PrettyPrint(sb);
            sb.Append(" :=");
            expr.PrettyPrint(sb);
            sb.AppendLine();
        }

        internal override void Typecheck(FastPgm pgm)
        {

            var domAlph = pgm.FindAlphabetDef(domain.name);

            var defs = new Dictionary<string, Def>();
            bool skipNextDef = false;
            foreach (var d in pgm.defs)
                if (d.kind != DefKind.Query)
                {
                    if (d.id.text == this.func.name.text)
                        skipNextDef = true;
                    else
                        if (!skipNextDef)
                            defs[d.id.text] = d;
                        else
                            if (d.kind != DefKind.Def)
                                defs[d.id.text] = d;
                }
            expr.BLECheckSorts(defs, pgm);

            //Check that type of expr is same as type of signature
            if (expr.domain.name.text != domain.name.text)
                throw new FastParseException(expr.func.name.Location,
                    string.Format("'{0}' has domain '{1}' but the inside expression has domain '{2}'", func.name.text, domain.name.text, expr.domain.name.text));
        }
    }

    public class TransDefDef : DefDef
    {
        public FastSort range;
        public BuiltinTransExp expr;

        public TransDefDef(FastToken name, FastToken alphabet1, FastToken alphabet2, BuiltinTransExp expr)
            : base(DefDefKind.Trans, new FastSort(alphabet1, FastSortKind.Tree))
        {
            this.func = new FuncSymbol(name, 1);
            range = new FastSort(alphabet2, FastSortKind.Tree);
            this.expr = expr;
        }

        public override FastToken id
        {
            get { return func.name; }
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("Def ");
            func.name.PrettyPrint(sb);
            sb.Append(" : ");
            domain.name.PrettyPrint(sb);
            sb.Append(" -> ");
            range.name.PrettyPrint(sb);
            sb.Append(" :=");
            expr.PrettyPrint(sb);
            sb.AppendLine();
        }

        internal override void Typecheck(FastPgm pgm)
        {
            var rangeAlph = pgm.FindAlphabetDef(range.name);
            var domAlph = pgm.FindAlphabetDef(domain.name);

            var defs = new Dictionary<string, Def>();
            bool skipNextDef = false;
            foreach (var d in pgm.defs)
            {
                if (d.id.text == func.name.text)
                    skipNextDef = true;
                else
                    if (!skipNextDef)
                        defs[d.id.text] = d;
                    else
                        if (d.kind != DefKind.Def)
                            defs[d.id.text] = d;
            }

            expr.BTECheckSorts(defs, pgm);

            //Check that type of expr is same as type of signature
            if (expr.domain.name.text != domain.name.text)
                throw new FastParseException(expr.func.name.Location,
                    string.Format("'{0}' has domain '{1}' but the inside expression has domain '{2}'", func.name.text, domain.name.text, expr.domain.name.text));

            if (expr.range.name.text != range.name.text)
                throw new FastParseException(expr.func.name.Location,
                    string.Format("'{0}' has range '{1}' but the inside expression has range '{2}'", func.name.text, range.name.text, expr.range.name.text));
        }
    }

    //public enum QueryKind { Contains, IsEmpty, IsEquiv, Display, GenCode, String, Typecheck }

    public abstract class QueryDef : Def
    {
        //public QueryKind queryKind;
        public QueryDef(FastToken token)
            : base(DefKind.Query)
        {
            this.func = new FuncSymbol(token, 0);
        }

        public FuncSymbol func;

        public override FastToken id
        {
            get { return func.name; }
        }
    }

    public abstract class BoolQueryDef : QueryDef
    {
        public Boolean isAssert = false;
        public Boolean assertTrue = false;
        public BoolQueryDef(FastToken query)
            : base(query)
        { }
    }

    public class DisplayQueryDef : QueryDef
    {

        public string toPrintVar
        {
            get { return this.id.text; }
        }

        public DisplayQueryDef(FastToken toPrintVar)
            : base(toPrintVar)
        {
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.AppendFormat("Print {0}", toPrintVar);
        }

        internal override void Typecheck(FastPgm pgm)
        {
            var defs = new Dictionary<string, Def>();
            foreach (var d in pgm.defs)
                if (!(d is QueryDef))
                    if (d.id.text == this.toPrintVar) //the name to print was found
                        return;

            throw new FastParseException(func.name.Location, string.Format("'{0}' is undefined", toPrintVar));
        }
    }

    public enum PLang { CSharp, Javascript }

    public class GenCodeQueryDef : QueryDef
    {
        public PLang language;
        public GenCodeQueryDef(FastToken op)
            : base(op)
        {
            if (op.text == "gen_csharp")
                this.language = PLang.CSharp;
            else
                if (op.text == "gen_js")
                    this.language = PLang.Javascript;
                else
                    throw new FastParseException(op.Location, string.Format("undefined operator '{0}'", op.text));
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            switch (language)
            {
                case PLang.CSharp:
                    sb.AppendFormat("Print gen_csharp");
                    break;
                case PLang.Javascript:
                    sb.AppendFormat("Print gen_js");
                    break;
            }
        }

        internal override void Typecheck(FastPgm pgm) { }
    }

    public class ContainsQueryDef : BoolQueryDef
    {
        public BuiltinLangExp language;
        public FExp expr;

        public ContainsQueryDef(FastToken print, BuiltinLangExp lang, FExp tree)
            : base(print)
        {
            this.language = lang;
            this.expr = tree;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            if (!isAssert)
                sb.Append("Print");
            else
                if (assertTrue)
                    sb.Append("AssertTrue");
                else
                    sb.Append("AssertFalse");
            if (language == null)
                expr.PrettyPrint(sb);
            else
            {
                sb.Append("(contains ");
                sb.Append(language.ToString());
                sb.Append(" ");
                expr.PrettyPrint(sb);
                sb.Append(")");
            }
            sb.AppendLine("");
        }

        internal override void Typecheck(FastPgm pgm)
        {
            string dom = "";

            var defs = new Dictionary<string, Def>();
            foreach (var d in pgm.defs)
            {
                if (!(d is QueryDef))
                    if (d.id.text == func.name.text)
                        break;
                    else
                        defs[d.id.text] = d;
            }

            //Case in which we are not using apply
            if (language != null)
            {
                language.BLECheckSorts(defs, pgm);
                dom = language.domain.name.text;
            }

            var subs = new HashSet<string>();
            if (expr.kind != FExpKind.Var)
                throw new FastParseException(expr.token.Location, string.Format("'{0}' must be a variable", expr.token.text));
            else
            {
                var d = defs[(expr as Variable).token.text];

                if (d.kind == DefKind.Def && (d as DefDef).ddkind == DefDefKind.Tree)
                {
                    if ((d as TreeDef).domain.name.text != dom)
                        throw new FastParseException(expr.token.Location, string.Format("'{0}' does not have domain '{1}'", expr.token.text, dom));

                    return;
                }
                else
                    throw new FastParseException(expr.token.Location, string.Format("'{0}' is not a tree", expr.token.text));

                throw new FastParseException(expr.token.Location, string.Format("'{0}' is undefined", expr.token.text));
            }
        }
    }

    public abstract class EquivQueryDef : BoolQueryDef
    {
        public bool isTransEquiv;
        BuiltinExp expr1;
        BuiltinExp expr2;

        public EquivQueryDef(FastToken equiv, BuiltinExp expr1, BuiltinExp expr2, bool isTransEquiv)
            : base(equiv)
        {
            this.expr1 = expr1;
            this.expr2 = expr2;
            this.isTransEquiv = isTransEquiv;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            if (!isAssert)
                sb.Append("Print");
            else
                if (assertTrue)
                    sb.Append("AssertTrue");
                else
                    sb.Append("AssertFalse");
            sb.Append("(eq ");
            expr1.PrettyPrint(sb);
            sb.Append(" ");
            expr2.PrettyPrint(sb);
            sb.Append(")");
            sb.AppendLine("");
        }
    }

    public class LangEquivQueryDef : EquivQueryDef
    {
        public BuiltinLangExp lang1;
        public BuiltinLangExp lang2;

        public LangEquivQueryDef(FastToken equiv, BuiltinLangExp lang1, BuiltinLangExp lang2)
            : base(equiv, lang1, lang2, false)
        {
            this.lang1 = lang1;
            this.lang2 = lang2;
        }

        internal override void Typecheck(FastPgm pgm)
        {
            var defs = new Dictionary<string, Def>();
            foreach (var d in pgm.defs)
            {
                if (!(d is QueryDef))
                    if (d.id.text == func.name.text)
                        break;
                    else
                        defs[d.id.text] = d;
            }

            lang1.BLECheckSorts(defs, pgm);
            lang2.BLECheckSorts(defs, pgm);
        }
    }

    public class TransEquivQueryDef : EquivQueryDef
    {
        public BuiltinTransExp trans1;
        public BuiltinTransExp trans2;

        public TransEquivQueryDef(FastToken equiv, BuiltinTransExp trans1, BuiltinTransExp trans2)
            : base(equiv, trans1, trans2, true)
        {
            this.trans1 = trans1;
            this.trans2 = trans2;
        }

        internal override void Typecheck(FastPgm pgm)
        {
            var defs = new Dictionary<string, Def>();
            foreach (var d in pgm.defs)
            {
                if (!(d is QueryDef))
                    if (d.id.text == func.name.text)
                        break;
                    else
                        defs[d.id.text] = d;
            }

            trans1.BTECheckSorts(defs, pgm);
            trans2.BTECheckSorts(defs, pgm);
        }
    }


    public abstract class IsEmptyQueryDef : BoolQueryDef
    {
        public bool isTrans;
        BuiltinExp expr;

        public IsEmptyQueryDef(FastToken isempty, BuiltinExp expr, bool isTrans)
            : base(isempty)
        {
            this.expr = expr;
            this.isTrans = isTrans;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            if (!isAssert)
                sb.Append("Print");
            else
                if (assertTrue)
                    sb.Append("AssertTrue");
                else
                    sb.Append("AssertFalse");
            sb.Append("(is_empty ");
            expr.PrettyPrint(sb);
            sb.Append(")");
            sb.AppendLine("");
        }

    }

    public class IsEmptyLangQueryDef : IsEmptyQueryDef
    {
        public BuiltinLangExp lang;

        public IsEmptyLangQueryDef(FastToken isempty, BuiltinLangExp lang)
            : base(isempty, lang, false)
        {
            this.lang = lang;
        }

        internal override void Typecheck(FastPgm pgm)
        {
            var defs = new Dictionary<string, Def>();
            foreach (var d in pgm.defs)
            {
                if (!(d is QueryDef))
                    if (d.id.text == func.name.text)
                        break;
                    else
                        defs[d.id.text] = d;
            }

            lang.BLECheckSorts(defs, pgm);
        }
    }

    public class IsEmptyTransQueryDef : IsEmptyQueryDef
    {
        public BuiltinTransExp trans;

        public IsEmptyTransQueryDef(FastToken isempty, BuiltinTransExp trans)
            : base(isempty, trans, true)
        {
            this.trans = trans;
        }
        internal override void Typecheck(FastPgm pgm)
        {
            var defs = new Dictionary<string, Def>();
            foreach (var d in pgm.defs)
            {
                if (!(d is QueryDef))
                    if (d.id.text == func.name.text)
                        break;
                    else
                        defs[d.id.text] = d;
            }

            trans.BTECheckSorts(defs, pgm);
        }
    }

    public class TypecheckQueryDef : BoolQueryDef
    {
        public BuiltinTransExp trans;
        public BuiltinLangExp input;
        public BuiltinLangExp output;

        public TypecheckQueryDef(FastToken isempty, BuiltinLangExp input, BuiltinTransExp trans, BuiltinLangExp output)
            : base(isempty)
        {
            this.trans = trans;
            this.input = input;
            this.output = output;
        }

        public override FastToken id
        {
            get { return func.name; }
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            if (!isAssert)
                sb.Append("Print");
            else
                if (assertTrue)
                    sb.Append("AssertTrue");
                else
                    sb.Append("AssertFalse");
            sb.Append("(typecheck ");
            input.PrettyPrint(sb);
            sb.Append(" ");
            trans.PrettyPrint(sb);
            sb.Append(" ");
            output.PrettyPrint(sb);
            sb.Append(")");
            sb.AppendLine("");
        }

        internal override void Typecheck(FastPgm pgm)
        {
            var defs = new Dictionary<string, Def>();
            foreach (var d in pgm.defs)
            {
                if (!(d is QueryDef))
                    if (d.id.text == func.name.text)
                        break;
                    else
                        defs[d.id.text] = d;
            }

            input.BLECheckSorts(defs, pgm);
            trans.BTECheckSorts(defs, pgm);
            output.BLECheckSorts(defs, pgm);
        }
    }

    public class StringQueryDef : QueryDef
    {
        public string message;
        public StringQueryDef(FastToken msg)
            : base(msg)
        {
            this.message = msg.text.Substring(1, msg.text.Length - 2);
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.AppendFormat("Print \"{0}\"", message);
        }

        internal override void Typecheck(FastPgm pgm) { }
    }

    #region BuiltinExpressions
    public abstract class BuiltinExp
    {
        public int arity;
        public FastSort domain;
        public FuncSymbol func;

        public BuiltinExp(FastToken func, int arity)
        {
            this.func = new FuncSymbol(func, arity);
            this.arity = arity;
        }

        public abstract void PrettyPrint(StringBuilder sb);
    }


    public enum BuiltinLangExpKind { Var, Intersection, Difference, Domain, Union, Complement, Minimization, Preimage }
    public enum BuiltinTransExpKind { Var, Composition, RestrictionInp, RestrictionOut }

    public abstract class BuiltinLangExp : BuiltinExp
    {
        public BuiltinLangExpKind kind;

        public BuiltinLangExp(FastToken func, int arity, BuiltinLangExpKind kind) :
            base(func, arity)
        {
            this.kind = kind;
        }

        internal abstract void BLECheckSorts(Dictionary<string, Def> defs, FastPgm pgm);
    }

    public abstract class BuiltinTransExp : BuiltinExp
    {
        public BuiltinTransExpKind kind;

        public FastSort range;

        public BuiltinTransExp(FastToken func, int arity, BuiltinTransExpKind kind) :
            base(func, arity)
        {
            this.kind = kind;
        }

        internal abstract void BTECheckSorts(Dictionary<string, Def> defs, FastPgm pgm);
    }

    public class CompositionExp : BuiltinTransExp
    {
        public BuiltinTransExp arg1;
        public BuiltinTransExp arg2;

        public CompositionExp(FastToken func, BuiltinTransExp arg1, BuiltinTransExp arg2)
            : base(func, 2, BuiltinTransExpKind.Composition)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(" ");
            arg2.PrettyPrint(sb);
            sb.Append(")");
        }

        internal override void BTECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BTECheckSorts(defs, pgm);
            arg2.BTECheckSorts(defs, pgm);

            if (arg1.range.name.text != arg2.domain.name.text)
                throw new FastParseException(arg1.func.name.Location,
                    string.Format("the range of '{0}' is different from the domain of '{1}'", arg1.func.name.text, arg2.func.name.text));

            this.domain = arg1.domain;
            this.range = arg2.range;
        }

        public override string ToString()
        {
            return string.Format("(compose {0} {1})", arg1, arg2);
        }
    }

    public class RestrictionOutExp : BuiltinTransExp
    {
        public BuiltinTransExp arg1;
        public BuiltinLangExp arg2;

        public RestrictionOutExp(FastToken func, BuiltinTransExp arg1, BuiltinLangExp arg2)
            : base(func, 2, BuiltinTransExpKind.RestrictionOut)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(" ");
            arg2.PrettyPrint(sb);
            sb.Append(")");
        }

        internal override void BTECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BTECheckSorts(defs, pgm);
            arg2.BLECheckSorts(defs, pgm);

            if (arg1.range.name.text != arg2.domain.name.text)
                throw new FastParseException(arg1.func.name.Location,
                    string.Format("the range of '{0}' is different from the domain of '{1}'", arg1.func.name.text, arg2.func.name.text));

            this.domain = arg1.domain;
            this.range = arg1.range;
        }

        public override string ToString()
        {
            return string.Format("(restrict_out {0} {1})", arg1, arg2);
        }
    }

    public class RestrictionInpExp : BuiltinTransExp
    {
        public BuiltinTransExp arg1;
        public BuiltinLangExp arg2;

        public RestrictionInpExp(FastToken func, BuiltinTransExp arg1, BuiltinLangExp arg2)
            : base(func, 2, BuiltinTransExpKind.RestrictionInp)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(" ");
            arg2.PrettyPrint(sb);
            sb.Append(")");
        }

        internal override void BTECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BTECheckSorts(defs, pgm);
            arg2.BLECheckSorts(defs, pgm);

            if (arg1.domain.name.text != arg2.domain.name.text)
                throw new FastParseException(arg1.func.name.Location,
                    string.Format("the domain of '{0}' is different from the domain of '{1}'", arg1.func.name.text, arg2.func.name.text));

            this.domain = arg1.domain;
            this.range = arg1.range;
        }

        public override string ToString()
        {
            return string.Format("(restrict_inp {0} {1})", arg1, arg2);
        }
    }

    public class TransNameExp : BuiltinTransExp
    {
        public TransNameExp(FastToken func)
            : base(func, 0, BuiltinTransExpKind.Var) { }

        public override void PrettyPrint(StringBuilder sb)
        {
            func.PrettyPrint(sb);
        }

        internal override void BTECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            //Check wheter the variable is defined
            if (!defs.ContainsKey(func.name.text))
                throw new FastParseException(func.name.Location,
                    string.Format("undefined name '{0}'", func.name.text));

            var def = defs[func.name.text];
            if (def is TransDef)
            {
                var defCast = def as TransDef;
                //Set to public if forgot
                defCast.isPublic = true;
                this.domain = defCast.domain;
                this.range = defCast.range;
            }
            else
            {
                if (def is TransDefDef)
                {
                    var defCast = def as TransDefDef;
                    this.domain = defCast.domain;
                    this.range = defCast.range;
                }
                else
                    throw new FastParseException(func.name.Location,
                        string.Format("the name '{0}' does not define a transformation", func.name.text));
            }
        }

        public override string ToString()
        {
            return func.name.text;
        }
    }

    public class UnionExp : BuiltinLangExp
    {
        public BuiltinLangExp arg1;
        public BuiltinLangExp arg2;

        public UnionExp(FastToken func, BuiltinLangExp arg1, BuiltinLangExp arg2)
            : base(func, 2, BuiltinLangExpKind.Union)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(" ");
            arg2.PrettyPrint(sb);
            sb.Append(")");
        }
        internal override void BLECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BLECheckSorts(defs, pgm);
            arg2.BLECheckSorts(defs, pgm);

            if (arg1.domain.name.text != arg2.domain.name.text)
                throw new FastParseException(arg1.func.name.Location,
                    string.Format("the domain of '{0}' is different from the domain of '{1}'", arg1.func.name.text, arg2.func.name.text));

            this.domain = arg1.domain;
        }
        public override string ToString()
        {
            return string.Format("(union {0} {1})", arg1, arg2);
        }
    }

    public class IntersectionExp : BuiltinLangExp
    {
        public BuiltinLangExp arg1;
        public BuiltinLangExp arg2;

        public IntersectionExp(FastToken func, BuiltinLangExp arg1, BuiltinLangExp arg2)
            : base(func, 2, BuiltinLangExpKind.Intersection)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(" ");
            arg2.PrettyPrint(sb);
            sb.Append(")");
        }
        internal override void BLECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BLECheckSorts(defs, pgm);
            arg2.BLECheckSorts(defs, pgm);

            if (arg1.domain.name.text != arg2.domain.name.text)
                throw new FastParseException(arg1.func.name.Location,
                    string.Format("the domain of '{0}' is different from the domain of '{1}'", arg1.func.name.text, arg2.func.name.text));

            this.domain = arg1.domain;
        }
        public override string ToString()
        {
            return string.Format("(difference {0} {1})", arg1, arg2);
        }
    }

    public class DifferenceExp : BuiltinLangExp
    {
        public BuiltinLangExp arg1;
        public BuiltinLangExp arg2;

        public DifferenceExp(FastToken func, BuiltinLangExp arg1, BuiltinLangExp arg2)
            : base(func, 2, BuiltinLangExpKind.Difference)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(" ");
            arg2.PrettyPrint(sb);
            sb.Append(")");
        }
        internal override void BLECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BLECheckSorts(defs, pgm);
            arg2.BLECheckSorts(defs, pgm);

            if (arg1.domain.name.text != arg2.domain.name.text)
                throw new FastParseException(arg1.func.name.Location,
                    string.Format("the domain of '{0}' is different from the domain of '{1}'", arg1.func.name.text, arg2.func.name.text));

            this.domain = arg1.domain;
        }
        public override string ToString()
        {
            return string.Format("(intersect {0} {1})", arg1, arg2);
        }
    }

    public class ComplementExp : BuiltinLangExp
    {
        public BuiltinLangExp arg1;

        public ComplementExp(FastToken func, BuiltinLangExp arg1)
            : base(func, 1, BuiltinLangExpKind.Complement)
        {
            this.arg1 = arg1;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(")");
        }
        internal override void BLECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BLECheckSorts(defs, pgm);
            this.domain = arg1.domain;
        }
        public override string ToString()
        {
            return string.Format("(complement {0})", arg1);
        }
    }

    public class MinimizeExp : BuiltinLangExp
    {
        public BuiltinLangExp arg1;

        public MinimizeExp(FastToken func, BuiltinLangExp arg1)
            : base(func, 1, BuiltinLangExpKind.Minimization)
        {
            this.arg1 = arg1;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(")");
        }
        internal override void BLECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BLECheckSorts(defs, pgm);
            this.domain = arg1.domain;
        }
        public override string ToString()
        {
            return string.Format("(minimize {0})", arg1);
        }
    }

    public class DomainExp : BuiltinLangExp
    {
        public BuiltinTransExp arg1;

        public DomainExp(FastToken func, BuiltinTransExp arg1)
            : base(func, 1, BuiltinLangExpKind.Domain)
        {
            this.arg1 = arg1;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(")");
        }
        internal override void BLECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BTECheckSorts(defs, pgm);
            this.domain = arg1.domain;
        }
        public override string ToString()
        {
            return string.Format("(domain {0})", arg1);
        }
    }

    public class PreimageExp : BuiltinLangExp
    {
        public BuiltinTransExp arg1;
        public BuiltinLangExp arg2;

        public PreimageExp(FastToken func, BuiltinTransExp arg1, BuiltinLangExp arg2)
            : base(func, 2, BuiltinLangExpKind.Preimage)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            sb.Append("(");
            func.PrettyPrint(sb);
            sb.Append(" ");
            arg1.PrettyPrint(sb);
            sb.Append(" ");
            arg2.PrettyPrint(sb);
            sb.Append(")");
        }
        internal override void BLECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            arg1.BTECheckSorts(defs, pgm);
            this.domain = arg1.domain;
            arg2.BLECheckSorts(defs, pgm);
            if (arg1.range.name.text != arg2.domain.name.text)
                throw new FastParseException(arg1.func.name.Location,
                    string.Format("the range of '{0}' is different from the domain of '{1}'", arg1.func.name.text, arg2.func.name.text));
        }
        public override string ToString()
        {
            return string.Format("(domain {0})", arg1);
        }
    }

    public class LangNameExp : BuiltinLangExp
    {
        public LangNameExp(FastToken func)
            : base(func, 0, BuiltinLangExpKind.Var) { }

        public override void PrettyPrint(StringBuilder sb)
        {
            func.PrettyPrint(sb);
        }

        internal override void BLECheckSorts(Dictionary<string, Def> defs, FastPgm pgm)
        {
            //Check wheter the variable is defined
            if (!defs.ContainsKey(func.name.text))
                throw new FastParseException(func.name.Location,
                    string.Format("undefined name '{0}'", func.name.text));

            var def = defs[func.name.text];
            if (def is LangDef)
            {
                var defCast = def as LangDef;
                //Set to public if forgot
                defCast.isPublic = true;
                this.domain = defCast.domain;
            }
            else
            {
                if (def is LangDefDef)
                {
                    var defCast = def as LangDefDef;
                    this.domain = defCast.domain;
                }
                else
                    throw new FastParseException(func.name.Location,
                        string.Format("the name '{0}' does not define a language", func.name.text));
            }
        }
        public override string ToString()
        {
            return func.name.text;
        }
    }

    #endregion
    #endregion

    public class Pattern : Ast
    {
        public FastToken symbol;
        public List<FastToken> children = new List<FastToken>();
        HashSet<string> ids = new HashSet<string>();

        public bool Contains(string childname)
        {
            return ids.Contains(childname);
        }

        public Pattern(FastToken symbol, IEnumerable<FastToken> childList)
        {
            this.symbol = symbol;
            foreach (var c in childList)
            {
                if (!ids.Add(c.text))
                    throw new FastParseException(c.Location, string.Format("Duplicate ID '{0}'", c.text));
                children.Add(c);
            }
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            symbol.PrettyPrint(sb);
            sb.Append("(");
            for (int i = 0; i < children.Count; i++)
            {
                children[i].PrettyPrint(sb);
                if (i < children.Count - 1)
                    sb.Append(",");
            }
            sb.Append(")");
        }
    }

    public class GuardedExp : Ast
    {
        public Pattern pat;
        public FExp where;
        public List<FExp> given;
        public FExp to;

        public GuardedExp(Pattern pat)
        {
            this.pat = pat;
            this.where = BoolValue.True;
            this.given = new List<FExp>();
            given.Add(BoolValue.True);
            this.to = null;
        }


        public GuardedExp(Pattern pat, FExp where, List<FExp> given)
        {
            this.pat = pat;
            this.where = where;
            this.given = given;
            this.to = null;
        }

        public GuardedExp(Pattern pat, FExp where, List<FExp> given, FExp to)
        {
            this.pat = pat;
            this.where = where;
            this.given = given;
            this.to = to;
        }

        public void AddGivenCase(FExp gc)
        {
            given.Add(gc);
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            pat.PrettyPrint(sb);
            sb.Append(" where ");
            where.PrettyPrint(sb);
            sb.Append(" given ");
            foreach (var g in given)
            {
                g.PrettyPrint(sb);
                sb.Append(" ");
            }
            if (to != null)
            {
                sb.Append(" to ");
                to.PrettyPrint(sb);
            }
        }
    }

    /// <summary>
    /// The top level Ast. Represents a Fast program.
    /// </summary>
    public class FastPgm : Ast
    {
        public List<Def> defs = new List<Def>();
        //public List<QueryDef> queries = new List<QueryDef>();
        internal Dictionary<string, Def> defsMap = new Dictionary<string, Def>();

        public void Add(Def def)
        {
            if (def is QueryDef)
            {
                //queries.Add((QueryDef)def);
                defs.Add(def);
            }
            else
            {
                if (defsMap.ContainsKey(def.id.text))
                    throw new FastParseException(def.id.Location, string.Format("ID '{0}' is defined twice", def.id.text));
                defsMap[def.id.text] = def;
                defs.Add(def);
            }
        }

        public void Add(IEnumerable<Def> moredefs)
        {
            foreach (var def in moredefs)
                defs.Add(def);
        }

        public override void PrettyPrint(StringBuilder sb)
        {
            foreach (var def in defs)
            {
                def.PrettyPrint(sb);
                sb.AppendLine();
            }
        }

        public void Typecheck()
        {
            foreach (var def in defs)
            {
                def.Typecheck(this);
            }
        }

        public Def FindDef(FastToken id)
        {
            Def d;
            if (defsMap.TryGetValue(id.text, out d))
                return d;
            else
                throw new FastParseException(id.Location, string.Format("ID '{0}' is undefined", id.text));
        }

        public FastSort GetConstantSort(FastToken id)
        {
            ConstDef d = FindDef(id) as ConstDef;
            if (d == null)
                throw new FastParseException(id.Location, string.Format("ID '{0}' is undefined", id.text));
            return d.sort;
        }

        public AlphabetDef FindAlphabetDef(FastToken alph)
        {
            Def d;
            if (!defsMap.TryGetValue(alph.text, out d))
                throw new FastParseException(alph.Location, string.Format("Alphabet '{0}' is undefined", alph.text));
            AlphabetDef ad = d as AlphabetDef;
            if (ad == null)
                throw new FastParseException(alph.Location, string.Format("ID '{0}' is not an alphabet", alph.text));
            return ad;
        }

        public FunctionDef FindFunctionDef(FastToken func)
        {
            Def d;
            if (!defsMap.TryGetValue(func.text, out d))
                throw new FastParseException(func.Location, string.Format("Function '{0}' is undefined", func.text));
            FunctionDef ad = d as FunctionDef;
            if (ad == null)
                throw new FastParseException(func.Location, string.Format("ID '{0}' is not a function", func.text));
            return ad;
        }

        public EnumDef FindEnumDef(FastToken enu)
        {
            Def d;
            if (!defsMap.TryGetValue(enu.text, out d))
                throw new FastParseException(enu.Location, string.Format("Enum '{0}' is undefined", enu.text));
            EnumDef ed = d as EnumDef;
            if (ed == null)
                throw new FastParseException(enu.Location, string.Format("ID '{0}' is not an Enum", enu.text));
            return ed;
        }

        /// <summary>
        /// Parses a fast program from a source string. Calls Parser.ParseFromString.
        /// </summary>
        /// <param name="source">given source string</param>
        /// <param name="typecheck">if false then typechecking is omitted</param>
        /// <seealso cref="Microsoft.Fast.Parser"/>
        public static FastPgm Parse(string source, bool typecheck = true)
        {
            return Parser.ParseFromString(source, typecheck);
        }

        /// <summary>
        /// Parses a fast program from a source stream. Calls Parser.ParseFromStream.
        /// </summary>
        /// <param name="source">given source stream</param>
        /// <param name="typecheck">if false then typechecking is omitted</param>
        /// <seealso cref="Microsoft.Fast.Parser"/>       
        public static FastPgm Parse(Stream source, bool typecheck = true)
        {
            return Parser.ParseFromStream(source, typecheck);
        }
    }
}