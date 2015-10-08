using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using QUT.Gppg;

namespace Microsoft.Fast.AST
{
    internal partial class FastPgmParser : ShiftReduceParser<object, FastLexLocation>
    {
        public FastPgmParser(Stream str, string file = null)
            : base(new Scanner(str))
        {
            InitializeAliasses();
            if (file != null)
                ((Scanner)(this.Scanner)).sourcefile = file;
        }

        /// <summary>
        /// Parses a single Fast program from multiple files.
        /// Returns null if files is empty.
        /// </summary>
        /// <param name="files">given files</param>
        public static FastPgm ParseFiles(params string[] files)
        {
            FileStream stream = null;
            FastPgm pgm = null;
            foreach (var file in files)
            {
                try
                {
                    FileInfo fi = new FileInfo(file);
                    stream = fi.OpenRead();
                    var parser = new FastPgmParser(stream, fi.FullName);
                    bool ok = parser.Parse();
                    if (!ok)
                        throw new FastParseException("Error: fast parser failed", fi.FullName);
                    if (pgm == null)
                        pgm = parser.program;
                    else
                        pgm.Add(parser.program.defs);
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                        stream = null;
                    }
                }
            }
            return pgm;
        }

        /// <summary>
        /// Parses a Fast program from given text.
        /// </summary>
        public static FastPgm Parse(string text, string filename = null)
        {
            MemoryStream mstr = null;
            FastPgm pgm = null;
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
        public static FastPgm Parse(Stream stream, string filename = null)
        {
            var parser = new FastPgmParser(stream, filename);
            bool ok = parser.Parse();
            if (ok)
                return parser.program;
            else
                throw new FastParseException("Error: fast parser failed", filename);
        }

        private static void InitializeAliasses()
        {
            if (aliasses == null)
            {
                aliasses = new Dictionary<int, string>();
                aliasses[(int)Tokens.LBRACKET] = "'['";
                aliasses[(int)Tokens.RBRACKET] = "']'";
                aliasses[(int)Tokens.LBRACE] = "'{'";
                aliasses[(int)Tokens.RBRACE] = "'}'";
                aliasses[(int)Tokens.LPAR] = "'('";
                aliasses[(int)Tokens.RPAR] = "')'";
                aliasses[(int)Tokens.COMMA] = "','";
                aliasses[(int)Tokens.COLON] = "':'";
                aliasses[(int)Tokens.BAR] = "'|'";
                aliasses[(int)Tokens.LT] = "'<'";
                aliasses[(int)Tokens.GT] = "'>'";
                aliasses[(int)Tokens.LE] = "'<='";
                aliasses[(int)Tokens.GE] = "'>='";
                aliasses[(int)Tokens.EQ] = "'=='";
                aliasses[(int)Tokens.NE] = "'!='";
                aliasses[(int)Tokens.PLUS] = "'+'";
                aliasses[(int)Tokens.MINUS] = "'-'";
                aliasses[(int)Tokens.TIMES] = "'*'";
                aliasses[(int)Tokens.DIV] = "'/'";
                aliasses[(int)Tokens.MOD] = "'%'";
                aliasses[(int)Tokens.SHL] = "'<<'";
                aliasses[(int)Tokens.SHR] = "'>>'";
                aliasses[(int)Tokens.NOT] = "'!'";
                aliasses[(int)Tokens.AND] = "'&&'";
                aliasses[(int)Tokens.OR] = "'||'";
                aliasses[(int)Tokens.IMPLIES] = "'=>'";
                aliasses[(int)Tokens.BVXOR] = "'^'";
                aliasses[(int)Tokens.BVAND] = "'&'";
                aliasses[(int)Tokens.BVNOT] = "'~'";
                aliasses[(int)Tokens.ASSIGN] = "':='";
                aliasses[(int)Tokens.RIGHT_ARROW] = "'->'";
                aliasses[(int)Tokens.CONST] = "'Const'";
                aliasses[(int)Tokens.FUN] = "'Fun'";
                aliasses[(int)Tokens.ALPHABET] = "'Alphabet'";
                aliasses[(int)Tokens.ENUM] = "'Const'";
                aliasses[(int)Tokens.LANG] = "'Lang'";
                aliasses[(int)Tokens.TRANS] = "'Trans'";
                aliasses[(int)Tokens.DEF] = "'Def'";
                aliasses[(int)Tokens.TREE] = "'Tree'";
                aliasses[(int)Tokens.PRINT] = "'Print'";
                aliasses[(int)Tokens.CONST] = "'Const'";
                aliasses[(int)Tokens.ASSERT_TRUE] = "'AssertTrue'";
                aliasses[(int)Tokens.ASSERT_FALSE] = "'AssertFalse'";
                aliasses[(int)Tokens.WHERE] = "'where'";
                aliasses[(int)Tokens.GIVEN] = "'given'";
                aliasses[(int)Tokens.TO] = "'to'";
                aliasses[(int)Tokens.ITE] = "'?'";
                aliasses[(int)Tokens.TRUE] = "'true'";
                aliasses[(int)Tokens.FALSE] = "'false'";
            }
        }

        static int Test(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Error: wrong arguments, usage: parsefast <path>");
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

        #region FastPgm construction
        FastPgm program = null;
        FastPgm MkFastPgm(object defs)
        {
            var def_list = (Cons<Def>)defs;
            var pgm = new FastPgm();
            foreach (var def in def_list)
                pgm.Add(def);
            program = pgm;
            return pgm;
        }
        #endregion

        #region DefList construction
        Cons<Def> MkDefList(object first, object rest)
        {
            Def f = (Def)first;
            Cons<Def> r = (Cons<Def>)rest;
            return new Cons<Def>(f, r);
        }
        Cons<Def> MkEmptyDefList()
        {
            return Cons<Def>.Empty;
        }
        #endregion

        #region Public setting
        void SetPublic(object def)
        {
            LangOrTransDef def1 = (LangOrTransDef)def;
            def1.isPublic = true;
        }
        #endregion

        #region EnumDef construction
        EnumDef MkEnumDef(object name, object elems)
        {
            FastToken ft = (FastToken)name;
            Check(char.IsUpper(ft.text[0]), ft.Location, "Invalid Enum type name '{0}', must start with upper case letter", ft.text);
            Cons<FastToken> members = (Cons<FastToken>)elems;
            EnumDef enumdef = new EnumDef(ft);
            foreach (var token in members)
                enumdef.Add(token);
            return enumdef;
        }
        #endregion

        static void Check(bool cond, FastLexLocation loc, string error, params object[] error_args)
        {
            if (!cond)
                throw new FastParseException(loc, string.Format(error, error_args));
        }

        #region FastTokenList construction
        Cons<FastToken> MkTokenList(object first, object rest)
        {
            FastToken f = (FastToken)first;
            Cons<FastToken> r = (Cons<FastToken>)rest;
            return new Cons<FastToken>(f, r);
        }
        Cons<FastToken> MkEmptyTokenList()
        {
            return Cons<FastToken>.Empty;
        }
        #endregion

        #region TreeDef construction
        TreeDef MkTreeDef(object acc, object alph, object arg)
        {
            FastToken accToken = (FastToken)acc;
            FastToken alphToken = (FastToken)alph;
            if (arg is object[])
            {
                BuiltinTransExp tran = (BuiltinTransExp)(((object[])arg)[0]);
                FExp tree = (FExp)(((object[])arg)[1]);
                return new TreeAppDef(accToken, alphToken, tran, tree);
            }
            else if (arg is BuiltinLangExp)
            {
                BuiltinLangExp lang = (BuiltinLangExp)arg;
                return new TreeWitnessDef(accToken, alphToken, lang);
            }
            else
            {
                FExp expr = (FExp)arg;
                return new TreeExpDef(accToken, alphToken, expr);
            }
        }
        #endregion

        #region LangDefDef construction
        LangDefDef MkLangDefDef(object name, object alph, object expr)
        {
            return new LangDefDef((FastToken)name, (FastToken)alph, (BuiltinLangExp)expr);
        }
        #endregion

        #region TransDefDef construction
        TransDefDef MkTransDefDef(object name, object alph1, object alph2, object expr)
        {
            return new TransDefDef((FastToken)name, (FastToken)alph1, (FastToken)alph2, (BuiltinTransExp)expr);
        }
        #endregion

        #region FunctionDef construction
        FunctionDef MkFunctionDef(object name, object vars, object sort, object expr)
        {
            var pairs = (Cons<KeyValuePair<FastToken, FastToken>>)vars;
            var varsList = new List<KeyValuePair<FastToken, FastSort>>();
            var s = FastSort.GetSort((FastToken)sort);
            var f = (FastToken)name;
            var body = (FExp)expr;
            foreach (var kv in pairs)
                varsList.Add(new KeyValuePair<FastToken, FastSort>(kv.Key, FastSort.GetSort(kv.Value)));
            return new FunctionDef(f, varsList, s, body);
        }
        #endregion

        #region FastTokenPairList construction
        Cons<KeyValuePair<FastToken, FastToken>> MkFastTokenPairList(object v, object sort, object rest)
        {
            KeyValuePair<FastToken, FastToken> f = new KeyValuePair<FastToken, FastToken>((FastToken)v, (FastToken)sort);
            Cons<KeyValuePair<FastToken, FastToken>> r = (Cons<KeyValuePair<FastToken, FastToken>>)rest;
            return new Cons<KeyValuePair<FastToken, FastToken>>(f, r);
        }
        Cons<KeyValuePair<FastToken, FastToken>> MkEmptyFastTokenPairList()
        {
            return Cons<KeyValuePair<FastToken, FastToken>>.Empty;
        }
        #endregion

        #region ConstDef construction
        ConstDef MkConstDef(object name, object sort, object expr)
        {
            return new ConstDef((FastToken)name, (FastToken)sort, (FExp)expr);
        }
        #endregion

        #region AlphabetDef construction

        AlphabetDef MkAlphabetDef(object name, object arg1, object arg2)
        {
            var alphname = (FastToken)name;
            var fields = (Cons<KeyValuePair<FastToken, FastToken>>)arg1;
            var constructors = (Cons<KeyValuePair<FastToken, FastToken>>)arg2;
            var alph = new AlphabetDef(alphname, fields, constructors);
            return alph;
        }
        #endregion

        #region GuardedExpList construction
        Cons<GuardedExp> MkGuardedExpList(object first, object rest)
        {
            var f = (GuardedExp)first;
            Cons<GuardedExp> r = (Cons<GuardedExp>)rest;
            return new Cons<GuardedExp>(f, r);
        }
        Cons<GuardedExp> MkEmptyGuardedExpList()
        {
            return Cons<GuardedExp>.Empty;
        }
        #endregion

        #region LangDef construction
        LangDef MkLangDef(object acc, object alph, object match, object gexprs)
        {
            var accT = (FastToken)acc;
            var alphT = (FastToken)alph;
            var matchT = (FastToken)match;
            var gexpr_list = (Cons<GuardedExp>)gexprs;
            var lang = new LangDef(accT, alphT, matchT);
            foreach (var gexpr in gexpr_list)
                lang.AddCase(gexpr);
            return lang;
        }
        LangDef MkLangDef(object acc, object alph, object gexprs)
        {
            var accT = (FastToken)acc;
            var alphT = (FastToken)alph;
            var gexpr_list = (Cons<GuardedExp>)gexprs;
            var lang = new LangDef(accT, alphT, null);
            foreach (var gexpr in gexpr_list)
                lang.AddCase(gexpr);
            return lang;
        }
        #endregion

        #region TransDef construction
        TransDef MkTransDef(object trans, object domain, object range, object match, object gexprs)
        {
            var transT = (FastToken)trans;
            var domainT = (FastToken)domain;
            var rangeT = (FastToken)range;
            var matchT = (FastToken)match;
            var gexpr_list = (Cons<GuardedExp>)gexprs;

            var tdef = new TransDef(transT, domainT, rangeT, matchT);
            foreach (var gexpr in gexpr_list)
                tdef.AddCase(gexpr);

            return tdef;
        }
        TransDef MkTransDef(object trans, object domain, object range, object gexprs)
        {
            var transT = (FastToken)trans;
            var domainT = (FastToken)domain;
            var rangeT = (FastToken)range;
            var gexpr_list = (Cons<GuardedExp>)gexprs;

            var tdef = new TransDef(transT, domainT, rangeT, null);
            foreach (var gexpr in gexpr_list)
                tdef.AddCase(gexpr);

            return tdef;
        }
        #endregion

        #region Assertions
        void SetAssertTrue(object assertion)
        {
            ((BoolQueryDef)assertion).isAssert = true;
            ((BoolQueryDef)assertion).assertTrue = true;
        }

        void SetAssertFalse(object assertion)
        {
            ((BoolQueryDef)assertion).isAssert = true;
            ((BoolQueryDef)assertion).assertTrue = false;
        }
        #endregion

        #region FExpList construction
        Cons<FExp> MkFExpList(object first, object rest)
        {
            var f = (FExp)first;
            var r = (Cons<FExp>)rest;
            return new Cons<FExp>(f, r);
        }
        Cons<FExp> MkEmptyFExpList()
        {
            return Cons<FExp>.Empty;
        }
        Cons<FExp> MkFExpListFromElems(params object[] elems)
        {
            var res = Cons<FExp>.Empty;
            for (int i = elems.Length - 1; i >= 0; i--)
                res = new Cons<FExp>((FExp)(elems[i]), res);
            return res;
        }
        #endregion

        #region AppExp construction
        AppExp MkAppExp(object lhs, object rhs)
        {
            var f = (FastToken)lhs;
            var args = (Cons<FExp>)rhs;
            var app = new AppExp(f, args);
            return app;
        }
        #endregion

        #region FExp from id construction
        FExp MkId(object id)
        {
            var token = (FastToken)id;

            string[] parts = token.text.Split('.');

            //This is the case X.Y
            if (parts.Length == 2)
            {
                if (char.IsUpper(token.text[0]))
                    //X starts with a capital letter, then this is an enum
                    return new EnumValue(token);
                else
                    //Otherwise we are accessing the attribute of the current node
                    return new AttributeVariable(token);
            }
            else
            {
                var res = new Variable(token);
                return res;
            }
        }
        #endregion

        #region GuardedExp constructor
        GuardedExp MkGuardedExp(object pat, object where, object given, object to = null)
        {
            Pattern p = (Pattern)pat;
            FExp w = (where == null ? BoolValue.True : (FExp)where);
            List<FExp> g = (given == null ? new List<FExp>() : new List<FExp>((Cons<FExp>)given));
            FExp t = to as FExp;
            GuardedExp ge = new GuardedExp(p, w, g, t); 
            return ge;
        }
        #endregion

        #region Pattern cobstruction
        Pattern MkPattern(object name, object children)
        {
            var token = (FastToken)name;
            var childList = (Cons<FastToken>)children;

            var pat = new Pattern(token, childList);

            return pat;
        }
        #endregion

        #region Misc expression contructions
        BoolValue MkBoolValue(object b)
        {
            var bv = (FastToken)b;
            return new BoolValue(bv);
        }


        FastToken MkZero()
        {
            return new FastToken("0");
        }

        StringValue MkStringValue(object str)
        {
            return new StringValue((FastToken)str);
        }

        NumericValue MkNumericValue(object num)
        {
            return new NumericValue((FastToken)num);
        }

        RecordExp MkRecordExp(object lbracket, object args)
        {
            var token = (FastToken)lbracket;
            var fields = (Cons<FExp>)args;
            var rec = new RecordExp(token, fields);
            return rec;
        }

        StringQueryDef MkStringQueryDef(object str)
        {
            return new StringQueryDef((FastToken)str);
        }

        DisplayQueryDef MkDisplayQueryDef(object id)
        {
            return new DisplayQueryDef((FastToken)id);
        }

        ContainsQueryDef MkContainsQueryDef(object q, object lang, object tree)
        {
            return new ContainsQueryDef((FastToken)q, (BuiltinLangExp)lang, (FExp)tree);
        }

        IsEmptyLangQueryDef MkIsEmptyLangQueryDef(object q, object lang)
        {
            return new IsEmptyLangQueryDef((FastToken)q, (BuiltinLangExp)lang);
        }

        TypecheckQueryDef MkTypecheckQueryDef(object q, object lang1, object trans, object lang2)
        {
            return new TypecheckQueryDef((FastToken)q, (BuiltinLangExp)lang1, (BuiltinTransExp)trans, (BuiltinLangExp)lang2);
        }

        GenCodeQueryDef MkGenCodeQueryDef(object q)
        {
            return new GenCodeQueryDef((FastToken)q);
        }

        LangEquivQueryDef MkLangEquivQueryDef(object q, object lang1, object lang2)
        {
            return new LangEquivQueryDef((FastToken)q, (BuiltinLangExp)lang1, (BuiltinLangExp)lang2);
        }

        TransEquivQueryDef MkTransEquivQueryDef(object q, object t1, object t2)
        {
            return new TransEquivQueryDef((FastToken)q, (BuiltinTransExp)t1, (BuiltinTransExp)t2);
        }

        IsEmptyTransQueryDef MkIsEmptyTransQueryDef(object q, object t)
        {
            return new IsEmptyTransQueryDef((FastToken)q, (BuiltinTransExp)t);
        }

        CompositionExp MkCompositionExp(object comp, object t1, object t2)
        {
            return new CompositionExp((FastToken)comp, (BuiltinTransExp)t1, (BuiltinTransExp)t2);
        }

        RestrictionInpExp MkRestrictionInpExp(object r, object t, object l)
        {
            return new RestrictionInpExp((FastToken)r, (BuiltinTransExp)t, (BuiltinLangExp)l);
        }

        RestrictionOutExp MkRestrictionOutExp(object r, object t, object l)
        {
            return new RestrictionOutExp((FastToken)r, (BuiltinTransExp)t, (BuiltinLangExp)l);
        }

        TransNameExp MkTransNameExp(object n)
        {
            return new TransNameExp((FastToken)n);
        }

        IntersectionExp MkIntersectionExp(object op, object l1, object l2)
        {
            return new IntersectionExp((FastToken)op, (BuiltinLangExp)l1, (BuiltinLangExp)l2);
        }

        DifferenceExp MkDifferenceExp(object op, object l1, object l2)
        {
            return new DifferenceExp((FastToken)op, (BuiltinLangExp)l1, (BuiltinLangExp)l2);
        }

        UnionExp MkUnionExp(object op, object l1, object l2)
        {
            return new UnionExp((FastToken)op, (BuiltinLangExp)l1, (BuiltinLangExp)l2);
        }

        DomainExp MkDomainExp(object op, object t)
        {
            return new DomainExp((FastToken)op, (BuiltinTransExp)t);
        }

        PreimageExp MkPreimageExp(object op, object t, object l)
        {
            return new PreimageExp((FastToken)op, (BuiltinTransExp)t, (BuiltinLangExp)l);
        }

        ComplementExp MkComplementExp(object op, object l)
        {
            return new ComplementExp((FastToken)op, (BuiltinLangExp)l);
        }

        MinimizeExp MkMinimizeExp(object op, object l)
        {
            return new MinimizeExp((FastToken)op, (BuiltinLangExp)l);
        }

        LangNameExp MkLangNameExp(object id)
        {
            return new LangNameExp((FastToken)id);
        }
        #endregion

        //#region ITE construction
        //FExp MkITE(object ite, object cond, object truecase, object falsecase)
        //{
        //    FastToken i = (FastToken)ite;
        //    FExp c = (FExp)cond;
        //    FExp t = (FExp)truecase;
        //    FExp f = (FExp)falsecase;
        //    return new AppExp(i, new FExp[]{c,t,f});
        //}
        //#endregion
        /// <summary>
        /// Simply linked list of elements
        /// </summary>
        /// <typeparam name="T">element type</typeparam>
    }

    /// <summary>
    /// Simply linked list of elements.
    /// </summary>
    /// <typeparam name="T">element type</typeparam>
    public class Cons<T> : IEnumerable<T>
    {
        T first;
        Cons<T> rest;
        int count;

        /// <summary>
        /// Gets the first element of this list.
        /// </summary>
        public T First
        {
            get
            {
                if (count == 0)
                    throw new InvalidOperationException("Empty list");
                return first;
            }
        }

        /// <summary>
        /// Gets the rest of the list.
        /// </summary>
        public Cons<T> Rest
        {
            get
            {
                if (count == 0)
                    throw new InvalidOperationException("Empty list");
                return rest;
            }
        }

        /// <summary>
        /// Gets the number of elements in the list.
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Gets the i'th element, i must be in the range 0..Count-1.
        /// The operation is linear in the length of the list.
        /// </summary>
        /// <param name="i">index of the element to get</param>
        public T GetElement(int i)
        {
            if (i < 0 || i >= count)
                throw new IndexOutOfRangeException();
            var curr = this;
            for (int j = 0; j < i; j++)
                curr = curr.rest;
            return curr.first;
        }

        /// <summary>
        /// Gets the i'th element, i must be in the range 0..Count-1.
        /// The operation is linear in the length of the list.
        /// </summary>
        /// <param name="i">index of the element to get</param>
        public T this[int i]
        {
            get
            {
                return GetElement(i);
            }
        }

        Cons()
        {
            this.first = default(T);
            this.rest = null;
            this.count = 0;
        }

        readonly static Cons<T> empty = new Cons<T>();

        /// <summary>
        /// The empty list.
        /// </summary>
        public static Cons<T> Empty
        {
            get { return empty; }
        }

        /// <summary>
        /// Returns true if the list is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return count == 0; }
        }

        /// <summary>
        /// Constructs a new Cons list.
        /// </summary>
        /// <param name="first">the first element</param>
        /// <param name="rest">the rest of the list</param>
        public Cons(T first, Cons<T> rest)
        {
            if (rest == null)
                throw new ArgumentNullException("rest");
            this.first = first;
            this.rest = rest;
            this.count = rest.count + 1;
        }

        /// <summary>
        /// Make a new list of the given elements.
        /// </summary>
        /// <param name="elems">given elements</param>
        public static Cons<T> Mk(params T[] elems)
        {
            var res = empty;
            for (int i = elems.Length - 1; i >= 0; i--)
                res = new Cons<T>(elems[i], res);
            return res;
        }

        /// <summary>
        /// Gets the enumerator of the elements in this Cons list.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return new ConsEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ConsEnumerator(this);
        }

        class ConsEnumerator : IEnumerator<T>
        {
            Cons<T> orig;
            Cons<T> cons;
            T current;
            internal ConsEnumerator(Cons<T> cons)
            {
                this.cons = cons;
                this.orig = cons;
                this.current = default(T);
            }

            public T Current
            {
                get
                {
                    return current;
                }
            }

            public void Dispose()
            { }

            object System.Collections.IEnumerator.Current
            {
                get { return current; }
            }

            public bool MoveNext()
            {
                if (cons.IsEmpty)
                    return false;
                else
                {
                    current = cons.first;
                    cons = cons.rest;
                    return true;
                }
            }

            public void Reset()
            {
                this.cons = this.orig;
                this.current = default(T);
            }
        }

        //public override void PrettyPrint(StringBuilder sb)
        //{
        //    sb.Append("(");
        //    var curr = this;
        //    bool firstdone = false;
        //    while (!curr.isempty)
        //    {
        //        if (firstdone)
        //            sb.Append(" ");
        //        curr.first.PrettyPrint(sb);
        //        curr = curr.rest;
        //        firstdone = true;
        //    }
        //    sb.Append(")");
        //}

        /// <summary>
        /// Returns true iff the lists have the same length and have equal elements.
        /// </summary>
        /// <param name="list">other list to compare to</param>
        public bool Equals(Cons<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (list.count != count)
                return false;

            var list1 = this;
            var list2 = list;
            for (int j = 0; j < count; j++)
            {
                if (!object.Equals(list1.first, list2.first))
                    return false;
                list1 = list1.rest;
                list2 = list2.rest;
            }
            return true;
        }
    }

    internal class FastLexLocation : LexLocation, IMerge<FastLexLocation>
    {
        string file;
        /// <summary>
        /// Default no-arg constructor.
        /// </summary>
        public FastLexLocation()
            : base()
        { }

        /// <summary>
        /// Source file of the location
        /// </summary>
        internal string File
        {
            get
            {
                return file;
            }
        }

        public FastLexLocation(int sl, int sc, int el, int ec, string file) :
            base(sl, sc, el, ec)
        { this.file = file; }

        public FastLexLocation Merge(FastLexLocation last)
        {
            return new FastLexLocation(this.StartLine, this.StartColumn, last.EndLine, last.EndColumn, file);
        }

        public static FastLexLocation operator +(FastLexLocation loc1, FastLexLocation loc2)
        {
            return loc1.Merge(loc2);
        }

        public override string ToString()
        {
            return string.Format("{4}({0},{1},{2},{3})", StartLine, StartColumn, EndLine, EndColumn, (file == null ? "" : file));
        }
    }
}
