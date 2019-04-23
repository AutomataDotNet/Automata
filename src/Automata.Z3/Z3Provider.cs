using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

using Microsoft.Automata;

using Microsoft.Automata.Z3.Internal;

namespace Microsoft.Automata.Z3
{
    /// <summary>
    /// Provides extended Z3 functionality
    /// </summary>
    public class Z3Provider : IContext<FuncDecl,Expr,Sort>
    {
        //Z3 Context
        internal readonly Microsoft.Z3.Context z3;
        internal readonly Dictionary<string,string> z3config;
        internal readonly Z3Solver solver;

        internal const string tupleSortNamePrefix = "T";

        BitWidth encoding = BitWidth.BV16;

        CharSetSolver bddb;

        /// <summary>
        /// Character set solver
        /// </summary>
        public CharSetSolver CharSetProvider
        {
            get
            {
                return bddb;
            }
        }

        public IRegexConverter<Expr> RegexConverter
        {
            get { return regexConverter; }
        }

        Sort characterSort;
        /// <summary>
        /// Gets the character sort, which depends on Eccoding. Default is BV16.
        /// </summary>
        public Sort CharacterSort
        {
            get
            {
                return characterSort;
            }
        }

        Sort stringSort;
        /// <summary>
        /// Gets the sort 'list of character'.
        /// </summary>
        public Sort StringSort
        {
            get
            {
                return stringSort;
            }
        }

        //extended context
        DeclaredFuncDecls declaredFuncDecls;

        public Sort IntSort { get { return z3.MkIntSort(); } }
        public Sort RealSort { get { return z3.MkRealSort(); } }
        public Sort BoolSort { get { return z3.MkBoolSort(); } }
        readonly Expr trueExpr;
        public Expr True { get { return trueExpr; } }
        readonly Expr falseExpr;
        public Expr False { get { return falseExpr; } }

        Sort unitSort;

        Expr unitConst;

        //successor number info
        Expr succZero = null;
        FuncDecl succIncr = null;
        FuncDecl succDecr = null;
        Sort succNumberSort = null;

        //tree info
        Dictionary<Sort, UnrankedTreeInfo> treeInfoMap = new Dictionary<Sort, UnrankedTreeInfo>();
        Dictionary<Tuple<string, Sort>, Sort> treeSortMap = new Dictionary<Tuple<string, Sort>, Sort>();

        //binary tree info
        Dictionary<Sort, BinaryTreeInfo> binaryTreeInfoMap = new Dictionary<Sort, BinaryTreeInfo>();
        Dictionary<Tuple<string, Sort>, Sort> binaryTreeSortMap = new Dictionary<Tuple<string, Sort>, Sort>();

        RegexToAutomatonConverter<Expr> regexConverter;

        LibraryProvider library;

        /// <summary>
        /// Functionality to manipulate trees over ranked alphabets.
        /// </summary>
        public TreeTheory TT { get { return treeTheory; } }
        TreeTheory treeTheory;

        #region Constructors

        /// <summary>
        /// default constructor
        /// </summary>
        public Z3Provider()
            : this(BitWidth.BV16)
        { }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="encoding"></param>
        public Z3Provider(BitWidth encoding)
        {
            if (!CharacterEncodingTool.IsSpecified(encoding))
                throw new AutomataException(AutomataExceptionKind.CharacterEncodingIsUnspecified);

            //Log.Open("c:\\\\tmp\\z3log.txt");

            z3config = new Dictionary<string, string>();
            z3config["MODEL"]= "true";
            z3config["auto_config"] = "false";
            //z3config["unsat_core"] = "true";
            //z3config["proof"] = "true";
            //z3config["mbqi"] = "false";
            //z3config["smt.mbqi"] = "false";
            //z3config["PARTIAL_MODELS"] = "true";
            z3 = new Context(z3config);

            solver = new Z3Solver(z3.MkSolver(),this);

            var par = z3.MkParams();
            par.Add(":mbqi", false);
            par.Add(":auto_config", false);
            //par.Add(":produce-unsat-cores", true);
            par.Add(":unsat-core", true);
            solver.Solver.Parameters = par;
            //foreach (var n in solver.ParameterDescriptions.Names)
            //{
            //    Console.WriteLine("{0}", n);
            //}
            solver.Solver.ParameterDescriptions.Validate(par);

            //z3.OpenLog("c:\\log.z3");
            falseExpr = z3.MkFalse();
            trueExpr = z3.MkTrue();
            characterSort = FromEncodingToSort(encoding);

            declaredFuncDecls = new DeclaredFuncDecls();
            bddb = new CharSetSolver(encoding);
            this.encoding = encoding;
            tpp = new ExprPrettyPrinter(this);
            regexConverter = new RegexToAutomatonConverter<Expr>(this);
            unitSort = MkTupleSort();
            unitConst = MkTuple();
            treeTheory = new TreeTheory(this);

            stringSort = MkListSort(characterSort);

            library = new LibraryProvider(this);

            MainSolver.Push();
            
        }

        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="z3context"></param>
        public Z3Provider(BitWidth encoding, Context z3context)
        {
            if (!CharacterEncodingTool.IsSpecified(encoding))
                throw new AutomataException(AutomataExceptionKind.CharacterEncodingIsUnspecified);

            z3 = z3context;

            solver = new Z3Solver(z3.MkSolver(),this);
            //z3.OpenLog("c:\\log.z3");
            falseExpr = z3.MkFalse(); 
            trueExpr = z3.MkTrue();
            characterSort = FromEncodingToSort(encoding);

            declaredFuncDecls = new DeclaredFuncDecls();
            bddb = new CharSetSolver(encoding);
            this.encoding = encoding;
            tpp = new ExprPrettyPrinter(this);
            regexConverter = new RegexToAutomatonConverter<Expr>(this);
            unitSort = MkTupleSort();
            unitConst = MkTuple();
            treeTheory = new TreeTheory(this);

            stringSort = MkListSort(characterSort);

            library = new LibraryProvider(this);

            MainSolver.Push();

        }


        Sort FromEncodingToSort(BitWidth encoding)
        {
            switch (encoding)
            {
                case BitWidth.BV7 : return z3.MkBitVecSort(7);
                case BitWidth.BV8: return z3.MkBitVecSort(8);
                case BitWidth.BV16: return z3.MkBitVecSort(16);
                case BitWidth.BV32: return z3.MkBitVecSort(32);
                default: return z3.MkIntSort();
            }
        }

        /// <summary>
        /// Construct the provider for the given Z3 context.
        /// </summary>
        public Z3Provider(Context z3): this(BitWidth.BV16, z3)
        {}

        #endregion

        /// <summary>
        /// layered dispose
        /// </summary>
        public void Dispose()
        {
            z3.Dispose();
            if (solver != null)
            {
                solver.Dispose();
            }
        }

        public void SetCompactView()
        {
            tpp.compactview = true;
        }

        #region Direct Z3 wrappers

        /// <summary>
        /// Make a variable with given index and sort.
        /// A variable is an uninterpreted constant with integer index.
        /// </summary>
        /// <param name="index">index of the variable</param>
        /// <param name="sort">sort of the variable</param>
        public Expr MkVar(uint index, Sort sort) 
        {
            return z3.MkConst(z3.MkSymbol((int)index), sort);
            //return z3.MkBound(id, sort);

        }

        /// <summary>
        /// Gets the index of the variable v.
        /// </summary>
        public uint GetVarIndex(Expr v)
        {
            if (!IsVar(v))
                throw new AutomataException(AutomataExceptionKind.ExpectingVariable);

            return (uint)((IntSymbol)v.FuncDecl.Name).Int;
            //return v.Index;
        }

        /// <summary>
        /// Checks that v is a constant whose name is an integer symbol.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool IsVar(Expr v)
        {
            if (v.IsConst && v.FuncDecl.Name.IsIntSymbol())
                return true;
            return false;
            //return v.ASTKind == Z3_ast_kind.Z3_VAR_AST;
        }

        /// <summary>
        /// wrap MkConst
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public Expr MkConst(string name, Sort sort)
        {
            return z3.MkConst(name, sort);
        }

        /// <summary>
        /// wrapper
        /// </summary>
        /// <param name="f"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Expr MkApp(FuncDecl f, params Expr[] args)
        {
            //
            // TBD: cannot check this, some built-in functions have also the kind Uninterpreted
            //      but have in fact an interpretation 
            //
            //if (GetZ3_decl_kind(f) == Z3_decl_kind.Z3_OP_UNINTERPRETED)
            //    if (!IsDefined(f))
            //        throw new AutomataException(AutomataExceptionKind.FunctionDeclarationIsInvalid);
            return z3.MkApp(f, args);
        }

        /// <summary>
        /// wrapper
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Expr MkLe(Expr x, Expr y)
        {
            return z3.MkLe((ArithExpr)x, (ArithExpr)y);
        }

        /// <summary>
        /// wrapper
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Expr MkGe(Expr x, Expr y)
        {
            return z3.MkGe((ArithExpr)x, (ArithExpr)y);
        }

        public Expr MkGt(Expr x, Expr y)
        {
            return z3.MkGt((ArithExpr)x, (ArithExpr)y);
        }

        public Expr MkLt(Expr x, Expr y)
        {
            return z3.MkLt((ArithExpr)x, (ArithExpr)y);
        }

        public Expr MkBvUlt(Expr c1, Expr c2)
        {
            return z3.MkBVULT((BitVecExpr)c1, (BitVecExpr)c2);
        }

        public Expr MkBvUgt(Expr c1, Expr c2)
        {
            return z3.MkBVUGT((BitVecExpr)c1, (BitVecExpr)c2);
        }

        public Expr MkBvUle(Expr c1, Expr c2)
        {
            return z3.MkBVULE((BitVecExpr)c1, (BitVecExpr)c2);
        }

        public Expr MkBvUge(Expr c1, Expr c2)
        {
            return z3.MkBVUGE((BitVecExpr)c1, (BitVecExpr)c2);
        }


        public Expr MkDiv(Expr t1, Expr t2)
        {
            return z3.MkDiv((ArithExpr)t1, (ArithExpr)t2);
        }

        public Expr MkRem(Expr t1, Expr t2)
        {
            return z3.MkRem((IntExpr)t1, (IntExpr)t2);
        }

        public Expr MkEmptySet(Sort elemSort)
        {
            return z3.MkEmptySet(elemSort);
        }

        /// <summary>
        /// wrapper
        /// </summary>
        /// <param name="tupleSort"></param>
        /// <returns></returns>
        public FuncDecl GetTupleConstructor(Sort tupleSort)
        {
            if (tupleSort is TupleSort)
                return ((TupleSort)tupleSort).MkDecl;
            if (tupleSort is DatatypeSort)
                return ((DatatypeSort)tupleSort).Constructors[0];
            throw new AutomataException("Wrong expression");            
        }

        /// <summary>
        /// Gets the k'th tuple field of the given tuple sort.
        /// </summary>
        public FuncDecl GetTupleField(Sort tupleSort, int k)
        {
            if (tupleSort is TupleSort)
                return ((TupleSort)tupleSort).FieldDecls[k];
            if (tupleSort is DatatypeSort)
                return ((DatatypeSort)tupleSort).Accessors[0][k];
            throw new AutomataException("Wrong expression");
        }

        /// <summary>
        /// Gets the nr k such that f is the k'th field of the tuple sort. Returns -1 if f is not a field.
        /// </summary>
        public int GetTupleFieldNr(Sort tupleSort, FuncDecl f)
        {
            var fs = ((TupleSort)tupleSort).FieldDecls;
            for (int i = 0; i < fs.Length; i++)
                if (fs[i].Equals(f))
                    return i;
            return -1;
        }

        /// <summary>
        /// Returns true iff f is a tuple field of the given tuple sort.
        /// </summary>
        public bool IsTupleField(Sort tupleSort, FuncDecl f)
        {
            if(tupleSort is TupleSort)
                return Array.Exists(((TupleSort)tupleSort).FieldDecls, x => x.Equals(f));
            if (tupleSort is DatatypeSort)
                return Array.Exists(((DatatypeSort)tupleSort).Accessors[0], x => x.Equals(f));
            throw new AutomataException("Wrong expression");
        }



        /// <summary>
        /// wrapper
        /// </summary>
        /// <param name="tupleSort"></param>
        /// <returns></returns>
        public int GetTupleLength(Sort tupleSort)
        {
            if(tupleSort is DatatypeSort)
                return (int)((DatatypeSort)tupleSort).Accessors[0].Length;
            if(tupleSort is TupleSort)
                return (int)((TupleSort)tupleSort).FieldDecls.Length;
            throw new AutomataException("Wrong expression");
        }

       /// <summary>
       /// Create tuple sort with given name (a record).
       /// </summary>
       /// <param name="name">name of the sort</param>
       /// <param name="projectionNames">field names</param>
       /// <param name="elemSorts">field sorts</param>
       /// <param name="f">(output) the resulting constructor</param>
       /// <param name="projections">(output) the resulting projection functions corrresponding to the projection names</param>
       /// <returns></returns>
        public Sort MkTupleSort(string name, string[] projectionNames, Sort[] elemSorts, out FuncDecl f, out FuncDecl[] projections)
        {
            Symbol[] symbs = new Symbol[projectionNames.Length];
            for (int i = 0; i < symbs.Length; i++)
                symbs[i] = z3.MkSymbol(projectionNames[i]);            
            TupleSort tupSort = z3.MkTupleSort(z3.MkSymbol(name), symbs, elemSorts);
            f = tupSort.MkDecl;
            projections = tupSort.FieldDecls;
            return tupSort;
        }

        /// <summary>
        /// wrapper
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public Expr MkEq(Expr t1, Expr t2)
        {
            return z3.MkEq(t1, t2);
        }

        public Expr MkNot(Expr formula)
        {
            if (formula.ASTKind == Z3_ast_kind.Z3_APP_AST && formula.FuncDecl.DeclKind == Z3_decl_kind.Z3_OP_NOT)
                return formula.Args[0];
            if (formula.Equals(True))
                return False;
            if (formula.Equals(False))
                return True;
            return z3.MkNot((BoolExpr)formula);
        }

        public Expr MkUnaryMinus(Expr arg)
        {
            return z3.MkUnaryMinus((ArithExpr)arg);
        }

        public Status Check()
        {
            return solver.Solver.Check();
        }

        public Expr MkAnd(IEnumerable<Expr> formulas)
        {
            return MkAnd(new List<Expr>(formulas).ToArray());
        }

        public Expr MkAndDoNotSimplify(Expr[] formulas)
        {
            var cformulas = new BoolExpr[formulas.Length];
            for (int i = 0; i < cformulas.Length; i++)
                cformulas[i] = (BoolExpr)formulas[i];
            return z3.MkAnd(cformulas);
        }

        /// <summary>
        /// Assumes that cond1 is sat and cond2 is sat.
        /// If cond1 implies cond2 then returns cond1.
        /// If cond2 implies cond1 then returns cond2.
        /// If (cond1 and cond2) is unsat then returns False.
        /// Otherwise returns (cond1 and cond2).
        /// </summary>
        /// <param name="cond1"></param>
        /// <param name="cond2"></param>
        /// <returns></returns>
        internal Expr MkAndSimplify(Expr cond1, Expr cond2)
        {
            if (cond1.Equals(True))
                return cond2;

            if (cond2.Equals(True))
                return cond1;

            if (cond1.Equals(cond2))
                return cond1;

            if (!IsSatisfiable(MkAnd(cond2, cond1)))
                return False;

            if (!IsSatisfiable(MkAnd(cond1, MkNot(cond2))))
                return cond1;

            if (!IsSatisfiable(MkAnd(cond2, MkNot(cond1))))
                return cond2;

            return Z3.MkAnd((BoolExpr)cond1, (BoolExpr)cond2);
        }

        public Expr MkAnd(params Expr[] formulas)
        {
            var cformulas = new BoolExpr[formulas.Length];
            for (int i = 0; i < cformulas.Length; i++)
                cformulas[i] = (BoolExpr)formulas[i];
            
            if (cformulas.Length == 0)
                return True;
            if (cformulas.Length == 1)
                return cformulas[0];
            if (Array.Exists(cformulas, f => f.Equals(False)))
                return False;
            if (cformulas.Length == 2)
            {
                if (cformulas[0].Equals(cformulas[1]))
                    return cformulas[0];
                if (MkNot(cformulas[0]).Equals(cformulas[1]))
                    return False;
            }
            if (cformulas.Length == 2 && cformulas[1].ASTKind == Z3_ast_kind.Z3_APP_AST &&
                cformulas[1].FuncDecl.DeclKind == Z3_decl_kind.Z3_OP_AND &&
                cformulas[1].Args.Length == 2)
            {
                var f1 = cformulas[0];
                var f2 = (BoolExpr) cformulas[1].Args[0];
                var f3 = (BoolExpr) cformulas[1].Args[1];
                var f1andf2 = (f1.Equals(f2) ? f1 : z3.MkAnd(f1, f2));
                return z3.MkAnd(f1andf2, f3);
            }

            return z3.MkAnd(cformulas);
        }

        public Expr MkOr(IEnumerable<Expr> formulas)
        {
            return MkOr(new List<Expr>(formulas).ToArray());
        }

        public Expr MkOr(params Expr[] formulas)
        {
            var cformulas = new BoolExpr[formulas.Length];
            for (int i = 0; i < cformulas.Length; i++)
                cformulas[i] = (BoolExpr)formulas[i];

            if (cformulas.Length == 0)
                return False;
            if (cformulas.Length == 1)
                return cformulas[0];
            if (Array.Exists(cformulas, f => f.Equals(True)))
                return True;
            if (cformulas.Length == 2)
            {
                if (cformulas[0].Equals(cformulas[1]))
                    return cformulas[0];
                if (MkNot(cformulas[0]).Equals(cformulas[1]))
                    return True;
            }

            return z3.MkOr(cformulas);
        }

        public Sort GetSort(Expr t)
        {
            return t.Sort;
        }

        /// <summary>
        /// Make numeral term t1 + t2 + ... + tn
        /// </summary>
        public Expr MkAdd(params Expr[] ts)
        {
            ArithExpr[] cts = new ArithExpr[ts.Length];
            for (int i = 0; i < cts.Length; i++)
                cts[i] = (ArithExpr)ts[i];

            return z3.MkAdd(cts);
        }

        /// <summary>
        /// Make bitvector addition t1 + t2
        /// </summary>
        public Expr MkBvAdd(Expr t1, Expr t2)
        {
            if (t1 is IntNum)
            {

            }
            return z3.MkBVAdd((BitVecExpr)t1, (BitVecExpr)t2);
        }

        public Expr MkBvAddMany(params Expr[] ts)
        {
            if (ts.Length < 2)
            {
                throw new ArgumentException();
            }

            Expr cur = ts[0];
            for (int i = 1; i < ts.Length; ++i)
            {
                cur = MkBvAdd(cur, ts[i]);
            }

            return cur;
        }

        /// <summary>
        /// Make numeral term t1 - t2
        /// </summary>
        public Expr MkSub(Expr t1, Expr t2)
        {
            return z3.MkSub(new ArithExpr[] { (ArithExpr)t1, (ArithExpr)t2 });
        }

        /// <summary>
        /// Make bitvector subtraction t1 - t2
        /// </summary>
        public Expr MkBvSub(Expr t1, Expr t2)
        {
            return z3.MkBVSub((BitVecExpr)t1, (BitVecExpr)t2);
        }

        /// <summary>
        /// Make bitvector multiplication t1 * t2
        /// </summary>
        public Expr MkBvMul(Expr t1, Expr t2)
        {
            return z3.MkBVMul((BitVecExpr)t1, (BitVecExpr)t2);
        }

        public Expr MkZeroExt(uint i, Expr t)
        {
            return z3.MkZeroExt(i, (BitVecExpr)t);
        }

        /// <summary>
        /// Make integer term t1 modulo t2
        /// </summary>
        public Expr MkMod(Expr t1, Expr t2)
        {
            return z3.MkMod((IntExpr)t1, (IntExpr)t2);
        }

        public Expr MkArrayConst(string name, Sort domainSort, Sort rangeSort)
        {
            return z3.MkArrayConst(name , domainSort, rangeSort);
        }

        public Expr MkArraySelect(Expr array, Expr index)
        {
            return z3.MkSelect((ArrayExpr)array, index);
        }

        public Expr MkArrayStore(Expr array, Expr index, Expr value)
        {
            return z3.MkStore((ArrayExpr)array, index, value);
        }

        public FuncDecl GetAppDecl(Expr t)
        {
            return t.FuncDecl;
        }

        /// <summary>
        /// Make numeral term t1 * t2
        /// </summary>
        public Expr MkMul(Expr term, Expr term_2)
        {
            return z3.MkMul((ArithExpr)term, (ArithExpr)term_2); ;
        }

        public Expr[] GetAppArgs(Expr t)
        {
            return t.Args;
        }

        public Z3_decl_kind GetZ3_decl_kind(FuncDecl f)
        {
            return f.DeclKind;
        }



        public bool IsSatisfiable(Expr constraint)
        {
            return MainSolver.IsSatisfiable(constraint);
            //if (constraint.Equals(False))
            //    return false;
            //else if (constraint.Equals(True))
            //    return true;
            //else
            //{
            //    solver.Push();
            //    solver.Assert((BoolExpr)constraint);
            //    var res = solver.Solver.Check();
            //    solver.Pop();
            //    if (res == Status.UNSATISFIABLE)
            //        return false;
            //    else
            //        return true;
            //}
        }

        /// <summary>
        /// Converts the term to an equivalent simpler form.
        /// </summary>
        public Expr Simplify(Expr term)
        {
            var res = term.Simplify();
            return res;
        }

        /// <summary>
        /// Converts the term to an equivalent term in negation normal form.
        /// </summary>
        public Expr ToNNF(Expr term)
        {
            return ToNNF1(term, true, new Dictionary<Expr, Expr>(), new Dictionary<Expr, Expr>()); //TBD
        }

        Expr ToNNF1(Expr t, bool pos, IDictionary<Expr, Expr> posCase, IDictionary<Expr, Expr> negCase)
        {
            Expr nnf;
            if (pos)
            {
                if (posCase.TryGetValue(t, out nnf))
                    return nnf;
            }
            else
            {
                if (negCase.TryGetValue(t, out nnf))
                    return nnf;
            }

            Z3_ast_kind kind = GetZ3_ast_kind(t);

            switch (kind)
            {
                case Z3_ast_kind.Z3_VAR_AST:
                    {
                        #region variables
                        if (t.Sort.Equals(BoolSort))
                            nnf = (pos ? t : MkNot(t));
                        else
                            nnf = t;
                        break;
                        #endregion
                    }
                case Z3_ast_kind.Z3_NUMERAL_AST:
                    {
                        #region numerals
                        nnf = t;
                        break;
                        #endregion
                    }
                case Z3_ast_kind.Z3_APP_AST:
                    {
                        #region compound terms
                        FuncDecl f = GetAppDecl(t);
                        Expr[] args = GetAppArgs(t);
                        Sort tSort = t.Sort;
                        if (!tSort.Equals(BoolSort))
                        {
                            Expr[] nnf_args = Array.ConvertAll(args, arg => ToNNF1(arg, true, posCase, negCase));
                            //check if t is a tuple that trivially reconstructs itself
                            //e.g if t is a tuple the form (r.0,r.1,r.2) where r is a 3-tuple then it reduces to r
                            if (IsTrivialTupleReconstruct(t, nnf_args))
                            {
                                nnf = nnf_args[0].Args[0];
                                break;
                            }
                            else
                            {
                                nnf = MkApp(f, nnf_args);
                                break;
                            }
                        }

                        Z3_decl_kind dkind = GetZ3_decl_kind(f);
                        switch (dkind)
                        {
                            case Z3_decl_kind.Z3_OP_NOT:
                                {
                                    #region not
                                    nnf = ToNNF1(args[0], !pos, posCase, negCase);
                                    break;
                                    #endregion
                                }
                            case Z3_decl_kind.Z3_OP_OR:
                                {
                                    #region or
                                    Expr[] nnf_args = Array.ConvertAll(args, arg => ToNNF1(arg, pos, posCase, negCase));
                                    if (nnf_args.Length == 0)
                                    {
                                        nnf = (pos ? False : True);
                                    }
                                    else if (nnf_args.Length == 1)
                                    {
                                        nnf = nnf_args[0];
                                    }
                                    else
                                    {
                                        nnf = (pos ? MkOr(nnf_args) : MkAnd(nnf_args));
                                    }
                                    break;
                                    #endregion
                                }
                            case Z3_decl_kind.Z3_OP_AND:
                                {
                                    #region and
                                    Expr[] nnf_args = Array.ConvertAll(args, arg => ToNNF1(arg, pos, posCase, negCase));
                                    if (nnf_args.Length == 0)
                                    {
                                        nnf = (pos ? True : False);
                                    }
                                    else if (nnf_args.Length == 1)
                                    {
                                        nnf = nnf_args[0];
                                    }
                                    else
                                    {
                                        nnf = (pos ? MkAnd(nnf_args) : MkOr(nnf_args));
                                    }
                                    break;
                                    #endregion
                                }
                            case Z3_decl_kind.Z3_OP_ITE:
                                {
                                    #region ite
                                    var cond = ToNNF1(args[0], true, posCase, negCase);
                                    var truecase = ToNNF1(args[1], pos, posCase, negCase);
                                    var falsecase = ToNNF1(args[2], pos, posCase, negCase);
                                    nnf = MkIte(cond, truecase, falsecase);
                                    break;
                                    #endregion
                                }
                            case Z3_decl_kind.Z3_OP_TRUE:
                                {
                                    nnf = (pos ? True : False);
                                    break;
                                }
                            case Z3_decl_kind.Z3_OP_FALSE:
                                {
                                    nnf = (pos ? False : True);
                                    break;
                                }
                            case Z3_decl_kind.Z3_OP_IFF:
                                {
                                    var lhs = (BoolExpr)ToNNF1(args[0], true, posCase, negCase);
                                    var rhs = (BoolExpr)ToNNF1(args[1], true, posCase, negCase);
                                    nnf = (pos ? z3.MkIff(lhs, rhs) : MkNot(z3.MkIff(lhs, rhs)));
                                    break;
                                }
                            case Z3_decl_kind.Z3_OP_EQ:
                                {
                                    var lhs = ToNNF1(args[0], true, posCase, negCase);
                                    var rhs = ToNNF1(args[1], true, posCase, negCase);
                                    if (lhs.Equals(rhs))
                                    {
                                        nnf = (pos ? True : False);
                                    }
                                    else
                                    {
                                        nnf = (pos ? z3.MkEq(lhs, rhs) : MkNot(MkEq(lhs, rhs)));
                                    }
                                    break;
                                }
                            case Z3_decl_kind.Z3_OP_LE:
                                {
                                    var lhs = ToNNF1(args[0], true, posCase, negCase);
                                    var rhs = ToNNF1(args[1], true, posCase, negCase);
                                    if(lhs is ArithExpr )
                                        nnf = (pos ? z3.MkLe((ArithExpr)lhs, (ArithExpr)rhs) : z3.MkGt((ArithExpr)lhs, (ArithExpr)rhs));
                                    else
                                        nnf = (pos ? z3.MkBVULE((BitVecExpr)lhs, (BitVecExpr)rhs) : z3.MkBVUGT((BitVecExpr)lhs, (BitVecExpr)rhs));
                                    break;
                                }
                            case Z3_decl_kind.Z3_OP_LT:
                                {
                                    var lhs = ToNNF1(args[0], true, posCase, negCase);
                                    var rhs = ToNNF1(args[1], true, posCase, negCase);

                                    if (lhs is ArithExpr)
                                        nnf = (pos ? z3.MkLt((ArithExpr)lhs, (ArithExpr)rhs) : z3.MkGe((ArithExpr)lhs, (ArithExpr)rhs));
                                    else
                                        nnf = (pos ? z3.MkBVULT((BitVecExpr)lhs, (BitVecExpr)rhs) : z3.MkBVUGE((BitVecExpr)lhs, (BitVecExpr)rhs));
                                    break;
                                }
                            case Z3_decl_kind.Z3_OP_GE:
                                {
                                    var lhs = ToNNF1(args[0], true, posCase, negCase);
                                    var rhs = ToNNF1(args[1], true, posCase, negCase);

                                    if (lhs is ArithExpr)
                                        nnf = (pos ? z3.MkGe((ArithExpr)lhs, (ArithExpr)rhs) : z3.MkLt((ArithExpr)lhs, (ArithExpr)rhs));
                                    else
                                        nnf = (pos ? z3.MkBVUGE((BitVecExpr)lhs, (BitVecExpr)rhs) : z3.MkBVULT((BitVecExpr)lhs, (BitVecExpr)rhs));
                                    break;
                                }
                            case Z3_decl_kind.Z3_OP_GT:
                                {
                                    var lhs = ToNNF1(args[0], true, posCase, negCase);
                                    var rhs = ToNNF1(args[1], true, posCase, negCase);

                                    if (lhs is ArithExpr)
                                        nnf = (pos ? z3.MkGt((ArithExpr)lhs, (ArithExpr)rhs) : z3.MkLe((ArithExpr)lhs, (ArithExpr)rhs));
                                    else
                                        nnf = (pos ? z3.MkBVUGT((BitVecExpr)lhs, (BitVecExpr)rhs) : z3.MkBVULE((BitVecExpr)lhs, (BitVecExpr)rhs));
                                    break;
                                }
                            default:
                                {
                                    nnf = (pos ? t : MkNot(t));
                                    break;
                                }
                        }
                        break;
                        #endregion
                    }
                default:
                    {
                        nnf = (t.Sort.Equals(BoolSort) && !pos ? MkNot(t) : t);
                        break;
                    }
            }
            if (pos)
                posCase[t] = nnf;
            else
                negCase[t] = nnf;
            return nnf;
        }

        private bool IsTrivialTupleReconstruct(Expr t, Expr[] args)
        {
            if (args.Length < 1)
                return false;

            Sort tSort = t.Sort;
            if (!tSort.Name.ToString().Equals(tupleSortNamePrefix + args.Length))
                return false;

            Sort tupleSort = MkTupleSort(Array.ConvertAll(args, a => GetSort(a)));
            if (!tSort.Equals(tupleSort))
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (GetZ3_ast_kind(args[i]) != Z3_ast_kind.Z3_APP_AST)
                    return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (GetZ3_ast_kind(args[i]) != Z3_ast_kind.Z3_APP_AST || GetAppArgs(args[i]).Length != 1 || 
                    !GetAppDecl(args[i]).Equals(GetTupleField(tupleSort, i)))
                    return false;
            }

            Expr r = GetAppArgs(args[0])[0];
            if (!r.Sort.Equals(tupleSort))
                return false;

            for (int i = 1; i < args.Length; i++)
                if (!GetAppArgs(args[i])[0].Equals(r))
                    return false;

            return true;
        }

        //public Status NextSat(out Model model)
        //{
        //    return CheckAndGetModel(out model);
        //}

        public Sort GetRange(FuncDecl funcDecl)
        {
            return funcDecl.Range;
        }

        public Expr MkNumeral(int n, Sort sort)
        {
            return z3.MkNumeral(n, sort);
        }
        public Expr MkNumeral(long n, Sort sort)
        {
            return z3.MkNumeral(n, sort);
        }
        public Expr MkNumeral(string numeral, Sort sort)
        {
            return z3.MkNumeral(numeral, sort);
        }
        public Expr MkNumeral(uint n, Sort sort)
        {
            return z3.MkNumeral(n, sort);
        }
        public Expr MkNumeral(ulong n, Sort sort)
        {
            return z3.MkNumeral(n, sort);
        }

        /// <summary>
        /// All terms in ts are pairwise distinct
        /// </summary>
        public Expr MkDistinct(Expr[] ts)
        {
            return z3.MkDistinct(ts);
        }

        public Expr MkSetAdd(Expr set, Expr elem)
        {
            return z3.MkSetAdd((ArrayExpr)set, elem);
        }

        /// <summary>
        /// Make the formula that set1 is a subset of set2
        /// </summary>
        public Expr MkSetSubset(Expr set1, Expr set2)
        {
            return z3.MkSetSubset((ArrayExpr)set2, (ArrayExpr)set1);
        }

        public Z3_ast_kind GetZ3_ast_kind(Expr t)
        {
            return t.ASTKind;
        }

        public uint GetNumeralUInt(Expr t)
        {
            var ts = GetSort(t);
            if (t is IntNum)
                return ((IntNum)t).UInt;
            if (t is BitVecNum)
                return ((BitVecNum)t).UInt;
            //avoiding z3 bug: (GetSort(t) is TupleSort) == false even when t is a tuple
            if ((GetSort(t).Name.ToString().StartsWith("$")) && (GetTupleLength(GetSort(t)) == 1))
            {
                var v = GetAppArgs(t)[0];
                if (v is IntNum)
                    return ((IntNum)v).UInt;
                if (v is BitVecNum)
                    return ((BitVecNum)v).UInt;
                throw new AutomataException(AutomataExceptionKind.UnexpectedSort);
            }
            throw new AutomataException(AutomataExceptionKind.UnexpectedSort);
        }

        public ulong GetNumeralUInt64(Expr t)
        {
            if (t is IntNum)
                return ((IntNum)t).UInt64;
            if (t is BitVecNum)
                return ((BitVecNum)t).UInt64;
            throw new AutomataException("Wrong expr");
        }

        public int GetNumeralInt(Expr t)
        {
            if (t is IntNum)
                return ((IntNum)t).Int;
            if (t is BitVecNum)
                return ((BitVecNum)t).Int;
            throw new AutomataException("Wrong expr");
        }

        public long GetNumeralInt64(Expr t)
        {
            if (t is IntNum)
                return ((IntNum)t).Int64;
            if (t is BitVecNum)
                return ((BitVecNum)t).Int64;
            throw new AutomataException("Wrong expr");
        }

        public Sort MkBitVecSort(uint sz)
        {
            return z3.MkBitVecSort(sz);
        }

        /// <summary>
        /// Ite(cond,t,f) equals t, if cond is true; equals f, otherwise.
        /// </summary>
        public Expr MkIte(Expr cond, Expr t, Expr f)
        {
            return z3.MkITE((BoolExpr)cond, t, f);
        }

        /// <summary>
        /// Bitwise and of bitvectors
        /// </summary>
        public Expr MkBvAnd(Expr t1, Expr t2)
        {
            return z3.MkBVAND((BitVecExpr)t1, (BitVecExpr)t2);
        }

        /// <summary>
        /// Bitwise or of bitvectors
        /// </summary>
        public Expr MkBvOr(Expr t1, Expr t2)
        {
            return z3.MkBVOR((BitVecExpr)t1, (BitVecExpr)t2);
        }

        ///// <summary>
        ///// Rotate right
        ///// </summary>
        //public Expr MkBvRotateRight(uint i, Expr t2)
        //{
        //    return z3.MkBvRotateRight(i, t2);
        //}

        /// <summary>
        /// Logical shift right.
        /// Equivalent to (unsigned) division of t by 2^i.
        /// </summary>
        public Expr MkBvShiftRight(uint i, Expr t)
        {
            return z3.MkBVLSHR((BitVecExpr)t, z3.MkBV(i, ((BitVecSort)t.Sort).Size));
        }

        /// <summary>
        /// Shift left. Equivalent to multiplication of t by 2^i.
        /// </summary>
        public Expr MkBvShiftLeft(uint i, Expr t)
        {
            return z3.MkBVSHL((BitVecExpr)t, z3.MkBV(i, ((BitVecSort)t.Sort).Size));
        }

        /// <summary>
        /// Make a fresh constant of the given sort.
        /// </summary>
        public Expr MkFreshConst(string prefix, Sort sort)
        {
            return MkApp(MkFreshFuncDecl(prefix, new Sort[0], sort), new Expr[0]);
        }

        /// <summary>
        /// Make a fresh constant of the same sort ast the sort of t, 
        /// shorthand for MkFreshConst(prefix, GetSort(t)).
        /// </summary>
        public Expr MkFreshConst(string prefix, Expr t)
        {
            return MkFreshConst(prefix, GetSort(t));
        }

        /// <summary>
        /// Make an uninterpreted function declaration fname: domain -> range.
        /// Assumes that no uninterpreted function with the given types already exists.
        /// </summary>
        public FuncDecl MkFuncDecl(string fname, Sort domain, Sort range)
        {
            FuncDecl f;
            if (declaredFuncDecls.TryGetFuncDecl(out f, fname, domain))
                throw new AutomataException(AutomataExceptionKind.FunctionIsAlreadyDeclared);

            f = z3.MkFuncDecl(fname, domain, range);
            declaredFuncDecls.AddFuncDecl(f, fname, domain);
            return f;
        }

        public FuncDecl MkFuncDecl(string fname, Sort[] domain, Sort range)
        {
            FuncDecl f;
            if (declaredFuncDecls.TryGetFuncDecl(out f, fname, (object[])domain))
                throw new AutomataException(AutomataExceptionKind.FunctionIsAlreadyDeclared);

            f = z3.MkFuncDecl(fname, domain, range);
            declaredFuncDecls.AddFuncDecl(f, fname, (object[])domain);
            return f;
        }

        public FuncDecl MkFreshFuncDecl(string prefix, Sort[] domain, Sort range)
        {
            FuncDecl f = z3.MkFreshFuncDecl(prefix, domain, range);
            //ec.AddFuncDecl(f, f.Name.ToString(), (object[])f.Domain);
            return f;
        }

        /// <summary>
        /// Gets the name of the function declaration.
        /// </summary>
        public string GetDeclName(FuncDecl f)
        {
            return f.Name.ToString();
        }

        /// <summary>
        /// Gets the name of the function declaration by omitting the parameter suffix if there is one.
        /// </summary>
        public string GetDeclShortName(FuncDecl f)
        {
            string[] split = System.Text.RegularExpressions.Regex.Split(f.Name.ToString(), @"\[.*\]$");
            return split[0];
        }

        /// <summary>
        /// Gets the function declaration parameters if the name has a parameter suffix [p_1:p_2:...:p_k].
        /// </summary>
        public uint[] GetDeclParameters(FuncDecl f)
        {
            var p = f.Parameters;
            uint[] res = new uint[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                if (!uint.TryParse(p[i].ToString(), out res[i]))
                    return new uint[] { }; //in the built-in case some parameters may be non-uints
            }
            if (p.Length == 0)
            {
                //see if p mathces the pattern ...[n:m:...:k]
                string[] split = System.Text.RegularExpressions.Regex.Split(f.Name.ToString(), @"\[");
                if (split.Length == 2 && split[1].EndsWith("]")) 
                {
                    string ps = split[1].Remove(split[1].Length - 1);
                    try
                    {
                        res = Array.ConvertAll(ps.Split(':'), uint.Parse);
                    }
                    catch
                    {
                        throw new AutomataException(AutomataExceptionKind.InvalidArguments);
                    }
                }
            }
            return res;
        }


        public Sort[] GetDomain(FuncDecl f)
        {
            return f.Domain;
        }

        /// <summary>
        /// Make a term that tests that the list is nonempty.
        /// </summary>
        public Expr MkIsCons(Expr list)
        {
            Sort listSort = GetSort(list);
            return MkApp(GetIsCons(listSort), list);
        }

        /// <summary>
        /// Make a term that tests that the list is empty.
        /// </summary>
        public Expr MkIsNil(Expr list)
        {
            Sort listSort = GetSort(list);
            return MkApp(GetIsNil(listSort), list);
        }

        /// <summary>
        /// Make a list sort with the given name and element sort
        /// </summary>
        Sort MkListSort(string name, Sort elemSort)
        {            
            ListSort listSort = z3.MkListSort(name, elemSort);
            return listSort;
        }

        /// <summary>
        /// Make the sort for T ::= Tree(N, S) | Leaf(L), S := Cons(T, S) | 
        /// </summary>
        /// <param name="name">name of the sort T, name of the sort S is name + "LIST"</param>
        /// <param name="nodeSort">sort of N</param>
        /// <param name="leafSort">sort of L</param>
        /// <returns>the resulting tree sort</returns>
        public Sort MkTreeSort(string name, Sort nodeSort, Sort leafSort)
        {
            Sort tree_sort;
            var key = new Tuple<string, Sort>(name, nodeSort);
            if (treeSortMap.TryGetValue(key, out tree_sort))
                return tree_sort;

            FuncDecl root_decl, children_decl, tree_decl, leaf_decl, getLeaf_decl, isNode_decl, isLeaf_decl;
            FuncDecl empty_decl, first_decl, rest_decl, cons_decl, isEmpty_decl, isCons_decl;

            string[] node_subtrees = new string[] { "GetNode", "GetSubtrees" };
            Sort[] node_subtrees_sorts = new Sort[] { nodeSort, null };
            uint[] node_subtrees_sorts_refs = new uint[] { 0, 0 }; //the first unknown sort is TreeList with index 0
            string[] leaf = new string[] { "GetLeaf" };
            Sort[] leaf_sorts = new Sort[] { leafSort };
            uint[] leaf_sorts_refs = new uint[]{0};

            string[] first_rest = new string[] { "GetFirst", "GetRest" };
            Sort[] first_rest_sorts = new Sort[] { null, null };
            uint[] first_rest_sorts_refs = new uint[] { 1, 0 }; //the first unknown sort is Tree and the second is TreeList

            var tree_con = z3.MkConstructor("Tree", "IsTree", node_subtrees, node_subtrees_sorts, node_subtrees_sorts_refs);
            var leaf_con = z3.MkConstructor("Leaf", "IsLeaf", leaf, leaf_sorts, leaf_sorts_refs);

            var empty_con = z3.MkConstructor("Empty", "IsEmpty", new string[]{}, new Sort[]{}, new uint[]{});
            var cons_con = z3.MkConstructor("Cons", "IsCons", first_rest, first_rest_sorts, first_rest_sorts_refs);

            //the indices above are determined by their order in the arrays for creating the sorts
            var tree_constructors = new Constructor[] { tree_con, leaf_con };
            var treeList_constructors = new Constructor[] { empty_con, cons_con };
            var sort_names = new string[] { name + "LIST", name };
            var sort_constructors = new Constructor[][] { treeList_constructors, tree_constructors };
            var sorts = z3.MkDatatypeSorts(sort_names, sort_constructors);
            tree_sort = sorts[1];
            var treeList_sort = sorts[0];

            tree_decl = tree_con.ConstructorDecl;
            var treeAccessors = tree_con.AccessorDecls;
            root_decl = treeAccessors[0];
            children_decl = treeAccessors[1];
            isNode_decl = tree_con.TesterDecl;

            leaf_decl = leaf_con.ConstructorDecl;
            var leafAccessors = leaf_con.AccessorDecls;
            getLeaf_decl = leafAccessors[0];
            isLeaf_decl = leaf_con.TesterDecl;

            cons_decl = cons_con.ConstructorDecl;
            empty_decl = empty_con.ConstructorDecl;
            var emptyTreeList = MkApp(empty_decl);
            var treeListAccessors = cons_con.AccessorDecls;
            first_decl = treeListAccessors[0];
            rest_decl = treeListAccessors[1];
            isEmpty_decl = empty_con.TesterDecl;
            isCons_decl = cons_con.TesterDecl;

            treeInfoMap[tree_sort] =
                new UnrankedTreeInfo(treeList_sort, root_decl, children_decl, tree_decl,
                    leaf_decl, getLeaf_decl, isNode_decl, isLeaf_decl,
                    emptyTreeList, first_decl, rest_decl,
                    cons_decl, isEmpty_decl, isCons_decl);

            treeSortMap[key] = tree_sort;
            return tree_sort;
        }

        private void InitSuccArithmetic()
        {
            Sort[] fieldSorts = new Sort[1];
            uint[] fieldRefs = new uint[1];
            Constructor Zcon = z3.MkConstructor("0", "=0", new string[]{}, new Sort[]{}, new uint[]{});
            Constructor Scon = z3.MkConstructor("+1", ">0", new string[] { "-1" }, fieldSorts, fieldRefs);
            succNumberSort = z3.MkDatatypeSort("SuccNumber", new Constructor[] { Zcon, Scon });
            succZero = MkApp(Zcon.ConstructorDecl);
            succIncr = Scon.ConstructorDecl;
            succDecr = Scon.AccessorDecls[0];
        }
        
        #endregion


        #region Axiom creation, hides pattern creation
        /// <summary>
        /// Creates and asserts the axiom 'Forall vars (lhs[vars] = rhs[vars])'.
        /// Assumes that lhs and rhs have the same sort.
        /// Assumes that vars is an ordered sequence of all the variables that occur in lhs
        /// and that vars[i] is a variable with index i for i = 0,..,vars.Length-1.
        /// Assumes that all variables in rhs occur in vars.
        /// Assumes that the function symbol of lhs is an uninterpreted function symbol.
        /// </summary>
        /// <param name="lhs">term whose function symbol is uninterpreted</param>
        /// <param name="rhs">term that defines the meaning of lhs</param>
        /// <param name="vars">a sequence of distinct variables</param>
        public void AssertAxiom(Expr lhs, Expr rhs, params Expr[] vars)
        {
            Z3_decl_kind dkind = GetAppDecl(lhs).DeclKind;
            if (GetAppDecl(lhs).DeclKind != Z3_decl_kind.Z3_OP_UNINTERPRETED)
                throw new AutomataException(AutomataExceptionKind.AssertAxiom_UnexpectedFuncSymbol);

            if (!GetSort(lhs).Equals(GetSort(rhs)))
                throw new AutomataException(AutomataExceptionKind.AssertAxiom_SortMismatch);

            //for (uint i = 0; i < vars.Length; i++)
            //    if (GetVarIndex(vars[i]) != i)
            //        throw new AutomataException(AutomataExceptionKind.AssertAxiom_IncorrectVariables);

            HashSet<Expr> allVars = new HashSet<Expr>(vars);

            var lvars = new List<Expr>( GetVars(lhs));
            var rvars = new List<Expr>(GetVars(rhs));

            bool ok = (allVars.Count == vars.Length &&
                allVars.SetEquals(GetVars(lhs)) &&
                allVars.IsSupersetOf(GetVars(rhs)));

            if (!ok)
                throw new AutomataException(AutomataExceptionKind.AssertAxiom_IncorrectVariables);

            this.MainSolver.Assert(MkEqForall(lhs, rhs, vars));
        }


        /// <summary>
        /// Creates and the axiom 'Forall vars (lhs[vars] = rhs[vars])'.
        /// Assumes that lhs and rhs have the same sort.
        /// Assumes that all variables in rhs occur in vars.
        /// Assumes that the function symbol of lhs is an uninterpreted function symbol.
        /// </summary>
        /// <param name="lhs">term whose function symbol is uninterpreted</param>
        /// <param name="rhs">term that defines the meaning of lhs</param>
        /// <param name="vars">a sequence of distinct variables</param>
        public Expr MkAxiom(Expr lhs, Expr rhs, params Expr[] vars)
        {
            Z3_decl_kind dkind = GetAppDecl(lhs).DeclKind;
            if (GetAppDecl(lhs).DeclKind != Z3_decl_kind.Z3_OP_UNINTERPRETED)
                throw new AutomataException(AutomataExceptionKind.AssertAxiom_UnexpectedFuncSymbol);

            if (!GetSort(lhs).Equals(GetSort(rhs)))
                throw new AutomataException(AutomataExceptionKind.AssertAxiom_SortMismatch);

            HashSet<Expr> allVars = new HashSet<Expr>(vars);

            var lvars = new List<Expr>(GetVars(lhs));
            var rvars = new List<Expr>(GetVars(rhs));

            bool ok = (allVars.Count == vars.Length &&
                allVars.SetEquals(GetVars(lhs)) &&
                allVars.IsSupersetOf(GetVars(rhs)));

            if (!ok)
                throw new AutomataException(AutomataExceptionKind.AssertAxiom_IncorrectVariables);

            var ax = MkEqForall(lhs, rhs, vars);

            return ax;
        }

        /// <summary>
        /// Creates the axiom 'Forall bvs (lhs = rhs)'.
        /// </summary>
        public Expr MkEqForall(Expr lhs, Expr rhs, params Expr[] variables)
        {
            if (variables == null || variables.Length == 0)
                return MkEq(lhs, rhs);

            Expr[] bv_Names = new Expr[variables.Length];
            Sort[] bv_Sorts = new Sort[variables.Length];
            Dictionary<Expr,Expr> subs = new Dictionary<Expr,Expr>();
            for (int i = 0; i < variables.Length; i++)
            {
                bv_Names[i] = z3.MkConst(z3.MkSymbol("#" + i.ToString()),variables[i].Sort);
                bv_Sorts[i] = variables[i].Sort;
                subs[variables[i]]=bv_Names[i];
            }
            Expr body = MkEq(lhs, rhs);
            Pattern pat = z3.MkPattern(new Expr[] { lhs });            

            Array.Reverse(bv_Names);
            Array.Reverse(bv_Sorts);

            var body1 = ApplySubstitution(body, subs);

            var pat1 = z3.MkPattern(new Expr[] { ApplySubstitution(lhs, subs)});    
            Expr ax = z3.MkForall(bv_Names, body1, 1, new Pattern[] { pat1 });
                //z3.MkForall(bv_Sorts, bv_Names, body, 0, new Pattern[] { pat });

            return ax;
        }
        #endregion

        #region enum definitions

        Dictionary<Sort, Expr[]> enum_elems_map = new Dictionary<Sort, Expr[]>();
        Dictionary<string, Sort> enum_sort_map = new Dictionary<string, Sort>();

        /// <summary>
        /// Make a new enumeration sort with the given name.
        /// </summary>
        /// <param name="name">name of the enumeration sort</param>
        /// <param name="enum_elem_names">elements of the enumeration sort</param>
        /// <returns></returns>
        public Sort MkEnumSort(string name, params string[] enum_elem_names)
        {
            if (string.IsNullOrEmpty(name) || enum_elem_names.Length == 0 || Array.Exists(enum_elem_names, string.IsNullOrEmpty))
                throw new AutomataException(AutomataExceptionKind.InvalidEnumDeclaration);

            if (enum_sort_map.ContainsKey(name))
                throw new AutomataException(AutomataExceptionKind.EnumWithThisNameIsAlreadyDefined);

            var enum_sort = z3.MkEnumSort(name, enum_elem_names);
            FuncDecl[] enum_consts = enum_sort.ConstDecls;
            FuncDecl[] enum_testers = enum_sort.TesterDecls;

            var enum_elems = Array.ConvertAll(enum_consts, c => MkApp(c));
            enum_elems_map[enum_sort] = enum_elems;
            enum_sort_map[name] = enum_sort;
            return enum_sort;
        }

        /// <summary>
        /// Enumerates all the members of the enum with the given name.
        /// </summary>
        /// <param name="enum_name">name of an enum</param>
        public IEnumerable<string> GetEnumElements(string enum_name)
        {
            foreach (var t in enum_elems_map[GetEnumSort(enum_name)])
                yield return t.FuncDecl.Name.ToString();
        }

        /// <summary>
        /// Gets the number of elements in the enum sort.
        /// </summary>
        /// <param name="enum_sort">given enum sort</param>
        public int GetEnumCount(Sort enum_sort)
        {
            Expr[] enum_elems;
            if (!enum_elems_map.TryGetValue(enum_sort, out enum_elems))
                throw new AutomataException(AutomataExceptionKind.UnexpectedSort);
            return enum_elems.Length;
        }

        /// <summary>
        /// Gets the enum sort of the given name.
        /// </summary>
        /// <param name="enum_name">name of an enum</param>
        public Sort GetEnumSort(string enum_name)
        {
            Sort enum_sort;
            if (!enum_sort_map.TryGetValue(enum_name, out enum_sort))
                throw new AutomataException(AutomataExceptionKind.UndefinedEnum);
            return enum_sort;
        }

        /// <summary>
        /// Gets the enum element.
        /// </summary>
        /// <param name="enum_name">name of enum</param>
        /// <param name="enum_element_name">name of element in the enum</param>
        public Expr GetEnumElement(string enum_name, string enum_element_name)
        {
            Expr[] enum_elems = enum_elems_map[GetEnumSort(enum_name)];
            for (int i = 0; i < enum_elems.Length; i++)
                if (enum_elems[i].FuncDecl.Name.ToString().Equals(enum_element_name))
                    return enum_elems[i];
            throw new AutomataException(AutomataExceptionKind.UndefinedEnumElement);
        }
        #endregion

        //-----------------------------------------------------------
        //--- The following functions do not refer to z3 directly ---
        //-----------------------------------------------------------

        #region Derived Z3 Wrappers (use the above direct wrappers)

        /// <summary>
        /// Makes a tuple sort with default constructor name of the sorts in elemSorts.
        /// </summary>
        /// <param name="elemSorts">given element sorts</param>
        public Sort MkTupleSort(params Sort[] elemSorts)
        {
            return MkTupleSort(tupleSortNamePrefix + elemSorts.Length.ToString(), elemSorts);
        }

        /// <summary>
        /// Return true iff the sort is a tuple sort.
        /// </summary>
        public bool IsTupleSort(Sort sort)
        {
            var dsort = sort as DatatypeSort;
            if (dsort == null)
                return false;
            if (dsort.NumConstructors == 1)
                return true;
            else
                return false;
        }

        /// <summary>
        ///  Makes a tuple sort of the sorts in elemSorts.
        /// </summary>
        /// <param name="name">constructor name of the tuple</param>
        /// <param name="elemSorts">given element sorts</param>
        public Sort MkTupleSort(string name, params Sort[] elemSorts)
        {
            string[] projectionNames = new string[elemSorts.Length];
            FuncDecl[] projections = new FuncDecl[elemSorts.Length];
            for (int i = 0; i < elemSorts.Length; i++)
                projectionNames[i] = i.ToString(); //REMOVED "."
            FuncDecl f;
            Sort s = MkTupleSort(name, projectionNames, elemSorts, out f, out projections);
            return s;
        }

        /// <summary>
        /// Makes a tuple with a default constructor name of the terms in ts, ts must me nonempty.
        /// </summary>
        /// <param name="ts">nonempty parameter list of terms</param>
        /// <returns>tuple of the terms in ts</returns>
        public Expr MkTuple(params Expr[] ts)
        {
            Sort[] sorts = new Sort[ts.Length];
            for (int i = 0; i < ts.Length; i++)
                sorts[i] = GetSort(ts[i]);
            Sort tupleSort = MkTupleSort(sorts);
            Expr tuple = MkApp(GetTupleConstructor(tupleSort), ts);
            return tuple;
        }

        /// <summary>
        /// Makes a tuple of the terms in ts, ts must me nonempty.
        /// </summary>
        /// <param name="name">name of the tuple constructor</param>
        /// <param name="ts">nonempty parameter list of terms</param>
        /// <returns>tuple of the terms in ts</returns>
        public Expr MkTuple(string name, params Expr[] ts)
        {
            Sort[] sorts = new Sort[ts.Length];
            for (int i = 0; i < ts.Length; i++)
                sorts[i] = GetSort(ts[i]);
            Sort tupleSort = MkTupleSort(name, sorts);
            Expr tuple = MkApp(GetTupleConstructor(tupleSort), ts);
            return tuple;
        }

        /// <summary>
        /// Makes a list with the default name 'List' of the terms. 
        /// Assumes that all terms in ts have the same sort and that ts is nonempty.
        /// </summary>
        public Expr MkList(params Expr[] ts)
        {
            //Contract.Requires(ts.Length > 0);
            return MkListWithRest(ts, GetNil(MkListSort(GetSort(ts[0]))));
        }

        /// <summary>
        /// Makes a list with the default name 'List' of the terms. 
        /// </summary>
        public Expr MkListFromString(string inputElems, Sort elemSort)
        {
          Expr[] ts = new Expr[inputElems.Length];
          for (int i = 0; i < ts.Length; i++)
            ts[i] = MkNumeral((int)inputElems[i], elemSort);
          return MkListWithRest(ts, GetNil(MkListSort(elemSort)));
        }

        /// <summary>
        /// Makes a list of the terms in elems (may be null or empty), with rest as the remaining list.
        /// Assumes that all terms in elems have the same sort 
        /// and that rest is a term of the corresponding list sort.
        /// </summary>
        public Expr MkListWithRest(Expr[] elems, Expr rest)
        {
            if (elems == null || elems.Length == 0)
                return rest;

            FuncDecl cons = GetCons(GetSort(rest));
            Expr list = rest;
            for (int i = elems.Length - 1; i >= 0; i--)
                list = MkApp(cons, elems[i], list);

            return list;
        }

        /// <summary>
        /// Returns true iff t is a ground term (contains no free variables).
        /// </summary>
        public bool IsGround(Expr t)
        {
            foreach (var v in GetVars(t))
                return false;
            return true;
        }

        /// <summary>
        /// Enumerates all variables in t without repetitions, 
        /// assumes t does not contain quantifiers
        /// </summary>
        public IEnumerable<Expr> GetVars(Expr t)
        {
            return GetVars1(t, new Dictionary<Expr, bool>());
        }

        private IEnumerable<Expr> GetVars1(Expr t, Dictionary<Expr, bool> traversed)
        {
            if (traversed.ContainsKey(t))
                yield break;
            else
            {
                traversed.Add(t, true);
                Z3_ast_kind kind = t.ASTKind;
                if (IsVar(t))
                    yield return t;
                else if (kind == Z3_ast_kind.Z3_APP_AST)
                    foreach (Expr s in GetAppArgs(t))
                        foreach (Expr v in GetVars1(s, traversed))
                            yield return v;
                else if (kind == Z3_ast_kind.Z3_QUANTIFIER_AST)
                {
                    var e = t as Quantifier;
                    foreach (Expr v in GetVars1(e.Body, traversed))
                        if (!Array.Exists(e.Args, x => x.Equals(v)))
                            yield return v;
                }
                else
                    yield break;
            }
        }

        //public FuncDecl MkPredicateDecl(Expr definition, params Sort[] bvSorts)
        //{
        //    FuncDecl pred;
        //    if (ec.TryGetFuncDecl(out pred, "Predicate", definition))
        //        return pred;

        //    pred = MkFreshFuncDecl("Predicate", bvSorts, BoolSort);
        //    ec.AddFuncDecl(pred, "Predicate", definition);

        //    Expr[] bvs = new Expr[bvSorts.Length];
        //    for (int i = 0; i < bvs.Length; i++)
        //        bvs[i] = MkBound((uint)i, bvSorts[i]);
        //    Expr ax = MkEqForall(MkApp(pred, bvs), definition, bvs);
        //    AssertCnstr(ax);
        //    return pred;
        //}

        public Expr MkInt(int k)
        {
            return MkNumeral(k, IntSort);
        }

        public Status CheckSatisfiability(Expr formula, Expr freeVar)
        {
            MainSolver.Push();
            Sort sort = GetSort(freeVar);
            Expr tmp = MkFreshConst("tmp", sort);
            Expr formula_inst = ApplySubstitution(formula, freeVar, tmp);
            MainSolver.Assert(formula_inst);
            Status sat = Check();
            MainSolver.Pop();
            return sat;
        }


       /// <summary>
        /// Make the term that projects the p'th element from tuple z
       /// </summary>
       /// <param name="p">id of the element to be projected, must be between 0 and arity(tuple)-1</param>
       /// <param name="z">the given tuple term</param>
       /// <returns></returns>
        public Expr MkProj(int p, Expr z)
        {
            Sort s = GetSort(z);
            Z3_ast_kind tkind = GetZ3_ast_kind(z);
            if (tkind == Z3_ast_kind.Z3_APP_AST && GetAppDecl(z).Equals(GetTupleConstructor(s)))
                return GetAppArgs(z)[p];
            else
            {                
                FuncDecl f = GetTupleField(s, p);
                Expr t = MkApp(f, z);
                return t;
            }
        }

        public Expr MkNeq(Expr t1, Expr t2)
        {
            return MkNot(MkEq(t1, t2));
        }
        #endregion

        #region Substitution

        public Expr ApplySubstitution(Expr t, IDictionary<Expr, Expr> substitution)
        {
            Expr v;
            if (substitution.TryGetValue(t, out v))
                return v;

            Z3_ast_kind kind = GetZ3_ast_kind(t);
            if (kind != Z3_ast_kind.Z3_APP_AST)
                return t;

            FuncDecl f = GetAppDecl(t);
            Expr[] args = GetAppArgs(t);
            Expr[] args1 = new Expr[args.Length];
            for (int i = 0; i < args.Length; i++)
                args1[i] = ApplySubstitution(args[i], substitution);

            //if (GetZ3_decl_kind(f) == Z3_decl_kind.Z3_OP_Eq && args1[0].Equals(args1[1]))
            //    return True;

            Expr t1 = MkApp(f, args1);
            substitution[t] = t1;
            return t1;
        }

        public Expr ApplySubstitution(Expr t, Expr key, Expr val)
        {
            //return t.Substitute(key, val);
            Dictionary<Expr, Expr> subst = new Dictionary<Expr, Expr>();
            subst.Add(key, val);
            return ApplySubstitution(t, subst);
        }

        public Expr ApplySubstitution(Expr t, Expr[] keys, Expr[] vals)
        {
            //return t.Substitute(keys, vals);
            Dictionary<Expr, Expr> subst = new Dictionary<Expr, Expr>();
            for (int i = 0; i < keys.Length; i++)
                subst.Add(keys[i], vals[i]);
            return ApplySubstitution(t, subst);
        }

        public Expr ApplySubstitution(Expr t, Expr key1, Expr val1, Expr key2, Expr val2)
        {
            //return t.Substitute(new Expr[] { key1, key2 }, new Expr[] { val1, val2 });
            Dictionary<Expr, Expr> subst = new Dictionary<Expr, Expr>();
            subst.Add(key1, val1);
            subst.Add(key2, val2);
            return ApplySubstitution(t, subst);
        }

        #endregion

        #region Successor numbers
        public Sort SuccNumberSort
        {
            get
            {
                if (succNumberSort != null)
                    return succNumberSort;
                InitSuccArithmetic();
                return succNumberSort;
            }

        }
        public Expr SuccZero
        {
            get
            {
                if (succZero != null)
                    return succZero;
                InitSuccArithmetic();
                return succZero;
            }
        }
        public Expr MkSuccessor(Expr succNumber)
        {
            //if (presburgerIncr == null)
            //    InitPresburgerArithmetic();
            return MkApp(succIncr, succNumber);
        }
        /// <summary>
        /// Assumes that the argument is a positive successor number
        /// </summary>
        public Expr MkPredecessor(Expr succNumber)
        {
            //if (presburberDecr == null)
            //    InitPresburgerArithmetic();
            return MkApp(succDecr, succNumber);
        }

        public Expr MkSuccNumber(int n)
        {
            Expr t = SuccZero;
            while (n-- > 0)
                t = MkSuccessor(t);
            return t;
        }
        #endregion

        #region Lists

        public FuncDecl GetCons(Sort listSort)
        {
            var ls = listSort as ListSort;
            if (ls != null)
            {
                return ls.ConsDecl;
            }
            else
            {
                var ds = listSort as DatatypeSort;
                if (ds == null || ds.Constructors.Length != 2)
                    throw new AutomataException(AutomataExceptionKind.NotListSort);
                return ds.Constructors[1];
            }
        }
        public Expr GetNil(Sort listSort)
        {
            var ls = listSort as ListSort;
            if (ls != null)
            {
                return ls.Nil;
            }
            else
            {
                var ds = listSort as DatatypeSort;
                if (ds == null || ds.Constructors.Length != 2)
                    throw new AutomataException(AutomataExceptionKind.NotListSort);
                FuncDecl nilDecl =  ds.Constructors[0];
                var nil = MkApp(nilDecl);
                return nil;
            }
        }
        public FuncDecl GetIsCons(Sort listSort)
        {
            var ls = listSort as ListSort;
            if (ls != null)
            {
                return ls.IsConsDecl;
            }
            else
            {
                var ds = listSort as DatatypeSort;
                if (ds == null || ds.Recognizers.Length != 2)
                    throw new AutomataException(AutomataExceptionKind.NotListSort);
                return ds.Recognizers[1];
            }
        }
        public FuncDecl GetIsNil(Sort listSort)
        {
            var ls = listSort as ListSort;
            if (ls != null)
            {
                return ls.IsNilDecl;
            }
            else
            {
                var ds = listSort as DatatypeSort;
                if (ds == null || ds.Recognizers.Length != 2)
                    throw new AutomataException(AutomataExceptionKind.NotListSort);
                return ds.Recognizers[0];
            }
        }

        public FuncDecl GetFirst(Sort listSort)
        {
            var ls = listSort as ListSort;
            if (ls != null)
            {
                return ls.HeadDecl;
            }
            else
            {
                var ds = listSort as DatatypeSort;
                if (ds == null || ds.Accessors.Length != 2 || ds.Accessors[1].Length != 2)
                    throw new AutomataException(AutomataExceptionKind.NotListSort);
                return ds.Accessors[1][0];
            }
        }
        public FuncDecl GetRest(Sort listSort)
        {
            var ls = listSort as ListSort;
            if (ls != null)
            {
                return ls.TailDecl;
            }
            else
            {
                var ds = listSort as DatatypeSort;
                if (ds == null || ds.Accessors.Length != 2 || ds.Accessors[1].Length != 2)
                    throw new AutomataException(AutomataExceptionKind.NotListSort);
                return ds.Accessors[1][1];
            }
        }

        
        /// <summary>
        /// Gets the element sort of a list sort.
        /// </summary>
        public Sort GetElemSort(Sort listSort)
        {
            FuncDecl cons = GetCons(listSort);
            Sort elemSort = GetDomain(cons)[0];
            return elemSort;
        }

        /// <summary>
        /// Make a list sort with the default name 'List' and given element sort
        /// </summary>
        Dictionary<Sort, Sort> listSorts = new Dictionary<Sort, Sort>();
        public Sort MkListSort(Sort elemSort)
        {
            Sort listSort;
            if (listSorts.TryGetValue(elemSort, out listSort))
                return listSort;
            listSort = MkListSort("List", elemSort);
            listSorts[elemSort] = listSort;
            return listSort;
        }

        public bool IsListSort(Sort sort)
        {
            var ls = sort as ListSort;
            if (ls != null)
            {
                return true;
            }
            else
            {
                var ds = sort as DatatypeSort;
                if (ds != null && ds.NumConstructors == 2 && ds.Accessors[0].Length == 0 && 
                    ds.Accessors[1].Length == 2 && ds.Accessors[1][1].Range.Equals(sort))
                    return true;
                return false;
            }
        }

        //internal FuncDecl MkSlidingWindowDecl(int k, Expr paddingElem)
        //{
        //    if (k < 2)
        //        throw new ArgumentException("sliding window size must be at least 2", "k");

        //    string slideName = "Slide" + k;
        //    FuncDecl slide;
        //    if (ec.TryGetFuncDecl(out slide, "Slide" + k, paddingElem))
        //        return slide;

        //    Sort elemSort = GetSort(paddingElem);
        //    Sort listSort = MkListSort(elemSort);

        //    Sort[] projSorts = new Sort[k];
        //    for (int i = 0; i < k; i++)
        //        projSorts[i] = elemSort;

        //    Sort outElemSort = MkTupleSort(projSorts);
        //    Sort outListSort = MkListSort(outElemSort);
        //    Expr emptyOutList = GetNil(outListSort);
        //    Expr emptyList = GetNil(listSort);

        //    slide = MkFuncDecl(slideName, listSort, outListSort);

        //    var baseCase = MkEq(MkApp(slide, emptyList), emptyOutList);
        //    AssertCnstr(baseCase);

        //    for (int n = 1; n <= k; n++)
        //    {
        //        var axiom = DefineSlidingWindowAxiomCase(slide, k, n, elemSort, listSort, outElemSort, outListSort, emptyList, emptyOutList, paddingElem);
        //        AssertCnstr(axiom);
        //    }

        //    ec.AddFuncDecl(slide, slideName, paddingElem);
        //    return slide;
        //}

        //public Expr DefineSlidingWindowAxiomCase(FuncDecl slide, int k, int n,
        //          Sort elemSort, Sort listSort, Sort outElemSort,
        //          Sort outListSort, Expr emptyList, Expr emptyOutList, Expr paddingElem)
        //{
        //    Expr[] bvars = new Expr[n];
        //    for (int i = 0; i < n; i++)
        //        bvars[i] = MkBound((uint)i, elemSort);

        //    Expr restVar = MkBound((uint)n, listSort);

        //    Expr leftArg = (n < k ? emptyList : restVar);

        //    for (int i = n - 1; i >= 0; i--)
        //        leftArg = MkApp(GetCons(listSort), bvars[i], leftArg);

        //    Expr lhs = MkApp(slide, leftArg);

        //    Expr[] outElems = new Expr[n];
        //    for (int i = 0; i < n; i++)
        //    {
        //        Expr[] tupleElems = new Expr[k];
        //        for (int j = 0; j < k; j++)
        //        {
        //            if ((i+j) < n)
        //                tupleElems[j] = bvars[i+j];
        //            else
        //                tupleElems[j] = paddingElem;
        //        }
        //        outElems[i] = MkTuple(tupleElems);
        //    }

        //    Expr rhs;
        //    if (n < k)
        //    {
        //        rhs = emptyOutList;
        //        for (int i = n-1; i >=0; i--)
        //            rhs = MkApp(GetCons(outListSort), outElems[i], rhs);
        //    }
        //    else
        //    {
        //        var rhsTuple = MkTuple(bvars);
        //        var rhsRest = restVar;
        //        for (int i = bvars.Length - 1; i > 0; i--)
        //            rhsRest = MkApp(GetCons(listSort), bvars[i], rhsRest);
        //        rhs = MkApp(GetCons(outListSort), rhsTuple, MkApp(slide, rhsRest));
        //    }

        //    Expr axiom;
        //    if (n < k)
        //        axiom = MkEqForall(lhs, rhs, bvars);
        //    else
        //    {
        //        var bvars1 = new List<Expr>(bvars);
        //        bvars1.Add(restVar);
        //        axiom = MkEqForall(lhs, rhs, bvars1.ToArray());
        //    }
        //    return axiom;
        //}



        //public FuncDecl MkFoldDecl(int k, Sort elemSort)
        //{
        //    if (k < 1)
        //        throw new ArgumentException("fold bound must be positive", "k");

        //    string foldName = "Fold" + k;
        //    FuncDecl fold;
        //    if (ec.TryGetFuncDecl(out fold, foldName, elemSort))
        //        return fold;

        //    Sort ySort = MkListSort(elemSort);
        //    Sort xSort = MkListSort(ySort);

        //    Expr x = MkBound(0, xSort);
        //    Expr y = MkBound(1, ySort);

        //    fold = MkFreshFuncDecl(foldName, new Sort[] { xSort, ySort }, BoolSort);


        //    Expr lhs = MkApp(fold, x, y);

        //    Expr[] rhs_disjuncts = new Expr[k + 1];
        //    for (int n = 0; n <= k; n++)
        //        rhs_disjuncts[n] = BodyDisjunctOfFold(fold, n, x, y);

        //    Expr rhs = MkOr(rhs_disjuncts);

        //    Expr axiom = MkEqForall(lhs, rhs, x, y);
        //    AssertCnstr(axiom);

        //    ec.AddFuncDecl(fold, foldName, elemSort);
        //    return fold;
        //}

        //public FuncDecl MkFoldWitBoundDecl(int k, Sort elemSort)
        //{
        //    if (k < 1)
        //        throw new ArgumentException("fold bound must be positive", "k");

        //    string foldName = "FoldB" + k;
        //    FuncDecl fold;
        //    if (ec.TryGetFuncDecl(out fold, foldName, elemSort))
        //        return fold;

        //    Sort ySort = MkListSort(elemSort);
        //    Sort xSort = MkListSort(ySort);

        //    Expr x = MkBound(0, xSort);
        //    Expr y = MkBound(1, ySort);
        //    Expr z = MkBound(2, SuccNumberSort);

        //    fold = MkFreshFuncDecl(foldName, new Sort[] { xSort, ySort, SuccNumberSort }, BoolSort);


        //    Expr lhs = MkApp(fold, x, y, MkSuccessor(z));

        //    Expr[] rhs_disjuncts = new Expr[k + 1];
        //    for (int n = 0; n <= k; n++)
        //        rhs_disjuncts[n] = BodyDisjunctOfFoldWithBound(fold, n, x, y, z);

        //    Expr rhs = MkOr(rhs_disjuncts);

        //    Expr axiom = MkEqForall(lhs, rhs, x, y, z);

        //    AssertCnstr(axiom);

        //    Expr lhs_base = MkApp(fold, x, y, SuccZero);
        //    Expr rhs_base = MkAnd(MkEq(x, NilOf(x)), MkEq(y, NilOf(y)));

        //    Expr axiom_base = MkEqForall(lhs_base, rhs_base, x, y);

        //    AssertCnstr(axiom_base);

        //    ec.AddFuncDecl(fold, foldName, elemSort);
        //    return fold;
        //}

        private Expr BodyDisjunctOfFold(FuncDecl fold, int n, Expr x, Expr y)
        {
            if (n == 0)
                return MkAnd(MkEq(x, NilOf(x)), MkEq(y, NilOf(y)));
            else
                return MkAnd(MkNeq(x, NilOf(x)),
                             HasLengthEq(n, HeadOf(x)),
                             HasLengthGe(n, y),
                             PrefixEq(n, HeadOf(x), y),
                             MkApp(fold, TailOf(x), nthTailOf(n, y)));
        }

        private Expr BodyDisjunctOfFoldWithBound(FuncDecl fold, int n, Expr x, Expr y, Expr bound)
        {
            if (n == 0)
                return MkAnd(MkEq(x, NilOf(x)), MkEq(y, NilOf(y)));
            else
                return MkAnd(MkNeq(x, NilOf(x)),
                             HasLengthEq(n, HeadOf(x)),
                             HasLengthGe(n, y),
                             PrefixEq(n, HeadOf(x), y),
                             MkApp(fold, TailOf(x), nthTailOf(n, y), bound));
        }

        private Expr PrefixEq(int n, Expr list1, Expr list2)
        {
            if (n == 1)
                return MkEq(HeadOf(list1), HeadOf(list2));
            else
                return MkAnd(MkEq(HeadOf(list1), HeadOf(list2)), PrefixEq(n - 1, TailOf(list1), TailOf(list2)));
        }

        private Expr nthTailOf(int n, Expr list)
        {
            if (n == 0)
                return list;
            else
                return TailOf(nthTailOf(n - 1, list));
        }

        internal Expr HasLengthGe(int n, Expr list)
        {
            if (n == 1)
                return MkNeq(list, NilOf(list));
            else
                return MkAnd(MkNeq(list, NilOf(list)), HasLengthGe(n - 1, TailOf(list)));
        }

        private Expr HasLengthEq(int n, Expr list)
        {
            if (n == 1)
                return MkAnd(MkNeq(list, NilOf(list)), MkEq(TailOf(list), NilOf(list)));
            else
                return MkAnd(MkNeq(list, NilOf(list)), HasLengthEq(n - 1, TailOf(list)));
        }

        private Expr TailOf(Expr list)
        {
            return MkApp(GetRest(GetSort(list)), list);
        }

        private Expr HeadOf(Expr list)
        {
            return MkApp(GetFirst(GetSort(list)), list);
        }

        private Expr NilOf(Expr list)
        {
            return GetNil(GetSort(list));
        }

        //private Expr HasLength_iter(int n, Expr list, Expr term)
        //{
        //    if (n == 1)

        //}

        #endregion

        #region Trees
        /// <summary>
        /// Make the sort for T ::= Tree(N, S) | Leaf(L), S := Cons(T, S) | Empty. The name of sort T is "TREE", name of sort S is "TREELIST".
        /// </summary>
        /// <param name="nodeSort">sort of N</param>
        /// <param name="leafSort">sort of L</param>
        /// <returns>the resulting tree sort</returns>
        public Sort MkTreeSort(Sort nodeSort, Sort leafSort)
        {
            return MkTreeSort("TREE", nodeSort, leafSort);
        }

        public Expr MkTree(Sort treeSort, Expr node, params Expr[] children)
        {
            UnrankedTreeInfo ti = treeInfoMap[treeSort];
            Expr treeList = ti.EmptyTreeList;
            for (int i = children.Length - 1; i >= 0; i--)
                treeList = MkApp(ti.MkCons, children[i], treeList);
            Expr tree = MkApp(ti.MkNode, node, treeList);
            return tree;
        }

        public Expr MkLeaf(Sort treeSort, Expr leafValue)
        {
            UnrankedTreeInfo ti = treeInfoMap[treeSort];
            Expr leaf = MkApp(ti.MkLeaf, leafValue);
            return leaf;
        } 

        public Expr MkIsLeaf(Expr tree)
        {
            Sort treeSort = GetSort(tree);
            UnrankedTreeInfo ti = treeInfoMap[treeSort];
            Expr isLeaf = MkApp(ti.IsLeaf, tree);
            return isLeaf;
        }

        public Expr MkHasNoSubtrees(Expr tree)
        {
            Sort treeSort = GetSort(tree);
            UnrankedTreeInfo ti = treeInfoMap[treeSort];
            Expr isLeafLike = MkApp(ti.IsEmpty, MkApp(ti.GetNodeSubtrees, tree));
            return isLeafLike;
        }

        /// <summary>
        /// Creates a function Yield such that Yield(t) = list of leaf values in t where t is a tree of treeSort
        /// </summary>
        //public FuncDecl MkYieldDecl(Sort treeSort)
        //{
        //    FuncDecl yield;
        //    if (ec.TryGetFuncDecl(out yield, "Yield", treeSort))
        //        return yield;

        //    UnrankedTreeInfo ti = treeInfoMap[treeSort];
        //    Sort leafValueSort = GetRange(ti.GetLeafValue);
        //    Sort nodeValueSort = GetRange(ti.GetNodeLabel);
        //    Sort listSort = MkListSort(leafValueSort);
        //    Expr nil = GetNil(listSort);
        //    FuncDecl cons = GetCons(listSort);

        //    yield = MkFreshFuncDecl("Yield", new Sort[] { treeSort }, listSort);
        //    var yieldrec = MkFreshFuncDecl("YieldRec", new Sort[] { treeSort, listSort }, listSort);
        //    var yieldsub = MkFreshFuncDecl("YieldSub", new Sort[] { ti.TreeListSort, listSort }, listSort);

        //    ec.AddFuncDecl(yield, "Yield", treeSort);

        //    //ax0: Yield(t) = YieldRec(t,nil)
        //    Expr t0 = MkBound(0, treeSort);
        //    Expr ax0 = MkEqForall(MkApp(yield, t0), MkApp(yieldrec, t0, nil), t0);

        //    //ax1: YieldRec(Leaf(x), y) = cons(x,y)
        //    Expr x1 = MkBound(0, leafValueSort);
        //    Expr y1 = MkBound(1, listSort);
        //    Expr ax1 = MkEqForall(MkApp(yieldrec, MkApp(ti.MkLeaf, x1), y1), MkApp(cons, x1, y1),x1,y1);

        //    //ax2: YieldRec(Tree(x,s), y) = YieldSub(s,y)
        //    Expr x2 = MkBound(0, nodeValueSort);
        //    Expr s2 = MkBound(1, ti.TreeListSort);
        //    Expr y2 = MkBound(2, listSort);
        //    Expr ax2 = MkEqForall(MkApp(yieldrec, MkApp(ti.MkNode, x2, s2), y2),
        //                          MkApp(yieldsub, s2, y2), x2, s2, y2);

        //    //ax3: YieldSub(empty, y) = y
        //    Expr y3 = MkBound(0, listSort);
        //    Expr ax3 = MkEqForall(MkApp(yieldsub, ti.EmptyTreeList, y3), y3, y3);

        //    //ax4: YieldSub(Cons(t,s), y) = YieldRec(t, YieldSub(s,y))
        //    Expr t4 = MkBound(0, treeSort);
        //    Expr s4 = MkBound(1, ti.TreeListSort);
        //    Expr y4 = MkBound(2, listSort);
        //    Expr ax4 = MkEqForall(MkApp(yieldsub, MkApp(ti.MkCons, t4, s4), y4),
        //                          MkApp(yieldrec, t4, MkApp(yieldsub, s4, y4)), t4, s4, y4);

        //    AssertCnstr(ax0);
        //    AssertCnstr(ax1);
        //    AssertCnstr(ax2);
        //    AssertCnstr(ax3);
        //    AssertCnstr(ax4);

        //    return yield;
        //}

        /// <summary>
        /// Logically equivalent to MkYieldDecl but with more general patterns, may not terminate.
        /// </summary>
        //public FuncDecl MkYieldDecl2(Sort treeSort)
        //{
        //    FuncDecl yield;
        //    if (ec.TryGetFuncDecl(out yield, "Yield2", treeSort))
        //        return yield;

        //    UnrankedTreeInfo ti = treeInfoMap[treeSort];
        //    Sort leafValueSort = GetRange(ti.GetLeafValue);
        //    Sort nodeValueSort = GetRange(ti.GetNodeLabel);
        //    Sort listSort = MkListSort(leafValueSort);
        //    Expr nil = GetNil(listSort);
        //    FuncDecl cons = GetCons(listSort);

        //    yield = MkFreshFuncDecl("Yield", new Sort[] { treeSort }, listSort);
        //    var yieldrec = MkFreshFuncDecl("YieldRec", new Sort[] { treeSort, listSort }, listSort);
        //    var yieldsub = MkFreshFuncDecl("YieldSub", new Sort[] { ti.TreeListSort, listSort }, listSort);

        //    ec.AddFuncDecl(yield, "Yield", treeSort);

        //    //ax0: Yield(t) = YieldRec(t,nil)
        //    Expr t0 = MkBound(0, treeSort);
        //    Expr ax0 = MkEqForall(MkApp(yield, t0), MkApp(yieldrec, t0, nil), t0);

        //    //ax1: YieldRec(t, y) = ite(IsLeaf(t), cons(GetLeaf(t),y), YieldSub(GetSubtrees(t),y))
        //    Expr t1 = MkBound(0, treeSort);
        //    Expr y1 = MkBound(1, listSort);
        //    Expr ax1_lhs = MkApp(yieldrec, t1, y1);
        //    Expr ax1_rhs = MkIte(MkApp(ti.IsLeaf, t1),
        //                            MkApp(cons, MkApp(ti.GetLeafValue, t1), y1),
        //                            MkApp(yieldsub, MkApp(ti.GetNodeSubtrees, t1), y1));
        //    Expr ax1 = MkEqForall(ax1_lhs, ax1_rhs, t1, y1);

        //    //ax2: YieldSub(t, y) = ite(IsEmpty(t), y, YieldRec(First(t), YieldSub(Rest(t),y))
        //    Expr t2 = MkBound(0, ti.TreeListSort);
        //    Expr y2 = MkBound(1, listSort);
        //    Expr ax2_lhs = MkApp(yieldsub, t2, y2);
        //    Expr ax2_rhs = MkIte(MkApp(ti.IsEmpty, t2), y2,
        //                            MkApp(yieldrec, MkApp(ti.GetFirst, t2), MkApp(yieldsub, MkApp(ti.GetRest, t2), y2)));
        //    Expr ax2 = MkEqForall(ax2_lhs, ax2_rhs, t2, y2);

        //    AssertCnstr(ax0);
        //    AssertCnstr(ax1);
        //    AssertCnstr(ax2);

        //    return yield;
        //}

        //public Expr MkYield(Expr tree)
        //{
        //    FuncDecl yieldDecl = MkYieldDecl(GetSort(tree));
        //    Expr yield = MkApp(yieldDecl, tree);
        //    return yield;
        //}

        //public Expr MkYield2(Expr tree)
        //{
        //    FuncDecl yieldDecl = MkYieldDecl2(GetSort(tree));
        //    Expr yield = MkApp(yieldDecl, tree);
        //    return yield;
        //}

        internal UnrankedTreeInfo GetTreeInfo(Sort treeSort)
        {
            return treeInfoMap[treeSort];
        }

        internal BinaryTreeInfo GetBinaryTreeInfo(Sort treeSort)
        {
            return binaryTreeInfoMap[treeSort];
        }
        #endregion

        #region Binary Trees

        /// <summary>
        /// Make the sort for T ::= BinaryTree(T, T) | Leaf(L), the sort name is "BINARYTREE"
        /// </summary>
        /// <param name="elemSort">sort of L</param>
        /// <returns>the resulting binary tree sort</returns>
        public Sort MkBinaryTreeSort(Sort elemSort)
        {
            return MkBinaryTreeSort("BINARYTREE", elemSort);
        }

        /// <summary>
        /// Make the sort for T ::= BinaryTree(T, T) | Leaf(L)
        /// </summary>
        /// <param name="name">name of the sort T</param>
        /// <param name="leafSort">sort of L</param>
        /// <returns>the resulting binary tree sort</returns>
        public Sort MkBinaryTreeSort(string name, Sort leafSort)
        {
            Sort tree_sort;
            var key = new Tuple<string, Sort>(name, leafSort);
            if (binaryTreeSortMap.TryGetValue(key, out tree_sort))
                return tree_sort;

            FuncDecl left_decl, right_decl, tree_decl, leaf_decl, getLeaf_decl, isBinaryTree_decl, isLeaf_decl;

            string[] node_subtrees = new string[] { "GetLeft", "GetRight" };
            Sort[] node_subtrees_sorts = new Sort[] { null, null };
            uint[] node_subtrees_sorts_refs = new uint[] { 0, 0 }; //the unknown sort is BinaryTree with index 0
            string[] leaf = new string[] { "GetLeaf" };
            Sort[] leaf_sorts = new Sort[] { leafSort };
            uint[] leaf_sorts_refs = new uint[] { 0 };

            var tree_con = z3.MkConstructor("BinaryTree", "IsBinaryTree", node_subtrees, node_subtrees_sorts, node_subtrees_sorts_refs);
            var leaf_con = z3.MkConstructor("Leaf", "IsLeaf", leaf, leaf_sorts, leaf_sorts_refs);

            //the indices above are determined by their order in the arrays for creating the sorts
            var tree_constructors = new Constructor[] { tree_con, leaf_con };
            var sort_names = new string[] { name };
            var sort_constructors = new Constructor[][] { tree_constructors };
            var sorts = z3.MkDatatypeSorts(sort_names, sort_constructors);
            tree_sort = sorts[0];

            tree_decl = tree_con.ConstructorDecl;
            var treeAccessors = tree_con.AccessorDecls;
            left_decl = treeAccessors[0];
            right_decl = treeAccessors[1];
            isBinaryTree_decl = tree_con.TesterDecl;

            leaf_decl = leaf_con.ConstructorDecl;
            var leafAccessors = leaf_con.AccessorDecls;
            getLeaf_decl = leafAccessors[0];
            isLeaf_decl = leaf_con.TesterDecl;

            binaryTreeInfoMap[tree_sort] =
                new BinaryTreeInfo(tree_decl, leaf_decl, getLeaf_decl,
                    isBinaryTree_decl, isLeaf_decl, left_decl, right_decl);

            binaryTreeSortMap[key] = tree_sort;
            return tree_sort;
        }


        /// <summary>
        /// Construct the binary tree with the given left and right subtrees.
        /// </summary>
        public Expr MkBinaryTree(Sort treeSort, Expr left, Expr right)
        {
            BinaryTreeInfo ti = binaryTreeInfoMap[treeSort];
            Expr tree = MkApp(ti.MkTree, left, right);
            return tree;
        }

        /// <summary>
        /// Construct the leaf binary tree with the given leaf value
        /// </summary>
        public Expr MkBinaryTreeLeaf(Sort treeSort, Expr leafValue)
        {
            BinaryTreeInfo ti = binaryTreeInfoMap[treeSort];
            Expr leaf = MkApp(ti.MkLeaf, leafValue);
            return leaf;
        }

        /// <summary>
        /// Apply the tester that the tree is a leaf binary tree
        /// </summary>
        public Expr MkBinaryTreeIsLeaf(Expr tree)
        {
            Sort treeSort = GetSort(tree);
            BinaryTreeInfo ti = binaryTreeInfoMap[treeSort];
            Expr isLeaf = MkApp(ti.IsLeaf, tree);
            return isLeaf;
        }

        /// <summary>
        /// Apply the tester that the tree is a non-leaf binary tree
        /// </summary>
        public Expr MkBinaryTreeIsTree(Expr tree)
        {
            Sort treeSort = GetSort(tree);
            BinaryTreeInfo ti = binaryTreeInfoMap[treeSort];
            Expr isTree = MkApp(ti.IsTree, tree);
            return isTree;
        }

        /// <summary>
        /// Apply the left subtree accessor to the binary tree
        /// </summary>
        public Expr MkBinaryTreeGetLeft(Expr tree)
        {
            Sort treeSort = GetSort(tree);
            BinaryTreeInfo ti = binaryTreeInfoMap[treeSort];
            Expr left = MkApp(ti.GetLeft, tree);
            return left;
        }

        /// <summary>
        /// Apply the right subtree accessor to the binary tree
        /// </summary>
        public Expr MkBinaryTreeGetRight(Expr tree)
        {
            Sort treeSort = GetSort(tree);
            BinaryTreeInfo ti = binaryTreeInfoMap[treeSort];
            Expr right = MkApp(ti.GetRight, tree);
            return right;
        }

        #endregion

        #region model generation



        #endregion

        #region character constraints
        //public Expr MkRangeConstraint(bool caseInsensitive, char lower, char upper)
        //{
        //    //TBD: case insensitivity
        //    return MkInCharRange(lower, upper);
        //}

        //public Expr MkCharConstraint(bool caseInsensitive, char c)
        //{
        //    //TBD: case insensitivity
        //    return MkInCharRange(c, c);
        //}

        //public Expr MkRangesConstraint(bool caseInsensitive, IEnumerable<char[]> ranges)
        //{
        //    Expr res = False;
        //    foreach (var range in ranges)
        //    {
        //        res = MkOr(res, MkRangeConstraint(caseInsensitive, range[0], range[1]));
        //    }
        //    return res;
        //}

        //        /// <summary>
        ///// Make a constraint for 'a lte x & x lte b'
        ///// </summary>
        //public Expr MkInCharRange(char a, char b)
        //{
        //    return MkInCharRange(false, a, b);
        //}

        ///// <summary>
        ///// Make a constraint for '~(a lte x & x lte b)'
        ///// </summary>
        //Expr MkInCharRange(bool negate, char a, char b)
        //{
        //    Expr x = _char;
        //    if (a == b)
        //    {
        //        return (negate ? z3p.MkNeq(x, MkChar(a)) : z3p.MkEq(x, MkChar(a)));
        //    }

        //    Expr cond = (negate 
        //        ? z3p.MkOr(MkCharLt(x, MkChar(a)), MkCharLt(MkChar(b), x))
        //        : z3p.MkAnd(MkCharLe(MkChar(a), x), MkCharLe(x, MkChar(b))));
        //    return cond;
        //}

        #endregion

        #region RegEx and Seq


        public Sort GetReElemSort(Sort seqSort)
        {
            return ___Hack(seqSort);
        }

        public Sort GetSeqElemSort(Sort reSort)
        {
            return ___Hack(reSort);
        }

        private Sort ___Hack(Sort s)
        {
            var sname = s.ToString();
            int i = sname.IndexOf("bv ");
            int k = int.Parse(sname.Substring(i + 3, sname.Length - i - 5));
            Sort charSort = z3.MkBitVecSort((uint)k);
            return charSort;
        }

        #endregion

        #region ISMTSolver<FuncDecl,Expr,Sort> Members

        /// <summary>
        /// The sort MkTupleSort() containing the single fixed element UnitConst = MkTuple().
        /// </summary>
        public Sort UnitSort { get { return unitSort; } }

        /// <summary>
        /// The single fixed element MkTuple() of sort UnitSort = MkTupleSort().
        /// </summary>
        public Expr UnitConst { get { return unitConst; } }

        public Expr MkHexProj(int n, Expr c)
        {
            Sort s = GetSort(c);
            if (!(s is BitVecSort))
                throw new AutomataException(AutomataExceptionKind.SortMismatch);

            var maskExpr = (BitVecExpr)(z3.MkNumeral(0xF, s));
            var c1 = (n == 0 ? (BitVecExpr)c : z3.MkBVRotateRight((uint)(n * 4), (BitVecExpr)c));
            var res = z3.MkBVAND(c1, maskExpr);
            return res;
        }

        public Expr MkHexProj(int n, Expr c, Sort resSort)
        {
            Sort s = GetSort(c);
            if (!(s is BitVecSort) || !(resSort is BitVecSort))
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            var maskExpr = (BitVecExpr)z3.MkNumeral(0xF, s);
            var c1 = (n == 0 ? (BitVecExpr)c : z3.MkBVRotateRight((uint)(n * 4), (BitVecExpr)c));
            var res = (BitVecExpr)Simplify(z3.MkBVAND(c1, maskExpr));
            if (!resSort.Equals(s))
            {
                //convert the bitvector to resSort
                uint m = ((BitVecSort)s).Size;
                uint k = ((BitVecSort)resSort).Size;
                if (k > m)
                    throw new AutomataException(AutomataExceptionKind.InvalidArguments);
                //extract the bits
                res = (BitVecExpr)Simplify(z3.MkExtract(k - 1, 0, res));
            }
            return res;
        }

        /// <summary>
        /// Convert bv to the given sort (by appending 0's at the beginning when the given sort has more bits).
        /// </summary>
        public Expr ConvertBitVector(Expr bv, Sort sort)
        {
            Sort bv_sort = GetSort(bv);
            if (!(bv_sort is BitVecSort) || !(sort is BitVecSort))
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            if (bv_sort.Equals(sort))
                return bv;

            uint k = ((BitVecSort)bv_sort).Size;
            uint m = ((BitVecSort)sort).Size;
            Expr res = null;
            if (k == m)
                res = bv;
            else if (m < k)
                res = z3.MkExtract(m - 1, 0, (BitVecExpr)bv);
            else
                res = z3.MkConcat((BitVecExpr)(z3.MkNumeral(0, z3.MkBitVecSort(m - k))), (BitVecExpr)bv);
            return res;
        }

        public Expr MkListCons(Expr first, Expr rest)
        {
            return MkApp(GetCons(GetSort(rest)), first, rest);
        }

        public Expr MkFirstOfList(Expr list)
        {
            return MkApp(GetFirst(GetSort(list)), list);
        }

        public Expr MkRestOfList(Expr list)
        {
            return MkApp(GetRest(GetSort(list)), list);
        }


        //IDictionary<Expr, IValue<Expr>> ISolver<FuncDecl, Expr, Sort>.GetModel(Expr assertion, params Expr[] termsToEvaluate)
        //{
        //    throw new NotImplementedException();
        //}

        public Expr MkCharGe(Expr x, Expr y)
        {
            Sort s = GetSort(x);
            if (s is BitVecSort)
                return z3.MkBVUGE((BitVecExpr)x, (BitVecExpr)y);
            else
                return z3.MkGe((ArithExpr)x, (ArithExpr)y);
        }

        public Expr MkCharGt(Expr x, Expr y)
        {
            Sort s = GetSort(x);
            if (s is BitVecSort)
                return z3.MkBVUGT((BitVecExpr)x, (BitVecExpr)y);
            else
                return z3.MkGt((ArithExpr)x, (ArithExpr)y);
        }

        public Expr MkCharLe(Expr x, Expr y)
        {
            Sort s = GetSort(x);
            if (s is BitVecSort)
                return z3.MkBVULE((BitVecExpr)x, (BitVecExpr)y);
            else
                return z3.MkLe((ArithExpr)x, (ArithExpr)y);
        }

        public Expr MkCharLt(Expr x, Expr y)
        {
            Sort s = GetSort(x);
            if (s is BitVecSort)
                return z3.MkBVULT((BitVecExpr)x, (BitVecExpr)y);
            else
                return z3.MkLt((ArithExpr)x, (ArithExpr)y);
        }

        public Expr MkCharAdd(Expr x, Expr y)
        {
            Sort s = GetSort(x);
            if (s is BitVecSort)
                return z3.MkBVAdd((BitVecExpr)x, (BitVecExpr)y);
            else
                return z3.MkAdd((ArithExpr)x, (ArithExpr)y);
        }

        public Expr MkCharSub(Expr x, Expr y)
        {
            Sort s = GetSort(x);
            if (s is BitVecSort)
                return z3.MkBVSub((BitVecExpr)x, (BitVecExpr)y);
            else
                return z3.MkSub((ArithExpr)x, (ArithExpr)y);
        }

        public Expr MkCharMul(Expr x, Expr y)
        {
            Sort s = GetSort(x);
            if (s is BitVecSort)
                return z3.MkBVMul((BitVecExpr)x, (BitVecExpr)y);
            else
                return z3.MkMul((ArithExpr)x, (ArithExpr)y);
        }

        /// <summary>
        /// Create the term extracting the ASCII code of the k'th decimal digit of 
        /// the decimal integer representation of character term c.
        /// For example MkDec(3,43210) is equivalent to 3.
        /// </summary>
        public Expr MkDec(int k, Expr c)
        {
            Sort s = GetSort(c);
            if (k == 0)
                return MkDecLSB(c, s);
            else
            {
                int n = 10;
                for (int i = 0; i < k - 1; k++)
                    n = n * n;
                return MkDecOther(n, c, s);
            }
        }

        Expr MkDecLSB(Expr c, Sort s)
        {
            if (!(s is BitVecSort))
            {
                var res = z3.MkBVURem((BitVecExpr)c, (BitVecExpr)MkNumeral(10, s));
                var res1 = z3.MkBVAdd(res, (BitVecExpr)MkNumeral(48, s));
                var res2 = Simplify(res1);
                return res2;
            }
            else
            {
                var res = z3.MkRem((IntExpr)c, (IntExpr)MkNumeral(10, s));
                var res1 = z3.MkAdd(res, (IntExpr)MkNumeral(48, s));
                var res2 = Simplify(res1);
                return res2;
            }

        }

        public Expr MkCharRem(Expr x, Expr y)
        {
            if (x.Sort is BitVecSort)
            {
                return z3.MkBVURem((BitVecExpr)x, (BitVecExpr)y);
            }
            else if (x.Sort is IntSort) {
                return this.MkRem(x,y);
            }
            else {
                throw new AutomataException(AutomataExceptionKind.InternalError);
            }
        }

        public Expr MkCharDiv(Expr x, Expr y)
        {
            if ((x.Sort is BitVecSort))
            {
                return z3.MkBVUDiv((BitVecExpr)x, (BitVecExpr)y);
            }
            else if ((x.Sort is IntSort))
            {
                return this.MkDiv(x, y);
            }
            else
            {
                throw new AutomataException(AutomataExceptionKind.InternalError);
            }
        }

        /// <summary>
        /// Get the Least Significant Half-Byte (LSHB) from the character.
        /// </summary>
        public Expr MkCharLSHB(Expr c)
        {
            Sort s = GetSort(c);
            if (!(s is BitVecSort))
                return z3.MkBVAND((BitVecExpr)c, (BitVecExpr)z3.MkNumeral(15, s));
            else
                return z3.MkRem((IntExpr)c, (IntExpr)z3.MkNumeral(16, s));
        }


        Expr MkDecOther(int n, Expr c, Sort s)
        {
            if (!(s is BitVecSort))
            {
                var res = z3.MkBVURem(z3.MkBVUDiv((BitVecExpr)c, (BitVecExpr)MkNumeral(n, s)), (BitVecExpr)MkNumeral(10, s));
                var res1 = z3.MkBVAdd(res, (BitVecExpr)MkNumeral(48, s));
                var res2 = Simplify(res1);
                return res2;
            }
            else
            {
                var res = z3.MkRem((IntExpr)z3.MkDiv((IntExpr)c, (IntExpr)MkNumeral(n, s)), (IntExpr)MkNumeral(10, s));
                var res1 = z3.MkAdd(res, (IntExpr)MkNumeral(48, s));
                var res2 = Simplify(res1);
                return res2;
            }
        }

        public char GetCharValue(Expr t)
        {
            if(t is IntNum)
                return (char)((IntNum)t).Int;
            if(t is BitVecNum)
                return (char)((BitVecNum)t).Int;
            throw new AutomataException("Wrong expr");
        }

        #endregion

        #region IBooleanAlgebra<Expr> Members

        /// <summary>
        /// Given an array of satisfiable Boolean terms {c_1, c_2, ..., c_n} where n>0.
        /// Enumerate all satisfiable Boolean combinations Tuple({b_1, b_2, ..., b_n}, c)
        /// where c is satisfisable and equivalent to c'_1 and c'_2 and ... and c'_n, 
        /// where c'_i = c_i if b_i = true and c'_i is Not(c_i) otherwise.
        /// </summary>
        /// <param name="conds">nonempty array of Boolean terms</param>
        /// <returns>Booolean combinations that are satisfiable</returns>
        public IEnumerable<Tuple<bool[], Expr>> GenerateMintermsWithCubes(params Expr[] conds)
        {
            int n = conds.Length;
            var z3p = this;
            if (n ==0 )
                yield return new Tuple<bool[], Expr>(new bool[0] {}, True);
            else if (n == 1)
                yield return new Tuple<bool[], Expr>(new bool[1] {true}, conds[0]);
            else
            {
                var solutions = new List<bool[]>();
                //collect all the satisfiable combinations of all the conditions into solutions

                z3p.MainSolver.Push(); //push a new context for asserting the cube of the conditions
                //var tmp = z3p.MkConst("$charConst$", charSort);

                Expr[] conds1 = (z3p.MkAndDoNotSimplify(conds)).Args;
                if (conds1.Length != conds.Length)
                    throw new Exception("Internal error");

                var bools = new Expr[n];
                var eqs = new Expr[n];
                for (int i = 0; i < n; i++)
                {
                    bools[i] = z3p.MkConst("b#" + i, z3p.BoolSort);
                    eqs[i] = z3p.MkEq(bools[i], conds1[i]);
                }

                var cube = z3p.MkAnd(eqs);

                z3p.MainSolver.Assert(cube); //assert the cube

                z3p.MainSolver.Push(); //push a new context for checking the first solution
                Model model;
                Status checkRes = z3p.solver.CheckAndGetModel(out model);
                while (checkRes != Status.UNSATISFIABLE)
                {
                    var solution = Array.ConvertAll(bools, c => model.Eval(c,true).Equals(z3p.True) ? true : false);
                    model.Dispose();
                    z3p.MainSolver.Pop(); //pop the context for the solution
                    solutions.Add(solution);
                    Expr[] negated = new Expr[n];
                    for (int i = 0; i < n; i++)
                        negated[i] = (solution[i] ? z3p.MkNot(bools[i]) : bools[i]);
                    z3p.MainSolver.Assert(z3p.MkOr(negated)); //exclude this solution by asserting the negation
                    z3p.MainSolver.Push(); //push a new context for checking the next solution
                    checkRes = z3p.solver.CheckAndGetModel(out model);
                }
                z3p.MainSolver.Pop(); //pop the context of the final (failing) check
                z3p.MainSolver.Pop(); //pop the context where the cube was asserted
                foreach (var solution in solutions)
                {
                    Expr[] solConds = new Expr[n];
                    for (int i = 0; i < n; i++)
                        solConds[i] = (solution[i] ? conds[i] : z3p.MkNot(conds[i]));
                    yield return
                        new Tuple<bool[], Expr>(solution, z3p.MkAnd(solConds));
                }
            }
        }


        MintermGenerator<Expr> mtg = null;
        MintermGenerator<Expr> MTG
        {
            get
            {
                if (mtg == null)
                    mtg = new MintermGenerator<Expr>(this);
                return mtg;
            }
        }
        public IEnumerable<Tuple<bool[], Expr>> GenerateMinterms(params Expr[] conds)
        {
            return GenerateMintermsWithCubes(conds);
        }
        
        #endregion

        #region IBoolAlg<Expr> Members


        public Expr MkOr(Expr predicate1, Expr predicate2)
        {
            if (predicate1.Equals(predicate2))
                return predicate1;
            if (predicate1.Equals(False))
                return predicate2;
            if (predicate2.Equals(False))
                return predicate1;
            if (predicate1.Equals(True))
                return True;
            return z3.MkOr((BoolExpr)predicate1, (BoolExpr)predicate2);
        }

        public Expr MkAnd(Expr predicate1, Expr predicate2)
        {
            if (predicate1.Equals(predicate2))
                return predicate1;
            if (predicate1.Equals(True))
                return predicate2;
            if (predicate2.Equals(True))
                return predicate1;
            if (predicate1.Equals(False))
                return False;
            return z3.MkAnd((BoolExpr)predicate1, (BoolExpr)predicate2);
        }

        public bool AreEquivalent(Expr predicate1, Expr predicate2)
        {
            if (predicate1.Equals(predicate2))
                return true;
            return !IsSatisfiable(MkNeq(predicate1, predicate2));
        }

        #endregion

        #region ICharacterConstraintSolver<Expr> Members

        public BitWidth Encoding
        {
            get
            {
                return encoding;
            }
        }

        public Expr MkRangeConstraint(char lower, char upper, bool caseInsensitive = false)
        {
            //ignoring case for now
            if (lower == upper)
                return MkCharConstraint( lower, caseInsensitive);
            return MkAnd(MkCharLe(MkNumeral((int)lower, CharacterSort), MkVar(0, CharacterSort)),
                MkCharLe(MkVar(0, CharacterSort), MkNumeral((int)upper, CharacterSort)));
        }

        public Expr MkCharConstraint( char c, bool caseInsensitive = false)
        {
            return MkEq(MkVar(0, CharacterSort), MkNumeral((int)c, CharacterSort));
        }

        public Expr MkRangesConstraint(bool caseInsensitive, IEnumerable<char[]> ranges)
        {
            List<Expr> terms = new List<Expr>();
            foreach (var r in ranges)
                terms.Add(MkRangeConstraint(r[0], r[1], caseInsensitive));
            Expr constr = MkOr(terms);
            return constr;
        }

        #endregion

        #region ICharArithm<Expr,Sort> Members

        /// <summary>
        /// wrapper
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public Expr MkCharExpr(char c)
        {
            return z3.MkNumeral((int)c, CharSort);
        }

        /// <summary>
        /// wrapper
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Expr MkCharVar(uint id)
        {
            return MkVar(id, CharSort);
        }

        /// <summary>
        /// wrapper
        /// </summary>
        public Expr CharVar { get { return MkCharVar(0); } }

        /// <summary>
        /// wrapper
        /// </summary>
        public Sort CharSort
        {
            get
            {
                switch (encoding)
                {
                    case BitWidth.BV7: return MkBitVecSort(7);
                    case BitWidth.BV8: return MkBitVecSort(8);
                    case BitWidth.BV16: return MkBitVecSort(16);
                    case BitWidth.BV32: return MkBitVecSort(32);
                    default:
                        throw new AutomataException(AutomataExceptionKind.CharacterEncodingIsUnspecified);
                }
            }
        }

        #endregion


        #region CharSet to Expr conversion

        Dictionary<BDD, Expr> bddToExprCache = new Dictionary<BDD, Expr>();

        /// <summary>
        /// Make a maximally shared ITE-term that encodes the given character set bdd by using Shannon expansions.
        /// Uses MkCharVar(0) as the variable.
        /// </summary>
        public Expr ConvertFromCharSet(BDD set) 
        {
            return MkIteExprFromBdd(set);
        }

        Dictionary<Expr, BDD> _ConvertToCharSet_cache = new Dictionary<Expr, BDD>();

        /// <summary>
        /// Assumes that the condition is satisfiable and if not True has a single free variable CharVar.
        /// </summary>
        public BDD ConvertToCharSet(Expr cond)
        {
            return ConvertToCharSet(cond, characterSort);
        }

        /// <summary>
        /// Assumes that the condition is satisfiable and if not True has a single free variable CharVar.
        /// </summary>
        public BDD ConvertToCharSet(Expr cond, Sort sort)
        {
            var vars = new List<Expr>(GetVars(cond));
            if (vars.Count==0)
                return this.CharSetProvider.True;
            if (vars.Count > 1)
                throw new AutomataException("There can be at most one free variable");

            BDD res;
            if (_ConvertToCharSet_cache.TryGetValue(cond, out res))
                return res;

            res = FindInInterval(cond, vars[0], 0, ((long)1 << ((int)Encoding))-(long)1,sort);
            _ConvertToCharSet_cache[cond] = res;
            return res;
        }

        private BDD FindInInterval(Expr cond, Expr variable, long l, long u, Sort sort)
        {
            var lower = MkNumeral(l, sort);
            var upper = MkNumeral(u, sort);

            var range = MkAnd(MkCharLe(lower, variable), MkCharLe(variable, upper));
            if(sort!=characterSort)
                range = MkAnd(MkLe(lower, variable), MkLe(variable, upper));


            var range_but_cond = MkAnd(range, MkNot(cond));
            var range_and_cond = MkAnd(range, cond);
            if (!IsSatisfiable(range_but_cond))
                return this.CharSetProvider.MkRangeConstraint( (char)l, (char)u);
            else if (!IsSatisfiable(range_and_cond))
                return this.CharSetProvider.False;
            else
            {
                var v1 = FindInInterval(cond, variable, l, (l + u) / 2,sort);
                var v2 = FindInInterval(cond, variable, ((l + u) / 2) + 1, u, sort);
                return this.CharSetProvider.MkOr(v1, v2);
            }
        }

        /// <summary>
        /// Make an ITE term that encodes the given bdd
        /// </summary>
        private Expr MkIteExprFromBdd(BDD bdd)
        {
            if (bdd.IsFull)
                return True;
            if (bdd.IsEmpty)
                return False;

            Expr res;
            if (bddToExprCache.TryGetValue(bdd, out res))
                return res;

            res = MkIte(MkBitIsZero(bdd.Ordinal), MkIteExprFromBdd(bdd.Zero), MkIteExprFromBdd(bdd.One));
            bddToExprCache[bdd] = res;
            return res;
        }

        private Expr MkBitIsZero(int bitNr)
        {
            int bitMap = (1 << bitNr);
            Expr bitMapExpr = MkNumeral(bitMap, CharSort);
            Expr zero = z3.MkNumeral(0, CharSort);
            if (CharSort.Equals(IntSort))
                throw new NotImplementedException("TBD: Bdd to term conversion using integers");

            Expr cond = MkAnd(MkEq(MkBvAnd(MkCharVar(0), bitMapExpr), zero)); 
            return cond;
        }

        private Expr MkBitIsOne(int ord)
        {
            int bitNr = (CharacterEncodingTool.Truncate(Encoding)) - ord - 1;
            int bitMap = (1 << bitNr);
            Expr bitMapExpr = MkNumeral(bitMap, CharSort);
            if (CharSort.Equals(IntSort))
                throw new NotImplementedException("TBD: Bdd to term conversion using integers");

            Expr cond = MkAnd(MkEq(MkBvAnd(MkCharVar(0), bitMapExpr), bitMapExpr));
            return cond;
        }

        #endregion
  
        
        public Expr MkCharPredicate(string predName, Expr body)
        {
            FuncDecl pred;
            if (!declaredFuncDecls.TryGetFuncDecl(out pred, predName, CharacterSort))
                pred = MkFuncDecl(predName, CharSort, BoolSort);

            var lhs = MkApp(pred, MkCharVar(0));
            var axiom = MkEqForall(lhs, body, MkCharVar(0));
            MainSolver.Assert(axiom);
            return lhs;
        }

        #region chooser

        /// <summary>
        /// Gets the chooser of the z3 provider.
        /// </summary>
        public Chooser Chooser
        {
            get { return CharSetProvider.Chooser; }
        }

        #endregion

        #region IPrettyPrinter<Expr> Members

        ExprPrettyPrinter tpp;

        public string PrettyPrint(Expr t)
        {
            return tpp.DescribeExpr(t);
        }

        public string PrettyPrint2(Expr t, Func<Expr, string> lookupVarName)
        {
            tpp.compactview = true;
            tpp.__lookupVarName = lookupVarName;
            string res = tpp.DescribeExpr(t);
            tpp.compactview = false;
            tpp.__lookupVarName = null;
            return res;
        }

        /// <summary>
        /// Use lookup to determine if a term is given a particular presentation.
        /// </summary>
        public string PrettyPrint(Expr t, Func<Expr, string> varLookup)
        {
            return tpp.DescribeExpr(t, varLookup);
        }

        /// <summary>
        /// Use lookup to determine if a term is given a particular presentation.
        /// </summary>
        public string PrettyPrintCS(Expr t, Func<Expr, string> varLookup)
        {
            return tpp.DescribeExprCS(t, varLookup);
        }

        #endregion

        public Expr MkBvExtract(uint high, uint low, Expr bv)
        {
            return z3.MkExtract(high, low, (BitVecExpr)bv);
        }

        #region ITupleTheory<Expr,Sort> Members


        public Sort GetTupleElementSort(Sort tupleSort, int p)
        {
            return GetRange(GetTupleField(tupleSort, p));
        }

        #endregion

        /// <summary>
        /// Undelying Z3 context. For advanced use only.
        /// </summary>
        public Context Z3
        {
            get { return z3; }
        }

        /// <summary>
        /// Undelying Z3 context. For advanced use only.
        /// </summary>
        public Solver Z3S
        {
            get { return solver.Solver; }
        }

        Dictionary<Expr, Expr> _RewriteExpr_cache = new Dictionary<Expr, Expr>();
        Func<Expr, Expr> _ExprRewriter = null;
        /// <summary>
        /// Rewrite the subterms using the rewriter. If the subterm rewriter returns null, then, if the
        /// term is an App term then the subterm rewriter is applied recursively to the subterms of the term, otherwise 
        /// kept as is. Identical subterms are cached and only rewritten once.
        /// </summary>
        /// <param name="term">term to be rewritten</param>
        /// <param name="SubtermRewriter">subterm rewriter</param>
        public Expr RewriteExpr(Expr term, Func<Expr, Expr> SubtermRewriter)
        {
            _ExprRewriter = SubtermRewriter;
            Expr res = RewriteExpr1(term);
            _RewriteExpr_cache.Clear();
            _ExprRewriter = null;
            return res;
        }
        Expr RewriteExpr1(Expr term)
        {
            Expr res;
            if (!_RewriteExpr_cache.TryGetValue(term, out res))
            {
                res = _ExprRewriter(term);
                if (res == null)
                {
                    var kind = term.ASTKind;
                    if (kind == Z3_ast_kind.Z3_APP_AST)
                    {
                        var args = Array.ConvertAll(term.Args, RewriteExpr1);
                        res = MkApp(term.FuncDecl, args);
                    }
                    else
                        res = term;
                }
                _RewriteExpr_cache[term] = res;
            }
            return res;
        }

        /// <summary>
        /// Try to convert the given predicate into a set of characters.
        /// </summary>
        /// <param name="pred">given predicate may contain at most one variable that must be a character variable</param>
        /// <param name="set">equivalent set</param>
        public bool TryConvertToCharSet(Expr pred, out BDD set)
        {
            if (!pred.Sort.Equals(this.BoolSort))
            {
                set = null;
                return false;
            }

            var vars = new List<Expr>(GetVars(pred));

            if (vars.Count == 0 || (vars.Count == 1 && vars[0].Equals(CharVar)))
            {
                set = ConvertToCharSet(pred);
                return true;
            }
            else
            {
                set = null;
                return false;
            }

        }

        public Automaton<BDD> ConvertAutomatonGuardsFromExpr(Automaton<Expr> aut)
        {
            var aut1 = Automaton<BDD>.Create(this.CharSetProvider, aut.InitialState, aut.GetFinalStates(), ConvertMoves(aut.GetMoves()));
            aut1.isDeterministic = aut.isDeterministic;
            aut1.isEpsilonFree = aut.isEpsilonFree;
            return aut1;
        }

        public Automaton<Expr> ConvertAutomatonGuardsToExpr(Automaton<BDD> aut) 
        {
            var aut1 = Automaton<Expr>.Create(this, aut.InitialState, aut.GetFinalStates(), ConvertMoves2(aut.GetMoves()));
            aut1.isDeterministic = aut.isDeterministic;
            aut1.isEpsilonFree = aut.isEpsilonFree;
            return aut1;
        }

        private IEnumerable<Move<BDD>> ConvertMoves(IEnumerable<Move<Expr>> moves)
        {
            foreach (var move in moves)
            {
                if (move.IsEpsilon)
                    yield return Move<BDD>.Epsilon(move.SourceState,move.TargetState);
                else{
                    BDD cond;
                    if (TryConvertToCharSet(move.Label, out cond))
                    {
                        yield return Move<BDD>.Create(move.SourceState, move.TargetState, cond);
                    }
                    else
                    {
                        throw new AutomataException(AutomataExceptionKind.LabelIsNotConvertableToBvSet);
                    }
                }
            }
        }

        private IEnumerable<Move<Expr>> ConvertMoves2(IEnumerable<Move<BDD>> moves)
        {
            foreach (var move in moves)
            {
                if (move.IsEpsilon)
                    yield return Move<Expr>.Epsilon(move.SourceState, move.TargetState);
                else
                {
                    Expr cond = ConvertFromCharSet(move.Label);
                    yield return Move<Expr>.Create(move.SourceState, move.TargetState, cond);
                }
            }
        }

        /// <summary>
        /// Gets the interface to the library.
        /// </summary>
        public ILibrary<Expr> Library
        {
            get { return library; }
        }

        public Expr MkSet(uint e)
        {
            var num = MkNumeral(e, CharacterSort);
            var set = MkEq(CharVar, num);
            return set;
        }

        public uint Choose(Expr s)
        {
            throw new NotImplementedException();
        }

        #region ILifted
        /// <summary>
        /// Make a list sort with the default name 'List' and given element sort
        /// </summary>
        Dictionary<Sort, DatatypeSort> liftedSorts = new Dictionary<Sort, DatatypeSort>();
        public Sort MkOptionSort(Sort elemSort)
        {
            DatatypeSort ls;
            if (liftedSorts.TryGetValue(elemSort, out ls))
                return ls;

            var some = z3.MkConstructor("Some", "IsSome", new string[] { "Extract" }, new Sort[] { elemSort });
            var none = z3.MkConstructor("None", "IsNone");

            ls = z3.MkDatatypeSort("Lifted", new Constructor[] { some, none });

            liftedSorts[elemSort] = ls;
            return ls;
        }

        public Expr MkNone(Sort liftedSort)
        {
            DatatypeSort ls = (DatatypeSort)liftedSort;
            var none = z3.MkApp(ls.Constructors[1]);
            return none;
        }

        public Sort GetOptionValueSort(Sort liftedSort)
        {
            DatatypeSort ls = (DatatypeSort)liftedSort;
            var baseSort = ls.Constructors[0].Domain[0];
            return baseSort;
        }

        public Expr MkSome(Expr t)
        {
            var s = t.Sort;
            DatatypeSort ls = (DatatypeSort)MkOptionSort(t.Sort);
            var some = ls.Constructors[0];
            var res = z3.MkApp(some, t);
            return res;
        }

        public Expr MkIsSome(Expr lifted)
        {
            var ls = (DatatypeSort)lifted.Sort;
            var is_some = ls.Recognizers[0];
            var res = z3.MkApp(is_some, lifted);
            return res;
        }

        public Expr MkIsNone(Expr lifted)
        {
            var ls = (DatatypeSort)lifted.Sort;
            var is_none = ls.Recognizers[1];
            var res = z3.MkApp(is_none, lifted);
            return res;
        }

        public Expr MkGetSomeValue(Expr lifted) 
        {
            var ls = (DatatypeSort)lifted.Sort;
            if (lifted.ASTKind == Z3_ast_kind.Z3_APP_AST && lifted.FuncDecl.Equals(ls.Constructors[0]))
            {
                var unwrapped = lifted.Args[0];
                return unwrapped;
            }
            var extract = ls.Accessors[0][0];
            var extracted = z3.MkApp(extract, lifted);
            return extracted;
        }

        public bool IsOption(Sort sort)
        {
            DatatypeSort ds = sort as DatatypeSort;
            if (ds == null)
                return false;
            if (liftedSorts.ContainsValue(ds))
                return true;
            else
                return false;
        }
        #endregion

        public Expr MkExists(Expr body, params Expr[] vars)
        {
            return Z3.MkExists(vars, body);
        }

        /// <summary>
        /// Make a new solver. The new solver has no assertions.
        /// </summary>
        /// <param name="logic">given logic</param>
        public ISolver<Expr> MkSolver(string logic = null)
        {
            if (logic == null)
                return new Z3Solver(z3.MkSolver(), this);
            else
                return new Z3Solver(z3.MkSolver(logic), this);
        }

        /// <summary>
        /// Main solver of the context.
        /// </summary>
        public ISolver<Expr> MainSolver
        {
            get { return solver; }
        }


        public bool IsExtensional
        {
            get { return false; }
        }

        public Expr MkSymmetricDifference(Expr p1, Expr p2)
        {
            return MkNeq(p1, p2);
        }

        public bool CheckImplication(Expr lhs, Expr rhs)
        {
            return !IsSatisfiable(MkAnd(lhs,MkNot(rhs)));
        }

        public bool IsAtomic
        {
            get { return false; }
        }

        public Expr GetAtom(Expr psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
            //var v = this.solver.FindOneMember(psi);
            //if (v == null)
            //    return False;

            //Expr atom = MkEq(MkVar(0, GetSort(v.Value)), v.Value);
            //return atom;
        }

        public bool EvaluateAtom(Expr atom, Expr psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }


        public Expr MkDiff(Expr predicate1, Expr predicate2)
        {
            return MkAnd(predicate1, MkNot(predicate2));
        }

        public char ChooseUniformly(Expr s)
        {
            throw new NotSupportedException();
        }

        public ulong ComputeDomainSize(Expr set)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<char> GenerateAllCharacters(Expr set)
        {
            throw new NotImplementedException();
        }

        public BDD ConvertToCharSet(IBDDAlgebra solver, Expr pred)
        {
            return null;
        }

        public Expr[] GetPartition()
        {
            throw new NotImplementedException();
        }

        public string SerializePredicate(Expr s)
        {
            throw new NotImplementedException();
        }

        public Expr DeserializePredicate(string s)
        {
            throw new NotImplementedException();
        }
    }
}

